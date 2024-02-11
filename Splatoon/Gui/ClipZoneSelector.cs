using Dalamud.Interface.Windowing;
using Splatoon.Utils;

namespace Splatoon.Gui;

internal class ClipZoneSelector : Window
{
    public ClipZoneSelector() : base("Splatoon clip zone selector", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoSavedSettings, true)
    {
        this.RespectCloseHotkey = false;
        this.Position = Vector2.Zero;
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);
    }

    volatile int upd = -1;
    int selected = -1;
    int bringToFront = -1;
    int mainIndex = 0;

    public override void OnOpen()
    {
        upd = -1;
        selected = -1;
    }

    public override void PostDraw()
    {
        var toRem = -1;
        for (int i = 0; i < P.Config.ClipZones.Count; i++)
        {
            var invalidText = string.Empty;
            var r = P.Config.ClipZones[i];
            ImGuiHelpers.ForceNextWindowMainViewport();
            var cond = upd == i ? ImGuiCond.Always : ImGuiCond.Appearing;
            if(upd == i)
            {
                upd = -1;
            }
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new(r.Rect.X, r.Rect.Y), cond);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.SetNextWindowSize(new(r.Rect.Width, r.Rect.Height), cond);
            ImGui.SetNextWindowSizeConstraints(new(10, 10), ImGuiHelpers.MainViewport.Size);
            var col = GradientColor.Get(ImGuiEx.Vector4FromRGBA(0x00ff0077), ImGuiEx.Vector4FromRGBA(0x00770077));
            if (r.Rect.X < 0 || r.Rect.Y < 0 || r.Rect.Bottom > ImGuiHelpers.MainViewport.Size.Y || r.Rect.Right > ImGuiHelpers.MainViewport.Size.X)
            {
                invalidText = "     Zone should be within screen boundaries";
                col = GradientColor.Get(ImGuiEx.Vector4FromRGBA(0xff000077), ImGuiEx.Vector4FromRGBA(0x77000077));
            }
            if(selected == i)
            {
                if (invalidText == "")
                {
                    col = GradientColor.Get(ImGuiEx.Vector4FromRGBA(0x00ffff77), ImGuiEx.Vector4FromRGBA(0x00777777));
                }
                else
                {
                    col = GradientColor.Get(ImGuiEx.Vector4FromRGBA(0xff00ff77), ImGuiEx.Vector4FromRGBA(0x77007777));
                }
                selected = -1;
            }
            ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGuiEx.Vector4FromRGBA(Colors.Transparent));
            if (i == bringToFront)
            {
                bringToFront = -1;
                ImGui.SetNextWindowFocus();
            }
            if (ImGui.Begin($"##renderable{r.GUID}", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoScrollbar))
            {
                if (ImGui.IsWindowFocused())
                {
                    selected = i;
                }
                if (CImGui.igFindWindowDisplayIndex(CImGui.igGetCurrentWindow()) < mainIndex)
                {
                    CImGui.igBringWindowToDisplayFront(CImGui.igGetCurrentWindow());
                }
                if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    ImGui.OpenPopup($"Renderable{r.GUID}");
                }
                var x = (int)ImGui.GetWindowPos().X;
                var y = (int)ImGui.GetWindowPos().Y;
                var w = (int)ImGui.GetWindowSize().X;
                var h = (int)ImGui.GetWindowSize().Y;
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
                if (ImGui.BeginPopup($"Renderable{r.GUID}"))
                {
                    selected = i;

                    ImGui.SetNextItemWidth(150f);
                    if (ImGui.InputInt("X", ref x, 1, 10)) upd = i;
                    ImGui.SetNextItemWidth(150f);
                    if (ImGui.InputInt("Y", ref y, 1, 10)) upd = i;
                    ImGui.SetNextItemWidth(150f);
                    if (ImGui.InputInt("Width", ref w, 1, 10)) upd = i;
                    ImGui.SetNextItemWidth(150f);
                    if (ImGui.InputInt("Height", ref h, 1, 10)) upd = i;
                    if (w < 10) w = 10;
                    if (h < 10) h = 10;

                    ImGui.Separator();

                    HandlePopupMenu();
                    ImGui.Separator();
                    if (ImGui.Selectable("Delete this zone"))
                    {
                        toRem = i;
                        //DuoLog.Information($"to remove: {toRem} ({i})");
                    }
                    ImGui.EndPopup();
                }
                ImGui.PopStyleVar();
                ImGui.Dummy(new(15,15));
                if (invalidText != "")
                {
                    ImGuiEx.TextWrapped(invalidText);
                }
                ImGuiEx.Text($"   Zone {i}\n   Right-click to open menu.");
                if (w < 10) w = 10;
                if (h < 10) h = 10;
                P.Config.ClipZones[i].Rect = new(x,y,w,h);

                var bCol = (col with { W = 1f }).ToUint();
                ImGui.GetWindowDrawList().AddLine(new(x, y), new(x + w, y), bCol, 5f);
                ImGui.GetWindowDrawList().AddLine(new(x, y), new(x, y + h), bCol, 5f);
                ImGui.GetWindowDrawList().AddLine(new(x+w-1, y+h-1), new(x + w-1, y), bCol, 5f);
                ImGui.GetWindowDrawList().AddLine(new(x + w - 1, y + h-1), new(x, y + h-1), bCol, 5f);
            }
            ImGui.End();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }
        if (toRem > -1)
        {
            P.Config.ClipZones.RemoveAt(toRem);
        }
        ImGui.PopStyleVar(2);
    }

    public override void Draw()
    {
        mainIndex = CImGui.igFindWindowDisplayIndex(CImGui.igGetCurrentWindow());
        ImGui.Dummy(new(20f, 20f));

        ImGuiEx.Text($"       You have {P.Config.ClipZones.Count} clip zones.");
        ImGuiEx.Text($"       Right click to bring context menu.");
        if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("Main czs popup");
        }
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5f, 5f));
        if (ImGui.BeginPopup("Main czs popup"))
        {
            HandlePopupMenu();
            ImGui.EndPopup();
        }
        ImGui.PopStyleVar();
    }

    void HandlePopupMenu()
    {
        if (P.Config.ClipZones.Count < MAX_CLIP_ZONES)
        {
            if (ImGui.Selectable("Add new zone"))
            {
                P.Config.ClipZones.Add(new(100, 100, 300, 300));
            }
            ImGui.Separator();
        }
        for(int i = 0; i < P.Config.ClipZones.Count; i++)
        {
            if(ImGui.Selectable($"Select zone {i}")) bringToFront = i;
        }
        ImGui.Separator();
        if (ImGui.Selectable("Save configuration and exit"))
        {
            this.IsOpen = false;
            P.Config.Save();
        }
    }
}
