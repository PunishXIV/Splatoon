using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.ChatMethods;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker;

public class Boss1_Roar : SplatoonScript
{
    public enum Clockwise
    {
        Clockwise,
        CounterClockwise
    }

    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    private const uint RoarId = 35524;
    private readonly Dictionary<Direction, Debuff> _enemyDebuffs = [];


    private readonly Dictionary<uint, Debuff> _playerDebuffs = [];

    private readonly Dictionary<string, Direction> _solvedDirections = [];
    private State _state = State.None;

    public override HashSet<uint>? ValidTerritories => [1179, 1180];
    public override Metadata? Metadata => new(1, "Garume");

    private Config C => Controller.GetConfig<Config>();

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe)) Reset();
    }

    public override void OnCombatEnd()
    {
        Reset();
    }

    private void Reset()
    {
        _playerDebuffs.Clear();
        _enemyDebuffs.Clear();
        _solvedDirections.Clear();
        _state = State.None;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == RoarId) _state = State.Start;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action is { RowId: 35528 }) _state = State.End;
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status Status)
    {
        if(_state != State.Spread) return;
        if(Status.StatusId == 3748)
        {
            _state = State.Bait;
            var myDirection = _solvedDirections.FirstOrDefault(x => x.Key == Player.Name).Value;
            var position = myDirection switch
            {
                Direction.North => new Vector2(0, -12),
                Direction.East => new Vector2(12, 0),
                Direction.South => new Vector2(0, 12),
                Direction.West => new Vector2(-12, 0),
                _ => Vector2.Zero
            };

            if(Controller.TryGetElementByName("Bait", out var element))
            {
                element.SetOffPosition(position.ToVector3());
                element.radius = 1f;
                element.Enabled = true;
            }
        }
    }


    public override void OnSetup()
    {
        var element = new Element(0)
        {
            thicc = 6f,
            tether = true
        };
        Controller.RegisterElement("Bait", element);
    }

    public override void OnUpdate()
    {
        if(_state is State.Spread or State.Bait)
            Controller.GetRegisteredElements()
                .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
        else
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnGainBuffEffect(uint sourceId, Status Status)
    {
        if(_state != State.Start) return;
        switch(Status.StatusId)
        {
            // Bind
            case 3788:
                _playerDebuffs[sourceId] = Debuff.Bind;
                break;
            // Bubble
            case 3743:
                _playerDebuffs[sourceId] = Debuff.Bubble;
                break;
            default:
                {
                    if(sourceId.GetObject() is IBattleChara { DataId: 0x40A1 } or { DataId: 0x40A8 })
                    {
                        var enemy = (IBattleChara)sourceId.GetObject()!;
                        if(enemy.Position.X < -5)
                            _enemyDebuffs[Direction.West] = Debuff.Bubble;
                        else if(enemy.Position.X > 5)
                            _enemyDebuffs[Direction.East] = Debuff.Bubble;
                        else if(enemy.Position.Z > 5)
                            _enemyDebuffs[Direction.South] = Debuff.Bubble;
                        else if(enemy.Position.Z < -5) _enemyDebuffs[Direction.North] = Debuff.Bubble;
                    }

                    break;
                }
        }

        if(_playerDebuffs.Count == 4 && _enemyDebuffs.Count == 2)
        {
            var directions = Enum.GetValues<Direction>().ToList();
            foreach(var direction in directions) _enemyDebuffs.TryAdd(direction, Debuff.Bind);

            var start = directions.IndexOf(C.StartDirection);
            var adjustedDirections = directions.Skip(start).Concat(directions.Take(start)).ToList();
            if(C.Clockwise == Clockwise.CounterClockwise) adjustedDirections.Reverse();

            var fakeParty = FakeParty.Get().ToArray();
            foreach(var member in C.PartyMembers)
            {
                if(fakeParty.FirstOrDefault(x => x.Name.ToString() == member) is not { } player) continue;
                if(_playerDebuffs.TryGetValue(player.EntityId, out var debuff))
                {
                    var direction = adjustedDirections.First(x => _enemyDebuffs[x] != debuff);
                    _solvedDirections[member] = direction;
                    adjustedDirections.Remove(direction);
                }
            }

            if(_solvedDirections.Count != 4)
                DuoLog.Warning($"[{GetType().Name.Replace("_", " ")}] Failed to solve directions.");

            var myDirection = _solvedDirections.FirstOrDefault(x => x.Key == Player.Name).Value;
            var position = (myDirection, C.SafeFromEnemyClockwise) switch
            {
                (Direction.North, Clockwise.Clockwise) => new Vector2(10, -10),
                (Direction.North, Clockwise.CounterClockwise) => new Vector2(-10, -10),
                (Direction.East, Clockwise.Clockwise) => new Vector2(10, 10),
                (Direction.East, Clockwise.CounterClockwise) => new Vector2(10, -10),
                (Direction.South, Clockwise.Clockwise) => new Vector2(-10, 10),
                (Direction.South, Clockwise.CounterClockwise) => new Vector2(10, 10),
                (Direction.West, Clockwise.Clockwise) => new Vector2(-10, -10),
                (Direction.West, Clockwise.CounterClockwise) => new Vector2(-10, 10),
                _ => Vector2.Zero
            };

            if(Controller.TryGetElementByName("Bait", out var element))
            {
                element.SetOffPosition(position.ToVector3());
                element.radius = 2f;
                element.Enabled = true;
            }

            _state = State.Spread;
        }
    }

    private bool DrawPriorityList()
    {
        if(C.PartyMembers.Length != 4)
            C.PartyMembers = ["", "", "", ""];

        ImGuiEx.Text("Priority list");
        ImGui.SameLine();
        ImGuiEx.Spacing();
        if(ImGui.Button("Perform test")) SelfTest();

        ImGui.PushID("prio");
        ImGui.Text("Clockwise");
        for(var i = 0; i < C.PartyMembers.Length; i++)
        {
            ImGui.PushID($"prioelement{i}");
            ImGui.Text($"Character {i + 1}");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            ImGui.InputText($"##Character{i}", ref C.PartyMembers[i], 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if(ImGui.BeginCombo("##partysel", "Select from party"))
            {
                foreach(var x in FakeParty.Get())
                    if(ImGui.Selectable(x.Name.ToString()))
                        C.PartyMembers[i] = x.Name.ToString();
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if(ImGui.Button($"  T  ##{i}") && Svc.Targets.Target is IPlayerCharacter pc)
                C.PartyMembers[i] = pc.Name.ToString();
            ImGuiEx.Tooltip("Fill name from your current target");

            ImGui.PopID();
        }

        ImGui.Text("Counter Clockwise");

        ImGui.PopID();
        return false;
    }

    private void SelfTest()
    {
        Svc.Chat.PrintChat(new XivChatEntry
        {
            Message = new SeStringBuilder()
                .AddUiForeground($"= {GetType().Name.Replace("_", " ")} self-test =", (ushort)UIColor.LightBlue).Build()
        });
        var party = FakeParty.Get().ToArray();
        var isCorrect = C.PartyMembers.All(x => !string.IsNullOrEmpty(x));

        if(!isCorrect)
        {
            Svc.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground("Priority list is not filled correctly.", (ushort)UIColor.Red).Build()
            });
            return;
        }

        if(party.Length != 8)
        {
            isCorrect = false;
            Svc.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground("Can only be tested in content.", (ushort)UIColor.Red).Build()
            });
        }

        foreach(var player in party)
            if(C.PartyMembers.All(x => x != player.Name.ToString()))
            {
                isCorrect = false;
                Svc.Chat.PrintChat(new XivChatEntry
                {
                    Message = new SeStringBuilder()
                        .AddUiForeground($"Player {player.Name} is not in the priority list.", (ushort)UIColor.Red)
                        .Build()
                });
            }

        if(isCorrect)
            Svc.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground("Test Success!", (ushort)UIColor.Green).Build()
            });
        else
            Svc.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground("!!! Test failed !!!", (ushort)UIColor.Red).Build()
            });
    }


    public override void OnSettingsDraw()
    {
        if(ImGuiEx.CollapsingHeader("General"))
        {
            ImGui.Text("Party Members");
            DrawPriorityList();
            ImGui.Separator();
            ImGuiEx.EnumCombo("Clockwise", ref C.Clockwise);
            ImGuiEx.EnumCombo("Start Direction", ref C.StartDirection);
            ImGuiEx.EnumCombo("Safe from Enemy Clockwise", ref C.SafeFromEnemyClockwise);
            ImGuiEx.Tooltip("Clockwise direction to be safe from enemy");
            ImGui.Text("Bait Color:");
            ImGuiComponents.HelpMarker(
                "Change the color of the bait and the text that will be displayed on your bait.\nSetting different values makes it rainbow.");
            ImGui.Indent();
            ImGui.ColorEdit4("Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
            ImGui.SameLine();
            ImGui.ColorEdit4("Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
            ImGui.Unindent();
        }

        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            if(ImGui.Button("Reset")) Reset();

            ImGui.Text($"State: {_state}");
            ImGui.Text("Player Debuffs");
            ImGui.Indent();
            foreach(var (key, value) in _playerDebuffs)
                ImGui.Text($"{key.GetObject()?.Name} - {value}");
            ImGui.Unindent();
            ImGui.Text("Enemy Debuffs");
            ImGui.Indent();
            foreach(var (key, value) in _enemyDebuffs)
                ImGui.Text($"{key} - {value}");
            ImGui.Unindent();

            ImGui.Text("Solved Directions");
            ImGui.Indent();
            foreach(var (key, value) in _solvedDirections)
                ImGui.Text($"{key} - {value}");
            ImGui.Unindent();
        }
    }

    private enum Debuff
    {
        Bind,
        Bubble
    }

    private enum State
    {
        None,
        Start,
        Spread,
        Bait,
        End
    }

    private class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public Clockwise Clockwise = Clockwise.Clockwise;
        public string[] PartyMembers = ["", "", "", ""];
        public Clockwise SafeFromEnemyClockwise = Clockwise.Clockwise;
        public Direction StartDirection = Direction.North;
    }
}