using ECommons.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Modules;
internal static class EmulatedCombatTimer
{
    public static long CombatStart = 0;

    public static void Tick()
    {
        if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback])
        {
            if(CombatStart == 0)
            {
                if(Svc.Objects.OfType<IBattleNpc>().Any(x => x.IsTargetable && x.CurrentHp < x.MaxHp))
                {
                    CombatStart = Environment.TickCount64;
                    PluginLog.Debug($"Combat simulation starts");
                }
            }
        }
    }

    public static void OnDirectorUpdate(DirectorUpdateCategory cat)
    {
        if(cat == DirectorUpdateCategory.Commence || cat == DirectorUpdateCategory.Recommence || cat == DirectorUpdateCategory.Wipe)
        {
            CombatStart = 0;
            if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback])
            {
                P.CombatEnded();
            }
        }
    }

    public static bool IsInCombat()
    {
        return CombatStart != 0;
    }

    public static long GetCombatTimerMs()
    {
        return Environment.TickCount64 - CombatStart;
    }

    public static float GetCombatTimerSeconds()
    {
        return (float)GetCombatTimerMs() / 1000f;
    }
}