using System;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;


namespace BetterMountRoulette;

public static class MountManager
{
    private static readonly unsafe AgentMountNoteBook* AgentMountNoteBook =
        (AgentMountNoteBook*)Plugin.GameGui.GetAgentById((int)AgentId.MountNotebook).Address;

    private static readonly ExcelSheet<Mount> MountSheet = Plugin.DataManager.GetExcelSheet<Mount>();

    private static readonly unsafe ActionManager* ActionManager =
        FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();

    public static Mount? GetMount(string mountName)
    {
        return MountSheet
               // Sheet includes empty values.
               .Where((mount) => mount.Singular.ExtractText().Equals(mountName, StringComparison.OrdinalIgnoreCase))
               .FirstOrNull();
    }

    public static Mount? GetMount(uint mountId)
    {
        if (MountSheet.GetRowOrDefault(mountId) is not { } mount)
        {
            return null;
        }

        // Sheet includes empty values.
        return mount.Singular.IsEmpty ? null : mount;
    }

    public static unsafe Mount? GetSelectedMountInMountGuide()
    {
        uint? mountId = AgentMountNoteBook->ViewType switch
        {
            AddonMinionMountBase.ViewType.Normal => AgentMountNoteBook->SelectedIdInNormalView,
            AddonMinionMountBase.ViewType.Favorites => AgentMountNoteBook->SelectedIdInFavoritesView,
            AddonMinionMountBase.ViewType.Search => AgentMountNoteBook->SelectedIdInSearchView,
            _ => null
        };

        return mountId is not null ? GetMount(mountId.Value) : null;
    }

    public static unsafe void SummonMount(Mount mount)
    {
        ActionManager->UseAction(ActionType.Mount, (uint)mount.RowId);
    }
}
