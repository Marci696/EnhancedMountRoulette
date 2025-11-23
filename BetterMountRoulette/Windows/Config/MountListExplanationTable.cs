using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BetterMountRoulette.Commands;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
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
        flags: ImGuiTableFlags.BordersInnerH,
        // Grow automatically to fit content.
        outerSize: new Vector2(1200, 0)
    );

    protected override Dictionary<string, SetupColumn> GetSetupColumns() => new()
    {
        [NameColumn] = () => ImGui.TableSetupColumn(NameColumn, ImGuiTableColumnFlags.WidthStretch, 1.8f),
        [ExplanationColumn] = () => ImGui.TableSetupColumn(ExplanationColumn, ImGuiTableColumnFlags.WidthStretch, 10),
    };

    protected override IEnumerable<Row> GetRows()
    {
        IEnumerable<(DrawColumnCallback DrawColumnName, DrawColumnCallback DrawExplanation)> rows =
            MountListTable.FixedOrderedColumnsIds.Select((columnId) =>
                {
                    DrawColumnCallback drawFunction = columnId switch
                    {
                        MountListTable.NameColumn => DrawNameExplanation,
                        MountListTable.DefaultColumn => DrawDefaultCheckboxExplanation,
                        MountListTable.OwnedMountsTableColumn => DrawOwnedMountsExplanation,
                        MountListTable.CopyToClipboardColumn => DrawCopyToClipboardExplanation,
                        MountListTable.RemoveColumn => DrawRemoveListExplanation,
                        MountListTable.TypeColumn => DrawMountListTypeExplanation,
                        _ => () => { },
                    };

                    DrawColumnCallback drawColumnName = columnId switch
                    {
                        // todo can i have the icon here instead?
                        MountListTable.CopyToClipboardColumn => () =>
                        {
                            PaddingY(20);
                            CenterHorizontally();

                            DrawIcon(
                                CopyToClipboardIconConfig.Icon,
                                CopyToClipboardIconConfig.Color
                            );
                        },
                        MountListTable.RemoveColumn => () =>
                        {
                            CenterHorizontally();

                            DrawIcon(
                                RemoveIconConfig.Icon,
                                RemoveIconConfig.Color
                            );
                        },
                        _ => () => ImGui.TextWrapped(columnId),
                    };

                    return (drawColumnName, drawFunction);
                }
            );

        foreach (var (drawColumnName, drawExplanation) in rows)
        {
            yield return new Row(
                new Dictionary<string, DrawColumnCallback>
                {
                    [NameColumn] = drawColumnName,
                    [ExplanationColumn] = drawExplanation
                },
                "mountListExplanation"
            );
        }
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

    private void DrawDefaultCheckboxExplanation()
    {
        ImGui.TextWrapped(
            "Only one list can be set as default. The defaults only purpose is to be used "
            + "when no list is specified in the command:"
        );

        ImGui.TextColoredWrapped(CommandColor, SummonMountCommand.GetCommandWithListName());
    }

    private void DrawOwnedMountsExplanation()
    {
        ImGui.TextWrapped(
            "This table shows which of your currently owned mounts will be used when summoning your mount.\n\n"
            + "While you can add and remove mounts from your list via the Context Menu (Right Click)"
            + " of your Mount Guide, you can also do it from here."
        );
    }

    private void DrawCopyToClipboardExplanation()
    {
        ImGui.TextWrapped(
            "Click on the icon and the macro for calling just this mount list will be copied to your clipboard. "
            + "Simply open \"User Macros\" from the game menu, and paste it into a new one.\n\n"
            + "This macro can then be used to summon only mounts from this list."
        );
    }

    private void DrawRemoveListExplanation()
    {
        ImGui.TextWrapped(
            "Clicking on the trash icon will delete this list. This action cannot be undone."
        );
    }

    private void DrawMountListTypeExplanation()
    {
        if (ImGui.CollapsingHeader("Whitelist", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextWrapped(
                "Any new mounts you acquire, will not automatically be added to this list. "
                + "This list is static until you decide to add more mounts to it."
            );

            EmptyLine();
        }

        if (ImGui.CollapsingHeader("Blacklist", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextWrapped(
                "This list is dynamic and will be updated as you acquire new mounts."
                + "Only those you have blacklisted will never be summoned."
            );
        }
    }
}
