
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;
internal class P2_Light_Rampant : SplatoonScript
{
    #region enums
    private enum State
    {
        None = 0,
        LightRampantCasting,
        TetherSpawned,
        TowerSpawned,
        AoeSET1,
        AoeSET2,
        AoeSET3,
        AoeSET4,
        AoeSET5,
        WaitStack,
        AvoidAOE,
        TowerIn,
        Banish,
    }
    #endregion

    #region class
    public class Config : IEzConfig
    {
        public bool NorthSwap = false;
        public PriorityData Priority = new();
    }

    private class PartyData
    {
        public int index = 0;
        public bool Mine => EntityId == Player.Object.EntityId;
        public uint EntityId = 0;
        public IPlayerCharacter? Object => (IPlayerCharacter)EntityId.GetObject() ?? null;
        public uint TetherPairId1 = 0;
        public uint TetherPairId2 = 0;
        public Direction TowerDirection = Direction.None;

        public PartyData(uint entityId)
        {
            EntityId = entityId;
        }
    }
    #endregion

    #region const
    private readonly List<(Direction, Vector3)> TowerPos =
    [
        new (Direction.NorthWest, new Vector3(86, 0, 92)),
        new (Direction.North, new Vector3(100, 0, 84)),
        new (Direction.NorthEast, new Vector3(114, 0, 92)),
        new (Direction.SouthEast, new Vector3(114, 0, 108)),
        new (Direction.South, new Vector3(100, 0, 116)),
        new (Direction.SouthWest, new Vector3(86, 0, 108)),
    ];
    #endregion

    #region public properties
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(2, "redmoon & Smoothtalk");
    #endregion

    #region private properties
    private State _state = State.None;
    private List<PartyData> _partyDataList = [];
    private int _onTetherCount = 0;
    private bool? _isClockwiseRotation = null;
    private Config C => Controller.GetConfig<Config>();
    private string NewPlayer = "";
    #endregion

    #region public methods
    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0) { tether = true, radius = 3f, thicc = 6f });
        Controller.RegisterElement("BaitObject", new Element(1) { tether = true, refActorComparisonType = 2, radius = 0.5f, thicc = 6f });
        for(var i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"Circle{i}", new Element(1) { radius = 5.0f, refActorComparisonType = 2, thicc = 2f, fillIntensity = 0.2f });
        }
        Controller.RegisterElement("DropSpotN1", new Element(0) { radius = 0.3f, thicc = 2f, Filled = true, fillIntensity = 1f });
        Controller.RegisterElement("DropSpotS1", new Element(0) { radius = 0.3f, thicc = 2f, Filled = true, fillIntensity = 1f });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 40212)
        {
            SetListEntityId();

            _state = State.LightRampantCasting;
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(_state != State.TetherSpawned) return;
        if(position == 9 && data1 == 1 && data2 == 2)
        {
            var pc = GetMinedata();

            if(pc.TowerDirection != Direction.None)
            {
                Controller.GetElementByName("Bait").SetRefPosition(TowerPos.Find(x => x.Item1 == pc.TowerDirection).Item2);
                Controller.GetElementByName("Bait").tether = true;
                Controller.GetElementByName("Bait").Enabled = true;
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null) return;
        var castId = set.Action.Value.RowId;

        if(castId == 40213)
        {
            OnReset();
        }
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Where(x => x.Value.Enabled).Each(x => x.Value.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint());
    }

    public override void OnReset()
    {
        _state = State.None;
        _onTetherCount = 0;
        _partyDataList.Clear();
        _isClockwiseRotation = null;
        HideAllElements();
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(_state != State.LightRampantCasting) return;
        if(data2 == 0 && data3 == 110 && data5 == 15)
        {
            var partyData = _partyDataList.Find(x => x.EntityId == source);
            if(partyData == null) return;
            partyData.TetherPairId1 = target;
            _onTetherCount++;

            if(_onTetherCount == 6)
            {
                if(C.Priority.GetFirstValidList() == null)
                {
                    PluginLog.Error("PriorityList is null");
                    OnReset();
                }

                if(ParseTether())
                {
                    _state = State.TetherSpawned;
                }
                else
                {
                    _state = State.None;
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("If NorthWest, NorthEast Swap, Check This box");
        ImGui.Checkbox("NorthSwap", ref C.NorthSwap);

        ImGui.Text("Set NorthWest ClockWise (ex. NorthWest -> North -> NorthEast -> SouthEast -> South -> SouthWest)");
        ImGui.Text("Swaper is NorthEast and SouthEast Players");
        C.Priority.Draw();
        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"OnTetherCount: {_onTetherCount}");
            ImGui.Text($"PartyDataList.Count: {_partyDataList.Count}");
            if(_isClockwiseRotation == null) ImGui.Text($"ClockwiseRotate: None");
            else ImGui.Text($"ClockwiseRotate: {_isClockwiseRotation}");
            if(_partyDataList.Count <= 0) return;
            foreach(var pc in _partyDataList)
            {
                if(pc.EntityId == 0) continue;
                ImGui.Text($"PartyData: {pc.EntityId.GetObject().Name.ToString()}");
                if(pc.TetherPairId1 == 0) continue;
                ImGui.Text($"TetherPairId1: {pc.TetherPairId1.GetObject().Name.ToString()}");
                if(pc.TetherPairId2 == 0) continue;
                ImGui.Text($"TetherPairId2: {pc.TetherPairId2.GetObject().Name.ToString()}");
                ImGui.Text($"TowerDirection: {pc.TowerDirection}");
            }
        }
    }
    #endregion

    #region private methods
    private readonly CombatRole[] SupportRoles = new CombatRole[] { CombatRole.Tank, CombatRole.Healer };
    private bool ParseTether()
    {
        foreach(var pc in _partyDataList)
        {
            if(pc.TetherPairId1 == 0) continue;

            var pair2 = _partyDataList.Find(x => x.TetherPairId1 == pc.EntityId);
            if(pair2 == null) continue;
            pc.TetherPairId2 = pair2.EntityId;
        }

        if(_partyDataList.Where(x => x.TetherPairId1 != 0 && x.TetherPairId2 != 0).Count() != 6) return false;

        var tetherList = _partyDataList.Where(x => x.TetherPairId1 != 0 && x.TetherPairId2 != 0).ToList();
        List<PartyData> tethersSortedList = [];
        var priority = C.Priority.GetPlayers(_ => true);
        if(priority == null || priority.Count != 8)
        {
            PluginLog.Information("PriorityList is null");
            return false;
        }

        // tetherListをpriority順に並べる
        foreach(var pc in priority)
        {
            var tether = tetherList.Find(x => x.EntityId == pc.IGameObject.EntityId);
            if(tether != null)
            {
                tethersSortedList.Add(tether);
            }
        }

        var Index = 0;
        foreach(var pc in tethersSortedList)
        {
            if(pc == null)
            {
                PluginLog.Information($"EntityId: {pc.EntityId} {Index}");
            }
            Index++;
        }

        if(priority == null || priority.Count == 0) return false;

        _isClockwiseRotation = ClockwiseRotation(tethersSortedList);

        // since both healers have puddles, SW DPS rotates to NW spot, move that player to 0 
        if(_isClockwiseRotation == true)
        {
            //insert last position first
            tethersSortedList.Insert(0, tethersSortedList[5]);

            //delete the last element
            tethersSortedList.RemoveAt(6);
        }

        // NW
        var x = _partyDataList.Find(x => x.EntityId == tethersSortedList[0].EntityId.GetObject().EntityId).TowerDirection = (C.NorthSwap != true) ? Direction.NorthWest : Direction.NorthEast;

        // N
        x = _partyDataList.Find(x => x.EntityId == tethersSortedList[1].EntityId.GetObject().EntityId).TowerDirection = (C.NorthSwap != true) ? Direction.North : Direction.South;

        // NE
        x = _partyDataList.Find(x => x.EntityId == tethersSortedList[2].EntityId.GetObject().EntityId).TowerDirection = (C.NorthSwap != true) ? Direction.NorthEast : Direction.NorthWest;

        // SE
        x = _partyDataList.Find(x => x.EntityId == tethersSortedList[3].EntityId.GetObject().EntityId).TowerDirection = Direction.SouthEast;

        // S
        x = _partyDataList.Find(x => x.EntityId == tethersSortedList[4].EntityId.GetObject().EntityId).TowerDirection = (C.NorthSwap != true) ? Direction.South : Direction.North;

        // SW
        x = _partyDataList.Find(x => x.EntityId == tethersSortedList[5].EntityId.GetObject().EntityId).TowerDirection = Direction.SouthWest;


        if(_partyDataList.Where(x => x.TowerDirection != Direction.None).Count() != 6) return false;
        else return true;
    }

    // Handle the odd case where two supports are selected and a dps becomes top left of the hexagon
    private bool? ClockwiseRotation(List<PartyData> tethersSortedList)
    {
        List<CombatRole> tetheredRoles = [];

        foreach(var pc in tethersSortedList)
        {
            if(pc.Object is IPlayerCharacter player)
            {
                var role = CharacterFunctions.GetRole(player);

                tetheredRoles.Add(role);
            }
        }

        // Count how many THJobs appear in the tetheredJobs list
        var thRoleCount = tetheredRoles.Count(role => SupportRoles.Contains(role));

        // Check if count is 2 (4 dps, 2 supports)
        if(thRoleCount == 2)
        {
            return true; // CW Rotation LR case detected
        }

        return false; // No Rotation LR case
    }

    #endregion

    #region API
    public enum Direction
    {
        None = 0,
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    public class DirectionalVector
    {
        public Direction Direction { get; }
        public Vector3 Position { get; }

        public DirectionalVector(Direction direction, Vector3 position)
        {
            Direction = direction;
            Position = position;
        }

        public override string ToString()
        {
            return $"{Direction}: {Position}";
        }
    }

    private void SetListEntityId()
    {
        _partyDataList.Clear();

        var index = 0;

        foreach(var pc in FakeParty.Get())
        {
            var job = pc.GetJob();
            _partyDataList.Add(new PartyData(0));

            _partyDataList[index].index = index;
            _partyDataList[index].EntityId = pc.EntityId;
            index++;
        }
    }

    private PartyData GetMinedata() => _partyDataList.Find(x => x.Mine);

    private void HideAllElements() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    #endregion
}