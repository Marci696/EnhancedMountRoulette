using Dalamud.Game.Command;
using EnhancedMountRoulette.Configuration;

namespace EnhancedMountRoulette.Commands;

internal class CreateMountListCommand(MountListType mountListType) : ICommand
{
    public string Command => "/bmr-add-" + mountListType.AsString();

    public CommandInfo CommandInfo
    {
        get
        {
            var typeName = mountListType.AsString();

            return new CommandInfo(Handler)
            {
                HelpMessage =
                    $"Add a new mount {typeName}. Specify a name by calling it like /bmr-add-{typeName} myName"
            };
        }
    }

    private void Handler(string _, string arguments)
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
        if (ConfigManager.Instance.GetMountList(mountListName) != null)
        {
            Chat.Write(
                $"Mount list with the name \"{mountListName}\" already exists!",
                isError: true
            );
        }

        var newMountList = new MountList()
        {
            Name = mountListName,
            Type = mountListType,
        };

        ConfigManager.Instance.StoreMountList(newMountList);

        Chat.Write($"Your new list \"{mountListName}\" was created.");
    }
}
