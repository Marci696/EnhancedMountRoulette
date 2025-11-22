using System;
using System.Collections.Generic;
using System.Numerics;
using BetterMountRoulette.Configuration;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using static BetterMountRoulette.Windows.DrawHelper;


namespace BetterMountRoulette.Windows.Config;

public class MountListTable : Table
{
    // No idea why only the first item needs a space, all the others following are automatically padded.
    private const string NameColumn = " List Name";
    private const string TypeColumn = "Type";
    private const string DefaultColumn = "Default?";
    private const string OwnedMountsTableColumn = "Considered during Mount action";
    private const string FetchTypeColumn = "Summon Type";
    private const string RemoveColumn = "###removeColumn";

    public override string[] OrderedColumnIds =>
    [
        NameColumn,
        TypeColumn,
        DefaultColumn,
        OwnedMountsTableColumn,
        // todo keep in line
        FetchTypeColumn,
        RemoveColumn,
    ];

    protected override ImRaii.IEndObject BeginTable() => ImRaii.Table(
        "mountListTable",
        6,
        (ImGuiTableFlags.Borders & ~ImGuiTableFlags.BordersOuter) | ImGuiTableFlags.Hideable,
        // Grow automatically to fit content.
        new Vector2(1400, 0)
    );

    protected override Dictionary<string, SetupColumn> GetSetupColumns()
    {
        return new Dictionary<string, SetupColumn>()
        {
            [NameColumn] = () => ImGui.TableSetupColumn(NameColumn, ImGuiTableColumnFlags.WidthStretch, 3),
            [TypeColumn] = () => ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch, 1.5f),
            [DefaultColumn] = () => ImGui.TableSetupColumn(DefaultColumn, ImGuiTableColumnFlags.WidthStretch, 0.7f),
            [OwnedMountsTableColumn] = () => ImGui.TableSetupColumn(
                OwnedMountsTableColumn,
                ImGuiTableColumnFlags.WidthStretch,
                5
            ),
            [FetchTypeColumn] = () => ImGui.TableSetupColumn(
                FetchTypeColumn,
                ImGuiTableColumnFlags.WidthStretch,
                1.5f
            ),
            [RemoveColumn] = () => ImGui.TableSetupColumn(RemoveColumn, ImGuiTableColumnFlags.WidthStretch, 0.3f),
        };
    }

    protected override IEnumerable<Row> GetRows()
    {
        foreach (var mountList in ConfigManager.Instance.OrderedMountList)
        {
            yield return new Row(GetRowColumns(mountList), "mountList_" + mountList.Id);
        }
    }

    private Dictionary<string, DrawColumnCallback> GetRowColumns(MountList mountList) => new()
    {
        [NameColumn] = () => DrawNameColumn(mountList),
        [TypeColumn] = () => DrawTypeColumn(mountList),
        [DefaultColumn] = () => DrawIsDefaultColumn(mountList),
        [OwnedMountsTableColumn] = new OwnedMountsTable(mountList).Draw,
        [FetchTypeColumn] = () => DrawFetchTypeColumn(mountList),
        [RemoveColumn] = () => DrawDeleteListColumn(mountList),
    };

    private void DrawNameColumn(MountList mountList)
    {
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
            else if (ConfigManager.Instance.MountLists.ContainsKey(mountListName))
            {
                // TODO change color
                Text("Mount list with this name already exists.");
            }
            else
            {
                ConfigManager.Instance.RenameMountList(mountList, mountListName);
            }
        }
    }

    private void DrawTypeColumn(MountList mountList)
    {
        FullWidth();

        int currentListTypeIndex = (int)mountList.Type;
        if (ImGui.Combo("###Type", ref currentListTypeIndex, new[] { "Whitelist", "Blacklist" }))
        {
            ConfigManager.Instance.ChangeMountListType(mountList, (MountListType)currentListTypeIndex);
        }
    }

    private void DrawIsDefaultColumn(MountList mountList)
    {
        CenterHorizontally();

        var checkboxValue = mountList.IsDefault;
        if (ImGui.Checkbox("###checkbox", ref checkboxValue))
        {
            ConfigManager.Instance.StoreMountList(new MountList(mountList) { IsDefault = checkboxValue });
        }
    }

    private void DrawFetchTypeColumn(MountList mountList)
    {
        FullWidth();

        // todo find better way to do this
        int currentFetchTypeIndex = (int)mountList.FetchNextType;
        if (ImGui.Combo("###FetchNextType", ref currentFetchTypeIndex, Enum.GetNames<FetchNextType>()))
        {
            ConfigManager.Instance.StoreMountList(
                new MountList(mountList) { FetchNextType = (FetchNextType)currentFetchTypeIndex }
            );
        }
    }

    private void DrawDeleteListColumn(MountList mountList)
    {
        CenterHorizontally();

        var open = ConfirmationWindow(
            "Delete Confirmation",
            $"Are you sure you want to delete your list \"{mountList.Name}\"?",
            () => ConfigManager.Instance.RemoveMountList(mountList)
        );

        if (RemoveIconButton("Delete mount list"))
        {
            open();
        }
    }
}
