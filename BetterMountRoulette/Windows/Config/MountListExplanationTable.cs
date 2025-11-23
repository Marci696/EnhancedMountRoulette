using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BetterMountRoulette.Commands;
using BetterMountRoulette.Configuration;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
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
                        // todo rename fetchType to SummonType ?
                        MountListTable.FetchTypeColumn => DrawSummonTypeExplanation,
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
        PaddingY(2);

        if (ImGui.CollapsingHeader(
                Enum.GetName(MountListType.Whitelist)
            ))
        {
            ImGui.TextWrapped(
                "Any new mounts you acquire, will not automatically be added to this list. "
                + "This list is static until you decide to add more mounts to it."
            );

            EmptyLine();
        }

        if (ImGui.CollapsingHeader(
                Enum.GetName(MountListType.Blacklist)
            ))
        {
            ImGui.TextWrapped(
                "This list is dynamic and will be updated as you acquire new mounts."
                + "Only those you have blacklisted will never be summoned."
            );
        }

        PaddingY(2);
    }

    private void DrawSummonTypeExplanation()
    {
        PaddingY(2);

        if (ImGui.CollapsingHeader(
                Enum.GetName(FetchNextType.Random)
            ))
        {
            ImGui.TextWrapped(
                "Each summon action will draw a random mount from the list.\n\n"
                + "This is the same as the games Mount Roulette that you find in the Mount Guide. "
                + "The main disadvantage of it is, that you can get the same mount twice in a row.\n\n"
                + "Personally, I would not recommend this option."
            );

            EmptyLine();
        }

        if (ImGui.CollapsingHeader(Enum.GetName(FetchNextType.Shuffle)))
        {
            // todo add note for that the list is reset on changes
            ImGui.TextWrapped(
                "All your considered mounts for summoning are put into a list and shuffled.\n"
                + "Each summon action will remove one mount from this list.\n"
                + "Once this internal shuffled list is empty, "
                + "it will create a new shuffled list and continue the loop.\n\n"
                + "The main advantage of this option is, that you will never get the same mount again until you "
                + "have gone through all mounts from your list.\n\n"
                + "This is my personal recommendation."
            );

            EmptyLine();
        }

        if (ImGui.CollapsingHeader(Enum.GetName(FetchNextType.Sequential)))
        {
            ImGui.TextWrapped(
                "It will summon each mount by the order of which Square Enix added them to the game.\n\n"
                + "Pros: You will never get the same mount twice in a row.\n"
                + "Cons: It will be in the same order every time."
            );
        }

        PaddingY(2);
    }
}
