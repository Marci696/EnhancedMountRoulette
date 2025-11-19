using System;

namespace BetterMountRoulette.Windows;

using Dalamud.Bindings.ImGui;

static class DrawHelper
{
    public static void Text(string text, TextScale textScale = TextScale.Normal)
    {
        var scale = ImGui.GetIO().FontGlobalScale;

        using (new Use(
                () => ImGui.SetWindowFontScale(scale * textScale.ToFloat()),
                () => ImGui.SetWindowFontScale(scale)
            ))
        {
            ImGui.Text(text);
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
