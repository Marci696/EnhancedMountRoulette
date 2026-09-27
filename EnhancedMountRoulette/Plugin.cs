using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using EnhancedMountRoulette.Commands;
using EnhancedMountRoulette.Windows.Native;
using KamiToolKit;

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

    private CommandManager? CommandManager { get; set; }

    private ConfigAddon? ConfigAddon { get; set; }

    private MountNotebookContextMenu? MountNotebookContextMenu { get; set; }

    public Plugin()
    {
        // TODO remove once no longer custom xiv struct version
        InteropGenerator.Runtime.Resolver.GetInstance.Setup();
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        InteropGenerator.Runtime.Resolver.GetInstance.Resolve();

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await KamiToolKitLibrary.InitializeAsync(PluginInterface, "Enhanced Mount Roulette");

        await FfxivCollectMountData.InitializeAsync();

        ConfigAddon = new ConfigAddon
        {
            InternalName = "EMRConfig",
            Title = "Enhanced Mount Roulette",
            Size = new Vector2(900.0f, 620.0f),
        };

        MountNotebookContextMenu = new MountNotebookContextMenu();
        CommandManager = new CommandManager(ConfigAddon);

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        CommandManager?.Dispose();
        MountNotebookContextMenu?.Dispose();
        ConfigAddon?.Dispose();

        KamiToolKitLibrary.Dispose();
    }

    public void ToggleConfigUi() => ConfigAddon?.Toggle();
}
