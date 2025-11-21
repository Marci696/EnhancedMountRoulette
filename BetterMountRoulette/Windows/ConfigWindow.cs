using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Numerics;
using BetterMountRoulette.Configuration;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Style;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using static BetterMountRoulette.Windows.DrawHelper;

namespace BetterMountRoulette.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration.Configuration configuration;

    private Dictionary<int, string> mountNameFilters = new();

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Configuration.Configuration configuration) : base(
        "Better Mount Roulette Configuration"
    )
    {
        //  Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        // Flags |= ImGuiWindowFlags.NoResize;

        //      Size = new Vector2(1200, 800);
        // Decides that size is used while opening, but is not static
        SizeCondition = ImGuiCond.Appearing;

        this.configuration = configuration;
    }

    public void Dispose() { }

    public override void PostDraw()
    {
        base.PostDraw();

        // Cleanup the dictionary for filter strings;
        var mountListIds = configuration.MountLists.Values.Select((mountList => mountList.Id)).ToHashSet();
        mountNameFilters = mountNameFilters.Where((pair => mountListIds.Contains(pair.Key))).ToDictionary();
    }

    public override void Draw()
    {
        ImGui.ShowMetricsWindow();

        PaddingY(10);

        using (
            ImRaii.Table(
                "mountListTable",
                6,
                (ImGuiTableFlags.Borders & ~ImGuiTableFlags.BordersOuter) | ImGuiTableFlags.Hideable,
                // Grow automatically to fit content.
                new Vector2(1400, 0)
            ))
        {
            // No idea why only the first item needs a space, all the others following are automatically padded.
            ImGui.TableSetupColumn(" List Name", ImGuiTableColumnFlags.WidthStretch, 3);
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch, 1.5f);
            ImGui.TableSetupColumn("Default?", ImGuiTableColumnFlags.WidthStretch, 0.7f);
            ImGui.TableSetupColumn(
                "Considered during Mount action",
                ImGuiTableColumnFlags.WidthStretch,
                5
            );
            ImGui.TableSetupColumn("Summon Type", ImGuiTableColumnFlags.WidthStretch, 1.5f);
            ImGui.TableSetupColumn("###removeColumn", ImGuiTableColumnFlags.WidthStretch, 0.3f);

            ImGui.TableHeadersRow();

            foreach (var mountList in configuration.OrderedMountList)
            {
                using (ImRaii.PushId("mountList_" + mountList.Id))
                {
                    RenderMountList(mountList);
                }
            }
        }

        if (ImGui.Button("Add new whitelist"))
        {
            configuration.StoreMountList(
                new MountList() { Name = configuration.FindNewMountListName(), Type = MountListType.Whitelist }
            );
        }

        PaddingX(10);
        ImGui.SameLine();

        if (ImGui.Button("Add new blacklist"))
        {
            configuration.StoreMountList(
                new MountList() { Name = configuration.FindNewMountListName(), Type = MountListType.Blacklist }
            );
        }

        PaddingY(10);
    }

    private void RenderMountList(MountList mountList)
    {
        ImGui.TableNextRow();

        // todo get rid of index
        var columnIndex = 0;

        ImGui.TableNextColumn();

        var mountListName = mountList.Name;

        FullWidth();

        // Goes into the if block when something changed.
        if (ImGui.InputText("###name", ref mountListName, 50))
        {
            if (mountListName.Length == 0)
            {
                // TODO change color
                Text("Name can not be empty.");
            }
            else if (configuration.MountLists.ContainsKey(mountListName))
            {
                // TODO change color
                Text("Mount list with this name already exists.");
            }
            else
            {
                configuration.RenameMountList(mountList, mountListName);
            }
        }

        ImGui.TableNextColumn();

        FullWidth();

        int currentListTypeIndex = (int)mountList.Type;
        if (ImGui.Combo("###Type", ref currentListTypeIndex, new[] { "Whitelist", "Blacklist" }))
        {
            configuration.ChangeMountListType(mountList, (MountListType)currentListTypeIndex);
        }

        ImGui.TableNextColumn();

        CenterHorizontally();

        var checkboxValue = mountList.IsDefault;
        if (ImGui.Checkbox("###checkbox", ref checkboxValue))
        {
            configuration.StoreMountList(new MountList(mountList) { IsDefault = checkboxValue });
        }

        ImGui.TableNextColumn();

        RenderAvailableMountsSection(mountList);

        ImGui.TableNextColumn();

        FullWidth();

        // todo find better way to do this
        int currentFetchTypeIndex = (int)mountList.FetchNextType;
        if (ImGui.Combo("###FetchNextType", ref currentFetchTypeIndex, Enum.GetNames<FetchNextType>()))
        {
            configuration.StoreMountList(
                new MountList(mountList) { FetchNextType = (FetchNextType)currentFetchTypeIndex }
            );
        }

        ImGui.TableNextColumn();

        CenterHorizontally();

        if (RemoveIconButton("Delete mount list"))
        {
            configuration.RemoveMountList(mountList);
        }


        ImGui.TableNextRow();
    }

    private void RenderMountIcon(Mount mount, Vector4 tintColor)
    {
        if (Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup() { IconId = mount.Icon }).GetWrapOrDefault() is
            not { } texture)
        {
            return;
        }

        var scale = ImGui.GetIO().FontGlobalScale;

        ImGui.Image(texture.Handle, size: new Vector2(20, 20) * new Vector2(scale, scale), tintCol: tintColor);
    }

    private void RenderAvailableMountsSection(MountList mountList)
    {
        // todo maybe set based on game language setting
        var textInfo = CultureInfo.CurrentCulture.TextInfo;

        var mountNameFilter = mountNameFilters.GetValueOrDefault(mountList.Id, "");

        var ownedMountIds = MountManager.GetOwnedMountIds();

        var availableMountsForSummoning = mountList.GetAvailableMountsForSummoning(ownedMountIds).ToList();

        var filteredAvailableMountsForSummoning = MapMountIdsToFilteredMounts(
                mountList.GetAvailableMountsForSummoning(ownedMountIds),
                mountNameFilter
            )
            .ToList();
        var filteredOwnedButUnavailableMountsForList =
            MapMountIdsToFilteredMounts(
                mountList.GetOwnedButUnavailableMountsForSummoning(ownedMountIds),
                mountNameFilter
            );

        if (ImGui.CollapsingHeader(
                $"{availableMountsForSummoning.Count} / {ownedMountIds.Count}###collapsedMounts_" + mountList.Id
            ))
        {
            /*using (
                ImRaii.Child(
                    "ownedMountsList_" + mountList.Id,
                    new Vector2(0, 300),
                    border: false,
                    flags: ImGuiWindowFlags.AlwaysVerticalScrollbar
                )
            )*/
            {
                using (ImRaii.Table(
                        "mountTable_" + mountList.Id,
                        2,
                        ImGuiTableFlags.ScrollY | ImGuiTableFlags.Borders & ~ImGuiTableFlags.BordersV,
                        new Vector2(0, 300)
                    ))
                {
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 10);
                    ImGui.TableSetupColumn("##Remove", ImGuiTableColumnFlags.WidthStretch, 1);
                    ImGui.TableHeadersRow();

                    #region Name Filter

                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);

                    Text("Filter:");

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(ImGui.GetColumnWidth() / 2);

                    if (ImGui.InputText("###Filter", ref mountNameFilter, 50))
                    {
                        mountNameFilters[mountList.Id] = mountNameFilter;
                    }


                    ImGui.SameLine();
                    PaddingX(15);

                    if (ImGui.Button("Add All"))
                    {
                        configuration.ConsiderAllMountsForSummoning(mountList, ownedMountIds);
                    }

                    ImGui.SameLine();
                    PaddingX(5);

                    if (ImGui.Button("Remove All"))
                    {
                        configuration.OverlookAllMountsForSummoning(mountList, ownedMountIds);
                    }

                    #endregion

                    foreach (var mount in filteredAvailableMountsForSummoning)
                    {
                        using (ImRaii.PushId("mount_available_" + mount.RowId))
                        {
                            RenderMountItem(mountList, mount, isInSummonList: true, textInfo);
                        }
                    }

                    foreach (var mount in filteredOwnedButUnavailableMountsForList)
                    {
                        using (ImRaii.PushId("mount_unavailable_" + mount.RowId))
                        {
                            RenderMountItem(mountList, mount, isInSummonList: false, textInfo);
                        }
                    }
                }
            }
        }
    }

    public void RenderMountItem(MountList mountList, Mount mount, bool isInSummonList, TextInfo textInfo)
    {
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);

        var tintColor = !isInSummonList ? new Vector4(1, 1, 1, .3f) : new Vector4(1, 1, 1, 1);
        RenderMountIcon(mount, tintColor);

        ImGui.SameLine();

        // todo only do it in english game language, as the others are correctly written already
        Vector4? textColor = !isInSummonList ? new Vector4(0.6f, 0.6f, 0.6f, 1) : null;

        Text(textInfo.ToTitleCase(mount.Singular.ExtractText()), color: textColor);

        ImGui.TableSetColumnIndex(1);

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

    private static IEnumerable<Mount> MapMountIdsToFilteredMounts(IEnumerable<uint> mountIds, string mountNameFilter)
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
}
