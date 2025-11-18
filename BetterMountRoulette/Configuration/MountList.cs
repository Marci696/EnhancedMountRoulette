using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace BetterMountRoulette.Configuration;

public class MountList
{
    private static int MountListIdCounter = 0;

    public string Name { get; init; } = "";

    public MountListType Type { get; set; } = MountListType.Blacklist;

    public ImmutableHashSet<uint> MountIds
    {
        get => _mountIds.ToImmutableHashSet();
        init => _mountIds = [..value];
    }

    public FetchNextType FetchNextType { get; init; } = FetchNextType.Random;

    public bool IsDefault { get; init; } = false;

    /// <summary>
    /// Identifies the mount list, even when the name is changed.
    /// </summary>
    ///
    /// <remarks>
    /// Helpful when rendering in ImGui lists .e.g. that need an identifier for inputs.
    /// </remarks>
    public int Id { get; init; } = MountListIdCounter++;
    
    private HashSet<uint> _mountIds = [];

    private Queue<uint> queuedMountIds = [];

    public MountList() { }

    public MountList(MountList copyFrom)
    {
        Name = copyFrom.Name;
        MountIds = copyFrom.MountIds;
        FetchNextType = copyFrom.FetchNextType;
        Type = copyFrom.Type;
        IsDefault = copyFrom.IsDefault;
        Id = copyFrom.Id;
    }

    public List<uint> GetAvailableMountsForList(HashSet<uint> ownedMountIds)
    {
        return GetAvailableMountsForList(ownedMountIds.ToList());
    }

    public List<uint> GetAvailableMountsForList(List<uint> ownedMountIds)
    {
        var ids = (Type == MountListType.Whitelist
            ? ownedMountIds.Intersect(MountIds)
            : ownedMountIds.Except(MountIds)).ToList();

        return ids;
    }

    public uint? GetNextMountIdToSummon(HashSet<uint> ownedMountIds)
    {
        return GetNextMountIdToSummon(ownedMountIds.ToList());
    }

    public uint? GetNextMountIdToSummon(List<uint> ownedMountIds)
    {
        if (ownedMountIds.Count == 0)
        {
            return null;
        }

        var availableMountsForList = GetAvailableMountsForList(ownedMountIds);

        if (availableMountsForList.Count == 0)
        {
            return null;
        }

        if (FetchNextType == FetchNextType.Random)
        {
            var randomNumber = Random.Shared.Next(availableMountsForList.Count);

            return availableMountsForList[randomNumber];
        }

        if (queuedMountIds.Count == 0)
        {
            if (FetchNextType == FetchNextType.Shuffle)
            {
                var mountIdSpan = CollectionsMarshal.AsSpan(availableMountsForList);
                Random.Shared.Shuffle(mountIdSpan);

                queuedMountIds = new Queue<uint>([..mountIdSpan]);
            }
            else
            {
                queuedMountIds = new Queue<uint>(availableMountsForList);
            }
        }

        return queuedMountIds.Dequeue();
    }
}

public enum MountListType
{
    Whitelist,
    Blacklist
}

public enum FetchNextType
{
    Random,
    Shuffle,
    Sequential
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
