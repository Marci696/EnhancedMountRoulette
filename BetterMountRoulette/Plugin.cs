using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Linq;
using BetterMountRoulette.Windows;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using Lumina;
using Lumina.Extensions;


namespace BetterMountRoulette;

enum MenuItemAction
{
    Add,
    Remove
}

// Maps to UIColor RowId
enum ColorMap
{
    Green = 46,
    Red = 18,
}

public sealed class Plugin : IDalamudPlugin
{
    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ITextureProvider TextureProvider { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    internal static IDataManager DataManager { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;

    [PluginService]
    internal static IChatGui ChatGui { get; private set; } = null!;

    [PluginService]
    internal static IContextMenu ContextMenu { get; private set; } = null!;

    [PluginService]
    internal static IGameGui GameGui { get; private set; } = null!;

    private const string CommandName = "/pmbmroulette";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("BetterMountRoulette");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    private unsafe AgentMountNoteBook* AgentMountNoteBook;

    public Plugin()
    {
        // TODO remove once no longer custom xiv struct version
        InteropGenerator.Runtime.Resolver.GetInstance.Setup();
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        InteropGenerator.Runtime.Resolver.GetInstance.Resolve();

        unsafe
        {
            this.AgentMountNoteBook = (AgentMountNoteBook*)GameGui.GetAgentById((int)AgentId.MountNotebook).Address;
        }

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        // todo better way to ensure default exists
        Configuration.GetOrCreateDefaultMountList();

        // You might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        // TODO replace
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        CommandManager.AddHandler("/better-mount-blacklist-add", new CommandInfo(OnBlacklistCommand)
        {
            HelpMessage = "Blacklist a mount by name"
        });
        CommandManager.AddHandler("/better-mount-blacklist-remove", new CommandInfo(OnBlacklistRemoveCommand)
        {
            HelpMessage = "Remove a mount from blacklist"
        });
        CommandManager.AddHandler("/bmr-clear", new CommandInfo(OnClearCommand)
        {
            HelpMessage = "Clears all lists."
        });
        CommandManager.AddHandler("/better-mount-blacklist-current", new CommandInfo(OnCurrentBlacklistCommand)
        {
            HelpMessage = "Show currently blacklisted mounts"
        });
        CommandManager.AddHandler("/bmr", new CommandInfo(OnCallMountCommand)
        {
            HelpMessage =
                "Calls a random mount from a list. /bmr will use the default use. To use mount from your custom list use /bmr listName"
        });
        CommandManager.AddHandler("/bmr-add-whitelist",
                                  new CommandInfo(OnAddWhitelistCommand)
                                  {
                                      HelpMessage =
                                          "Add a new mount whitelist. Specify a name by calling it like /bmr-add-whitelist myName"
                                  });
        CommandManager.AddHandler("/bmr-add-blacklist",
                                  new CommandInfo(OnAddBlacklistCommand)
                                  {
                                      HelpMessage =
                                          "Add a new mount blacklist. Specify a name by calling it like /bmr-add-blacklist myName"
                                  });

        // Tell the UI system that we want our windows to be drawn throught he window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        ContextMenu.OnMenuOpened += OnContextMenuOpened;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [BetterMountRoulette] ===A cool log message from Sample Plugin===
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anythign during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        Log.Debug($"Command: {command}, Args: {args}");

        var mount = GetMount(args);

        if (mount == null)
        {
            Log.Debug("No mount found by that name");
        }
        else
        {
            var isMountUnlocked = IsMountUnlocked(checked((int)mount.Value.RowId));

            Log.Debug(
                $" Mount \"{mount.Value.Singular.ExtractText()}\" with icon {mount.Value.Icon} is unlocked: {isMountUnlocked}");
        }

        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
    }

    private void OnAddWhitelistCommand(string command, string args)
    {
        var newMountListName = args.Trim();
        if (newMountListName.Length == 0)
        {
            ChatGui.PrintError(
                "You need to specify a name for your new mount list. Add it like this: /bmr-add-whitelist myName");

            return;
        }

        this.CreateNewMountList(newMountListName, MountListType.Whitelist);
    }

    private void OnAddBlacklistCommand(string command, string args)
    {
        var newMountListName = args.Trim();
        if (newMountListName.Length == 0)
        {
            ChatGui.PrintError(
                "You need to specify a name for your new mount list. Add it like this: /bmr-add-blacklist myName");

            return;
        }

        this.CreateNewMountList(newMountListName, MountListType.Blacklist);
    }


    private void CreateNewMountList(string mountListName, MountListType mountListType)
    {
        if (Configuration.GetMountList(mountListName) != null)
        {
            ChatGui.PrintError($"Mount list with the name \"{mountListName}\" already exists!");
        }

        Configuration.StoreMountList(new MountList()
        {
            Name = mountListName,
            Type = MountListType.Whitelist,
        });

        ChatGui.Print($"Your new list \"{mountListName}\" was created.");
    }

    private void OnBlacklistCommand(string command, string args)
    {
        var mount = GetMount(args);

        if (mount == null)
        {
            ChatGui.PrintError($"No mount found for the name \"{args}\"");

            return;
        }

        var mountId = mount.Value.RowId;

        return;

        /*Configuration.GetOrCreateDefaultMountList()?.BlacklistedIds.Add(mountId);
        Configuration.Save();

        ChatGui.Print($"\"{mount.Value.Singular.ExtractText()}\" was blacklisted");*/
    }

    private void OnBlacklistRemoveCommand(string command, string args)
    {
        var mount = GetMount(args);

        return;

        /*if (mount == null)
        {
            ChatGui.PrintError($"No mount found for the name \"{args}\"");

            return;
        }

        var mountId = mount.Value.RowId;

        Configuration.GetOrCreateDefaultMountList()?.BlacklistedIds.Remove(mountId);
        Configuration.Save();

        ChatGui.Print($"\"{mount.Value.Singular.ExtractText()}\" was removed from blacklist");*/
    }

    private void OnClearCommand(string command, string args)
    {
        Configuration.ClearMountList();

        ChatGui.Print("All lists were removed.");
    }

    private void OnCurrentBlacklistCommand(string command, string args)
    {
        var mountList = Configuration.GetOrCreateDefaultMountList();
        if (mountList == null)
        {
            return;
        }

        var blacklistedMounts = new List<string>();

        return;

        /*foreach (var mountId in mountList.BlacklistedIds)
        {
            var mount = GetMount(mountId);

            if (mount == null)
            {
                continue;
            }

            blacklistedMounts.Add(mount.Value.Singular.ExtractText());
        }
        ChatGui.Print("Current Blacklist: " + string.Join(", ", blacklistedMounts));*/
    }

    private void OnCallMountCommand(string command, string args)
    {
        var listName = args.Trim();

        var mountList = listName.Length > 0
                            ? Configuration.GetMountList(listName)
                            : Configuration.GetOrCreateDefaultMountList();

        if (mountList == null)
        {
            ChatGui.PrintError($"No mount list found for the name \"{listName}\"");

            return;
        }

        var mountIdsForShuffle = this.GetAvailableMountsForList(mountList);

        if (mountIdsForShuffle.Count == 0)
        {
            ChatGui.PrintError("No relevant mounts found for the list.");

            return;
        }

        var randomNumber = Random.Shared.Next(mountIdsForShuffle.Count);
        var mountIdToMount = mountIdsForShuffle[randomNumber];

        if (GetMount(mountIdToMount) is not { } mount)
        {
            ChatGui.PrintError("Unexpected error occured: Mount not found by id");

            return;
        }

        Log.Debug($"Trying to mount {mount.RowId} {mount.Singular.ExtractText()}");

        this.CallMount(mount);
    }

    private HashSet<uint> GetOwnedMountIds()
    {
        // TODO see if it can use memory hook to safe performance to not always check that;
        HashSet<uint> ownedMountIds = [];
        var mountSheet = DataManager.GetExcelSheet<Mount>();

        foreach (var mount in mountSheet)
        {
            unsafe
            {
                // Skip invalid mounts
                if (mount.Singular.IsEmpty || mount.Order < 0)
                    continue;

                var mountList = PlayerState.Instance();
                var mounts = mountList->UnlockedMountsBitArray;

                // Use mount.Order as the bit array index, not the row ID
                bool isMountUnlocked = mounts.Get(mount.Order);

                if (!isMountUnlocked)
                {
                    continue;
                }

                ownedMountIds.Add(mount.RowId);
            }
        }

        return ownedMountIds;
    }

    private List<uint> GetAvailableMountsForList(MountList mountList)
    {
        var ownedMountIds = GetOwnedMountIds();

        var ids = (mountList.Type == MountListType.Whitelist
                       ? ownedMountIds.Intersect(mountList.MountIds)
                       : ownedMountIds.Except(mountList.MountIds)).ToList();

        return ids;
    }

    private unsafe void CallMount(Mount mount)
    {
        ActionManager.Instance()->UseAction(ActionType.Mount, (uint)mount.RowId);
    }

    private void OnContextMenuOpened(IMenuOpenedArgs args)
    {
        if (args.AddonName != "MountNoteBook")
        {
            return;
        }

        uint mountOrderId;
        unsafe
        {
            mountOrderId = (uint)AgentMountNoteBook->SelectedId;
        }

        if (GetMount(mountOrderId) is not { } mount)
        {
            // TODO change to debug
            ChatGui.PrintError($"Mount with order {mountOrderId} not found");

            return;
        }

        foreach (var mountListType in Enum.GetValues<MountListType>())
        {
            args.AddMenuItem(new MenuItem()
            {
                Name = mountListType == MountListType.Whitelist
                           ? "---- Roulette WhiteLists: ----"
                           : "---- Roulette BlackLists: ----",
                IsEnabled = false,
                PrefixChar = 'R',
            });

            foreach (var mountList in Configuration.GetMountLists(mountListType))
            {
                args.AddMenuItem(this.MountListToMenuItem(mountList, mount));
            }
        }
    }

    private Mount? GetMount(string mountName)
    {
        var mountSheet = DataManager.GetExcelSheet<Mount>();

        return mountSheet
               // Sheet includes empty values.
               .Where((mount) => mount.Singular.ExtractText().Equals(mountName, StringComparison.OrdinalIgnoreCase))
               .FirstOrNull();
    }

    private Mount? GetMount(uint mountId)
    {
        var mountSheet = DataManager.GetExcelSheet<Mount>();

        var mount = mountSheet.GetRowOrDefault(mountId);

        if (!mount.HasValue)
        {
            return null;
        }

        if (mount.Value.Singular.IsEmpty)
        {
            return null;
        }

        return mount;
    }

    private Mount? GetMountByOrderId(uint orderId)
    {
        var mountSheet = DataManager.GetExcelSheet<Mount>();

        return mountSheet.FirstOrDefault(mount => mount.Order == orderId);
    }

    private bool IsMountUnlocked(int mountId)
    {
        unsafe
        {
            var mountList = PlayerState.Instance();

            var unlockedMounts = mountList->UnlockedMountsBitArray;

            return unlockedMounts.Get(mountId + 1);
        }
    }

    private MenuItem MountListToMenuItem(MountList mountList, Mount mount)
    {
        var mountId = mount.RowId;
        var mountName = mount.Singular.ExtractText();

        var isMountIdInList = mountList.MountIds.Contains(mountId);

        /*var colors = DataManager.GetExcelSheet<UIColor>();

        foreach (UIColor color in colors)
        {
            ChatGui.Print($"Dark {color.RowId} {color.Dark:X}", "Some Tag", (ushort) color.RowId);
            ChatGui.Print($"Light {color.RowId} {color.Light:X}", "Some Tag", (ushort) color.RowId);
            ChatGui.Print($"ClassicFF {color.RowId} {color.ClassicFF:X}", "Some Tag", (ushort) color.RowId);
            ChatGui.Print($"ClearBlue {color.RowId} {color.ClearBlue:X}", "Some Tag", (ushort) color.RowId);
        }*/

        string namePrefix;
        SeIconChar prefixChar;
        ColorMap prefixColor;

        if (mountList.Type == MountListType.Whitelist)
        {
            if (isMountIdInList)
            {
                namePrefix = "Ignore in";
                prefixChar = SeIconChar.Cross;
                prefixColor = ColorMap.Red;
            }
            else
            {
                namePrefix = "Summon in";
                prefixChar = SeIconChar.BoxedPlus;
                prefixColor = ColorMap.Green;
            }
        }
        else
        {
            if (isMountIdInList)
            {
                namePrefix = "Summon in";
                prefixChar = SeIconChar.BoxedPlus;
                prefixColor = ColorMap.Green;
            }
            else
            {
                namePrefix = "Ignore in";
                prefixChar = SeIconChar.Cross;
                prefixColor = ColorMap.Red;
            }
        }

        return new MenuItem()
        {
            Name = $"{namePrefix} {mountList.Name}",
            IsEnabled = true,
            Prefix = prefixChar,
            PrefixColor = (ushort)prefixColor,
            OnClicked = (_) =>
            {
                if (!isMountIdInList)
                {
                    mountList.MountIds.Add(mountId);
                    ChatGui.Print($"Added #{mountId} {mountName} to list {mountList.Name}");
                }
                else
                {
                    mountList.MountIds.Remove(mountId);
                    ChatGui.Print($"Removed #{mountId} {mountName} from list {mountList.Name}");
                }

                Configuration.Save();
            },
        };
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();

    public void ToggleMainUi() => MainWindow.Toggle();
}
