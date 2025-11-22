using BetterMountRoulette.Configuration;
using Dalamud.Game.Command;
using Dalamud.Utility;

namespace BetterMountRoulette.Commands;

internal class SummonMountCommand : ICommand
{
    public const string CommandName = "/bmr";

    public string Command => CommandName;
    
    public static string GetMacro(MountList mountList)
    {
        return GetMacro(mountList.Name);
    }

    public static string GetMacro(string? mountListName = null)
    {
        return $"/micon \"flying mount roulette\"\n{GetCommandWithListName(mountListName)}";
    }

    public static string GetCommandWithListName(string? mountListName = null)
    {
        return CommandName + (mountListName is not null ? $" {mountListName}" : string.Empty);
    }

    public CommandInfo CommandInfo => new(Handler)
    {
        HelpMessage =
            "Calls a random mount from a list. /bmr will use the default use. To use mount from your custom list use /bmr listName"
    };

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
            if (ConfigManager.Instance.GetDefaultMountList() is not { } defaultMountList)
            {
                Chat.Write($"No default mount list exists", true);

                return;
            }

            mountList = defaultMountList;
        }
        else
        {
            if (ConfigManager.Instance.GetMountList(listName) is not { } mountListByName)
            {
                Chat.Write($"No mount list found for the name \"{listName}\"", true);

                return;
            }

            mountList = mountListByName;
        }

        MountManager.SummonNextMountInList(mountList);
    }
}
