using System;
using System.Collections.Generic;
using Dalamud.Game.Gui.ContextMenu;

namespace BetterMountRoulette;

public enum MountListType
{
    Whitelist,
    Blacklist
}

[Serializable]
public class MountList
{
    public string Name { get; set; } = "";
    
    public MountListType Type { get; set; } = MountListType.Blacklist;
    
    public HashSet<uint> MountIds { get; set; } = [];
}
