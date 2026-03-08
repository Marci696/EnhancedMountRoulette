using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.Utility.Raii;
using EnhancedMountRoulette.Commands;
using EnhancedMountRoulette.Configuration;
using static EnhancedMountRoulette.Windows.DrawHelper;

namespace EnhancedMountRoulette.Windows.Config;

public class MountListTable : Table
{
    // No idea why only the first item needs a space, all the others following are automatically padded.
    public const string NameColumn = " List Name";
    public const string TypeColumn = "Type";
    public const string DefaultColumn = "Default?";
    public const string OwnedMountsTableColumn = "Considered during Mount action";
    public const string FetchTypeColumn = "Summon Type";
    public const string RemoveColumn = "###removeColumn";
    public const string CopyToClipboardColumn = "###clipboard";

    public static readonly Vector2 TableSize = new(1400, 0);

    private static readonly Vector4 ErrorMessageColor = RgbaToImgGuiVector(186, 6, 6, 1);

    public static readonly string[] FixedOrderedColumnsIds =
    [
        NameColumn,
        DefaultColumn,
        OwnedMountsTableColumn,
        TypeColumn,
        FetchTypeColumn,
        CopyToClipboardColumn,
        RemoveColumn,
    ];

    public override string[] OrderedColumnIds => FixedOrderedColumnsIds;

    protected override ImRaii.IEndObject BeginTable() => ImRaii.Table(
        "mountListTable",
        OrderedColumnIds.Length,
        ImGuiTableFlags.Borders,
        // Grow automatically to fit content.
        TableSize
    );

    protected override Dictionary<string, SetupColumn> GetSetupColumns()
    {
        return new Dictionary<string, SetupColumn>()
        {
            [NameColumn] = () => ImGui.TableSetupColumn(NameColumn, ImGuiTableColumnFlags.WidthStretch, 2.7f),
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
            [CopyToClipboardColumn] = () => ImGui.TableSetupColumn(
                CopyToClipboardColumn,
                ImGuiTableColumnFlags.WidthStretch,
                0.3f
            ),
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
        [CopyToClipboardColumn] = () => DrawCopyToClipboardColumn(mountList),
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
                Text("Name can not be empty.", color: ErrorMessageColor);
            }
            else if (ConfigManager.Instance.MountLists.ContainsKey(mountListName))
            {
                Text("List with this name already exists.", color: ErrorMessageColor);
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
        if (ImGui.Combo("###Type", ref currentListTypeIndex, Enum.GetNames<MountListType>()))
        {
            ConfigManager.Instance.ChangeMountListType(mountList, (MountListType)currentListTypeIndex);
        }
    }

    private void DrawFetchTypeColumn(MountList mountList)
    {
        FullWidth();

        int currentFetchTypeIndex = (int)mountList.FetchNextType;
        if (ImGui.Combo("###FetchNextType", ref currentFetchTypeIndex, Enum.GetNames<FetchNextType>()))
        {
            ConfigManager.Instance.StoreMountList(
                new MountList(mountList) { FetchNextType = (FetchNextType)currentFetchTypeIndex }
            );
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

    private void DrawDeleteListColumn(MountList mountList)
    {
        CenterHorizontally();

        var open = ConfirmationWindow(
            "Delete Confirmation",
            $"Are you sure you want to delete your list \"{mountList.Name}\"?",
            () => ConfigManager.Instance.RemoveMountList(mountList)
        );

        if (RemoveIconButton("deleteMountList", tooltip: "Delete list"))
        {
            open();
        }
    }

    private void DrawCopyToClipboardColumn(MountList mountList)
    {
        CenterHorizontally();

        if (CopyClipboardButton("Copy to clipboard", tooltip: "Copy macro to clipboard"))
        {
            ImGui.SetClipboardText(SummonMountCommand.GetMacro(mountList));

            Plugin.ToastGui.ShowNormal(
                "Copied to clipboard",
                new ToastOptions { Position = ToastPosition.Bottom, Speed = ToastSpeed.Fast }
            );
        }
    }
}
