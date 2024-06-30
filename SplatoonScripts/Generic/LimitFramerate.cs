using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class LimitFramerate : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;
    long LastFrame;

    public override void OnUpdate()
    {
        if (Framework.Instance()->WindowInactive
            && !Svc.Condition[ConditionFlag.OccupiedInQuestEvent])
        {
            var diff = Environment.TickCount64 - LastFrame;
            if (diff >= 0 && diff < 16)
            {
                Thread.Sleep((int)(16 - diff));
            }
            LastFrame = Environment.TickCount64;
        }
    }
}
