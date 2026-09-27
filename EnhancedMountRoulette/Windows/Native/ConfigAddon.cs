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
    private HorizontalListNode? rootLayout;
    private VerticalListNode? leftColumn;
    private ListNode<MountList, MountListItemNode>? mountListNode;
    private MountListEditorNode? editorNode;

    private MountList? selectedMountList;

    protected override void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan)
    {
        rootLayout = new HorizontalListNode
        {
            Position = ContentStartPosition,
            Size = ContentSize,
            ItemSpacing = 8.0f,
        };
        rootLayout.AttachNode(this);

        leftColumn = new VerticalListNode
        {
            Size = new Vector2(240.0f, ContentSize.Y),
            ItemSpacing = 6.0f,
            FitWidth = true,
        };
        rootLayout.AddNode(leftColumn);

        var addButtons = new HorizontalListNode
        {
            Size = new Vector2(240.0f, 28.0f),
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

        addButtons.AddNode(addWhitelist);
        addButtons.AddNode(addBlacklist);
        leftColumn.AddNode(addButtons);

        var listHeader = new ResNode
        {
            Size = new Vector2(240.0f, 18.0f),
        };

        var defaultHeader = new TextNode
        {
            Position = new Vector2(MountListItemNode.CheckboxLeft, 0.0f),
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
            Size = new Vector2(200.0f, 18.0f),
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
            Size = new Vector2(240.0f, ContentSize.Y - 56.0f),
            ItemSpacing = 2.0f,
            OptionsList = ConfigManager.Instance.OrderedMountList,
            OnItemSelected = OnMountListSelected,
            AutoResetScroll = false,
        };
        leftColumn.AddNode(mountListNode);

        editorNode = new MountListEditorNode
        {
            Size = new Vector2(ContentSize.X - 248.0f, ContentSize.Y),
            OnListsChanged = RefreshMountLists,
        };
        rootLayout.AddNode(editorNode);

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
    }
}
