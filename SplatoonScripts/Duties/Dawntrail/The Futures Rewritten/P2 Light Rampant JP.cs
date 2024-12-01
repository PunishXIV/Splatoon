using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.PartyFunctions;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P2_Light_Rampant_JP : SplatoonScript
{
    public enum Direction
    {
        None = -1,
        North = 270,
        NorthEast = 315 + 15,
        SouthEast = 45 - 15,
        South = 90,
        SouthWest = 135 + 15,
        NorthWest = 225 - 15
    }

    public enum State
    {
        None,
        Start,
        Split,
        End
    }

    private readonly HashSet<string> _aoeTargets = new();

    private State _state = State.None;
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(1, "Garume");

    public Config C => Controller.GetConfig<Config>();

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5,
        uint p6, ulong targetId,
        byte replaying)
    {
        if (_state == State.Start)
        {
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _aoeTargets.Clear();
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (_state is (State.Start or State.Split) && vfxPath == "vfx/lockon/eff/target_ae_s7k1.avfx")
        {
            if (target.GetObject() is IPlayerCharacter player) _aoeTargets.Add(player.Name.ToString());

            var count = 0;
            foreach (var aoeTarget in _aoeTargets)
                if (C.Players.Contains(aoeTarget))
                    count++;

            var direction = C.Directions[count];

            DuoLog.Warning($"Direction: {direction} Count: {count}");
            var radius = 16f;
            var center = new Vector2(100f, 100f);
            var angle = (int)direction;
            var x = center.X + radius * MathF.Cos(angle * MathF.PI / 180);
            var y = center.Y + radius * MathF.Sin(angle * MathF.PI / 180);

            if (Controller.TryGetElementByName("Bait", out var bait))
            {
                bait.Enabled = true;
                bait.SetOffPosition(new Vector3(x, 0, y));
            }

            _state = State.Split;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action.Value.RowId == 40213) _state = State.End;
    }

    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0)
        {
            radius = 4f,
            thicc = 6f,
            tether = true,
            overlayText = "<< Go Here >>",
            overlayFScale = 3f,
            overlayVOffset = 3f
        });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 40212) _state = State.Start;
    }

    public override void OnUpdate()
    {
        if (_state == State.Split)
            Controller.GetRegisteredElements()
                .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
        else
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnSettingsDraw()
    {
        if (C.Players.Count == 0) C.Players.Add("");
        var toRem = -1;
        for (var i = 0; i < C.Players.Count; i++)
        {
            ImGui.SetCursorPosX(30);
            ImGuiEx.Text($"{C.Players[i]}");
            ImGui.SameLine();
            if (ImGui.SmallButton("Delete##" + i)) toRem = i;
        }

        if (toRem != -1) C.Players.RemoveAt(toRem);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(120f);
        if (ImGui.BeginCombo("##partysel", "Select from party"))
        {
            foreach (var x in FakeParty.Get().Select(x => x.Name.ToString())
                         .Union(UniversalParty.Members.Select(x => x.Name)).ToHashSet())
                if (ImGui.Selectable(x))
                    C.Players.Add(x);
            ImGui.EndCombo();
        }

        foreach (var direction in C.Directions)
        {
            var dir = direction.Value;
            ImGui.SetCursorPosX(30);
            ImGui.Text(direction.Key.ToString());
            ImGui.SameLine();
            ImGuiEx.EnumCombo("##" + direction.Key, ref dir);
            C.Directions[direction.Key] = dir;
        }

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"State: {_state}");
            ImGuiEx.Text($"AOE Targets: {_aoeTargets.Print()}");
        }
    }


    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();

        public Dictionary<int, Direction> Directions = new()
        {
            [0] = Direction.None,
            [1] = Direction.None,
            [2] = Direction.None
        };

        public List<string> Players = [""];
    }
}