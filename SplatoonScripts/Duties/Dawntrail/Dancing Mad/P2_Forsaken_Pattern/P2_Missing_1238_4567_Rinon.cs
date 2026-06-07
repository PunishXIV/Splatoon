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

internal class P2_Missing_1238_4567_Rinon : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(3, "mirage, Poneglyph");
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

    // Step1 ResolveInitialGroup: party pairs by priority index (plan4).
    private static readonly (int A, int B)[] Step1PartyPairIndices = [(0, 2), (1, 3), (4, 6), (5, 7)];
    private const int Step1PartyPairCount = 4;
    private const int StackPairCount = 2;
    private const int NonStackPairCount = 2;

    // Step1 initial role display uses step 1 only.
    private const int Step1DisplayStep = 1;

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
    private const int MaxPatternAssignments = 8;
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
            Rule("211_LeftTowerStack", PositionBasis.LeftTower, 182f, 1.2f),
            Rule("211_LeftTowerCone", PositionBasis.LeftTower, 0f, 3.15f),
            Rule("211_LeftTowerStackOutside", PositionBasis.LeftTower, 0f, 5f),
            Rule("211_LeftTowerBaitCone", PositionBasis.LeftTower, 173f, 4.55f),
            Rule("211_RightTowerStack", PositionBasis.RightTower, 211f, 3.5f),
            Rule("211_RightTowerSpread", PositionBasis.RightTower, 43f, 3.55f),
            Rule("211_RightTowerStackOutside", PositionBasis.RightTower, 193f, 4.45f),
        ]),
        new(1, 0, 2, 2,
        [
            Rule("022_LeftTowerCone", PositionBasis.LeftTower, 179f, 3.5f),
            Rule("022_RightTowerCone", PositionBasis.RightTower, 183f, 3.65f),
            Rule("022_LeftTowerSpread", PositionBasis.LeftTower, 338f, 3.5f),
            Rule("022_RightTowerSpread", PositionBasis.RightTower, 20f, 3.6f),
            Rule("022_LeftHelper", PositionBasis.LeftTower, 91f, 4.55f),
            Rule("022_LeftBait", PositionBasis.Center, 316f, 5.4f),
            Rule("022_RightHelper", PositionBasis.RightTower, 267f, 4.4f),
            Rule("022_RightBait", PositionBasis.Center, 40f, 5.7f),
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

    private sealed class PlayerInfo
    {
        public required IPlayerCharacter Player;
        public MechanicHalf Half;
        public DebuffKind Debuff;
        public string? RoleLabel;
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
        public PatternRuleSettings[][] PatternRules = CreateDefaultPatternRules();

        public void EnsureDefaults()
        {
            PriorityData ??= CreateDefaultPriorityData();
            if(DebugPatternPreview >= PatternCount)
                DebugPatternPreview = DebugPatternPreviewNone;
            DebugPatternPreviewRole = ClampPatternPreviewRole(DebugPatternPreview, DebugPatternPreviewRole);
            PatternRules = EnsurePatternRules(PatternRules);
        }

        public void ResetToDefaults()
        {
            PriorityData = CreateDefaultPriorityData();
            DebugPatternPreview = DebugPatternPreviewNone;
            DebugPatternPreviewRole = DebugPatternPreviewRoleNone;
            PatternRules = CreateDefaultPatternRules();
        }

        private static PriorityData CreateDefaultPriorityData()
            => new()
            {
                Name = "Wave Cannon tower priority",
                Description = "Default: H2 H1 ST MT | D1 D2 D3 D4",
                PriorityLists =
                [
                    new PriorityList
                    {
                        IsRole = true,
                        List =
                        [
                            new JobbedPlayer { Role = RolePosition.H2 },
                            new JobbedPlayer { Role = RolePosition.H1 },
                            new JobbedPlayer { Role = RolePosition.T2 },
                            new JobbedPlayer { Role = RolePosition.T1 },
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
        if(ImGui.BeginTabBar("##P212384567PairSettings"))
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
        ImGui.TextUnformatted(
            "Priority — Step1 pairs by list index: [1,3], [2,4], [5,7], [6,8] (0-based: 0-2, 1-3, 4-6, 5-7).");
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
        ImGui.TextUnformatted(_initialGroupResolved ? "Initial group (pairs): resolved" : "Initial group (pairs): —");
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
        ImGui.TextUnformatted("Step1 pairs (priority index)");
        for(var pairIndex = 0; pairIndex < Step1PartyPairCount; pairIndex++)
        {
            if(!TryGetStep1Pair(pairIndex, out var playerA, out var playerB))
            {
                ImGui.TextUnformatted($"  P{pairIndex}: (invalid)");
                continue;
            }

            var (indexA, indexB) = Step1PartyPairIndices[pairIndex];
            if(!TryClassifyStep1Pair(playerA, playerB, out _, out var partner, out var isStackPair))
            {
                ImGui.TextUnformatted(
                    $"  P{pairIndex} [{indexA},{indexB}]: {FormatPlayerDebuff(playerA)} + {FormatPlayerDebuff(playerB)} (invalid)");
                continue;
            }

            if(isStackPair)
            {
                ImGui.TextUnformatted(
                    $"  P{pairIndex} [{indexA},{indexB}]: Stack+{FormatDebuffKindDebug(partner.Debuff)} -> FirstHalf");
            }
            else
            {
                ImGui.TextUnformatted(
                    $"  P{pairIndex} [{indexA},{indexB}]: {FormatPlayerDebuff(playerA)} + {FormatPlayerDebuff(playerB)} -> SecondHalf");
            }
        }
    }

    private static string FormatPlayerDebuff(PlayerInfo info)
        => $"{info.Player.Name} ({FormatDebuffKindDebug(info.Debuff)})";

    private void DrawDebugInfosSection()
    {
        C.EnsureDefaults();
        ImGui.TextUnformatted("Player infos (live, pair strat)");
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

    // Returns two players for a step1 pair by priority-list indices.
    private bool TryGetStep1Pair(int pairIndex, out PlayerInfo playerA, out PlayerInfo playerB)
    {
        playerA = null!;
        playerB = null!;
        if(pairIndex < 0 || pairIndex >= Step1PartyPairCount || _infos.Count != PartyPlayerCount)
            return false;

        var (indexA, indexB) = Step1PartyPairIndices[pairIndex];
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

        if(!ImGui.BeginTable($"##12384567PairInfos{title}", 6,
               ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            return;

        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 20f);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 200f);
        ImGui.TableSetupColumn("Half", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Debuff", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Role", ImGuiTableColumnFlags.WidthFixed, 200f);
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

    private bool IsSupportPlayer(IPlayerCharacter player)
        => GetPriorityIndex(player) is >= 0 and < 4;

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

        var stackPairs = new List<(PlayerInfo Stack, PlayerInfo Partner)>();
        var nonStackPairPlayers = new List<PlayerInfo>();

        for(var pairIndex = 0; pairIndex < Step1PartyPairCount; pairIndex++)
        {
            if(!TryGetStep1Pair(pairIndex, out var playerA, out var playerB))
                return false;

            if(!TryClassifyStep1Pair(playerA, playerB, out var stackPlayer, out var partnerPlayer,
                    out var isStackPair))
                return false;

            if(isStackPair)
                stackPairs.Add((stackPlayer, partnerPlayer));
            else
            {
                nonStackPairPlayers.Add(playerA);
                nonStackPairPlayers.Add(playerB);
            }
        }

        if(stackPairs.Count != StackPairCount || nonStackPairPlayers.Count != NonStackPairCount * 2)
            return false;

        PlayerInfo? spreadStackPair = null;
        PlayerInfo? spreadPartner = null;
        PlayerInfo? coneStackPair = null;
        PlayerInfo? conePartner = null;

        foreach(var (stack, partner) in stackPairs)
        {
            switch(partner.Debuff)
            {
                case DebuffKind.Spread:
                    if(spreadStackPair != null)
                        return false;
                    spreadStackPair = stack;
                    spreadPartner = partner;
                    break;
                case DebuffKind.Cone:
                    if(coneStackPair != null)
                        return false;
                    coneStackPair = stack;
                    conePartner = partner;
                    break;
                default:
                    return false;
            }
        }

        if(spreadStackPair == null || spreadPartner == null || coneStackPair == null || conePartner == null)
            return false;

        if(!TryApplyFirstHalfSpreadPair(spreadStackPair, spreadPartner)
            || !TryApplyFirstHalfConePair(coneStackPair, conePartner)
            || !TryApplySecondHalfFromNonStackPairs(nonStackPairPlayers))
            return false;

        return _infos.All(i => i.Half != MechanicHalf.None && i.RoleLabel != null);
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

    private bool TryApplyFirstHalfSpreadPair(PlayerInfo stackPlayer, PlayerInfo spreadPlayer)
    {
        if(stackPlayer.Debuff != DebuffKind.Stack || spreadPlayer.Debuff != DebuffKind.Spread)
            return false;

        var supportStack = IsSupportPlayer(stackPlayer.Player);
        stackPlayer.Half = MechanicHalf.First;
        stackPlayer.RoleLabel = supportStack ? "211_LeftTowerStack" : "211_RightTowerStack";
        spreadPlayer.Half = MechanicHalf.First;
        spreadPlayer.RoleLabel = "211_RightTowerSpread";
        return true;
    }

    private bool TryApplyFirstHalfConePair(PlayerInfo stackPlayer, PlayerInfo conePlayer)
    {
        if(stackPlayer.Debuff != DebuffKind.Stack || conePlayer.Debuff != DebuffKind.Cone)
            return false;

        var supportStack = IsSupportPlayer(stackPlayer.Player);
        stackPlayer.Half = MechanicHalf.First;
        stackPlayer.RoleLabel = supportStack ? "211_LeftTowerStack" : "211_RightTowerStack";
        conePlayer.Half = MechanicHalf.First;
        conePlayer.RoleLabel = "211_LeftTowerCone";
        return true;
    }

    // Assigns second-half outside roles by priority across both non-stack pairs.
    private bool TryApplySecondHalfFromNonStackPairs(IReadOnlyList<PlayerInfo> players)
    {
        if(players.Count != NonStackPairCount * 2)
            return false;

        var ordered = OrderInfosByPriority(players).ToList();
        ordered[0].Half = MechanicHalf.Second;
        ordered[0].RoleLabel = "211_LeftTowerStackOutside";
        ordered[1].Half = MechanicHalf.Second;
        ordered[1].RoleLabel = "211_LeftTowerBaitCone";
        ordered[2].Half = MechanicHalf.Second;
        ordered[2].RoleLabel = "211_RightTowerStackOutside";
        ordered[3].Half = MechanicHalf.Second;
        ordered[3].RoleLabel = "211_RightTowerStackOutside";
        return true;
    }

    private void ResolvePattern(int patternId)
    {
        foreach(var info in _infos)
            info.RoleLabel = ResolvePatternRole(patternId, info);
    }

    private string? ResolvePatternRole(int patternId, PlayerInfo info)
    {
        return patternId switch
        {
            0 => ResolvePattern211(info),
            1 => ResolvePattern022(info),
            _ => null,
        };
    }

    private string? ResolvePattern211(PlayerInfo info)
    {
        if(IsTower(info))
        {
            return info.Debuff switch
            {
                DebuffKind.Stack => GetPriorityRank(
                    _infos.Where(i => IsTower(i) && i.Debuff == DebuffKind.Stack), info) switch
                {
                    0 => "211_LeftTowerStack",
                    1 => "211_RightTowerStack",
                    _ => null,
                },
                DebuffKind.Spread => "211_RightTowerSpread",
                DebuffKind.Cone => "211_LeftTowerCone",
                _ => null,
            };
        }

        return GetPriorityRank(_infos.Where(i => !IsTower(i)), info) switch
        {
            0 => "211_LeftTowerStackOutside",
            1 => "211_LeftTowerBaitCone",
            2 or 3 => "211_RightTowerStackOutside",
            _ => null,
        };
    }

    private string? ResolvePattern022(PlayerInfo info)
    {
        if(IsTower(info))
        {
            if(info.Debuff == DebuffKind.Spread)
            {
                return GetPriorityRank(
                    _infos.Where(i => IsTower(i) && i.Debuff == DebuffKind.Spread), info) switch
                {
                    0 => "022_LeftTowerSpread",
                    1 => "022_RightTowerSpread",
                    _ => null,
                };
            }

            if(info.Debuff == DebuffKind.Cone)
            {
                return GetPriorityRank(
                    _infos.Where(i => IsTower(i) && i.Debuff == DebuffKind.Cone), info) switch
                {
                    0 => "022_LeftTowerCone",
                    1 => "022_RightTowerCone",
                    _ => null,
                };
            }

            return null;
        }

        return GetPriorityRank(_infos.Where(i => !IsTower(i)), info) switch
        {
            0 => "022_LeftHelper",
            1 => "022_LeftBait",
            2 => "022_RightBait",
            3 => "022_RightHelper",
            _ => null,
        };
    }

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

        if(_step == Step1DisplayStep && !_initialGroupResolved)
        {
            if(!TryResolveInitialGroup())
                return false;

            _initialGroupResolved = true;
        }

        if(_step == Step1DisplayStep)
        {
            if(!_initialGroupResolved)
                return false;

            var info = GetBasePlayerInfo();
            if(info?.RoleLabel == null)
                return false;

            patternId = 0;
            roleLabel = info.RoleLabel;
            return true;
        }

        if(!_initialGroupResolved)
            return false;

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
