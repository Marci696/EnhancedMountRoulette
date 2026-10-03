using System;
using Lumina.Excel.Sheets;

namespace EnhancedMountRoulette.Addons.Settings.MountListItemEditor;

public sealed record MountEntry(
    Mount Mount,
    bool IsOwned,
    bool IsInSummonList,
    int SeatCount,
    string OwnedDisplay,
    float? OwnedPercent,
    string? Patch,
    Action<MountEntry> ToggleMembership,
    int RowIndex = 0
);
