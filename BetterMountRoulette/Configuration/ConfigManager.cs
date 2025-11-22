using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Dalamud.Configuration;
using Lumina.Excel.Sheets;

namespace BetterMountRoulette.Configuration;

public sealed class ConfigManager
{
    private static readonly IEqualityComparer<string> Comparer = StringComparer.OrdinalIgnoreCase;

    private static ConfigManager? _instance;

    public static ConfigManager Instance
    {
        get
        {
            _instance ??= new();

            return _instance;
        }
    }

    public ImmutableDictionary<string, MountList> MountLists => _mountLists.ToImmutableDictionary(Comparer);

    public List<MountList> OrderedMountList => _mountLists.Values.OrderBy(mountList => mountList.Id).ToList();

    private Dictionary<string, MountList> _mountLists;

    private ConfigManager()
    {
        var data = Plugin.PluginInterface.GetPluginConfig() as SerializableConfiguration
            ?? new SerializableConfiguration();

        _mountLists =
            data.MountLists.ToDictionary(
                (keyValuePair) => keyValuePair.Value.Name,
                (keyValuePair) => new MountList()
                {
                    Name = keyValuePair.Value.Name,
                    FetchNextType = keyValuePair.Value.FetchNextType,
                    Type = keyValuePair.Value.Type,
                    MountIds = ImmutableHashSet.CreateRange(keyValuePair.Value.MountIds),
                    IsDefault = keyValuePair.Value.IsDefault
                },
                comparer: Comparer
            );
    }

    public List<MountList> GetMountLists(MountListType type)
    {
        return _mountLists.Values.Where(mountList => mountList.Type == type).ToList();
    }

    public MountList? GetDefaultMountList()
    {
        return _mountLists.Values.FirstOrDefault((mountList) => mountList.IsDefault);
    }

    public MountList? GetMountList(string listName)
    {
        return _mountLists.GetValueOrDefault(listName);
    }

    public void StoreMountList(MountList mountList)
    {
        // Mark all others as not default.
        if (mountList.IsDefault)
        {
            foreach (var currentEntry in _mountLists.Where((current) => current.Value.IsDefault).ToList())
            {
                _mountLists[currentEntry.Key] = new MountList(currentEntry.Value) { IsDefault = false };
            }
        }

        _mountLists[mountList.Name] = mountList;

        Save();
    }

    public void RemoveMountList(MountList mountList)
    {
        _mountLists.Remove(mountList.Name);
        Save();
    }

    public void CleanMountList(MountList mountList)
    {
        StoreMountList(
            new MountList(mountList)
            {
                // With empty MountIds
                MountIds = [],
            }
        );
    }

    public void AddMountToList(MountList mountList, Mount mount)
    {
        // todo should keep shuffle list and just modify based on new id?
        StoreMountList(new MountList(mountList) { MountIds = mountList.MountIds.Add(mount.RowId) });

        Chat.Write($"Added #{mount.RowId} {mount.Singular.ExtractText()} to list {mountList.Name}");
    }

    // todo should keep shuffle list and just modify based on new id?
    public void RemoveMountFromList(MountList mountList, Mount mount)
    {
        StoreMountList(new MountList(mountList) { MountIds = mountList.MountIds.Remove(mount.RowId) });

        Chat.Write($"Added #{mount.RowId} {mount.Singular.ExtractText()} to list {mountList.Name}");
    }

    public void ConsiderAllMountsForSummoning(MountList mountList, IEnumerable<uint> mountIds)
    {
        StoreMountList(
            new MountList(mountList)
            {
                MountIds = mountList.Type == MountListType.Whitelist
                    ? mountIds.ToImmutableHashSet()
                    : ImmutableHashSet<uint>.Empty
            }
        );
    }

    public void OverlookAllMountsForSummoning(MountList mountList, IEnumerable<uint> mountIds)
    {
        StoreMountList(
            new MountList(mountList)
            {
                MountIds = mountList.Type == MountListType.Whitelist
                    ? ImmutableHashSet<uint>.Empty
                    : mountIds.ToImmutableHashSet()
            }
        );
    }

    public void ConsiderMountForSummoning(MountList mountList, Mount mount)
    {
        var newMountIds = mountList.Type == MountListType.Whitelist
            ? mountList.MountIds.Add(mount.RowId)
            : mountList.MountIds.Remove(mount.RowId);

        StoreMountList(new MountList(mountList) { MountIds = newMountIds });
    }

    public void OverlookMountFromSummoning(MountList mountList, Mount mount)
    {
        var newMountIds = mountList.Type == MountListType.Whitelist
            ? mountList.MountIds.Remove(mount.RowId)
            : mountList.MountIds.Add(mount.RowId);

        StoreMountList(new MountList(mountList) { MountIds = newMountIds });
    }

    public void ClearMountLists()
    {
        _mountLists.Clear();
        Save();
    }

    public void RenameMountList(MountList mountList, string newName)
    {
        _mountLists.Remove(mountList.Name);

        StoreMountList(new MountList(mountList) { Name = newName });
    }

    public void ChangeMountListType(MountList mountList, MountListType newMountListType)
    {
        if (mountList.Type == newMountListType)
        {
            return;
        }

        var ownedMountIds = MountManager.GetOwnedMountIds();

        StoreMountList(
            new MountList(mountList)
            {
                Type = newMountListType,
                MountIds =
                    MountList.GetMoundIdsForMountList(
                            newMountListType,
                            ownedMountIds: MountManager.GetOwnedMountIds(),
                            mountIdsConsideredForSummoning: mountList.GetAvailableMountsForSummoning(ownedMountIds)
                                .ToHashSet()
                        )
                        .ToImmutableHashSet()
            }
        );
    }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(
            new SerializableConfiguration()
            {
                MountLists = _mountLists.ToDictionary(
                    (pair => pair.Value.Name),
                    pair => new SerializableMountList()
                    {
                        Name = pair.Value.Name,
                        MountIds = pair.Value.MountIds.ToList(),
                        FetchNextType = pair.Value.FetchNextType,
                        Type = pair.Value.Type,
                        IsDefault = pair.Value.IsDefault,
                    },
                    comparer: Comparer
                )
            }
        );
    }

    [Serializable]
    private class SerializableMountList
    {
        public string Name { get; set; } = "";

        public List<uint> MountIds { get; set; } = [];

        public MountListType Type { get; set; } = MountListType.Blacklist;

        public FetchNextType FetchNextType { get; set; } = FetchNextType.Random;

        public bool IsDefault { get; set; } = false;
    }

    [Serializable]
    private class SerializableConfiguration : IPluginConfiguration
    {
        public const string DefaultMountListName = "Default";

        public int Version { get; set; } = 2;

        // Default is only assigned when nothing else is found.
        public Dictionary<string, SerializableMountList> MountLists { get; set; } =
            new(Comparer)
            {
                [DefaultMountListName] = new SerializableMountList
                {
                    Name = DefaultMountListName,
                    IsDefault = true,
                }
            };
    }

    public string FindNewMountListName()
    {
        var standardNewName = "New";

        var newName = standardNewName;
        var increment = 0;

        while (_mountLists.ContainsKey(newName))
        {
            newName = $"{standardNewName} {++increment}";
        }

        return newName;
    }
}
