using ECommons.Reflection;

namespace Splatoon.Structures;

internal class S2WInfo
{
    private string x;
    private string y;
    private string z;
    private object cls;

    internal S2WInfo(object cls, string x, string y, string z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.cls = cls;
    }

    internal void Apply(float xf, float yf, float zf)
    {
        if(x != null) cls.SetFoP(x, xf);
        if(y != null) cls.SetFoP(y, yf);
        if(z != null) cls.SetFoP(z, zf);
    }

    internal (float x, float y, float z) GetValues()
    {
        return (cls.GetFoP(x) as float? ?? 0, cls.GetFoP(y) as float? ?? 0, cls.GetFoP(z) as float? ?? 0);
    }
}
