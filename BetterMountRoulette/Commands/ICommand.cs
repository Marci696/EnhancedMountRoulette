using Dalamud.Game.Command;

namespace BetterMountRoulette.Commands;

internal interface ICommand
{
    string Command { get; }

    CommandInfo CommandInfo { get; }
}
