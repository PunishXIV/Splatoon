using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.SimpleGui;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using Element = Splatoon.Element;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P1_Burn_Strike_Tower : SplatoonScript
{
    private readonly ImGuiEx.RealtimeDragDrop<Job> _dragDrop = new("DragDropJob", x => x.ToString());

    private readonly Dictionary<int, List<uint>> TowerCastIds = new()
    {
        { 1, [0x9CC7, 0x9CC3] },
        { 2, [0x9CBD, 0x9CBA] },
        { 3, [0x9CBE, 0x9CBB] },
        { 4, [0x9CBF, 0x9CBC] }
    };

    private IBattleNpc?[] _currentTowers = new IBattleNpc[3];

    private IBattleNpc? _myTower;

    private State _state = State.None;
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(3, "Garume");


    private Config C => Controller.GetConfig<Config>();


    public override void OnSettingsDraw()
    {
        ImGui.Text("General");
        C.PriorityData.Draw();
        ImGui.Separator();
        ImGui.Checkbox("Enabled Fixed Priority", ref C.FixEnabled);
        ImGuiEx.HelpMarker(
            "If enabled, the priority will be fixed based on the order of the fixed priority list.\n1st -> North, 2nd -> Center, 3rd -> South");
        if (C.FixEnabled)
        {
            ImGui.Indent();

            if (C.PriorityData.PriorityLists.First().IsRole)
            {
                ImGuiEx.EnumCombo("1st Fixed Role", ref C.FirstFixRole);
                ImGuiEx.EnumCombo("2nd Fixed Role", ref C.SecondFixRole);
                ImGuiEx.EnumCombo("3rd Fixed Role", ref C.ThirdFixRole);
            }
            else
            {
                ImGui.SliderInt("1st Fixed Index", ref C.FirstFixIndex, 0, 5);
                ImGui.SliderInt("2nd Fixed Index", ref C.SecondFixIndex, 0, 5);
                ImGui.SliderInt("3rd Fixed Index", ref C.ThirdFixIndex, 0, 5);

                if (C.FirstFixIndex == C.SecondFixIndex || C.FirstFixIndex == C.ThirdFixIndex ||
                    C.SecondFixIndex == C.ThirdFixIndex)
                    ImGuiEx.Text(EColor.RedBright, "Indexes must be different");
            }

            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text("State: " + _state);
            ImGui.Text("My Tower: " + _myTower);
            ImGui.Text("Towers: ");
            foreach (var tower in _currentTowers)
                ImGui.Text(tower + " " + tower?.Position);
        }
    }

    public override void OnScriptUpdated(uint previousVersion)
    {
        if (previousVersion < 3)
            new PopupWindow(() =>
            {
                ImGuiEx.Text($"""
                              Warning: Splatoon Script 
                              {InternalData.Name}
                              was updated. 
                              If you were using the fixed priority feature,
                              Please make sure to set the fixed roles again.
                              """);
            });
    }

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            tether = true,
            radius = 4f,
            thicc = 6f,
            overlayText = "<< Go Here >>",
            overlayVOffset = 3f,
            overlayFScale = 3f
        };
        Controller.RegisterElement("Bait", element);
    }

    public override void OnReset()
    {
        _state = State.None;
        _myTower = null;
        _currentTowers = new IBattleNpc[3];
    }

    public override void OnUpdate()
    {
        if (_state == State.Split)
            Controller.GetRegisteredElements()
                .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
        else
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_state != State.Split) return;
        if (TowerCastIds.Values.Any(x => x.Contains(set.Action.Value.RowId)))
            _state = State.End;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (_state == State.Split) return;
        if (castId is 40135 or 40129) _state = State.Start;

        if (TowerCastIds.Values.Any(x => x.Contains(castId)))
            if (source.GetObject() is IBattleNpc npc)
            {
                switch (npc.Position.Z)
                {
                    case < 95:
                        _currentTowers[0] = npc;
                        break;
                    case < 105:
                        _currentTowers[1] = npc;
                        break;
                    default:
                        _currentTowers[2] = npc;
                        break;
                }

                if (_currentTowers.Any(x => x == null)) return;
                _state = State.Split;
                var list = C.PriorityData.GetFirstValidList();
                if (list is null)
                {
                    DuoLog.Warning("[P1 Burn Strike Tower] Priority list is not 6");
                    return;
                }

                if (C.FixEnabled)
                {
                    List<string> nonFixed;
                    if (list.IsRole)
                    {
                        var myRole = list.List.First(x => x.IsInParty(true, out var upm) && upm.Name == Player.Name)
                            .Role;
                        if (C.FirstFixRole == myRole)
                            _myTower = _currentTowers[0];
                        else if (C.SecondFixRole == myRole)
                            _myTower = _currentTowers[1];
                        else if (C.ThirdFixRole == myRole)
                            _myTower = _currentTowers[2];
                        nonFixed = list.List.Where(x =>
                                x.Role != C.FirstFixRole && x.Role != C.SecondFixRole && x.Role != C.ThirdFixRole)
                            .Select(x => x.Name.Split("@").First()).ToList();
                    }
                    else
                    {
                        var myIndex = C.PriorityData.GetOwnIndex(x => true);
                        if (myIndex == C.FirstFixIndex)
                            _myTower = _currentTowers[0];
                        else if (myIndex == C.SecondFixIndex)
                            _myTower = _currentTowers[1];
                        else if (myIndex == C.ThirdFixIndex)
                            _myTower = _currentTowers[2];

                        nonFixed = list.List.Where(x =>
                            x.Name != list.List[C.FirstFixIndex].Name &&
                            x.Name != list.List[C.SecondFixIndex].Name &&
                            x.Name != list.List[C.ThirdFixIndex].Name).Select(x => x.Name.Split("@").First()).ToList();
                    }

                    foreach (var tower in _currentTowers)
                    {
                        if (tower is null)
                        {
                            DuoLog.Warning("[P1 Burn Strike Tower] Tower is null");
                            continue;
                        }

                        var towerCount = TowerCastIds.First(x => x.Value.Contains(tower.CastActionId)).Key;
                        var remaining = towerCount - 1;

                        if (remaining == 0) continue;

                        for (var i = 0; i < remaining; i++)
                        {
                            if (nonFixed.First() == Player.Name)
                                _myTower = tower;
                            nonFixed.RemoveAt(0);
                        }
                    }
                }

                else
                {
                    var index = 0;
                    foreach (var tower in _currentTowers)
                    {
                        if (tower is null)
                        {
                            DuoLog.Warning("[P1 Burn Strike Tower] Tower is null");
                            continue;
                        }

                        var towerCount = TowerCastIds.First(x => x.Value.Contains(tower.CastActionId)).Key;
                        var lastIndex = index;
                        index += towerCount;

                        for (var i = lastIndex; i < index; i++)
                            if (list?.List[i].Name == Player.Name)
                                _myTower = tower;
                    }
                }

                if (Controller.TryGetElementByName("Bait", out var bait))
                {
                    bait.Enabled = true;
                    bait.SetOffPosition(_myTower.Position);
                }
            }
    }

    private enum State
    {
        None,
        Start,
        Split,
        End
    }

    private class Config : IEzConfig
    {
        public readonly Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public readonly Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();

        public int FirstFixIndex;
        public RolePosition FirstFixRole = RolePosition.H1;

        public bool FixEnabled;
        public PriorityData6 PriorityData = new();
        public int SecondFixIndex = 1;
        public RolePosition SecondFixRole = RolePosition.H2;
        public int ThirdFixIndex = 5;
        public RolePosition ThirdFixRole = RolePosition.R2;
    }

    private class PriorityData6 : PriorityData
    {
        public override int GetNumPlayers()
        {
            return 6;
        }
    }
}