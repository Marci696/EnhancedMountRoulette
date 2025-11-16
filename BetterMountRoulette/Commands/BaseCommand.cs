using Dalamud.Game.Command;

namespace BetterMountRoulette.Commands;

public abstract class BaseCommand(Configuration.Configuration configuration)
{
    public abstract string Command { get; }

    public abstract CommandInfo CommandInfo { get; }

    protected readonly Configuration.Configuration Configuration = configuration;
}
