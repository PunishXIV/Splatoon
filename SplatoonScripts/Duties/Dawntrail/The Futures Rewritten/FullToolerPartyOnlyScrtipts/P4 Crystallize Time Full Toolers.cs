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
    private enum State
    {
        None = 0,
        GetBuffs,
        HourGlassWater,
        IceElapAelo,
        holy,
        SetReturn,
        GetRemoveBuff,
        Split
    }

    private enum AeloSubState
    {
        None = 0,
        Wait,
        GetDragon,
        AvoidSouth,
        SetReturn
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
        public bool Sprint = true;
        public bool ArmsLength = true;
        public bool FastRunRedIce3 = true;
    }

    private class RemoveBuff
    {
        public Vector3 Position = Vector3.Zero;
        public uint AssignEntityId = 0;
    }

    private class PartyData
    {
        public int Index = 0;
        public bool Mine => this.EntityId == Player.Object.EntityId;
        public uint EntityId;
        public IPlayerCharacter? Object => (IPlayerCharacter)this.EntityId.GetObject()! ?? null;
        public string CrowColor = "";
        public Gimmick Gimmick = Gimmick.None;
        public string AeloLR = "";
        public string Ice3LR = "";

        public bool IsTank => TankJobs.Contains(Object?.GetJob() ?? Job.WHM);
        public bool IsHealer => HealerJobs.Contains(Object?.GetJob() ?? Job.PLD);
        public bool IsTH => IsTank || IsHealer;
        public bool IsMeleeDps => MeleeDpsJobs.Contains(Object?.GetJob() ?? Job.MCH);
        public bool IsRangedDps => RangedDpsJobs.Contains(Object?.GetJob() ?? Job.MNK);
        public bool IsDps => IsMeleeDps || IsRangedDps;

        public PartyData(uint entityId, int index)
        {
            EntityId = entityId;
            //if (this.EntityId == Player.Object.EntityId) this.Mine = true;
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
    public override Metadata? Metadata => new(2, "redmoon");
    #endregion

    #region private properties
    /********************************************************************/
    /* private properties                                               */
    /********************************************************************/
    private State _state = State.None;
    private AeloSubState _aeloSubState = AeloSubState.None;
    private Config C => Controller.GetConfig<Config>();
    private List<PartyData> _partyDataList = new();
    private MineRoleAction? _mineRoleAction = null;
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
    #endregion

    #region public methods
    /********************************************************************/
    /* public methods                                                   */
    /********************************************************************/
    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0) { tether = true, radius = 3f, thicc = 6f });
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

        Controller.RegisterElement($"Line", new Element(2) { radius = 0f, thicc = 6f, fillIntensity = 0.5f });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 40298)
        {
            SetListEntityIdByJob();
            SetState(State.GetBuffs);
        }

        if (_state == State.None) return;

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
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action == null) return;
        var castId = set.Action.Value.RowId;

        if (castId == 40299 && _state == State.HourGlassWater)
        {
            SetState(State.IceElapAelo);
        }

        if (castId == 40274 && _state == State.IceElapAelo)
        {
            SetState(State.holy);
        }

        if (castId == 40277 && _state == State.holy)
        {
            HideAllElements();
            var pc = GetMinedata();
            if (pc == null || pc.Object == null) return;
            bool ExistBlue = pc.Object.StatusList.Any(x => x.StatusId == 3264);
            _StateProcEnd = false;
            if (pc.CrowColor == "")
            {
                SetState(State.SetReturn);
            }
            else
            {
                SetState(State.GetRemoveBuff);
            }
        }

        if (castId == 40241 && set.SourceCharacter != null)
        {
            RemoveBuff removeBuff = new();
            removeBuff.Position = set.SourceCharacter.Value.Position;
            _removeBuffPosList.Add(removeBuff);

            if (_removeBuffPosList.Count == 4)
            {
                AdjustRemoveBuff();
                _BeforeReturnProcDone = true;
            }
        }

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

        if (castId == 40299)
        {
            _maelstromCount++;
            if (_maelstromCount == 3 && _aeloSubState == AeloSubState.Wait)
            {
                HideAllElements();
                _aeloSubState = AeloSubState.GetDragon;
            }
            else if (_maelstromCount == 5)
            {
                HideAllElements();
                if (_aeloSubState == AeloSubState.AvoidSouth)
                {
                    _aeloSubState = AeloSubState.SetReturn;
                }
            }
        }

        if (castId == 40332)
        {
            this.OnReset();
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_state == State.None) return;
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
    }

    public override void OnGainBuffEffect(uint sourceId, FFXIVClientStructs.FFXIV.Client.Game.Status Status)
    {
        if (_state != State.GetBuffs) return;
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

        // 全ての情報が揃ったら、次のステップへ
        if (_partyDataList.All(x => x.Gimmick != Gimmick.None && x.CrowColor != "") && _mineRoleAction == null)
        {
            var mine = GetMinedata();
            if (mine == null) return;
            // 自分のアクションメソッドを設定
            // 青+エラプション
            if (mine.Gimmick == Gimmick.Elap && mine.CrowColor == "blue")
            {
                _mineRoleAction = BlueElap; // 1人
            }
            // 赤+エアロガ
            else if (mine.Gimmick == Gimmick.Ice3 && mine.CrowColor == "red")
            {
                _mineRoleAction = RedIce3; // 2人
            }
            // 赤+ブリザガ
            else if (mine.Gimmick == Gimmick.Ice3 && mine.CrowColor == "red")
            {
                _mineRoleAction = RedAelo; // 2人
            }
            // 青+そのた
            else if (mine.CrowColor == "blue")
            {
                _mineRoleAction = Blue; // 3人 (Water3, Holy, Ice3)
            }

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

            // DEBUG
            //_mineRoleAction = RedIce3; // TODO あとで消す
            //_partyDataList.Each(x => x.Mine = false);
            //_partyDataList[0].Mine = true;

            SetState(State.HourGlassWater);
        }
    }

    public override void OnRemoveBuffEffect(uint sourceId, FFXIVClientStructs.FFXIV.Client.Game.Status Status)
    {
        if (_state == State.None) return;
        var statusId = Status.StatusId;

        if (statusId == 4208)
        {
            _removeReturnCount++;
            if (_removeReturnCount == 8)
            {
                var pc = GetMinedata();
                if (pc == null || pc.Object == null) return;
                if (pc.CrowColor == "")
                {
                    SetState(State.Split);
                }
                else
                {
                    SetState(State.GetRemoveBuff);
                }
            }
        }

        if (statusId == 3263) // 聖竜の爪 red
        {
            var pc = _partyDataList.Find(x => x.EntityId == sourceId);
            if (pc != null)
            {
                pc.CrowColor = "";
                if (_aeloSubState == AeloSubState.GetDragon && pc.Mine)
                {
                    if (_maelstromCount >= 5)
                    {
                        _aeloSubState = AeloSubState.SetReturn;
                    }
                    else
                    {
                        _aeloSubState = AeloSubState.AvoidSouth;
                    }
                }
            }
        }

        if (statusId == 3264) // 聖竜の牙 blue
        {
            var pc = _partyDataList.Find(x => x.EntityId == sourceId);
            if (pc != null)
            {
                pc.CrowColor = "";
            }

            pc = GetMinedata();
            if (pc == null || pc.Object == null) return;

            if (pc.CrowColor == "" && pc.EntityId == sourceId)
            {
                SetState(State.SetReturn);
            }
        }
    }

    public override void OnUpdate()
    {
        if (_state == State.None) return;

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

        // Sprint, ArmsLength Automations
        do
        {
            if (C.Sprint && !_usedSprint)
            {
                var pc = GetMinedata();
                if (pc == null || pc.Object == null) break;

                // 赤+ブリザガ
                if (pc.CrowColor == "red" && pc.Gimmick == Gimmick.Ice3)
                {
                    var ice3 = pc.Object?.StatusList.FirstOrDefault(x => x.StatusId is 2462) ?? null;
                    if (ice3 == null) break;

                    if (ice3.RemainingTime > 3.0f) break;

                    if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                    {
                        if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, 3u))
                        {
                            DuoLog.Information("Sprint");
                            _usedSprint = true;
                        }
                    }
                    else
                    {
                        if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, 3u))
                        {
                            actionManager->UseAction(ActionType.Action, 3u);
                        }

                        if (!_usedSprint && actionManager->IsRecastTimerActive(ActionType.Action, 3u))
                        {
                            _usedSprint = true;
                        }
                    }
                }

                // 赤+エアロガ
                if (pc.CrowColor == "red" && pc.Gimmick == Gimmick.Aelo)
                {
                    var aelo = pc.Object?.StatusList.FirstOrDefault(x => x.StatusId is 4208) ?? null;
                    if (aelo == null) break;

                    if (aelo.RemainingTime > 17.0f) break;

                    if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                    {
                        if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, 3u))
                        {
                            DuoLog.Information("Sprint");
                            _usedSprint = true;
                        }
                    }
                    else
                    {
                        if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, 3u))
                        {
                            actionManager->UseAction(ActionType.Action, 3u);
                        }

                        if (!_usedSprint && actionManager->IsRecastTimerActive(ActionType.Action, 3u))
                        {
                            _usedSprint = true;
                        }
                    }
                }
            }
        } while (false);

        do
        {
            if (C.ArmsLength && !_usedArmsLength)
            {
                var pc = GetMinedata();
                if (pc == null || pc.Object == null) break;

                var return4Buff = pc.Object?.StatusList.FirstOrDefault(x => x.StatusId == 2452) ?? null;
                if (return4Buff == null) break;

                if (return4Buff.RemainingTime > 1.5f) break;

                if (HealerJobs.Contains(Player.Object.GetJob()) || MagicDpsJobs.Contains(Player.Object.GetJob()))
                {
                    // ヒーラー、魔法DPS
                    if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                    {
                        if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, 7559u))
                        {
                            DuoLog.Information("SureCast");
                            _usedSprint = true;
                        }
                    }
                    else
                    {
                        if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, 7559u))
                        {
                            actionManager->UseAction(ActionType.Action, 7559u);
                        }

                        if (!_usedSprint && actionManager->IsRecastTimerActive(ActionType.Action, 7559u))
                        {
                            _usedSprint = true;
                        }
                    }
                }
                else
                {
                    // タンク、近接DPS、遠隔DPS
                    if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                    {
                        if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, 7548u))
                        {
                            DuoLog.Information("ArmsLength");
                            _usedArmsLength = true;
                        }
                    }
                    else
                    {
                        if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, 7548u))
                        {
                            actionManager->UseAction(ActionType.Action, 7548u);
                        }

                        if (!_usedArmsLength && actionManager->IsRecastTimerActive(ActionType.Action, 7548u))
                        {
                            _usedArmsLength = true;
                        }
                    }

                }
            }
        } while (false);

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
        _state = State.None;
        _aeloSubState = AeloSubState.None;
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
        HideAllElements();
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Sprint", ref C.Sprint);
        ImGui.Checkbox("ArmsLength", ref C.ArmsLength);
        ImGui.Checkbox("FastRunRedIce3", ref C.FastRunRedIce3);
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"AeloSubState: {_aeloSubState}");
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
    private void SetState(State state)
    {
        HideAllElements();
        ResetCircleElement();
        _StateProcEnd = false;
        _StateProcEndCommon = false;
        _state = state;
    }

    private void AdjustRemoveBuff()
    {
        // RemoveBuffPosList が4つでなければ処理を中止
        if (_removeBuffPosList.Count != 4) return;

        // CrowColorが"blue"のエントリを抽出
        var blues = _partyDataList.Where(x => x.CrowColor == "blue").ToList();

        // 割り当て可能なRemoveBuffリストのコピーを作成
        var unassignedBuffs = new List<RemoveBuff>(_removeBuffPosList);

        foreach (var blue in blues)
        {
            // blue.Objectがnullの場合スキップ
            if (blue.Object == null) continue;

            // 最も近い未割り当てのRemoveBuffを探す
            var nearestBuff = unassignedBuffs
                .OrderBy(buff => Vector3.Distance(blue.Object.Position, buff.Position))
                .FirstOrDefault();

            if (nearestBuff == null) break;

            // EntityIdを割り当て
            nearestBuff.AssignEntityId = blue.EntityId;

            // 割り当て済みのBuffをリストから削除
            unassignedBuffs.Remove(nearestBuff);
        }
    }

    private void BlueElap() // 1人
    {
        if (_StateProcEnd) return;
        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;

        if (_state == State.HourGlassWater)
        {
            if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                ApplyElement("Bait", new Vector3(112f, 0, 85f));
            else
                ApplyElement("Bait", new Vector3(88f, 0, 85f));

            _StateProcEnd = true;
        }
        else if (_state == State.IceElapAelo)
        {
            if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                ApplyElement("Bait", new Vector3(112f, 0, 85f));
            else
                ApplyElement("Bait", new Vector3(88f, 0, 85f));

            _StateProcEnd = true;
        }
        else if (_state == State.holy)
        {
            if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                ApplyElement("Bait", new Vector3(112f, 0, 85f));
            else
                ApplyElement("Bait", new Vector3(88f, 0, 85f));

            _StateProcEnd = true;
        }
        else if (_state == State.GetRemoveBuff)
        {
            var removeBuff = _removeBuffPosList.Find(x => x.AssignEntityId == pc.EntityId);
            if (removeBuff != null)
            {
                ApplyElement("Bait", removeBuff.Position);
                _StateProcEnd = true;
            }
            else
            {
                ApplyElement("Bait", DirectionCalculator.Direction.North, 18f);
            }
        }
        else if (_state == State.SetReturn)
        {
            SetReturn();
        }
    }

    private void Blue() // 3人 (Water3, Holy, Ice3)
    {
        if (_StateProcEnd) return;
        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;

        if (_state == State.HourGlassWater)
        {
            if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                ApplyElement("Bait", new Vector3(88f, 0, 115f));
            else
                ApplyElement("Bait", new Vector3(112f, 0, 115f));
        }
        else if (_state == State.IceElapAelo)
        {
            if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                ApplyElement("Bait", new Vector3(91f, 0, 115.6f));
            else
                ApplyElement("Bait", new Vector3(109f, 0, 115.6f));

            if (Controller.TryGetElementByName("Line", out var el))
            {
                if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                {
                    var aelo = _partyDataList.Find(x => x.Gimmick == Gimmick.Aelo && x.AeloLR == "L");
                    if (aelo == null || aelo.Object == null) return;
                    var overDistance = Vector3.Distance(pc.Object.Position, aelo.Object.Position);
                    el.SetRefPosition(aelo.Object.Position);
                    var endPos = GetExtendedAndClampedPosition(aelo.Object.Position, pc.Object.Position, 34 + overDistance, 70f);
                    el.SetOffPosition(endPos);
                    el.Enabled = true;
                }
                else
                {
                    // NorthWest
                    var aelo = _partyDataList.Find(x => x.Gimmick == Gimmick.Aelo && x.AeloLR == "R");
                    if (aelo == null || aelo.Object == null) return;
                    var overDistance = Vector3.Distance(pc.Object.Position, aelo.Object.Position);
                    el.SetRefPosition(aelo.Object.Position);
                    var endPos = GetExtendedAndClampedPosition(aelo.Object.Position, pc.Object.Position, 34 + overDistance, 70f);
                    el.SetOffPosition(endPos);
                    el.Enabled = true;
                }
            }
        }
        else if (_state == State.holy)
        {
            if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                ApplyElement("Bait", new Vector3(112f, 0, 85f));
            else
                ApplyElement("Bait", new Vector3(88f, 0, 85f));
        }
        else if (_state == State.GetRemoveBuff)
        {
            var removeBuff = _removeBuffPosList.Find(x => x.AssignEntityId == pc.EntityId);
            if (removeBuff != null)
            {
                ApplyElement("Bait", removeBuff.Position);
                _StateProcEnd = true;
            }
            else
            {
                ApplyElement("Bait", DirectionCalculator.Direction.North, 18f);
            }
        }
        else if (_state == State.SetReturn)
        {
            SetReturn();
        }
    }

    private void RedAelo() // 2人
    {
        if (_StateProcEnd) return;
        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;

        if (_state == State.HourGlassWater)
        {
            if (pc.AeloLR == "L")
                ApplyElement("Bait", new Vector3(88f, 0, 115f));
            else
                ApplyElement("Bait", new Vector3(112f, 0, 115f));
        }
        else if (_state == State.IceElapAelo)
        {
            if (pc.AeloLR == "L")
                ApplyElement("Bait", new Vector3(90.008f, 0, 117.148f));
            else
                ApplyElement("Bait", new Vector3(109.881f, 0, 117.086f));
        }
        else if (_state is State.holy or State.GetRemoveBuff or State.SetReturn)
        {
            if (_aeloSubState == AeloSubState.None)
            {
                if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast && pc.AeloLR == "L")
                    _aeloSubState = AeloSubState.GetDragon;
                else if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast && pc.AeloLR == "R")
                    _aeloSubState = AeloSubState.Wait;
                else if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthWest && pc.AeloLR == "L")
                    _aeloSubState = AeloSubState.Wait;
                else if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthWest && pc.AeloLR == "R")
                    _aeloSubState = AeloSubState.GetDragon;
            }

            if (_aeloSubState == AeloSubState.Wait || _aeloSubState == AeloSubState.AvoidSouth)
            {
                ApplyElement("Bait", DirectionCalculator.Direction.South, 18f);
            }
            else if (_aeloSubState == AeloSubState.GetDragon)
            {
                if (pc.AeloLR == "L")
                {
                    if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthEast)
                        ApplyElement("Bait", new Vector3(90f, 0, 107f));
                    else
                        ApplyElement("Bait", new Vector3(90.5f, 0, 109f));
                }
                else
                {
                    if (_slowHourGlassDirection == DirectionCalculator.Direction.NorthWest)
                        ApplyElement("Bait", new Vector3(110f, 0, 107f));
                    else
                        ApplyElement("Bait", new Vector3(109.5f, 0, 109f));
                }
            }
            else if (_aeloSubState == AeloSubState.SetReturn)
            {
                _BeforeReturnProcDone = true;
                SetReturn();
            }
        }
    }

    private void RedIce3() // 2人
    {
        if (_StateProcEnd) return;
        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;

        if (_state == State.HourGlassWater)
        {
            if (pc.Ice3LR == "L")
                ApplyElement("Bait", new Vector3(87f, 0, 100f));
            else
                ApplyElement("Bait", new Vector3(113f, 0, 100f));
        }
        else if (_state == State.IceElapAelo)
        {
            if (pc.Ice3LR == "L")
                ApplyElement("Bait", new Vector3(87f, 0, 100f));
            else
                ApplyElement("Bait", new Vector3(113f, 0, 100f));
        }
        else if (_state == State.holy)
        {
            ApplyElement("Bait", DirectionCalculator.Direction.North, 18f);
            if (C.FastRunRedIce3)
            {
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

                _runEndTime = Environment.TickCount64 + 3000;
            }
            _StateProcEnd = true;
        }
        else if (_state == State.SetReturn)
        {
            SetReturn();
        }
        else if (_state == State.GetRemoveBuff)
        {
            var removeBuff = _removeBuffPosList.Find(x => x.AssignEntityId == pc.EntityId);
            if (removeBuff != null)
            {
                ApplyElement("Bait", removeBuff.Position);
                _StateProcEnd = true;
            }
        }
    }

    private void CommonUpdate()
    {
        if (_StateProcEndCommon) return;

        if (_state == State.HourGlassWater)
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
        else if (_state == State.IceElapAelo)
        {
            // Dark Blizzard III
            var ice = _partyDataList.FirstOrDefault(x => x.Gimmick == Gimmick.Ice3);
            if (ice != null && Controller.TryGetElementByName("CircleFixed0", out var el) && ice.Object != null)
            {
                el.SetRefPosition(ice.Object.Position);
                el.color = 0xC80000FF; // red
                el.radius = 3.0f;
                el.Donut = 12.0f;
                el.Enabled = true;
            }

            var anotherIce = _partyDataList.FirstOrDefault(
                x => x.Gimmick == Gimmick.Ice3 && ice != null && x.EntityId != ice.EntityId);
            if (anotherIce != null && Controller.TryGetElementByName("CircleFixed1", out el) && anotherIce.Object != null)
            {
                el.SetRefPosition(anotherIce.Object.Position);
                el.color = 0xC80000FF; // red
                el.radius = 3.0f;
                el.Donut = 12.0f;
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
        else if (_state == State.holy)
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
        else if (_state == State.SetReturn)
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
    }

    private void SetReturn()
    {
        if (!_BeforeReturnProcDone)
        {
            ApplyElement("Bait", DirectionCalculator.Direction.North, 18f);
            return;
        }

        var pc = GetMinedata();
        if (pc == null || pc.Object == null) return;

        if (_firstKnockback == DirectionCalculator.Direction.None ||
            _secondKnockback == DirectionCalculator.Direction.None)
        {
            ApplyElement("Bait", new Vector3(100f, 0, 100f));
            return;
        }

        if (pc.Index == 0)
        {
            if (_secondKnockback == DirectionCalculator.Direction.North)
                ApplyElement("Bait", new Vector3(100f, 0, 88f));
            else
                ApplyElement("Bait", new Vector3(100f, 0, 112f));
        }
        else if (pc.Index == 1)
        {
            if (_secondKnockback == DirectionCalculator.Direction.North)
            {
                if (_firstKnockback == DirectionCalculator.Direction.East)
                    ApplyElement("Bait", new Vector3(107f, 0, 95f));
                else
                    ApplyElement("Bait", new Vector3(93f, 0, 95f));
            }
            else
            {
                if (_firstKnockback == DirectionCalculator.Direction.East)
                    ApplyElement("Bait", new Vector3(107f, 0, 105f));
                else
                    ApplyElement("Bait", new Vector3(93f, 0, 105f));
            }
        }
        else
        {
            if (_secondKnockback == DirectionCalculator.Direction.North)
                ApplyElement("Bait", new Vector3(100f, 0, 95f));
            else
                ApplyElement("Bait", new Vector3(100f, 0, 105f));
        }

        _StateProcEnd = true;
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
    #endregion
}
