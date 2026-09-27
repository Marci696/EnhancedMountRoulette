using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedMountRoulette.Configuration;
using EnhancedMountRoulette.Windows.Native;

namespace EnhancedMountRoulette.Commands;

internal class CommandManager : IDisposable
{
    private List<ICommand> Commands { get; }

    public CommandManager(ConfigAddon configAddon)
    {
        Commands =
        [
            new SummonMountCommand(),
            .. Enum.GetValues<MountListType>()
                .Select(mountListType => new CreateMountListCommand(mountListType)),
            new ClearMountListCommand(),
            new DeleteMountListCommand(),
            new DeleteAllMountListsCommand(),
            new OpenSettingsMenu(configAddon),
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
