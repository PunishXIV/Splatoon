using ECommons.GameHelpers;
using ECommons.MathHelpers;
using Splatoon.Structures;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Modules
{
    internal class UnsafeElement
    {
        internal bool[] IsUnsafeElement;
        internal HashSet<string> UnsafeElementRequesters = [];
        internal UnsafeElement()
        {
            try
            {
                IsUnsafeElement = Svc.PluginInterface.GetOrCreateData<bool[]>("Splatoon.IsInUnsafeZone", () => [false]);
                UnsafeElementRequesters = Svc.PluginInterface.GetOrCreateData<HashSet<string>>("Splatoon.UnsafeElementRequesters", () => []);
            }
            catch(Exception e)
            {
                e.Log();
            }
        }

        internal bool IsEnabled => UnsafeElementRequesters.Count > 0;

        internal void ProcessCircle(Vector3 Middle, float Radius)
        {
            if(!Player.Available) return;
            var dist = Vector3.Distance(Player.Object.Position, Middle);
            if (dist < Radius) IsUnsafeElement[0] = true;
        }

        internal void ProcessDonut(Vector3 Middle, float Radius, float donutRadius)
        {
            if (!Player.Available) return;
            var dist = Vector3.Distance(Player.Object.Position, Middle);
            if (dist.InRange(Radius, Radius + donutRadius)) IsUnsafeElement[0] = true;
        }

        internal void ProcessLine(DisplayObjectLine line)
        {
            if (!Player.Available) return;
            var p = Utils.GetPlayerPositionXZY();
            if (PointInPolygon(p.X, p.Y, line.Bounds))
            {
                IsUnsafeElement[0] = true;
            }
        }

        // Return True if the point is in the polygon.
        public bool PointInPolygon(float X, float Y, Vector2[] Points)
        {
            // Get the angle between the point and the
            // first and last vertices.
            int max_point = Points.Length - 1;
            float total_angle = GetAngle(
                Points[max_point].X, Points[max_point].Y,
                X, Y,
                Points[0].X, Points[0].Y);

            // Add the angles from the point
            // to each other pair of vertices.
            for (int i = 0; i < max_point; i++)
            {
                total_angle += GetAngle(
                    Points[i].X, Points[i].Y,
                    X, Y,
                    Points[i + 1].X, Points[i + 1].Y);
            }

            // The total angle should be 2 * PI or -2 * PI if
            // the point is in the polygon and close to zero
            // if the point is outside the polygon.
            return (Math.Abs(total_angle) > 0.000001);
        }
        // Return the angle ABC.
        // Return a value between PI and -PI.
        // Note that the value is the opposite of what you might
        // expect because Y coordinates increase downward.
        public static float GetAngle(float Ax, float Ay,
            float Bx, float By, float Cx, float Cy)
        {
            // Get the dot product.
            float dot_product = DotProduct(Ax, Ay, Bx, By, Cx, Cy);

            // Get the cross product.
            float cross_product = CrossProductLength(Ax, Ay, Bx, By, Cx, Cy);

            // Calculate the angle.
            return (float)Math.Atan2(cross_product, dot_product);
        }
        // Return the dot product AB . BC.
        // Note that AB x BC = |AB| * |BC| * Cos(theta).
        private static float DotProduct(float Ax, float Ay,
            float Bx, float By, float Cx, float Cy)
        {
            // Get the vectors' coordinates.
            float BAx = Ax - Bx;
            float BAy = Ay - By;
            float BCx = Cx - Bx;
            float BCy = Cy - By;

            // Calculate the dot product.
            return (BAx * BCx + BAy * BCy);
        }
        // Return the cross product AB x BC.
        // The cross product is a vector perpendicular to AB
        // and BC having length |AB| * |BC| * Sin(theta) and
        // with direction given by the right-hand rule.
        // For two vectors in the X-Y plane, the result is a
        // vector with X and Y components 0 so the Z component
        // gives the vector's length and direction.
        public static float CrossProductLength(float Ax, float Ay,
            float Bx, float By, float Cx, float Cy)
        {
            // Get the vectors' coordinates.
            float BAx = Ax - Bx;
            float BAy = Ay - By;
            float BCx = Cx - Bx;
            float BCy = Cy - By;

            // Calculate the Z coordinate of the cross product.
            return (BAx * BCy - BAy * BCx);
        }
    }
}
