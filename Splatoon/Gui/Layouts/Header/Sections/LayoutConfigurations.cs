using Splatoon.Serializables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Gui.Layouts.Header.Sections;
internal static class LayoutConfigurations
{
    private static ImGuiEx.RealtimeDragDrop<LayoutSubconfiguration> DragDrop = new("LayoutSubDD", x => x.Guid.ToString(), true);
    internal static void DrawLayoutConfigurations(this Layout layout, bool allowAddition = true, int width = 0)
    {
        try
        {
            foreach(var x in layout.Subconfigurations)
            {
                if(layout.Subconfigurations.Count(c => c.Guid == x.Guid) > 1)
                {
                    x.Guid = Guid.NewGuid();
                }
            }
            var selectedConf = layout.Subconfigurations.FirstOrDefault(x => x.Guid == layout.SelectedSubconfigurationID);

            if(selectedConf == null && layout.SelectedSubconfigurationID != Guid.Empty)
            {
                layout.SelectedSubconfigurationID = Guid.Empty;
            }
            if(width == 0)
            {
                ImGuiEx.SetNextItemFullWidth();
            }
            else
            {
                ImGui.SetNextItemWidth(width);
            }
            ImGuiEx.InputWithRightButtonsArea($"ComboLC{layout.GUID}", () =>
            {
                if(ImGui.BeginCombo("##layoutConfiguration", selectedConf.GetName(layout), ImGuiComboFlags.HeightLarge))
                {
                    if(ImGui.Selectable($"Default Configuration", selectedConf == null))
                    {
                        layout.SelectedSubconfigurationID = Guid.Empty;
                    }
                    ImGui.Separator();
                    DragDrop.Begin();
                    for(var i = 0; i < layout.Subconfigurations.Count; i++)
                    {
                        var conf = layout.Subconfigurations[i];
                        if(allowAddition)
                        {
                            DragDrop.NextRow();
                            DragDrop.DrawButtonDummy(conf, layout.Subconfigurations, i);
                            ImGui.SameLine();
                        }
                        var col = DragDrop.SetRowColor(conf, false);
                        if(col) ImGui.PushStyleColor(ImGuiCol.Header, DragDrop.HighlightColor);
                        if(ImGui.Selectable($"{conf.GetName(layout)}##{conf.Guid}", col || conf.Guid == selectedConf?.Guid))
                        {
                            layout.SelectedSubconfigurationID = conf.Guid;
                        }
                        if(col) ImGui.PopStyleColor();
                    }
                    DragDrop.End();
                    if(allowAddition)
                    {
                        ImGui.Separator();
                        ImGuiEx.Text(UiBuilder.IconFont, FontAwesomeIcon.Plus.ToIconString());
                        ImGui.SameLine();
                        if(ImGui.Selectable($"Add New based on Default Configuration"))
                        {
                            var newConf = new LayoutSubconfiguration
                            {
                                Elements = layout.ElementsL.JSONClone()
                            };
                            layout.Subconfigurations.Add(newConf);
                            layout.SelectedSubconfigurationID = newConf.Guid;
                        }
                        if(selectedConf != null)
                        {
                            ImGuiEx.Text(UiBuilder.IconFont, FontAwesomeIcon.Clone.ToIconString());
                            ImGui.SameLine();

                            if(ImGui.Selectable($"Add New based on {selectedConf.GetName(layout)}"))
                            {
                                var newConf = selectedConf.JSONClone();
                                newConf.Guid = Guid.NewGuid();
                                if(newConf.Name != "")
                                {
                                    for(var i = 1; i < 999; i++)
                                    {
                                        var newName = newConf.Name + (i == 1 ? $" (copy)" : $" (copy {i})");
                                        if(!layout.Subconfigurations.Any(n => n.Name == newName))
                                        {
                                            newConf.Name = newName;
                                            break;
                                        }
                                    }
                                }
                                layout.Subconfigurations.Add(newConf);
                                layout.SelectedSubconfigurationID = newConf.Guid;
                            }
                        }
                    }
                    ImGui.EndCombo();
                }
            }, () =>
            {
                if(allowAddition)
                {
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl && selectedConf != null))
                    {
                        new TickScheduler(() => layout.Subconfigurations.Remove(selectedConf));
                    }
                    ImGuiEx.Tooltip(selectedConf == null ? "Default Configuration can not be removed" : "Hold CTRL and click to remove configuration");
                }
            });
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    internal static void DrawLayoutConfigurationName(this Layout layout)
    {
        try
        {
            var selectedConf = layout.Subconfigurations.FirstOrDefault(x => x.Guid == layout.SelectedSubconfigurationID);
            if(selectedConf != null)
            {
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputTextWithHint("##confName", selectedConf.GetName(layout), ref selectedConf.Name, 100);
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }
}
