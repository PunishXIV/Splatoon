using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Splatoon.VFX.Interfaces;

namespace Splatoon.VFX.Items.Donut
{
    internal unsafe class FXDonutController : ItemController<FXDonut, FXDonutDescriptor>
    {
        internal override string FileName => "fxdonut.avfx";
        internal const float baseRadius = 4f;
    }
}
