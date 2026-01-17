using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers.LegacyPlayer;
using ECommons.ImGuiMethods;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M9S_Hell_In_a_Cell : SplatoonScript
{
    // ====== State ======
    public enum State
    {
        None,
        FirstTower,
        FirstResolve1,
        FirstResolve2,
        SecondTower,
        SecondResolve1,
        SecondResolve2,
        Done
    }

    private const uint StartCastingId = 45973;
    private const uint TowerCastId = 45974;
    private const uint SpreadCastId = 45980;
    private const uint StackCastId = 45981;
    private const uint EndCastId = 45939;
    private const string ElemGo = "Go";
    private static readonly Vector2 Center = new(100f, 100f);
    private static readonly Vector2 NorthEdge = new(100f, 80f);
    private static readonly float RingRadius = 5f;
    private readonly List<Gap> _gaps = []; // 塔間（4つ）
    private readonly List<Vector2> _sortedTowers = []; // 北→時計回りで4つ
    private readonly Dictionary<uint, IBattleNpc> _towerById = new();

    private ResolveKind _resolveKind = ResolveKind.None;

    private State _state = State.None;
    private int _narrowGapIndex;

    public override Metadata Metadata => new(4, "Garume, NightmareXIV");
    public override HashSet<uint>? ValidTerritories => [1321];

    private Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        Controller.RegisterElement(ElemGo, new Element(0)
        {
            Name = "Go",
            Enabled = false,
            tether = true,
            radius = 3f,
            thicc = 9.0f,
            Filled = false,
            color = 0xFF00FF00
        });
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox($"Simple mode", ref C.SimpleMode);
        ImGuiEx.HelpMarker("Instead of priority list, use fixed number and party");
        if(!C.SimpleMode)
        {
            C.PriorityData.Draw();
        }
        else
        {
            ImGui.SetNextItemWidth(150f);
            ImGui.SliderInt("Your position", ref C.SimpleModeCnt, 1, 4);
            ImGuiEx.TextV($"Your tower set:");
            ImGui.SameLine();
            ImGuiEx.RadioButtonBool("1", "2", ref C.SimpleModeGroup1, true);
        }

            ImGui.Checkbox("Spread Clockwise From Wide Gap", ref C.SpreadClockwiseFromWide);
        ImGui.Checkbox("Tower Order: narrow-gap base (2-3-4-1)", ref C.UseNarrowGapTowerOrder);
        
        ImGui.Text("Bait Color:");
        ImGuiComponents.HelpMarker(
            "Change the color of the bait and the text that will be displayed on your bait.\nSetting different values makes it rainbow.");
        ImGui.Indent();
        ImGui.ColorEdit4("Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.SameLine();
        ImGui.ColorEdit4("Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
        ImGui.Unindent();

        if (ImGuiEx.CollapsingHeader("PriorityList Guide (JP)"))
        {
            ImGui.TextWrapped("このスクリプトは PriorityList（PriorityData）の並び順で 8人の役割を決めます。");
            ImGui.Spacing();

            ImGui.BulletText("前半4人（0〜3番）＝ MT組 / 後半4人（4〜7番）＝ ST組");
            ImGui.BulletText("1回目（前半）は MT組が塔、2回目（後半）は ST組が塔");
            ImGui.BulletText("塔の割り当ては「北→時計回り」。塔担当4人の 1〜4番がその順で入ります");
            ImGui.BulletText("散開（Spread）は「塔に入っていない側の4人」が動きます");
            ImGui.BulletText("散開側の1番目（後半ならMT組1番目）が『空き（いちばん広い塔間）』に入り、そこを北として時計回りに割り当てます");
            ImGui.BulletText("反時計回りにしたい場合はSpread Clockwise From Wide Gapのチェックを外してください");
            ImGui.BulletText("間隔が狭い塔を北にして時計回りに 2,3,4,1 としたい場合は、Tower Order: narrow-gap base (2-3-4-1)にチェックを付けてください。");
        }

        if (ImGuiEx.CollapsingHeader("PriorityList Guide (EN)"))
        {
            ImGui.TextWrapped("This script uses the Priority List (PriorityData) order to assign roles/positions.");
            ImGui.Spacing();

            ImGui.BulletText("First 4 entries (0–3) = MT group / Last 4 entries (4–7) = ST group");
            ImGui.BulletText("1st set: MT group takes towers. 2nd set: ST group takes towers");
            ImGui.BulletText("Tower order is North → clockwise. Tower-side players #1–#4 take towers in that order");
            ImGui.BulletText("Spread is done by the non-tower group (the other 4 players)");
            ImGui.BulletText(
                "For Spread: the #1 player of the spread-side group (in the 2nd set, MT #1) goes to the open gap (widest gap). That gap becomes 'North', then assignments go clockwise");
            ImGui.BulletText("If you want counter clockwise instead, uncheck 'Spread Clockwise From Wide Gap'");
            ImGui.BulletText(
                "If you want to treat the narrowest tower gap as 'North' and assign towers clockwise as 2,3,4,1, check Tower Order: narrow-gap base (2-3-4-1)");
        }

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"Resolve: {_resolveKind}");
            ImGui.Text($"Towers: {_sortedTowers.Count}");
            ImGui.Text($"Gaps: {_gaps.Count}");
            ImGui.Text($"Center: {Center.X:0.0}, {Center.Y:0.0}");
            ImGui.Text($"NorthEdge: {NorthEdge.X:0.0}, {NorthEdge.Y:0.0}");
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _resolveKind = ResolveKind.None;

        _towerById.Clear();
        _sortedTowers.Clear();
        _gaps.Clear();

        if (Controller.TryGetElementByName(ElemGo, out var e))
            e.Enabled = false;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == TowerCastId)
        {
            if (_state == State.None)
                StartFirstCycle();

            if (_state == State.FirstResolve2)
                StartSecondCycle();

            if (_state == State.FirstTower || _state == State.SecondTower)
                TryAddTower(source);

            return;
        }

        if (StartCastingId != 0 && castId == StartCastingId)
        {
            if (_state == State.None || _state == State.Done) StartFirstCycle();
            return;
        }

        if (castId is SpreadCastId or StackCastId)
        {
            _resolveKind = castId == SpreadCastId ? ResolveKind.Spread : ResolveKind.Stack;

            _state = _state switch
            {
                State.FirstTower => State.FirstResolve1,
                State.FirstResolve1 => State.FirstResolve2,
                State.SecondTower => State.SecondResolve1,
                State.SecondResolve1 => State.SecondResolve2,
                State.SecondResolve2 => State.Done,
                _ => _state
            };
        }

        if (castId == EndCastId && _state == State.SecondResolve2) _state = State.Done;
    }

    public override void OnUpdate()
    {
        if (!Controller.TryGetElementByName(ElemGo, out var go))
            return;

        go.Enabled = false;

        if (_state is State.None or State.Done)
            return;

        Controller.GetRegisteredElements()
            .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());

        int myIndex;
        if(!C.SimpleMode)
        {
            var prios = C.PriorityData.GetPlayers(_ => true)?.ToArray();
            if(prios == null || prios.Length < 8)
                return;

            var baseId = BasePlayer.EntityId;
            myIndex = Array.FindIndex(prios, p => p.IGameObject?.EntityId == baseId);
            if(myIndex is < 0 or >= 8)
                return;
        }
        else
        {
            myIndex = C.SimpleModeCnt - 1 + (C.SimpleModeGroup1 ? 0 : 4);
        }

        var isMtGroup = myIndex < 4;
        var isStGroup = !isMtGroup;
        var towersReady = _sortedTowers.Count == 4 && _gaps.Count == 4;
        var firstTowerHolders = _state is State.FirstTower or State.FirstResolve1 or State.FirstResolve2;
        var secondTowerHolders = _state is State.SecondTower or State.SecondResolve1 or State.SecondResolve2;

        if (towersReady)
        {
            if (firstTowerHolders && isMtGroup)
            {
                ShowGo(go, _sortedTowers[TowerIdx(myIndex)]);
                return;
            }

            if (secondTowerHolders && isStGroup)
            {
                ShowGo(go, _sortedTowers[TowerIdx(myIndex - 4)]);
                return;
            }
        }

        if (_state is State.FirstTower or State.SecondTower)
        {
            if (!towersReady)
                return;

            var towerGroupIsMt = _state == State.FirstTower;
            var i4 = isMtGroup ? myIndex : myIndex - 4;

            if (towerGroupIsMt)
            {
                var pos = isMtGroup ? _sortedTowers[TowerIdx(myIndex)] : _gaps[myIndex - 4].MidPoint;
                ShowGo(go, pos);
            }
            else
            {
                var pos = isStGroup ? _sortedTowers[TowerIdx(i4)] : _gaps[myIndex].MidPoint;
                ShowGo(go, pos);
            }

            return;
        }

        if (_resolveKind == ResolveKind.None || !towersReady)
            return;

        var groupStart = firstTowerHolders ? 4 : 0;

        if (firstTowerHolders && isMtGroup)
            return;
        if (!firstTowerHolders && isStGroup)
            return;

        var rel = myIndex - groupStart;

        var tankGap = _gaps.OrderByDescending(x => x.SizeDeg).First();
        var tankPos = tankGap.MidPoint;

        var northAngle = tankGap.MidDeg + 180f;
        if (northAngle >= 360f) northAngle -= 360f;

        var candidates = _gaps
            .Where(g => g.Index != tankGap.Index)
            .OrderByDescending(g => g.SizeDeg)
            .Take(2)
            .ToArray();

        var northGap = candidates
            .OrderBy(g => ClockwiseDelta(northAngle, g.MidDeg))
            .First();
        var nextGap = candidates.First(g => g.Index != northGap.Index);

        Vector2 myPos;

        if (_resolveKind == ResolveKind.Stack)
            myPos = tankPos;
        else
            myPos = rel switch
            {
                0 => tankPos,
                1 or 2 => northGap.MidPoint,
                _ => nextGap.MidPoint
            };

        ShowGo(go, myPos);
    }

    private float ClockwiseDelta(float fromDeg, float toDeg)
    {
        var d = C.SpreadClockwiseFromWide ? fromDeg - toDeg : toDeg - fromDeg;
        if (d < 0f) d += 360f;
        return d;
    }

    private void StartFirstCycle()
    {
        _state = State.FirstTower;
        _resolveKind = ResolveKind.None;
        _towerById.Clear();
        _sortedTowers.Clear();
        _gaps.Clear();
    }

    private void StartSecondCycle()
    {
        _state = State.SecondTower;
        _resolveKind = ResolveKind.None;
        _towerById.Clear();
        _sortedTowers.Clear();
        _gaps.Clear();
    }

    private void TryAddTower(uint source)
    {
        if (source.GetObject() is not IBattleNpc bn)
            return;

        _towerById.TryAdd(bn.EntityId, bn);

        if (_towerById.Count == 4)
            BuildTowerCache();
    }

    private void BuildTowerCache()
    {
        _sortedTowers.Clear();
        _gaps.Clear();

        var towers = _towerById.Values
            .Select(t => ToXz(t.Position))
            .Select(p => new { Pos = p, Deg = AngleFromNorthClockwise(p) })
            .OrderBy(x => x.Deg)
            .Select(x => x.Pos)
            .ToList();

        if (towers.Count != 4)
            return;

        _sortedTowers.AddRange(towers);
        _gaps.AddRange(BuildGaps(_sortedTowers));
        _narrowGapIndex = _gaps.OrderBy(g => g.SizeDeg).First().Index;
    }
    
    private int TowerIdx(int role)
    {
        if (!C.UseNarrowGapTowerOrder) return role;
        var off = role switch { 0 => 3, 1 => 0, 2 => 1, 3 => 2, _ => 0 };
        return (_narrowGapIndex + off) % 4;
    }

    private static Vector2 ToXz(Vector3 v)
    {
        return new Vector2(v.X, v.Z);
    }

    private static float AngleFromNorthClockwise(Vector2 p)
    {
        var dx = p.X - Center.X;
        var dz = p.Y - Center.Y;

        var rad = MathF.Atan2(dx, -dz);
        var deg = rad * 180f / MathF.PI;
        if (deg < 0f) deg += 360f;
        return deg;
    }

    private static List<Gap> BuildGaps(List<Vector2> towers4)
    {
        var res = new List<Gap>(4);

        var angles = towers4.Select(AngleFromNorthClockwise).ToArray();

        for (var i = 0; i < 4; i++)
        {
            var a0 = angles[i];
            var a1 = angles[(i + 1) % 4];

            var diff = a1 - a0;
            if (diff < 0f) diff += 360f;

            var midDeg = a0 + diff / 2f;
            if (midDeg >= 360f) midDeg -= 360f;

            res.Add(new Gap
            {
                Index = i,
                SizeDeg = diff,
                MidDeg = midDeg,
                MidPoint = PointFromDeg(midDeg, RingRadius)
            });
        }

        return res;
    }

    private static Vector2 PointFromDeg(float deg, float radius)
    {
        var rad = deg * MathF.PI / 180f;

        var x = Center.X + MathF.Sin(rad) * radius;
        var z = Center.Y - MathF.Cos(rad) * radius;

        return new Vector2(x, z);
    }


    private static void ShowGo(dynamic elem, Vector2 pos)
    {
        elem.Enabled = true;
        elem.refX = pos.X;
        elem.refY = pos.Y;
    }

    private enum ResolveKind
    {
        None,
        Spread,
        Stack
    }

    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public PriorityData PriorityData = new();
        public bool SpreadClockwiseFromWide = true;
        public bool UseNarrowGapTowerOrder = false;
        public bool SimpleMode = false;
        public int SimpleModeCnt = 1;
        public bool SimpleModeGroup1 = true;
    }

    private class Gap
    {
        public int Index;
        public float MidDeg;
        public Vector2 MidPoint;
        public float SizeDeg;
    }
}