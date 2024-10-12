using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
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
    public enum StackSafeDirection
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
    private int _redSphereCount;

    private SafeDirection _safeDirection = SafeDirection.None;

    private StackSafeDirection _stackSafeDirection = StackSafeDirection.None;

    private State _state = State.None;
    public override HashSet<uint>? ValidTerritories => [968];

    public override Metadata? Metadata => new(1, "Garume");

    private Config C => Controller.GetConfig<Config>();

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_state != State.None) return;
        if (set.Action is null) return;
        if (set.Action.RowId == WrothFlamesCastId) _state = State.Start;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (_state != State.Stack) return;
        if (_heatTailCastIds.Contains(castId))
        {
            if (source.GetObject() is not IBattleChara sourceChara) return;
            _state = State.Split;
            _safeDirection = Math.Abs(sourceChara.Position.Z - 100f) > 0.1f
                ? SafeDirection.Center
                : _stackSafeDirection switch
                {
                    StackSafeDirection.NorthEast => SafeDirection.North,
                    StackSafeDirection.NorthWest => SafeDirection.North,
                    StackSafeDirection.SouthEast => SafeDirection.South,
                    StackSafeDirection.SouthWest => SafeDirection.South,
                    _ => SafeDirection.None
                };
            var baitPosition = GetBaitPosition(Player.Object.EntityId, _safeDirection);
            if (Controller.TryGetElementByName("Bait", out var element))
            {
                element.Enabled = true;
                element.SetOffPosition(baitPosition.ToVector3(0));
            }
        }
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
            _stackSafeDirection = (isNorth, isEast) switch
            {
                (true, true) => StackSafeDirection.SouthWest,
                (true, false) => StackSafeDirection.SouthEast,
                (false, true) => StackSafeDirection.NorthWest,
                (false, false) => StackSafeDirection.NorthEast
            };

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

    private Vector2 GetBaitPosition(uint characterEntityId, SafeDirection safeDirection)
    {
        var myDebuff = _debuffs.Where(x => x.Value.Contains(characterEntityId)).Select(x => (Debuff)x.Key)
            .FirstOrDefault();
        var characterName = FakeParty.Get().First(x => x.EntityId == characterEntityId).Name.ToString();
        var myDebuffCharacterNames = _debuffs[(ushort)myDebuff].Select(x => x.GetObject()?.Name.ToString()).ToList();
        var myDebuffPriorityList = C.Priority.Where(x => myDebuffCharacterNames.Contains(x)).ToList();
        var myDebuffPriority = myDebuffPriorityList.IndexOf(characterName);
        var x = GetDebuffPositionX(myDebuff, myDebuffPriority);
        var y = safeDirection switch
        {
            SafeDirection.North => 115f,
            SafeDirection.Center => 100f,
            SafeDirection.South => 85f,
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
        DrawPriorityList();
        ImGui.ColorEdit4("Bait Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Bait Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);

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

            ImGui.Text($"My Bait Position: {GetBaitPosition(Player.Object.EntityId, SafeDirection.North)}");
            ImGui.Text($"Safe Direction: {_safeDirection}");
            ImGui.Text($"Stack Safe Direction: {_stackSafeDirection}");
        }
    }

    private enum Debuff : ushort
    {
        Spread = 2758,
        Stack = 2759,
        None = ushort.MaxValue
    }

    private enum SafeDirection
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
        public string[] Priority = ["", "", "", "", "", "", "", ""];
    }
}