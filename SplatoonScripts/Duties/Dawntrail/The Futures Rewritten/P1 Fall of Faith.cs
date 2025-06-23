using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.PartyFunctions;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P1_Fall_of_Faith : SplatoonScript
{
    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    private readonly ImGuiEx.RealtimeDragDrop<Job> DragDrop = new("DragDropJob", x => x.ToString());

    private Dictionary<string, PlayerData> _partyDatas = [];

    private State _state = State.None;

    private int _tetherCount = 1;
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(2, "Garume");
    private Config C => Controller.GetConfig<Config>();

    public override void OnStartingCast(uint source, uint castId)
    {
        if(_state == State.None && castId is 40137 or 40140)
        {
            _state = State.Start;
            var hasDebuffPlayer = FakeParty.Get().First(x => x.StatusList.Any(x => x.StatusId == 1051));
            if(castId == 40137)
                _partyDatas[hasDebuffPlayer.Name.ToString()] = new PlayerData(Debuff.Red, C.Tether1Direction, 1);
            else
                _partyDatas[hasDebuffPlayer.Name.ToString()] = new PlayerData(Debuff.Blue, C.Tether1Direction, 1);

            ApplyElement();
        }

        if(_state == State.Split && castId == 40170) _state = State.End;
    }


    public override void OnSetup()
    {
        for(var i = 0; i < 4; i++)
        {
            var element = new Element(1)
            {
                overlayVOffset = 3f,
                overlayFScale = 2f
            };
            Controller.RegisterElement("Tether" + i, element);
        }

        var bait = new Element(0)
        {
            thicc = 6f,
            tether = true
        };

        Controller.RegisterElement("Bait", bait);
    }

    private Vector2 GetPosition(Direction direction)
    {
        return direction switch
        {
            Direction.North => new Vector2(100, 85),
            Direction.East => new Vector2(115, 100),
            Direction.South => new Vector2(100, 115),
            Direction.West => new Vector2(85, 100),
            _ => Vector2.Zero
        };
    }

    public override void OnReset()
    {
        _state = State.None;
        _partyDatas = [];
        _tetherCount = 1;
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(_state != State.Start) return;
        _tetherCount++;
        if(_tetherCount is > 4 or < 2) return;
        if(target.GetObject() is not IPlayerCharacter targetPlayer) return;
        var name = targetPlayer.Name.ToString();

        var debuff = data3 switch
        {
            249 => Debuff.Red,
            287 => Debuff.Blue,
            _ => Debuff.None
        };
        _partyDatas[name] = _tetherCount switch
        {
            1 => new PlayerData(debuff, C.Tether1Direction, 1),
            2 => new PlayerData(debuff, C.Tether2Direction, 2),
            3 => new PlayerData(debuff, C.Tether3Direction, 3),
            4 => new PlayerData(debuff, C.Tether4Direction, 4),
            _ => _partyDatas[name]
        };

        if(_tetherCount == 4)
        {
            _state = State.Split;
            var noTether = C.PriorityData.GetPlayers(x => !_partyDatas.ContainsKey(x.Name));
            if(noTether == null)
            {
                DuoLog.Warning("[P1 Fall of Faith] NoTether is null");
                return;
            }
            _partyDatas[noTether[0].Name] = new PlayerData(Debuff.None, C.NoTether12Direction, 0);
            _partyDatas[noTether[1].Name] = new PlayerData(Debuff.None, C.NoTether12Direction, 0);
            _partyDatas[noTether[2].Name] = new PlayerData(Debuff.None, C.NoTether34Direction, 0);
            _partyDatas[noTether[3].Name] = new PlayerData(Debuff.None, C.NoTether34Direction, 0);
        }

        ApplyElement();
    }

    private void ApplyElement()
    {
        if(_partyDatas.TryGetValue(Player.Name, out var value) && Controller.TryGetElementByName("Bait", out var bait))
        {
            bait.Enabled = true;
            bait.SetOffPosition(GetPosition(value.Direction).ToVector3());
        }

        var index = 0;
        foreach(var data in _partyDatas.Where(x => x.Value.Debuff != Debuff.None))
        {
            var text = data.Value.Debuff switch
            {
                Debuff.Red => C.RedTetherText.Get() + data.Value.Count,
                Debuff.Blue => C.BlueTetherText.Get() + data.Value.Count,
                _ => string.Empty
            };

            if(Controller.TryGetElementByName("Tether" + index, out var tether))
            {
                tether.Enabled = true;
                tether.refActorName = data.Key;
                tether.overlayText = text;
            }

            index++;
        }
    }

    public override void OnUpdate()
    {
        switch(_state)
        {
            case State.None or State.End:
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
                break;
            case State.Start or State.Split:
                {
                    if(Controller.TryGetElementByName("Bait", out var bait))
                        bait.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
                    break;
                }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("General");

        ImGuiEx.EnumCombo("Tether1Direction##Tether1", ref C.Tether1Direction);
        ImGuiEx.EnumCombo("Tether2Direction##Tether2", ref C.Tether2Direction);
        ImGuiEx.EnumCombo("Tether3Direction##Tether1", ref C.Tether3Direction);
        ImGuiEx.EnumCombo("Tether4Direction##Tether1", ref C.Tether4Direction);
        ImGuiEx.EnumCombo("NoTether12Direction##NoTether12", ref C.NoTether12Direction);
        ImGuiEx.EnumCombo("NoTether34Direction##NoTether34", ref C.NoTether34Direction);

        ImGui.Separator();

        C.PriorityData.Draw();

        ImGui.Separator();

        ImGui.Text("RedTetherText:");
        ImGui.SameLine();
        var redTether = C.RedTetherText.Get();
        C.RedTetherText.ImGuiEdit(ref redTether);

        ImGui.Text("BlueTetherText:");
        ImGui.SameLine();
        var blueTether = C.BlueTetherText.Get();
        C.BlueTetherText.ImGuiEdit(ref blueTether);

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text("PartyDirection");
            ImGui.Indent();
            foreach(var (key, value) in _partyDatas) ImGui.Text($"{key} : {value}");
            ImGui.Unindent();

            ImGui.Text($"State: {_state}");
            ImGui.Text($"TetherCount: {_tetherCount}");
        }
    }

    private enum Debuff
    {
        None,
        Red,
        Blue
    }

    private enum State
    {
        None,
        Start,
        Split,
        End
    }

    private record PlayerData(Debuff Debuff, Direction Direction, int Count);


    public class Config : IEzConfig
    {
        public readonly Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public readonly Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();

        public InternationalString BlueTetherText = new() { Jp = "雷 散開" };

        public Direction NoTether12Direction = Direction.North;
        public Direction NoTether34Direction = Direction.South;

        public PriorityData PriorityData = new();

        public InternationalString RedTetherText = new() { Jp = "炎 ペア割" };

        public Direction Tether1Direction = Direction.North;
        public Direction Tether2Direction = Direction.South;
        public Direction Tether3Direction = Direction.North;
        public Direction Tether4Direction = Direction.South;
    }
}
