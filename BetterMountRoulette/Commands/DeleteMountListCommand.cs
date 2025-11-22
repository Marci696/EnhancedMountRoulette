using BetterMountRoulette.Configuration;
using Dalamud.Game.Command;

namespace BetterMountRoulette.Commands;

internal class DeleteMountListCommand : ICommand
{
    public string Command => "/bmr-delete-list";

    public CommandInfo CommandInfo => new(Handler)
        { HelpMessage = $"Deletes a mount list. Usage like {Command} myName" };

    private void Handler(string _, string arguments)
    {
        var listName = arguments.Trim();

        if (listName.Length == 0)
        {
            Chat.Write(
                $"You need to specify a name for the list do delete. For example: {Command} myName",
                isError: true
            );

            return;
        }

        if (ConfigManager.Instance.GetMountList(listName) is not { } mountList)
        {
            Chat.Write(
                $"No mount list found for the name \"{listName}\"",
                isError: true
            );

            return;
        }

        ConfigManager.Instance.RemoveMountList(mountList);

        Chat.Write($"List \"{mountList.Name}\" was deleted.");
    }
}
