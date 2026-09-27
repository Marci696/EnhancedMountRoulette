using System;
using Lumina.Excel.Sheets;

namespace EnhancedMountRoulette.Windows.Native;

public sealed record MountEntry(
    Mount Mount,
    bool IsInSummonList,
    int SeatCount,
    string OwnedDisplay,
    float? OwnedPercent,
    string? Patch,
    Action<MountEntry> ToggleMembership,
    int RowIndex = 0
);
