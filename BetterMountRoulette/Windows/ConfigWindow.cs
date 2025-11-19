using System;
using System.Collections.Generic;
using System.Linq;
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

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Configuration.Configuration configuration) : base(
        "Better Mount Roulette Configuration"
    )
    {
        //  Flags |= ImGuiWindowFlags.AlwaysAutoResize;

        //    Size = new Vector2(800, 800);
        // Decides that size is used while opening, but is not static
        SizeCondition = ImGuiCond.Appearing;

        this.configuration = configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        PaddingY(10);

        using (
            ImRaii.Table(
                "mountListTable",
                6,
                (ImGuiTableFlags.Borders & ~ImGuiTableFlags.BordersOuter) | ImGuiTableFlags.Hideable,
                // Grow automatically to fit content.
                new Vector2(0, 0)
            ))
        {
            // No idea why only the first item needs a space, all the others following are automatically padded.
            ImGui.TableSetupColumn(" List Name", ImGuiTableColumnFlags.WidthStretch, 3);
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch, 2);
            ImGui.TableSetupColumn("Default?", ImGuiTableColumnFlags.WidthStretch, 0.7f);
            ImGui.TableSetupColumn(
                "Considered during Mount action",
                ImGuiTableColumnFlags.WidthStretch,
                5
            );
            ImGui.TableSetupColumn("Summon Type", ImGuiTableColumnFlags.WidthStretch, 2);
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

        // Change X cross icon to red.
        using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1)))
        {
            var standardSize = ImGui.GetFrameHeight();

            CenterHorizontally(standardSize);

            if (ImGuiComponents.IconButton(
                    "Delete List",
                    // Looks like an X cross.
                    icon: FontAwesomeIcon.Times,
                    size: new Vector2(standardSize, standardSize),
                    // Hide background
                    defaultColor: new Vector4(0, 0, 0, 0),
                    hoveredColor: new Vector4(0.3f, 0.3f, 0.3f, 1),
                    // Color when it is clicked.
                    activeColor: new Vector4(0.6f, 0.6f, 0.6f, 1)
                ))
            {
                configuration.RemoveMountList(mountList);
            }
        }


        ImGui.TableNextRow();
    }

    private void RenderMountIcon(Mount mount)
    {
        if (Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup() { IconId = mount.Icon }).GetWrapOrDefault() is
            not { } texture)
        {
            return;
        }

        var scale = ImGui.GetIO().FontGlobalScale;

        ImGui.Image(texture.Handle, new Vector2(20, 20) * new Vector2(scale, scale));
    }

    private void RenderAvailableMountsSection(MountList mountList)
    {
        // TODO find way to cache between each draw
        var availableMountsForList = MountManager.GetAvailableMountsForList(mountList);
        var ownedIds = MountManager.GetOwnedMountIds();

        if (ImGui.CollapsingHeader($"{availableMountsForList.Count} / {ownedIds.Count}###collapsedMounts")
            && availableMountsForList.Count > 0)
        {
            using (
                ImRaii.Child(
                    "availableMounts",
                    new Vector2(0, 300),
                    border: false,
                    flags: ImGuiWindowFlags.AlwaysVerticalScrollbar
                )
            )
            {
                using (ImRaii.Table(
                        "mountTable",
                        2,
                        ImGuiTableFlags.Borders
                    ))
                {
                    ImGui.TableSetupColumn("##Mount", ImGuiTableColumnFlags.WidthStretch, 10);
                    ImGui.TableSetupColumn("##Remove", ImGuiTableColumnFlags.WidthStretch, 2);

                    ImGui.TableHeadersRow();

                    foreach (var mountId in availableMountsForList)
                    {
                        if (MountManager.GetMount(mountId) is not { } mount)
                        {
                            continue;
                        }

                        ImGui.TableNextRow();

                        ImGui.TableSetColumnIndex(0);

                        RenderMountIcon(mount);

                        ImGui.SameLine();

                        Text(mount.Singular.ExtractText());

                        ImGui.TableSetColumnIndex(1);

                        if (ImGui.Button("Remove###" + mountId))
                        {
                            Chat.Write("Clicked remove mount" + mount.RowId);
                        }
                    }
                }
            }
        }
    }
}
