using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Dalamud.Game.Gui.Toast;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.ContextMenu;
using KamiToolKit.Nodes;
using EnhancedMountRoulette.Commands;
using EnhancedMountRoulette.Configuration;
using EnhancedMountRoulette.Windows.Native;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using AgentContext = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentContext;

namespace EnhancedMountRoulette.Windows.Native.MountListItemEditor;

public class MountListItemEditorNode : ResNode
{
    private const float SettingsRowY = 0.0f;
    private const float DividerY = 32.0f;
    private const float FilterRowY = 40.0f;
    private const float HeaderRowY = 76.0f;
    private const float MountsListY = 104.0f;
    private const float ActionsRowHeight = 28.0f;
    private const float ActionsRowGap = 6.0f;
    private const float HeaderButtonHeight = 24.0f;
    private const float RowHeight = 28.0f;
    private const float ListScrollbarInset = 16.0f;

    public System.Action? OnListsChanged { get; set; }

    /// <summary>
    /// Returns the owning native addon id used to bind context menus.
    /// </summary>
    public Func<uint>? GetOwnerAddonId { get; set; }

    private MountList? selectedList;

    private readonly MountListFilters filters = new();

    private (MountSortMode Mode, bool Ascending) sorting = (MountSortMode.Name, true);

    /// <summary>
    /// Set once during final dispose. Blocks further UI work (context menus, dropdown
    /// collapse) so deferred callbacks cannot touch nodes after teardown starts.
    /// Not set on ordinary hide/show — the window can reopen.
    /// </summary>
    private bool isTearingDown;

    /// <summary>
    /// Cancels a deferred Framework.RunOnTick context-menu open.
    /// Right-click schedules the open for the next frame; hide/dispose cancel this so we
    /// never call AgentContext after the addon is gone.
    /// </summary>
    private CancellationTokenSource contextMenuCancellationTokenSource = new();

    private readonly MountListSettingsRowNode settingsRow;
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
    private readonly HorizontalLineNode settingsDivider;
    private readonly HorizontalListNode filterRow;
    private readonly HorizontalListNode actionsRow;

    private readonly ConfirmationDialogNode confirmationDialog;

    private readonly ContextMenu mountContextMenu = new();

    public MountListItemEditorNode()
    {
        settingsRow = AddSettingsRow();
        settingsDivider = AddSettingsDivider();
        filterRow = AddFilterRow(
            out searchInput,
            out selectionFilterLabel,
            out selectionFilterDropDown,
            out seatFilterLabel,
            out seatFilterDropDown,
            out ownedFilterLabel,
            out ownedFilterCheckbox
        );
        columnHeader = AddColumnHeader(
            out nameSortButton,
            out ownedSortButton,
            out patchSortButton,
            out seatsSortButton
        );
        mountsNode = AddMountsList();
        actionsRow = AddActionsRow(out addAllButton, out removeAllButton);
        emptyHint = AddEmptyHint();
        confirmationDialog = AddConfirmationDialog();

        UpdateSortHeaderLabels();
        SetEditorVisible(false);
    }

    public void Select(MountList mountList)
    {
        var listChanged = selectedList is null || selectedList.Id != mountList.Id;
        selectedList = mountList;
        SetEditorVisible(true);

        settingsRow.Load(mountList);

        if (listChanged)
        {
            ResetFilters();
        }

        confirmationDialog.Hide();
        RefreshMountEntries();
    }

    private void ResetFilters()
    {
        filters.Reset();

        searchInput.String = filters.SearchText;
        selectionFilterDropDown.SelectedOption = filters.Selection;
        seatFilterDropDown.SelectedOption = filters.Seats;

        // Writing IsChecked triggers OnClick; suppress so we don't refresh mid-reset.
        var ownedClick = ownedFilterCheckbox.OnClick;
        ownedFilterCheckbox.OnClick = null;
        ownedFilterCheckbox.IsChecked = filters.OwnedOnly;
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
        if (isTearingDown)
        {
            return;
        }

        settingsRow.CollapseDropDowns();
        selectionFilterDropDown.Collapse(playSoundEffect: false);
        seatFilterDropDown.Collapse(playSoundEffect: false);
    }

    /// <summary>
    /// Safe work before the addon hides (window can open again later).
    /// Cancels deferred context-menu opens and collapses dropdown popups.
    /// </summary>
    public void PrepareForHide()
    {
        if (isTearingDown)
        {
            return;
        }

        CancelPendingContextMenuOpen(replaceTokenSource: true);
        CollapseOpenDropDowns();
        mountContextMenu.Close();
    }

    /// <summary>
    /// Final teardown before node disposal. Must not run on ordinary hide/show.
    /// </summary>
    public void PrepareForTeardown()
    {
        if (isTearingDown)
        {
            return;
        }

        isTearingDown = true;
        MountEntryItemNode.OnOpenContextMenu = null;
        CancelPendingContextMenuOpen(replaceTokenSource: false);

        settingsRow.CollapseDropDowns();
        selectionFilterDropDown.Collapse(playSoundEffect: false);
        seatFilterDropDown.Collapse(playSoundEffect: false);
        mountContextMenu.Close();
    }

    private void CancelPendingContextMenuOpen(bool replaceTokenSource)
    {
        contextMenuCancellationTokenSource.Cancel();
        contextMenuCancellationTokenSource.Dispose();

        if (replaceTokenSource)
        {
            contextMenuCancellationTokenSource = new CancellationTokenSource();
        }

        MountRouletteMenuItems.SuppressNativeMountMenuInjection = false;
    }

    protected override void Dispose(bool isNativeDestructor)
    {
        PrepareForTeardown();
        mountContextMenu.Dispose();
        base.Dispose(isNativeDestructor);
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        LayoutEditorChrome();
        LayoutSortHeaderButtons();
    }

    private MountListSettingsRowNode AddSettingsRow()
    {
        var row = new MountListSettingsRowNode
        {
            Position = new Vector2(0.0f, SettingsRowY),
            OnNameCommitted = RenameList,
            OnTypeSelected = ChangeListType,
            OnFetchTypeSelected = ChangeFetchType,
            OnDeleteClicked = ConfirmAndDeleteList,
            OnCopyMacroClicked = CopyMacro,
        };
        row.AttachNode(this);
        return row;
    }

    private void ChangeListType(MountListType type)
    {
        if (selectedList is null)
        {
            return;
        }

        ConfigManager.Instance.ChangeMountListType(selectedList, type);
        RefreshSelectedList();
        OnListsChanged?.Invoke();
    }

    private void ChangeFetchType(FetchNextType fetchType)
    {
        if (selectedList is null)
        {
            return;
        }

        ConfigManager.Instance.StoreMountList(new MountList(selectedList) { FetchNextType = fetchType });
        RefreshSelectedList();
        OnListsChanged?.Invoke();
    }

    private void CopyMacro()
    {
        if (selectedList is null)
        {
            return;
        }

        Dalamud.Bindings.ImGui.ImGui.SetClipboardText(SummonMountCommand.GetMacro(selectedList));
        Plugin.ToastGui.ShowNormal(
            "Copied to clipboard",
            new ToastOptions { Position = ToastPosition.Bottom, Speed = ToastSpeed.Fast }
        );
    }

    private HorizontalLineNode AddSettingsDivider()
    {
        var divider = new HorizontalLineNode
        {
            Position = new Vector2(0.0f, DividerY),
            Size = new Vector2(600.0f, 4.0f),
        };
        divider.AttachNode(this);
        return divider;
    }

    private HorizontalListNode AddFilterRow(
        out TextInputNode searchInputNode,
        out TextNode selectionLabel,
        out EnumDropDownNode<MountSelectionFilter> selectionDropDown,
        out TextNode seatLabel,
        out EnumDropDownNode<MountSeatFilter> seatDropDown,
        out TextNode ownedLabel,
        out CheckboxNode ownedCheckbox
    )
    {
        var row = new HorizontalListNode
        {
            Position = new Vector2(0.0f, FilterRowY),
            Size = new Vector2(600.0f, RowHeight),
            ItemSpacing = 6.0f,
        };
        row.AttachNode(this);

        searchInputNode = new TextInputNode
        {
            Size = new Vector2(145.0f, RowHeight),
            PlaceholderString = "Search mounts...",
            OnInputReceived = value =>
            {
                filters.SearchText = value.ToString();
                RefreshMountEntries();
            },
        };
        row.AddNode(searchInputNode);

        selectionLabel = AddFilterCategoryLabel("Selected:", 70.0f);
        row.AddNode(selectionLabel);

        selectionDropDown = new EnumDropDownNode<MountSelectionFilter>
        {
            Size = new Vector2(140.0f, RowHeight),
            Options =
            [
                MountSelectionFilter.All,
                MountSelectionFilter.Selected,
                MountSelectionFilter.Unselected,
            ],
            SelectedOption = MountSelectionFilter.All,
            OnOptionSelected = option =>
            {
                filters.Selection = option;
                RefreshMountEntries();
            },
        };
        selectionDropDown.GetLabelFunction = FormatSelectionFilterLabel;
        row.AddNode(selectionDropDown);

        seatLabel = AddFilterCategoryLabel("Seats:", 48.0f);
        row.AddNode(seatLabel);

        seatDropDown = new EnumDropDownNode<MountSeatFilter>
        {
            Size = new Vector2(90.0f, RowHeight),
            Options =
            [
                MountSeatFilter.All,
                MountSeatFilter.Single,
                MountSeatFilter.Multi,
            ],
            SelectedOption = MountSeatFilter.All,
            OnOptionSelected = option =>
            {
                filters.Seats = option;
                RefreshMountEntries();
            },
        };
        seatDropDown.GetLabelFunction = FormatSeatFilterLabel;
        row.AddNode(seatDropDown);

        ownedLabel = AddFilterCategoryLabel("Owned:", 52.0f);
        row.AddNode(ownedLabel);

        ownedCheckbox = new CheckboxNode
        {
            Size = new Vector2(20.0f, 20.0f),
            String = string.Empty,
            IsChecked = true,
            OnClick = isChecked =>
            {
                filters.OwnedOnly = isChecked;
                RefreshMountEntries();
            },
        };
        // Attach directly to the filter row so component events stay under the list node tree.
        ownedCheckbox.Y = (RowHeight - ownedCheckbox.Height) / 2.0f;
        row.AddNode(ownedCheckbox);

        return row;
    }

    private ResNode AddColumnHeader(
        out TextButtonNode nameButton,
        out TextButtonNode ownedButton,
        out TextButtonNode patchButton,
        out TextButtonNode seatsButton
    )
    {
        var header = new ResNode
        {
            Position = new Vector2(0.0f, HeaderRowY),
            Size = new Vector2(600.0f, HeaderButtonHeight),
        };
        header.AttachNode(this);

        nameButton = AddSortHeaderButton("Name ▲", MountSortMode.Name, MountEntryItemNode.NameLeft, 200.0f);
        nameButton.AttachNode(header);

        ownedButton = AddSortHeaderButton(
            "Own%",
            MountSortMode.Owned,
            220.0f,
            MountEntryItemNode.OwnedWidth
        );
        ownedButton.AttachNode(header);

        patchButton = AddSortHeaderButton(
            "Patch",
            MountSortMode.Patch,
            280.0f,
            MountEntryItemNode.PatchWidth
        );
        patchButton.AttachNode(header);

        seatsButton = AddSortHeaderButton(
            "Seats",
            MountSortMode.Seats,
            320.0f,
            MountEntryItemNode.SeatsWidth
        );
        seatsButton.AttachNode(header);

        return header;
    }

    private TextButtonNode AddSortHeaderButton(
        string label,
        MountSortMode mode,
        float x,
        float width
    )
    {
        return new TextButtonNode
        {
            Position = new Vector2(x, 0.0f),
            Size = new Vector2(width, HeaderButtonHeight),
            String = label,
            OnClick = () => ToggleSort(mode),
        };
    }

    private ListNode<MountEntry, MountEntryItemNode> AddMountsList()
    {
        var list = new ListNode<MountEntry, MountEntryItemNode>
        {
            Position = new Vector2(0.0f, MountsListY),
            Size = new Vector2(600.0f, 400.0f),
            ItemSpacing = 0.0f,
            OptionsList = [],
            AutoResetScroll = false,
        };
        list.AttachNode(this);
        MountEntryItemNode.OnOpenContextMenu = OpenMountContextMenu;
        return list;
    }

    private HorizontalListNode AddActionsRow(
        out TextButtonNode addAllButtonNode,
        out TextButtonNode removeAllButtonNode
    )
    {
        var row = new HorizontalListNode
        {
            Position = new Vector2(0.0f, 500.0f),
            Size = new Vector2(600.0f, ActionsRowHeight),
            ItemSpacing = 6.0f,
        };
        row.AttachNode(this);

        addAllButtonNode = new TextButtonNode
        {
            Size = new Vector2(90.0f, RowHeight),
            String = "Add All",
            OnClick = ConfirmAndAddAll,
        };
        NativeButtonStyles.StyleAsAdd(addAllButtonNode);
        row.AddNode(addAllButtonNode);

        removeAllButtonNode = new TextButtonNode
        {
            Size = new Vector2(100.0f, RowHeight),
            String = "Remove All",
            OnClick = ConfirmAndRemoveAll,
        };
        NativeButtonStyles.StyleAsRemove(removeAllButtonNode);
        row.AddNode(removeAllButtonNode);

        return row;
    }

    private TextNode AddEmptyHint()
    {
        var hint = new TextNode
        {
            Position = new Vector2(0.0f, 0.0f),
            Size = new Vector2(600.0f, 40.0f),
            FontSize = 14,
            String = "Select a mount list to edit.",
            IsVisible = true,
        };
        hint.AttachNode(this);
        return hint;
    }

    private ConfirmationDialogNode AddConfirmationDialog()
    {
        var dialog = new ConfirmationDialogNode
        {
            Position = Vector2.Zero,
            Size = new Vector2(600.0f, 400.0f),
        };
        dialog.AttachNode(this);
        return dialog;
    }

    private void LayoutEditorChrome()
    {
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
    }

    private void LayoutSortHeaderButtons()
    {
        var toggleX = Width
            - MountEntryItemNode.ToggleWidth
            - MountEntryItemNode.RightPadding
            - ListScrollbarInset;
        var seatsX = toggleX - MountEntryItemNode.ColumnGap - MountEntryItemNode.SeatsWidth;
        var patchX = seatsX - MountEntryItemNode.ColumnGap - MountEntryItemNode.PatchWidth;
        var ownedX = patchX - MountEntryItemNode.ColumnGap - MountEntryItemNode.OwnedWidth;

        PlaceSortHeaderButton(seatsSortButton, seatsX, MountEntryItemNode.SeatsWidth);
        PlaceSortHeaderButton(patchSortButton, patchX, MountEntryItemNode.PatchWidth);
        PlaceSortHeaderButton(ownedSortButton, ownedX, MountEntryItemNode.OwnedWidth);

        nameSortButton.Position = new Vector2(MountEntryItemNode.NameLeft, 0.0f);
        nameSortButton.Size = new Vector2(
            Math.Max(40.0f, ownedX - MountEntryItemNode.ColumnGap - MountEntryItemNode.NameLeft),
            HeaderButtonHeight
        );
    }

    private void PlaceSortHeaderButton(TextButtonNode button, float x, float width)
    {
        button.Position = new Vector2(x, 0.0f);
        button.Size = new Vector2(width, HeaderButtonHeight);
    }

    private void SetEditorVisible(bool visible)
    {
        emptyHint.IsVisible = !visible;
        settingsRow.IsVisible = visible;
        settingsDivider.IsVisible = visible;
        filterRow.IsVisible = visible;
        actionsRow.IsVisible = visible;
        columnHeader.IsVisible = visible;
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
        sorting = sorting.Mode == mode
            ? (mode, !sorting.Ascending)
            : (mode, true);

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
        if (sorting.Mode != mode)
        {
            return label;
        }

        return sorting.Ascending ? $"{label} ▲" : $"{label} ▼";
    }

    private void ConfirmAndDeleteList()
    {
        if (selectedList is not { } listToDelete)
        {
            return;
        }

        confirmationDialog.Show(
            $"Are you sure you want to delete your list \"{listToDelete.Name}\"?",
            () =>
            {
                ConfigManager.Instance.RemoveMountList(listToDelete);
                selectedList = null;
                SetEditorVisible(false);
                OnListsChanged?.Invoke();
            }
        );
    }

    private void ConfirmAndAddAll()
    {
        if (selectedList is not { } list)
        {
            return;
        }

        var mountIds = GetFilteredOwnedMountIds(inSummonList: false);
        if (mountIds.Count == 0)
        {
            return;
        }

        confirmationDialog.Show(
            $"Add {mountIds.Count} filtered owned {(mountIds.Count == 1 ? "mount" : "mounts")} to \"{list.Name}\"?",
            () =>
            {
                ConfigManager.Instance.ConsiderAllMountsForSummoning(list, mountIds);
                RefreshSelectedList();
            }
        );
    }

    private void ConfirmAndRemoveAll()
    {
        if (selectedList is not { } list)
        {
            return;
        }

        var mountIds = GetFilteredOwnedMountIds(inSummonList: true);
        if (mountIds.Count == 0)
        {
            return;
        }

        confirmationDialog.Show(
            $"Remove {mountIds.Count} filtered owned {(mountIds.Count == 1 ? "mount" : "mounts")} from \"{list.Name}\"?",
            () =>
            {
                ConfigManager.Instance.OverlookAllMountsForSummoning(list, mountIds);
                RefreshSelectedList();
            }
        );
    }

    private List<uint> GetFilteredOwnedMountIds(bool inSummonList) =>
        GetFilteredMountEntries()
            .Where(entry => entry.IsOwned && entry.IsInSummonList == inSummonList)
            .Select(entry => entry.Mount.RowId)
            .ToList();

    private void RenameList(string newName)
    {
        if (selectedList is null || string.IsNullOrWhiteSpace(newName) || newName == selectedList.Name)
        {
            return;
        }

        if (ConfigManager.Instance.MountLists.ContainsKey(newName))
        {
            settingsRow.SetName(selectedList.Name);
            return;
        }

        ConfigManager.Instance.RenameMountList(selectedList, newName);
        RefreshSelectedList();
        OnListsChanged?.Invoke();
    }

    private void RefreshSelectedList()
    {
        if (selectedList is null)
        {
            return;
        }

        selectedList = ConfigManager.Instance.OrderedMountList.FirstOrDefault(list => list.Id == selectedList.Id)
            ?? ConfigManager.Instance.MountLists.GetValueOrDefault(selectedList.Name);

        if (selectedList is not null)
        {
            Select(selectedList);
        }
    }

    private void RefreshMountEntries()
    {
        if (selectedList is null)
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
        if (selectedList is null)
        {
            return [];
        }

        var ownedMountIds = MountManager.GetOwnedMountIds();
        var available = selectedList.GetAvailableMountsForSummoning(ownedMountIds).ToHashSet();

        IEnumerable<Mount> candidateMounts;
        if (filters.OwnedOnly)
        {
            var unavailable = selectedList.GetOwnedButUnavailableMountsForSummoning(ownedMountIds);
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

            if (!string.IsNullOrEmpty(filters.SearchText)
                && !mount.Singular.ExtractText().Contains(
                    filters.SearchText,
                    StringComparison.CurrentCultureIgnoreCase
                ))
            {
                continue;
            }

            var isOwned = ownedMountIds.Contains(mountId);
            var isInList = available.Contains(mountId);
            var seatCount = MountManager.GetSeatCount(mount);

            if (filters.Selection == MountSelectionFilter.Selected && !isInList)
            {
                continue;
            }

            if (filters.Selection == MountSelectionFilter.Unselected && isInList)
            {
                continue;
            }

            if (filters.Seats == MountSeatFilter.Single && seatCount != 1)
            {
                continue;
            }

            if (filters.Seats == MountSeatFilter.Multi && seatCount < 2)
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
        IOrderedEnumerable<MountEntry> ordered = sorting.Mode switch
        {
            MountSortMode.Seats => sorting.Ascending
                ? entries.OrderBy(entry => entry.SeatCount)
                    .ThenBy(entry => entry.Mount.Singular.ExtractText(), StringComparer.CurrentCultureIgnoreCase)
                : entries.OrderByDescending(entry => entry.SeatCount)
                    .ThenByDescending(entry => entry.Mount.Singular.ExtractText(), StringComparer.CurrentCultureIgnoreCase),
            MountSortMode.Owned => sorting.Ascending
                ? entries.OrderBy(entry => entry.OwnedPercent ?? float.MaxValue)
                    .ThenBy(entry => entry.Mount.Singular.ExtractText(), StringComparer.CurrentCultureIgnoreCase)
                : entries.OrderByDescending(entry => entry.OwnedPercent ?? float.MinValue)
                    .ThenByDescending(entry => entry.Mount.Singular.ExtractText(), StringComparer.CurrentCultureIgnoreCase),
            MountSortMode.Patch => sorting.Ascending
                ? entries.OrderBy(entry => entry.Patch, Comparer<string?>.Create(MountCollectInfo.ComparePatch))
                    .ThenBy(entry => entry.Mount.Singular.ExtractText(), StringComparer.CurrentCultureIgnoreCase)
                : entries.OrderByDescending(entry => entry.Patch, Comparer<string?>.Create(MountCollectInfo.ComparePatch))
                    .ThenByDescending(entry => entry.Mount.Singular.ExtractText(), StringComparer.CurrentCultureIgnoreCase),
            _ => sorting.Ascending
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
        if (selectedList is null || !entry.IsOwned)
        {
            return;
        }

        if (entry.IsInSummonList)
        {
            ConfigManager.Instance.OverlookMountFromSummoning(selectedList, entry.Mount);
        }
        else
        {
            ConfigManager.Instance.ConsiderMountForSummoning(selectedList, entry.Mount);
        }

        RefreshSelectedList();
    }

    private static TextNode AddFilterCategoryLabel(string text, float width) =>
        new()
        {
            Size = new Vector2(width, RowHeight),
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

    private unsafe void OpenMountContextMenu(Mount mount)
    {
        if (isTearingDown)
        {
            return;
        }

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

        // Open on the next tick so the MouseUp that follows MouseDown does not
        // immediately dismiss the freshly opened AgentContext menu.
        var ownerAddonId = GetOwnerAddonId?.Invoke() ?? 0u;
        var token = contextMenuCancellationTokenSource.Token;

        Plugin.Framework.RunOnTick(
            () =>
            {
                if (isTearingDown || token.IsCancellationRequested)
                {
                    return;
                }

                // Suppress only around Open(): if the tick is cancelled, we must
                // not leave SuppressNativeMountMenuInjection stuck true.
                MountRouletteMenuItems.SuppressNativeMountMenuInjection = true;
                try
                {
                    // KamiToolKit Open() uses bindToOwner:true with whatever focus exists.
                    // Without a focused text input that closes the menu immediately, so
                    // re-bind to our addon (or open unbound) after items are registered.
                    mountContextMenu.Open();

                    var agent = AgentContext.Instance();
                    if (agent is null)
                    {
                        return;
                    }

                    if (ownerAddonId is not 0)
                    {
                        agent->OpenContextMenuForAddon(ownerAddonId);
                    }
                    else
                    {
                        agent->OpenContextMenu(bindToOwner: false);
                    }
                }
                finally
                {
                    MountRouletteMenuItems.SuppressNativeMountMenuInjection = false;
                }
            },
            cancellationToken: token
        );
    }
}
