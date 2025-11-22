using System;
using System.Collections.Generic;
using System.Linq;
using BetterMountRoulette.Configuration;
using Dalamud.Game.Command;

namespace BetterMountRoulette.Commands;

internal class CommandManager : IDisposable
{
    private List<ICommand> Commands { get; }

    public CommandManager()
    {
        Commands =
        [
            new SummonMountCommand(),
            .. Enum.GetValues<MountListType>()
                .Select(mountListType => new CreateMountListCommand(mountListType)),
            new ClearMountListCommand(),
            new DeleteMountListCommand(),
            new DeleteAllMountListsCommand(),
            // TODO add command to add and remove currently mounted mount
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
