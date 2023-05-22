using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using ECommons.Hooks;
using Lumina.Data.Parsing.Tex.Buffers;
using Splatoon.Modules;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Memory
{
    internal unsafe static class DirectorUpdateProcessor
    {
        internal static void ProcessDirectorUpdate(long a1, long a2, DirectorUpdateCategory a3, uint a4, uint a5, int a6, int a7)
        {
            if (P.Config.Logging)
            {
                var text = $"Director Update: {a3:X}, {a4:X8}, {a5:X8}, {a6:X8}, {a7:X8}";
                Logger.Log(text);
                PluginLog.Verbose(text);
            }
            ScriptingProcessor.OnDirectorUpdate(a3);
        }
    }
}
