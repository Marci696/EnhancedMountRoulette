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
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina;
using Lumina.Extensions;


namespace BetterMountRoulette;

enum MenuItemAction
{
    Add,
    Remove
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
        CommandManager.AddHandler("/better-mount-blacklist-clear", new CommandInfo(OnClearBlacklistCommand)
        {
            HelpMessage = "Clear blacklist"
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

    private void OnClearBlacklistCommand(string command, string args)
    {
        return;

        /*Configuration.GetOrCreateDefaultMountList()?.BlacklistedIds.Clear();
        Configuration.Save();

        ChatGui.Print("Blacklist cleared");*/
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

        var randomNumber = Random.Shared.Next(mountIdsForShuffle.Count);
        var mountIdToMount = mountIdsForShuffle[randomNumber];

        this.Mount(mountIdToMount);
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

        return (mountList.Type == MountListType.Whitelist
                    ? ownedMountIds.Intersect(mountList.MountIds)
                    : ownedMountIds.Except(mountList.MountIds)).ToList();
    }

    private unsafe void Mount(uint mountId)
    {
        ActionManager.Instance()->UseAction(ActionType.Mount, mountId);
    }

    private void OnContextMenuOpened(IMenuOpenedArgs args)
    {
        if (args.AddonName != "MountNoteBook")
        {
            return;
        }

        uint mountId;
        unsafe
        {
            mountId = (uint)AgentMountNoteBook->SelectedId;
        }

        args.AddMenuItem(new MenuItem() { IsEnabled = false, Name = "----------- Roulette -----------" });


        foreach (MenuItemAction action in Enum.GetValues<MenuItemAction>())
        {
            args.AddMenuItem(this.CreateMenuItem(action, mountId));
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

    private bool IsMountUnlocked(int mountId)
    {
        unsafe
        {
            var mountList = PlayerState.Instance();

            var unlockedMounts = mountList->UnlockedMountsBitArray;

            return unlockedMounts.Get(mountId + 1);
        }
    }

    private MenuItem CreateMenuItem(MenuItemAction action, uint mountId)
    {
        return new MenuItem()
        {
            Name = action == MenuItemAction.Add ? "Add to Roulette-List" : "Remove from Roulette-List",
            Prefix = action == MenuItemAction.Add ? SeIconChar.BoxedPlus : SeIconChar.Cross,
            IsEnabled = true,
            OnClicked = (menuItemClickedArgs) =>
            {
                var subMenuItems = Enum.GetValues<MountListType>()
                                       .SelectMany(mountListType =>
                                                       this.CreateSubMenuItems(mountListType, action, mountId))
                                       .ToList();

                menuItemClickedArgs.OpenSubmenu(subMenuItems);
            }
        };
    }

    private List<MenuItem> CreateSubMenuItems(MountListType mountListType, MenuItemAction action, uint mountId)
    {
        var headerName = mountListType == MountListType.Whitelist ? "Whitelist" : "Blacklist";
        var menuItems = new List<MenuItem>
            { new() { Name = $"--- {headerName}  ---", IsEnabled = false } };


        return menuItems.Concat(Configuration.GetMountLists(mountListType)
                                             .Select((mountList => this.MountListToMenuItem(
                                                             mountList, mountId, action)))).ToList();
    }

    private MenuItem MountListToMenuItem(MountList mountList, uint mountId, MenuItemAction action)
    {
        var isMountIdInList = mountList.MountIds.Contains(mountId);

        return new MenuItem()
        {
            Name = mountList.Name,
            IsEnabled = action == MenuItemAction.Add
                            ? !isMountIdInList
                            : isMountIdInList,
            OnClicked = (_) =>
            {
                if (action == MenuItemAction.Add)
                {
                    mountList.MountIds.Add(mountId);
                }
                else
                {
                    mountList.MountIds.Remove(mountId);
                }

                Configuration.Save();
            },
        };
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();

    public void ToggleMainUi() => MainWindow.Toggle();
}
