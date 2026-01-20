using System;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers.LegacyPlayer;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace SplatoonScriptsOfficial.Generic;

public unsafe class AutoHeal : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => [];
    public override Metadata? Metadata => new(1, "redmoon");

    private static ActionManager* ActionManager => FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
    private long _lastHealTime = 0;

    public override void OnUpdate()
    {
        // 周期 1秒
        if (EzThrottler.Throttle("AutoHeal", 1000)) return;
        if (!IsCombat()) return;
        // スキル後10秒経過していたら初期化
        if (Environment.TickCount - _lastHealTime > 10000) _lastHealTime = 0;
        var basePlayer = Splatoon.Splatoon.BasePlayer;
        if (basePlayer.GetRole() != CombatRole.DPS) return;
        if (basePlayer.IsDead) return;
        if (_lastHealTime != 0) return;
        // HPが閾値以下
        if (basePlayer.CurrentHp * 100 / basePlayer.MaxHp > Controller.GetConfig<Config>().thresholdHpPercent) return;

        // Bloodbath (MNK)
        if (basePlayer.GetJob() is Job.MNK or Job.DRG or Job.NIN or Job.SAM or Job.RPR or Job.VPR)
        {
            if (!ActionManager->IsRecastTimerActive(ActionType.Action, 7542u))
            {
                ActionManager->UseAction(ActionType.Action, 7542u);
                if (ActionManager->IsRecastTimerActive(ActionType.Action, 7542u) ||
                    Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                    _lastHealTime = Environment.TickCount;

                if (Svc.Condition[ConditionFlag.DutyRecorderPlayback]) DuoLog.Information("Used Bloodbath");
            }
        }

        // Second Wind
        if (basePlayer.GetJob() is Job.MNK or Job.DRG or Job.NIN or Job.SAM or Job.RPR or Job.VPR or Job.BRD
            or Job.MCH
            or Job.DNC)
        {
            if (!ActionManager->IsRecastTimerActive(ActionType.Action, 7541u))
            {
                ActionManager->UseAction(ActionType.Action, 7541u);
                if (ActionManager->IsRecastTimerActive(ActionType.Action, 7541u) ||
                    Svc.Condition[ConditionFlag.DutyRecorderPlayback]
                   ) _lastHealTime = Environment.TickCount;

                if (Svc.Condition[ConditionFlag.DutyRecorderPlayback]) DuoLog.Information("Used Second Wind");
            }
        }
    }

    public override void OnReset()
    {
        _lastHealTime = 0;
    }

    public class Config : IEzConfig
    {
        public int thresholdHpPercent = 70;
    }

    public override void OnSettingsDraw()
    {
        ImGui.SliderInt("Heal Threshold HP Percent", ref Controller.GetConfig<Config>().thresholdHpPercent, 1, 100);
    }

    private static bool IsCombat()
    {
        return Svc.Condition[ConditionFlag.DutyRecorderPlayback]
            ? Svc.Objects.OfType<IBattleNpc>().Any(x => x.IsTargetable && x.CurrentHp < x.MaxHp)
            : Svc.Condition[ConditionFlag.InCombat];
    }
}