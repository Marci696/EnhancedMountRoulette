using System;
using System.Collections.Generic;
using System.Numerics;
using BetterMountRoulette.Configuration;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
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

        foreach (var mountListType in Enum.GetValues<MountListType>())
        {
            using (new Use(() => ImGui.PushID("mountListType_" + mountListType), ImGui.PopID))
            {
                PaddingY(10);

                Text(mountListType.AsString().FirstCharToUpper(), TextScale.H2);

                PaddingY(10);

                using (new Use(
                        () =>
                        {
                            ImGui.BeginTable(
                                "mountListTable" + mountListType,
                                3,
                                ImGuiTableFlags.Borders | ImGuiTableFlags.Hideable,
                                new Vector2(0, 0)
                            );
                        },
                        ImGui.EndTable
                    ))
                {
                    ImGui.TableSetupColumn("List Name", ImGuiTableColumnFlags.WidthStretch, initWidthOrWeight: 3);

                    ImGui.TableSetupColumn(
                        "Considered during Mount action",
                        ImGuiTableColumnFlags.WidthStretch,
                        initWidthOrWeight: 3
                    );

                    ImGui.TableSetupColumn("", flags: ImGuiTableColumnFlags.WidthStretch, initWidthOrWeight: 1);

                    ImGui.TableHeadersRow();

                    foreach (var mountList in configuration.GetMountLists(mountListType))
                    {
                        using (new Use(() => ImGui.PushID("mountList_" + mountList.GetHashCode()), ImGui.PopID))
                        {
                            RenderMountList(mountList);
                        }
                    }
                }


                PaddingY(10);

                ImGui.Separator();

                RenderAddNewListSection(mountListType);
            }
        }

        // Can't ref a property, so use a local copy
        /*var configValue = configuration.SomePropertyToBeSavedAndWithADefault;
        if (ImGui.Checkbox("Random Config Bool", ref configValue))
        {
            // Can save immediately on change if you don't want to provide a "Save and Close" button
            configuration.Save();
        }*/

        /*var movable = configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable Config Window", ref movable))
        {
            configuration.IsConfigWindowMovable = movable;
            configuration.Save();
        }*/
    }

    private void RenderAddNewListSection(MountListType mountListType)
    {
        ref var inputString = ref (mountListType == MountListType.Whitelist
            ? ref WhitelistCreateInputString
            : ref BlacklistCreateInputString);


        Text("Name of new entry:");

        ImGui.SameLine();

        ImGui.InputText(
            "",
            ref inputString,
            255
        );

        if (inputString.Length == 0)
        {
            Text("Name can not be empty.");
        }
        else if (configuration.MountLists.ContainsKey(inputString))
        {
            // todo color
            Text("Mount list with this name already exists.");
        }
        else
        {
            if (ImGui.Button("Add"))
            {
                configuration.StoreMountList(new MountList() { Name = inputString, Type = mountListType });
                inputString = "";
            }
        }
    }

    private void RenderMountList(MountList mountList)
    {
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);

        var mountName = mountList.Name;

        NextItemWidth(250);

        // Goes into the if block when something changed.
        if (ImGui.InputText("", ref mountName, 50))
        {
            if (mountName.Length == 0)
            {
                // TODO change color
                Text("Name can not be empty.");
            }
            else if (configuration.MountLists.ContainsKey(mountName))
            {
                // TODO change color
                Text("Mount list with this name already exists.");
            }
            else
            {
                configuration.RenameMountList(mountList, mountName);
            }
        }

        ImGui.TableSetColumnIndex(2);

        if (ImGui.Button("Delete List"))
        {
            configuration.RemoveMountList(mountList);
        }

        ImGui.TableSetColumnIndex(1);

        RenderAvailableMountsSection(mountList);

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

        if (ImGui.CollapsingHeader($"{availableMountsForList.Count} / {ownedIds.Count}"))
        {
            using (new Use(
                    () =>
                    {
                        ImGui.BeginChild(
                            "availableMounts",
                            new Vector2(0, 300),
                            flags: ImGuiWindowFlags.AlwaysVerticalScrollbar
                        );
                    },
                    ImGui.EndChild
                ))
            {
                using (new Use(
                        () =>
                        {
                            ImGui.BeginTable(
                                "mountTable",
                                2,
                                ImGuiTableFlags.Borders
                            );
                        },
                        ImGui.EndTable
                    ))
                {
                    ImGui.TableSetupColumn("Mount");
                    ImGui.TableSetupColumn("");

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
