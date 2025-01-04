using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
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
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten.FullToolerPartyOnlyScrtipts;
internal unsafe class P3_Apocalypse_Full_Toolers :SplatoonScript
{
    #region types
    /********************************************************************/
    /* types                                                            */
    /********************************************************************/
    private enum State
    {
        None = 0,
        GimmickStart,
        Stack1,
        Split1,
        Split2,
        Stack2,
        TankAttack,
        Knockback
    }
    #endregion

    #region class
    /******************************************************** ************/
    /* class                                                            */
    /********************************************************************/
    public class Config :IEzConfig
    {
        public bool Master = false;
        public bool UseFastCheat = false;
        public float FastCheatDefault = 1.0f;
        public float FastCheat = 1.5f;
    }

    private class PartyData
    {
        public int Index = 0;
        public bool Mine = false;
        public uint EntityId = 0;
        public int DebuffTime = 0;
        public IPlayerCharacter? Object => (IPlayerCharacter)this.EntityId.GetObject() ?? null;
        public string MTSTGroup = "None";

        public bool IsTank => TankJobs.Contains(Object?.GetJob() ?? Job.WHM);
        public bool IsHealer => HealerJobs.Contains(Object?.GetJob() ?? Job.PLD);
        public bool IsMeleeDps => MeleeDpsJobs.Contains(Object?.GetJob() ?? Job.MCH);
        public bool IsRangedDps => RangedDpsJobs.Contains(Object?.GetJob() ?? Job.MNK);

        public PartyData(uint entityId, int index)
        {
            EntityId = entityId;
            Index = index;
            Mine = this.EntityId == Player.Object.EntityId;
        }
    }
    #endregion

    #region const
    /********************************************************************/
    /* const                                                            */
    /********************************************************************/
    private readonly Vector3[] elementPos = {
        // south (基準点: (100,0,108))
        new Vector3(99f, 0f, 107.2f),
        new Vector3(101f, 0f, 107.2f),
        new Vector3(99f, 0f, 109.2f),
        new Vector3(101f, 0f, 109.2f),

        // north (基準点: (100,0,92))
        new Vector3(99f, 0f, 92.8f),
        new Vector3(101f, 0f, 92.8f),
        new Vector3(99f, 0f, 90.8f),
        new Vector3(101f, 0f, 90.8f),
    };
    #endregion

    #region public properties
    /********************************************************************/
    /* public properties                                                */
    /********************************************************************/
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(11, "redmoon");
    #endregion

    #region private properties
    /********************************************************************/
    /* private properties                                               */
    /********************************************************************/
    private State _state = State.None;
    private List<PartyData> _partyDataList = new();
    private int _DebuffCount = 0;
    private DirectionCalculator.Direction _startDirection = DirectionCalculator.Direction.None;
    private bool _isClockwise = false;
    private ClockDirectionCalculator _clockDirectionCalculator = new(DirectionCalculator.Direction.None);
    private uint _gaiaEntityId = 0;
    #endregion

    #region public methods
    /********************************************************************/
    /* public methods                                                    */
    /********************************************************************/
    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0) { tether = true, radius = 3f, thicc = 6f });
        Controller.RegisterElement("PrevBait", new Element(0) { tether = true, radius = 3f, thicc = 6f, color = 0x400000FF, fillIntensity = 0.2f });
        Controller.RegisterElement("BaitObject", new Element(1) { tether = true, refActorComparisonType = 2, radius = 0.5f, thicc = 6f });
        for (var i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"Circle{i}", new Element(1) { radius = 5.0f, refActorComparisonType = 2, thicc = 2f, fillIntensity = 0.2f });
        }
        for (var i = 0; i < 7; i++)
        {
            Controller.RegisterElement($"2Circle{i}", new Element(0) { radius = 5.0f, refActorComparisonType = 2, thicc = 2f, fillIntensity = 0.5f });
        }
        Controller.RegisterElement($"Line", new Element(2) { radius = 5.0f, thicc = 2f, fillIntensity = 0.5f });

        for (var i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"split{i}", new Element(0)
            {
                radius = 0.3f,
                refActorComparisonType = 2,
                thicc = 2f,
                fillIntensity = 0.5f,
                color = 0xC8FF00FF,
                refX = elementPos[i].X,
                refY = elementPos[i].Z,
                refZ = elementPos[i].Y,
            });
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 40269)
        {
            SetListEntityIdByJob();
            //_partyDataList[0].Mine = false;
            //_partyDataList[1].Mine = false;
            //_partyDataList[2].Mine = true;
            //_partyDataList[3].Mine = false;
            //_partyDataList[4].Mine = false;
            //_partyDataList[5].Mine = false;
            //_partyDataList[6].Mine = false;
            //_partyDataList[7].Mine = false;
            _state = State.GimmickStart;
        }
        if (_state == State.None) return;
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, ulong targetId, byte replaying)
    {
        if (_state == State.None) return;
        if (command == 413 && p1 == 4 && (p2 == 16 || p2 == 64) && _startDirection == DirectionCalculator.Direction.None)
        {
            if (sourceId.TryGetObject(out var obj))
            {
                _startDirection = DirectionCalculator.DividePoint(obj.Position, 14f, new Vector3(100, 0, 100)) switch
                {
                    DirectionCalculator.Direction.South => DirectionCalculator.Direction.North,
                    DirectionCalculator.Direction.SouthEast => DirectionCalculator.Direction.NorthWest,
                    DirectionCalculator.Direction.East => DirectionCalculator.Direction.West,
                    DirectionCalculator.Direction.SouthWest => DirectionCalculator.Direction.NorthEast,
                    _ => DirectionCalculator.DividePoint(obj.Position, 14f, new Vector3(100, 0, 100))
                };

                _clockDirectionCalculator = new(_startDirection);

                _isClockwise = p2 switch
                {
                    16 => true,  // 時計回り
                    64 => false, // 反時計回り
                    _ => false,
                };
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action == null) return;
        var castId = set.Action.Value.RowId;

        if (castId == 40271 && _state == State.Stack1)
        {
            HideAllElements();
            if (Controller.GetConfig<Config>().UseFastCheat)
            {
                Chat.Instance.ExecuteCommand($"/pdrspeed {Controller.GetConfig<Config>().FastCheat}");
            }
            ShowSplit1Pos(true);
            _state = State.Split1;
        }
        if (castId == 40271 && _state == State.Stack2)
        {
            HideAllElements();
            ShowTankAttackPos();
            _state = State.TankAttack;
        }
        if (castId == 40288 && _state == State.Split1 && _startDirection != DirectionCalculator.Direction.None)
        {
            HideAllElements();
            ShowAoePos();
            ShowSplit2Pos();
            _state = State.Split2;
        }
        if (castId == 40274 && _state == State.Split2)
        {
            HideAllElements();
            ShowStack2Pos();
            _state = State.Stack2;
        }
        if (castId == 40181 && _state == State.TankAttack)
        {
            HideAllElements();
            if (set.Target is IBattleNpc npc)
            {
                ShowKnockbackPos(npc);
                _state = State.Knockback;
            }
            else
            {
                PluginLog.Error("Target is not IBattleNpc");
                _state = State.None;
            }
        }
        if (castId == 40264 && _state == State.Knockback)
        {
            this.OnReset();
        }
    }

    public override void OnUpdate()
    {
        if (_state == State.None) return;

        if (Controller.TryGetElementByName("Bait", out var el))
        {
            if (el.Enabled) el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }

        if (Controller.TryGetElementByName("BaitObject", out el))
        {
            if (el.Enabled) el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }

        if (Controller.TryGetElementByName("Line", out el) && _gaiaEntityId.TryGetObject(out var obj) && el.Enabled == true)
        {
            var overDistance = Vector3.Distance(Player.Position, obj.Position);
            el.SetRefPosition(obj.Position);
            var endPos = GetExtendedAndClampedPosition(obj.Position, Player.Position, 15 + overDistance, 50f);
            el.SetOffPosition(endPos);
        }
    }

    public override void OnReset()
    {
        var c = Controller.GetConfig<Config>();
        _state = State.None;
        _DebuffCount = 0;
        _startDirection = DirectionCalculator.Direction.None;
        _isClockwise = false;
        _clockDirectionCalculator = new(DirectionCalculator.Direction.None);
        _gaiaEntityId = 0;
        _partyDataList.Clear();
        HideAllElements();

        if (Controller.GetConfig<Config>().UseFastCheat)
        {
            Chat.Instance.ExecuteCommand($"/pdrspeed {Controller.GetConfig<Config>().FastCheatDefault}");
        }

        if (!c.Master) return;
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

    public override void OnGainBuffEffect(uint sourceId, Status Status)
    {
        if (_state == State.None) return;
        if (Status.StatusId == 2461)
        {
            _DebuffCount++;
            var pc = _partyDataList.Find(x => x.EntityId == sourceId);
            if (pc == null) return;

            pc.DebuffTime = Status.RemainingTime switch
            {
                >= 35 => 40,
                (>= 25) and (<= 35) => 30,
                (>= 5) and (<= 15) => 10,
                _ => 0,
            };

            if (pc.DebuffTime == 0) return;

            if (_DebuffCount == 6)
            {
                if (ParseDebuff())
                {
                    HideAllElements();
                    ShowStack1Pos();
                    _state = State.Stack1;
                }
                else _state = State.None;
            }
        }
    }

    public override void OnSettingsDraw()
    {
        var C = Controller.GetConfig<Config>();
        ImGui.Checkbox("Master", ref C.Master);
        ImGui.Checkbox("Use Fast Cheat", ref C.UseFastCheat);
        if (C.UseFastCheat)
        {
            ImGui.SliderFloat("Fast Cheat", ref C.FastCheat, 1.0f, 2.0f);
            ImGui.SliderFloat("Fast Cheat Default", ref C.FastCheatDefault, 1.0f, 2.0f);
        }
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"DebuffCount: {_DebuffCount}");
            ImGui.Text($"Clockwise: {_isClockwise}");
            ImGui.Text($"StartDirection: {_startDirection}");
            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach (var x in _partyDataList)
            {
                IPlayerCharacter? pcObj = x.EntityId.GetObject() as IPlayerCharacter ?? null;
                if (pcObj == null) continue;

                Entries.Add(new ImGuiEx.EzTableEntry("Index", () => ImGui.Text(x.Index.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Mine", () => ImGui.Text(x.Mine.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("EntityId", () => ImGui.Text(x.EntityId.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Job", () => ImGui.Text(pcObj.GetJob().ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("DebuffTime", () => ImGui.Text(x.DebuffTime.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("MTSTGroup", () => ImGui.Text(x.MTSTGroup)));
            }
            ImGuiEx.EzTable(Entries);
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

    private bool ParseDebuff()
    {
        var mine = GetMinedata();
        if (mine == null) return false;

        _partyDataList[0].MTSTGroup = "MT";
        _partyDataList[1].MTSTGroup = "ST";

        foreach (var pc in _partyDataList)
        {
            if (pc.MTSTGroup != "None") continue;
            if (pc.DebuffTime == 40)
            {
                if (_partyDataList.Any(x => x.DebuffTime == 40 && x.MTSTGroup == "MT")) pc.MTSTGroup = "ST";
                else pc.MTSTGroup = "MT";
            }
            else if (pc.DebuffTime == 30)
            {
                if (_partyDataList.Any(x => x.DebuffTime == 30 && x.MTSTGroup == "MT")) pc.MTSTGroup = "ST";
                else pc.MTSTGroup = "MT";
            }
            else if (pc.DebuffTime == 10)
            {
                if (_partyDataList.Any(x => x.DebuffTime == 10 && x.MTSTGroup == "MT")) pc.MTSTGroup = "ST";
                else pc.MTSTGroup = "MT";
            }
            else
            {
                if (_partyDataList.Any(x => x.DebuffTime == 0 && x.MTSTGroup == "MT")) pc.MTSTGroup = "ST";
                else pc.MTSTGroup = "MT";
            }
        }

        if (_partyDataList.Any(x => x.MTSTGroup == "None")) return false;

        // Set Head Marker
        var MTs = _partyDataList.Where(x => x.MTSTGroup == "MT").ToList();
        var STs = _partyDataList.Where(x => x.MTSTGroup == "ST").ToList();
        var C = Controller.GetConfig<Config>();
        if (!C.Master) return true;
        for (var i = 0; i < 4; i++)
        {
            if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
            {
                int index = GetPlayerTag(MTs[i].EntityId);
                if (index != -1) DuoLog.Information($"Name: {MTs[i].Object?.Name} Command: /mk attack <{index}>");

                //if (i < 2) // マーカー２つしかない
                //{
                //    index = GetPlayerTag(STs[i].EntityId);
                //    if (index != -1) DuoLog.Information($"Name: {STs[i].Object?.Name} Command: /mk bind <{index}>");
                //}
                //else
                //{
                //    index = GetPlayerTag(STs[i].EntityId);
                //    if (index != -1) DuoLog.Information($"Name: {STs[i].Object?.Name} Command: /mk stop <{index}>");
                //}
            }
            else
            {
                int index = GetPlayerTag(MTs[i].EntityId);
                if (index != -1) Chat.Instance.ExecuteCommand($"/mk attack <{index}>");

                if (i < 2) // マーカー２つしかない
                {
                    index = GetPlayerTag(STs[i].EntityId);
                    if (index != -1) Chat.Instance.ExecuteCommand($"/mk bind <{index}>");
                }
                else
                {
                    index = GetPlayerTag(STs[i].EntityId);
                    if (index != -1) Chat.Instance.ExecuteCommand($"/mk stop <{index}>");
                }
            }
        }
        return true;
    }

    private void ShowStack1Pos()
    {
        var pc = GetMinedata();
        if (pc == null) return;

        if (pc.MTSTGroup == "MT")
        {
            ApplyElement("Bait", DirectionCalculator.Direction.North, 8.0f);
        }
        else
        {
            ApplyElement("Bait", DirectionCalculator.Direction.South, 8.0f);
        }

        var Debuff10s = _partyDataList.Where(x => x.DebuffTime == 10).ToList();
        if (Debuff10s.Count == 0) return;

        for (var i = 0; i < 2; i++)
        {
            if (Controller.TryGetElementByName($"Circle{i}", out var el))
            {
                el.refActorObjectID = Debuff10s[i].EntityId;
                el.thicc = 6f;
                el.fillIntensity = 0.5f;
                el.color = 0xC000FF00;
                el.radius = 6.0f;
                el.Filled = false;
                el.Enabled = true;
            }
        }

        for (var i = 0; i < 8; i++)
        {
            if (Controller.TryGetElementByName($"split{i}", out var el))
            {
                el.Enabled = true;
            }
        }

        ShowSplit1Pos(false);
    }

    private void ShowSplit1Pos(bool isGuide)
    {
        //var pc = GetMinedata();
        //if (pc == null) return;

        //string MTST = pc.MTSTGroup;
        //if (MTST == "None") return;

        //var MTSTs = _partyDataList.Where(x => x.MTSTGroup == MTST).ToList();
        //if (MTSTs.Count != 4) return;

        //// その中で何番目かを取得
        //var index = MTSTs.FindIndex(x => x.EntityId == pc.EntityId);
        //if (index == -1) return;

        //if (isGuide)
        //{
        //    if (MTST == "MT")
        //    {
        //        if (index == 0)
        //        {
        //            ApplyElement("Bait", DirectionCalculator.Direction.NorthEast, 7.0f);
        //        }
        //        else if (index == 1)
        //        {
        //            ApplyElement("Bait", DirectionCalculator.Direction.NorthWest, 7.0f);
        //        }
        //        else if (index == 2)
        //        {
        //            ApplyElement("Bait", DirectionCalculator.Direction.NorthEast, 14.0f);
        //        }
        //        else if (index == 3)
        //        {
        //            ApplyElement("Bait", DirectionCalculator.Direction.NorthWest, 14.0f);
        //        }
        //    }
        //    else
        //    {
        //        if (index == 0)
        //        {
        //            ApplyElement("Bait", DirectionCalculator.Direction.SouthEast, 7.0f);
        //        }
        //        else if (index == 1)
        //        {
        //            ApplyElement("Bait", DirectionCalculator.Direction.SouthWest, 7.0f);
        //        }
        //        else if (index == 2)
        //        {
        //            ApplyElement("Bait", DirectionCalculator.Direction.SouthEast, 14.0f);
        //        }
        //        else if (index == 3)
        //        {
        //            ApplyElement("Bait", DirectionCalculator.Direction.SouthWest, 14.0f);
        //        }
        //    }
        //}
        //else
        //{
        //    if (MTST == "MT")
        //    {
        //        if (index == 0)
        //        {
        //            ApplyElement("PrevBait", DirectionCalculator.Direction.NorthEast, 7.0f);
        //        }
        //        else if (index == 1)
        //        {
        //            ApplyElement("PrevBait", DirectionCalculator.Direction.NorthWest, 7.0f);
        //        }
        //        else if (index == 2)
        //        {
        //            ApplyElement("PrevBait", DirectionCalculator.Direction.NorthEast, 14.0f);
        //        }
        //        else if (index == 3)
        //        {
        //            ApplyElement("PrevBait", DirectionCalculator.Direction.NorthWest, 14.0f);
        //        }
        //    }
        //    else
        //    {
        //        if (index == 0)
        //        {
        //            ApplyElement("PrevBait", DirectionCalculator.Direction.SouthEast, 7.0f);
        //        }
        //        else if (index == 1)
        //        {
        //            ApplyElement("PrevBait", DirectionCalculator.Direction.SouthWest, 7.0f);
        //        }
        //        else if (index == 2)
        //        {
        //            ApplyElement("PrevBait", DirectionCalculator.Direction.SouthEast, 14.0f);
        //        }
        //        else if (index == 3)
        //        {
        //            ApplyElement("PrevBait", DirectionCalculator.Direction.SouthWest, 14.0f);
        //        }
        //    }
        //}
    }

    private void ShowSplit2Pos()
    {
        var pc = GetMinedata();
        if (pc == null) return;

        // その中で何番目かを取得

        var MTs = _partyDataList.Where(x => x.MTSTGroup == "MT").ToList();
        var STs = _partyDataList.Where(x => x.MTSTGroup == "ST").ToList();

        // 自分がどちらか、何番目かを取得
        bool isMT = pc.MTSTGroup == "MT";
        int index = isMT ? MTs.FindIndex(x => x.EntityId == pc.EntityId) : STs.FindIndex(x => x.EntityId == pc.EntityId);
        if (index == -1) return;

        Vector3 pos1 = CalculatePositionFromDirection(_clockDirectionCalculator.GetDirectionFromClock(_isClockwise ? 4 : 7), 10.0f);
        Vector3 pos2 = CalculatePositionFromDirection(_clockDirectionCalculator.GetDirectionFromClock(_isClockwise ? 10 : 1), 10.0f);

        float distance1 = Vector3.Distance(_partyDataList[0].Object.Position, pos1);
        float distance2 = Vector3.Distance(_partyDataList[0].Object.Position, pos2);

        int MTsClock = 0;
        int MTsClockDps = 0;
        int STsClock = 0;
        int STsClockDps = 0;

        if (distance1 < distance2)
        {
            MTsClock = _isClockwise ? 4 : 7;
            MTsClockDps = _isClockwise ? 3 : 9;
            STsClock = _isClockwise ? 10 : 1;
            STsClockDps = _isClockwise ? 9 : 3;
        }
        else
        {
            MTsClock = _isClockwise ? 10 : 1;
            MTsClockDps = _isClockwise ? 9 : 3;
            STsClock = _isClockwise ? 4 : 7;
            STsClockDps = _isClockwise ? 3 : 9;
        }

        if (isMT)
        {
            if (index == 0)
            {
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(MTsClock), 10.0f);
            }
            else if (index == 1)
            {
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(MTsClockDps), 10.0f);
            }
            else if (index == 2)
            {
                ApplyElement("Bait", DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(MTsClock)) + (_isClockwise ? -15 : 15), 19.0f);
            }
            else if (index == 3)
            {
                ApplyElement("Bait", DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(MTsClock)) + (_isClockwise ? 15 : -15), 19.0f);
            }
        }
        else
        {
            if (index == 0)
            {
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(STsClock), 10.0f);
            }
            else if (index == 1)
            {
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(STsClockDps), 10.0f);
            }
            else if (index == 2)
            {
                ApplyElement("Bait", DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(STsClock)) + (_isClockwise ? -15 : 15), 19.0f);
            }
            else if (index == 3)
            {
                ApplyElement("Bait", DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(STsClock)) + (_isClockwise ? 15 : -15), 19.0f);
            }
        }


        //switch (index)
        //{
        //    case 0:
        //        ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(_isClockwise ? 4 : 7), 10.0f);
        //        break;
        //    case 1:
        //        ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(_isClockwise ? 10 : 1), 10.0f);
        //        break;
        //    case 2:
        //        ApplyElement("Bait", DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(_isClockwise ? 4 : 7)) + (_isClockwise ? -15 : 15), 19.0f);
        //        break;
        //    case 3:
        //        ApplyElement("Bait", DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(_isClockwise ? 10 : 1)) + (_isClockwise ? -15 : 15), 19.0f);
        //        break;
        //    case 4:
        //        ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(_isClockwise ? 3 : 9), 10.0f);
        //        break;
        //    case 5:
        //        ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(_isClockwise ? 9 : 3), 10.0f);
        //        break;
        //    case 6:
        //        ApplyElement("Bait", DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(_isClockwise ? 4 : 7)) + (_isClockwise ? 15 : -15), 19.0f);
        //        break;
        //    case 7:
        //        ApplyElement("Bait", DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(_isClockwise ? 10 : 1)) + (_isClockwise ? 15 : -15), 19.0f);
        //        break;
        //}

        ShowAoePos();
    }

    private void ShowAoePos()
    {
        DirectionCalculator.Direction oppsiteDirection = DirectionCalculator.GetOppositeDirection(_startDirection);

        for (int i = 0; i < 3; i++)
        {
            int angle = (int)(_startDirection + ((_isClockwise ? 45 : -45) * i)) % 360;
            ApplyElement($"2Circle{i}", DirectionCalculator.GetDirectionFromAngle(_startDirection, angle), 14f, 9f, tether: false);
        }

        for (int i = 0; i < 3; i++)
        {
            int angle = (int)(oppsiteDirection + ((_isClockwise ? 45 : -45) * i)) % 360;
            ApplyElement($"2Circle{i + 3}", DirectionCalculator.GetDirectionFromAngle(oppsiteDirection, angle), 14f, 9f, tether: false);
        }

        ApplyElement("2Circle6", _startDirection, 0f, 9f, false);
    }

    private void ShowStack2Pos()
    {
        var pc = GetMinedata();
        if (pc == null) return;

        Vector3 pos1 = CalculatePositionFromDirection(_clockDirectionCalculator.GetDirectionFromClock(_isClockwise ? 4 : 7), 10.0f);
        Vector3 pos2 = CalculatePositionFromDirection(_clockDirectionCalculator.GetDirectionFromClock(_isClockwise ? 10 : 1), 10.0f);

        float distance1 = Vector3.Distance(_partyDataList[0].Object.Position, pos1);
        float distance2 = Vector3.Distance(_partyDataList[0].Object.Position, pos2);

        int MTsClock = 0;
        int STsClock = 0;
        if (distance1 < distance2)
        {
            MTsClock = _isClockwise ? 4 : 7;
            STsClock = _isClockwise ? 10 : 1;
        }
        else
        {
            MTsClock = _isClockwise ? 10 : 1;
            STsClock = _isClockwise ? 4 : 7;
        }

        string MTST = pc.MTSTGroup;
        if (MTST == "None") return;

        ApplyElement("Bait", DirectionCalculator.GetAngle(
            _clockDirectionCalculator.GetDirectionFromClock((MTST == "MT") ? MTsClock : STsClock)) + (_isClockwise ? -22f : 22f), 5.2f);

        var Debuff30s = _partyDataList.Where(x => x.DebuffTime == 30).ToList();
        if (Debuff30s.Count == 0) return;

        for (var i = 0; i < 2; i++)
        {
            if (Controller.TryGetElementByName($"Circle{i}", out var el))
            {
                el.refActorObjectID = Debuff30s[i].EntityId;
                el.thicc = 6f;
                el.fillIntensity = 0.5f;
                el.color = 0xC000FF00;
                el.radius = 6.0f;
                el.Enabled = true;
            }
        }
        ShowAoe2Pos();
    }

    private void ShowAoe2Pos()
    {
        DirectionCalculator.Direction oppsiteDirection = DirectionCalculator.GetOppositeDirection(_startDirection);

        for (int i = 0; i < 3; i++)
        {
            int angle = (int)(_startDirection + (45 * (i + 2))) % 360;
            ApplyElement($"2Circle{i}", DirectionCalculator.GetDirectionFromAngle(_startDirection, angle), 14f, 9f, tether: false);
        }

        for (int i = 0; i < 3; i++)
        {
            int angle = (int)(oppsiteDirection + (45 * (i + 2))) % 360;
            ApplyElement($"2Circle{i + 3}", DirectionCalculator.GetDirectionFromAngle(oppsiteDirection, angle), 14f, 9f, tether: false);
        }
    }

    private void ShowTankAttackPos()
    {
        var pc = GetMinedata();
        if (pc == null) return;

        if (pc.Index == 1)
        {
            Vector3 pos3 = CalculatePositionFromDirection(_clockDirectionCalculator.GetDirectionFromClock(3), 19.0f);
            Vector3 pos9 = CalculatePositionFromDirection(_clockDirectionCalculator.GetDirectionFromClock(9), 19.0f);
            float distance3 = Vector3.Distance(Player.Position, pos3);
            float distance9 = Vector3.Distance(Player.Position, pos9);
            if (distance3 < distance9)
            {
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(3), 19.0f);
            }
            else
            {
                ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(9), 19.0f);
            }
        }
        else
        {
            ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(1), 0.0f);
        }

        ShowAoe3Pos();
    }

    private void ShowAoe3Pos()
    {
        DirectionCalculator.Direction oppsiteDirection = DirectionCalculator.GetOppositeDirection(_startDirection);

        for (int i = 0; i < 3; i++)
        {
            int angle = (int)(_startDirection + (45 * (i + 3))) % 360;
            ApplyElement($"2Circle{i}", DirectionCalculator.GetDirectionFromAngle(_startDirection, angle), 14f, 9f, tether: false);
        }

        for (int i = 0; i < 3; i++)
        {
            int angle = (int)(oppsiteDirection + (45 * (i + 3))) % 360;
            ApplyElement($"2Circle{i + 3}", DirectionCalculator.GetDirectionFromAngle(oppsiteDirection, angle), 14f, 9f, tether: false);
        }
    }

    private void ShowKnockbackPos(IBattleNpc npc)
    {
        var pc = GetMinedata();
        if (pc == null) return;

        _gaiaEntityId = npc.EntityId;

        string MTST = pc.MTSTGroup;
        if (MTST == "None") return;

        if (Controller.TryGetElementByName("BaitObject", out var el))
        {
            el.refActorObjectID = npc.EntityId;
            el.includeRotation = true;
            el.offY = 2f;
            el.AdditionalRotation = MathHelper.DegToRad((MTST == "MT") ? 160 : 200);
            el.Enabled = true;
        }

        //if (Controller.TryGetElementByName("Line", out el))
        //{
        //    el.thicc = 6f;
        //    el.radius = 0f;
        //    el.color = 0xC8FF00FF;
        //    var overDistance = Vector3.Distance(Player.Position, npc.Position);
        //    el.SetRefPosition(npc.Position);
        //    var endPos = GetExtendedAndClampedPosition(npc.Position, Player.Position, 15 + overDistance, 50f);
        //    el.SetOffPosition(endPos);
        //    el.Enabled = true;
        //}
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
        Job.WHM,
        Job.AST,
        Job.SCH,
        Job.SGE,
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

            // clockの値に応じて方向オフセット(ステップ)を定義
            // 30度刻みのclockを45度刻みの8方向へ近似的にマッピング
            // 0:同方向、1,2:右斜め上、3:右、4,5:右斜め下、6:反対、7,8:左斜め下、9:左、10,11:左斜め上
            // ※ ここを修正ポイント
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

    private Vector3 BasePosition => new Vector3(100, 0, 100);

    private Vector3 CalculatePositionFromAngle(float angle, float radius = 0f)
    {
        return BasePosition + (radius * new Vector3(
            MathF.Cos(MathF.PI * angle / 180f),
            0,
            MathF.Sin(MathF.PI * angle / 180f)
        ));
    }

    private Vector3 CalculatePositionFromDirection(DirectionCalculator.Direction direction, float radius = 0f)
    {
        var angle = DirectionCalculator.GetAngle(direction);
        return CalculatePositionFromAngle(angle, radius);
    }

    /// <summary>
    /// Elementへの実適用処理を行う"大元"のメソッド。
    /// </summary>
    private void InternalApplyElement(Element element, Vector3 position, float elementRadius, bool filled, bool tether)
    {
        element.Enabled = true;
        element.radius = elementRadius;
        element.tether = tether;
        element.Filled = filled;
        element.SetRefPosition(position);
    }

    //----------------------- 公開ApplyElementメソッド群 -----------------------

    // Elementインスタンスと直接的な座標指定
    public void ApplyElement(Element element, Vector3 position, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        InternalApplyElement(element, position, elementRadius, filled, tether);
    }

    // Elementインスタンスと角度指定
    public void ApplyElement(Element element, float angle, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        var position = CalculatePositionFromAngle(angle, radius);
        InternalApplyElement(element, position, elementRadius, filled, tether);
    }

    // Elementインスタンスと方向指定
    public void ApplyElement(Element element, DirectionCalculator.Direction direction, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        var position = CalculatePositionFromDirection(direction, radius);
        InternalApplyElement(element, position, elementRadius, filled, tether);
    }

    // Element名と直接的な座標指定
    public void ApplyElement(string elementName, Vector3 position, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            InternalApplyElement(element, position, elementRadius, filled, tether);
        }
    }

    // Element名と角度指定
    public void ApplyElement(string elementName, float angle, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            var position = CalculatePositionFromAngle(angle, radius);
            InternalApplyElement(element, position, elementRadius, filled, tether);
        }
    }

    // Element名と方向指定
    public void ApplyElement(string elementName, DirectionCalculator.Direction direction, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            var position = CalculatePositionFromDirection(direction, radius);
            InternalApplyElement(element, position, elementRadius, filled, tether);
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

    private static Vector3 GetExtendedAndClampedPosition(
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

    private static void ExceptionReturn(string message)
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
