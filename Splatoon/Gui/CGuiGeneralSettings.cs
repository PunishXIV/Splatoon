using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.LanguageHelpers;
using ECommons.Reflection;
using NightmareUI;
using NightmareUI.PrimaryUI;
using Splatoon.Gui.Priority;
using Splatoon.Modules;
using Splatoon.Serializables;
using Splatoon.SplatoonScripting.Priority;
using Splatoon.Utility;
using Localization = ECommons.LanguageHelpers.Localization;

namespace Splatoon;

internal partial class CGui
{
    private void DisplayGeneralSettings()
    {
        ImGuiEx.Text("Game version: ".Loc());
        ImGui.SameLine(0, 0);
        ImGuiEx.TextCopy(P.loader.gVersion);
        new NuiBuilder().Section("Logging and Web API", collapsible: false)
            .Widget(() =>
            {
                ImGuiUtils.SizedText("Use web API".Loc(), WidthLayout);
                ImGui.SameLine();
                if(ImGui.Checkbox("##usewebapi", ref P.Config.UseHttpServer))
                {
                    P.SetupShutdownHttp(P.Config.UseHttpServer);
                }
                ImGui.SameLine();
                if(P.Config.UseHttpServer)
                {
                    ImGuiEx.Text("http://127.0.0.1:" + P.Config.port + "/");
                    if(ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        if(ImGui.IsMouseReleased(ImGuiMouseButton.Left) && ImGui.GetMouseDragDelta(ImGuiMouseButton.Left) == Vector2.Zero)
                        {
                            Utils.ProcessStart("http://127.0.0.1:" + P.Config.port + "/");
                        }
                    }
                }
                else
                {
                    ImGuiEx.Text("Port: ".Loc());
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragInt("##webapiport", ref P.Config.port, float.Epsilon, 1, 65535);
                    if(ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Please only change if you have really good reason".Loc());
                    }
                    if(P.Config.port < 1 || P.Config.port > 65535) P.Config.port = 47774;
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100f);
                    if(ImGui.Button("Default".Loc()))
                    {
                        P.Config.port = 47774;
                    }
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(250f);
                if(ImGui.Button("Open web API guide".Loc()))
                {
                    Utils.ProcessStart("https://github.com/PunishXIV/Splatoon#web-api-beta");
                }

                if(ImGui.Checkbox("Enable logging".Loc(), ref Splatoon.P.Config.Logging))
                {
                    Logger.OnTerritoryChanged();
                }
                ImGuiComponents.HelpMarker("Enable logging, which will log chat messages, casts and VFX info into log files. ".Loc());
                ImGui.SameLine();
                ImGui.Checkbox("Log position".Loc(), ref Splatoon.P.Config.LogPosition);
                ImGuiComponents.HelpMarker("Log object position in casting information log lines".Loc());
                ImGui.Checkbox("Disallow scripts from queueing commands and message sending".Loc(), ref P.Config.NoChat);
            })

            .Section("Information", visible: () => Svc.PluginInterface.InstalledPlugins.Any(x => x.IsLoaded && x.InternalName == "ARealmRecorded"))
            .Widget(() =>
            {
                ImGuiEx.TextWrapped($"Looking to watch expired Duty Recorder replays?");
                ImGuiEx.TextWrapped($"Become supporter of NightmareXIV with \"Nightmare\" tier or higher get access to replay upgrade tool. Replays are processed locally on your own PC, without sending data across the Internet. ");
                ImGuiEx.LineCentered("NxivSupport", () =>
                {
                    if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.DoorOpen, "Join Now"))
                    {
                        ShellStart("https://subscribe.nightmarexiv.org/");
                    }
                    ImGuiEx.Tooltip($"Join \"Nightmare\" tier or higher to gain access to the tool. ");
                    ImGui.SameLine();
                    if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.DoorOpen, "Ask a question or See Details in Discord"))
                    {
                        ShellStart("https://discord.gg/BeeRFKDJD3");
                    }
                    ImGuiEx.Tooltip($"After joining Discord, head to \"Early Access Program\" channel. If you have a question, head to \"General\" channel and create a ticket. ");
                });
            })

            .Section("Alias")
            .Widget(() =>
            {
                ImGuiEx.TextWrapped("You can define alias for /splatoon command here. Leave empty to remove alias.".Loc());
                ImGuiEx.InputWithRightButtonsArea(() =>
                {
                    ImGui.InputText("##alias", ref P.Config.Alias, 50);
                }, () =>
                {
                    if(P.Config.Alias.Length > 0 && !P.Config.Alias.StartsWith('/'))
                    {
                        ImGuiEx.HelpMarker("Alias must start with slash /", ImGuiColors.DalamudRed, FontAwesomeIcon.ExclamationTriangle.ToIconString(), sameLine:false);
                        ImGui.SameLine();
                    }
                    if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Check, "Apply"))
                    {
                        S.AliasManager.SetAlias();
                    }
                });
            })

            .Section("Language", collapsible: false)
            .Widget(() =>
            {
                ImGuiEx.TextV("Splatoon language: ".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f.Scale());
                if(ImGui.BeginCombo("##langsel", Splatoon.P.Config.PluginLanguage == null ? "Game language".Loc() : Splatoon.P.Config.PluginLanguage.Loc()))
                {
                    if(ImGui.Selectable("Game language".Loc()))
                    {
                        Splatoon.P.Config.PluginLanguage = null;
                        Localization.Init(GameLanguageString);
                    }
                    foreach(var x in GetAvaliableLanguages())
                    {
                        if(ImGui.Selectable(x.Loc()))
                        {
                            Splatoon.P.Config.PluginLanguage = x;
                            Localization.Init(Splatoon.P.Config.PluginLanguage);
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.Checkbox("Localization logging".Loc(), ref Localization.Logging);
                ImGui.SameLine();
                if(ImGui.Button("Save entries: ??".Loc(Splatoon.P.Config.PluginLanguage ?? GameLanguageString)))
                {
                    Localization.Save(Splatoon.P.Config.PluginLanguage ?? GameLanguageString);
                }
                ImGui.SameLine();
                if(ImGui.Button("Rescan language files".Loc()))
                {
                    GetAvaliableLanguages(true);
                }
            })

            .Section("UI settings", collapsible: false)
            .Widget(() =>
            {
                ImGui.Checkbox("Use hexadecimal numbers".Loc(), ref P.Config.Hexadecimal);
                ImGui.Checkbox("Enable tether on Splatoon find command".Loc(), ref P.Config.TetherOnFind);
                ImGui.Checkbox("Force show Splatoon's UI when game UI is hidden".Loc(), ref P.Config.ShowOnUiHide);
            })

            .Section("Scripts configuration and priority lists", collapsible: false)
            .Widget(() =>
            {
                ImGui.Checkbox("Disable script cache".Loc(), ref P.Config.DisableScriptCache);
                var state = DalamudReflector.GetDtrEntryState(InfoBar.EntryName);
                if(ImGui.Checkbox("Enable info bar priority indicator", ref state))
                {
                    DalamudReflector.SetDtrEntryState(InfoBar.EntryName, state);
                }
                ImGui.SetNextItemWidth(150f);
                ImGuiEx.EnumCombo("Priority assignment auto-loading notification", ref Splatoon.P.Config.ScriptPriorityNotification);
                ImGuiEx.TreeNodeCollapsingHeader("Preferred Role Assignments", () =>
                {
                    ImGuiEx.Text($"Select role assignments that you would like to assigned to yourself via autofill function");
                    foreach(var j in Enum.GetValues<Job>().Where(x => x > 0 && !x.IsUpgradeable() && x.IsCombat()).OrderBy(x => Splatoon.P.PriorityPopupWindow.GetOrderedRoleIndex(x)))
                    {
                        var pref = Splatoon.P.Config.PreferredPositions.SafeSelect(j);
                        var name = PriorityPopupWindow.ConfiguredNames.SafeSelect(pref) ?? "No preferred position";
                        ImGui.PushID(j.ToString());
                        ImGui.SetNextItemWidth(150f);
                        if(ImGui.BeginCombo("##jselect", name, ImGuiComboFlags.HeightLarge))
                        {
                            foreach(var x in Enum.GetValues<RolePosition>())
                            {
                                if(ImGui.Selectable(PriorityPopupWindow.ConfiguredNames.SafeSelect(x) ?? "No preferred position", pref == x))
                                {
                                    Splatoon.P.Config.PreferredPositions[j] = x;
                                }
                            }
                            ImGui.EndCombo();
                        }
                        ImGui.SameLine();
                        if(ThreadLoadImageHandler.TryGetIconTextureWrap(j.GetIcon(), true, out var tex))
                        {
                            ImGui.Image(tex.Handle, new Vector2(ImGui.GetFrameHeight()));
                            ImGui.SameLine();
                        }
                        ImGuiEx.TextV(j.ToString());
                        ImGui.PopID();
                    }
                });
                ImGuiEx.TreeNodeCollapsingHeader("Edit saved priority lists", () =>
                {
                    Dictionary<uint, List<RolePlayerAssignment>> dict = [];
                    foreach(var x in Splatoon.P.Config.RolePlayerAssignments)
                    {
                        if(!dict.TryGetValue(x.Territory, out var list))
                        {
                            list = [];
                            dict[x.Territory] = list;
                        }
                        list.Add(x);
                    }
                    foreach(var x in dict)
                    {
                        ImGui.PushID(x.Key.ToString());
                        ImGuiEx.TreeNodeCollapsingHeader($"{ExcelTerritoryHelper.GetName(x.Key)} - {x.Value.Count} assignments###edit{x.Key}", () =>
                        {
                            if(ImGui.BeginTable($"PrioTable{x.Key}", 2, ImGuiEx.DefaultTableFlags))
                            {
                                ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthStretch);
                                ImGui.TableSetupColumn("2");

                                foreach(var a in x.Value)
                                {
                                    ImGui.PushID(a.ToString());
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    var lst = a.Players.Select(p => $"{PriorityPopupWindow.ConfiguredNames.SafeSelect(PriorityPopupWindow.RolePositions.SafeSelect(a.Players.IndexOf(p)))}: {p.Name}{(p.Jobs.Count > 0 ? $" - {p.Jobs.Print()}" : "")}");
                                    ImGuiEx.TextV($"{lst.Print()}");
                                    ImGuiEx.Tooltip(lst.Print("\n"));
                                    ImGui.TableNextColumn();
                                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                                    {
                                        new TickScheduler(() => Splatoon.P.Config.RolePlayerAssignments.Remove(a));
                                    }
                                    ImGui.PopID();
                                }

                                ImGui.EndTable();
                            }
                        });
                        ImGui.PopID();
                    }

                });
            })

            .Section("Miscellaneous", collapsible: false)
            .Widget(() =>
            {
                if(ImGui.Button("Open backup directory".Loc()))
                {
                    Utils.ProcessStart(Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "Backups"));
                }
                ImGui.Separator();
                ImGuiEx.Text("Contact developer:".Loc());
                ImGui.SameLine();
                if(ImGui.Button("Github".Loc()))
                {
                    Utils.ProcessStart("https://github.com/PunishXIV/Splatoon/issues");
                }
                ImGui.SameLine();
                if(ImGui.Button("Discord".Loc()))
                {
                    ImGui.SetClipboardText(Splatoon.DiscordURL);
                    Svc.Chat.Print("[Splatoon] Server invite link: ".Loc() + Splatoon.DiscordURL);
                    Utils.ProcessStart(Splatoon.DiscordURL);
                }
                ImGui.Checkbox("Disable stream notice (effective only after restart)".Loc(), ref Splatoon.P.Config.NoStreamWarning);
            })

            .Section("Script auto-reloading (for developers)", collapsible: true)
            .TextWrapped("Add pathes to folders that contain scripts that you are editing. Do NOT add Splatoon's own configuration folder here.")
            .Widget(() =>
            {
                for(var i = 0; i < Splatoon.P.Config.FileWatcherPathes.Count; i++)
                {
                    var index = i;
                    var f = Splatoon.P.Config.FileWatcherPathes[i];
                    ImGuiEx.InputWithRightButtonsArea(() =>
                    {
                        if(ImGui.InputTextWithHint("##path to folder" + index, "Path to folder...", ref f, 2000))
                        {
                            Splatoon.P.Config.FileWatcherPathes[index] = f;
                        }
                    }, () =>
                    {
                        if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, "Trash" + index))
                        {
                            new TickScheduler(() => Splatoon.P.Config.FileWatcherPathes.RemoveAt(index));
                        }
                    });
                }
                ImGuiEx.LineCentered(() =>
                {
                    if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add New"))
                    {
                        Splatoon.P.Config.FileWatcherPathes.Add("");
                    }
                    ImGui.SameLine();
                    if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Check, "Apply settings"))
                    {
                        S.ScriptFileWatcher.StartWatching();
                    }
                });
            }).Draw();

        Svc.PluginInterface.UiBuilder.DisableUserUiHide = P.Config.ShowOnUiHide;
    }
}
