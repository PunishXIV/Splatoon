using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using Splatoon.Utility;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

internal class P2_Misisng_KT_Alt : SplatoonScript
{
    #region Metadata

    public override Metadata Metadata { get; } = new(3, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryDmad];

    #endregion

    #region Constant

    private const uint TerritoryDmad = 1363;
    private const int SceneP2 = 7;

    private const uint StatusStack = 5084;
    private const uint StatusSpread = 5085;
    private const uint StatusCone = 5086;

    private const uint FuturesEndCast = 47826;
    private const uint PastsEndCast = 47827;
    private const uint AllThingsEndingCast1 = 47836;
    private const uint AllThingsEndingCast2 = 47837;

    private const uint MapEffectTowerIndexMin = 1;
    private const uint MapEffectTowerIndexMax = 8;
    private const ushort MapEffectTowerSpawnData1 = 1;
    private const ushort MapEffectTowerSpawnData2 = 2;
    private const ushort MapEffectTowerClearData1 = 4;
    private const ushort MapEffectTowerClearData2 = 8;
    private const int TowerPairMapStepOffset = 2;

    private const int PartyPlayerCount = 8;
    private const int Step1PartyPairCount = 4;
    private const int StackPairCount = 2;
    private const int NonStackPairCount = 2;
    private const int ActiveStepMin = 1;
    private const int ActiveStepMax = 8;
    private const int PatternCount = 2;
    private const int RoleCountPerPattern = 8;
    private const int MaxPatternAssignments = 8;
    private const int DebugPatternPreviewNone = -1;
    private const int DebugPatternPreviewRoleNone = -1;

    private const int Step4DebuffReminderTrigger = 4;
    private const float Step4DebuffReminderDelayBaseSec = 3f;
    private const float Step4DebuffReminderDelayRandomSpanSec = 1.5f;
    private const string DefaultSpreadEchoText = "Circle";
    private const string DefaultConeEchoText = "Cone";
    private const int MarkerEchoTextMaxLength = 64;
    // UI font scale for priority notice banner (unrelated to RegisterElementFromCode JSON).

    private static readonly string[] MarkerResolveKindLabels = ["None", "Attack", "Stop", "Bind"];

    private const float InterludeNavDistanceFromCenter = 4f;
    private static readonly Vector3 Step8InterludeNavPosition = new(100f, 0f, 95f);
    private const float DefaultRangeInside = 3.25f;
    private const float DefaultRangeOutside = 4.75f;
    private const float TowerOffsetCardinal = 8f;
    private const float TowerOffsetDiagonal = 5.7f;
    private const float AngleTolerance = 22.5f;

    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);
    private static readonly Vector3 TrueNorth = new(100f, 0f, 120f);

    private static readonly (int A, int B)[] Step1PartyPairIndices = [(0, 2), (1, 3), (4, 6), (5, 7)];
    private static readonly HashSet<int> FirstHalfTowerSteps = [1, 2, 3, 8];
    private static readonly HashSet<int> SecondHalfTowerSteps = [4, 5, 6, 7];

    private const string ElActiveTower0 = "ActiveTower0";
    private const string ElActiveTower1 = "ActiveTower1";
    private const string ElMyRole = "MyRole";

    private const string Role211LeftStack = "211_LeftStack";
    private const string Role211Cone = "211_Cone";
    private const string Role211RightStack = "211_RightStack";
    private const string Role211Spread = "211_Spread";
    private const string Role211Tank = "211_Tank";
    private const string Role211Healer = "211_Healer";
    private const string Role211Melee = "211_Melee";
    private const string Role211Range = "211_Range";
    private const string Role022LeftCone = "022_LeftCone";
    private const string Role022LeftSpread = "022_LeftSpread";
    private const string Role022RightCone = "022_RightCone";
    private const string Role022RightSpread = "022_RightSpread";
    private const string Role022Tank = "022_Tank";
    private const string Role022Healer = "022_Healer";
    private const string Role022Melee = "022_Melee";
    private const string Role022Range = "022_Range";

    private static readonly (string Label, Vector3 Position)[] TowerSlots =
    [
        ("N", new(ArenaCenter.X, 0f, ArenaCenter.Z - TowerOffsetCardinal)),
        ("NE", new(ArenaCenter.X + TowerOffsetDiagonal, 0f, ArenaCenter.Z - TowerOffsetDiagonal)),
        ("E", new(ArenaCenter.X + TowerOffsetCardinal, 0f, ArenaCenter.Z)),
        ("SE", new(ArenaCenter.X + TowerOffsetDiagonal, 0f, ArenaCenter.Z + TowerOffsetDiagonal)),
        ("S", new(ArenaCenter.X, 0f, ArenaCenter.Z + TowerOffsetCardinal)),
        ("SW", new(ArenaCenter.X - TowerOffsetDiagonal, 0f, ArenaCenter.Z + TowerOffsetDiagonal)),
        ("W", new(ArenaCenter.X - TowerOffsetCardinal, 0f, ArenaCenter.Z)),
        ("NW", new(ArenaCenter.X - TowerOffsetDiagonal, 0f, ArenaCenter.Z - TowerOffsetDiagonal)),
    ];

    private static readonly PatternDefinition[] Patterns =
    [
        new(0, 2, 1, 1,
        [
            Rule(Role211LeftStack, PositionBasis.LeftTower, 0, 0),
            Rule(Role211Cone, PositionBasis.LeftTower, 315f, DefaultRangeInside),
            Rule(Role211RightStack, PositionBasis.RightTower, 200f, DefaultRangeInside),
            Rule(Role211Spread, PositionBasis.RightTower, 0f, DefaultRangeInside),
            Rule(Role211Tank, PositionBasis.LeftTower, 225f, DefaultRangeOutside),
            Rule(Role211Healer, PositionBasis.LeftTower, 315f, DefaultRangeOutside),
            Rule(Role211Melee, PositionBasis.RightTower, 135f, DefaultRangeOutside),
            Rule(Role211Range, PositionBasis.RightTower, 135f, DefaultRangeOutside),
        ]),
        new(1, 0, 2, 2,
        [
            Rule(Role022LeftCone, PositionBasis.Center, 240f, 5.0f),
            Rule(Role022LeftSpread, PositionBasis.LeftTower, 315f, DefaultRangeInside),
            Rule(Role022RightCone, PositionBasis.Center, 120f, 5.0f),
            Rule(Role022RightSpread, PositionBasis.RightTower, 45f, DefaultRangeInside),
            Rule(Role022Tank, PositionBasis.Center, 315f, 7.0f),
            Rule(Role022Healer, PositionBasis.Center, 270f, 10.0f),
            Rule(Role022Melee, PositionBasis.Center, 45f, 7.0f),
            Rule(Role022Range, PositionBasis.Center, 90f, 10.0f),
        ]),
    ];

    private static readonly string[] BasisComboLabels = ["LeftTower", "RightTower", "Center"];

    private static readonly string[] DebugPatternPreviewLabels =
    [
        "None",
        "211 (2/1/1)",
        "022 (0/2/2)",
    ];

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();

    private sealed class Config : IEzConfig
    {
        public PriorityData PriorityData = CreateDefaultPriorityData();
        public PatternRuleSettings[][] PatternRules = CreateDefaultPatternRules();
        public int DebugPatternPreview = DebugPatternPreviewNone;
        public int DebugPatternPreviewRole = DebugPatternPreviewRoleNone;
        public int Step1RoleMode = (int)Step1AssignmentMode.ChangeWithPair;
        public int TowerPairSwapSideMode = (int)TowerPairSwapSide.BackSide;
        public MarkerResolveKind SpreadMarkerType = MarkerResolveKind.None;
        public MarkerResolveKind ConeMarkerType = MarkerResolveKind.None;
        public bool SpreadUseEcho;
        public bool SpreadUseMarker;
        public bool ConeUseEcho;
        public bool ConeUseMarker;
        public string SpreadEchoText = DefaultSpreadEchoText;
        public string ConeEchoText = DefaultConeEchoText;

        public void EnsureDefaults()
        {
            PriorityData ??= CreateDefaultPriorityData();
            PatternRules = EnsurePatternRules(PatternRules);
            if(DebugPatternPreview >= PatternCount)
                DebugPatternPreview = DebugPatternPreviewNone;
            DebugPatternPreviewRole = ClampPatternPreviewRole(DebugPatternPreview, DebugPatternPreviewRole);
            Step1RoleMode = ClampStep1RoleMode(Step1RoleMode);
            TowerPairSwapSideMode = ClampTowerPairSwapSide(TowerPairSwapSideMode);
            SpreadMarkerType = ClampMarkerResolveKind(SpreadMarkerType);
            ConeMarkerType = ClampMarkerResolveKind(ConeMarkerType);
            SpreadEchoText = NormalizeReminderEchoText(SpreadEchoText, DefaultSpreadEchoText);
            ConeEchoText = NormalizeReminderEchoText(ConeEchoText, DefaultConeEchoText);
        }

        public void ResetToDefaults()
        {
            PriorityData = CreateDefaultPriorityData();
            PatternRules = CreateDefaultPatternRules();
            DebugPatternPreview = DebugPatternPreviewNone;
            DebugPatternPreviewRole = DebugPatternPreviewRoleNone;
            Step1RoleMode = (int)Step1AssignmentMode.ChangeWithPair;
            TowerPairSwapSideMode = (int)TowerPairSwapSide.BackSide;
            SpreadMarkerType = MarkerResolveKind.None;
            ConeMarkerType = MarkerResolveKind.None;
            SpreadUseEcho = false;
            SpreadUseMarker = false;
            ConeUseEcho = false;
            ConeUseMarker = false;
            SpreadEchoText = DefaultSpreadEchoText;
            ConeEchoText = DefaultConeEchoText;
        }

        private static MarkerResolveKind ClampMarkerResolveKind(MarkerResolveKind kind)
            => Enum.IsDefined(kind) ? kind : MarkerResolveKind.None;

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

    #region State

    private readonly Vector3[] _activeTowerPositions = [Vector3.Zero, Vector3.Zero];
    private readonly uint[] _activeTowerEntityIds = [0, 0];
    private readonly List<uint> _pendingTowerSpawnPositions = [];
    private readonly List<uint> _pendingTowerClearPositions = [];
    private readonly List<PlayerInfo> _infos = [];

    private bool _hasActiveTowers;
    private bool _initialGroupResolved;
    private bool _interludeBossCastSeen;
    private int _step;
    private int _lastTowerRoleStep;
    private InterludeNavPhase _interludeNavPhase;
    private string? _lastTowerDebuffSignature;
    private bool _step4DebuffReminderSent;
    private string? _step4DebuffReminderSkipReason;
    private bool _step4DebuffReminderSkipLogged;
    private long _step4DebuffReminderDueAt;

    #endregion

    #region Private Class

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

    private enum Step1AssignmentMode
    {
        ChangeWithPair,
        ChangeStackOnly,
    }

    private enum TowerPairSwapSide
    {
        BackSide,
        FrontSide,
        Priority,
    }

    private enum MarkerResolveKind
    {
        None,
        Attack,
        Stop,
        Bind,
    }

    private sealed class PlayerInfo
    {
        public required IPlayerCharacter Player;
        public MechanicHalf Half;
        public DebuffKind Debuff;
        public string? RoleLabel;
        public Dictionary<int, string> RoleCacheByStep = [];
    }

    private sealed class PatternRuleSettings
    {
        public int Basis;
        public float AngleDeg;
        public float Range;
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        C.EnsureDefaults();

        Controller.RegisterElementFromCode(ElActiveTower0,
            """{"Enabled":false,"radius":4.0,"thicc":6.0,"fillIntensity":0.25,"Filled":false}""",
            overwrite: true);
        Controller.RegisterElementFromCode(ElActiveTower1,
            """{"Enabled":false,"radius":4.0,"thicc":6.0,"fillIntensity":0.25,"Filled":false}""",
            overwrite: true);
        Controller.RegisterElementFromCode(ElMyRole,
            """{"Enabled":false,"radius":0.25,"Donut":0.1,"fillIntensity":0.544,"tether":true}""",
            overwrite: true);

        for(var i = 0; i < MaxPatternAssignments; i++)
        {
            Controller.RegisterElementFromCode(GetRolePreviewElementName(i),
                """{"Enabled":false,"radius":0.25,"Donut":0.1,"fillIntensity":0.544,"tether":false}""",
                overwrite: true);
        }
    }

    public override void OnUpdate()
    {
        if(Controller.Scene != SceneP2)
        {
            ResetState();
            DisableAllMarkers();
            return;
        }

        UpdateInterludeNavPhase();
        UpdateActiveTowerMarkers();
        TryRunStep4DebuffReminder();
        LogStep4DebuffReminderSkipOnce();
        UpdateFieldMarkers();
    }

    public override void OnReset()
        => ResetState();

    public override void OnStartingCast(uint source, uint castId)
    {
        if(Controller.Scene != SceneP2)
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
        if(Controller.Scene != SceneP2)
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
        if(ImGui.BeginTabBar("##P2MissingKTAltSettings"))
        {
            if(ImGui.BeginTabItem("Main###tabMain"))
            {
                DrawMainSettings();
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

    #region Private Method

    // Assign Step1 roles using configured stack-pair assignment mode.
    private void AssignStep1Roles()
    {
        C.EnsureDefaults();
        if((Step1AssignmentMode)C.Step1RoleMode == Step1AssignmentMode.ChangeStackOnly)
            AssignStep1RolesChangeStackOnly();
        else
            AssignStep1RolesChangeWithPair();
    }

    // Assign Step1 roles per stack pair (stack + cone/spread partner).
    private void AssignStep1RolesChangeWithPair()
    {
        for(var pairIndex = 0; pairIndex < Step1PartyPairCount; pairIndex++)
        {
            if(!TryGetStep1Pair(pairIndex, out var playerA, out var playerB))
                return;

            if(!TryClassifyStep1Pair(playerA, playerB, out var stackPlayer, out var partnerPlayer, out var isStackPair))
                return;

            if(!isStackPair)
                continue;

            CountPairDebuffKinds(playerA, playerB, out var stack, out var cone, out var spread);

            if(stack == 1 && cone == 1 && spread == 0)
            {
                stackPlayer.RoleLabel = Role211LeftStack;
                partnerPlayer.RoleLabel = Role211Cone;
            }
            else if(stack == 1 && cone == 0 && spread == 1)
            {
                stackPlayer.RoleLabel = Role211RightStack;
                partnerPlayer.RoleLabel = Role211Spread;
            }
        }

        AssignStep1SecondGroupRoles();
    }

    // Assign Step1 roles with stack players resolved by priority across FirstGroup pairs.
    private void AssignStep1RolesChangeStackOnly()
    {
        var stackPlayers = new List<PlayerInfo>();

        for(var pairIndex = 0; pairIndex < Step1PartyPairCount; pairIndex++)
        {
            if(!TryGetStep1Pair(pairIndex, out var playerA, out var playerB))
                return;

            if(!TryClassifyStep1Pair(playerA, playerB, out var stackPlayer, out var partnerPlayer, out var isStackPair))
                return;

            if(!isStackPair)
                continue;

            CountPairDebuffKinds(playerA, playerB, out _, out _, out _);
            stackPlayers.Add(stackPlayer);
            partnerPlayer.RoleLabel = partnerPlayer.Debuff switch
            {
                DebuffKind.Cone => Role211Cone,
                DebuffKind.Spread => Role211Spread,
                _ => partnerPlayer.RoleLabel,
            };
        }

        var orderedStacks = OrderInfosByPriority(stackPlayers).ToList();
        if(orderedStacks.Count > 0)
            orderedStacks[0].RoleLabel = Role211LeftStack;
        if(orderedStacks.Count > 1)
            orderedStacks[1].RoleLabel = Role211RightStack;

        AssignStep1SecondGroupRoles();
    }

    // Assign Step1 SecondGroup field roles by priority rank across all four players.
    private void AssignStep1SecondGroupRoles()
    {
        var secondGroupPlayers = OrderInfosByPriority(_infos.Where(i => i.Half == MechanicHalf.Second)).ToList();
        if(secondGroupPlayers.Count != 4)
            return;

        secondGroupPlayers[0].RoleLabel = Role211Healer;
        secondGroupPlayers[1].RoleLabel = Role211Tank;
        secondGroupPlayers[2].RoleLabel = Role211Melee;
        secondGroupPlayers[3].RoleLabel = Role211Range;
    }

    // Assign Step4 tower 022 roles and field THMR, then caller caches tower OldRole.
    private void AssignStep4Roles()
    {
        for(var pairIndex = 0; pairIndex < Step1PartyPairCount; pairIndex++)
        {
            if(!TryGetStep1Pair(pairIndex, out var playerA, out var playerB))
                return;

            if(!TryClassifyStep1Pair(playerA, playerB, out _, out _, out var isStackPair))
                return;

            if(isStackPair || !IsTower(playerA) || !IsTower(playerB))
                continue;

            CountPairDebuffKinds(playerA, playerB, out var stack, out var cone, out var spread);

            if(stack == 0 && cone == 2 && spread == 0)
            {
                AssignPriorityRoles(playerA, playerB, Role022LeftCone, Role022RightCone);
                continue;
            }

            if(stack == 0 && cone == 0 && spread == 2)
                AssignPriorityRoles(playerA, playerB, Role022LeftSpread, Role022RightSpread);
        }

        var fieldPlayers = OrderInfosByPriority(_infos.Where(i => !IsTower(i))).ToList();
        if(fieldPlayers.Count != 4)
            return;

        fieldPlayers[0].RoleLabel = Role022Healer;
        fieldPlayers[1].RoleLabel = Role022Tank;
        fieldPlayers[2].RoleLabel = Role022Melee;
        fieldPlayers[3].RoleLabel = Role022Range;
    }

    // Cache tower player RoleLabel keyed by step; field players are not cached.
    private void CacheTowerDeterminedRoles(int step)
    {
        foreach(var info in _infos)
        {
            if(!IsTower(info))
            {
                info.RoleCacheByStep.Remove(step);
                continue;
            }

            if(string.IsNullOrEmpty(info.RoleLabel))
                continue;

            info.RoleCacheByStep[step] = info.RoleLabel;
        }
    }

    // Return whether the player was on tower at a specific step.
    private static bool IsTowerAtStep(PlayerInfo info, int step)
    {
        if(FirstHalfTowerSteps.Contains(step))
            return info.Half == MechanicHalf.First;
        if(SecondHalfTowerSteps.Contains(step))
            return info.Half == MechanicHalf.Second;
        return false;
    }

    // Return cached tower role for a player at a specific step.
    private static string? GetCachedRoleAtStep(PlayerInfo info, int step)
        => info.RoleCacheByStep.TryGetValue(step, out var role) ? role : null;

    // Return step index used to look up cached roles during recalculation.
    private int GetRoleLookupCacheStep()
    {
        if(_step <= ActiveStepMin)
            return ActiveStepMin;

        if(!TryGetActiveMechanicHalf(out var activeHalf))
            return _step - 1;

        var towerSteps = activeHalf == MechanicHalf.First
            ? FirstHalfTowerSteps
            : SecondHalfTowerSteps;

        var previousTowerStep = towerSteps.Where(s => s < _step).DefaultIfEmpty().Max();
        return previousTowerStep > 0 ? previousTowerStep : ActiveStepMin;
    }

    // Classify step1 pair as stack pair or non-stack pair.
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

    // Resolve initial First/Second group from alternate priority pairs.
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

    // Return two players for a step1 pair by priority-list indices.
    private bool TryGetStep1Pair(int pairIndex, out PlayerInfo playerA, out PlayerInfo playerB)
    {
        playerA = null!;
        playerB = null!;
        if(pairIndex < 0 || pairIndex >= Step1PartyPairCount || _infos.Count != PartyPlayerCount)
            return false;

        var (indexA, indexB) = Step1PartyPairIndices[pairIndex];
        playerA = _infos[indexA];
        playerB = _infos[indexB];
        return true;
    }

    // Detect pattern id from active tower group debuff counts.
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

    // Resolve live role labels and apply step/pattern rules.
    private bool TryUpdateLiveRoles(out string roleLabel)
    {
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

        if(!TryDetectPartyPattern(out var patternId))
            return false;

        var towerDebuffSignature = BuildTowerDebuffSignature();
        var towerDebuffsChanged = towerDebuffSignature != _lastTowerDebuffSignature;
        var stepChanged = _step != _lastTowerRoleStep;

        if(stepChanged)
        {
            if(_step == 1)
            {
                AssignStep1Roles();
                CacheTowerDeterminedRoles(_step);
            }
            else if(_step == 4)
            {
                AssignStep4Roles();
                CacheTowerDeterminedRoles(_step);
            }
            else if(IsPatternRecalculationStep(_step))
            {
                RecalculatePatternTowerRoles(patternId);
                CacheTowerDeterminedRoles(_step);
            }

            _lastTowerRoleStep = _step;
        }
        else if(IsPatternRecalculationStep(_step) && towerDebuffsChanged && AnyTowerPlayerHasCachedOldRole())
        {
            RecalculatePatternTowerRoles(patternId);
            CacheTowerDeterminedRoles(_step);
        }

        if(stepChanged || towerDebuffsChanged)
            _lastTowerDebuffSignature = towerDebuffSignature;

        if(_step != 1)
            ResolveFieldRoles(patternId);

        var baseInfo = GetBasePlayerInfo();
        if(baseInfo?.RoleLabel == null)
            return false;

        roleLabel = baseInfo.RoleLabel;
        return true;
    }

    // Return whether the step uses pattern tower recalculation.
    private static bool IsPatternRecalculationStep(int step)
        => step is 2 or 3 or 5 or 6 or 7 or 8;

    // Recalculate tower roles from cached OldRole pairs for the active pattern.
    private void RecalculatePatternTowerRoles(int patternId)
    {
        if(!AnyTowerPlayerHasCachedOldRole())
            return;

        if(patternId == 0)
        {
            ReassignCachedTowerPair(Role022LeftCone, Role022LeftSpread, ApplyPattern0Left022Pair);
            ReassignCachedTowerPair(Role022RightCone, Role022RightSpread, ApplyPattern0Right022Pair);
        }
        else if(patternId == 1)
        {
            ReassignCachedTowerPair(Role211LeftStack, Role211Cone, ApplyPattern1Left211Pair);
            ReassignCachedTowerPair(Role211RightStack, Role211Spread, ApplyPattern1Right211Pair);
        }
    }

    // Apply cached tower pair reassignment when both OldRole labels exist on tower.
    private void ReassignCachedTowerPair(string frontRole, string backRole, Action<PlayerInfo, PlayerInfo> assignPair)
    {
        var frontPlayer = FindTowerInfoByOldRole(frontRole);
        var backPlayer = FindTowerInfoByOldRole(backRole);
        if(frontPlayer == null || backPlayer == null)
            return;

        assignPair(frontPlayer, backPlayer);
    }

    // Pattern0 left 022 cached pair -> 211 roles.
    private void ApplyPattern0Left022Pair(PlayerInfo frontPlayer, PlayerInfo backPlayer)
    {
        CountPairDebuffKinds(frontPlayer, backPlayer, out var stack, out var cone, out var spread);

        if(stack == 0 && cone == 1 && spread == 1)
        {
            AssignPattern0ConeSpreadRoles(frontPlayer, backPlayer);
            return;
        }

        if(stack == 2 && cone == 0 && spread == 0)
        {
            AssignCrossTowerPairRoles(frontPlayer, backPlayer, Role211LeftStack, Role211RightStack);
            return;
        }

        if(stack == 1 && cone == 1 && spread == 0)
        {
            AssignDebuffRole(frontPlayer, DebuffKind.Stack, Role211LeftStack);
            AssignDebuffRole(backPlayer, DebuffKind.Stack, Role211LeftStack);
            AssignDebuffRole(frontPlayer, DebuffKind.Cone, Role211Cone);
            AssignDebuffRole(backPlayer, DebuffKind.Cone, Role211Cone);
            return;
        }

        if(stack == 1 && cone == 0 && spread == 1)
        {
            AssignDebuffRole(frontPlayer, DebuffKind.Stack, Role211LeftStack);
            AssignDebuffRole(backPlayer, DebuffKind.Stack, Role211LeftStack);
            AssignDebuffRole(frontPlayer, DebuffKind.Spread, Role211Spread);
            AssignDebuffRole(backPlayer, DebuffKind.Spread, Role211Spread);
        }
    }

    // Pattern0 right 022 cached pair -> 211 roles.
    private void ApplyPattern0Right022Pair(PlayerInfo frontPlayer, PlayerInfo backPlayer)
    {
        CountPairDebuffKinds(frontPlayer, backPlayer, out var stack, out var cone, out var spread);

        if(stack == 0 && cone == 1 && spread == 1)
        {
            AssignPattern0ConeSpreadRoles(frontPlayer, backPlayer);
            return;
        }

        if(stack == 2 && cone == 0 && spread == 0)
        {
            AssignCrossTowerPairRoles(frontPlayer, backPlayer, Role211RightStack, Role211LeftStack);
            return;
        }

        if(stack == 1 && cone == 1 && spread == 0)
        {
            AssignDebuffRole(frontPlayer, DebuffKind.Stack, Role211RightStack);
            AssignDebuffRole(backPlayer, DebuffKind.Stack, Role211RightStack);
            AssignDebuffRole(frontPlayer, DebuffKind.Cone, Role211Cone);
            AssignDebuffRole(backPlayer, DebuffKind.Cone, Role211Cone);
            return;
        }

        if(stack == 1 && cone == 0 && spread == 1)
        {
            AssignDebuffRole(frontPlayer, DebuffKind.Stack, Role211RightStack);
            AssignDebuffRole(backPlayer, DebuffKind.Stack, Role211RightStack);
            AssignDebuffRole(frontPlayer, DebuffKind.Spread, Role211Spread);
            AssignDebuffRole(backPlayer, DebuffKind.Spread, Role211Spread);
        }
    }

    // Pattern0 {0,1,1}: assign cone/spread to global 211 slots.
    private static void AssignPattern0ConeSpreadRoles(PlayerInfo playerA, PlayerInfo playerB)
    {
        AssignDebuffRole(playerA, DebuffKind.Cone, Role211Cone);
        AssignDebuffRole(playerA, DebuffKind.Spread, Role211Spread);
        AssignDebuffRole(playerB, DebuffKind.Cone, Role211Cone);
        AssignDebuffRole(playerB, DebuffKind.Spread, Role211Spread);
    }

    // Pattern1 left 211 cached pair -> 022 roles.
    private void ApplyPattern1Left211Pair(PlayerInfo frontPlayer, PlayerInfo backPlayer)
    {
        CountPairDebuffKinds(frontPlayer, backPlayer, out var stack, out var cone, out var spread);

        if(stack == 0 && cone == 1 && spread == 1)
        {
            AssignDebuffRole(frontPlayer, DebuffKind.Cone, Role022LeftCone);
            AssignDebuffRole(frontPlayer, DebuffKind.Spread, Role022LeftSpread);
            AssignDebuffRole(backPlayer, DebuffKind.Cone, Role022LeftCone);
            AssignDebuffRole(backPlayer, DebuffKind.Spread, Role022LeftSpread);
            return;
        }

        if(stack == 0 && cone == 2 && spread == 0)
        {
            AssignCrossTowerPairRoles(frontPlayer, backPlayer, Role022LeftCone, Role022RightCone);
            return;
        }

        if(stack == 0 && cone == 0 && spread == 2)
            AssignCrossTowerPairRoles(frontPlayer, backPlayer, Role022LeftSpread, Role022RightSpread);
    }

    // Pattern1 right 211 cached pair -> 022 roles.
    private void ApplyPattern1Right211Pair(PlayerInfo frontPlayer, PlayerInfo backPlayer)
    {
        CountPairDebuffKinds(frontPlayer, backPlayer, out var stack, out var cone, out var spread);

        if(stack == 0 && cone == 1 && spread == 1)
        {
            AssignDebuffRole(frontPlayer, DebuffKind.Cone, Role022RightCone);
            AssignDebuffRole(frontPlayer, DebuffKind.Spread, Role022RightSpread);
            AssignDebuffRole(backPlayer, DebuffKind.Cone, Role022RightCone);
            AssignDebuffRole(backPlayer, DebuffKind.Spread, Role022RightSpread);
            return;
        }

        if(stack == 0 && cone == 2 && spread == 0)
        {
            AssignCrossTowerPairRoles(frontPlayer, backPlayer, Role022RightCone, Role022LeftCone);
            return;
        }

        if(stack == 0 && cone == 0 && spread == 2)
            AssignCrossTowerPairRoles(frontPlayer, backPlayer, Role022RightSpread, Role022LeftSpread);
    }

    // Assign front/back roles when one side crosses to the opposite tower Side.
    private void AssignCrossTowerPairRoles(PlayerInfo frontPlayer, PlayerInfo backPlayer, string frontSideRole,
        string oppositeSideRole)
    {
        if((TowerPairSwapSide)C.TowerPairSwapSideMode == TowerPairSwapSide.Priority)
        {
            AssignCrossTowerPairRolesByPriority(frontPlayer, backPlayer, frontSideRole, oppositeSideRole);
            return;
        }

        if((TowerPairSwapSide)C.TowerPairSwapSideMode == TowerPairSwapSide.FrontSide)
        {
            frontPlayer.RoleLabel = oppositeSideRole;
            backPlayer.RoleLabel = frontSideRole;
            return;
        }

        frontPlayer.RoleLabel = frontSideRole;
        backPlayer.RoleLabel = oppositeSideRole;
    }

    // Assign cross-tower roles by priority: higher -> left role, lower -> right role.
    private void AssignCrossTowerPairRolesByPriority(PlayerInfo playerA, PlayerInfo playerB, string roleA,
        string roleB)
    {
        var leftRole = IsLeftTowerRole(roleA) ? roleA : roleB;
        var rightRole = IsLeftTowerRole(roleA) ? roleB : roleA;
        var ordered = OrderInfosByPriority([playerA, playerB]).ToList();
        ordered[0].RoleLabel = leftRole;
        ordered[1].RoleLabel = rightRole;
    }

    // Return whether a role label is the left-tower slot for cross-tower assignment.
    private static bool IsLeftTowerRole(string roleLabel)
        => roleLabel is Role211LeftStack or Role022LeftCone or Role022LeftSpread;

    // Assign field roles for non-tower players from pattern id.
    private void ResolveFieldRoles(int patternId)
    {
        foreach(var info in _infos)
        {
            if(!IsTower(info))
                info.RoleLabel = ResolveFieldRoleLabel(patternId, info);
        }
    }

    // Resolve a field role label from priority rank and pattern id.
    private string? ResolveFieldRoleLabel(int patternId, PlayerInfo info)
    {
        var rank = GetPriorityRank(_infos.Where(i => !IsTower(i)), info);
        if(rank < 0 || rank > 3)
            return null;

        if(patternId == 0)
        {
            return rank switch
            {
                0 => Role211Healer,
                1 => Role211Tank,
                2 => Role211Melee,
                3 => Role211Range,
                _ => null,
            };
        }

        return rank switch
        {
            0 => Role022Healer,
            1 => Role022Tank,
            2 => Role022Melee,
            3 => Role022Range,
            _ => null,
        };
    }

    // Assign role when player debuff matches expected kind.
    private static void AssignDebuffRole(PlayerInfo info, DebuffKind expected, string roleLabel)
    {
        if(info.Debuff == expected)
            info.RoleLabel = roleLabel;
    }

    // Assign two role labels ordered by priority within the pair.
    private void AssignPriorityRoles(PlayerInfo playerA, PlayerInfo playerB, string primaryLabel, string secondaryLabel)
    {
        var ordered = OrderInfosByPriority([playerA, playerB]).ToList();
        ordered[0].RoleLabel = primaryLabel;
        ordered[1].RoleLabel = secondaryLabel;
    }

    // Count stack/spread/cone within a two-player pair.
    private static void CountPairDebuffKinds(PlayerInfo playerA, PlayerInfo playerB, out int stack, out int cone,
        out int spread)
    {
        stack = 0;
        cone = 0;
        spread = 0;
        CountDebuffKind(playerA.Debuff, ref stack, ref cone, ref spread);
        CountDebuffKind(playerB.Debuff, ref stack, ref cone, ref spread);
    }

    // Increment debuff counters for a single debuff kind.
    private static void CountDebuffKind(DebuffKind debuff, ref int stack, ref int cone, ref int spread)
    {
        switch(debuff)
        {
            case DebuffKind.Stack:
                stack++;
                break;
            case DebuffKind.Cone:
                cone++;
                break;
            case DebuffKind.Spread:
                spread++;
                break;
        }
    }

    // Return whether any tower player has a cached role for lookup step.
    private bool AnyTowerPlayerHasCachedOldRole()
    {
        var cacheStep = GetRoleLookupCacheStep();
        return _infos.Any(i => IsTower(i) && !string.IsNullOrEmpty(GetCachedRoleAtStep(i, cacheStep)));
    }

    // Find a tower player whose cached role at lookup step matches.
    private PlayerInfo? FindTowerInfoByOldRole(string oldRoleLabel)
    {
        var cacheStep = GetRoleLookupCacheStep();
        return _infos.FirstOrDefault(i => IsTower(i) && GetCachedRoleAtStep(i, cacheStep) == oldRoleLabel);
    }

    // Build a signature of tower player debuffs for change detection.
    private string BuildTowerDebuffSignature()
    {
        var parts = new List<string>();
        foreach(var info in OrderInfosByPriority(_infos.Where(IsTower)))
            parts.Add($"{info.Player.EntityId}:{info.Debuff}");
        return string.Join("|", parts);
    }

    // Return whether the player is on tower for the current step half.
    private bool IsTower(PlayerInfo info)
        => IsTowerAtStep(info, _step);

    // Return active mechanic half for the current step.
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

    // Count debuffs among players in the active half group.
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

    // Ensure player infos match ordered party by priority list.
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

    // Refresh debuff kinds from live status list.
    private void UpdateDebuffs()
    {
        foreach(var info in _infos)
            info.Debuff = GetDebuffKind(info.Player);
    }

    // Return debuff kind from status ids.
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

    // Return party members ordered by configured priority list.
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

    // Sort players by priority index then entity id.
    private IEnumerable<IPlayerCharacter> OrderByPriority(IEnumerable<IPlayerCharacter> players)
        => players.OrderBy(GetPriorityIndex).ThenBy(p => p.EntityId);

    // Sort player infos by priority index then entity id.
    private IEnumerable<PlayerInfo> OrderInfosByPriority(IEnumerable<PlayerInfo> infos)
        => infos.OrderBy(i => GetPriorityIndex(i.Player)).ThenBy(i => i.Player.EntityId);

    // Return priority list index for a player.
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

    // Return priority rank within a subset of player infos.
    private int GetPriorityRank(IEnumerable<PlayerInfo> subset, PlayerInfo target)
    {
        var ordered = OrderInfosByPriority(subset).ToList();
        return ordered.FindIndex(i => i.Player.EntityId == target.Player.EntityId);
    }

    // Return base player info for the local player.
    private PlayerInfo? GetBasePlayerInfo()
    {
        if(BasePlayer == null)
            return null;

        return _infos.FirstOrDefault(i => i.Player.EntityId == BasePlayer.EntityId);
    }

    // Run Step4 FirstGroup debuff echo/marker reminder once per pull when configured.
    private void TryRunStep4DebuffReminder()
    {
        C.EnsureDefaults();

        if(_step4DebuffReminderSent)
        {
            _step4DebuffReminderSkipReason = null;
            return;
        }

        if(_step != Step4DebuffReminderTrigger)
        {
            _step4DebuffReminderSkipReason = null;
            if(!_step4DebuffReminderSent)
                _step4DebuffReminderDueAt = 0;
            return;
        }

        if(!TryEnsureInfos())
        {
            _step4DebuffReminderSkipReason = "party not ready";
            return;
        }

        UpdateDebuffs();

        if(!_initialGroupResolved)
        {
            if(!TryResolveInitialGroup())
            {
                _step4DebuffReminderSkipReason = "initial group unresolved";
                return;
            }

            _initialGroupResolved = true;
        }

        var baseInfo = GetBasePlayerInfo();
        if(baseInfo == null)
        {
            _step4DebuffReminderSkipReason = "self not in party list";
            return;
        }

        if(baseInfo.Half != MechanicHalf.First)
        {
            _step4DebuffReminderSkipReason = $"half is {FormatHalf(baseInfo.Half)} (need first)";
            return;
        }

        string? echoText = null;
        string? markCommand = null;

        switch(baseInfo.Debuff)
        {
            case DebuffKind.Spread:
                if(C.SpreadUseEcho)
                    echoText = NormalizeReminderEchoText(C.SpreadEchoText, DefaultSpreadEchoText);
                if(C.SpreadUseMarker)
                    markCommand = GetMarkCommand(C.SpreadMarkerType);
                break;
            case DebuffKind.Cone:
                if(C.ConeUseEcho)
                    echoText = NormalizeReminderEchoText(C.ConeEchoText, DefaultConeEchoText);
                if(C.ConeUseMarker)
                    markCommand = GetMarkCommand(C.ConeMarkerType);
                break;
        }

        if(echoText == null && markCommand == null)
        {
            _step4DebuffReminderSkipReason = baseInfo.Debuff switch
            {
                DebuffKind.Spread =>
                    "Debuff: Spread. If You Need Marking, Please Set Auto Marking Config.",
                DebuffKind.Cone =>
                    "Debuff: Cone. If You Need Marking, Please Set Auto Marking Config.",
                _ => $"debuff is {FormatDebuffKindDebug(baseInfo.Debuff)} (need Spread/Cone)",
            };
            return;
        }

        if(_step4DebuffReminderDueAt == 0)
        {
            _step4DebuffReminderDueAt = Environment.TickCount64 + ComputeStep4DebuffReminderDelayMs();
            _step4DebuffReminderSkipReason = null;
            return;
        }

        if(Environment.TickCount64 < _step4DebuffReminderDueAt)
            return;

        _step4DebuffReminderSent = true;
        _step4DebuffReminderSkipReason = null;
        _step4DebuffReminderSkipLogged = false;
        _step4DebuffReminderDueAt = 0;

        if(echoText != null)
            RunEchoCommand(echoText);
        if(markCommand != null)
            RunMarkCommand(markCommand);
    }

    // Log Step4 debuff reminder skip reason once when reminder will not fire.
    private void LogStep4DebuffReminderSkipOnce()
    {
        if(_step4DebuffReminderSent || _step <= Step4DebuffReminderTrigger || _step4DebuffReminderSkipReason == null
            || _step4DebuffReminderSkipLogged || _step4DebuffReminderDueAt != 0)
            return;

        if(_step4DebuffReminderSkipReason.StartsWith("Debuff:", StringComparison.Ordinal))
            DuoLog.Information(_step4DebuffReminderSkipReason);
        else if(Svc.Condition[ConditionFlag.DutyRecorderPlayback])
            DuoLog.Information($"Step4 debuff reminder skipped: {_step4DebuffReminderSkipReason}");

        _step4DebuffReminderSkipLogged = true;
    }

    // Return random Step4 debuff reminder delay in milliseconds.
    private static long ComputeStep4DebuffReminderDelayMs()
    {
        var seconds = Step4DebuffReminderDelayBaseSec
            + Random.Shared.NextSingle() * Step4DebuffReminderDelayRandomSpanSec;
        return (long)(seconds * 1000f);
    }

    // Execute a self-target marker chat command.
    private static void RunMarkCommand(string command)
    {
        if(Svc.Condition[ConditionFlag.DutyRecorderPlayback])
            DuoLog.Information($"Step4 debuff reminder mark: {command}");
        else
            Chat.Instance.ExecuteCommand(command);
    }

    // Execute an echo chat command with configured text.
    private static void RunEchoCommand(string text)
    {
        var command = $"/echo {text}";
        if(Svc.Condition[ConditionFlag.DutyRecorderPlayback])
            DuoLog.Information($"Step4 debuff reminder echo: {command}");
        else
            Chat.Instance.ExecuteCommand(command);
    }

    // Trim echo text and fall back when empty.
    private static string NormalizeReminderEchoText(string? text, string fallback)
    {
        var normalized = text?.Trim();
        return string.IsNullOrEmpty(normalized) ? fallback : normalized;
    }

    // Map marker kind to self-target mark command.
    private static string? GetMarkCommand(MarkerResolveKind kind)
        => kind switch
        {
            MarkerResolveKind.Attack => "/mk attack <me>",
            MarkerResolveKind.Stop => "/mk stop <me>",
            MarkerResolveKind.Bind => "/mk bind <me>",
            _ => null,
        };

    // Find role assignment index across all patterns.
    private static bool TryFindRoleAssignment(string roleLabel, out int patternId, out int assignmentIndex)
    {
        patternId = -1;
        assignmentIndex = -1;
        if(string.IsNullOrEmpty(roleLabel))
            return false;

        for(var i = 0; i < PatternCount; i++)
        {
            assignmentIndex = Array.FindIndex(Patterns[i].Assignments, a => a.Label == roleLabel);
            if(assignmentIndex < 0)
                continue;

            patternId = i;
            return true;
        }

        return false;
    }

    // Resolve configured position rule for a pattern assignment index.
    private PositionRule GetConfiguredRule(int patternId, int assignmentIndex)
    {
        var settings = C.PatternRules[patternId][assignmentIndex];
        return new PositionRule((PositionBasis)ClampBasisIndex(settings.Basis), settings.AngleDeg, settings.Range);
    }

    // Resolve world position from a tower-relative position rule.
    private Vector3? ResolvePositionRule(PositionRule rule)
    {
        if(!_hasActiveTowers)
            return null;

        if(!TryResolveTowerRoleIndices(out var rightIndex, out var leftIndex))
            return null;

        return rule.Basis switch
        {
            PositionBasis.RightTower => OffsetFromTowerAtCompassAngle(
                _activeTowerPositions[rightIndex],
                NormalizeAngle(GetAngleTowardCenterFromTower(_activeTowerPositions[rightIndex]) + rule.AngleDeg),
                rule.Range),
            PositionBasis.LeftTower => OffsetFromTowerAtCompassAngle(
                _activeTowerPositions[leftIndex],
                NormalizeAngle(GetAngleTowardCenterFromTower(_activeTowerPositions[leftIndex]) + rule.AngleDeg),
                rule.Range),
            PositionBasis.Center => OffsetFromTowerAtCompassAngle(
                ArenaCenter,
                NormalizeAngle(
                    GetAngleFromTrueNorth(
                        (_activeTowerPositions[rightIndex] + _activeTowerPositions[leftIndex]) * 0.5f) +
                    rule.AngleDeg),
                rule.Range),
            _ => null,
        };
    }

    // Resolve left/right tower indices from arena geometry.
    private bool TryResolveTowerRoleIndices(out int rightIndex, out int leftIndex)
    {
        rightIndex = 0;
        leftIndex = 1;
        if(!_hasActiveTowers)
        {
            if(!IsPatternPreviewActive())
                return false;

            return true;
        }

        var angle1 = GetAngleFromTrueNorth(_activeTowerPositions[0]);
        var angle2 = GetAngleFromTrueNorth(_activeTowerPositions[1]);
        var delta = NormalizeAngle(angle2 - angle1);
        if(delta > 180f)
            delta -= 360f;

        if(MathF.Abs(MathF.Abs(delta) - 90f) > AngleTolerance)
        {
            if(!IsPatternPreviewActive())
                return false;

            return true;
        }

        if(delta > 0f)
        {
            rightIndex = 0;
            leftIndex = 1;
        }
        else
        {
            rightIndex = 1;
            leftIndex = 0;
        }

        return true;
    }

    // Count debuffs among tower players for the current step.
    private void CountDebuffKindsFromTowerInfos(out int stack, out int spread, out int cone)
    {
        stack = 0;
        spread = 0;
        cone = 0;

        foreach(var info in _infos.Where(IsTower))
        {
            switch(info.Debuff)
            {
                case DebuffKind.Stack: stack++; break;
                case DebuffKind.Spread: spread++; break;
                case DebuffKind.Cone: cone++; break;
            }
        }
    }

    // Offset a point from a tower at a compass angle and range.
    private static Vector3 OffsetFromTowerAtCompassAngle(Vector3 towerPos, float angleDeg, float range)
        => CalculatePointCircle(towerPos, range, angleDeg);

    // Compute a point on a circle from center, radius, and degree (util.cs).
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

    // Normalize angle to 0-360 (util.cs).
    private static float NormalizeAngle(float degree)
        => (degree % 360f + 360f) % 360f;

    // Return compass angle from arena center toward true north reference.
    private static float GetAngleFromTrueNorth(Vector3 position)
    {
        var northAngle = MathHelper.GetRelativeAngle(ArenaCenter, TrueNorth);
        var posAngle = MathHelper.GetRelativeAngle(ArenaCenter, position);
        return NormalizeAngle(posAngle - northAngle);
    }

    // Return angle from tower toward arena center.
    private static float GetAngleTowardCenterFromTower(Vector3 towerPos)
        => NormalizeAngle(GetAngleFromTrueNorth(towerPos) + 180f);

    // Handle paired tower spawn map effects and advance step.
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
        SetActiveTowers([CreateTowerHit(reference), CreateTowerHit(paired)]);
    }

    // Buffer tower clear map effect positions.
    private void AddTowerClearPosition(uint position)
    {
        AddUniquePairPosition(_pendingTowerClearPositions, position);
        if(_pendingTowerClearPositions.Count != 2)
            return;

        if(!TryGetTowerPairReference(_pendingTowerClearPositions[0], _pendingTowerClearPositions[1], out _, out _))
        {
            _pendingTowerClearPositions.Clear();
            return;
        }

        _pendingTowerClearPositions.Clear();
    }

    // Keep at most two unique map positions in a pending list.
    private static void AddUniquePairPosition(List<uint> list, uint position)
    {
        if(list.Count >= 2)
            list.Clear();

        if(!list.Contains(position))
            list.Add(position);
    }

    // Store active tower slot positions and increment step counter.
    private void SetActiveTowers(IReadOnlyList<(string Label, Vector3 Position, uint EntityId)> hits)
    {
        if(hits.Count != 2)
            return;

        _activeTowerPositions[0] = hits[0].Position;
        _activeTowerPositions[1] = hits[1].Position;
        _activeTowerEntityIds[0] = hits[0].EntityId;
        _activeTowerEntityIds[1] = hits[1].EntityId;
        _hasActiveTowers = true;
        _step++;
        TryRunStep4DebuffReminder();
    }

    // Create tower hit tuple from map slot index.
    private static (string Label, Vector3 Position, uint EntityId) CreateTowerHit(uint mapIndex)
    {
        var slot = TowerSlots[mapIndex - 1];
        return (slot.Label, slot.Position, mapIndex);
    }

    // Return whether map effect data indicates tower spawn.
    private static bool IsTowerSpawnMapEffect(ushort data1, ushort data2)
        => data1 == MapEffectTowerSpawnData1 && data2 == MapEffectTowerSpawnData2;

    // Return whether map effect data indicates tower clear.
    private static bool IsTowerClearMapEffect(ushort data1, ushort data2)
        => data1 == MapEffectTowerClearData1 && data2 == MapEffectTowerClearData2;

    // Return whether map slot index is a tower ring slot.
    private static bool IsTowerMapPosition(uint position)
        => position is >= MapEffectTowerIndexMin and <= MapEffectTowerIndexMax;

    // Resolve adjacent tower pair reference from two map indices.
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

    // Advance map ring index by steps modulo eight slots.
    private static uint AddMapSteps(uint position, int steps)
        => (uint)(((int)position - 1 + steps + 8) % 8 + 1);

    // Advance interlude nav phase from boss cast state.
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

    // Return whether any targetable boss is casting the given action id.
    private static bool IsAnyBossCasting(uint castId)
        => Svc.Objects.OfType<IBattleNpc>().Any(x => x.IsTargetable && x.IsCasting && x.CastActionId == castId);

    // Return interlude navigation position between or opposite towers.
    private bool TryGetInterludeNavPosition(out Vector3 position, out string label)
    {
        position = default;
        label = "";
        if(_interludeNavPhase is not (InterludeNavPhase.PastGap or InterludeNavPhase.FutureOpposite))
            return false;
        if(IsAnyBossCasting(PastsEndCast) || IsAnyBossCasting(FuturesEndCast))
            return false;
        if(!_hasActiveTowers)
            return false;

        label = _interludeNavPhase == InterludeNavPhase.PastGap ? "between towers" : "opposite side";
        position = _step == ActiveStepMax
            ? Step8InterludeNavPosition
            : ResolveTowerRelativeInterludeNavPosition();
        return true;
    }

    // Return interlude nav position from active tower pair direction (steps 1-7).
    private Vector3 ResolveTowerRelativeInterludeNavPosition()
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

    // Update active tower ring marker elements.
    private void UpdateActiveTowerMarkers()
    {
        UpdateActiveTowerMarker(ElActiveTower0, 0);
        UpdateActiveTowerMarker(ElActiveTower1, 1);
    }

    // Update one active tower marker element.
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
        element.Enabled = true;
    }

    // Enable local role nav marker with AttentionColor (rainbow when configured).
    private void EnableMyRoleMarker(Vector3 position, string label)
        => EnableRoleMarker(ElMyRole, position, label, tether: true);

    // Enable a role marker element at a world position.
    private void EnableRoleMarker(string elementName, Vector3 position, string label, bool tether,
        bool showOverlayText = false)
    {
        if(!Controller.TryGetElementByName(elementName, out var element))
            return;

        element.SetRefPosition(position);
        element.color = Controller.AttentionColor;
        element.overlayText = showOverlayText ? label : "";
        element.tether = tether;
        element.Enabled = true;
    }

    // Disable local role nav marker.
    private void DisableMyRoleMarker()
    {
        if(Controller.TryGetElementByName(ElMyRole, out var element))
        {
            element.Enabled = false;
            element.tether = false;
        }
    }

    // Return element name for a pattern preview role marker index.
    private static string GetRolePreviewElementName(int index)
        => $"RolePreview_{index}";

    // Disable all pattern preview role markers.
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

    // Update field markers for live role or pattern preview.
    private void UpdateFieldMarkers()
    {
        if(Controller.Scene != SceneP2)
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
            EnableMyRoleMarker(interludePosition, interludeLabel);
            return;
        }

        DisableMyRoleMarker();

        if(!_hasActiveTowers || _step is < ActiveStepMin or > ActiveStepMax)
            return;

        if(!TryUpdateLiveRoles(out var roleLabel))
            return;

        if(!TryFindRoleAssignment(roleLabel, out var assignmentPatternId, out var assignmentIndex))
            return;

        if(ResolvePositionRule(GetConfiguredRule(assignmentPatternId, assignmentIndex)) is not { } position)
            return;

        EnableMyRoleMarker(position, roleLabel);
    }

    // Show configured pattern role positions on the field.
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

            EnableRoleMarker(GetRolePreviewElementName(i), position, pattern.Assignments[i].Label, showRoleTether,
                showOverlayText: true);
        }
    }

    // Disable all visible markers.
    private void DisableAllMarkers()
    {
        DisableMyRoleMarker();
        DisableAllRolePreviewMarkers();
        if(Controller.TryGetElementByName(ElActiveTower0, out var tower0))
            tower0.Enabled = false;
        if(Controller.TryGetElementByName(ElActiveTower1, out var tower1))
            tower1.Enabled = false;
    }

    // Reset runtime state on wipe or scene leave.
    private void ResetState()
    {
        _hasActiveTowers = false;
        _step = 0;
        _lastTowerRoleStep = 0;
        _lastTowerDebuffSignature = null;
        _initialGroupResolved = false;
        _interludeNavPhase = InterludeNavPhase.None;
        _interludeBossCastSeen = false;
        _pendingTowerSpawnPositions.Clear();
        _pendingTowerClearPositions.Clear();
        _activeTowerPositions[0] = Vector3.Zero;
        _activeTowerPositions[1] = Vector3.Zero;
        _activeTowerEntityIds[0] = 0;
        _activeTowerEntityIds[1] = 0;
        _infos.Clear();
        _step4DebuffReminderSent = false;
        _step4DebuffReminderSkipReason = null;
        _step4DebuffReminderSkipLogged = false;
        _step4DebuffReminderDueAt = 0;
    }

    // Draw debug tab with live player info and pattern preview controls.
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

    // Draw current step and live pattern summary for debug.
    private void DrawDebugStepSection()
    {
        ImGui.TextUnformatted("Step");
        ImGui.Separator();
        ImGui.TextUnformatted(_hasActiveTowers ? $"Step: {_step}" : "Step: —");
        ImGui.TextUnformatted(_initialGroupResolved ? "Initial group: resolved" : "Initial group: —");

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
        }

        DrawDebugStep4DebuffReminderSection();
    }

    // Draw Step4 FirstGroup debuff reminder debug status.
    private void DrawDebugStep4DebuffReminderSection()
    {
        C.EnsureDefaults();
        ImGui.Spacing();
        ImGui.TextUnformatted("Step4 debuff reminder");
        ImGui.Separator();
        ImGui.TextUnformatted(
            $"Spread: {FormatMarkerRuleDebug(C.SpreadUseEcho, C.SpreadEchoText, C.SpreadUseMarker, C.SpreadMarkerType)}");
        ImGui.TextUnformatted(
            $"Cone: {FormatMarkerRuleDebug(C.ConeUseEcho, C.ConeEchoText, C.ConeUseMarker, C.ConeMarkerType)}");
        ImGui.TextUnformatted(_step4DebuffReminderSent
            ? "Step4 debuff reminder: sent"
            : "Step4 debuff reminder: not sent");
        if(!_step4DebuffReminderSent && _step4DebuffReminderDueAt != 0)
        {
            var remainingSec = MathF.Max(0f, (_step4DebuffReminderDueAt - Environment.TickCount64) / 1000f);
            ImGui.TextUnformatted($"Step4 debuff reminder pending: {remainingSec:0.0}s");
        }

        if(!_step4DebuffReminderSent && _step4DebuffReminderSkipReason != null)
            ImGui.TextUnformatted($"Step4 debuff reminder skip: {_step4DebuffReminderSkipReason}");
    }

    // Draw live player info tables split by mechanic half.
    private void DrawDebugInfosSection()
    {
        C.EnsureDefaults();
        ImGui.TextUnformatted("Player infos (live)");
        ImGui.Separator();

        if(TryEnsureInfos())
        {
            UpdateDebuffs();
            if(!_initialGroupResolved)
                TryResolveInitialGroup();
        }

        if(_infos.Count == 0)
        {
            ImGui.TextUnformatted("  (no party data)");
            return;
        }

        DrawPlayerInfosTable("First Half", GetInfosByMechanicHalf(MechanicHalf.First));
        ImGui.Spacing();
        DrawPlayerInfosTable("Second Half", GetInfosByMechanicHalf(MechanicHalf.Second));

        var unresolvedCount = CountUnresolvedInfos();
        if(unresolvedCount > 0)
            ImGui.TextUnformatted($"Unresolved group: {unresolvedCount} player(s)");

        CountDebuffKindsFromTowerInfos(out var stack, out var spread, out var cone);
        ImGui.TextUnformatted($"Tower counts: stack={stack} spread={spread} cone={cone}");
    }

    // Return player infos for one mechanic half ordered by priority.
    private List<PlayerInfo> GetInfosByMechanicHalf(MechanicHalf half)
        => OrderInfosByPriority(_infos.Where(i => i.Half == half)).ToList();

    // Count players without resolved mechanic half.
    private int CountUnresolvedInfos()
        => _infos.Count(i => i.Half == MechanicHalf.None);

    // Draw one player info table for debug.
    private void DrawPlayerInfosTable(string title, IReadOnlyList<PlayerInfo> infos)
    {
        ImGui.TextUnformatted(title);
        if(infos.Count == 0)
        {
            ImGui.TextUnformatted("  (empty)");
            return;
        }

        if(!ImGui.BeginTable($"##P2MissingKTAltInfos{title}", 7,
               ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            return;

        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 20f);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 200f);
        ImGui.TableSetupColumn("Half", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Debuff", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Role", ImGuiTableColumnFlags.WidthFixed, 200f);
        ImGui.TableSetupColumn("Cache", ImGuiTableColumnFlags.WidthFixed, 220f);
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
            ImGui.TextUnformatted(FormatRoleCacheForDebug(info));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(isTower ? "tower" : "field");

            if(isTower)
                ImGui.PopStyleColor();
        }

        ImGui.EndTable();
    }

    // Draw pattern preview combo and optional single-role filter.
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

    // Draw role filter combo when a pattern preview is active.
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

    // Build combo labels for pattern preview role filter.
    private static string[] BuildPatternRoleComboLabels(PatternDefinition pattern)
    {
        var labels = new string[pattern.Assignments.Length + 1];
        labels[0] = "None (all roles)";
        for(var i = 0; i < pattern.Assignments.Length; i++)
            labels[i + 1] = pattern.Assignments[i].Label;
        return labels;
    }

    // Clamp debug preview role index to valid assignment range.
    private static int ClampPatternPreviewRole(int patternPreview, int roleIndex)
    {
        if(!IsPatternPreviewActive(patternPreview))
            return DebugPatternPreviewRoleNone;
        if(roleIndex < 0)
            return DebugPatternPreviewRoleNone;
        var assignmentCount = Patterns[patternPreview].Assignments.Length;
        return roleIndex >= assignmentCount ? DebugPatternPreviewRoleNone : roleIndex;
    }

    // Clamp Step1 role assignment mode to valid enum range.
    private static int ClampStep1RoleMode(int mode)
        => mode switch
        {
            (int)Step1AssignmentMode.ChangeStackOnly => (int)Step1AssignmentMode.ChangeStackOnly,
            _ => (int)Step1AssignmentMode.ChangeWithPair,
        };

    // Clamp tower pair swap side to valid enum range.
    private static int ClampTowerPairSwapSide(int mode)
        => mode switch
        {
            (int)TowerPairSwapSide.FrontSide => (int)TowerPairSwapSide.FrontSide,
            (int)TowerPairSwapSide.Priority => (int)TowerPairSwapSide.Priority,
            _ => (int)TowerPairSwapSide.BackSide,
        };

    // Return whether a pattern preview index selects an active preview.
    private static bool IsPatternPreviewActive(int patternPreview)
        => patternPreview >= 0 && patternPreview < PatternCount;

    // Return whether config pattern preview is active.
    private bool IsPatternPreviewActive()
        => IsPatternPreviewActive(C.DebugPatternPreview);

    // Convert config preview index to combo box index.
    private static int DebugPatternPreviewToComboIndex(int preview)
        => preview < 0 ? 0 : preview + 1;

    // Convert combo box index to config preview index.
    private static int ComboIndexToDebugPatternPreview(int comboIndex)
        => comboIndex <= 0 ? DebugPatternPreviewNone : comboIndex - 1;

    // Refresh field markers after pattern rule edits.
    private void OnPatternRulesChanged()
        => UpdateFieldMarkers();

    // Format mechanic half for debug display.
    private static string FormatHalf(MechanicHalf half)
        => half switch
        {
            MechanicHalf.First => "first",
            MechanicHalf.Second => "second",
            _ => "—",
        };

    // Format previous-step role cache for debug display.
    private string FormatRoleCacheForDebug(PlayerInfo info)
    {
        if(_step <= ActiveStepMin)
            return "—";

        var lookupStep = GetRoleLookupCacheStep();
        if(GetCachedRoleAtStep(info, lookupStep) is { } lookup)
            return $"S{lookupStep}: {lookup}";

        return "—";
    }

    // Format debuff kind with status id for debug display.
    private static string FormatDebuffKindDebug(DebuffKind kind)
        => kind switch
        {
            DebuffKind.Stack => "Stack (5084)",
            DebuffKind.Spread => "Spread (5085)",
            DebuffKind.Cone => "Cone (5086)",
            _ => "—",
        };

    // Draw main settings tab with priority and pattern sections.
    private void DrawMainSettings()
    {
        C.EnsureDefaults();

        ImGui.Spacing();
        ImGui.SetWindowFontScale(1.2f);
        DrawCenteredText(EColor.YellowBright, "Please Set Priority");
        DrawCenteredText(EColor.YellowBright, "H1, H2, T1, T2, M1, M2, R1, R2");
        ImGui.SetWindowFontScale(1f);
        ImGui.Spacing();

        DrawSettingsSectionHeader("General");
        ImGui.TextUnformatted("Starting Pairs: [H1,T1], [H2,T2], [M1,R1], [M2,R2].");
        ImGui.TextUnformatted("Steps 1,2,3,8 = FirstGroup tower. / Steps 4,5,6,7 = SecondGroup tower.");
        
        DrawSettingsSectionHeader("First Stack Swap Rule");
        DrawStep1RoleModeSettings();

        DrawSettingsSectionHeader("Tower Pair Swap Side");
        DrawTowerPairSwapSideSettings();

        DrawSettingsSectionHeader("Debuff Reminder");
        ImGui.TextUnformatted("For Step8, FirstGroup saves debuff in Step4.");
        ImGui.TextUnformatted("Spread:");
        DrawMarkerEchoRow("Echo Message##Spread", ref C.SpreadUseEcho, ref C.SpreadEchoText, DefaultSpreadEchoText);
        DrawMarkerKindRow("Auto Marking##Spread", ref C.SpreadUseMarker, ref C.SpreadMarkerType);
        ImGui.TextUnformatted("Cone:");
        DrawMarkerEchoRow("Echo Message##Cone", ref C.ConeUseEcho, ref C.ConeEchoText, DefaultConeEchoText);
        DrawMarkerKindRow("Auto Marking##Cone", ref C.ConeUseMarker, ref C.ConeMarkerType);

        DrawSettingsSectionHeader("Priority");
        ImGui.TextUnformatted("H1, H2, T1, T2, M1, M2, R1, R2");
        C.PriorityData.Draw();

        DrawSettingsSectionHeader("Pattern");
        DrawPatternAssignmentTables();
    }

    // Draw Step1 role assignment mode toggles.
    private void DrawStep1RoleModeSettings()
    {
        C.EnsureDefaults();
        var mode = C.Step1RoleMode;
        if(ImGui.RadioButton("Pair Debuff (Cone => Left, Spread => Right)###step1WithPair",
                mode == (int)Step1AssignmentMode.ChangeWithPair))
            mode = (int)Step1AssignmentMode.ChangeWithPair;
        if(ImGui.RadioButton("Stack Debuff Priority (Swap Stack Only)###step1StackOnly",
                mode == (int)Step1AssignmentMode.ChangeStackOnly))
            mode = (int)Step1AssignmentMode.ChangeStackOnly;
        C.Step1RoleMode = ClampStep1RoleMode(mode);
    }

    // Draw tower pair front/back swap side toggles for pattern recalculation.
    private void DrawTowerPairSwapSideSettings()
    {
        C.EnsureDefaults();
        var mode = C.TowerPairSwapSideMode;
        if(ImGui.RadioButton("Back Side###towerPairBackSide",
                mode == (int)TowerPairSwapSide.BackSide))
            mode = (int)TowerPairSwapSide.BackSide;
        if(ImGui.RadioButton("Front Side###towerPairFrontSide",
                mode == (int)TowerPairSwapSide.FrontSide))
            mode = (int)TowerPairSwapSide.FrontSide;
        if(ImGui.RadioButton("Priority (Left: Lower to Right / Right: Higher to Left)###towerPairPriority",
                mode == (int)TowerPairSwapSide.Priority))
            mode = (int)TowerPairSwapSide.Priority;
        C.TowerPairSwapSideMode = ClampTowerPairSwapSide(mode);
    }

    // Draw marker kind combo for Step4 debuff reminder settings.
    private static void DrawMarkerResolveKindCombo(string label, ref MarkerResolveKind kind)
    {
        var idx = (int)kind;
        if(idx < 0 || idx >= MarkerResolveKindLabels.Length)
            idx = 0;

        ImGui.SetNextItemWidth(120f);
        if(ImGui.Combo(label, ref idx, MarkerResolveKindLabels, MarkerResolveKindLabels.Length))
            kind = (MarkerResolveKind)idx;
    }

    // Draw one echo row: checkbox and text field on the same line.
    private void DrawMarkerEchoRow(string label, ref bool enabled, ref string echoText, string defaultEchoText)
    {
        ImGui.PushID($"{label}EchoRow");
        ImGui.Checkbox(label, ref enabled);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f);
        ImGui.BeginDisabled(!enabled);
        ImGui.InputText("##text", ref echoText, MarkerEchoTextMaxLength);
        ImGui.EndDisabled();
        ImGui.PopID();

        if(enabled && string.IsNullOrWhiteSpace(echoText))
            echoText = defaultEchoText;
    }

    // Draw one marker row: checkbox and marker combo on the same line.
    private void DrawMarkerKindRow(string label, ref bool enabled, ref MarkerResolveKind markerType)
    {
        ImGui.PushID($"{label}MarkerRow");
        ImGui.Checkbox(label, ref enabled);
        ImGui.SameLine();
        ImGui.BeginDisabled(!enabled);
        DrawMarkerResolveKindCombo("##type", ref markerType);
        ImGui.EndDisabled();
        ImGui.PopID();
    }

    // Format configured echo/marker rule for debug display.
    private static string FormatMarkerRuleDebug(bool useEcho, string echoText, bool useMarker,
        MarkerResolveKind markerType)
    {
        var parts = new List<string>(2);
        if(useEcho)
            parts.Add($"Echo \"{echoText}\"");
        if(useMarker)
            parts.Add($"Marker {FormatMarkerResolveKind(markerType)}");
        return parts.Count == 0 ? "(none)" : string.Join(" + ", parts);
    }

    // Format marker resolve kind for debug display.
    private static string FormatMarkerResolveKind(MarkerResolveKind kind)
        => MarkerResolveKindLabels[(int)kind];

    // Draw colored text centered in the current content region.
    private static void DrawCenteredText(Vector4 color, string text)
    {
        var textWidth = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X - textWidth) * 0.5f);
        ImGuiEx.Text(color, text);
    }

    // Draw a settings section header with spacing, disabled title, and separator.
    private static void DrawSettingsSectionHeader(string title)
    {
        ImGui.Spacing();
        ImGui.TextDisabled(title);
        ImGui.Separator();
    }

    // Draw editable pattern assignment tables in settings UI.
    private void DrawPatternAssignmentTables()
    {
        foreach(var pattern in Patterns)
        {
            var header =
                $"Pattern {pattern.Id} (stack: {pattern.StackCount}, cone: {pattern.ConeCount}, spread: {pattern.SpreadCount})###patAlt{pattern.Id}";
            if(!ImGui.TreeNode(header))
                continue;

            DrawPatternAssignmentTable(pattern);
            ImGui.TreePop();
        }
    }

    // Draw one pattern assignment table.
    private void DrawPatternAssignmentTable(PatternDefinition pattern)
    {
        if(!ImGui.BeginTable($"PatternAlt_{pattern.Id}###patAltTable", 5,
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
            var settings = C.PatternRules[pattern.Id][i];
            var basis = settings.Basis;
            var angleDeg = settings.AngleDeg;
            var range = settings.Range;
            var changed = false;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(pattern.Assignments[i].Label);

            ImGui.PushID($"patAltRule_{pattern.Id}_{i}");
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
                settings.AngleDeg = NormalizeAngle(angleDeg);
                settings.Range = MathF.Max(0f, range);
                OnPatternRulesChanged();
            }

            ImGui.TableNextColumn();
            if(ResolvePositionRule(GetConfiguredRule(pattern.Id, i)) is { } pos)
                ImGui.TextUnformatted($"({pos.X:F1}, {pos.Z:F1})");
        }

        ImGui.EndTable();
    }

    // Clamp basis combo index to valid enum range.
    private static int ClampBasisIndex(int basis)
        => basis switch
        {
            < 0 => 0,
            > 2 => 2,
            _ => basis,
        };

    // Create default pattern rule settings from pattern definitions.
    private static PatternRuleSettings[][] CreateDefaultPatternRules()
    {
        var rules = new PatternRuleSettings[PatternCount][];
        for(var patternId = 0; patternId < PatternCount; patternId++)
        {
            rules[patternId] = new PatternRuleSettings[RoleCountPerPattern];
            for(var i = 0; i < RoleCountPerPattern; i++)
            {
                var assignment = Patterns[patternId].Assignments[i];
                rules[patternId][i] = new PatternRuleSettings
                {
                    Basis = (int)assignment.Rule.Basis,
                    AngleDeg = assignment.Rule.AngleDeg,
                    Range = assignment.Rule.Range,
                };
            }
        }

        return rules;
    }

    // Ensure pattern rules array matches pattern definitions.
    private static PatternRuleSettings[][] EnsurePatternRules(PatternRuleSettings[][]? rules)
    {
        var defaults = CreateDefaultPatternRules();
        if(rules == null || rules.Length != PatternCount)
            return defaults;

        for(var patternId = 0; patternId < PatternCount; patternId++)
        {
            if(rules[patternId] == null || rules[patternId].Length != RoleCountPerPattern)
                rules[patternId] = defaults[patternId];
        }

        return rules;
    }

    // Create a pattern assignment rule entry.
    private static PatternAssignment Rule(string label, PositionBasis basis, float angleDeg, float range)
        => new(label, new PositionRule(basis, angleDeg, range));

    #endregion
}
