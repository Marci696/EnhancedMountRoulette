using System;
using System.Collections.Generic;
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

namespace BetterMountRoulette.Windows.MountListTable;

public class OwnedMountsTable(Configuration.Configuration configuration)
{
    private static Dictionary<int, string> MountNameFilters = new();
    
    private static readonly Vector2 TableSize = new(0, 300);

    public static void ClearMountNameFilters(IEnumerable<int> mountListIds)
    {
        MountNameFilters = MountNameFilters.Where(pair => mountListIds.Contains(pair.Key))
            .ToDictionary();
    }

    public void Draw(MountList mountList)
    {
        var mountNameFilter = MountNameFilters.GetValueOrDefault(mountList.Id, "");
        var ownedMountIds = MountManager.GetOwnedMountIds();
        var availableMountsForSummoning = mountList.GetAvailableMountsForSummoning(ownedMountIds).ToList();

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

        if (ImGui.CollapsingHeader(
                $"{availableMountsForSummoning.Count} / {ownedMountIds.Count}###collapsedMounts_" + mountList.Id
            ))
        {
            {
                using (ImRaii.Table(
                        "mountTable_" + mountList.Id,
                        2,
                        ImGuiTableFlags.ScrollY | (ImGuiTableFlags.Borders & ~ImGuiTableFlags.BordersV),
                        TableSize
                    ))
                {
                    DrawHeadersRow();

                    DrawFilterAndActionsRow(mountList, ownedMountIds);

                    foreach (var mount in filteredAvailableMountsForSummoning)
                    {
                        using (ImRaii.PushId("mount_available_" + mount.RowId))
                        {
                            DrawMountItem(mountList, mount, isInSummonList: true);
                        }
                    }

                    foreach (var mount in filteredOwnedButUnavailableMountsForList)
                    {
                        using (ImRaii.PushId("mount_unavailable_" + mount.RowId))
                        {
                            DrawMountItem(mountList, mount, isInSummonList: false);
                        }
                    }
                }
            }
        }
    }

    private static void DrawHeadersRow()
    {
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 10);
        ImGui.TableSetupColumn("##Remove", ImGuiTableColumnFlags.WidthStretch, 1);
        ImGui.TableHeadersRow();
    }

    private void DrawFilterAndActionsRow(MountList mountList, HashSet<uint> ownedMountIds)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();

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
    }

    public void DrawMountItem(MountList mountList, Mount mount, bool isInSummonList)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();

        var tintColor = !isInSummonList ? new Vector4(1, 1, 1, .3f) : new Vector4(1, 1, 1, 1);
        DrawMountIcon(mount, tintColor);

        ImGui.SameLine();

        // todo only do it in english game language, as the others are correctly written already
        Vector4? textColor = !isInSummonList ? new Vector4(0.6f, 0.6f, 0.6f, 1) : null;

        Text(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mount.Singular.ExtractText()), color: textColor);

        ImGui.TableNextColumn();

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

    private void DrawMountIcon(Mount mount, Vector4 tintColor)
    {
        if (Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup() { IconId = mount.Icon }).GetWrapOrDefault() is
            not { } texture)
        {
            return;
        }

        var scale = ImGui.GetIO().FontGlobalScale;

        ImGui.Image(texture.Handle, size: new Vector2(20, 20) * new Vector2(scale, scale), tintCol: tintColor);
    }
}
