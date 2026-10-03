using Dalamud.Game.Command;
using EnhancedMountRoulette.Addons.Settings;

namespace EnhancedMountRoulette.Commands;

internal class OpenSettingsCommand(SettingsAddon nativeSettings) : ICommand
{
    public string Command => "/emr-settings";

    public CommandInfo CommandInfo => new(Handler) { HelpMessage = "Opens Settings-Menu" };

    private void Handler(string _, string __)
    {
        nativeSettings.Toggle();
    }
}
