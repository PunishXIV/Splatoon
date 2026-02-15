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
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    public override Metadata Metadata => new(4, "Garume, Enthusiastus");
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
            radius = 2f,
            thicc = 10f,
            tether = true,
            overlayBGColor = 0xFF000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 2f,
            overlayFScale = 2f
        });
    }

    public override void OnReset()
    {
        _waveState = -1;
        _spawnedBalls.Clear();
        _actiondBallCount = 0;
        _playerOrderForBalls = [];
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

        var westTank = prio[0]; // MT
        var westHealer = prio[1]; // H1
        var westMelee = prio[2]; // D1
        var westRanged = prio[3]; // D3
        var eastTank = prio[4]; // ST
        var eastHealer = prio[5]; // H2
        var eastMelee = prio[6]; // D2
        var eastRanged = prio[7]; // D4

        // Purple is always on one side (strategy assumption)
        var purpleBalls = _spawnedBalls.Where(x => x.Kind == BallKind.Purple).ToList();
        if (purpleBalls.Count != 2)
        {
            DuoLog.Warning($"Purple ball count is not 2. Count={purpleBalls.Count}");
            return false;
        }
        var purpleSide = purpleBalls[0].Dir;

        // Build per-side assignment queues (this is the key change)
        List<(uint ObjectId, string Name)> westPurple = new();
        List<(uint ObjectId, string Name)> eastPurple = new();
        List<(uint ObjectId, string Name)> westGreen = new();
        List<(uint ObjectId, string Name)> eastGreen = new();
        if (C.PurpleRelative)
        {
            if (purpleSide == Direction.West)
            {
                // West has 2 purples, East has 4 greens.
                // Tank without purple (EastTank) swaps with ranged on purple side (WestRanged).
                westPurple.Add((westTank.IGameObject.EntityId, westTank.IGameObject.Name.ToString())); // 1st purple
                westPurple.Add((eastTank.IGameObject.EntityId, eastTank.IGameObject.Name.ToString())); // 2nd purple (swapped tank)

                westGreen.Add((westHealer.IGameObject.EntityId, westHealer.IGameObject.Name.ToString())); // green #1
                westGreen.Add((eastHealer.IGameObject.EntityId, eastHealer.IGameObject.Name.ToString()));  // green #2

                eastGreen.Add((westMelee.IGameObject.EntityId, eastMelee.IGameObject.Name.ToString())); // green #1
                eastGreen.Add((eastMelee.IGameObject.EntityId, eastMelee.IGameObject.Name.ToString()));  // green #2
                eastGreen.Add((westRanged.IGameObject.EntityId, westRanged.IGameObject.Name.ToString())); // green #3
                eastGreen.Add((eastRanged.IGameObject.EntityId, eastRanged.IGameObject.Name.ToString())); // green #4 (swapped ranged)
            }
            else
            {
                // East has 2 purples, West has 4 greens.
                // Tank without purple (WestTank) swaps with ranged on purple side (EastRanged).
                eastPurple.Add((westTank.IGameObject.EntityId, westTank.IGameObject.Name.ToString())); // 1st purple
                eastPurple.Add((eastTank.IGameObject.EntityId, eastTank.IGameObject.Name.ToString())); // 2nd purple (swapped tank)

                eastGreen.Add((westHealer.IGameObject.EntityId, westHealer.IGameObject.Name.ToString())); // green #1
                eastGreen.Add((eastHealer.IGameObject.EntityId, eastHealer.IGameObject.Name.ToString()));  // green #2

                westGreen.Add((westMelee.IGameObject.EntityId, eastMelee.IGameObject.Name.ToString())); // green #1
                westGreen.Add((eastMelee.IGameObject.EntityId, eastMelee.IGameObject.Name.ToString()));  // green #2
                westGreen.Add((westRanged.IGameObject.EntityId, westRanged.IGameObject.Name.ToString())); // green #3
                westGreen.Add((eastRanged.IGameObject.EntityId, eastRanged.IGameObject.Name.ToString())); // green #4 (swapped ranged)
            }
        }
        else
        {
            if (purpleSide == Direction.West)
            {
                // West has 2 purples, East has 4 greens.
                // Tank without purple (EastTank) swaps with ranged on purple side (WestRanged).
                westPurple.Add((westTank.IGameObject.EntityId, westTank.IGameObject.Name.ToString())); // 1st purple
                westPurple.Add((eastTank.IGameObject.EntityId, eastTank.IGameObject.Name.ToString())); // 2nd purple (swapped tank)

                westGreen.Add((westHealer.IGameObject.EntityId, westHealer.IGameObject.Name.ToString())); // green #1
                westGreen.Add((westMelee.IGameObject.EntityId, westMelee.IGameObject.Name.ToString()));  // green #2

                eastGreen.Add((eastHealer.IGameObject.EntityId, eastHealer.IGameObject.Name.ToString())); // green #1
                eastGreen.Add((eastMelee.IGameObject.EntityId, eastMelee.IGameObject.Name.ToString()));  // green #2
                eastGreen.Add((eastRanged.IGameObject.EntityId, eastRanged.IGameObject.Name.ToString())); // green #3
                eastGreen.Add((westRanged.IGameObject.EntityId, westRanged.IGameObject.Name.ToString())); // green #4 (swapped ranged)
            }
            else
            {
                // East has 2 purples, West has 4 greens.
                // Tank without purple (WestTank) swaps with ranged on purple side (EastRanged).
                eastPurple.Add((eastTank.IGameObject.EntityId, eastTank.IGameObject.Name.ToString())); // 1st purple
                eastPurple.Add((westTank.IGameObject.EntityId, westTank.IGameObject.Name.ToString())); // 2nd purple (swapped tank)

                eastGreen.Add((eastHealer.IGameObject.EntityId, eastHealer.IGameObject.Name.ToString())); // green #1
                eastGreen.Add((eastMelee.IGameObject.EntityId, eastMelee.IGameObject.Name.ToString()));  // green #2

                westGreen.Add((westHealer.IGameObject.EntityId, westHealer.IGameObject.Name.ToString())); // green #1
                westGreen.Add((westMelee.IGameObject.EntityId, westMelee.IGameObject.Name.ToString()));  // green #2
                westGreen.Add((westRanged.IGameObject.EntityId, westRanged.IGameObject.Name.ToString())); // green #3
                westGreen.Add((eastRanged.IGameObject.EntityId, eastRanged.IGameObject.Name.ToString())); // green #4 (swapped ranged)
            }
        }


        var ret = new PlayerData[_spawnedBalls.Count];
        var wp = 0; var wg = 0; var ep = 0; var eg = 0;
        for (var i = 0; i < _spawnedBalls.Count; i++)
        {
            var b = _spawnedBalls[i];
            (uint ObjectId, string Name) pick;

            if (b.Dir == Direction.West)
            {
                if (b.Kind == BallKind.Purple)
                {
                    if (wp >= westPurple.Count) { DuoLog.Warning("West purple overflow"); return false; }
                    pick = westPurple[wp++];
                }
                else
                {
                    if (wg >= westGreen.Count) { DuoLog.Warning("West green overflow"); return false; }
                    pick = westGreen[wg++];
                }
            }
            else
            {
                if (b.Kind == BallKind.Purple)
                {
                    if (ep >= eastPurple.Count) { DuoLog.Warning("East purple overflow"); return false; }
                    pick = eastPurple[ep++];
                }
                else
                {
                    if (eg >= eastGreen.Count) { DuoLog.Warning("East green overflow"); return false; }
                    pick = eastGreen[eg++];
                }
            }

            ret[i] = new PlayerData
            {
                Direction = b.Dir,
                Kind = b.Kind,
                ObjectId = pick.ObjectId,
                Name = pick.Name
            };
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
        });
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action.Value.RowId is ActionHitA or ActionHitB && ++_actiondBallCount % 2 == 0 &&
            _waveState is >= 1 and <= 4) _waveState++;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Positioning purple relative? ('NA Strat' support always purple side, dps always pure green side)", ref C.PurpleRelative);

        C.PriorityData.Draw();
        ImGui.ColorEdit4("Color1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Color2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
        if (ImGui.CollapsingHeader("Guide (JP)"))
        {
            ImGui.TextWrapped("西側をMT組、東側をST組と見ます。優先順位の1・2番は紫球を受け、それ以降は順番に緑球を受けます。");
            ImGui.TextWrapped("例：紫球が MT → OT、緑球の優先が H→近接→遠隔 の場合は「MT H1 M1 R1 OT H2 M2 R2」と入力してください。");
        }
        if (ImGui.CollapsingHeader("Guide (EN)"))
        {
            ImGui.TextWrapped("West = MT, East = OT. Priority 1–2 take purple orbs; everyone after that takes green orbs in order.");
            ImGui.TextWrapped("Example: if purple is MT → OT and green priority is H → Melee → Ranged, enter: \"MT H1 M1 R1 OT H2 M2 R2\".");
            ImGui.TextWrapped("Purple relative expects priority as \"MT H1 M1 R1 OT H2 M2 R2\". For purple prio: MT > OT ; H1 > H2 and green prio: M1 > M2 > R1 > R2");
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

    public override Dictionary<int, string>? Changelog => new()
    {
        { 2, "プレイヤーの選出ロジックを修正しました。それに伴い優先順位の設定方法が変わったので一度確認してください。\n" +
             "I’ve fixed the player selection logic. Because of that, the way you set the priority order has changed, so please review it once.\n" }
    };

    public class PlayerData { public Direction Direction; public bool First; public BallKind Kind; public string Name = ""; public uint ObjectId; public float Offset; public string Text = ""; public int Wave, Order; }

    public class Config : IEzConfig
    {
        public bool PurpleRelative = false;
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public PriorityData PriorityData = new();
    }
}
