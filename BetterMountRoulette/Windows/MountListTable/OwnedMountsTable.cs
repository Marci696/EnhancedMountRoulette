using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using BetterMountRoulette.Configuration;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using static BetterMountRoulette.Windows.DrawHelper;
using Action = System.Action;

namespace BetterMountRoulette.Windows.MountListTable;

public class OwnedMountsTable(Configuration.Configuration configuration, MountList mountList)
{
    private static Dictionary<int, string> MountNameFilters = new();

    private static readonly Vector2 TableSize = new(0, 300);

    private static readonly ReadOnlyCollection<ColorsForInList> ColorsMapForIsInList = Array.AsReadOnly(
        [
            new ColorsForInList(
                new Vector4(0.6f, 0.6f, 0.6f, 1),
                RgbaToImgGuiVector(255, 255, 255, .3f)
            ),
            new ColorsForInList(null, RgbaToImgGuiVector(255, 255, 255, 1))
        ]
    );

    public static void ClearMountNameFilters(IEnumerable<int> mountListIds)
    {
        MountNameFilters = MountNameFilters.Where(pair => mountListIds.Contains(pair.Key))
            .ToDictionary();
    }

    public void Draw()
    {
        var ownedMountIds = MountManager.GetOwnedMountIds();
        var availableMountsForSummoning = mountList.GetAvailableMountsForSummoning(ownedMountIds).ToList();

        if (ImGui.CollapsingHeader(
                $"{availableMountsForSummoning.Count} / {ownedMountIds.Count}###collapsedMounts_" + mountList.Id
            ))
        {
            DrawTable(
                ImRaii.Table(
                    "mountTable_" + mountList.Id,
                    2,
                    ImGuiTableFlags.ScrollY | (ImGuiTableFlags.Borders & ~ImGuiTableFlags.BordersV),
                    TableSize
                ),
                SetupColumns,
                rowCallbacks: [GetDrawFilterAndActionsRow(ownedMountIds), ..GetDrawMountRow(ownedMountIds)]
            );
        }
    }

    private static void SetupColumns()
    {
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 10);
        ImGui.TableSetupColumn("##Remove", ImGuiTableColumnFlags.WidthStretch, 1);
    }

    private IEnumerable<DrawColumnCallback> GetDrawFilterAndActionsRow(HashSet<uint> ownedMountIds)
    {
        yield return () =>
        {
            Text("Filter:");

            ImGui.SameLine();

            ImGui.SetNextItemWidth(ImGui.GetColumnWidth() / 2);

            var mountNameFilter = MountNameFilters.GetValueOrDefault(mountList.Id, "");

            if (ImGui.InputText("###Filter", ref mountNameFilter, 50))
            {
                MountNameFilters[mountList.Id] = mountNameFilter;
            }

            var addConfirmationPopupName = ConfirmationWindow(
                "Confirm replacement###add-all",
                "Are you sure you want to overwrite your current list,\nby adding all mounts to it?",
                () => configuration.ConsiderAllMountsForSummoning(mountList, ownedMountIds)
            );
            var removeConfirmationPopupName = ConfirmationWindow(
                "Confirm replacement###remove-all",
                "Are you sure you want to overwrite your current list,\nby removing all mounts from it?",
                () => configuration.OverlookAllMountsForSummoning(mountList, ownedMountIds)
            );

            ImGui.SameLine();
            PaddingX(15);

            if (ImGui.Button("Add All"))
            {
                ImGui.OpenPopup(addConfirmationPopupName);
            }

            ImGui.SameLine();
            PaddingX(5);

            if (ImGui.Button("Remove All"))
            {
                ImGui.OpenPopup(removeConfirmationPopupName);
            }
        };
    }

    private IEnumerable<IEnumerable<DrawColumnCallback>> GetDrawMountRow(HashSet<uint> ownedMountIds)
    {
        var mountNameFilter = MountNameFilters.GetValueOrDefault(mountList.Id, "");
        var filteredAvailableMountsForSummoning = MapMountIdsToFilteredMounts(
                mountNameFilter,
                mountList.GetAvailableMountsForSummoning(ownedMountIds)
            )
            .ToList();
        var filteredOwnedButUnavailableMountsForList =
            MapMountIdsToFilteredMounts(
                mountNameFilter,
                mountList.GetOwnedButUnavailableMountsForSummoning(ownedMountIds)
            );


        foreach (var mount in filteredAvailableMountsForSummoning)
        {
            using (ImRaii.PushId("mount_available_" + mount.RowId))
            {
                yield return GetMountRowColumns(mount, isInSummonList: true);
            }
        }

        foreach (var mount in filteredOwnedButUnavailableMountsForList)
        {
            using (ImRaii.PushId("mount_unavailable_" + mount.RowId))
            {
                yield return GetMountRowColumns(mount, isInSummonList: false);
            }
        }
    }

    private IEnumerable<DrawColumnCallback> GetMountRowColumns(Mount mount, bool isInSummonList)
    {
        var colors = ColorsMapForIsInList[Convert.ToInt32(isInSummonList)];

        return
        [
            () => DrawIconAndTextColumn(mount, isInSummonList, colors), () => DrawListActions(mount, isInSummonList)
        ];
    }

    private void DrawListActions(Mount mount, bool isInSummonList)
    {
        CenterHorizontally();

        if (!isInSummonList)
        {
            if (AddIconButton("Add"))
            {
                configuration.ConsiderMountForSummoning(mountList, mount);
            }
        }
        else
        {
            if (RemoveIconButton("Remove"))
            {
                configuration.OverlookMountFromSummoning(mountList, mount);
            }
        }
    }

    private void DrawIconAndTextColumn(Mount mount, bool isInSummonList, ColorsForInList colors)
    {
        DrawMountIcon(mount, colors);

        ImGui.SameLine();

        Text(
            CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mount.Singular.ExtractText()),
            color: colors.NameText
        );
    }

    private IEnumerable<Mount> MapMountIdsToFilteredMounts(string mountNameFilter, IEnumerable<uint> mountIds)
    {
        var availableMountsForListEnumerator = mountIds
            .Select((MountManager.GetMount))
            .OfType<Mount>();

        if (mountNameFilter.IsNullOrEmpty())
        {
            return availableMountsForListEnumerator;
        }

        return availableMountsForListEnumerator.Where(mount =>
            mount.Singular.ExtractText().Contains(mountNameFilter, StringComparison.CurrentCultureIgnoreCase)
        );
    }

    private void DrawMountIcon(Mount mount, ColorsForInList colors)
    {
        if (Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup() { IconId = mount.Icon }).GetWrapOrDefault() is
            not { } texture)
        {
            return;
        }

        var scale = ImGui.GetIO().FontGlobalScale;

        ImGui.Image(
            texture.Handle,
            size: new Vector2(20, 20) * new Vector2(scale, scale),
            tintCol: colors.MountIconTint
        );
    }

    private record ColorsForInList(Vector4? NameText, Vector4 MountIconTint);
}
