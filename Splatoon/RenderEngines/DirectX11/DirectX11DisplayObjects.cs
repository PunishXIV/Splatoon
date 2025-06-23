using ECommons.MathHelpers;
using Splatoon.Serializables;
using Splatoon.Structures;

namespace Splatoon.RenderEngines.DirectX11;
public class DirectX11DisplayObjects
{
    public class VfxDisplayObject : DisplayObject
    {
        public string id;
    }

    public class DisplayObjectDot : VfxDisplayObject
    {
        public float x, y, z, thickness;
        public uint color;

        public DisplayObjectDot(string id, float x, float y, float z, float thickness, uint color)
        {
            this.id = id;
            this.x = x;
            this.y = y;
            this.z = z;
            this.thickness = thickness;
            this.color = color;
        }
    }

    public class DisplayObjectFan : VfxDisplayObject
    {
        public readonly Vector3 origin;

        public readonly float innerRadius, outerRadius, angleMin, angleMax;
        public readonly DisplayStyle style;
        public DisplayObjectFan(string id, Vector3 origin, float innerRadius, float outerRadius, float angleMin, float angleMax, DisplayStyle style)
        {
            this.id = id;
            this.origin = origin;
            this.innerRadius = innerRadius;
            this.outerRadius = outerRadius;
            this.angleMin = angleMin;
            this.angleMax = angleMax;
            this.style = style;
        }
    }

    public class DisplayObjectCircle : DisplayObjectFan
    {
        public DisplayObjectCircle(string id, Vector3 origin, float radius, DisplayStyle style) : base(id, origin, 0, radius, 0, 2 * MathF.PI, style)
        {
        }
    }

    public class DisplayObjectDonut : DisplayObjectFan
    {
        public DisplayObjectDonut(string id, Vector3 origin, float innerRadius, float donutRadius, DisplayStyle style) : base(id, origin, innerRadius, innerRadius + donutRadius, 0, 2 * MathF.PI, style)
        {
        }
    }

    public class DisplayObjectLine : VfxDisplayObject
    {
        public readonly Vector3 start, stop;
        public readonly float radius;
        public readonly DisplayStyle style;
        public readonly LineEnd startStyle, endStyle;

        public DisplayObjectLine(string id, Vector3 start, Vector3 stop, float radius, DisplayStyle style, LineEnd startStyle = LineEnd.None, LineEnd endStyle = LineEnd.None)
        {
            this.id = id;
            this.start = start;
            this.stop = stop;
            this.radius = radius;
            this.style = style;
            this.startStyle = startStyle;
            this.endStyle = endStyle;
        }

        public DisplayObjectLine(string id, float ax, float ay, float az, float bx, float by, float bz, float thickness, uint color, LineEnd startStyle = LineEnd.None, LineEnd endStyle = LineEnd.None)
        {
            start = new Vector3(ax, az, ay);
            stop = new Vector3(bx, bz, by);
            radius = 0;
            style = new DisplayStyle(color, thickness, 0f, 0, 0);
            this.startStyle = startStyle;
            this.endStyle = endStyle;
        }
        public float Length
        {
            get
            {
                return (stop - start).Length();
            }
        }
        public Vector3 Direction
        {
            get
            {
                return Vector3.Normalize(stop - start);
            }
        }
        public Vector3 Perpendicular
        {
            get
            {
                return Vector3.Normalize(Vector3.Cross(Direction, Vector3.UnitY));
            }
        }

        public Vector3 PerpendicularRadius
        {
            get
            {
                return Perpendicular * radius;
            }
        }

        public Vector2[] Bounds
        {
            get
            {
                return [
                    (start - PerpendicularRadius).ToVector2(),
                    (start + PerpendicularRadius).ToVector2(),
                    (stop - PerpendicularRadius).ToVector2(),
                    (stop + PerpendicularRadius).ToVector2(),
                ];
            }
        }
    }
    public class DisplayObjectText : VfxDisplayObject
    {
        public float x, y, z, fscale;
        public string text;
        public uint bgcolor, fgcolor;
        public DisplayObjectText(string id, float x, float y, float z, string text, uint bgcolor, uint fgcolor, float fscale)
        {
            this.id = id;
            this.x = x;
            this.y = y;
            this.z = z;
            this.text = text;
            this.bgcolor = bgcolor;
            this.fgcolor = fgcolor;
            this.fscale = fscale;
        }
    }
}
