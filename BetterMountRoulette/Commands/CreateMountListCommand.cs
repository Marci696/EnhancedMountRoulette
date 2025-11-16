using BetterMountRoulette.Configuration;
using Dalamud.Game.Command;

namespace BetterMountRoulette.Commands;

public class CreateMountListCommand(Configuration.Configuration configuration, MountListType mountListType)
    : BaseCommand(configuration)
{
    public MountListType MountListType { get; } = mountListType;

    public override string Command => "/bmr-add-" + MountListType.AsString();

    public override CommandInfo CommandInfo
    {
        get
        {
            var typeName = MountListType.AsString();

            return new CommandInfo(Handler)
            {
                HelpMessage =
                    $"Add a new mount {typeName}. Specify a name by calling it like /bmr-add-{typeName} myName"
            };
        }
    }

    public void Handler(string _, string arguments)
    {
        var newMountListName = arguments.Trim();
        if (newMountListName.Length == 0)
        {
            Chat.Write(
                "You need to specify a name for your new mount list. Add it like this: /bmr-add-blacklist myName",
                isError: true
            );

            return;
        }

        CreateNewMountList(newMountListName);
    }

    protected void CreateNewMountList(string mountListName)
    {
        if (Configuration.GetMountList(mountListName) != null)
        {
            Chat.Write(
                $"Mount list with the name \"{mountListName}\" already exists!",
                isError: true
            );
        }

        var newMountList = new MountList()
        {
            Name = mountListName,
            Type = MountListType,
        };

        Configuration.StoreMountList(newMountList);

        Chat.Write($"Your new list \"{mountListName}\" was created.");
    }
}
