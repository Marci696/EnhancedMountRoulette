using System;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using EnhancedMountRoulette.Configuration;

namespace EnhancedMountRoulette.Windows.Native;

/// <summary>
/// Left-side mount-lists overview: add whitelist/blacklist, list header, and selectable lists.
/// </summary>
public class MountListsOverviewNode : VerticalListNode
{
    public const float PreferredWidth = 240.0f;

    private const float ItemSpacingY = 6.0f;
    private const float AddButtonHeight = 28.0f;
    private const float ListHeaderHeight = 18.0f;
    private const float ChromeHeight = AddButtonHeight + ListHeaderHeight + (ItemSpacingY * 2.0f);
    private const float HeaderTextGray = 0.85f;

    private readonly ListNode<MountList, MountListItemNode> mountListNode;

    private MountList? selectedMountList;

    public Action? OnListsChanged { get; set; }

    public Action<MountList>? OnMountListSelected { get; set; }

    public MountList? SelectedMountList => selectedMountList;

    public MountListsOverviewNode(float height)
    {
        Size = new Vector2(PreferredWidth, height);
        ItemSpacing = ItemSpacingY;
        FitWidth = true;

        AddNode(CreateAddButtonsRow());
        AddNode(CreateListHeader());

        MountListItemNode.OnListsChanged = () => OnListsChanged?.Invoke();

        mountListNode = new ListNode<MountList, MountListItemNode>
        {
            Size = new Vector2(PreferredWidth, Math.Max(80.0f, height - ChromeHeight)),
            ItemSpacing = 2.0f,
            OptionsList = ConfigManager.Instance.OrderedMountList,
            OnItemSelected = HandleItemSelected,
            AutoResetScroll = false,
        };
        AddNode(mountListNode);
    }

    public void Update()
    {
        mountListNode.Update();
    }

    public void DetachCallbacks()
    {
        MountListItemNode.OnListsChanged = null;
    }

    /// <summary>
    /// Reloads lists from config and keeps (or restores) a valid selection.
    /// </summary>
    public void Refresh()
    {
        var lists = ConfigManager.Instance.OrderedMountList;

        if (selectedMountList is null || lists.All(list => list.Id != selectedMountList.Id))
        {
            selectedMountList = lists.FirstOrDefault();
        }
        else
        {
            selectedMountList = lists.First(list => list.Id == selectedMountList.Id);
        }

        // OptionsList assignment may FullRebuild and clear SelectedItems; set selection after.
        mountListNode.OptionsList = lists;
        SyncSelectionHighlight();
    }

    public void Select(MountList mountList)
    {
        selectedMountList = mountList;
        SyncSelectionHighlight();
    }

    private void HandleItemSelected(MountList? mountList)
    {
        if (mountList is null)
        {
            return;
        }

        selectedMountList = mountList;
        OnMountListSelected?.Invoke(mountList);
    }

    private void SyncSelectionHighlight()
    {
        mountListNode.SelectedItems.Clear();
        if (selectedMountList is not null)
        {
            mountListNode.SelectedItems.Add(selectedMountList);
        }

        mountListNode.Update();
    }

    private HorizontalListNode CreateAddButtonsRow()
    {
        var addButtons = new HorizontalListNode
        {
            Size = new Vector2(PreferredWidth, AddButtonHeight),
            ItemSpacing = 4.0f,
        };

        addButtons.AddNode(CreateAddListButton("Add Whitelist", MountListType.Whitelist));
        addButtons.AddNode(CreateAddListButton("Add Blacklist", MountListType.Blacklist));
        return addButtons;
    }

    private TextButtonNode CreateAddListButton(string label, MountListType listType)
    {
        var button = new TextButtonNode
        {
            Size = new Vector2(118.0f, AddButtonHeight),
            String = label,
            OnClick = () =>
            {
                ConfigManager.Instance.StoreMountList(
                    new MountList
                    {
                        Name = ConfigManager.Instance.FindNewMountListName(),
                        Type = listType,
                    }
                );
                OnListsChanged?.Invoke();
            },
        };
        NativeButtonStyles.StyleAsAdd(button);
        return button;
    }

    private static ResNode CreateListHeader()
    {
        var listHeader = new ResNode
        {
            Size = new Vector2(PreferredWidth, ListHeaderHeight),
        };

        var headerContentWidth = PreferredWidth - MountListItemNode.ListContentRightInset;
        var headerTextColor = new Vector4(HeaderTextGray, HeaderTextGray, HeaderTextGray, 1.0f);

        var nameHeader = new TextNode
        {
            Position = new Vector2(MountListItemNode.TextLeft, 0.0f),
            Size = new Vector2(
                headerContentWidth - MountListItemNode.TextLeft - MountListItemNode.CheckboxColumnWidth - 4.0f,
                ListHeaderHeight
            ),
            FontSize = 11,
            LineSpacing = 11,
            AlignmentType = AlignmentType.Left,
            TextColor = headerTextColor,
            String = "List",
        };
        nameHeader.AttachNode(listHeader);

        var defaultHeader = new TextNode
        {
            Position = new Vector2(headerContentWidth - MountListItemNode.CheckboxColumnWidth, 0.0f),
            Size = new Vector2(MountListItemNode.CheckboxColumnWidth, ListHeaderHeight),
            FontSize = 11,
            LineSpacing = 11,
            AlignmentType = AlignmentType.Center,
            TextColor = headerTextColor,
            String = "Default?",
        };
        defaultHeader.AttachNode(listHeader);

        return listHeader;
    }
}
