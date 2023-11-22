using ImGuiNET;
using Splatoon.Structures;
using System.Runtime.InteropServices;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using static Splatoon.Utils.Gui;

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

    public enum VertexConnection
    {
        NoConnection,
        ConnectLastAndFirst,
    }

    public enum StrokeConnection
    {
        NoConnection,
        ConnectOriginAndEnd,
    }

    public static bool Visible(this LineClipStatus status)
    {
        return status != LineClipStatus.NotVisible;
    }

    public static LineClipStatus ClipLineToPlane(in Vector4 plane, ref Vector3 a, ref Vector3 b, out float t)
    {
        t = 0f;
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

    public static Vector2 WorldToScreen(in Matrix4x4 viewProj, in Vector3 worldPos)
    {
        TransformCoordinate(worldPos, viewProj, out Vector3 viewPos);
        return new Vector2(
            0.5f * ImGuiHelpers.MainViewport.Size.X * (1 + viewPos.X),
            0.5f * ImGuiHelpers.MainViewport.Size.Y * (1 - viewPos.Y)) + ImGuiHelpers.MainViewport.Pos;
    }

    public class RenderShape(DisplayStyle style, VertexConnection connectStyle, StrokeConnection strokeStyle)
    {
        internal DisplayStyle style = style;
        internal VertexConnection connectStyle = connectStyle;
        internal StrokeConnection strokeStyle = strokeStyle;
        internal List<LineSegment> segments = new();

        internal struct LineSegment(Vector3 origin, Vector3 end)
        {
            public Vector3 origin = origin;
            public Vector3 end = end;
        }

        public void Add(Vector3 origin, Vector3 end)
        {
            segments.Add(new(origin, end));
        }

        public void Draw(in Matrix4x4 viewProj)
        {
            Vector4 nearPlane = viewProj.Column3();

            int count = segments.Count;
            bool[] cull = new bool[count];

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            uint vtxBase = drawList._VtxCurrentIdx;

            Vector2[] originScreenPositions = new Vector2[count];
            Vector2[] endScreenPositions = new Vector2[count];
            ushort[] originVtx = new ushort[count];
            ushort[] endVtx = new ushort[count];

            // Clip lines to near plane and prepare vertices for drawing
            var segmentsSpan = CollectionsMarshal.AsSpan(segments);
            LineSegment prevSegment = new();
            for (int i = 0; i < count; i++)
            {
                ref LineSegment segment = ref segmentsSpan[i];
                var status = ClipLineToPlane(nearPlane, ref segment.origin, ref segment.end, out float t);
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
            
            // Draw triangles
            for (int i = 0; i < count; i++)
            {
                int nextIndex = (i + 1) % count;
                if (cull[i] || cull[nextIndex])
                {
                    continue;
                }
                if (i + 1 == count && connectStyle == VertexConnection.NoConnection)
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

            if (style.strokeThickness > 0 && (style.strokeColor & 0xFF000000) > 0)
            {

                // Path and stroke origin vertices
                int pathLength = 0;
                int strokeCount = originScreenPositions.Length;
                if (connectStyle == VertexConnection.ConnectLastAndFirst)
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
                if (strokeStyle == StrokeConnection.NoConnection)
                {
                    pathLength = 0;
                    drawList.PathStroke(style.strokeColor, ImDrawFlags.None, style.strokeThickness);
                }

                // Path and stroke end vertices
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
                if (strokeStyle == StrokeConnection.ConnectOriginAndEnd)
                {
                    if (!cull[0])
                    {
                        drawList.PathLineToMergeDuplicate(originScreenPositions[0]);
                    }
                }
                drawList.PathStroke(style.strokeColor, ImDrawFlags.None, style.strokeThickness);
            }
        }
    }
}