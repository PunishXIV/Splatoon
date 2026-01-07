using Dalamud.Interface.Colors;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.LanguageHelpers;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace Splatoon.Gui.Tabs;

public unsafe static class TabProjection
{
    public static void Draw()
    {
        ImGuiEx.TextWrapped($"""
            Splatoon Projection allows you to see limited amount of attacks without having to import/create layouts.
            - Likely only basic attacks will be shown
            - Some attacks may be shown incorrectly, sometimes stuff that already telegraphed will be highlighted. It's strongly recommended to have a macro with "/splatoon pbl" command to instantly blacklist all currently displayed projected casts. Then you can send them to be included into plugin. 
            - If a layout imported and enabled for current attack, it will not be processed by projection. 
            """);
        var impact = (S.Projection.LastSw * 1000.0) / Stopwatch.Frequency;
        ImGuiEx.Text($"Projection Performance Impact: {impact:F1}ms");
        ImGui.SameLine();
        if(impact < 0.25f)
        {
            ImGuiEx.Text(EColor.GreenBright, "Good");
        }
        else if(impact < 1f)
        {
            ImGuiEx.Text(EColor.YellowBright, "Acceptable");
        }
        else
        {
            ImGuiEx.Text(EColor.RedBright, "Warning");
        }
        ImGuiEx.Checkbox("Render Splatoon Projection by default", ref P.Config.EnableProjection);
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

        ImGuiEx.Text("Currently projectable actions:");
        if(ImGuiEx.BeginDefaultTable("ProjAct", ["~Action", "Caster", "NameId", "DataId", "Blacklisted", "Blacklisting Ls", "Whitelisting Ls", "Suppressing Ls", "##Control"]))
        {
            foreach(var x in S.Projection.ProjectingItems)
            {
                var col = x.Rendered;
                if(col) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.ParsedGreen);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.Text(ExcelActionHelper.GetActionName(x.Descriptor.Id, true));
                ImGui.TableNextColumn();
                var obj = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(o => o.EntityId == x.CasterObjectID);
                if(obj != null)
                {
                    ImGuiEx.Text($"{obj.Name}");
                    ImGui.TableNextColumn();
                    ImGuiEx.Text($"{obj.NameId}");
                    ImGui.TableNextColumn();
                    ImGuiEx.Text($"{obj.DataId}");
                }
                else
                {
                    ImGuiEx.Text("Unknown?");
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                }
                ImGui.TableNextColumn();
                ImGuiEx.Text($"{x.IsBlacklisted}");
                ImGui.TableNextColumn();
                ImGuiEx.Text(x.BlacklistingLayouts.Print("\n"));
                ImGui.TableNextColumn();
                ImGuiEx.Text(x.WhitelistingLayouts.Print("\n"));
                ImGui.TableNextColumn();
                ImGuiEx.Text(x.SuppressingLayouts.Print("\n"));
                ImGui.TableNextColumn();
                if(!P.Config.ProjectionBlacklistedActions.Any(a => a.Action == x.Descriptor.Id))
                {
                    if(ImGuiEx.SmallIconButton(FontAwesomeIcon.Times))
                    {
                        P.Config.ProjectionBlacklistedActions.Add(new()
                        {
                            Action = x.Descriptor.Id,
                            DataId = obj?.DataId ?? 0,
                            ModelId = (uint)(obj != null ? obj.Struct()->ModelContainer.ModelCharaId : 0),
                            NameId = obj?.NameId ?? 0,
                            Territory = Player.Territory,
                        });
                    }
                }
                else
                {
                    if(ImGuiEx.SmallIconButton(FontAwesomeIcon.TrashRestore))
                    {
                        P.Config.ProjectionBlacklistedActions.RemoveAll(a => a.Action == x.Descriptor.Id);
                    }
                }

                if(col) ImGui.PopStyleColor();
            }
            ImGui.EndTable();
        }

        ImGui.Separator();

        ImGuiEx.Text("Blacklisted Actions:");
        if(ImGuiEx.BeginDefaultTable("Blac", ["~Action", "Zone", "Caster", "Data", "Model", "##control"]))
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
