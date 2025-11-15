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
    Red = 18,
    Green = 46,
    Lila = 48,
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

    private const string ChatTag = "BetterMountRoulette";

    // todo pick different color
    private const ushort ChatTagColor = (ushort)ColorMap.Lila;

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

        CommandManager.AddHandler("/bmr", new CommandInfo(OnCallMountCommand)
        {
            HelpMessage =
                "Calls a random mount from a list. /bmr will use the default use. To use mount from your custom list use /bmr listName"
        });
        CommandManager.AddHandler("/bmr-delete-all-lists", new CommandInfo(OnDeleteAllListsCommand)
        {
            HelpMessage = "Clears all lists."
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
        CommandManager.AddHandler("/bmr-clear-list",
                                  new CommandInfo(OnClearListCommand)
                                  {
                                      HelpMessage =
                                          "Clear mount list, resetting it to an empty list. Usage like /bm-clear-list myName"
                                  });
        CommandManager.AddHandler("/bmr-delete-list", new CommandInfo(OnDeleteListCommand)
        {
            HelpMessage = "Deletes a mount list. Usage like /bm-delete-list myName"
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
        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
    }

    private void OnAddWhitelistCommand(string command, string args)
    {
        var newMountListName = args.Trim();
        if (newMountListName.Length == 0)
        {
            ChatGui.PrintError(
                "You need to specify a name for your new mount list. Add it like this: /bmr-add-whitelist myName",
                ChatTag, ChatTagColor);

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
                "You need to specify a name for your new mount list. Add it like this: /bmr-add-blacklist myName",
                ChatTag, ChatTagColor);

            return;
        }

        this.CreateNewMountList(newMountListName, MountListType.Blacklist);
    }


    private void CreateNewMountList(string mountListName, MountListType mountListType)
    {
        if (Configuration.GetMountList(mountListName) != null)
        {
            ChatGui.PrintError($"Mount list with the name \"{mountListName}\" already exists!", ChatTag, ChatTagColor);
        }

        Configuration.StoreMountList(new MountList()
        {
            Name = mountListName,
            Type = MountListType.Whitelist,
        });

        ChatGui.Print($"Your new list \"{mountListName}\" was created.", ChatTag, ChatTagColor);
    }

    private void OnDeleteAllListsCommand(string command, string args)
    {
        Configuration.ClearMountList();

        ChatGui.Print("All lists were removed.", ChatTag, ChatTagColor);
    }

    private void OnClearListCommand(string command, string args)
    {
        var listName = args.Trim();

        if (listName.Length == 0)
        {
            ChatGui.PrintError(
                "You need to specify a name for the list do clear. For example: /bmr-clear-list myName",
                ChatTag, ChatTagColor);

            return;
        }

        if (Configuration.GetMountList(listName) is not { } mountList)
        {
            ChatGui.PrintError($"No mount list found for the name \"{listName}\"", ChatTag, ChatTagColor);

            return;
        }

        Configuration.CleanMountList(mountList);

        ChatGui.Print($"List \"{mountList.Name}\" was cleared.", ChatTag, ChatTagColor);
    }

    private void OnDeleteListCommand(string command, string args)
    {
        var listName = args.Trim();

        if (listName.Length == 0)
        {
            ChatGui.PrintError(
                "You need to specify a name for the list do delete. For example: /bmr-delete-list myName",
                ChatTag, ChatTagColor);

            return;
        }

        if (Configuration.GetMountList(listName) is not { } mountList)
        {
            ChatGui.PrintError($"No mount list found for the name \"{listName}\"", ChatTag, ChatTagColor);

            return;
        }

        Configuration.CleanMountList(mountList);

        ChatGui.Print($"List \"{mountList.Name}\" was deleted.", ChatTag, ChatTagColor);
    }


    private void OnCallMountCommand(string command, string args)
    {
        var listName = args.Trim();

        var mountList = listName.Length > 0
                            ? Configuration.GetMountList(listName)
                            : Configuration.GetOrCreateDefaultMountList();

        if (mountList == null)
        {
            ChatGui.PrintError($"No mount list found for the name \"{listName}\"", ChatTag, ChatTagColor);

            return;
        }

        var mountIdsForShuffle = this.GetAvailableMountsForList(mountList);

        if (mountIdsForShuffle.Count == 0)
        {
            ChatGui.PrintError("No relevant mounts found for the list.", ChatTag, ChatTagColor);

            return;
        }

        var randomNumber = Random.Shared.Next(mountIdsForShuffle.Count);
        var mountIdToMount = mountIdsForShuffle[randomNumber];

        if (GetMount(mountIdToMount) is not { } mount)
        {
            ChatGui.PrintError("Unexpected error occured: Mount not found by id", ChatTag, ChatTagColor);

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
            Log.Error($"Mount with order {mountOrderId} not found");

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

    private MenuItem MountListToMenuItem(MountList mountList, Mount mount)
    {
        var mountId = mount.RowId;
        var mountName = mount.Singular.ExtractText();

        var isMountIdInList = mountList.MountIds.Contains(mountId);

        /*
        var colors = DataManager.GetExcelSheet<UIColor>();

        foreach (UIColor color in colors)
        {
            ChatGui.Print($"Dark {color.RowId} {color.Dark:X}", "Some Tag", (ushort) color.RowId);
        }
        */

        var isCurrentlySummonedInList = mountList.Type == MountListType.Whitelist ? isMountIdInList : !isMountIdInList;

        string namePrefix;
        SeIconChar prefixChar;
        ColorMap prefixColor;

        if (isCurrentlySummonedInList)
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
                    ChatGui.Print($"Added #{mountId} {mountName} to list {mountList.Name}", ChatTag, ChatTagColor);
                }
                else
                {
                    mountList.MountIds.Remove(mountId);
                    ChatGui.Print($"Removed #{mountId} {mountName} from list {mountList.Name}", ChatTag, ChatTagColor);
                }

                Configuration.Save();
            },
        };
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();

    public void ToggleMainUi() => MainWindow.Toggle();
}
