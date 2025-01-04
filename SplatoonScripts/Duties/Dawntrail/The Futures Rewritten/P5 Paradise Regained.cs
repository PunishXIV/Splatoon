using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
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

    public enum FirstBaitType
    {
        GoToSecondOrThirdTower,
        GoToOppositeFirstTower
    }

    public enum MoveType
    {
        FirstBait,
        SecondBait,
        Tower
    }

    public enum SecondBaitType
    {
        GoToFirstTower,
        GoToSafe
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

    public enum TowerType
    {
        First,
        Second,
        Third,
        Left,
        Right,
        FirstSafe,
        SecondSafe,
        LeftWhenSecondCleave,
        RightWhenSecondCleave
    }

    private readonly List<TowerData> _towers = [];

    private AttackType? _firstAttack;

    private Vector3 _firstBaitPosition;
    private Vector3 _secondBaitPosition;

    private State _state = State.None;

    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(3, "Garume");

    public Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        Controller.TryRegisterElement("Tower", new Element(0)
        {
            radius = 3f,
            thicc = 6f,
            overlayFScale = 3f,
            overlayVOffset = 3f,
            overlayText = "<< Go Here >>"
        });
        Controller.TryRegisterElement("PredictTower", new Element(0)
        {
            radius = 3f,
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
            overlayText = "<< Next >>"
        });

        Controller.TryRegisterElement("TankAOE", new Element(0)
        {
            radius = 5f,
            thicc = 6f,
            color = EColor.RedBright.ToUint()
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

    public void SetAoe(Vector3 position)
    {
        SetElementPosition("TankAOE", position);
    }

    public void HideAoe()
    {
        if (Controller.TryGetElementByName("TankAOE", out var aoe)) aoe.Enabled = false;
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
            if (C.ShowAOE)
            {
                Vector3 aoePosition;
                if (_firstAttack == AttackType.Light)
                    aoePosition = FakeParty.Get().Select(x => x.Position)
                        .MaxBy(x => Vector3.Distance(x, new Vector3(100, 0, 100)));
                else
                    aoePosition = FakeParty.Get().Select(x => x.Position)
                        .MinBy(x => Vector3.Distance(x, new Vector3(100, 0, 100)));

                SetAoe(aoePosition);
            }

            var firstTower = _towers[0];
            if (C.MoveType == MoveType.FirstBait)
                switch (C.FirstBaitType)
                {
                    case FirstBaitType.GoToSecondOrThirdTower:
                    {
                        var tankDirection = firstTower.RealAngle + (_firstAttack == AttackType.Light ? -120 : 120);
                        if (tankDirection >= 360) tankDirection -= 360;
                        if (tankDirection < 0) tankDirection += 360;
                        var position = new Vector3(100, 0, 92f);
                        _firstBaitPosition =
                            MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), tankDirection.DegreesToRadians(),
                                position);
                        SetBaitPosition(_firstBaitPosition);

                        tankDirection = firstTower.OppositeRealAngle;
                        position = new Vector3(100, 0, 100 - (_firstAttack == AttackType.Light ? 2f : 14f));
                        _secondBaitPosition =
                            MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), tankDirection.DegreesToRadians(),
                                position);
                        SetPredictBaitPosition(_secondBaitPosition);
                        break;
                    }
                    case FirstBaitType.GoToOppositeFirstTower:
                    {
                        var tankDirection = firstTower.OppositeRealAngle;
                        var position = new Vector3(100, 0, 92f);
                        _firstBaitPosition =
                            MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), tankDirection.DegreesToRadians(),
                                position);
                        SetBaitPosition(_firstBaitPosition);

                        position = new Vector3(100, 0, 100 - (_firstAttack == AttackType.Light ? 2f : 14f));
                        _secondBaitPosition =
                            MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), tankDirection.DegreesToRadians(),
                                position);
                        SetPredictBaitPosition(_secondBaitPosition);
                        break;
                    }
                }

            if (C.MoveType == MoveType.SecondBait)
                switch (C.SecondBaitType)
                {
                    case SecondBaitType.GoToFirstTower:
                    {
                        var tankDirection = firstTower.RealAngle;

                        var position = new Vector3(100, 0, 100f - (_firstAttack == AttackType.Light ? 14f : 2f));
                        _firstBaitPosition =
                            MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), tankDirection.DegreesToRadians(),
                                position);
                        SetBaitPosition(_firstBaitPosition);

                        tankDirection += _firstAttack == AttackType.Light ? -60 : 60;
                        position = new Vector3(100, 0, 92f);
                        _secondBaitPosition =
                            MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), tankDirection.DegreesToRadians(),
                                position);
                        SetPredictBaitPosition(_secondBaitPosition);
                        break;
                    }
                    case SecondBaitType.GoToSafe:
                    {
                        var tankDirection = firstTower.RealAngle + (_firstAttack == AttackType.Light ? -60 : 60);
                        if (tankDirection >= 360) tankDirection -= 360;
                        if (tankDirection < 0) tankDirection += 360;

                        var radius = new Vector3(100, 0, 100 - (_firstAttack == AttackType.Light ? 14f : 2f));
                        _firstBaitPosition =
                            MathHelper.RotateWorldPoint(new Vector3(100, 0, 100), tankDirection.DegreesToRadians(),
                                radius);
                        SetBaitPosition(_firstBaitPosition);

                        var position = new Vector3(100, 0, 92f);
                        _secondBaitPosition = MathHelper.RotateWorldPoint(new Vector3(100, 0, 100),
                            tankDirection.DegreesToRadians(), position);
                        SetPredictBaitPosition(_secondBaitPosition);
                        break;
                    }
                }

            if (C.MoveType == MoveType.Tower)
                switch (C.TowerType)
                {
                    case TowerType.First:
                        SetTowerPosition(firstTower.Position.ToVector3(0));
                        break;
                    case TowerType.Second:
                    {
                        if (_towers.Count == 2)
                            SetTowerPosition(_towers[1].Position.ToVector3(0));
                        break;
                    }
                    case TowerType.Third:
                    {
                        if (_towers.Count == 3)
                            SetPredictTowerPosition(_towers[2].Position.ToVector3(0));
                        break;
                    }
                    case TowerType.Left:
                    {
                        if (_towers.TryGetFirst(x => x.IsLeft == true, out var tower))
                        {
                            if (_towers.IndexOf(tower) == 1 && _firstAttack == AttackType.Dark)
                                SetTowerPosition(tower.Position.ToVector3(0));
                            else
                                SetPredictTowerPosition(tower.Position.ToVector3(0));
                        }

                        break;
                    }
                    case TowerType.Right:
                    {
                        if (_towers.TryGetFirst(x => x.IsLeft == false, out var tower))
                        {
                            if (_towers.IndexOf(tower) == 1 && _firstAttack == AttackType.Light)
                                SetTowerPosition(tower.Position.ToVector3(0));
                            else
                                SetPredictTowerPosition(tower.Position.ToVector3(0));
                        }

                        break;
                    }
                    case TowerType.FirstSafe:
                    {
                        var isLeft = _firstAttack == AttackType.Dark;
                        if (_towers.TryGetFirst(x => x.IsLeft == isLeft, out var tower))
                            SetTowerPosition(tower.Position.ToVector3(0));

                        break;
                    }
                    case TowerType.SecondSafe:
                    {
                        var isLeft = _firstAttack == AttackType.Light;
                        if (_towers.TryGetFirst(x => x.IsLeft == isLeft, out var tower))
                            SetPredictTowerPosition(tower.Position.ToVector3(0));

                        break;
                    }
                    case TowerType.LeftWhenSecondCleave:
                    {
                        if (_towers.TryGetFirst(x => x.IsLeft == true, out var tower))
                            SetPredictTowerPosition(tower.Position.ToVector3(0));

                        break;
                    }
                    case TowerType.RightWhenSecondCleave:
                    {
                        if (_towers.TryGetFirst(x => x.IsLeft == false, out var tower))
                            SetPredictTowerPosition(tower.Position.ToVector3(0));

                        break;
                    }
                }
        }

        if (_state == State.SecondTower)
        {
            if (C.ShowAOE)
            {
                Vector3 aoePosition;
                if (_firstAttack == AttackType.Light)
                    aoePosition = FakeParty.Get().Select(x => x.Position)
                        .MinBy(x => Vector3.Distance(x, new Vector3(100, 0, 100)));
                else
                    aoePosition = FakeParty.Get().Select(x => x.Position)
                        .MaxBy(x => Vector3.Distance(x, new Vector3(100, 0, 100)));

                SetAoe(aoePosition);
            }

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
                HidePredictTower();
                switch (C.TowerType)
                {
                    case TowerType.First:
                        HideTower();
                        break;
                    case TowerType.Second:
                        SetTowerPosition(_towers[1].Position.ToVector3(0));
                        break;
                    case TowerType.Third:
                    {
                        if (_towers.Count == 3) SetTowerPosition(_towers[2].Position.ToVector3(0));
                        break;
                    }
                    case TowerType.Left:
                    {
                        if (_towers.TryGetFirst(x => x.IsLeft == true, out var tower))
                            SetTowerPosition(tower.Position.ToVector3(0));
                        break;
                    }
                    case TowerType.Right:
                    {
                        if (_towers.TryGetFirst(x => x.IsLeft == false, out var tower))
                            SetTowerPosition(tower.Position.ToVector3(0));
                        break;
                    }
                    case TowerType.FirstSafe:
                    {
                        var isLeft = _firstAttack == AttackType.Dark;
                        if (_towers.TryGetFirst(x => x.IsLeft == isLeft, out var tower))
                            SetTowerPosition(tower.Position.ToVector3(0));

                        break;
                    }
                    case TowerType.SecondSafe:
                    {
                        var isLeft = _firstAttack == AttackType.Light;
                        if (_towers.TryGetFirst(x => x.IsLeft == isLeft, out var tower))
                            SetTowerPosition(tower.Position.ToVector3(0));

                        break;
                    }
                    case TowerType.LeftWhenSecondCleave:
                    {
                        if (_towers.TryGetFirst(x => x.IsLeft == true, out var tower))
                            SetTowerPosition(tower.Position.ToVector3(0));

                        break;
                    }
                    case TowerType.RightWhenSecondCleave:
                    {
                        if (_towers.TryGetFirst(x => x.IsLeft == false, out var tower))
                            SetTowerPosition(tower.Position.ToVector3(0));

                        break;
                    }
                }
            }
        }

        if (_state == State.ThirdTower)
        {
            HideAoe();
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
                HideTower();
                HidePredictTower();

                if (C.TowerType == TowerType.Third)
                {
                    SetTowerPosition(_towers[2].Position.ToVector3(0));
                }
                else if (C.TowerType == TowerType.Left)
                {
                    if (_towers.TryGetFirst(x => x.IsLeft == true, out var tower)) ShowTowerIfThird(tower);
                }
                else if (C.TowerType == TowerType.Right)
                {
                    if (_towers.TryGetFirst(x => x.IsLeft == false, out var tower))
                        ShowTowerIfThird(tower);
                }
                else if (C.TowerType == TowerType.LeftWhenSecondCleave)
                {
                    if (_towers.TryGetFirst(x => x.IsLeft == true, out var tower))
                        ShowTowerIfThird(tower);
                }
                else if (C.TowerType == TowerType.RightWhenSecondCleave)
                {
                    if (_towers.TryGetFirst(x => x.IsLeft == false, out var tower))
                        ShowTowerIfThird(tower);
                }
                else if (C.TowerType == TowerType.FirstSafe)
                {
                    var isLeft = _firstAttack == AttackType.Dark;
                    if (_towers.TryGetFirst(x => x.IsLeft == isLeft, out var tower))
                        ShowTowerIfThird(tower);
                }
                else if (C.TowerType == TowerType.SecondSafe)
                {
                    var isLeft = _firstAttack == AttackType.Light;
                    if (_towers.TryGetFirst(x => x.IsLeft == isLeft, out var tower))
                        ShowTowerIfThird(tower);
                }
            }
        }
    }

    private void ShowTowerIfThird(TowerData tower)
    {
        if (_towers.IndexOf(tower) == 2)
            SetTowerPosition(_towers[2].Position.ToVector3(0));
    }


    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 40319)
        {
            Reset();
            _state = State.Start;
        }

        if (_state != State.None && _firstAttack == null && castId == 40233) _firstAttack = AttackType.Dark;
        if (_state != State.None && _firstAttack == null && castId == 40313) _firstAttack = AttackType.Light;
    }

    public override void OnReset()
    {
        Reset();
    }

    public void Reset()
    {
        _state = State.None;
        _firstAttack = null;
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
            _state = _state switch
            {
                State.FirstTower => State.SecondTower,
                State.SecondTower => State.ThirdTower,
                State.ThirdTower => State.End,
                _ => _state
            };
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.EnumCombo("Move Type", ref C.MoveType);
        if (C.MoveType == MoveType.FirstBait)
            ImGuiEx.EnumCombo("First Bait Type", ref C.FirstBaitType);
        if (C.MoveType == MoveType.SecondBait)
            ImGuiEx.EnumCombo("Second Bait Type", ref C.SecondBaitType);
        if (C.MoveType == MoveType.Tower)
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
        ImGui.Checkbox("Show Tank AOE", ref C.ShowAOE);
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"Attack: {_firstAttack}");
            ImGui.Text($"First Bait: {_firstBaitPosition}");
            ImGui.Text($"Second Bait: {_secondBaitPosition}");
            ImGui.Text($"Towers: {_towers.Count}");
            for (var i = 0; i < _towers.Count; i++)
            {
                var tower = _towers[i];
                ImGui.Text($"Tower {i + 1}: {tower.Position} {tower.Direction} {tower.RealAngle} {tower.IsLeft}");
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

        public int RealAngle
        {
            get
            {
                var center = new Vector2(100, 100);
                var angle = Math.Atan2(Position.Y - center.Y, Position.X - center.X) * 180 / Math.PI;
                angle += 90;
                if (angle < 0) angle += 360;
                angle %= 360;
                return (int)angle;
            }
        }

        public int OppositeRealAngle
        {
            get
            {
                var center = new Vector2(100, 100);
                var angle = Math.Atan2(center.Y - Position.Y, center.X - Position.X) * 180 / Math.PI;
                angle += 90;
                if (angle < 0) angle += 360;
                angle %= 360;
                return (int)angle;
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
        public FirstBaitType FirstBaitType = FirstBaitType.GoToOppositeFirstTower;
        public MoveType MoveType = MoveType.Tower;

        public Vector4 PredictColor = EColor.RedBright;
        public SecondBaitType SecondBaitType = SecondBaitType.GoToSafe;
        public bool ShowAOE = true;
        public bool ShowPredict = true;
        public TowerType TowerType = TowerType.First;
    }
}