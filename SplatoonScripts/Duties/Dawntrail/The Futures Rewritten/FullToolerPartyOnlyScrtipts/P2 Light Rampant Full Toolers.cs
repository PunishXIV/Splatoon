using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten.FullToolerPartyOnlyScrtipts;
internal class P2_Light_Rampant_Full_Toolers : SplatoonScript
{
    #region enums
    private enum State
    {
        None = 0,
        LightRampantCasting,
        TetherSpawned,
        TowerSpawned,
        SphereSpawn1,
        SphereSpawn2,
        TowerIn,
        Banish,
    }

    private enum NobuffState
    {
        None = 0,
        Set,
        AoeSET1,
        AoeSET2,
        AoeSET3,
        AoeSET4,
        AoeSET5,
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

    private class SphereData
    {
        public uint EntityId = 0;
        public Direction TowerDirection = Direction.None;

        public SphereData(uint entityId, Direction towerDirection)
        {
            EntityId = entityId;
            TowerDirection = towerDirection;
        }
    }
    #endregion

    #region const
    private readonly List<(Direction, Vector3)> TowerPos =
    [
        new (Direction.NorthWest, new Vector3(86, 0, 92)),
        new (Direction.North, new Vector3(100, 0, 84)),
        new (Direction.NorthEast, new Vector3(114, 0, 92)),
        new (Direction.East, new Vector3(120, 0, 100)),
        new (Direction.SouthEast, new Vector3(114, 0, 108)),
        new (Direction.South, new Vector3(100, 0, 116)),
        new (Direction.SouthWest, new Vector3(86, 0, 108)),
        new (Direction.West, new Vector3(80, 0, 100)),
    ];

    private readonly Vector3[] WestNeetSetReminderPos =
    {
        new(120f, 0f, 100f),
        new(113.740f, 0f, 100f),
        new(107.460f, 0f, 100f),
        new(100f, 0f, 103.000f),
        new(103.940f, 0f, 109.320f),
    };

    private readonly Vector3[] EastNeetSetReminderPos =
    {
        new(80f, 0f, 100f),
        new(86.260f, 0f, 100f),
        new(92.540f, 0f, 100f),
        new(100f, 0f, 96.000f),
        new(96.040f, 0f, 90.260f),
    };
    #endregion

    #region public properties
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(10, "redmoon");
    #endregion

    #region private properties
    private State _state = State.None;
    private List<PartyData> _partyDataList = [];
    private int _onTetherCount = 0;
    private List<SphereData> _sphereDataList = [];
    private int _luminusCount = 0;
    private bool _northSpawn = false;
    private bool _transLock = false;
    private uint _bashId = 0;
    #endregion

    #region public methods
    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0) { tether = true, radius = 3f, thicc = 6f });
        Controller.RegisterElement("CircleFix", new Element(0) { radius = 3f, thicc = 6f });
        Controller.RegisterElement("BaitObject", new Element(1) { tether = true, refActorComparisonType = 2, radius = 0.5f, thicc = 6f });
        for(var i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"Circle{i}", new Element(1) { radius = 5.0f, refActorComparisonType = 2, thicc = 2f, fillIntensity = 0.2f });
        }
        for(var i = 0; i < 5; i++)
        {
            Controller.RegisterElement($"WestDropSpot{i}", new Element(0) { radius = 0.2f, thicc = 2f, Filled = true, fillIntensity = 1f });
            Controller.GetElementByName($"WestDropSpot{i}").SetRefPosition(WestNeetSetReminderPos[i]);
            Controller.RegisterElement($"EastDropSpot{i}", new Element(0) { radius = 0.2f, thicc = 2f, Filled = true, fillIntensity = 1f });
            Controller.GetElementByName($"EastDropSpot{i}").SetRefPosition(EastNeetSetReminderPos[i]);
        }
        for(var i = 0; i < 4; i++)
        {
            Controller.RegisterElement($"WestDropLine{i}", new Element(2) { radius = 0f, thicc = 3f, Filled = true, fillIntensity = 1f });
            Controller.GetElementByName($"WestDropLine{i}").SetRefPosition(WestNeetSetReminderPos[i]);
            Controller.GetElementByName($"WestDropLine{i}").SetOffPosition(WestNeetSetReminderPos[i + 1]);
            Controller.RegisterElement($"EastDropLine{i}", new Element(2) { radius = 0f, thicc = 3f, Filled = true, fillIntensity = 1f });
            Controller.GetElementByName($"EastDropLine{i}").SetRefPosition(EastNeetSetReminderPos[i]);
            Controller.GetElementByName($"EastDropLine{i}").SetOffPosition(EastNeetSetReminderPos[i + 1]);
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 40212)
        {
            SetListEntityIdByJob();

            _state = State.LightRampantCasting;
        }
        if(castId is 40220 or 40221)
        {
            _bashId = castId;
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(_state != State.TetherSpawned) return;
        if(position == 9 && data1 == 1 && data2 == 2)
        {
            HideAllElements();
            var pc = GetMinedata();

            if(pc.TetherPairId1 != 0)
            {
                Controller.GetElementByName("Bait").SetRefPosition(TowerPos.Find(x => x.Item1 == pc.TowerDirection).Item2);
                Controller.GetElementByName("Bait").radius = 4f;
                Controller.GetElementByName("Bait").thicc = 6f;
                Controller.GetElementByName("Bait").tether = true;
                Controller.GetElementByName("Bait").Enabled = true;
            }
            else
            {
                Controller.GetElementByName("Bait").SetRefPosition(TowerPos.Find(x => x.Item1 == pc.TowerDirection).Item2);
                Controller.GetElementByName("Bait").radius = 0.3f;
                Controller.GetElementByName("Bait").thicc = 6f;
                Controller.GetElementByName("Bait").tether = true;
                Controller.GetElementByName("Bait").Enabled = true;
                _ = new TickScheduler(delegate
                {
                    var pc = GetMinedata();
                    if(pc.TowerDirection == Direction.None) return;
                    if(pc.TowerDirection == Direction.West) ShowEastDrops();
                    if(pc.TowerDirection == Direction.East) ShowWestDrops();
                }, 3500);
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null) return;
        var castId = set.Action.Value.RowId;
        if(castId == 40218)
        {
            _luminusCount++;
            if(_luminusCount == 1 && (GetMinedata().TowerDirection == Direction.East || GetMinedata().TowerDirection == Direction.West))
            {
                Controller.GetElementByName("Bait").Enabled = false;
            }
            if(_luminusCount == 10)
            {
                HideAllElements();
                if(_sphereDataList.Count >= 3)
                {
                    _state = State.SphereSpawn1;
                    ShowSphereAoeAndGuide1();
                }
                else
                {
                    DuoLog.Error("SphereDataList Count is not enough");
                    _state = State.None;
                    return;
                }
            }
        }
        if(castId == 40219 && _state == State.SphereSpawn1)
        {
            HideAllElements();
            _state = State.SphereSpawn2;
            ShowSphereAoeAndGuide2();
            _transLock = true;
            _ = new TickScheduler(delegate
            {
                _transLock = false;
            }, 1000);
        }
        if(castId == 40219 && _state == State.SphereSpawn2 && !_transLock)
        {
            HideAllElements();
            _state = State.TowerIn;
            ShowTowerIn();
        }
        if(castId is 40213 && _state == State.TowerIn)
        {
            HideAllElements();
            _state = State.Banish;
            ShowBanish(_bashId);
        }
        if(castId is 40220 or 40221)
        {
            OnReset();
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(vfxPath.Contains("vfx/common/eff/mon_pop1t.avfx") && _state is State.TetherSpawned or State.SphereSpawn1)
        {
            var closestDirection = Direction.North;
            var closestDistance = float.MaxValue;
            foreach(var towerData in TowerPos)
            {
                var distance = Vector3.Distance(target.GetObject().Position, towerData.Item2);
                if(distance < closestDistance)
                {
                    closestDistance = distance;
                    closestDirection = towerData.Item1;
                }
            }

            _sphereDataList.Add(new SphereData(target, closestDirection));
        }
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Where(x => x.Value.Enabled).Each(x => x.Value.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint());
        if(Controller.TryGetElementByName("CircleFix", out var el))
        {
            el.color = 0xFF0000FF;
        }
        for(var i = 0; i < 8; i++)
        {
            if(Controller.TryGetElementByName($"Circle{i}", out el))
            {
                el.color = 0xFF0000FF;
                el.fillIntensity = 0.5f;
            }
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _onTetherCount = 0;
        _partyDataList.Clear();
        _sphereDataList.Clear();
        _luminusCount = 0;
        _northSpawn = false;
        _transLock = false;
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
        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"OnTetherCount: {_onTetherCount}");
            ImGui.Text($"LuminusCount: {_luminusCount}");
            ImGui.Text($"PartyDataList.Count: {_partyDataList.Count}");
            if(_partyDataList.Count <= 0) return;
            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach(var x in _partyDataList)
            {
                Entries.Add(new ImGuiEx.EzTableEntry("Index", true, () => ImGui.Text(x.index.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("EntityId", true, () => ImGui.Text(x.EntityId.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text(x.EntityId.GetObject().Name.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Mine", true, () => ImGui.Text(x.Mine.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("TetherPairId1", true, () => ImGui.Text(x.TetherPairId1.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("TetherPairId2", true, () => ImGui.Text(x.TetherPairId2.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("TowerDirection", true, () => ImGui.Text(x.TowerDirection.ToString())));
            }

            ImGuiEx.EzTable(Entries);
        }
    }
    #endregion

    #region private methods
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

        var neetPc = _partyDataList.Where(x => x.TetherPairId1 == 0 && x.TetherPairId2 == 0).ToList();
        var allHealer = neetPc.All(x => x.index == 0 || x.index == 7);
        var nonHealer = neetPc.All(x => x.index != 0 && x.index != 7);


        DuoLog.Information($"neetPc: {neetPc.Count}, allHealer: {allHealer}, nonHealer: {nonHealer}");
        DuoLog.Information($"neetPc0: {neetPc[0].Object.Name}, neetPc1: {neetPc[1].Object.Name}");

        // ヒラはどちらも線付き
        if(nonHealer)
        {
            DuoLog.Information("Healer is not all neet");
            var h1 = _partyDataList.Find(x => x.index == 0);
            var h2 = _partyDataList.Find(x => x.index == 7);
            if(h1 == null) return false;

            h1.TowerDirection = Direction.North;
            h2.TowerDirection = Direction.SouthEast;

            var pc = (h2.TetherPairId1 == h1.EntityId) ? _partyDataList.Find(x => x.EntityId == h2.TetherPairId2) : _partyDataList.Find(x => x.EntityId == h2.TetherPairId1);
            DuoLog.Information($"pc: {pc.Object.Name}");
            if(pc == null) return false;
            pc.TowerDirection = Direction.NorthWest;

            var pc2 = (pc.TetherPairId1 == h2.EntityId) ? _partyDataList.Find(x => x.EntityId == pc.TetherPairId2) : _partyDataList.Find(x => x.EntityId == pc.TetherPairId1);
            DuoLog.Information($"pc2: {pc2.Object.Name}");
            if(pc2 == null) return false;
            pc2.TowerDirection = Direction.South;

            var pc3 = (pc2.TetherPairId1 == pc.EntityId) ? _partyDataList.Find(x => x.EntityId == pc2.TetherPairId2) : _partyDataList.Find(x => x.EntityId == pc2.TetherPairId1);
            DuoLog.Information($"pc3: {pc3.Object.Name}");
            if(pc3 == null) return false;
            pc3.TowerDirection = Direction.NorthEast;

            var pc4 = (pc3.TetherPairId1 == pc2.EntityId) ? _partyDataList.Find(x => x.EntityId == pc3.TetherPairId2) : _partyDataList.Find(x => x.EntityId == pc3.TetherPairId1);
            DuoLog.Information($"pc4: {pc4.Object.Name}");
            if(pc4 == null) return false;
            pc4.TowerDirection = Direction.SouthWest;

            // NEETは適当
            neetPc[0].TowerDirection = Direction.West;
            neetPc[1].TowerDirection = Direction.East;
        }
        // ヒラはどちらも線なし
        else if(allHealer)
        {
            DuoLog.Information("Healer is all neet");
            // index = 1から始める
            var mt = _partyDataList.Find(x => x.index == 1);
            if(mt == null) return false;
            var pc = _partyDataList.Find(x => x.EntityId == mt.TetherPairId1);
            if(pc == null) return false;

            mt.TowerDirection = Direction.North;
            pc.TowerDirection = Direction.SouthEast;

            var pc2 = (pc.TetherPairId1 == mt.EntityId) ? _partyDataList.Find(x => x.EntityId == pc.TetherPairId2) : _partyDataList.Find(x => x.EntityId == pc.TetherPairId1);
            if(pc2 == null) return false;
            pc2.TowerDirection = Direction.NorthWest;

            var pc3 = (pc2.TetherPairId1 == pc.EntityId) ? _partyDataList.Find(x => x.EntityId == pc2.TetherPairId2) : _partyDataList.Find(x => x.EntityId == pc2.TetherPairId1);
            if(pc3 == null) return false;
            pc3.TowerDirection = Direction.South;

            var pc4 = (pc3.TetherPairId1 == pc2.EntityId) ? _partyDataList.Find(x => x.EntityId == pc3.TetherPairId2) : _partyDataList.Find(x => x.EntityId == pc3.TetherPairId1);
            if(pc4 == null) return false;
            pc4.TowerDirection = Direction.NorthEast;

            var pc5 = (pc4.TetherPairId1 == pc3.EntityId) ? _partyDataList.Find(x => x.EntityId == pc4.TetherPairId2) : _partyDataList.Find(x => x.EntityId == pc4.TetherPairId1);
            if(pc5 == null) return false;
            pc5.TowerDirection = Direction.SouthWest;

            // NEETは適当
            neetPc.Find(x => x.index == 0).TowerDirection = Direction.West;
            neetPc.Find(x => x.index == 7).TowerDirection = Direction.East;
        }
        // ヒラはどちらか線付き
        else
        {
            DuoLog.Information("Healer is atleast Healer");
            var h = _partyDataList.Find(x => (x.index == 0 || x.index == 7) && x.TetherPairId1 != 0 && x.TetherPairId2 != 0);
            if(h == null) return false;

            h.TowerDirection = Direction.North;

            var pc = (h.TetherPairId1 == h.EntityId) ? _partyDataList.Find(x => x.EntityId == h.TetherPairId2) : _partyDataList.Find(x => x.EntityId == h.TetherPairId1);
            if(pc == null) return false;
            pc.TowerDirection = Direction.SouthEast;

            var pc2 = (pc.TetherPairId1 == h.EntityId) ? _partyDataList.Find(x => x.EntityId == pc.TetherPairId2) : _partyDataList.Find(x => x.EntityId == pc.TetherPairId1);
            if(pc2 == null) return false;
            pc2.TowerDirection = Direction.NorthWest;

            var pc3 = (pc2.TetherPairId1 == pc.EntityId) ? _partyDataList.Find(x => x.EntityId == pc2.TetherPairId2) : _partyDataList.Find(x => x.EntityId == pc2.TetherPairId1);
            if(pc3 == null) return false;
            pc3.TowerDirection = Direction.South;

            var pc4 = (pc3.TetherPairId1 == pc2.EntityId) ? _partyDataList.Find(x => x.EntityId == pc3.TetherPairId2) : _partyDataList.Find(x => x.EntityId == pc3.TetherPairId1);
            if(pc4 == null) return false;
            pc4.TowerDirection = Direction.NorthEast;

            var pc5 = (pc4.TetherPairId1 == pc3.EntityId) ? _partyDataList.Find(x => x.EntityId == pc4.TetherPairId2) : _partyDataList.Find(x => x.EntityId == pc4.TetherPairId1);
            if(pc5 == null) return false;
            pc5.TowerDirection = Direction.SouthWest;

            // NEETはヒラはWest
            var neetHealer = neetPc.Find(x => x.index == 0 || x.index == 7);
            if(neetHealer == null)
            {
                DuoLog.Information("Not Found Healer");
            }
            DuoLog.Information($"Healer: {neetHealer.Object.Name}");
            neetHealer.TowerDirection = Direction.East;

            // NEETは適当
            var neet = neetPc.Find(x => x.EntityId != neetHealer.EntityId);
            neet.TowerDirection = Direction.West;
        }

        if(_partyDataList.Where(x => x.TowerDirection != Direction.None).Count() != 8) return false;

        return true;
    }

    private void ShowEastDrops()
    {
        for(var i = 0; i < 5; i++)
        {
            Controller.GetElementByName($"EastDropSpot{i}").Enabled = true;
        }
        for(var i = 0; i < 4; i++)
        {
            Controller.GetElementByName($"EastDropLine{i}").Enabled = true;
        }
    }

    private void ShowWestDrops()
    {
        for(var i = 0; i < 5; i++)
        {
            Controller.GetElementByName($"WestDropSpot{i}").Enabled = true;
        }
        for(var i = 0; i < 4; i++)
        {
            Controller.GetElementByName($"WestDropLine{i}").Enabled = true;
        }
    }

    private void ShowSphereAoeAndGuide1()
    {
        // サークル表示
        for(var i = 0; i < 3; i++)
        {
            if(_sphereDataList[i].TowerDirection == Direction.North)
            {
                _northSpawn = true;
            }
            Controller.GetElementByName($"Circle{i}").refActorObjectID = _sphereDataList[i].EntityId;
            Controller.GetElementByName($"Circle{i}").Filled = true;
            Controller.GetElementByName($"Circle{i}").radius = 11.0f;
            Controller.GetElementByName($"Circle{i}").thicc = 4f;
            Controller.GetElementByName($"Circle{i}").Enabled = true;
        }

        if(Controller.TryGetElementByName("Bait", out var element))
        {
            element.Enabled = true;
            element.radius = 0.3f;

            var pc = GetMinedata();
            if(pc.Object.Position.Z < 100) // North
            {
                if(_northSpawn)
                {
                    element.SetRefPosition(new Vector3(111.280f, 0, 88.640f));
                }
                else
                {
                    element.SetRefPosition(new Vector3(103.300f, 0, 84.320f));
                }
            }
            else // South
            {
                if(_northSpawn)
                {
                    element.SetRefPosition(new Vector3(95.680f, 0, 115.480f));
                }
                else
                {
                    element.SetRefPosition(new Vector3(88.7f, 0, 111.280f));
                }
            }
        }
    }

    private void ShowSphereAoeAndGuide2()
    {
        // サークル表示
        for(var i = 3; i < 6; i++)
        {
            Controller.GetElementByName($"Circle{i}").refActorObjectID = _sphereDataList[i].EntityId;
            Controller.GetElementByName($"Circle{i}").Filled = true;
            Controller.GetElementByName($"Circle{i}").radius = 11.0f;
            Controller.GetElementByName($"Circle{i}").thicc = 4f;
            Controller.GetElementByName($"Circle{i}").Enabled = true;
        }

        if(Controller.TryGetElementByName("Bait", out var element))
        {
            var pc = GetMinedata();
            if(pc.Object.Position.Z > 100) // North
            {
                if(!_northSpawn)
                {
                    element.SetRefPosition(new Vector3(112, 0, 85));
                }
                else
                {
                    element.SetRefPosition(new Vector3(106, 0, 83));
                }
            }
            else // South
            {
                if(!_northSpawn)
                {
                    element.SetRefPosition(new Vector3(93, 0, 118));
                }
                else
                {
                    element.SetRefPosition(new Vector3(88, 0, 115));
                }
            }
            element.Enabled = true;
            element.radius = 0.3f;
        }
    }


    private void ShowTowerIn()
    {
        var pc = GetMinedata();
        var LightDebuff = pc.Object.StatusList.Where(x => x.StatusId == 2257).First();
        var hasDebuff2 = LightDebuff.Param == 2;

        if(hasDebuff2)
        {
            ApplyElement("Bait", Direction.North, 0f);
        }
        else
        {
            ApplyElement("Bait", pc.index switch
            {
                0 => Direction.West,
                1 => Direction.South,
                2 => Direction.SouthWest,
                3 => Direction.SouthEast,
                4 => Direction.NorthWest,
                5 => Direction.NorthEast,
                6 => Direction.North,
                7 => Direction.East,
                _ => Direction.None
            }
            , 8f);

            if(Controller.TryGetElementByName("CircleFix", out var element))
            {
                element.Filled = true;
                element.radius = 4.0f;
                element.thicc = 2f;
                element.fillIntensity = 0.5f;
                element.color = 0xFF0000FF;
                element.SetRefPosition(new Vector3(100, 0, 100));
                element.Enabled = true;
            }
        }

        foreach(var pc2 in _partyDataList)
        {
            var LightDebuff2 = pc2.Object.StatusList.Where(x => x.StatusId == 2257).First();
            var hasDebuff22 = LightDebuff2.Param == 2;
            if(hasDebuff22)
            {
                DuoLog.Information($"Has Debuff 2: {pc2.Object.Name.ToString()}");
            }
        }
    }

    private void ShowBanish(uint castId)
    {
        var pc = GetMinedata();

        if(castId == 40220) // Stack
        {
            ApplyElement("Bait", pc.index switch
            {
                0 => Direction.West,
                1 => Direction.South,
                2 => Direction.West,
                3 => Direction.South,
                4 => Direction.North,
                5 => Direction.East,
                6 => Direction.North,
                7 => Direction.East,
                _ => Direction.None
            }
            , 8f);
        }
        else if(castId == 40221) // No Stack
        {
            ApplyElement("Bait", pc.index switch
            {
                0 => Direction.West,
                1 => Direction.South,
                2 => Direction.SouthWest,
                3 => Direction.SouthEast,
                4 => Direction.NorthWest,
                5 => Direction.NorthEast,
                6 => Direction.North,
                7 => Direction.East,
                _ => Direction.None
            }
            , 8f);
        }
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

    public enum LR
    {
        Left = 0,
        Right
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

    private PartyData? GetMinedata() => _partyDataList.Find(x => x.Mine) ?? null;

    private void SetListEntityIdByJob()
    {
        _partyDataList.Clear();

        // リストに８人分の初期インスタンス生成
        for(var i = 0; i < 8; i++)
        {
            _partyDataList.Add(new PartyData(0));
            _partyDataList[i].index = i;
        }

        foreach(var pc in FakeParty.Get())
        {
            var job = pc.GetJob();
            switch(job)
            {
                case Job.WHM:
                case Job.AST:
                    _partyDataList[0].EntityId = pc.EntityId;
                    break;

                case Job.PLD:
                    _partyDataList[1].EntityId = pc.EntityId;
                    break;

                case Job.SAM:
                case Job.MNK:
                case Job.DRG:
                case Job.RPR:
                    _partyDataList[2].EntityId = pc.EntityId;
                    break;

                case Job.NIN:
                case Job.VPR:
                case Job.RDM:
                case Job.BLM:
                case Job.SMN:
                    _partyDataList[3].EntityId = pc.EntityId;
                    break;

                case Job.BRD:
                case Job.MCH:
                case Job.DNC:
                    _partyDataList[4].EntityId = pc.EntityId;
                    break;

                case Job.PCT:
                    _partyDataList[5].EntityId = pc.EntityId;
                    break;

                case Job.WAR:
                case Job.DRK:
                case Job.GNB:
                    _partyDataList[6].EntityId = pc.EntityId;
                    break;

                case Job.SCH:
                case Job.SGE:
                    _partyDataList[7].EntityId = pc.EntityId;
                    break;
            }
        }
    }

    private Direction DividePoint(Vector3 Position, float Distance, Vector3? Center = null)
    {
        // Distance, Centerの値を用いて、８方向のベクトルを生成
        var directionalVectors = GenerateDirectionalVectors(Distance, Center ?? new Vector3(100, 0, 100));

        // ８方向の内、最も近い方向ベクトルを取得
        var closestDirection = Direction.North;
        var closestDistance = float.MaxValue;
        foreach(var directionalVector in directionalVectors)
        {
            var distance = Vector3.Distance(Position, directionalVector.Position);
            if(distance < closestDistance)
            {
                closestDistance = distance;
                closestDirection = directionalVector.Direction;
            }
        }

        return closestDirection;
    }

    public static List<DirectionalVector> GenerateDirectionalVectors(float distance, Vector3? center = null)
    {
        var directionalVectors = new List<DirectionalVector>();

        // 各方向のオフセット計算
        foreach(Direction direction in Enum.GetValues(typeof(Direction)))
        {
            if(direction == Direction.None) continue; // Noneはスキップ

            var offset = direction switch
            {
                Direction.North => new Vector3(0, 0, -1),
                Direction.NorthEast => Vector3.Normalize(new Vector3(1, 0, -1)),
                Direction.East => new Vector3(1, 0, 0),
                Direction.SouthEast => Vector3.Normalize(new Vector3(1, 0, 1)),
                Direction.South => new Vector3(0, 0, 1),
                Direction.SouthWest => Vector3.Normalize(new Vector3(-1, 0, 1)),
                Direction.West => new Vector3(-1, 0, 0),
                Direction.NorthWest => Vector3.Normalize(new Vector3(-1, 0, -1)),
                _ => Vector3.Zero
            };

            // 距離を適用して座標を計算
            var position = (center ?? new Vector3(100, 0, 100)) + (offset * distance);

            // リストに追加
            directionalVectors.Add(new DirectionalVector(direction, position));
        }

        return directionalVectors;
    }

    // 2つのDirectionを比較して、角度を返す。角度は正しい値ではなく0, 45, 90, 135, 180の値を返す
    private int GetTwoPointAngle(Direction direction1, Direction direction2)
    {
        // enumの値を数値に変換
        var angle1 = (int)direction1;
        var angle2 = (int)direction2;

        // 環状の差分を計算
        var diff = (angle2 - angle1 + 8) % 8; // 環状に補正して差分を取得

        // 差分に応じた角度を計算（時計回りで正、反時計回りで負）
        var angle = diff switch
        {
            0 => 0,
            1 => 45,
            2 => 90,
            3 => 135,
            4 => 180,
            5 => -135,
            6 => -90,
            7 => -45,
            _ => 0 // このケースは通常発生しない
        };

        return angle;
    }

    // Directionと45倍数の角度から角度を算出してDirectionを返す
    private Direction GetDirectionFromAngle(Direction direction, int angle)
    {
        if(angle == 45)
        {
            return direction switch
            {
                Direction.North => Direction.NorthEast,
                Direction.NorthEast => Direction.East,
                Direction.East => Direction.SouthEast,
                Direction.SouthEast => Direction.South,
                Direction.South => Direction.SouthWest,
                Direction.SouthWest => Direction.West,
                Direction.West => Direction.NorthWest,
                Direction.NorthWest => Direction.North,
                _ => Direction.None
            };
        }
        else if(angle == 90)
        {
            return direction switch
            {
                Direction.North => Direction.East,
                Direction.NorthEast => Direction.SouthEast,
                Direction.East => Direction.South,
                Direction.SouthEast => Direction.SouthWest,
                Direction.South => Direction.West,
                Direction.SouthWest => Direction.NorthWest,
                Direction.West => Direction.North,
                Direction.NorthWest => Direction.NorthEast,
                _ => Direction.None
            };
        }
        else if(angle == 135)
        {
            return direction switch
            {
                Direction.North => Direction.NorthWest,
                Direction.NorthWest => Direction.West,
                Direction.West => Direction.SouthWest,
                Direction.SouthWest => Direction.South,
                Direction.South => Direction.SouthEast,
                Direction.SouthEast => Direction.East,
                Direction.East => Direction.NorthEast,
                Direction.NorthEast => Direction.North,
                _ => Direction.None
            };
        }
        else if(angle == 180)
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
                _ => Direction.None
            };
        }
        else if(angle == -45)
        {
            return direction switch
            {
                Direction.North => Direction.NorthWest,
                Direction.NorthWest => Direction.West,
                Direction.West => Direction.SouthWest,
                Direction.SouthWest => Direction.South,
                Direction.South => Direction.SouthEast,
                Direction.SouthEast => Direction.East,
                Direction.East => Direction.NorthEast,
                Direction.NorthEast => Direction.North,
                _ => Direction.None
            };
        }
        else if(angle == -90)
        {
            return direction switch
            {
                Direction.North => Direction.West,
                Direction.NorthWest => Direction.SouthWest,
                Direction.West => Direction.South,
                Direction.SouthWest => Direction.SouthEast,
                Direction.South => Direction.East,
                Direction.SouthEast => Direction.NorthEast,
                Direction.East => Direction.North,
                Direction.NorthEast => Direction.NorthWest,
                _ => Direction.None
            };
        }
        else if(angle == -135)
        {
            return direction switch
            {
                Direction.North => Direction.NorthEast,
                Direction.NorthEast => Direction.East,
                Direction.East => Direction.SouthEast,
                Direction.SouthEast => Direction.South,
                Direction.South => Direction.SouthWest,
                Direction.SouthWest => Direction.West,
                Direction.West => Direction.NorthWest,
                Direction.NorthWest => Direction.North,
                _ => Direction.None
            };
        }
        else if(angle == -180)
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
                _ => Direction.None
            };
        }
        return Direction.None;
    }

    // 2つのDirectionを比較して、左右どちらかを返す。左なら-1、右なら1、同じまたは逆なら0を返す
    private LR GetTwoPointLeftRight(Direction direction1, Direction direction2)
    {
        if(direction1 == Direction.North && direction2 == Direction.NorthEast) return LR.Right;
        if(direction1 == Direction.NorthEast && direction2 == Direction.East) return LR.Right;
        if(direction1 == Direction.East && direction2 == Direction.SouthEast) return LR.Right;
        if(direction1 == Direction.SouthEast && direction2 == Direction.South) return LR.Right;
        if(direction1 == Direction.South && direction2 == Direction.SouthWest) return LR.Right;
        if(direction1 == Direction.SouthWest && direction2 == Direction.West) return LR.Right;
        if(direction1 == Direction.West && direction2 == Direction.NorthWest) return LR.Right;
        if(direction1 == Direction.NorthWest && direction2 == Direction.North) return LR.Right;

        if(direction1 == Direction.North && direction2 == Direction.West) return LR.Left;
        if(direction1 == Direction.West && direction2 == Direction.South) return LR.Left;
        if(direction1 == Direction.South && direction2 == Direction.East) return LR.Left;
        if(direction1 == Direction.East && direction2 == Direction.North) return LR.Left;

        if(direction1 == Direction.North && direction2 == Direction.SouthEast) return LR.Right;
        if(direction1 == Direction.NorthEast && direction2 == Direction.South) return LR.Right;
        if(direction1 == Direction.East && direction2 == Direction.SouthWest) return LR.Right;
        if(direction1 == Direction.SouthEast && direction2 == Direction.West) return LR.Right;
        if(direction1 == Direction.South && direction2 == Direction.NorthWest) return LR.Right;
        if(direction1 == Direction.SouthWest && direction2 == Direction.North) return LR.Right;
        if(direction1 == Direction.West && direction2 == Direction.NorthEast) return LR.Right;
        if(direction1 == Direction.NorthWest && direction2 == Direction.East) return LR.Right;

        return LR.Left;
    }
    private void HideAllElements() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    private Direction Rotate45Clockwise(Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.NorthEast,
            Direction.NorthEast => Direction.East,
            Direction.East => Direction.SouthEast,
            Direction.SouthEast => Direction.South,
            Direction.South => Direction.SouthWest,
            Direction.SouthWest => Direction.West,
            Direction.West => Direction.NorthWest,
            Direction.NorthWest => Direction.North,
            _ => Direction.None
        };
    }

    private Direction GetOppositeDirection(Direction direction)
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
            _ => Direction.None
        };
    }

    private void ApplyElement(string elementName, Direction direction, float radius, float elementRadius = 0.3f, bool isTether = true)
    {
        var position = new Vector3(100, 0, 100);
        var angle = GetAngle(direction);
        position += radius * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));
        if(Controller.TryGetElementByName(elementName, out var element))
        {
            element.Enabled = true;
            element.radius = elementRadius;
            element.tether = isTether;
            element.SetRefPosition(position);
        }
    }

    private float GetAngle(Direction direction)
    {
        return direction switch
        {
            Direction.North => 270,
            Direction.NorthEast => 315,
            Direction.East => 0,
            Direction.SouthEast => 45,
            Direction.South => 90,
            Direction.SouthWest => 135,
            Direction.West => 180,
            Direction.NorthWest => 225,
            _ => 0
        };
    }

    /// <summary>
    /// Calculates the correction angle needed to rotate the object to face the target.
    /// </summary>
    /// <param name="origin">The current position of the object.</param>
    /// <param name="target">The position of the target.</param>
    /// <param name="rotation">The current rotation angle of the object (in radian).</param>
    /// <returns>The correction angle (in degrees) needed to face the target.</returns>
    public static float GetCorrectionAngle(Vector3 origin, Vector3 target, float rotation) => GetCorrectionAngle(MathHelper.ToVector2(origin), MathHelper.ToVector2(target), rotation);

    public static float GetCorrectionAngle(Vector2 origin, Vector2 target, float rotation)
    {
        // Calculate the relative angle to the target
        var direction = target - origin;
        var relativeAngle = MathF.Atan2(direction.Y, direction.X) * (180 / MathF.PI);

        // Normalize relative angle to 0-360 range
        relativeAngle = (relativeAngle + 360) % 360;

        // Calculate the correction angle
        var correctionAngle = (relativeAngle - ConvertRotationRadiansToDegrees(rotation) + 360) % 360;

        // Adjust correction angle to range -180 to 180 for shortest rotation
        if(correctionAngle > 180)
            correctionAngle -= 360;

        return correctionAngle;
    }

    /// <summary>
    /// Converts a rotation angle in radians to degrees in a system where:
    /// - North is 0°
    /// - Angles increase clockwise
    /// - Range is 0° to 360°
    /// </summary>
    /// <param name="radians">The rotation angle in radians.</param>
    /// <returns>The equivalent rotation angle in degrees.</returns>
    public static float ConvertRotationRadiansToDegrees(float radians)
    {
        // Convert radians to degrees with coordinate system adjustment
        var degrees = ((-radians * (180 / MathF.PI)) + 180) % 360;

        // Ensure the result is within the 0° to 360° range
        return degrees < 0 ? degrees + 360 : degrees;
    }

    /// <summary>
    /// Converts a rotation angle in degrees to radians in a system where:
    /// - North is 0°
    /// - Angles increase clockwise
    /// - Range is -π to π
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    /// <returns>The equivalent rotation angle in radians.</returns>
    public static float ConvertDegreesToRotationRadians(float degrees)
    {
        // Convert degrees to radians with coordinate system adjustment
        var radians = -(degrees - 180) * (MathF.PI / 180);

        // Normalize the result to the range -π to π
        radians = ((radians + MathF.PI) % (2 * MathF.PI)) - MathF.PI;

        return radians;
    }
    #endregion
}
