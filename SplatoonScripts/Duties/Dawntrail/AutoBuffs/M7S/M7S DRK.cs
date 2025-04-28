using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.AutoBuffs.M7S;
internal unsafe class M7S_DRK :SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1261];
    public override Metadata? Metadata => new(1, "redmoon");

    private enum State
    {
        Start,
        SpoaSack,
        Stone,
        Ivy,
        Building,
        Twin,
        Wall,
        Seed,
        Finish,
    }

    Queue<uint> _actionQueue = new();
    private (uint, int) _preserveActionData = (0, 0);
    private ActionManager* actionManager = ActionManager.Instance();

    private int _countDown = 0;
    private int farNearSmash = 0;
    private int _bruteSmash = 0;
    private bool _spoaSack = false;
    private int _thornsSmash = 0;
    private int _lightning = 0;
    private int _bruteSpark = 0;
    private int _thornsWorld = 0;
    private int _powerDive = 0;
    private int _SmashImpact = 0;
    private int _bruteDive = 0;
    private int _thornsFace = 0;




    // キューから取り出してアクションを実行する
    public override void OnUpdate()
    {
        // 予約されたアクションを実行する
        if (_preserveActionData.Item1 != 0)
        {
            var recastDetail = SearchAction(_preserveActionData.Item1);
            if (recastDetail != null)
            {
                ++_preserveActionData.Item2;
            }

            if (DoAction(_preserveActionData.Item1))
            {
                DuoLog.Information($"Used {_preserveActionData.Item1}");
                _preserveActionData.Item1 = 0;
                _preserveActionData.Item2 = 0;
            }
            else
            {
                if (_preserveActionData.Item2 >= 20)
                {
                    _preserveActionData.Item1 = 0;
                    _preserveActionData.Item2 = 0;
                }
            }

            return;
        }

        while (_actionQueue.Count > 0)
        {
            uint actionId = _actionQueue.Dequeue();
            if (actionId == 0) continue;
            var recastDetail = SearchAction(actionId);
            if (recastDetail != null)
            {
                // もし5秒以上ならば諦める
                if (recastDetail.Value.Elapsed > 5)
                {
                    DuoLog.Error($"Drop {actionId}");
                    return;
                }
            }

            if (!DoAction(actionId))
            {
                // それ以下ならば予約する
                _preserveActionData.Item1 = actionId;
                _preserveActionData.Item2 = 0;
                return;
            }
        }
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
