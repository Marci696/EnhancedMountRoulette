using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using EnhancedMountRoulette.Configuration;
using EnhancedMountRoulette.Windows;
using KamiToolKit.ContextMenu;
using Lumina.Excel.Sheets;
using Lumina.Text;
using Lumina.Text.ReadOnly;

namespace EnhancedMountRoulette;

internal static class MountRouletteMenuItems
{
    /// <summary>
    /// When true, skip injecting into Dalamud's native context menu.
    /// KamiToolKit menus also open AgentContext and would otherwise get duplicated entries.
    /// </summary>
    public static bool SuppressNativeMountMenuInjection { get; set; }

    public static string GetHeaderLabel(MountListType mountListType) => mountListType == MountListType.Whitelist
        ? "---- Roulette WhiteLists: ----"
        : "---- Roulette BlackLists: ----";

    public static MountListMenuItem CreateForMountList(MountList mountList, Mount mount)
    {
        var isCurrentlyConsideredForSummoning = MountManager.GetAvailableMountsFromListForSummoning(mountList)
            .Contains(mount.RowId);

        if (isCurrentlyConsideredForSummoning)
        {
            return new MountListMenuItem(
                Name: $"Ignore in {mountList.Name}",
                Prefix: SeIconChar.Cross,
                PrefixColor: ColorMap.Red,
                ImGuiPrefixColor: DrawHelper.RgbaToImgGuiVector(183, 29, 6, 0.8f),
                IsCurrentlyConsideredForSummoning: true,
                MountList: mountList,
                Mount: mount
            );
        }

        return new MountListMenuItem(
            Name: $"Summon in {mountList.Name}",
            Prefix: SeIconChar.BoxedPlus,
            PrefixColor: ColorMap.Green,
            ImGuiPrefixColor: DrawHelper.RgbaToImgGuiVector(21, 146, 21, 0.8f),
            IsCurrentlyConsideredForSummoning: false,
            MountList: mountList,
            Mount: mount
        );
    }

    public static void ApplyToNativeContextMenu(IMenuOpenedArgs args, Mount mount)
    {
        foreach (var mountListType in Enum.GetValues<MountListType>())
        {
            args.AddMenuItem(
                new MenuItem
                {
                    Name = GetHeaderLabel(mountListType),
                    IsEnabled = false,
                    PrefixChar = 'R',
                }
            );

            foreach (var mountList in ConfigManager.Instance.GetMountLists(mountListType))
            {
                args.AddMenuItem(CreateForMountList(mountList, mount).ToNativeMenuItem());
            }
        }
    }

    public static void DrawImGuiEntries(Mount mount)
    {
        foreach (var mountListType in Enum.GetValues<MountListType>())
        {
            ImGui.Separator();
            ImGui.TextDisabled(GetHeaderLabel(mountListType));

            foreach (var mountList in ConfigManager.Instance.GetMountLists(mountListType))
            {
                CreateForMountList(mountList, mount).DrawImGuiSelectable();
            }
        }
    }

    public static void ApplyToKamiContextMenu(ContextMenu contextMenu, Mount mount)
    {
        foreach (var mountListType in Enum.GetValues<MountListType>())
        {
            contextMenu.AddItem(
                new ContextMenuItem
                {
                    Name = MountListMenuItem.FormatKamiMenuName(
                        SeIconChar.BoxedLetterR,
                        color: null,
                        GetHeaderLabel(mountListType)
                    ),
                    IsEnabled = false,
                    OnClick = static () => { },
                    // KamiToolKit orders ascending; keep Summon/Favorites above these.
                    DisplayPriority = mountListType == MountListType.Whitelist ? 30 : 50,
                }
            );

            foreach (var mountList in ConfigManager.Instance.GetMountLists(mountListType))
            {
                var item = CreateForMountList(mountList, mount);
                contextMenu.AddItem(
                    new ContextMenuItem
                    {
                        Name = item.ToKamiMenuName(),
                        OnClick = item.ToggleMountInList,
                        DisplayPriority = mountListType == MountListType.Whitelist ? 31 : 51,
                    }
                );
            }
        }
    }
}

internal record MountListMenuItem(
    string Name,
    SeIconChar Prefix,
    ColorMap PrefixColor,
    Vector4 ImGuiPrefixColor,
    bool IsCurrentlyConsideredForSummoning,
    MountList MountList,
    Mount Mount
)
{
    public MenuItem ToNativeMenuItem() => new()
    {
        Name = Name,
        IsEnabled = true,
        Prefix = Prefix,
        PrefixColor = (ushort)PrefixColor,
        OnClicked = (_) => ToggleMountInList(),
    };

    public ReadOnlySeString ToKamiMenuName() => FormatKamiMenuName(Prefix, (ushort)PrefixColor, Name);

    public static ReadOnlySeString FormatKamiMenuName(SeIconChar icon, ushort? color, string name)
    {
        var builder = new SeStringBuilder();
        if (color is { } colorType)
        {
            builder.PushColorType(colorType);
        }

        builder.Append(icon.ToIconChar());

        if (color is not null)
        {
            builder.PopColorType();
        }

        return builder.Append(' ').Append(name).ToReadOnlySeString();
    }

    public void DrawImGuiSelectable()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiPrefixColor);
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.Text(GetImGuiIcon().ToIconString());
        }

        ImGui.PopStyleColor();

        ImGui.SameLine();

        if (ImGui.Selectable(Name))
        {
            ToggleMountInList();
        }
    }

    private FontAwesomeIcon GetImGuiIcon() =>
        Prefix == SeIconChar.Cross ? FontAwesomeIcon.Times : FontAwesomeIcon.PlusSquare;

    public void ToggleMountInList()
    {
        if (!IsCurrentlyConsideredForSummoning)
        {
            ConfigManager.Instance.ConsiderMountForSummoning(MountList, Mount);
        }
        else
        {
            ConfigManager.Instance.OverlookMountFromSummoning(MountList, Mount);
        }
    }
}
