using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using Splatoon;
using Splatoon.SplatoonScripting;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Forked_Tower_Magic;

internal class Boss_4_Elementaly_Evocation : SplatoonScript
{
    #region Metadata

    public override Metadata Metadata { get; } = new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryMagic];

    #endregion

    #region Constant

    private const uint TerritoryMagic = 1346;

    private const uint CastControl = 0xBD0A;

    private const uint FloorIce = 2015240;
    private const uint FloorFire = 2015241;
    private const uint FloorThunder = 2015242;

    private const uint BallFire = 0x4B6D;
    private const uint BallIce = 0x4B6C;
    private const uint BallThunder = 0x4B6E;

    private const uint EffectFire = 48431;
    private const uint EffectIce = 48432;
    private const uint EffectThunder = 48433;

    private const uint CueFire = 2015243;
    private const uint CueIce = 2015244;
    private const uint CueThunder = 2015245;

    private static readonly uint[] FloorDataIds = [FloorFire, FloorIce, FloorThunder];
    private static readonly uint[] BallDataIds = [BallFire, BallIce, BallThunder];
    private static readonly uint[] WaveEffectIds = [EffectFire, EffectIce, EffectThunder];
    private static readonly uint[] CueDataIds = [CueFire, CueIce, CueThunder];

    // Geometry, independent of layout JSON.
    private static readonly Vector3 ArenaCenter = new(0f, -684f, -628f);
    private const float NaviRadius = 10f;

    private const string OverlayStart = "!! start position !!";
    private const string OverlayNext = "!! next position !!";

    private const int MaxDisplayStep = 2;
    private const int WaveHitCount = 4;
    private const int SlotCount = 12;
    private const int BallCount = 6;

    #endregion

    #region Config

    // No IEzConfig in this script.

    #endregion

    #region State

    private bool _active;
    private bool _ballsReady;
    private bool _releaseCycleDone;
    private int _step;
    private ElementKind _safeElement = ElementKind.None;
    private ElementKind _firstCue = ElementKind.None;
    private bool _cueLocked;
    private int _waveHits;
    private readonly List<nint> _waveBallPtrs = [];
    private readonly List<nint> _snapBallPtrs = [];
    private readonly List<Slot> _floors = [];
    private readonly List<Slot> _ballSnap = [];
    private readonly List<float> _startDegrees = [];

    #endregion

    #region Private Class

    private enum ElementKind
    {
        None,
        Fire,
        Ice,
        Thunder,
    }

    private sealed class Slot
    {
        public ElementKind Kind;
        public float Degree;
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Start0", """{"Name":"Start0","Enabled":false,"refY":-628.0,"refZ":-684.0,"radius":0.7,"color":3355639552,"Filled":false,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":1.6,"overlayFScale":1.2,"thicc":6.0,"tether":false,"overlayText":"!! start position !!"}""", overwrite: true);
        Controller.RegisterElementFromCode("Start1", """{"Name":"Start1","Enabled":false,"refY":-628.0,"refZ":-684.0,"radius":0.7,"color":3355639552,"Filled":false,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":1.6,"overlayFScale":1.2,"thicc":6.0,"tether":false,"overlayText":"!! start position !!"}""", overwrite: true);
    }

    public override void OnUpdate()
    {
        if(!_active)
        {
            HideNavi();
            return;
        }

        if(_step > MaxDisplayStep)
        {
            _releaseCycleDone = true;
            HideNavi();
            return;
        }

        _floors.Clear();
        _floors.AddRange(CollectFloorsFromNorth());

        if(_ballSnap.Count == 0 && _ballsReady)
            TrySnapshotBalls();

        if(_ballSnap.Count == BallCount && _safeElement == ElementKind.None)
            TryResolveSafe();

        TryDetectFirstCue();

        if(_startDegrees.Count != 2)
        {
            HideNavi();
            return;
        }

        var overlay = _step == 0 ? OverlayStart : OverlayNext;
        ApplyNavi("Start0", _startDegrees[0], overlay);
        ApplyNavi("Start1", _startDegrees[1], overlay);
    }

    public override void OnReset()
    {
        ResetMechanic();
        HideNavi();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId != CastControl) return;
        ResetMechanic();
        _active = true;
    }

    public override void OnObjectCreation(nint newObjectPtr)
    {
        _ = new TickScheduler(() =>
        {
            if(!_active) return;
            var obj = Svc.Objects.FirstOrDefault(o => o.Address == newObjectPtr);
            if(obj is not IBattleNpc npc) return;
            if(!BallDataIds.Contains(npc.DataId)) return;
            if(_waveBallPtrs.Contains(newObjectPtr)) return;

            _waveBallPtrs.Add(newObjectPtr);
            if(_waveBallPtrs.Count < BallCount) return;

            StartBallWave(_waveBallPtrs.ToList());
            _waveBallPtrs.Clear();
        });
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!_active || _step > MaxDisplayStep || _startDegrees.Count != 2) return;
        if(set.Action == null) return;
        var id = set.Action.Value.RowId;
        if(!WaveEffectIds.Contains(id)) return;

        _waveHits++;
        if(_waveHits < WaveHitCount) return;
        _step++;
        _waveHits = 0;
    }

    public override void OnSettingsDraw()
    {
        if(!ImGuiEx.CollapsingHeader("Debug")) return;
        ImGuiEx.Text($"active={_active} ballsReady={_ballsReady} cycleDone={_releaseCycleDone} spawn={_waveBallPtrs.Count} step={_step} safe={_safeElement} cue={_firstCue} cueLocked={_cueLocked}");
        ImGuiEx.Text($"floors={_floors.Count} balls={_ballSnap.Count} start={_startDegrees.Count} wave={_waveHits}");
        foreach(var floor in _floors)
            ImGuiEx.Text($"Floor {floor.Kind} {floor.Degree:0}");
        foreach(var ball in _ballSnap)
            ImGuiEx.Text($"Ball {ball.Kind} {ball.Degree:0}");
    }

    #endregion

    #region Private Method

    // Clear mechanic state.
    private void ResetMechanic()
    {
        _active = false;
        _ballsReady = false;
        _releaseCycleDone = false;
        _step = 0;
        _safeElement = ElementKind.None;
        _firstCue = ElementKind.None;
        _cueLocked = false;
        _waveHits = 0;
        _waveBallPtrs.Clear();
        _snapBallPtrs.Clear();
        _floors.Clear();
        _ballSnap.Clear();
        _startDegrees.Clear();
        HideNavi();
    }

    // Restart from a new 6-ball spawn; keep 元素制御 active.
    private void StartBallWave(List<nint> ptrs)
    {
        if(_ballSnap.Count > 0 || _step > 0 || _releaseCycleDone)
            ResetBallWave();

        _snapBallPtrs.Clear();
        _snapBallPtrs.AddRange(ptrs);
        _ballsReady = true;
    }

    // Clear one ball-wave navi; keep 元素制御 active.
    private void ResetBallWave()
    {
        _ballsReady = false;
        _releaseCycleDone = false;
        _step = 0;
        _safeElement = ElementKind.None;
        _firstCue = ElementKind.None;
        _cueLocked = false;
        _waveHits = 0;
        _snapBallPtrs.Clear();
        _ballSnap.Clear();
        _startDegrees.Clear();
        HideNavi();
    }

    // Collect 6 floors clockwise from north (0°).
    private List<Slot> CollectFloorsFromNorth()
    {
        var points = new List<Slot>();
        foreach(var dataId in FloorDataIds)
        {
            var obj = FindFloor(dataId);
            if(obj == null) continue;
            var rot = RoundAngleByStep(obj.Rotation * 180f / MathF.PI, 60f);
            var kind = FloorKind(dataId);
            points.Add(new Slot { Kind = kind, Degree = rot });
            points.Add(new Slot { Kind = kind, Degree = NormalizeAngle(rot + 180f) });
        }
        points.Sort((a, b) => a.Degree.CompareTo(b.Degree));
        return points;
    }

    // Snapshot the 6 balls from this OnObjectCreation wave.
    private void TrySnapshotBalls()
    {
        var balls = new List<IBattleNpc>();
        foreach(var ptr in _snapBallPtrs)
        {
            var obj = Svc.Objects.FirstOrDefault(o => o.Address == ptr);
            if(obj is not IBattleNpc npc) return;
            if(!BallDataIds.Contains(npc.DataId)) return;
            balls.Add(npc);
        }
        if(balls.Count != BallCount) return;

        var snap = new List<Slot>();
        foreach(var ball in balls)
        {
            var kind = BallKind(ball.DataId);
            if(kind == ElementKind.None) return;
            var deg = AngleFromCenter(ArenaCenter, ball.Position);
            deg = NormalizeAngle(RoundAngleByStep(NormalizeAngle(deg - 30f), 60f) + 30f);
            snap.Add(new Slot { Kind = kind, Degree = deg });
        }
        snap.Sort((a, b) => a.Degree.CompareTo(b.Degree));
        _ballSnap.AddRange(snap);
    }

    // Pick the ball element whose two floor neighbors make all 3 elements.
    private void TryResolveSafe()
    {
        if(_floors.Count != BallCount) return;
        var matches = new List<ElementKind>();
        for(var i = 1; i < SlotCount; i += 2)
        {
            var ball = KindAt(_ballSnap, i * 30f);
            var left = KindAt(_floors, (i - 1) * 30f);
            var right = KindAt(_floors, ((i + 1) % SlotCount) * 30f);
            if(ball == ElementKind.None || left == ElementKind.None || right == ElementKind.None) return;
            if(ball != left && ball != right && left != right)
                matches.Add(ball);
        }
        if(matches.Count == 0) return;

        var safe = matches.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key;
        var starts = _floors.Where(x => x.Kind == safe).Select(x => x.Degree).Distinct().OrderBy(x => x).ToList();
        if(starts.Count != 2) return;

        _safeElement = safe;
        _startDegrees.AddRange(starts);
    }

    // Draw one navi point, rainbow when configured.
    private void ApplyNavi(string name, float startDegree, string overlay)
    {
        if(!Controller.TryGetElementByName(name, out var element)) return;
        var degree = NormalizeAngle(startDegree - 60f * _step - StartAdjust());
        element.Enabled = true;
        element.overlayText = overlay;
        element.color = Controller.AttentionColor;
        element.SetRefPosition(CalculatePointCircle(ArenaCenter, NaviRadius, degree));
    }

    // Hide both navi elements.
    private void HideNavi()
    {
        if(Controller.TryGetElementByName("Start0", out var a)) a.Enabled = false;
        if(Controller.TryGetElementByName("Start1", out var b)) b.Enabled = false;
    }

    // Find the floor EventObj for a DataId.
    private static IEventObj? FindFloor(uint dataId)
        => Svc.Objects.OfType<IEventObj>().FirstOrDefault(x => x.DataId == dataId);

    // Lock the first fire/ice/thunder cue EventObj; ignore later ones.
    private void TryDetectFirstCue()
    {
        if(_cueLocked) return;
        foreach(var dataId in CueDataIds)
        {
            if(FindFloor(dataId) == null) continue;
            var kind = CueKind(dataId);
            if(kind == ElementKind.None) continue;
            _firstCue = kind;
            _cueLocked = true;
            return;
        }
    }

    // Extra -60° when the first cue matches the navi element.
    private float StartAdjust()
        => _cueLocked && _firstCue == _safeElement && _safeElement != ElementKind.None ? 60f : 0f;

    // Map cue EventObj DataId to element.
    private static ElementKind CueKind(uint dataId) => dataId switch
    {
        CueFire => ElementKind.Fire,
        CueIce => ElementKind.Ice,
        CueThunder => ElementKind.Thunder,
        _ => ElementKind.None,
    };

    // Map floor DataId to element.
    private static ElementKind FloorKind(uint dataId) => dataId switch
    {
        FloorFire => ElementKind.Fire,
        FloorIce => ElementKind.Ice,
        FloorThunder => ElementKind.Thunder,
        _ => ElementKind.None,
    };

    // Map ball DataId to element.
    private static ElementKind BallKind(uint dataId) => dataId switch
    {
        BallFire => ElementKind.Fire,
        BallIce => ElementKind.Ice,
        BallThunder => ElementKind.Thunder,
        _ => ElementKind.None,
    };

    // Find a slot kind at a rounded degree.
    private static ElementKind KindAt(List<Slot> slots, float degree)
    {
        var target = NormalizeAngle(degree);
        foreach(var slot in slots)
        {
            if(MathF.Abs(slot.Degree - target) < 1f) return slot.Kind;
        }
        return ElementKind.None;
    }

    // Angle in degrees where 0 is -Z.
    private static float AngleFromCenter(Vector3 center, Vector3 pos)
    {
        var dx = pos.X - center.X;
        var dz = pos.Z - center.Z;
        return NormalizeAngle(MathF.Atan2(dx, -dz) * 180f / MathF.PI);
    }

    // Point on a circle where 0° is -Z.
    private static Vector3 CalculatePointCircle(Vector3 center, float radius, float degree)
    {
        var radian = degree.DegToRad();
        var sin = MathF.Sin(radian);
        var cos = MathF.Cos(radian);
        return new Vector3(
            center.X + sin * radius,
            center.Y,
            center.Z - cos * radius);
    }

    // Wrap degrees into 0-360.
    private static float NormalizeAngle(float degree)
        => (degree % 360f + 360f) % 360f;

    // Round degree to step, then normalize to 0-360 (360 becomes 0).
    private static float RoundAngleByStep(float degree, float step)
        => NormalizeAngle((float)Math.Round(degree / step) * step);

    #endregion
}
