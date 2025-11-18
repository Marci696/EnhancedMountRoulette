using BetterMountRoulette.Configuration;
using Dalamud.Game.Command;
using Dalamud.Utility;

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
        if (MountManager.UnmountIfMounted())
        {
            return;
        }

        var listName = arguments.Trim();

        MountList mountList;

        if (listName.IsNullOrEmpty())
        {
            if (Configuration.GetDefaultMountList() is not { } defaultMountList)
            {
                Chat.Write($"No default mount list exists", true);

                return;
            }

            mountList = defaultMountList;
        }
        else
        {
            if (Configuration.GetMountList(listName) is not { } mountListByName)
            {
                Chat.Write($"No mount list found for the name \"{listName}\"", true);

                return;
            }

            mountList = mountListByName;
        }

        MountManager.SummonNextMountInList(mountList);
    }
}
