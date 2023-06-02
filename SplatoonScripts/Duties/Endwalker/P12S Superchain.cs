using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P12S_Superchain : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1154 };
        public override Metadata? Metadata => new(1, "NightmareXIV");

        const uint ID_Mastersphere = 16176;
        const uint ID_AOEBall = 16177;
        const uint ID_Protean = 16179;
        const uint ID_Donut = 16178;

        Dictionary<uint, uint[]> Attachments = new();

        public override void OnTetherCreate(uint source, uint target, byte data2, byte data3, byte data5)
        {
            var t = target.GetObject();
            var s = source.GetObject();
            if (t != null && t.DataId == ID_Mastersphere)
            {
                if(s != null)
                {
                    if(s.DataId == ID_AOEBall)
                    {
                        DuoLog.Information($"AOE");
                    }
                    if(s.DataId == ID_Donut)
                    {
                        DuoLog.Information($"Donut");
                    }
                }
            }
            if(s != null && s.DataId == ID_Mastersphere)
            {
                DuoLog.Information($"Mastersphere");
            }
        }
    }
}
