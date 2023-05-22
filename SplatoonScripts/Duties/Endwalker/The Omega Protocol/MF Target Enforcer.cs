using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Logging;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol
{
    public class MF_Target_Enforcer : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1122 };

        public override Metadata? Metadata => new(1, "NightmareXIV");

        const string ThrottlerName = "MFTE.Settarget";

        nint? Target = null;

        public class Effects
        {
            public const uint NoAttackM = 3499;
            public const uint NoAttackF = 3500;
            public const uint MaleForm = 3454;
            public const uint FemaleForm = 1675;
            public const uint Invulnerability = 671;
        }

        public override void OnUpdate()
        {
            if(Svc.Targets.Target?.Address != Target)
            {
                Target = Svc.Targets.Target?.Address;
                EzThrottler.Throttle(ThrottlerName, 200, true);
            }
            if (!Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat] || Svc.ClientState.LocalPlayer == null) return;
            {
                if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == Effects.NoAttackM) && Svc.Targets.Target is BattleNpc b && b.StatusList.Any(x => x.StatusId == Effects.MaleForm))
                {
                    var female = Svc.Objects.FirstOrDefault(x => x is BattleNpc b && !b.IsDead && b.IsTargetable() && b.StatusList.Any(z => z.StatusId == Effects.FemaleForm));
                    if (female != null)
                    {
                        if (EzThrottler.Throttle(ThrottlerName, 200))
                        {
                            DuoLog.Information($"Setting female target");
                            Svc.Targets.SetTarget(female);
                        }
                    }
                }
            }

            {
                if (Svc.Targets.Target is BattleNpc b && b.StatusList.Any(x => x.StatusId == Effects.FemaleForm) && (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == Effects.NoAttackF) || b.StatusList.Any(x => x.StatusId == Effects.Invulnerability)))
                {
                    var male = Svc.Objects.FirstOrDefault(x => x is BattleNpc b && !b.IsDead && b.IsTargetable() && b.StatusList.Any(z => z.StatusId == Effects.MaleForm));
                    if (male != null)
                    {
                        if (EzThrottler.Throttle(ThrottlerName, 200))
                        {
                            DuoLog.Information($"Setting male target");
                            Svc.Targets.SetTarget(male);
                        }
                    }
                }
            }
        }

        public override void OnMessage(string Message)
        {
            if (!Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]) return;
            if (Message == "Omega-F uses Limitless Synergy.")
            {
                var male = Svc.Objects.FirstOrDefault(x => x is BattleNpc b && !b.IsDead && b.IsTargetable() && b.StatusList.Any(z => z.StatusId == Effects.MaleForm));
                if (male != null)
                {
                    if (EzThrottler.Throttle(ThrottlerName, 200))
                    {
                        DuoLog.Information($"Setting male target");
                        Svc.Targets.SetTarget(male);
                    }
                }
            }
        }
    }
}
