using System.Collections.Generic;
using System.Net.Mime;
using System.Numerics;
using BetterMountRoulette.Commands;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using static BetterMountRoulette.Windows.DrawHelper;

namespace BetterMountRoulette.Windows.Config;

public class MountListExplanationTable : Table
{
    private const string NameColumn = "Column Name";
    private const string ExplanationColumn = "Explanation";

    public override string[] OrderedColumnIds => [NameColumn, ExplanationColumn];

    protected override ImRaii.IEndObject BeginTable() => ImRaii.Table(
        "mountListTable",
        OrderedColumnIds.Length,
        flags: 0,
        // Grow automatically to fit content.
        outerSize: new Vector2(1200, 0)
    );

    protected override Dictionary<string, SetupColumn> GetSetupColumns() => new()
    {
        [NameColumn] = () => ImGui.TableSetupColumn(NameColumn, ImGuiTableColumnFlags.WidthStretch, 1),
        [ExplanationColumn] = () => ImGui.TableSetupColumn(ExplanationColumn, ImGuiTableColumnFlags.WidthStretch, 10),
    };

    protected override IEnumerable<Row> GetRows()
    {
        return
        [
            new Row(
                new Dictionary<string, DrawColumnCallback>()
                {
                    [NameColumn] = () => ImGui.Text(MountListTable.NameColumn),
                    [ExplanationColumn] = DrawNameExplanation
                },
                "mountListExplanation"
            )
        ];
    }

    private void DrawNameExplanation()
    {
        var exampleName = "Frontline PVP";

        ImGui.TextWrapped(
            "You can change the name to whatever you like, only condition is that it needs to be unique.\n\n"
            + $"Name is relevant when using the command to summon a mount.\n"
            + $"If your list is called \"{exampleName}\" for example, you summon its mounts via:"
        );

        ImGui.TextColoredWrapped(CommandColor, SummonMountCommand.GetCommandWithListName(exampleName.ToLower()));

        ImGui.SameLine();
        ImGui.Text("(note that it is case insensitive)");
    }
}
