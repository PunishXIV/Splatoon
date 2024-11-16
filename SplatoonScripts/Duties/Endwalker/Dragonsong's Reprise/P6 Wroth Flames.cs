using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
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
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon;
using Splatoon.Serializables;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public class P6_Wroth_Flames : SplatoonScript
{
    private const uint WrothFlamesCastId = 27973;
    private const ushort SpreadDebuffId = 2758;
    private const ushort StackDebuffId = 2759;
    private const ushort NoDebuffId = ushort.MaxValue;

    private readonly Dictionary<ushort, List<uint>> _debuffs =
        new()
        {
            { (ushort)Debuff.Spread, [] },
            { (ushort)Debuff.Stack, [] },
            { (ushort)Debuff.None, [] }
        };

    private readonly uint[] _heatTailCastIds = [27949, 27950];
    private readonly uint[] _heatWingCastIds = [27947, 27948];
    private int _redSphereCount;

    private SafeSpreadDirection _safeSpreadDirection = SafeSpreadDirection.None;

    private StackSafeDirection _stackSafeDirection = StackSafeDirection.None;

    private State _state = State.None;
    public override HashSet<uint>? ValidTerritories => [968];

    public override Metadata? Metadata => new(5, "Garume");

    private Config C => Controller.GetConfig<Config>();

    private IBattleChara? Hraesvelgr => Svc.Objects
        .Where(o => o.IsTargetable)
        .FirstOrDefault(o => o.DataId == 0x3145) as IBattleChara;

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_state != State.None) return;
        if (set.Action is null) return;
        if (set.Action.Value.RowId == WrothFlamesCastId) _state = State.Start;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (_state != State.Stack) return;
        var isHeatTail = _heatTailCastIds.Contains(castId);
        var isHeatWing = _heatWingCastIds.Contains(castId);
        if (isHeatTail || isHeatWing)
        {
            _state = State.Split;
            _safeSpreadDirection = isHeatWing
                ? SafeSpreadDirection.Center
                : _stackSafeDirection switch
                {
                    StackSafeDirection.NorthEast => SafeSpreadDirection.South,
                    StackSafeDirection.NorthWest => SafeSpreadDirection.South,
                    StackSafeDirection.SouthEast => SafeSpreadDirection.North,
                    StackSafeDirection.SouthWest => SafeSpreadDirection.North,
                    _ => SafeSpreadDirection.None
                };
            var baitPosition = GetBaitPosition(Player.Object.EntityId, _safeSpreadDirection);
            if (Controller.TryGetElementByName("Bait", out var element))
            {
                element.Enabled = true;
                element.SetOffPosition(baitPosition.ToVector3(0));
            }
        }
    }
    
    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (!C.ShouldCheckOnStart)
            return;
        if (category == DirectorUpdateCategory.Commence ||
            (category == DirectorUpdateCategory.Recommence && Controller.Phase == 2))
            SelfTest();
    }
    
    private void SelfTest()
    {
        Svc.Chat.PrintChat(new XivChatEntry
        {
            Message = new SeStringBuilder()
                .AddUiForeground("= P6 Wroth Flames self-test =", (ushort)UIColor.LightBlue).Build()
        });
        var party = FakeParty.Get().ToArray();
        var isCorrect = C.Priority.All(x => !string.IsNullOrEmpty(x));

        if (!isCorrect)
        {
            Svc.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground("Priority list is not filled correctly.", (ushort)UIColor.Red).Build()
            });
            return;
        }

        if (party.Length != 8)
        {
            isCorrect = false;
            Svc.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground("Can only be tested in content.", (ushort)UIColor.Red).Build()
            });
        }

        foreach (var player in party)
            if (C.Priority.All(x => x != player.Name.ToString()))
            {
                isCorrect = false;
                Svc.Chat.PrintChat(new XivChatEntry
                {
                    Message = new SeStringBuilder()
                        .AddUiForeground($"Player {player.Name} is not in the priority list.", (ushort)UIColor.Red)
                        .Build()
                });
            }

        if (isCorrect)
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

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (_state != State.DebuffGained) return;
        if (vfxPath != "vfx/common/eff/mon_pop1t.avfx") return;
        if (target.GetObject() is not IBattleChara redSphere) return;
        _redSphereCount++;
        if (_redSphereCount == 6)
        {
            _state = State.Stack;
            var isNorth = redSphere.Position.Z < 100f;
            var isEast = redSphere.Position.X > 100f;

            StackSafeDirection[] redSphereSafeDirection = (isNorth, isEast) switch
            {
                (true, true) => [StackSafeDirection.SouthEast, StackSafeDirection.SouthWest],
                (true, false) => [StackSafeDirection.SouthWest, StackSafeDirection.SouthEast],
                (false, true) => [StackSafeDirection.NorthEast, StackSafeDirection.NorthWest],
                (false, false) => [StackSafeDirection.NorthWest, StackSafeDirection.NorthEast]
            };

            if (C.PrioritizeSecondRedBallDiagonal) redSphereSafeDirection = redSphereSafeDirection.Reverse().ToArray();
            if (C.PrioritizeWest)
                if (redSphereSafeDirection[1].ToString().Contains("West"))
                    redSphereSafeDirection = redSphereSafeDirection.Reverse().ToArray();

            var hraesvelgrPositionX = Hraesvelgr?.Position.X ?? 100f;
            StackSafeDirection[] hraesvelgrSafeDirection = Math.Abs(hraesvelgrPositionX - 100f) < 0.1f
                ?
                [
                    StackSafeDirection.NorthEast, StackSafeDirection.NorthWest,
                    StackSafeDirection.SouthEast, StackSafeDirection.SouthWest
                ]
                : hraesvelgrPositionX > 105f
                    ? [StackSafeDirection.NorthWest, StackSafeDirection.SouthWest]
                    : [StackSafeDirection.NorthEast, StackSafeDirection.SouthEast];

            _stackSafeDirection = redSphereSafeDirection.Intersect(hraesvelgrSafeDirection).First();

            var stackPosition = GetStackPosition(_stackSafeDirection);
            if (Controller.TryGetElementByName("Bait", out var element))
            {
                element.Enabled = true;
                element.SetOffPosition(stackPosition.ToVector3(0));
            }
        }
    }

    public override void OnUpdate()
    {
        if (_state is State.None or State.End)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            return;
        }

        Controller.GetRegisteredElements()
            .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
    }

    private Vector2 GetStackPosition(StackSafeDirection direction)
    {
        return direction switch
        {
            StackSafeDirection.NorthEast => new Vector2(120, 80),
            StackSafeDirection.NorthWest => new Vector2(80, 80),
            StackSafeDirection.SouthEast => new Vector2(120, 120),
            StackSafeDirection.SouthWest => new Vector2(80, 120),
            StackSafeDirection.None => new Vector2(100, 100),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public override void OnReset()
    {
        _debuffs[SpreadDebuffId].Clear();
        _debuffs[StackDebuffId].Clear();
        _debuffs[NoDebuffId].Clear();
        _state = State.None;
        _redSphereCount = 0;
    }

    private float GetDebuffPositionX(Debuff debuff, int index)
    {
        switch (debuff, index)
        {
            case (Debuff.Stack, 0):
            case (Debuff.None, 0):
                return 85f;
            case (Debuff.Stack, 1):
            case (Debuff.None, 1):
                return 91f;
            case (Debuff.Spread, 0):
                return 97f;
            case (Debuff.Spread, 1):
                return 103f;
            case (Debuff.Spread, 2):
                return 109f;
            case (Debuff.Spread, 3):
                return 115f;
        }

        return default;
    }

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            thicc = 6f,
            radius = 1f,
            tether = true,
            LineEndA = LineEnd.Arrow
        };
        Controller.TryRegisterElement("Bait", element);
    }

    private Vector2 GetBaitPosition(uint characterEntityId, SafeSpreadDirection safeSpreadDirection)
    {
        var myDebuff = _debuffs.Where(x => x.Value.Contains(characterEntityId)).Select(x => (Debuff)x.Key)
            .FirstOrDefault();
        var characterName = FakeParty.Get().First(x => x.EntityId == characterEntityId).Name.ToString();
        var myDebuffCharacterNames = _debuffs[(ushort)myDebuff].Select(x => x.GetObject()?.Name.ToString()).ToList();
        var myDebuffPriorityList = C.Priority.Where(x => myDebuffCharacterNames.Contains(x)).ToList();
        var myDebuffPriority = myDebuffPriorityList.IndexOf(characterName);
        var x = GetDebuffPositionX(myDebuff, myDebuffPriority);
        var y = safeSpreadDirection switch
        {
            SafeSpreadDirection.North => 85f,
            SafeSpreadDirection.Center => 100f,
            SafeSpreadDirection.South => 115f,
            _ => 100f
        };

        return new Vector2(x, y);
    }

    public override void OnGainBuffEffect(uint sourceId, Status Status)
    {
        if (_state != State.Start) return;
        if (_debuffs.TryGetValue(Status.StatusId, out var list)) list.Add(sourceId);
        if (_debuffs[SpreadDebuffId].Count == 4 && _debuffs[StackDebuffId].Count == 2)
        {
            foreach (var player in FakeParty.Get())
            {
                if (_debuffs[SpreadDebuffId].Contains(player.EntityId))
                    continue;
                if (_debuffs[StackDebuffId].Contains(player.EntityId))
                    continue;
                _debuffs[NoDebuffId].Add(player.EntityId);
            }

            _state = State.DebuffGained;
        }
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status Status)
    {
        if (_state != State.Split) return;
        if (Status.StatusId == StackDebuffId) _state = State.End;
    }

    private bool DrawPriorityList()
    {
        if (C.Priority.Length != 8)
            C.Priority = ["", "", "", "", "", "", "", ""];

        ImGuiEx.Text("Priority list");
        ImGui.SameLine();
        ImGuiEx.Spacing();
        if (ImGui.Button("Perform test")) SelfTest();
        
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
                foreach (var x in FakeParty.Get())
                    if (ImGui.Selectable(x.Name.ToString()))
                        C.Priority[i] = x.Name.ToString();
                ImGui.EndCombo();
            }

            ImGui.PopID();
        }

        ImGui.PopID();
        return false;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("General Settings");
        ImGui.Indent();
        ImGui.ColorEdit4("Bait Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Bait Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
        ImGui.Checkbox("Check on start", ref C.ShouldCheckOnStart);
        ImGui.Unindent();

        ImGui.Text("Stack Settings");
        ImGui.Indent();
        ImGui.Checkbox("Prioritize Second Red Ball Diagonal", ref C.PrioritizeSecondRedBallDiagonal);
        if (C.PrioritizeSecondRedBallDiagonal)
            C.PrioritizeWest = false;
        ImGui.Checkbox("Prioritize West", ref C.PrioritizeWest);
        if (C.PrioritizeWest)
            C.PrioritizeSecondRedBallDiagonal = false;
        ImGui.Unindent();
        
        ImGui.Text("Spread Settings");
        ImGui.Indent();
        DrawPriorityList();
        ImGui.Unindent();
        
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text("Debuffs");
            ImGui.Indent();
            foreach (var debuff in _debuffs)
            {
                ImGui.Text($"Debuff: {debuff.Key}");
                ImGui.Indent();
                foreach (var entityId in debuff.Value)
                {
                    var player = FakeParty.Get().First(x => x.EntityId == entityId);
                    ImGui.Text($"{player.Name}");
                }

                ImGui.Unindent();
            }

            ImGui.Unindent();

            ImGui.Text($"Safe Spread Direction: {_safeSpreadDirection}");
            ImGui.Text($"Stack Safe Direction: {_stackSafeDirection}");
        }
    }

    private enum StackSafeDirection
    {
        None,
        NorthEast,
        NorthWest,
        SouthEast,
        SouthWest
    }

    private enum State
    {
        None,
        Start,
        DebuffGained,
        Stack,
        Split,
        End
    }

    private enum Debuff : ushort
    {
        Spread = 2758,
        Stack = 2759,
        None = ushort.MaxValue
    }


    private enum SafeSpreadDirection
    {
        North,
        Center,
        South,
        None
    }

    private class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public bool PrioritizeSecondRedBallDiagonal;
        public bool PrioritizeWest;
        public string[] Priority = ["", "", "", "", "", "", "", ""];
        public bool ShouldCheckOnStart;
    }
}
