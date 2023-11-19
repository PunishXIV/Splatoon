using Dalamud.Game.ClientState.Conditions;
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
    const int RADIAL_SEGMENTS_PER_UNIT = 6;
    const int MINIMUM_CIRCLE_SEGMENTS = 12;

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

    TriClipStatus DrawTriangle(in Vector4 clipPlane, ref Triangle tri)
    {
        var status = ClipTriangleToPlane(in clipPlane, ref tri, out Triangle quadfill);
        if (status == TriClipStatus.NotVisible)
            return TriClipStatus.NotVisible;

        DrawTriangle(tri);

        if (status == TriClipStatus.A_Clipped ||
            status == TriClipStatus.B_Clipped ||
            status == TriClipStatus.C_Clipped)
        {
            DrawTriangle(quadfill);
        }

        void DrawTriangle(in Triangle tri)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            drawList.PrimReserve(3, 3);
            uint vtxBase = drawList._VtxCurrentIdx;

            drawList.PrimWriteVtx(WorldToScreen(tri.a), UV, tri.aColor);
            drawList.PrimWriteVtx(WorldToScreen(tri.b), UV, tri.bColor);
            drawList.PrimWriteVtx(WorldToScreen(tri.c), UV, tri.cColor);
            drawList.PrimWriteIdx((ushort)(vtxBase + 0));
            drawList.PrimWriteIdx((ushort)(vtxBase + 1));
            drawList.PrimWriteIdx((ushort)(vtxBase + 2));
        }
        return status;
    }

    struct Vertex(Vector3 point, uint color)
    {
        public readonly Vector3 point = point;
        public readonly uint color = color;
    }

    // https://en.wikipedia.org/wiki/Triangle_strip
    void DrawTriangleStrip(Vertex[] vertices)
    {
        var nearPlane = ViewProj.Column3();

        int vertexCount = vertices.Length;
        int triangleCount = vertexCount - 2;

        for (int i = 0; i < triangleCount; i++)
        {
            Triangle tri = new(
                    vertices[i].point,
                    vertices[i + 1].point,
                    vertices[i + 2].point,
                    vertices[i].color,
                    vertices[i + 1].color,
                    vertices[i + 2].color);
            DrawTriangle(nearPlane, ref tri);
        }
    }

    public void DrawTriangleFanWorld(DisplayObjectFan e)
    {
        var nearPlane = ViewProj.Column3();
        float totalAngle = e.angleMax - e.angleMin;
        int segments = RadialSegments(e.radius, totalAngle);
        float angleStep = totalAngle / segments;

        int vertexCount = segments + 1;
        var worldPoints = new List<Vector3>(vertexCount);

        for (int step = 0; step < vertexCount; step++)
        {
            float angle = e.angleMin + step * angleStep;
            Vector3 point = e.origin;
            point.Y += e.radius;
            point = RotatePoint(e.origin, angle, point);
            worldPoints.Add(XZY(point));
        }

        Vector3 origin = XZY(e.origin);
        for (int n = 0; n < segments; n++)
        {
            Triangle tri = new(
                    origin,
                    worldPoints[n],
                    worldPoints[n + 1],
                    e.style.originFillColor,
                    e.style.endFillColor,
                    e.style.endFillColor);
            DrawTriangle(nearPlane, ref tri);
        }

        /*
        // Stroke
        if (e.style.strokeColor != 0)
        {
            var flags = ImDrawFlags.None;
            // Don't include the origin if this is a complete circle
            if (MathF.Abs(e.angleMin + e.angleMax) < 2 * MathF.PI - 0.0001)
            {
                flags = ImDrawFlags.Closed;
                drawList.PathLineTo(screenPointOrigin);
            }
            foreach (Vector2 screenPoint in screenPoints)
            {
                drawList.PathLineTo(screenPoint);
            }
            drawList.PathStroke(e.style.strokeColor, flags, e.style.strokeThickness);
        }
        */
    }

    void DrawLineWorld(DisplayObjectLine e)
    {
        var nearPlane = ViewProj.Column3();

        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        if (e.radius == 0)
        {
            if (p.Profiler.Enabled) p.Profiler.GuiLines.StartTick();
            Vector3 start = e.start;
            Vector3 stop = e.stop;
            if (ClipLineToPlane(nearPlane, ref start, ref stop) == LineClipStatus.NotVisible)
                return;

            drawList.PathLineTo(WorldToScreen(start));
            drawList.PathLineTo(WorldToScreen(stop));
            drawList.PathStroke(e.style.strokeColor, ImDrawFlags.None, e.style.strokeThickness);
            if (p.Profiler.Enabled) p.Profiler.GuiLines.StopTick();
        }
        else
        {
            var leftStart = e.start - e.PerpendicularRadius;
            var leftStop = e.stop - e.PerpendicularRadius;

            var rightStart = e.start + e.PerpendicularRadius;
            var rightStop = e.stop + e.PerpendicularRadius;

            Triangle left = new(leftStart, rightStart, leftStop, e.style.originFillColor, e.style.originFillColor, e.style.endFillColor);
            Triangle right = new(rightStart, leftStop, rightStop, e.style.originFillColor, e.style.endFillColor, e.style.endFillColor);

            var leftStatus = DrawTriangle(nearPlane, ref left);
            var rightStatus = DrawTriangle(nearPlane, ref right);
            /*
            var flags = ImDrawFlags.Closed;

            if (leftStatus == TriClipStatus.A_Clipped ||
                leftStatus == TriClipStatus.B_Clipped ||
                leftStatus == TriClipStatus.C_Clipped)
            {
                flags = ImDrawFlags.None;
            }
            if (rightStatus == TriClipStatus.A_Clipped ||
                rightStatus == TriClipStatus.B_Clipped ||
                rightStatus == TriClipStatus.C_Clipped)
            {
                flags = ImDrawFlags.None;
            }

            List<Vector3> strokePoints = new();

            if (ClipLineToPlane(nearPlane, ref leftStart, ref leftStop) != LineClipStatus.NotVisible)
            {
                strokePoints.Add(leftStart);
                strokePoints.Add(leftStop);
            }
            if (ClipLineToPlane(nearPlane, ref rightStart, ref rightStop) != LineClipStatus.NotVisible)
            {
                strokePoints.Add(rightStop);
                strokePoints.Add(rightStart);
            }
            if (strokePoints.Count > 0)
            {
                strokePoints.ForEach(point => drawList.PathLineTo(WorldToScreen(point)));
                drawList.PathStroke(e.style.strokeColor, flags, e.style.strokeThickness);
            }
            */

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

    public void DrawDonutWorld(DisplayObjectDonut e)
    {
        int segments = RadialSegments(e.radius);
        var worldPosInside = GetCircle(e.origin, e.radius, segments);
        var worldPosOutside = GetCircle(e.origin, e.radius + e.donutRadius, segments);
        var screenPosInside = new Vector2[segments];
        var screenPosOutside = new Vector2[segments];
        var worldPosStrip = new Vertex[(segments + 1) * 2];

        var length = worldPosInside.Length;
        for (int i = 0; i < length; i++)
        {
            worldPosStrip[2 * i] = new(worldPosInside[i], e.style.originFillColor);
            //screenPosInside[i] = inside.pos;

            worldPosStrip[2 * i + 1] = new(worldPosOutside[i], e.style.endFillColor);
            //screenPosOutside[i] = outside.pos;
        }
        worldPosStrip[length * 2] = worldPosStrip[0];
        worldPosStrip[length * 2 + 1] = worldPosStrip[1];
        DrawTriangleStrip(worldPosStrip);

        /*
        foreach (var pos in screenPosInside)
        {
            ImGui.GetWindowDrawList().PathLineTo(pos);
        }
        if (e.style.strokeColor != 0)
        {
            ImGui.GetWindowDrawList().PathStroke(e.style.strokeColor, ImDrawFlags.None, e.style.strokeThickness);
        }
        foreach (var pos in screenPosOutside)
        {
            ImGui.GetWindowDrawList().PathLineTo(pos);
        }
        if (e.style.strokeColor != 0)
        {
            ImGui.GetWindowDrawList().PathStroke(e.style.strokeColor, ImDrawFlags.None, e.style.strokeThickness);
        }
        */
    }

    public Vector3[] GetCircle(in Vector3 origin, in float radius, in int segments)
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

    private Vector2 WorldToScreen(Vector3 worldPos)
    {
        TransformCoordinate(worldPos, ViewProj, out Vector3 viewPos);
        return new Vector2(
            0.5f * ImGuiHelpers.MainViewport.Size.X * (1 + viewPos.X),
            0.5f * ImGuiHelpers.MainViewport.Size.Y * (1 - viewPos.Y)) + ImGuiHelpers.MainViewport.Pos;
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
