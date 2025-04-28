using Dalamud.Interface.Components;
using ECommons.LanguageHelpers;

namespace Splatoon.ConfigGui.CGuiLayouts.LayoutDrawHeader.Subcommands;

internal static class Freezing
{
    internal static void DrawFreezing(this Layout layout)
    {
        if(layout.Freezing)
        {
            ImGuiEx.Text("Freeze for:".Loc());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragFloat("##freezeTime", ref layout.FreezeFor, 0.1f, 0.1f, 99999f, $"{layout.FreezeFor:F1}");
            ImGuiEx.HelpMarker("Duration in seconds to display frozen elements.".Loc());

            ImGuiEx.Text("Refreeze interval:".Loc());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragFloat("##freezeInt", ref layout.IntervalBetweenFreezes, 0.1f, 0.1f, 99999f, $"{layout.IntervalBetweenFreezes:F1}");
            ImGuiEx.HelpMarker("Interval in seconds between creation of new frozen elements.\nA lower number means more elements spawned.".Loc());
            if(layout.IntervalBetweenFreezes < 0.5f)
            {
                ImGuiEx.HelpMarker("Warning: your interval between freezes is very low. Please ensure that this is intentional.", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            }

            ImGuiEx.Text("Display delay:".Loc());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragFloat("##freezeDD", ref layout.FreezeDisplayDelay, 0.1f, 0, 99999, $"{layout.FreezeDisplayDelay:F1}");
            ImGuiEx.HelpMarker("The delay in seconds before a newly created frozen element will be displayed.".Loc());

            ImGuiEx.Text("Reset on:".Loc());
            ImGui.SameLine();
            ImGui.Checkbox("Combat end".Loc(), ref layout.FreezeResetCombat);
            ImGui.SameLine();
            ImGui.Checkbox("Zone change".Loc(), ref layout.FreezeResetTerr);
        }
    }
}
