using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace BetterMountRoulette;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
    
    public HashSet<uint> BlacklistedMountIds { get; set; } = [];

    // The below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
