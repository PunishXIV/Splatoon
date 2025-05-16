using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M6S_Cloud_Navigation : SplatoonScript
{
    private const uint CloudDataId = 18339;
    private readonly string _basePlayerOverride = string.Empty;
    private readonly List<IntPtr> _aoeList = [];
    private CloudDirection _currentCloudDirection;
    private Vector3 _lastPosition;
    private State _state = State.None;
    private Config C => Controller.GetConfig<Config>();

    public override HashSet<uint>? ValidTerritories => [1259];
    public override Metadata? Metadata => new(3, "Garume");

    private static IBattleNpc? Cloud =>
        Svc.Objects.FirstOrDefault(x => x.DataId == CloudDataId) as IBattleNpc;

    private IPlayerCharacter BasePlayer =>
        string.IsNullOrEmpty(_basePlayerOverride)
            ? Player.Object
            : Svc.Objects.OfType<IPlayerCharacter>()
                .FirstOrDefault(x => x.Name.ToString().EqualsIgnoreCase(_basePlayerOverride)) ?? Player.Object;

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"Configure your position:");
        ImGuiEx.RadioButtonBool("Right side when looking at the cloud", "Left side when looking at the cloud", ref C.IsRight);
        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"Cloud Direction: {_currentCloudDirection}");
            ImGui.Text($"Cloud Position: {Cloud?.Position.ToString() ?? "null"}");
            ImGui.Text($"State: {_state}");
            foreach(var aoe in _aoeList)
            {
                var player = Svc.Objects.OfType<IPlayerCharacter>().FirstOrDefault(x => x.Address == aoe);
                if(player != null)
                    ImGui.Text($"AoE Target: {player.Name}");
            }
        }
    }

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            radius = 3f,
            thicc = 15f,
            tether = true
        };
        Controller.RegisterElement("Bait", element);
    }

    private CloudDirection GetDirectionFromPosition(Vector3 position)
    {
        if(position.Z > 100)
            return CloudDirection.South;
        return position.X < 100 ? CloudDirection.NorthWest : CloudDirection.NorthEast;
    }

    private CloudDirection ChangeDirectionFromVector(CloudDirection current, Vector3 vector)
    {
        switch(current)
        {
            case CloudDirection.South when vector.Z < 0:
                return vector.X < 0 ? CloudDirection.NorthWest : CloudDirection.NorthEast;
            case CloudDirection.South:
                return CloudDirection.South;
            case CloudDirection.NorthEast when vector.Z < 0.5f:
                return vector.X > 0 ? CloudDirection.NorthEast : CloudDirection.NorthWest;
            case CloudDirection.NorthEast:
                return CloudDirection.South;
            case CloudDirection.NorthWest when vector.Z < 0.5f:
                return vector.X < 0 ? CloudDirection.NorthWest : CloudDirection.NorthEast;
            case CloudDirection.NorthWest:
                return CloudDirection.South;
            default:
                return current;
        }

    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(_state is State.None or State.End)
            return;

        if(target.GetObject() is IPlayerCharacter player &&
            vfxPath == "vfx/lockon/eff/m0922trg_t2w.avfx")
        {
            if(_aoeList.Count >= 2)
                _aoeList.Clear();

            _aoeList.Add(player.Address);
            UpdateBaitPosition();
        }
    }

    private Vector3 GetSafePosition()
    {
        var isPlayerInAoe = _aoeList.Contains(BasePlayer.Address);
        switch(_currentCloudDirection)
        {
            case CloudDirection.NorthWest:
                return isPlayerInAoe
                    ? C.IsRight ? new Vector3(115, 0, 85) : new Vector3(100, 0, 115)
                    : new Vector3(107, 0, 106);
            case CloudDirection.NorthEast:
                return isPlayerInAoe
                    ? C.IsRight ? new Vector3(100, 0, 115) : new Vector3(85, 0, 85)
                    : new Vector3(91, 0, 103);
            case CloudDirection.South:
                return isPlayerInAoe
                    ? C.IsRight ? new Vector3(85, 0, 85) : new Vector3(115, 0, 85)
                    : new Vector3(101, 0, 91);
            default:
                return Vector3.Zero;
        }
    }

    public override void OnUpdate()
    {
        var cloud = Cloud;

        switch(_state)
        {
            case State.None:
                if(cloud != null)
                {
                    _lastPosition = cloud.Position;
                    _currentCloudDirection = GetDirectionFromPosition(cloud.Position);
                    _state = State.Stopping;
                    UpdateBaitPosition();
                }

                break;

            case State.Stopping:
                if(cloud == null)
                {
                    _state = State.End;
                    return;
                }

                if(_lastPosition != cloud.Position)
                {
                    _state = State.Moving;
                    var movementVector = cloud.Position - _lastPosition;
                    var normalized = Vector3.Normalize(movementVector);
                    _currentCloudDirection = ChangeDirectionFromVector(_currentCloudDirection, normalized);
                    UpdateBaitPosition();
                }

                break;

            case State.Moving:
                if(cloud != null && _lastPosition == cloud.Position)
                {
                    _state = State.Stopping;
                    _currentCloudDirection = GetDirectionFromPosition(cloud.Position);
                }

                break;
        }

        if(_state is State.Moving or State.Stopping)
        {
            Controller.GetRegisteredElements().Each(x =>
            {
                x.Value.Enabled = true;
                x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
            });
            if(cloud != null)
                _lastPosition = cloud.Position;
        }
        else
        {
            DisableRegisteredElements();
        }
    }

    private void UpdateBaitPosition()
    {
        if(Controller.TryGetElementByName("Bait", out var baitElement))
            baitElement.SetOffPosition(GetSafePosition());
    }

    private void DisableRegisteredElements()
    {
        foreach(var element in Controller.GetRegisteredElements())
            element.Value.Enabled = false;
    }

    public override void OnReset()
    {
        _state = State.None;
        _aoeList.Clear();
        _lastPosition = Vector3.Zero;
    }

    private enum CloudDirection
    {
        NorthWest,
        NorthEast,
        South
    }

    private enum State
    {
        None,
        Stopping,
        Moving,
        End
    }

    private class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public bool IsRight = true;
    }
}