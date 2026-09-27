using System;
using Dalamud.Game.Gui.ContextMenu;
using Lumina.Excel.Sheets;

namespace EnhancedMountRoulette;

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

        Plugin.Log.Debug($"Selected mount {selectedMount.RowId} {selectedMount.Singular.ExtractText()}");

        MountRouletteMenuItems.ApplyToNativeContextMenu(args, selectedMount);
    }
}
