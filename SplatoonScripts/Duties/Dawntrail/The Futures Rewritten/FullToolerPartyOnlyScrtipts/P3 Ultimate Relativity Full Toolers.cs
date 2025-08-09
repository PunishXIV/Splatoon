using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten.FullToolerPartyOnlyScrtipts;
internal class P3_Ultimate_Relativity_Full_Toolers : SplatoonScript
{
    #region types
    /********************************************************************/
    /* types                                                            */
    /********************************************************************/
    private enum State
    {
        None = 0,
        GimmickStart,
        Fire1,
        Interval1,
        Laser1,
        Fire2,
        Interval2,
        Laser2,
        Fire3,
        Interval3,
        Laser3,
        Stack
    }

    private enum Wise
    {
        Clockwise = 0,
        CounterClockwise
    }

    private delegate void MineRoleAction();
    #endregion

    #region class
    /********************************************************************/
    /* class                                                            */
    /********************************************************************/
    public class Config : IEzConfig
    {
        public bool NorthSwap = false;
        public PriorityData Priority = new();
    }

    private class HourGlassData
    {
        public uint EntityId = 0;
        public Wise wise = Wise.Clockwise;
        public int ClockDirection = 0;

        public HourGlassData(uint entityId, Wise wise)
        {
            EntityId = entityId;
            this.wise = wise;
        }
    }

    private class PartyData
    {
        public int Index = 0;
        public bool Mine => EntityId == Player.Object.EntityId;
        public uint EntityId = 0;
        public int DebuffTime = 0;
        public bool IsBlizzard = false;
        public bool IsEruption = false;
        public bool IsDarkWater = false;
        public bool IsStacker = false;
        public IPlayerCharacter? Object => (IPlayerCharacter)EntityId.GetObject()! ?? null;

        public bool IsTank => TankJobs.Contains(Object?.GetJob() ?? Job.WHM);
        public bool IsHealer => HealerJobs.Contains(Object?.GetJob() ?? Job.PLD);
        public bool IsTH => IsTank || IsHealer;
        public bool IsMeleeDps => MeleeDpsJobs.Contains(Object?.GetJob() ?? Job.MCH);
        public bool IsRangedDps => RangedDpsJobs.Contains(Object?.GetJob() ?? Job.MNK);
        public bool IsDps => IsMeleeDps || IsRangedDps;

        public PartyData(uint entityId, int index)
        {
            EntityId = entityId;
            Index = index;
        }
    }
    #endregion

    #region const
    /********************************************************************/
    /* const                                                            */
    /********************************************************************/
    private enum Debuff : uint
    {
        Holy = 0x996, // ホーリガ
        Fire = 0x997, // ファイガ
        ShadowEye = 0x998, // シャドウアイ
        Eruption = 0x99C, // エラプ
        Blizzard = 0x99E, // ブリザガ
        DarkWater = 0x99D, // ウォタガ
    }
    #endregion

    #region public properties
    /********************************************************************/
    /* public properties                                                */
    /********************************************************************/
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(7, "redmoon");
    #endregion

    #region private properties
    /********************************************************************/
    /* private properties                                               */
    /********************************************************************/
    private State _state = State.None;
    private List<PartyData> _partyDataList = [];
    private List<DirectionCalculator.Direction> _tetherDarkDirection = [];
    private List<DirectionCalculator.Direction> _tetherLightDirection = [];
    private List<HourGlassData> _allHourGlasses = [];
    private List<HourGlassData> _currentHourGlasses = [];
    private ClockDirectionCalculator? _clockDirectionCalculator = null;
    private MineRoleAction mineRoleAction = delegate { PluginLog.Information("mineRoleAction is null"); };
    private bool _transLock = false;
    private int _beamCount = 0;
    private bool _isBlizzardTH = false;
    private bool _showSinboundMeltdown = false;
    #endregion

    #region public methods
    /********************************************************************/
    /* public methods                                                   */
    /********************************************************************/
    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0) { tether = true, radius = 0.5f, thicc = 6f });
        Controller.RegisterElement("ReturnPoint", new Element(0) { tether = true, radius = 0.5f, thicc = 6f, color = 0x80FF00FF });
        Controller.RegisterElement("Text", new Element(0)
        {
            offX = 100f,
            offY = 100f,
            overlayFScale = 5f,
            overlayVOffset = 5f,
            radius = 0f
        });

        for(var i = 0; i < 3; ++i)
        {
            var element = new Element(2)
            {
                thicc = 2f,
                radius = 2.5f,
                fillIntensity = 0.25f,
                Filled = true
            };
            Controller.RegisterElement($"SinboundMeltdown{i}", element);
        }

        Controller.RegisterElement($"CircleObject", new Element(1)
        {
            thicc = 2f,
            radius = 2.5f,
            fillIntensity = 0.25f,
            refActorComparisonType = 2,
            Filled = true
        });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 40266)
        {
            SetListEntityIdByJob();
            _state = State.GimmickStart;
        }

        if(_state == State.None) return;

        if(castId == 40291) _showSinboundMeltdown = true;

        if(castId == 40269) OnReset();
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null) return;
        var castId = set.Action.Value.RowId;

        if(castId == 40276 && !_transLock) // Fire
        {
            _transLock = true;
            _ = new TickScheduler(() => _transLock = false, 2000);

            _state = _state switch
            {
                State.Fire1 => State.Interval1,
                State.Fire2 => State.Interval2,
                State.Fire3 => State.Interval3,
                _ => State.None
            };

            HideAllElements();
            mineRoleAction();
        }

        if(castId == 40291 && !_transLock) // Laser
        {
            _transLock = true;
            _ = new TickScheduler(() => _transLock = false, 2000);

            _state = _state switch
            {
                State.Laser1 => State.Fire2,
                State.Laser2 => State.Fire3,
                State.Laser3 => State.Laser3, // リターンがあるのでエラプ後に遷移させる
                _ => State.None
            };

            if(_state == State.Laser3 && mineRoleAction.Method.Name == "MiddleFire")
            {
                HideAllElements();
                var pc = GetMinedata();
                if(pc == null) return;
                var clock = pc.IsDarkWater ? 3 : 9;
                if(_clockDirectionCalculator == null) return;
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(clock), 8f);
            }
            else
            {
                HideAllElements();
                mineRoleAction();
            }
        }

        if(castId == 40274 && !_transLock) // Return (Eruption)
        {
            _transLock = true;
            _ = new TickScheduler(() => _transLock = false, 1000);

            _state = State.Stack;

            HideAllElements();
            mineRoleAction();
        }

        if(castId == 40286 && !_transLock) // Stack
        {
            _transLock = true;
            _ = new TickScheduler(() => _transLock = false, 1000);

            OnReset();
        }

        if(castId is 40291 or 40235)
        {
            HideSinboundMeltdown();
            _showSinboundMeltdown = false;
        }
    }

    public override void OnUpdate()
    {
        if(_state == State.None) return;

        if(_showSinboundMeltdown) ShowSinboundMeltdown();

        ShowStackRange();


        if(Controller.TryGetElementByName("Bait", out var el))
        {
            if(el.Enabled) el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _tetherDarkDirection.Clear();
        _tetherLightDirection.Clear();
        _allHourGlasses.Clear();
        _currentHourGlasses.Clear();
        _partyDataList.Clear();
        _clockDirectionCalculator = null;
        mineRoleAction = delegate { PluginLog.Information("mineRoleAction is null"); };
        HideAllElements();
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(_state == State.None) return;
        if(data2 == 0 && data3 == 134 && data5 == 15 && source.TryGetObject(out var obj))
        {
            _tetherLightDirection.Add(DirectionCalculator.DividePoint(obj.Position, 10));

        }
        else if(data2 == 0 && data3 == 133 && data5 == 15 && source.TryGetObject(out var obj2))
        {
            _tetherDarkDirection.Add(DirectionCalculator.DividePoint(obj2.Position, 10));
        }

        // デバフ設定未完了
        if(_partyDataList.Any(x => x.DebuffTime == 0))
        {
            foreach(var x in _partyDataList)
            {
                if(x.EntityId.TryGetObject(out var obj3) && obj3 is IPlayerCharacter pc)
                {
                    var debuff = pc.StatusList.FirstOrDefault(x => x.StatusId == (uint)Debuff.Fire);
                    if(debuff == null)
                    {
                        debuff = pc.StatusList.FirstOrDefault(x => x.StatusId == (uint)Debuff.Blizzard);
                        if(debuff == null) continue;
                        x.IsBlizzard = true;
                        if(pc.StatusList.Any(x => x.StatusId == (uint)Debuff.Eruption)) x.IsEruption = true;
                        if(pc.StatusList.Any(x => x.StatusId == (uint)Debuff.DarkWater)) x.IsDarkWater = true;
                        if(pc.StatusList.Any(x => x.StatusId is (uint)Debuff.DarkWater or (uint)Debuff.ShadowEye)) x.IsStacker = true;
                        continue;
                    }
                    x.DebuffTime = debuff.RemainingTime switch
                    {
                        >= 21 => 30,
                        >= 11 => 20,
                        >= 1 => 10,
                        _ => 0,
                    };

                    if(pc.StatusList.Any(x => x.StatusId == (uint)Debuff.Eruption)) x.IsEruption = true;
                    if(pc.StatusList.Any(x => x.StatusId == (uint)Debuff.DarkWater)) x.IsDarkWater = true;
                    if(pc.StatusList.Any(x => x.StatusId is (uint)Debuff.DarkWater or (uint)Debuff.ShadowEye)) x.IsStacker = true;
                }
            }

            // 早ファイガ、遅ファイガのどちらかが２人しかおらず、そこにブリザガを割り当てる
            // 早ファイガ
            if(_partyDataList.Count(x => x.DebuffTime == 10) == 2)
            {
                var blizzard = _partyDataList.Find(x => x.IsBlizzard);
                if(blizzard != null)
                {
                    blizzard.DebuffTime = 10;
                }
            }
            // 遅ファイガ
            if(_partyDataList.Count(x => x.DebuffTime == 30) == 2)
            {
                var blizzard = _partyDataList.Find(x => x.IsBlizzard);
                if(blizzard != null)
                {
                    blizzard.DebuffTime = 30;
                }
            }

            if(_partyDataList.Any(x => x.DebuffTime == 0))
            {
                _state = State.None;
                return;
            }

            // 自分自身のデバフ時間、isBlizzardから関数設定
            var my = GetMinedata();
            if(my == null)
            {
                _state = State.None;
                return;
            }

            if(my.DebuffTime == 10) mineRoleAction = EarlyFire;
            else if(my.DebuffTime == 20) mineRoleAction = MiddleFire;
            else if(my.DebuffTime == 30) mineRoleAction = LateFire;

            if(_partyDataList.Any(x => x.IsTH && x.IsBlizzard)) _isBlizzardTH = true;
        }

        if(_tetherLightDirection.Count == 3 && _tetherDarkDirection.Count == 2)
        {
            var direction = DirectionCalculator.GetDirectionFromAngle(_tetherDarkDirection[0], 90);
            if(direction == DirectionCalculator.Direction.None) return;
            if(_tetherLightDirection.Any(x => x == direction))
            {
                _clockDirectionCalculator = new ClockDirectionCalculator(direction);
            }
            else
            {
                _clockDirectionCalculator = new ClockDirectionCalculator(DirectionCalculator.GetOppositeDirection(direction));
            }

            if(mineRoleAction == null || !_clockDirectionCalculator.isValid) return;
            _state = State.Fire1;
            mineRoleAction();
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(_state == State.None) return;

        if(vfxPath == "vfx/common/eff/m0489_stlp_left01f_c0d1.avfx")
        {
            if(target.TryGetObject(out var obj))
            {
                HourGlassData hourGlassData = new(target, Wise.CounterClockwise);
                if(_clockDirectionCalculator == null) return;
                hourGlassData.ClockDirection = _clockDirectionCalculator.GetClockFromDirection(DirectionCalculator.DividePoint(obj.Position, 10));
                _allHourGlasses.Add(hourGlassData);
            }
        }
        else if(vfxPath == "vfx/common/eff/m0489_stlp_right_c0d1.avfx")
        {
            if(target.TryGetObject(out var obj))
            {
                HourGlassData hourGlassData = new(target, Wise.Clockwise);
                if(_clockDirectionCalculator == null) return;
                hourGlassData.ClockDirection = _clockDirectionCalculator.GetClockFromDirection(DirectionCalculator.DividePoint(obj.Position, 10));
                _allHourGlasses.Add(hourGlassData);
            }
        }

        if(_allHourGlasses.Count == 3 && _state == State.Interval1)
        {
            _currentHourGlasses.Clear();
            // 0 ~ 2
            _currentHourGlasses.AddRange(_allHourGlasses.GetRange(0, 3));
            _state = State.Laser1;

            HideAllElements();
            mineRoleAction();
        }
        if(_allHourGlasses.Count == 6 && _state == State.Interval2)
        {
            _currentHourGlasses.Clear();
            // 3 ~ 5
            _currentHourGlasses.AddRange(_allHourGlasses.GetRange(3, 3));
            _state = State.Laser2;

            HideAllElements();
            mineRoleAction();
        }
        if(_allHourGlasses.Count == 8 && _state == State.Interval3)
        {
            _currentHourGlasses.Clear();
            // 6 ~ 7
            _currentHourGlasses.AddRange(_allHourGlasses.GetRange(6, 2));
            _state = State.Laser3;

            HideAllElements();
            mineRoleAction();
        }

    }

    public override void OnSettingsDraw()
    {
        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            if(_clockDirectionCalculator != null)
            {
                ImGui.Text($"12ClockDirection: {_clockDirectionCalculator.Get12ClockDirection()}");
            }
            ImGui.Text($"TransLock: {_transLock}");
            ImGui.Text($"IsBlizzardTH: {_isBlizzardTH}");
            ImGui.Text($"TetherLightDirection: {_tetherLightDirection.Count}");
            ImGui.Text($"TetherDarkDirection: {_tetherDarkDirection.Count}");
            ImGui.Text($"AllHourGlasses: {_allHourGlasses.Count}");
            ImGui.Text($"CurrentHourGlasses: {_currentHourGlasses.Count}");
            ImGui.Text($"PartyData: {_partyDataList.Count}");
            ImGui.Text($"MineRoleAction: {mineRoleAction.Method.Name}");
            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach(var x in _partyDataList)
            {
                var pcObj = x.EntityId.GetObject() as IPlayerCharacter ?? null;
                if(pcObj == null) continue;
                Entries.Add(new ImGuiEx.EzTableEntry("Index", true, () => ImGui.Text(x.Index.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("EntityId", true, () => ImGui.Text(x.EntityId.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text(pcObj.Name.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Mine", true, () => ImGui.Text(x.Mine.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("DebuffTime", true, () => ImGui.Text(x.DebuffTime.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsBlizzard", true, () => ImGui.Text(x.IsBlizzard.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsEruption", true, () => ImGui.Text(x.IsEruption.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsDarkWater", true, () => ImGui.Text(x.IsDarkWater.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsStacker", true, () => ImGui.Text(x.IsStacker.ToString())));
            }
            ImGuiEx.EzTable(Entries);

            ImGui.Text("HourGlasses");
            List<ImGuiEx.EzTableEntry> Entries2 = [];
            foreach(var x in _allHourGlasses)
            {
                Entries2.Add(new ImGuiEx.EzTableEntry("EntityId", true, () => ImGui.Text(x.EntityId.ToString())));
                Entries2.Add(new ImGuiEx.EzTableEntry("Wise", true, () => ImGui.Text(x.wise.ToString())));
                Entries2.Add(new ImGuiEx.EzTableEntry("ClockDirection", true, () => ImGui.Text(x.ClockDirection.ToString())));
            }
            ImGuiEx.EzTable(Entries2);

            ImGui.Text("CurrentHourGlasses");
            List<ImGuiEx.EzTableEntry> Entries3 = [];
            foreach(var x in _currentHourGlasses)
            {
                Entries3.Add(new ImGuiEx.EzTableEntry("EntityId", true, () => ImGui.Text(x.EntityId.ToString())));
                Entries3.Add(new ImGuiEx.EzTableEntry("Wise", true, () => ImGui.Text(x.wise.ToString())));
                Entries3.Add(new ImGuiEx.EzTableEntry("ClockDirection", true, () => ImGui.Text(x.ClockDirection.ToString())));
            }
            ImGuiEx.EzTable(Entries3);
        }
    }
    #endregion

    #region private methods
    /********************************************************************/
    /* private methods                                                  */
    /********************************************************************/
    private PartyData? GetMinedata() => _partyDataList.Find(x => x.Mine) ?? null;

    private void SetListEntityIdByJob()
    {
        _partyDataList.Clear();
        var tmpList = new List<PartyData>();

        foreach(var pc in FakeParty.Get())
        {
            tmpList.Add(new PartyData(pc.EntityId, Array.IndexOf(jobOrder, pc.GetJob())));
        }

        // Sort by job order
        tmpList.Sort((a, b) => a.Index.CompareTo(b.Index));
        foreach(var data in tmpList)
        {
            _partyDataList.Add(data);
        }

        // Set index
        for(var i = 0; i < _partyDataList.Count; i++)
        {
            _partyDataList[i].Index = i;
        }
    }

    private void EarlyFire() // 担当 3人 担当 1,6,10時
    {
        if(_clockDirectionCalculator == null) return;
        var pc = GetMinedata();
        if(pc == null) return;
        // 自分を含む同じ担当を抽出
        var sameRoleList = _partyDataList.Where(x => x.DebuffTime == 10);
        if(sameRoleList.Count() != 3) { ExceptionReturn("sameRoleList.Count() != 3"); return; }
        ;
        var index = 0;
        foreach(var x in sameRoleList)
        {
            if(x.EntityId == pc.EntityId) break;
            index++;
        }

        switch(_state)
        {
            case State.Fire1: // ファイガ捨て
                // ブリザガなら真ん中
                if(pc.IsBlizzard)
                {
                    ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0);
                    break;
                }

                if(index == 0) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 18);
                else if(index == 1) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(6), 18);
                else if(index == 2) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(10), 18);
                break;
            case State.Interval1:
            case State.Laser1: // リターン(エラプ)捨て
                var range = pc.IsStacker ? 2f : 9f;
                if(index == 0) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), range);
                else if(index == 1) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(6), range);
                else if(index == 2) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(10), range);
                break;

            case State.Fire2: // 中央で頭割り
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0);
                break;
            case State.Interval2:
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0);
                break;

            case State.Laser2: // ビーム誘導
                HourGlassData? myHourGlass = null;
                var myClock = index switch
                {
                    0 => 1,
                    1 => 6,
                    2 => 10,
                    _ => 0
                };
                myHourGlass = _currentHourGlasses.Find(x => x.ClockDirection == myClock);
                if(myHourGlass == null) { ExceptionReturn("myHourGlass is null"); return; }
                ;

                var angle = _clockDirectionCalculator.GetAngle(myClock);
                if(myHourGlass.wise == Wise.Clockwise) angle -= 12;
                else angle += 12;

                ApplyElement("Bait", angle, 10.2f);
                break;
            case State.Fire3: // 中央で頭割り
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0);
                break;
            case State.Interval3:
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0);
                break;
            case State.Laser3: // ニート、向きだけ注意
                ApplyElement("Bait", DirectionCalculator.GetOppositeDirection(_clockDirectionCalculator.GetDirectionFromClock(index switch
                {
                    0 => 1,
                    1 => 6,
                    2 => 10,
                    _ => 0
                })), 2);

                // リターン設置側に向く
                var clock = index switch
                {
                    0 => 1,
                    1 => 6,
                    2 => 10,
                    _ => 0
                };

                if(Controller.TryGetElementByName("ReturnPoint", out var el))
                {
                    var position = new Vector3(100, 0, 100);
                    var angle1 = DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(clock));
                    position += 10 * new Vector3(MathF.Cos(MathF.PI * angle1 / 180f), 0, MathF.Sin(MathF.PI * angle1 / 180f));
                    el.Enabled = true;
                    el.radius = 0.5f;
                    el.tether = false;
                    el.overlayVOffset = 4.0f;
                    el.overlayFScale = 3.0f;
                    el.overlayText = "リターン位置";
                    el.SetRefPosition(position);
                }
                break;
            case State.Stack:
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0);
                break;
        }
    }

    private void MiddleFire() // 担当 2人 担当 3,9時
    {
        if(_clockDirectionCalculator == null) return;
        var pc = GetMinedata();
        if(pc == null) return;
        switch(_state)
        {
            case State.Fire1: // 中央で頭割り
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0);
                break;
            case State.Interval1:
            case State.Laser1: // ウォタガなら3時、エラプなら9時でリターン捨て
                if(pc.IsDarkWater) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(3), 2f);
                else if(pc.IsEruption) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(9), 9f);
                break;

            case State.Fire2: // ウォタガなら3時、エラプなら9時でファイガ捨て
                if(pc.IsDarkWater) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(3), 18);
                else if(pc.IsEruption) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(9), 18);
                break;
            case State.Interval2:
            case State.Laser2: // 中央でニート
            case State.Fire3: // 中央で頭割り
            case State.Interval3:
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0);
                break;
            case State.Laser3: // ウォタガなら3時、エラプなら9時でビーム誘導
                if(pc.IsDarkWater)
                {
                    HourGlassData? myHourGlass = null;
                    myHourGlass = _currentHourGlasses.Find(x => x.ClockDirection == 3);
                    if(myHourGlass == null) { ExceptionReturn("myHourGlass is null"); return; }
                    ;

                    var angle = _clockDirectionCalculator.GetAngle(3);
                    if(myHourGlass.wise == Wise.Clockwise) angle -= 12;
                    else angle += 12;

                    ApplyElement("Bait", angle, 10.2f);
                }
                else if(pc.IsEruption)
                {
                    HourGlassData? myHourGlass = null;
                    myHourGlass = _currentHourGlasses.Find(x => x.ClockDirection == 9);
                    if(myHourGlass == null) { ExceptionReturn("myHourGlass is null"); return; }
                    ;

                    var angle = _clockDirectionCalculator.GetAngle(9);
                    if(myHourGlass.wise == Wise.Clockwise) angle -= 12;
                    else angle += 12;

                    ApplyElement("Bait", angle, 10.2f);
                }
                else { ExceptionReturn("pc.IsDarkWater and pc.IsEruption is false"); return; }

                // リターン設置側に向く
                var clock = pc.IsDarkWater ? 3 : 9;

                if(Controller.TryGetElementByName("ReturnPoint", out var el))
                {
                    var position = new Vector3(100, 0, 100);
                    var angle1 = DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(clock));
                    position += 20 * new Vector3(MathF.Cos(MathF.PI * angle1 / 180f), 0, MathF.Sin(MathF.PI * angle1 / 180f));
                    el.Enabled = true;
                    el.radius = 0.5f;
                    el.tether = false;
                    el.overlayVOffset = 4.0f;
                    el.overlayFScale = 3.0f;
                    el.overlayText = "リターン位置";
                    el.SetRefPosition(position);
                }
                break;
            case State.Stack:
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0);
                break;
        }
    }

    private void LateFire() // 担当 3人 担当 0,4,7時
    {
        if(_clockDirectionCalculator == null) return;
        var pc = GetMinedata();
        if(pc == null) return;
        // 自分を含む同じ担当を抽出
        var sameRoleList = _partyDataList.Where(x => x.DebuffTime == 30);
        if(sameRoleList.Count() != 3) { ExceptionReturn("sameRoleList.Count() != 3"); return; }
        ;
        var index = 0;
        foreach(var x in sameRoleList)
        {
            if(x.EntityId == pc.EntityId) break;
            index++;
        }

        switch(_state)
        {
            case State.Fire1: // 中央で頭割り
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0);
                break;
            case State.Interval1:
            case State.Laser1: // ビーム誘導
                HourGlassData? myHourGlass = null;
                var myClock = index switch
                {
                    0 => 0,
                    1 => 4,
                    2 => 7,
                    _ => 0
                };
                myHourGlass = _currentHourGlasses.Find(x => x.ClockDirection == myClock);
                if(myHourGlass == null) { ExceptionReturn("myHourGlass is null"); return; }
                ;

                var angle = _clockDirectionCalculator.GetAngle(myClock);
                if(myHourGlass.wise == Wise.Clockwise) angle -= 12;
                else angle += 12;

                ApplyElement("Bait", angle, 10.2f);
                break;
            case State.Fire2: // 中央で頭割り
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0);
                break;
            case State.Interval2:
            case State.Laser2: // リターン設置
                var range = pc.IsStacker ? 2f : 9.5f;
                if(index == 0) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(0), range);
                else if(index == 1) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(4), range);
                else if(index == 2) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(7), range);
                break;
            case State.Fire3: // ファイガ捨て
                // ブリザガなら真ん中
                if(pc.IsBlizzard)
                {
                    ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0);
                    break;
                }

                if(index == 0) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(0), 18);
                else if(index == 1) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(4), 18);
                else if(index == 2) ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(7), 18);
                break;
            case State.Interval3:
            case State.Laser3: // ニート(向きだけ注意)
                ApplyElement("Bait", DirectionCalculator.GetOppositeDirection(_clockDirectionCalculator.GetDirectionFromClock(index switch
                {
                    0 => 0,
                    1 => 4,
                    2 => 7,
                    _ => 0
                })), 2);

                // リターン設置側に向く
                if(index == 0) ApplyElement("BaitSight", _clockDirectionCalculator.GetDirectionFromClock(0), 10);
                else if(index == 1) ApplyElement("BaitSight", _clockDirectionCalculator.GetDirectionFromClock(4), 10);
                else if(index == 2) ApplyElement("BaitSight", _clockDirectionCalculator.GetDirectionFromClock(7), 10);
                var clock = index switch
                {
                    0 => 0,
                    1 => 4,
                    2 => 7,
                    _ => 0
                };

                if(Controller.TryGetElementByName("ReturnPoint", out var el))
                {
                    var position = new Vector3(100, 0, 100);
                    var angle1 = DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(clock));
                    position += 10 * new Vector3(MathF.Cos(MathF.PI * angle1 / 180f), 0, MathF.Sin(MathF.PI * angle1 / 180f));
                    el.Enabled = true;
                    el.radius = 0.5f;
                    el.tether = false;
                    el.overlayVOffset = 4.0f;
                    el.overlayFScale = 3.0f;
                    el.overlayText = "リターン位置";
                    el.SetRefPosition(position);
                }
                break;
            case State.Stack:
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0);
                break;
        }
    }

    private void ShowSinboundMeltdown()
    {
        var hourGlassesList = Svc.Objects.Where(x => x is IBattleNpc npc && npc.CastActionId == 40291 && npc.IsCasting);
        if(hourGlassesList.Count() == 0) return;

        var pcs = FakeParty.Get().ToList();
        if(pcs.Count != 8) return;

        var i = 0;
        foreach(var hourglass in hourGlassesList)
        {
            // Search for the closest player
            var closestPlayer = pcs.MinBy(x => Vector3.Distance(x.Position, hourglass.Position));
            if(closestPlayer == null) return;

            // Show Element
            if(Controller.TryGetElementByName($"SinboundMeltdown{i}", out var element))
            {
                var extPos = GetExtendedAndClampedPosition(hourglass.Position, closestPlayer.Position, 25f, 30f);
                element.SetRefPosition(hourglass.Position);
                element.SetOffPosition(extPos);
                element.Enabled = true;
                i++;
            }
        }
    }

    private void HideSinboundMeltdown()
    {
        for(var i = 0; i < 3; ++i)
            if(Controller.TryGetElementByName($"SinboundMeltdown{i}", out var element))
                element.Enabled = false;
    }

    private void ShowStackRange()
    {
        // だれかのホーリガが5秒以下
        if(FakeParty.Get().ToList().Any(x => x.StatusList.Any(y => y.StatusId == (uint)Debuff.Holy && y.RemainingTime <= 5)))
        {
            // そのだれかを探す
            var holyPlayer = FakeParty.Get().ToList().Find(x => x.StatusList.Any(y => y.StatusId == (uint)Debuff.Holy && y.RemainingTime <= 5));
            if(holyPlayer == null) return;

            if(Controller.TryGetElementByName("CircleObject", out var el))
            {
                el.refActorObjectID = holyPlayer.EntityId;
                el.radius = 6f;
                el.thicc = 6f;
                el.color = 0xC800FF00;
                el.Filled = false;
                el.Enabled = true;
            }
        }
        else
        {
            if(Controller.TryGetElementByName("CircleObject", out var el))
            {
                el.Enabled = false;
            }
        }
    }

    #endregion

    #region API
    /********************************************************************/
    /* API                                                              */
    /********************************************************************/
    private static readonly Job[] jobOrder =
    {
        Job.DRK,
        Job.WAR,
        Job.GNB,
        Job.PLD,
        Job.WHM,
        Job.AST,
        Job.SCH,
        Job.SGE,
        Job.DRG,
        Job.VPR,
        Job.SAM,
        Job.MNK,
        Job.RPR,
        Job.NIN,
        Job.BRD,
        Job.MCH,
        Job.DNC,
        Job.RDM,
        Job.SMN,
        Job.PCT,
        Job.BLM,
    };

    private static readonly Job[] TankJobs = { Job.DRK, Job.WAR, Job.GNB, Job.PLD };
    private static readonly Job[] HealerJobs = { Job.WHM, Job.AST, Job.SCH, Job.SGE };
    private static readonly Job[] MeleeDpsJobs = { Job.DRG, Job.VPR, Job.SAM, Job.MNK, Job.RPR, Job.NIN };
    private static readonly Job[] RangedDpsJobs = { Job.BRD, Job.MCH, Job.DNC };
    private static readonly Job[] MagicDpsJobs = { Job.RDM, Job.SMN, Job.PCT, Job.BLM };
    private static readonly Job[] DpsJobs = MeleeDpsJobs.Concat(RangedDpsJobs).Concat(MagicDpsJobs).ToArray();
    private enum Role
    {
        Tank,
        Healer,
        MeleeDps,
        RangedDps,
        MagicDps
    }

    public class DirectionCalculator
    {
        public enum Direction : int
        {
            None = -1,
            East = 0,
            SouthEast = 1,
            South = 2,
            SouthWest = 3,
            West = 4,
            NorthWest = 5,
            North = 6,
            NorthEast = 7,
        }

        public enum LR : int
        {
            Left = -1,
            SameOrOpposite = 0,
            Right = 1
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

        public static int Round45(int value) => (int)(MathF.Round((float)value / 45) * 45);
        public static Direction GetOppositeDirection(Direction direction) => GetDirectionFromAngle(direction, 180);

        public static Direction DividePoint(Vector3 Position, float Distance, Vector3? Center = null)
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

        public static Direction GetDirectionFromAngle(Direction direction, int angle)
        {
            if(direction == Direction.None) return Direction.None; // 無効な方向の場合

            // 方向数（8方向: North ~ NorthWest）
            const int directionCount = 8;

            // 角度を45度単位に丸め、-180～180の範囲に正規化
            angle = ((Round45(angle) % 360) + 360) % 360; // 正の値に変換して360で正規化
            if(angle > 180) angle -= 360;

            // 現在の方向のインデックス
            var currentIndex = (int)direction;

            // 45度ごとのステップ計算と新しい方向の計算
            var step = angle / 45;
            var newIndex = (currentIndex + step + directionCount) % directionCount;

            return (Direction)newIndex;
        }

        public static LR GetTwoPointLeftRight(Direction direction1, Direction direction2)
        {
            // 不正な方向の場合（None）
            if(direction1 == Direction.None || direction2 == Direction.None)
                return LR.SameOrOpposite;

            // 方向数（8つ: North ~ NorthWest）
            var directionCount = 8;

            // 差分を循環的に計算
            var difference = ((int)direction2 - (int)direction1 + directionCount) % directionCount;

            // LRを直接返す
            return difference == 0 || difference == directionCount / 2
                ? LR.SameOrOpposite
                : (difference < directionCount / 2 ? LR.Right : LR.Left);
        }

        public static int GetTwoPointAngle(Direction direction1, Direction direction2)
        {
            // 不正な方向を考慮
            if(direction1 == Direction.None || direction2 == Direction.None)
                return 0;

            // enum の値を数値として扱い、環状の差分を計算
            var diff = ((int)direction2 - (int)direction1 + 8) % 8;

            // 差分から角度を計算
            return diff <= 4 ? diff * 45 : (diff - 8) * 45;
        }

        public static float GetAngle(Direction direction)
        {
            if(direction == Direction.None) return 0; // 無効な方向の場合

            // 45度単位で計算し、0度から始まる時計回りの角度を返す
            return (int)direction * 45 % 360;
        }

        private static List<DirectionalVector> GenerateDirectionalVectors(float distance, Vector3? center = null)
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
    }

    public class ClockDirectionCalculator
    {
        private DirectionCalculator.Direction _12ClockDirection = DirectionCalculator.Direction.None;
        public bool isValid => _12ClockDirection != DirectionCalculator.Direction.None;
        public DirectionCalculator.Direction Get12ClockDirection() => _12ClockDirection;

        public ClockDirectionCalculator(DirectionCalculator.Direction direction)
        {
            _12ClockDirection = direction;
        }

        // _12ClockDirectionを0時方向として、指定時計からの方向を取得
        public DirectionCalculator.Direction GetDirectionFromClock(int clock)
        {
            if(!isValid)
                return DirectionCalculator.Direction.None;

            // 特別ケース: clock = 0 の場合、_12ClockDirection をそのまま返す
            if(clock == 0)
                return _12ClockDirection;

            // 12時計位置を8方向にマッピング
            var clockToDirectionMapping = new Dictionary<int, int>
        {
            { 0, 0 },   // Same as _12ClockDirection
            { 1, 1 }, { 2, 1 },   // Diagonal right up
            { 3, 2 },             // Right
            { 4, 3 }, { 5, 3 },   // Diagonal right down
            { 6, 4 },             // Opposite
            { 7, -3 }, { 8, -3 }, // Diagonal left down
            { 9, -2 },            // Left
            { 10, -1 }, { 11, -1 } // Diagonal left up
        };

            // 現在の12時方向をインデックスとして取得
            var baseIndex = (int)_12ClockDirection;

            // 時計位置に基づくステップを取得
            var step = clockToDirectionMapping[clock];

            // 新しい方向を計算し、範囲を正規化
            var targetIndex = (baseIndex + step + 8) % 8;

            // 対応する方向を返す
            return (DirectionCalculator.Direction)targetIndex;
        }

        public int GetClockFromDirection(DirectionCalculator.Direction direction)
        {
            if(!isValid)
                throw new InvalidOperationException("Invalid state: _12ClockDirection is not set.");

            if(direction == DirectionCalculator.Direction.None)
                throw new ArgumentException("Direction cannot be None.", nameof(direction));

            // 各方向に対応する最小の clock 値を定義
            var directionToClockMapping = new Dictionary<int, int>
            {
                { 0, 0 },   // Same as _12ClockDirection
                { 1, 1 },   // Diagonal right up (SouthEast)
                { 2, 3 },   // Right (South)
                { 3, 4 },   // Diagonal right down (SouthWest)
                { 4, 6 },   // Opposite (West)
                { 5, 7 },   // Diagonal left down (NorthWest)
                { 6, 9 },   // Left (North)
                { 7, 10 }   // Diagonal left up (NorthEast)
            };

            // 現在の12時方向をインデックスとして取得
            var baseIndex = (int)_12ClockDirection;

            // 指定された方向のインデックス
            var targetIndex = (int)direction;

            // 差分を計算し、時計方向に正規化
            var step = (targetIndex - baseIndex + 8) % 8;

            // 該当する clock を取得
            return directionToClockMapping[step];
        }

        public float GetAngle(int clock) => DirectionCalculator.GetAngle(GetDirectionFromClock(clock));
    }

    private void HideAllElements() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    private void ApplyElement(string elementName, DirectionCalculator.Direction direction, float radius = 0f, float elementRadius = 0.3f, bool tether = true)
    {
        var position = new Vector3(100, 0, 100);
        var angle = DirectionCalculator.GetAngle(direction);
        position += radius * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));
        if(Controller.TryGetElementByName(elementName, out var element))
        {
            element.Enabled = true;
            element.radius = elementRadius;
            element.tether = tether;
            element.SetRefPosition(position);
        }
    }

    private void ApplyElement(string elementName, float angle, float radius = 0f, float elementRadius = 0.3f, bool tether = true)
    {
        var position = new Vector3(100, 0, 100);
        position += radius * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));
        if(Controller.TryGetElementByName(elementName, out var element))
        {
            element.Enabled = true;
            element.radius = elementRadius;
            element.tether = tether;
            element.SetRefPosition(position);
        }
    }

    private static float GetCorrectionAngle(Vector3 origin, Vector3 target, float rotation) => GetCorrectionAngle(MathHelper.ToVector2(origin), MathHelper.ToVector2(target), rotation);

    private static float GetCorrectionAngle(Vector2 origin, Vector2 target, float rotation)
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

    private static float ConvertRotationRadiansToDegrees(float radians)
    {
        // Convert radians to degrees with coordinate system adjustment
        var degrees = ((-radians * (180 / MathF.PI)) + 180) % 360;

        // Ensure the result is within the 0° to 360° range
        return degrees < 0 ? degrees + 360 : degrees;
    }

    private static float ConvertDegreesToRotationRadians(float degrees)
    {
        // Convert degrees to radians with coordinate system adjustment
        var radians = -(degrees - 180) * (MathF.PI / 180);

        // Normalize the result to the range -π to π
        radians = ((radians + MathF.PI) % (2 * MathF.PI)) - MathF.PI;

        return radians;
    }

    public static Vector3 GetExtendedAndClampedPosition(Vector3 center, Vector3 currentPos, float extensionLength, float? limit)
    {
        // Calculate the normalized direction vector from the center to the current position
        var direction = Vector3.Normalize(currentPos - center);

        // Extend the position by the specified length
        var extendedPos = currentPos + (direction * extensionLength);

        // If limit is null, return the extended position without clamping
        if(!limit.HasValue)
        {
            return extendedPos;
        }

        // Calculate the distance from the center to the extended position
        var distanceFromCenter = Vector3.Distance(center, extendedPos);

        // If the extended position exceeds the limit, clamp it within the limit
        if(distanceFromCenter > limit.Value)
        {
            return center + (direction * limit.Value);
        }

        // If within the limit, return the extended position as is
        return extendedPos;
    }

    public static void ExceptionReturn(string message)
    {
        PluginLog.Error(message);
    }
    #endregion
}
