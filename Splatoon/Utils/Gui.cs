namespace Splatoon.Utils;

public static unsafe class Gui
{
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