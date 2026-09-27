using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Gui.Toast;
using KamiToolKit.ContextMenu;
using KamiToolKit.Nodes;
using EnhancedMountRoulette.Commands;
using EnhancedMountRoulette.Configuration;
using Lumina.Excel.Sheets;

namespace EnhancedMountRoulette.Windows.Native;

public class MountListEditorNode : ResNode
{
    public System.Action? OnListsChanged { get; set; }

    private MountList? boundList;
    private string mountFilter = "";

    private readonly TextInputNode nameInput;
    private readonly StringDropDownNode typeDropDown;
    private readonly StringDropDownNode fetchTypeDropDown;
    private readonly TextButtonNode deleteButton;
    private readonly TextButtonNode copyMacroButton;
    private readonly TextInputNode searchInput;
    private readonly TextButtonNode addAllButton;
    private readonly TextButtonNode removeAllButton;
    private readonly ListNode<MountEntry, MountEntryItemNode> mountsNode;
    private readonly TextNode emptyHint;
    private readonly HorizontalListNode settingsRow;
    private readonly HorizontalListNode filterRow;

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
            Position = new Vector2(0.0f, 0.0f),
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

        filterRow = new HorizontalListNode
        {
            Position = new Vector2(0.0f, 36.0f),
            Size = new Vector2(600.0f, 28.0f),
            ItemSpacing = 6.0f,
        };
        filterRow.AttachNode(this);

        searchInput = new TextInputNode
        {
            Size = new Vector2(220.0f, 28.0f),
            PlaceholderString = "Search mounts...",
            MaxCharacters = 50,
            OnInputReceived = value =>
            {
                mountFilter = value.ToString();
                RefreshMountEntries();
            },
        };
        filterRow.AddNode(searchInput);

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

                ConfigManager.Instance.ConsiderAllMountsForSummoning(boundList, MountManager.GetOwnedMountIds());
                RefreshBoundList();
                RefreshMountEntries();
            },
        };
        filterRow.AddNode(addAllButton);

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

                ConfigManager.Instance.OverlookAllMountsForSummoning(boundList, MountManager.GetOwnedMountIds());
                RefreshBoundList();
                RefreshMountEntries();
            },
        };
        filterRow.AddNode(removeAllButton);

        mountsNode = new ListNode<MountEntry, MountEntryItemNode>
        {
            Position = new Vector2(0.0f, 72.0f),
            Size = new Vector2(600.0f, 400.0f),
            ItemSpacing = 1.0f,
            OptionsList = [],
            AutoResetScroll = false,
            OnItemSelected = entry =>
            {
                if (entry is not null)
                {
                    OpenMountContextMenu(entry.Mount);
                }
            },
        };
        mountsNode.AttachNode(this);

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

        SetEditorVisible(false);
    }

    public void Bind(MountList mountList)
    {
        boundList = mountList;
        SetEditorVisible(true);

        nameInput.String = mountList.Name;
        typeDropDown.SelectedOption = mountList.Type.ToString();
        fetchTypeDropDown.SelectedOption = mountList.FetchNextType.ToString();

        confirmationDialog.Hide();
        RefreshMountEntries();
    }

    public void Update()
    {
        mountsNode.Update();
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();

        mountsNode.Size = new Vector2(Width, Math.Max(100.0f, Height - 72.0f));
        emptyHint.Width = Width;
        confirmationDialog.Size = Size;
    }

    private void SetEditorVisible(bool visible)
    {
        emptyHint.IsVisible = !visible;
        settingsRow.IsVisible = visible;
        filterRow.IsVisible = visible;
        nameInput.IsVisible = visible;
        typeDropDown.IsVisible = visible;
        fetchTypeDropDown.IsVisible = visible;
        deleteButton.IsVisible = visible;
        copyMacroButton.IsVisible = visible;
        searchInput.IsVisible = visible;
        addAllButton.IsVisible = visible;
        removeAllButton.IsVisible = visible;
        mountsNode.IsVisible = visible;
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

        var ownedMountIds = MountManager.GetOwnedMountIds();
        var available = boundList.GetAvailableMountsForSummoning(ownedMountIds).ToHashSet();
        var unavailable = boundList.GetOwnedButUnavailableMountsForSummoning(ownedMountIds);

        var entries = new List<MountEntry>();

        foreach (var mountId in available.Concat(unavailable))
        {
            if (MountManager.GetMount(mountId) is not { } mount)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(mountFilter)
                && !mount.Singular.ExtractText().Contains(mountFilter, StringComparison.CurrentCultureIgnoreCase))
            {
                continue;
            }

            var isInList = available.Contains(mountId);
            entries.Add(new MountEntry(mount, isInList, ToggleMembership));
        }

        mountsNode.OptionsList = entries;
    }

    private void ToggleMembership(MountEntry entry)
    {
        if (boundList is null)
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

    private void OpenMountContextMenu(Mount mount)
    {
        mountContextMenu.Clear();

        mountContextMenu.AddItem("Summon", () => MountManager.SummonMount(mount));

        var isFavorite = MountManager.IsMountFavorite(mount);
        mountContextMenu.AddItem(
            isFavorite ? "Remove from Favorites" : "Add to Favorites",
            () => MountManager.ToggleMountFavorite(mount)
        );

        MountRouletteMenuItems.ApplyToKamiContextMenu(mountContextMenu, mount);
        mountContextMenu.Open();
    }
}
