using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using EnhancedMountRoulette.Windows;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using EnhancedMountRoulette.Commands;
using EnhancedMountRoulette.Windows.Config;

namespace EnhancedMountRoulette;

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

    [PluginService]
    internal static IToastGui ToastGui { get; private set; } = null!;

    public readonly WindowSystem WindowSystem = new("EnhancedMountRoulette");

    private CommandManager CommandManager { get; init; }

    private ConfigWindow ConfigWindow { get; init; }
    private MountNotebookContextMenu MountNotebookContextMenu { get; init; }

    public Plugin()
    {
        // TODO remove once no longer custom xiv struct version
        InteropGenerator.Runtime.Resolver.GetInstance.Setup();
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        InteropGenerator.Runtime.Resolver.GetInstance.Resolve();

        MountNotebookContextMenu = new MountNotebookContextMenu();
        ConfigWindow = new ConfigWindow();
        WindowSystem.AddWindow(ConfigWindow);

        CommandManager = new CommandManager(ConfigWindow);

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();

        MountNotebookContextMenu.Dispose();

        CommandManager.Dispose();
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
}
