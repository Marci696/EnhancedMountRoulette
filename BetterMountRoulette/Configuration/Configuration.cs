using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Dalamud.Configuration;
using Lumina.Excel.Sheets;

namespace BetterMountRoulette.Configuration;

public class Configuration
{
    private static readonly IEqualityComparer<string> Comparer = StringComparer.OrdinalIgnoreCase;
    
    public ImmutableDictionary<string, MountList> MountLists =>
        _mountLists.ToImmutableDictionary(Comparer);

    private Dictionary<string, MountList> _mountLists;

    public Configuration()
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

        // Store new list as default.
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

    public void SetMountListAsDefault(MountList mountList)
    {
        StoreMountList(new MountList(mountList) { IsDefault = true });
    }

    public void ClearMountList()
    {
        _mountLists.Clear();
        Save();
    }

    public void RenameMountList(MountList mountList, string newName)
    {
        _mountLists.Remove(mountList.Name);

        StoreMountList(new MountList(mountList) { Name = newName });
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
}
