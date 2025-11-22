using System;
using System.Linq;
using System.Numerics;
using BetterMountRoulette.Configuration;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using static BetterMountRoulette.Windows.DrawHelper;

namespace BetterMountRoulette.Windows.Config;

public class ConfigWindow : Window, IDisposable
{
    private readonly MountListTable mountListTable = new MountListTable();

    public ConfigWindow() : base(
        "Better Mount Roulette Configuration"
    )
    {
        // Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        // Flags |= ImGuiWindowFlags.NoResize;
        
         
        // Auto resize when it is opened.
        SizeCondition = ImGuiCond.Appearing;

        Size = MountListTable.TableSize with { Y = 500 };
    }

    public void Dispose() { }

    public override void PostDraw()
    {
        base.PostDraw();
        
        OwnedMountsTable.ClearMountNameFilters(
            ConfigManager.Instance.MountLists.Values.Select((mountList => mountList.Id))
        );
    }

    public override void Draw()
    {
        //     ImGui.ShowMetricsWindow();

        PaddingY(10);

        mountListTable.Draw();

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
