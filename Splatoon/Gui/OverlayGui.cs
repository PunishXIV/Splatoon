using Dalamud.Game.ClientState.Conditions;
using ImGuiNET;
using Splatoon.Structures;
using System.Runtime.InteropServices;


namespace Splatoon.Gui;

unsafe class OverlayGui : IDisposable
{
    static readonly Vector2 UV = ImGui.GetFontTexUvWhitePixel();
    // TODO make configurable
    // Low detail 2-3
    // Med detail 4-5
    // High detail 6+
    const int RADIAL_SEGMENTS_PER_UNIT = 4;
    const int MINIMUM_CIRCLE_SEGMENTS = 12;
    const int LINEAR_SEGMENTS_PER_UNIT = 1;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetMatrixSingletonDelegate();
    private GetMatrixSingletonDelegate _getMatrixSingleton { get; init; }

    public Matrix4x4 ViewProj { get; private set; }
    public Vector2 ViewportSize { get; private set; }

    readonly Splatoon p;
    int uid = 0;
    public OverlayGui(Splatoon p)
    {
        this.p = p;
        Svc.PluginInterface.UiBuilder.Draw += Draw;
        // Ripped from https://github.com/awgil/ffxiv_bossmod/blob/master/BossMod/Framework/Camera.cs#L32
        var funcAddress = Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
        _getMatrixSingleton = Marshal.GetDelegateForFunctionPointer<GetMatrixSingletonDelegate>(funcAddress);
    }

    public void Dispose()
    {
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
    }

    // Dynamic LoD for circles and cones
    // TODO it would be would be more efficient to adjust based on camera distance
    public static int RadialSegments(float radius, float angleRadians = MathF.PI * 2)
    {
        float circumference = angleRadians * radius;
        int segments = (int)(circumference * RADIAL_SEGMENTS_PER_UNIT);

        float angularPercent = angleRadians / (MathF.PI * 2);
        int minimumSegments = Math.Max((int)(MINIMUM_CIRCLE_SEGMENTS * angularPercent), 1);
        return Math.Max(segments, minimumSegments);
    }
    public static int HorizontalLinearSegments(float radius)
    {
        return Math.Max((int)(radius / LINEAR_SEGMENTS_PER_UNIT), 1);
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
            var matrixSingleton = _getMatrixSingleton();
            ViewProj = ReadMatrix(matrixSingleton + 0x1b4);
            ViewportSize = ReadVec2(matrixSingleton + 0x1f4);
            try
            {
                void Draw()
                {
                    foreach (var element in p.displayObjects)
                    {
                        if (element is DisplayObjectDonut elementDonut)
                        {
                            DrawDonutWorld(elementDonut);
                        }
                        else if (element is DisplayObjectFan elementFan)
                        {
                            DrawTriangleFanWorld(elementFan);
                        }
                    }
                    // Draw lines and dots second because they're hard to see when covered by another shape.
                    foreach (var element in p.displayObjects)
                    {
                        if (element is DisplayObjectLine elementLine)
                        {
                            DrawLineWorld(elementLine);
                        }
                        else if (element is DisplayObjectDot elementDot)
                        {
                            DrawPoint(elementDot);
                        }
                    }
                    // Draw text last because it's most critical top be legible.
                    foreach (var element in p.displayObjects)
                    {
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
                        //var trans = e.Trans != 1.0f;
                        //if (trans) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, e.Trans);
                        ImGui.PushClipRect(new Vector2(e.Rect.X, e.Rect.Y), new Vector2(e.Rect.Right, e.Rect.Bottom), false);
                        Draw();
                        ImGui.PopClipRect();
                        //if(trans)ImGui.PopStyleVar();
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


    public void DrawTriangleFanWorld(DisplayObjectFan e)
    {
        float totalAngle = e.angleMax - e.angleMin;
        int segments = RadialSegments(e.radius, totalAngle);
        float angleStep = totalAngle / segments;

        int vertexCount = segments + 1;

        bool isCircle = totalAngle == MathF.PI * 2;
        StrokeConnection strokeStyle = isCircle ? StrokeConnection.NoConnection : StrokeConnection.ConnectOriginAndEnd;
        RenderShape fan = new(e.style, VertexConnection.NoConnection, strokeStyle);
        for (int step = 0; step < vertexCount; step++)
        {
            float angle = e.angleMin + step * angleStep;
            Vector3 point = e.origin;
            point.Y += e.radius;
            point = RotatePoint(e.origin, angle, point);
            fan.Add(XZY(e.origin), XZY(point));
        }
        fan.Draw(ViewProj); 
    }

    public void DrawDonutWorld(DisplayObjectDonut e)
    {
        int segments = RadialSegments(e.radius + e.donutRadius);
        var worldPosInside = GetCircle(e.origin, e.radius, segments);
        var worldPosOutside = GetCircle(e.origin, e.radius + e.donutRadius, segments);

        RenderShape donut = new(e.style, VertexConnection.ConnectLastAndFirst, StrokeConnection.NoConnection);

        var length = worldPosInside.Length;
        for (int i = 0; i < length; i++)
        {
            donut.Add(worldPosInside[i], worldPosOutside[i]);
        }
        donut.Draw(ViewProj);
    }

    void DrawLineWorld(DisplayObjectLine e)
    {
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        if (e.radius == 0)
        {
            if (p.Profiler.Enabled) p.Profiler.GuiLines.StartTick();
            var nearPlane = ViewProj.Column3();

            Vector3 start = e.start;
            Vector3 stop = e.stop;
            if (ClipLineToPlane(nearPlane, ref start, ref stop, out float _) == LineClipStatus.NotVisible)
                return;

            drawList.PathLineTo(WorldToScreen(ViewProj, start));
            drawList.PathLineTo(WorldToScreen(ViewProj, stop));
            drawList.PathStroke(e.style.strokeColor, ImDrawFlags.None, e.style.strokeThickness);
            if (p.Profiler.Enabled) p.Profiler.GuiLines.StopTick();
        }
        else
        {
            var leftStart = e.start - e.PerpendicularRadius;
            var leftStop = e.stop - e.PerpendicularRadius;

            var rightStart = e.start + e.PerpendicularRadius;
            var rightStop = e.stop + e.PerpendicularRadius;

            // This is a tiny hack. Instead of clipping the line horizontally properly, we just cull segments that are offscreen
            // By segmenting the line horizontally, culling offscreen segments still leaves segments on screen.
            // A better fix would be to clip the line horizontally instead of culling offscreen segments.
            int segments = HorizontalLinearSegments(e.radius);
            Vector3 perpendicularStep = e.PerpendicularRadius * 2 / segments;

            RenderShape line = new(e.style, VertexConnection.NoConnection, StrokeConnection.ConnectOriginAndEnd);
            for (int step = 0; step < segments; step++)
            {
                line.Add(leftStart + step * perpendicularStep, leftStop + step * perpendicularStep);
                
            }
            line.Add(rightStart, rightStop);
            line.Draw(ViewProj);
        }
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
        if (Svc.GameGui.WorldToScreen(new Vector3(e.x, e.z, e.y), out Vector2 pos))
            ImGui.GetWindowDrawList().AddCircleFilled(
            new Vector2(pos.X, pos.Y),
            e.thickness,
            ImGui.GetColorU32(e.color),
            MINIMUM_CIRCLE_SEGMENTS);
    }

    public static Vector3[] GetCircle(in Vector3 origin, in float radius, in int segments)
    {
        float totalAngle = MathF.PI * 2;
        float angleStep = totalAngle / segments;

        Vector3[] elements = new Vector3[segments];

        for (int step = 0; step < segments; step++)
        {
            float angle = step * angleStep;
            Vector3 point = origin;
            point.Y += radius;
            elements[step] = XZY(RotatePoint(origin, angle, point));
        }

        return elements;
    }

    private static unsafe Matrix4x4 ReadMatrix(IntPtr address)
    {
        var p = (float*)address;
        Matrix4x4 mtx = new();
        for (var i = 0; i < 16; i++)
            mtx[i / 4, i % 4] = *p++;
        return mtx;
    }
    private static unsafe Vector2 ReadVec2(IntPtr address)
    {
        var p = (float*)address;
        return new(p[0], p[1]);
    }
}
