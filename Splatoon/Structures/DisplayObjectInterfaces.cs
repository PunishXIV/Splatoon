

namespace Splatoon.Structures;

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

public class DisplayObjectCircle : DisplayObject
{
    public float x, y, z, radius, thickness;
    public uint color;
    public bool filled;
    public DisplayObjectCircle(float x, float y, float z, float radius, float thickness, uint color, bool filled)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.radius = radius;
        this.thickness = thickness;
        this.color = color;
        this.filled = filled;
    }
}

public class DisplayObjectDonut : DisplayObject
{
    public float x, y, z, radius, donut;
    public uint color;
    public DisplayObjectDonut(float x, float y, float z, float radius, float donut, uint color)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.radius = radius;
        this.donut = donut;
        this.color = color;
    }
}

public class DisplayObjectLine : DisplayObject
{
    public float ax, ay, az, bx, by, bz, thickness;
    public uint color;

    public DisplayObjectLine(float ax, float ay, float az, float bx, float by, float bz, float thickness, uint color)
    {
        this.ax = ax;
        this.ay = ay;
        this.az = az;
        this.bx = bx;
        this.by = by;
        this.bz = bz;
        this.thickness = thickness;
        this.color = color;
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

