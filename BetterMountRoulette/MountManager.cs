using System;
using System.Collections.Generic;
using System.Linq;
using BetterMountRoulette.Configuration;
using Dalamud.Plugin.Services;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using InteropGenerator.Runtime;
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

    private static readonly unsafe PlayerState* PlayerState =
        FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState.Instance();

    private static readonly unsafe Conditions* PlayerConditions = Conditions.Instance();

    public static unsafe bool IsMounted => PlayerConditions->Mounted;

    public static Mount? GetMount(string mountName)
    {
        var mount = MountSheet
            // Sheet includes empty values.
            .Where((mount) => mount.Singular.ExtractText()
                .Equals(mountName, StringComparison.OrdinalIgnoreCase)
            )
            .FirstOrNull();

        if (mount is null)
        {
            return null;
        }

        return IsMountEntryEmpty(mount.Value) ? null : mount.Value;
    }

    public static Mount? GetMount(uint mountId)
    {
        if (MountSheet.GetRowOrDefault(mountId) is not { } mount)
        {
            return null;
        }

        return IsMountEntryEmpty(mount) ? null : mount;
    }

    public static bool IsMountEntryEmpty(Mount mount)
    {
        return mount.Singular.IsEmpty || mount.Order < 0;
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


    public static List<uint> GetAvailableMountsForList(MountList mountList)
    {
        var ownedMountIds = GetOwnedMountIds();

        return mountList.GetAvailableMountsForList(ownedMountIds);
    }

    public static void SummonNextMountInList(MountList mountList)
    {
        if (mountList.GetNextMountIdToSummon(GetOwnedMountIds()) is not { } nextMountIdToSummon)
        {
            Chat.Write("No relevant mounts found for the list.", isError: true);

            return;
        }

        SummonMount(nextMountIdToSummon);
    }

    public static unsafe void SummonMount(uint mountId)
    {
        ActionManager->UseAction(ActionType.Mount, mountId);
    }


    public static unsafe void SummonMount(Mount mount)
    {
        ActionManager->UseAction(ActionType.Mount, (uint)mount.RowId);
    }

    /// <summary>
    /// Will return true if player was mounted and will call unmount.
    /// </summary>
    public static unsafe bool UnmountIfMounted()
    {
        if (!IsMounted)
        {
            return false;
        }

        // ID should not matter for unmounting, use the mount id owned by every player (company chocobo) just in case.
        ActionManager->UseAction(ActionType.Mount, 1);

        return true;
    }

    public static HashSet<uint> GetOwnedMountIds()
    {
        // TODO see if it can use memory hook to safe performance to not always check that;
        HashSet<uint> ownedMountIds = [];

        // It is a list of all mounts in the Game. Player owns those with value of true.
        BitArray mountsBitArray;
        unsafe
        {
            mountsBitArray = PlayerState->UnlockedMountsBitArray;
        }

        foreach (var mount in MountSheet)
        {
            // Skip invalid mounts
            if (IsMountEntryEmpty(mount))
            {
                continue;
            }

            // Use mount.Order as the bit array index, not the row ID.
            var isMountUnlocked = mountsBitArray.Get(mount.Order);
            if (!isMountUnlocked)
            {
                continue;
            }

            ownedMountIds.Add(mount.RowId);
        }

        return ownedMountIds;
    }
}
