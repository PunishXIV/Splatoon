namespace Splatoon.Structures;

[Serializable]
public class Point2
{
    public float X = 0;
    public float Y = 0;


    public Vector2 ToVector2()
    {
        return new(X, Y);
    }
}

public static class Vector2Ex
{
    public static Point2 ToPoint2(this Vector2 v)
    {
        return new()
        {
            X = v.X,
            Y = v.Y,
        };
    }
}
