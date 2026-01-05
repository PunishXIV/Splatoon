using ECommons.ExcelServices;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Splatoon.Gui.Tabs;

public static class TabProjection
{
    public static void Draw()
    {
        ImGuiEx.TextWrapped($"""
            Splatoon Projection allows you to see limited amount of attacks without having to import/create layouts.
            - Likely only basic attacks will be shown
            - Some attacks may be shown incorrectly, sometimes stuff that already telegraphed will be highlighted. It's strongly recommended to have a macro with "/splatoon pbl" command to instantly blacklist all currently displayed projected casts. Then you can send them to be included into plugin. 
            - If a layout imported and enabled for current attack, it will not be processed by projection. 
            """);
        ImGuiEx.Checkbox("Enable Splatoon Projection", ref P.Config.EnableProjection);
        ImGui.ColorEdit4("Color 1", ref P.Config.ProjectionColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Color 2", ref P.Config.ProjectionColor2, ImGuiColorEditFlags.NoInputs);
        ImGui.SetNextItemWidth(150f);
        ImGui.SliderFloat("Fill Intensity", ref P.Config.ProjectionFillIntensity, 0f, 1f);
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.SliderIntAsFloat("Pulse duration, seconds", ref P.Config.ProjectionPulseTime.ValidateRange(100, 10000), 500, 2000);
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("Cast Animation", ref P.Config.ProjectionCastAnimation);
        ImGui.ColorEdit4("Cast Animation Color 1", ref P.Config.AnimationColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Cast Animation Color 2", ref P.Config.AnimationColor2, ImGuiColorEditFlags.NoInputs);

        ImGui.Separator();

        ImGuiEx.Text("Blacklisted Actions:");
        if(ImGuiEx.BeginDefaultTable(["~Action", "Zone", "Caster", "Data", "Model", "##control"]))
        {
            foreach(var x in P.Config.ProjectionBlacklistedActions.OrderBy(x => x.Territory))
            {
                ImGui.PushID(x.Action.ToString());
                ImGui.TableNextRow();
                ImGuiEx.SimpleTableTextColumns(
                    ExcelActionHelper.GetActionName(x.Action, true),
                    ExcelTerritoryHelper.GetName(x.Territory, true),
                    $"{BNpcName.GetRef(x.NameId).ValueNullable?.Singular.ToString()} #{x.NameId}",
                    $"{x.DataId}",
                    $"{x.ModelId}");
                ImGui.TableNextColumn();
                if(ImGuiEx.SmallIconButton(FontAwesomeIcon.Trash))
                {
                    new TickScheduler(() => P.Config.ProjectionBlacklistedActions.Remove(x));
                }
                ImGui.PopID();
            } 
            ImGui.EndTable();
        }
    }
}
