using System;
using System.Linq;
using System.Numerics;
using EnhancedMountRoulette.Addons.Settings.Footer;
using EnhancedMountRoulette.Addons.Settings.MountListItemEditor;
using EnhancedMountRoulette.Addons.Settings.MountListsOverview;
using EnhancedMountRoulette.Configuration;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;

namespace EnhancedMountRoulette.Addons.Settings;

public unsafe class SettingsAddon : NativeAddon
{
    private const float FooterProgressWidth = MountListsOverviewNode.PreferredWidth * 4.0f / 3.0f;
    private const float ColumnDividerWidth = 4.0f;
    private const float ColumnSpacing = 16.0f;
    private const float ContentSpacing = 6.0f;
    private static readonly float FooterHeight = FooterNode.PreferredHeight + ContentSpacing;

    private MountListsOverviewNode? mountListsOverviewNode;
    private MountListItemEditorNode? mountListItemEditorNode;
    private FooterNode? footerNode;

    protected override void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan)
    {
        var contentRoot = new VerticalListNode
        {
            Position = ContentStartPosition,
            Size = ContentSize,
            ItemSpacing = ContentSpacing,
            FitWidth = false,
        };
        contentRoot.AttachNode(this);

        AddMain(contentRoot);
        AddFooter(contentRoot);
        SelectInitialMountList();
    }

    protected override void OnUpdate(AtkUnitBase* addon)
    {
        mountListsOverviewNode?.Update();
        mountListItemEditorNode?.Update();
    }

    protected override void OnShow(AtkUnitBase* addon)
    {
        // Owned/total only changes on acquire or patch (restart). Recount when the window opens.
        footerNode?.Refresh();
    }

    protected override void OnHide(AtkUnitBase* addon)
    {
        // Dropdown popups reattach to the addon root while open; collapse first
        // so Escape → Close does not finalize them with live event links.
        // Also cancel any deferred context-menu open scheduled for the next tick.
        mountListItemEditorNode?.PrepareForHide();
    }

    public override void Dispose()
    {
        // Collapse/cancel before KamiToolKit Close()/finalize tears nodes down.
        mountListItemEditorNode?.PrepareForTeardown();
        mountListsOverviewNode?.DetachCallbacks();
        MountEntryItemNode.OnOpenContextMenu = null;
        base.Dispose();
    }

    private void AddMain(VerticalListNode contentRoot)
    {
        var mainHeight = ContentSize.Y - FooterHeight;
        var mainRow = new HorizontalListNode
        {
            Size = new Vector2(ContentSize.X, mainHeight),
            ItemSpacing = ColumnSpacing,
        };
        contentRoot.AddNode(mainRow);

        AddMountListsOverview(mainRow, mainHeight);
        AddVerticalDivider(mainRow, mainHeight);
        AddMountListItemEditor(mainRow, mainHeight);
    }

    private void AddMountListsOverview(HorizontalListNode mainRow, float height)
    {
        mountListsOverviewNode = new MountListsOverviewNode(height)
        {
            OnListsChanged = RefreshMountLists,
            OnMountListSelected = OnMountListSelected,
        };
        mainRow.AddNode(mountListsOverviewNode);
    }

    private void AddVerticalDivider(HorizontalListNode mainRow, float height)
    {
        // Overlay only — VerticalLineNode.Size bypasses Width/Height overrides and would
        // report ContentSize.Y as layout width if added to the horizontal list.
        // VerticalLineNode is a horizontal line rotated 90° around origin (0,0), so the
        // bar occupies [X - Width, X]. Offset by Width/2 to center it in the column gap.
        var columnDivider = new VerticalLineNode();
        columnDivider.Width = ColumnDividerWidth;
        columnDivider.Height = height;
        columnDivider.Position = new Vector2(
            MountListsOverviewNode.PreferredWidth + ColumnSpacing / 2.0f + ColumnDividerWidth / 2.0f,
            0.0f
        );
        columnDivider.AttachNode(mainRow);
    }

    private void AddMountListItemEditor(HorizontalListNode mainRow, float height)
    {
        mountListItemEditorNode = new MountListItemEditorNode
        {
            Size = new Vector2(
                ContentSize.X - MountListsOverviewNode.PreferredWidth - ColumnSpacing,
                height
            ),
            OnListsChanged = RefreshMountLists,
            GetOwnerAddonId = () => (uint)AddonId,
        };
        mainRow.AddNode(mountListItemEditorNode);
    }

    private void AddFooter(VerticalListNode contentRoot)
    {
        footerNode = new FooterNode(ContentSize.X, FooterProgressWidth);
        contentRoot.AddNode(footerNode);
    }

    private void SelectInitialMountList()
    {
        if (ConfigManager.Instance.OrderedMountList.FirstOrDefault() is not { } first)
        {
            return;
        }

        mountListsOverviewNode?.Select(first);
        mountListItemEditorNode?.Select(first);
    }

    private void OnMountListSelected(MountList mountList)
    {
        mountListItemEditorNode?.Select(mountList);
    }

    private void RefreshMountLists()
    {
        mountListsOverviewNode?.Refresh();

        if (mountListsOverviewNode?.SelectedMountList is { } selected)
        {
            mountListItemEditorNode?.Select(selected);
        }
    }
}
