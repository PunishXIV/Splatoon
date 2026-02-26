using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Generic;

public unsafe class LittleLadiesDay2026AutoFarm : SplatoonScript
{
    public override Metadata Metadata { get; } = new(2, "NightmareXIV, Knightmore");
    public override HashSet<uint>? ValidTerritories { get; } = [130];

    Dictionary<uint, uint> DataIdToActionId = new()
    {
        [18859] = 44501,
        [18860] = 44502,
        [18861] = 44503,
        [18862] = 44504
    };

    List<IBattleChara> AllChara => Svc.Objects.OfType<IBattleChara>().Where(x => x.BaseId.EqualsAny(this.DataIdToActionId.Keys) && x.IsTargetable && Player.DistanceTo(x) < 35f).ToList();

    public override void OnUpdate()
    {
        if(Vector2.Distance(new(-37.500f, -140.000f), Player.Position.ToVector2()) < 20)
        {
            if(Player.Status.Any(x => x.StatusId == 1494))
            {
                var fate = FateManager.Instance()->CurrentFate;
                if(fate != null && fate->FateId != 0)
                {
                    foreach(var (baseId, actionId) in this.DataIdToActionId)
                    {
                        var target = AllChara.Shuffle().FirstOrDefault(x => x.BaseId == baseId);
                        if(target == null) continue;

                        if(EzThrottler.Check("UseAction") && EzThrottler.Check($"Use{AllChara.Select(x => x.DataId).Print()}"))
                        {
                            EzThrottler.Throttle("UseAction", 3000);
                            Svc.Targets.Target = target;
                            Controller.Schedule(() =>
                            {
                                if(Svc.Targets.Target != null)
                                {
                                    ActionManager.Instance()->UseAction(ActionType.Action, actionId, target.ObjectId);
                                }
                            }, Random.Shared.Next(1000));

                            DuoLog.Information($"Use action {ExcelActionHelper.GetActionName(actionId, true)}");
                        }
                        break;
                    }
                }
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Source.AddressEquals(Player.Object) && set.Target?.DataId.EqualsAny(this.DataIdToActionId.Keys) == true)
        {
            EzThrottler.Throttle($"Use{AllChara.Select(x => x.DataId).Print()}", 12000, true);
        }
    }

    public override void OnSettingsDraw()
    {
        if(ImGui.CollapsingHeader("Debug"))
        {
            EzThrottler.ImGuiPrintDebugInfo();
        }
    }
}
