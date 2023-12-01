

using ECommons.MathHelpers;

namespace Splatoon.Structures;

public struct DisplayStyle
{
    public readonly uint strokeColor;
    public readonly float strokeThickness;
    public readonly uint originFillColor;
    public readonly uint endFillColor;

    public DisplayStyle(uint strokeColor, float strokeThickness, uint originFillColor, uint endFillColor)
    {
        this.strokeColor = strokeColor;
        this.strokeThickness = strokeThickness;
        this.originFillColor = originFillColor;
        this.endFillColor = endFillColor;
    }

    public readonly uint fillColor(float amount)
    {
        return Lerp(originFillColor, endFillColor, amount);
    }
}

public class DisplayObjectDot : DisplayObject
{
    public float x, y, z, thickness;
    public uint color;

    public DisplayObjectDot(float x, float y, float z, float thickness, uint color)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.thickness = thickness;
        this.color = color;
    }
}

public class DisplayObjectFan : DisplayObject
{
    public Vector3 origin;

    public float radius, angleMin, angleMax;
    public DisplayStyle style;
    public DisplayObjectFan(Vector3 origin, float radius, float angleMin, float angleMax, DisplayStyle style)
    {
        this.origin = origin;
        this.radius = radius;
        this.angleMin = angleMin;
        this.angleMax = angleMax;
        this.style = style;
    }
}

public class DisplayObjectCircle : DisplayObjectFan
{
    public DisplayObjectCircle(Vector3 origin, float radius, DisplayStyle style) : base(origin, radius, 0, 2 * MathF.PI, style)
    {

    }
}

public class DisplayObjectDonut : DisplayObjectCircle
{
    public float donutRadius;
    public DisplayObjectDonut(Vector3 origin, float innerRadius, float donutRadius, DisplayStyle style) : base(origin, innerRadius, style)
    {
        this.donutRadius = donutRadius;
    }
}

public class DisplayObjectLine : DisplayObject
{
    public readonly Vector3 start, stop;
    public readonly float radius;
    public readonly DisplayStyle style;

    public DisplayObjectLine(Vector3 start, Vector3 stop, float radius, DisplayStyle style)
    {
        this.start = start;
        this.stop = stop;
        this.radius = radius;
        this.style = style;
    }

    public DisplayObjectLine(float ax, float ay, float az, float bx, float by, float bz, float thickness, uint color)
    {
        this.start = new Vector3(ax, az, ay);
        this.stop = new Vector3(bx, bz, by);
        this.radius = 0;
        this.style = new DisplayStyle(color, thickness, 0, 0);
    }
    public Vector3 Direction
    {
        get
        {
            return stop - start;
        }
    }

    public Vector3 PerpendicularRadius
    {
        get
        {
            return radius * Vector3.Normalize(Vector3.Cross(Direction, Vector3.UnitY));
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
public class DisplayObjectText : DisplayObject
{
    public float x, y, z, fscale;
    public string text;
    public uint bgcolor, fgcolor;
    public DisplayObjectText(float x, float y, float z, string text, uint bgcolor, uint fgcolor, float fscale)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.text = text;
        this.bgcolor = bgcolor;
        this.fgcolor = fgcolor;
        this.fscale = fscale;
    }
}
public class DisplayObjectRect : DisplayObject
{
    public DisplayObjectLine l1;
    public DisplayObjectLine l2;
}
public class DisplayObjectPolygon : DisplayObject
{
    public Element e;
    public DisplayObjectPolygon(Element e)
    {
        this.e = e;
    }
}
public interface DisplayObject { }

