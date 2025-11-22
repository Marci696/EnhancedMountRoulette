using System.Numerics;
using BetterMountRoulette.Commands;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.Components;
using static BetterMountRoulette.Windows.DrawHelper;

namespace BetterMountRoulette.Windows.Config;

public class Explanation : IDrawable
{
    private static readonly Vector4 CommandColor = RgbaToImgGuiVector(219, 4, 198, 255);
    private static readonly Vector4 MacroColor = RgbaToImgGuiVector(222, 121, 7, 1);

    public void Draw()
    {
        if (ImGui.CollapsingHeader("Explanation"))
        {
            ImGui.TextWrapped(
                "This plugin adds a new way to control, what mounts you will call upon during mount roulette.\n\n"
                + "You can summon your as default marked list via:"
            );

            Text(SummonMountCommand.CommandName, color: CommandColor);

            ImGui.TextWrapped("or any of your custom lists via:");

            Text(SummonMountCommand.CommandName + " your list name", color: CommandColor);

            PaddingY(ImGui.GetTextLineHeight());

            ImGui.TextWrapped("To get it into your hotbar, create a macro with a content such as:");

            var macroText = $"/micon \"flying mount roulette\"\n{SummonMountCommand.CommandName}";

            Text(
                macroText,
                color: MacroColor
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
