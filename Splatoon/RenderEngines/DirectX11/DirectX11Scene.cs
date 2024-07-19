using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Splatoon.RenderEngines.DirectX11.Render;
using Splatoon.Structures;


namespace Splatoon.RenderEngines.DirectX11;

internal unsafe class DirectX11Scene : IDisposable
{
    private readonly TimeSpan ErrorLogFrequency = TimeSpan.FromSeconds(30);
    private const int RADIAL_SEGMENTS_PER_RADIUS_UNIT = 20;
    private const int MINIMUM_CIRCLE_SEGMENTS = 24;
    public const int MAXIMUM_CIRCLE_SEGMENTS = 240;
    private Renderer renderer;
    private AutoClipZones autoClipZones;
    private int uid = 0;
    private DateTime lastErrorLogTime = DateTime.MinValue;
    private DirectX11Renderer DirectX11Renderer;
    public DirectX11Scene(DirectX11Renderer dx11renderer)
    {
        this.DirectX11Renderer = dx11renderer;
        renderer = new Renderer();
        autoClipZones = new AutoClipZones(renderer);
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
        var angularPercent = angleRadians / (MathF.PI * 2);
        var segments = (int)(RADIAL_SEGMENTS_PER_RADIUS_UNIT * radius * angularPercent);
        var minimumSegments = Math.Max((int)(MINIMUM_CIRCLE_SEGMENTS * angularPercent), 1);
        var maximumSegments = Math.Max((int)(MAXIMUM_CIRCLE_SEGMENTS * angularPercent), 1);
        return Math.Clamp(segments, minimumSegments, maximumSegments);
    }

    private void Draw()
    {
        if (P.Profiler.Enabled) P.Profiler.Gui.StartTick();
        try
        {
            if (Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Svc.Condition[ConditionFlag.WatchingCutscene78])
            {
                return;
            }

            var fadeMiddleWidget = (AtkUnitBase*)Svc.GameGui.GetAddonByName("FadeMiddle", 1);
            var fadeBlackWidget = (AtkUnitBase*)Svc.GameGui.GetAddonByName("FadeBlack", 1);
            if ((fadeMiddleWidget != null && fadeMiddleWidget->IsVisible) ||
                (fadeBlackWidget != null && fadeBlackWidget->IsVisible))
            {
                return;
            }

            uid = 0;
            try
            {
                if (P.Profiler.Enabled) P.Profiler.GuiDirect3d.StartTick();
                var renderTarget = Direct3DDraw();
                if (P.Profiler.Enabled) P.Profiler.GuiDirect3d.StopTick();

                void Draw()
                {
                    // Draw pre-rendered shape fills.
                    ImGui.GetWindowDrawList().AddImage(renderTarget.ImguiHandle, ImGuiHelpers.MainViewport.Pos, ImGuiHelpers.MainViewport.Pos + new Vector2(renderTarget.Size.X, renderTarget.Size.Y));

                    // Draw dots and text last because they are most critical to be legible.
                    foreach (var element in DirectX11Renderer.DisplayObjects)
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
                ImGui.Begin("Splatoon DirectX11 Scene", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar
                    | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoSavedSettings);
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
                var now = DateTime.Now;
                if (now - lastErrorLogTime > ErrorLogFrequency)
                {
                    lastErrorLogTime = now;
                    P.Log("Splatoon exception: please report it to developer", true);
                    P.Log(e.Message, true);
                    P.Log(e.StackTrace, true);
                }
            }
        }
        catch (Exception e)
        {
            P.Log("Caught exception: " + e.Message, true);
            P.Log(e.StackTrace, true);
        }
        if (P.Profiler.Enabled) P.Profiler.Gui.StopTick();
    }

    private RenderTarget Direct3DDraw()
    {
        try
        {
            renderer.BeginFrame();
            foreach (var element in DirectX11Renderer.DisplayObjects)
            {
                if (element is DisplayObjectFan elementFan)
                {
                    var totalAngle = elementFan.angleMax - elementFan.angleMin;
                    var segments = RadialSegments(elementFan.outerRadius, totalAngle);
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
            if (P.Config.AutoClipNativeUI) autoClipZones.Update();
        }
        catch (Exception e)
        {
            var now = DateTime.Now;
            if (now - lastErrorLogTime > ErrorLogFrequency)
            {
                if (e is IndexOutOfRangeException)
                {
                    lastErrorLogTime = now;
                    P.Log("Splatoon exception: " + e.Message + " Please adjust misconfigured presets causing excessive elements, or report it to developer if you believe this limit is too low.", true);
                }
                else
                {
                    throw;
                }
            }
        }

        return renderer.EndFrame();
    }

    public void DrawTextWorld(DisplayObjectText e)
    {
        if (Svc.GameGui.WorldToScreen(
                        new Vector3(e.x, e.z, e.y),
                        out var pos))
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
        if (Svc.GameGui.WorldToScreen(new Vector3(e.x, e.y, e.z), out var pos))
            ImGui.GetWindowDrawList().AddCircleFilled(
            new Vector2(pos.X, pos.Y),
            e.thickness,
            ImGui.GetColorU32(e.color),
            MINIMUM_CIRCLE_SEGMENTS);
    }
}
