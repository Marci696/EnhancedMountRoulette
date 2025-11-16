using System;
using System.Numerics;
using BetterMountRoulette.Configuration;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

namespace BetterMountRoulette.Windows;

enum TextScale
{
    H1,
    H2,
    H3,
    H4,
    Normal,
}

internal static class TextScaleExtensions
{
    public static float ToFloat(this TextScale textScale) => textScale switch
    {
        TextScale.H1 => 2f,
        TextScale.H2 => 1.8f,
        TextScale.H3 => 1.6f,
        TextScale.H4 => 1.4f,
        _ => 1f
    };
}

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration.Configuration configuration;

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Configuration.Configuration configuration) : base(
        "Configuration Window"
    )
    {
        //  Flags |= ImGuiWindowFlags.AlwaysAutoResize;

        Size = new Vector2(400, 400);
        // Decides that size is used while opening, but is not static
        SizeCondition = ImGuiCond.Appearing;

        this.configuration = configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        /*// Flags must be added or removed before Draw() is being called, or they won't apply
        if (configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }*/
    }

    public override void Draw()
    {
        var scale = ImGui.GetIO().FontGlobalScale;

        ImGui.SetWindowFontScale(scale);


        foreach (var mountListType in Enum.GetValues<MountListType>())
        {
            PaddingY(10);

            Text(mountListType.AsString().FirstCharToUpper(), TextScale.H2);

            PaddingY(10);

            uint mountListCounter = 0;
            foreach (var mountList in configuration.GetMountLists(mountListType))
            {
                // Add a separator between the row above.
                if (mountListCounter++ > 0)
                {
                    PaddingY(5);
                    ImGui.Separator();
                    PaddingY(5);
                }

                RenderMountList(mountList);
            }
        }

        // Can't ref a property, so use a local copy
        /*var configValue = configuration.SomePropertyToBeSavedAndWithADefault;
        if (ImGui.Checkbox("Random Config Bool", ref configValue))
        {
            // Can save immediately on change if you don't want to provide a "Save and Close" button
            configuration.Save();
        }*/

        /*var movable = configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable Config Window", ref movable))
        {
            configuration.IsConfigWindowMovable = movable;
            configuration.Save();
        }*/
    }

    private void RenderMountList(MountList mountList)
    {
        // Needed so the inputs with the same label such as the button work for each entry.
        ImGui.PushID(mountList.GetHashCode());

        var mountName = mountList.Name;

        // Goes into the if block when something changed.
        if (ImGui.InputText("Name", ref mountName, 255))
        {
            if (mountName.Length == 0)
            {
                // TODO change color
                Text("Name can not be empty.");
            }
            else if (configuration.MountLists.ContainsKey(mountName))
            {
                // TODO change color
                Text("Mount list with this name already exists.");
            }
            else
            {
                configuration.RenameMountList(mountList, mountName);
            }
        }

        ImGui.SameLine();
        
        if (ImGui.Button("Remove"))
        {
            configuration.RemoveMountList(mountList);
        }

        ImGui.PopID();
    }

    private void Text(string text, TextScale textScale = TextScale.Normal)
    {
        var scale = ImGui.GetIO().FontGlobalScale;

        ImGui.SetWindowFontScale(scale * textScale.ToFloat());

        ImGui.Text(text);

        ImGui.SetWindowFontScale(scale);
    }

    private void PaddingY(float padding)
    {
        var scale = ImGui.GetIO().FontGlobalScale;

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * scale));
    }
}
