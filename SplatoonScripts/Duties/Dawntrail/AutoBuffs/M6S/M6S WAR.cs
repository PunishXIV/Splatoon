using Dalamud.Game.ClientState.Conditions;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.AutoBuffs.M6S;
internal unsafe class M6S_WAR :SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1257, 1259, 1261];
    public override Metadata? Metadata => new(1, "Redmoon");

    private long _onCombatStartTime = 0;
    private long _planedCombatStartTime = 0;
    private bool _normalOperation = false;

    private ActionManager* actionManager = ActionManager.Instance();
    private Chat _chat = Chat.Instance;

    public override void OnMessage(string Message)
    {
        if (Message.Contains("戦闘開始！"))
        {
            _onCombatStartTime = Environment.TickCount64;
        }

        if (Message.Contains("戦闘開始まで"))
        {
            // 文字列から数字を抽出
            Match match = Regex.Match(Message, @"戦闘開始まで(\d+)秒");

            if (match.Success)
            {
                int seconds = int.Parse(match.Groups[1].Value);
                _planedCombatStartTime = Environment.TickCount64 + (seconds * 1000);
            }
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (!_normalOperation) return;
        if (Svc.ClientState.TerritoryType == 1261) M7sOnStartingCast(source, castId);
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!set.Action.HasValue) return;
        if (!_normalOperation) return;
        if (Svc.ClientState.TerritoryType == 1261) M7sOnActionEffectEvent(set);
    }

    public override void OnUpdate()
    {
        // 戦闘開始時
        if (_onCombatStartTime > 0)
        {
            // 20 seconds after combat start, remove the buff
            if (!(Environment.TickCount64 - _onCombatStartTime > 25000)) return;
            _onCombatStartTime = 0;
            _planedCombatStartTime = 0;
            _chat.SendMessage("/rotation Manual");
            _normalOperation = true;
        }

        // カウントダウン中
        if (_planedCombatStartTime > 0)
        {
            // M5S, M6SはMT
            if (Svc.ClientState.TerritoryType == 1257 ||
                Svc.ClientState.TerritoryType == 1259)
            {
                if (Player.Object.StatusList.All(x => x.StatusId != 0x2E7))
                {
                    DoAction(3629);
                }
            }
            // M7S, M8SはST
            if (Svc.ClientState.TerritoryType == 1261 ||
                Svc.ClientState.TerritoryType == 1263)
            {
                if (Player.Object.StatusList.Any(x => x.StatusId == 0x2E8))
                {
                    DoAction(32067);
                }
            }
        }

        if (!_normalOperation) return;

        if (Svc.ClientState.TerritoryType == 1261) M7sDo();
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"{Svc.ClientState.TerritoryType}");
        ImGuiEx.Text($"OnCombatStartTime: {_onCombatStartTime}");
        ImGuiEx.Text($"PlanedCombatStartTime: {_planedCombatStartTime}");
        ImGui.Checkbox("_normalOperation", ref _normalOperation);
    }

    public override void OnReset()
    {
        _onCombatStartTime = 0;
        _planedCombatStartTime = 0;
        _normalOperation = false;
        _chat.SendMessage("/rotation Off");
    }

    private void M7sDo()
    {
        if (Player.Object.StatusList.All(x => x.StatusId != 0x2E7))
        {
            DoAction(3629);
        }
    }

    private void M7sOnStartingCast(uint source, uint castId)
    {
        if (castId == 42416) _chat.SendMessage("/rotation Off");
    }

    private void M7sOnActionEffectEvent(ActionEffectSet set)
    {
        if (!set.Action.HasValue) return;
        if (set.Action.Value.RowId == 42357) _chat.SendMessage("/rotation Manual");
    }

    private bool DoAction(uint actionId)
    {
        if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
        {
            DuoLog.Information($"Used {actionId}");
            return true;
        }
        else
        {
            if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, actionId))
            {
                actionManager->UseAction(ActionType.Action, actionId);
            }

            if (actionManager->IsRecastTimerActive(ActionType.Action, actionId))
            {
                return true;
            }
        }
        return false;
    }

    private RecastDetail? SearchAction(uint actionId)
    {
        for (int i = 0; i < 80; i++)
        {
            if (actionManager->Cooldowns[i].ActionId == actionId)
            {
                return actionManager->Cooldowns[i];
            }
        }
        return null;
    }
}
