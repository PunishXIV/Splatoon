using Dalamud.Game.ClientState.JobGauge.Types;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Hooks;
using ECommons.Logging;
using ECommons.Schedulers;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol
{
    public class Dancer_Auto_Step : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1122 };
        TickScheduler? Sch = null;

        public override void OnMessage(string Message)
        {
            if(Message == "Experiment concluded. I am the Alpha. I am the Omega.")
            {
                CastTech();
            }
            if(Message == "<blip> Diverting energy to party member generation. Testing efficacy of combat role allocation...")
            {
                DuoLog.Information($"Scheduling cast...");
                Sch?.Dispose();
                Sch = new TickScheduler(CastStd, 22000);
            }
            if (Message.Contains(">31624)"))
            {
                DuoLog.Information($"Scheduling cast (delta)...");
                Sch?.Dispose();
                Sch = new TickScheduler(CastStd, 48000);
            }
            if (Message.Contains(">32788)"))
            {
                DuoLog.Information($"Scheduling cast (sigma)...");
                Sch?.Dispose();
                Sch = new TickScheduler(CastStd, 56000);
            }
            if (Message.Contains(">32789)"))
            {
                DuoLog.Information($"Scheduling cast (omega)...");
                Sch?.Dispose();
                Sch = new TickScheduler(CastStd, 50000);
            }
            if(Message.Contains("You recover from the effect of Down for the Count."))
            {
                DuoLog.Information($"Scheduling cast (p6)...");
                Sch?.Dispose();
                Sch = new TickScheduler(CastTech, 500);
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category.EqualsAny(DirectorUpdateCategory.Wipe, DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence))
            {
                Sch?.Dispose();
            }
        }

        void CastTech()
        {
            DuoLog.Information($"Casting tech...");
            if (!Svc.ClientState.LocalPlayer.IsDead && Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]
                    && !Svc.Gauges.Get<DNCGauge>().IsDancing && EzThrottler.Throttle("DAS.Technical"))
            {
                DuoLog.Information($"Casting tech 2...");
                Chat.Instance.SendMessage("/ac \"Technical Step\"");
            }
        }

        void CastStd()
        {
            DuoLog.Information($"Casting standard...");
            if (!Svc.ClientState.LocalPlayer.IsDead && Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]
                    && !Svc.Gauges.Get<DNCGauge>().IsDancing && EzThrottler.Throttle("DAS.Technical"))
            {
                DuoLog.Information($"Casting standard 2...");
                Chat.Instance.SendMessage("/ac \"Standard Step\"");
            }
        }
    }
}
