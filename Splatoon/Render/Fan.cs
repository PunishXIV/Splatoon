using Splatoon.Serializables;
using Splatoon.Structures;

namespace Splatoon.Render;
public class Fan
{
    public static void Stroke(Stroke.Data.Builder stroke, DisplayObjectFan fan, int segments)
    {
        Vector3 origin = XZY(fan.origin);
        DisplayStyle style = fan.style;
        float totalAngle = fan.angleMax - fan.angleMin;
        float angleStep = totalAngle / segments;

        bool isCircle = totalAngle == MathF.PI * 2;

        if (isCircle)
        {
            Vector3[] outerPoints = new Vector3[segments];
            Vector3[] innerPoints = new Vector3[segments];

            for (int step = 0; step < segments; step++)
            {
                float angle = fan.angleMin + step * angleStep;
                Vector3 offset = new(MathF.Cos(angle), 0, MathF.Sin(angle));
                outerPoints[step] = origin + fan.outerRadius * offset;
                innerPoints[step] = origin + fan.innerRadius * offset;
            }

            if (fan.innerRadius > 0)
            {
                stroke.Add(innerPoints, style.strokeThickness, style.strokeColor.ToVector4(), true);
            }
            stroke.Add(outerPoints, style.strokeThickness, style.strokeColor.ToVector4(), true);
        }
        else
        {
            int vertexCount = segments + 1;
            if (fan.innerRadius > 0)
            {
                vertexCount *= 2;
            }
            else
            {
                vertexCount += 1;
            }
            Vector3[] points = new Vector3[vertexCount];

            for (int step = 0; step <= segments; step++)
            {
                float angle = MathF.PI / 2 + fan.angleMin + step * angleStep;
                Vector3 offset = new(MathF.Cos(angle), 0, MathF.Sin(angle));
                points[step] = origin + fan.outerRadius * offset;
                if (fan.innerRadius > 0)
                {
                    int innerIndex = vertexCount - step - 1;
                    points[innerIndex] = origin + fan.innerRadius * offset;
                }
            }
            if (fan.innerRadius == 0)
            {
                points[segments + 1] = origin;
            }
            stroke.Add(points, style.strokeThickness, style.strokeColor.ToVector4(), true);
        }
    }
}
