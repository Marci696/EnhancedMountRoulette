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

namespace BetterMountRoulette.Windows.MountListTable;

public class OwnedMountsTable(MountList mountList)
    : Table
{
    private const string NameColumn = "Name";
    private const string RemoveColumn = "###Remove";

    private static Dictionary<int, string> MountNameFilters = new();

    private static readonly Vector2 TableSize = new(0, 300);

    private static readonly ReadOnlyCollection<ColorsForInList> ColorsMapForIsInList = Array.AsReadOnly(
        [
            new ColorsForInList(
                RgbaToImgGuiVector(153, 153, 153, 1),
                RgbaToImgGuiVector(255, 255, 255, .3f)
            ),
            new ColorsForInList(null, RgbaToImgGuiVector(255, 255, 255, 1))
        ]
    );

    public override string[] OrderedColumnIds => [NameColumn, RemoveColumn,];

    private readonly HashSet<uint> ownedMountIds = MountManager.GetOwnedMountIds();

    private string mountNameFilter = MountNameFilters.GetValueOrDefault(mountList.Id, "");

    public static void ClearMountNameFilters(IEnumerable<int> mountListIds)
    {
        MountNameFilters = MountNameFilters.Where(pair => mountListIds.Contains(pair.Key))
            .ToDictionary();
    }


    public override void Draw()
    {
        var availableMountsForSummoning = mountList.GetAvailableMountsForSummoning(ownedMountIds).ToList();

        if (ImGui.CollapsingHeader(
                $"{availableMountsForSummoning.Count} / {ownedMountIds.Count}###collapsedMounts_" + mountList.Id
            ))
        {
            base.Draw();
        }
    }

    protected override ImRaii.IEndObject BeginTable() => ImRaii.Table(
        "mountTable_" + mountList.Id,
        2,
        ImGuiTableFlags.ScrollY | (ImGuiTableFlags.Borders & ~ImGuiTableFlags.BordersV),
        TableSize
    );

    protected override Dictionary<string, SetupColumn> GetSetupColumns()
    {
        return new Dictionary<string, SetupColumn>
        {
            [NameColumn] = () => ImGui.TableSetupColumn(NameColumn, ImGuiTableColumnFlags.WidthStretch, 10),
            [RemoveColumn] = () => ImGui.TableSetupColumn(RemoveColumn, ImGuiTableColumnFlags.WidthStretch, 1),
        };
    }

    protected override IEnumerable<Row> GetRows()
    {
        yield return new Row(GetDrawFilterAndActionsRow(), "actionRow");

        var filteredAvailableMountsForSummoning = MapMountIdsToFilteredMounts(
                mountList.GetAvailableMountsForSummoning(ownedMountIds)
            )
            .ToList();
        var filteredOwnedButUnavailableMountsForList =
            MapMountIdsToFilteredMounts(
                mountList.GetOwnedButUnavailableMountsForSummoning(ownedMountIds)
            );

        foreach (var mount in filteredAvailableMountsForSummoning)
        {
            yield return new Row(GetMountRowColumns(mount, isInSummonList: true), "mount_available_" + mount.RowId);
        }

        foreach (var mount in filteredOwnedButUnavailableMountsForList)
        {
            yield return new Row(GetMountRowColumns(mount, isInSummonList: false), "mount_unavailable_" + mount.RowId);
        }
    }

    private Dictionary<string, DrawColumnCallback> GetDrawFilterAndActionsRow()
    {
        return new Dictionary<string, DrawColumnCallback>()
        {
            [NameColumn] = () =>
            {
                Text("Filter:");

                ImGui.SameLine();

                ImGui.SetNextItemWidth(ImGui.GetColumnWidth() / 2);

                if (ImGui.InputText("###Filter", ref mountNameFilter, 50))
                {
                    MountNameFilters[mountList.Id] = mountNameFilter;
                }

                var addConfirmationPopupName = ConfirmationWindow(
                    "Confirm replacement###add-all",
                    "Are you sure you want to overwrite your current list,\nby adding all mounts to it?",
                    () => ConfigManager.Instance.ConsiderAllMountsForSummoning(mountList, ownedMountIds)
                );
                var removeConfirmationPopupName = ConfirmationWindow(
                    "Confirm replacement###remove-all",
                    "Are you sure you want to overwrite your current list,\nby removing all mounts from it?",
                    () => ConfigManager.Instance.OverlookAllMountsForSummoning(mountList, ownedMountIds)
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
        };
    }

    private Dictionary<string, DrawColumnCallback> GetMountRowColumns(Mount mount, bool isInSummonList)
    {
        var colors = ColorsMapForIsInList[Convert.ToInt32(isInSummonList)];

        return new Dictionary<string, DrawColumnCallback>
        {
            [NameColumn] = () => DrawIconAndTextColumn(mount, colors),
            [RemoveColumn] = () => DrawListActions(mount, isInSummonList)
        };
    }

    private void DrawListActions(Mount mount, bool isInSummonList)
    {
        CenterHorizontally();

        if (!isInSummonList)
        {
            if (AddIconButton("Add"))
            {
                ConfigManager.Instance.ConsiderMountForSummoning(mountList, mount);
            }
        }
        else
        {
            if (RemoveIconButton("Remove"))
            {
                ConfigManager.Instance.OverlookMountFromSummoning(mountList, mount);
            }
        }
    }

    private void DrawIconAndTextColumn(Mount mount, ColorsForInList colors)
    {
        DrawMountIcon(mount, colors);

        ImGui.SameLine();

        Text(
            CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mount.Singular.ExtractText()),
            color: colors.NameText
        );
    }

    private IEnumerable<Mount> MapMountIdsToFilteredMounts(IEnumerable<uint> mountIds)
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
