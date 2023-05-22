using ECommons.Reflection;

namespace Splatoon.Structures;

internal class S2WInfo
{
    string x;
    string y;
    string z;
    object cls;

    internal S2WInfo(object cls, string x, string y, string z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.cls = cls;
    }

    internal void Apply(float xf, float yf, float zf)
    {
        cls.SetFoP(x, xf);
        cls.SetFoP(y, yf);
        cls.SetFoP(z, zf);
    }

    internal (float x, float y, float z) GetValues()
    {
        return (cls.GetFoP<float>(x), cls.GetFoP<float>(y), cls.GetFoP<float>(z));
    }
}
