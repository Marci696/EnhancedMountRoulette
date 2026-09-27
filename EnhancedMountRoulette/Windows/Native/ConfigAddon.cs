using System;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;
using EnhancedMountRoulette.Configuration;

namespace EnhancedMountRoulette.Windows.Native;

public unsafe class ConfigAddon : NativeAddon
{
    private const float LeftColumnWidth = 240.0f;
    private const float OwnershipProgressWidth = LeftColumnWidth * 4.0f / 3.0f;
    private const float ColumnDividerWidth = 4.0f;
    private const float ColumnSpacing = 16.0f;
    private const float MainLayoutSpacing = 6.0f;
    private const float FooterLineHeight = 4.0f;
    private const float LeftChromeHeight = 28.0f + 18.0f + (MainLayoutSpacing * 2.0f);

    private VerticalListNode? mainLayout;
    private HorizontalListNode? columnsLayout;
    private VerticalListNode? leftColumn;
    private VerticalLineNode? columnDivider;
    private HorizontalLineNode? footerDivider;
    private ListNode<MountList, MountListItemNode>? mountListNode;
    private OwnedMountsProgressNode? ownershipProgressNode;
    private MountListEditorNode? editorNode;

    private MountList? selectedMountList;

    protected override void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan)
    {
        mainLayout = new VerticalListNode
        {
            Position = ContentStartPosition,
            Size = ContentSize,
            ItemSpacing = MainLayoutSpacing,
            FitWidth = false,
        };
        mainLayout.AttachNode(this);

        var columnsHeight = ContentSize.Y
            - FooterLineHeight
            - OwnedMountsProgressNode.PreferredHeight
            - (MainLayoutSpacing * 2.0f);

        columnsLayout = new HorizontalListNode
        {
            Size = new Vector2(ContentSize.X, columnsHeight),
            ItemSpacing = ColumnSpacing,
        };
        mainLayout.AddNode(columnsLayout);

        leftColumn = new VerticalListNode
        {
            Size = new Vector2(LeftColumnWidth, columnsHeight),
            ItemSpacing = MainLayoutSpacing,
            FitWidth = true,
        };
        columnsLayout.AddNode(leftColumn);

        var addButtons = new HorizontalListNode
        {
            Size = new Vector2(LeftColumnWidth, 28.0f),
            ItemSpacing = 4.0f,
        };

        var addWhitelist = new TextButtonNode
        {
            Size = new Vector2(118.0f, 28.0f),
            String = "Add Whitelist",
            OnClick = () =>
            {
                ConfigManager.Instance.StoreMountList(
                    new MountList
                    {
                        Name = ConfigManager.Instance.FindNewMountListName(),
                        Type = MountListType.Whitelist,
                    }
                );
                RefreshMountLists();
            },
        };
        NativeButtonStyles.StyleAsAdd(addWhitelist);

        var addBlacklist = new TextButtonNode
        {
            Size = new Vector2(118.0f, 28.0f),
            String = "Add Blacklist",
            OnClick = () =>
            {
                ConfigManager.Instance.StoreMountList(
                    new MountList
                    {
                        Name = ConfigManager.Instance.FindNewMountListName(),
                        Type = MountListType.Blacklist,
                    }
                );
                RefreshMountLists();
            },
        };
        NativeButtonStyles.StyleAsAdd(addBlacklist);

        addButtons.AddNode(addWhitelist);
        addButtons.AddNode(addBlacklist);
        leftColumn.AddNode(addButtons);

        var listHeader = new ResNode
        {
            Size = new Vector2(LeftColumnWidth, 18.0f),
        };

        var headerContentWidth = LeftColumnWidth - MountListItemNode.ListContentRightInset;

        var defaultHeader = new TextNode
        {
            Position = new Vector2(headerContentWidth - MountListItemNode.CheckboxColumnWidth, 0.0f),
            Size = new Vector2(MountListItemNode.CheckboxColumnWidth, 18.0f),
            FontSize = 11,
            LineSpacing = 11,
            AlignmentType = AlignmentType.Center,
            TextColor = new Vector4(0.85f, 0.85f, 0.85f, 1.0f),
            String = "Default?",
        };
        defaultHeader.AttachNode(listHeader);

        var nameHeader = new TextNode
        {
            Position = new Vector2(MountListItemNode.TextLeft, 0.0f),
            Size = new Vector2(
                headerContentWidth - MountListItemNode.TextLeft - MountListItemNode.CheckboxColumnWidth - 4.0f,
                18.0f
            ),
            FontSize = 11,
            LineSpacing = 11,
            AlignmentType = AlignmentType.Left,
            TextColor = new Vector4(0.85f, 0.85f, 0.85f, 1.0f),
            String = "List",
        };
        nameHeader.AttachNode(listHeader);

        leftColumn.AddNode(listHeader);

        MountListItemNode.OnListsChanged = RefreshMountLists;

        mountListNode = new ListNode<MountList, MountListItemNode>
        {
            Size = new Vector2(LeftColumnWidth, Math.Max(80.0f, columnsHeight - LeftChromeHeight)),
            ItemSpacing = 2.0f,
            OptionsList = ConfigManager.Instance.OrderedMountList,
            OnItemSelected = OnMountListSelected,
            AutoResetScroll = false,
        };
        leftColumn.AddNode(mountListNode);

        editorNode = new MountListEditorNode
        {
            Size = new Vector2(ContentSize.X - LeftColumnWidth - ColumnSpacing, columnsHeight),
            OnListsChanged = RefreshMountLists,
            GetOwnerAddonId = () => (uint)AddonId,
        };
        columnsLayout.AddNode(editorNode);

        // Overlay only — VerticalLineNode.Size bypasses Width/Height overrides and would
        // report ContentSize.Y as layout width if added to the horizontal list.
        columnDivider = new VerticalLineNode();
        columnDivider.Width = ColumnDividerWidth;
        columnDivider.Height = columnsHeight;
        columnDivider.Position = new Vector2(
            LeftColumnWidth + (ColumnSpacing - ColumnDividerWidth) / 2.0f,
            0.0f
        );
        columnDivider.AttachNode(columnsLayout);

        footerDivider = new HorizontalLineNode
        {
            Size = new Vector2(ContentSize.X, FooterLineHeight),
        };
        mainLayout.AddNode(footerDivider);

        ownershipProgressNode = new OwnedMountsProgressNode
        {
            Size = new Vector2(OwnershipProgressWidth, OwnedMountsProgressNode.PreferredHeight),
        };
        mainLayout.AddNode(ownershipProgressNode);
        ownershipProgressNode.Refresh();

        if (ConfigManager.Instance.OrderedMountList.FirstOrDefault() is { } first)
        {
            selectedMountList = first;
            editorNode.Bind(first);
        }
    }

    protected override void OnUpdate(AtkUnitBase* addon)
    {
        mountListNode?.Update();
        editorNode?.Update();
    }

    protected override void OnHide(AtkUnitBase* addon)
    {
        // Dropdown popups reattach to the addon root while open; collapse first
        // so Escape → Close does not finalize them with live event links.
        editorNode?.CollapseOpenDropDowns();
    }

    private void OnMountListSelected(MountList? mountList)
    {
        if (mountList is null)
        {
            return;
        }

        selectedMountList = mountList;
        editorNode?.Bind(mountList);
    }

    private void RefreshMountLists()
    {
        if (mountListNode is null)
        {
            return;
        }

        var lists = ConfigManager.Instance.OrderedMountList;
        mountListNode.OptionsList = lists;

        if (selectedMountList is null || lists.All(list => list.Id != selectedMountList.Id))
        {
            selectedMountList = lists.FirstOrDefault();
        }
        else
        {
            selectedMountList = lists.First(list => list.Id == selectedMountList.Id);
        }

        if (selectedMountList is not null)
        {
            editorNode?.Bind(selectedMountList);
        }

        ownershipProgressNode?.Refresh();
    }
}
