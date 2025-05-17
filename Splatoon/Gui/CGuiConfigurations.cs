using Dalamud.Interface.Colors;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods.TerritorySelection;
using Lumina.Excel.Sheets;
using Splatoon.Gui.Layouts.Header.Sections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Gui;
internal static class CGuiConfigurations
{
    static uint SelectedZone = 0;

    public static void Draw()
    {
        if(ImGui.RadioButton("All Zones", SelectedZone == 0)) SelectedZone = 0;
        var current = SelectedZone == 0 || SelectedZone == Player.Territory;

        if(ImGui.RadioButton(ExcelTerritoryHelper.GetName(Player.Territory), SelectedZone == Player.Territory)) SelectedZone = Player.Territory;

        if(ImGui.RadioButton(current ? "Select Zone...":ExcelTerritoryHelper.GetName(SelectedZone), SelectedZone != 0 && !current))
        {
            new TerritorySelector(current ? Player.Territory : SelectedZone, (selector, zone) =>
            {
                SelectedZone = zone;
                selector.Close();
            }, "Select Zone")
            {
                HiddenTerritories = Svc.Data.GetExcelSheet<TerritoryType>().Select(x => x.RowId).Where(t => !P.Config.LayoutsL.Any(l => l.Subconfigurations.Count > 0 && l.ZoneLockH.Contains((ushort)t))).ToArray(),
                SelectedCategory = TerritorySelector.Category.All,
            };
        }
        if(ImGuiEx.BeginDefaultTable("##Configurations", ["~Layout Name", "Zone", "Selected Configuration"]))
        {
            foreach(var x in P.Config.LayoutsL)
            {
                if(SelectedZone != 0 && x.ZoneLockH.Count != 0 && !x.ZoneLockH.Contains((ushort)SelectedZone)) continue;
                if(x.Subconfigurations.Count > 0)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV(x.Enabled?null:ImGuiColors.DalamudGrey3,$"{x.GetName()}");

                    ImGui.TableNextColumn();
                    if(x.ZoneLockH.Count == 0)
                    {
                        ImGuiEx.TextV($"All Zones");
                    }
                    else if(x.ZoneLockH.Count == 1)
                    {
                        ImGuiEx.TextV($"{ExcelTerritoryHelper.GetName(x.ZoneLockH.First())}");
                    }
                    else
                    {
                        ImGuiEx.TextV($"{x.ZoneLockH.Count} zones");
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
    }
}
