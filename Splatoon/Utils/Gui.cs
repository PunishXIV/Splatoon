using Splatoon.Structures;
using System.Runtime.InteropServices;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace Splatoon.Utils;

public static unsafe class Gui
{
    static readonly Vector2 UV = ImGui.GetFontTexUvWhitePixel();

    public enum LineClipStatus
    {
        NotVisible,
        NotClipped,
        A_Clipped,
        B_Clipped,
    }

    public static bool Visible(this LineClipStatus status)
    {
        return status != LineClipStatus.NotVisible;
    }

    public static LineClipStatus ClipLineToPlane(in Vector4 plane, ref Vector3 a, ref Vector3 b)
    {
        var aDot = Vector4.Dot(new(a, 1), plane);
        var bDot = Vector4.Dot(new(b, 1), plane);
        bool aVis = aDot > 0;
        bool bVis = bDot > 0;
        if (aVis && bVis)
            return LineClipStatus.NotClipped;
        if (!aVis && !bVis)
            return LineClipStatus.NotVisible;
        Vector3 plane3 = new(plane.X, plane.Y, plane.Z);

        Vector3 ab = b - a;
        var t = -aDot / Vector3.Dot(ab, plane3);
        Vector3 abClipped = a + ab * t;
        if (aVis)
        {
            b = abClipped;
            return LineClipStatus.B_Clipped;
        }
        a = abClipped;
        return LineClipStatus.A_Clipped;
    }

    public class RenderShape
    {
        internal DisplayStyle style;
        internal List<LineSegment> segments;
        internal bool connectLastAndFirst;
        internal bool connectOriginAndEndStroke;

        public struct LineSegment(Vector3 origin, Vector3 end)
        {
            public Vector3 origin = origin;
            public Vector3 end = end;
        }

        public RenderShape(DisplayStyle style, bool connectLastAndFirst, bool connectOriginAndEndStroke)
        {
            segments = new();
            this.style = style;
            this.connectLastAndFirst = connectLastAndFirst;
            this.connectOriginAndEndStroke = connectOriginAndEndStroke;
        }

        public void Add(Vector3 origin, Vector3 end)
        {
            Add(new(origin, end));
        }

        public void Add(LineSegment segment)
        {
            segments.Add(segment);
        }

        public static LineClipStatus ClipLineToPlane(in Vector4 plane, ref LineSegment segment, out float t)
        {
            t = 0f;
            ref var a = ref segment.origin;
            ref var b = ref segment.end;

            var aDot = Vector4.Dot(new(a, 1), plane);
            var bDot = Vector4.Dot(new(b, 1), plane);
            bool aVis = aDot > 0;
            bool bVis = bDot > 0;

            if (aVis && bVis)
                return LineClipStatus.NotClipped;
            if (!aVis && !bVis)
                return LineClipStatus.NotVisible;
            Vector3 plane3 = new(plane.X, plane.Y, plane.Z);

            Vector3 ab = b - a;
            t = -aDot / Vector3.Dot(ab, plane3);
            Vector3 abClipped = a + ab * t;
            if (aVis)
            {
                b = abClipped;
                return LineClipStatus.B_Clipped;
            }
            a = abClipped;
            return LineClipStatus.A_Clipped;
        }

        public void Draw(in Matrix4x4 viewProj)
        {
            Vector4 nearPlane = viewProj.Column3();

            int count = segments.Count;
            bool[] cull = new bool[count];


            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            uint vtxBase = drawList._VtxCurrentIdx;

            Vector2[] originScreenPositions = new Vector2[segments.Count];
            Vector2[] endScreenPositions = new Vector2[segments.Count];
            ushort[] originVtx = new ushort[segments.Count];
            ushort[] endVtx = new ushort[segments.Count];

            var segmentsSpan = CollectionsMarshal.AsSpan(segments);
            LineSegment prevSegment = new();
            for (int i = 0; i < segments.Count; i++)
            {
                var status = ClipLineToPlane(nearPlane, ref segmentsSpan[i], out float t);
                if (status == LineClipStatus.NotVisible)
                {
                    cull[i] = true;
                    continue;
                }
                uint originColor = style.originFillColor;
                uint endColor = style.endFillColor;
                if (status == LineClipStatus.A_Clipped)
                {
                    originColor = Lerp(style.originFillColor, style.endFillColor, t);
                }
                else if (status == LineClipStatus.B_Clipped)
                {
                    endColor = Lerp(style.originFillColor, style.endFillColor, t);
                }

                var segment = segments[i];

                if (segment.end == prevSegment.end)
                {
                    endVtx[i] = endVtx[i-1];
                    endScreenPositions[i] = endScreenPositions[i - 1];
                }
                else
                {
                    var endScreenPos = WorldToScreen(viewProj, segment.end);
                    endScreenPositions[i] = endScreenPos;
                    drawList.PrimReserve(0, 1);
                    drawList.PrimWriteVtx(endScreenPos, UV, endColor);
                    endVtx[i] = (ushort)(vtxBase++);
                }

                if (segment.origin == prevSegment.origin)
                {
                    originVtx[i] = originVtx[i-1];
                    originScreenPositions[i] = originScreenPositions[i - 1];
                }
                else
                {
                    var originScreenPos = WorldToScreen(viewProj, segment.origin);
                    originScreenPositions[i] = originScreenPos;
                    drawList.PrimReserve(0, 1);
                    drawList.PrimWriteVtx(originScreenPos, UV, originColor);
                    originVtx[i] = (ushort)(vtxBase++);
                }

                prevSegment = segment;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                int nextIndex = (i + 1) % segments.Count;
                if (cull[i] || cull[nextIndex])
                {
                    continue;
                }
                if (i + 1 == segments.Count && !connectLastAndFirst)
                {
                    break;
                }

                if (originVtx[i] != originVtx[nextIndex])
                {
                    drawList.PrimReserve(3, 0);

                    drawList.PrimWriteIdx(originVtx[i]);
                    drawList.PrimWriteIdx(originVtx[nextIndex]);
                    drawList.PrimWriteIdx(endVtx[i]);
                }

                if (endVtx[i] != endVtx[nextIndex])
                {
                    drawList.PrimReserve(3, 0);

                    drawList.PrimWriteIdx(endVtx[i]);
                    drawList.PrimWriteIdx(originVtx[nextIndex]);
                    drawList.PrimWriteIdx(endVtx[nextIndex]);
                }
            }
            int pathLength = 0;

            int strokeCount = originScreenPositions.Length;
            if (connectLastAndFirst)
            {
                strokeCount++;
            }

            for (int i = 0; i < strokeCount; i++)
            {
                int idx = i % originScreenPositions.Length;
                if (cull[idx])
                {
                    if (pathLength > 0)
                    {
                        pathLength = 0;
                        drawList.PathStroke(style.strokeColor, ImDrawFlags.None, style.strokeThickness);
                    }
                    continue;
                }
                pathLength++;
                drawList.PathLineToMergeDuplicate(originScreenPositions[idx]);
            }
            if (!connectOriginAndEndStroke)
            {
                pathLength = 0;
                drawList.PathStroke(style.strokeColor, ImDrawFlags.None, style.strokeThickness);
            }

            for (int i = 0; i < strokeCount; i++)
            {
                // Stroke right to left so ends of lines join properly.
                int idx = (2 * count - 1 - i) % count;
                if (cull[idx])
                {
                    if (pathLength > 0)
                    {
                        pathLength = 0;
                        drawList.PathStroke(style.strokeColor, ImDrawFlags.None, style.strokeThickness);
                    }
                    continue;
                }

                pathLength++;
                drawList.PathLineToMergeDuplicate(endScreenPositions[idx]);
            }
            if (connectOriginAndEndStroke)
            {
                if (!cull[0])
                {
                    drawList.PathLineToMergeDuplicate(originScreenPositions[0]);
                }
            }
            drawList.PathStroke(style.strokeColor, ImDrawFlags.None, style.strokeThickness);
        }
    }

    public static Vector2 WorldToScreen(in Matrix4x4 viewProj, in Vector3 worldPos)
    {
        TransformCoordinate(worldPos, viewProj, out Vector3 viewPos);
        return new Vector2(
            0.5f * ImGuiHelpers.MainViewport.Size.X * (1 + viewPos.X),
            0.5f * ImGuiHelpers.MainViewport.Size.Y * (1 - viewPos.Y)) + ImGuiHelpers.MainViewport.Pos;
    }
}