using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using Splatoon.VFX.Interfaces;

namespace Splatoon.VFX.Items.Donut
{
    internal unsafe readonly record struct FXDonutDescriptor : ItemDescriptor<FXDonut>
    {
        string ItemDescriptor<FXDonut>.Name => ToString();

        internal readonly Vector3 Pos;
        internal readonly float Brightness;
        internal readonly uint Color;
        internal readonly float RadiusInner;
        internal readonly float RadiusOuter;

        Vector3 ItemDescriptor<FXDonut>.Position => Pos;

        public FXDonutDescriptor(Vector3 position, float brightness, uint color, float radiusInner, float radiusOuter)
        {
            Brightness = brightness;
            Color = color;
            RadiusInner = radiusInner;
            RadiusOuter = radiusOuter;
            Pos = position;
        }

        public override string ToString()
        {
            return $"d{Brightness.AsUInt32():X8}{Color:X8}{RadiusInner.AsUInt32():X8}{RadiusOuter.AsUInt32():X8}.avfx";
        }

        void ItemDescriptor<FXDonut>.Transform(FXDonut* reference)
        {
            var v4 = Color.ToVector4();
            var alpha = v4.W * 2;
            reference->Color = new(v4.X * alpha, v4.Y * alpha, v4.Z * alpha);
            var scl = (RadiusInner + RadiusOuter / 2) / FXDonutController.baseRadius;
            reference->ScaleX = scl;
            reference->ScaleY = 20f;
            reference->ScaleZ = scl;
            reference->DonutRadiusThickness = 0.25f;
            VFXManager.Debug($"Transformed {(nint)reference:X16}");
        }
    }
}
