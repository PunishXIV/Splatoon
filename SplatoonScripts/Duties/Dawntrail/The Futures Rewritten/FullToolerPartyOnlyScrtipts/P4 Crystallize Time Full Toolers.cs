using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Automation;
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
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten.FullToolerPartyOnlyScrtipts;
internal unsafe class P4_Crystallize_Time_Full_Toolers :SplatoonScript
{
    #region types
    /********************************************************************/
    /* types                                                            */
    /********************************************************************/
    private enum StateCommon
    {
        None = 0,
        GetBuffs,
        HourGlassWater,
        IceElapAelo,
        holy,
        SetReturn,
        Split,
    }

    private enum StateAeloFirst
    {
        None = 0,
        HourGlassWater,
        IceElapAelo,
        GetDragon,
        AvoidNorth,
        SetReturn,
        Split
    }

    private enum StateAeloSecond
    {
        None = 0,
        HourGlassWater,
        IceElapAelo,
        Wait,
        GetDragon,
        SetReturn,
        Split
    }

    private enum StateRedIce3
    {
        None = 0,
        HourGlassWater,
        IceElapAelo,
        holy,
        SetReturn,
        Split
    }

    private enum StateBlueFirst
    {
        None = 0,
        HourGlassWater,
        IceElapAelo,
        holy,
        WaitNorth,
        GetRemoveBuff,
        SetReturn,
        Split
    }

    private enum StateBlueSecond
    {
        None = 0,
        HourGlassWater,
        IceElapAelo,
        holy,
        WaitNorth,
        SetReturn,
        GetRemoveBuff,
    }

    private enum WaveState
    {
        None = 0,
        Wave1,
        Wave2,
        Wave3,
        Wave4,
        Wave5,
        Wave6,
        Wave7,
    }

    private enum Gimmick
    {
        None = 0,
        Ice3,
        Water3,
        Elap,
        Aelo,
        Holy,
    }

    private delegate void MineRoleAction();
    #endregion

    #region class
    /********************************************************************/
    /* class                                                            */
    /********************************************************************/
    public class Config :IEzConfig
    {
        public bool ArmsLength = true;
        public bool FastRunRedIce3 = true;
        public float FastCheatDefault = 1.0f;
        public float FastCheat = 1.5f;
        public bool IsMaster = false;
    }

    private class RemoveBuff
    {
        public Vector3 Position = Vector3.Zero;
        public uint AssignEntityId = 0;
    }

    private class PartyData
    {
        public int Index = 0;
        public bool Mine = false;
        public uint EntityId;
        public IPlayerCharacter? Object => (IPlayerCharacter)this.EntityId.GetObject()! ?? null;
        public string CrowColor = "";
        public Gimmick Gimmick = Gimmick.None;
        public string AeloLR = "";
        public string Ice3LR = "";
        public int AttackIndex = 0;

        public bool IsTank => TankJobs.Contains(Object?.GetJob() ?? Job.WHM);
        public bool IsHealer => HealerJobs.Contains(Object?.GetJob() ?? Job.PLD);
        public bool IsTH => IsTank || IsHealer;
        public bool IsMeleeDps => MeleeDpsJobs.Contains(Object?.GetJob() ?? Job.MCH);
        public bool IsRangedDps => RangedDpsJobs.Contains(Object?.GetJob() ?? Job.MNK);
        public bool IsDps => IsMeleeDps || IsRangedDps;

        public PartyData(uint entityId, int index)
        {
            EntityId = entityId;
            this.Mine = this.EntityId == Player.Object.EntityId;
            Index = index;
        }
    }
    #endregion

    #region const
    /********************************************************************/
    /* const                                                            */
    /********************************************************************/
    #endregion

    #region public properties
    /********************************************************************/
    /* public properties                                                */
    /********************************************************************/
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(20, "redmoon");
    #endregion

    #region private properties
    /********************************************************************/
    /* private properties                                               */
    /********************************************************************/
    private StateCommon _state = StateCommon.None;
    private StateAeloFirst _aeloFirst = StateAeloFirst.None;
    private StateAeloSecond _aeloSecond = StateAeloSecond.None;
    private StateRedIce3 _redIce3 = StateRedIce3.None;
    private StateBlueFirst _blueFirst = StateBlueFirst.None;
    private StateBlueSecond _blueSecond = StateBlueSecond.None;
    private Config C => Controller.GetConfig<Config>();
    private List<PartyData> _partyDataList = new();
    private MineRoleAction? _mineRoleAction = null;
    private MineRoleAction? _mineBlueRoleAction = null;
    private int _removeReturnCount = 0;
    private DirectionCalculator.Direction _slowHourGlassDirection = DirectionCalculator.Direction.None;
    private WaveState _waveState = WaveState.None;
    private DirectionCalculator.Direction _firstKnockback = DirectionCalculator.Direction.None;
    private DirectionCalculator.Direction _secondKnockback = DirectionCalculator.Direction.None;
    private bool _StateProcEnd = false;
    private bool _StateProcEndCommon = false;
    private List<RemoveBuff> _removeBuffPosList = new();
    private ActionManager* actionManager = ActionManager.Instance();
    private bool _usedSprint = false;
    private bool _usedArmsLength = false;
    private bool _BeforeReturnProcDone = false;
    private int _maelstromCount = 0;
    private long _runEndTime = 0;
    private bool _stackBlizzard = false;
    private Vector3 _lastVnavPos = Vector3.Zero;
    #endregion

    #region public methods
    /********************************************************************/
    /* public methods                                                   */
    /********************************************************************/
    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0) { tether = true, radius = 3f, thicc = 6f });

        Controller.RegisterElement("BaitD1", new Element(0) { tether = false, radius = 0.3f, thicc = 6f, color = 0xC8FF0000 });
        Controller.RegisterElement("BaitD2", new Element(0) { tether = false, radius = 0.3f, thicc = 6f, color = 0xC8FF0000 });
        Controller.RegisterElement("BaitD3", new Element(0) { tether = false, radius = 0.3f, thicc = 6f, color = 0xC800FF00 });
        Controller.RegisterElement("BaitD4", new Element(0) { tether = false, radius = 0.3f, thicc = 6f, color = 0xC800FF00 });

        Controller.RegisterElement("BaitObject", new Element(1)
        {
            tether = true,
            refActorComparisonType = 2,
            radius = 0.5f,
            thicc = 6f
        });

        for (var i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"CircleFixed{i}", new Element(0) { radius = 5.0f, thicc = 2f, fillIntensity = 0.5f });
        }

        for (var i = 0; i < 4; i++)
        {
            Controller.RegisterElement($"CircleFixed1{i}", new Element(0) { radius = 5.0f, thicc = 2f, fillIntensity = 0.5f });
        }

        Controller.RegisterElement($"Line", new Element(2) { radius = 0f, thicc = 6f, fillIntensity = 0.5f });

        Controller.RegisterElement($"Wave1", new Element(2) { radius = 0f, thicc = 6f, fillIntensity = 0.5f });
        Controller.RegisterElement($"Wave2", new Element(2) { radius = 0f, thicc = 6f, fillIntensity = 0.5f });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 40298)
        {
            this.OnReset();
            SetListEntityIdByJob();
            //// DEBUG
            //_partyDataList.Each(x => x.Mine = false);
            //_partyDataList[1].Mine = true;
            SetState(StateCommon.GetBuffs);
        }

        if (_state == StateCommon.None) return;

        if (castId == 40251 && _waveState == WaveState.None)
        {
            if (source.TryGetObject(out var obj))
            {
                _firstKnockback = DirectionCalculator.DividePoint(obj.Position, 20.0f);
            }
        }

        if (castId == 40251 && _waveState != WaveState.None)
        {
            if (source.TryGetObject(out var obj))
            {
                _secondKnockback = DirectionCalculator.DividePoint(obj.Position, 20.0f);
            }

            if (_secondKnockback != DirectionCalculator.Direction.None && _redIce3 == StateRedIce3.holy)
            {
                SetState(StateRedIce3.SetReturn);
            }

            if (_secondKnockback != DirectionCalculator.Direction.None && _blueSecond == StateBlueSecond.WaitNorth)
            {
                SetState(StateBlueSecond.SetReturn);
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_state == StateCommon.None) return;
        if (set.Action == null) return;
        var castId = set.Action.Value.RowId;

        if (castId == 40251 && _waveState == WaveState.None)
        {
            if (set.SourceCharacter != null)
            {
                _firstKnockback = DirectionCalculator.DividePoint(set.SourceCharacter.Value.Position, 20.0f);
            }
            _waveState = WaveState.Wave1;
        }

        if (castId == 40251 && _waveState == WaveState.Wave3)
        {
            if (set.SourceCharacter != null)
            {
                _secondKnockback = DirectionCalculator.DividePoint(set.SourceCharacter.Value.Position, 20.0f);
            }
            _waveState = WaveState.Wave4;
        }

        if (castId == 40253 && _waveState != WaveState.Wave3 && _waveState != WaveState.None)
        {
            _waveState++;
        }

        if (castId == 40241 && set.SourceCharacter != null)
        {
            RemoveBuff removeBuff = new();
            removeBuff.Position = set.SourceCharacter.Value.Position;
            _removeBuffPosList.Add(removeBuff);
        }

        if (castId == 40299)
        {
            _maelstromCount++;

            if (_maelstromCount is 1 or 3 or 5)
            {
                HideAllElements();
                if (_state == StateCommon.SetReturn)
                {
                    SetState(StateCommon.Split);
                }

                if (_maelstromCount >= 5 && _blueFirst == StateBlueFirst.WaitNorth)
                {
                    SetState(StateBlueFirst.GetRemoveBuff);
                }
            }
        }

        if (castId == 40332)
        {
            this.OnReset();
        }

        if (castId == 40299 && _state == StateCommon.HourGlassWater)
        {
            SetState(StateCommon.IceElapAelo);
        }

        if (castId == 40274 && _state == StateCommon.IceElapAelo)
        {
            SetState(StateCommon.holy);
        }

        if (castId == 40277 && _state == StateCommon.holy)
        {
            SetState(StateCommon.SetReturn);
        }

        if (_aeloFirst != StateAeloFirst.None)
        {
            if (castId == 40299 && _aeloFirst == StateAeloFirst.HourGlassWater)
            {
                SetState(StateAeloFirst.IceElapAelo);
            }

            if (castId == 40274 && _aeloFirst == StateAeloFirst.IceElapAelo)
            {
                _ = new TickScheduler(() => SetState(StateAeloFirst.GetDragon), 1000);
            }
        }
        else if (_aeloSecond != StateAeloSecond.None)
        {
            if (castId == 40299 && _aeloSecond == StateAeloSecond.HourGlassWater)
            {
                _ = new TickScheduler(() => SetState(StateAeloSecond.IceElapAelo), 1000);
            }

            if (castId == 40274 && _aeloSecond == StateAeloSecond.IceElapAelo)
            {
                SetState(StateAeloSecond.Wait);
            }

            if (_maelstromCount == 3 && _aeloSecond == StateAeloSecond.Wait)
            {
                SetState(StateAeloSecond.GetDragon);
            }
        }
        else if (_redIce3 != StateRedIce3.None)
        {
            if (castId == 40299 && _redIce3 == StateRedIce3.HourGlassWater)
            {
                SetState(StateRedIce3.IceElapAelo);
            }

            if (castId == 40274 && _redIce3 == StateRedIce3.IceElapAelo)
            {
                SetState(StateRedIce3.holy);
            }
        }
        else if (_blueFirst != StateBlueFirst.None)
        {
            if (castId == 40299 && _blueFirst == StateBlueFirst.HourGlassWater)
            {
                SetState(StateBlueFirst.IceElapAelo);
            }

            if (castId == 40274 && _blueFirst == StateBlueFirst.IceElapAelo)
            {
                SetState(StateBlueFirst.holy);
            }

            if (castId == 40277 && _blueFirst == StateBlueFirst.holy)
            {
                SetState(StateBlueFirst.WaitNorth);
            }
        }
        else if (_blueSecond != StateBlueSecond.None)
        {
            if (castId == 40299 && _blueSecond == StateBlueSecond.HourGlassWater)
            {
                SetState(StateBlueSecond.IceElapAelo);
            }

            if (castId == 40274 && _blueSecond == StateBlueSecond.IceElapAelo)
            {
                SetState(StateBlueSecond.holy);
            }

            if (castId == 40277 && _blueSecond == StateBlueSecond.holy)
            {
                SetState(StateBlueSecond.WaitNorth);
            }
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_state == StateCommon.None) return;
        if (_slowHourGlassDirection == DirectionCalculator.Direction.None &&
            data2 == 0 && data3 == 133 && data5 == 15 && source.TryGetObject(out var obj))
        {
            DirectionCalculator.Direction direction = DirectionCalculator.DividePoint(obj.Position, 10.0f);

            direction = direction switch
            {
                DirectionCalculator.Direction.SouthEast => DirectionCalculator.Direction.NorthWest,
                DirectionCalculator.Direction.SouthWest => DirectionCalculator.Direction.NorthEast,
                _ => direction
            };

            _slowHourGlassDirection = direction;
        }

        // 全ての情報が揃ったら、次のステップへ
        if (_partyDataList.All(x => x.Gimmick != Gimmick.None && x.CrowColor != "")
            && _mineRoleAction == null
            && _slowHourGlassDirection != DirectionCalculator.Direction.None)
        {
            var mine = GetMinedata();
            if (mine == null) return;

            // エアロガ調整 indexが若い人が左
            var aelos = _partyDataList.Where(x => x.Gimmick == Gimmick.Aelo).ToList();
            if (aelos.Count != 2) return;

            if (aelos[0].Index > aelos[1].Index)
            {
                aelos[0].AeloLR = "R";
                aelos[1].AeloLR = "L";
            }
            else
            {
                aelos[0].AeloLR = "L";
                aelos[1].AeloLR = "R";
            }

            // 赤ブリザガ調整 indexが若い人が左
            var ices = _partyDataList.Where(x => x.Gimmick == Gimmick.Ice3 && x.CrowColor == "red").ToList();
            if (ices.Count != 2) return;

            if (ices[0].Index > ices[1].Index)
            {
                ices[0].Ice3LR = "R";
                ices[1].Ice3LR = "L";
            }
            else
            {
                ices[0].Ice3LR = "L";
                ices[1].Ice3LR = "R";
            }

            var i = 1;
            foreach (var x in _partyDataList.Where(x => x.CrowColor == "blue"))
            {
                x.AttackIndex = i;
                i++;
            }

            if (C.IsMaster)
            {
                // マーカー付与
                foreach (var x in _partyDataList)
                {
                    int tag = GetPlayerTag(x.EntityId);
                    if (tag == -1) continue;
                    if (x.Gimmick == Gimmick.Ice3 && x.CrowColor == "red")
                    {
                        if (Svc.Condition[ConditionFlag.DutyRecorderPlayback]) DuoLog.Information($"/mk bind <{tag}>");
                        else Chat.Instance.ExecuteCommand($"/mk bind <{tag}>");
                    }
                    else if (x.Gimmick == Gimmick.Aelo)
                    {
                        if (Svc.Condition[ConditionFlag.DutyRecorderPlayback]) DuoLog.Information($"/mk stop <{tag}>");
                        else Chat.Instance.ExecuteCommand($"/mk stop <{tag}>");
                    }
                    else
                    {
                        if (Svc.Condition[ConditionFlag.DutyRecorderPlayback]) DuoLog.Information($"/mk attack <{tag}>");
                        else Chat.Instance.ExecuteCommand($"/mk attack <{tag}>");
                    }
                }
            }

            // 自分のアクションメソッドを設定
            // 赤+エアロガ
            if (mine.Gimmick == Gimmick.Aelo && mine.CrowColor == "red")
            {
                if ((_slowHourGlassDirection == DirectionCalculator.Direction.NorthWest && mine.AeloLR == "R") ||
                    (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast && mine.AeloLR == "L"))
                {
                    _mineRoleAction = RedAeloFirst; // 1人
                    SetState(StateAeloFirst.HourGlassWater);
                }
                else
                {
                    _mineRoleAction = RedAeloSecond; // 1人
                    SetState(StateAeloSecond.HourGlassWater);
                }
            }
            // 赤+ブリザガ
            else if (mine.Gimmick == Gimmick.Ice3 && mine.CrowColor == "red")
            {
                _mineRoleAction = RedIce3; // 2人
                SetState(StateRedIce3.HourGlassWater);
            }
            // 青
            else if (mine.CrowColor == "blue")
            {
                _mineRoleAction = BlueBefore; // 3人 (Water3, Holy, Ice3)
                if (mine.AttackIndex <= 2)
                {
                    SetState(StateBlueFirst.HourGlassWater);
                    _mineBlueRoleAction = BlueAfterFirst;
                }
                else
                {
                    SetState(StateBlueSecond.HourGlassWater);
                    _mineBlueRoleAction = BlueAfterSecond;
                }
            }

            Chat.Instance.ExecuteCommand($"/pdrspeed {C.FastCheat}");

            SetState(StateCommon.HourGlassWater);
        }
    }

    public override void OnGainBuffEffect(uint sourceId, FFXIVClientStructs.FFXIV.Client.Game.Status Status)
    {
        if (_state == StateCommon.None) return;
        var statusId = Status.StatusId;

        PartyData? pc = null;

        switch (statusId)
        {
            case 2454:
                pc = _partyDataList.Find(x => x.EntityId == sourceId);
                if (pc != null) pc.Gimmick = Gimmick.Holy;
                break;

            case 2460:
                pc = _partyDataList.Find(x => x.EntityId == sourceId);
                if (pc != null) pc.Gimmick = Gimmick.Elap;
                break;

            case 2461:
                pc = _partyDataList.Find(x => x.EntityId == sourceId);
                if (pc != null) pc.Gimmick = Gimmick.Water3;
                break;

            case 2462:
                pc = _partyDataList.Find(x => x.EntityId == sourceId);
                if (pc != null) pc.Gimmick = Gimmick.Ice3;
                break;

            case 2463:
                pc = _partyDataList.Find(x => x.EntityId == sourceId);
                if (pc != null) pc.Gimmick = Gimmick.Aelo;
                break;

            case 3263: // 聖竜の爪 red
                pc = _partyDataList.Find(x => x.EntityId == sourceId);
                if (pc != null) pc.CrowColor = "red";
                break;

            case 3264: // 聖竜の牙 blue
                pc = _partyDataList.Find(x => x.EntityId == sourceId);
                if (pc != null) pc.CrowColor = "blue";
                break;

            default:
                // NOP
                break;
        }
    }

    public override void OnRemoveBuffEffect(uint sourceId, FFXIVClientStructs.FFXIV.Client.Game.Status Status)
    {
        if (_state == StateCommon.None) return;
        var statusId = Status.StatusId;

        if (statusId == 4208)
        {
            _removeReturnCount++;
            SetState(StateCommon.Split);
            if (_removeReturnCount == 8)
            {
                if (_blueFirst == StateBlueFirst.SetReturn)
                {
                    SetState(StateBlueFirst.Split);
                }
                else if (_blueSecond == StateBlueSecond.SetReturn)
                {
                    SetState(StateBlueSecond.GetRemoveBuff);
                }
                else if (_redIce3 == StateRedIce3.SetReturn)
                {
                    SetState(StateRedIce3.Split);
                }
                else if (_aeloFirst == StateAeloFirst.SetReturn)
                {
                    SetState(StateAeloFirst.Split);
                }
                else if (_aeloSecond == StateAeloSecond.SetReturn)
                {
                    SetState(StateAeloSecond.Split);
                }
            }
        }

        if (statusId == 3263) // 聖竜の爪 red
        {
            var pc = _partyDataList.Find(x => x.EntityId == sourceId);
            if (pc != null)
            {
                pc.CrowColor = "";
            }

            var mine = GetMinedata();
            if (mine == null) return;

            if (mine.Gimmick != Gimmick.Aelo) return;

            var anotherAelo = _partyDataList.Find(x => x.Gimmick == Gimmick.Aelo && x.EntityId != mine.EntityId);
            if (anotherAelo == null) return;

            if (mine.EntityId == sourceId && _aeloFirst == StateAeloFirst.GetDragon)
            {
                SetState(StateAeloFirst.AvoidNorth);
            }
            else if (mine.EntityId == sourceId && _aeloSecond == StateAeloSecond.GetDragon)
            {
                SetState(StateAeloSecond.SetReturn);
            }

            if (!_partyDataList.Any(x => x.CrowColor == "red"))
            {
                if (_aeloFirst != StateAeloFirst.None) SetState(StateAeloFirst.SetReturn);
                else SetState(StateAeloSecond.SetReturn);
            }
        }

        if (statusId == 3264) // 聖竜の牙 blue
        {
            var pc = _partyDataList.Find(x => x.EntityId == sourceId);
            if (pc != null)
            {
                pc.CrowColor = "";
            }

            var mine = GetMinedata();
            if (mine == null) return;
            if (mine.EntityId != sourceId) return;

            if (_blueFirst == StateBlueFirst.GetRemoveBuff)
            {
                SetState(StateBlueFirst.SetReturn);
            }
        }
    }

    public override void OnUpdate()
    {
        if (_state == StateCommon.None) return;

        if (_mineRoleAction != null && _slowHourGlassDirection != DirectionCalculator.Direction.None) _mineRoleAction();

        CommonUpdate();

        if (Controller.TryGetElementByName("Bait", out var el))
        {
            if (el.Enabled) el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }

        if (Controller.TryGetElementByName("BaitObject", out el))
        {
            if (el.Enabled) el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }

        UseArmsLength();

        if (_runEndTime != 0 && _stackBlizzard)
        {
            var pc = GetMinedata();
            if (pc == null || pc.Object == null) return;
            bool redIsNone = pc.Object.StatusList.All(x => x.StatusId != 3263);

            if (redIsNone)
            {
                if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                {
                    if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                        DuoLog.Information($"/vnav moveto 112 0 85");
                    else
                        DuoLog.Information($"/vnav moveto 88 0 85");
                }
                else
                {
                    if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                        Chat.Instance.ExecuteCommand($"/vnav moveto 112 0 85");
                    else
                        Chat.Instance.ExecuteCommand($"/vnav moveto 88 0 85");
                }

                if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                    ApplyElement("Bait", new Vector3(112f, 0, 85f));
                else
                    ApplyElement("Bait", new Vector3(88f, 0, 85f));

                _stackBlizzard = false;
            }
        }

        if (_runEndTime != 0 && _runEndTime < Environment.TickCount64)
        {
            if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
            {
                DuoLog.Information("/vnav stop");
            }
            else
            {
                Chat.Instance.ExecuteCommand("/vnav stop");
            }
            _runEndTime = 0;
        }
    }

    public override void OnReset()
    {
        _state = StateCommon.None;
        _aeloFirst = StateAeloFirst.None;
        _aeloSecond = StateAeloSecond.None;
        _redIce3 = StateRedIce3.None;
        _blueFirst = StateBlueFirst.None;
        _blueSecond = StateBlueSecond.None;
        _removeReturnCount = 0;
        _mineRoleAction = null;
        _partyDataList.Clear();
        _removeBuffPosList.Clear();
        _slowHourGlassDirection = DirectionCalculator.Direction.None;
        _waveState = WaveState.None;
        _firstKnockback = DirectionCalculator.Direction.None;
        _secondKnockback = DirectionCalculator.Direction.None;
        _StateProcEnd = false;
        _StateProcEndCommon = false;
        _usedSprint = false;
        _usedArmsLength = false;
        _BeforeReturnProcDone = false;
        _maelstromCount = 0;
        _runEndTime = 0;
        _stackBlizzard = false;
        _lastVnavPos = Vector3.Zero;
        Chat.Instance.ExecuteCommand($"/pdrspeed {C.FastCheatDefault}");
        HideAllElements();

        var c = Controller.GetConfig<Config>();
        if (!c.IsMaster) return;
        Chat.Instance.ExecuteCommand($"/mk off <attack>");
        Chat.Instance.ExecuteCommand($"/mk off <attack>");
        Chat.Instance.ExecuteCommand($"/mk off <attack>");
        Chat.Instance.ExecuteCommand($"/mk off <attack>");
        Chat.Instance.ExecuteCommand($"/mk off <attack>");
        Chat.Instance.ExecuteCommand($"/mk off <attack>");
        Chat.Instance.ExecuteCommand($"/mk off <attack>");
        Chat.Instance.ExecuteCommand($"/mk off <attack>");
        Chat.Instance.ExecuteCommand($"/mk off <bind>");
        Chat.Instance.ExecuteCommand($"/mk off <bind>");
        Chat.Instance.ExecuteCommand($"/mk off <bind>");
        Chat.Instance.ExecuteCommand($"/mk off <stop>");
        Chat.Instance.ExecuteCommand($"/mk off <stop>");
        Chat.Instance.ExecuteCommand($"/mk off <square>");
        Chat.Instance.ExecuteCommand($"/mk off <circle>");
        Chat.Instance.ExecuteCommand($"/mk off <triangle>");
        Chat.Instance.ExecuteCommand($"/mk off <cross>");
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("ArmsLength", ref C.ArmsLength);
        ImGui.Checkbox("FastRunRedIce3", ref C.FastRunRedIce3);
        ImGui.SliderFloat("FastCheat", ref C.FastCheat, 1.0f, 1.7f);
        ImGui.SliderFloat("FastCheatDefault", ref C.FastCheatDefault, 1.0f, 1.7f);
        ImGui.Checkbox("IsMaster", ref C.IsMaster);
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            if (ImGui.Button("SetReturnDebug"))
            {
                SetReturn(0, "BaitD1");
                SetReturn(1, "BaitD2");
                SetReturn(2, "BaitD3");
                SetReturn(3, "BaitD4");
            }

            ImGui.Text($"State: {_state}");
            ImGui.Text($"AeloFirst: {_aeloFirst}");
            ImGui.Text($"AeloSecond: {_aeloSecond}");
            ImGui.Text($"RedIce3: {_redIce3}");
            ImGui.Text($"BlueFirst: {_blueFirst}");
            ImGui.Text($"BlueSecond: {_blueSecond}");
            ImGui.Text($"RemoveReturnCount: {_removeReturnCount}");
            ImGui.Text($"_usedSprint: {_usedSprint}");
            ImGui.Text($"_usedArmsLength: {_usedArmsLength}");
            ImGui.Text($"SlowHourGlass: {_slowHourGlassDirection}");
            ImGui.Text($"PartyDataList: {_partyDataList.Count}");
            ImGui.Text($"WaveState: {_waveState}");
            ImGui.Text($"FirstKnockback: {_firstKnockback}");
            ImGui.Text($"SecondKnockback: {_secondKnockback}");
            ImGui.Text($"StateProcEnd: {_StateProcEnd}");
            ImGui.Text($"_MineRoleAction: {_mineRoleAction?.Method.Name}");
            ImGui.Text($"_BeforeReturnProcDone: {_BeforeReturnProcDone}");
            ImGui.Text($"_maelstromCount: {_maelstromCount}");
            ImGui.Text($"_runEndTime: {_runEndTime}");
            ImGui.Text($"_stackBlizzard: {_stackBlizzard}");
            ImGui.Text($"_lastVnavPos: {_lastVnavPos.ToString()}");

            ImGui.Text("PartyDataList");
            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach (var x in _partyDataList)
            {
                Entries.Add(new ImGuiEx.EzTableEntry("Index", true, () => ImGui.Text(x.Index.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("EntityId", true, () => ImGui.Text(x.EntityId.ToString())));
                if (x.Object != null)
                {
                    Entries.Add(new ImGuiEx.EzTableEntry("Position", true, () => ImGui.Text(x.Object.Position.ToString())));
                    Entries.Add(new ImGuiEx.EzTableEntry("Job", true, () => ImGui.Text(x.Object.GetJob().ToString())));
                    Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text(x.Object.Name.ToString())));
                }
                else
                {
                    Entries.Add(new ImGuiEx.EzTableEntry("Position", true, () => ImGui.Text("null")));
                    Entries.Add(new ImGuiEx.EzTableEntry("Job", true, () => ImGui.Text("null")));
                    Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text("null")));
                }
                Entries.Add(new ImGuiEx.EzTableEntry("Mine", true, () => ImGui.Text(x.Mine.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("CrowColor", true, () => ImGui.Text(x.CrowColor)));
                Entries.Add(new ImGuiEx.EzTableEntry("Gimmick", true, () => ImGui.Text(x.Gimmick.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("AeloLR", true, () => ImGui.Text(x.AeloLR)));
                Entries.Add(new ImGuiEx.EzTableEntry("Ice3LR", true, () => ImGui.Text(x.Ice3LR)));
                Entries.Add(new ImGuiEx.EzTableEntry("AttackIndex", true, () => ImGui.Text(x.AttackIndex.ToString())));
            }
            ImGuiEx.EzTable(Entries);

            ImGui.Text("RemoveBuffPosList");
            List<ImGuiEx.EzTableEntry> Entries2 = [];
            foreach (var x in _removeBuffPosList)
            {
                Entries2.Add(new ImGuiEx.EzTableEntry("Position", true, () => ImGui.Text(x.Position.ToString())));
                if (x.AssignEntityId == 0 || x.AssignEntityId.GetObject() == null)
                    Entries2.Add(new ImGuiEx.EzTableEntry("AssignName", true, () => ImGui.Text("null")));
                else
                    Entries2.Add(new ImGuiEx.EzTableEntry("AssignName", true, () => ImGui.Text(x.AssignEntityId.GetObject().Name.ToString())));
            }
            ImGuiEx.EzTable(Entries2);
        }
    }
    #endregion

    #region private methods
    /********************************************************************/
    /* private methods                                                  */
    /********************************************************************/
    private void SetState(StateCommon state)
    {
        HideAllElements();
        ResetCircleElement();
        _StateProcEnd = false;
        _StateProcEndCommon = false;
        _lastVnavPos = Vector3.Zero;
        _state = state;
    }
    private void SetState(StateAeloFirst state)
    {
        HideAllElements();
        ResetCircleElement();
        _StateProcEnd = false;
        _StateProcEndCommon = false;
        _lastVnavPos = Vector3.Zero;
        _aeloFirst = state;
    }
    private void SetState(StateAeloSecond state)
    {
        HideAllElements();
        ResetCircleElement();
        _StateProcEnd = false;
        _StateProcEndCommon = false;
        _lastVnavPos = Vector3.Zero;
        _aeloSecond = state;
    }
    private void SetState(StateRedIce3 state)
    {
        HideAllElements();
        ResetCircleElement();
        _StateProcEnd = false;
        _StateProcEndCommon = false;
        _lastVnavPos = Vector3.Zero;
        _redIce3 = state;
    }
    private void SetState(StateBlueFirst state)
    {
        HideAllElements();
        ResetCircleElement();
        _StateProcEnd = false;
        _StateProcEndCommon = false;
        _lastVnavPos = Vector3.Zero;
        _blueFirst = state;
    }
    private void SetState(StateBlueSecond state)
    {
        HideAllElements();
        ResetCircleElement();
        _StateProcEnd = false;
        _StateProcEndCommon = false;
        _lastVnavPos = Vector3.Zero;
        _blueSecond = state;
    }

    private void RedAeloFirst() // 1人
    {
        if (_StateProcEnd) return;
        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;

        if (_aeloFirst == StateAeloFirst.HourGlassWater)
        {
            if (pc.AeloLR == "L") ApplyElement("Bait", new Vector3(88f, 0, 115f));
            else ApplyElement("Bait", new Vector3(112f, 0, 115f));
        }
        else if (_aeloFirst == StateAeloFirst.IceElapAelo)
        {
            if (pc.AeloLR == "L") ApplyElement("Bait", new Vector3(90.228f, 0, 116.768f));
            else ApplyElement("Bait", new Vector3(109.601f, 0, 116.626f));
        }
        else if (_aeloFirst == StateAeloFirst.GetDragon)
        {
            if (pc.AeloLR == "L") ApplyElement("Bait", new Vector3(92f, 0, 110f));
            else ApplyElement("Bait", new Vector3(108f, 0, 110f));

            do
            {
                if (C.FastRunRedIce3)
                {
                    if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                    {
                        if (pc.AeloLR == "L")
                        {
                            if (_lastVnavPos == new Vector3(92, 0, 110)) break;
                            DuoLog.Information($"/vnav moveto 92 0 110");
                            _lastVnavPos = new Vector3(92, 0, 110);
                        }
                        else
                        {
                            if (_lastVnavPos == new Vector3(108, 0, 110)) break;
                            DuoLog.Information($"/vnav moveto 108 0 110");
                            _lastVnavPos = new Vector3(108, 0, 110);
                        }
                    }
                    else
                    {
                        if (pc.AeloLR == "L")
                        {
                            if (_lastVnavPos == new Vector3(92, 0, 110)) break;
                            Chat.Instance.ExecuteCommand($"/vnav moveto 92 0 110");
                            _lastVnavPos = new Vector3(92, 0, 110);
                        }
                        else
                        {
                            if (_lastVnavPos == new Vector3(108, 0, 110)) break;
                            Chat.Instance.ExecuteCommand($"/vnav moveto 108 0 110");
                            _lastVnavPos = new Vector3(108, 0, 110);
                        }
                    }
                    _runEndTime = Environment.TickCount64 + 10000;
                }
            } while (false);

            _StateProcEnd = true;
        }
        else if (_aeloFirst == StateAeloFirst.AvoidNorth)
        {
            if (_firstKnockback == DirectionCalculator.Direction.West)
            {
                ApplyElement("Bait", new Vector3(102f, 0, 118f));
            }
            else
            {
                ApplyElement("Bait", new Vector3(98f, 0, 118f));
            }

            do
            {
                if (C.FastRunRedIce3)
                {
                    if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                    {
                        if (_firstKnockback == DirectionCalculator.Direction.West)
                        {
                            if (_lastVnavPos == new Vector3(102, 0, 118)) break;
                            DuoLog.Information($"/vnav moveto 102 0 118");
                            _lastVnavPos = new Vector3(102, 0, 118);
                        }
                        else
                        {
                            if (_lastVnavPos == new Vector3(98, 0, 118)) break;
                            DuoLog.Information($"/vnav moveto 98 0 118");
                            _lastVnavPos = new Vector3(98, 0, 118);
                        }
                    }
                    else
                    {
                        if (_firstKnockback == DirectionCalculator.Direction.West)
                        {
                            if (_lastVnavPos == new Vector3(102, 0, 118)) break;
                            Chat.Instance.ExecuteCommand($"/vnav moveto 102 0 118");
                            _lastVnavPos = new Vector3(102, 0, 118);
                        }
                        else
                        {
                            if (_lastVnavPos == new Vector3(98, 0, 118)) break;
                            Chat.Instance.ExecuteCommand($"/vnav moveto 98 0 118");
                            _lastVnavPos = new Vector3(98, 0, 118);
                        }
                    }
                    _runEndTime = Environment.TickCount64 + 10000;
                }
            } while (false);

            _StateProcEnd = true;
        }
        else if (_aeloFirst == StateAeloFirst.SetReturn)
        {
            SetReturn();
        }
        else if (_aeloFirst == StateAeloFirst.Split)
        {
            Split();
        }
    }

    private void RedAeloSecond() // 1人
    {
        if (_StateProcEnd) return;
        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;

        if (_aeloSecond == StateAeloSecond.HourGlassWater)
        {
            if (pc.AeloLR == "L") ApplyElement("Bait", new Vector3(88f, 0, 115f));
            else ApplyElement("Bait", new Vector3(112f, 0, 115f));
        }
        else if (_aeloSecond == StateAeloSecond.IceElapAelo)
        {
            if (pc.AeloLR == "L") ApplyElement("Bait", new Vector3(90.008f, 0, 117.148f));
            else ApplyElement("Bait", new Vector3(109.881f, 0, 117.086f));
        }
        else if (_aeloSecond == StateAeloSecond.Wait)
        {
            if (pc.AeloLR == "R") ApplyElement("Bait", DirectionCalculator.GetAngle(DirectionCalculator.Direction.South) - 15f, 18f);
            else ApplyElement("Bait", DirectionCalculator.GetAngle(DirectionCalculator.Direction.South) + 15f, 18f);

            if (C.FastRunRedIce3)
            {
                if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                {
                    if (pc.AeloLR == "R")
                    {
                        var angle = DirectionCalculator.GetAngle(DirectionCalculator.Direction.South) - 15f;
                        var position = new Vector3(100, 0, 100);
                        position += 18f * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));

                        DuoLog.Information($"/vnav moveto {position.X} 0 {position.Z}");
                    }
                    else
                    {
                        var angle = DirectionCalculator.GetAngle(DirectionCalculator.Direction.South) + 15f;
                        var position = new Vector3(100, 0, 100);
                        position += 18f * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));

                        DuoLog.Information($"/vnav moveto {position.X} 0 {position.Z}");
                    }
                }
                else
                {
                    if (pc.AeloLR == "R")
                    {
                        var angle = DirectionCalculator.GetAngle(DirectionCalculator.Direction.South) - 15f;
                        var position = new Vector3(100, 0, 100);
                        position += 18f * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));

                        Chat.Instance.ExecuteCommand($"/vnav moveto {position.X} 0 {position.Z}");
                    }
                    else
                    {
                        var angle = DirectionCalculator.GetAngle(DirectionCalculator.Direction.South) + 15f;
                        var position = new Vector3(100, 0, 100);
                        position += 18f * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));

                        Chat.Instance.ExecuteCommand($"/vnav moveto {position.X} 0 {position.Z}");
                    }
                }
                _runEndTime = Environment.TickCount64 + 10000;
            }

            _StateProcEnd = true;
        }
        else if (_aeloSecond == StateAeloSecond.GetDragon)
        {
            if (pc.AeloLR == "R") ApplyElement("Bait", new Vector3(106f, 0, 111.5f));
            else ApplyElement("Bait", new Vector3(94f, 0, 111.5f));

            do
            {
                if (C.FastRunRedIce3)
                {
                    if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                    {
                        if (pc.AeloLR == "R")
                        {
                            if (_lastVnavPos == new Vector3(106, 0, 111.5f)) break;
                            DuoLog.Information($"/vnav moveto 106 0 111.5");
                            _lastVnavPos = new Vector3(106, 0, 111.5f);
                        }
                        else
                        {
                            if (_lastVnavPos == new Vector3(94, 0, 111.5f)) break;
                            DuoLog.Information($"/vnav moveto 94 0 111.5");
                            _lastVnavPos = new Vector3(94, 0, 111.5f);
                        }
                    }
                    else
                    {
                        if (pc.AeloLR == "R")
                        {
                            if (_lastVnavPos == new Vector3(106, 0, 111.5f)) break;
                            Chat.Instance.ExecuteCommand($"/vnav moveto 106 0 111.5");
                            _lastVnavPos = new Vector3(106f, 0, 111.5f);
                        }
                        else
                        {
                            if (_lastVnavPos == new Vector3(94, 0, 111.5f)) break;
                            Chat.Instance.ExecuteCommand($"/vnav moveto 94 0 111.5");
                            _lastVnavPos = new Vector3(94f, 0, 111.5f);
                        }
                    }
                    _runEndTime = Environment.TickCount64 + 10000;
                }
            } while (false);

            _StateProcEnd = true;
        }
        else if (_aeloSecond == StateAeloSecond.SetReturn)
        {
            do
            {
                if (C.FastRunRedIce3)
                {
                    if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                    {
                        if (_firstKnockback == DirectionCalculator.Direction.West)
                        {
                            if (_lastVnavPos == new Vector3(102, 0, 118)) break;
                            DuoLog.Information($"/vnav moveto 102 0 118");
                            _lastVnavPos = new Vector3(102, 0, 118);
                        }
                        else
                        {
                            if (_lastVnavPos == new Vector3(98, 0, 118)) break;
                            DuoLog.Information($"/vnav moveto 98 0 118");
                            _lastVnavPos = new Vector3(98, 0, 118);
                        }
                    }
                    else
                    {
                        if (_firstKnockback == DirectionCalculator.Direction.West)
                        {
                            if (_lastVnavPos == new Vector3(102, 0, 118)) break;
                            Chat.Instance.ExecuteCommand($"/vnav moveto 102 0 118");
                            _lastVnavPos = new Vector3(102, 0, 118);
                        }
                        else
                        {
                            if (_lastVnavPos == new Vector3(98, 0, 118)) break;
                            Chat.Instance.ExecuteCommand($"/vnav moveto 98 0 118");
                            _lastVnavPos = new Vector3(98, 0, 118);
                        }
                    }
                    _runEndTime = Environment.TickCount64 + 10000;
                }
            } while (false);

            SetReturn();
        }
        else if (_aeloSecond == StateAeloSecond.Split)
        {
            Split();
        }
    }

    private void RedIce3() // 2人
    {
        if (_redIce3 == StateRedIce3.None) return;
        if (_StateProcEnd) return;
        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;

        if (_redIce3 == StateRedIce3.HourGlassWater)
        {
            if (pc.Ice3LR == "L")
                ApplyElement("Bait", new Vector3(87f, 0, 100f));
            else
                ApplyElement("Bait", new Vector3(113f, 0, 100f));

            _StateProcEnd = true;
        }
        else if (_redIce3 == StateRedIce3.IceElapAelo)
        {
            if (pc.Ice3LR == "L")
                ApplyElement("Bait", new Vector3(87f, 0, 100f));
            else
                ApplyElement("Bait", new Vector3(113f, 0, 100f));

            if (C.FastRunRedIce3)
            {
                var Ice3Buff = pc.Object?.StatusList.FirstOrDefault(x => x.StatusId == 2462) ?? null;
                if (Ice3Buff == null) return;

                if (Ice3Buff.RemainingTime > 0.5f) return;

                if ((_slowHourGlassDirection == DirectionCalculator.Direction.NorthWest && pc.Ice3LR == "L") ||
                (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast && pc.Ice3LR == "R"))
                {
                    _stackBlizzard = true;
                }

                var angle = DirectionCalculator.GetAngle(DirectionCalculator.Direction.North);
                var position = new Vector3(100, 0, 100);
                position += 18f * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));

                if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                {
                    DuoLog.Information($"/vnav moveto {position.X} 0 {position.Z}");
                }
                else
                {
                    Chat.Instance.ExecuteCommand($"/vnav moveto {position.X} 0 {position.Z}");
                }
                _runEndTime = Environment.TickCount64 + 10000;
            }

            _StateProcEnd = true;
        }
        else if (_redIce3 == StateRedIce3.holy)
        {
            //if ((_slowHourGlassDirection == DirectionCalculator.Direction.NorthWest && pc.Ice3LR == "L") ||
            //    (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast && pc.Ice3LR == "R"))
            //    if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
            //        ApplyElement("Bait", new Vector3(112f, 0, 85f));
            //    else
            //        ApplyElement("Bait", new Vector3(88f, 0, 85f));
            //else
            //    ApplyElement("Bait", DirectionCalculator.Direction.North, 18f);

            ApplyElement("Bait", DirectionCalculator.Direction.North, 18f);
        }
        else if (_redIce3 == StateRedIce3.SetReturn)
        {
            SetReturn();
        }
        else if (_redIce3 == StateRedIce3.Split)
        {
            Split();
        }
    }

    private void BlueElap() // 1人
    {
        if (_StateProcEnd) return;
        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;
        if (_blueFirst == StateBlueFirst.HourGlassWater || _blueSecond == StateBlueSecond.HourGlassWater)
        {
            if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                ApplyElement("Bait", new Vector3(112f, 0, 85f));
            else
                ApplyElement("Bait", new Vector3(88f, 0, 85f));

            _StateProcEnd = true;
        }
        else if (_blueFirst == StateBlueFirst.IceElapAelo || _blueSecond == StateBlueSecond.IceElapAelo)
        {
            if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                ApplyElement("Bait", new Vector3(112f, 0, 85f));
            else
                ApplyElement("Bait", new Vector3(88f, 0, 85f));

            _StateProcEnd = true;
        }
    }

    private void BlueBefore() // 3人 (Water3, Holy, Ice3)
    {
        if (_StateProcEnd) return;
        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;

        if (_blueFirst == StateBlueFirst.HourGlassWater || _blueSecond == StateBlueSecond.HourGlassWater)
        {
            if (pc.Gimmick == Gimmick.Elap)
            {
                BlueElap();
                return;
            }

            if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                ApplyElement("Bait", new Vector3(88f, 0, 115f));
            else
                ApplyElement("Bait", new Vector3(112f, 0, 115f));
        }
        else if (_blueFirst == StateBlueFirst.IceElapAelo || _blueSecond == StateBlueSecond.IceElapAelo)
        {

            if (pc.Gimmick == Gimmick.Elap)
            {
                BlueElap();
                return;
            }

            if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                ApplyElement("Bait", new Vector3(91f, 0, 115.6f));
            else
                ApplyElement("Bait", new Vector3(109f, 0, 115.6f));

            //if (Controller.TryGetElementByName("Line", out var el))
            //{
            //    if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
            //    {
            //        var aelo = _partyDataList.Find(x => x.Gimmick == Gimmick.Aelo && x.AeloLR == "L");
            //        if (aelo == null || aelo.Object == null) return;
            //        var overDistance = Vector3.Distance(pc.Object.Position, aelo.Object.Position);
            //        el.SetRefPosition(aelo.Object.Position);
            //        var endPos = GetExtendedAndClampedPosition(aelo.Object.Position, pc.Object.Position, 34 + overDistance, 70f);
            //        el.SetOffPosition(endPos);
            //        el.Enabled = true;
            //    }
            //    else
            //    {
            //        // NorthWest
            //        var aelo = _partyDataList.Find(x => x.Gimmick == Gimmick.Aelo && x.AeloLR == "R");
            //        if (aelo == null || aelo.Object == null) return;
            //        var overDistance = Vector3.Distance(pc.Object.Position, aelo.Object.Position);
            //        el.SetRefPosition(aelo.Object.Position);
            //        var endPos = GetExtendedAndClampedPosition(aelo.Object.Position, pc.Object.Position, 34 + overDistance, 70f);
            //        el.SetOffPosition(endPos);
            //        el.Enabled = true;
            //    }
            //}
        }
        else if (_blueFirst == StateBlueFirst.holy || _blueSecond == StateBlueSecond.holy)
        {
            if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                ApplyElement("Bait", new Vector3(112f, 0, 85f));
            else
                ApplyElement("Bait", new Vector3(88f, 0, 85f));
        }
        else if (_blueFirst != StateBlueFirst.None)
        {
            BlueAfterFirst();
        }
        else
        {
            // _blueSecond != StateBlueSecond.None
            BlueAfterSecond();
        }
    }

    private void BlueAfterFirst() // 2人 (Water3, Holy, Ice3)
    {
        if (_StateProcEnd) return;
        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;

        if (_blueFirst == StateBlueFirst.WaitNorth)
        {
            ApplyElement("Bait", DirectionCalculator.Direction.North, 18f);
        }
        if (_blueFirst == StateBlueFirst.GetRemoveBuff)
        {
            ShowBlueLocation();
        }
        else if (_blueFirst == StateBlueFirst.SetReturn)
        {
            SetReturn();
        }
        else if (_blueFirst == StateBlueFirst.Split)
        {
            Split();
        }
    }

    private void BlueAfterSecond() // 2人 (Water3, Holy, Ice3)
    {
        if (_StateProcEnd) return;
        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;

        if (_blueSecond == StateBlueSecond.WaitNorth)
        {
            ApplyElement("Bait", DirectionCalculator.Direction.North, 18f);
        }
        else if (_blueSecond == StateBlueSecond.SetReturn)
        {
            SetReturn();
        }
        else if (_blueSecond == StateBlueSecond.GetRemoveBuff)
        {
            Split();
        }
    }

    private void ShowBlueLocation()
    {
        if (_StateProcEnd) return;
        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;
        if (_removeBuffPosList.Count == 0) return;

        // buffが2 ~ 3人の場合
        if (_removeBuffPosList.Count >= 2 && _removeBuffPosList.Count <= 3)
        {
            // 若番の２人以外は何もしない
            if (pc.AttackIndex > 2) return;
            ApplyElement("Bait", _removeBuffPosList[pc.AttackIndex - 1].Position);
        }
        if (_removeBuffPosList.Count == 4)
        {
            ApplyElement("Bait", _removeBuffPosList[pc.AttackIndex - 1].Position);
        }
    }

    private void CommonUpdate()
    {
        if (_StateProcEndCommon) return;

        if (_state == StateCommon.HourGlassWater)
        {
            // Dark Water
            var water = _partyDataList.Find(x => x.Gimmick == Gimmick.Water3);
            if (water != null && Controller.TryGetElementByName("CircleFixed0", out var el) && water.Object != null)
            {
                el.SetRefPosition(water.Object.Position);
                el.color = 0xC800FF00; // Green
                el.radius = 6.0f;
                el.thicc = 6.0f;
                el.Filled = false;
                el.Enabled = true;
            }

            // Maelstrom 南北の砂時計確定
            if (Controller.TryGetElementByName("CircleFixed1", out el))
            {
                ApplyElement(el, DirectionCalculator.Direction.North, 11.0f, 12.0f, true, false);
            }

            if (Controller.TryGetElementByName("CircleFixed2", out el))
            {
                ApplyElement(el, DirectionCalculator.Direction.South, 11.0f, 12.0f, true, false);
            }
        }
        if (_state == StateCommon.IceElapAelo)
        {
            //// Dark Blizzard III
            var ice = _partyDataList.FirstOrDefault(x => x.Gimmick == Gimmick.Ice3);
            if (ice != null && Controller.TryGetElementByName("CircleFixed0", out var el) && ice.Object != null)
            {
                el.SetRefPosition(ice.Object.Position);
                el.color = 0xC80000FF; // red
                el.radius = 3.0f;
                el.Donut = 9.0f;
                el.Enabled = true;
            }

            var anotherIce = _partyDataList.FirstOrDefault(
                x => x.Gimmick == Gimmick.Ice3 && ice != null && x.EntityId != ice.EntityId);
            if (anotherIce != null && Controller.TryGetElementByName("CircleFixed1", out el) && anotherIce.Object != null)
            {
                el.SetRefPosition(anotherIce.Object.Position);
                el.color = 0xC80000FF; // red
                el.radius = 3.0f;
                el.Donut = 9.0f;
                el.Enabled = true;
            }

            // Dark Eruption
            var elap = _partyDataList.FirstOrDefault(x => x.Gimmick == Gimmick.Elap);
            if (elap != null && Controller.TryGetElementByName("CircleFixed2", out el) && elap.Object != null)
            {
                el.SetRefPosition(elap.Object.Position);
                el.color = 0xC80000FF; // red
                el.radius = 6.0f;
                el.Enabled = true;
            }
        }
        if (_state == StateCommon.holy)
        {
            // Dark Holy
            var holy = _partyDataList.FirstOrDefault(x => x.Gimmick == Gimmick.Holy);
            if (holy != null && Controller.TryGetElementByName("CircleFixed0", out var el) && holy.Object != null)
            {
                el.SetRefPosition(holy.Object.Position);
                el.color = 0xC800FF00; // Green
                el.thicc = 6.0f;
                el.radius = 6.0f;
                el.Enabled = true;
            }

            // Maelstrom
            var normalHourGlassDirection = _slowHourGlassDirection switch
            {
                DirectionCalculator.Direction.NorthEast => DirectionCalculator.Direction.NorthWest,
                _ => DirectionCalculator.Direction.NorthEast
            };

            float correctinAngle = _slowHourGlassDirection switch
            {
                DirectionCalculator.Direction.NorthEast => -15.0f,
                _ => +15.0f
            };
            if (Controller.TryGetElementByName("CircleFixed1", out el))
            {
                ApplyElement(el, DirectionCalculator.GetAngle(normalHourGlassDirection) + correctinAngle, 11.0f, 12.0f, true, false);
            }

            if (Controller.TryGetElementByName("CircleFixed2", out el))
            {
                ApplyElement(el, DirectionCalculator.GetAngle(DirectionCalculator.GetOppositeDirection(normalHourGlassDirection)) + correctinAngle, 11.0f, 12.0f, true, false);
            }
        }
        if (_state == StateCommon.SetReturn)
        {
            float correctinAngle = _slowHourGlassDirection switch
            {
                DirectionCalculator.Direction.NorthEast => +15.0f,
                _ => -15.0f
            };

            if (Controller.TryGetElementByName("CircleFixed1", out var el))
            {
                ApplyElement(el, DirectionCalculator.GetAngle(_slowHourGlassDirection) + correctinAngle, 11.0f, 12.0f, true, false);
            }

            if (Controller.TryGetElementByName("CircleFixed2", out el))
            {
                ApplyElement(el, DirectionCalculator.GetAngle(DirectionCalculator.GetOppositeDirection(_slowHourGlassDirection)) + correctinAngle, 11.0f, 12.0f, true, false);
            }
            _StateProcEndCommon = true;
        }
        if (_aeloFirst == StateAeloFirst.SetReturn || _aeloSecond == StateAeloSecond.SetReturn || _redIce3 == StateRedIce3.SetReturn)
        {
            int i = 0;
            foreach (var x in _removeBuffPosList)
            {
                if (Controller.TryGetElementByName($"CircleFixed1{i}", out var el))
                {
                    ApplyElement(el, x.Position, 1f, tether: false);
                }
                i++;
            }
        }
    }

    private void SetReturn(int index = 0xFFFF, string elname = "Bait")
    {
        PartyData? pc = null;

        if (index == 0xFFFF)
        {
            pc = GetMinedata();
            if (pc == null || pc.Object == null) return;
        }
        else
        {
            pc = _partyDataList[index];
            if (pc == null || pc.Object == null) return;
        }

        if (_firstKnockback == DirectionCalculator.Direction.None ||
            _secondKnockback == DirectionCalculator.Direction.None)
        {
            if (pc.Gimmick == Gimmick.Aelo) ApplyElement(elname, DirectionCalculator.Direction.South, 18f);
            else ApplyElement(elname, DirectionCalculator.Direction.North, 18f);
            return;
        }

        // DEBUG
        //_firstKnockback = DirectionCalculator.Direction.East;
        //_secondKnockback = DirectionCalculator.Direction.South;

        if (pc.Index == 6)
        {
            ApplyElement(elname, DirectionCalculator.GetAngle(_secondKnockback), 12f);
        }
        else if (pc.Index == 7)
        {
            if ((_firstKnockback == DirectionCalculator.Direction.East && _secondKnockback == DirectionCalculator.Direction.South) ||
                (_firstKnockback == DirectionCalculator.Direction.West && _secondKnockback == DirectionCalculator.Direction.North))
                ApplyElement(elname, DirectionCalculator.GetAngle(_secondKnockback) - 45f, 8f);
            else
                ApplyElement(elname, DirectionCalculator.GetAngle(_secondKnockback) + 45f, 8f);
        }
        else if (pc.Index is 2 or 4 or 0) // D1
        {
            if ((_firstKnockback == DirectionCalculator.Direction.East && _secondKnockback == DirectionCalculator.Direction.South) ||
                (_firstKnockback == DirectionCalculator.Direction.West && _secondKnockback == DirectionCalculator.Direction.North))
                ApplyElement(elname, DirectionCalculator.GetAngle(_secondKnockback) - 35f, 5f);
            else
                ApplyElement(elname, DirectionCalculator.GetAngle(_secondKnockback) + 35f, 5f);
        }
        else // D2
        {
            if ((_firstKnockback == DirectionCalculator.Direction.East && _secondKnockback == DirectionCalculator.Direction.South) ||
                (_firstKnockback == DirectionCalculator.Direction.West && _secondKnockback == DirectionCalculator.Direction.North))
                ApplyElement(elname, DirectionCalculator.GetAngle(_secondKnockback) + 15f, 10f);
            else
                ApplyElement(elname, DirectionCalculator.GetAngle(_secondKnockback) - 15f, 10f);
        }
    }

    private void Split()
    {
        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;
        if (_removeBuffPosList.Count == 0) return;

        if (!_StateProcEnd)
        {
            Chat.Instance.ExecuteCommand($"/pdrspeed 1.7");
            _StateProcEnd = true;
        }


        Vector3 pos = new Vector3();

        if (pc.AttackIndex != 0) // Blue
        {
            ApplyElement("Bait", _removeBuffPosList[pc.AttackIndex - 1].Position);

            pos = _removeBuffPosList[pc.AttackIndex - 1].Position;
        }
        else // Red
        {
            if (pc.Gimmick == Gimmick.Aelo && pc.AeloLR == "L")
            {
                pos = new Vector3(93.376f, 0, 103.187f);
            }
            else if (pc.Gimmick == Gimmick.Aelo && pc.AeloLR == "R")
            {
                pos = new Vector3(100.461f, 0, 106.201f);
            }
            else if (pc.Gimmick == Gimmick.Ice3 && pc.Ice3LR == "L")
            {
                pos = new Vector3(95.302f, 0, 96.108f);
            }
            else if (pc.Gimmick == Gimmick.Ice3 && pc.Ice3LR == "R")
            {
                pos = new Vector3(103.871f, 0, 97.839f);
            }
        }

        ApplyElement("Bait", pos);

        do
        {
            if (C.FastRunRedIce3)
            {
                if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                {
                    if (_lastVnavPos == pos) break;
                    DuoLog.Information($"/vnav moveto {pos.X} 0 {pos.Z}");
                    _lastVnavPos = pos;
                }
                else
                {
                    if (_lastVnavPos == pos) break;
                    Chat.Instance.ExecuteCommand($"/vnav moveto {pos.X} 0 {pos.Z}");
                    _lastVnavPos = pos;
                }
                _runEndTime = Environment.TickCount64 + 10000;
            }
        } while (false);
    }

    private void UseArmsLength()
    {
        if (!C.ArmsLength || _usedArmsLength) return;

        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;

        var return4Buff = pc.Object?.StatusList.FirstOrDefault(x => x.StatusId == 2452) ?? null;
        if (return4Buff == null) return;

        if (return4Buff.RemainingTime > 2.0f) return;

        uint castID = 0;

        if (HealerJobs.Contains(Player.Object.GetJob()) || MagicDpsJobs.Contains(Player.Object.GetJob()))
        {
            castID = 7559u; // SureCast
        }
        else
        {
            castID = 7548u; // ArmsLength
        }

        if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
        {
            if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, castID))
            {
                DuoLog.Information((castID == 7548) ? "ArmsLength" : "SureCast");
                _usedArmsLength = true;
            }
        }
        else
        {
            if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, castID))
            {
                actionManager->UseAction(ActionType.Action, castID);
            }

            if (actionManager->IsRecastTimerActive(ActionType.Action, castID))
            {
                _usedArmsLength = true;
            }
        }
    }

    private void ResetCircleElement()
    {
        for (var i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"CircleFixed{i}", new Element(0) { radius = 5.0f, thicc = 2f, fillIntensity = 0.5f }, true);
        }

        for (var i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"CircleObject{i}", new Element(1) { radius = 5.0f, refActorComparisonType = 2, thicc = 6f, fillIntensity = 0.5f }, true);
        }
    }

    private PartyData? GetMinedata() => _partyDataList.Find(x => x.Mine) ?? null;

    private void SetListEntityIdByJob()
    {
        _partyDataList.Clear();
        var tmpList = new List<PartyData>();

        foreach (var pc in FakeParty.Get())
        {
            tmpList.Add(new PartyData(pc.EntityId, Array.IndexOf(jobOrder, pc.GetJob())));
        }

        // Sort by job order
        tmpList.Sort((a, b) => a.Index.CompareTo(b.Index));
        foreach (var data in tmpList)
        {
            _partyDataList.Add(data);
        }

        // Set index
        for (var i = 0; i < _partyDataList.Count; i++)
        {
            _partyDataList[i].Index = i;
        }
    }
    #endregion

    #region API
    /********************************************************************/
    /* API                                                              */
    /********************************************************************/
    private static readonly Job[] jobOrder =
    {
        Job.WHM,
        Job.AST,
        Job.SCH,
        Job.SGE,
        Job.BRD,
        Job.MCH,
        Job.DNC,
        Job.RDM,
        Job.SMN,
        Job.PCT,
        Job.BLM,
        Job.DRG,
        Job.VPR,
        Job.SAM,
        Job.MNK,
        Job.RPR,
        Job.NIN,
        Job.DRK,
        Job.WAR,
        Job.GNB,
        Job.PLD,
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
        public enum Direction :int
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

        public enum LR :int
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
            foreach (var directionalVector in directionalVectors)
            {
                var distance = Vector3.Distance(Position, directionalVector.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestDirection = directionalVector.Direction;
                }
            }

            return closestDirection;
        }

        public static Direction GetDirectionFromAngle(Direction direction, int angle)
        {
            if (direction == Direction.None) return Direction.None; // 無効な方向の場合

            // 方向数（8方向: North ~ NorthWest）
            const int directionCount = 8;

            // 角度を45度単位に丸め、-180～180の範囲に正規化
            angle = ((Round45(angle) % 360) + 360) % 360; // 正の値に変換して360で正規化
            if (angle > 180) angle -= 360;

            // 現在の方向のインデックス
            int currentIndex = (int)direction;

            // 45度ごとのステップ計算と新しい方向の計算
            int step = angle / 45;
            int newIndex = (currentIndex + step + directionCount) % directionCount;

            return (Direction)newIndex;
        }

        public static LR GetTwoPointLeftRight(Direction direction1, Direction direction2)
        {
            // 不正な方向の場合（None）
            if (direction1 == Direction.None || direction2 == Direction.None)
                return LR.SameOrOpposite;

            // 方向数（8つ: North ~ NorthWest）
            int directionCount = 8;

            // 差分を循環的に計算
            int difference = ((int)direction2 - (int)direction1 + directionCount) % directionCount;

            // LRを直接返す
            return difference == 0 || difference == directionCount / 2
                ? LR.SameOrOpposite
                : (difference < directionCount / 2 ? LR.Right : LR.Left);
        }

        public static int GetTwoPointAngle(Direction direction1, Direction direction2)
        {
            // 不正な方向を考慮
            if (direction1 == Direction.None || direction2 == Direction.None)
                return 0;

            // enum の値を数値として扱い、環状の差分を計算
            int diff = ((int)direction2 - (int)direction1 + 8) % 8;

            // 差分から角度を計算
            return diff <= 4 ? diff * 45 : (diff - 8) * 45;
        }

        public static float GetAngle(Direction direction)
        {
            if (direction == Direction.None) return 0; // 無効な方向の場合

            // 45度単位で計算し、0度から始まる時計回りの角度を返す
            return (int)direction * 45 % 360;
        }

        private static List<DirectionalVector> GenerateDirectionalVectors(float distance, Vector3? center = null)
        {
            var directionalVectors = new List<DirectionalVector>();

            // 各方向のオフセット計算
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                if (direction == Direction.None) continue; // Noneはスキップ

                Vector3 offset = direction switch
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
                Vector3 position = (center ?? new Vector3(100, 0, 100)) + (offset * distance);

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
            if (!isValid)
                return DirectionCalculator.Direction.None;

            // 特別ケース: clock = 0 の場合、_12ClockDirection をそのまま返す
            if (clock == 0)
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
            int baseIndex = (int)_12ClockDirection;

            // 時計位置に基づくステップを取得
            int step = clockToDirectionMapping[clock];

            // 新しい方向を計算し、範囲を正規化
            int targetIndex = (baseIndex + step + 8) % 8;

            // 対応する方向を返す
            return (DirectionCalculator.Direction)targetIndex;
        }

        public int GetClockFromDirection(DirectionCalculator.Direction direction)
        {
            if (!isValid)
                throw new InvalidOperationException("Invalid state: _12ClockDirection is not set.");

            if (direction == DirectionCalculator.Direction.None)
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
            int baseIndex = (int)_12ClockDirection;

            // 指定された方向のインデックス
            int targetIndex = (int)direction;

            // 差分を計算し、時計方向に正規化
            int step = (targetIndex - baseIndex + 8) % 8;

            // 該当する clock を取得
            return directionToClockMapping[step];
        }

        public float GetAngle(int clock) => DirectionCalculator.GetAngle(GetDirectionFromClock(clock));
    }

    private void HideAllElements() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    // original
    private void ApplyElement(
        [NotNull] Element element,
        Vector3 position,
        float elementRadius = 0.3f,
        bool Filled = true,
        bool tether = true)
    {
        element.Enabled = true;
        element.radius = elementRadius;
        element.tether = tether;
        element.Filled = Filled;
        element.SetRefPosition(position);
    }

    // mutable
    private void ApplyElement(
        string elementName, // mutable
        Vector3 position,
        float elementRadius = 0.3f,
        bool Filled = true,
        bool tether = true)
    {
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            ApplyElement(element, position, elementRadius, Filled, tether);
        }
    }

    private void ApplyElement(
        [NotNull] Element element,
        float angle, // mutable
        float radius = 0f,
        float elementRadius = 0.3f,
        bool Filled = true,
        bool tether = true)
    {
        var position = new Vector3(100, 0, 100);
        position += radius * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));
        ApplyElement(element, position, elementRadius, Filled, tether);
    }

    private void ApplyElement(
        [NotNull] Element element, // mutable
        DirectionCalculator.Direction direction, // mutable
        float radius = 0f,
        float elementRadius = 0.3f,
        bool Filled = true,
        bool tether = true)
    {
        var angle = DirectionCalculator.GetAngle(direction);
        ApplyElement(element, angle, radius, elementRadius, Filled, tether);
    }


    private void ApplyElement(
        string elementName, // mutable
        DirectionCalculator.Direction direction, // mutable
        float radius = 0f,
        float elementRadius = 0.3f,
        bool Filled = true,
        bool tether = true)
    {
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            var angle = DirectionCalculator.GetAngle(direction);
            ApplyElement(element, angle, radius, elementRadius, Filled, tether);
        }
    }

    private void ApplyElement(
        string elementName, // mutable
        float angle,
        float radius = 0f,
        float elementRadius = 0.3f,
        bool Filled = true,
        bool tether = true)
    {
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            ApplyElement(element, angle, radius, elementRadius, Filled, tether);
        }
    }

    private static float GetCorrectionAngle(Vector3 origin, Vector3 target, float rotation) =>
            GetCorrectionAngle(MathHelper.ToVector2(origin), MathHelper.ToVector2(target), rotation);

    private static float GetCorrectionAngle(Vector2 origin, Vector2 target, float rotation)
    {
        // Calculate the relative angle to the target
        Vector2 direction = target - origin;
        float relativeAngle = MathF.Atan2(direction.Y, direction.X) * (180 / MathF.PI);

        // Normalize relative angle to 0-360 range
        relativeAngle = (relativeAngle + 360) % 360;

        // Calculate the correction angle
        float correctionAngle = (relativeAngle - ConvertRotationRadiansToDegrees(rotation) + 360) % 360;

        // Adjust correction angle to range -180 to 180 for shortest rotation
        if (correctionAngle > 180)
            correctionAngle -= 360;

        return correctionAngle;
    }

    private static float ConvertRotationRadiansToDegrees(float radians)
    {
        // Convert radians to degrees with coordinate system adjustment
        float degrees = ((-radians * (180 / MathF.PI)) + 180) % 360;

        // Ensure the result is within the 0° to 360° range
        return degrees < 0 ? degrees + 360 : degrees;
    }

    private static float ConvertDegreesToRotationRadians(float degrees)
    {
        // Convert degrees to radians with coordinate system adjustment
        float radians = -(degrees - 180) * (MathF.PI / 180);

        // Normalize the result to the range -π to π
        radians = ((radians + MathF.PI) % (2 * MathF.PI)) - MathF.PI;

        return radians;
    }

    public static Vector3 GetExtendedAndClampedPosition(
        Vector3 center, Vector3 currentPos, float extensionLength, float? limit)
    {
        // Calculate the normalized direction vector from the center to the current position
        Vector3 direction = Vector3.Normalize(currentPos - center);

        // Extend the position by the specified length
        Vector3 extendedPos = currentPos + (direction * extensionLength);

        // If limit is null, return the extended position without clamping
        if (!limit.HasValue)
        {
            return extendedPos;
        }

        // Calculate the distance from the center to the extended position
        float distanceFromCenter = Vector3.Distance(center, extendedPos);

        // If the extended position exceeds the limit, clamp it within the limit
        if (distanceFromCenter > limit.Value)
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

    private unsafe int GetPlayerTag(uint entityId)
    {
        for (int i = 1; i <= 8; i++)
        {
            var obj = FakePronoun.Resolve($"<{i}>");
            if (obj != null && obj->EntityId == entityId)
            {
                return i;
            }
        }
        return -1;
    }
    #endregion
}
