using System;
using System.Linq;
using System.Numerics;
using BetterMountRoulette.Configuration;
using BetterMountRoulette.Windows.MountListTable;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using static BetterMountRoulette.Windows.DrawHelper;

namespace BetterMountRoulette.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration.Configuration configuration;

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

        // Cleanup the dictionary for filter strings
        OwnedMountsTable.ClearMountNameFilters(configuration.MountLists.Values.Select((mountList => mountList.Id)));
    }

    public override void Draw()
    {
        //     ImGui.ShowMetricsWindow();

        PaddingY(10);

        // todo find out why this double name is needed
        new MountListTable.MountListTable(configuration).Draw();

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
        ImGui.TableNextColumn();


        FullWidth();

        var mountListName = mountList.Name;
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

        DrawTypeColumn(mountList);

        ImGui.TableNextColumn();

        new OwnedMountsTable(configuration, mountList).Draw();

        ImGui.TableNextColumn();

        DrawFetchTypeColumn(mountList);

        ImGui.TableNextColumn();

        DeleteListColumn(mountList);


        ImGui.TableNextRow();
    }

    private void DeleteListColumn(MountList mountList)
    {
        CenterHorizontally();

        if (RemoveIconButton("Delete mount list"))
        {
            configuration.RemoveMountList(mountList);
        }
    }

    private void DrawFetchTypeColumn(MountList mountList)
    {
        FullWidth();

        // todo find better way to do this
        int currentFetchTypeIndex = (int)mountList.FetchNextType;
        if (ImGui.Combo("###FetchNextType", ref currentFetchTypeIndex, Enum.GetNames<FetchNextType>()))
        {
            configuration.StoreMountList(
                new MountList(mountList) { FetchNextType = (FetchNextType)currentFetchTypeIndex }
            );
        }
    }

    private void DrawTypeColumn(MountList mountList)
    {
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
    }
}
