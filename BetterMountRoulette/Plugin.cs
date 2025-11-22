using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using BetterMountRoulette.Commands;
using BetterMountRoulette.Windows;
using BetterMountRoulette.Windows.Config;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace BetterMountRoulette;

/**
 * Maps to UIColor RowId
 *
 * @see https://exd.camora.dev/sheet/UIColor for available numbers.
 */
enum ColorMap : ushort
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
    internal static ICommandManager DalamudCommandManager { get; private set; } = null!;

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
    
    public readonly WindowSystem WindowSystem = new("BetterMountRoulette");
    
    private CommandManager CommandManager { get; init; }

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    private MountNotebookContextMenu MountNotebookContextMenu { get; init; }

    public Plugin()
    {
        // TODO remove once no longer custom xiv struct version
        InteropGenerator.Runtime.Resolver.GetInstance.Setup();
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        InteropGenerator.Runtime.Resolver.GetInstance.Resolve();
        
        MountNotebookContextMenu = new MountNotebookContextMenu();
        CommandManager = new CommandManager();

        // You might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow();
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        
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

        MountNotebookContextMenu.Dispose();

        CommandManager.Dispose();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();

    public void ToggleMainUi() => MainWindow.Toggle();
}
