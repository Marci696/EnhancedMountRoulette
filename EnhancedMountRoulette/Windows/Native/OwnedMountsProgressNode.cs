using System;
using System.Globalization;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;

namespace EnhancedMountRoulette.Windows.Native;

/// <summary>
/// Owned/total mount progress with a native Parameter_Gauge frame and a solid fill.
/// Label sits to the left of the bar for use as a full-width footer.
/// </summary>
public class OwnedMountsProgressNode : ResNode
{
    public const float BarHeight = 20.0f;
    public const float PreferredHeight = BarHeight;
    private const float LabelWidth = 110.0f;
    private const float LabelGap = 8.0f;

    private const float FillInsetX = 6.0f;
    private const float FillInsetY = 4.0f;

    private static readonly Vector4 FillColor = new(0.72f, 0.58f, 0.28f, 0.95f);
    private static readonly Vector4 HeaderColor = new(0.85f, 0.85f, 0.85f, 1.0f);

    private readonly TextNode headerNode;
    private readonly SimpleNineGridNode backgroundNode;
    private readonly SimpleNineGridNode fillNode;
    private readonly SimpleNineGridNode borderNode;
    private readonly TextNode labelNode;

    private int lastOwned = -1;
    private int lastTotal = -1;

    public OwnedMountsProgressNode()
    {
        headerNode = new TextNode
        {
            Position = Vector2.Zero,
            Size = new Vector2(LabelWidth, BarHeight),
            FontSize = 12,
            LineSpacing = 12,
            AlignmentType = AlignmentType.Left,
            TextColor = HeaderColor,
            String = "Owned mounts:",
        };
        headerNode.AttachNode(this);

        backgroundNode = new SimpleNineGridNode
        {
            Position = new Vector2(LabelWidth + LabelGap, 0.0f),
            TexturePath = "ui/uld/Parameter_Gauge.tex",
            TextureSize = new Vector2(160.0f, 20.0f),
            TextureCoordinates = new Vector2(0.0f, 100.0f),
            LeftOffset = 20,
            RightOffset = 20,
        };
        backgroundNode.AttachNode(this);

        // Textured fill (not ColorImageNode): empty image nodes have been implicated in
        // AtkEventManager.ClearEvents crashes during addon finalization.
        fillNode = new SimpleNineGridNode
        {
            Position = new Vector2(LabelWidth + LabelGap + FillInsetX, FillInsetY),
            TexturePath = "ui/uld/PartyList_GaugeCast.tex",
            TextureSize = new Vector2(188.0f, 7.0f),
            TextureCoordinates = new Vector2(8.0f, 3.0f),
            LeftOffset = 10,
            RightOffset = 10,
            IsVisible = false,
        };
        fillNode.Color = new Vector4(1.0f, 1.0f, 1.0f, FillColor.W);
        fillNode.AddColor = new Vector3(FillColor.X, FillColor.Y, FillColor.Z);
        fillNode.AttachNode(this);

        borderNode = new SimpleNineGridNode
        {
            Position = new Vector2(LabelWidth + LabelGap, 0.0f),
            TexturePath = "ui/uld/Parameter_Gauge.tex",
            TextureSize = new Vector2(160.0f, 20.0f),
            TextureCoordinates = new Vector2(0.0f, 0.0f),
            LeftOffset = 20,
            RightOffset = 20,
        };
        borderNode.AttachNode(this);

        labelNode = new TextNode
        {
            Position = new Vector2(LabelWidth + LabelGap, 0.0f),
            FontSize = 11,
            LineSpacing = 11,
            AlignmentType = AlignmentType.Center,
            TextFlags = TextFlags.Edge,
            TextColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
            String = "0/0 (0%)",
        };
        labelNode.AttachNode(this);
    }

    public void Refresh()
    {
        var (owned, total) = MountManager.GetOwnershipCounts();
        if (owned == lastOwned && total == lastTotal)
        {
            return;
        }

        lastOwned = owned;
        lastTotal = total;

        ApplyFill();
        labelNode.String = string.Create(
            CultureInfo.InvariantCulture,
            $"{owned}/{total} ({(total > 0 ? (int)Math.Round(100.0 * owned / total) : 0)}%)"
        );
    }

    private float BarLeft => LabelWidth + LabelGap;

    private float BarWidth => Math.Max(0.0f, Width - BarLeft);

    private void ApplyFill()
    {
        var maxFillWidth = Math.Max(0.0f, BarWidth - (FillInsetX * 2.0f));
        var fillHeight = Math.Max(0.0f, BarHeight - (FillInsetY * 2.0f));
        var fraction = lastTotal > 0
            ? Math.Clamp(lastOwned / (float)lastTotal, 0.0f, 1.0f)
            : 0.0f;

        fillNode.Position = new Vector2(BarLeft + FillInsetX, FillInsetY);
        fillNode.Height = fillHeight;
        fillNode.Width = maxFillWidth * fraction;
        fillNode.IsVisible = fillNode.Width > 0.5f;
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();

        headerNode.Size = new Vector2(LabelWidth, Height);

        var barSize = new Vector2(BarWidth, Height);
        backgroundNode.Position = new Vector2(BarLeft, 0.0f);
        backgroundNode.Size = barSize;
        borderNode.Position = new Vector2(BarLeft, 0.0f);
        borderNode.Size = barSize;
        labelNode.Position = new Vector2(BarLeft, 0.0f);
        labelNode.Size = barSize;

        ApplyFill();
    }
}
