using System;
using System.Numerics;
using EnhancedMountRoulette.Configuration;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;

namespace EnhancedMountRoulette.Addons.Settings.MountListsOverview;

public class MountListItemNode : ListItemNode<MountList>, IListItemNode
{
    public static float ItemHeight => 40.0f;

    public static Action? OnListsChanged { get; set; }

    public const float CheckboxSize = 20.0f;
    public const float CheckboxColumnWidth = 54.0f;
    public const float TextLeft = 8.0f;
    public const float TextRightPadding = 4.0f;

    /// <summary>
    /// Matches ListNode item width: scrollbar (8) + inner padding (8).
    /// </summary>
    public const float ListContentRightInset = 16.0f;

    private static readonly Vector4 OddRowBackground = new(1.0f, 1.0f, 1.0f, 0.08f);
    private static readonly Vector4 SubtitleTextColor = new(0.75f, 0.75f, 0.75f, 1.0f);

    private readonly ColorImageNode stripeNode;
    private readonly CheckboxNode defaultCheckbox;
    private readonly TextNode nameNode;
    private readonly TextNode subtitleNode;

    public MountListItemNode()
    {
        stripeNode = AddStripe();
        nameNode = AddName();
        subtitleNode = AddSubtitle();
        defaultCheckbox = AddDefaultCheckbox();
        Size = new Vector2(240.0f, ItemHeight);
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        LayoutColumns();
    }

    protected override void SetNodeData(MountList itemData)
    {
        UpdateStripe(itemData);
        UpdateTexts(itemData);
        UpdateDefaultCheckbox(itemData);
    }

    private ColorImageNode AddStripe()
    {
        var node = new ColorImageNode
        {
            Position = Vector2.Zero,
            Size = new Vector2(240.0f, ItemHeight),
            Color = OddRowBackground,
            IsVisible = false,
        };
        node.AttachNode(this, NodePosition.AsFirstChild);
        return node;
    }

    private TextNode AddName()
    {
        var node = new TextNode
        {
            Position = new Vector2(TextLeft, 4.0f),
            Size = new Vector2(180.0f, 16.0f),
            FontSize = 12,
            LineSpacing = 12,
            AlignmentType = AlignmentType.Left,
        };
        node.AttachNode(this);
        return node;
    }

    private TextNode AddSubtitle()
    {
        var node = new TextNode
        {
            Position = new Vector2(TextLeft, 20.0f),
            Size = new Vector2(180.0f, 14.0f),
            FontSize = 10,
            LineSpacing = 10,
            AlignmentType = AlignmentType.Left,
            TextColor = SubtitleTextColor,
        };
        node.AttachNode(this);
        return node;
    }

    private CheckboxNode AddDefaultCheckbox()
    {
        var checkbox = new CheckboxNode
        {
            Position = new Vector2(200.0f, (ItemHeight - CheckboxSize) / 2.0f),
            Size = new Vector2(CheckboxSize, CheckboxSize),
            String = string.Empty,
            OnClick = isChecked =>
            {
                if (ItemData is null)
                {
                    return;
                }

                ConfigManager.Instance.StoreMountList(new MountList(ItemData) { IsDefault = isChecked });
                OnListsChanged?.Invoke();
            },
        };
        checkbox.AttachNode(this);
        return checkbox;
    }

    private void LayoutColumns()
    {
        stripeNode.Size = Size;

        // CheckboxNode draws its box on the left (~Height-4 wide); center that box in the column.
        var boxSize = CheckboxSize - 4.0f;
        var checkboxX = Width - CheckboxColumnWidth + (CheckboxColumnWidth - boxSize) / 2.0f;
        defaultCheckbox.Position = new Vector2(checkboxX, (Height - CheckboxSize) / 2.0f);
        defaultCheckbox.Size = new Vector2(CheckboxSize, CheckboxSize);

        var textWidth = Math.Max(40.0f, Width - TextLeft - CheckboxColumnWidth - TextRightPadding);
        nameNode.Position = new Vector2(TextLeft, 4.0f);
        nameNode.Size = new Vector2(textWidth, 16.0f);

        subtitleNode.Position = new Vector2(TextLeft, 20.0f);
        subtitleNode.Size = new Vector2(textWidth, 14.0f);
    }

    private void UpdateStripe(MountList itemData)
    {
        var rowIndex = ConfigManager.Instance.OrderedMountList.FindIndex(list => list.Id == itemData.Id);
        stripeNode.IsVisible = rowIndex >= 0 && rowIndex % 2 == 1;
    }

    private void UpdateTexts(MountList itemData)
    {
        nameNode.String = itemData.Name;
        subtitleNode.String = $"{itemData.Type} · {itemData.FetchNextType}";
    }

    private void UpdateDefaultCheckbox(MountList itemData)
    {
        var previousOnClick = defaultCheckbox.OnClick;
        defaultCheckbox.OnClick = null;
        defaultCheckbox.IsChecked = itemData.IsDefault;
        defaultCheckbox.OnClick = previousOnClick;
    }
}
