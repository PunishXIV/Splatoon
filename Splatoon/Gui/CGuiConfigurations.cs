using Dalamud.Interface.Colors;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods.TerritorySelection;
using ECommons.LanguageHelpers;
using Lumina.Excel.Sheets;
using Splatoon.Gui.Layouts.Header.Sections;
using Splatoon.Services;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Gui;
internal static class CGuiConfigurations
{
    private static uint SelectedZone = 0;
    private static List<string> AvailableConfigurations = [];

    public static void Draw()
    {
        string requestedConfiguration = null;
        if(ImGui.RadioButton("All Zones".Loc(), SelectedZone == 0)) SelectedZone = 0;
        var current = SelectedZone == 0 || SelectedZone == Player.Territory;
        ImGui.SameLine();
        if(ImGui.RadioButton(ExcelTerritoryHelper.GetName(Player.Territory), SelectedZone == Player.Territory)) SelectedZone = Player.Territory;
        ImGui.SameLine();
        if(ImGui.RadioButton(current ? "Select Zone...".Loc() : ExcelTerritoryHelper.GetName(SelectedZone), SelectedZone != 0 && !current))
        {
            new TerritorySelector(current ? Player.Territory : SelectedZone, (selector, zone) =>
            {
                SelectedZone = zone;
                selector.Close();
            }, "Select Zone".Loc())
            {
                HiddenTerritories = Svc.Data.GetExcelSheet<TerritoryType>().Select(x => x.RowId).Where(t => !P.Config.LayoutsL.Any(l => l.Subconfigurations.Count > 0 && l.ZoneLockH.Contains((ushort)t))).ToArray(),
                SelectedCategory = TerritorySelector.Category.All,
            };
        }
        ImGui.Checkbox("Hide disabled layouts/scripts".Loc(), ref P.Config.ConfigurationsHideDisabled);
        ImGuiEx.SetNextItemFullWidth();
        if(ImGui.BeginCombo("##switchAll", "Switch all displayed layouts/scripts to configuration, if supported:".Loc(), ImGuiComboFlags.HeightLarge))
        {
            if(ImGui.Selectable("Default"))
            {
                requestedConfiguration = Guid.Empty.ToString();
            }
            foreach(var x in AvailableConfigurations)
            {
                if(ImGui.Selectable(x))
                {
                    requestedConfiguration = x;
                }
            }
            ImGui.EndCombo();
        }
        AvailableConfigurations.Clear();
        if(ImGuiEx.BeginDefaultTable("##Configurations", ["~" + "Layout Name".Loc(), "Zone".Loc(), "Selected Configuration".Loc()]))
        {
            foreach(var x in P.Config.LayoutsL)
            {
                if(SelectedZone != 0 && x.ZoneLockH.Count != 0 && !x.ZoneLockH.Contains((ushort)SelectedZone)) continue;
                if(P.Config.ConfigurationsHideDisabled && !x.Enabled) continue;
                if(x.Subconfigurations.Count > 0)
                {
                    foreach(var n in x.Subconfigurations)
                    {
                        if(n.Name != "" && !AvailableConfigurations.Contains(n.Name))
                        {
                            AvailableConfigurations.Add(n.Name);
                        }
                    }
                    if(requestedConfiguration != null)
                    {
                        if(x.Subconfigurations.TryGetFirst(v => v.Name == requestedConfiguration, out var switchTo))
                        {
                            x.SelectedSubconfigurationID = switchTo.Guid;
                        }
                        else if(requestedConfiguration == Guid.Empty.ToString())
                        {
                            x.SelectedSubconfigurationID = Guid.Empty;
                        }
                    }
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV(x.Enabled ? null : ImGuiColors.DalamudGrey3, $"{x.GetName()}");

                    ImGui.TableNextColumn();
                    if(x.ZoneLockH.Count == 0)
                    {
                        ImGuiEx.TextV($"All Zones".Loc());
                    }
                    else if(x.ZoneLockH.Count == 1)
                    {
                        ImGuiEx.TextV($"{ExcelTerritoryHelper.GetName(x.ZoneLockH.First())}");
                    }
                    else
                    {
                        ImGuiEx.TextV($"?? zones".Loc(x.ZoneLockH.Count));
                        ImGuiEx.Tooltip(x.ZoneLockH.Select(z => ExcelTerritoryHelper.GetName(z)).Print("\n"));
                    }

                    ImGui.TableNextColumn();
                    ImGui.PushID(x.GUID.ToString());
                    x.DrawLayoutConfigurations(false, (int)Math.Max(200, ImGui.GetContentRegionMax().X / 2.2f));
                    ImGui.PopID();
                }
            }
            ImGui.EndTable();
        }


        if(ImGuiEx.BeginDefaultTable("##ScriptConfigurations", ["~" + "Script Name".Loc(), "Zone".Loc(), "Selected Configuration".Loc()]))
        {
            var toReload = new HashSet<SplatoonScript>();
            foreach(var script in ScriptingProcessor.Scripts.OrderBy(x => x.InternalData.Namespace).ThenBy(x => x.InternalData.Name))
            {
                if(script.InternalData.Blacklisted) continue;
                if(SelectedZone != 0 && script.ValidTerritories != null && script.ValidTerritories.Count != 0 && !script.ValidTerritories.Contains(SelectedZone)) continue;
                if(P.Config.ConfigurationsHideDisabled && script.IsDisabledByUser) continue;
                if(script.TryGetAvailableConfigurations(out var configs) && configs.Count > 0)
                {
                    foreach(var n in configs)
                    {
                        if(n.Value != "" && !AvailableConfigurations.Contains(n.Value))
                        {
                            AvailableConfigurations.Add(n.Value);
                        }
                    }
                    if(requestedConfiguration != null && script.TryGetAvailableConfigurations(out var confList))
                    {
                        if(confList.FindKeysByValue(requestedConfiguration).TryGetFirst(out var confKey))
                        {
                            if(script.InternalData.CurrentConfigurationKey != confKey)
                            {
                                script.ApplyConfiguration(confKey, out var act);
                                if(act != null) toReload.Add(script);
                            }
                        }
                        else if(requestedConfiguration == Guid.Empty.ToString())
                        {
                            script.ApplyDefaultConfiguration(out var act);
                            if(act != null) toReload.Add(script);
                        }
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV(!script.IsDisabledByUser ? null : ImGuiColors.DalamudGrey3, $"{script.InternalData.Name.Replace("_", " ")}");

                    ImGui.TableNextColumn();
                    if((script.ValidTerritories?.Count ?? 0) == 0)
                    {
                        ImGuiEx.TextV($"All Zones".Loc());
                    }
                    else if(script.ValidTerritories.Count == 1)
                    {
                        ImGuiEx.TextV($"{ExcelTerritoryHelper.GetName(script.ValidTerritories.First())}");
                    }
                    else
                    {
                        ImGuiEx.TextV($"?? zones".Loc(script.ValidTerritories.Count));
                        ImGuiEx.Tooltip(script.ValidTerritories.Select(z => ExcelTerritoryHelper.GetName(z)).Print("\n"));
                    }

                    ImGui.TableNextColumn();
                    ImGui.PushID(script.InternalData.FullName);
                    script.DrawConfigurationSelector((int)Math.Max(200, ImGui.GetContentRegionMax().X / 2.2f));
                    ImGui.PopID();
                }
            }
            ImGui.EndTable();

            if(toReload.Count > 0)
            {
                ScriptingProcessor.ReloadScripts(toReload, false);
            }
        }
    }
}
