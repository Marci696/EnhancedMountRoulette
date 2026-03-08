using Dalamud.Bindings.ImGui;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace EnhancedMountRoulette;

public static class Chat
{
    private const string ChatTag = "EnhancedMountRoulette";

    private const ushort ChatTagColor = (ushort)ColorMap.Lila;

    public static void Write(string message, bool isError = false)
    {
        if (isError)
        {
            Plugin.ChatGui.PrintError(message, ChatTag, ChatTagColor);
        }
        else
        {
            Plugin.ChatGui.Print(message, ChatTag, ChatTagColor);
        }
    }
}
