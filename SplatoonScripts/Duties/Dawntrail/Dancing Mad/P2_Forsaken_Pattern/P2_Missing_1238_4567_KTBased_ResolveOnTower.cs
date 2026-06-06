using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ECommons.PartyFunctions;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using Splatoon.Utility;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

internal class P2_Missing_1238_4567_KTBased_ResolveOnTower : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryDmad];

    #endregion

    #region Constants

    private const uint TerritoryDmad = 1363;

    private const uint StatusStack = 5084;
    private const uint StatusSpread = 5085;
    private const uint StatusCone = 5086;

    private const uint FuturesEndCast = 47826;
    private const uint PastsEndCast = 47827;
    private const uint AllThingsEndingCast1 = 47836;
    private const uint AllThingsEndingCast2 = 47837;
    private const float InterludeNavDistanceFromCenter = 4f;

    // Map effect indices 1-8 (P2_Forsaken_beta): spawn 1/2, clear 4/8.
    private const uint MapEffectTowerIndexMin = 1;
    private const uint MapEffectTowerIndexMax = 8;
    private const ushort MapEffectTowerSpawnData1 = 1;
    private const ushort MapEffectTowerSpawnData2 = 2;
    private const ushort MapEffectTowerClearData1 = 4;
    private const ushort MapEffectTowerClearData2 = 8;
    // Two towers are 90° apart on the 8-slot ring (+2 indices), not opposite (+4).
    private const int TowerPairMapStepOffset = 2;

    private const int SceneP2 = 7;
    private const int PartyPlayerCount = 8;

    // Step1 ResolveInitialGroup: party pairs by priority index (plan5).
    private static readonly (int A, int B)[] Step1PartyPairIndicesAlternate = [(0, 2), (1, 3), (4, 6), (5, 7)];
    private static readonly (int A, int B)[] Step1PartyPairIndicesAdjacent = [(0, 1), (2, 3), (4, 5), (6, 7)];
    private const int Step1PartyPairCount = 4;
    private static readonly string[] Step1PairModeLabels =
    [
        "Alternate (T1H1, T2H2, M1R1, M2R2)",
        "Adjacent (T1T2, H1H2, M1M2, R1R2)",
    ];
    private const int StackPairCount = 2;
    private const int NonStackPairCount = 2;

    private const int ActiveStepMin = 1;
    private const int ActiveStepMax = 8;

    // FirstHalf group towers on steps 1,2,3,8; SecondHalf on steps 4,5,6,7.
    private static readonly HashSet<int> FirstHalfTowerSteps = [1, 2, 3, 8];
    private static readonly HashSet<int> SecondHalfTowerSteps = [4, 5, 6, 7];

    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);
    private static readonly Vector3 TrueNorth = new(100f, 0f, 120f);
    private const float AngleTolerance = 22.5f;
    private const float DefaultRangeInside = 3.25f;
    private const float DefaultRangeOutside = 4.75f;
    private const int PatternCount = 2;
    private const int MaxPatternAssignments = 10;
    private const int DebugPatternPreviewNone = -1;
    private const int DebugPatternPreviewRoleNone = -1;
    private const float TowerOffsetCardinal = 8f;
    private const float TowerOffsetDiagonal = 5.7f;

    private static readonly (string Label, Vector3 Position)[] TowerSlots =
    [
        ("N",  new(ArenaCenter.X, 0f, ArenaCenter.Z - TowerOffsetCardinal)),
        ("NE", new(ArenaCenter.X + TowerOffsetDiagonal, 0f, ArenaCenter.Z - TowerOffsetDiagonal)),
        ("E",  new(ArenaCenter.X + TowerOffsetCardinal, 0f, ArenaCenter.Z)),
        ("SE", new(ArenaCenter.X + TowerOffsetDiagonal, 0f, ArenaCenter.Z + TowerOffsetDiagonal)),
        ("S",  new(ArenaCenter.X, 0f, ArenaCenter.Z + TowerOffsetCardinal)),
        ("SW", new(ArenaCenter.X - TowerOffsetDiagonal, 0f, ArenaCenter.Z + TowerOffsetDiagonal)),
        ("W",  new(ArenaCenter.X - TowerOffsetCardinal, 0f, ArenaCenter.Z)),
        ("NW", new(ArenaCenter.X - TowerOffsetDiagonal, 0f, ArenaCenter.Z - TowerOffsetDiagonal)),
    ];

    private const string ElActiveTower0 = "ActiveTower0";
    private const string ElActiveTower1 = "ActiveTower1";
    private const string ElMyRole = "MyRole";

    private static readonly string[] BasisComboLabels = ["LeftTower", "RightTower", "Center"];

    private static readonly string[] DebugPatternPreviewLabels =
    [
        "None",
        "211 (2/1/1)",
        "022 (0/2/2)",
    ];

    private static readonly PatternDefinition[] Patterns =
    [
        new(0, 2, 1, 1,
        [
            Rule("211_StackPriority1", PositionBasis.LeftTower, 180f, 2.0f),
            Rule("211_StackPriority2", PositionBasis.RightTower, 180f, 3.25f),
            Rule("211_OtherPriority1", PositionBasis.LeftTower, 0f, 3.25f),
            Rule("211_OtherPriority2", PositionBasis.RightTower, 0f, 3.25f),
            Rule("211_Spread", PositionBasis.RightTower, 0f, 3.25f),
            Rule("211_Cone", PositionBasis.LeftTower, 0f, 3.25f),
            Rule("211_NotTowerPriority1", PositionBasis.LeftTower, 0f, 4.75f),
            Rule("211_NotTowerPriority2", PositionBasis.LeftTower, 180f, 4.75f),
            Rule("211_NotTowerPriority3", PositionBasis.RightTower, 180f, 4.75f),
            Rule("211_NotTowerPriority4", PositionBasis.RightTower, 180f, 4.75f),
        ]),
        new(1, 0, 2, 2,
        [
            Rule("022_SpreadPriority1", PositionBasis.LeftTower, 0f, 3.25f),
            Rule("022_SpreadPriority2", PositionBasis.RightTower, 0f, 3.25f),
            Rule("022_ConePriority1", PositionBasis.LeftTower, 180f, 3.25f),
            Rule("022_ConePriority2", PositionBasis.RightTower, 180f, 3.25f),
            Rule("022_DemisePriority1", PositionBasis.LeftTower, 90f, 4.75f),
            Rule("022_DemisePriority2", PositionBasis.Center, 315f, 5.0f),
            Rule("022_DemisePriority3", PositionBasis.Center, 45f, 5.0f),
            Rule("022_DemisePriority4", PositionBasis.RightTower, 270f, 4.75f),
        ]),
    ];

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();
    private IPlayerCharacter BasePlayer => Controller.BasePlayer;

    #endregion

    #region State

    private readonly Vector3[] _activeTowerPositions = [Vector3.Zero, Vector3.Zero];
    private readonly uint[] _activeTowerEntityIds = [0, 0];
    private readonly List<uint> _pendingTowerSpawnPositions = [];
    private readonly List<uint> _pendingTowerClearPositions = [];
    private bool _hasActiveTowers;
    private int _step;
    private Tower1Side _tower1Side = Tower1Side.Unknown;
    private readonly List<PlayerInfo> _infos = [];
    private bool _initialGroupResolved;
    private bool _initialRolesAssigned;
    private InterludeNavPhase _interludeNavPhase;
    private bool _interludeBossCastSeen;

    #endregion

    #region Types

    private readonly record struct PositionRule(PositionBasis Basis, float AngleDeg, float Range);

    private readonly record struct PatternAssignment(string Label, PositionRule Rule);

    private readonly record struct PatternDefinition(
        int Id, int StackCount, int SpreadCount, int ConeCount, PatternAssignment[] Assignments);

    private enum PositionBasis
    {
        LeftTower,
        RightTower,
        Center,
    }

    private enum Tower1Side
    {
        Unknown,
        Left,
        Right,
    }

    private enum DebuffKind
    {
        None,
        Stack,
        Spread,
        Cone,
    }

    private enum MechanicHalf
    {
        None,
        First,
        Second,
    }

    private enum InterludeNavPhase
    {
        None,
        CastingPast,
        CastingFuture,
        PastGap,
        FutureOpposite,
    }

    private enum Step1PairModeKind
    {
        Alternate,
        Adjacent,
    }

    private readonly record struct OldRoleSwapGroup(string RoleA, string RoleB, int PrioritySuffix);

    private static readonly OldRoleSwapGroup[] OldRoleSwapGroups =
    [
        new("211_StackPriority1", "211_OtherPriority1", 1),
        new("211_StackPriority2", "211_OtherPriority2", 2),
        new("022_SpreadPriority1", "022_ConePriority1", 1),
        new("022_SpreadPriority2", "022_ConePriority2", 2),
    ];

    private sealed class PlayerInfo
    {
        public required IPlayerCharacter Player;
        public MechanicHalf Half;
        public DebuffKind Debuff;
        public string? RoleLabel;
        public string? OldRole;
    }

    private readonly record struct TowerHit(string Label, Vector3 SlotPosition, uint EntityId);

    private sealed class PatternRuleSettings
    {
        public int Basis;
        public float AngleDeg;
        public float Range;
    }

    private sealed class Config : IEzConfig
    {
        public PriorityData PriorityData = CreateDefaultPriorityData();
        public int DebugPatternPreview = DebugPatternPreviewNone;
        public int DebugPatternPreviewRole = DebugPatternPreviewRoleNone;
        public int Step1PairMode = (int)Step1PairModeKind.Alternate;
        public PatternRuleSettings[][] PatternRules = CreateDefaultPatternRules();

        public void EnsureDefaults()
        {
            PriorityData ??= CreateDefaultPriorityData();
            if(DebugPatternPreview >= PatternCount)
                DebugPatternPreview = DebugPatternPreviewNone;
            DebugPatternPreviewRole = ClampPatternPreviewRole(DebugPatternPreview, DebugPatternPreviewRole);
            if(Step1PairMode is < (int)Step1PairModeKind.Alternate or > (int)Step1PairModeKind.Adjacent)
                Step1PairMode = (int)Step1PairModeKind.Alternate;
            PatternRules = EnsurePatternRules(PatternRules);
        }

        public void ResetToDefaults()
        {
            PriorityData = CreateDefaultPriorityData();
            DebugPatternPreview = DebugPatternPreviewNone;
            DebugPatternPreviewRole = DebugPatternPreviewRoleNone;
            Step1PairMode = (int)Step1PairModeKind.Alternate;
            PatternRules = CreateDefaultPatternRules();
        }

        private static PriorityData CreateDefaultPriorityData()
            => new()
            {
                PriorityLists =
                [
                    new PriorityList
                    {
                        IsRole = true,
                        List =
                        [
                            new JobbedPlayer { Role = RolePosition.H1 },
                            new JobbedPlayer { Role = RolePosition.H2 },
                            new JobbedPlayer { Role = RolePosition.T1 },
                            new JobbedPlayer { Role = RolePosition.T2 },
                            new JobbedPlayer { Role = RolePosition.M1 },
                            new JobbedPlayer { Role = RolePosition.M2 },
                            new JobbedPlayer { Role = RolePosition.R1 },
                            new JobbedPlayer { Role = RolePosition.R2 },
                        ],
                    },
                ],
            };
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        C.EnsureDefaults();

        var activeTowerColor = EColor.CyanBright.ToUint();
        Controller.RegisterElement(ElActiveTower0, new Element(0)
        {
            Enabled = false,
            color = activeTowerColor,
            radius = 4f,
            thicc = 6f,
            fillIntensity = 0.25f,
            Filled = false,
        });
        Controller.RegisterElement(ElActiveTower1, new Element(0)
        {
            Enabled = false,
            color = activeTowerColor,
            radius = 4f,
            thicc = 6f,
            fillIntensity = 0.25f,
            Filled = false,
        });

        for(var i = 0; i < MaxPatternAssignments; i++)
        {
            Controller.RegisterElement(GetRolePreviewElementName(i), new Element(0)
            {
                Enabled = false,
                radius = 0.25f,
                Donut = 0.1f,
                fillIntensity = 0.544f,
            });
        }

        Controller.RegisterElement(ElMyRole, new Element(0)
        {
            Enabled = false,
            radius = 0.25f,
            Donut = 0.1f,
            fillIntensity = 0.544f,
            tether = true,
        });
    }

    public override void OnUpdate()
    {
        if(!IsPhaseActive())
        {
            ResetState();
            DisableAllMarkers();
            return;
        }

        UpdateInterludeNavPhase();
        UpdateActiveTowerMarkers();
        UpdateFieldMarkers();
    }

    public override void OnReset()
        => ResetState();

    public override void OnStartingCast(uint source, uint castId)
    {
        if(!IsPhaseActive())
            return;

        if(castId == PastsEndCast)
        {
            _interludeNavPhase = InterludeNavPhase.CastingPast;
            _interludeBossCastSeen = false;
        }
        else if(castId == FuturesEndCast)
        {
            _interludeNavPhase = InterludeNavPhase.CastingFuture;
            _interludeBossCastSeen = false;
        }
        else if(castId is AllThingsEndingCast1 or AllThingsEndingCast2)
        {
            _interludeNavPhase = InterludeNavPhase.None;
            _interludeBossCastSeen = false;
        }
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe))
            ResetState();
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(!IsPhaseActive())
            return;

        if(IsTowerSpawnMapEffect(data1, data2) && IsTowerMapPosition(position))
        {
            AddTowerSpawnPosition(position);
            return;
        }

        if(IsTowerClearMapEffect(data1, data2) && IsTowerMapPosition(position))
            AddTowerClearPosition(position);
    }

    public override void OnSettingsDraw()
    {
        if(ImGui.BeginTabBar("##P212384567KTSettings"))
        {
            if(ImGui.BeginTabItem("Main###tabMain"))
            {
                DrawPrioritySettings();
                DrawAllPatternAssignmentTables();
                ImGui.EndTabItem();
            }

            if(ImGui.BeginTabItem("Debug###tabDebug"))
            {
                DrawDebugTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    #endregion

    #region Settings UI

    private bool IsPhaseActive()
        => Controller.Scene == SceneP2;

    private void DrawPrioritySettings()
    {
        C.EnsureDefaults();
        var pairMode = C.Step1PairMode;
        if(ImGui.Combo("Step1 pair mode", ref pairMode, Step1PairModeLabels, Step1PairModeLabels.Length))
            C.Step1PairMode = pairMode;

        ImGui.TextUnformatted(GetStep1PairModeDescription(C.Step1PairMode));
        ImGui.TextUnformatted("Step2+: FirstHalf towers on steps 1,2,3,8; SecondHalf towers on steps 4,5,6,7.");

        ImGui.Spacing();
        ImGui.TextUnformatted($"Priority list");
        ImGui.Separator();

        C.PriorityData.Draw();
    }

    private void DrawAllPatternAssignmentTables()
    {
        C.EnsureDefaults();
        ImGui.Spacing();
        ImGui.TextUnformatted($"Pattern assignments ({PatternCount} patterns)");
        ImGui.Separator();

        foreach(var pattern in Patterns)
        {
            var header =
                $"Pattern {pattern.Id} (stack: {pattern.StackCount}/ spread: {pattern.SpreadCount}/ cone: {pattern.ConeCount})###patDef{pattern.Id}";
            if(!ImGui.TreeNode(header))
                continue;

            DrawPatternAssignmentTable(pattern);
            ImGui.TreePop();
        }
    }

    private void DrawPatternAssignmentTable(PatternDefinition pattern)
    {
        if(!ImGui.BeginTable($"PatternAssignments_{pattern.Id}###patTable", 5,
               ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            return;

        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 220f);
        ImGui.TableSetupColumn("Basis", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Angle", ImGuiTableColumnFlags.WidthFixed, 88f);
        ImGui.TableSetupColumn("Range", ImGuiTableColumnFlags.WidthFixed, 88f);
        ImGui.TableSetupColumn("Position", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        for(var i = 0; i < pattern.Assignments.Length; i++)
        {
            var assignment = pattern.Assignments[i];
            var settings = C.PatternRules[pattern.Id][i];
            var basis = settings.Basis;
            var angleDeg = settings.AngleDeg;
            var range = settings.Range;
            var changed = false;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(assignment.Label);

            ImGui.PushID($"patRule_{pattern.Id}_{i}");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1f);
            if(ImGui.Combo("##basis", ref basis, BasisComboLabels, BasisComboLabels.Length))
                changed = true;

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1f);
            if(ImGui.DragFloat("##angle", ref angleDeg, 1f, 0f, 360f, "%.0f"))
                changed = true;

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1f);
            if(ImGui.DragFloat("##range", ref range, 0.05f, 0f, 15f, "%.2f"))
                changed = true;
            ImGui.PopID();

            if(changed)
            {
                settings.Basis = ClampBasisIndex(basis);
                settings.AngleDeg = NormalizeAngle360(angleDeg);
                settings.Range = MathF.Max(0f, range);
                OnPatternRulesChanged();
            }

            ImGui.TableNextColumn();
            if(ResolvePositionRule(GetConfiguredRule(pattern.Id, i)) is { } pos)
                ImGui.TextUnformatted(FormatVector3(pos));
            else
                ImGui.TextUnformatted(_hasActiveTowers ? "(unresolved)" : "(no towers)");
        }

        ImGui.EndTable();
    }

    private void DrawDebugTab()
    {
        DrawDebugStepSection();
        ImGui.Spacing();
        DrawDebugInfosSection();
        ImGui.Spacing();
        ImGui.TextUnformatted("Preview");
        ImGui.Separator();
        DrawDebugPreviewSection();
    }

    private void DrawDebugStepSection()
    {
        ImGui.TextUnformatted("Step");
        ImGui.Separator();
        ImGui.TextUnformatted(_hasActiveTowers ? $"Step: {_step}" : "Step: —");
        ImGui.TextUnformatted(_initialGroupResolved ? "Initial group (KT pairs): resolved" : "Initial group (KT pairs): —");
        ImGui.TextUnformatted(_initialRolesAssigned ? "Initial roles (step1): assigned" : "Initial roles (step1): —");
        if(_hasActiveTowers && _step is >= ActiveStepMin and <= ActiveStepMax)
        {
            if(TryGetActiveMechanicHalf(out var activeHalf))
            {
                CountDebuffKindsFromActiveGroup(out var stack, out var spread, out var cone);
                ImGui.TextUnformatted(
                    $"Active group ({FormatHalf(activeHalf)}): stack={stack} spread={spread} cone={cone}");
            }

            ImGui.TextUnformatted(TryDetectPartyPattern(out var patternId)
                ? $"Live pattern: {DebugPatternPreviewLabels[patternId + 1]}"
                : "Live pattern: — (no match)");

            ImGui.TextUnformatted($"Active tower ids: {_activeTowerEntityIds[0]}, {_activeTowerEntityIds[1]}");
            ImGui.TextUnformatted($"Pending spawns: {FormatMapPositionList(_pendingTowerSpawnPositions)}");
            ImGui.TextUnformatted($"Pending clears: {FormatMapPositionList(_pendingTowerClearPositions)}");
            ImGui.TextUnformatted($"Interlude nav: {_interludeNavPhase}");
        }
    }

    private void DrawDebugStep1PairsSection()
    {
        ImGui.TextUnformatted($"Step1 pair mode: {Step1PairModeLabels[C.Step1PairMode]}");
        ImGui.TextUnformatted("Step1 pairs (priority index)");
        var pairIndices = GetStep1PartyPairIndices();
        for(var pairIndex = 0; pairIndex < Step1PartyPairCount; pairIndex++)
        {
            if(!TryGetStep1Pair(pairIndex, out var playerA, out var playerB))
            {
                ImGui.TextUnformatted($"  P{pairIndex}: (invalid)");
                continue;
            }

            var (indexA, indexB) = pairIndices[pairIndex];
            if(!TryClassifyStep1Pair(playerA, playerB, out _, out var partner, out var isStackPair))
            {
                ImGui.TextUnformatted(
                    $"  P{pairIndex} [{indexA},{indexB}]: {FormatPlayerDebuff(playerA)} + {FormatPlayerDebuff(playerB)} (invalid)");
                continue;
            }

            if(isStackPair)
            {
                var roleSuffix = _initialRolesAssigned
                    ? $" ({GetStep1StackPairRoleLabels(partner.Debuff)})"
                    : "";
                ImGui.TextUnformatted(
                    $"  P{pairIndex} [{indexA},{indexB}]: Stack+{FormatDebuffKindDebug(partner.Debuff)} -> FirstHalf{roleSuffix}");
            }
            else
            {
                ImGui.TextUnformatted(
                    $"  P{pairIndex} [{indexA},{indexB}]: {FormatPlayerDebuff(playerA)} + {FormatPlayerDebuff(playerB)} -> SecondHalf");
            }
        }
    }

    private static string GetStep1StackPairRoleLabels(DebuffKind partnerDebuff)
        => partnerDebuff switch
        {
            DebuffKind.Cone => "211_StackPriority1 / 211_OtherPriority1",
            DebuffKind.Spread => "211_StackPriority2 / 211_OtherPriority2",
            _ => "?",
        };

    private static string FormatPlayerDebuff(PlayerInfo info)
        => $"{info.Player.Name} ({FormatDebuffKindDebug(info.Debuff)})";

    private void DrawDebugInfosSection()
    {
        C.EnsureDefaults();
        ImGui.TextUnformatted("Player infos (live, ResolveOnTower)");
        ImGui.Separator();

        if(_infos.Count == 0)
        {
            ImGui.TextUnformatted("  (no party data)");
            return;
        }

        DrawDebugStep1PairsSection();
        ImGui.Spacing();

        DrawPlayerInfosTable("First Half", GetInfosByMechanicHalf(MechanicHalf.First));
        ImGui.Spacing();
        DrawPlayerInfosTable("Second Half", GetInfosByMechanicHalf(MechanicHalf.Second));

        var unresolvedCount = CountUnresolvedInfos();
        if(unresolvedCount > 0)
            ImGui.TextUnformatted($"Unresolved (step 1): {unresolvedCount} player(s)");

        CountDebuffKindsFromTowerInfos(out var stack, out var spread, out var cone);
        ImGui.TextUnformatted($"Tower counts: stack={stack} spread={spread} cone={cone}");
    }

    private static string GetStep1PairModeDescription(int pairMode)
        => pairMode == (int)Step1PairModeKind.Adjacent
            ? "Priority — Step1 pairs by list: [T1, T2], [H1, H2], [M1, M2], [R1, R2] (index: 0-1, 2-3, 4-5, 6-7)."
            : "Priority — Step1 pairs by list: [T1, H1], [T2, H2], [M1, R1], [M2, R2] (index: 0-2, 1-3, 4-6, 5-7).";

    private (int A, int B)[] GetStep1PartyPairIndices()
        => C.Step1PairMode == (int)Step1PairModeKind.Adjacent
            ? Step1PartyPairIndicesAdjacent
            : Step1PartyPairIndicesAlternate;

    // Returns two players for a step1 pair by priority-list indices.
    private bool TryGetStep1Pair(int pairIndex, out PlayerInfo playerA, out PlayerInfo playerB)
    {
        playerA = null!;
        playerB = null!;
        if(pairIndex < 0 || pairIndex >= Step1PartyPairCount || _infos.Count != PartyPlayerCount)
            return false;

        var (indexA, indexB) = GetStep1PartyPairIndices()[pairIndex];
        if(indexA < 0 || indexA >= PartyPlayerCount || indexB < 0 || indexB >= PartyPlayerCount)
            return false;

        playerA = _infos[indexA];
        playerB = _infos[indexB];
        return true;
    }

    private List<PlayerInfo> GetInfosByMechanicHalf(MechanicHalf half)
        => OrderInfosByPriority(_infos.Where(i => i.Half == half)).ToList();

    private int CountUnresolvedInfos()
        => _infos.Count(i => i.Half == MechanicHalf.None);

    private void DrawPlayerInfosTable(string title, IReadOnlyList<PlayerInfo> infos)
    {
        ImGui.TextUnformatted(title);
        if(infos.Count == 0)
        {
            ImGui.TextUnformatted("  (empty)");
            return;
        }

        if(!ImGui.BeginTable($"##12384567KTInfos{title}", 7,
               ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            return;

        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 20f);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 200f);
        ImGui.TableSetupColumn("Half", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Debuff", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Role", ImGuiTableColumnFlags.WidthFixed, 200f);
        ImGui.TableSetupColumn("OldRole", ImGuiTableColumnFlags.WidthFixed, 200f);
        ImGui.TableSetupColumn("Side", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        for(var i = 0; i < infos.Count; i++)
        {
            var info = infos[i];
            var priorityIndex = GetPriorityIndex(info.Player);
            var priority = priorityIndex is >= 0 and < PartyPlayerCount ? priorityIndex + 1 : 0;
            var isTower = IsTower(info);

            ImGui.TableNextRow();
            if(isTower)
                ImGui.PushStyleColor(ImGuiCol.Text, EColor.YellowBright);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(priority > 0 ? $"{priority}" : "—");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(info.Player.Name.ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(FormatHalf(info.Half));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(FormatDebuffKindDebug(info.Debuff));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(info.RoleLabel ?? "—");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(info.OldRole ?? "—");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(isTower ? "tower" : "field");

            if(isTower)
                ImGui.PopStyleColor();
        }

        ImGui.EndTable();
    }

    private void DrawDebugPreviewSection()
    {
        var previewIndex = DebugPatternPreviewToComboIndex(C.DebugPatternPreview);
        if(ImGui.Combo("Pattern", ref previewIndex, DebugPatternPreviewLabels, DebugPatternPreviewLabels.Length))
        {
            C.DebugPatternPreview = ComboIndexToDebugPatternPreview(previewIndex);
            C.DebugPatternPreviewRole = DebugPatternPreviewRoleNone;
            UpdateFieldMarkers();
        }

        if(IsPatternPreviewActive())
            DrawPatternPreviewRoleFilter();
    }

    private void DrawPatternPreviewRoleFilter()
    {
        C.EnsureDefaults();
        var pattern = Patterns[C.DebugPatternPreview];
        var roleLabels = BuildPatternRoleComboLabels(pattern);
        var roleComboIndex = C.DebugPatternPreviewRole + 1;
        if(ImGui.Combo("Role", ref roleComboIndex, roleLabels, roleLabels.Length))
        {
            C.DebugPatternPreviewRole = roleComboIndex <= 0
                ? DebugPatternPreviewRoleNone
                : roleComboIndex - 1;
            UpdateFieldMarkers();
        }
    }

    private static string[] BuildPatternRoleComboLabels(PatternDefinition pattern)
    {
        var labels = new string[pattern.Assignments.Length + 1];
        labels[0] = "None";
        for(var i = 0; i < pattern.Assignments.Length; i++)
            labels[i + 1] = pattern.Assignments[i].Label;
        return labels;
    }

    private static int ClampPatternPreviewRole(int patternPreview, int roleIndex)
    {
        if(!IsPatternPreviewActive(patternPreview))
            return DebugPatternPreviewRoleNone;
        if(roleIndex < 0)
            return DebugPatternPreviewRoleNone;
        var assignmentCount = Patterns[patternPreview].Assignments.Length;
        return roleIndex >= assignmentCount ? DebugPatternPreviewRoleNone : roleIndex;
    }

    private static bool IsPatternPreviewActive(int patternPreview)
        => patternPreview >= 0 && patternPreview < PatternCount;

    private bool IsPatternPreviewActive()
        => IsPatternPreviewActive(C.DebugPatternPreview);

    private static int DebugPatternPreviewToComboIndex(int preview)
        => preview < 0 ? 0 : preview + 1;

    private static int ComboIndexToDebugPatternPreview(int comboIndex)
        => comboIndex <= 0 ? DebugPatternPreviewNone : comboIndex - 1;

    private void OnPatternRulesChanged()
        => UpdateFieldMarkers();

    private static string FormatHalf(MechanicHalf half)
        => half switch
        {
            MechanicHalf.First => "first",
            MechanicHalf.Second => "second",
            _ => "—",
        };

    private static string FormatDebuffKindDebug(DebuffKind kind)
        => kind switch
        {
            DebuffKind.Stack => "Stack (5084)",
            DebuffKind.Spread => "Spread (5085)",
            DebuffKind.Cone => "Cone (5086)",
            _ => "—",
        };

    #endregion

    #region Tower Tracking

    private void AddTowerSpawnPosition(uint position)
    {
        AddUniquePairPosition(_pendingTowerSpawnPositions, position);
        if(_pendingTowerSpawnPositions.Count != 2)
            return;

        if(!TryGetTowerPairReference(_pendingTowerSpawnPositions[0], _pendingTowerSpawnPositions[1],
                out var reference, out var paired))
        {
            _pendingTowerSpawnPositions.Clear();
            return;
        }

        _pendingTowerSpawnPositions.Clear();
        _pendingTowerClearPositions.Clear();
        SetActiveTowers(CreateTowerHits(reference, paired));
    }

    private void AddTowerClearPosition(uint position)
    {
        AddUniquePairPosition(_pendingTowerClearPositions, position);
        if(_pendingTowerClearPositions.Count != 2)
            return;

        if(!TryGetTowerPairReference(_pendingTowerClearPositions[0], _pendingTowerClearPositions[1],
                out _, out _))
        {
            _pendingTowerClearPositions.Clear();
            return;
        }

        _pendingTowerClearPositions.Clear();
    }

    private static void AddUniquePairPosition(List<uint> list, uint position)
    {
        if(list.Count >= 2)
            list.Clear();

        if(!list.Contains(position))
            list.Add(position);
    }

    private void SetActiveTowers(List<TowerHit> hits)
    {
        if(hits.Count != 2)
            return;

        _activeTowerPositions[0] = hits[0].SlotPosition;
        _activeTowerPositions[1] = hits[1].SlotPosition;
        _activeTowerEntityIds[0] = hits[0].EntityId;
        _activeTowerEntityIds[1] = hits[1].EntityId;
        _hasActiveTowers = true;

        _step++;
        _tower1Side = ResolveTower1Side(hits[0].SlotPosition, hits[1].SlotPosition);
    }

    private static List<TowerHit> CreateTowerHits(uint reference, uint paired)
        => [CreateTowerHit(reference), CreateTowerHit(paired)];

    private static TowerHit CreateTowerHit(uint mapIndex)
    {
        var slot = TowerSlots[mapIndex - 1];
        return new TowerHit(slot.Label, slot.Position, mapIndex);
    }

    private static bool IsTowerMapPosition(uint position)
        => position is >= MapEffectTowerIndexMin and <= MapEffectTowerIndexMax;

    private static bool IsTowerSpawnMapEffect(ushort data1, ushort data2)
        => data1 == MapEffectTowerSpawnData1 && data2 == MapEffectTowerSpawnData2;

    private static bool IsTowerClearMapEffect(ushort data1, ushort data2)
        => data1 == MapEffectTowerClearData1 && data2 == MapEffectTowerClearData2;

    private static bool TryGetTowerPairReference(uint first, uint second, out uint reference, out uint paired)
    {
        if(AddMapSteps(first, TowerPairMapStepOffset) == second)
        {
            reference = first;
            paired = second;
            return true;
        }

        if(AddMapSteps(second, TowerPairMapStepOffset) == first)
        {
            reference = second;
            paired = first;
            return true;
        }

        reference = 0;
        paired = 0;
        return false;
    }

    private static uint AddMapSteps(uint position, int steps)
        => (uint)(((int)position - 1 + steps + 8) % 8 + 1);

    private static string FormatMapPositionList(IReadOnlyList<uint> positions)
        => positions.Count == 0 ? "none" : string.Join(", ", positions);

    private void UpdateInterludeNavPhase()
    {
        switch(_interludeNavPhase)
        {
            case InterludeNavPhase.CastingPast:
                if(IsAnyBossCasting(PastsEndCast))
                    _interludeBossCastSeen = true;
                else if(_interludeBossCastSeen)
                    _interludeNavPhase = InterludeNavPhase.PastGap;
                break;
            case InterludeNavPhase.CastingFuture:
                if(IsAnyBossCasting(FuturesEndCast))
                    _interludeBossCastSeen = true;
                else if(_interludeBossCastSeen)
                    _interludeNavPhase = InterludeNavPhase.FutureOpposite;
                break;
            case InterludeNavPhase.PastGap:
                if(IsAnyBossCasting(PastsEndCast))
                {
                    _interludeNavPhase = InterludeNavPhase.CastingPast;
                    _interludeBossCastSeen = true;
                }
                break;
            case InterludeNavPhase.FutureOpposite:
                if(IsAnyBossCasting(FuturesEndCast))
                {
                    _interludeNavPhase = InterludeNavPhase.CastingFuture;
                    _interludeBossCastSeen = true;
                }
                break;
        }
    }

    private static bool IsAnyBossCasting(uint castId)
        => Svc.Objects.OfType<IBattleNpc>().Any(x => x.IsTargetable && x.IsCasting && x.CastActionId == castId);

    private bool IsInterludeEndCastActive()
        => IsAnyBossCasting(PastsEndCast) || IsAnyBossCasting(FuturesEndCast);

    private bool TryGetInterludeNavPosition(out Vector3 position, out string label)
    {
        position = default;
        label = "";
        if(_interludeNavPhase is not (InterludeNavPhase.PastGap or InterludeNavPhase.FutureOpposite))
            return false;
        if(IsInterludeEndCastActive())
            return false;
        if(!_hasActiveTowers)
            return false;

        position = ResolveInterludeNavPosition();
        label = _interludeNavPhase == InterludeNavPhase.PastGap ? "between towers" : "opposite side";
        return true;
    }

    // Past: toward active tower pair at 4m from center; Future: opposite side through center at 4m.
    private Vector3 ResolveInterludeNavPosition()
    {
        var pairMidpoint = (_activeTowerPositions[0] + _activeTowerPositions[1]) * 0.5f;
        var towardTowers = pairMidpoint - ArenaCenter;
        towardTowers.Y = 0;
        if(towardTowers.LengthSquared() < 0.0001f)
            towardTowers = new Vector3(0f, 0f, -1f);
        towardTowers = Vector3.Normalize(towardTowers);

        return _interludeNavPhase == InterludeNavPhase.PastGap
            ? ArenaCenter + towardTowers * InterludeNavDistanceFromCenter
            : ArenaCenter - towardTowers * InterludeNavDistanceFromCenter;
    }

    private void UpdateActiveTowerMarkers()
    {
        UpdateActiveTowerMarker(ElActiveTower0, 0);
        UpdateActiveTowerMarker(ElActiveTower1, 1);
    }

    private void UpdateActiveTowerMarker(string elementName, int index)
    {
        if(!Controller.TryGetElementByName(elementName, out var element))
            return;

        if(!_hasActiveTowers)
        {
            element.Enabled = false;
            return;
        }

        element.SetRefPosition(_activeTowerPositions[index]);
        element.color = EColor.CyanBright.ToUint();
        element.overlayText = "";
        element.Enabled = true;
    }

    private void ResetState()
    {
        _hasActiveTowers = false;
        _step = 0;
        _tower1Side = Tower1Side.Unknown;
        _pendingTowerSpawnPositions.Clear();
        _pendingTowerClearPositions.Clear();
        _activeTowerPositions[0] = Vector3.Zero;
        _activeTowerPositions[1] = Vector3.Zero;
        _activeTowerEntityIds[0] = 0;
        _activeTowerEntityIds[1] = 0;
        _infos.Clear();
        _initialGroupResolved = false;
        _initialRolesAssigned = false;
        _interludeNavPhase = InterludeNavPhase.None;
        _interludeBossCastSeen = false;

        if(Controller.TryGetElementByName(ElActiveTower0, out var tower0))
            tower0.Enabled = false;
        if(Controller.TryGetElementByName(ElActiveTower1, out var tower1))
            tower1.Enabled = false;
        DisableAllRolePreviewMarkers();
        DisableMyRoleMarker();
    }

    private void DisableAllMarkers()
    {
        if(Controller.TryGetElementByName(ElActiveTower0, out var tower0))
            tower0.Enabled = false;
        if(Controller.TryGetElementByName(ElActiveTower1, out var tower1))
            tower1.Enabled = false;
        DisableAllRolePreviewMarkers();
        DisableMyRoleMarker();
    }

    #endregion

    #region Markers

    private void UpdateFieldMarkers()
    {
        if(!IsPhaseActive())
        {
            DisableAllRolePreviewMarkers();
            DisableMyRoleMarker();
            return;
        }

        if(IsPatternPreviewActive())
        {
            UpdatePatternPreviewMarkers();
            DisableMyRoleMarker();
            return;
        }

        DisableAllRolePreviewMarkers();

        if(TryGetInterludeNavPosition(out var interludePosition, out var interludeLabel))
        {
            EnableRoleMarker(ElMyRole, interludePosition, interludeLabel, tether: true);
            return;
        }

        DisableMyRoleMarker();

        if(!_hasActiveTowers || _step is < ActiveStepMin or > ActiveStepMax)
            return;

        if(!TryUpdateLiveRoles(out var patternId, out var roleLabel))
            return;

        var assignmentIndex = FindAssignmentIndex(patternId, roleLabel);
        if(assignmentIndex < 0)
            return;

        if(ResolvePositionRule(GetConfiguredRule(patternId, assignmentIndex)) is not { } position)
            return;

        EnableRoleMarker(ElMyRole, position, roleLabel, tether: true);
    }

    private void EnableRoleMarker(string elementName, Vector3 position, string label, bool tether)
    {
        if(!Controller.TryGetElementByName(elementName, out var element))
            return;

        element.SetRefPosition(position);
        element.color = Controller.AttentionColor;
        element.overlayText = label;
        element.tether = tether;
        element.Enabled = true;
    }

    private static string GetRolePreviewElementName(int index)
        => $"RolePreview_{index}";

    private void DisableAllRolePreviewMarkers()
    {
        for(var i = 0; i < MaxPatternAssignments; i++)
        {
            if(Controller.TryGetElementByName(GetRolePreviewElementName(i), out var element))
            {
                element.Enabled = false;
                element.tether = false;
            }
        }
    }

    private void DisableMyRoleMarker()
    {
        if(Controller.TryGetElementByName(ElMyRole, out var element))
        {
            element.Enabled = false;
            element.tether = false;
        }
    }

    private void UpdatePatternPreviewMarkers()
    {
        DisableAllRolePreviewMarkers();

        if(!_hasActiveTowers || !IsPatternPreviewActive())
            return;

        C.EnsureDefaults();
        var pattern = Patterns[C.DebugPatternPreview];
        var showAllRoles = C.DebugPatternPreviewRole < 0;
        var showRoleTether = !showAllRoles;
        for(var i = 0; i < pattern.Assignments.Length; i++)
        {
            if(i >= MaxPatternAssignments)
                break;
            if(!showAllRoles && i != C.DebugPatternPreviewRole)
                continue;

            if(ResolvePositionRule(GetConfiguredRule(C.DebugPatternPreview, i)) is not { } position)
                continue;

            EnableRoleMarker(GetRolePreviewElementName(i), position, pattern.Assignments[i].Label, showRoleTether);
        }
    }

    #endregion

    #region Position Resolution

    private Vector3? ResolvePositionRule(PositionRule rule)
    {
        if(!_hasActiveTowers)
            return null;

        var basis = rule.Basis;
        var angleDeg = rule.AngleDeg;
        var range = rule.Range;

        if(!TryResolveTowerRoleIndices(out var rightIndex, out var leftIndex))
            return null;

        return basis switch
        {
            PositionBasis.RightTower => OffsetFromTowerAtCompassAngle(
                _activeTowerPositions[rightIndex],
                NormalizeAngle360(GetAngleTowardCenterFromTower(_activeTowerPositions[rightIndex]) + angleDeg),
                range),
            PositionBasis.LeftTower => OffsetFromTowerAtCompassAngle(
                _activeTowerPositions[leftIndex],
                NormalizeAngle360(GetAngleTowardCenterFromTower(_activeTowerPositions[leftIndex]) + angleDeg),
                range),
            PositionBasis.Center => OffsetFromTowerAtCompassAngle(
                ArenaCenter,
                NormalizeAngle360(
                    GetAngleFromTrueNorth(
                        (_activeTowerPositions[rightIndex] + _activeTowerPositions[leftIndex]) * 0.5f) + angleDeg),
                range),
            _ => null,
        };
    }

    private bool TryResolveTowerRoleIndices(out int rightIndex, out int leftIndex)
    {
        if(TryResolveTowerRoles(out rightIndex, out leftIndex))
            return true;

        if(!IsPatternPreviewActive())
            return false;

        rightIndex = 0;
        leftIndex = 1;
        return true;
    }

    private static float Distance2d(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    private static string FormatVector3(Vector3 v)
        => $"({v.X:F2}, {v.Y:F2}, {v.Z:F2})";

    private static float GetAngleFromTrueNorth(Vector3 position)
    {
        var northAngle = MathHelper.GetRelativeAngle(ArenaCenter, TrueNorth);
        var posAngle = MathHelper.GetRelativeAngle(ArenaCenter, position);
        return NormalizeAngle360(posAngle - northAngle);
    }

    private static Tower1Side ResolveTower1Side(Vector3 tower1Pos, Vector3 tower2Pos)
    {
        var angle1 = GetAngleFromTrueNorth(tower1Pos);
        var angle2 = GetAngleFromTrueNorth(tower2Pos);
        var diff = NormalizeAngle360(angle2 - angle1);

        if(IsNearAngle(diff, 90f))
            return Tower1Side.Right;
        if(IsNearAngle(diff, 270f))
            return Tower1Side.Left;
        return Tower1Side.Unknown;
    }

    private static float NormalizeAngle360(float angle)
        => (angle % 360f + 360f) % 360f;

    private static bool IsNearAngle(float angle, float target)
    {
        var diff = MathF.Abs(NormalizeAngle360(angle - target));
        return diff <= AngleTolerance || MathF.Abs(diff - 360f) <= AngleTolerance;
    }

    private bool TryResolveTowerRoles(out int rightIndex, out int leftIndex)
    {
        switch(_tower1Side)
        {
            case Tower1Side.Right:
                rightIndex = 0;
                leftIndex = 1;
                return true;
            case Tower1Side.Left:
                rightIndex = 1;
                leftIndex = 0;
                return true;
            default:
                rightIndex = 0;
                leftIndex = 0;
                return false;
        }
    }

    private static float GetAngleTowardCenterFromTower(Vector3 towerPos)
        => NormalizeAngle360(GetAngleFromTrueNorth(towerPos) + 180f);

    private static Vector3 OffsetFromTowerAtCompassAngle(Vector3 towerPos, float compassDeg, float range)
    {
        var rad = compassDeg * (MathF.PI / 180f);
        return new Vector3(
            towerPos.X + MathF.Sin(rad) * range,
            towerPos.Y,
            towerPos.Z - MathF.Cos(rad) * range);
    }

    #endregion

    #region Pattern Rules

    private static PatternRuleSettings[][] CreateDefaultPatternRules()
    {
        var rules = new PatternRuleSettings[PatternCount][];
        foreach(var pattern in Patterns)
        {
            rules[pattern.Id] = new PatternRuleSettings[pattern.Assignments.Length];
            for(var i = 0; i < pattern.Assignments.Length; i++)
            {
                var rule = pattern.Assignments[i].Rule;
                rules[pattern.Id][i] = new PatternRuleSettings
                {
                    Basis = (int)rule.Basis,
                    AngleDeg = rule.AngleDeg,
                    Range = rule.Range,
                };
            }
        }

        return rules;
    }

    private static PatternRuleSettings[][] EnsurePatternRules(PatternRuleSettings[][]? rules)
    {
        var defaults = CreateDefaultPatternRules();
        if(rules == null || rules.Length != PatternCount)
            return defaults;

        for(var patternId = 0; patternId < PatternCount; patternId++)
        {
            var expectedCount = Patterns[patternId].Assignments.Length;
            if(rules[patternId] == null || rules[patternId].Length != expectedCount)
                rules[patternId] = defaults[patternId];
            else
            {
                for(var i = 0; i < expectedCount; i++)
                {
                    if(rules[patternId][i] == null)
                        rules[patternId][i] = defaults[patternId][i];
                    else
                        rules[patternId][i].Basis = ClampBasisIndex(rules[patternId][i].Basis);
                }
            }
        }

        return rules;
    }

    private PositionRule GetConfiguredRule(int patternId, int assignmentIndex)
    {
        C.EnsureDefaults();
        var settings = C.PatternRules[patternId][assignmentIndex];
        return new PositionRule((PositionBasis)settings.Basis, settings.AngleDeg, settings.Range);
    }

    private static int ClampBasisIndex(int basis)
        => basis switch
        {
            < 0 => 0,
            > 2 => 2,
            _ => basis,
        };

    private static PatternAssignment Rule(string label, PositionBasis basis, float angleDeg, float range)
        => new(label, new PositionRule(basis, angleDeg, range));

    private static int FindAssignmentIndex(int patternId, string roleLabel)
        => Array.FindIndex(Patterns[patternId].Assignments, a => a.Label == roleLabel);

    #endregion

    #region Role Resolution

    private static DebuffKind GetDebuffKind(IPlayerCharacter player)
    {
        if(player.StatusList.Any(s => s.StatusId == StatusStack))
            return DebuffKind.Stack;
        if(player.StatusList.Any(s => s.StatusId == StatusSpread))
            return DebuffKind.Spread;
        if(player.StatusList.Any(s => s.StatusId == StatusCone))
            return DebuffKind.Cone;
        return DebuffKind.None;
    }

    private List<IPlayerCharacter> GetOrderedPartyPlayers()
    {
        C.EnsureDefaults();
        var priority = C.PriorityData.GetPlayers(_ => true);
        if(priority == null || priority.Count != PartyPlayerCount)
            return [];

        var names = priority.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        var members = Controller.GetPartyMembers()
            .Where(p => names.Contains(p.Name.ToString()))
            .ToList();
        if(members.Count != PartyPlayerCount)
            return [];

        return OrderByPriority(members).ToList();
    }

    private int GetPriorityIndex(IPlayerCharacter player)
    {
        var priorityList = C.PriorityData.GetPlayers(_ => true);
        if(priorityList == null)
            return int.MaxValue;

        var name = player.Name.ToString();
        for(var i = 0; i < priorityList.Count; i++)
        {
            if(priorityList[i].Name == name)
                return i;
        }

        return int.MaxValue;
    }

    private IEnumerable<PlayerInfo> OrderInfosByPriority(IEnumerable<PlayerInfo> infos)
        => infos.OrderBy(i => GetPriorityIndex(i.Player)).ThenBy(i => i.Player.EntityId);

    private IEnumerable<IPlayerCharacter> OrderByPriority(IEnumerable<IPlayerCharacter> players)
    {
        return players.OrderBy(GetPriorityIndex).ThenBy(p => p.EntityId);
    }

    private bool TryEnsureInfos()
    {
        var party = GetOrderedPartyPlayers();
        if(party.Count != PartyPlayerCount)
            return false;

        if(_infos.Count != PartyPlayerCount)
        {
            _infos.Clear();
            foreach(var player in party)
            {
                _infos.Add(new PlayerInfo
                {
                    Player = player,
                    Half = MechanicHalf.None,
                    Debuff = GetDebuffKind(player),
                });
            }
        }

        return true;
    }

    private void UpdateDebuffs()
    {
        foreach(var info in _infos)
            info.Debuff = GetDebuffKind(info.Player);
    }

    // Returns the mechanic half whose debuffs are used for pattern detection at the current step.
    private bool TryGetActiveMechanicHalf(out MechanicHalf half)
    {
        if(FirstHalfTowerSteps.Contains(_step))
        {
            half = MechanicHalf.First;
            return true;
        }

        if(SecondHalfTowerSteps.Contains(_step))
        {
            half = MechanicHalf.Second;
            return true;
        }

        half = MechanicHalf.None;
        return false;
    }

    // Counts debuffs among players in the active half group for the current step.
    private void CountDebuffKindsFromActiveGroup(out int stack, out int spread, out int cone)
    {
        stack = 0;
        spread = 0;
        cone = 0;
        if(!TryGetActiveMechanicHalf(out var activeHalf))
            return;

        foreach(var info in _infos)
        {
            if(info.Half != activeHalf)
                continue;

            switch(info.Debuff)
            {
                case DebuffKind.Stack: stack++; break;
                case DebuffKind.Spread: spread++; break;
                case DebuffKind.Cone: cone++; break;
            }
        }
    }

    private void CountDebuffKindsFromTowerInfos(out int stack, out int spread, out int cone)
    {
        stack = 0;
        spread = 0;
        cone = 0;
        foreach(var info in _infos)
        {
            if(!IsTower(info))
                continue;

            switch(info.Debuff)
            {
                case DebuffKind.Stack: stack++; break;
                case DebuffKind.Spread: spread++; break;
                case DebuffKind.Cone: cone++; break;
            }
        }
    }

    private bool TryDetectPartyPattern(out int patternId)
    {
        patternId = -1;
        if(!TryEnsureInfos())
            return false;

        if(!TryGetActiveMechanicHalf(out _))
            return false;

        CountDebuffKindsFromActiveGroup(out var stack, out var spread, out var cone);

        for(var i = 0; i < PatternCount; i++)
        {
            var pattern = Patterns[i];
            if(pattern.StackCount != stack || pattern.SpreadCount != spread || pattern.ConeCount != cone)
                continue;

            if(patternId >= 0)
            {
                patternId = -1;
                return false;
            }

            patternId = i;
        }

        return patternId >= 0;
    }

    private bool IsTower(PlayerInfo info)
    {
        if(FirstHalfTowerSteps.Contains(_step))
            return info.Half == MechanicHalf.First;
        if(SecondHalfTowerSteps.Contains(_step))
            return info.Half == MechanicHalf.Second;
        return false;
    }

    private bool TryResolveInitialGroup()
    {
        if(!TryEnsureInfos())
            return false;

        UpdateDebuffs();

        var stackPairCount = 0;
        var nonStackPlayerCount = 0;

        for(var pairIndex = 0; pairIndex < Step1PartyPairCount; pairIndex++)
        {
            if(!TryGetStep1Pair(pairIndex, out var playerA, out var playerB))
                return false;

            if(!TryClassifyStep1Pair(playerA, playerB, out _, out _, out var isStackPair))
                return false;

            if(isStackPair)
            {
                playerA.Half = MechanicHalf.First;
                playerB.Half = MechanicHalf.First;
                stackPairCount++;
            }
            else
            {
                playerA.Half = MechanicHalf.Second;
                playerB.Half = MechanicHalf.Second;
                nonStackPlayerCount += 2;
            }
        }

        if(stackPairCount != StackPairCount || nonStackPlayerCount != NonStackPairCount * 2)
            return false;

        return _infos.All(i => i.Half != MechanicHalf.None);
    }

    // Classifies a step1 pair as stack+partner or non-stack pair.
    private static bool TryClassifyStep1Pair(PlayerInfo playerA, PlayerInfo playerB, out PlayerInfo stackPlayer,
        out PlayerInfo partnerPlayer, out bool isStackPair)
    {
        stackPlayer = null!;
        partnerPlayer = null!;
        isStackPair = false;

        var stackA = playerA.Debuff == DebuffKind.Stack;
        var stackB = playerB.Debuff == DebuffKind.Stack;

        if(stackA && stackB)
            return false;

        if(stackA)
        {
            isStackPair = true;
            stackPlayer = playerA;
            partnerPlayer = playerB;
            return partnerPlayer.Debuff is DebuffKind.Spread or DebuffKind.Cone;
        }

        if(stackB)
        {
            isStackPair = true;
            stackPlayer = playerB;
            partnerPlayer = playerA;
            return partnerPlayer.Debuff is DebuffKind.Spread or DebuffKind.Cone;
        }

        isStackPair = false;
        return playerA.Debuff is DebuffKind.Spread or DebuffKind.Cone
            && playerB.Debuff is DebuffKind.Spread or DebuffKind.Cone;
    }

    private void ResolvePattern(int patternId)
    {
        if(!_initialRolesAssigned && _step == ActiveStepMin)
        {
            AssignInitialRoles();
            _initialRolesAssigned = true;
        }
        else
            AssignRolesForCurrentStep(patternId);

        CacheAllPlayerOldRoles();
    }

    // plan8 L3-15: FirstHalf 211 stack-pair roles, SecondHalf NotTower roles (Half-based, not IsTower).
    private void AssignInitialRoles()
    {
        foreach(var info in _infos)
        {
            info.RoleLabel = info.Half == MechanicHalf.First
                ? ResolveFirstHalfInitial211TowerRole(info)
                : ResolveNotTower211Role(info);
        }
    }

    // plan8 L19-37: pattern-based field/tower roles with OldRole transform on tower swaps.
    private void AssignRolesForCurrentStep(int patternId)
    {
        foreach(var info in _infos.Where(i => !IsTower(i)))
            info.RoleLabel = ResolveNotTowerRole(patternId, info);

        foreach(var info in _infos.Where(i => IsTower(i)))
            info.RoleLabel = null;

        foreach(var info in _infos.Where(i => IsTower(i)))
        {
            if(OldRoleMatchesTargetPattern(info.OldRole, patternId))
                info.RoleLabel = info.OldRole;
        }

        foreach(var group in OldRoleSwapGroups)
            TryAssignRolesFromOldRoleGroup(group, patternId);

        foreach(var info in _infos.Where(i => IsTower(i) && i.RoleLabel == null))
            info.RoleLabel = BootstrapTowerRole(patternId, info);
    }

    private static bool OldRoleMatchesTargetPattern(string? oldRole, int patternId)
    {
        if(string.IsNullOrEmpty(oldRole))
            return false;

        return patternId switch
        {
            0 => oldRole.StartsWith("211_", StringComparison.Ordinal)
                && !oldRole.StartsWith("211_NotTower", StringComparison.Ordinal),
            1 => oldRole.StartsWith("022_", StringComparison.Ordinal)
                && !oldRole.StartsWith("022_Demise", StringComparison.Ordinal),
            _ => false,
        };
    }

    private string? BootstrapTowerRole(int patternId, PlayerInfo info)
        => patternId == 0
            ? ResolveFirstHalfInitial211TowerRole(info)
            : ResolveBootstrap022TowerRole(info);

    private void CacheAllPlayerOldRoles()
    {
        foreach(var info in _infos)
            info.OldRole = info.RoleLabel;
    }

    private void TryAssignRolesFromOldRoleGroup(OldRoleSwapGroup group, int patternId)
    {
        var playerA = _infos.FirstOrDefault(i => i.OldRole == group.RoleA);
        var playerB = _infos.FirstOrDefault(i => i.OldRole == group.RoleB);
        if(playerA == null || playerB == null)
            return;

        if(!IsTower(playerA) || !IsTower(playerB))
            return;

        if(playerA.RoleLabel != null || playerB.RoleLabel != null)
            return;

        if(patternId == 0)
        {
            if(!group.RoleA.StartsWith("022_", StringComparison.Ordinal))
                return;

            AssignRolesFrom022OldRoleGroup(group, playerA, playerB);
            return;
        }

        if(patternId == 1)
        {
            if(!group.RoleA.StartsWith("211_", StringComparison.Ordinal))
                return;

            AssignRolesFrom211OldRoleGroup(group, playerA, playerB);
        }
    }

    private void AssignRolesFrom211OldRoleGroup(OldRoleSwapGroup group, PlayerInfo playerA, PlayerInfo playerB)
    {
        var suffix = group.PrioritySuffix;

        if(playerA.Debuff != playerB.Debuff)
        {
            Assign211To022ByDebuff(playerA, suffix);
            Assign211To022ByDebuff(playerB, suffix);
            return;
        }

        CountDebuffKindsFromTowerInfos(out var stack, out var spread, out var cone);
        if(stack == 0 && spread == 2 && cone == 0)
        {
            AssignPairByPriorityRank(playerA, playerB, "022_SpreadPriority1", "022_SpreadPriority2");
            return;
        }

        if(stack == 0 && spread == 0 && cone == 2)
        {
            AssignPairByPriorityRank(playerA, playerB, "022_ConePriority1", "022_ConePriority2");
            return;
        }

        switch(playerA.Debuff)
        {
            case DebuffKind.Spread:
                AssignPairByPriorityRank(playerA, playerB, "022_SpreadPriority1", "022_SpreadPriority2");
                break;
            case DebuffKind.Cone:
                AssignPairByPriorityRank(playerA, playerB, "022_ConePriority1", "022_ConePriority2");
                break;
        }
    }

    private void Assign211To022ByDebuff(PlayerInfo info, int suffix)
    {
        info.RoleLabel = info.Debuff switch
        {
            DebuffKind.Spread => $"022_SpreadPriority{suffix}",
            DebuffKind.Cone => $"022_ConePriority{suffix}",
            _ => info.RoleLabel,
        };
    }

    private void AssignRolesFrom022OldRoleGroup(OldRoleSwapGroup group, PlayerInfo playerA, PlayerInfo playerB)
    {
        var suffix = group.PrioritySuffix;

        if(playerA.Debuff != playerB.Debuff)
        {
            Assign022To211ByDebuff(playerA, suffix);
            Assign022To211ByDebuff(playerB, suffix);
            return;
        }

        CountDebuffKindsFromTowerInfos(out var stack, out var spread, out var cone);
        if(stack == 2 && spread == 0 && cone == 0)
        {
            AssignPairByPriorityRank(playerA, playerB, "211_StackPriority1", "211_StackPriority2");
            return;
        }

        if(playerA.Debuff == DebuffKind.Stack)
            AssignPairByPriorityRank(playerA, playerB, "211_StackPriority1", "211_StackPriority2");
    }

    private void Assign022To211ByDebuff(PlayerInfo info, int suffix)
    {
        info.RoleLabel = info.Debuff switch
        {
            DebuffKind.Stack => $"211_StackPriority{suffix}",
            DebuffKind.Spread => $"211_OtherPriority{suffix}",
            DebuffKind.Cone => $"211_OtherPriority{suffix}",
            _ => info.RoleLabel,
        };
    }

    private void AssignPairByPriorityRank(PlayerInfo playerA, PlayerInfo playerB, string labelPriority1,
        string labelPriority2)
    {
        if(GetPriorityRank([playerA, playerB], playerA) == 0)
        {
            playerA.RoleLabel = labelPriority1;
            playerB.RoleLabel = labelPriority2;
        }
        else
        {
            playerA.RoleLabel = labelPriority2;
            playerB.RoleLabel = labelPriority1;
        }
    }

    private string? ResolveFirstHalfInitial211TowerRole(PlayerInfo info)
    {
        for(var pairIndex = 0; pairIndex < Step1PartyPairCount; pairIndex++)
        {
            if(!TryGetStep1Pair(pairIndex, out var playerA, out var playerB))
                return null;

            if(!TryClassifyStep1Pair(playerA, playerB, out var stackPlayer, out var partnerPlayer, out var isStackPair)
                || !isStackPair)
                continue;

            if(stackPlayer.Half != MechanicHalf.First || partnerPlayer.Half != MechanicHalf.First)
                continue;

            if(stackPlayer.Player.EntityId != info.Player.EntityId
                && partnerPlayer.Player.EntityId != info.Player.EntityId)
                continue;

            return info.Player.EntityId == stackPlayer.Player.EntityId
                ? partnerPlayer.Debuff switch
                {
                    DebuffKind.Cone => "211_StackPriority1",
                    DebuffKind.Spread => "211_StackPriority2",
                    _ => null,
                }
                : partnerPlayer.Debuff switch
                {
                    DebuffKind.Cone => "211_OtherPriority1",
                    DebuffKind.Spread => "211_OtherPriority2",
                    _ => null,
                };
        }

        return null;
    }

    private string? ResolveBootstrap022TowerRole(PlayerInfo info)
    {
        if(info.Debuff == DebuffKind.Spread)
        {
            return GetPriorityRank(
                _infos.Where(i => IsTower(i) && i.Debuff == DebuffKind.Spread), info) switch
            {
                0 => "022_SpreadPriority1",
                1 => "022_SpreadPriority2",
                _ => null,
            };
        }

        if(info.Debuff == DebuffKind.Cone)
        {
            return GetPriorityRank(
                _infos.Where(i => IsTower(i) && i.Debuff == DebuffKind.Cone), info) switch
            {
                0 => "022_ConePriority1",
                1 => "022_ConePriority2",
                _ => null,
            };
        }

        return null;
    }

    private string? ResolveNotTowerRole(int patternId, PlayerInfo info)
        => patternId == 0 ? ResolveNotTower211Role(info) : ResolveNotTower022Role(info);

    private string? ResolveNotTower211Role(PlayerInfo info)
        => GetPriorityRank(_infos.Where(i => !IsTower(i)), info) switch
        {
            0 => "211_NotTowerPriority1",
            1 => "211_NotTowerPriority2",
            2 => "211_NotTowerPriority3",
            3 => "211_NotTowerPriority4",
            _ => null,
        };

    private string? ResolveNotTower022Role(PlayerInfo info)
        => GetPriorityRank(_infos.Where(i => !IsTower(i)), info) switch
        {
            0 => "022_DemisePriority1",
            1 => "022_DemisePriority2",
            2 => "022_DemisePriority3",
            3 => "022_DemisePriority4",
            _ => null,
        };

    private int GetPriorityRank(IEnumerable<PlayerInfo> subset, PlayerInfo target)
    {
        var ordered = OrderInfosByPriority(subset).ToList();
        return ordered.FindIndex(i => i.Player.EntityId == target.Player.EntityId);
    }

    private PlayerInfo? GetBasePlayerInfo()
    {
        if(BasePlayer == null)
            return null;

        return _infos.FirstOrDefault(i => i.Player.EntityId == BasePlayer.EntityId);
    }

    private bool TryUpdateLiveRoles(out int patternId, out string roleLabel)
    {
        patternId = -1;
        roleLabel = "";

        if(!TryEnsureInfos())
            return false;

        UpdateDebuffs();

        if(!_initialGroupResolved)
        {
            if(!TryResolveInitialGroup())
                return false;

            _initialGroupResolved = true;
        }

        if(!TryDetectPartyPattern(out patternId))
            return false;

        ResolvePattern(patternId);

        var baseInfo = GetBasePlayerInfo();
        if(baseInfo?.RoleLabel == null)
            return false;

        roleLabel = baseInfo.RoleLabel;
        return true;
    }

    #endregion
}
