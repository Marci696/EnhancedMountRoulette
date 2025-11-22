using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace BetterMountRoulette.Windows;

using Dalamud.Bindings.ImGui;

static class DrawHelper
{
    public static Vector4 RgbaToImgGuiVector(byte red, byte green, byte blue, float alpha) =>
        new(red / 255f, green / 255f, blue / 255f, alpha);

    public static void Text(string text, TextScale textScale = TextScale.Normal, Vector4? color = null)
    {
        var scale = ImGui.GetIO().FontGlobalScale;

        using (new Use(
                () => ImGui.SetWindowFontScale(scale * textScale.ToFloat()),
                () => ImGui.SetWindowFontScale(scale)
            ))
        {
            if (color is { } textColor)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, textColor))
                {
                    ImGui.Text(text);
                }
            }
            else
            {
                ImGui.Text(text);
            }
        }
    }

    public static void PaddingY(float padding)
    {
        var scale = ImGui.GetIO().FontGlobalScale;

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * scale));
    }

    public static void PaddingX(float padding)
    {
        var scale = ImGui.GetIO().FontGlobalScale;

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (padding * scale));
    }

    public static void NextItemWidth(float width)
    {
        var scale = ImGui.GetIO().FontGlobalScale;

        ImGui.SetNextItemWidth(width * scale);
    }

    public static void FullWidth()
    {
        ImGui.SetNextItemWidth(-1f);
    }

    public static void CenterHorizontally()
    {
        // Frame height is the standard size for a square for most things such as icons and checkboxes.
        CenterHorizontally(ImGui.GetFrameHeight());
    }

    public static void CenterHorizontally(float itemWidth)
    {
        ImGui.SetCursorPosX(
            ImGui.GetCursorPosX()
            // Find out how much space is available and set position to the middle.
            + (ImGui.GetContentRegionAvail().X / 2)
            // Take item size into account, to start drawing earlier to center the middle of the item.
            - (itemWidth / 2.0f)
        );
    }

    // TODO maybe not working
    public static void CenterVertically()
    {
        CenterVertically(ImGui.GetFrameHeight());
    }

    // TODO maybe not working
    public static void CenterVertically(float itemHeight)
    {
        ImGui.SetCursorPosY(
            ImGui.GetCursorPosY()
            // Find out how much space is available and set position to the middle.
            + (ImGui.GetContentRegionAvail().Y / 2)
            // Take item size into account, to start drawing earlier to center the middle of the item.
            - (itemHeight / 2.0f)
        );
    }

    public static bool RemoveIconButton(string id, Vector2? size = null)
    {
        // Change X cross icon to red.
        using (ImRaii.PushColor(ImGuiCol.Text, RgbaToImgGuiVector(183, 29, 6, 1)))
        {
            return ImGuiComponents.IconButton(
                id,
                // Looks like an X cross.
                icon: FontAwesomeIcon.Times,
                size: size ?? new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight()),
                // Hide background
                defaultColor: new Vector4(0, 0, 0, 0),
                hoveredColor: RgbaToImgGuiVector(76, 76, 76, 1),
                // Color when it is clicked.
                activeColor: RgbaToImgGuiVector(153, 153, 153, 1)
            );
        }
    }

    public static bool AddIconButton(string label, Vector2? size = null)
    {
        // Change X cross icon to green.
        using (ImRaii.PushColor(ImGuiCol.Text, RgbaToImgGuiVector(21, 146, 21, 1)))
        {
            return ImGuiComponents.IconButton(
                label,
                icon: FontAwesomeIcon.HeartCirclePlus,
                size: size ?? new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight()),
                // Hide background
                defaultColor: new Vector4(0, 0, 0, 0),
                hoveredColor: RgbaToImgGuiVector(76, 76, 76, 1),
                // Color when it is clicked.
                activeColor: RgbaToImgGuiVector(153, 153, 153, 1)
            );
        }
    }

    public static void DrawColumns(IEnumerable<Action> columnCallbacks)
    {
        foreach (var columnCallback in columnCallbacks)
        {
            ImGui.TableNextColumn();
            columnCallback();
        }
    }

    public static string ConfirmationWindow(string name, string confirmationQuestion, Action onConfirm)
    {
        ImGui.SetNextWindowSize(new Vector2(500, 0));
        if (ImGui.BeginPopupModal(name, ImGuiWindowFlags.NoResize))
        {
            ImGui.TextWrapped(confirmationQuestion);

            PaddingY(10);

            if (ImGui.Button("Yes", new Vector2(225, 0)))
            {
                onConfirm.Invoke();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - 225);
            if (ImGui.Button("No", new Vector2(225, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        return name;
    }
}

class Use : IDisposable
{
    private EndFunc End;

    public delegate void BeginFunc();

    public delegate void EndFunc();

    public Use(
        BeginFunc begin,
        EndFunc end
    )
    {
        End = end;

        begin();
    }

    public void Dispose()
    {
        End();
    }
}

enum TextScale
{
    H1,
    H2,
    H3,
    H4,
    Normal,
}

internal static class TextScaleExtensions
{
    public static float ToFloat(this TextScale textScale) => textScale switch
    {
        TextScale.H1 => 2f,
        TextScale.H2 => 1.8f,
        TextScale.H3 => 1.6f,
        TextScale.H4 => 1.4f,
        _ => 1f
    };
}
