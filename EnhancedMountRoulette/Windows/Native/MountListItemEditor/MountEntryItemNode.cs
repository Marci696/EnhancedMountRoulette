using System;
using System.Globalization;
using System.Numerics;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using EnhancedMountRoulette.Windows.Native;

namespace EnhancedMountRoulette.Windows.Native.MountListItemEditor;

public unsafe class MountEntryItemNode : ListItemNode<MountEntry>, IListItemNode
{
    public static Action<Mount>? OnOpenContextMenu { get; set; }

    public static float ItemHeight => 28.0f;

    public const float MountIconSize = 24.0f;
    public const float MountIconLeft = 6.0f;
    public const float NameGap = 8.0f;
    public const float NameLeft = MountIconLeft + MountIconSize + NameGap;
    public const float SeatsWidth = 56.0f;
    public const float OwnedWidth = 72.0f;
    public const float PatchWidth = 52.0f;
    public const float ToggleWidth = 70.0f;
    public const float ToggleHeight = 24.0f;
    public const float RightPadding = 4.0f;
    public const float ColumnGap = 6.0f;

    private static readonly Vector4 OddRowBackground = new(1.0f, 1.0f, 1.0f, 0.08f);
    private static readonly Vector4 TextInList = new(1.0f, 1.0f, 1.0f, 1.0f);
    private static readonly Vector4 TextOwnedNotInList = new(0.6f, 0.6f, 0.6f, 1.0f);
    private static readonly Vector4 TextUnowned = new(0.45f, 0.45f, 0.45f, 1.0f);

    private readonly ColorImageNode stripeNode;
    private readonly IconImageNode mountIconNode;
    private readonly TextNode nameNode;
    private readonly TextNode ownedNode;
    private readonly TextNode patchNode;
    private readonly TextNode seatsNode;
    private readonly TextButtonNode toggleButton;

    public MountEntryItemNode()
    {
        stripeNode = AddStripe();
        mountIconNode = AddMountIcon();
        nameNode = AddName();
        ownedNode = AddOwned();
        patchNode = AddPatch();
        seatsNode = AddSeats();
        toggleButton = AddToggleButton();

        AddEvent(AtkEventType.MouseDown, OnRowMouseDown);
        Size = new Vector2(480.0f, ItemHeight);
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        LayoutColumns();
    }

    protected override void SetNodeData(MountEntry itemData)
    {
        UpdateStripe(itemData);
        UpdateMountIcon(itemData);
        UpdateTexts(itemData);
        UpdateToggleButton(itemData);
    }

    private ColorImageNode AddStripe()
    {
        var node = new ColorImageNode
        {
            Position = Vector2.Zero,
            Size = new Vector2(480.0f, ItemHeight),
            Color = OddRowBackground,
            IsVisible = false,
        };
        node.AttachNode(this, NodePosition.AsFirstChild);
        return node;
    }

    private IconImageNode AddMountIcon()
    {
        var node = new IconImageNode
        {
            Position = new Vector2(MountIconLeft, (ItemHeight - MountIconSize) / 2.0f),
            Size = new Vector2(MountIconSize, MountIconSize),
            TextureSize = new Vector2(MountIconSize, MountIconSize),
            FitTexture = true,
        };
        node.AttachNode(this);
        return node;
    }

    private TextNode AddName()
    {
        return AddColumnText(AlignmentType.Left);
    }

    private TextNode AddOwned()
    {
        return AddColumnText(AlignmentType.Center);
    }

    private TextNode AddPatch()
    {
        return AddColumnText(AlignmentType.Center);
    }

    private TextNode AddSeats()
    {
        return AddColumnText(AlignmentType.Center);
    }

    private TextNode AddColumnText(AlignmentType alignment)
    {
        var node = new TextNode
        {
            Position = Vector2.Zero,
            Size = new Vector2(40.0f, ItemHeight),
            FontSize = 12,
            LineSpacing = 12,
            AlignmentType = alignment,
        };
        node.AttachNode(this);
        return node;
    }

    private TextButtonNode AddToggleButton()
    {
        var button = new TextButtonNode
        {
            Position = new Vector2(0.0f, (ItemHeight - ToggleHeight) / 2.0f),
            Size = new Vector2(ToggleWidth, ToggleHeight),
            String = "Add",
            OnClick = () =>
            {
                if (ItemData is { } entry)
                {
                    entry.ToggleMembership(entry);
                }
            },
        };
        NativeButtonStyles.StyleAsAdd(button);
        button.AttachNode(this);
        return button;
    }

    private void LayoutColumns()
    {
        stripeNode.Size = Size;

        mountIconNode.Position = new Vector2(MountIconLeft, (Height - MountIconSize) / 2.0f);

        toggleButton.Position = new Vector2(
            Width - ToggleWidth - RightPadding,
            (Height - toggleButton.Height) / 2.0f
        );

        PlaceFixedColumn(seatsNode, toggleButton.X - ColumnGap - SeatsWidth, SeatsWidth);
        PlaceFixedColumn(patchNode, seatsNode.X - ColumnGap - PatchWidth, PatchWidth);
        PlaceFixedColumn(ownedNode, patchNode.X - ColumnGap - OwnedWidth, OwnedWidth);

        nameNode.Position = new Vector2(NameLeft, 0.0f);
        nameNode.Size = new Vector2(
            Math.Max(40.0f, ownedNode.X - ColumnGap - NameLeft),
            Height
        );
    }

    private void PlaceFixedColumn(TextNode node, float x, float width)
    {
        node.Position = new Vector2(x, 0.0f);
        node.Size = new Vector2(width, Height);
    }

    private void UpdateStripe(MountEntry itemData)
    {
        stripeNode.IsVisible = itemData.RowIndex % 2 == 1;
    }

    private void UpdateMountIcon(MountEntry itemData)
    {
        mountIconNode.IconId = itemData.Mount.Icon;
        mountIconNode.Alpha = itemData.IsOwned
            ? (itemData.IsInSummonList ? 1.0f : 0.35f)
            : 0.25f;
    }

    private void UpdateTexts(MountEntry itemData)
    {
        var textColor = ResolveTextColor(itemData);

        nameNode.String = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
            itemData.Mount.Singular.ExtractText()
        );
        nameNode.TextColor = textColor;

        ownedNode.String = itemData.OwnedDisplay;
        ownedNode.TextColor = textColor;

        patchNode.String = string.IsNullOrEmpty(itemData.Patch) ? "�" : itemData.Patch;
        patchNode.TextColor = textColor;

        seatsNode.String = itemData.SeatCount.ToString(CultureInfo.InvariantCulture);
        seatsNode.TextColor = textColor;
    }

    private void UpdateToggleButton(MountEntry itemData)
    {
        if (!itemData.IsOwned)
        {
            toggleButton.IsVisible = false;
            return;
        }

        toggleButton.IsVisible = true;
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

    private static Vector4 ResolveTextColor(MountEntry itemData)
    {
        if (!itemData.IsOwned)
        {
            return TextUnowned;
        }

        return itemData.IsInSummonList ? TextInList : TextOwnedNotInList;
    }

    private void OnRowMouseDown(
        AtkEventListener* thisPtr,
        AtkEventType eventType,
        int eventParam,
        AtkEvent* atkEvent,
        AtkEventData* atkEventData
    )
    {
        // ButtonId 1 = right mouse button
        if (atkEventData->MouseData.ButtonId is not 1)
        {
            return;
        }

        if (ItemData is not { IsOwned: true, Mount: var mount })
        {
            return;
        }

        OnOpenContextMenu?.Invoke(mount);
    }
}
