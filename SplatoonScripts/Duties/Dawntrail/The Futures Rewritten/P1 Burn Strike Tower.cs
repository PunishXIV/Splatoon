using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.PartyFunctions;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using ImGuiNET;
using Splatoon.SplatoonScripting;
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
    public override Metadata? Metadata => new(2, "Garume");


    private Config C => Controller.GetConfig<Config>();

    private unsafe bool DrawPriorityList()
    {
        if (C.Priority.Length != 6)
            C.Priority = ["", "", "", "", "", ""];

        ImGuiEx.Text("Priority list");
        ImGui.SameLine();
        ImGuiEx.Spacing();
        // if (ImGui.Button("Perform test")) SelfTest();
        ImGui.SameLine();
        if (ImGui.Button("Fill by job"))
        {
            HashSet<(string, Job)> party = [];
            foreach (var x in FakeParty.Get())
                party.Add((x.Name.ToString(), x.GetJob()));

            var proxy = InfoProxyCrossRealm.Instance();
            for (var i = 0; i < proxy->GroupCount; i++)
            {
                var group = proxy->CrossRealmGroups[i];
                for (var c = 0; c < proxy->CrossRealmGroups[i].GroupMemberCount; c++)
                {
                    var x = group.GroupMembers[c];
                    party.Add((x.Name.Read(), (Job)x.ClassJobId));
                }
            }

            var index = 0;
            foreach (var job in C.Jobs.Where(job => party.Any(x => x.Item2 == job)))
            {
                C.Priority[index] = party.First(x => x.Item2 == job).Item1;
                index++;
            }

            for (var i = index; i < C.Priority.Length; i++)
                C.Priority[i] = "";
        }

        ImGuiEx.Tooltip("The list is populated based on the job.\nYou can adjust the priority from the option header.");

        ImGui.SameLine();

        ImGui.Checkbox("Enabled Fixed Priority", ref C.FixEnabled);
        ImGuiEx.Tooltip(
            "If enabled, the priority will be fixed based on the order of the fixed priority list.\n1st -> North, 2nd -> Center, 3rd -> South");

        if (C.FixEnabled && C.FixedPriority.Count(x => x) !=3)
        {
            ImGuiEx.Text(EColor.RedBright,"Please select 3 fixed.");
        }
        
        ImGui.PushID("prio");
        for (var i = 0; i < C.Priority.Length; i++)
        {
            ImGui.PushID($"prioelement{i}");
            ImGui.Text($"Character {i + 1}");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            ImGui.InputText($"##Character{i}", ref C.Priority[i], 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.BeginCombo("##partysel", "Select from party"))
            {
                foreach (var x in FakeParty.Get().Select(x => x.Name.ToString())
                             .Union(UniversalParty.Members.Select(x => x.Name)).ToHashSet())
                    if (ImGui.Selectable(x))
                        C.Priority[i] = x;
                ImGui.EndCombo();
            }

            if (C.FixEnabled)
            {
                ImGui.SameLine();
                ImGui.Checkbox($"##FixedPriority{i}", ref C.FixedPriority[i]);
            }


            ImGui.PopID();
        }

        ImGui.PopID();
        return false;
    }


    public override void OnSettingsDraw()
    {
        ImGui.Text("General");
        DrawPriorityList();

        if (ImGuiEx.CollapsingHeader("Option"))
        {
            _dragDrop.Begin();
            foreach (var job in C.Jobs)
            {
                _dragDrop.NextRow();
                ImGui.Text(job.ToString());
                ImGui.SameLine();

                if (ThreadLoadImageHandler.TryGetIconTextureWrap((uint)job.GetIcon(), false, out var texture))
                {
                    ImGui.Image(texture.ImGuiHandle, new Vector2(24f));
                    ImGui.SameLine();
                }

                ImGui.SameLine();
                _dragDrop.DrawButtonDummy(job, C.Jobs, C.Jobs.IndexOf(job));
            }

            _dragDrop.End();
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

                if (_currentTowers.All(x => x != null))
                {
                    _state = State.Split;

                    if (C.FixEnabled)
                    {
                        var index = 0;
                        for (var i = 0; i < C.FixedPriority.Length; i++)
                            if (C.FixedPriority[i])
                            {
                                if (C.Priority[i] == Player.Name)
                                    _myTower = _currentTowers[index];
                                index++;
                            }

                        var nonFixed = C.Priority.Where((x, i) => C.FixedPriority[i] == false).ToList();
                        foreach (var tower in _currentTowers)
                        {
                            var towerCount = TowerCastIds.First(x => x.Value.Contains(tower.CastActionId)).Key;
                            var remaining = towerCount - 1;

                            if (remaining == 0)
                            {
                                index++;
                                continue;
                            }

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
                            var towerCount = TowerCastIds.First(x => x.Value.Contains(tower.CastActionId)).Key;
                            var lastIndex = index;
                            index += towerCount;

                            for (var i = lastIndex; i < index; i++)
                                if (C.Priority[i] == Player.Name)
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

        public readonly List<Job> Jobs =
        [
            Job.WHM,
            Job.SCH,
            Job.AST,
            Job.SGE,
            Job.VPR,
            Job.DRG,
            Job.MNK,
            Job.SAM,
            Job.RPR,
            Job.NIN,
            Job.BRD,
            Job.MCH,
            Job.DNC,
            Job.BLM,
            Job.SMN,
            Job.RDM,
            Job.PCT
        ];

        public bool[] FixedPriority = [false, false, false, false, false, false];

        public bool FixEnabled;
        public string[] Priority = ["", "", "", "", "", ""];
    }
}