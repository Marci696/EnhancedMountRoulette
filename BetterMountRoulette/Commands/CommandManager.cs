using System;
using System.Collections.Generic;
using System.Linq;
using BetterMountRoulette.Configuration;
using Dalamud.Game.Command;

namespace BetterMountRoulette.Commands;

internal class CommandManager
{
    private List<ICommand> Commands { get; }

    public CommandManager(Configuration.Configuration configuration)
    {
        Commands =
        [
            new SummonMountCommand(configuration),
            .. Enum.GetValues<MountListType>()
                .Select(mountListType => new CreateMountListCommand(configuration, mountListType)),
            new DeleteAllMountListsCommand(configuration),
            new DeleteListCommand(configuration),
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
        }
    }
}
