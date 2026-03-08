using Dalamud.Game.Command;
using EnhancedMountRoulette.Configuration;

namespace EnhancedMountRoulette.Commands;

internal class DeleteAllMountListsCommand : ICommand
{
    public string Command => "/bmr-delete-all-lists";

    public CommandInfo CommandInfo => new(Handler) { HelpMessage = "Deletes all mount lists." };

    private void Handler(string _, string __)
    {
        ConfigManager.Instance.ClearMountLists();

        Chat.Write("All lists were removed.");
    }
}
