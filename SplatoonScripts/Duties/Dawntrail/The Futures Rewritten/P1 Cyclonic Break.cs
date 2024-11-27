using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using ECommons.Throttlers;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;
internal class P1_Cyclonic_Break :SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1238];
    public override Metadata? Metadata => new(1, "Redmoon");

    class PlayerData
    {
        public uint EntityId;
        public Vector3 Position;
        public PlayerData(uint entityId)
        {
            EntityId = entityId;
            Position = Vector3.Zero;
        }
    }

    const uint _cyclonicBreakCastFireId = 40144;
    const uint _cyclonicBreakCastThunderId = 40148;
    const uint _cyclonicBreakFireId = 40146;
    const uint _cyclonicBreakThunderId = 40149;
    const uint _secondCyclonicBreakCastFireId = 40329;
    const uint _secondCyclonicBreakCastThunderId = 40330;
    //const uint _secondCyclonicBreakFireId = 40146;
    //const uint _secondCyclonicBreakThunderId = 40149;
    List<PlayerData> _playerList = new List<PlayerData>();
    uint _enemyId = 0;
    int _actionCount = 0;
    bool _gimmickStarted = false;
    bool _checked = false;
    bool _isFirst = false;

    public override void OnSetup()
    {
        for (int i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"Cone{i}", new(5) { radius = 20.0f, coneAngleMin = -14, coneAngleMax = 14, refActorComparisonType = 2, includeRotation = true });
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == _cyclonicBreakCastFireId || castId == _cyclonicBreakCastThunderId)
        {
            _enemyId = Svc.Objects.FirstOrDefault(x => x.IsTargetable && x.ObjectKind == ObjectKind.BattleNpc && x.DataId == 0x459B).EntityId;
            foreach (var player in FakeParty.Get())
            {
                _playerList.Add(new PlayerData(player.EntityId));
            }
            if (_playerList.Count == 8 && _enemyId != 0u)
            {
                _gimmickStarted = true;
                _isFirst = true;
            }
        }
        if (castId == _secondCyclonicBreakCastFireId || castId == _secondCyclonicBreakCastThunderId)
        {
            _enemyId = Svc.Objects.FirstOrDefault(x => x is IBattleNpc obj && obj.IsCharacterVisible() && obj.ObjectKind == ObjectKind.BattleNpc && obj.DataId == 0x459C).EntityId;
            foreach (var player in FakeParty.Get())
            {
                _playerList.Add(new PlayerData(player.EntityId));
            }
            if (_playerList.Count == 8 && _enemyId != 0u)
            {
                _ = new TickScheduler(delegate { _gimmickStarted = true; _isFirst = true; }, 2000);
            }
        }
        if (castId == 40154 || castId == 40137)
        {
            OnReset();
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!_gimmickStarted) return;
        if (set.Action == null) return;
        if (set.Action.Value.RowId == _cyclonicBreakCastFireId || set.Action.Value.RowId == _cyclonicBreakCastThunderId ||
            set.Action.Value.RowId == _secondCyclonicBreakCastFireId || set.Action.Value.RowId == _secondCyclonicBreakCastThunderId)
        {
            Hide();
            _isFirst = false;
            foreach (var player in _playerList)
            {
                var obj = player.EntityId.GetObject();
                if (obj != null)
                {
                    player.Position = obj.Position;
                }
            }
            Show();
            _actionCount++;
        }
        else if (set.Action.Value.RowId == _cyclonicBreakFireId)
        {
            if (_checked) return;
            Hide();
            if (_actionCount == 3)
            {
                OnReset();
                return;
            }
            foreach (var player in _playerList)
            {
                var obj = player.EntityId.GetObject();
                if (obj != null)
                {
                    player.Position = obj.Position;
                }
            }
            Show();
            _actionCount++;
            _checked = true;
            _ = new TickScheduler(delegate { _checked = false; }, 400);
        }
        else if (set.Action.Value.RowId == 40165)
        {
            this.OnReset();
            return;
        }
    }

    public override void OnUpdate()
    {
        if (!_isFirst || !_gimmickStarted) return;
        DynamicShow();
    }

    public override void OnReset()
    {
        _gimmickStarted = false;
        _isFirst = false;
        _playerList.Clear();
        _actionCount = 0;
        EzThrottler.Reset("CyclonicBreakFire");
        Hide();
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"Gimmick Started: {_gimmickStarted}");
            ImGui.Text($"Is First: {_isFirst}");
            ImGui.Text($"Enemy: {_enemyId.GetObject().EntityId}");
            ImGui.Text($"Action Count: {_actionCount}");
            int i = 0;
            if (_gimmickStarted || _playerList.Count == 8)
            {
                foreach (var player in _playerList)
                {
                    var obj = player.EntityId.GetObject();
                    if (obj == null) continue;
                    ImGui.Text($"Player {obj.Name.ToString()}: RelativeRotation: {MathHelper.GetRelativeAngle(player.Position, _enemyId.GetObject().Position)}: {player.Position}");
                }
            }
        }
    }

    private void Hide()
    {
        PluginLog.Information("Hiding cones");
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    private void DynamicShow()
    {
        foreach (var player in _playerList)
        {
            var obj = player.EntityId.GetObject();
            if (obj != null)
            {
                player.Position = obj.Position;
            }
        }

        for (int i = 0; i < 8; i++)
        {
            var cone = Controller.GetElementByName($"Cone{i}");
            var player = _playerList[i];
            var angle = MathHelper.GetRelativeAngle(player.Position, _enemyId.GetObject().Position);
            if (cone != null)
            {
                cone.SetRefPosition(_enemyId.GetObject().Position);
                cone.AdditionalRotation = MathHelper.DegToRad(angle);
                cone.Enabled = true;
            }
        }
    }

    private void Show()
    {
        PluginLog.Information("Showing cones");
        for (int i = 0; i < 8; i++)
        {
            var cone = Controller.GetElementByName($"Cone{i}");
            var player = _playerList[i];
            var angle = MathHelper.GetRelativeAngle(player.Position, _enemyId.GetObject().Position);
            if (cone != null)
            {
                cone.SetRefPosition(_enemyId.GetObject().Position);
                cone.AdditionalRotation = MathHelper.DegToRad(angle);
                cone.Enabled = true;
            }
        }
    }
}
