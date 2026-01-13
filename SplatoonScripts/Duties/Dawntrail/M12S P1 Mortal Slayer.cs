using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using ECommons.Throttlers;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M12S_P1_Mortal_Slayer : SplatoonScript
{
    public enum BallKind { Green, Purple }
    public enum Direction { East, West }
    private const uint CastStart = 46229;
    private const uint ActionHitA = 46232;
    private const uint ActionHitB = 46230;
    private const uint BallPurpleId = 19200;
    private const uint BallGreenId = 19201;

    private int _actiondBallCount;
    private PlayerData[] _playerOrderForBalls = [];
    private List<(BallKind Kind, Direction Dir, int Wave)> _spawnedBalls = [];
    private int _waveState;
    public override Metadata Metadata => new(1, "Garume");
    public override HashSet<uint>? ValidTerritories => [1327];

    public Config C => Controller.GetConfig<Config>();

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if (packet->ActionID != CastStart) return;
        _waveState = -1;
        _spawnedBalls.Clear();
        _actiondBallCount = 0;
        _playerOrderForBalls = [];
    }

    public override void OnSetup()
    {
        Controller.RegisterElement("Guide", new Element(0)
        {
            radius = 2f, thicc = 10f, tether = true, overlayBGColor = 0xFF000000, overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 2f, overlayFScale = 2f
        });
    }

    public override void OnUpdate()
    {
        if (_spawnedBalls.Count == 8 && EzThrottler.Throttle("M12S_Mortal_Slayer_TryBuildPlayerOrder", 100))
            BuildPlayerOrder();

        var e = Controller.GetElementByName("Guide");
        e.Enabled = false;
        e.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
        e.overlayText = "";

        if (_waveState is <= 0 or >= 5) return;

        var me = _playerOrderForBalls.FirstOrDefault(x => x.ObjectId == Controller.BasePlayer.EntityId);
        if (me == null) return;
        var currentWave = _waveState;
        var myTurn = currentWave == me.Wave;
        e.Enabled = true;
        e.overlayText = me.Text;
        e.radius = myTurn ? 2f : 0f;
        e.thicc = myTurn ? 10f : 0f;
        e.tether = myTurn;
        e.SetRefPosition(myTurn
            ? CalcPos(me.Direction, me.First, me.Offset).ToVector3()
            : Controller.BasePlayer.Position);
    }

    private static Vector2 CalcPos(Direction dir, bool first, float offset)
    {
        var dx = first ? -1f : 1f;
        var east = dir == Direction.East;
        var pos = new Vector2(east ? 107f : 93f, 90f);
        pos.X += dx * offset;
        pos.Y += (east ? -dx : dx) * offset;
        return pos;
    }

    private bool BuildPlayerOrder()
    {
        var prio = C.PriorityData.GetPlayers(_ => true)?.ToList();
        if (prio?.Count < 8) { DuoLog.Warning($"PriorityData.GetPlayers() returned insufficient players. Count={prio?.Count ?? 0}"); return false; }
        var ordered = _spawnedBalls.Chunk(2).SelectMany(chunk => chunk.OrderBy(b => b.Dir == Direction.West ? 0 : 1)).ToArray();
        var ret = new PlayerData[ordered.Length];
        var purpleSlot = 0; var greenSlot = 2;
        for (var i = 0; i < ordered.Length; i++)
        {
            var b = ordered[i];
            var idx = b.Kind == BallKind.Purple ? purpleSlot++ : greenSlot++;
            if ((uint)idx >= (uint)prio.Count) { DuoLog.Warning($"Priority index out of range. idx={idx}, prioCount={prio.Count}"); return false; }
            var p = prio[idx];
            ret[i] = new PlayerData { Direction = b.Dir, Kind = b.Kind, ObjectId = p.IGameObject.EntityId, Name = p.IGameObject.Name.ToString() };
        }
        for (var i = 0; i < ret.Length; i += 2)
        {
            var a = ret[i]; var b = ret[i + 1]; var w = i / 2 + 1; var off = a.Direction == b.Direction ? 3f : 0f;
            a.Wave = b.Wave = w; a.First = true; b.First = false; a.Order = i + 1; b.Order = i + 2; a.Offset = b.Offset = off;
            a.Text = $"{(a.Direction == Direction.East ? "E" : "W")} Wave:{w}"; b.Text = $"{(b.Direction == Direction.East ? "E" : "W")} Wave:{w}";
        }
        _playerOrderForBalls = ret;
        return true;
    }

    public override void OnObjectCreation(IntPtr newObjectPtr)
    {
        if (_waveState != -1) return;
        _ = new TickScheduler(() =>
        {
            var gameObject = Svc.Objects.FirstOrDefault(o => o.Address == newObjectPtr);
            var id = gameObject?.DataId ?? 0;
            if (id is not (BallPurpleId or BallGreenId)) return;
            _spawnedBalls.Add((id == BallPurpleId ? BallKind.Purple : BallKind.Green, gameObject.Position.X > 100 ? Direction.East : Direction.West, _spawnedBalls.Count / 2 + 1));
            if (_spawnedBalls.Count % 2 == 0) BuildPlayerOrder();
            if (_spawnedBalls.Count < 8) return;
            _waveState = 1;
            _spawnedBalls = _spawnedBalls.Chunk(2)
                .SelectMany(chunk => chunk.OrderBy(b => b.Dir == Direction.West ? 0 : 1)).ToList();
        });
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action.Value.RowId is ActionHitA or ActionHitB && ++_actiondBallCount % 2 == 0 &&
            _waveState is >= 1 and <= 4) _waveState++;
    }

    public override void OnSettingsDraw()
    {
        C.PriorityData.Draw();
        ImGui.ColorEdit4("Color1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Color2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
        if (ImGui.CollapsingHeader("Guide (JP)"))
        {
            ImGui.TextWrapped("西側をMT組、東側をST組と見ます。優先順位の1・2番は紫球を受け、それ以降は順番に緑球を受けます。");
            ImGui.TextWrapped("例：紫球が MT → OT、緑球の優先が H→近接→遠隔 の場合は「MT OT H1 H2 M1 M2 R1 R2」と入力してください。");
        }
        if (ImGui.CollapsingHeader("Guide (EN)"))
        {
            ImGui.TextWrapped("West = MT, East = OT. Priority 1–2 take purple orbs; everyone after that takes green orbs in order.");
            ImGui.TextWrapped("Example: if purple is MT → OT and green priority is H → Melee → Ranged, enter: \"MT OT H1 H2 M1 M2 R1 R2\".");
        }
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_waveState}");
            ImGuiEx.EzTable("Balls Information", _spawnedBalls.SelectMany(x => new[]
            {
                new ImGuiEx.EzTableEntry("Wave", () => ImGuiEx.Text(x.Wave.ToString())),
                new ImGuiEx.EzTableEntry("Kind", () => ImGuiEx.Text(x.Kind.ToString())),
                new ImGuiEx.EzTableEntry("Direction", () => ImGuiEx.Text(x.Dir.ToString()))
            }));

            ImGuiEx.EzTable("Player Order For Balls", _playerOrderForBalls.SelectMany(x => new[]
            {
                new ImGuiEx.EzTableEntry("Wave", () => ImGuiEx.Text(x.Wave.ToString())),
                new ImGuiEx.EzTableEntry("Kind", () => ImGuiEx.Text(x.Kind.ToString())),
                new ImGuiEx.EzTableEntry("Direction", () => ImGuiEx.Text(x.Direction.ToString())),
                new ImGuiEx.EzTableEntry("ObjectId", () => ImGuiEx.Text(x.ObjectId.ToString())),
                new ImGuiEx.EzTableEntry("Name", () => ImGuiEx.Text(x.Name))
            }));

            ImGui.Text($"My index: {Array.FindIndex(_playerOrderForBalls, x => x.ObjectId == Controller.BasePlayer.EntityId)}");
        }
    }
    public class PlayerData { public Direction Direction; public bool First; public BallKind Kind; public string Name = ""; public uint ObjectId; public float Offset; public string Text = ""; public int Wave, Order; }

    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public PriorityData PriorityData = new();
    }
}
