using System.Numerics;
using BetterMountRoulette.Commands;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using static BetterMountRoulette.Windows.DrawHelper;

namespace BetterMountRoulette.Windows.Config;

public class Explanation : IDrawable
{
    private readonly MountListExplanationTable mountListExplanationTable = new MountListExplanationTable();

    public void Draw()
    {
        if (ImGui.CollapsingHeader("Explanation"))
        {
            ImGui.TextWrapped(
                "This plugin adds a new way to control, what mounts you will call upon during mount roulette.\n\n"
                + "You can have one default list and multiple custom ones for use cases such as "
                + "PVP, for showing off, etc."
            );

            PaddingY(ImGui.GetTextLineHeight());

            using (ImRaii.PushIndent())
            {
                DrawCommandUsage();

                PaddingY(ImGui.GetTextLineHeight());

                DrawTableUsage();
                
                PaddingY(ImGui.GetTextLineHeight());
                
                mountListExplanationTable.Draw();
            }
        }
    }

    private static void DrawCommandUsage()
    {
        if (ImGui.CollapsingHeader("Command usage", ImGuiTreeNodeFlags.DefaultOpen))
        {
            Text("You can summon your as default marked list via:");

            Text(SummonMountCommand.CommandName, color: CommandColor);

            ImGui.TextWrapped("or any of your custom lists via:");

            Text(SummonMountCommand.CommandName + " your list name", color: CommandColor);

            PaddingY(ImGui.GetTextLineHeight());

            ImGui.TextWrapped("To get it into your hotbar, create a macro with a content such as:");

            var macroText = SummonMountCommand.GetMacro();

            Text(
                macroText,
                color: CommandColor
            );

            if (ImGui.Button("Copy to clipboard"))
            {
                ImGui.SetClipboardText(macroText);

                Plugin.ToastGui.ShowNormal(
                    "Copied to clipboard",
                    new ToastOptions() { Position = ToastPosition.Bottom, Speed = ToastSpeed.Fast }
                );
            }
        }
    }

    private static void DrawTableUsage()
    {
        if (ImGui.CollapsingHeader("Table usage", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextWrapped(
                "In the table below you can create and edit different lists depending on your needs.\n\n"
            );
        }
    }
}
