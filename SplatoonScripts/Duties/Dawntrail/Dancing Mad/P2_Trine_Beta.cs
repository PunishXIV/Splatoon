using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.PartyFunctions;
using Splatoon;
using Splatoon.Serializables;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate;

public class P2_Trine_Beta : SplatoonScript
{
    private const uint TerritoryDancingMadUltimate = 1363;
    private const uint TrineSetup = 47839;
    private const uint TrineWave = 47840;
    private const uint WingCleaveLeftOrRight = 47821;
    private const uint WingCleaveOtherSide = 47822;
    private const uint TankbusterCast = 50311;
    private const uint TankbusterHit = 47823;
    private const uint UltimateEmbrace = 49740;
    private const uint DefinitionOfInsanity = 47842;
    private const uint TrineTelegraphDataIdA = 0x001EBFB2;
    private const uint TrineTelegraphDataIdB = 0x001EBFB3;
    private const float TrineCenterToSideX = 2.886751f;
    private const float TrineCenterToPointX = 5.773502f;
    private const float TrineCenterToPointZ = 5.0f;
    private const float TrineSideLength = 10.0f;
    private const float TrineSideTolerance = 1.35f;
    private const float CloseTriangleCenterDistance = 15.275f;
    private const float MediumTriangleCenterDistance = 26.457f;
    private const float FarTriangleCenterDistance = 30.550f;
    private const float FirstMoveClearance = 5.5f;
    private const int MinimumSafeCandidatesPerSpot = 2;
    private const float TrineExplosionClearance = 6.4f;
    private const float PartyFinalSearchRadius = 4.0f;
    private const float PartyOutwardOffset = 2.0f;
    private const float PartyFinalOutwardOffset = 2.0f;
    private const float TankNearFinalOutwardOffset = 1.5f;
    private const float TankNearSearchMaxRadius = 13.0f;
    private const float TankFarRadius = 17.0f;
    private const float OffTankFinalOutwardOffset = 2.0f;
    private const float TankNearInwardSearchRadius = 2.0f;
    private const float ArenaUsableRadius = 19.0f;
    private const float RouteArrowStartDistance = 2.0f;
    private const float RouteArrowEndPadding = 1.0f;
    private const float RouteArrowThickness = 18.0f;

    private static readonly int[] ExpectedWaveCounts = [9, 3, 9];
    private static readonly Vector3 ArenaCenter = new(100.0f, 0.0f, 100.0f);
    private static readonly Vector3 HalfRoomWestSafeDestination = new(97.0f, 0.0f, 100.0f);
    private static readonly Vector3 HalfRoomEastSafeDestination = new(103.0f, 0.0f, 100.0f);
    private static readonly InternationalString MainDescriptionText = new()
    {
        En =
            "Shows your P2 Trine positions: half-room wait, first Trine dodge, and final tankbuster spread. The priority below is only for the final tankbuster split: first tank stays near, second tank goes far.",
        Jp =
            "P2トライン用です。半面攻撃の待機位置、1回目トライン後の移動先、最後の強攻撃散開位置を表示します。下の優先順位は最後の強攻撃用で、1番目のタンクが近く、2番目のタンクが外周です。"
    };
    private static readonly InternationalString ShowSharedRouteMarkersText = new()
    {
        En = "Show shared route markers",
        Jp = "移動先をまとめて表示"
    };
    private static readonly InternationalString ShowSharedRouteMarkersDescriptionText = new()
    {
        En =
            "When enabled, shows tank and party route markers. First-move shared markers include thick arrows; self-only destination markers hide those arrows.",
        Jp =
            "有効にすると、パーティ用とタンク用の2点を同時に表示します。1回目移動の共有表示では太い矢印も表示し、自分用の移動先表示では矢印を消します。"
    };

    private readonly List<Vector3> _currentWavePositions = [];
    private readonly HashSet<uint> _currentWaveSources = [];
    private readonly List<Vector3> _firstWavePositions = [];
    private readonly List<TelegraphTriangleSignal> _telegraphTriangles = [];
    private readonly HashSet<uint> _telegraphSignalSources = [];
    private readonly Dictionary<string, Vector3> _destinationCache = new(StringComparer.OrdinalIgnoreCase);

    private bool _active;
    private bool _hasDestination;
    private int _currentWaveIndex;
    private int _noSourceSignals;
    private Vector3? _fallbackPartyDestination;
    private Vector3? _fallbackTankDestination;
    private bool _hasFirstWaveRoute;
    private bool _showFirstMoveRouteArrows;
    private Vector3 _firstWaveTankDestination;
    private Vector3 _firstWavePartyDestination;

    public override HashSet<uint>? ValidTerritories { get; } = [TerritoryDancingMadUltimate];
    public override Metadata Metadata => new(5, "Garume");

    private Config C
    {
        get
        {
            var config = Controller.GetConfig<Config>();
            config.EnsureDefaults();
            return config;
        }
    }
    private new IPlayerCharacter BasePlayer => global::Splatoon.Splatoon.BasePlayer;

    public override void OnSetup()
    {
        Controller.RegisterElement("Destination", new Element(0)
        {
            Enabled = false,
            radius = 1.25f,
            thicc = 5.0f,
            fillIntensity = 0.25f,
            color = 0xC800BFFF,
            tether = true
        });
        Controller.RegisterElement("TankDestination", new Element(0)
        {
            Enabled = false,
            radius = 1.25f,
            thicc = 5.0f,
            fillIntensity = 0.25f,
            color = 0xC84080FF,
            tether = true
        });
        Controller.RegisterElement("PartyDestination", new Element(0)
        {
            Enabled = false,
            radius = 1.25f,
            thicc = 5.0f,
            fillIntensity = 0.25f,
            color = 0xC800FF80,
            tether = true
        });
        Controller.RegisterElement("TankRouteArrow", CreateRouteArrowElement(0xC84080FF));
        Controller.RegisterElement("PartyRouteArrow", CreateRouteArrowElement(0xC800FF80));
    }

    public override void OnCombatStart()
    {
        ResetState();
    }

    public override void OnReset()
    {
        ResetState();
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe))
            ResetState();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId is UltimateEmbrace or DefinitionOfInsanity)
        {
            ClearActiveState();
            return;
        }

        if (castId == TrineSetup)
        {
            StartTrine();
            return;
        }

        if (!_active) return;

        if (castId is WingCleaveLeftOrRight or WingCleaveOtherSide)
        {
            SetHalfRoomSafeDestination(castId);
            return;
        }

        if (castId == TankbusterCast)
        {
            TryCompleteCurrentWave(partialAllowed: true);
            TryApplyFinalDestination();
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        var actionId = set.Action?.RowId ?? 0;
        if (actionId == TankbusterHit)
        {
            ClearActiveState();
            return;
        }

        if (actionId != TrineWave) return;

        if (!_active)
            StartTrine();

        AddCurrentWaveSource(set);
        TryCompleteCurrentWave(partialAllowed: false);
    }

    public override void OnObjectEffect(uint target, uint data1, uint data2)
    {
        var obj = target.GetObject();
        if (!_active) return;

        CaptureTrineTelegraphSignal(target, obj, data1, data2);
    }

    public override void OnUpdate()
    {
        ApplyDisplay();
    }

    public override void OnSettingsDraw()
    {
        C.EnsureDefaults();

        ImGui.TextWrapped(MainDescriptionText.Get());
        ImGui.Separator();
        ImGui.Checkbox(ShowSharedRouteMarkersText.Get(), ref C.ShowSharedRouteMarkers);
        ImGui.TextWrapped(ShowSharedRouteMarkersDescriptionText.Get());
        ImGui.Separator();
        C.PriorityData.Draw();
    }

    private void StartTrine()
    {
        ClearActiveState();
        _active = true;
    }

    private void AddCurrentWaveSource(ActionEffectSet set)
    {
        var source = set.Source;
        if (source == null)
        {
            _noSourceSignals++;
            return;
        }

        if (!_currentWaveSources.Add(source.EntityId))
            return;

        var position = source.Position;
        if (_currentWavePositions.Any(existing => Vector3.DistanceSquared(existing, position) < 0.04f))
            return;

        _currentWavePositions.Add(position);
    }

    private void CaptureTrineTelegraphSignal(uint target, IGameObject? obj, uint data1, uint data2)
    {
        if (_telegraphTriangles.Count >= 7)
            return;

        if (data1 != 16 || data2 != 32 || obj == null || !IsTrineTelegraphObject(obj))
            return;

        if (!_telegraphSignalSources.Add(target))
            return;

        var order = _telegraphTriangles.Count + 1;
        var position = NormalizeY(obj.Position);
        _telegraphTriangles.Add(new TelegraphTriangleSignal(order, obj.DataId, position));
    }

    private void TryCompleteCurrentWave(bool partialAllowed)
    {
        if (_currentWaveIndex < 0 || _currentWaveIndex >= ExpectedWaveCounts.Length)
        {
            return;
        }

        var observedCount = _currentWaveSources.Count + _noSourceSignals;
        if (!partialAllowed && observedCount < ExpectedWaveCounts[_currentWaveIndex])
            return;

        if (partialAllowed && observedCount == 0 && _currentWavePositions.Count == 0)
            return;

        CompleteCurrentWave();
    }

    private void CompleteCurrentWave()
    {
        var completedWave = _currentWaveIndex;

        if (completedWave == 0)
        {
            _firstWavePositions.Clear();
            _firstWavePositions.AddRange(_currentWavePositions);
            SolveDestinationAfterFirstWave();
        }

        _currentWaveIndex++;
        _currentWavePositions.Clear();
        _currentWaveSources.Clear();
        _noSourceSignals = 0;
    }

    private void SolveDestinationAfterFirstWave()
    {
        SolveDestination(_firstWavePositions);
    }

    private void SetHalfRoomSafeDestination(uint castId)
    {
        var westSafe = castId == WingCleaveOtherSide;
        var destination = westSafe ? HalfRoomWestSafeDestination : HalfRoomEastSafeDestination;
        CacheSameDestinationForAllPlayers(destination);
    }

    private void SolveDestination(IReadOnlyList<Vector3> firstWavePositions)
    {
        if (!TrySolveFirstWaveRoute(firstWavePositions, out var tankDestination, out var partyDestination))
        {
            ClearDestinationCache();
            return;
        }

        _hasFirstWaveRoute = true;
        _firstWaveTankDestination = tankDestination;
        _firstWavePartyDestination = partyDestination;
        CacheRoleDestinations(partyDestination, tankDestination);
    }

    private void TryApplyFinalDestination()
    {
        if (!_hasFirstWaveRoute)
            return;

        if (!TryBuildThirdWaveTelegraphTriangles(out var thirdTriangles))
            return;

        var hazardPoints = thirdTriangles.SelectMany(triangle => triangle.Vertices).ToList();
        if (hazardPoints.Count == 0)
            return;

        var partyIdeal = _firstWavePartyDestination;
        var partyDestination = AdjustPointAwayFromHazardPoints(partyIdeal, hazardPoints, TrineExplosionClearance,
            PartyFinalSearchRadius);
        partyDestination = MoveOutwardFromArenaCenter(partyDestination, PartyFinalOutwardOffset);
        partyDestination = AdjustPointAwayFromHazardPoints(partyDestination, hazardPoints, TrineExplosionClearance,
            PartyFinalSearchRadius);

        var centralTriangle = thirdTriangles.MinBy(triangle => DistanceSquaredXZ(triangle.Center, ArenaCenter));
        var direction = SelectCentralEdgeDirection(centralTriangle, _firstWaveTankDestination);
        var mainTankDestination = FindNearestSafePointOnRay(centralTriangle.Center, direction, hazardPoints,
            TrineExplosionClearance, 0.0f, TankNearSearchMaxRadius);
        mainTankDestination = RefineTankNearDestinationInward(mainTankDestination, centralTriangle.Center, direction,
            hazardPoints, TrineExplosionClearance);
        mainTankDestination = MoveOutwardFromArenaCenter(mainTankDestination, TankNearFinalOutwardOffset);
        mainTankDestination = AdjustPointAwayFromHazardPoints(mainTankDestination, hazardPoints, TrineExplosionClearance,
            1.5f);
        var offTankDestination = AdjustPointAwayFromHazardPoints(centralTriangle.Center + direction * TankFarRadius,
            hazardPoints, TrineExplosionClearance, 1.5f);
        offTankDestination = MoveOutwardFromArenaCenter(offTankDestination, OffTankFinalOutwardOffset);
        offTankDestination = AdjustPointAwayFromHazardPoints(offTankDestination, hazardPoints, TrineExplosionClearance,
            1.5f);

        CacheFinalDestinations(partyDestination, mainTankDestination, offTankDestination);
    }

    private bool TrySolveFirstWaveRoute(IReadOnlyList<Vector3> firstWavePositions, out Vector3 tankDestination,
        out Vector3 partyDestination)
    {
        tankDestination = Vector3.Zero;
        partyDestination = Vector3.Zero;

        var firstTriangles = BuildFirstWaveTriangles(firstWavePositions);
        if (firstTriangles.Count != 3)
            return false;

        if (!TryBuildSecondWaveTelegraphTriangle(out var secondTriangle))
            return false;

        if (!TryBuildThirdWaveTelegraphTriangles(out var thirdTriangles))
            return false;

        var middleTriangle = thirdTriangles.MinBy(triangle => DistanceSquaredXZ(triangle.Center, ArenaCenter));
        var remainingHazardPoints = secondTriangle.Vertices
            .Concat(thirdTriangles.SelectMany(triangle => triangle.Vertices))
            .ToList();
        var safeGroups = BuildSafeCandidateGroups(firstTriangles, middleTriangle, remainingHazardPoints);
        if (safeGroups.Count == 0)
            return false;

        var reliableSafeGroups = safeGroups
            .Where(group => group.Candidates.Count >= MinimumSafeCandidatesPerSpot)
            .ToList();
        if (reliableSafeGroups.Count > 0)
            safeGroups = reliableSafeGroups;

        var singleGroup = SelectSingleSafeGroup(safeGroups, firstTriangles);
        partyDestination = SelectClosestCandidateToMiddleSide(singleGroup);

        var tankGroup = SelectDoubleSafeGroup(safeGroups, singleGroup, partyDestination);
        tankDestination = ReferenceEquals(tankGroup, singleGroup)
            ? SelectAwayCandidate(tankGroup, partyDestination)
            : SelectClosestCandidateToMiddleSide(tankGroup);

        partyDestination = MoveOutwardFromArenaCenter(partyDestination, PartyOutwardOffset);

        return true;
    }

    private bool TryBuildSecondWaveTelegraphTriangle(out TrineTriangle triangle)
    {
        var secondSignal = _telegraphTriangles.FirstOrDefault(signal => signal.Order == 4);
        if (secondSignal.Order != 4)
        {
            triangle = TrineTriangle.Empty;
            return false;
        }

        if (!TryBuildTelegraphTriangle(secondSignal.Center, secondSignal.DataId, out triangle))
            return false;

        return true;
    }

    private bool TryBuildThirdWaveTelegraphTriangles(out List<TrineTriangle> triangles)
    {
        var thirdSignals = _telegraphTriangles
            .Where(signal => signal.Order is >= 5 and <= 7)
            .OrderBy(signal => signal.Order)
            .ToList();

        if (thirdSignals.Count != 3)
        {
            triangles = [];
            return false;
        }

        triangles = [];
        foreach (var signal in thirdSignals)
        {
            if (!TryBuildTelegraphTriangle(signal.Center, signal.DataId, out var triangle))
            {
                triangles.Clear();
                return false;
            }

            triangles.Add(triangle);
        }

        return true;
    }

    private static List<SafeCandidateGroup> BuildSafeCandidateGroups(IReadOnlyList<TrineTriangle> firstTriangles,
        TrineTriangle middleTriangle, IReadOnlyList<Vector3> remainingHazardPoints)
    {
        var groups = GetEdgeMidpoints(middleTriangle.Vertices)
            .Select((midpoint, index) => new SafeCandidateGroup(index, midpoint,
                NormalizeXZ(midpoint - middleTriangle.Center)))
            .Where(group => group.Direction != Vector3.Zero)
            .ToList();

        foreach (var candidate in firstTriangles.SelectMany(triangle => triangle.Vertices).Select(NormalizeY))
        {
            if (!IsInsideUsableArena(candidate))
                continue;

            var clearance = MinimumDistanceToPoints(candidate, remainingHazardPoints);
            if (clearance < FirstMoveClearance)
                continue;

            var candidateDirection = NormalizeXZ(candidate - middleTriangle.Center);
            if (candidateDirection == Vector3.Zero)
                continue;

            var group = groups
                .OrderByDescending(item => DotXZ(item.Direction, candidateDirection))
                .First();
            group.Candidates.Add(new SafeCandidate(candidate, clearance,
                DistanceXZ(candidate, group.EdgeMidpoint)));
        }

        return groups
            .Where(group => group.Candidates.Count > 0)
            .OrderBy(group => group.EdgeIndex)
            .ToList();
    }

    private static SafeCandidateGroup SelectSingleSafeGroup(IReadOnlyList<SafeCandidateGroup> safeGroups,
        IReadOnlyList<TrineTriangle> firstTriangles)
    {
        return safeGroups
            .OrderByDescending(group => group.Candidates.Count)
            .ThenBy(group => firstTriangles.Min(triangle => DistanceXZ(triangle.Center, group.EdgeMidpoint)))
            .First();
    }

    private static SafeCandidateGroup SelectDoubleSafeGroup(IReadOnlyList<SafeCandidateGroup> safeGroups,
        SafeCandidateGroup singleGroup, Vector3 partyDestination)
    {
        if (safeGroups.Count == 1)
            return singleGroup;

        return safeGroups
            .Where(group => !ReferenceEquals(group, singleGroup))
            .OrderBy(group => DotXZ(group.Direction, singleGroup.Direction))
            .ThenByDescending(group => DistanceSquaredXZ(SelectClosestCandidateToMiddleSide(group), partyDestination))
            .ThenByDescending(group => group.Candidates.Count)
            .First();
    }

    private static Vector3 SelectClosestCandidateToMiddleSide(SafeCandidateGroup group)
    {
        return group.Candidates
            .OrderBy(candidate => candidate.DistanceToEdge)
            .ThenByDescending(candidate => candidate.Clearance)
            .Select(candidate => candidate.Position)
            .First();
    }

    private static Vector3 SelectAwayCandidate(SafeCandidateGroup group, Vector3 tankDestination)
    {
        return group.Candidates
            .OrderByDescending(candidate => DistanceSquaredXZ(candidate.Position, tankDestination))
            .ThenByDescending(candidate => DistanceSquaredXZ(candidate.Position, ArenaCenter))
            .ThenByDescending(candidate => candidate.Clearance)
            .Select(candidate => candidate.Position)
            .First();
    }

    private static bool TryBuildTelegraphTriangle(Vector3 center, uint dataId, out TrineTriangle triangle)
    {
        var normalized = NormalizeY(center);
        triangle = dataId switch
        {
            TrineTelegraphDataIdA => new TrineTriangle(
            [
                new Vector3(normalized.X + TrineCenterToPointX, 0.0f, normalized.Z),
                new Vector3(normalized.X - TrineCenterToSideX, 0.0f, normalized.Z + TrineCenterToPointZ),
                new Vector3(normalized.X - TrineCenterToSideX, 0.0f, normalized.Z - TrineCenterToPointZ)
            ]),
            TrineTelegraphDataIdB => new TrineTriangle(
            [
                new Vector3(normalized.X - TrineCenterToPointX, 0.0f, normalized.Z),
                new Vector3(normalized.X + TrineCenterToSideX, 0.0f, normalized.Z + TrineCenterToPointZ),
                new Vector3(normalized.X + TrineCenterToSideX, 0.0f, normalized.Z - TrineCenterToPointZ)
            ]),
            _ => TrineTriangle.Empty
        };

        return !ReferenceEquals(triangle, TrineTriangle.Empty);
    }

    private static List<TrineTriangle> BuildFirstWaveTriangles(IReadOnlyList<Vector3> positions)
    {
        var uniquePositions = new List<Vector3>();
        foreach (var position in positions)
            AddUniquePosition(uniquePositions, NormalizeY(position));

        if (uniquePositions.Count != 9)
            return [];

        var candidates = new List<TrineTriangleCandidate>();
        for (var i = 0; i < uniquePositions.Count - 2; i++)
        for (var j = i + 1; j < uniquePositions.Count - 1; j++)
        for (var k = j + 1; k < uniquePositions.Count; k++)
        {
            if (!IsTrineTriangle(uniquePositions[i], uniquePositions[j], uniquePositions[k]))
                continue;

            candidates.Add(new TrineTriangleCandidate([i, j, k],
                new TrineTriangle([uniquePositions[i], uniquePositions[j], uniquePositions[k]])));
        }

        var bestScore = float.MaxValue;
        List<TrineTriangle>? bestTriangles = null;

        for (var i = 0; i < candidates.Count - 2; i++)
        for (var j = i + 1; j < candidates.Count - 1; j++)
        for (var k = j + 1; k < candidates.Count; k++)
        {
            if (!CoversEveryPositionOnce(candidates[i], candidates[j], candidates[k], uniquePositions.Count))
                continue;

            var triangles = new List<TrineTriangle> { candidates[i].Triangle, candidates[j].Triangle, candidates[k].Triangle };
            var distances = new[]
            {
                DistanceXZ(triangles[0].Center, triangles[1].Center),
                DistanceXZ(triangles[0].Center, triangles[2].Center),
                DistanceXZ(triangles[1].Center, triangles[2].Center)
            }.OrderBy(distance => distance).ToArray();

            var score = MathF.Abs(distances[0] - CloseTriangleCenterDistance) +
                MathF.Abs(distances[1] - MediumTriangleCenterDistance) +
                MathF.Abs(distances[2] - FarTriangleCenterDistance);

            if (score >= bestScore)
                continue;

            bestScore = score;
            bestTriangles = triangles;
        }

        return bestTriangles ?? [];
    }

    private static bool CoversEveryPositionOnce(TrineTriangleCandidate a, TrineTriangleCandidate b,
        TrineTriangleCandidate c, int positionCount)
    {
        var counts = new int[positionCount];
        foreach (var index in a.Indices.Concat(b.Indices).Concat(c.Indices))
            counts[index]++;

        return counts.All(count => count == 1);
    }

    private static bool IsTrineTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        return IsTrineSide(a, b) && IsTrineSide(a, c) && IsTrineSide(b, c);
    }

    private static bool IsTrineSide(Vector3 a, Vector3 b)
    {
        return MathF.Abs(DistanceXZ(a, b) - TrineSideLength) <= TrineSideTolerance;
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        return MathF.Sqrt(DistanceSquaredXZ(a, b));
    }

    private static float DistanceSquaredXZ(Vector3 a, Vector3 b)
    {
        var x = a.X - b.X;
        var z = a.Z - b.Z;
        return x * x + z * z;
    }

    private static Vector3 AdjustPointAwayFromHazardPoints(Vector3 ideal, IReadOnlyList<Vector3> hazardPoints,
        float clearance, float searchRadius)
    {
        var normalizedIdeal = NormalizeY(ideal);
        if (IsPointClearOfHazardPoints(normalizedIdeal, hazardPoints, clearance))
            return normalizedIdeal;

        var best = normalizedIdeal;
        var bestClearance = MinimumDistanceToPoints(normalizedIdeal, hazardPoints);
        var bestScore = float.MaxValue;

        const float radiusStep = 0.25f;
        const int directionSteps = 48;
        for (var radius = radiusStep; radius <= searchRadius + 0.001f; radius += radiusStep)
        {
            var foundAtThisRadius = false;
            for (var i = 0; i < directionSteps; i++)
            {
                var angle = 2.0f * MathF.PI * i / directionSteps;
                var candidate = NormalizeY(new Vector3(
                    normalizedIdeal.X + MathF.Cos(angle) * radius,
                    0.0f,
                    normalizedIdeal.Z + MathF.Sin(angle) * radius));

                if (!IsInsideUsableArena(candidate))
                    continue;

                var candidateClearance = MinimumDistanceToPoints(candidate, hazardPoints);
                if (candidateClearance > bestClearance)
                {
                    best = candidate;
                    bestClearance = candidateClearance;
                }

                if (candidateClearance < clearance)
                    continue;

                var score = radius - DistanceXZ(candidate, ArenaCenter) * 0.01f;
                if (score >= bestScore)
                    continue;

                best = candidate;
                bestScore = score;
                foundAtThisRadius = true;
            }

            if (foundAtThisRadius)
                return best;
        }

        return best;
    }

    private static Vector3 FindNearestSafePointOnRay(Vector3 origin, Vector3 direction, IReadOnlyList<Vector3> hazardPoints,
        float clearance, float minRadius, float maxRadius)
    {
        var normalizedOrigin = NormalizeY(origin);
        var normalizedDirection = NormalizeXZ(direction);
        if (normalizedDirection == Vector3.Zero)
            normalizedDirection = Vector3.UnitX;

        const float radiusStep = 0.1f;
        for (var radius = minRadius; radius <= maxRadius + 0.001f; radius += radiusStep)
        {
            var candidate = normalizedOrigin + normalizedDirection * radius;
            if (!IsInsideUsableArena(candidate))
                continue;

            if (IsPointClearOfHazardPoints(candidate, hazardPoints, clearance))
                return NormalizeY(candidate);
        }

        return NormalizeY(normalizedOrigin + normalizedDirection * maxRadius);
    }

    private static Vector3 RefineTankNearDestinationInward(Vector3 destination, Vector3 origin, Vector3 direction,
        IReadOnlyList<Vector3> hazardPoints, float clearance)
    {
        var normalizedDestination = NormalizeY(destination);
        var normalizedOrigin = NormalizeY(origin);
        var normalizedDirection = NormalizeXZ(direction);
        if (normalizedDirection == Vector3.Zero)
            return normalizedDestination;

        var best = normalizedDestination;
        var bestScore = DistanceXZ(best, ArenaCenter);

        const float radiusStep = 0.2f;
        const int directionSteps = 32;
        for (var radius = radiusStep; radius <= TankNearInwardSearchRadius + 0.001f; radius += radiusStep)
        {
            for (var i = 0; i < directionSteps; i++)
            {
                var angle = 2.0f * MathF.PI * i / directionSteps;
                var candidate = NormalizeY(new Vector3(
                    normalizedDestination.X + MathF.Cos(angle) * radius,
                    0.0f,
                    normalizedDestination.Z + MathF.Sin(angle) * radius));

                if (!IsInsideUsableArena(candidate))
                    continue;

                if (!IsPointClearOfHazardPoints(candidate, hazardPoints, clearance))
                    continue;

                var originToCandidate = NormalizeXZ(candidate - normalizedOrigin);
                if (originToCandidate != Vector3.Zero && DotXZ(originToCandidate, normalizedDirection) < 0.5f)
                    continue;

                var score = DistanceXZ(candidate, ArenaCenter) + DistanceXZ(candidate, normalizedDestination) * 0.05f;
                if (score >= bestScore)
                    continue;

                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private static bool IsPointClearOfHazardPoints(Vector3 point, IReadOnlyList<Vector3> hazardPoints, float clearance)
    {
        return MinimumDistanceToPoints(point, hazardPoints) >= clearance;
    }

    private static float MinimumDistanceToPoints(Vector3 point, IReadOnlyList<Vector3> hazardPoints)
    {
        if (hazardPoints.Count == 0)
            return float.PositiveInfinity;

        var minimum = float.PositiveInfinity;
        foreach (var hazardPoint in hazardPoints)
            minimum = MathF.Min(minimum, DistanceXZ(point, hazardPoint));

        return minimum;
    }

    private static Vector3 SelectCentralEdgeDirection(TrineTriangle centralTriangle, Vector3 routeDestination)
    {
        var routeDirection = NormalizeXZ(routeDestination - centralTriangle.Center);
        if (routeDirection == Vector3.Zero)
            routeDirection = Vector3.UnitX;

        return GetEdgeMidpoints(centralTriangle.Vertices)
            .Select(midpoint => NormalizeXZ(midpoint - centralTriangle.Center))
            .Where(direction => direction != Vector3.Zero)
            .OrderByDescending(direction => DotXZ(direction, routeDirection))
            .DefaultIfEmpty(Vector3.UnitX)
            .First();
    }

    private static Vector3 NormalizeXZ(Vector3 vector)
    {
        var length = MathF.Sqrt(vector.X * vector.X + vector.Z * vector.Z);
        if (length < 0.001f)
            return Vector3.Zero;

        return new Vector3(vector.X / length, 0.0f, vector.Z / length);
    }

    private static float DotXZ(Vector3 a, Vector3 b)
    {
        return a.X * b.X + a.Z * b.Z;
    }

    private static bool IsInsideUsableArena(Vector3 position)
    {
        return DistanceXZ(position, ArenaCenter) <= ArenaUsableRadius;
    }

    private static Vector3 MoveOutwardFromArenaCenter(Vector3 position, float distance)
    {
        var normalized = NormalizeY(position);
        var direction = NormalizeXZ(normalized - ArenaCenter);
        if (direction == Vector3.Zero)
            return normalized;

        return ClampToUsableArena(normalized + direction * distance);
    }

    private static Vector3 ClampToUsableArena(Vector3 position)
    {
        var normalized = NormalizeY(position);
        var direction = NormalizeXZ(normalized - ArenaCenter);
        if (direction == Vector3.Zero || DistanceXZ(normalized, ArenaCenter) <= ArenaUsableRadius)
            return normalized;

        return NormalizeY(ArenaCenter + direction * ArenaUsableRadius);
    }

    private static Vector3 NormalizeY(Vector3 position)
    {
        return new Vector3(position.X, 0.0f, position.Z);
    }

    private static IEnumerable<Vector3> GetEdgeMidpoints(IReadOnlyList<Vector3> vertices)
    {
        for (var i = 0; i < vertices.Count; i++)
        {
            var a = vertices[i];
            var b = vertices[(i + 1) % vertices.Count];
            yield return new Vector3((a.X + b.X) / 2.0f, 0.0f, (a.Z + b.Z) / 2.0f);
        }
    }

    private void CacheSameDestinationForAllPlayers(Vector3 destination)
    {
        ClearDestinationCache();

        var normalizedDestination = NormalizeY(destination);
        foreach (var player in GetPartyPlayers())
            CacheDestinationForPlayer(player, normalizedDestination);

        _fallbackPartyDestination = normalizedDestination;
        _fallbackTankDestination = normalizedDestination;
        _hasDestination = true;
    }

    private void CacheRoleDestinations(Vector3 partyDestination, Vector3 tankDestination)
    {
        ClearDestinationCache();

        var partyPosition = NormalizeY(partyDestination);
        var tankPosition = NormalizeY(tankDestination);
        foreach (var player in GetPartyPlayers())
            CacheDestinationForPlayer(player, player.GetRole() == CombatRole.Tank ? tankPosition : partyPosition);

        _fallbackPartyDestination = partyPosition;
        _fallbackTankDestination = tankPosition;
        _hasDestination = true;
        _showFirstMoveRouteArrows = true;
    }

    private void CacheFinalDestinations(Vector3 partyDestination, Vector3 mainTankDestination,
        Vector3 offTankDestination)
    {
        ClearDestinationCache();

        var partyPosition = NormalizeY(partyDestination);
        var mainTankPosition = NormalizeY(mainTankDestination);
        var offTankPosition = NormalizeY(offTankDestination);
        var orderedTanks = GetOrderedTanksForFinal();

        foreach (var player in GetPartyPlayers())
        {
            var position = player.GetRole() switch
            {
                CombatRole.Tank when orderedTanks.Count > 1 && SameCharacter(player, orderedTanks[1]) =>
                    offTankPosition,
                CombatRole.Tank => mainTankPosition,
                _ => partyPosition
            };
            CacheDestinationForPlayer(player, position);
        }

        _fallbackPartyDestination = partyPosition;
        _fallbackTankDestination = mainTankPosition;
        _hasDestination = true;
    }

    private void ClearDestinationCache()
    {
        _destinationCache.Clear();
        _fallbackPartyDestination = null;
        _fallbackTankDestination = null;
        _hasDestination = false;
        _showFirstMoveRouteArrows = false;
    }

    private void CacheDestinationForPlayer(IPlayerCharacter player, Vector3 destination)
    {
        _destinationCache[GetPlayerKey(player)] = destination;
    }

    private bool TryGetCachedDestination(IPlayerCharacter? player, out Vector3 destination)
    {
        if (player != null)
        {
            if (_destinationCache.TryGetValue(GetPlayerKey(player), out destination))
                return true;

            var roleFallback = player.GetRole() == CombatRole.Tank
                ? _fallbackTankDestination
                : _fallbackPartyDestination;
            if (roleFallback.HasValue)
            {
                destination = roleFallback.Value;
                return true;
            }
        }

        destination = default;
        return false;
    }

    private List<IPlayerCharacter> GetOrderedTanksForFinal()
    {
        var orderedTanks = C.PriorityData.GetPlayers(member =>
                member.IGameObject is IPlayerCharacter player && player.GetRole() == CombatRole.Tank)
            .Select(member => (IPlayerCharacter)member.IGameObject)
            .Take(2)
            .ToList();

        var partyTanks = GetPartyPlayers()
            .Where(player => player.GetRole() == CombatRole.Tank)
            .OrderBy(player => player.EntityId)
            .ToList();

        foreach (var partyTank in partyTanks)
        {
            if (orderedTanks.Count >= 2)
                break;

            if (orderedTanks.Any(tank => SameCharacter(tank, partyTank)))
                continue;

            orderedTanks.Add(partyTank);
        }

        return orderedTanks;
    }

    private List<IPlayerCharacter> GetPartyPlayers()
    {
        var players = Controller.GetPartyMembers()
            .OfType<IPlayerCharacter>()
            .ToList();
        var me = BasePlayer;
        if (me != null && players.All(player => !SameCharacter(player, me)))
            players.Add(me);

        return players;
    }

    private static string GetPlayerKey(IPlayerCharacter player)
    {
        var name = player.Name.ToString();
        return string.IsNullOrWhiteSpace(name) ? $"entity:{player.EntityId:X8}" : name;
    }

    private void ApplyDisplay()
    {
        Controller.GetRegisteredElements().Each(element => element.Value.Enabled = false);

        var me = BasePlayer;
        if (me == null) return;

        if (_active && C.ShowSharedRouteMarkers && _hasFirstWaveRoute &&
            _fallbackTankDestination.HasValue && _fallbackPartyDestination.HasValue)
        {
            var tankDestination = me.GetRole() == CombatRole.Tank && TryGetCachedDestination(me, out var myDestination)
                ? myDestination
                : _fallbackTankDestination.Value;
            ShowDestinationElement("TankDestination", tankDestination, 0xC84080FF);
            ShowDestinationElement("PartyDestination", _fallbackPartyDestination.Value, 0xC800FF80);
            if (_showFirstMoveRouteArrows)
            {
                ShowRouteArrow("TankRouteArrow", _firstWaveTankDestination, 0xC84080FF);
                ShowRouteArrow("PartyRouteArrow", _firstWavePartyDestination, 0xC800FF80);
            }
            return;
        }

        if (_active && _hasDestination && TryGetCachedDestination(me, out var cached) &&
            Controller.TryGetElementByName("Destination", out var destination))
        {
            destination.Enabled = true;
            destination.SetRefPosition(cached);
            destination.color = GetRainbowColor();
            destination.overlayText = "";
        }
    }

    private void ShowDestinationElement(string name, Vector3 position, uint color)
    {
        if (!Controller.TryGetElementByName(name, out var element))
            return;

        element.Enabled = true;
        element.SetRefPosition(position);
        element.color = color;
        element.overlayText = "";
    }

    private static Element CreateRouteArrowElement(uint color) => new(2)
    {
        Enabled = false,
        radius = 0.0f,
        thicc = RouteArrowThickness,
        color = color,
        LineEndB = LineEnd.Arrow
    };

    private void ShowRouteArrow(string name, Vector3 destination, uint color)
    {
        if (!Controller.TryGetElementByName(name, out var element))
            return;

        var direction = NormalizeXZ(destination - ArenaCenter);
        if (direction == Vector3.Zero)
            return;

        var distance = DistanceXZ(ArenaCenter, destination);
        var startDistance = MathF.Min(RouteArrowStartDistance, distance * 0.4f);
        var endPadding = MathF.Min(RouteArrowEndPadding, MathF.Max(0.0f, distance - startDistance) * 0.3f);
        var start = ArenaCenter + direction * startDistance;
        var end = destination - direction * endPadding;

        if (DistanceSquaredXZ(start, end) < 0.25f)
        {
            start = ArenaCenter;
            end = destination;
        }

        element.Enabled = true;
        element.SetRefPosition(start);
        element.SetOffPosition(end);
        element.color = color;
        element.overlayText = "";
    }

    private static uint GetRainbowColor()
    {
        var hue = Environment.TickCount64 % 4000L / 4000.0f;
        var (red, green, blue) = HsvToRgb(hue, 0.9f, 1.0f);
        return 0xC8000000u |
               ((uint)Math.Clamp((int)MathF.Round(red * 255.0f), 0, 255) << 16) |
               ((uint)Math.Clamp((int)MathF.Round(green * 255.0f), 0, 255) << 8) |
               (uint)Math.Clamp((int)MathF.Round(blue * 255.0f), 0, 255);
    }

    private static (float Red, float Green, float Blue) HsvToRgb(float hue, float saturation, float value)
    {
        var sector = hue * 6.0f;
        var index = (int)MathF.Floor(sector);
        var fraction = sector - index;
        var p = value * (1.0f - saturation);
        var q = value * (1.0f - saturation * fraction);
        var t = value * (1.0f - saturation * (1.0f - fraction));

        return (index % 6) switch
        {
            0 => (value, t, p),
            1 => (q, value, p),
            2 => (p, value, t),
            3 => (p, q, value),
            4 => (t, p, value),
            _ => (value, p, q)
        };
    }

    private void ResetState()
    {
        ClearActiveState();
    }

    private void ClearActiveState()
    {
        _active = false;
        ClearDestinationCache();
        _currentWaveIndex = 0;
        _noSourceSignals = 0;
        _hasFirstWaveRoute = false;
        _firstWaveTankDestination = Vector3.Zero;
        _firstWavePartyDestination = Vector3.Zero;
        _currentWavePositions.Clear();
        _currentWaveSources.Clear();
        _firstWavePositions.Clear();
        _telegraphTriangles.Clear();
        _telegraphSignalSources.Clear();
        Controller.GetRegisteredElements().Each(element => element.Value.Enabled = false);
    }

    private static bool IsTrineTelegraphObject(IGameObject obj)
    {
        return obj.DataId is TrineTelegraphDataIdA or TrineTelegraphDataIdB;
    }

    private static void AddUniquePosition(List<Vector3> positions, Vector3 candidate)
    {
        if (positions.Any(existing => Vector3.DistanceSquared(existing, candidate) < 0.04f))
            return;

        positions.Add(candidate);
    }

    private static bool SameCharacter(IPlayerCharacter left, IPlayerCharacter right)
    {
        if (left.EntityId != 0 && left.EntityId == right.EntityId)
            return true;

        return string.Equals(left.Name.ToString(), right.Name.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TrineTriangle
    {
        public static readonly TrineTriangle Empty = new([]);

        public TrineTriangle(List<Vector3> vertices)
        {
            Vertices = vertices;
            Center = vertices.Count == 0
                ? Vector3.Zero
                : new Vector3(vertices.Average(vertex => vertex.X), 0.0f, vertices.Average(vertex => vertex.Z));
        }

        public List<Vector3> Vertices { get; }
        public Vector3 Center { get; }
    }

    private readonly struct TelegraphTriangleSignal
    {
        public TelegraphTriangleSignal(int order, uint dataId, Vector3 center)
        {
            Order = order;
            DataId = dataId;
            Center = center;
        }

        public int Order { get; }
        public uint DataId { get; }
        public Vector3 Center { get; }
    }

    private sealed class SafeCandidateGroup
    {
        public SafeCandidateGroup(int edgeIndex, Vector3 edgeMidpoint, Vector3 direction)
        {
            EdgeIndex = edgeIndex;
            EdgeMidpoint = edgeMidpoint;
            Direction = direction;
        }

        public int EdgeIndex { get; }
        public Vector3 EdgeMidpoint { get; }
        public Vector3 Direction { get; }
        public List<SafeCandidate> Candidates { get; } = [];
    }

    private readonly struct SafeCandidate
    {
        public SafeCandidate(Vector3 position, float clearance, float distanceToEdge)
        {
            Position = position;
            Clearance = clearance;
            DistanceToEdge = distanceToEdge;
        }

        public Vector3 Position { get; }
        public float Clearance { get; }
        public float DistanceToEdge { get; }
    }

    private readonly struct TrineTriangleCandidate
    {
        public TrineTriangleCandidate(int[] indices, TrineTriangle triangle)
        {
            Indices = indices;
            Triangle = triangle;
        }

        public int[] Indices { get; }
        public TrineTriangle Triangle { get; }
    }

    public sealed class TankPriorityData : PriorityData
    {
        public override int GetNumPlayers() => 2;
    }

    public sealed class Config : IEzConfig
    {
        public bool ShowSharedRouteMarkers;

        public TankPriorityData PriorityData = new()
        {
            Name = "Trine MT/OT priority",
            Description = "Default: T1 then T2. Used only for MT/OT tankbuster split.",
            PriorityLists =
            [
                new PriorityList
                {
                    IsRole = true,
                    List =
                    [
                        new JobbedPlayer { Role = RolePosition.T1 },
                        new JobbedPlayer { Role = RolePosition.T2 }
                    ]
                }
            ]
        };

        public void EnsureDefaults()
        {
            PriorityData ??= new TankPriorityData();
            PriorityData.Name = "Trine MT/OT priority";
            PriorityData.Description = "Default: T1 then T2. Used only for MT/OT tankbuster split.";
            PriorityData.PriorityLists ??= [];
            if (PriorityData.PriorityLists.Count == 0)
                PriorityData.PriorityLists.Add(new PriorityList
                {
                    IsRole = true,
                    List =
                    [
                        new JobbedPlayer { Role = RolePosition.T1 },
                        new JobbedPlayer { Role = RolePosition.T2 }
                    ]
                });

            foreach (var list in PriorityData.PriorityLists.Where(list => list != null))
            {
                list.List ??= [];
                while (list.List.Count > 2)
                    list.List.RemoveAt(list.List.Count - 1);
                while (list.List.Count < 2)
                    list.List.Add(new JobbedPlayer());
            }
        }
    }
}
