using System;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using Serilog;

namespace BetterMountRoulette;

public class MountNotebookContextMenu
{
    private readonly Configuration configuration;
    
    public MountNotebookContextMenu(Configuration configuration)
    {
        this.configuration = configuration;

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
            args.AddMenuItem(new MenuItem()
            {
                Name = mountListType == MountListType.Whitelist
                           ? "---- Roulette WhiteLists: ----"
                           : "---- Roulette BlackLists: ----",
                IsEnabled = false,
                PrefixChar = 'R',
            });

            foreach (var mountList in configuration.GetMountLists(mountListType))
            {
                args.AddMenuItem(MountListToMenuItem(mountList, selectedMount));
            }
        }
    }

    private MenuItem MountListToMenuItem(MountList mountList, Mount mount)
    {
        var mountId = mount.RowId;
        var mountName = mount.Singular.ExtractText();

        var isMountIdInList = mountList.MountIds.Contains(mountId);
        var isCurrentlySummonedInList = mountList.Type == MountListType.Whitelist ? isMountIdInList : !isMountIdInList;

        string namePrefix;
        SeIconChar prefixChar;
        ColorMap prefixColor;

        if (isCurrentlySummonedInList)
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
                if (!isMountIdInList)
                {
                    mountList.MountIds.Add(mountId);
                    Chat.Write($"Added #{mountId} {mountName} to list {mountList.Name}");
                }
                else
                {
                    mountList.MountIds.Remove(mountId);
                    Chat.Write($"Removed #{mountId} {mountName} from list {mountList.Name}");
                }

                configuration.Save();
            },
        };
    }
}
