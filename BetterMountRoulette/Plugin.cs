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
        CommandManager.AddHandler("/better-mount-roulette", new CommandInfo(OnCallMountCommand)
        {
            HelpMessage = "Calls a random mount from the list"
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

        Configuration.GetDefaultMountList()?.BlacklistedIds.Add(mountId);
        Configuration.Save();

        ChatGui.Print($"\"{mount.Value.Singular.ExtractText()}\" was blacklisted");
    }

    private void OnBlacklistRemoveCommand(string command, string args)
    {
        var mount = GetMount(args);

        if (mount == null)
        {
            ChatGui.PrintError($"No mount found for the name \"{args}\"");

            return;
        }

        var mountId = mount.Value.RowId;

        Configuration.GetDefaultMountList()?.BlacklistedIds.Remove(mountId);
        Configuration.Save();

        ChatGui.Print($"\"{mount.Value.Singular.ExtractText()}\" was removed from blacklist");
    }

    private void OnClearBlacklistCommand(string command, string args)
    {
        Configuration.GetDefaultMountList()?.BlacklistedIds.Clear();
        Configuration.Save();

        ChatGui.Print("Blacklist cleared");
    }

    private void OnCurrentBlacklistCommand(string command, string args)
    {
        var mountList = Configuration.GetDefaultMountList();
        if (mountList == null)
        {
            return;
        }

        var blacklistedMounts = new List<string>();

        foreach (var mountId in mountList.BlacklistedIds)
        {
            var mount = GetMount(mountId);

            if (mount == null)
            {
                continue;
            }

            blacklistedMounts.Add(mount.Value.Singular.ExtractText());
        }

        ChatGui.Print("Current Blacklist: " + string.Join(", ", blacklistedMounts));
    }

    private void OnCallMountCommand(string command, string args)
    {
        var defaultMountList = Configuration.GetDefaultMountList();
        if (defaultMountList == null)
        {
            return;
        }

        var mountIdsForShuffle = this.GetAvailableMountsForList(defaultMountList);

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

        return (mountList.IncludeNotMentionedMountIds
                    ? ownedMountIds
                    : ownedMountIds.Intersect(mountList.WhitelistedIds)).Except(mountList.BlacklistedIds).ToList();
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

        args.AddMenuItem(new MenuItem() {IsEnabled = false, Name = "----------- Roulette -----------"});
        args.AddMenuItem(new MenuItem()
        {
            Name = "Add to Roulette-List",
            Prefix = SeIconChar.BoxedPlus,
            IsEnabled = true,
            OnClicked = (menuItemClickedArgs) =>
            {
                var menuItems = Configuration.MountLists.Select((mountList => new MenuItem()
                                                                    {
                                                                        Name = mountList.Name,
                                                                        IsEnabled = true,
                                                                        OnClicked = (_) =>
                                                                        {
                                                                            mountList.WhitelistedIds.Add(mountId);
                                                                            mountList.BlacklistedIds.Remove(mountId);
                                                                            Configuration.Save();
                                                                        }
                                                                    })).ToList();
                menuItemClickedArgs.OpenSubmenu(menuItems);
            }
        });

        args.AddMenuItem(new MenuItem()
        {
            Name = "Remove from Roulette-List",
            Prefix = SeIconChar.Cross,
            IsEnabled = true,
            OnClicked = (menuItemClickedArgs) =>
            {
                var menuItems = Configuration.MountLists.Select((mountList => new MenuItem()
                                                                    {
                                                                        Name = mountList.Name,
                                                                        IsEnabled = true,
                                                                        OnClicked = (_) =>
                                                                        {
                                                                            mountList.WhitelistedIds.Remove(mountId);
                                                                            mountList.BlacklistedIds.Add(mountId);
                                                                            Configuration.Save();
                                                                        },
                                                                    })).ToList();
                menuItemClickedArgs.OpenSubmenu(menuItems);
            }
        });
    }

    private Mount? GetMount(string mountName)
    {
        var mountSheet = DataManager.GetExcelSheet<Mount>();

        return mountSheet.Where((mount, index) =>
                                    mount.Singular.ExtractText().Equals(mountName, StringComparison.OrdinalIgnoreCase))
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

    public void ToggleConfigUi() => ConfigWindow.Toggle();

    public void ToggleMainUi() => MainWindow.Toggle();
}
