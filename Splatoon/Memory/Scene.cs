using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Memory
{
    public static unsafe class Scene
    {
        internal static byte* ActiveScene = null;
        internal static void Init()
        {
            var n = (nint)EnvManager.Instance();
            if (n == nint.Zero)
            {
                PluginLog.Error($"EnvManager was zero");
            }
            else
            {
                ActiveScene = (byte*)(n + 36);
            }
        }
    }
}
