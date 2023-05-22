using Dalamud.Interface.Colors;
using ECommons.LanguageHelpers;

namespace Splatoon.Gui;

internal static class TabContributors
{
    internal static void Draw()
    {
        ImGuiEx.TextWrapped("Thanks to all the people who have contributed to Splatoon! Here is the list of ones who contributed and wanted to be mentioned. Can't find yourself here/website link is wrong? Please open an issue on GitHub or Discord and I will add you. You can specify your name as well as your website/social network account, if you wish.".Loc());
        ImGuiEx.Text(ImGuiColors.DalamudRed, "Warning, this list is under construction. Many people are missing here.");
        ImGui.Separator();

        ImGuiEx.Text("玖祁 - Chinese translation");
        ImGuiEx.Text("jojo - presets and presets translation");
        ImGuiEx.Text("FrostEffects - presets"); Link("Carrd", "https://frostffxiv.carrd.co/");
        ImGuiEx.Text("莫灵喵 - presets");
        ImGuiEx.Text("LAMMY - presets"); Link("Github", "https://github.com/LAMMY-33");
        ImGuiEx.Text($"Ry - colorblind focus, battle data");
        ImGuiEx.Text($"Errer - presets"); Link("Github", "https://github.com/Errerer/");
        ImGuiEx.Text($"Ouyk - presets");
        ImGuiEx.Text($"Exnter - presets"); Link("Github", "https://github.com/Exnter/");
    }

    static void Link(string preview, string Url)
    {
        ImGui.SameLine();
        ImGuiEx.Text(ImGuiColors.DalamudGrey, preview ?? Url);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                ShellStart(Url);
            }
        }
    }
}
