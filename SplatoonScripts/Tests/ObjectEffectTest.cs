using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests
{
    public class ObjectEffectTest : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new();

        public override void OnObjectEffect(uint target, ushort data1, ushort data2)
        {
            PluginLog.Information($"Object effect on {target.GetObject()} = {data1}, {data2}");
        }
    }
}
