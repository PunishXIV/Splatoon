using Dalamud.Interface.Components;
using ECommons.LanguageHelpers;
using Newtonsoft.Json;
using Splatoon.ConfigGui;
using Splatoon.ConfigGui.CGuiLayouts.LayoutDrawHeader.Subcommands;
using Splatoon.Gui.Layouts.Header.Sections;
using Splatoon.Utility;

namespace Splatoon;

internal partial class CGui
{
    private string NewGroupName = "";

    private void LayoutDrawHeader(Layout layout)
    {
        if(ImGui.BeginTable("SingleLayoutEdit", 2, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerH))
        {
            ImGui.TableSetupColumn("##LayoutEdit1", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("##LayoutEdit2", ImGuiTableColumnFlags.WidthStretch);

            //ImGui.TableHeadersRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV("Group:".Loc());
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            if(ImGui.BeginCombo("##group", $"{(layout.Group == "" ? "- No group -".Loc() : $"{layout.Group}")}"))
            {
                if(ImGui.Selectable("- No group -".Loc()))
                {
                    layout.Group = "";
                }
                foreach(var x in P.Config.GroupOrder)
                {
                    if(ImGui.Selectable(x))
                    {
                        layout.Group = x;
                    }
                }
                void Add()
                {
                    layout.Group = NewGroupName;
                    NewGroupName = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGuiEx.InputWithRightButtonsArea("SelectGroup", delegate
                {
                    if(ImGui.InputTextWithHint("##NewGroupName", "New group...".Loc(), ref NewGroupName, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        Add();
                    }
                    NewGroupName = NewGroupName.SanitizeName();
                }, delegate
                {
                    if(ImGui.Button("Add".Loc()))
                    {
                        Add();
                    }
                });
                ImGui.EndCombo();
            }


            ImGui.TableNextColumn();
            ImGuiEx.TextV("Export:".Loc());
            ImGui.TableNextColumn();
            if(ImGui.Button("Copy to clipboard".Loc()))
            {
                layout.ExportToClipboard();
            }
            ImGui.SameLine();
            ImGuiEx.TextV("Share:".Loc());
            ImGui.SameLine();
            if(ImGui.Button("GitHub".Loc()))
            {
                layout.ExportToClipboard();
                Contribute.OpenGithubPresetSubmit();
            }
            ImGui.SameLine(0, 1);
            if(ImGui.Button("Discord".Loc()))
            {
                layout.ExportToClipboard();
                Contribute.OpenDiscordLink();
            }
            ImGui.SameLine();
            if(ImGui.Button("Copy for Web API".Loc()))
            {
                HTTPExportToClipboard(layout);
            }
            ImGuiEx.Tooltip("Hold ALT to copy raw JSON (for usage with post body or you'll have to urlencode it yourself)\nHold CTRL and click to copy urlencoded raw".Loc());


            ImGui.TableNextColumn();
            ImGui.Checkbox("Enabled".Loc(), ref layout.Enabled);

            if(layout.IsVisible())
            {
                ImGuiEx.HelpMarker("This layout is currently being rendered".Loc(), EColor.GreenBright, FontAwesomeIcon.Eye.ToIconString());
            }
            else
            {
                ImGuiEx.HelpMarker("This layout is currently not being rendered".Loc(), EColor.White, FontAwesomeIcon.EyeSlash.ToIconString());
            }
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(150f.Scale());
            if(ImGui.BeginCombo("##phaseSelectorL", $"{(layout.Phase == 0 ? "Any phase".Loc() : $"Phase ??".Loc(layout.Phase))}"))
            {
                if(ImGui.Selectable("Any phase".Loc())) layout.Phase = 0;
                if(ImGui.Selectable("Phase 1 (doorboss)".Loc())) layout.Phase = 1;
                if(ImGui.Selectable("Phase 2 (post-doorboss)".Loc())) layout.Phase = 2;
                ImGuiEx.Text("Manual phase selection:".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(30f.Scale());
                ImGui.DragInt("##mPSel", ref layout.Phase, 0.1f, 0, 9);
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            ImGui.Checkbox("Disable in duty".Loc(), ref layout.DisableInDuty);

            ImGui.TableNextColumn();
            ImGuiEx.TextV("Name:".Loc());
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            if(ImGui.InputText("##name", ref layout.Name, 100))
            {
                layout.Name = layout.Name.SanitizeName();
            }

            ImGui.TableNextColumn();
            ImGuiEx.TextV("Intl. Name:".Loc());
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            layout.InternationalName.ImGuiEdit(ref layout.Name);

            ImGui.TableNextColumn();
            ImGuiEx.TextV("Display conditions:".Loc());
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            ImGui.Combo("##dcn", ref layout.DCond, Layout.DisplayConditions, Layout.DisplayConditions.Length);

            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            if(ImGui.BeginCombo("##zlock", layout.IsZoneBlacklist ? "Zone Blacklist".Loc() : "Zone Whitelist".Loc()))
            {
                if(ImGui.Selectable("Whitelist mode".Loc()))
                {
                    layout.IsZoneBlacklist = false;
                }
                if(ImGui.Selectable("Blacklist mode".Loc()))
                {
                    layout.IsZoneBlacklist = true;
                }
                ImGui.EndCombo();
            }
            ImGui.TableNextColumn();
            layout.DrawZlockSelector();

            ImGui.TableNextColumn();

            ImGuiEx.TextV("Scene");
            ImGui.TableNextColumn();
            layout.DrawSceneSelector();

            ImGui.TableNextColumn();
            ImGuiEx.TextV("Job lock".Loc());
            ImGui.TableNextColumn();
            layout.DrawJlockSelector();

            var selectedConf = layout.Subconfigurations.FirstOrDefault(x => x.Guid == layout.SelectedSubconfigurationID);
            ImGui.TableNextColumn();
            ImGuiEx.TextV(selectedConf == null?EColor.GreenBright:EColor.YellowBright,"Configuration".Loc());
            ImGui.TableNextColumn();
            layout.DrawLayoutConfigurations();

            if(selectedConf != null)
            {
                ImGui.TableNextColumn();
                ImGuiEx.TextV(selectedConf == null ? EColor.GreenBright : EColor.YellowBright, "Configuration Name".Loc());
                ImGui.TableNextColumn();
                layout.DrawLayoutConfigurationName();
            }

            ImGui.TableNextColumn();
            ImGui.Checkbox("Distance limit".Loc(), ref layout.UseDistanceLimit);
            ImGui.TableNextColumn();
            layout.DrawDistanceLimit();

            ImGui.TableNextColumn();
            ImGuiEx.TextV("Multiple Conditions".Loc());
            ImGui.TableNextColumn();
            ImGuiEx.RadioButtonBool("AND##mcc", "OR##mcc", ref layout.ConditionalAnd, true);

            ImGui.TableNextColumn();
            ImGui.Checkbox("Freeze".Loc(), ref layout.Freezing);
            ImGuiComponents.HelpMarker(
@"Freeze is an advanced setting that can have negative side effects.
When the requirements to display an element are met,
a new element is created and frozen in place and displayed for a duration.
New frozen elements are created every refreeze interval.".Loc());
            ImGui.TableNextColumn();
            layout.DrawFreezing();

            ImGui.TableNextColumn();
            ImGui.Checkbox("Enable triggers".Loc(), ref layout.UseTriggers);
            if(layout.UseTriggers)
            {
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add".Loc()))
                {
                    layout.Triggers.Add(new Trigger());
                }
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Copy, "Copy".Loc()))
                {
                    ImGui.SetClipboardText(JsonConvert.SerializeObject(layout.Triggers));
                }
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Paste, "Paste Replace".Loc(), ImGui.GetIO().KeyCtrl || layout.Triggers.Count == 0))
                {
                    try
                    {
                        layout.Triggers = JsonConvert.DeserializeObject<List<Trigger>>(ImGui.GetClipboardText());
                    }
                    catch(Exception e)
                    {
                        Notify.Error(e.Message);
                    }
                }
                if(layout.Triggers.Count != 0)
                {
                    ImGuiEx.Tooltip("Hold CTRL and click. Existing triggers will be overwritten.".Loc());
                }
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Paste, "Paste Add".Loc()))
                {
                    try
                    {
                        var newTriggers = JsonConvert.DeserializeObject<List<Trigger>>(ImGui.GetClipboardText());
                        foreach(var t in newTriggers)
                        {
                            layout.Triggers.Add(t);
                        }
                    }
                    catch(Exception e)
                    {
                        Notify.Error(e.Message);
                    }
                }
            }
            ImGui.TableNextColumn();
            layout.DrawTriggers();

            ImGui.EndTable();
        }


        var i = layout.Name;
        var topCursorPos = ImGui.GetCursorPos();
    }
}
