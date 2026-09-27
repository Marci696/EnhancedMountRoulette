using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Gui.Toast;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.ContextMenu;
using KamiToolKit.Nodes;
using EnhancedMountRoulette.Commands;
using EnhancedMountRoulette.Configuration;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace EnhancedMountRoulette.Windows.Native;

public class MountListEditorNode : ResNode
{
    private const float SettingsRowY = 0.0f;
    private const float DividerY = 32.0f;
    private const float FilterRowY = 40.0f;
    private const float HeaderRowY = 76.0f;
    private const float MountsListY = 104.0f;
    private const float ActionsRowHeight = 28.0f;
    private const float ActionsRowGap = 6.0f;
    private const float HeaderButtonHeight = 24.0f;

    public System.Action? OnListsChanged { get; set; }

    private MountList? boundList;
    private string mountFilter = "";
    private MountSortMode sortMode = MountSortMode.Name;
    private bool sortAscending = true;
    private MountSelectionFilter selectionFilter = MountSelectionFilter.All;
    private MountSeatFilter seatFilter = MountSeatFilter.All;
    private bool ownedOnlyFilter = true;

    private readonly TextInputNode nameInput;
    private readonly StringDropDownNode typeDropDown;
    private readonly StringDropDownNode fetchTypeDropDown;
    private readonly TextButtonNode deleteButton;
    private readonly TextButtonNode copyMacroButton;
    private readonly TextInputNode searchInput;
    private readonly TextButtonNode addAllButton;
    private readonly TextButtonNode removeAllButton;
    private readonly EnumDropDownNode<MountSelectionFilter> selectionFilterDropDown;
    private readonly EnumDropDownNode<MountSeatFilter> seatFilterDropDown;
    private readonly TextNode selectionFilterLabel;
    private readonly TextNode seatFilterLabel;
    private readonly TextNode ownedFilterLabel;
    private readonly CheckboxNode ownedFilterCheckbox;
    private readonly TextButtonNode nameSortButton;
    private readonly TextButtonNode seatsSortButton;
    private readonly TextButtonNode ownedSortButton;
    private readonly TextButtonNode patchSortButton;
    private readonly ListNode<MountEntry, MountEntryItemNode> mountsNode;
    private readonly TextNode emptyHint;
    private readonly ResNode columnHeader;
    private readonly HorizontalListNode settingsRow;
    private readonly HorizontalLineNode settingsDivider;
    private readonly HorizontalListNode filterRow;
    private readonly HorizontalListNode actionsRow;

    private readonly ConfirmationDialogNode confirmationDialog;

    private readonly ContextMenu mountContextMenu = new();

    public MountListEditorNode()
    {
        confirmationDialog = new ConfirmationDialogNode
        {
            Position = Vector2.Zero,
            Size = new Vector2(600.0f, 400.0f),
        };

        settingsRow = new HorizontalListNode
        {
            Position = new Vector2(0.0f, SettingsRowY),
            Size = new Vector2(600.0f, 28.0f),
            ItemSpacing = 6.0f,
        };
        settingsRow.AttachNode(this);

        nameInput = new TextInputNode
        {
            Size = new Vector2(160.0f, 28.0f),
            PlaceholderString = "List name",
            MaxCharacters = 50,
            OnInputComplete = value => RenameList(value.ToString()),
        };
        nameInput.OnFocusLost = () => RenameList(nameInput.String.ToString());
        settingsRow.AddNode(nameInput);

        typeDropDown = new StringDropDownNode
        {
            Size = new Vector2(110.0f, 28.0f),
            Options = Enum.GetNames<MountListType>().ToList(),
            OnOptionSelected = option =>
            {
                if (boundList is null || !Enum.TryParse<MountListType>(option, out var type))
                {
                    return;
                }

                ConfigManager.Instance.ChangeMountListType(boundList, type);
                RefreshBoundList();
                OnListsChanged?.Invoke();
            },
        };
        settingsRow.AddNode(typeDropDown);

        fetchTypeDropDown = new StringDropDownNode
        {
            Size = new Vector2(150.0f, 28.0f),
            Options = Enum.GetNames<FetchNextType>().ToList(),
            OnOptionSelected = option =>
            {
                if (boundList is null || !Enum.TryParse<FetchNextType>(option, out var fetchType))
                {
                    return;
                }

                ConfigManager.Instance.StoreMountList(new MountList(boundList) { FetchNextType = fetchType });
                RefreshBoundList();
                OnListsChanged?.Invoke();
            },
        };
        settingsRow.AddNode(fetchTypeDropDown);

        deleteButton = new TextButtonNode
        {
            Size = new Vector2(70.0f, 28.0f),
            String = "Delete",
            OnClick = ConfirmAndDeleteList,
        };
        NativeButtonStyles.StyleAsRemove(deleteButton);
        settingsRow.AddNode(deleteButton);

        copyMacroButton = new TextButtonNode
        {
            Size = new Vector2(90.0f, 28.0f),
            String = "Copy Macro",
            OnClick = () =>
            {
                if (boundList is null)
                {
                    return;
                }

                Dalamud.Bindings.ImGui.ImGui.SetClipboardText(SummonMountCommand.GetMacro(boundList));
                Plugin.ToastGui.ShowNormal(
                    "Copied to clipboard",
                    new ToastOptions { Position = ToastPosition.Bottom, Speed = ToastSpeed.Fast }
                );
            },
        };
        settingsRow.AddNode(copyMacroButton);

        settingsDivider = new HorizontalLineNode
        {
            Position = new Vector2(0.0f, DividerY),
            Size = new Vector2(600.0f, 4.0f),
        };
        settingsDivider.AttachNode(this);

        filterRow = new HorizontalListNode
        {
            Position = new Vector2(0.0f, FilterRowY),
            Size = new Vector2(600.0f, 28.0f),
            ItemSpacing = 6.0f,
        };
        filterRow.AttachNode(this);

        searchInput = new TextInputNode
        {
            Size = new Vector2(145.0f, 28.0f),
            PlaceholderString = "Search mounts...",
            OnInputReceived = value =>
            {
                mountFilter = value.ToString();
                RefreshMountEntries();
            },
        };
        filterRow.AddNode(searchInput);

        selectionFilterLabel = CreateFilterCategoryLabel("Selected:", 70.0f);
        filterRow.AddNode(selectionFilterLabel);

        selectionFilterDropDown = new EnumDropDownNode<MountSelectionFilter>
        {
            Size = new Vector2(140.0f, 28.0f),
            Options =
            [
                MountSelectionFilter.All,
                MountSelectionFilter.Selected,
                MountSelectionFilter.Unselected,
            ],
            SelectedOption = MountSelectionFilter.All,
            OnOptionSelected = option =>
            {
                selectionFilter = option;
                RefreshMountEntries();
            },
        };
        selectionFilterDropDown.GetLabelFunction = FormatSelectionFilterLabel;
        filterRow.AddNode(selectionFilterDropDown);

        seatFilterLabel = CreateFilterCategoryLabel("Seats:", 48.0f);
        filterRow.AddNode(seatFilterLabel);

        seatFilterDropDown = new EnumDropDownNode<MountSeatFilter>
        {
            Size = new Vector2(90.0f, 28.0f),
            Options =
            [
                MountSeatFilter.All,
                MountSeatFilter.Single,
                MountSeatFilter.Multi,
            ],
            SelectedOption = MountSeatFilter.All,
            OnOptionSelected = option =>
            {
                seatFilter = option;
                RefreshMountEntries();
            },
        };
        seatFilterDropDown.GetLabelFunction = FormatSeatFilterLabel;
        filterRow.AddNode(seatFilterDropDown);

        ownedFilterLabel = CreateFilterCategoryLabel("Owned:", 52.0f);
        filterRow.AddNode(ownedFilterLabel);

        ownedFilterCheckbox = new CheckboxNode
        {
            Size = new Vector2(20.0f, 20.0f),
            String = string.Empty,
            IsChecked = true,
            OnClick = isChecked =>
            {
                ownedOnlyFilter = isChecked;
                RefreshMountEntries();
            },
        };
        // Attach directly to the filter row so component events stay under the list node tree.
        ownedFilterCheckbox.Y = (28.0f - ownedFilterCheckbox.Height) / 2.0f;
        filterRow.AddNode(ownedFilterCheckbox);

        columnHeader = new ResNode
        {
            Position = new Vector2(0.0f, HeaderRowY),
            Size = new Vector2(600.0f, HeaderButtonHeight),
        };
        columnHeader.AttachNode(this);

        nameSortButton = new TextButtonNode
        {
            Position = new Vector2(MountEntryItemNode.NameLeft, 0.0f),
            Size = new Vector2(200.0f, HeaderButtonHeight),
            String = "Name ▲",
            OnClick = () => ToggleSort(MountSortMode.Name),
        };
        nameSortButton.AttachNode(columnHeader);

        ownedSortButton = new TextButtonNode
        {
            Position = new Vector2(220.0f, 0.0f),
            Size = new Vector2(MountEntryItemNode.OwnedWidth, HeaderButtonHeight),
            String = "Own%",
            OnClick = () => ToggleSort(MountSortMode.Owned),
        };
        ownedSortButton.AttachNode(columnHeader);

        patchSortButton = new TextButtonNode
        {
            Position = new Vector2(280.0f, 0.0f),
            Size = new Vector2(MountEntryItemNode.PatchWidth, HeaderButtonHeight),
            String = "Patch",
            OnClick = () => ToggleSort(MountSortMode.Patch),
        };
        patchSortButton.AttachNode(columnHeader);

        seatsSortButton = new TextButtonNode
        {
            Position = new Vector2(320.0f, 0.0f),
            Size = new Vector2(MountEntryItemNode.SeatsWidth, HeaderButtonHeight),
            String = "Seats",
            OnClick = () => ToggleSort(MountSortMode.Seats),
        };
        seatsSortButton.AttachNode(columnHeader);

        mountsNode = new ListNode<MountEntry, MountEntryItemNode>
        {
            Position = new Vector2(0.0f, MountsListY),
            Size = new Vector2(600.0f, 400.0f),
            ItemSpacing = 0.0f,
            OptionsList = [],
            AutoResetScroll = false,
            OnItemSelected = entry =>
            {
                if (entry is { IsOwned: true })
                {
                    OpenMountContextMenu(entry.Mount);
                }
            },
        };
        mountsNode.AttachNode(this);

        actionsRow = new HorizontalListNode
        {
            Position = new Vector2(0.0f, 500.0f),
            Size = new Vector2(600.0f, ActionsRowHeight),
            ItemSpacing = 6.0f,
        };
        actionsRow.AttachNode(this);

        addAllButton = new TextButtonNode
        {
            Size = new Vector2(90.0f, 28.0f),
            String = "Add All",
            OnClick = () =>
            {
                if (boundList is null)
                {
                    return;
                }

                ConfigManager.Instance.ConsiderAllMountsForSummoning(
                    boundList,
                    GetFilteredMountEntries()
                        .Where(entry => entry.IsOwned)
                        .Select(entry => entry.Mount.RowId)
                );
                RefreshBoundList();
                RefreshMountEntries();
            },
        };
        NativeButtonStyles.StyleAsAdd(addAllButton);
        actionsRow.AddNode(addAllButton);

        removeAllButton = new TextButtonNode
        {
            Size = new Vector2(100.0f, 28.0f),
            String = "Remove All",
            OnClick = () =>
            {
                if (boundList is null)
                {
                    return;
                }

                ConfigManager.Instance.OverlookAllMountsForSummoning(
                    boundList,
                    GetFilteredMountEntries()
                        .Where(entry => entry.IsOwned)
                        .Select(entry => entry.Mount.RowId)
                );
                RefreshBoundList();
                RefreshMountEntries();
            },
        };
        NativeButtonStyles.StyleAsRemove(removeAllButton);
        actionsRow.AddNode(removeAllButton);

        emptyHint = new TextNode
        {
            Position = new Vector2(0.0f, 0.0f),
            Size = new Vector2(600.0f, 40.0f),
            FontSize = 14,
            String = "Select a mount list to edit.",
            IsVisible = true,
        };
        emptyHint.AttachNode(this);

        confirmationDialog.AttachNode(this);

        UpdateSortHeaderLabels();
        SetEditorVisible(false);
    }

    public void Bind(MountList mountList)
    {
        var listChanged = boundList is null || boundList.Id != mountList.Id;
        boundList = mountList;
        SetEditorVisible(true);

        nameInput.String = mountList.Name;
        typeDropDown.SelectedOption = mountList.Type.ToString();
        fetchTypeDropDown.SelectedOption = mountList.FetchNextType.ToString();

        if (listChanged)
        {
            ResetFilters();
        }

        confirmationDialog.Hide();
        RefreshMountEntries();
    }

    private void ResetFilters()
    {
        mountFilter = "";
        selectionFilter = MountSelectionFilter.All;
        seatFilter = MountSeatFilter.All;
        ownedOnlyFilter = true;

        searchInput.String = "";
        selectionFilterDropDown.SelectedOption = MountSelectionFilter.All;
        seatFilterDropDown.SelectedOption = MountSeatFilter.All;

        // Writing IsChecked triggers OnClick; suppress so we don't refresh mid-reset.
        var ownedClick = ownedFilterCheckbox.OnClick;
        ownedFilterCheckbox.OnClick = null;
        ownedFilterCheckbox.IsChecked = true;
        ownedFilterCheckbox.OnClick = ownedClick;
    }

    public void Update()
    {
        mountsNode.Update();
    }

    /// <summary>
    /// Collapses any open filter/settings dropdowns. Call before the parent
    /// addon hides so popup nodes reattached to the root are not finalized mid-open.
    /// </summary>
    public void CollapseOpenDropDowns()
    {
        typeDropDown.Collapse(playSoundEffect: false);
        fetchTypeDropDown.Collapse(playSoundEffect: false);
        selectionFilterDropDown.Collapse(playSoundEffect: false);
        seatFilterDropDown.Collapse(playSoundEffect: false);
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();

        var actionsY = Height - ActionsRowHeight;
        actionsRow.Position = new Vector2(0.0f, actionsY);
        actionsRow.Width = Width;

        mountsNode.Size = new Vector2(
            Width,
            Math.Max(80.0f, actionsY - ActionsRowGap - MountsListY)
        );
        emptyHint.Width = Width;
        columnHeader.Width = Width;
        settingsDivider.Width = Width;
        settingsRow.Width = Width;
        filterRow.Width = Width;
        confirmationDialog.Size = Size;

        var toggleX = Width - MountEntryItemNode.ToggleWidth - MountEntryItemNode.RightPadding
            - 16.0f; // list scrollbar inset
        var seatsX = toggleX - MountEntryItemNode.ColumnGap - MountEntryItemNode.SeatsWidth;

        seatsSortButton.Position = new Vector2(seatsX, 0.0f);
        seatsSortButton.Size = new Vector2(MountEntryItemNode.SeatsWidth, HeaderButtonHeight);

        var patchX = seatsX - MountEntryItemNode.ColumnGap - MountEntryItemNode.PatchWidth;
        patchSortButton.Position = new Vector2(patchX, 0.0f);
        patchSortButton.Size = new Vector2(MountEntryItemNode.PatchWidth, HeaderButtonHeight);

        var ownedX = patchX - MountEntryItemNode.ColumnGap - MountEntryItemNode.OwnedWidth;
        ownedSortButton.Position = new Vector2(ownedX, 0.0f);
        ownedSortButton.Size = new Vector2(MountEntryItemNode.OwnedWidth, HeaderButtonHeight);

        nameSortButton.Position = new Vector2(MountEntryItemNode.NameLeft, 0.0f);
        nameSortButton.Size = new Vector2(
            Math.Max(40.0f, ownedX - MountEntryItemNode.ColumnGap - MountEntryItemNode.NameLeft),
            HeaderButtonHeight
        );
    }

    private void SetEditorVisible(bool visible)
    {
        emptyHint.IsVisible = !visible;
        settingsRow.IsVisible = visible;
        settingsDivider.IsVisible = visible;
        filterRow.IsVisible = visible;
        actionsRow.IsVisible = visible;
        columnHeader.IsVisible = visible;
        nameInput.IsVisible = visible;
        typeDropDown.IsVisible = visible;
        fetchTypeDropDown.IsVisible = visible;
        deleteButton.IsVisible = visible;
        copyMacroButton.IsVisible = visible;
        searchInput.IsVisible = visible;
        addAllButton.IsVisible = visible;
        removeAllButton.IsVisible = visible;
        selectionFilterLabel.IsVisible = visible;
        selectionFilterDropDown.IsVisible = visible;
        seatFilterLabel.IsVisible = visible;
        seatFilterDropDown.IsVisible = visible;
        ownedFilterLabel.IsVisible = visible;
        ownedFilterCheckbox.IsVisible = visible;
        nameSortButton.IsVisible = visible;
        ownedSortButton.IsVisible = visible;
        patchSortButton.IsVisible = visible;
        seatsSortButton.IsVisible = visible;
        mountsNode.IsVisible = visible;
    }

    private void ToggleSort(MountSortMode mode)
    {
        if (sortMode == mode)
        {
            sortAscending = !sortAscending;
        }
        else
        {
            sortMode = mode;
            sortAscending = true;
        }

        UpdateSortHeaderLabels();
        RefreshMountEntries();
    }

    private void UpdateSortHeaderLabels()
    {
        nameSortButton.String = FormatSortLabel("Name", MountSortMode.Name);
        ownedSortButton.String = FormatSortLabel("Own%", MountSortMode.Owned);
        patchSortButton.String = FormatSortLabel("Patch", MountSortMode.Patch);
        seatsSortButton.String = FormatSortLabel("Seats", MountSortMode.Seats);
    }

    private string FormatSortLabel(string label, MountSortMode mode)
    {
        if (sortMode != mode)
        {
            return label;
        }

        return sortAscending ? $"{label} ▲" : $"{label} ▼";
    }

    private void ConfirmAndDeleteList()
    {
        if (boundList is not { } listToDelete)
        {
            return;
        }

        confirmationDialog.Show(
            $"Are you sure you want to delete your list \"{listToDelete.Name}\"?",
            () =>
            {
                ConfigManager.Instance.RemoveMountList(listToDelete);
                boundList = null;
                SetEditorVisible(false);
                OnListsChanged?.Invoke();
            }
        );
    }

    private void RenameList(string newName)
    {
        if (boundList is null || string.IsNullOrWhiteSpace(newName) || newName == boundList.Name)
        {
            return;
        }

        if (ConfigManager.Instance.MountLists.ContainsKey(newName))
        {
            nameInput.String = boundList.Name;
            return;
        }

        ConfigManager.Instance.RenameMountList(boundList, newName);
        RefreshBoundList();
        OnListsChanged?.Invoke();
    }

    private void RefreshBoundList()
    {
        if (boundList is null)
        {
            return;
        }

        boundList = ConfigManager.Instance.OrderedMountList.FirstOrDefault(list => list.Id == boundList.Id)
            ?? ConfigManager.Instance.MountLists.GetValueOrDefault(boundList.Name);

        if (boundList is not null)
        {
            Bind(boundList);
        }
    }

    private void RefreshMountEntries()
    {
        if (boundList is null)
        {
            mountsNode.OptionsList = [];
            return;
        }

        var sorted = SortEntries(GetFilteredMountEntries());
        for (var i = 0; i < sorted.Count; i++)
        {
            sorted[i] = sorted[i] with { RowIndex = i };
        }

        mountsNode.OptionsList = sorted;
    }

    private List<MountEntry> GetFilteredMountEntries()
    {
        if (boundList is null)
        {
            return [];
        }

        var ownedMountIds = MountManager.GetOwnedMountIds();
        var available = boundList.GetAvailableMountsForSummoning(ownedMountIds).ToHashSet();

        IEnumerable<Mount> candidateMounts;
        if (ownedOnlyFilter)
        {
            var unavailable = boundList.GetOwnedButUnavailableMountsForSummoning(ownedMountIds);
            candidateMounts = available
                .Concat(unavailable)
                .Select(MountManager.GetMount)
                .OfType<Mount>();
        }
        else
        {
            candidateMounts = MountManager.GetAllMounts();
        }

        var entries = new List<MountEntry>();

        foreach (var mount in candidateMounts)
        {
            var mountId = mount.RowId;

            if (!string.IsNullOrEmpty(mountFilter)
                && !mount.Singular.ExtractText().Contains(mountFilter, StringComparison.CurrentCultureIgnoreCase))
            {
                continue;
            }

            var isOwned = ownedMountIds.Contains(mountId);
            var isInList = available.Contains(mountId);
            var seatCount = MountManager.GetSeatCount(mount);

            if (selectionFilter == MountSelectionFilter.Selected && !isInList)
            {
                continue;
            }

            if (selectionFilter == MountSelectionFilter.Unselected && isInList)
            {
                continue;
            }

            if (seatFilter == MountSeatFilter.Single && seatCount != 1)
            {
                continue;
            }

            if (seatFilter == MountSeatFilter.Multi && seatCount < 2)
            {
                continue;
            }

            var collectInfo = FfxivCollectMountData.Get(mountId);
            entries.Add(
                new MountEntry(
                    mount,
                    isOwned,
                    isInList,
                    seatCount,
                    collectInfo?.OwnedDisplay ?? "—",
                    collectInfo?.OwnedPercent,
                    collectInfo?.Patch,
                    ToggleMembership
                )
            );
        }

        return entries;
    }

    private List<MountEntry> SortEntries(List<MountEntry> entries)
    {
        IOrderedEnumerable<MountEntry> ordered = sortMode switch
        {
            MountSortMode.Seats => sortAscending
                ? entries.OrderBy(entry => entry.SeatCount)
                    .ThenBy(entry => entry.Mount.Singular.ExtractText(), StringComparer.CurrentCultureIgnoreCase)
                : entries.OrderByDescending(entry => entry.SeatCount)
                    .ThenByDescending(entry => entry.Mount.Singular.ExtractText(), StringComparer.CurrentCultureIgnoreCase),
            MountSortMode.Owned => sortAscending
                ? entries.OrderBy(entry => entry.OwnedPercent ?? float.MaxValue)
                    .ThenBy(entry => entry.Mount.Singular.ExtractText(), StringComparer.CurrentCultureIgnoreCase)
                : entries.OrderByDescending(entry => entry.OwnedPercent ?? float.MinValue)
                    .ThenByDescending(entry => entry.Mount.Singular.ExtractText(), StringComparer.CurrentCultureIgnoreCase),
            MountSortMode.Patch => sortAscending
                ? entries.OrderBy(entry => entry.Patch, Comparer<string?>.Create(MountCollectInfo.ComparePatch))
                    .ThenBy(entry => entry.Mount.Singular.ExtractText(), StringComparer.CurrentCultureIgnoreCase)
                : entries.OrderByDescending(entry => entry.Patch, Comparer<string?>.Create(MountCollectInfo.ComparePatch))
                    .ThenByDescending(entry => entry.Mount.Singular.ExtractText(), StringComparer.CurrentCultureIgnoreCase),
            _ => sortAscending
                ? entries.OrderBy(entry => entry.Mount.Singular.ExtractText(), StringComparer.CurrentCultureIgnoreCase)
                : entries.OrderByDescending(
                    entry => entry.Mount.Singular.ExtractText(),
                    StringComparer.CurrentCultureIgnoreCase
                ),
        };

        return ordered.ToList();
    }

    private void ToggleMembership(MountEntry entry)
    {
        if (boundList is null || !entry.IsOwned)
        {
            return;
        }

        if (entry.IsInSummonList)
        {
            ConfigManager.Instance.OverlookMountFromSummoning(boundList, entry.Mount);
        }
        else
        {
            ConfigManager.Instance.ConsiderMountForSummoning(boundList, entry.Mount);
        }

        RefreshBoundList();
        RefreshMountEntries();
    }

    private static TextNode CreateFilterCategoryLabel(string text, float width) =>
        new()
        {
            Size = new Vector2(width, 28.0f),
            FontSize = 12,
            LineSpacing = 12,
            AlignmentType = AlignmentType.Right,
            TextFlags = TextFlags.Edge,
            TextColor = new Vector4(0.85f, 0.85f, 0.85f, 1.0f),
            String = text,
        };

    private static ReadOnlySeString FormatSelectionFilterLabel(MountSelectionFilter filter) =>
        filter switch
        {
            MountSelectionFilter.Selected => "Selected",
            MountSelectionFilter.Unselected => "Unselected",
            _ => "All",
        };

    private static ReadOnlySeString FormatSeatFilterLabel(MountSeatFilter filter) =>
        filter switch
        {
            MountSeatFilter.Single => "One",
            MountSeatFilter.Multi => "Multi",
            _ => "All",
        };

    private void OpenMountContextMenu(Mount mount)
    {
        mountContextMenu.Clear();

        mountContextMenu.AddItem(
            new ContextMenuItem
            {
                Name = "Summon",
                OnClick = () => MountManager.SummonMount(mount),
                DisplayPriority = 10,
            }
        );

        var isFavorite = MountManager.IsMountFavorite(mount);
        mountContextMenu.AddItem(
            new ContextMenuItem
            {
                Name = isFavorite ? "Remove from Favorites" : "Add to Favorites",
                OnClick = () => MountManager.ToggleMountFavorite(mount),
                DisplayPriority = 20,
            }
        );

        MountRouletteMenuItems.ApplyToKamiContextMenu(mountContextMenu, mount);
        try
        {
            MountRouletteMenuItems.SuppressNativeMountMenuInjection = true;
            mountContextMenu.Open();
        }
        finally
        {
            MountRouletteMenuItems.SuppressNativeMountMenuInjection = false;
        }
    }
}
