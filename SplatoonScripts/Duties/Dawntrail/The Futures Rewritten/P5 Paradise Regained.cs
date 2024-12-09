using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ECommons;
using ECommons.Configuration;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P5_Paradise_Regained : SplatoonScript
{
    public enum AttackType
    {
        Light,
        Dark
    }

    public enum Direction
    {
        North = 0,
        NorthEast = 45,
        East = 90,
        SouthEast = 135,
        South = 180,
        SouthWest = 225,
        West = 270,
        NorthWest = 315
    }

    public enum MoveType
    {
        FirstBait,
        SecondBait,
        Tower
    }

    public enum State
    {
        None,
        Start,
        FirstTower,
        SecondTower,
        ThirdTower,
        End
    }

    public enum TowerDirection
    {
        Clockwise,
        CounterClockwise,
        NorthEast,
        NorthWest
    }

    public enum TowerType
    {
        First,
        Second,
        Third
    }

    private readonly List<TowerData> _towers = [];

    private AttackType? _currentAttack = null;

    private Vector3 _firstBaitPosition;
    private Vector3 _secondBaitPosition;

    private State _state = State.None;

    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(1, "Garume");

    public Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        Controller.TryRegisterElement("Tower", new Element(0)
        {
            radius = 4f,
            thicc = 6f,
            overlayFScale = 3f,
            overlayVOffset = 3f,
            overlayText = "<< Go Here >>"
        });
        Controller.TryRegisterElement("PredictTower", new Element(0)
        {
            radius = 4f,
            thicc = 6f,
            overlayFScale = 3f,
            overlayVOffset = 3f,
            overlayText = "Your Tower",
            color = EColor.RedBright.ToUint()
        });

        Controller.TryRegisterElement("Bait", new Element(0)
        {
            radius = 0.5f,
            thicc = 6f,
            overlayFScale = 3f,
            overlayVOffset = 3f,
            overlayText = "<< Go Here >>"
        });

        Controller.TryRegisterElement("PredictBait", new Element(0)
        {
            radius = 0.5f,
            thicc = 6f,
            overlayFScale = 3f,
            overlayVOffset = 3f,
            overlayText = "<< Go Here >>"
        });
    }

    public Direction ReverseDirection(Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.South,
            Direction.NorthEast => Direction.SouthWest,
            Direction.East => Direction.West,
            Direction.SouthEast => Direction.NorthWest,
            Direction.South => Direction.North,
            Direction.SouthWest => Direction.NorthEast,
            Direction.West => Direction.East,
            Direction.NorthWest => Direction.SouthEast,
            _ => Direction.North
        };
    }

    public override void OnUpdate()
    {
        if (_state is State.None or State.Start or State.End)
        {
            Controller.GetRegisteredElements().Each(e => e.Value.Enabled = false);
            return;
        }

        Controller.GetRegisteredElements().Where(x => x.Key is "Bait" or "Tower").Each(e =>
            e.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());

        if (_state == State.FirstTower)
        {
            var firstTower = _towers[0];
            if (C.MoveType == MoveType.FirstBait)
            {
                var tankDirection = ReverseDirection(firstTower.Direction);
                var position = new Vector3(100, 0, 96f);
                _firstBaitPosition =
                    MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), ((int)tankDirection).DegreesToRadians(), position);
                if (Controller.TryGetElementByName("Bait", out var bait))
                {
                    bait.Enabled = true;
                    bait.SetRefPosition(_firstBaitPosition);
                }

                position = new Vector3(100, 0, 100 - (_currentAttack == AttackType.Light ? 2f : 14f));
                _secondBaitPosition =
                    MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), ((int)tankDirection).DegreesToRadians(), position);
                if (Controller.TryGetElementByName("PredictBait", out var predictBait))
                {
                    predictBait.Enabled = true;
                    predictBait.SetRefPosition(_secondBaitPosition);
                }
            }

            if (C.MoveType == MoveType.SecondBait)
            {
                var tankDirection = (int)firstTower.Direction + (_currentAttack == AttackType.Light ? -45 : 45);
                if (tankDirection >= 360) tankDirection -= 360;
                if (tankDirection < 0) tankDirection += 360;

                var radius = new Vector3(100, 0, 100 - (_currentAttack == AttackType.Light ? 14f : 2f));
                _firstBaitPosition = MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), tankDirection.DegreesToRadians(), radius);
                if (Controller.TryGetElementByName("Bait", out var bait))
                {
                    bait.Enabled = true;
                    bait.SetRefPosition(_firstBaitPosition);
                }

                var position = new Vector3(100, 0, 96f);
                _secondBaitPosition = MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), tankDirection.DegreesToRadians(), position);
                if (Controller.TryGetElementByName("PredictBait", out var predictBait))
                {
                    predictBait.Enabled = true;
                    predictBait.SetRefPosition(_secondBaitPosition);
                }
            }

            if (C.MoveType == MoveType.Tower)
            {
                if (C.TowerType == TowerType.First)
                    if (Controller.TryGetElementByName("Tower", out var e))
                    {
                        e.Enabled = true;
                        e.SetRefPosition(firstTower.Position.ToVector3(0));
                    }

                if (C.TowerType == TowerType.Second)
                    if (_towers.Count == 2)
                    {
                        var secondTower = _towers[1];
                        if (Controller.TryGetElementByName("PredictTower", out var e))
                        {
                            e.Enabled = true;
                            e.SetRefPosition(secondTower.Position.ToVector3(0));
                        }
                    }
            }
        }

        if (_state == State.SecondTower)
        {
            if (C.MoveType == MoveType.FirstBait)
            {
                if (Controller.TryGetElementByName("Bait", out var bait))
                {
                    bait.Enabled = true;
                    bait.SetRefPosition(_secondBaitPosition);
                }

                if (Controller.TryGetElementByName("PredictBait", out var predictBait)) predictBait.Enabled = false;
            }

            if (C.MoveType == MoveType.SecondBait)
            {
                if (Controller.TryGetElementByName("Bait", out var bait))
                {
                    bait.Enabled = true;
                    bait.SetRefPosition(_secondBaitPosition);
                }

                if (Controller.TryGetElementByName("PredictBait", out var predictBait)) predictBait.Enabled = false;
            }

            if (C.MoveType == MoveType.Tower)
            {
                if (C.TowerType == TowerType.First)
                    if (Controller.TryGetElementByName("Tower", out var e))
                        e.Enabled = false;

                if (C.TowerType == TowerType.Second)
                {
                    var secondTower = _towers[1];
                    if (Controller.TryGetElementByName("Tower", out var secondElement))
                    {
                        secondElement.Enabled = true;
                        secondElement.SetRefPosition(secondTower.Position.ToVector3(0));
                    }
                }

                if (C.TowerType == TowerType.Third)
                    if (_towers.Count == 3)
                    {
                        var thirdTower = _towers[2];
                        if (Controller.TryGetElementByName("PredictTower", out var e))
                        {
                            e.Enabled = true;
                            e.SetRefPosition(thirdTower.Position.ToVector3(0));
                        }
                    }
            }

            return;
        }

        if (_state == State.ThirdTower)
        {
            if (C.MoveType == MoveType.FirstBait)
            {
                if (Controller.TryGetElementByName("Bait", out var bait))
                {
                    bait.Enabled = true;
                    bait.SetRefPosition(_secondBaitPosition);
                }

            }
            
            if (C.MoveType == MoveType.SecondBait)
            {
                if (Controller.TryGetElementByName("Bait", out var bait))
                {
                    bait.Enabled = true;
                    bait.SetRefPosition(_secondBaitPosition);
                }
            }

            if (C.MoveType == MoveType.Tower)
            {
                if (C.TowerType == TowerType.Second)
                    if (Controller.TryGetElementByName("Tower", out var e))
                        e.Enabled = false;

                if (C.TowerType == TowerType.Third)
                {
                    var thirdTower = _towers[2];
                    if (Controller.TryGetElementByName("Tower", out var thirdElement))
                    {
                        thirdElement.Enabled = true;
                        thirdElement.SetRefPosition(thirdTower.Position.ToVector3(0));
                    }
                }
            }
        }
    }


    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 40319)
        {
            Reset();
            _state = State.Start;
        }
        if (_state != State.None && _currentAttack == null && castId == 40233) _currentAttack = AttackType.Dark;
        if (_state != State.None && _currentAttack == null && castId == 40313) _currentAttack = AttackType.Light;
    }

    public override void OnReset()
    {
        Reset();
    }

    public void Reset()
    {
        _state = State.None;
        _currentAttack = null;
        _towers.Clear();
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if (_state is State.None or State.End) return;
        if (data1 == 1 && data2 == 2)
        {
            if (position == 51)
                _towers.Add(
                    new TowerData { Position = new Vector2(93.93782f, 96.5f), Direction = Direction.NorthWest });
            if (position == 52)
                _towers.Add(
                    new TowerData { Position = new Vector2(106.0622f, 96.5f), Direction = Direction.NorthEast });
            if (position == 53)
                _towers.Add(
                    new TowerData { Position = new Vector2(100f, 107f), Direction = Direction.South });

            if (_towers.Count == 1)
                _state = State.FirstTower;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action is null) return;

        if (set.Action.Value.RowId == 40320)
        {
            if (_state == State.FirstTower) _state = State.SecondTower;
            else if (_state == State.SecondTower) _state = State.ThirdTower;
            else if (_state == State.ThirdTower) _state = State.End;
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Show Predict", ref C.ShowPredict);
        ImGui.ColorEdit4("Predict Color", ref C.PredictColor);
        ImGuiEx.EnumCombo("Move Type", ref C.MoveType);
        ImGuiEx.EnumCombo("Tower Type", ref C.TowerType);
        ImGuiEx.EnumCombo("Tower Direction", ref C.TowerDirection);

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"Attack: {_currentAttack}");
            ImGui.Text($"First Bait: {_firstBaitPosition}");
            ImGui.Text($"Second Bait: {_secondBaitPosition}");
            ImGui.Text($"Towers: {_towers.Count}");
            for (var i = 0; i < _towers.Count; i++)
            {
                var tower = _towers[i];
                ImGui.Text($"Tower {i + 1}: {tower.Position} {tower.Direction}");
            }
        }
    }

    public record TowerData
    {
        public Direction Direction;
        public Vector2 Position;
    }

    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public MoveType MoveType = MoveType.Tower;

        public Vector4 PredictColor = EColor.RedBright;
        public bool ShowPredict = true;
        public TowerDirection TowerDirection = TowerDirection.Clockwise;
        public TowerType TowerType = TowerType.First;
    }
}