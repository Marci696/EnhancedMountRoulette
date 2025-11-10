using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Linq;
using BetterMountRoulette.Windows;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
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

    private const string CommandName = "/pmbmroulette";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("BetterMountRoulette");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // You might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

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

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [BetterMountRoulette] ===A cool log message from Sample Plugin===
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");

        Log.Debug("Blacklisted mounts: " + Configuration.BlacklistedMountIds.ToString());

        // UnlockedMounts();
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

        Configuration.BlacklistedMountIds.Add(mountId);
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

        Configuration.BlacklistedMountIds.Remove(mountId);
        Configuration.Save();

        ChatGui.Print($"\"{mount.Value.Singular.ExtractText()}\" was removed from blacklist");
    }

    private void OnClearBlacklistCommand(string command, string args)
    {
        Configuration.BlacklistedMountIds.Clear();
        Configuration.Save();

        ChatGui.Print("Blacklist cleared");
    }

    private void OnCurrentBlacklistCommand(string command, string args)
    {
        var blacklistedMounts = new List<string>();

        foreach (var mountId in Configuration.BlacklistedMountIds)
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
        var availableMountsForShuffle = new List<uint>();
        string ownedMounts = "";

        unsafe
        {
            var mountSheet = DataManager.GetExcelSheet<Mount>();

            foreach (var mount in mountSheet)
            {
                // Skip invalid mounts
                if (mount.Singular.IsEmpty || mount.Order < 0)
                    continue;

                var mountList = PlayerState.Instance();
                var mounts = mountList->UnlockedMountsBitArray;

                // Use mount.Order as the bit array index, not the row ID
                bool isMountUnlocked = mounts.Get(mount.Order);
                uint mountId = mount.RowId;

                ownedMounts += $"{mountId}: {mount.Singular.ExtractText()}, owned: {isMountUnlocked}\n";

                if (isMountUnlocked && !Configuration.BlacklistedMountIds.Contains(mountId))
                {
                    availableMountsForShuffle.Add(mountId);
                }
            }
        }

        var randomNumber = Random.Shared.Next(availableMountsForShuffle.Count);
        var mountIdToMount = availableMountsForShuffle[randomNumber];

        Log.Information(ownedMounts);
        Log.Information($"MountIdToMount: {mountIdToMount}, Name: {GetMount(mountIdToMount)?.Singular.ExtractText()}");

        unsafe
        {
            ActionManager.Instance()->UseAction(ActionType.Mount, mountIdToMount);
        }
    }

    private void UnlockedMounts()
    {
        unsafe
        {
            var mountList = PlayerState.Instance();
            var unlockedMounts = mountList->UnlockedMountsBitArray;

            foreach (var (index, value) in unlockedMounts)
            {
                Log.Information($"Index: {index}, IsUnlocked: {value}");
            }
        }
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
