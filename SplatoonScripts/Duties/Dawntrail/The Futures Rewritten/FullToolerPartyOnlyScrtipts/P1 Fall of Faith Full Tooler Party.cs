using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten.FullToolerPartyOnlyScrtipts;
internal class P1_Fall_of_Faith_Full_Tooler_Party :SplatoonScript
{
    #region Enums
    enum State
    {
        None,
        Casting,
        Soil1End,
        Soil2End,
        Soil3End,
        End
    }

    enum LR
    {
        Left,
        Right
    }

    enum FireThunder
    {
        Fire,
        Thunder
    }
    #endregion

    #region class
    class PartyData
    {
        public uint EntityId;
        public LR LR;
        public int PriorityNum = 0;
        public int TetherNum = 0;
        public PartyData(uint entityId)
        {
            EntityId = entityId;
            LR = LR.Left;
            PriorityNum = 0;
            TetherNum = 0;
        }
    }
    #endregion

    #region public Fields
    public override HashSet<uint>? ValidTerritories { get; } = [1238];
    public override Metadata? Metadata => new(7, "Redmoon");

    public class Config :IEzConfig
    {
        public PriorityData LRPriority = new();
        public PriorityData NoBufferPriority = new();
    }
    #endregion

    #region private Fields
    Config C => this.Controller.GetConfig<Config>();
    List<PartyData> _partyDatas = new();
    List<FireThunder> _fireThunders = new();
    State _state = State.None;
    int _tetherCount = 0;
    int _soilEndCount = 0;
    bool _gimmickEnded = false;
    #endregion

    #region Public Methods
    public override void OnSetup()
    {
        Controller.RegisterElement("LeftTether", new Splatoon.Element(0) { refX = 94.0f, refY = 100.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("LeftTetherNext", new Splatoon.Element(0) { refX = 94.0f, refY = 98.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("LeftNone1", new Splatoon.Element(0) { refX = 94.0f, refY = 102.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("LeftNone2", new Splatoon.Element(0) { refX = 92.0f, refY = 100.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("RightTether", new Splatoon.Element(0) { refX = 106.0f, refY = 100.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("RightTetherNext", new Splatoon.Element(0) { refX = 106.0f, refY = 98.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("RightNone1", new Splatoon.Element(0) { refX = 106.0f, refY = 102.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("RightNone2", new Splatoon.Element(0) { refX = 108.0f, refY = 100.0f, thicc = 10.0f, tether = true, radius = 0.5f });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (!_gimmickEnded && _state == State.None && castId is 40170) // Fall of Faith Cast Too Late
        {
            foreach (var pc in FakeParty.Get())
            {
                _partyDatas.Add(new PartyData(pc.EntityId));
            }
            _state = State.Casting;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action == null) return;
        if (_state == State.None) return;
        if (set.Action.Value.RowId is 40156 or 40142)
        {
            ++_state;
            if (_state == State.End)
            {
                _state = State.None;
                _gimmickEnded = true;
                HideAllElements();
            }
            ShowElements();
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (!(_state == State.Casting)) return;

        if (data2 == 0 && data3 == 249 && data5 == 15) // fire
        {
            _fireThunders.Add(FireThunder.Fire);
            ++_tetherCount;
            if (_tetherCount == 1 || _tetherCount == 3)
            {
                foreach (var pc in _partyDatas)
                {
                    if (pc.EntityId == target.GetObject().EntityId)
                    {
                        pc.LR = LR.Left;
                        pc.TetherNum = _tetherCount;

                        if (pc.EntityId == Svc.ClientState.LocalPlayer.EntityId)
                        {
                            if (_tetherCount == 1)
                            {
                                Controller.GetElementByName("LeftTether").Enabled = true;
                            }
                            else
                            {
                                if (_fireThunders[0] == FireThunder.Thunder)
                                {
                                    Controller.GetElementByName("LeftTetherNext").Enabled = true;
                                }
                                else
                                {
                                    Controller.GetElementByName("LeftNone2").Enabled = true;
                                }
                            }
                        }
                        break;
                    }
                }
            }
            else
            {
                foreach (var pc in _partyDatas)
                {
                    if (pc.EntityId == target.GetObject().EntityId)
                    {
                        pc.LR = LR.Right;
                        pc.TetherNum = _tetherCount;
                        if (pc.EntityId == Svc.ClientState.LocalPlayer.EntityId)
                        {
                            if (_tetherCount == 2)
                            {
                                Controller.GetElementByName("RightTether").Enabled = true;
                            }
                            else
                            {
                                if (_fireThunders[1] == FireThunder.Thunder)
                                {
                                    Controller.GetElementByName("RightTetherNext").Enabled = true;
                                }
                                else
                                {
                                    Controller.GetElementByName("RightNone2").Enabled = true;
                                }
                            }
                        }
                        break;
                    }

                }
            }
        }
        else if (data2 == 0 && data3 == 287 && data5 == 15) // thunder
        {
            _fireThunders.Add(FireThunder.Thunder);
            ++_tetherCount;
            if (_tetherCount == 1 || _tetherCount == 3)
            {
                foreach (var pc in _partyDatas)
                {
                    if (pc.EntityId == target.GetObject().EntityId)
                    {
                        pc.LR = LR.Left;
                        pc.TetherNum = _tetherCount;

                        if (pc.EntityId == Svc.ClientState.LocalPlayer.EntityId)
                        {
                            if (_tetherCount == 1)
                            {
                                Controller.GetElementByName("LeftTether").Enabled = true;
                            }
                            else
                            {
                                if (_fireThunders[0] == FireThunder.Thunder)
                                {
                                    Controller.GetElementByName("LeftTetherNext").Enabled = true;
                                }
                                else
                                {
                                    Controller.GetElementByName("LeftNone2").Enabled = true;
                                }
                            }
                        }
                        break;
                    }
                }
            }
            else
            {
                foreach (var pc in _partyDatas)
                {
                    if (pc.EntityId == target.GetObject().EntityId)
                    {
                        pc.LR = LR.Right;
                        pc.TetherNum = _tetherCount;
                        if (pc.EntityId == Svc.ClientState.LocalPlayer.EntityId)
                        {
                            if (_tetherCount == 2)
                            {
                                Controller.GetElementByName("RightTether").Enabled = true;
                            }
                            else
                            {
                                if (_fireThunders[1] == FireThunder.Thunder)
                                {
                                    Controller.GetElementByName("RightTetherNext").Enabled = true;
                                }
                                else
                                {
                                    Controller.GetElementByName("RightNone2").Enabled = true;
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        if (_tetherCount == 4)
        {
            ParseData();
            ShowElements();
        }
    }

    public override void OnUpdate()
    {
        if (_state == State.None || _gimmickEnded) return;

        Element? el = Controller.GetRegisteredElements().Where(Element => Element.Value.Enabled).FirstOrDefault().Value;
        if (el == null) return;
        el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
    }

    public override void OnReset()
    {
        _partyDatas.Clear();
        _fireThunders.Clear();
        _state = State.None;
        _tetherCount = 0;
        _soilEndCount = 0;
        _gimmickEnded = false;
        HideAllElements();
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Left Right Priority");
        C.LRPriority.Draw();
        ImGui.NewLine();
        ImGui.Text("No Buffer Priority");
        C.NoBufferPriority.Draw();

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"Tether Count: {_tetherCount}");
            ImGui.Text($"Soil End Count: {_soilEndCount}");
            ImGui.Text($"Gimmick Ended: {_gimmickEnded}");
            if (_fireThunders.Count > 0)
            {
                ImGui.Text("Fire Thunders:");
                foreach (var x in _fireThunders)
                {
                    ImGui.Text(x.ToString());
                }
            }
            else
            {
                ImGui.Text($"Fire Thunders: None");
            }

            ImGui.NewLine();
            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach (var x in _partyDatas)
            {
                Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text(x.EntityId.GetObject().Name.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("ObjectId", () => ImGui.Text(x.EntityId.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("LR", () => ImGui.Text(x.LR.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("PriorityNum", () => ImGui.Text(x.PriorityNum.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("TetherNum", () => ImGui.Text(x.TetherNum.ToString())));
            }

            ImGuiEx.EzTable(Entries);
        }
    }
    #endregion

    #region Private Methods
    private void HideAllElements() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    private void ParseData()
    {
        // No Buffer Check
        var LRBuffers = C.NoBufferPriority.GetPlayers(x => x.IGameObject is IPlayerCharacter pc && pc.StatusList.All(z => z.StatusId != 1051));
        if (LRBuffers == null || LRBuffers.Count() == 0)
        {
            if (LRBuffers == null) DuoLog.Error("No Buffer Priority List is Null");
            else DuoLog.Error("No Buffer Priority List is Empty");
            _state = State.End;
            return;
        }

        int i = 0;
        foreach (var LRBuffer in LRBuffers)
        {
            foreach (var pc in _partyDatas)
            {
                if (pc.EntityId == LRBuffer.IGameObject.EntityId)
                {
                    var obj = pc.EntityId.GetObject();
                    if (obj == null)
                    {
                        DuoLog.Error("Object is Null 357");
                    }
                    pc.LR = (i < 2) ? LR.Left : LR.Right;
                    ++i;
                    break;
                }
            }
        }

        var leftNoBuffPcs = _partyDatas.Where(x => x.LR == LR.Left && x.EntityId.GetObject() is IPlayerCharacter pc && pc.StatusList.All(z => z.StatusId != 1051)).ToList();
        var rightNoBuffPcs = _partyDatas.Where(x => x.LR == LR.Right && x.EntityId.GetObject() is IPlayerCharacter pc && pc.StatusList.All(z => z.StatusId != 1051)).ToList();

        if (leftNoBuffPcs.Count() != 2 || rightNoBuffPcs.Count() != 2)
        {
            DuoLog.Error("No Buffer Priority List is not 2");
            _state = State.End;
            return;
        }

        var list = C.LRPriority.GetPlayers(_ => true);
        int indexFirst = list.FindIndex(x => x.IGameObject.EntityId == leftNoBuffPcs[0].EntityId);
        int indexSecond = list.FindIndex(x => x.IGameObject.EntityId == leftNoBuffPcs[1].EntityId);
        if (indexFirst == -1 || indexSecond == -1)
        {
            DuoLog.Error("No Buffer Priority Index is -1");
            _state = State.End;
            return;
        }
        if (indexFirst > indexSecond)
        {
            leftNoBuffPcs[0].PriorityNum = 1;
            leftNoBuffPcs[1].PriorityNum = 2;
        }
        else
        {
            leftNoBuffPcs[0].PriorityNum = 2;
            leftNoBuffPcs[1].PriorityNum = 1;
        }


        indexFirst = list.FindIndex(x => x.IGameObject.EntityId == rightNoBuffPcs[0].EntityId);
        indexSecond = list.FindIndex(x => x.IGameObject.EntityId == rightNoBuffPcs[1].EntityId);
        if (indexFirst == -1 || indexSecond == -1)
        {
            DuoLog.Error("No Buffer Priority Index is -1");
            _state = State.End;
            return;
        }

        if (indexFirst > indexSecond)
        {
            rightNoBuffPcs[0].PriorityNum = 1;
            rightNoBuffPcs[1].PriorityNum = 2;
        }
        else
        {
            rightNoBuffPcs[0].PriorityNum = 2;
            rightNoBuffPcs[1].PriorityNum = 1;
        }

    }

    private void ShowElements()
    {
        HideAllElements();
        if (_state == State.Casting)
        {
            foreach (var pc in _partyDatas)
            {
                if (pc.EntityId != Svc.ClientState.LocalPlayer.EntityId) continue;
                if (pc.LR == LR.Left)
                {
                    if (_fireThunders[0] == FireThunder.Fire)
                    {
                        if (pc.TetherNum == 1) Controller.GetElementByName("LeftTether").Enabled = true;
                        else Controller.GetElementByName("LeftNone2").Enabled = true;
                        break;
                    }
                    else
                    {
                        if (pc.TetherNum == 1) Controller.GetElementByName("LeftTether").Enabled = true;
                        else if (pc.TetherNum == 3) Controller.GetElementByName("LeftTetherNext").Enabled = true;
                        else if (pc.PriorityNum == 1) Controller.GetElementByName("LeftNone1").Enabled = true;
                        else Controller.GetElementByName("LeftNone2").Enabled = true;
                        break;
                    }
                }
                else
                {
                    if (_fireThunders[1] == FireThunder.Fire)
                    {
                        if (pc.TetherNum == 2) Controller.GetElementByName("RightTether").Enabled = true;
                        else Controller.GetElementByName("RightNone2").Enabled = true;
                        break;
                    }
                    else
                    {
                        if (pc.TetherNum == 2) Controller.GetElementByName("RightTether").Enabled = true;
                        else if (pc.TetherNum == 4) Controller.GetElementByName("RightTetherNext").Enabled = true;
                        else if (pc.PriorityNum == 1) Controller.GetElementByName("RightNone1").Enabled = true;
                        else Controller.GetElementByName("RightNone2").Enabled = true;
                        break;
                    }
                }
            }
        }
        else if (_state == State.Soil1End)
        {
            foreach (var pc in _partyDatas)
            {
                if (pc.EntityId != Svc.ClientState.LocalPlayer.EntityId) continue;
                if (pc.LR == LR.Left)
                {
                    if (_fireThunders[2] == FireThunder.Fire)
                    {
                        if (pc.TetherNum == 3) Controller.GetElementByName("LeftTether").Enabled = true;
                        else Controller.GetElementByName("LeftNone2").Enabled = true;
                        break;
                    }
                    else
                    {
                        if (pc.TetherNum == 3) Controller.GetElementByName("LeftTether").Enabled = true;
                        else if (pc.TetherNum == 1) Controller.GetElementByName("LeftTetherNext").Enabled = true;
                        else if (pc.PriorityNum == 1) Controller.GetElementByName("LeftNone1").Enabled = true;
                        else Controller.GetElementByName("LeftNone2").Enabled = true;
                        break;
                    }
                }
                else
                {
                    if (_fireThunders[1] == FireThunder.Fire)
                    {
                        if (pc.TetherNum == 2) Controller.GetElementByName("RightTether").Enabled = true;
                        else Controller.GetElementByName("RightNone2").Enabled = true;
                        break;
                    }
                    else
                    {
                        if (pc.TetherNum == 2) Controller.GetElementByName("RightTether").Enabled = true;
                        else if (pc.TetherNum == 4) Controller.GetElementByName("RightTetherNext").Enabled = true;
                        else if (pc.PriorityNum == 1) Controller.GetElementByName("RightNone1").Enabled = true;
                        else Controller.GetElementByName("RightNone2").Enabled = true;
                        break;
                    }
                }
            }
        }
        else if (_state == State.Soil2End)
        {
            foreach (var pc in _partyDatas)
            {
                if (pc.EntityId != Svc.ClientState.LocalPlayer.EntityId) continue;
                if (pc.LR == LR.Left)
                {
                    if (_fireThunders[2] == FireThunder.Fire)
                    {
                        if (pc.TetherNum == 3) Controller.GetElementByName("LeftTether").Enabled = true;
                        else Controller.GetElementByName("LeftNone2").Enabled = true;
                        break;
                    }
                    else
                    {
                        if (pc.TetherNum == 3) Controller.GetElementByName("LeftTether").Enabled = true;
                        else if (pc.TetherNum == 1) Controller.GetElementByName("LeftTetherNext").Enabled = true;
                        else if (pc.PriorityNum == 1) Controller.GetElementByName("LeftNone1").Enabled = true;
                        else Controller.GetElementByName("LeftNone2").Enabled = true;
                        break;
                    }
                }
                else
                {
                    if (_fireThunders[3] == FireThunder.Fire)
                    {
                        if (pc.TetherNum == 4) Controller.GetElementByName("RightTether").Enabled = true;
                        else Controller.GetElementByName("RightNone2").Enabled = true;
                        break;
                    }
                    else
                    {
                        if (pc.TetherNum == 4) Controller.GetElementByName("RightTether").Enabled = true;
                        else if (pc.TetherNum == 2) Controller.GetElementByName("RightTetherNext").Enabled = true;
                        else if (pc.PriorityNum == 1) Controller.GetElementByName("RightNone1").Enabled = true;
                        else Controller.GetElementByName("RightNone2").Enabled = true;
                        break;
                    }
                }
            }
        }
        else if (_state == State.Soil3End)
        {
            foreach (var pc in _partyDatas)
            {
                if (pc.EntityId != Svc.ClientState.LocalPlayer.EntityId) continue;
                if (pc.LR == LR.Left)
                {
                    Controller.GetElementByName("LeftNone2").Enabled = true;
                    break;
                }
                else
                {
                    if (_fireThunders[3] == FireThunder.Fire)
                    {
                        if (pc.TetherNum == 4) Controller.GetElementByName("RightTether").Enabled = true;
                        else Controller.GetElementByName("RightNone2").Enabled = true;
                        break;
                    }
                    else
                    {
                        if (pc.TetherNum == 4) Controller.GetElementByName("RightTether").Enabled = true;
                        else if (pc.TetherNum == 2) Controller.GetElementByName("RightTetherNext").Enabled = true;
                        else if (pc.PriorityNum == 1) Controller.GetElementByName("RightNone1").Enabled = true;
                        else Controller.GetElementByName("RightNone2").Enabled = true;
                        break;
                    }
                }
            }
        }
    }
    #endregion
}
