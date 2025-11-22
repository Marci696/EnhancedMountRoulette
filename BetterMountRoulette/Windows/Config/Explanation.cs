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
    private static readonly Vector4 CommandColor = RgbaToImgGuiVector(222, 121, 7, 1);

    public void Draw()
    {
        if (ImGui.CollapsingHeader("Explanation"))
        {
            ImGui.TextWrapped(
                "This plugin adds a new way to control, what mounts you will call upon during mount roulette."
            );

            PaddingY(ImGui.GetTextLineHeight());

            using (ImRaii.PushIndent(1))
            {
                DrawCommandUsage();
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
}
