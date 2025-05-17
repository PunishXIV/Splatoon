using Splatoon.Serializables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Gui.Layouts.Header.Sections;
internal static class LayoutConfigurations
{
    internal static void DrawLayoutConfigurations(this Layout layout, bool allowAddition = true, int width = 0)
    {
        try
        {
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
            if(ImGui.BeginCombo("##layoutConfiguration", selectedConf.GetName(layout), ImGuiComboFlags.HeightLarge))
            {
                if(ImGui.Selectable($"Default Configuration"))
                {
                    layout.SelectedSubconfigurationID = Guid.Empty;
                }
                foreach(var conf in layout.Subconfigurations)
                {
                    if(ImGui.Selectable($"{conf.GetName(layout)}##{conf.Guid}"))
                    {
                        layout.SelectedSubconfigurationID = conf.Guid;
                    }
                }
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
                            layout.Subconfigurations.Add(newConf);
                            layout.SelectedSubconfigurationID = newConf.Guid;
                        }
                    }
                }
                ImGui.EndCombo();
            }
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
