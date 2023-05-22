using ECommons.LanguageHelpers;

namespace Splatoon.ConfigGui.CGuiLayouts.LayoutDrawHeader.Subcommands;

internal static class DistanceLimit
{
    internal static void DrawDistanceLimit(this Layout layout)
    {
        if (layout.UseDistanceLimit)
        {
            ImGui.SetNextItemWidth(150f);
            ImGui.Combo("##dlimittype", ref layout.DistanceLimitType, new string[] { "Distance to current target".Loc(), "Distance to element".Loc() }, 2);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragFloat("##dlimit1", ref layout.MinDistance, 0.1f);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Including this value".Loc());
            ImGui.SameLine();
            ImGuiEx.Text("-");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragFloat("##dlimit2", ref layout.MaxDistance, 0.1f);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Excluding this value".Loc());
            if (layout.DistanceLimitType == 0)
            {
                ImGuiEx.TextV("Hitbox:".Loc());
                ImGui.SameLine();
                ImGui.Checkbox("+my##", ref layout.DistanceLimitMyHitbox);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Add my hitbox value to distance calculation".Loc());
                ImGui.SameLine();
                ImGui.Checkbox("+target##", ref layout.DistanceLimitTargetHitbox);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Add target's hitbox value to distance calculation".Loc());
            }
        }
    }
}
