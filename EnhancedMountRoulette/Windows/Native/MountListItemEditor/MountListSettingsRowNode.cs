using System;
using System.Linq;
using System.Numerics;
using KamiToolKit.Nodes;
using EnhancedMountRoulette.Configuration;
using EnhancedMountRoulette.Windows.Native;

namespace EnhancedMountRoulette.Windows.Native.MountListItemEditor;

/// <summary>
/// Top editor strip: list name, type, fetch mode, delete, and copy-macro.
/// </summary>
public class MountListSettingsRowNode : HorizontalListNode
{
    public const float PreferredHeight = 28.0f;

    public Action<string>? OnNameCommitted { get; set; }

    public Action<MountListType>? OnTypeSelected { get; set; }

    public Action<FetchNextType>? OnFetchTypeSelected { get; set; }

    public Action? OnDeleteClicked { get; set; }

    public Action? OnCopyMacroClicked { get; set; }

    private readonly TextInputNode nameInput;
    private readonly StringDropDownNode typeDropDown;
    private readonly StringDropDownNode fetchTypeDropDown;

    public MountListSettingsRowNode()
    {
        Size = new Vector2(600.0f, PreferredHeight);
        ItemSpacing = 6.0f;

        nameInput = AddNameInput();
        typeDropDown = AddTypeDropDown();
        fetchTypeDropDown = AddFetchTypeDropDown();
        AddDeleteButton();
        AddCopyMacroButton();
    }

    public void Load(MountList mountList)
    {
        nameInput.String = mountList.Name;
        typeDropDown.SelectedOption = mountList.Type.ToString();
        fetchTypeDropDown.SelectedOption = mountList.FetchNextType.ToString();
    }

    public void SetName(string name)
    {
        nameInput.String = name;
    }

    public void CollapseDropDowns()
    {
        typeDropDown.Collapse(playSoundEffect: false);
        fetchTypeDropDown.Collapse(playSoundEffect: false);
    }

    private TextInputNode AddNameInput()
    {
        var input = new TextInputNode
        {
            Size = new Vector2(160.0f, PreferredHeight),
            PlaceholderString = "List name",
            MaxCharacters = 50,
            OnInputComplete = value => OnNameCommitted?.Invoke(value.ToString()),
        };
        input.OnFocusLost = () => OnNameCommitted?.Invoke(input.String.ToString());
        AddNode(input);
        return input;
    }

    private StringDropDownNode AddTypeDropDown()
    {
        var dropDown = new StringDropDownNode
        {
            Size = new Vector2(110.0f, PreferredHeight),
            Options = Enum.GetNames<MountListType>().ToList(),
            OnOptionSelected = option =>
            {
                if (Enum.TryParse<MountListType>(option, out var type))
                {
                    OnTypeSelected?.Invoke(type);
                }
            },
        };
        AddNode(dropDown);
        return dropDown;
    }

    private StringDropDownNode AddFetchTypeDropDown()
    {
        var dropDown = new StringDropDownNode
        {
            Size = new Vector2(150.0f, PreferredHeight),
            Options = Enum.GetNames<FetchNextType>().ToList(),
            OnOptionSelected = option =>
            {
                if (Enum.TryParse<FetchNextType>(option, out var fetchType))
                {
                    OnFetchTypeSelected?.Invoke(fetchType);
                }
            },
        };
        AddNode(dropDown);
        return dropDown;
    }

    private void AddDeleteButton()
    {
        var button = new TextButtonNode
        {
            Size = new Vector2(70.0f, PreferredHeight),
            String = "Delete",
            OnClick = () => OnDeleteClicked?.Invoke(),
        };
        NativeButtonStyles.StyleAsRemove(button);
        AddNode(button);
    }

    private void AddCopyMacroButton()
    {
        var button = new TextButtonNode
        {
            Size = new Vector2(90.0f, PreferredHeight),
            String = "Copy Macro",
            OnClick = () => OnCopyMacroClicked?.Invoke(),
        };
        AddNode(button);
    }
}
