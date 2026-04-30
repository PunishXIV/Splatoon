using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Plugin.Ipc.Exceptions;
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
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Splatoon;
using Splatoon.Serializables;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Splatoon.Splatoon;

using ECommons.DalamudServices.Legacy;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public unsafe class P6_Wroth_Flames : SplatoonScript
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

    public override Metadata? Metadata => new(8, "Garume, damolitionn, NightmareXIV");

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
            if(C.UsingAMs == UsingAMs.Yes && C.UseAMAssignments)
            {
                if(Controller.TryGetElementByName("Bait", out var element))
                {
                    element.Enabled = false;
                }
                var suffix = _safeSpreadDirection switch
                {
                    SafeSpreadDirection.North => "North",
                    SafeSpreadDirection.South => "South",
                    _ => "Center"
                };
                string el = "";
                var m = MarkingController.Instance();
                if(m->Markers[0].ObjectId == BasePlayer.ObjectId) el = "Attack1";
                if(m->Markers[1].ObjectId == BasePlayer.ObjectId) el = "Attack2";
                if(m->Markers[2].ObjectId == BasePlayer.ObjectId) el = "Attack3";
                if(m->Markers[3].ObjectId == BasePlayer.ObjectId) el = "Attack4";
                if(m->Markers[5].ObjectId == BasePlayer.ObjectId) el = "Bind1";
                if(m->Markers[6].ObjectId == BasePlayer.ObjectId) el = "Bind2";
                if(m->Markers[8].ObjectId == BasePlayer.ObjectId) el = "Ignore1";
                if(m->Markers[9].ObjectId == BasePlayer.ObjectId) el = "Ignore2";
                if(Controller.TryGetElementByName(el+suffix, out var e))
                {
                    e.Enabled = true;
                }
                else
                {
                    PluginLog.Error($"Could not find {el + suffix}");
                }
            }
            else
            {
                var baitPosition = GetBaitPosition(Player.Object.EntityId, _safeSpreadDirection);
                if(Controller.TryGetElementByName("Bait", out var element))
                {
                    element.Enabled = true;
                    element.SetOffPosition(baitPosition.ToVector3(0));
                }
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
        var isCorrect = C.Priority.GetFirstValidList() is not null;

        if (!isCorrect)
        {
            Svc.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground("!!! Test failed !!!", (ushort)UIColor.Red)
                    .AddUiForeground("Priority list is not filled correctly.", (ushort)UIColor.Red).Build()
            });
            return;
        }

        Svc.Chat.PrintChat(new XivChatEntry
        {
            Message = new SeStringBuilder()
                .AddUiForeground("Test Success!", (ushort)UIColor.Green).Build()
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

            if (C.PrioritizeSecondRedBallDiagonal) Array.Reverse(redSphereSafeDirection);
            if (C.PrioritizeWest)
                if (redSphereSafeDirection[1].ToString().Contains("West"))
                    Array.Reverse(redSphereSafeDirection);

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
            Controller.Hide();
            return;
        }

        Controller.GetRegisteredElements()
            .Each(x => x.Value.color = Controller.AttentionColor);
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
        if (C.UsingAMs == UsingAMs.No)
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
        }
        else
        {
            var m = MarkingController.Instance();

            if (m->Markers[0] == Player.Object.EntityId)
                return 85f;
            if (m->Markers[1] == Player.Object.EntityId)
                return 91f;
            if (m->Markers[2] == Player.Object.EntityId)
                return 97f;
            if (m->Markers[3] == Player.Object.EntityId)
                return 103f;
            if (m->Markers[5] == Player.Object.EntityId || m->Markers[6] == Player.Object.EntityId)
                return 109f;
            if (m->Markers[8] == Player.Object.EntityId || m->Markers[9] == Player.Object.EntityId)
                return 115;
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

        foreach(var x in ((string, string)[])[("North", "85.0"), ("Center", "100.0"), ("South", "115.0")])
        {
            Controller.RegisterElementFromCode($"Attack1{x.Item1}", $$"""{"Name":"","refX":79.0,"refY":{{x.Item2}},"radius":1.0,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"tether":true}""");
            Controller.RegisterElementFromCode($"Attack2{x.Item1}", $$"""{"Name":"","refX":87.4,"refY":{{x.Item2}},"radius":1.0,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"tether":true}""");
            Controller.RegisterElementFromCode($"Attack3{x.Item1}", $$"""{"Name":"","refX":95.8,"refY":{{x.Item2}},"radius":1.0,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"tether":true}""");
            Controller.RegisterElementFromCode($"Attack4{x.Item1}", $$"""{"Name":"","refX":104.2,"refY":{{x.Item2}},"radius":1.0,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"tether":true}""");
            Controller.RegisterElementFromCode($"Ignore1{x.Item1}", $$"""{"Name":"","refX":112.6,"refY":{{x.Item2}},"radius":1.0,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"tether":true}""");
            Controller.RegisterElementFromCode($"Ignore2{x.Item1}", $$"""{"Name":"","refX":112.6,"refY":{{x.Item2}},"radius":1.0,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"tether":true}""");
            Controller.RegisterElementFromCode($"Bind1{x.Item1}", $$"""{"Name":"","refX":121.0,"refY":{{x.Item2}},"radius":1.0,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"tether":true}""");
            Controller.RegisterElementFromCode($"Bind2{x.Item1}", $$"""{"Name":"","refX":121.0,"refY":{{x.Item2}},"radius":1.0,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"tether":true}""");
        }
    }

    private Vector2 GetBaitPosition(uint characterEntityId, SafeSpreadDirection safeSpreadDirection)
    {
        var myDebuff = _debuffs.Where(x => x.Value.Contains(characterEntityId)).Select(x => (Debuff)x.Key).FirstOrDefault();
        var characterName = FakeParty.Get().First(x => x.EntityId == characterEntityId).Name.ToString();
        var myDebuffCharacterNames = _debuffs[(ushort)myDebuff].Select(x => x.GetObject()?.Name.ToString()).ToList();
        var myDebuffPriorityList = C.Priority.GetPlayers(x => myDebuffCharacterNames.Contains(x.Name))?.Select(x => x.Name);
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

        ImGui.Text("Using AMs?");
        ImGuiEx.EnumRadio(ref C.UsingAMs, true);

        if (C.UsingAMs == UsingAMs.No)
        {
            ImGui.Indent();
            C.Priority.Draw();
            ImGui.Unindent();
        }
        else
        {
            ImGui.Indent();
            ImGui.Checkbox("Use static automarker position assignments", ref C.UseAMAssignments);
            ImGui.Unindent();
        }

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

            if (C.UsingAMs == UsingAMs.No)
            {
                var prioNames = C.Priority.GetPlayers(x => true)?.Select(x => x.Name).ToList();
                if (prioNames != null)
                {
                    for (int i = 0; i < prioNames.Count; i++)
                        ImGui.Text($"Priority {i + 1}: {prioNames[i]}");
                }
            }
                ImGui.Unindent();

            ImGui.Text($"Safe Spread Direction: {_safeSpreadDirection}");
            ImGui.Text($"Stack Safe Direction: {_stackSafeDirection}");

            var array = MarkingController.Instance()->Markers;
            for(var i = 0; i < array.Length; i++)
            {
                var x = array[i];
                ImGuiEx.Text($"{i}: {x.ObjectId} - {x.ObjectId.GetObject()}");
            }
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

    private enum UsingAMs
    {
        Yes,
        No
    }

    private class Config : IEzConfig
    {
        public PriorityData Priority = new();
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public bool PrioritizeSecondRedBallDiagonal;
        public bool PrioritizeWest;
        public bool ShouldCheckOnStart;
        public UsingAMs UsingAMs = UsingAMs.Yes;
        public bool UseAMAssignments = false;
    }
}