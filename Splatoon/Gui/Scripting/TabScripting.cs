using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.LanguageHelpers;
using Splatoon.SplatoonScripting;

namespace Splatoon.Gui.Scripting;

internal static class TabScripting
{
    internal static volatile bool ForceUpdate = false;
    internal static string Search = "";
    internal static void Draw()
    {
        if (ScriptingProcessor.ThreadIsRunning)
        {
            ImGuiEx.LineCentered("ThreadCompilerRunning", delegate
            {
                ImGuiEx.Text(GradientColor.Get(ImGuiColors.DalamudWhite, ImGuiColors.ParsedPink), "Scripts are being installed, please wait...".Loc());
            });
        }
        ImGuiEx.TextWrapped(ImGuiColors.DalamudOrange, "Please note that scripts have direct and unrestricted access to your PC and game. Ensure that you know what you're installing.".Loc());
        var force = ForceUpdate;
        if(ImGui.Checkbox($"Force Update".Loc(), ref force)) ForceUpdate = force;
        ImGui.SameLine();
        if(ImGui.Button("Clear cache, rescan directory and reload all scripts".Loc()))
        {
            var dir = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "ScriptCache");
            foreach(var x in Directory.GetFiles(dir))
            {
                if (x.EndsWith(".bin"))
                {
                    PluginLog.Information($"Deleting {x}");
                    File.Delete(x);
                }
            }
            ScriptingProcessor.ReloadAll();
        }
        ImGui.SameLine();
        if(ImGui.Button("Install from clipboard (code or trusted URL)".Loc()))
        {
            var text = ImGui.GetClipboardText();
            if (ScriptingProcessor.IsUrlTrusted(text))
            {
                ScriptingProcessor.DownloadScript(text);
            }
            else 
            {
                ScriptingProcessor.CompileAndLoad(text, null);
            }
        }
        ImGui.SetNextItemWidth(250f);
        ImGui.InputTextWithHint("##search", "Search...", ref Search, 50);
        
        var openConfig = ScriptingProcessor.Scripts.FirstOrDefault(x => x.InternalData.ConfigOpen);

        if(openConfig != null)
        {
            DrawScriptGroup([openConfig]);
        }
        else
        {
            if(Search != "")
            {
                DrawScriptGroup(ScriptingProcessor.Scripts);
            }
            var namespaces = ScriptingProcessor.Scripts.Select(x => x.InternalData.Namespace).Distinct().Order();
            foreach(var nsp in namespaces)
            {
                ImGuiEx.TreeNodeCollapsingHeader(nsp.Replace("_", " ").Replace(".", " - "), () =>
                {
                    ImGui.PushID(nsp);
                    DrawScriptGroup(ScriptingProcessor.Scripts.Where(x => x.InternalData.Namespace == nsp).OrderBy(x => x.InternalData.Name));
                    ImGui.PopID();
                });
            }
        }

        void DrawScriptGroup(IEnumerable<SplatoonScript> scripts)
        {
            if(ImGui.BeginTable("##scriptsTable", 6, ImGuiTableFlags.BordersInner | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("State");
                ImGui.TableSetupColumn("##c1");
                ImGui.TableSetupColumn("##c2");
                ImGui.TableSetupColumn("##c3");
                ImGui.TableSetupColumn("##c4");
                ImGui.TableHeadersRow();
                foreach(var script in scripts)
                {
                    var searchSplot = Search.Split(",", StringSplitOptions.TrimEntries);
                    if(!(Search == "" || script.InternalData.Name.ContainsAny(StringComparison.OrdinalIgnoreCase, searchSplot) || script.InternalData.Namespace.ContainsAny(StringComparison.OrdinalIgnoreCase, searchSplot))) continue;
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushID(script.InternalData.GUID);
                    ImGuiEx.TextV($"{script.InternalData.Name.Replace("_", " ")}");
                    if(script.Metadata?.Description == null)
                    {
                        ImGuiEx.Tooltip($"{script.InternalData.Namespace}");
                    }
                    else
                    {
                        ImGuiEx.Tooltip($"{script.InternalData.Namespace}\n{script.Metadata.Description}");
                    }
                    if(ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        ImGui.SetClipboardText($"{script.InternalData.FullName}");
                        Notify.Success("Copied to clipboard");
                    }
                    if(script.Metadata?.Version != null)
                    {
                        ImGui.SameLine();
                        ImGuiEx.Text(ImGuiColors.DalamudGrey2, $"v{script.Metadata.Version}");
                    }
                    if(script.Metadata?.Author != null)
                    {
                        ImGui.SameLine();
                        ImGuiEx.Text(ImGuiColors.DalamudGrey2, $"by {script.Metadata.Author}");
                    }

                    ImGui.TableNextColumn();

                    if(script.InternalData.Blacklisted)
                    {
                        ImGuiEx.TextV(ImGuiColors.DalamudGrey3, "Blacklisted".Loc());
                        ImGuiComponents.HelpMarker("This script was blacklisted due to compatibility issues. Please wait for it's new version to be released.".Loc());
                    }
                    else if(!script.InternalData.Allowed)
                    {
                        ImGuiEx.TextV(ImGuiColors.ParsedGold, "Preparing".Loc());
                        ImGuiComponents.HelpMarker("This script is being prepared for enabling and will be available shortly.".Loc());
                    }
                    else if(script.IsDisabledByUser)
                    {
                        ImGuiEx.TextV(ImGuiColors.DalamudRed, "Disabled".Loc());
                        ImGuiComponents.HelpMarker("This script has been disabled by you.".Loc());
                    }
                    else if(script.IsEnabled)
                    {
                        ImGuiEx.TextV(ImGuiColors.ParsedGreen, "Active".Loc());
                        ImGuiComponents.HelpMarker("This script is currently active and being executed.".Loc());
                    }
                    else
                    {
                        ImGuiEx.TextV(ImGuiColors.DalamudYellow, "Inactive".Loc());
                        ImGuiComponents.HelpMarker("This script is currently inactive because you're not in a zone for which it was designed.".Loc());
                    }
                    ImGui.TableNextColumn();

                    if(!script.InternalData.Allowed || script.InternalData.Blacklisted)
                    {
                        if(ImGuiEx.IconButton(FontAwesomeIcon.Play))
                        {
                            script.InternalData.Allowed = true;
                            script.InternalData.Blacklisted = false;
                            script.UpdateState();
                        }
                        ImGuiEx.Tooltip("Forcefully allow this script to be enabled. Consequences of this action will be unpredictable.");
                    }
                    else
                    {

                        var e = P.Config.DisabledScripts.Contains(script.InternalData.FullName);
                        if(ImGuiEx.IconButton(e ? FontAwesomeIcon.PlayCircle : FontAwesomeIcon.PauseCircle))
                        {
                            if(e)
                            {
                                P.Config.DisabledScripts.Remove(script.InternalData.FullName);
                            }
                            else
                            {
                                P.Config.DisabledScripts.Add(script.InternalData.FullName);
                            }
                            ScriptingProcessor.Scripts.ForEach(x => x.UpdateState());
                        }
                        ImGuiEx.Tooltip(e ? "Enable script".Loc() : "Disable script".Loc());
                    }

                    ImGui.TableNextColumn();

                    if(script.InternalData.SettingsPresent)
                    {
                        if(ImGuiEx.IconButton(FontAwesomeIcon.Cog))
                        {
                            if(script.InternalData.ConfigOpen)
                            {
                                openConfig.Controller.SaveConfig();
                            }
                            script.InternalData.ConfigOpen = !script.InternalData.ConfigOpen;
                        }
                        ImGuiEx.Tooltip("Open script's settings".Loc());
                    }
                    else if(script.Controller.GetRegisteredElements().Count > 0)
                    {
                        if(ImGuiEx.IconButton(FontAwesomeIcon.PaintBrush))
                        {
                            if(script.InternalData.ConfigOpen)
                            {
                                openConfig.Controller.SaveConfig();
                            }
                            script.InternalData.ConfigOpen = !script.InternalData.ConfigOpen;
                        }
                        ImGuiEx.Tooltip("Open element editor".Loc());
                    }
                    else
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0f);
                        ImGuiEx.IconButton(FontAwesomeIcon.Cog);
                        ImGui.PopStyleVar();
                        //ImGuiEx.Tooltip("This script contains no settings");
                    }

                    ImGui.TableNextColumn();

                    if(ImGuiEx.IconButton("\uf0e2"))
                    {
                        ScriptingProcessor.ReloadScript(script);
                    }
                    ImGuiEx.Tooltip("Reload this script");

                    ImGui.TableNextColumn();

                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash) && ImGui.GetIO().KeyCtrl)
                    {
                        if(!script.InternalData.Path.IsNullOrEmpty() && script.InternalData.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        {
                            new TickScheduler(() =>
                            {
                                script.Disable();
                                ScriptingProcessor.Scripts = ScriptingProcessor.Scripts.Remove(script);
                                DeleteFileToRecycleBin(script.InternalData.Path);
                            });
                        }
                        else
                        {
                            Notify.Error("Error deleting".Loc());
                        }
                    }
                    ImGuiEx.Tooltip("Delete script. Hold CTRL + click".Loc());
                    ImGui.PopID();
                }
                ImGui.EndTable();
            }
        }

        if (openConfig != null)
        {
            ImGuiEx.LineCentered("ScriptConfigTitle", delegate
            {
                ImGuiEx.Text(ImGuiColors.DalamudYellow, $"{openConfig.InternalData.FullName} configuration");
            });
            ImGui.Separator();
            ImGuiEx.EzTabBar("##scriptConfig", 
                (openConfig.InternalData.SettingsPresent?"Configuration":null, () => {
                    try
                    {
                        openConfig.OnSettingsDraw();
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
                }, null, false),
                (openConfig.Controller.GetRegisteredElements().Count>0?"Registered elements":null, openConfig.DrawRegisteredElements, null, false)
                );
            
            ImGuiEx.LineCentered("ScriptConfig", delegate
            {
                if (ImGui.Button("Close and save configuration"))
                {
                    openConfig.InternalData.ConfigOpen = false;
                    openConfig.Controller.SaveConfig();
                }
            });
        }
    }
}
