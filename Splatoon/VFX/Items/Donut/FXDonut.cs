using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.VFX.Items.Donut
{
    [StructLayout(LayoutKind.Explicit)]
    internal record struct FXDonut
    {
        [FieldOffset(0x184)] internal float ScaleX;
        [FieldOffset(0x190)] internal float ScaleY;
        [FieldOffset(0x19C)] internal float ScaleZ;
        [FieldOffset(0x1F38)] internal Vector3 Color;
        [FieldOffset(0x2310)] internal float DonutRadiusThickness;
    }
}
