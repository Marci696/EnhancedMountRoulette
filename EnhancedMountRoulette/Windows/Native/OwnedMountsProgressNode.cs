using System;
using System.Globalization;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;

namespace EnhancedMountRoulette.Windows.Native;

/// <summary>
/// Owned/total mount progress with a native Parameter_Gauge frame and a solid fill.
/// </summary>
public class OwnedMountsProgressNode : ResNode
{
    public const float HeaderHeight = 16.0f;
    public const float BarHeight = 20.0f;
    public const float PreferredHeight = HeaderHeight + BarHeight;

    private const float FillInsetX = 6.0f;
    private const float FillInsetY = 4.0f;

    private static readonly Vector4 FillColor = new(0.72f, 0.58f, 0.28f, 0.95f);
    private static readonly Vector4 HeaderColor = new(0.85f, 0.85f, 0.85f, 1.0f);

    private readonly TextNode headerNode;
    private readonly SimpleNineGridNode backgroundNode;
    private readonly ColorImageNode fillNode;
    private readonly SimpleNineGridNode borderNode;
    private readonly TextNode labelNode;

    private int lastOwned = -1;
    private int lastTotal = -1;

    public OwnedMountsProgressNode()
    {
        headerNode = new TextNode
        {
            Position = Vector2.Zero,
            FontSize = 11,
            LineSpacing = 11,
            AlignmentType = AlignmentType.Left,
            TextColor = HeaderColor,
            String = "Owned mounts",
        };
        headerNode.AttachNode(this);

        backgroundNode = new SimpleNineGridNode
        {
            Position = new Vector2(0.0f, HeaderHeight),
            TexturePath = "ui/uld/Parameter_Gauge.tex",
            TextureSize = new Vector2(160.0f, 20.0f),
            TextureCoordinates = new Vector2(0.0f, 100.0f),
            LeftOffset = 20,
            RightOffset = 20,
        };
        backgroundNode.AttachNode(this);

        fillNode = new ColorImageNode
        {
            Position = new Vector2(FillInsetX, HeaderHeight + FillInsetY),
            Color = FillColor,
            IsVisible = false,
        };
        fillNode.AttachNode(this);

        borderNode = new SimpleNineGridNode
        {
            Position = new Vector2(0.0f, HeaderHeight),
            TexturePath = "ui/uld/Parameter_Gauge.tex",
            TextureSize = new Vector2(160.0f, 20.0f),
            TextureCoordinates = new Vector2(0.0f, 0.0f),
            LeftOffset = 20,
            RightOffset = 20,
        };
        borderNode.AttachNode(this);

        labelNode = new TextNode
        {
            Position = new Vector2(0.0f, HeaderHeight),
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

    private void ApplyFill()
    {
        var maxFillWidth = Math.Max(0.0f, Width - (FillInsetX * 2.0f));
        var fillHeight = Math.Max(0.0f, BarHeight - (FillInsetY * 2.0f));
        var fraction = lastTotal > 0
            ? Math.Clamp(lastOwned / (float)lastTotal, 0.0f, 1.0f)
            : 0.0f;

        fillNode.Position = new Vector2(FillInsetX, HeaderHeight + FillInsetY);
        fillNode.Height = fillHeight;
        fillNode.Width = maxFillWidth * fraction;
        fillNode.IsVisible = fillNode.Width > 0.5f;
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();

        headerNode.Size = new Vector2(Width, HeaderHeight);

        var barSize = new Vector2(Width, BarHeight);
        backgroundNode.Position = new Vector2(0.0f, HeaderHeight);
        backgroundNode.Size = barSize;
        borderNode.Position = new Vector2(0.0f, HeaderHeight);
        borderNode.Size = barSize;
        labelNode.Position = new Vector2(0.0f, HeaderHeight);
        labelNode.Size = barSize;

        ApplyFill();
    }
}
