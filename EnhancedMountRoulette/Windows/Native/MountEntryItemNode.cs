using System;
using System.Globalization;
using System.Numerics;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;

namespace EnhancedMountRoulette.Windows.Native;

public unsafe class MountEntryItemNode : ListItemNode<MountEntry>, IListItemNode
{
    public static Action<Mount>? OnOpenContextMenu { get; set; }

    public static float ItemHeight => 28.0f;

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

    private static readonly Vector4 OddRowBackground = new(1.0f, 1.0f, 1.0f, 0.08f);

    private readonly ColorImageNode stripeNode;
    private readonly IconImageNode iconNode;
    private readonly TextNode nameNode;
    private readonly TextNode ownedNode;
    private readonly TextNode patchNode;
    private readonly TextNode seatsNode;
    private readonly TextButtonNode toggleButton;

    public MountEntryItemNode()
    {
        stripeNode = new ColorImageNode
        {
            Position = Vector2.Zero,
            Size = new Vector2(480.0f, ItemHeight),
            Color = OddRowBackground,
            IsVisible = false,
        };
        stripeNode.AttachNode(this, NodePosition.AsFirstChild);

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

        ownedNode = new TextNode
        {
            Position = new Vector2(220.0f, 0.0f),
            Size = new Vector2(OwnedWidth, ItemHeight),
            FontSize = 12,
            LineSpacing = 12,
            AlignmentType = AlignmentType.Center,
        };
        ownedNode.AttachNode(this);

        patchNode = new TextNode
        {
            Position = new Vector2(280.0f, 0.0f),
            Size = new Vector2(PatchWidth, ItemHeight),
            FontSize = 12,
            LineSpacing = 12,
            AlignmentType = AlignmentType.Center,
        };
        patchNode.AttachNode(this);

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

        AddEvent(AtkEventType.MouseDown, OnRowMouseDown);

        Size = new Vector2(480.0f, ItemHeight);
    }

    private unsafe void OnRowMouseDown(
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

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();

        stripeNode.Size = Size;

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

        patchNode.Position = new Vector2(
            seatsNode.X - ColumnGap - PatchWidth,
            0.0f
        );
        patchNode.Size = new Vector2(PatchWidth, Height);

        ownedNode.Position = new Vector2(
            patchNode.X - ColumnGap - OwnedWidth,
            0.0f
        );
        ownedNode.Size = new Vector2(OwnedWidth, Height);

        nameNode.Position = new Vector2(NameLeft, 0.0f);
        nameNode.Size = new Vector2(
            Math.Max(40.0f, ownedNode.X - ColumnGap - NameLeft),
            Height
        );
    }

    protected override void SetNodeData(MountEntry itemData)
    {
        stripeNode.IsVisible = itemData.RowIndex % 2 == 1;

        iconNode.IconId = itemData.Mount.Icon;
        iconNode.Alpha = itemData.IsOwned
            ? (itemData.IsInSummonList ? 1.0f : 0.35f)
            : 0.25f;

        nameNode.String = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
            itemData.Mount.Singular.ExtractText()
        );
        nameNode.TextColor = itemData.IsOwned
            ? (itemData.IsInSummonList
                ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                : new Vector4(0.6f, 0.6f, 0.6f, 1.0f))
            : new Vector4(0.45f, 0.45f, 0.45f, 1.0f);

        ownedNode.String = itemData.OwnedDisplay;
        ownedNode.TextColor = nameNode.TextColor;

        patchNode.String = string.IsNullOrEmpty(itemData.Patch) ? "—" : itemData.Patch;
        patchNode.TextColor = nameNode.TextColor;

        seatsNode.String = itemData.SeatCount.ToString(CultureInfo.InvariantCulture);
        seatsNode.TextColor = nameNode.TextColor;

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
}
