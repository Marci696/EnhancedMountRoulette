using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Linq;
using BetterMountRoulette.Windows;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
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

        UnlockedMounts();
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
            var isMountUnlocked = IsMountUnlocked(checked((int)mount.Value.RowId + 1));
            
            Log.Debug($" Mount \"{mount.Value.Singular.ExtractText()}\" is unlocked: {isMountUnlocked}");
        }

        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
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

    private bool IsMountUnlocked(int mountId)
    {
        unsafe
        {
            var mountList = PlayerState.Instance();

            var unlockedMounts = mountList->UnlockedMountsBitArray;

            return unlockedMounts.Get(mountId);
        }
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
