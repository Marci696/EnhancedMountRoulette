using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using EnhancedMountRoulette.Configuration;

namespace EnhancedMountRoulette.Windows.Native;

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

    private readonly CheckboxNode defaultCheckbox;
    private readonly TextNode nameNode;
    private readonly TextNode metaNode;

    public MountListItemNode()
    {
        nameNode = new TextNode
        {
            Position = new Vector2(TextLeft, 4.0f),
            Size = new Vector2(180.0f, 16.0f),
            FontSize = 12,
            LineSpacing = 12,
            AlignmentType = AlignmentType.Left,
        };
        nameNode.AttachNode(this);

        metaNode = new TextNode
        {
            Position = new Vector2(TextLeft, 20.0f),
            Size = new Vector2(180.0f, 14.0f),
            FontSize = 10,
            LineSpacing = 10,
            AlignmentType = AlignmentType.Left,
            TextColor = new Vector4(0.75f, 0.75f, 0.75f, 1.0f),
        };
        metaNode.AttachNode(this);

        defaultCheckbox = new CheckboxNode
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
        defaultCheckbox.AttachNode(this);

        Size = new Vector2(240.0f, ItemHeight);
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();

        // CheckboxNode draws its box on the left (~Height-4 wide); center that box in the column.
        var boxSize = CheckboxSize - 4.0f;
        var checkboxX = Width - CheckboxColumnWidth + (CheckboxColumnWidth - boxSize) / 2.0f;
        defaultCheckbox.Position = new Vector2(checkboxX, (Height - CheckboxSize) / 2.0f);
        defaultCheckbox.Size = new Vector2(CheckboxSize, CheckboxSize);

        var textWidth = Math.Max(40.0f, Width - TextLeft - CheckboxColumnWidth - TextRightPadding);
        nameNode.Position = new Vector2(TextLeft, 4.0f);
        nameNode.Size = new Vector2(textWidth, 16.0f);

        metaNode.Position = new Vector2(TextLeft, 20.0f);
        metaNode.Size = new Vector2(textWidth, 14.0f);
    }

    protected override void SetNodeData(MountList itemData)
    {
        nameNode.String = itemData.Name;
        metaNode.String = $"{itemData.Type} · {itemData.FetchNextType}";

        var previousOnClick = defaultCheckbox.OnClick;
        defaultCheckbox.OnClick = null;
        defaultCheckbox.IsChecked = itemData.IsDefault;
        defaultCheckbox.OnClick = previousOnClick;
    }
}
