using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedMountRoulette.Addons.Settings;
using EnhancedMountRoulette.Configuration;
using EnhancedMountRoulette.Windows.LegacySettings;

namespace EnhancedMountRoulette.Commands;

internal class CommandManager : IDisposable
{
    private List<ICommand> Commands { get; }

    public CommandManager(SettingsAddon nativeSettings, LegacySettingsWindow legacySettings)
    {
        Commands =
        [
            new SummonMountCommand(),
            .. Enum.GetValues<MountListType>()
                .Select(mountListType => new CreateMountListCommand(mountListType)),
            new ClearMountListCommand(),
            new DeleteMountListCommand(),
            new DeleteAllMountListsCommand(),
            new OpenSettingsCommand(nativeSettings),
            new OpenLegacySettingsCommand(legacySettings),
        ];

        foreach (var command in Commands)
        {
            Plugin.DalamudCommandManager.AddHandler(command.Command, command.CommandInfo);
        }
    }

    public void Dispose()
    {
        foreach (var command in Commands)
        {
            Plugin.DalamudCommandManager.RemoveHandler(command.Command);

            if (command is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
