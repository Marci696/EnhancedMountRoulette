using Dalamud.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BetterMountRoulette;

[Serializable]
public class Configuration : IPluginConfiguration
{
    // todo did i just add it as test, or is needed?
    public int Version { get; set; } = 1;

    public bool IsConfigWindowMovable { get; set; } = true;
    
    public int DefaultMountListIndex { get; set; } = 0;
    
    public List<MountList> MountLists { get; set; } = [];
    
    /*
    private Dictionary<string, MountList> MountListHashTable { get; set; } = new();
    */

    public MountList? GetDefaultMountList()
    {
        var defaultMountList = MountLists.ElementAtOrDefault(this.DefaultMountListIndex);
        if (defaultMountList == null)
        {
            // TODO just hacked, find better way
            defaultMountList = new MountList()
            {
                Name = "Default",
                IncludeNotMentionedMountIds = true,
            };
            MountLists.Add(defaultMountList);
            DefaultMountListIndex = 0;
            Save();
        }

        return defaultMountList;
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
