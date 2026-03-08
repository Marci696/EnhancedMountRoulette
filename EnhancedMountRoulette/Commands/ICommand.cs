using Dalamud.Game.Command;

namespace EnhancedMountRoulette.Commands;

internal interface ICommand
{
    string Command { get; }

    CommandInfo CommandInfo { get; }
}
