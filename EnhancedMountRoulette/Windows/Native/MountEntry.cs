using System;
using Lumina.Excel.Sheets;

namespace EnhancedMountRoulette.Windows.Native;

public sealed record MountEntry(
    Mount Mount,
    bool IsInSummonList,
    Action<MountEntry> ToggleMembership
);
