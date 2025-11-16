using Dalamud.Game.Command;

namespace BetterMountRoulette.Commands;

internal class ClearMountListCommand(Configuration.Configuration configuration) : ICommand
{
    public string Command => "/bmr-clear-list";

    public CommandInfo CommandInfo => new(Handler)
        { HelpMessage = $"Clear mount list, resetting it to an empty list. Usage like {Command} myName" };

    private void Handler(string _, string arguments)
    {
        var listName = arguments.Trim();

        if (listName.Length == 0)
        {
            Chat.Write(
                $"You need to specify a name for the list do clear. For example: {Command} myName",
                isError: true
            );

            return;
        }

        if (configuration.GetMountList(listName) is not { } mountList)
        {
            Chat.Write(
                $"No mount list found for the name \"{listName}\"",
                isError: true
            );

            return;
        }

        configuration.CleanMountList(mountList);

        Chat.Write($"List \"{mountList.Name}\" was cleared.");
    }
}
