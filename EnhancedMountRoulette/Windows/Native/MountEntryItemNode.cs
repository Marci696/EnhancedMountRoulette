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
    public static float ItemHeight => 28.0f;

    public const float IconSize = 24.0f;
    public const float IconLeft = 6.0f;
    public const float NameGap = 8.0f;
    public const float NameLeft = IconLeft + IconSize + NameGap;
    public const float SeatsWidth = 56.0f;
    public const float ToggleWidth = 70.0f;
    public const float RightPadding = 4.0f;
    public const float ColumnGap = 6.0f;

    private readonly IconImageNode iconNode;
    private readonly TextNode nameNode;
    private readonly TextNode seatsNode;
    private readonly TextButtonNode toggleButton;

    public MountEntryItemNode()
    {
        iconNode = new IconImageNode
        {
            Position = new Vector2(IconLeft, (ItemHeight - IconSize) / 2.0f),
            Size = new Vector2(IconSize, IconSize),
            TextureSize = new Vector2(IconSize, IconSize),
            FitTexture = true,
        };
        iconNode.AttachNode(this);

        nameNode = new TextNode
        {
            Position = new Vector2(NameLeft, 0.0f),
            Size = new Vector2(200.0f, ItemHeight),
            FontSize = 12,
            LineSpacing = 12,
            AlignmentType = AlignmentType.Left,
        };
        nameNode.AttachNode(this);

        seatsNode = new TextNode
        {
            Position = new Vector2(320.0f, 0.0f),
            Size = new Vector2(SeatsWidth, ItemHeight),
            FontSize = 12,
            LineSpacing = 12,
            AlignmentType = AlignmentType.Center,
        };
        seatsNode.AttachNode(this);

        toggleButton = new TextButtonNode
        {
            Position = new Vector2(400.0f, (ItemHeight - 24.0f) / 2.0f),
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

        Size = new Vector2(480.0f, ItemHeight);
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();

        iconNode.Position = new Vector2(IconLeft, (Height - IconSize) / 2.0f);

        toggleButton.Position = new Vector2(
            Width - ToggleWidth - RightPadding,
            (Height - toggleButton.Height) / 2.0f
        );

        seatsNode.Position = new Vector2(
            toggleButton.X - ColumnGap - SeatsWidth,
            0.0f
        );
        seatsNode.Size = new Vector2(SeatsWidth, Height);

        nameNode.Position = new Vector2(NameLeft, 0.0f);
        nameNode.Size = new Vector2(
            Math.Max(40.0f, seatsNode.X - ColumnGap - NameLeft),
            Height
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
