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

internal class P2_Missing_HalfSwap_Strat : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(10, "mirage");
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

    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);
    private static readonly Vector3 TrueNorth = new(100f, 0f, 120f);
    private const float AngleTolerance = 22.5f;
    private const float DefaultRangeInside = 3.25f;
    private const float DefaultRangeOutside = 4.75f;
    private const int PatternCount = 5;
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
    private const int GroupPlayerCount = 4;

    private static readonly string[] BasisComboLabels = ["LeftTower", "RightTower", "Center"];

    private static readonly (StratCell Cell, string? NameA, string? NameB)[] StratSpotMeta =
    [
        (StratCell.TopLeft, "Top.Left", "Left.Top"),
        (StratCell.TopCenter, "Top.Center", null),
        (StratCell.TopRight, "Top.Right", null),
        (StratCell.RightTop, null, "Right.Top"),
        (StratCell.RightMiddle, null, "Right.Middle"),
        (StratCell.RightBottom, null, "Right.Bottom"),
        (StratCell.BottomRight, "Bottom.Right", null),
        (StratCell.BottomCenter, "Bottom.Center", null),
        (StratCell.BottomLeft, "Bottom.Left", null),
        (StratCell.LeftBottom, null, "Left.Bottom"),
        (StratCell.LeftMiddle, null, "Left.Middle"),
        (StratCell.LeftTop, null, "Left.Top"),
    ];

    private static readonly float[] DefaultSpotRelativeAngles =
    [
        330f, 0f, 30f, 60f, 90f, 120f, 150f, 180f, 210f, 240f, 270f, 300f,
    ];

    private static readonly string[] DebugPatternPreviewLabels =
    [
        "None",
        "Pattern 0 (1/0/3)",
        "Pattern 1 (1/3/0)",
        "Pattern 2 (2/1/1)",
        "Pattern 3 (0/2/2)",
        "Pattern 4 (4/0/0)",
    ];

    private static readonly PatternDefinition[] Patterns =
    [
        new(0, 1, 0, 3,
        [
            RuleFromGridDefaults("debuff stack", TowerRole.Left, StratCell.LeftBottom, true),
            RuleFromGridDefaults("debuff cone priority1", TowerRole.Left, StratCell.LeftTop, true),
            RuleFromGridDefaults("debuff cone priority2", TowerRole.Right, StratCell.LeftMiddle, true),
            RuleFromGridDefaults("debuff cone priority3", TowerRole.Right, StratCell.LeftTop, true),
            RuleFromGridDefaults("bait cone", TowerRole.Left, StratCell.TopLeft, false),
            RuleFromGridDefaults("stack", TowerRole.Left, StratCell.LeftBottom, false),
        ]),
        new(1, 1, 3, 0,
        [
            RuleFromGridDefaults("debuff stack", TowerRole.Left, StratCell.LeftBottom, true),
            RuleFromGridDefaults("debuff spread priority1", TowerRole.Left, StratCell.RightMiddle, true),
            RuleFromGridDefaults("debuff spread priority2", TowerRole.Right, StratCell.BottomLeft, true),
            RuleFromGridDefaults("debuff spread priority3", TowerRole.Right, StratCell.TopRight, true),
            RuleFromGridDefaults("stack", TowerRole.Left, StratCell.LeftBottom, false),
        ]),
        new(2, 2, 1, 1,
        [
            RuleFromGridDefaults("debuff stack priority1", TowerRole.Left, StratCell.RightTop, true),
            RuleFromGridDefaults("debuff stack priority2", TowerRole.Right, StratCell.RightBottom, true),
            RuleFromGridDefaults("debuff cone", TowerRole.Left, StratCell.TopLeft, true),
            RuleFromGridDefaults("debuff spread", TowerRole.Right, StratCell.LeftMiddle, true),
            RuleFromGridDefaults("bait cone", TowerRole.Left, StratCell.TopLeft, false),
            RuleFromGridDefaults("stack alone", TowerRole.Left, StratCell.RightTop, false),
            RuleFromGridDefaults("stack other", TowerRole.Right, StratCell.RightBottom, false),
        ]),
        new(3, 0, 2, 2,
        [
            RuleFromGridDefaults("debuff cone priority1", TowerRole.Left, StratCell.TopRight, true),
            RuleFromGridDefaults("debuff cone priority2", TowerRole.Left, StratCell.TopCenter, true),
            RuleFromGridDefaults("debuff spread priority1", TowerRole.Right, StratCell.LeftMiddle, true),
            RuleFromGridDefaults("debuff spread priority2", TowerRole.Right, StratCell.RightMiddle, true),
            Rule("demise spread priority1", PositionBasis.Center, 0f, 0f),
            Rule("demise spread priority2", PositionBasis.Center, 0f, 5f),
            Rule("demise spread priority3", PositionBasis.Center, 45f, 5f),
            Rule("demise spread priority4", PositionBasis.Center, 315f, 5f),
        ]),
        new(4, 4, 0, 0,
        [
            Rule("debuff cone priority1", PositionBasis.LeftTower, 90f, 3.25f),
            Rule("debuff cone priority2", PositionBasis.LeftTower, 270f, 3.25f),
            Rule("debuff spread priority1", PositionBasis.RightTower, 90f, 3.25f),
            Rule("debuff spread priority2", PositionBasis.RightTower, 270f, 3.25f),
            Rule("demise spread priority1", PositionBasis.Center, 0f, 0f),
            Rule("demise spread priority2", PositionBasis.Center, 0f, 5f),
            Rule("demise spread priority3", PositionBasis.Center, 45f, 5f),
            Rule("demise spread priority4", PositionBasis.Center, 315f, 5f),
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

    private enum TowerRole
    {
        Right,
        Left,
    }

    private enum StratCell
    {
        TopLeft,
        TopCenter,
        TopRight,
        RightTop,
        RightMiddle,
        RightBottom,
        BottomRight,
        BottomCenter,
        BottomLeft,
        LeftBottom,
        LeftMiddle,
        LeftTop,
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

    private sealed class HalfSwapPriorityData4 : PriorityData
    {
        public override int GetNumPlayers() => GroupPlayerCount;
    }

    private sealed class HalfSwapPriorityData2 : PriorityData
    {
        public override int GetNumPlayers() => 2;
    }

    private readonly record struct TowerHit(string Label, Vector3 SlotPosition, uint EntityId);

    private readonly record struct HalfSwapPhaseContext(
        bool IsFirstHalf,
        HalfSwapPriorityData4 DebuffResolveGroup,
        HalfSwapPriorityData4 FixedRuleGroup);

    private sealed class PatternRuleSettings
    {
        public int Basis;
        public float AngleDeg;
        public float Range;
    }

    private sealed class Config : IEzConfig
    {
        public HalfSwapPriorityData4 Group1 = new();
        public HalfSwapPriorityData4 Group2 = new();
        public HalfSwapPriorityData2 Pattern0ConeBait = new();
        public HalfSwapPriorityData2 Pattern2ConeBait = new();
        public HalfSwapPriorityData2 Pattern2StackAlone = new();
        public int DebugPatternPreview = DebugPatternPreviewNone;
        public int DebugPatternPreviewRole = DebugPatternPreviewRoleNone;
        public PatternRuleSettings[][] PatternRules = CreateDefaultPatternRules();

        public void EnsureDefaults()
        {
            Group1 ??= new HalfSwapPriorityData4();
            Group2 ??= new HalfSwapPriorityData4();
            Pattern0ConeBait ??= new HalfSwapPriorityData2();
            Pattern2ConeBait ??= new HalfSwapPriorityData2();
            Pattern2StackAlone ??= new HalfSwapPriorityData2();
            if(DebugPatternPreview >= PatternCount)
                DebugPatternPreview = DebugPatternPreviewNone;
            DebugPatternPreviewRole = ClampPatternPreviewRole(DebugPatternPreview, DebugPatternPreviewRole);
            PatternRules = EnsurePatternRules(PatternRules);
        }

        public void ResetToDefaults()
        {
            Group1 = new HalfSwapPriorityData4();
            Group2 = new HalfSwapPriorityData4();
            Pattern0ConeBait = new HalfSwapPriorityData2();
            Pattern2ConeBait = new HalfSwapPriorityData2();
            Pattern2StackAlone = new HalfSwapPriorityData2();
            DebugPatternPreview = DebugPatternPreviewNone;
            DebugPatternPreviewRole = DebugPatternPreviewRoleNone;
            PatternRules = CreateDefaultPatternRules();
        }
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
        if(ImGui.BeginTabBar("##P2HalfSwapSettings"))
        {
            if(ImGui.BeginTabItem("Configuration###tabMain"))
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

    private void DrawDebugTab()
    {
        DrawDebugStepSection();
        ImGui.Spacing();
        DrawDebugGroupDebuffsSection();
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
        if(_hasActiveTowers && _step is >= 1 and <= 8)
        {
            ImGui.TextUnformatted(TryGetLiveResolvePattern(out var patternId)
                ? $"Live pattern: {patternId}"
                : "Live pattern: — (no match)");
        }
    }

    private void DrawDebugGroupDebuffsSection()
    {
        C.EnsureDefaults();
        ImGui.TextUnformatted("Group debuffs (live)");
        ImGui.Separator();
        DrawGroupDebuffDebug("Group1 (resolve step 1-4)", C.Group1);
        DrawGroupDebuffDebug("Group2 (resolve step 5-8)", C.Group2);
    }

    private void DrawGroupDebuffDebug(string title, HalfSwapPriorityData4 group)
    {
        ImGui.TextUnformatted(title);
        var players = OrderByGroupPriority(group, GetGroupPlayers(group)).ToList();
        if(players.Count == 0)
        {
            ImGui.TextUnformatted("  (no players in party)");
            return;
        }

        foreach(var player in players)
            ImGui.TextUnformatted($"  {player.Name}: {FormatDebuffKindDebug(GetDebuffKind(player))}");

        CountDebuffKindsFromPlayers(players, out var stack, out var spread, out var cone);
        ImGui.TextUnformatted($"  counts: stack={stack} spread={spread} cone={cone}");
    }

    private static void CountDebuffKindsFromPlayers(
        IReadOnlyList<IPlayerCharacter> players, out int stack, out int spread, out int cone)
    {
        stack = 0;
        spread = 0;
        cone = 0;
        foreach(var player in players)
        {
            switch(GetDebuffKind(player))
            {
                case DebuffKind.Stack: stack++; break;
                case DebuffKind.Spread: spread++; break;
                case DebuffKind.Cone: cone++; break;
            }
        }
    }

    private static string FormatDebuffKindDebug(DebuffKind kind)
        => kind switch
        {
            DebuffKind.Stack => "Stack (5084)",
            DebuffKind.Spread => "Spread (5085)",
            DebuffKind.Cone => "Cone (5086)",
            _ => "—",
        };

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

    #endregion

    #region Settings UI

    private bool IsPhaseActive()
        => Controller.Scene == SceneP2;

    private void DrawPrioritySettings()
    {
        C.EnsureDefaults();
        ImGui.TextUnformatted("Priority");
        ImGui.Separator();

        if (ImGui.TreeNode("Groups")) {
            ImGui.TextUnformatted("Group1 (debuff resolve step 1-4)");
            C.Group1.Draw();
            ImGui.TextUnformatted("Group2 (debuff resolve step 5-8)");
            C.Group2.Draw();
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Role Selection")) {
            ImGui.TextUnformatted("Pattern 0 cone bait");
            C.Pattern0ConeBait.Draw();
            ImGui.TextUnformatted("Pattern 2 cone bait");
            C.Pattern2ConeBait.Draw();
            ImGui.TextUnformatted("Pattern 2 stack alone");
            C.Pattern2StackAlone.Draw();
            ImGui.TreePop();
        }

        ImGui.Spacing();
   
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

    private void DrawAllPatternAssignmentTables()
    {
        C.EnsureDefaults();

        ImGui.TextUnformatted($"Pattern assignments ({PatternCount} patterns)");
        ImGui.Separator();

        foreach(var pattern in Patterns)
        {
            var header =
                $"Pattern {pattern.Id} (stack={pattern.StackCount} spread={pattern.SpreadCount} cone={pattern.ConeCount})###patDef{pattern.Id}";
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

        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 200f);
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

    private void OnPatternRulesChanged()
        => UpdateFieldMarkers();

    private static bool IsPatternPreviewActive(Config config)
        => IsPatternPreviewActive(config.DebugPatternPreview);

    private bool IsPatternPreviewActive()
        => IsPatternPreviewActive(C);

    private static int DebugPatternPreviewToComboIndex(int preview)
        => preview < 0 ? 0 : preview + 1;

    private static int ComboIndexToDebugPatternPreview(int comboIndex)
        => comboIndex <= 0 ? DebugPatternPreviewNone : comboIndex - 1;

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

        if(!TryGetLiveResolvePattern(out var patternId))
            return;

        if(!TryResolveLiveAssignment(patternId, out var assignmentIndex, out var label))
            return;

        if(ResolvePositionRule(GetConfiguredRule(patternId, assignmentIndex)) is not { } position)
            return;

        EnableRoleMarker(ElMyRole, position, label, tether: true);
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

    /// <summary>塔から中央を見たときの 0°（真北基準のコンパス角）。</summary>
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

    private static PatternAssignment RuleFromGridDefaults(string label, TowerRole tower, StratCell cell, bool inside)
    {
        GetDefaultGridSpotRule(tower, cell, inside, out var basis, out var angleDeg, out var range);
        return Rule(label, basis, angleDeg, range);
    }

    private static void GetDefaultGridSpotRule(
        TowerRole tower, StratCell cell, bool inside,
        out PositionBasis basis, out float angleDeg, out float range)
    {
        basis = TowerRoleToBasis(tower);
        var cellIndex = Array.FindIndex(StratSpotMeta, m => m.Cell == cell);
        angleDeg = cellIndex >= 0 ? DefaultSpotRelativeAngles[cellIndex] : 0f;
        range = inside ? DefaultRangeInside : DefaultRangeOutside;
    }

    private static PositionBasis TowerRoleToBasis(TowerRole role)
        => role == TowerRole.Right ? PositionBasis.RightTower : PositionBasis.LeftTower;

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

    private List<IPlayerCharacter> GetGroupPlayers(HalfSwapPriorityData4 data)
    {
        var priority = data.GetPlayers(_ => true);
        if(priority == null || priority.Count != GroupPlayerCount)
            return [];

        var names = priority.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        return Controller.GetPartyMembers()
            .Where(p => names.Contains(p.Name.ToString()))
            .ToList();
    }

    private bool IsLocalInGroup(HalfSwapPriorityData4 data)
        => data.GetOwnIndex(_ => true) >= 0;

    private bool IsLocalPrioritySlot(HalfSwapPriorityData2 data, int position = 1)
    {
        if(BasePlayer == null)
            return false;
        var slot = data.GetPlayer(_ => true, position);
        return slot != null && slot.IGameObject.AddressEquals(BasePlayer);
    }

    private static IEnumerable<IPlayerCharacter> OrderByGroupPriority(
        HalfSwapPriorityData4 data, IEnumerable<IPlayerCharacter> players)
    {
        var priorityList = data.GetPlayers(_ => true);
        if(priorityList == null)
            return players.OrderBy(x => x.EntityId);

        return players.OrderBy(p =>
        {
            var name = p.Name.ToString();
            for(var i = 0; i < priorityList.Count; i++)
            {
                if(priorityList[i].Name == name)
                    return i;
            }

            return int.MaxValue;
        }).ThenBy(x => x.EntityId);
    }

    private bool TryBuildPhaseContext(int step, out HalfSwapPhaseContext phase)
    {
        if(step is < 1 or > 8)
        {
            phase = default;
            return false;
        }

        var isFirstHalf = step <= 4;
        phase = new HalfSwapPhaseContext(
            isFirstHalf,
            isFirstHalf ? C.Group1 : C.Group2,
            isFirstHalf ? C.Group2 : C.Group1);
        return true;
    }

    private static bool TryDetectPattern(IReadOnlyList<IPlayerCharacter> groupPlayers, out int patternId)
    {
        patternId = -1;
        if(groupPlayers.Count != GroupPlayerCount)
            return false;

        var stack = 0;
        var spread = 0;
        var cone = 0;
        foreach(var player in groupPlayers)
        {
            switch(GetDebuffKind(player))
            {
                case DebuffKind.Stack:
                    stack++;
                    break;
                case DebuffKind.Spread:
                    spread++;
                    break;
                case DebuffKind.Cone:
                    cone++;
                    break;
                default:
                    return false;
            }
        }

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

    private bool TryGetLiveResolvePattern(out int patternId)
    {
        patternId = -1;
        if(!_hasActiveTowers || _step is < 1 or > 8)
            return false;

        C.EnsureDefaults();
        if(!TryBuildPhaseContext(_step, out var phase))
            return false;

        return TryDetectPattern(GetGroupPlayers(phase.DebuffResolveGroup), out patternId);
    }

    private bool TryResolveLiveAssignment(int patternId, out int assignmentIndex, out string label)
    {
        assignmentIndex = -1;
        label = "";

        if(patternId is < 0 or >= PatternCount)
            return false;

        if(BasePlayer == null)
            return false;

        C.EnsureDefaults();
        if(!TryBuildPhaseContext(_step, out var phase))
            return false;

        var resolvePlayers = GetGroupPlayers(phase.DebuffResolveGroup);

        string? roleLabel;
        if(IsLocalInGroup(phase.DebuffResolveGroup))
            roleLabel = ResolveDebuffGroupRole(patternId, phase.DebuffResolveGroup, BasePlayer, resolvePlayers);
        else if(IsLocalInGroup(phase.FixedRuleGroup))
            roleLabel = ResolveFixedGroupRole(patternId, phase.FixedRuleGroup);
        else
            return false;

        if(roleLabel == null)
            return false;

        assignmentIndex = FindAssignmentIndex(patternId, roleLabel);
        if(assignmentIndex < 0)
            return false;

        label = roleLabel;
        return true;
    }

    private static int FindAssignmentIndex(int patternId, string roleLabel)
        => Array.FindIndex(Patterns[patternId].Assignments, a => a.Label == roleLabel);

    private static string? ResolveDebuffGroupRole(
        int patternId, HalfSwapPriorityData4 group, IPlayerCharacter local,
        IReadOnlyList<IPlayerCharacter> resolvePlayers)
    {
        var myDebuff = GetDebuffKind(local);
        if(myDebuff == DebuffKind.None)
            return null;

        if(patternId == 4)
        {
            if(myDebuff != DebuffKind.Stack)
                return null;

            var ordered = OrderByGroupPriority(group, resolvePlayers).ToList();
            return ordered.FindIndex(p => p.EntityId == local.EntityId) switch
            {
                0 => "debuff cone priority1",
                1 => "debuff cone priority2",
                2 => "debuff spread priority1",
                3 => "debuff spread priority2",
                _ => null,
            };
        }

        var sameDebuff = OrderByGroupPriority(group,
                resolvePlayers.Where(p => GetDebuffKind(p) == myDebuff))
            .ToList();
        var rank = sameDebuff.FindIndex(p => p.EntityId == local.EntityId);
        if(rank < 0)
            return null;

        return patternId switch
        {
            0 => myDebuff switch
            {
                DebuffKind.Stack when rank == 0 => "debuff stack",
                DebuffKind.Cone when rank is >= 0 and <= 2 => $"debuff cone priority{rank + 1}",
                _ => null,
            },
            1 => myDebuff switch
            {
                DebuffKind.Stack when rank == 0 => "debuff stack",
                DebuffKind.Spread when rank is >= 0 and <= 2 => $"debuff spread priority{rank + 1}",
                _ => null,
            },
            2 => myDebuff switch
            {
                DebuffKind.Stack when rank is >= 0 and <= 1 => $"debuff stack priority{rank + 1}",
                DebuffKind.Cone when rank == 0 => "debuff cone",
                DebuffKind.Spread when rank == 0 => "debuff spread",
                _ => null,
            },
            3 => myDebuff switch
            {
                DebuffKind.Cone when rank is >= 0 and <= 1 => $"debuff cone priority{rank + 1}",
                DebuffKind.Spread when rank is >= 0 and <= 1 => $"debuff spread priority{rank + 1}",
                _ => null,
            },
            _ => null,
        };
    }

    private string? ResolveFixedGroupRole(int patternId, HalfSwapPriorityData4 ownGroup)
    {
        return patternId switch
        {
            0 => IsLocalPrioritySlot(C.Pattern0ConeBait) ? "bait cone" : "stack",
            1 => "stack",
            2 when IsLocalPrioritySlot(C.Pattern2ConeBait) => "bait cone",
            2 when IsLocalPrioritySlot(C.Pattern2StackAlone) => "stack alone",
            2 => "stack other",
            3 or 4 => ResolveDemiseLabel(ownGroup),
            _ => null,
        };
    }

    private static string? ResolveDemiseLabel(HalfSwapPriorityData4 group)
    {
        var index = group.GetOwnIndex(_ => true);
        if(index is < 0 or > 3)
            return null;
        return $"demise spread priority{index + 1}";
    }

    #endregion
}
