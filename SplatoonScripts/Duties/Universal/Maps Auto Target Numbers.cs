using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using ECommons.Throttlers;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Universal;
public unsafe sealed class Maps_Auto_Target_Numbers : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [.. Svc.Data.GetExcelSheet<TerritoryType>().Where(x => x.GetTerritoryIntendedUse() == TerritoryIntendedUseEnum.Treasure_Map_Duty).Select(x => x.RowId)];


    //NamePlateIconId:  60687 1
    //NamePlateIconId:  60688 2
    //NamePlateIconId:  60689 3
    //NamePlateIconId:  60690 4
    //NamePlateIconId:  60691 5

    uint[][] Numbers = [
        [60687],
        [60688],
        [60689],
        [60690],
        [60691],
        ];

    public override void OnUpdate()
    {
        var obj = Svc.Objects.OfType<IBattleNpc>().Where(x => x.IsTargetable && !x.IsDead && x.IsHostile());
        IBattleNpc? findOtherEligibleTarget() => obj.Where(x => !Numbers.Any(n => n.Contains(x.Struct()->NamePlateIconId))).Where(x => Player.DistanceTo(x) < (Player.Job.IsMeleeDps() || Player.Job.IsTank() ? 3f : 25f) + x.HitboxRadius).FirstOrDefault();
        int? lowestNumber = null;
        for(var i = 0; i < Numbers.Length; i++)
        {
            var number = Numbers[i];
            if(obj.TryGetFirst(x => x.Struct()->NamePlateIconId.EqualsAny(number), out var result))
            {
                EzThrottler.Throttle("AllowReswitching", 500, true);
                if(lowestNumber == null)
                {
                    lowestNumber = i;
                    if(!Svc.Targets.Target.AddressEquals(result) && Player.DistanceTo(result) < (Player.Job.IsMeleeDps() || Player.Job.IsTank()?3f:25f) + result.HitboxRadius)
                    {
                        SetTarget(result);
                    }
                }
                else
                {
                    if(Svc.Targets.Target.AddressEquals(result))
                    {
                        var candidate = findOtherEligibleTarget();
                        SetTarget(candidate);
                    }
                }
            }
        }
        if(lowestNumber == null)
        {
            if(!EzThrottler.Check("AllowReswitching"))
            {
                var candidate = findOtherEligibleTarget();
                SetTarget(candidate);
            }
        }
    }

    void SetTarget(IGameObject? obj)
    {
        if(EzThrottler.Throttle("SetTarget", 200))
        {
            if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
            {
                if(!Svc.Targets.Target.AddressEquals(obj)) Svc.Targets.Target = obj;
            }
            else
            {
                DuoLog.Information($"Would target: {obj}");
            }
        }
    }
}