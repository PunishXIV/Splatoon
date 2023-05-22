using Dalamud.Interface.Colors;
using ECommons.LanguageHelpers;
using Splatoon.Utils;

namespace Splatoon.ConfigGui.CGuiLayouts.LayoutDrawHeader.Subcommands;

internal static class Triggers
{
    internal static void DrawTriggers(this Layout layout)
    {
        if (layout.UseTriggers)
        {
            var deleteTrigger = -1;
            for (var n = 0; n < layout.Triggers.Count; n++)
            {
                ImGui.PushID(layout.Triggers[n].GUID);
                if (ImGui.Button("[X]##") && ImGui.GetIO().KeyCtrl)
                {
                    deleteTrigger = n;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Hold CTRL + left click to delete".Loc());
                }
                ImGui.SameLine();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.Combo("##trigger", ref layout.Triggers[n].Type, Trigger.Types, Trigger.Types.Length);

                ImGuiEx.TextV("Reset on:".Loc());
                ImGui.SameLine();
                ImGui.Checkbox("Combat exit".Loc(), ref layout.Triggers[n].ResetOnCombatExit);
                ImGui.SameLine();
                ImGui.Checkbox("Territory change".Loc(), ref layout.Triggers[n].ResetOnTChange);
                ImGui.SameLine();
                ImGuiEx.Text("State: ".Loc() + layout.Triggers[n].FiredState);
                if (layout.Triggers[n].Disabled)
                {
                    ImGui.SameLine();
                    ImGuiEx.Text(ImGuiColors.DalamudRed, $"Disabled until reset");
                }
                if (layout.Triggers[n].Type == 0 || layout.Triggers[n].Type == 1)
                {
                    ImGuiEx.TextV("Time: ".Loc());
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##triggertime1", ref layout.Triggers[n].TimeBegin, 0.1f, 0, 3599, "%.1f");
                    ImGui.SameLine();
                    ImGuiEx.Text(DateTimeOffset.FromUnixTimeMilliseconds((long)(layout.Triggers[n].TimeBegin * 1000)).ToString("mm:ss.f"));
                }
                else
                {
                    ImGuiEx.InputWithRightButtonsArea($"trigger{layout.Triggers[n].GUID}", delegate
                    {
                        layout.Triggers[n].MatchIntl.ImGuiEdit(ref layout.Triggers[n].Match, "Case-insensitive (partial) message");
                    }, delegate
                    {
                        var col = layout.Triggers[n].IsRegex;
                        if (col) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                        ImGui.Checkbox("Regex", ref layout.Triggers[n].IsRegex);
                        if (col) ImGui.PopStyleColor();
                    });
                    //ImGui.InputTextWithHint("##textinput1", "Case-insensitive message", ref layout.Triggers[n].Match, 1000);

                    //ImGui.SameLine();
                    ImGui.Checkbox($"Only fire once until reset", ref layout.Triggers[n].FireOnce);
                    ImGuiEx.TextV("Delay: ".Loc());
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##triggertime1", ref layout.Triggers[n].MatchDelay, 0.1f, 0, 3599, "%.1f");
                    ImGui.SameLine();
                    ImGuiEx.Text(DateTimeOffset.FromUnixTimeMilliseconds((long)(layout.Triggers[n].MatchDelay * 1000)).ToString("mm:ss.f"));
                    layout.Triggers[n].Match = layout.Triggers[n].Match.RemoveSymbols(InvalidSymbols);
                    layout.Triggers[n].MatchIntl.RemoveSymbols(InvalidSymbols);
                }
                ImGui.SameLine();
                ImGuiEx.TextV("Duration: ".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                ImGui.DragFloat("##triggertime2", ref layout.Triggers[n].Duration, 0.1f, 0, 3599, "%.1f");
                ImGui.SameLine();
                ImGuiEx.Text(layout.Triggers[n].Duration == 0 ? "Infinite".Loc() : DateTimeOffset.FromUnixTimeMilliseconds((long)(layout.Triggers[n].Duration * 1000)).ToString("mm:ss.f"));
                ImGui.Separator();
                ImGui.PopID();
            }
            if (deleteTrigger != -1)
            {
                try
                {
                    layout.Triggers.RemoveAt(deleteTrigger);
                }
                catch (Exception e)
                {
                    P.Log(e.Message + "\n" + e.StackTrace);
                }
            }
        }
    }
}
