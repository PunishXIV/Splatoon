using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Generic;
public class AutoDutyEnableTankStance : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [];
    [EzIPC] public Func<bool> IsStopped;

    public override void OnSetup()
    {
        EzIPC.Init(this, "AutoDuty", SafeWrapper.AnyException);
    }

    public override void OnUpdate()
    {
        if(Svc.Condition[ConditionFlag.BoundByDuty56] && !Player.IsAnimationLocked && EzThrottler.Throttle($"{nameof(AutoDutyEnableTankStance)}_EnableTS", 5000))
        {
            if(!IsStopped() && Player.Interactable && !Player.Object.StatusList.Any(x => x.StatusId.EqualsAny<uint>(1833)))
            {
                if(Player.Job == Job.GNB)
                {
                    Chat.Instance.ExecuteAction(16142);
                }
            }
        }
    }
}
