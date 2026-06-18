using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate;

public class P5_Exaflare_beta : SplatoonScript
{
    private const uint TerritoryDancingMadUltimate = 1363;
    private const uint TerritoryReplayZone = 179;
    private const uint ExaflareCast = 47932;
    private const uint CastSpread = 47934;
    private const float GroupInterval = 2.5f;
    private const float CastSpreadStart = 16.2f;
    private const float FirstImpactDelay = 4.6f;
    private const float ImpactInterval = 0.51f;
    private const float DiagonalStep = 5.0f;
    private const float MovementSpeed = 6.0f;
    private const float ImpactRadius = 6.0f;
    private const float EarlySafetyMargin = 1.25f;
    private const float LateSafetyMargin = 2.5f;
    private const int LateSafetyMarginStartGroup = 3;
    private const float StageGrace = 0.10f;
    private const float PreferredRadius = 13.5f;
    private const string DestinationElementName = "Destination";

    private static readonly Vector3 ArenaCenter = new(100.0f, 0.0f, 100.0f);
    private static readonly Vector3 InitialDestination = new(100.0f, 0.0f, 105.0f);
    private static readonly float StepDistance = MathF.Sqrt(DiagonalStep * DiagonalStep * 2.0f);
    private static readonly Vector4 NavigationColor1 = 0xC800FFFF.ToVector4();
    private static readonly Vector4 NavigationColor2 = 0xC8FF00FF.ToVector4();
    private static readonly float[] CandidateRadii = [6.5f, 8.0f, 9.5f, 11.0f, 12.5f, 14.0f, 15.5f, 17.0f, 18.5f, 19.0f];
    private static readonly List<Vector3> Candidates = CandidatePoints().ToList();

    private readonly List<LineCast> _lines = [];
    private bool _active;
    private uint _castSpreadSource;
    private int _currentStage = -1;
    private Vector3? _lastDestination;

    public override HashSet<uint>? ValidTerritories => [TerritoryDancingMadUltimate, TerritoryReplayZone];
    public override Metadata Metadata => new(1, "Garume");

    public override void OnSetup()
    {
        Controller.RegisterElement(DestinationElementName, new Element(0)
        {
            Enabled = false,
            radius = 1.15f,
            thicc = 5.0f,
            fillIntensity = 0.25f,
            color = 0xC800BFFF,
            tether = true
        });
    }

    public override void OnCombatStart() => ResetState();
    public override void OnCombatEnd() => ResetState();
    public override void OnReset() => ResetState();

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category is DirectorUpdateCategory.Commence or DirectorUpdateCategory.Recommence or DirectorUpdateCategory.Wipe)
            ResetState();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == ExaflareCast)
        {
            CaptureExaflare(source);
            return;
        }

        if (_active && castId == CastSpread)
            _castSpreadSource = source;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if ((set.Action?.RowId ?? 0) == CastSpread)
            ResetState();
    }

    public override void OnUpdate()
    {
        var player = Controller.BasePlayer;
        if (!_active || player == null)
        {
            DisableElements();
            return;
        }

        if (!TryGetElapsed(out var elapsed))
        {
            DisableElements();
            return;
        }

        if (elapsed > LastKnownHazardTime() + 0.20f)
        {
            ResetState();
            return;
        }

        var stage = CurrentStage(elapsed);
        if (_currentStage != stage || !_lastDestination.HasValue)
        {
            if (!TryPlanDestination(elapsed, player.Position, stage, out var nextDestination))
            {
                DisableElements();
                return;
            }

            _currentStage = stage;
            _lastDestination = nextDestination;
        }

        if (!_lastDestination.HasValue)
            return;

        DrawDestination(_lastDestination.Value);
    }

    private void CaptureExaflare(uint source)
    {
        if (_lines.Any(line => line.Source == source))
            return;

        if (source.GetObject() is not IBattleChara battle)
            return;

        var group = Math.Min(5, _lines.Count / 2);
        _lines.Add(new LineCast(
            source,
            group,
            group * GroupInterval,
            new Vector3(battle.Position.X, 0.0f, battle.Position.Z),
            new Vector3(MathF.Sin(battle.Rotation), 0.0f, MathF.Cos(battle.Rotation)) * StepDistance));

        _active = true;
    }

    private bool TryGetElapsed(out float elapsed)
    {
        elapsed = 0.0f;
        var found = false;

        foreach (var line in _lines)
        {
            if (line.Source.GetObject() is not IBattleChara battle)
                continue;
            if (!battle.IsCasting || battle.CastActionId != ExaflareCast)
                continue;

            elapsed = MathF.Max(elapsed, line.GroupStart + battle.CurrentCastTime);
            found = true;
        }

        if (_castSpreadSource.GetObject() is IBattleChara spread && spread.IsCasting && spread.CastActionId == CastSpread)
        {
            elapsed = MathF.Max(elapsed, CastSpreadStart + spread.CurrentCastTime);
            found = true;
        }

        return found;
    }

    private bool TryPlanDestination(float elapsed, Vector3 playerPosition, int stage, out Vector3 destination)
    {
        destination = default;
        var hazards = BuildHazards();
        if (hazards.Count == 0)
            return false;

        if (stage == 0)
        {
            destination = InitialDestination;
            return true;
        }

        var intervals = BuildIntervals(elapsed, stage, hazards);
        if (intervals.Count == 0)
            return false;

        if (TryPlanIntervals(playerPosition, hazards, intervals, out destination))
            return true;

        return TryPlanIntervals(playerPosition, hazards, new[] { intervals[0] }, out destination);
    }

    private bool TryPlanIntervals(Vector3 playerPosition, IReadOnlyCollection<Hazard> hazards, IReadOnlyList<RouteInterval> intervals, out Vector3 destination)
    {
        destination = default;
        var states = new List<RouteState> { new(0.0f, playerPosition, null) };

        foreach (var interval in intervals)
        {
            var activeHazards = hazards
                .Where(hazard => hazard.Time >= interval.Start + 0.001f && hazard.Time <= interval.End + 0.05f)
                .ToList();
            if (activeHazards.Count == 0)
                continue;

            var nextStates = new List<RouteState>();
            foreach (var state in states)
            {
                foreach (var candidate in Candidates)
                {
                    var distance = DistanceXZ(state.Position, candidate);
                    if (distance > MovementSpeed * MathF.Max(0.0f, interval.End - interval.Start) + 0.001f)
                        continue;
                    if (!IsPathSafe(state.Position, candidate, interval.Start, activeHazards))
                        continue;

                    var radius = DistanceXZ(ArenaCenter, candidate);
                    var cost = state.Cost + distance + MathF.Abs(radius - PreferredRadius) * 0.05f;
                    nextStates.Add(new RouteState(cost, candidate, state.FirstDestination ?? candidate));
                }
            }

            if (nextStates.Count == 0)
                return false;

            states = nextStates
                .OrderBy(state => state.Cost)
                .Take(120)
                .ToList();
        }

        var best = states.OrderBy(state => state.Cost).FirstOrDefault();
        if (!best.FirstDestination.HasValue)
            return false;

        destination = best.FirstDestination.Value;
        return true;
    }

    private int CurrentStage(float elapsed)
    {
        if (elapsed < FirstImpactDelay + StageGrace)
            return 0;

        return Math.Clamp((int)MathF.Floor((elapsed - FirstImpactDelay - StageGrace) / GroupInterval) + 1, 0, 6);
    }

    private static float StageEnd(int stage, float lastKnownTime)
        => stage < 6 ? FirstImpactDelay + StageGrace + GroupInterval * stage : lastKnownTime;

    private List<RouteInterval> BuildIntervals(float elapsed, int stage, IReadOnlyCollection<Hazard> hazards)
    {
        var intervals = new List<RouteInterval>();
        var start = elapsed;
        var lastKnownTime = hazards.Max(hazard => hazard.Time) + 0.20f;
        for (var currentStage = stage; currentStage <= 6; currentStage++)
        {
            var end = MathF.Min(StageEnd(currentStage, lastKnownTime), lastKnownTime);
            if (end > start + 0.05f)
                intervals.Add(new RouteInterval(start, end));

            start = end;
            if (start >= lastKnownTime - 0.01f)
                break;
        }

        return intervals;
    }

    private List<Hazard> BuildHazards()
    {
        var hazards = new List<Hazard>(_lines.Count * 6);
        foreach (var line in _lines)
        {
            for (var i = 0; i < 6; i++)
            {
                hazards.Add(new Hazard(
                    line.GroupStart + FirstImpactDelay + ImpactInterval * i,
                    line.Start + line.Step * (i + 1),
                    line.Group));
            }
        }

        return hazards;
    }

    private float LastKnownHazardTime()
    {
        return _lines.Count == 0
            ? 0.0f
            : _lines.Max(line => line.GroupStart + FirstImpactDelay + ImpactInterval * 5.0f);
    }

    private static IEnumerable<Vector3> CandidatePoints()
    {
        foreach (var radius in CandidateRadii)
        {
            for (var degree = 0; degree < 360; degree += 15)
            {
                var angle = degree * MathF.PI / 180.0f;
                yield return ArenaCenter + new Vector3(MathF.Sin(angle) * radius, 0.0f, -MathF.Cos(angle) * radius);
            }
        }
    }

    private static bool IsPathSafe(Vector3 start, Vector3 destination, float elapsed, IEnumerable<Hazard> hazards)
    {
        foreach (var hazard in hazards)
        {
            var radius = ImpactRadius + SafetyMarginFor(hazard.Group);
            var delta = MathF.Max(0.0f, hazard.Time - elapsed);
            var position = MoveTowards(start, destination, MovementSpeed * delta);
            if (DistanceXZ(position, hazard.Position) <= radius)
                return false;
        }

        return true;
    }

    private static Vector3 MoveTowards(Vector3 start, Vector3 destination, float maxDistance)
    {
        var distance = DistanceXZ(start, destination);
        if (distance <= maxDistance || distance < 0.001f)
            return destination;

        var t = maxDistance / distance;
        return new Vector3(
            start.X + (destination.X - start.X) * t,
            0.0f,
            start.Z + (destination.Z - start.Z) * t);
    }

    private void DrawDestination(Vector3 destination)
    {
        if (Controller.TryGetElementByName(DestinationElementName, out var marker))
        {
            marker.Enabled = true;
            marker.color = GradientColor.Get(NavigationColor1, NavigationColor2).ToUint();
            marker.SetRefPosition(destination);
        }
    }

    private void DisableElements()
    {
        foreach (var element in Controller.GetRegisteredElements().Values)
            element.Enabled = false;
    }

    private void ResetState()
    {
        _active = false;
        _castSpreadSource = 0;
        _currentStage = -1;
        _lastDestination = null;
        _lines.Clear();
        DisableElements();
    }

    private static float DistanceXZ(Vector3 left, Vector3 right)
    {
        var dx = left.X - right.X;
        var dz = left.Z - right.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    private static float SafetyMarginFor(int group)
        => group >= LateSafetyMarginStartGroup ? LateSafetyMargin : EarlySafetyMargin;

    private readonly record struct LineCast(uint Source, int Group, float GroupStart, Vector3 Start, Vector3 Step);
    private readonly record struct Hazard(float Time, Vector3 Position, int Group);
    private readonly record struct RouteInterval(float Start, float End);
    private readonly record struct RouteState(float Cost, Vector3 Position, Vector3? FirstDestination);
}
