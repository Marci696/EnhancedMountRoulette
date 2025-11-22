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
    public ConfigWindow() : base(
        "Better Mount Roulette Configuration"
    )
    {
        //  Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        // Flags |= ImGuiWindowFlags.NoResize;

        //      Size = new Vector2(1200, 800);
        // Decides that size is used while opening, but is not static
        SizeCondition = ImGuiCond.Appearing;
    }

    public void Dispose() { }

    public override void PostDraw()
    {
        base.PostDraw();

        // Cleanup the dictionary for filter strings
        // todo call from somewhere else?
        OwnedMountsTable.ClearMountNameFilters(ConfigManager.Instance.MountLists.Values.Select((mountList => mountList.Id)));
    }

    public override void Draw()
    {
        //     ImGui.ShowMetricsWindow();

        PaddingY(10);

        // todo find out why this double name is needed
        new MountListTable.MountListTable().Draw();

        if (ImGui.Button("Add new whitelist"))
        {
            ConfigManager.Instance.StoreMountList(
                new MountList() { Name = ConfigManager.Instance.FindNewMountListName(), Type = MountListType.Whitelist }
            );
        }

        PaddingX(10);
        ImGui.SameLine();

        if (ImGui.Button("Add new blacklist"))
        {
            ConfigManager.Instance.StoreMountList(
                new MountList() { Name = ConfigManager.Instance.FindNewMountListName(), Type = MountListType.Blacklist }
            );
        }

        PaddingY(10);
    }
}
