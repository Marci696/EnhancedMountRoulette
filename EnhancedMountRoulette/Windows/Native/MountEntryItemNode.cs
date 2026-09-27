using System;
using System.Globalization;
using System.Numerics;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;

namespace EnhancedMountRoulette.Windows.Native;

public class MountEntryItemNode : ListItemNode<MountEntry>, IListItemNode
{
    public const float SeparatorHeight = 4.0f;
    private const float ContentHeight = 26.0f;

    public static float ItemHeight => SeparatorHeight + ContentHeight + SeparatorHeight;

    public const float IconSize = 24.0f;
    public const float IconLeft = 6.0f;
    public const float NameGap = 8.0f;
    public const float NameLeft = IconLeft + IconSize + NameGap;
    public const float SeatsWidth = 56.0f;
    public const float OwnedWidth = 72.0f;
    public const float PatchWidth = 52.0f;
    public const float ToggleWidth = 70.0f;
    public const float RightPadding = 4.0f;
    public const float ColumnGap = 6.0f;

    private readonly IconImageNode iconNode;
    private readonly TextNode nameNode;
    private readonly TextNode ownedNode;
    private readonly TextNode patchNode;
    private readonly TextNode seatsNode;
    private readonly TextButtonNode toggleButton;
    private readonly HorizontalLineNode topSeparator;
    private readonly HorizontalLineNode bottomSeparator;

    public MountEntryItemNode()
    {
        topSeparator = new HorizontalLineNode
        {
            Position = Vector2.Zero,
            Size = new Vector2(480.0f, SeparatorHeight),
        };
        topSeparator.AttachNode(this);

        iconNode = new IconImageNode
        {
            Position = new Vector2(IconLeft, SeparatorHeight + (ContentHeight - IconSize) / 2.0f),
            Size = new Vector2(IconSize, IconSize),
            TextureSize = new Vector2(IconSize, IconSize),
            FitTexture = true,
        };
        iconNode.AttachNode(this);

        nameNode = new TextNode
        {
            Position = new Vector2(NameLeft, SeparatorHeight),
            Size = new Vector2(200.0f, ContentHeight),
            FontSize = 12,
            LineSpacing = 12,
            AlignmentType = AlignmentType.Left,
        };
        nameNode.AttachNode(this);

        ownedNode = new TextNode
        {
            Position = new Vector2(220.0f, SeparatorHeight),
            Size = new Vector2(OwnedWidth, ContentHeight),
            FontSize = 12,
            LineSpacing = 12,
            AlignmentType = AlignmentType.Center,
        };
        ownedNode.AttachNode(this);

        patchNode = new TextNode
        {
            Position = new Vector2(280.0f, SeparatorHeight),
            Size = new Vector2(PatchWidth, ContentHeight),
            FontSize = 12,
            LineSpacing = 12,
            AlignmentType = AlignmentType.Center,
        };
        patchNode.AttachNode(this);

        seatsNode = new TextNode
        {
            Position = new Vector2(320.0f, SeparatorHeight),
            Size = new Vector2(SeatsWidth, ContentHeight),
            FontSize = 12,
            LineSpacing = 12,
            AlignmentType = AlignmentType.Center,
        };
        seatsNode.AttachNode(this);

        toggleButton = new TextButtonNode
        {
            Position = new Vector2(400.0f, SeparatorHeight + (ContentHeight - 24.0f) / 2.0f),
            Size = new Vector2(ToggleWidth, 24.0f),
            String = "Add",
            OnClick = () =>
            {
                if (ItemData is { } entry)
                {
                    entry.ToggleMembership(entry);
                }
            },
        };
        NativeButtonStyles.StyleAsAdd(toggleButton);
        toggleButton.AttachNode(this);

        bottomSeparator = new HorizontalLineNode
        {
            Position = new Vector2(0.0f, ItemHeight - SeparatorHeight),
            Size = new Vector2(480.0f, SeparatorHeight),
        };
        bottomSeparator.AttachNode(this);

        Size = new Vector2(480.0f, ItemHeight);
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();

        var contentHeight = Math.Max(1.0f, Height - (SeparatorHeight * 2.0f));

        topSeparator.Position = Vector2.Zero;
        topSeparator.Size = new Vector2(Width, SeparatorHeight);

        bottomSeparator.Position = new Vector2(0.0f, Height - SeparatorHeight);
        bottomSeparator.Size = new Vector2(Width, SeparatorHeight);

        iconNode.Position = new Vector2(IconLeft, SeparatorHeight + (contentHeight - IconSize) / 2.0f);

        toggleButton.Position = new Vector2(
            Width - ToggleWidth - RightPadding,
            SeparatorHeight + (contentHeight - toggleButton.Height) / 2.0f
        );

        seatsNode.Position = new Vector2(
            toggleButton.X - ColumnGap - SeatsWidth,
            SeparatorHeight
        );
        seatsNode.Size = new Vector2(SeatsWidth, contentHeight);

        patchNode.Position = new Vector2(
            seatsNode.X - ColumnGap - PatchWidth,
            SeparatorHeight
        );
        patchNode.Size = new Vector2(PatchWidth, contentHeight);

        ownedNode.Position = new Vector2(
            patchNode.X - ColumnGap - OwnedWidth,
            SeparatorHeight
        );
        ownedNode.Size = new Vector2(OwnedWidth, contentHeight);

        nameNode.Position = new Vector2(NameLeft, SeparatorHeight);
        nameNode.Size = new Vector2(
            Math.Max(40.0f, ownedNode.X - ColumnGap - NameLeft),
            contentHeight
        );
    }

    protected override void SetNodeData(MountEntry itemData)
    {
        iconNode.IconId = itemData.Mount.Icon;
        iconNode.Alpha = itemData.IsInSummonList ? 1.0f : 0.35f;

        nameNode.String = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
            itemData.Mount.Singular.ExtractText()
        );
        nameNode.TextColor = itemData.IsInSummonList
            ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
            : new Vector4(0.6f, 0.6f, 0.6f, 1.0f);

        ownedNode.String = itemData.OwnedDisplay;
        ownedNode.TextColor = nameNode.TextColor;

        patchNode.String = string.IsNullOrEmpty(itemData.Patch) ? "—" : itemData.Patch;
        patchNode.TextColor = nameNode.TextColor;

        seatsNode.String = itemData.SeatCount.ToString(CultureInfo.InvariantCulture);
        seatsNode.TextColor = nameNode.TextColor;

        toggleButton.String = itemData.IsInSummonList ? "Remove" : "Add";
        if (itemData.IsInSummonList)
        {
            NativeButtonStyles.StyleAsRemove(toggleButton);
        }
        else
        {
            NativeButtonStyles.StyleAsAdd(toggleButton);
        }
    }
}
