using Dalamud.Game.Command;
using EnhancedMountRoulette.Configuration;
using EnhancedMountRoulette.Windows.Config;

namespace EnhancedMountRoulette.Commands;

internal class OpenSettingsMenu(ConfigWindow configWindow) : ICommand
{
    public string Command => "/bmr-settings";

    public CommandInfo CommandInfo => new(Handler) { HelpMessage = "Opens Settings-Menu" };

    private void Handler(string _, string __)
    {
        configWindow.IsOpen = true;
    }
}
