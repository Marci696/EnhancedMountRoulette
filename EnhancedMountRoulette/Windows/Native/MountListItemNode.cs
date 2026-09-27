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
    public const float CheckboxLeft = 4.0f;
    public const float CheckboxColumnWidth = 54.0f;
    public const float TextLeft = CheckboxLeft + CheckboxColumnWidth + 2.0f;

    private readonly CheckboxNode defaultCheckbox;
    private readonly TextNode nameNode;
    private readonly TextNode metaNode;

    public MountListItemNode()
    {
        defaultCheckbox = new CheckboxNode
        {
            Position = new Vector2(
                CheckboxLeft + (CheckboxColumnWidth - CheckboxSize) / 2.0f,
                (ItemHeight - CheckboxSize) / 2.0f
            ),
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

        Size = new Vector2(240.0f, ItemHeight);
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();

        defaultCheckbox.Position = new Vector2(
            CheckboxLeft + (CheckboxColumnWidth - CheckboxSize) / 2.0f,
            (Height - CheckboxSize) / 2.0f
        );
        defaultCheckbox.Size = new Vector2(CheckboxSize, CheckboxSize);

        var textWidth = Math.Max(40.0f, Width - TextLeft - 4.0f);
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
