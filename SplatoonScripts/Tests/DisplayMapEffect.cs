using Dalamud.Game.Gui.FlyText;
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
    public class DisplayMapEffect : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new();

        public override void OnMapEffect(uint position, ushort data1, ushort data2)
        {
            DuoLog.Information($"MapEffect: {position}, {data1}, {data2}");
            
        }
    }
}
