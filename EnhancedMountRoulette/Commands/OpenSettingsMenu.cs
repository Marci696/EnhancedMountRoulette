using Dalamud.Game.Command;
using EnhancedMountRoulette.Windows.Native;

namespace EnhancedMountRoulette.Commands;

internal class OpenSettingsMenu(ConfigAddon configAddon) : ICommand
{
    public string Command => "/emr-settings";

    public CommandInfo CommandInfo => new(Handler) { HelpMessage = "Opens Settings-Menu" };

    private void Handler(string _, string __)
    {
        configAddon.Toggle();
    }
}
