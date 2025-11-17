using System;
using BetterMountRoulette.Configuration;
using Dalamud.Game.Command;

namespace BetterMountRoulette.Commands;

internal class SummonMountCommand(Configuration.Configuration configuration) : ICommand
{
    public string Command => "/bmr";

    public CommandInfo CommandInfo => new(Handler)
    {
        HelpMessage =
            "Calls a random mount from a list. /bmr will use the default use. To use mount from your custom list use /bmr listName"
    };

    private Configuration.Configuration Configuration { get; } = configuration;

    private void Handler(string _, string arguments)
    {
        var listName = arguments.Trim();

        var mountList =
            Configuration.GetMountList(listName);

        if (mountList == null)
        {
            Chat.Write($"No mount list found for the name \"{listName}\"", true);

            return;
        }

        MountManager.SummonNextMountInList(mountList);
    }
}
