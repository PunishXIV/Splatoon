using Dalamud.Game;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ECommons.Configuration;
using ECommons.LanguageHelpers;
using ECommons.SimpleGui;
using Newtonsoft.Json;
using Splatoon.Modules.TranslationWorkspace;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Gui.Windows;
public unsafe sealed class TranslationWorkspaceWindow : Window
{
    public Page Page;
    public static ClientLanguage SourceLanguage = ClientLanguage.English;
    public static ClientLanguage TargetLanguage = Svc.Data.Language;
    public TranslationWorkspaceWindow(string name, Page page) : base(name, ImGuiWindowFlags.NoSavedSettings)
    {
        Page = page;    
        this.RespectCloseHotkey = false;
        this.IsOpen = true;
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    public override void OnClose()
    {
        EzConfigGui.WindowSystem.RemoveWindow(this);
        EzConfig.Save();
    }

    public override void Draw()
    {
        ImGuiEx.SetNextItemFullWidth();
        ImGui.InputText("##name", ref Page.Name, 100);
        ImGuiEx.Tooltip("Workspace name. Just for your convenience, never exported.".Loc());
        if(ImGuiEx.BeginDefaultTable("SelLang", ["Language", "~Select"], false))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV("Source Language:".Loc());
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            ImGuiEx.EnumCombo("##src", ref SourceLanguage);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextV("Target Language:".Loc());
            ImGui.TableNextColumn();
            ImGuiEx.SetNextItemFullWidth();
            ImGuiEx.EnumCombo("##tar", ref TargetLanguage);
            ImGui.EndTable();
        }

        if(SourceLanguage == TargetLanguage)
        {
            ImGuiEx.Text(EColor.RedBright, "Source and Target languages can not be the same!".Loc());
            return;
        }

        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Copy, "Copy Result to Clipboard".Loc()))
        {
            Copy(Page.Lines.Select(x =>
            {
                if(x.Kind == LineKind.Text) return x.Text;
                if(x.Kind == LineKind.Layout) return JsonConvert.SerializeObject(x.Layout);
                throw new InvalidOperationException();
            }).Join("\n"));
        }
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Trash, "Delete Workspace".Loc(), enabled:ImGuiEx.Shift && ImGuiEx.Ctrl))
        {
            new TickScheduler(() => P.Config.TranslatorPages.Remove(Page));
            this.IsOpen = false;
        }
        ImGuiEx.Text($"Hold SHIFT and CTRL and click this button to permanently delete this workspace. Only do it after you have sent pull request or committed and pushed changes to the repository.".Loc());

        if(ImGuiEx.BeginDefaultTable("Worktable", ["~Table"], false))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var nextSection = false;
            for(int i = 0; i < Page.Lines.Count; i++)
            {
                var line = Page.Lines[i];
                if(line.Kind == LineKind.Text && line.Text.Trim().EqualsAny("", "```")) continue;

                if(nextSection)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    nextSection = false;
                }

                if(line.Kind == LineKind.Text)
                {
                    ImGuiEx.Text(line.Text);
                }
                else if(line.Kind == LineKind.Layout)
                {
                    nextSection = true;
                    var l = line.Layout;

                    ImGui.Indent();

                    ImGuiEx.Text($"Layout name:");
                    ImGui.Indent();
                    EditField(l.InternationalName, l.Name);
                    ImGui.Unindent();

                    var triggers = l.Triggers.Where(x => x.Match != "" || !x.MatchIntl.IsEmpty()).ToArray();
                    if(triggers.Length > 0)
                    {
                        ImGuiEx.Text("Triggers:");
                        ImGui.Indent();
                        foreach(var x in triggers)
                        {
                            EditField(x.MatchIntl, x.Match);
                        }
                        ImGui.Unindent();
                    }

                    ImGuiEx.Text($"Elements:");
                    ImGui.Indent();

                    for(int j = 0; j < l.ElementsL.Count; j++)
                    {
                        var e = l.ElementsL[j];
                        var skipped = false;
                        if(e.Name != "" || !e.InternationalName.IsEmpty())
                        {
                            skipped = false;
                            ImGuiEx.Text($"Element {j + 1} name:");
                            ImGui.Indent();
                            EditField(e.InternationalName, e.Name);
                            ImGui.Unindent();
                        }
                        if(e.IsActorNameUsed())
                        {
                            skipped = false;
                            ImGuiEx.Text($"Element {j + 1} object name:");
                            ImGui.Indent();
                            EditField(e.refActorNameIntl, e.refActorName);
                            ImGui.Unindent();
                        }
                        if(e.overlayText != "" || !e.overlayTextIntl.IsEmpty())
                        {
                            skipped = false;
                            ImGuiEx.Text($"Element {j + 1} overlay:");
                            ImGui.Indent();
                            EditField(e.refActorNameIntl, e.refActorName);
                            ImGui.Unindent();
                        }
                        if(!skipped)
                        {
                            ImGui.Separator();
                        }
                    }

                    ImGui.Unindent();
                    ImGui.Unindent();

                }

            }
            ImGui.EndTable();
        }
    }

    public void EditField(InternationalString internationalStr, string str)
    {
        ImGui.PushID(internationalStr.guid);
        ImGuiEx.Text(internationalStr.Get(str, SourceLanguage));
        ImGuiEx.SetNextItemFullWidth();
        ImGui.InputTextWithHint("##input", $"Use Global Value: {str}", ref internationalStr.GetRefString(TargetLanguage), 2000);
        ImGuiEx.Tooltip($"Entered text will be used with game clients using language: {TargetLanguage}. If empty, global value will be used. Global value is:\n{str}");
        ImGui.PopID();
    }
}