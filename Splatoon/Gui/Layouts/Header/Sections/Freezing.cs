using ECommons.LanguageHelpers;

namespace Splatoon.ConfigGui.CGuiLayouts.LayoutDrawHeader.Subcommands;

internal static class Freezing
{
    internal static void DrawFreezing(this Layout layout)
    {
        if (layout.Freezing)
        {
            ImGuiEx.Text("Freeze for:".Loc());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragFloat("##freezeTime", ref layout.FreezeFor, 0.1f, 0, 99999, $"{layout.FreezeFor:F1}");
            ImGuiEx.Text("Refreeze interval:".Loc());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragFloat("##freezeInt", ref layout.IntervalBetweenFreezes, 0.1f, 0, 99999, $"{layout.IntervalBetweenFreezes:F1}");
            ImGuiEx.Text("Display delay:".Loc());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragFloat("##freezeDD", ref layout.FreezeDisplayDelay, 0.1f, 0, 99999, $"{layout.FreezeDisplayDelay:F1}");
            ImGuiEx.Text("Reset on:".Loc());
            ImGui.SameLine();
            ImGui.Checkbox("Combat end".Loc(), ref layout.FreezeResetCombat);
            ImGui.SameLine();
            ImGui.Checkbox("Zone change".Loc(), ref layout.FreezeResetTerr);
        }
    }
}
