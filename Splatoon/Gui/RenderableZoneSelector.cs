using Dalamud.Interface.Windowing;
using ECommons.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Gui
{
    internal class RenderableZoneSelector : Window
    {
        public RenderableZoneSelector() : base("Splatoon renderable zone selector", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoSavedSettings, true)
        {
            this.RespectCloseHotkey = false;
            this.Position = Vector2.Zero;
            P.Config.RenderableZonesValid = AreAllValid();
        }

        public override void PreDraw()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);
        }

        volatile int upd = -1;
        int selected = -1;
        int mainIndex = 0;

        public override void OnOpen()
        {
            upd = -1;
            selected = -1;
        }

        internal bool AreAllValid()
        {
            for (int i = 0; i < P.Config.RenderableZones.Count; i++)
            {
                var r = P.Config.RenderableZones[i];
                if (r.Rect.X < 0 || r.Rect.Y < 0 || r.Rect.Bottom > ImGuiHelpers.MainViewport.Size.Y || r.Rect.Right > ImGuiHelpers.MainViewport.Size.X)
                {
                    return false;
                }
                for (int j = 0; j < P.Config.RenderableZones.Count; j++)
                {
                    if (i == j) continue;
                    if (r.Rect.IntersectsWith(P.Config.RenderableZones[j].Rect))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override void PostDraw()
        {
            var toRem = -1;
            for (int i = 0; i < P.Config.RenderableZones.Count; i++)
            {
                var invalidText = string.Empty;
                var r = P.Config.RenderableZones[i];
                ImGuiHelpers.ForceNextWindowMainViewport();
                var cond = upd == i ? ImGuiCond.Always : ImGuiCond.Appearing;
                if(upd == i)
                {
                    upd = -1;
                }
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new(r.Rect.X, r.Rect.Y), cond);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
                ImGui.SetNextWindowSize(new(r.Rect.Width, r.Rect.Height), cond);
                ImGui.SetNextWindowSizeConstraints(new(100, 100), ImGuiHelpers.MainViewport.Size);
                var col = GradientColor.Get(ImGuiEx.Vector4FromRGBA(0x00ff0077), ImGuiEx.Vector4FromRGBA(0x00770077));
                if (r.Rect.X < 0 || r.Rect.Y < 0 || r.Rect.Bottom > ImGuiHelpers.MainViewport.Size.Y || r.Rect.Right > ImGuiHelpers.MainViewport.Size.X)
                {
                    invalidText = "Zone should be within screen boundaries";
                    col = GradientColor.Get(ImGuiEx.Vector4FromRGBA(0xff000077), ImGuiEx.Vector4FromRGBA(0x77000077));
                }
                for (int j = 0; j < P.Config.RenderableZones.Count; j++)
                {
                    if (i == j) continue;
                    if (r.Rect.IntersectsWith(P.Config.RenderableZones[j].Rect))
                    {
                        invalidText = "Different zones can not intersect";
                        col = GradientColor.Get(ImGuiEx.Vector4FromRGBA(0xff000077), ImGuiEx.Vector4FromRGBA(0x77000077));
                    }
                }
                if(selected == i)
                {
                    col = GradientColor.Get(ImGuiEx.Vector4FromRGBA(0xffff0077), ImGuiEx.Vector4FromRGBA(0x77770077));
                    selected = -1;
                }
                ImGui.PushStyleColor(ImGuiCol.WindowBg, col);
                if (ImGui.Begin($"##renderable{r.GUID}", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoScrollbar))
                {
                    //ImGuiEx.Text($"{CImGui.igFindWindowDisplayIndex(CImGui.igGetCurrentWindow())}");
                    //ImGuiEx.Text($"Zone {i}");
                    if (CImGui.igFindWindowDisplayIndex(CImGui.igGetCurrentWindow()) < mainIndex)
                    {
                        CImGui.igBringWindowToDisplayFront(CImGui.igGetCurrentWindow());
                    }
                    if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                    {
                        ImGui.OpenPopup($"Renderable{r.GUID}");
                    }
                    if (ImGui.BeginPopup($"Renderable{r.GUID}"))
                    {
                        selected = i;
                        if (ImGui.Selectable("Delete this zone"))
                        {
                            toRem = i;
                            //DuoLog.Information($"to remove: {toRem} ({i})");
                        }
                        ImGui.EndPopup();
                    }
                    var x = (int)ImGui.GetWindowPos().X;
                    var y = (int)ImGui.GetWindowPos().Y;
                    var w = (int)ImGui.GetWindowSize().X;
                    var h = (int)ImGui.GetWindowSize().Y;
                    if (invalidText != "")
                    {
                        ImGuiEx.TextWrapped(invalidText);
                    }
                    ImGui.SetNextItemWidth(60f);
                    if(ImGui.DragInt("X", ref x)) upd = i;
                    ImGui.SetNextItemWidth(60f);
                    if (ImGui.DragInt("Y", ref y)) upd = i; 
                    ImGui.SetNextItemWidth(60f);
                    if (ImGui.DragInt("Width", ref w)) upd = i; 
                    ImGui.SetNextItemWidth(60f);
                    if (ImGui.DragInt("Height", ref h)) upd = i;
                    if (w < 30) w = 30;
                    if (h < 30) h = 30;
                    P.Config.RenderableZones[i].Rect = new(x,y,w,h);
                }
                ImGui.End();
                ImGui.PopStyleColor();
                ImGui.PopStyleVar();
            }
            if (toRem > -1)
            {
                P.Config.RenderableZones.RemoveAt(toRem);
                //DuoLog.Information($"removing: {toRem}");
            }
            ImGui.PopStyleVar(2);
        }

        public override void Draw()
        {
            mainIndex = CImGui.igFindWindowDisplayIndex(CImGui.igGetCurrentWindow());
            //ImGuiEx.Text($"{CImGui.igFindWindowDisplayIndex(CImGui.igGetCurrentWindow())}");
            ImGui.Dummy(new(20f, 20f));
            P.Config.RenderableZonesValid = AreAllValid();
            if (!P.Config.RenderableZonesValid)
            {
                ImGuiEx.Text(GradientColor.Get(EColor.RedBright, EColor.YellowBright, 500), $"       Invalid configuration. Your settings will be ignored");
            }
            else
            {
                ImGuiEx.Text($"       You have {P.Config.RenderableZones.Count} renderable areas.");
                if (P.Config.RenderableZones.Count == 0)
                {
                    ImGuiEx.Text($"       Your whole screen will be used to render Splatoon draws.");
                }
                if (P.Config.RenderableZones.Count < 2)
                {
                    ImGuiEx.Text(EColor.GreenBright, "       Performance: untouched.");
                }
                else if (P.Config.RenderableZones.Count < 4)
                {
                    ImGuiEx.Text(EColor.OrangeBright, "       Performance: reduced. Systems with weaker CPU may suffer FPS issues.");
                }
                else
                {
                    ImGuiEx.Text(EColor.RedBright, "       Performance: severely degraded. You may experience serious FPS issues with your configuration.");
                }
            }
            ImGuiEx.Text($"       Right click to bring context menu.");
            if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup("Main rzs popup");
            }
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 10f));
            if (ImGui.BeginPopup("Main rzs popup"))
            {
                if(ImGui.Selectable("Add new zone"))
                {
                    P.Config.RenderableZones.Add(new(100, 100, 300, 300));
                }
                if(ImGui.Selectable("Save configuration and exit"))
                {
                    this.IsOpen = false;
                    P.Config.Save();
                }
                ImGui.EndPopup();
            }
            ImGui.PopStyleVar();
        }

        void HandlePopupMenu()
        {
            if(ImGui.IsMouseClicked(ImGuiMouseButton.Right)) 
            {
                
            }
        }
    }
}
