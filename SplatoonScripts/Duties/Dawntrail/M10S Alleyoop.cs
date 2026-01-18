using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ECommons.GameFunctions;
using ECommons.MathHelpers;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

internal class M10S_Alleyoop : SplatoonScript
{
    /*
     * Constants and Types
     */

    #region Constants and Types

    private class PlayerData
    {
        public uint EntityId;
        public float Angle;

        public PlayerData(uint entityId)
        {
            EntityId = entityId;
            Angle = 0;
        }
    }

    #endregion

    /*
     * Public Fields
     */

    #region Public Fields

    public override HashSet<uint>? ValidTerritories => [1323];
    public override Metadata? Metadata => new(1, "Redmoon");

    #endregion

    /*
     * Private Fields
     */

    #region Private Fields

    private List<PlayerData> _playerList = [];
    private uint _enemyId = 0;
    private bool _gimmickStarted = false;
    private bool _isFirst = false;
    private bool _isDouble = false;
    private int _gimmickCount = 0;
    private List<float> _angleList = [];

    private Config C => Controller.GetConfig<Config>();

    private IPlayerCharacter BasePlayer
    {
        get
        {
            return Splatoon.Splatoon.BasePlayer;
        }
    }

    #endregion

    /*
     * Public Methods
     */

    #region Public Methods

    public override void OnSetup()
    {
        for (var i = 0; i < 16; i++)
        {
            Controller.RegisterElement($"Cone{i}",
                new(5)
                {
                    radius = 50.0f, coneAngleMin = -15, coneAngleMax = 15, refActorComparisonType = 2,
                    includeRotation = true
                });
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId is 46557 or 46560)
        {
            if (castId == 46557) _isDouble = true;
            _enemyId = Svc.Objects.FirstOrDefault(x => x.EntityId == source)?.EntityId ?? 0;
            foreach (var player in FakeParty.Get())
            {
                if (_gimmickCount == 1)
                {
                    if (player.StatusList.Any(x => x.StatusId == 4974)) continue;
                }

                _playerList.Add(new PlayerData(player.EntityId));
            }

            _gimmickStarted = true;
            _isFirst = true;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!_gimmickStarted) return;
        if (set.Action.Value.RowId is 46560 or 46558)
        {
            Hide();
            _isFirst = false;
            foreach (var player in _playerList)
            {
                var obj = player.EntityId.GetObject();
                if (obj != null)
                {
                    player.Angle = MathHelper.GetRelativeAngle(obj.Position, _enemyId.GetObject().Position);
                }
            }

            Show();
        }

        if (set.Action.Value.RowId is 46559 or 46562)
        {
            _gimmickCount++;
            WormReset();
        }
    }

    public override void OnUpdate()
    {
        if (!_isFirst || !_gimmickStarted) return;
        try
        {
            DynamicShow();
        }
        catch (System.Exception e)
        {
            OnReset();
        }
    }

    public class Config : IEzConfig
    {
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"Gimmick Started: {_gimmickStarted}");
            ImGui.Text($"Is First: {_isFirst}");
            ImGui.Text($"Enemy: {_enemyId.GetObject().EntityId}");
            ImGui.Text($"IsDouble: {_isDouble}");
            ImGui.Text($"Gimmick Count: {_gimmickCount}");
            ImGui.Text("Angle List:");
            foreach (var angle in _angleList)
            {
                ImGui.Text($"{angle}");
            }

            var i = 0;
            if (_gimmickStarted || _playerList.Count == 8)
            {
                foreach (var player in _playerList)
                {
                    var obj = player.EntityId.GetObject();
                    if (obj == null) continue;
                    ImGui.Text($"Player {obj.Name.ToString()}: RelativeRotation: {player.Angle}");
                }
            }
        }
    }

    public override void OnReset()
    {
        _gimmickCount = 0;
        WormReset();
    }

    private void WormReset()
    {
        _gimmickStarted = false;
        _isFirst = false;
        _isDouble = false;
        _playerList.Clear();
        _angleList.Clear();
        Hide();
    }

    #endregion

    /*
     * Private Methods
     */

    #region Private Methods

    private void Hide() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    private void DynamicShow()
    {
        foreach (var player in _playerList)
        {
            var obj = player.EntityId.GetObject();
            if (obj != null)
            {
                player.Angle = MathHelper.GetRelativeAngle(obj.Position, _enemyId.GetObject().Position);
            }
        }

        int loopCount = _gimmickCount == 1 ? 4 : 8;
        for (var i = 0; i < loopCount; i++)
        {
            var cone = Controller.GetElementByName($"Cone{i}");
            var player = _playerList[i];
            if (cone != null)
            {
                cone.SetRefPosition(_enemyId.GetObject().Position);
                cone.AdditionalRotation = MathHelper.DegToRad(player.Angle);
                cone.coneAngleMin = -15;
                cone.coneAngleMax = 15;
                cone.Enabled = true;
            }
        }
    }

    private void Show()
    {
        if (!_isDouble)
        {
            foreach (var player in _playerList)
            {
                if ((player.Angle + 22.0f) > 360.0f)
                {
                    _angleList.Add(player.Angle + 22.0f - 360.0f);
                }
                else
                {
                    _angleList.Add(player.Angle + 22.0f);
                }

                if (player.Angle - 22.0f < 0.0f)
                {
                    _angleList.Add(player.Angle - 22.0f + 360.0f);
                }
                else
                {
                    _angleList.Add(player.Angle - 22.0f);
                }
            }

            int loopCount = _gimmickCount == 1 ? 8 : 16;
            for (var i = 0; i < loopCount; i++)
            {
                var cone = Controller.GetElementByName($"Cone{i}");
                var angle = _angleList[i];
                if (cone != null)
                {
                    cone.SetRefPosition(_enemyId.GetObject().Position);
                    cone.AdditionalRotation = MathHelper.DegToRad(angle);
                    cone.coneAngleMin = -9;
                    cone.coneAngleMax = 9;
                    cone.Enabled = true;
                }
            }
        }
        else
        {
            int loopCount = _gimmickCount == 1 ? 4 : 8;
            for (var i = 0; i < loopCount; i++)
            {
                var cone = Controller.GetElementByName($"Cone{i}");
                var player = _playerList[i];
                if (cone != null)
                {
                    cone.SetRefPosition(_enemyId.GetObject().Position);
                    cone.AdditionalRotation = MathHelper.DegToRad(player.Angle);
                    cone.coneAngleMin = -15;
                    cone.coneAngleMax = 15;
                    cone.Enabled = true;
                }
            }
        }
    }

    #endregion
}