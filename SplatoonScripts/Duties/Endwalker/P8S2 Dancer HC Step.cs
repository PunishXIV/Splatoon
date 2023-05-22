using Dalamud.Game.ClientState.JobGauge.Types;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    internal class P8S2_Dancer_HC_Step : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1088 };
        public override Metadata? Metadata => new(1, "NightmareXIV");
        long castAt = long.MaxValue;

        public override void OnSetup()
        {
            _ = Chat.Instance;
        }

        public override void OnUpdate()
        {
            if (this.Controller.InCombat && !Svc.Gauges.Get<DNCGauge>().IsDancing)
            {
                if (Environment.TickCount64 > castAt)
                {
                    DuoLog.Information($"Casting standard step");
                    castAt = long.MaxValue;
                    Chat.Instance.SendMessage("/ac \"Standard Step\"");
                }
            }
        }

        public override void OnMessage(string Message)
        {
            if(Message.Contains("Hephaistos casts High Concept."))
            {
                if(this.Controller.CombatSeconds < 150)
                {
                    //hc1 duration: 42 seconds, step at 42-14=28
                    castAt = Environment.TickCount64 + 27 * 1000;
                    DuoLog.Information($"Enqueued step cast in {(float)(castAt - Environment.TickCount64)/1000f} seconds");
                }
                else
                {
                    //hc2 duration: 42 seconds, step at 42-14=28
                    castAt = Environment.TickCount64 + 27 * 1000;
                    DuoLog.Information($"Enqueued step cast in {(float)(castAt - Environment.TickCount64) / 1000f} seconds");
                }
            }
        }

        public override void OnCombatEnd()
        {
            castAt = long.MaxValue;
        }
    }
}
