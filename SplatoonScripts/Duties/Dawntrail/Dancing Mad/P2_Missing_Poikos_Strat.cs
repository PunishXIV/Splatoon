using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
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

internal class P2_Missing_Poikos_Strat : SplatoonScript
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

    private const uint DataIdTower = 0x233C;
    private const float SlotMatchRadius = 0.5f;
    private const int SceneP2 = 7;
    private const int PartyPlayerCount = 8;

    // Step1 ResolveInitialGroup only: priority slots 1-4 and 5-8.
    private const int Step1PartySegmentSize = 4;
    private const int Step1PartySegment1Index = 0;
    private const int Step1PartySegment2Index = 4;

    // MechanicHalf.First / Second tower phase steps.
    private const int FirstHalfStepMin = 1;
    private const int FirstHalfStepMax = 4;
    private const int SecondHalfStepMin = 5;
    private const int SecondHalfStepMax = 8;

    private const int ActiveStepMin = FirstHalfStepMin;
    private const int ActiveStepMax = SecondHalfStepMax;

    private const int InitialGroupSpreadCount = 3;
    private const int InitialGroupStackCount = 1;
    private const int InitialGroupConeCount = 3;

    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);
    private static readonly Vector3 TrueNorth = new(100f, 0f, 120f);
    private const float AngleTolerance = 22.5f;
    private const float DefaultRangeInside = 3.25f;
    private const float DefaultRangeOutside = 4.75f;
    private const int PatternCount = 3;
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
        "400 (4/0/0)",
    ];

    private static readonly PatternDefinition[] Patterns =
    [
        new(0, 2, 1, 1,
        [
            Rule("211_LeftTowerStack", PositionBasis.LeftTower, 60f, DefaultRangeInside),
            Rule("211_LeftTowerCone", PositionBasis.LeftTower, 330f, DefaultRangeInside),
            Rule("211_LeftTowerStackOutside", PositionBasis.LeftTower, 60f, DefaultRangeOutside),
            Rule("211_LeftTowerBaitCone", PositionBasis.LeftTower, 330f, DefaultRangeOutside),
            Rule("211_RightTowerStack", PositionBasis.RightTower, 270f, DefaultRangeInside),
            Rule("211_RightTowerSpread", PositionBasis.RightTower, 90f, DefaultRangeInside),
            Rule("211_RightTowerStackOutside", PositionBasis.RightTower, 270f, DefaultRangeOutside),
        ]),
        new(1, 0, 2, 2,
        [
            Rule("022_LeftTowerConeLeft", PositionBasis.LeftTower, 30f, DefaultRangeInside),
            Rule("022_LeftTowerConeRight", PositionBasis.LeftTower, 0f, DefaultRangeInside),
            Rule("022_RightTowerSpreadLeft", PositionBasis.RightTower, 90f, DefaultRangeInside),
            Rule("022_RightTowerSpreadRight", PositionBasis.RightTower, 270f, DefaultRangeInside),
            Rule("022_LeftDemise1", PositionBasis.Center, 315f, 3.5f),
            Rule("022_LeftDemise2", PositionBasis.Center, 225f, 3.5f),
            Rule("022_RightDemise3", PositionBasis.Center, 45f, 3.55f),
            Rule("022_RightDemise4", PositionBasis.Center, 135f, 3.5f),
        ]),
        new(2, 4, 0, 0,
        [
            Rule("400_LeftTowerStackLeft", PositionBasis.LeftTower, 90f, DefaultRangeInside),
            Rule("400_LeftTowerStackRight", PositionBasis.LeftTower, 270f, DefaultRangeInside),
            Rule("400_RightTowerStackLeft", PositionBasis.RightTower, 90f, DefaultRangeInside),
            Rule("400_RightTowerStackRight", PositionBasis.RightTower, 270f, DefaultRangeInside),
            Rule("400_LeftDemise1", PositionBasis.Center, 315f, 3.5f),
            Rule("400_LeftDemise2", PositionBasis.Center, 225f, 3.5f),
            Rule("400_RightDemise3", PositionBasis.Center, 45f, 3.55f),
            Rule("400_RightDemise4", PositionBasis.Center, 135f, 3.5f),
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
    private readonly HashSet<uint> _knownTowerEntityIds = [];
    private bool _hasActiveTowers;
    private int _step;
    private Tower1Side _tower1Side = Tower1Side.Unknown;
    private readonly List<PlayerInfo> _infos = [];
    private bool _initialGroupResolved;

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

        UpdateActiveTowers();
        UpdateActiveTowerMarkers();
        UpdateFieldMarkers();
    }

    public override void OnReset()
        => ResetState();

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe))
            ResetState();
    }

    public override void OnSettingsDraw()
    {
        if(ImGui.BeginTabBar("##P2PoikosSettings"))
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
            $"Priority — slots {Step1PartySegment1Index + 1}-{Step1PartySegment1Index + Step1PartySegmentSize} and " +
            $"{Step1PartySegment2Index + 1}-{Step1PartySegment2Index + Step1PartySegmentSize} are used for Step1 initial group only.");

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
        ImGui.TextUnformatted(_initialGroupResolved ? "Initial group: resolved" : "Initial group: —");
        if(_hasActiveTowers && _step is >= ActiveStepMin and <= ActiveStepMax)
        {
            ImGui.TextUnformatted(TryDetectPartyPattern(out var patternId)
                ? $"Live pattern: {DebugPatternPreviewLabels[patternId + 1]}"
                : "Live pattern: — (no match)");
        }
    }

    private void DrawDebugInfosSection()
    {
        C.EnsureDefaults();
        ImGui.TextUnformatted("Player infos (live)");
        ImGui.Separator();

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
            ImGui.TextUnformatted($"Unresolved (step 1): {unresolvedCount} player(s)");

        CountDebuffKindsFromTowerInfos(out var stack, out var spread, out var cone);
        ImGui.TextUnformatted($"Tower counts: stack={stack} spread={spread} cone={cone}");
    }

    private IReadOnlyList<PlayerInfo> GetStep1PartySegment(int segmentIndex)
    {
        if(_infos.Count != PartyPlayerCount)
            return [];

        var start = segmentIndex switch
        {
            0 => Step1PartySegment1Index,
            1 => Step1PartySegment2Index,
            _ => -1,
        };
        if(start < 0)
            return [];

        return _infos.Skip(start).Take(Step1PartySegmentSize).ToList();
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

        if(!ImGui.BeginTable($"##poikosInfos{title}", 6,
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

    private void UpdateActiveTowers()
    {
        var hits = ScanTowers();

        if(hits.Count == 0)
        {
            ResetState();
            return;
        }

        if(hits.Count == 2 && !_hasActiveTowers)
        {
            SetActiveTowers(hits);
            return;
        }

        if(hits.Count >= 4 && _hasActiveTowers)
        {
            var currentIds = hits.Select(x => x.EntityId).ToHashSet();
            var newIds = currentIds.Except(_knownTowerEntityIds).ToHashSet();
            if(newIds.Count == 2)
            {
                var newHits = hits.Where(x => newIds.Contains(x.EntityId)).ToList();
                SetActiveTowers(newHits);
            }
        }
    }

    private void SetActiveTowers(List<TowerHit> hits)
    {
        if(hits.Count != 2)
            return;

        _activeTowerPositions[0] = hits[0].SlotPosition;
        _activeTowerPositions[1] = hits[1].SlotPosition;
        _activeTowerEntityIds[0] = hits[0].EntityId;
        _activeTowerEntityIds[1] = hits[1].EntityId;
        _knownTowerEntityIds.Add(hits[0].EntityId);
        _knownTowerEntityIds.Add(hits[1].EntityId);
        _hasActiveTowers = true;

        _step++;
        _tower1Side = ResolveTower1Side(hits[0].SlotPosition, hits[1].SlotPosition);
    }

    private List<TowerHit> ScanTowers()
    {
        var slotBest = new Dictionary<int, (float Distance, TowerHit Hit)>();

        foreach(var obj in Svc.Objects.Where(x => x.DataId == DataIdTower))
        {
            var bestSlotIndex = -1;
            var bestDistance = float.MaxValue;

            for(var i = 0; i < TowerSlots.Length; i++)
            {
                var distance = Distance2d(obj.Position, TowerSlots[i].Position);
                if(distance > SlotMatchRadius || distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestSlotIndex = i;
            }

            if(bestSlotIndex < 0)
                continue;

            var slot = TowerSlots[bestSlotIndex];
            var hit = new TowerHit(slot.Label, slot.Position, obj.EntityId);

            if(!slotBest.TryGetValue(bestSlotIndex, out var existing) || bestDistance < existing.Distance)
                slotBest[bestSlotIndex] = (bestDistance, hit);
        }

        return slotBest.Values
            .Select(x => x.Hit)
            .OrderBy(x => Array.FindIndex(TowerSlots, s => s.Label == x.Label))
            .ToList();
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
        _knownTowerEntityIds.Clear();
        _activeTowerPositions[0] = Vector3.Zero;
        _activeTowerPositions[1] = Vector3.Zero;
        _activeTowerEntityIds[0] = 0;
        _activeTowerEntityIds[1] = 0;
        _infos.Clear();
        _initialGroupResolved = false;

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

    private void CountDebuffKindsFromInfos(out int stack, out int spread, out int cone)
    {
        stack = 0;
        spread = 0;
        cone = 0;
        foreach(var info in _infos)
        {
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

        CountDebuffKindsFromInfos(out var stack, out var spread, out var cone);

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
        => (info.Half == MechanicHalf.First && _step is >= FirstHalfStepMin and <= FirstHalfStepMax)
           || (info.Half == MechanicHalf.Second && _step is >= SecondHalfStepMin and <= SecondHalfStepMax);

    private bool TryResolveInitialGroup()
    {
        if(!TryEnsureInfos())
            return false;

        UpdateDebuffs();

        var firstHalfGroup = GetStep1PartySegment(0).ToList();
        var secondHalfGroup = GetStep1PartySegment(1).ToList();

        if(!TryResolveInitialGroupSegment(firstHalfGroup) || !TryResolveInitialGroupSegment(secondHalfGroup))
            return false;

        return _infos.All(i => i.Half != MechanicHalf.None && i.RoleLabel != null);
    }

    private bool TryResolveInitialGroupSegment(IReadOnlyList<PlayerInfo> group)
    {
        if(group.Count != Step1PartySegmentSize)
            return false;

        CountDebuffKinds(group, out var stack, out var spread, out var cone);

        if(spread == InitialGroupSpreadCount && stack == InitialGroupStackCount)
            return ApplySpreadInitialGroup(group);
        if(cone == InitialGroupConeCount && stack == InitialGroupStackCount)
            return ApplyConeInitialGroup(group);

        return false;
    }

    private static void CountDebuffKinds(IReadOnlyList<PlayerInfo> group, out int stack, out int spread, out int cone)
    {
        stack = 0;
        spread = 0;
        cone = 0;
        foreach(var info in group)
        {
            switch(info.Debuff)
            {
                case DebuffKind.Stack: stack++; break;
                case DebuffKind.Spread: spread++; break;
                case DebuffKind.Cone: cone++; break;
            }
        }
    }

    private bool ApplySpreadInitialGroup(IReadOnlyList<PlayerInfo> group)
    {
        var stackPlayer = group.FirstOrDefault(i => i.Debuff == DebuffKind.Stack);
        if(stackPlayer == null)
            return false;

        stackPlayer.Half = MechanicHalf.First;
        stackPlayer.RoleLabel = "211_RightTowerStack";

        var spreadPlayers = OrderInfosByPriority(group.Where(i => i.Debuff == DebuffKind.Spread)).ToList();
        if(spreadPlayers.Count != InitialGroupSpreadCount)
            return false;

        spreadPlayers[0].Half = MechanicHalf.First;
        spreadPlayers[0].RoleLabel = "211_RightTowerSpread";
        spreadPlayers[1].Half = MechanicHalf.Second;
        spreadPlayers[1].RoleLabel = "211_RightTowerStackOutside";
        spreadPlayers[2].Half = MechanicHalf.Second;
        spreadPlayers[2].RoleLabel = "211_RightTowerStackOutside";

        return true;
    }

    private bool ApplyConeInitialGroup(IReadOnlyList<PlayerInfo> group)
    {
        var stackPlayer = group.FirstOrDefault(i => i.Debuff == DebuffKind.Stack);
        if(stackPlayer == null)
            return false;

        stackPlayer.Half = MechanicHalf.First;
        stackPlayer.RoleLabel = "211_LeftTowerStack";

        var conePlayers = OrderInfosByPriority(group.Where(i => i.Debuff == DebuffKind.Cone)).ToList();
        if(conePlayers.Count != InitialGroupConeCount)
            return false;

        conePlayers[0].Half = MechanicHalf.First;
        conePlayers[0].RoleLabel = "211_LeftTowerCone";
        conePlayers[1].Half = MechanicHalf.Second;
        conePlayers[1].RoleLabel = "211_LeftTowerStackOutside";
        conePlayers[2].Half = MechanicHalf.Second;
        conePlayers[2].RoleLabel = "211_LeftTowerBaitCone";

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
            2 => ResolvePattern400(info),
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
                    0 => "022_RightTowerSpreadLeft",
                    1 => "022_RightTowerSpreadRight",
                    _ => null,
                };
            }

            if(info.Debuff == DebuffKind.Cone)
            {
                return GetPriorityRank(
                    _infos.Where(i => IsTower(i) && i.Debuff == DebuffKind.Cone), info) switch
                {
                    0 => "022_LeftTowerConeLeft",
                    1 => "022_LeftTowerConeRight",
                    _ => null,
                };
            }

            return null;
        }

        return GetPriorityRank(_infos.Where(i => !IsTower(i)), info) switch
        {
            0 => "022_LeftDemise1",
            1 => "022_LeftDemise2",
            2 => "022_RightDemise3",
            3 => "022_RightDemise4",
            _ => null,
        };
    }

    private string? ResolvePattern400(PlayerInfo info)
    {
        if(IsTower(info))
        {
            if(info.Debuff != DebuffKind.Stack)
                return null;

            return GetPriorityRank(
                _infos.Where(i => IsTower(i) && i.Debuff == DebuffKind.Stack), info) switch
            {
                0 => "400_RightTowerStackLeft",
                1 => "400_RightTowerStackRight",
                2 => "400_LeftTowerStackLeft",
                3 => "400_LeftTowerStackRight",
                _ => null,
            };
        }

        return GetPriorityRank(_infos.Where(i => !IsTower(i)), info) switch
        {
            0 => "400_LeftDemise1",
            1 => "400_LeftDemise2",
            2 => "400_RightDemise3",
            3 => "400_RightDemise4",
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

        if(_step == FirstHalfStepMin && !_initialGroupResolved)
        {
            if(!TryResolveInitialGroup())
                return false;

            _initialGroupResolved = true;
        }

        if(_step == FirstHalfStepMin)
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
