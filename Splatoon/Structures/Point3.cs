namespace Splatoon.Structures;

[Serializable]
public class Point3
{
    public float X = 0;
    public float Y = 0;
    public float Z = 0;


    public Vector3 ToVector3()
    {
        return new Vector3(X, Y, Z);
    }
}

public static class Vector3Ex
{
    public static Point3 ToPoint3(this Vector3 v)
    {
        return new Point3
        {
            X = v.X,
            Y = v.Y,
            Z = v.Z
        };
    }
}
