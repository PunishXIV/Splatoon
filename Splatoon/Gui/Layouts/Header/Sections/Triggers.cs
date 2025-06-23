using Dalamud.Interface.Colors;
using ECommons.LanguageHelpers;
using Newtonsoft.Json;
using Splatoon.Utility;

namespace Splatoon.ConfigGui.CGuiLayouts.LayoutDrawHeader.Subcommands;

internal static class Triggers
{
    internal static void DrawTriggers(this Layout layout)
    {
        if(layout.UseTriggers)
        {
            for(var n = 0; n < layout.Triggers.Count; n++)
            {
                var trigger = layout.Triggers[n];
                ImGui.PushID(trigger.GUID);
                if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl))
                {
                    new TickScheduler(() => layout.Triggers.Remove(trigger));
                }
                ImGuiEx.Tooltip("Hold CTRL + left click to delete".Loc());
                ImGui.SameLine(0, 1);
                if(ImGuiEx.IconButton(FontAwesomeIcon.Copy))
                {
                    Copy(JsonConvert.SerializeObject((Trigger[])[trigger]));
                }
                ImGuiEx.Tooltip("Copy to clipboard");
                ImGui.SameLine();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.Combo("##trigger", ref trigger.Type, Trigger.Types, Trigger.Types.Length);

                ImGuiEx.TextV("Reset on:".Loc());
                ImGui.SameLine();
                ImGui.Checkbox("Combat exit".Loc(), ref trigger.ResetOnCombatExit);
                ImGui.SameLine();
                ImGui.Checkbox("Territory change".Loc(), ref trigger.ResetOnTChange);
                ImGui.SameLine();
                ImGuiEx.Text("State: ".Loc() + trigger.FiredState);
                if(trigger.Disabled)
                {
                    ImGui.SameLine();
                    ImGuiEx.Text(ImGuiColors.DalamudRed, $"Disabled until reset");
                }
                if(trigger.Type == 0 || trigger.Type == 1)
                {
                    ImGuiEx.TextV("Time: ".Loc());
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##triggertime1", ref trigger.TimeBegin, 0.1f, 0, 3599, "%.1f");
                    ImGui.SameLine();
                    ImGuiEx.Text(DateTimeOffset.FromUnixTimeMilliseconds((long)(trigger.TimeBegin * 1000)).ToString("mm:ss.f"));
                }
                else
                {
                    ImGuiEx.InputWithRightButtonsArea($"trigger{trigger.GUID}", delegate
                    {
                        trigger.MatchIntl.ImGuiEdit(ref trigger.Match, "Case-insensitive (partial) message");
                    }, delegate
                    {
                        var col = trigger.IsRegex;
                        if(col) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                        ImGui.Checkbox("Regex", ref trigger.IsRegex);
                        if(col) ImGui.PopStyleColor();
                    });
                    //ImGui.InputTextWithHint("##textinput1", "Case-insensitive message", ref trigger.Match, 1000);

                    //ImGui.SameLine();
                    ImGui.Checkbox($"Only fire once until reset", ref trigger.FireOnce);
                    ImGuiEx.TextV("Delay: ".Loc());
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##triggertime1", ref trigger.MatchDelay, 0.1f, 0, 3599, "%.1f");
                    ImGui.SameLine();
                    ImGuiEx.Text(DateTimeOffset.FromUnixTimeMilliseconds((long)(trigger.MatchDelay * 1000)).ToString("mm:ss.f"));
                    trigger.Match = trigger.Match.RemoveSymbols(InvalidSymbols);
                    trigger.MatchIntl.RemoveSymbols(InvalidSymbols);
                }
                ImGui.SameLine();
                ImGuiEx.TextV("Duration: ".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                ImGui.DragFloat("##triggertime2", ref trigger.Duration, 0.1f, 0, 3599, "%.1f");
                ImGui.SameLine();
                ImGuiEx.Text(trigger.Duration == 0 ? "Infinite".Loc() : DateTimeOffset.FromUnixTimeMilliseconds((long)(trigger.Duration * 1000)).ToString("mm:ss.f"));
                ImGui.Separator();
                ImGui.PopID();
            }
        }
    }
}
