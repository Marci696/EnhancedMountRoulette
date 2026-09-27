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

    private const float IconSize = 24.0f;
    private const float IconLeft = 6.0f;
    private const float NameGap = 8.0f;
    private const float NameLeft = IconLeft + IconSize + NameGap;
    private const float ToggleWidth = 70.0f;
    private const float ToggleRightPadding = 4.0f;

    private readonly IconImageNode iconNode;
    private readonly TextNode nameNode;
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

        toggleButton = new TextButtonNode
        {
            Position = new Vector2(320.0f, (ItemHeight - 24.0f) / 2.0f),
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
        toggleButton.AttachNode(this);

        Size = new Vector2(400.0f, ItemHeight);
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();

        iconNode.Position = new Vector2(IconLeft, (Height - IconSize) / 2.0f);

        nameNode.Position = new Vector2(NameLeft, 0.0f);
        nameNode.Size = new Vector2(
            Math.Max(40.0f, Width - NameLeft - ToggleWidth - ToggleRightPadding - 8.0f),
            Height
        );

        toggleButton.Position = new Vector2(
            Width - ToggleWidth - ToggleRightPadding,
            (Height - toggleButton.Height) / 2.0f
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

        toggleButton.String = itemData.IsInSummonList ? "Remove" : "Add";
    }
}
