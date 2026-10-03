using Dalamud.Game.Command;
using EnhancedMountRoulette.Windows.Settings;

namespace EnhancedMountRoulette.Commands;

internal class OpenLegacySettingsCommand(LegacySettingsWindow legacySettings) : ICommand
{
    public string Command => "/emr-legacy-settings";

    public CommandInfo CommandInfo => new(Handler)
    {
        HelpMessage = "Opens the legacy ImGui settings menu",
    };

    private void Handler(string _, string __)
    {
        legacySettings.Toggle();
    }
}
