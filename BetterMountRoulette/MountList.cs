using System;
using System.Collections.Generic;

namespace BetterMountRoulette;

[Serializable]
public class MountList
{
    public string Name { get; set; } = "";
    
    public bool IncludeNotMentionedMountIds { get; set; } = true;

    // TODO do this with union type, for either whitelist or blacklist mode?
    public HashSet<uint> WhitelistedIds { get; set; } = [];
    
    public HashSet<uint> BlacklistedIds { get; set; } = [];
}
