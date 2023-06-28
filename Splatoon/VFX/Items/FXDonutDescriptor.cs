using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.VFX.Items
{
    internal unsafe readonly record struct FXDonutDescriptor : IItemDescriptor<FXDonut>
    {
        string IItemDescriptor<FXDonut>.Name => this.ToString();

        internal readonly float Brightness;
        internal readonly uint Color;
        internal readonly float RadiusInner;
        internal readonly float RadiusOuter;

        public FXDonutDescriptor(float brightness, uint color, float radiusInner, float radiusOuter) : this()
        {
            Brightness = brightness;
            Color = color;
            RadiusInner = radiusInner;
            RadiusOuter = radiusOuter;
        }

        public override string ToString()
        {
            return $"d{Brightness.AsUInt32():X8}{Color:X8}{RadiusInner.AsUInt32():X8}{RadiusOuter.AsUInt32():X8}.avfx";
        }

        void IItemDescriptor<FXDonut>.Transform(FXDonut* reference)
        {
            var v4 = this.Color.ToVector4();
            var alpha = v4.W * 2;
            reference->Color = new(v4.X * alpha, v4.Y * alpha, v4.Z * alpha);
            var scl = (RadiusInner + RadiusOuter / 2) * FXDonutController.baseRadius;
            reference->ScaleX = scl;
            reference->ScaleY = scl;
            reference->ScaleZ = scl;
        }
    }
}
