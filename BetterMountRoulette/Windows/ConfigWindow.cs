using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BetterMountRoulette.Configuration;
using Dalamud.Bindings.ImGui;
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

    private string WhitelistCreateInputString = "";

    private string BlacklistCreateInputString = "";
    
    private MountList? NewMountList = null;

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Configuration.Configuration configuration) : base(
        "Configuration Window"
    )
    {
        //  Flags |= ImGuiWindowFlags.AlwaysAutoResize;

        //    Size = new Vector2(800, 800);
        // Decides that size is used while opening, but is not static
        SizeCondition = ImGuiCond.Appearing;

        this.configuration = configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        /*// Flags must be added or removed before Draw() is being called, or they won't apply
        if (configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }*/
    }

    public override void Draw()
    {
        var scale = ImGui.GetIO().FontGlobalScale;

        ImGui.SetWindowFontScale(scale);

        PaddingY(10);

        using (
            ImRaii.Table(
                "mountListTable",
                6,
                ImGuiTableFlags.Borders | ImGuiTableFlags.Hideable,
                new Vector2(0, 0)
            ))
        {
            ImGui.TableSetupColumn("List Name", ImGuiTableColumnFlags.WidthStretch, 3);
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch, 2);
            ImGui.TableSetupColumn("Is Default?", ImGuiTableColumnFlags.WidthStretch, 1);
            ImGui.TableSetupColumn(
                "Considered during Mount action",
                ImGuiTableColumnFlags.WidthStretch,
                5
            );
            ImGui.TableSetupColumn("Summon Type", ImGuiTableColumnFlags.WidthStretch, 2);
            ImGui.TableSetupColumn("###removeColumn", ImGuiTableColumnFlags.WidthStretch, 1);

            ImGui.TableHeadersRow();

            foreach (var mountList in configuration.OrderedMountList)
            {
                using (ImRaii.PushId("mountList_" + mountList.Id))
                {
                    RenderMountList(mountList);
                }
            }
        }

        if (ImGui.Button("Add new mount list"))
        {
            configuration.StoreMountList(new MountList() { Name = configuration.FindNewMountListName() });
        }

        PaddingY(10);
    }

    private void RenderMountList(MountList mountList)
    {
        ImGui.TableNextRow();

        var columnIndex = 0;

        ImGui.TableNextColumn();

        var mountListName = mountList.Name;

        NextItemWidth(250);

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
        
        // Make it go full width.
        ImGui.SetNextItemWidth(-1f);

        int currentListTypeIndex = mountList.Type == MountListType.Whitelist ? 0 : 1;
        if (ImGui.Combo("###Type", ref currentListTypeIndex, new[] { "Whitelist", "Blacklist" }))
        {
            // todo, must invert list when switching between those types   
        }

        ImGui.TableNextColumn();

        var checkboxValue = mountList.IsDefault;
        if (ImGui.Checkbox("###checkbox", ref checkboxValue))
        {
            configuration.StoreMountList(new MountList(mountList) { IsDefault = checkboxValue });
        }

        ImGui.TableNextColumn();

        RenderAvailableMountsSection(mountList);

        ImGui.TableNextColumn();

        // Make it go full width.
        ImGui.SetNextItemWidth(-1f);

        // todo find better way to do this
        int currentFetchTypeIndex = (int)mountList.FetchNextType;
        if (ImGui.Combo("###FetchNextType", ref currentFetchTypeIndex, Enum.GetNames<FetchNextType>()))
        {
            configuration.StoreMountList(
                new MountList(mountList) { FetchNextType = (FetchNextType)currentFetchTypeIndex }
            );
        }

        ImGui.TableNextColumn();

        if (ImGui.Button("Delete List"))
        {
            configuration.RemoveMountList(mountList);
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
