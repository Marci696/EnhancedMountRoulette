namespace EnhancedMountRoulette.Windows.Native;

/// <summary>
/// Mount list editor filter state. One instance lives on the editor and is mutated in place.
/// </summary>
internal sealed class MountListFilters
{
    public string SearchText { get; set; } = "";

    public MountSelectionFilter Selection { get; set; } = MountSelectionFilter.All;

    public MountSeatFilter Seats { get; set; } = MountSeatFilter.All;

    public bool OwnedOnly { get; set; } = true;

    public void Reset()
    {
        SearchText = "";
        Selection = MountSelectionFilter.All;
        Seats = MountSeatFilter.All;
        OwnedOnly = true;
    }
}
