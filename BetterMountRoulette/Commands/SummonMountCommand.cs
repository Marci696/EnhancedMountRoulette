using System;
using Dalamud.Game.Command;

namespace BetterMountRoulette.Commands;

public class SummonMountCommand(Configuration.Configuration configuration) : BaseCommand(configuration)
{
    public override string Command => "/bmr";

    public override CommandInfo CommandInfo => new(Handler)
    {
        HelpMessage =
            "Calls a random mount from a list. /bmr will use the default use. To use mount from your custom list use /bmr listName"
    };

    public void Handler(string _, string arguments)
    {
        var listName = arguments.Trim();

        var mountList = listName.Length > 0
                            ? Configuration.GetMountList(listName)
                            : Configuration.GetOrCreateDefaultMountList();

        if (mountList == null)
        {
            Chat.Write($"No mount list found for the name \"{listName}\"", true);

            return;
        }

        var mountIdsForShuffle = MountManager.GetAvailableMountsForList(mountList);

        if (mountIdsForShuffle.Count == 0)
        {
            Chat.Write("No relevant mounts found for the list.", isError: true);

            return;
        }

        var randomNumber = Random.Shared.Next(mountIdsForShuffle.Count);
        var mountIdToMount = mountIdsForShuffle[randomNumber];

        if (MountManager.GetMount(mountIdToMount) is not { } mount)
        {
            Chat.Write("Unexpected error occured: Mount not found by id", isError: true);
            return;
        }

        Plugin.Log.Debug($"Trying to mount {mount.RowId} {mount.Singular.ExtractText()}");

        MountManager.SummonMount(mount);
    }
}
