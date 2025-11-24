using System;
using System.Linq;
using BetterMountRoulette.Configuration;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Lumina.Excel.Sheets;

namespace BetterMountRoulette;

public class MountNotebookContextMenu : IDisposable
{
    public MountNotebookContextMenu()
    {
        Plugin.ContextMenu.OnMenuOpened += OnContextMenuOpened;
    }

    public void Dispose()
    {
        Plugin.ContextMenu.OnMenuOpened -= OnContextMenuOpened;
    }

    private void OnContextMenuOpened(IMenuOpenedArgs args)
    {
        if (args.AddonName != "MountNoteBook")
        {
            return;
        }

        if (MountManager.GetSelectedMountInMountGuide() is not { } selectedMount)
        {
            return;
        }

        Chat.Write($"Selected mount {selectedMount.RowId} {selectedMount.Singular.ExtractText()}");

        foreach (var mountListType in Enum.GetValues<MountListType>())
        {
            args.AddMenuItem(
                new MenuItem()
                {
                    Name = mountListType == MountListType.Whitelist
                        ? "---- Roulette WhiteLists: ----"
                        : "---- Roulette BlackLists: ----",
                    IsEnabled = false,
                    PrefixChar = 'R',
                }
            );

            foreach (var mountList in ConfigManager.Instance.GetMountLists(mountListType))
            {
                args.AddMenuItem(MountListToMenuItem(mountList, selectedMount));
            }
        }
    }

    private MenuItem MountListToMenuItem(MountList mountList, Mount mount)
    {
        var mountId = mount.RowId;
        var isCurrentlyConsideredForSummoning = MountManager.GetAvailableMountsFromListForSummoning(mountList)
            .Contains(mountId);

        string namePrefix;
        SeIconChar prefixChar;
        ColorMap prefixColor;

        if (isCurrentlyConsideredForSummoning)
        {
            namePrefix = "Ignore in";
            prefixChar = SeIconChar.Cross;
            prefixColor = ColorMap.Red;
        }
        else
        {
            namePrefix = "Summon in";
            prefixChar = SeIconChar.BoxedPlus;
            prefixColor = ColorMap.Green;
        }

        return new MenuItem()
        {
            Name = $"{namePrefix} {mountList.Name}",
            IsEnabled = true,
            Prefix = prefixChar,
            PrefixColor = (ushort)prefixColor,
            OnClicked = (_) =>
            {
                if (!isCurrentlyConsideredForSummoning)
                {
                    ConfigManager.Instance.AddMountToList(mountList, mount);
                }
                else
                {
                    ConfigManager.Instance.RemoveMountFromList(mountList, mount);
                }
            },
        };
    }
}
