namespace BetterMountRoulette.Windows;

using Dalamud.Bindings.ImGui;

static class DrawHelper
{
    public static void Text(string text, TextScale textScale = TextScale.Normal)
    {
        var scale = ImGui.GetIO().FontGlobalScale;

        ImGui.SetWindowFontScale(scale * textScale.ToFloat());

        ImGui.Text(text);

        ImGui.SetWindowFontScale(scale);
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
