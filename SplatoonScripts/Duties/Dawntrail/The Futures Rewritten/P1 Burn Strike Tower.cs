using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.SimpleGui;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Element = Splatoon.Element;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P1_Burn_Strike_Tower : SplatoonScript
{
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
    public override Metadata? Metadata => new(5, "Garume");
    private Config C => Controller.GetConfig<Config>();

    private int TowerCount(uint castId)
    {
        return TowerCastIds.First(x => x.Value.Contains(castId)).Key;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("General");
        C.PriorityData.Draw();
        ImGui.Separator();
        ImGui.Checkbox("Enabled Fixed Priority", ref C.FixEnabled);
        ImGuiEx.HelpMarker(
            "If enabled, the priority will be fixed based on the order of the fixed priority list.\n1st -> North, 2nd -> Center, 3rd -> South");
        if(C.FixEnabled)
        {
            ImGui.Indent();
            if(C.PriorityData.PriorityLists.First().IsRole)
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

                if(C.FirstFixIndex == C.SecondFixIndex || C.FirstFixIndex == C.ThirdFixIndex ||
                    C.SecondFixIndex == C.ThirdFixIndex)
                    ImGuiEx.Text(EColor.RedBright, "Indexes must be different");
            }

            ImGui.Checkbox("Enabled Flex Priority", ref C.FlexEnabled);
            ImGui.Unindent();
        }

        if(C.FlexEnabled)
        {
            ImGui.Indent();
            if(C.PriorityData.PriorityLists.First().IsRole)
            {
                ImGuiEx.EnumCombo("1st Flex Role", ref C.FirstFlexRole);
                ImGuiEx.EnumCombo("2nd Flex Role", ref C.SecondFlexRole);
                ImGuiEx.EnumCombo("3rd Flex Role", ref C.ThirdFlexRole);
            }
            else
            {
                ImGui.SliderInt("1st Flex Index", ref C.FirstFlexIndex, 0, 5);
                ImGui.SliderInt("2nd Flex Index", ref C.SecondFlexIndex, 0, 5);
                ImGui.SliderInt("3rd Flex Index", ref C.ThirdFlexIndex, 0, 5);

                if(C.FirstFlexIndex == C.SecondFlexIndex || C.FirstFlexIndex == C.ThirdFlexIndex ||
                    C.SecondFlexIndex == C.ThirdFlexIndex)
                    ImGuiEx.Text(EColor.RedBright, "Indexes must be different");
            }

            ImGui.Unindent();
        }

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text("State: " + _state);
            ImGui.Text("My Tower: " + _myTower);
            ImGui.Text("Towers: ");
            foreach(var tower in _currentTowers)
                ImGui.Text(tower + " " + tower?.Position);
        }
    }

    public override void OnScriptUpdated(uint previousVersion)
    {
        if(previousVersion < 3)
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
        if(_state == State.Split)
            Controller.GetRegisteredElements()
                .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
        else
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(_state != State.Split) return;
        if(TowerCastIds.Values.Any(x => x.Contains(set.Action.Value.RowId)))
            _state = State.End;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(_state == State.Split) return;
        if(castId is 40135 or 40129) _state = State.Start;
        if(!TowerCastIds.Values.Any(x => x.Contains(castId))) return;
        if(source.GetObject() is IBattleNpc npc)
            switch(npc.Position.Z)
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

        if(_currentTowers.Any(x => x == null)) return;
        _state = State.Split;
        var list = C.PriorityData.GetFirstValidList();
        var players = C.PriorityData.GetPlayers(x => true);
        var towers = _currentTowers.Where(x => x != null).Select(x => x!).ToList();
        if(list is null || players is null)
        {
            DuoLog.Warning("[P1 Burn Strike Tower] Priority list is not setup");
            return;
        }

        if(towers.Count != 3)
        {
            DuoLog.Warning("[P1 Burn Strike Tower] Tower is null");
            return;
        }

        var roleList = list.List
            .Select(x =>
                x.IsInParty(list.IsRole, out var upm) ? (upm.Name, x.Role) : ("", RolePosition.Not_Selected))
            .ToDictionary(x => x.Item1, x => x.Item2);

        if(C.FixEnabled)
        {
            List<string> nonFixed;
            if(list.IsRole)
            {
                var myRole = roleList[Player.Name];
                if(C.FirstFixRole == myRole)
                    _myTower = _currentTowers[0];
                else if(C.SecondFixRole == myRole)
                    _myTower = _currentTowers[1];
                else if(C.ThirdFixRole == myRole)
                    _myTower = _currentTowers[2];
                nonFixed = roleList
                    .Where(x => x.Value != C.FirstFixRole && x.Value != C.SecondFixRole &&
                                x.Value != C.ThirdFixRole).Select(x => x.Key).ToList();
            }
            else
            {
                var myIndex = C.PriorityData.GetOwnIndex(x => true);
                if(myIndex == C.FirstFixIndex)
                    _myTower = _currentTowers[0];
                else if(myIndex == C.SecondFixIndex)
                    _myTower = _currentTowers[1];
                else if(myIndex == C.ThirdFixIndex)
                    _myTower = _currentTowers[2];
                nonFixed = players
                    .Where((_, i) => i != C.FirstFixIndex && i != C.SecondFixIndex && i != C.ThirdFixIndex)
                    .Select(x => x.Name).ToList();
            }

            if(C.FlexEnabled)
            {
                Queue<string> nonDecided = [];
                List<string> flexPlayers;
                if(list.IsRole)
                {
                    var firstPlayer = roleList.First(x => x.Value == C.FirstFlexRole).Key;
                    var secondPlayer = roleList.First(x => x.Value == C.SecondFlexRole).Key;
                    var thirdPlayer = roleList.First(x => x.Value == C.ThirdFlexRole).Key;

                    if(TowerCount(_currentTowers[0]!.CastActionId) == 1)
                        nonDecided.Enqueue(firstPlayer);
                    else if(firstPlayer == Player.Name)
                        _myTower = _currentTowers[0];

                    if(TowerCount(_currentTowers[1]!.CastActionId) == 1)
                        nonDecided.Enqueue(secondPlayer);
                    else if(secondPlayer == Player.Name)
                        _myTower = _currentTowers[1];

                    if(TowerCount(_currentTowers[2]!.CastActionId) == 1)
                        nonDecided.Enqueue(thirdPlayer);
                    else if(thirdPlayer == Player.Name)
                        _myTower = _currentTowers[2];
                }
                else
                {
                    var firstPlayer = players[C.FirstFlexIndex].Name;
                    var secondPlayer = players[C.SecondFlexIndex].Name;
                    var thirdPlayer = players[C.ThirdFlexIndex].Name;

                    if(TowerCount(_currentTowers[0]!.CastActionId) == 1)
                        nonDecided.Enqueue(firstPlayer);
                    else if(firstPlayer == Player.Name)
                        _myTower = _currentTowers[0];

                    if(TowerCount(_currentTowers[1]!.CastActionId) == 1)
                        nonDecided.Enqueue(secondPlayer);
                    else if(secondPlayer == Player.Name)
                        _myTower = _currentTowers[1];

                    if(TowerCount(_currentTowers[2]!.CastActionId) == 1)
                        nonDecided.Enqueue(thirdPlayer);
                    else if(thirdPlayer == Player.Name)
                        _myTower = _currentTowers[2];
                }

                DuoLog.Warning("[P1 Burn Strike Tower] Non Decided: " + string.Join(", ", nonDecided));

                foreach(var tower in towers)
                {
                    var remaining = TowerCount(tower.CastActionId);
                    if(remaining < 3) continue;
                    foreach(var player in nonDecided.DequeueMultiple(TowerCount(tower.CastActionId) - 2))
                        if(player == Player.Name)
                            _myTower = tower;
                }
            }
            else
            {
                foreach(var tower in towers)
                {
                    var remaining = TowerCount(tower.CastActionId) - 1;
                    if(remaining == 0) continue;
                    for(var i = 0; i < remaining; i++)
                    {
                        if(nonFixed.First() == Player.Name)
                            _myTower = tower;
                        nonFixed.RemoveAt(0);
                    }
                }
            }
        }
        else
        {
            var index = 0;
            foreach(var tower in towers)
            {
                var lastIndex = index;
                index += TowerCount(tower.CastActionId);

                for(var i = lastIndex; i < index; i++)
                    if(players[i].Name == Player.Name)
                        _myTower = tower;
            }
        }

        if(Controller.TryGetElementByName("Bait", out var bait))
        {
            bait.Enabled = true;
            bait.SetOffPosition(_myTower.Position);
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

        public int FirstFlexIndex = 2;
        public RolePosition FirstFlexRole = RolePosition.M1;
        public bool FixEnabled;
        public bool FlexEnabled;
        public PriorityData6 PriorityData = new();
        public int SecondFixIndex = 1;
        public RolePosition SecondFixRole = RolePosition.H2;
        public int SecondFlexIndex = 3;
        public RolePosition SecondFlexRole = RolePosition.M2;
        public int ThirdFixIndex = 5;
        public RolePosition ThirdFixRole = RolePosition.R2;
        public int ThirdFlexIndex = 4;
        public RolePosition ThirdFlexRole = RolePosition.R1;
    }

    private class PriorityData6 : PriorityData
    {
        public override int GetNumPlayers()
        {
            return 6;
        }
    }
}

public static class QueueExtensions
{
    public static IEnumerable<T> DequeueMultiple<T>(this Queue<T> queue, int count)
    {
        for(var i = 0; i < count && queue.Count > 0; i++) yield return queue.Dequeue();
    }
}