using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M6S_Cloud_Navigation : SplatoonScript
{
    private enum CloudDirection
    {
        NorthWest,
        NorthEast,
        South
    }
    
    private const uint CloudDataId = 18339;
    private readonly string _basePlayerOverride = "";
    private List<IntPtr> _aoeList = new();
    private CloudDirection _currentCloudDirection;
    private Vector3 _lastPosition;
    private State _state = State.None;
    private Config C => Controller.GetConfig<Config>();
    public override HashSet<uint>? ValidTerritories => [1259];

    private static IBattleNpc? Cloud => Svc.Objects
        .FirstOrDefault(x => x.DataId == CloudDataId) as IBattleNpc;

    private IPlayerCharacter BasePlayer
    {
        get
        {
            if (_basePlayerOverride == "")
                return Player.Object;
            return Svc.Objects.OfType<IPlayerCharacter>()
                .FirstOrDefault(x => x.Name.ToString().EqualsIgnoreCase(_basePlayerOverride)) ?? Player.Object;
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("You are Right Side", ref C.IsRight);
        ImGuiEx.HelpMarker("When the clouds are in the back and the bridge is in the front, should you go right or left?");

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"Cloud Direction {_currentCloudDirection.ToString()}");
            ImGui.Text($"Cloud Position {Cloud?.Position.ToString() ?? "null"}");
            ImGui.Text($"State {_state.ToString()}");
            foreach (var aoe in _aoeList)
            {
                var name = Svc.Objects.OfType<IPlayerCharacter>().First(x => x.Address == aoe).Name;
                ImGui.Text($"Aoe Target {name}");
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
        if (position.Z > 100) return CloudDirection.South;
        if (position.X < 100) return CloudDirection.NorthWest;
        return CloudDirection.NorthEast;
    }

    private CloudDirection? ChangeDirectionFromVector(CloudDirection current, Vector3 vector)
    {
        switch (current)
        {
            case CloudDirection.South when vector.Z < 0:
                return vector.X < 0 ? CloudDirection.NorthWest : CloudDirection.NorthEast;
            case CloudDirection.South:
                return CloudDirection.South;
            case CloudDirection.NorthEast when vector.Z == 0:
                return vector.X > 0 ? CloudDirection.NorthEast : CloudDirection.NorthWest;
            case CloudDirection.NorthEast:
                return CloudDirection.South;
            case CloudDirection.NorthWest when vector.Z == 0:
                return vector.X < 0 ? CloudDirection.NorthWest : CloudDirection.NorthEast;
            case CloudDirection.NorthWest:
                return CloudDirection.South;
            default:
                return null;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (_state is State.None or State.End) return;
        if (target.GetObject() is IPlayerCharacter player && vfxPath == "vfx/lockon/eff/m0922trg_t2w.avfx")
        {
            if (_aoeList.Count >= 2) _aoeList.Clear();
            _aoeList.Add(player.Address);
            if (Controller.TryGetElementByName("Bait", out var baitElement))
                baitElement.SetOffPosition(GetSafePosition());
        }
    }

    private Vector3 GetSafePosition()
    {
        switch (_currentCloudDirection)
        {
            case CloudDirection.NorthWest:
            {
                if (_aoeList.Contains(BasePlayer.Address))
                    return C.IsRight ? new Vector3(115, 0, 85) : new Vector3(100, 0, 115);

                return new Vector3(107, 0, 106);
            }
            case CloudDirection.NorthEast:
            {
                if (_aoeList.Contains(BasePlayer.Address))
                    return C.IsRight ? new Vector3(100, 0, 115) : new Vector3(85, 0, 85);

                return new Vector3(91, 0, 103);
            }
            case CloudDirection.South:
            {
                if (_aoeList.Contains(BasePlayer.Address))
                    return C.IsRight ? new Vector3(85, 0, 85) : new Vector3(115, 0, 85);

                return new Vector3(101, 0, 91);
            }
            default:
                return Vector3.Zero;
        }
    }

    public override void OnUpdate()
    {
        if (_state == State.None)
        {
            var cloud = Cloud;
            if (cloud != null)
            {
                _lastPosition = cloud.Position;
                _currentCloudDirection = GetDirectionFromPosition(cloud.Position);
                _state = State.Stopping;
                PluginLog.Warning(_currentCloudDirection.ToString());
                if (Controller.TryGetElementByName("Bait", out var baitElement))
                    baitElement.SetOffPosition(GetSafePosition());
            }
        }

        if (_state == State.Stopping)
        {
            var cloud = Cloud;
            if (cloud == null)
            {
                _state = State.End;
                return;
            }

            if (_lastPosition != cloud.Position)
            {
                _state = State.Moving;
                var vector = cloud.Position - _lastPosition;
                var normalized = Vector3.Normalize(vector);

                _currentCloudDirection = ChangeDirectionFromVector(_currentCloudDirection, normalized)
                                         ?? _currentCloudDirection;

                if (Controller.TryGetElementByName("Bait", out var baitElement))
                    baitElement.SetOffPosition(GetSafePosition());
            }
        }

        if (_state == State.Moving)
        {
            var cloud = Cloud;
            if (cloud != null && _lastPosition == cloud.Position)
            {
                _state = State.Stopping;
                _currentCloudDirection = GetDirectionFromPosition(cloud.Position);
            }
        }

        if (_state is State.Moving or State.Stopping)
        {
            Controller.GetRegisteredElements().Each(x =>
            {
                x.Value.Enabled = true;
                x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
            });

            var cloud = Cloud;
            if (cloud != null) _lastPosition = cloud.Position;
        }
        else
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _aoeList.Clear();
        _lastPosition = Vector3.Zero;
    }

    private class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public bool IsRight = true;
    }
    
    private enum State
    {
        None,
        Stopping,
        Moving,
        End
    }
}