using System.Numerics;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace EnhancedMountRoulette.Addons.Settings;

public static class ButtonStyles
{
    // Button timelines overwrite BackgroundNode.MultiplyColor every frame.
    // Color (RGBA) is not timeline-driven, so tint via that instead.
    private static readonly Vector4 AddTint = new(0.45f, 1.0f, 0.55f, 1.0f);
    private static readonly Vector4 RemoveTint = new(1.0f, 0.45f, 0.45f, 1.0f);

    public static void StyleAsAdd(TextButtonNode button)
    {
        button.LabelNode.TextColor = ColorHelper.GetColor(50);
        button.BackgroundNode.Color = AddTint;
    }

    public static void StyleAsRemove(TextButtonNode button)
    {
        button.LabelNode.TextColor = ColorHelper.GetColor(50);
        button.BackgroundNode.Color = RemoveTint;
    }
}
