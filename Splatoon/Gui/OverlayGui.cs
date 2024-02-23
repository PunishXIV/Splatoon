using Dalamud.Game.ClientState.Conditions;
using Splatoon.Structures;
using Splatoon.Render;


namespace Splatoon.Gui;

unsafe class OverlayGui : IDisposable
{
    const int RADIAL_SEGMENTS_PER_RADIUS_UNIT = 20;
    const int MINIMUM_CIRCLE_SEGMENTS = 24;
    public const int MAXIMUM_CIRCLE_SEGMENTS = 240;

    Renderer renderer;
    AutoClipZones autoClipZones;
    readonly Splatoon p;
    int uid = 0;
    public OverlayGui(Splatoon p)
    {
        this.p = p;
        renderer = new Renderer();
        autoClipZones = new AutoClipZones(renderer, p);
        Svc.PluginInterface.UiBuilder.Draw += Draw;
    }

    public void Dispose()
    {
        renderer.Dispose();
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
    }

    // Dynamic LoD for circles and cones
    // TODO it would be would be more efficient to adjust based on camera distance
    public static int RadialSegments(float radius, float angleRadians = MathF.PI * 2)
    {
        float angularPercent = angleRadians / (MathF.PI * 2);
        int segments = (int)(RADIAL_SEGMENTS_PER_RADIUS_UNIT * radius * angularPercent);
        int minimumSegments = Math.Max((int)(MINIMUM_CIRCLE_SEGMENTS * angularPercent), 1);
        int maximumSegments = Math.Max((int)(MAXIMUM_CIRCLE_SEGMENTS * angularPercent), 1);
        return Math.Clamp(segments, minimumSegments, maximumSegments);
    }

    void Draw()
    {
        if (p.Profiler.Enabled) p.Profiler.Gui.StartTick();
        try
        {
            if (Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Svc.Condition[ConditionFlag.WatchingCutscene78])
            {
                return;
            }

            uid = 0;
            try
            {
                if (p.Profiler.Enabled) p.Profiler.GuiDirect3d.StartTick();
                RenderTarget renderTarget = Direct3DDraw();
                if (p.Profiler.Enabled) p.Profiler.GuiDirect3d.StopTick();

                void Draw()
                {
                    // Draw pre-rendered shape fills.
                    ImGui.GetWindowDrawList().AddImage(renderTarget.ImguiHandle, new(), new(renderTarget.Size.X, renderTarget.Size.Y));

                    // Draw dots and text last because they are most critical to be legible.
                    foreach (var element in p.displayObjects)
                    {
                        if (element is DisplayObjectDot elementDot)
                        {
                            DrawPoint(elementDot);
                        }
                        if (element is DisplayObjectText elementText)
                        {
                            DrawTextWorld(elementText);
                        }
                    }
                }

                ImGuiHelpers.ForceNextWindowMainViewport();
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
                ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);
                ImGui.Begin("Splatoon scene", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar
                    | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.AlwaysUseWindowPadding);
                if (P.Config.SplatoonLowerZ)
                {
                    CImGui.igBringWindowToDisplayBack(CImGui.igGetCurrentWindow());
                }
                if (P.Config.RenderableZones.Count == 0 || !P.Config.RenderableZonesValid)
                {
                    Draw();
                }
                else
                {
                    foreach (var e in P.Config.RenderableZones)
                    {
                        ImGui.PushClipRect(new Vector2(e.Rect.X, e.Rect.Y), new Vector2(e.Rect.Right, e.Rect.Bottom), false);
                        Draw();
                        ImGui.PopClipRect();
                    }
                }
                ImGui.End();
                ImGui.PopStyleVar();
            }
            catch (Exception e)
            {
                p.Log("Splatoon exception: please report it to developer", true);
                p.Log(e.Message, true);
                p.Log(e.StackTrace, true);
            }
        }
        catch (Exception e)
        {
            p.Log("Caught exception: " + e.Message, true);
            p.Log(e.StackTrace, true);
        }
        if (p.Profiler.Enabled) p.Profiler.Gui.StopTick();
    }

    RenderTarget Direct3DDraw()
    {
        renderer.BeginFrame();
        foreach (var element in p.displayObjects)
        {
            if (element is DisplayObjectFan elementFan)
            {
                float totalAngle = elementFan.angleMax - elementFan.angleMin;
                int segments = RadialSegments(elementFan.outerRadius, totalAngle);
                renderer.DrawFan(elementFan, segments);
            }
            else if (element is DisplayObjectLine elementLine)
            {
                renderer.DrawLine(elementLine);
            }
        }
        foreach (var zone in P.Config.ClipZones)
        {
            renderer.AddClipZone(zone.Rect);
        }
        if (p.Config.AutoClipNativeUI) autoClipZones.Update();

        return renderer.EndFrame();
    }

    public void DrawTextWorld(DisplayObjectText e)
    {
        if (Svc.GameGui.WorldToScreen(
                        new Vector3(e.x, e.z, e.y),
                        out Vector2 pos))
        {
            DrawText(e, pos);
        }
    }

    public void DrawText(DisplayObjectText e, Vector2 pos)
    {
        var scaled = e.fscale != 1f;
        var size = scaled ? ImGui.CalcTextSize(e.text) * e.fscale : ImGui.CalcTextSize(e.text);
        size = new Vector2(size.X + 10f, size.Y + 10f);
        ImGui.SetNextWindowPos(new Vector2(pos.X - size.X / 2, pos.Y - size.Y / 2));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 10f);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.ColorConvertU32ToFloat4(e.bgcolor));
        ImGui.BeginChild("##child" + e.text + ++uid, size, false,
            ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysUseWindowPadding);
        ImGui.PushStyleColor(ImGuiCol.Text, e.fgcolor);
        if (scaled) ImGui.SetWindowFontScale(e.fscale);
        ImGuiEx.Text(e.text);
        if (scaled) ImGui.SetWindowFontScale(1f);
        ImGui.PopStyleColor();
        ImGui.EndChild();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    public void DrawPoint(DisplayObjectDot e)
    {
        if (Svc.GameGui.WorldToScreen(new Vector3(e.x, e.y, e.z), out Vector2 pos))
            ImGui.GetWindowDrawList().AddCircleFilled(
            new Vector2(pos.X, pos.Y),
            e.thickness,
            ImGui.GetColorU32(e.color),
            MINIMUM_CIRCLE_SEGMENTS);
    }
}
