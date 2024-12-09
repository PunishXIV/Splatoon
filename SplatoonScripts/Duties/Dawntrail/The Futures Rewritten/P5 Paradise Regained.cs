using System.Collections.Generic;
using System.Numerics;
using ECommons;
using ECommons.Configuration;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P5_Paradise_Regained : SplatoonScript
{
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

    private State _state = State.None;

    private readonly List<Vector2> _towerPositions = [];

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
        Controller.TryRegisterElement("Predict Tower", new Element(0)
        {
            radius = 4f,
            thicc = 6f,
            overlayFScale = 3f,
            overlayVOffset = 3f,
            overlayText = "Your Tower"
        });

        Controller.TryRegisterElement("Bait", new Element(0)
        {
            radius = 0.5f,
            thicc = 6f,
            overlayFScale = 3f,
            overlayVOffset = 3f,
            overlayText = "<< Go Here >>"
        });
    }

    public override void OnUpdate()
    {
        if (_state is State.None or State.Start or State.End)
        {
            Controller.GetRegisteredElements().Each(e => e.Value.Enabled = false);
            return;
        }

        if (_state == State.FirstTower)
        {
            Controller.AddChatMessage("Please wait for the second tower to spawn.");
            return;
        }

        if (_state == State.SecondTower)
        {
            Controller.AddChatMessage("Please wait for the third tower to spawn.");
            return;
        }

        if (_state == State.ThirdTower)
        {
            Controller.AddChatMessage("Please wait for the fight to end.");
        }
    }


    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 40319) _state = State.Start;
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if (_state is State.None or State.Start) return;
        if (data1 == 1 && data2 == 2)
        {
            if (position == 51) _towerPositions.Add(new Vector2(93.93782f, 96.5f));
            if (position == 52) _towerPositions.Add(new Vector2(106, 96));
            if (position == 53) _towerPositions.Add(new Vector2(100, 107));

            if (_towerPositions.Count == 1)
                _state = State.FirstTower;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action is null) return;

        if (set.Action.Value.RowId == 40320)
        {
            if (_state == State.FirstTower) _state = State.SecondTower;
            if (_state == State.SecondTower) _state = State.ThirdTower;
            if (_state == State.ThirdTower) _state = State.End;
        }
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