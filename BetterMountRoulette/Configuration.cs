using Dalamud.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace BetterMountRoulette;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public const string DefaultMountListName = "Default";
    
    // todo did i just add it as test, or is needed?
    public int Version { get; set; } = 1;

    public bool IsConfigWindowMovable { get; set; } = true;
    
    public Dictionary<string, MountList> MountLists { get; set; } = new([], StringComparer.CurrentCultureIgnoreCase);

    public List<MountList> GetMountLists()
    {
        return MountLists.Values.ToList();
    }

    public List<MountList> GetMountLists(MountListType type)
    {
        return MountLists.Values.Where(mountList => mountList.Type == type).ToList();
    }

    public MountList GetOrCreateDefaultMountList()
    {
        var defaultMountList = GetMountList(DefaultMountListName);
        if (defaultMountList == null)
        {
            // TODO just hacked, find better way
            defaultMountList = new MountList()
            {
                Name = DefaultMountListName,
                Type = MountListType.Blacklist,
            };
            MountLists.Add(DefaultMountListName, defaultMountList);
            Save();
        }

        return defaultMountList;
    }

    public MountList? GetMountList(string listName)
    {
        return MountLists.GetValueOrDefault(listName);
    }

    public void StoreMountList(MountList mountList)
    {
        MountLists.Add(mountList.Name, mountList);
        Save();
    }

    public void RemoveMountList(MountList mountList)
    {
        MountLists.Remove(mountList.Name);
        Save();
    }

    public void CleanMountList(MountList mountList)
    {
        mountList.MountIds.Clear();
        Save();
    }

    public void ClearMountList()
    {
        MountLists.Clear();
        GetOrCreateDefaultMountList();
    }
    

    /*
    public MountList? GetMountList()
    {
        return MountListHashTable.
    }
    */
    
    // The below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
