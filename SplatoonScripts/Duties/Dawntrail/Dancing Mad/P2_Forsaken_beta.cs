using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate;

public class P2_Forsaken_beta : SplatoonScript
{
    private const uint TerritoryDancingMadUltimate = 1363;
    private const uint Forsaken = 47804;
    private const uint UltimateEmbrace = 49740;
    private const uint FuturesEndCast = 47826;
    private const uint PastsEndCast = 47827;
    private const uint AllThingsEndingCast1 = 47836;
    private const uint AllThingsEndingCast2 = 47837;
    private const uint MissingInventoryStatus = 5083;
    private const uint MissingHeadStackStatus = 5084;
    private const uint MissingCircleStatus = 5085;
    private const uint MissingFanStatus = 5086;
    private const int WaveCount = 8;
    private const int StageCount = 4;
    private const int BasicStageCount = 1;
    private const int PreviewElementCount = 64;
    private const string PreviewElementPrefix = "Preview";
    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);
    private const float TowerOffsetCardinal = 8f;
    private const float TowerOffsetDiagonal = 5.7f;

    private static readonly Vector3[] TowerPositions =
    [
        new(ArenaCenter.X, 0f, ArenaCenter.Z - TowerOffsetCardinal),
        new(ArenaCenter.X + TowerOffsetDiagonal, 0f, ArenaCenter.Z - TowerOffsetDiagonal),
        new(ArenaCenter.X + TowerOffsetCardinal, 0f, ArenaCenter.Z),
        new(ArenaCenter.X + TowerOffsetDiagonal, 0f, ArenaCenter.Z + TowerOffsetDiagonal),
        new(ArenaCenter.X, 0f, ArenaCenter.Z + TowerOffsetCardinal),
        new(ArenaCenter.X - TowerOffsetDiagonal, 0f, ArenaCenter.Z + TowerOffsetDiagonal),
        new(ArenaCenter.X - TowerOffsetCardinal, 0f, ArenaCenter.Z),
        new(ArenaCenter.X - TowerOffsetDiagonal, 0f, ArenaCenter.Z - TowerOffsetDiagonal),
    ];

    private static readonly WaveGroupKind[] DefaultWaveSequence =
    [
        WaveGroupKind.GroupA,
        WaveGroupKind.GroupA,
        WaveGroupKind.GroupA,
        WaveGroupKind.GroupB,
        WaveGroupKind.GroupB,
        WaveGroupKind.GroupB,
        WaveGroupKind.GroupB,
        WaveGroupKind.GroupA
    ];

    private static readonly RolePosition[] DefaultRolePriority =
    [
        RolePosition.H2,
        RolePosition.M2,
        RolePosition.T2,
        RolePosition.M1,
        RolePosition.R2,
        RolePosition.R1,
        RolePosition.T1,
        RolePosition.H1
    ];

    private static readonly RolePosition[] LegacyGenericRolePriority =
    [
        RolePosition.T1,
        RolePosition.T2,
        RolePosition.H1,
        RolePosition.H2,
        RolePosition.M1,
        RolePosition.M2,
        RolePosition.R1,
        RolePosition.R2
    ];

    private static readonly (int A, int B)[] PriorityIndexPairSlots1238_4567 =
    [
        (0, 2),
        (1, 3),
        (4, 6),
        (5, 7)
    ];

    private static readonly InternationalString MainDescriptionText = new()
    {
        En =
            "Forsaken beta is a dynamic P2 Missing helper. Settings are limited to group assignment, wave groups, and compact role placement tables.",
        Jp =
            "Forsaken beta はP2ミッシング用の動的補助です。設定はグループ、Wave処理グループ、役割ごとの配置表に絞っています。"
    };

    private static readonly InternationalString AssignmentDescriptionText = new()
    {
        En =
            "Set global priority and optional fixed groups. If both Group A/B are empty, Group A is auto-captured from the initial Missing forecast as both head-stack players plus the highest-priority circle and fan players; Group B is the complement.",
        Jp =
            "全体優先順位と任意の固定Group A/Bを設定します。Group A/Bが両方空の場合、Group Aはミッシング初期予兆から頭割り2名+優先順位が高い円1名+扇1名として自動取得し、Group Bは残り4名にします。"
    };

    private static readonly InternationalString AssignmentModeLabelText = new()
    {
        En = "Grouping mode",
        Jp = "グループ決定方式"
    };

    private static readonly InternationalString InitialForecastPriorityModeDescriptionText = new()
    {
        En =
            "Empty Group A/B: Group A becomes both head-stack players plus the highest-priority circle and fan players.",
        Jp =
            "Group A/Bが空の場合、Group Aを頭割り2名+優先順位が高い円1名+扇1名として取得します。"
    };

    private static readonly InternationalString PriorityIndexPairsModeDescriptionText = new()
    {
        En =
            "Empty Group A/B: priority slots 1+3, 2+4, 5+7, and 6+8 are treated as pairs. Pairs containing one head-stack become Group A for waves 1,2,3,8; non-stack pairs become Group B for waves 4,5,6,7.",
        Jp =
            "Group A/Bが空の場合、優先順位の1+3、2+4、5+7、6+8をペアとして扱います。頭割りを1名含むペアを1,2,3,8回目用のGroup A、頭割りを含まないペアを4,5,6,7回目用のGroup Bにします。"
    };

    private static readonly InternationalString[] AssignmentModeLabels =
    [
        new()
        {
            En = "Initial forecast priority",
            Jp = "初期予兆+優先順位"
        },
        new()
        {
            En = "Priority index pairs 1238/4567",
            Jp = "優先順位ペア 1238/4567"
        }
    ];

    private static readonly InternationalString WaveTableDescriptionText = new()
    {
        En =
            "Each wave chooses a resolving group. The default AAABBBBA sequence is Group A on waves 1-3 and 8, Group B on waves 4-7. Tower and All Things Ending share one live-pattern role placement table; Past/Future use fixed tower-relative movement.",
        Jp =
            "各Waveで処理グループを選びます。初期値はAAABBBBA、つまり1-3回目と8回目がGroup A、4-7回目がGroup Bです。塔と消滅の脚は現在パターンと役割ごとの共通配置表を使い、過去/未来は塔基準の固定移動にします。"
    };

    private readonly List<uint> _pendingTowerSpawnPositions = [];
    private readonly List<uint> _pendingTowerClearPositions = [];
    private readonly List<uint> _autoGroupAIds = [];
    private readonly List<uint> _autoGroupBIds = [];
    private PatternInfo _lastPattern = new();
    private string _lastRuleLabel = "";
    private string _lastSelectorLabel = "";
    private LiveDebuffKind _lastDebuff = LiveDebuffKind.Any;
    private int _lastDebuffRank;
    private int _lastSupportRank;
    private ParticipantSide _lastSide = ParticipantSide.Any;
    private bool _active;
    private bool _hasTowerReference;
    private bool _hasStage;
    private int _currentWave;
    private uint _referenceMapPosition;
    private StageKind _currentStage;
    private string _currentInstruction = "";
    private bool _hasDestination;
    private Vector3 _myDestination = Vector3.Zero;

    public override HashSet<uint>? ValidTerritories { get; } = [TerritoryDancingMadUltimate];
    public override Metadata Metadata => new(1, "Garume");

    private Config C => Controller.GetConfig<Config>();
    private IPlayerCharacter BasePlayer => Controller.BasePlayer;

    public override void OnSetup()
    {
        C.EnsureDefaults();

        Controller.RegisterElement("SelfInstruction", new Element(0)
        {
            Enabled = false,
            radius = 0f,
            thicc = 0f,
            overlayBGColor = 0xC8000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 3.0f,
            overlayFScale = 2.4f,
            overlayText = ""
        });

        Controller.RegisterElement("Destination", new Element(0)
        {
            Enabled = false,
            radius = 1.35f,
            thicc = 5f,
            fillIntensity = 0.25f,
            color = 0xC800FFFF,
            tether = true,
            overlayBGColor = 0xC8000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 2.2f,
            overlayFScale = 1.35f,
            overlayText = ""
        });

        for (var i = 0; i < PreviewElementCount; i++)
        {
            Controller.RegisterElement($"{PreviewElementPrefix}{i}", new Element(0)
            {
                Enabled = false,
                radius = 0.55f,
                thicc = 3f,
                fillIntensity = 0.3f,
                color = 0xC8FFA040,
                overlayBGColor = 0xC8000000,
                overlayTextColor = 0xFFFFFFFF,
                overlayVOffset = 1.4f,
                overlayFScale = 1.0f,
                overlayText = ""
            });
        }
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
        if (castId == UltimateEmbrace)
        {
            ResetState();
            return;
        }

        if (castId == Forsaken)
        {
            ResetState();
            _active = true;
            _currentInstruction = C.CollectingAssignmentsText.Get();
            ApplyDisplay();
            return;
        }

        if (!_active && !HasPartyMissingStatus()) return;

        if (castId == PastsEndCast)
            SetStage(StageKind.Past);
        else if (castId == FuturesEndCast)
            SetStage(StageKind.Future);
        else if (castId is AllThingsEndingCast1 or AllThingsEndingCast2)
            SetStage(StageKind.AllThingsEnding);
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action?.RowId != Forsaken) return;

        _active = true;
        TryCaptureAutoGroups();
        UpdateWaitingInstruction();
        ApplyDisplay();
    }

    public override void OnGainBuffEffect(uint sourceId, Status status)
    {
        if (!IsMissingStatus(status.StatusId)) return;

        _active = true;
        TryCaptureAutoGroups();
        UpdateWaitingInstruction();
        ApplyDisplay();
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        var inMissing = _active || HasPartyMissingStatus();
        if (!inMissing) return;

        _active = true;
        TryCaptureAutoGroups();

        if (IsTowerSpawnMapEffect(data1, data2) && IsTowerMapPosition(position))
        {
            AddTowerSpawnPosition(position);
            return;
        }

        if (IsTowerClearMapEffect(data1, data2) && IsTowerMapPosition(position))
            AddTowerClearPosition(position);
    }

    public override void OnUpdate()
    {
        if (_active && _hasStage)
            UpdateStageInstruction();

        ApplyDisplay();
    }

    public override void OnSettingsDraw()
    {
        C.EnsureDefaults();

        ImGui.TextWrapped(MainDescriptionText.Get());
        ImGui.Separator();

        if (ImGui.CollapsingHeader(C.AssignmentHeaderText.Get(), ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            ImGui.TextWrapped(AssignmentDescriptionText.Get());
            DrawAssignmentModeSettings();
            ImGui.Spacing();
            ImGui.TextUnformatted("Global priority");
            C.PriorityData.Draw();
            ImGui.Spacing();
            ImGui.TextUnformatted("Group A");
            C.GroupA.Draw();
            ImGui.Spacing();
            ImGui.TextUnformatted("Group B");
            C.GroupB.Draw();
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader(C.WaveTableHeaderText.Get(), ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            ImGui.TextWrapped(WaveTableDescriptionText.Get());
            DrawWaveSettings();
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader(C.BasicPositionsHeaderText.Get(), ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            DrawBasicPositionSettings();
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader(C.DisplayTextHeaderText.Get()))
        {
            ImGui.Indent();
            DrawInternationalString("Collecting assignments", C.CollectingAssignmentsText);
            DrawInternationalString("Waiting for assignment", C.WaitingForAssignmentText);
            DrawInternationalString("Waiting for wave", C.WaitingForWaveText);
            DrawInternationalString("Active instruction", C.ActiveInstructionText);
            DrawInternationalString("Inactive instruction", C.InactiveInstructionText);
            DrawInternationalString("Destination overlay", C.DestinationOverlayText);
            DrawInternationalString("Head stack debuff", C.HeadStackDebuffText);
            DrawInternationalString("Circle debuff", C.CircleDebuffText);
            DrawInternationalString("Fan debuff", C.FanDebuffText);
            DrawInternationalString("No debuff", C.NoDebuffText);
            ImGui.Unindent();
        }

        DrawDebugSettings();
    }

    private void DrawAssignmentModeSettings()
    {
        var mode = (int)C.AssignmentMode;
        ImGui.SetNextItemWidth(280f);
        if (ImGui.Combo(AssignmentModeLabelText.Get(), ref mode, BuildAssignmentModeComboLabels(), AssignmentModeLabels.Length))
            C.AssignmentMode = (AutoAssignmentMode)Math.Clamp(mode, 0, AssignmentModeLabels.Length - 1);

        ImGui.TextWrapped(AssignmentModeDescription(C.AssignmentMode).Get());
    }

    private void DrawWaveSettings()
    {
        for (var wave = 1; wave <= WaveCount; wave++)
        {
            ImGui.PushID($"Wave{wave}");
            var groupIndex = (int)C.Waves[wave - 1].ResolvingGroup;
            ImGui.SetNextItemWidth(180f);
            if (ImGui.Combo($"{FormatText(C.WaveHeaderText, wave)} resolving group", ref groupIndex, WaveGroupLabels, WaveGroupLabels.Length))
                C.Waves[wave - 1].ResolvingGroup = (WaveGroupKind)Math.Clamp(groupIndex, 0, WaveGroupLabels.Length - 1);
            ImGui.PopID();
        }
    }

    private void DrawBasicPositionSettings()
    {
        if (ImGui.TreeNode(C.PastFutureHeaderText.Get()))
        {
            ImGui.TextWrapped(C.PastFutureDescriptionText.Get());

            ImGui.PushID("PastFixed");
            ImGui.TextUnformatted(C.PastStageLabelText.Get());
            DrawInternationalString("Text", C.PastFixedText);
            DrawPositionRule(C.PastFixedPosition);
            ImGui.PopID();

            ImGui.PushID("FutureFixed");
            ImGui.TextUnformatted(C.FutureStageLabelText.Get());
            DrawInternationalString("Text", C.FutureFixedText);
            DrawPositionRule(C.FutureFixedPosition);
            ImGui.PopID();

            ImGui.TreePop();
        }

        for (var i = 0; i < C.BasicStages.Length; i++)
        {
            var stage = C.BasicStages[i];
            ImGui.PushID($"BasicStage{i}");
            if (ImGui.TreeNode(BasicStageLabel(stage.Kind)))
            {
                DrawBasicStageSettings(stage);
                ImGui.TreePop();
            }

            ImGui.PopID();
        }
    }

    private void DrawBasicStageSettings(BasicStageConfig stage)
    {
        foreach (var pattern in stage.Patterns)
        {
            ImGui.PushID($"Pattern{pattern.Pattern.Head}-{pattern.Pattern.Circle}-{pattern.Pattern.Fan}");
            if (ImGui.TreeNode(FormatPattern(pattern.Pattern)))
            {
                DrawPatternPlacementTable(pattern);
                ImGui.TreePop();
            }

            ImGui.PopID();
        }
    }

    private void DrawPatternPlacementTable(PatternPlacementConfig pattern)
    {
        if (!ImGui.BeginTable($"PlacementTable_{pattern.Pattern.Head}_{pattern.Pattern.Circle}_{pattern.Pattern.Fan}", 9,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            return;

        ImGui.TableSetupColumn("On", ImGuiTableColumnFlags.WidthFixed, 42f);
        ImGui.TableSetupColumn("Side", ImGuiTableColumnFlags.WidthFixed, 118f);
        ImGui.TableSetupColumn("Debuff", ImGuiTableColumnFlags.WidthFixed, 96f);
        ImGui.TableSetupColumn("Rank", ImGuiTableColumnFlags.WidthFixed, 62f);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Basis", ImGuiTableColumnFlags.WidthFixed, 130f);
        ImGui.TableSetupColumn("Angle", ImGuiTableColumnFlags.WidthFixed, 82f);
        ImGui.TableSetupColumn("Range", ImGuiTableColumnFlags.WidthFixed, 82f);
        ImGui.TableSetupColumn("Position", ImGuiTableColumnFlags.WidthFixed, 145f);
        ImGui.TableHeadersRow();

        var reference = GetPreviewReference(C.PreviewWave);

        for (var i = 0; i < pattern.Placements.Length; i++)
        {
            var placement = pattern.Placements[i];
            ImGui.PushID(i);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Checkbox("##enabled", ref placement.Enabled);

            ImGui.TableNextColumn();
            DrawSelectorSideCell(placement.Selector);

            ImGui.TableNextColumn();
            DrawSelectorDebuffCell(placement.Selector);

            ImGui.TableNextColumn();
            DrawSelectorRankCell(placement.Selector);

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1f);
            DrawInternationalStringCell("text", placement.Text);

            ImGui.TableNextColumn();
            DrawPositionBasisCell(placement.Position);

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1f);
            ImGui.DragFloat("##angle", ref placement.Position.AngleDeg, 1f, -720f, 720f, "%.0f");

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1f);
            ImGui.DragFloat("##range", ref placement.Position.Range, 0.05f, 0f, 30f, "%.2f");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(FormatVector3(ResolvePosition(placement.Position, reference)));

            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    private static void DrawSelectorSideCell(RoleSelector selector)
    {
        var sideIndex = (int)selector.Side;
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.Combo("##side", ref sideIndex, ParticipantSideLabels, ParticipantSideLabels.Length))
            selector.Side = (ParticipantSide)Math.Clamp(sideIndex, 0, ParticipantSideLabels.Length - 1);
    }

    private static void DrawSelectorDebuffCell(RoleSelector selector)
    {
        var debuffIndex = (int)selector.Debuff;
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.Combo("##debuff", ref debuffIndex, LiveDebuffLabels, LiveDebuffLabels.Length))
            selector.Debuff = (LiveDebuffKind)Math.Clamp(debuffIndex, 0, LiveDebuffLabels.Length - 1);
    }

    private static void DrawSelectorRankCell(RoleSelector selector)
    {
        var rank = selector.Rank;
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.Combo("##rank", ref rank, RankLabels, RankLabels.Length))
            selector.Rank = Math.Clamp(rank, 0, 4);
    }

    private static void DrawInternationalStringCell(string id, InternationalString text)
    {
        ImGui.PushID(id);
        var current = text.Get();
        text.ImGuiEdit(ref current);
        ImGui.PopID();
    }

    private static void DrawPositionBasisCell(PositionRule rule)
    {
        var basisIndex = (int)rule.Basis;
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.Combo("##basis", ref basisIndex, BasisLabels, BasisLabels.Length))
            rule.Basis = (PositionBasis)Math.Clamp(basisIndex, 0, BasisLabels.Length - 1);
    }

    private void DrawPositionRule(PositionRule rule)
    {
        var basisIndex = (int)rule.Basis;
        ImGui.SetNextItemWidth(140f);
        if (ImGui.Combo("Basis", ref basisIndex, BasisLabels, BasisLabels.Length))
            rule.Basis = (PositionBasis)Math.Clamp(basisIndex, 0, BasisLabels.Length - 1);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(90f);
        ImGui.DragFloat("Angle", ref rule.AngleDeg, 1f, -720f, 720f);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(90f);
        ImGui.DragFloat("Range", ref rule.Range, 0.05f, 0f, 30f);
    }

    private static void DrawInternationalString(string label, InternationalString text)
    {
        ImGui.PushID(label);
        ImGui.Text(label);
        ImGui.SameLine();
        var current = text.Get();
        text.ImGuiEdit(ref current);
        ImGui.PopID();
    }

    private void DrawDebugSettings()
    {
        if (!ImGui.CollapsingHeader("Debug")) return;

        ImGui.Indent();
        ImGui.TextUnformatted($"Active: {_active}");
        ImGui.TextUnformatted($"Current wave: {_currentWave}");
        ImGui.TextUnformatted($"Current stage: {(_hasStage ? _currentStage.ToString() : "none")}");
        ImGui.TextUnformatted($"Reference tower: {FormatMapPosition(_referenceMapPosition)}");
        ImGui.TextUnformatted($"Pair tower: {FormatMapPosition(IsTowerMapPosition(_referenceMapPosition) ? AddMapSteps(_referenceMapPosition, 2) : 0)}");
        ImGui.TextUnformatted($"Assignment mode: {C.AssignmentMode}");
        ImGui.TextUnformatted($"Auto Group A: {FormatAutoGroup(_autoGroupAIds)}");
        ImGui.TextUnformatted($"Auto Group B: {FormatAutoGroup(_autoGroupBIds)}");
        ImGui.TextUnformatted($"Pending spawns: {FormatMapPositionList(_pendingTowerSpawnPositions)}");
        ImGui.TextUnformatted($"Pending clears: {FormatMapPositionList(_pendingTowerClearPositions)}");
        ImGui.TextUnformatted($"Last pattern: H{_lastPattern.Head}/C{_lastPattern.Circle}/F{_lastPattern.Fan}");
        ImGui.TextUnformatted($"Last side: {_lastSide}");
        ImGui.TextUnformatted($"Last debuff/rank: {_lastDebuff} #{_lastDebuffRank}");
        ImGui.TextUnformatted($"Last support rank: {_lastSupportRank}");
        ImGui.TextUnformatted($"Last selector: {(_lastSelectorLabel.Length == 0 ? "none" : _lastSelectorLabel)}");
        ImGui.TextUnformatted($"Last matched rule: {_lastRuleLabel}");

        var me = BasePlayer;
        if (me != null)
        {
            ImGui.TextUnformatted($"BasePlayer: 0x{me.EntityId:X8}");
            ImGui.TextUnformatted($"BasePlayer current debuff: {CurrentDebuffFromPlayer(me)}");
            ImGui.TextUnformatted($"Has destination: {_hasDestination}");
            ImGui.TextUnformatted($"Destination: {_myDestination.X:0.00}, {_myDestination.Y:0.00}, {_myDestination.Z:0.00}");
        }

        if (ImGui.TreeNode("Preview"))
        {
            ImGui.Checkbox("Show preview", ref C.ShowPreview);

            ImGui.SetNextItemWidth(120f);
            ImGui.DragInt("Wave", ref C.PreviewWave, 0.1f, 1, WaveCount);
            C.PreviewWave = Math.Clamp(C.PreviewWave, 1, WaveCount);

            var stageIndex = (int)C.PreviewStage;
            ImGui.SetNextItemWidth(160f);
            if (ImGui.Combo("Stage", ref stageIndex, BuildStageComboLabels(), StageCount))
                C.PreviewStage = (StageKind)Math.Clamp(stageIndex, 0, StageCount - 1);

            if (C.ShowPreview)
                DrawPreviewElements(C.PreviewWave, C.PreviewStage);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Live debuffs"))
        {
            foreach (var player in GetPriorityOrderedParty())
                ImGui.TextUnformatted($"{player.Name}: 0x{player.EntityId:X8} {CurrentDebuffFromPlayer(player)}");
            ImGui.TreePop();
        }

        ImGui.Unindent();
    }

    private void AddTowerSpawnPosition(uint position)
    {
        AddUniquePairPosition(_pendingTowerSpawnPositions, position);
        if (_pendingTowerSpawnPositions.Count != 2) return;

        if (!TryGetPairReference(_pendingTowerSpawnPositions[0], _pendingTowerSpawnPositions[1],
                out _referenceMapPosition))
        {
            _pendingTowerSpawnPositions.Clear();
            return;
        }

        _pendingTowerSpawnPositions.Clear();
        _pendingTowerClearPositions.Clear();
        _currentWave = WaveFromReference(_referenceMapPosition);
        _hasTowerReference = true;
        _currentStage = StageKind.Tower;
        _hasStage = true;

        UpdateStageInstruction();
        ApplyDisplay();
    }

    private void AddTowerClearPosition(uint position)
    {
        AddUniquePairPosition(_pendingTowerClearPositions, position);
        if (_pendingTowerClearPositions.Count != 2) return;

        if (TryGetPairReference(_pendingTowerClearPositions[0], _pendingTowerClearPositions[1], out var reference) &&
            _currentWave == WaveFromReference(reference) &&
            _hasStage &&
            _currentStage == StageKind.Tower)
        {
            _currentInstruction = "";
            ClearDestination();
        }

        _pendingTowerClearPositions.Clear();
        ApplyDisplay();
    }

    private void SetStage(StageKind stage)
    {
        _active = true;
        _currentStage = stage;
        _hasStage = true;

        UpdateStageInstruction();
        ApplyDisplay();
    }

    private static void AddUniquePairPosition(List<uint> list, uint position)
    {
        if (list.Count >= 2)
            list.Clear();

        if (!list.Contains(position))
            list.Add(position);
    }

    private void UpdateWaitingInstruction()
    {
        if (_hasStage) return;

        var me = BasePlayer;
        if (me == null) return;

        var debuff = CurrentDebuffFromPlayer(me);
        ClearDestination();
        _currentInstruction = debuff == LiveDebuffKind.None
            ? C.WaitingForAssignmentText.Get()
            : FormatText(C.WaitingForWaveText, DebuffLabel(debuff));
    }

    private void UpdateStageInstruction()
    {
        var me = BasePlayer;
        if (me == null) return;

        if (!_hasTowerReference || !_hasStage || _currentWave is < 1 or > WaveCount)
        {
            _currentInstruction = FormatText(C.WaitingForWaveText, DebuffLabel(CurrentDebuffFromPlayer(me)));
            ClearDestination();
            return;
        }

        if (!TryBuildLiveContext(me, out var context))
        {
            _currentInstruction = C.WaitingForAssignmentText.Get();
            ClearDestination();
            return;
        }

        _lastPattern = context.Pattern;
        _lastSide = context.Side;
        _lastDebuff = context.Debuff;
        _lastDebuffRank = context.DebuffRank;
        _lastSupportRank = context.SupportRank;

        if (TryApplyBasicInstruction(context))
            return;

        _lastRuleLabel = "";
        _lastSelectorLabel = "";
        _currentInstruction = FormatText(
            C.InactiveInstructionText,
            _currentWave,
            DebuffLabel(context.Debuff),
            StageLabel(_currentStage));
        ClearDestination();
    }

    private bool TryApplyBasicInstruction(LiveContext context)
    {
        if (_currentStage == StageKind.Past)
            return ApplyFixedStageInstruction(C.PastFixedText, C.PastFixedPosition);

        if (_currentStage == StageKind.Future)
            return ApplyFixedStageInstruction(C.FutureFixedText, C.FutureFixedPosition);

        var stageKind = BasicStageFromStage(_currentStage);
        var stage = C.BasicStages.FirstOrDefault(item => item.Kind == stageKind);
        if (stage == null) return false;

        var pattern = stage.Patterns.FirstOrDefault(item => item.Pattern.Matches(context.Pattern));
        if (pattern == null) return false;

        var placement = pattern.Placements.FirstOrDefault(item => item.Matches(context));
        if (placement == null) return false;

        _lastRuleLabel = placement.Text.Get();
        _lastSelectorLabel = SelectorLabel(placement.Selector);
        SetDestination(ResolvePosition(placement.Position, _referenceMapPosition));
        _currentInstruction = FormatText(
            C.ActiveInstructionText,
            _currentWave,
            StageLabel(_currentStage),
            placement.Text.Get());
        return true;
    }

    private bool ApplyFixedStageInstruction(InternationalString text, PositionRule position)
    {
        _lastRuleLabel = text.Get();
        _lastSelectorLabel = "";
        SetDestination(ResolvePosition(position, _referenceMapPosition));
        _currentInstruction = FormatText(
            C.ActiveInstructionText,
            _currentWave,
            StageLabel(_currentStage),
            text.Get());
        return true;
    }

    private void ClearDestination()
    {
        _hasDestination = false;
        _myDestination = Vector3.Zero;
    }

    private void SetDestination(Vector3 destination)
    {
        _hasDestination = true;
        _myDestination = destination;
    }

    private bool TryBuildLiveContext(IPlayerCharacter me, out LiveContext context)
    {
        context = default;
        var wave = C.Waves[_currentWave - 1];
        var party = GetPriorityOrderedParty();
        if (party.Count == 0) return false;

        var resolvingGroup = GetGroupPlayers(wave.ResolvingGroup, party);
        if (resolvingGroup.Count == 0) return false;

        var supportGroup = GetSupportGroup(wave.ResolvingGroup, party, resolvingGroup);
        var side = resolvingGroup.Any(p => p.EntityId == me.EntityId)
            ? ParticipantSide.ResolvingGroup
            : supportGroup.Any(p => p.EntityId == me.EntityId)
                ? ParticipantSide.SupportGroup
                : ParticipantSide.Any;

        var sideGroup = side == ParticipantSide.SupportGroup ? supportGroup : resolvingGroup;
        var debuff = CurrentDebuffFromPlayer(me);
        var sameDebuffPlayers = sideGroup
            .Where(player => CurrentDebuffFromPlayer(player) == debuff)
            .ToList();
        var debuffIndex = sameDebuffPlayers.FindIndex(player => player.EntityId == me.EntityId);
        var debuffRank = debuff == LiveDebuffKind.None || debuffIndex < 0 ? 0 : debuffIndex + 1;
        var supportRank = side == ParticipantSide.SupportGroup
            ? supportGroup.FindIndex(player => player.EntityId == me.EntityId) + 1
            : 0;

        context = new LiveContext(
            side,
            debuff,
            debuffRank,
            supportRank,
            PatternInfo.FromPlayers(resolvingGroup));
        return true;
    }

    private List<IPlayerCharacter> GetPriorityOrderedParty()
    {
        var members = Controller.GetPartyMembers()
            .OfType<IPlayerCharacter>()
            .ToList();
        var priority = C.PriorityData.GetPlayers(_ => true);
        if (priority == null || priority.Count == 0)
            return members.OrderBy(player => player.EntityId).ToList();

        return members
            .OrderBy(player =>
            {
                for (var i = 0; i < priority.Count; i++)
                    if (priority[i].IGameObject.EntityId == player.EntityId)
                        return i;
                return int.MaxValue;
            })
            .ThenBy(player => player.EntityId)
            .ToList();
    }

    private List<IPlayerCharacter> GetGroupPlayers(WaveGroupKind group, IReadOnlyList<IPlayerCharacter> party)
    {
        if (group == WaveGroupKind.All)
            return party.ToList();

        var configured = GetConfiguredGroup(group == WaveGroupKind.GroupA ? C.GroupA : C.GroupB, party);
        if (configured.Count > 0)
            return configured;

        var oppositeConfigured = GetConfiguredGroup(group == WaveGroupKind.GroupA ? C.GroupB : C.GroupA, party);
        if (oppositeConfigured.Count > 0)
        {
            var oppositeIds = oppositeConfigured.Select(player => player.EntityId).ToHashSet();
            return party.Where(player => !oppositeIds.Contains(player.EntityId)).ToList();
        }

        var autoIds = group == WaveGroupKind.GroupA ? _autoGroupAIds : _autoGroupBIds;
        if (autoIds.Count > 0)
        {
            var ids = autoIds.ToHashSet();
            return party.Where(player => ids.Contains(player.EntityId)).ToList();
        }

        return [];
    }

    private static List<IPlayerCharacter> GetConfiguredGroup(PriorityData priorityData, IReadOnlyList<IPlayerCharacter> party)
    {
        var configured = priorityData.GetPlayers(_ => true);
        if (configured is not { Count: > 0 })
            return [];

        var ids = configured.Select(item => item.IGameObject.EntityId).ToHashSet();
        var players = party.Where(player => ids.Contains(player.EntityId)).ToList();
        if (players.Count == 0)
            return [];

        return players
            .OrderBy(player => configured.FindIndex(item => item.IGameObject.EntityId == player.EntityId))
            .ToList();
    }

    private void TryCaptureAutoGroups()
    {
        if (_autoGroupAIds.Count == 4 && _autoGroupBIds.Count == 4) return;

        var party = GetPriorityOrderedParty();
        if (party.Count < 8) return;

        if (GetConfiguredGroup(C.GroupA, party).Count > 0 || GetConfiguredGroup(C.GroupB, party).Count > 0)
            return;

        if (C.AssignmentMode == AutoAssignmentMode.PriorityIndexPairs1238_4567)
        {
            TryCapturePriorityIndexPairGroups(party);
            return;
        }

        TryCaptureInitialForecastPriorityGroups(party);
    }

    private void TryCaptureInitialForecastPriorityGroups(IReadOnlyList<IPlayerCharacter> party)
    {
        var heads = party.Where(player => CurrentDebuffFromPlayer(player) == LiveDebuffKind.HeadStack).ToList();
        var circles = party.Where(player => CurrentDebuffFromPlayer(player) == LiveDebuffKind.Circle).ToList();
        var fans = party.Where(player => CurrentDebuffFromPlayer(player) == LiveDebuffKind.Fan).ToList();
        if (heads.Count != 2 || circles.Count != 3 || fans.Count != 3)
            return;

        var groupA = heads
            .Concat(circles.Take(1))
            .Concat(fans.Take(1))
            .DistinctBy(player => player.EntityId)
            .ToList();
        if (groupA.Count != 4) return;

        var groupAIds = groupA.Select(player => player.EntityId).ToHashSet();
        var groupB = party.Where(player => !groupAIds.Contains(player.EntityId)).Take(4).ToList();
        if (groupB.Count != 4) return;

        SetAutoGroups(groupA, groupB);
    }

    private void TryCapturePriorityIndexPairGroups(IReadOnlyList<IPlayerCharacter> party)
    {
        if (party.Count < 8) return;

        var groupA = new List<IPlayerCharacter>();
        var groupB = new List<IPlayerCharacter>();
        foreach (var (firstIndex, secondIndex) in PriorityIndexPairSlots1238_4567)
        {
            var first = party[firstIndex];
            var second = party[secondIndex];
            var firstDebuff = CurrentDebuffFromPlayer(first);
            var secondDebuff = CurrentDebuffFromPlayer(second);
            var firstIsHead = firstDebuff == LiveDebuffKind.HeadStack;
            var secondIsHead = secondDebuff == LiveDebuffKind.HeadStack;

            if (firstIsHead && secondIsHead)
                return;

            if (firstIsHead || secondIsHead)
            {
                var partnerDebuff = firstIsHead ? secondDebuff : firstDebuff;
                if (partnerDebuff is not (LiveDebuffKind.Circle or LiveDebuffKind.Fan))
                    return;

                groupA.Add(first);
                groupA.Add(second);
                continue;
            }

            if (firstDebuff is not (LiveDebuffKind.Circle or LiveDebuffKind.Fan) ||
                secondDebuff is not (LiveDebuffKind.Circle or LiveDebuffKind.Fan))
                return;

            groupB.Add(first);
            groupB.Add(second);
        }

        if (groupA.Count != 4 || groupB.Count != 4)
            return;

        SetAutoGroups(groupA, groupB);
    }

    private void SetAutoGroups(IReadOnlyList<IPlayerCharacter> groupA, IReadOnlyList<IPlayerCharacter> groupB)
    {
        var groupAIds = groupA.Select(player => player.EntityId).ToHashSet();
        var groupBIds = groupB.Select(player => player.EntityId).ToHashSet();
        if (groupAIds.Count != 4 || groupBIds.Count != 4 || groupAIds.Overlaps(groupBIds))
            return;

        _autoGroupAIds.Clear();
        _autoGroupAIds.AddRange(groupA.Select(player => player.EntityId));
        _autoGroupBIds.Clear();
        _autoGroupBIds.AddRange(groupB.Select(player => player.EntityId));
    }

    private List<IPlayerCharacter> GetSupportGroup(
        WaveGroupKind group,
        IReadOnlyList<IPlayerCharacter> party,
        IReadOnlyList<IPlayerCharacter> resolvingGroup)
    {
        if (group == WaveGroupKind.All) return [];

        var support = GetGroupPlayers(group == WaveGroupKind.GroupA ? WaveGroupKind.GroupB : WaveGroupKind.GroupA, party);
        if (support.Count > 0) return support;

        var resolvingIds = resolvingGroup.Select(player => player.EntityId).ToHashSet();
        return party.Where(player => !resolvingIds.Contains(player.EntityId)).ToList();
    }

    private void ApplyDisplay()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

        var me = BasePlayer;
        if (me == null) return;

        if (Controller.TryGetElementByName("SelfInstruction", out var instruction))
        {
            instruction.Enabled = !string.IsNullOrWhiteSpace(_currentInstruction);
            instruction.SetRefPosition(me.Position);
            instruction.overlayText = _currentInstruction;
        }

        if (!_hasDestination) return;

        if (Controller.TryGetElementByName("Destination", out var destination))
        {
            destination.Enabled = true;
            destination.SetRefPosition(_myDestination);
            destination.color = RainbowColor();
            destination.overlayText = FormatText(
                C.DestinationOverlayText,
                _currentWave,
                DebuffLabel(CurrentDebuffFromPlayer(me)),
                StageLabel(_currentStage));
        }
    }

    private void DrawPreviewElements(int wave, StageKind stageKind)
    {
        var reference = GetPreviewReference(wave);
        if (!IsTowerMapPosition(reference)) return;

        var pair = AddMapSteps(reference, 2);
        DrawPreviewElement(0, TowerPosition(reference), C.ReferenceTowerPreviewText.Get(), 0xC8FFFFFF, 0.95f);
        DrawPreviewElement(1, TowerPosition(pair), C.PairedTowerPreviewText.Get(), 0xC840FF40, 0.95f);

        var index = 2;

        if (stageKind == StageKind.Past)
        {
            DrawPreviewElement(index, ResolvePosition(C.PastFixedPosition, reference), C.PastFixedText.Get(), 0xC8FFD040, 0.6f);
            return;
        }

        if (stageKind == StageKind.Future)
        {
            DrawPreviewElement(index, ResolvePosition(C.FutureFixedPosition, reference), C.FutureFixedText.Get(), 0xC840C0FF, 0.6f);
            return;
        }

        var basicStageKind = BasicStageFromStage(stageKind);
        var stage = C.BasicStages.FirstOrDefault(item => item.Kind == basicStageKind);
        if (stage == null) return;

        foreach (var placement in stage.Patterns.SelectMany(pattern => pattern.Placements).Where(placement => placement.Enabled))
        {
            if (index >= PreviewElementCount) break;

            var position = ResolvePosition(placement.Position, reference);
            DrawPreviewElement(index, position, $"{wave}: {placement.Text.Get()}", SelectorColor(placement.Selector), 0.6f);
            index++;
        }
    }

    private void DrawPreviewElement(int index, Vector3 position, string text, uint color, float radius)
    {
        if (!Controller.TryGetElementByName($"{PreviewElementPrefix}{index}", out var element)) return;

        element.Enabled = true;
        element.SetRefPosition(position);
        element.radius = radius;
        element.thicc = radius > 0.8f ? 6f : 3f;
        element.color = color;
        element.overlayTextColor = color;
        element.overlayText = text;
    }

    private Vector3 ResolvePosition(PositionRule rule, uint reference)
    {
        var pair = AddMapSteps(reference, 2);
        var refPosition = TowerPosition(reference);
        var pairPosition = TowerPosition(pair);
        var origin = rule.Basis switch
        {
            PositionBasis.ReferenceTower or PositionBasis.RightTower => refPosition,
            PositionBasis.PairedTower or PositionBasis.LeftTower => pairPosition,
            PositionBasis.TowerPairCenter => (refPosition + pairPosition) * 0.5f,
            PositionBasis.OppositeTowerPairCenter => (TowerPosition(AddMapSteps(reference, 4)) + TowerPosition(AddMapSteps(pair, 4))) * 0.5f,
            PositionBasis.ArenaCenter => ArenaCenter,
            _ => ArenaCenter
        };

        var baseAngle = rule.Basis switch
        {
            PositionBasis.ReferenceTower or PositionBasis.RightTower => AngleFrom(origin, ArenaCenter),
            PositionBasis.PairedTower or PositionBasis.LeftTower => AngleFrom(origin, ArenaCenter),
            _ => DirectionAngle(reference)
        };

        return OffsetFrom(origin, NormalizeAngle360(baseAngle + rule.AngleDeg), rule.Range);
    }

    private uint GetPreviewReference(int wave)
    {
        if (_hasTowerReference && _currentWave == wave && IsTowerMapPosition(_referenceMapPosition))
            return _referenceMapPosition;

        return ReferenceFromWave(wave);
    }

    private bool HasPartyMissingStatus()
    {
        return Controller.GetPartyMembers()
            .OfType<IPlayerCharacter>()
            .Any(player => player.StatusList.Any(status => IsMissingStatus(status.StatusId)));
    }

    private static LiveDebuffKind CurrentDebuffFromPlayer(IPlayerCharacter player)
    {
        foreach (var status in player.StatusList)
        {
            var debuff = DebuffFromStatus(status.StatusId);
            if (debuff != LiveDebuffKind.None)
                return debuff;
        }

        return LiveDebuffKind.None;
    }

    private static LiveDebuffKind DebuffFromStatus(uint statusId)
    {
        return statusId switch
        {
            MissingHeadStackStatus => LiveDebuffKind.HeadStack,
            MissingCircleStatus => LiveDebuffKind.Circle,
            MissingFanStatus => LiveDebuffKind.Fan,
            _ => LiveDebuffKind.None
        };
    }

    private static bool IsMissingStatus(uint statusId)
    {
        return statusId is MissingInventoryStatus or MissingHeadStackStatus or MissingCircleStatus or MissingFanStatus;
    }

    private static bool IsTowerSpawnMapEffect(ushort data1, ushort data2) => data1 == 1 && data2 == 2;
    private static bool IsTowerClearMapEffect(ushort data1, ushort data2) => data1 == 4 && data2 == 8;
    private static bool IsTowerMapPosition(uint position) => position is >= 1 and <= 8;

    private static bool TryGetPairReference(uint first, uint second, out uint reference)
    {
        if (AddMapSteps(first, 2) == second)
        {
            reference = first;
            return true;
        }

        if (AddMapSteps(second, 2) == first)
        {
            reference = second;
            return true;
        }

        reference = 0;
        return false;
    }

    private static uint AddMapSteps(uint position, int steps)
    {
        return (uint)(((int)position - 1 + steps + 8) % 8 + 1);
    }

    private static uint ReferenceFromWave(int wave)
    {
        return (uint)(((wave - 1 + 3) % 8) + 1);
    }

    private static int WaveFromReference(uint reference)
    {
        return ((int)reference - 4 + 8) % 8 + 1;
    }

    private static Vector3 TowerPosition(uint mapPosition)
    {
        if (!IsTowerMapPosition(mapPosition)) return ArenaCenter;
        return TowerPositions[mapPosition - 1];
    }

    private static float DirectionAngle(uint mapPosition)
    {
        return NormalizeAngle360(((int)mapPosition - 1) * 45f);
    }

    private static float AngleFrom(Vector3 origin, Vector3 target)
    {
        var dx = target.X - origin.X;
        var dz = target.Z - origin.Z;
        return NormalizeAngle360(MathF.Atan2(dx, -dz) * 180f / MathF.PI);
    }

    private static Vector3 OffsetFrom(Vector3 origin, float compassDeg, float range)
    {
        var rad = compassDeg * MathF.PI / 180f;
        return new Vector3(
            origin.X + MathF.Sin(rad) * range,
            origin.Y,
            origin.Z - MathF.Cos(rad) * range);
    }

    private static float NormalizeAngle360(float angle)
    {
        return (angle % 360f + 360f) % 360f;
    }

    private string DebuffLabel(LiveDebuffKind debuff)
    {
        return debuff switch
        {
            LiveDebuffKind.HeadStack => C.HeadStackDebuffText.Get(),
            LiveDebuffKind.Circle => C.CircleDebuffText.Get(),
            LiveDebuffKind.Fan => C.FanDebuffText.Get(),
            LiveDebuffKind.None => C.NoDebuffText.Get(),
            _ => debuff.ToString()
        };
    }

    private string StageLabel(StageKind stage)
    {
        return stage switch
        {
            StageKind.Tower => C.TowerStageLabelText.Get(),
            StageKind.Past => C.PastStageLabelText.Get(),
            StageKind.Future => C.FutureStageLabelText.Get(),
            StageKind.AllThingsEnding => C.AllThingsEndingStageLabelText.Get(),
            _ => stage.ToString()
        };
    }

    private string[] BuildStageComboLabels()
    {
        return AllStages().Select(StageLabel).ToArray();
    }

    private static string[] BuildAssignmentModeComboLabels()
    {
        return AssignmentModeLabels.Select(label => label.Get()).ToArray();
    }

    private static InternationalString AssignmentModeDescription(AutoAssignmentMode mode)
    {
        return mode switch
        {
            AutoAssignmentMode.PriorityIndexPairs1238_4567 => PriorityIndexPairsModeDescriptionText,
            _ => InitialForecastPriorityModeDescriptionText
        };
    }

    private string BasicStageLabel(BasicStageKind stage)
    {
        return stage switch
        {
            BasicStageKind.Tower => $"{C.TowerStageLabelText.Get()} / {C.AllThingsEndingStageLabelText.Get()}",
            _ => stage.ToString()
        };
    }

    private static BasicStageKind BasicStageFromStage(StageKind stage)
    {
        return BasicStageKind.Tower;
    }

    private static string FormatPattern(PatternInfo pattern)
    {
        return $"H{pattern.Head}/C{pattern.Circle}/F{pattern.Fan}";
    }

    private string SelectorLabel(RoleSelector selector)
    {
        selector ??= new RoleSelector();
        var sideText = selector.Side switch
        {
            ParticipantSide.ResolvingGroup => "Resolving",
            ParticipantSide.SupportGroup => "Support",
            _ => "Any"
        };

        var debuffText = DebuffLabel(selector.Debuff);
        var rankText = selector.Rank <= 0 ? "Any" : selector.Rank.ToString();
        return $"{sideText} {debuffText} {rankText}";
    }

    private static IEnumerable<StageKind> AllStages()
    {
        yield return StageKind.Tower;
        yield return StageKind.Past;
        yield return StageKind.Future;
        yield return StageKind.AllThingsEnding;
    }

    private static uint SelectorColor(RoleSelector selector)
    {
        return selector?.Debuff switch
        {
            LiveDebuffKind.HeadStack => 0xC8FFD040,
            LiveDebuffKind.Circle => 0xC8FFA040,
            LiveDebuffKind.Fan => 0xC84070FF,
            _ => 0xC8FFFFFF
        };
    }

    private string FormatMapPosition(uint position)
    {
        if (!IsTowerMapPosition(position)) return "none";
        return $"{position} ({MapPositionLabel(position)})";
    }

    private string FormatMapPositionList(IEnumerable<uint> positions)
    {
        var items = positions.Select(FormatMapPosition).ToList();
        return items.Count == 0 ? "none" : string.Join(", ", items);
    }

    private string FormatAutoGroup(IEnumerable<uint> entityIds)
    {
        var ids = entityIds.ToList();
        if (ids.Count == 0) return "none";

        var players = Controller.GetPartyMembers()
            .OfType<IPlayerCharacter>()
            .ToDictionary(player => player.EntityId, player => player.Name.ToString());
        return string.Join(", ", ids.Select(id => players.GetValueOrDefault(id, $"0x{id:X8}")));
    }

    private static string FormatVector3(Vector3 position)
    {
        return $"{position.X:0.00}, {position.Y:0.00}, {position.Z:0.00}";
    }

    private string MapPositionLabel(uint position)
    {
        return position switch
        {
            1 => C.NorthLabelText.Get(),
            2 => C.NorthEastLabelText.Get(),
            3 => C.EastLabelText.Get(),
            4 => C.SouthEastLabelText.Get(),
            5 => C.SouthLabelText.Get(),
            6 => C.SouthWestLabelText.Get(),
            7 => C.WestLabelText.Get(),
            8 => C.NorthWestLabelText.Get(),
            _ => "?"
        };
    }

    private static string FormatText(InternationalString text, params object[] args)
    {
        var format = text.Get() ?? "";
        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            return format;
        }
    }

    private static uint RainbowColor()
    {
        var hue = Environment.TickCount64 % 2400 / 2400f;
        var (r, g, b) = HsvToRgb(hue, 1f, 1f);
        return 0xC8000000u | ((uint)(r * 255f) << 16) | ((uint)(g * 255f) << 8) | (uint)(b * 255f);
    }

    private static (float R, float G, float B) HsvToRgb(float h, float s, float v)
    {
        var i = (int)MathF.Floor(h * 6f);
        var f = h * 6f - i;
        var p = v * (1f - s);
        var q = v * (1f - f * s);
        var t = v * (1f - (1f - f) * s);

        return (i % 6) switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q)
        };
    }

    private void ResetState()
    {
        _active = false;
        _hasTowerReference = false;
        _hasStage = false;
        _currentWave = 0;
        _referenceMapPosition = 0;
        _currentStage = StageKind.Tower;
        _currentInstruction = "";
        _hasDestination = false;
        _myDestination = Vector3.Zero;
        _pendingTowerSpawnPositions.Clear();
        _pendingTowerClearPositions.Clear();
        _autoGroupAIds.Clear();
        _autoGroupBIds.Clear();
        _lastPattern = new PatternInfo();
        _lastRuleLabel = "";
        _lastDebuff = LiveDebuffKind.Any;
        _lastDebuffRank = 0;
        _lastSupportRank = 0;
        _lastSide = ParticipantSide.Any;
        _lastSelectorLabel = "";
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    private static readonly string[] BasisLabels =
    [
        "ReferenceTower",
        "PairedTower",
        "LeftTower",
        "RightTower",
        "TowerPairCenter",
        "ArenaCenter",
        "OppositeTowerPairCenter",
    ];

    private static readonly string[] LiveDebuffLabels =
    [
        "Any",
        "None",
        "HeadStack",
        "Circle",
        "Fan",
    ];

    private static readonly string[] ParticipantSideLabels =
    [
        "Any",
        "ResolvingGroup",
        "SupportGroup",
    ];

    private static readonly string[] RankLabels =
    [
        "Any",
        "1",
        "2",
        "3",
        "4",
    ];

    private static readonly string[] WaveGroupLabels =
    [
        "Group A",
        "Group B",
        "All",
    ];

    public enum LiveDebuffKind
    {
        Any,
        None,
        HeadStack,
        Circle,
        Fan
    }

    public enum ParticipantSide
    {
        Any,
        ResolvingGroup,
        SupportGroup
    }

    public enum WaveGroupKind
    {
        GroupA,
        GroupB,
        All
    }

    public enum AutoAssignmentMode
    {
        InitialForecastPriority,
        PriorityIndexPairs1238_4567
    }

    public enum PositionBasis
    {
        ReferenceTower,
        PairedTower,
        LeftTower,
        RightTower,
        TowerPairCenter,
        ArenaCenter,
        OppositeTowerPairCenter
    }

    public enum StageKind
    {
        Tower,
        Past,
        Future,
        AllThingsEnding
    }

    public enum BasicStageKind
    {
        Tower
    }

    public enum RoleKind
    {
        Head1,
        Head2,
        Head3,
        Head4,
        Circle1,
        Circle2,
        Circle3,
        Circle4,
        Fan1,
        Fan2,
        Fan3,
        Fan4,
        Support1,
        Support2,
        Support3,
        Support4
    }

    public sealed class PositionRule
    {
        public PositionBasis Basis;
        public float AngleDeg;
        public float Range;

        public PositionRule()
        {
        }

        public PositionRule(PositionBasis basis, float angleDeg, float range)
        {
            Basis = basis;
            AngleDeg = angleDeg;
            Range = range;
        }

        public void Ensure()
        {
            if ((int)Basis < 0 || (int)Basis >= BasisLabels.Length)
                Basis = PositionBasis.ArenaCenter;
            if (Range < 0f)
                Range = 0f;
        }
    }

    public sealed class PatternInfo
    {
        public int Head = -1;
        public int Circle = -1;
        public int Fan = -1;

        public PatternInfo()
        {
        }

        public PatternInfo(int head, int circle, int fan)
        {
            Head = head;
            Circle = circle;
            Fan = fan;
        }

        public void Ensure()
        {
            Head = Math.Clamp(Head, -1, 4);
            Circle = Math.Clamp(Circle, -1, 4);
            Fan = Math.Clamp(Fan, -1, 4);
        }

        public bool Matches(PatternInfo actual)
        {
            return (Head < 0 || Head == actual.Head) &&
                   (Circle < 0 || Circle == actual.Circle) &&
                   (Fan < 0 || Fan == actual.Fan);
        }

        public static PatternInfo FromPlayers(IEnumerable<IPlayerCharacter> players)
        {
            var info = new PatternInfo(0, 0, 0);
            foreach (var player in players)
            {
                switch (CurrentDebuffFromPlayer(player))
                {
                    case LiveDebuffKind.HeadStack:
                        info.Head++;
                        break;
                    case LiveDebuffKind.Circle:
                        info.Circle++;
                        break;
                    case LiveDebuffKind.Fan:
                        info.Fan++;
                        break;
                }
            }

            return info;
        }
    }

    public readonly record struct LiveContext(
        ParticipantSide Side,
        LiveDebuffKind Debuff,
        int DebuffRank,
        int SupportRank,
        PatternInfo Pattern);

    public sealed class RoleSelector
    {
        public ParticipantSide Side = ParticipantSide.ResolvingGroup;
        public LiveDebuffKind Debuff = LiveDebuffKind.Any;
        public int Rank;

        public RoleSelector()
        {
        }

        public RoleSelector(ParticipantSide side, LiveDebuffKind debuff, int rank)
        {
            Side = side;
            Debuff = debuff;
            Rank = rank;
        }

        public void Ensure()
        {
            if ((int)Side < 0 || (int)Side >= ParticipantSideLabels.Length)
                Side = ParticipantSide.ResolvingGroup;
            if ((int)Debuff < 0 || (int)Debuff >= LiveDebuffLabels.Length)
                Debuff = LiveDebuffKind.Any;
            Rank = Math.Clamp(Rank, 0, 4);
        }

        public bool Matches(LiveContext context)
        {
            if (Side != ParticipantSide.Any && context.Side != Side)
                return false;
            if (Debuff != LiveDebuffKind.Any && context.Debuff != Debuff)
                return false;

            var rank = context.Side == ParticipantSide.SupportGroup && Debuff == LiveDebuffKind.Any
                ? context.SupportRank
                : context.DebuffRank;
            return Rank <= 0 || rank == Rank;
        }

        public static RoleSelector FromLegacy(RoleKind role)
        {
            return role switch
            {
                RoleKind.Head1 => new RoleSelector(ParticipantSide.ResolvingGroup, LiveDebuffKind.HeadStack, 1),
                RoleKind.Head2 => new RoleSelector(ParticipantSide.ResolvingGroup, LiveDebuffKind.HeadStack, 2),
                RoleKind.Head3 => new RoleSelector(ParticipantSide.ResolvingGroup, LiveDebuffKind.HeadStack, 3),
                RoleKind.Head4 => new RoleSelector(ParticipantSide.ResolvingGroup, LiveDebuffKind.HeadStack, 4),
                RoleKind.Circle1 => new RoleSelector(ParticipantSide.ResolvingGroup, LiveDebuffKind.Circle, 1),
                RoleKind.Circle2 => new RoleSelector(ParticipantSide.ResolvingGroup, LiveDebuffKind.Circle, 2),
                RoleKind.Circle3 => new RoleSelector(ParticipantSide.ResolvingGroup, LiveDebuffKind.Circle, 3),
                RoleKind.Circle4 => new RoleSelector(ParticipantSide.ResolvingGroup, LiveDebuffKind.Circle, 4),
                RoleKind.Fan1 => new RoleSelector(ParticipantSide.ResolvingGroup, LiveDebuffKind.Fan, 1),
                RoleKind.Fan2 => new RoleSelector(ParticipantSide.ResolvingGroup, LiveDebuffKind.Fan, 2),
                RoleKind.Fan3 => new RoleSelector(ParticipantSide.ResolvingGroup, LiveDebuffKind.Fan, 3),
                RoleKind.Fan4 => new RoleSelector(ParticipantSide.ResolvingGroup, LiveDebuffKind.Fan, 4),
                RoleKind.Support1 => new RoleSelector(ParticipantSide.SupportGroup, LiveDebuffKind.Any, 1),
                RoleKind.Support2 => new RoleSelector(ParticipantSide.SupportGroup, LiveDebuffKind.Any, 2),
                RoleKind.Support3 => new RoleSelector(ParticipantSide.SupportGroup, LiveDebuffKind.Any, 3),
                RoleKind.Support4 => new RoleSelector(ParticipantSide.SupportGroup, LiveDebuffKind.Any, 4),
                _ => new RoleSelector(ParticipantSide.ResolvingGroup, LiveDebuffKind.Any, 0)
            };
        }
    }

    public sealed class RolePlacement
    {
        public bool Enabled = true;
        public RoleKind Role;
        public RoleSelector Selector;
        public InternationalString Text = new() { En = "Role", Jp = "役割" };
        public PositionRule Position = new();

        public RolePlacement()
        {
        }

        public RolePlacement(RoleKind role, string en, string jp, PositionBasis basis, float angle, float range)
        {
            Role = role;
            Selector = RoleSelector.FromLegacy(role);
            Selector.Ensure();
            Text = new InternationalString { En = en, Jp = jp };
            Position = new PositionRule(basis, angle, range);
        }

        public RolePlacement(ParticipantSide side, LiveDebuffKind debuff, int rank, string en, string jp, PositionBasis basis, float angle, float range)
        {
            Role = RoleKind.Head1;
            Selector = new RoleSelector(side, debuff, rank);
            Text = new InternationalString { En = en, Jp = jp };
            Position = new PositionRule(basis, angle, range);
        }

        public void Ensure()
        {
            if ((int)Role < 0 || (int)Role >= Enum.GetValues<RoleKind>().Length)
                Role = RoleKind.Head1;
            Selector ??= RoleSelector.FromLegacy(Role);
            Selector.Ensure();
            Text ??= new InternationalString { En = "Role", Jp = "役割" };
            Position ??= new PositionRule();
            Position.Ensure();
        }

        public bool Matches(LiveContext context)
        {
            if (!Enabled) return false;

            Selector ??= RoleSelector.FromLegacy(Role);
            Selector.Ensure();
            return Selector.Matches(context);
        }
    }

    public sealed class PatternPlacementConfig
    {
        public PatternInfo Pattern = new();
        public RolePlacement[] Placements = [];

        public PatternPlacementConfig()
        {
        }

        public PatternPlacementConfig(PatternInfo pattern, params RolePlacement[] placements)
        {
            Pattern = pattern;
            Placements = placements;
        }

        public void Ensure()
        {
            Pattern ??= new PatternInfo();
            Pattern.Ensure();
            Placements ??= [];
            foreach (var placement in Placements)
                placement?.Ensure();
        }
    }

    public sealed class BasicStageConfig
    {
        public BasicStageKind Kind;
        public PatternPlacementConfig[] Patterns = [];

        public BasicStageConfig()
        {
        }

        public BasicStageConfig(BasicStageKind kind, params PatternPlacementConfig[] patterns)
        {
            Kind = kind;
            Patterns = patterns;
        }

        public void Ensure()
        {
            if ((int)Kind < 0 || (int)Kind >= BasicStageCount)
                Kind = BasicStageKind.Tower;
            Patterns ??= [];
            foreach (var pattern in Patterns)
                pattern?.Ensure();
        }
    }

    public sealed class WaveConfig
    {
        public WaveGroupKind ResolvingGroup;

        public void Ensure(int wave)
        {
            if ((int)ResolvingGroup < 0 || (int)ResolvingGroup >= WaveGroupLabels.Length)
                ResolvingGroup = DefaultResolvingGroup(wave);
        }
    }

    public sealed class PriorityData4 : PriorityData
    {
        public override int GetNumPlayers() => 4;
    }

    public sealed class Config : IEzConfig
    {
        public AutoAssignmentMode AssignmentMode;

        public InternationalString ActiveInstructionText = new()
        {
            En = "W{0} {1}: go to {2}",
            Jp = "{0}回目 {1}: {2}へ"
        };

        public InternationalString AllThingsEndingStageLabelText = new()
        {
            En = "All Things Ending",
            Jp = "消滅の脚"
        };

        public InternationalString AssignmentHeaderText = new()
        {
            En = "Assignment",
            Jp = "割当"
        };

        public InternationalString BasicPositionsHeaderText = new()
        {
            En = "Basic position table",
            Jp = "基本配置表"
        };

        public InternationalString CollectingAssignmentsText = new()
        {
            En = "Forsaken: collecting initial forecasts",
            Jp = "ミッシング: 初期予兆を収集中"
        };

        public InternationalString DestinationOverlayText = new()
        {
            En = "W{0} {1} {2}",
            Jp = "{0}回目 {1} {2}"
        };

        public InternationalString DisplayTextHeaderText = new()
        {
            En = "Display text",
            Jp = "表示テキスト"
        };

        public InternationalString EastLabelText = new()
        {
            En = "E",
            Jp = "東"
        };

        public InternationalString FutureStageLabelText = new()
        {
            En = "Future's End",
            Jp = "未来の終焉"
        };

        public PriorityData4 GroupA = new()
        {
            Name = "Forsaken beta Group A",
            Description = "Optional fixed Group A. Leave empty to auto-capture from the initial Missing forecast."
        };

        public PriorityData4 GroupB = new()
        {
            Name = "Forsaken beta Group B",
            Description = "Optional fixed Group B. Leave empty to use the complement of auto Group A."
        };

        public InternationalString HeadStackDebuffText = new()
        {
            En = "Head stack",
            Jp = "頭割り"
        };

        public InternationalString InactiveInstructionText = new()
        {
            En = "W{0} {1}: no matching rule during {2}",
            Jp = "{0}回目 {1}: {2}に一致Ruleなし"
        };

        public InternationalString CircleDebuffText = new()
        {
            En = "Circle",
            Jp = "円"
        };

        public InternationalString FanDebuffText = new()
        {
            En = "Fan",
            Jp = "扇"
        };

        public InternationalString NoDebuffText = new()
        {
            En = "No debuff",
            Jp = "デバフなし"
        };

        public InternationalString NorthEastLabelText = new()
        {
            En = "NE",
            Jp = "北東"
        };

        public InternationalString NorthLabelText = new()
        {
            En = "N",
            Jp = "北"
        };

        public InternationalString NorthWestLabelText = new()
        {
            En = "NW",
            Jp = "北西"
        };

        public InternationalString PairedTowerPreviewText = new()
        {
            En = "Paired tower",
            Jp = "ペア塔"
        };

        public InternationalString PastFutureDescriptionText = new()
        {
            En = "Past and Future are fixed tower-relative movements. Tower and All Things Ending share the same role placement table.",
            Jp = "過去と未来は塔基準の固定移動として扱います。塔と消滅の脚は同じ役割配置表を使います。"
        };

        public InternationalString PastFutureHeaderText = new()
        {
            En = "Past/Future fixed movement",
            Jp = "過去/未来 固定移動"
        };

        public InternationalString PastFixedText = new()
        {
            En = "Tower gap",
            Jp = "塔間"
        };

        public PositionRule PastFixedPosition = new(PositionBasis.TowerPairCenter, 0f, 0f);

        public InternationalString PastStageLabelText = new()
        {
            En = "Past's End",
            Jp = "過去の終焉"
        };

        public InternationalString FutureFixedText = new()
        {
            En = "Opposite side",
            Jp = "反対側"
        };

        public PositionRule FutureFixedPosition = new(PositionBasis.OppositeTowerPairCenter, 0f, 0f);

        public int PreviewWave = 1;
        public StageKind PreviewStage = StageKind.Tower;

        public PriorityData PriorityData = new()
        {
            Name = "Forsaken beta global priority",
            Description = "Used for auto Group A circle/fan selection and dynamic same-debuff rank ordering. Default order is fitted to the observed AAABBBBA fixed-partner samples.",
            PriorityLists =
            [
                new PriorityList
                {
                    IsRole = true,
                    List = CreateDefaultPriorityList()
                }
            ]
        };

        public InternationalString ReferenceTowerPreviewText = new()
        {
            En = "Reference tower",
            Jp = "基準塔"
        };

        public bool ShowPreview;

        public InternationalString SouthEastLabelText = new()
        {
            En = "SE",
            Jp = "南東"
        };

        public InternationalString SouthLabelText = new()
        {
            En = "S",
            Jp = "南"
        };

        public InternationalString SouthWestLabelText = new()
        {
            En = "SW",
            Jp = "南西"
        };

        public InternationalString TowerStageLabelText = new()
        {
            En = "Tower",
            Jp = "塔"
        };

        public InternationalString WaitingForAssignmentText = new()
        {
            En = "Forsaken: waiting for initial slots",
            Jp = "ミッシング: 初期スロット待ち"
        };

        public InternationalString WaitingForWaveText = new()
        {
            En = "{0}: wait for wave",
            Jp = "{0}: Wave待ち"
        };

        public InternationalString WaveHeaderText = new()
        {
            En = "Wave {0}",
            Jp = "{0}回目"
        };

        public InternationalString WaveTableHeaderText = new()
        {
            En = "Wave table",
            Jp = "Wave表"
        };

        public BasicStageConfig[] BasicStages = CreateDefaultBasicStages();

        public WaveConfig[] Waves = CreateDefaultWaves();

        public int DefaultsVersion;

        public InternationalString WestLabelText = new()
        {
            En = "W",
            Jp = "西"
        };

        public void EnsureDefaults()
        {
            PriorityData ??= new PriorityData();
            GroupA ??= new PriorityData4();
            GroupB ??= new PriorityData4();
            if ((int)AssignmentMode < 0 || (int)AssignmentMode >= AssignmentModeLabels.Length)
                AssignmentMode = AutoAssignmentMode.InitialForecastPriority;
            PastFixedText ??= new InternationalString { En = "Tower gap", Jp = "塔間" };
            FutureFixedText ??= new InternationalString { En = "Opposite side", Jp = "反対側" };
            PastFixedPosition ??= new PositionRule(PositionBasis.TowerPairCenter, 0f, 0f);
            FutureFixedPosition ??= new PositionRule(PositionBasis.OppositeTowerPairCenter, 0f, 0f);
            PastFixedPosition.Ensure();
            FutureFixedPosition.Ensure();

            if (BasicStages == null || BasicStages.Length != BasicStageCount)
            {
                var old = BasicStages ?? [];
                BasicStages = CreateDefaultBasicStages();
                for (var i = 0; i < Math.Min(old.Length, BasicStages.Length); i++)
                {
                    if (old[i] == null) continue;
                    old[i].Kind = BasicStageKind.Tower;
                    BasicStages[i] = old[i];
                }
            }

            for (var i = 0; i < BasicStages.Length; i++)
            {
                BasicStages[i] ??= CreateDefaultBasicStages()[i];
                BasicStages[i].Ensure();
            }

            if (Waves == null || Waves.Length != WaveCount)
            {
                var old = Waves ?? [];
                Waves = CreateDefaultWaves();
                for (var i = 0; i < Math.Min(old.Length, Waves.Length); i++)
                    if (old[i] != null)
                        Waves[i] = old[i];
            }

            if (DefaultsVersion < 2)
            {
                DefaultsVersion = 2;
            }

            if (DefaultsVersion < 3)
                DefaultsVersion = 3;

            if (DefaultsVersion < 4)
            {
                DefaultsVersion = 4;
            }

            if (DefaultsVersion < 5)
            {
                MigrateValidatedCoordinateDefaults();
                DefaultsVersion = 5;
            }

            if (DefaultsVersion < 6)
            {
                MigrateSupportSelectorDefaults();
                DefaultsVersion = 6;
            }

            if (DefaultsVersion < 7)
            {
                MigrateDefaultPriorityOrder();
                DefaultsVersion = 7;
            }

            for (var i = 0; i < Waves.Length; i++)
            {
                Waves[i] ??= new WaveConfig();
                Waves[i].Ensure(i + 1);
            }

            PreviewWave = Math.Clamp(PreviewWave, 1, WaveCount);
            if ((int)PreviewStage < 0 || (int)PreviewStage >= StageCount)
                PreviewStage = StageKind.Tower;
        }

        private void MigrateValidatedCoordinateDefaults()
        {
            var stage = BasicStages?.FirstOrDefault(item => item.Kind == BasicStageKind.Tower);
            if (stage == null) return;

            var h2c1f1 = stage.Patterns.FirstOrDefault(item => item.Pattern.Matches(new PatternInfo(2, 1, 1)));
            if (h2c1f1 != null)
            {
                UpdateIfOldDefault(h2c1f1, RoleKind.Head1, PositionBasis.LeftTower, 60f, 3.25f, PositionBasis.LeftTower, 0f, 2.2f);
                UpdateIfOldDefault(h2c1f1, RoleKind.Head2, PositionBasis.RightTower, 120f, 3.25f, PositionBasis.RightTower, 10f, 3.2f);
                UpdateIfOldDefault(h2c1f1, RoleKind.Fan1, PositionBasis.LeftTower, 330f, 3.25f, PositionBasis.LeftTower, 180f, 1.9f);
                UpdateIfOldDefault(h2c1f1, RoleKind.Circle1, PositionBasis.RightTower, 270f, 3.25f, PositionBasis.RightTower, 187f, 3.4f);
            }

            var h0c2f2 = stage.Patterns.FirstOrDefault(item => item.Pattern.Matches(new PatternInfo(0, 2, 2)));
            if (h0c2f2 != null)
            {
                UpdateIfOldDefault(h0c2f2, RoleKind.Fan1, PositionBasis.LeftTower, 30f, 3.25f, PositionBasis.LeftTower, 182f, 3.54f);
                UpdateIfOldDefault(h0c2f2, RoleKind.Fan2, PositionBasis.LeftTower, 0f, 3.25f, PositionBasis.LeftTower, 205f, 3.46f);
                UpdateIfOldDefault(h0c2f2, RoleKind.Circle1, PositionBasis.RightTower, 270f, 3.25f, PositionBasis.RightTower, 278f, 3.55f);
                UpdateIfOldDefault(h0c2f2, RoleKind.Circle2, PositionBasis.RightTower, 90f, 3.25f, PositionBasis.RightTower, 104f, 3.5f);
            }
        }

        private void MigrateSupportSelectorDefaults()
        {
            var stage = BasicStages?.FirstOrDefault(item => item.Kind == BasicStageKind.Tower);
            if (stage == null) return;

            var h2c1f1 = stage.Patterns.FirstOrDefault(item => item.Pattern.Matches(new PatternInfo(2, 1, 1)));
            if (h2c1f1 != null)
            {
                UpdateSupportIfOldDefault(h2c1f1, RoleKind.Support1, PositionBasis.LeftTower, 330f, 4.75f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Fan, 2, "Bait fan", "扇誘導", PositionBasis.LeftTower, 180f, 4.60f);
                UpdateSupportIfOldDefault(h2c1f1, RoleKind.Support2, PositionBasis.LeftTower, 60f, 4.75f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Fan, 1, "Head 1 support", "頭1補助", PositionBasis.LeftTower, 358f, 4.59f);
                UpdateSupportIfOldDefault(h2c1f1, RoleKind.Support3, PositionBasis.RightTower, 120f, 4.75f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Circle, 1, "Head 2 support", "頭2補助", PositionBasis.RightTower, 9f, 4.71f);
                UpdateSupportIfOldDefault(h2c1f1, RoleKind.Support4, PositionBasis.RightTower, 120f, 4.75f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Circle, 2, "Head 2 support", "頭2補助", PositionBasis.RightTower, 9f, 4.71f);
            }

            var h0c2f2 = stage.Patterns.FirstOrDefault(item => item.Pattern.Matches(new PatternInfo(0, 2, 2)));
            if (h0c2f2 != null)
            {
                UpdateSupportIfOldDefault(h0c2f2, RoleKind.Support1, PositionBasis.ArenaCenter, 0f, 0f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Circle, 1, "Outer circle", "外側円", PositionBasis.ArenaCenter, 271f, 6.14f);
                UpdateSupportIfOldDefault(h0c2f2, RoleKind.Support2, PositionBasis.ArenaCenter, 0f, 5f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Circle, 2, "Inner circle", "内側円", PositionBasis.ArenaCenter, 350f, 3.55f);
                UpdateSupportIfOldDefault(h0c2f2, RoleKind.Support3, PositionBasis.ArenaCenter, 45f, 5f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Fan, 1, "Back fan", "奥扇", PositionBasis.ArenaCenter, 201f, 4.56f);
                UpdateSupportIfOldDefault(h0c2f2, RoleKind.Support4, PositionBasis.ArenaCenter, 315f, 5f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Fan, 2, "Side fan", "横扇", PositionBasis.ArenaCenter, 96f, 3.58f);
            }
        }

        private void MigrateDefaultPriorityOrder()
        {
            var list = PriorityData.PriorityLists?.FirstOrDefault(item => item.IsRole);
            if (list?.List == null) return;

            var currentRoles = list.List.Select(item => item.Role).ToArray();
            if (currentRoles.Length != LegacyGenericRolePriority.Length)
                return;

            for (var i = 0; i < LegacyGenericRolePriority.Length; i++)
                if (currentRoles[i] != LegacyGenericRolePriority[i])
                    return;

            list.List = CreateDefaultPriorityList();
        }

        private static void UpdateIfOldDefault(
            PatternPlacementConfig pattern,
            RoleKind role,
            PositionBasis oldBasis,
            float oldAngle,
            float oldRange,
            PositionBasis newBasis,
            float newAngle,
            float newRange)
        {
            var placement = pattern.Placements.FirstOrDefault(item => item.Role == role);
            if (placement?.Position == null) return;
            if (placement.Position.Basis != oldBasis) return;
            if (!NearlyEqual(placement.Position.AngleDeg, oldAngle)) return;
            if (!NearlyEqual(placement.Position.Range, oldRange)) return;

            placement.Position.Basis = newBasis;
            placement.Position.AngleDeg = newAngle;
            placement.Position.Range = newRange;
        }

        private static void UpdateSupportIfOldDefault(
            PatternPlacementConfig pattern,
            RoleKind role,
            PositionBasis oldBasis,
            float oldAngle,
            float oldRange,
            ParticipantSide side,
            LiveDebuffKind debuff,
            int rank,
            string en,
            string jp,
            PositionBasis newBasis,
            float newAngle,
            float newRange)
        {
            var placement = pattern.Placements.FirstOrDefault(item => item.Role == role);
            if (placement?.Position == null) return;
            if (placement.Position.Basis != oldBasis) return;
            if (!NearlyEqual(placement.Position.AngleDeg, oldAngle)) return;
            if (!NearlyEqual(placement.Position.Range, oldRange)) return;

            placement.Selector = new RoleSelector(side, debuff, rank);
            placement.Text = new InternationalString { En = en, Jp = jp };
            placement.Position.Basis = newBasis;
            placement.Position.AngleDeg = newAngle;
            placement.Position.Range = newRange;
        }

        private static bool NearlyEqual(float left, float right)
        {
            return MathF.Abs(left - right) < 0.001f;
        }
    }

    private static WaveConfig[] CreateDefaultWaves()
    {
        var waves = new WaveConfig[WaveCount];
        for (var i = 0; i < waves.Length; i++)
            waves[i] = new WaveConfig { ResolvingGroup = DefaultResolvingGroup(i + 1) };
        return waves;
    }

    private static List<JobbedPlayer> CreateDefaultPriorityList()
    {
        return DefaultRolePriority.Select(role => new JobbedPlayer { Role = role }).ToList();
    }

    private static BasicStageConfig[] CreateDefaultBasicStages()
    {
        return
        [
            CreateDefaultTowerStage(BasicStageKind.Tower)
        ];
    }

    private static BasicStageConfig CreateDefaultTowerStage(BasicStageKind kind)
    {
        return new BasicStageConfig(
            kind,
            new PatternPlacementConfig(
                new PatternInfo(1, 0, 3),
                Role(RoleKind.Head1, "Debuff stack", "デバフ頭割り", PositionBasis.LeftTower, 240f, 3.25f),
                Role(RoleKind.Fan1, "Debuff fan 1", "デバフ扇1", PositionBasis.LeftTower, 300f, 3.25f),
                Role(RoleKind.Fan2, "Debuff fan 2", "デバフ扇2", PositionBasis.RightTower, 270f, 3.25f),
                Role(RoleKind.Fan3, "Debuff fan 3", "デバフ扇3", PositionBasis.RightTower, 300f, 3.25f),
                Role(RoleKind.Support1, "Bait fan", "扇誘導", PositionBasis.LeftTower, 330f, 4.75f),
                Role(RoleKind.Support2, "Stack support", "頭割り補助", PositionBasis.LeftTower, 240f, 4.75f),
                Role(RoleKind.Support3, "Stack support", "頭割り補助", PositionBasis.LeftTower, 240f, 4.75f),
                Role(RoleKind.Support4, "Stack support", "頭割り補助", PositionBasis.LeftTower, 240f, 4.75f)),
            new PatternPlacementConfig(
                new PatternInfo(1, 3, 0),
                Role(RoleKind.Head1, "Debuff stack", "デバフ頭割り", PositionBasis.LeftTower, 240f, 3.25f),
                Role(RoleKind.Circle1, "Debuff circle 1", "デバフ円1", PositionBasis.LeftTower, 90f, 3.25f),
                Role(RoleKind.Circle2, "Debuff circle 2", "デバフ円2", PositionBasis.RightTower, 210f, 3.25f),
                Role(RoleKind.Circle3, "Debuff circle 3", "デバフ円3", PositionBasis.RightTower, 30f, 3.25f),
                Role(RoleKind.Support1, "Stack support", "頭割り補助", PositionBasis.LeftTower, 240f, 4.75f),
                Role(RoleKind.Support2, "Stack support", "頭割り補助", PositionBasis.LeftTower, 240f, 4.75f),
                Role(RoleKind.Support3, "Stack support", "頭割り補助", PositionBasis.LeftTower, 240f, 4.75f),
                Role(RoleKind.Support4, "Stack support", "頭割り補助", PositionBasis.LeftTower, 240f, 4.75f)),
            new PatternPlacementConfig(
                new PatternInfo(2, 1, 1),
                Role(RoleKind.Head1, "Debuff stack 1", "デバフ頭1", PositionBasis.LeftTower, 0f, 2.2f),
                Role(RoleKind.Head2, "Debuff stack 2", "デバフ頭2", PositionBasis.RightTower, 10f, 3.2f),
                Role(RoleKind.Fan1, "Debuff fan", "デバフ扇", PositionBasis.LeftTower, 180f, 1.9f),
                Role(RoleKind.Circle1, "Debuff circle", "デバフ円", PositionBasis.RightTower, 187f, 3.4f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Circle, 1, "Head 2 support", "頭2補助", PositionBasis.RightTower, 9f, 4.71f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Circle, 2, "Head 2 support", "頭2補助", PositionBasis.RightTower, 9f, 4.71f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Fan, 1, "Head 1 support", "頭1補助", PositionBasis.LeftTower, 358f, 4.59f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Fan, 2, "Bait fan", "扇誘導", PositionBasis.LeftTower, 180f, 4.60f)),
            new PatternPlacementConfig(
                new PatternInfo(0, 2, 2),
                Role(RoleKind.Fan1, "Debuff fan 1", "デバフ扇1", PositionBasis.LeftTower, 182f, 3.54f),
                Role(RoleKind.Fan2, "Debuff fan 2", "デバフ扇2", PositionBasis.LeftTower, 205f, 3.46f),
                Role(RoleKind.Circle1, "Debuff circle 1", "デバフ円1", PositionBasis.RightTower, 278f, 3.55f),
                Role(RoleKind.Circle2, "Debuff circle 2", "デバフ円2", PositionBasis.RightTower, 104f, 3.5f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Circle, 1, "Outer circle", "外側円", PositionBasis.ArenaCenter, 271f, 6.14f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Circle, 2, "Inner circle", "内側円", PositionBasis.ArenaCenter, 350f, 3.55f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Fan, 1, "Back fan", "奥扇", PositionBasis.ArenaCenter, 201f, 4.56f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Fan, 2, "Side fan", "横扇", PositionBasis.ArenaCenter, 96f, 3.58f)),
            new PatternPlacementConfig(
                new PatternInfo(4, 0, 0),
                Role(RoleKind.Head1, "Head pattern 4-1", "頭4処理1", PositionBasis.LeftTower, 90f, 3.25f),
                Role(RoleKind.Head2, "Head pattern 4-2", "頭4処理2", PositionBasis.LeftTower, 270f, 3.25f),
                Role(RoleKind.Head3, "Head pattern 4-3", "頭4処理3", PositionBasis.RightTower, 90f, 3.25f),
                Role(RoleKind.Head4, "Head pattern 4-4", "頭4処理4", PositionBasis.RightTower, 270f, 3.25f),
                Role(RoleKind.Support1, "Demise spread 1", "消滅散開1", PositionBasis.ArenaCenter, 0f, 0f),
                Role(RoleKind.Support2, "Demise spread 2", "消滅散開2", PositionBasis.ArenaCenter, 0f, 5f),
                Role(RoleKind.Support3, "Demise spread 3", "消滅散開3", PositionBasis.ArenaCenter, 45f, 5f),
                Role(RoleKind.Support4, "Demise spread 4", "消滅散開4", PositionBasis.ArenaCenter, 315f, 5f)));
    }

    private static RolePlacement Role(RoleKind role, string en, string jp, PositionBasis basis, float angle, float range)
    {
        return new RolePlacement(role, en, jp, basis, angle, range);
    }

    private static RolePlacement Role(ParticipantSide side, LiveDebuffKind debuff, int rank, string en, string jp, PositionBasis basis, float angle, float range)
    {
        return new RolePlacement(side, debuff, rank, en, jp, basis, angle, range);
    }

    private static WaveGroupKind DefaultResolvingGroup(int wave)
    {
        return wave is >= 1 and <= WaveCount ? DefaultWaveSequence[wave - 1] : WaveGroupKind.GroupA;
    }
}
