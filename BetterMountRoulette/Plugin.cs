using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using BetterMountRoulette.Commands;
using BetterMountRoulette.Configuration;
using BetterMountRoulette.Windows;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

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


    private const string CommandName = "/pmbmroulette";

    private const string ChatTag = "BetterMountRoulette";

    // todo pick different color
    private const ushort ChatTagColor = (ushort)ColorMap.Lila;

    private Configuration.Configuration Configuration { get; init; }

    private CommandManager CommandManager { get; init; }

    public readonly WindowSystem WindowSystem = new("BetterMountRoulette");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    private MountNotebookContextMenu MountNotebookContextMenu { get; init; }

    public Plugin()
    {
        // TODO remove once no longer custom xiv struct version
        InteropGenerator.Runtime.Resolver.GetInstance.Setup();
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        InteropGenerator.Runtime.Resolver.GetInstance.Resolve();

        Configuration = PluginInterface.GetPluginConfig() as Configuration.Configuration
            ?? new Configuration.Configuration();
        // todo better way to ensure default exists
        Configuration.GetOrCreateDefaultMountList();

        MountNotebookContextMenu = new MountNotebookContextMenu(Configuration);
        CommandManager = new CommandManager(Configuration);

        // You might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(Configuration);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        DalamudCommandManager.AddHandler(
            "/bmr-delete-all-lists",
            new CommandInfo(OnDeleteAllListsCommand)
            {
                HelpMessage = "Clears all lists."
            }
        );
        DalamudCommandManager.AddHandler(
            "/bmr-clear-list",
            new CommandInfo(OnClearListCommand)
            {
                HelpMessage =
                    "Clear mount list, resetting it to an empty list. Usage like /bm-clear-list myName"
            }
        );
        DalamudCommandManager.AddHandler(
            "/bmr-delete-list",
            new CommandInfo(OnDeleteListCommand)
            {
                HelpMessage = "Deletes a mount list. Usage like /bm-delete-list myName"
            }
        );

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

        DalamudCommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
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
                ChatTag,
                ChatTagColor
            );

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
                ChatTag,
                ChatTagColor
            );

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

    public void ToggleConfigUi() => ConfigWindow.Toggle();

    public void ToggleMainUi() => MainWindow.Toggle();
}
