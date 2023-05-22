using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Gui.Layouts.Header.Sections
{
    internal static class SceneSelector
    {
        static int NewScene = 0;
        internal static void DrawSceneSelector(this Layout l)
        {
            ImGuiEx.SetNextItemFullWidth();
            if (ImGui.BeginCombo("##SceneSelector", l.Scenes.Count > 0 ? l.Scenes.Print() : "Any scene"))
            {
                ImGui.SetNextItemWidth(150f);
                ImGui.InputInt("##scenenum", ref NewScene, 1, 1);
                ImGui.SameLine();
                if (ImGui.Button("Add"))
                {
                    l.Scenes.Add(NewScene);
                }
                var toRem = -1;
                foreach(var sc in l.Scenes)
                {
                    ImGuiEx.Text($"{sc}");
                    ImGui.SameLine();
                    if (ImGui.SmallButton("Delete##scdel"+sc))
                    {
                        toRem = sc;
                    }
                }
                if (toRem > -1)
                {
                    l.Scenes.Remove(toRem);
                }
                ImGui.EndCombo();
            }
        }
    }
}
