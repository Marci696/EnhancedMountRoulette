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
        // todo call from somewhere else?
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
}
