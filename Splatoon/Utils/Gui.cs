using ImGuiNET;
using Splatoon.Structures;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
    public enum TriClipStatus
    {
        NotVisible,
        NotClipped,
        A_Clipped,
        B_Clipped,
        C_Clipped,
        AB_Clipped,
        BC_Clipped,
        AC_Clipped,
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

    public struct Vertex(Vector3 point, uint color)
    {
        public Vector3 point = point;
        public uint color = color;
    }

    public class Polygon
    {
        internal List<Vertex> vertices;
        internal List<TriangleEdges> triangles;
        internal List<int> stroke;

        public Polygon()
        {
            vertices = new();
            triangles = new();
            stroke = new();
        }

        internal struct TriangleEdges(Polygon p, int a, int b, int c)
        {
            private readonly Polygon p = p;
            public int aIndex = a;
            public int bIndex = b;
            public int cIndex = c;

            public Vector3 a
            { 
                get => p.vertices[aIndex].point;
                set
                {
                    var v = p.vertices[aIndex];
                    v.point = value;
                    p.vertices[aIndex] = v;
                }
            }
            public Vector3 b
            {
                get => p.vertices[bIndex].point;
                set
                {
                    var v = p.vertices[bIndex];
                    v.point = value;
                    p.vertices[bIndex] = v;
                }
            }
            public Vector3 c
            {
                get => p.vertices[cIndex].point;
                set
                {
                    var v = p.vertices[cIndex];
                    v.point = value;
                    p.vertices[cIndex] = v;
                }
            }

            public readonly Vector3 ab => b - a;
            public readonly Vector3 bc => c - b;
            public readonly Vector3 ca => a - c;

            public uint aColor
            {
                get => p.vertices[aIndex].color;
                set
                {
                    var v = p.vertices[aIndex];
                    v.color = value;
                    p.vertices[aIndex] = v;
                }
            }
            public uint bColor
            {
                get => p.vertices[bIndex].color;
                set
                {
                    var v = p.vertices[bIndex];
                    v.color = value;
                    p.vertices[bIndex] = v;
                }
            }
            public uint cColor
            {
                get => p.vertices[cIndex].color;
                set
                {
                    var v = p.vertices[cIndex];
                    v.color = value;
                    p.vertices[cIndex] = v;
                }
            }

            public Vector3 centroid
            {
                get => (a + b + c) / 3;
            }
        }
        public int addVertex(Vertex vertex)
        {
            int index = vertices.Count;
            vertices.Add(vertex);
            return index;
        }


        public int addVertex(Vector3 point, uint color)
        {
            return addVertex(new Vertex(point, color));
        }

        public void addTriangle(int a, int b, int c)
        {
            triangles.Add(new TriangleEdges(this, a,b,c));
        }

        private void splitVertex(int oldIndex, int newIndex)
        {
            var trianglesSpan = CollectionsMarshal.AsSpan(triangles);
            foreach (ref TriangleEdges tri in trianglesSpan)
            {
                if (tri.aIndex == oldIndex) tri.aIndex = newIndex;
                if (tri.bIndex == oldIndex) tri.bIndex = newIndex;
                if (tri.cIndex == oldIndex) tri.cIndex = newIndex;
            }
        }

        public void Draw(in Matrix4x4 viewProj)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            drawList.PrimReserve(3 * triangles.Count, vertices.Count);
            uint vtxBase = drawList._VtxCurrentIdx;

            foreach (Vertex vtx in vertices)
            {
                drawList.PrimWriteVtx(WorldToScreen(viewProj, vtx.point), UV, vtx.color);
            }

            foreach (TriangleEdges tri in triangles)
            {
                drawList.PrimWriteIdx((ushort)(vtxBase + tri.aIndex));
                drawList.PrimWriteIdx((ushort)(vtxBase + tri.bIndex));
                drawList.PrimWriteIdx((ushort)(vtxBase + tri.cIndex));
            }
        }

        public void DebugDraw(in Matrix4x4 viewProj)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            Draw(viewProj);
            for(int i = 0; i < vertices.Count; i++)
            {
                Vector2 pos = WorldToScreen(viewProj, vertices[i].point);
                DrawText(viewProj, "" +i, pos);
            }
            for (int i = 0; i < triangles.Count; i++)
            {
                TriangleEdges tri = triangles[i];
                Vector2 pos = WorldToScreen(viewProj, tri.centroid);
                DrawText(viewProj, "t" + i + "\n" + tri.aIndex + "," + tri.bIndex + "," + tri.cIndex, pos);
                drawList.PathLineTo(WorldToScreen(viewProj, tri.a));
                drawList.PathLineTo(WorldToScreen(viewProj, tri.b));
                drawList.PathLineTo(WorldToScreen(viewProj, tri.c));
                drawList.PathStroke(0xFF0000FF, ImDrawFlags.Closed, 2);
            }
        }

        int uid = 0;
        public void DrawText(in Matrix4x4 viewProj, string text, Vector2 pos)
        {
            var size = ImGui.CalcTextSize(text);
            size = new Vector2(size.X + 10f, size.Y + 10f);
            ImGui.SetNextWindowPos(new Vector2(pos.X - size.X / 2, pos.Y - size.Y / 2));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 10f);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.ColorConvertU32ToFloat4(0x000000FF));
            ImGui.BeginChild("##child" + text + ++uid, size, false,
                ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav
                | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysUseWindowPadding);
            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFFFFF);
            ImGuiEx.Text(text);
            ImGui.PopStyleColor();
            ImGui.EndChild();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(2);
        }

        private Vector2 WorldToScreen(in Matrix4x4 viewProj, in Vector3 worldPos)
        {
            TransformCoordinate(worldPos, viewProj, out Vector3 viewPos);
            return new Vector2(
                0.5f * ImGuiHelpers.MainViewport.Size.X * (1 + viewPos.X),
                0.5f * ImGuiHelpers.MainViewport.Size.Y * (1 - viewPos.Y)) + ImGuiHelpers.MainViewport.Pos;
        }

        public void ClipToPlane(in Vector4 plane)
        {
            var trianglesSpan = CollectionsMarshal.AsSpan(triangles);

            List<int> cullList = new();
            /*
            triangles.Sort((tri1, tri2) => {
                return Vector3.DistanceSquared(tri2.centroid, ImGuiHelpers.MainViewport.Pos) - Vector3.DistanceSquared(tri1.centroid, ImGuiHelpers.MainViewport.Pos);
                });
            */
            int count = triangles.Count;
            for (int i = 0; i < count; i++)
            {
                TriClipStatus status = ClipTriangleToPlane(plane, ref trianglesSpan[i]);
                if (status == TriClipStatus.NotVisible) cullList.Add(i);
            }

            cullList.Reverse();
            foreach (int i in cullList)
            {
                triangles.RemoveAt(i);
            }
        }

        /**
         * Clip reference variable triangle to supplied plane.
         * If only 1 corner is clipped the output is a quad; The quadfill output is populated with a second triangle to fill the quad.
         */
        private TriClipStatus ClipTriangleToPlane(in Vector4 plane, ref TriangleEdges tri)
        {
            var aDot = Vector4.Dot(new(tri.a, 1), plane);
            var bDot = Vector4.Dot(new(tri.b, 1), plane);
            var cDot = Vector4.Dot(new(tri.c, 1), plane);

            bool aVis = aDot > 0;
            bool bVis = bDot > 0;
            bool cVis = cDot > 0;

            if (aVis && bVis && cVis)
                return TriClipStatus.NotClipped;
            if (!aVis && !bVis && !cVis)
                return TriClipStatus.NotVisible;
            Vector3 plane3 = new(plane.X, plane.Y, plane.Z);

            var abT = -aDot / Vector3.Dot(tri.ab, plane3);
            var bcT = -bDot / Vector3.Dot(tri.bc, plane3);
            var caT = -cDot / Vector3.Dot(tri.ca, plane3);

            var abClipped = tri.a + tri.ab * abT;
            var bcClipped = tri.b + tri.bc * bcT;
            var caClipped = tri.c + tri.ca * caT;

            var abColor = Lerp(tri.aColor, tri.bColor, abT);
            var bcColor = Lerp(tri.bColor, tri.cColor, bcT);
            var caColor = Lerp(tri.cColor, tri.aColor, caT);

            // 1 visible, 2 clipped
            if (aVis && !bVis && !cVis)
            {
                tri.b = abClipped;
                tri.c = caClipped;
                tri.bColor = abColor;
                tri.cColor = caColor;
                return TriClipStatus.BC_Clipped;
            }
            else if (!aVis && bVis && !cVis)
            {
                tri.a = abClipped;
                tri.c = bcClipped;
                tri.aColor = abColor;
                tri.cColor = bcColor;
                return TriClipStatus.AC_Clipped;
            }
            else if (!aVis && !bVis && cVis)
            {
                tri.a = caClipped;
                tri.b = bcClipped;
                tri.aColor = caColor;
                tri.bColor = bcColor;
                return TriClipStatus.AB_Clipped;
            }
            // 2 visible, 1 clipped
            // OMG WTF
            else if (!aVis && bVis && cVis)
            {
                // Split vertex A
                // Clip A to AB
                // Add new vertex A' to AC
                // Triangle stays ABC
                // Fill triangle becomes A'AC

                /*
                 * References to A need to become either A or A'
                 * Depending if they share an edge with AC or AB
                 * If they don't share an edge probably doesn't matter?
                 */

                int fillIndex = addVertex(caClipped, caColor);
                splitVertex(tri.aIndex, fillIndex);
                triangles.Add(new TriangleEdges(this, fillIndex, tri.aIndex, tri.cIndex));

                tri.a = abClipped;
                tri.aColor = abColor;

                return TriClipStatus.A_Clipped;
            }
            else if (aVis && !bVis && cVis)
            {
                int fillIndex = addVertex(abClipped, abColor);
                splitVertex(tri.bIndex, fillIndex);
                triangles.Add(new TriangleEdges(this, tri.aIndex, fillIndex, tri.bIndex));

                tri.b = bcClipped;
                tri.bColor = bcColor;

                return TriClipStatus.B_Clipped;
            }
            else if (aVis && bVis && !cVis)
            {
                int fillIndex = addVertex(bcClipped, bcColor);
                splitVertex(tri.cIndex, fillIndex);
                triangles.Add(new TriangleEdges(this, tri.cIndex, tri.bIndex, fillIndex));

                tri.c = caClipped;
                tri.cColor = caColor;

                return TriClipStatus.C_Clipped;
            }
            // Impossible
            return TriClipStatus.NotClipped;
        }
    }

    public struct Triangle(Vector3 a, Vector3 b, Vector3 c, uint aColor, uint bColor, uint cColor)
    {
        public Vector3 a = a;
        public Vector3 b = b;
        public Vector3 c = c;
        public uint aColor = aColor;
        public uint bColor = bColor;
        public uint cColor = cColor;

        public readonly Vector3 ab => b - a;
        public readonly Vector3 bc => c - b;
        public readonly Vector3 ca => a - c;
    }

    /**
     * Clip reference variable triangle to supplied plane.
     * If only 1 corner is clipped the output is a quad; The quadfill output is populated with a second triangle to fill the quad.
     */
    public static TriClipStatus ClipTriangleToPlane(in Vector4 plane, ref Triangle tri, out Triangle quadfill)
    {
        quadfill = tri;
        var aDot = Vector4.Dot(new(tri.a, 1), plane);
        var bDot = Vector4.Dot(new(tri.b, 1), plane);
        var cDot = Vector4.Dot(new(tri.c, 1), plane);

        bool aVis = aDot > 0;
        bool bVis = bDot > 0;
        bool cVis = cDot > 0;

        if (aVis && bVis && cVis)
            return TriClipStatus.NotClipped;
        if (!aVis && !bVis && !cVis)
            return TriClipStatus.NotVisible;
        Vector3 plane3 = new(plane.X, plane.Y, plane.Z);

        var abT = -aDot / Vector3.Dot(tri.ab, plane3);
        var bcT = -bDot / Vector3.Dot(tri.bc, plane3);
        var caT = -cDot / Vector3.Dot(tri.ca, plane3);

        var abClipped = tri.a + tri.ab * abT;
        var bcClipped = tri.b + tri.bc * bcT;
        var caClipped = tri.c + tri.ca * caT;

        var abColor = Lerp(tri.aColor, tri.bColor, abT);
        var bcColor = Lerp(tri.bColor, tri.cColor, bcT);
        var caColor = Lerp(tri.cColor, tri.aColor, caT);

        // 1 visible, 2 clipped
        if (aVis && !bVis && !cVis)
        {
            tri.b = abClipped;
            tri.c = caClipped;
            tri.bColor = abColor;
            tri.cColor = caColor;
            return TriClipStatus.BC_Clipped;
        }
        else if (!aVis && bVis && !cVis)
        {
            tri.a = abClipped;
            tri.c = bcClipped;
            tri.aColor = abColor;
            tri.cColor = bcColor;
            return TriClipStatus.AC_Clipped;
        }
        else if (!aVis && !bVis && cVis)
        {
            tri.a = caClipped;
            tri.b = bcClipped;
            tri.aColor = caColor;
            tri.bColor = bcColor;
            return TriClipStatus.AB_Clipped;
        }
        // 2 visible, 1 clipped
        else if (!aVis && bVis && cVis)
        {
            tri.a = quadfill.b = abClipped;
            quadfill.a = caClipped;
            tri.aColor = quadfill.bColor = abColor;
            quadfill.aColor = caColor;
            return TriClipStatus.A_Clipped;
        }
        else if (aVis && !bVis && cVis)
        {
            tri.b = quadfill.c = bcClipped;
            quadfill.b = abClipped;
            tri.bColor = quadfill.cColor = bcColor;
            quadfill.bColor = abColor;
            return TriClipStatus.B_Clipped;
        }
        else if (aVis && bVis && !cVis)
        {
            tri.c = quadfill.a = caClipped;
            quadfill.c = bcClipped;
            tri.cColor = quadfill.aColor = caColor;
            quadfill.cColor = bcColor;
            return TriClipStatus.C_Clipped;
        }
        // Impossible
        return TriClipStatus.NotClipped;
    }
}