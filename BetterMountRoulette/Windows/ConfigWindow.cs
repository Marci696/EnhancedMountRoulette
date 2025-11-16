using System;
using System.Collections.Generic;
using System.Numerics;
using BetterMountRoulette.Configuration;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using static BetterMountRoulette.Windows.DrawHelper;

namespace BetterMountRoulette.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration.Configuration configuration;

    private string WhitelistCreateInputString = "";

    private string BlacklistCreateInputString = "";

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Configuration.Configuration configuration) : base(
        "Configuration Window"
    )
    {
        //  Flags |= ImGuiWindowFlags.AlwaysAutoResize;

        //    Size = new Vector2(800, 800);
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
            ImGui.SameLine();
            ImGui.BeginChild("mountListType_" + mountListType, new Vector2(500, 0), border: true);
            
            ImGui.PushID("mountListType_" + mountListType);
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

            PaddingY(10);

            ImGui.Separator();

            RenderAddNewListSection(mountListType);

            ImGui.PopID();
            
            ImGui.EndChild();
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

    private void RenderAddNewListSection(MountListType mountListType)
    {
        ref var inputString = ref (mountListType == MountListType.Whitelist
            ? ref WhitelistCreateInputString
            : ref BlacklistCreateInputString);


        Text("Name of new entry:");

        ImGui.SameLine();

        ImGui.InputText(
            "",
            ref inputString,
            255
        );

        if (inputString.Length == 0)
        {
            Text("Name can not be empty.");
        }
        else if (configuration.MountLists.ContainsKey(inputString))
        {
            // todo color
            Text("Mount list with this name already exists.");
        }
        else
        {
            if (ImGui.Button("Add"))
            {
                configuration.StoreMountList(new MountList() { Name = inputString, Type = mountListType });
                inputString = "";
            }
        }
    }

    private void RenderMountList(MountList mountList)
    {
        // Needed so the inputs with the same label such as the button work for each entry.
        ImGui.PushID(mountList.GetHashCode());

        var mountName = mountList.Name;

        Text("Name:");

        // Goes into the if block when something changed.
        if (ImGui.InputText("", ref mountName, 255))
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

        PaddingY(10);

        RenderAvailableMountsSection(mountList);

        ImGui.PopID();
    }

    private void RenderMountIcon(Mount mount)
    {
        if (Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup() { IconId = mount.Icon }).GetWrapOrDefault() is
            not { } texture)
        {
            return;
        }

        var scale = ImGui.GetIO().FontGlobalScale;

        ImGui.Image(texture.Handle, new Vector2(20, 20) * new Vector2(scale, scale));
    }

    private void RenderAvailableMountsSection(MountList mountList)
    {
        // TODO find way to cache between each draw
        var availableMountsForList = MountManager.GetAvailableMountsForList(mountList);

        if (ImGui.CollapsingHeader($"Mounts currently in pool to be summoned ({availableMountsForList.Count})"))
        {
            foreach (var mountId in availableMountsForList)
            {
                if (MountManager.GetMount(mountId) is not { } mount)
                {
                    continue;
                }

                MountManager.GetAvailableMountsForList(mountList);

                PaddingX(10);


                ImGui.Bullet();

                RenderMountIcon(mount);

                ImGui.SameLine();

                Text(mount.Singular.ExtractText());
            }
        }
    }
}
