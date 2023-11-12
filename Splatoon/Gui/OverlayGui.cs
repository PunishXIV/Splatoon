using Dalamud.Game.ClientState.Conditions;
using ECommons.Configuration;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using Splatoon.Structures;
using System.Collections;
using System.Reflection;

namespace Splatoon.Gui;

unsafe class OverlayGui : IDisposable
{
    const int MINIMUM_CIRCLE_SEGMENTS = 12;
    // TODO make configurable
    // Low detail 2-3
    // Med detail 4-5
    // High detail 6+
    const int RADIAL_SEGMENTS_PER_UNIT = 6;
    const int LINEAR_SEGMENTS_PER_UNIT = 1;

    readonly Splatoon p;
    int uid = 0;
    public OverlayGui(Splatoon p)
    {
        this.p = p;
        Svc.PluginInterface.UiBuilder.Draw += Draw;
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

    // Dynamic LoD for lines
    public static int LinearSegments(float length)
    {
        return Math.Max((int)(length / LINEAR_SEGMENTS_PER_UNIT), 1);
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
                void Draw()
                {
                    foreach (var element in p.displayObjects)
                    {
                        if (element is DisplayObjectCircle elementCircle)
                        {
                            DrawRingWorld(elementCircle);
                        }
                        else if (element is DisplayObjectDot elementDot)
                        {
                            DrawPoint(elementDot);
                        }
                        else if (element is DisplayObjectText elementText)
                        {
                            DrawTextWorld(elementText);
                        }
                        else if (element is DisplayObjectLine elementLine)
                        {
                            DrawLineWorld(elementLine);
                        }
                        else if (element is DisplayObjectDonut elementDonut)
                        {
                            DrawDonutWorld(elementDonut);
                        }
                        else if (element is DisplayObjectFan elementFan)
                        {
                            DrawTriangleFanWorld(elementFan);
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

    internal struct Vertex
    {
        public Vector2 pos;
        public bool vis;
        public uint color;
        public Vertex(Vector2 pos, bool vis, uint color)
        {
            this.pos = pos;
            this.vis = vis;
            this.color = color;
        }
    }


    // https://en.wikipedia.org/wiki/Triangle_strip
    void DrawTriangleStrip(Vertex[] points)
    {
        Vector2 uv = ImGui.GetFontTexUvWhitePixel();
        int vertexCount = points.Length;
        int triangleCount = vertexCount - 2;

        // If all vertices of a triangle are not visible, cull the triangle.
        // This lowers distortion when the line is passing behind the camera.
        //
        // TODO
        // This is not perfect. It is possible for part of a triangle to
        // intersect with the screen despite all vertices being off screen.
        // In this case, there can be a small gap when the triangle is at the corner of the screen.
        int cullCount = 0;
        bool[] cull = new bool[triangleCount];
        for (uint i = 0; i < triangleCount; i++)
        {
            if (!points[i].vis && !points[i + 1].vis && !points[i + 2].vis)
            {
                cullCount++;
                cull[i] = true;
            }
        }

        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        uint vtxBase = drawList._VtxCurrentIdx;
        drawList.PrimReserve((triangleCount - cullCount) * 3, vertexCount);
        foreach (Vertex vtx in points)
        {
            drawList.PrimWriteVtx(vtx.pos, uv, vtx.color);
        }

        for (uint i = 0; i < triangleCount; i++)
        {
            if (cull[i])
            {
                continue;
            }
            // Vertices must be specified clockwise; the order of the first 2 vertices alternates every triangle.
            uint first = i % 2; // 0 if even, 1 if odd
            uint second = (i + 1) % 2; // 1 if even, 0 if odd
            drawList.PrimWriteIdx((ushort)(vtxBase + i + first));
            drawList.PrimWriteIdx((ushort)(vtxBase + i + second));
            drawList.PrimWriteIdx((ushort)(vtxBase + i + 2));
        }
    }

    internal Vector3 TranslateToScreen(double x, double y, double z)
    {
        Vector2 tenp;
        Svc.GameGui.WorldToScreen(
            new Vector3((float)x, (float)y, (float)z),
            out tenp
        );
        return new Vector3(tenp.X, tenp.Y, (float)z);
    }

    void DrawLineWorld(DisplayObjectLine e)
    {
        if (e.radius == 0)
        {
            if (p.Profiler.Enabled) p.Profiler.GuiLines.StartTick();
            var result = GetAdjustedLine(e.start, e.stop);
            if (result.posA == null) return;
            ImGui.GetWindowDrawList().PathLineTo(new Vector2(result.posA.Value.X, result.posA.Value.Y));
            ImGui.GetWindowDrawList().PathLineTo(new Vector2(result.posB.Value.X, result.posB.Value.Y));
            ImGui.GetWindowDrawList().PathStroke(e.style.strokeColor, ImDrawFlags.None, e.style.strokeThickness);
            if (p.Profiler.Enabled) p.Profiler.GuiLines.StopTick();
        }
        else
        {
            int segments = LinearSegments(Vector3.Distance(e.start, e.stop));
            Vector3 dirStep = e.Direction / segments;

            bool anyVis = false;
            List<Vertex> points = new List<Vertex>();
            for (int i = 0; i <= segments; i++)
            {
                uint color = Lerp(e.style.originFillColor, e.style.endFillColor, (float) i / segments);
                Vector3 centerPoint = e.start + dirStep * i;

                bool vis = Svc.GameGui.WorldToScreen(centerPoint - e.PerpendicularRadius, out Vector2 leftPos);
                anyVis = anyVis || vis;
                points.Add(new Vertex(leftPos, vis, color));

                vis = Svc.GameGui.WorldToScreen(centerPoint + e.PerpendicularRadius, out Vector2 rightPos);
                anyVis = anyVis || vis;
                points.Add(new Vertex(rightPos, vis, color));
            }

            if (!anyVis)
            {
                return;
            }

            // Fill
            DrawTriangleStrip(points.ToArray());

            // Stroke
            // TODO This is way too complicated; surely there's a better way to do this!?
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            // Stroke order is forwards for left points and backwards for right points
            int[] strokeOrder = new int[points.Count];
            for (int i = 0; i < points.Count / 2; i++)
            {
                strokeOrder[i] = i * 2;
                strokeOrder[points.Count - 1 - i] = i * 2 + 1;
            }
            // Stroke the line; culling vertices if both neighbors are not visible.
            bool prevVis = points[strokeOrder[points.Count - 1]].vis;
            for (int i = 0; i < points.Count; i++)
            {
                int nextIdx = strokeOrder[(i + 1) % points.Count];
                bool nextVis = points[nextIdx].vis;

                int currIdx = strokeOrder[i];
                if (prevVis || nextVis)
                {
                    drawList.PathLineTo(points[currIdx].pos);
                }
                prevVis = points[currIdx].vis;
            }
            drawList.PathStroke(e.style.strokeColor, ImDrawFlags.Closed, e.style.strokeThickness);
        }
    }

    (Vector2? posA, Vector2? posB) GetAdjustedLine(Vector3 pointA, Vector3 pointB)
    {
        var resultA = Svc.GameGui.WorldToScreen(new Vector3(pointA.X, pointA.Y, pointA.Z), out Vector2 posA);
        if (!resultA && !p.DisableLineFix)
        {
            var posA2 = GetLineClosestToVisiblePoint(pointA,
            (pointB - pointA) / p.CurrentLineSegments, 0, p.CurrentLineSegments);
            if (posA2 == null)
            {
                if (p.Profiler.Enabled) p.Profiler.GuiLines.StopTick();
                return (null, null);
            }
            else
            {
                posA = posA2.Value;
            }
        }
        var resultB = Svc.GameGui.WorldToScreen(new Vector3(pointB.X, pointB.Y, pointB.Z), out Vector2 posB);
        if (!resultB && !p.DisableLineFix)
        {
            var posB2 = GetLineClosestToVisiblePoint(pointB,
            (pointA - pointB) / p.CurrentLineSegments, 0, p.CurrentLineSegments);
            if (posB2 == null)
            {
                if (p.Profiler.Enabled) p.Profiler.GuiLines.StopTick();
                return (null, null);
            }
            else
            {
                posB = posB2.Value;
            }
        }

        return (posA, posB);
    }

    Vector2? GetLineClosestToVisiblePoint(Vector3 currentPos, Vector3 delta, int curSegment, int numSegments)
    {
        if (curSegment > numSegments) return null;
        var nextPos = currentPos + delta;
        if (Svc.GameGui.WorldToScreen(new Vector3(nextPos.X, nextPos.Z, nextPos.Y), out Vector2 pos))
        {
            var preciseVector = GetLineClosestToVisiblePoint(currentPos, (nextPos - currentPos) / p.Config.lineSegments, 0, p.Config.lineSegments);
            return preciseVector.HasValue ? preciseVector.Value : pos;
        }
        else
        {
            return GetLineClosestToVisiblePoint(nextPos, delta, ++curSegment, numSegments);
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

    // TODO swap to use triangle fan
    public void DrawRingWorld(DisplayObjectCircle e)
    {
        int seg = p.Config.segments / 2;
        Svc.GameGui.WorldToScreen(new Vector3(
            e.x + e.radius * (float)Math.Sin(p.CamAngleX),
            e.z,
            e.y + e.radius * (float)Math.Cos(p.CamAngleX)
            ), out Vector2 refpos);
        var visible = false;
        Vector2?[] elements = new Vector2?[p.Config.segments];
        for (int i = 0; i < p.Config.segments; i++)
        {
            visible = Svc.GameGui.WorldToScreen(
                new Vector3(e.x + e.radius * (float)Math.Sin(Math.PI / seg * i),
                e.z,
                e.y + e.radius * (float)Math.Cos(Math.PI / seg * i)
                ),
                out Vector2 pos)
                || visible;
            if (pos.Y > refpos.Y || P.Config.NoCircleFix) elements[i] = new Vector2(pos.X, pos.Y);
        }
        if (visible)
        {
            foreach (var pos in elements)
            {
                if (pos == null) continue;
                ImGui.GetWindowDrawList().PathLineTo(pos.Value);
            }
            if (e.style.originFillColor != 0)
            {
                ImGui.GetWindowDrawList().PathFillConvex(e.style.originFillColor);
            }

            foreach (var pos in elements)
            {
                if (pos == null) continue;
                ImGui.GetWindowDrawList().PathLineTo(pos.Value);
            }
            if (e.style.strokeColor != 0)
            {
                ImGui.GetWindowDrawList().PathStroke(e.style.strokeColor, ImDrawFlags.Closed, e.style.strokeThickness);
            }
        }
    }

    public void DrawTriangleFanWorld(DisplayObjectFan e)
    {
        Vector2 uv = ImGui.GetFontTexUvWhitePixel();
        float totalAngle = e.angleMax - e.angleMin;
        int segments = RadialSegments(e.radius, totalAngle);
        float angleStep = totalAngle / segments;

        int vertexCount = segments + 1;
        var screenPoints = new ArrayList(vertexCount);


        bool visible = Svc.GameGui.WorldToScreen(XZY(e.origin), out Vector2 screenPointOrigin);

        for (int step = 0; step < vertexCount; step++)
        {
            float angle = e.angleMin + step * angleStep;
            Vector3 point = e.origin;
            point.Y += e.radius;
            point = RotatePoint(e.origin, angle, point);
            visible = Svc.GameGui.WorldToScreen(XZY(point), out Vector2 screenPosPoint) || visible;
            screenPoints.Add(screenPosPoint);
        }

        if (!visible)
        {
            return;
        }

        ImDrawListPtr drawList = ImGui.GetWindowDrawList();

        // Fill
        uint vtxBase = drawList._VtxCurrentIdx;
        drawList.PrimReserve(segments * 3, 1 + screenPoints.Count);
        drawList.PrimWriteVtx(screenPointOrigin, uv, e.style.originFillColor);
        foreach (Vector2 screenPoint in screenPoints)
        {
            drawList.PrimWriteVtx(screenPoint, uv, e.style.endFillColor);
        }
        for (int n = 0; n < segments; n++)
        {
            drawList.PrimWriteIdx((ushort)(vtxBase));
            drawList.PrimWriteIdx((ushort)(vtxBase + 1 + n));
            drawList.PrimWriteIdx((ushort)(vtxBase + 1 + ((n + 1) % vertexCount)));
        }

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
        int segments = RadialSegments(e.innerRadius);
        var worldPosInside = GetCircle(e.x, e.y, e.z, e.innerRadius, segments);
        var worldPosOutside = GetCircle(e.x, e.y, e.z, e.innerRadius + e.donutRadius, segments);
        var screenPosInside = new Vector2[segments];
        var screenPosOutside = new Vector2[segments];
        var screenPosStrip = new Vertex[segments * 2];

        bool anyVis = false;
        var length = worldPosInside.Length;
        for (int i = 0; i < length; i++)
        {
            bool vis = Svc.GameGui.WorldToScreen(worldPosInside[i], out Vector2 inside);
            anyVis = anyVis || vis;
            screenPosStrip[2 * i] = new Vertex(inside, vis, e.style.originFillColor);
            screenPosInside[i] = inside;

            vis = Svc.GameGui.WorldToScreen(worldPosOutside[i], out Vector2 outside);
            anyVis = anyVis || vis;
            screenPosStrip[2 * i + 1] = new Vertex(outside, vis, e.style.endFillColor);
            screenPosOutside[i] = outside;
        }
        DrawTriangleStrip(screenPosStrip);

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
    }

    public Vector3[] GetCircle(float x, float y, float z, float radius, int segment)
    {
        int seg = segment / 2;

        Vector3[] elements = new Vector3[segment];

        for (int i = 0; i < segment; i++)
        {
            elements[i] = new Vector3(
                x + radius * (float)Math.Sin(Math.PI / seg * i),
                z,
                y + radius * (float)Math.Cos(Math.PI / seg * i)
                );
        }

        return elements;
    }

    #region API for cut thing behind the camera. These are core code which passed tests.
    private static IEnumerable<Vector2> GetPtsOnScreen(IEnumerable<Vector3> pts)
    {
        var cameraPts = pts.Select(WorldToCamera).ToArray();
        var changedPts = new List<Vector3>(cameraPts.Length * 2);

        for (int i = 0; i < cameraPts.Length; i++)
        {
            var pt1 = cameraPts[i];
            var pt2 = cameraPts[(i + 1) % cameraPts.Length];

            if (pt1.Z > 0 && pt2.Z <= 0)
            {
                GetPointOnPlane(pt1, ref pt2);
            }
            if (pt2.Z > 0 && pt1.Z <= 0)
            {
                GetPointOnPlane(pt2, ref pt1);
            }

            if (changedPts.Count > 0 && Vector3.Distance(pt1, changedPts[changedPts.Count - 1]) > 0.001f)
            {
                changedPts.Add(pt1);
            }

            changedPts.Add(pt2);
        }

        return changedPts.Where(p => p.Z > 0).Select(p =>
        {
            CameraToScreen(p, out var screenPos, out _);
            return screenPos;
        });
    }

    const float PLANE_Z = 0.001f;
    private static void GetPointOnPlane(Vector3 front, ref Vector3 back)
    {
        if (front.Z < 0) return;
        if (back.Z > 0) return;

        var ratio = (PLANE_Z - back.Z) / (front.Z - back.Z);
        back.X = (front.X - back.X) * ratio + back.X;
        back.Y = (front.Y - back.Y) * ratio + back.Y;
        back.Z = PLANE_Z;
    }

    #region These are the api in the future... Maybe. https://github.com/goatcorp/Dalamud/pull/1203
    static readonly FieldInfo _matrix = Svc.GameGui.GetType().GetRuntimeFields().FirstOrDefault(f => f.Name == "getMatrixSingleton");
    private static unsafe Vector3 WorldToCamera(Vector3 worldPos)
    {
        var matrix = (MulticastDelegate)_matrix.GetValue(Svc.GameGui);
        var matrixSingleton = (IntPtr)matrix.DynamicInvoke();

        var viewProjectionMatrix = *(Matrix4x4*)(matrixSingleton + 0x1b4);
        return Vector3.Transform(worldPos, viewProjectionMatrix);
    }

    private static unsafe bool CameraToScreen(Vector3 cameraPos, out Vector2 screenPos, out bool inView)
    {
        screenPos = new Vector2(cameraPos.X / MathF.Abs(cameraPos.Z), cameraPos.Y / MathF.Abs(cameraPos.Z));
        var windowPos = ImGuiHelpers.MainViewport.Pos;

        var device = Device.Instance();
        float width = device->Width;
        float height = device->Height;

        screenPos.X = (0.5f * width * (screenPos.X + 1f)) + windowPos.X;
        screenPos.Y = (0.5f * height * (1f - screenPos.Y)) + windowPos.Y;

        var inFront = cameraPos.Z > 0;
        inView = inFront &&
                 screenPos.X > windowPos.X && screenPos.X < windowPos.X + width &&
                 screenPos.Y > windowPos.Y && screenPos.Y < windowPos.Y + height;

        return inFront;
    }
    #endregion
    #endregion
}
