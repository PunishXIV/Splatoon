using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Generic;

public unsafe class LittleLadiesDay2026AutoFarm : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV, Knightmore");
    public override HashSet<uint>? ValidTerritories { get; } = [130];

    Dictionary<uint, uint> DataIdToActionId = new()
    {
        [18859] = 44501,
        [18860] = 44502,
        [18861] = 44503,
        [18862] = 44504
    };

    public override void OnUpdate()
    {
        if(Player.Status.Any(x => x.StatusId == 1494))
        {
            var fate = FateManager.Instance()->CurrentFate;
            if(fate != null && fate->FateId == 2044)
            {
                var allChara = Svc.Objects.OfType<IBattleNpc>().Where(x => x.IsTargetable && Player.DistanceTo(x) < 35f).ToList();
                foreach(var (baseId, actionId) in this.DataIdToActionId)
                {
                    var target = allChara.FirstOrDefault(x => x.BaseId == baseId);
                    if(target == null) continue;

                    if(EzThrottler.Check("UseAction") && EzThrottler.Check($"Use{target.DataId}"))
                    {
                        EzThrottler.Throttle("UseAction", 1000);
                        Svc.Targets.Target = target;
                        ActionManager.Instance()->UseAction(ActionType.Action, actionId, target.ObjectId);
                        DuoLog.Information($"Use action {ExcelActionHelper.GetActionName(actionId, true)}");
                    }
                    break;
                }
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Source.AddressEquals(Player.Object) && set.Target?.DataId.EqualsAny(this.DataIdToActionId.Keys) == true)
        {
            EzThrottler.Throttle($"Use{set.Target?.DataId}", 10000, true);
        }
    }
}
