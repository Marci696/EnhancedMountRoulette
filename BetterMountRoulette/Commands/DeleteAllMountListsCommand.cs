using Dalamud.Game.Command;

namespace BetterMountRoulette.Commands;

internal class DeleteAllMountListsCommand(Configuration.Configuration configuration) : ICommand
{
    public string Command => "/bmr-delete-all-lists";

    public CommandInfo CommandInfo => new(Handler) { HelpMessage = "Deletes all mount lists." };

    private void Handler(string _, string __)
    {
        configuration.ClearMountList();

        Chat.Write("All lists were removed.");
    }
}
