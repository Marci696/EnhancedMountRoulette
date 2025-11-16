using System;
using System.Collections.Generic;

namespace BetterMountRoulette.Configuration;

[Serializable]
public class MountList
{
    public string Name { get; set; } = "";

    public MountListType Type { get; set; } = MountListType.Blacklist;

    public HashSet<uint> MountIds { get; set; } = [];
}

public enum MountListType
{
    Whitelist,
    Blacklist
}

public static class MountListTypeExtensions
{
    public static string AsString(this MountListType mountListType) => mountListType switch
    {
        MountListType.Whitelist => "whitelist",
        MountListType.Blacklist => "blacklist",
        _ => "unknown"
    };
}
