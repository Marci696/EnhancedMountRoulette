using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Dalamud.Configuration;

namespace BetterMountRoulette.Configuration;

public class Configuration
{
    public ImmutableDictionary<string, MountList> MountLists => _mountLists.ToImmutableDictionary();

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
                    MountIds = ImmutableHashSet.CreateRange(keyValuePair.Value.MountIds)
                }
            );
    }

    public List<MountList> GetMountLists(MountListType type)
    {
        return MountLists.Values.Where(mountList => mountList.Type == type).ToList();
    }

    public MountList? GetDefaultMountList()
    {
        return MountLists.Values.FirstOrDefault((mountList) => mountList.IsDefault);
    }

    public MountList? GetMountList(string listName)
    {
        return MountLists.GetValueOrDefault(listName);
    }

    public void StoreMountList(MountList mountList)
    {
        // Mark all others as not default.
        if (mountList.IsDefault)
        {
            foreach (var currentEntry in MountLists.Where((current) => current.Value.IsDefault).ToList())
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
                        Name = pair.Value.Name, MountIds = pair.Value.MountIds.ToList(),
                        FetchNextType = pair.Value.FetchNextType, Type = pair.Value.Type
                    }
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
            new(StringComparer.CurrentCultureIgnoreCase)
            {
                [DefaultMountListName] = new SerializableMountList
                {
                    Name = DefaultMountListName,
                    IsDefault = true,
                }
            };
    }
}
