using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using EnhancedMountRoulette.Commands;
using Dalamud.Interface.Windowing;
using EnhancedMountRoulette.Addons.Settings;
using EnhancedMountRoulette.Windows.Settings;
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

    [PluginService]
    internal static IFramework Framework { get; private set; } = null!;

    private CommandManager? CommandManager { get; set; }

    private SettingsAddon? SettingsAddon { get; set; }

    private LegacySettingsWindow? LegacySettingsWindow { get; set; }

    private WindowSystem? WindowSystem { get; set; }

    private MountNotebookContextMenu? MountNotebookContextMenu { get; set; }

    /// <summary>
    /// Serializes async init vs <see cref="Dispose"/> so they cannot create and tear down
    /// windows/commands/KamiToolKit at the same time. Does not lock game or UI state.
    /// </summary>
    private readonly Lock initSync = new();

    /// <summary>
    /// Cancelled in <see cref="Dispose"/> so a still-running <see cref="InitializeAsync"/>
    /// can bail out before creating UI after unload has started.
    /// </summary>
    private readonly CancellationTokenSource initCancellationTokenSource = new();

    /// <summary>
    /// True after <see cref="Dispose"/> has begun; init checks this under <see cref="initSync"/>.
    /// </summary>
    private bool isDisposed;

    /// <summary>
    /// True after <see cref="KamiToolKitLibrary.InitializeAsync"/> succeeded for this plugin
    /// instance, so <see cref="Dispose"/> knows whether <see cref="KamiToolKitLibrary.Dispose"/>
    /// must run (including the case where unload raced mid-init).
    /// </summary>
    private bool isKamiToolKitLibraryInitialized;

    public Plugin()
    {
        _ = InitializeAsync(initCancellationTokenSource.Token);
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await KamiToolKitLibrary.InitializeAsync(PluginInterface, "Enhanced Mount Roulette");

        lock (initSync)
        {
            if (isDisposed)
            {
                KamiToolKitLibrary.Dispose();
                return;
            }

            isKamiToolKitLibraryInitialized = true;
        }

        await FfxivCollectMountData.InitializeAsync();

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        lock (initSync)
        {
            if (isDisposed)
            {
                return;
            }

            SettingsAddon = new SettingsAddon
            {
                InternalName = "EMRConfig",
                Title = "Enhanced Mount Roulette",
                Size = new Vector2(900.0f, 620.0f),
            };

            LegacySettingsWindow = new LegacySettingsWindow();
            WindowSystem = new WindowSystem("EnhancedMountRoulette");
            WindowSystem.AddWindow(LegacySettingsWindow);

            MountNotebookContextMenu = new MountNotebookContextMenu();
            CommandManager = new CommandManager(SettingsAddon, LegacySettingsWindow);

            PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        }
    }

    public void Dispose()
    {
        lock (initSync)
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

            if (WindowSystem is not null)
            {
                PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
                WindowSystem.RemoveAllWindows();
            }

            CommandManager?.Dispose();
            MountNotebookContextMenu?.Dispose();
            SettingsAddon?.Dispose();
            LegacySettingsWindow?.Dispose();

            if (isKamiToolKitLibraryInitialized)
            {
                KamiToolKitLibrary.Dispose();
            }
        }

        initCancellationTokenSource.Cancel();
        initCancellationTokenSource.Dispose();
    }

    public void ToggleConfigUi() => SettingsAddon?.Toggle();
}
