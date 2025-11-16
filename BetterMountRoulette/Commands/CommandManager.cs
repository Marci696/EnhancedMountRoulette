using System.Collections.Generic;
using Dalamud.Game.Command;

namespace BetterMountRoulette.Commands;

public class CommandManager
{
    private List<BaseCommand> Commands;

    public CommandManager(Configuration.Configuration configuration)
    {
        Commands = [new SummonMountCommand(configuration)];

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
