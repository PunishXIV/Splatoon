using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Components;
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
        Third,
        Left,
        Right
    }

    private readonly List<TowerData> _towers = [];

    private AttackType? _currentAttack;

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

    public void SetElementPosition(string name, Vector3 position)
    {
        if (Controller.TryGetElementByName(name, out var element))
        {
            element.Enabled = true;
            element.SetRefPosition(position);
        }
    }

    public void SetBaitPosition(Vector3 position)
    {
        SetElementPosition("Bait", position);
    }

    public void HideBait()
    {
        if (Controller.TryGetElementByName("Bait", out var bait)) bait.Enabled = false;
    }

    public void SetPredictBaitPosition(Vector3 position)
    {
        SetElementPosition("PredictBait", position);
    }

    public void HidePredictBait()
    {
        if (Controller.TryGetElementByName("PredictBait", out var predictBait)) predictBait.Enabled = false;
    }

    public void SetTowerPosition(Vector3 position)
    {
        SetElementPosition("Tower", position);
    }

    public void HideTower()
    {
        if (Controller.TryGetElementByName("Tower", out var tower)) tower.Enabled = false;
    }

    public void SetPredictTowerPosition(Vector3 position)
    {
        SetElementPosition("PredictTower", position);
    }

    public void HidePredictTower()
    {
        if (Controller.TryGetElementByName("PredictTower", out var predictTower)) predictTower.Enabled = false;
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
                    MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), ((int)tankDirection).DegreesToRadians(),
                        position);
                SetBaitPosition(_firstBaitPosition);

                position = new Vector3(100, 0, 100 - (_currentAttack == AttackType.Light ? 2f : 14f));
                _secondBaitPosition =
                    MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), ((int)tankDirection).DegreesToRadians(),
                        position);
                SetPredictBaitPosition(_secondBaitPosition);
            }

            if (C.MoveType == MoveType.SecondBait)
            {
                var tankDirection = (int)firstTower.Direction + (_currentAttack == AttackType.Light ? -45 : 45);
                if (tankDirection >= 360) tankDirection -= 360;
                if (tankDirection < 0) tankDirection += 360;

                var radius = new Vector3(100, 0, 100 - (_currentAttack == AttackType.Light ? 14f : 2f));
                _firstBaitPosition =
                    MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), tankDirection.DegreesToRadians(), radius);
                SetBaitPosition(_firstBaitPosition);

                var position = new Vector3(100, 0, 96f);
                _secondBaitPosition = MathHelper.RotateWorldPoint(new Vector3(100, 0, 100),
                    tankDirection.DegreesToRadians(), position);
                SetPredictBaitPosition(_secondBaitPosition);
            }

            if (C.MoveType == MoveType.Tower)
            {
                if (C.TowerType == TowerType.First)
                    SetTowerPosition(firstTower.Position.ToVector3(0));

                if (C.TowerType == TowerType.Second)
                    if (_towers.Count == 2)
                        SetTowerPosition(_towers[1].Position.ToVector3(0));
                if (C.TowerType == TowerType.Third)
                    if (_towers.Count == 3)
                        SetPredictTowerPosition(_towers[2].Position.ToVector3(0));
                if (C.TowerType == TowerType.Left)
                    if (_towers.TryGetFirst(x => x.IsLeft == true, out var tower))
                    {
                        if (_towers.IndexOf(tower) == 1)
                            SetTowerPosition(tower.Position.ToVector3(0));
                        else
                            SetPredictTowerPosition(tower.Position.ToVector3(0));
                    }

                if (C.TowerType == TowerType.Right)
                    if (_towers.TryGetFirst(x => x.IsLeft == false, out var tower))
                    {
                        if (_towers.IndexOf(tower) == 1)
                            SetTowerPosition(tower.Position.ToVector3(0));
                        else
                            SetPredictTowerPosition(tower.Position.ToVector3(0));
                    }
            }
        }

        if (_state == State.SecondTower)
        {
            if (C.MoveType == MoveType.FirstBait)
            {
                SetBaitPosition(_secondBaitPosition);
                HidePredictBait();
            }

            if (C.MoveType == MoveType.SecondBait)
            {
                SetBaitPosition(_secondBaitPosition);
                HidePredictBait();
            }

            if (C.MoveType == MoveType.Tower)
            {
                if (C.TowerType == TowerType.First)
                    HideTower();
                if (C.TowerType == TowerType.Second)
                {
                    SetTowerPosition(_towers[1].Position.ToVector3(0));
                    HidePredictTower();
                }

                if (C.TowerType == TowerType.Third)
                    if (_towers.Count == 3)
                    {
                        SetTowerPosition(_towers[2].Position.ToVector3(0));
                        HidePredictTower();
                    }

                if (C.TowerType == TowerType.Left)
                    if (_towers.TryGetFirst(x => x.IsLeft == true, out var tower))
                    {
                        SetTowerPosition(tower.Position.ToVector3(0));
                        HidePredictTower();
                    }

                if (C.TowerType == TowerType.Right)
                    if (_towers.TryGetFirst(x => x.IsLeft == false, out var tower))
                    {
                        SetTowerPosition(tower.Position.ToVector3(0));
                        HidePredictTower();
                    }
            }
        }

        if (_state == State.ThirdTower)
        {
            if (C.MoveType == MoveType.FirstBait)
            {
                SetBaitPosition(_secondBaitPosition);
                HidePredictBait();
            }

            if (C.MoveType == MoveType.SecondBait)
            {
                SetBaitPosition(_secondBaitPosition);
                HidePredictBait();
            }

            if (C.MoveType == MoveType.Tower)
            {
                if (C.TowerType == TowerType.Second)
                    HideTower();

                if (C.TowerType == TowerType.Third)
                {
                    SetTowerPosition(_towers[2].Position.ToVector3(0));
                    HidePredictTower();
                }

                if (C.TowerType == TowerType.Left)
                    if (_towers.TryGetFirst(x => x.IsLeft == true, out var tower))
                    {
                        if (_towers.IndexOf(tower) == 2)
                        {
                            SetTowerPosition(_towers[2].Position.ToVector3(0));
                            HidePredictTower();
                        }
                        else
                        {
                            HideTower();
                        }
                    }

                if (C.TowerType == TowerType.Right)
                    if (_towers.TryGetFirst(x => x.IsLeft == false, out var tower))
                    {
                        if (_towers.IndexOf(tower) == 2)
                        {
                            SetTowerPosition(_towers[2].Position.ToVector3(0));
                            HidePredictTower();
                        }
                        else
                        {
                            HideTower();
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
            switch (position)
            {
                case 51:
                    _towers.Add(
                        new TowerData { Position = new Vector2(93.93782f, 96.5f), Direction = Direction.NorthWest });
                    break;
                case 52:
                    _towers.Add(
                        new TowerData { Position = new Vector2(106.0622f, 96.5f), Direction = Direction.NorthEast });
                    break;
                case 53:
                    _towers.Add(
                        new TowerData { Position = new Vector2(100f, 107f), Direction = Direction.South });
                    break;
            }

            switch (_towers.Count)
            {
                case 1:
                    _state = State.FirstTower;
                    break;
                case 2:
                {
                    var diff = _towers[0].AngleDifference(_towers[1]);
                    _towers[1].IsLeft = diff <= 180;
                    break;
                }
                case 3:
                {
                    _towers[2].IsLeft = !_towers[1].IsLeft;
                    break;
                }
            }
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
        ImGuiEx.EnumCombo("Move Type", ref C.MoveType);
        ImGuiEx.EnumCombo("Tower Type", ref C.TowerType);

        ImGui.Text("Bait Color:");
        ImGuiComponents.HelpMarker(
            "Change the color of the bait and the text that will be displayed on your bait.\nSetting different values makes it rainbow.");
        ImGui.Indent();
        ImGui.ColorEdit4("Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.SameLine();
        ImGui.ColorEdit4("Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
        ImGui.Unindent();

        ImGui.Checkbox("Show Predict", ref C.ShowPredict);
        ImGui.ColorEdit4("Predict Color", ref C.PredictColor, ImGuiColorEditFlags.NoInputs);

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
                ImGui.Text($"Tower {i + 1}: {tower.Position} {tower.Direction} {tower.IsLeft}");
            }

            ImGui.Text($"Angle Difference: {_towers[0].AngleDifference(_towers[1])}");
        }
    }

    public record TowerData
    {
        public Direction Direction;
        public bool? IsLeft;
        public Vector2 Position;

        private float NormalizeAngle
        {
            get
            {
                var angle = (int)Direction;
                if (angle < 0) angle += 360;
                return angle;
            }
        }

        public float AngleDifference(TowerData other)
        {
            var angle = NormalizeAngle;
            var otherAngle = other.NormalizeAngle;
            var diff = otherAngle - angle;
            if (diff < 0) diff += 360;
            return diff;
        }
    }

    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public MoveType MoveType = MoveType.Tower;

        public Vector4 PredictColor = EColor.RedBright;
        public bool ShowPredict = true;
        public TowerType TowerType = TowerType.First;
    }
}