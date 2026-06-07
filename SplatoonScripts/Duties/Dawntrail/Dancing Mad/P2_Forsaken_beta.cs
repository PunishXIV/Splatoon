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
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate;

public class P2_Forsaken_beta : SplatoonScript<P2_Forsaken_beta.Config>
{
    private const uint TerritoryDancingMadUltimate = 1363;
    private const uint Forsaken = 47804;
    private const uint UltimateEmbrace = 49740;
    private const uint FuturesEndCast = 47826;
    private const uint PastsEndCast = 47827;
    private const uint FuturesEndAction1 = 47830;
    private const uint PastsEndAction = 47831;
    private const uint FuturesEndAction2 = 47832;
    private const uint AllThingsEndingCast1 = 47836;
    private const uint AllThingsEndingCast2 = 47837;
    private const uint MissingInventoryStatus = 5083;
    private const uint MissingHeadStackStatus = 5084;
    private const uint MissingCircleStatus = 5085;
    private const uint MissingFanStatus = 5086;
    private const int WaveCount = 8;
    private const int StageCount = 4;
    private const int BasicStageCount = 1;
    private const int PairCount = 4;
    private const int PreviewElementCount = 64;
    private const string PreviewElementPrefix = "Preview";
    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);
    private const float TowerOffsetCardinal = 8f;
    private const float TowerOffsetDiagonal = 5.7f;
    private const int CurrentDefaultsVersion = 13;

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
        RolePosition.H1,
        RolePosition.T2,
        RolePosition.T1,
        RolePosition.M1,
        RolePosition.M2,
        RolePosition.R1,
        RolePosition.R2
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

    private static readonly RolePosition[] LegacyIncompleteRolePriority =
    [
        RolePosition.H2,
        RolePosition.T2,
        RolePosition.M1,
        RolePosition.R2,
        RolePosition.R1,
        RolePosition.T1,
        RolePosition.H1
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
            "Forsaken beta is a dynamic P2 Missing helper. Settings are limited to global priority, optional pairs, wave sets, and compact role placement tables.",
        Jp =
            "Forsaken beta はP2ミッシング用の動的補助です。設定は全体優先順位、任意ペア、Wave処理セット、役割ごとの配置表に絞っています。"
    };

    private static readonly InternationalString AssignmentDescriptionText = new()
    {
        En =
            "Set global priority, optional explicit pairs, and how those pairs split into the 1/2/3/8 set and the 4/5/6/7 set. Without explicit pairs, the script uses priority pairs 1+3, 2+4, 5+7, and 6+8.",
        Jp =
            "全体優先順位、任意の明示ペア、そのペアを1/2/3/8組と4/5/6/7組へ分ける方法を設定します。明示ペアがない場合は、優先順位の1+3、2+4、5+7、6+8をペアにします。"
    };

    private static readonly InternationalString PairSettingsDescriptionText = new()
    {
        En =
            "Explicit pairs override the priority-pair fallback when all four pairs match the party. Keep pair splitting off for whole-pair strategies where the pair containing a head-stack resolves together on waves 1/2/3/8.",
        Jp =
            "4ペアすべてがPTに一致する場合、優先順位ペアより明示ペアを優先します。頭割りを含むペアごと1/2/3/8回目を処理する戦略では、ペア分割をOFFのまま使います。"
    };

    private static readonly InternationalString SplitHeadStackPairsText = new()
    {
        En = "Split head-stack pairs",
        Jp = "頭割りペアを分割する"
    };

    private static readonly InternationalString SplitHeadStackPairsDescriptionText = new()
    {
        En =
            "Off: a pair containing one head-stack player resolves together on waves 1/2/3/8, and non-head pairs resolve together on waves 4/5/6/7. On: only the head-stack player from a head pair resolves on waves 1/2/3/8; that player's pair buddy resolves on waves 4/5/6/7. Pairs without a head-stack split by global priority, with the higher-priority player going to 1/2/3/8 and the lower-priority player going to 4/5/6/7.",
        Jp =
            "OFF: 頭割りを1名含むペアはペアごと1/2/3/8回目を処理し、頭割りを含まないペアはペアごと4/5/6/7回目を処理します。ON: 頭割りペアでは頭割り本人だけが1/2/3/8回目を処理し、その相方は4/5/6/7回目を処理します。頭割りを含まないペアは全体優先順位で分割し、優先順位が高い人を1/2/3/8、低い人を4/5/6/7へ送ります。"
    };

    private static readonly InternationalString InitialHeadStackRankModeText = new()
    {
        En = "Initial head-stack rank",
        Jp = "1回目頭割りランク"
    };

    private static readonly InternationalString InitialHeadStackRankModeDescriptionText = new()
    {
        En =
            "Controls wave 1 head-stack left/right assignment. Partner debuff uses the head player's pair buddy marker. Priority order uses the current global priority. Role side sends global-priority positions 1-4 to the left head-stack slot and positions 5-8 to the right head-stack slot.",
        Jp =
            "1回目の頭割り左右を決めます。相方デバフは頭割り本人のペア相方マーカーを使います。優先順位は現在の全体優先順位をそのまま使います。ロール側は全体優先順位の1-4番を左頭割り、5-8番を右頭割りにします。"
    };

    private static readonly InternationalString[] InitialHeadStackRankModeLabelTexts =
    [
        new() { En = "Partner debuff", Jp = "相方デバフ" },
        new() { En = "Priority order", Jp = "優先順位" },
        new() { En = "Role side", Jp = "ロール側" }
    ];

    private static readonly InternationalString PairHeaderText = new()
    {
        En = "Pair {0}",
        Jp = "ペア{0}"
    };

    private static readonly InternationalString PairValidationHeaderText = new()
    {
        En = "Pair validation",
        Jp = "ペア設定チェック"
    };

    private static readonly InternationalString WaveTableDescriptionText = new()
    {
        En =
            "Each wave chooses which set resolves towers. The default AAABBBBA sequence uses the first set on waves 1-3 and 8, and the second set on waves 4-7. Tower and All Things Ending share one live-pattern role placement table; Past/Future use fixed tower-relative movement.",
        Jp =
            "各Waveでどちらのセットが塔を処理するかを選びます。初期値はAAABBBBA、つまり1-3回目と8回目が前半セット、4-7回目が後半セットです。塔と消滅の脚は現在パターンと役割ごとの共通配置表を使い、過去/未来は塔基準の固定移動にします。"
    };

    private readonly List<uint> _pendingTowerSpawnPositions = [];
    private readonly List<uint> _pendingTowerClearPositions = [];
    private readonly List<uint> _firstSetIds = [];
    private readonly List<uint> _secondSetIds = [];
    private readonly Dictionary<uint, LiveDebuffKind> _initialHeadPartnerDebuffs = [];
    private readonly Dictionary<uint, LiveDebuffKind> _observedDebuffs = [];
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
    private int _observedTowerWave;
    private uint _referenceMapPosition;
    private bool _hasPendingTowerDisplay;
    private int _pendingTowerDisplayWave;
    private uint _pendingTowerDisplayReference;
    private StageKind _currentStage;
    private readonly Dictionary<uint, LiveContext> _stageContexts = [];
    private bool _allowLiveContextRefresh;
    private bool _waitingForLiveDebuffRefresh;
    private int _waitingForLiveDebuffRefreshWave;
    private string _currentInstruction = "";
    private bool _hasDestination;
    private Vector3 _myDestination = Vector3.Zero;
    private int _settingsPreviewIndex;
    private bool _settingsPreviewDrewReference;
    private string _lastCaptureBlockLog = "";
    private string _lastContextFailureLog = "";
    private string _lastContextResolvedLog = "";
    private string _lastInstructionLog = "";

    public override HashSet<uint>? ValidTerritories { get; } = [TerritoryDancingMadUltimate];
    public override Metadata Metadata => new(9, "Garume");

    private new IPlayerCharacter BasePlayer => global::Splatoon.Splatoon.BasePlayer;

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
            radius = 0.25f,
            Donut = 0.1f,
            thicc = 3f,
            fillIntensity = 0.55f,
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
            _currentInstruction = DisplayText(C.ShowCollectingAssignmentsText, C.CollectingAssignmentsText);
            DebugLog($"CAST_START Forsaken active={_active}");
            ApplyDisplay();
            return;
        }

        if (!_active && !HasPartyMissingStatus()) return;

        if (castId is PastsEndCast or FuturesEndCast)
        {
            DebugLog($"CAST_START {(castId == PastsEndCast ? "Past's End" : "Future's End")} pendingWave={_pendingTowerDisplayWave} hasPending={_hasPendingTowerDisplay} observedWave={_observedTowerWave} currentWave={_currentWave}");
            TryActivatePendingTowerDisplay(StageKind.Tower, wave => wave is >= 4 and <= WaveCount && wave % 2 == 0);
            return;
        }

        if (IsAllThingsEnding(actionId: castId))
        {
            DebugLog($"CAST_START All Things Ending pendingWave={_pendingTowerDisplayWave} hasPending={_hasPendingTowerDisplay} observedWave={_observedTowerWave} currentWave={_currentWave}");
            TryActivatePendingTowerDisplay(StageKind.AllThingsEnding, wave => wave is >= 3 and <= WaveCount && wave % 2 == 1);
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        var actionId = set.Action?.RowId ?? 0;

        if (actionId == Forsaken)
        {
            _active = true;
            DebugLog("ACTION Forsaken");
            TryCaptureResolvingSets();
            UpdateWaitingInstruction();
            ApplyDisplay();
            return;
        }

        if (!_active && !HasPartyMissingStatus()) return;

        if (actionId == PastsEndAction)
        {
            DebugLog("ACTION Past's End resolved");
            SetStage(StageKind.Past);
        }
        else if (actionId is FuturesEndAction1 or FuturesEndAction2)
        {
            DebugLog("ACTION Future's End resolved");
            SetStage(StageKind.Future);
        }
    }

    private static bool IsAllThingsEnding(uint actionId) => actionId is AllThingsEndingCast1 or AllThingsEndingCast2;

    public override void OnGainBuffEffect(uint sourceId, Status status)
    {
        if (!IsMissingStatus(status.StatusId)) return;

        _active = true;
        var debuff = DebuffFromStatus(status.StatusId);
        if (debuff != LiveDebuffKind.None)
            _observedDebuffs[sourceId] = debuff;

        DebugLogOnce(ref _lastCaptureBlockLog, $"status-{sourceId:X8}-{status.StatusId}", $"STATUS_GAIN missing source=0x{sourceId:X8} status={status.StatusId} debuff={debuff}");
        TryCaptureResolvingSets();
        if (debuff != LiveDebuffKind.None && _allowLiveContextRefresh)
            _stageContexts.Clear();

        if (debuff != LiveDebuffKind.None
            && _waitingForLiveDebuffRefresh
            && _waitingForLiveDebuffRefreshWave == _currentWave
            && _hasStage)
        {
            _waitingForLiveDebuffRefresh = false;
            _waitingForLiveDebuffRefreshWave = 0;
            DebugLog($"LIVE_DEBUFF_REFRESH wave={_currentWave} status={status.StatusId}");
            UpdateStageInstruction();
            ApplyDisplay();
            return;
        }

        if (_hasStage)
            UpdateStageInstruction();
        else
            UpdateWaitingInstruction();

        ApplyDisplay();
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        var inMissing = _active || HasPartyMissingStatus();
        if (!inMissing) return;

        _active = true;
        TryCaptureResolvingSets();

        if (IsTowerSpawnMapEffect(data1, data2) && IsTowerMapPosition(position))
        {
            DebugLog($"MAP_EFFECT tower spawn pos={FormatMapPosition(position)} pendingBefore={FormatMapPositionList(_pendingTowerSpawnPositions)} observedWave={_observedTowerWave}");
            AddTowerSpawnPosition(position);
            return;
        }

        if (IsTowerClearMapEffect(data1, data2) && IsTowerMapPosition(position))
        {
            DebugLog($"MAP_EFFECT tower clear pos={FormatMapPosition(position)} pendingBefore={FormatMapPositionList(_pendingTowerClearPositions)} observedWave={_observedTowerWave} currentWave={_currentWave}");
            AddTowerClearPosition(position);
        }
    }

    public override void OnUpdate()
    {
        if (_active)
            TryCaptureResolvingSets();

        if (_active && _hasStage)
            UpdateStageInstruction();

        ApplyDisplay();

        ApplyScaleData();
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(200f);
        ImGui.SliderFloat("Text offset, px", ref C.VOffsetMod, -5, 5);
        ImGui.SetNextItemWidth(200f);
        ImGui.SliderFloat("Global overlay text scale multiplier", ref C.ScaleMod, 0, 3);
        C.EnsureDefaults();
        _settingsPreviewIndex = 0;
        _settingsPreviewDrewReference = false;

        ImGui.TextWrapped(MainDescriptionText.Get());
        ImGui.Separator();

        if (ImGui.CollapsingHeader(C.AssignmentHeaderText.Get(), ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            ImGui.TextWrapped(AssignmentDescriptionText.Get());
            ImGui.TextUnformatted("Global priority");
            C.PriorityData.Draw();
            ImGui.Spacing();
            DrawPairSettings();
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
            DrawDisplayTextSetting("Collecting assignments", ref C.ShowCollectingAssignmentsText, C.CollectingAssignmentsText);
            DrawDisplayTextSetting("Waiting for assignment", ref C.ShowWaitingForAssignmentText, C.WaitingForAssignmentText);
            DrawDisplayTextSetting("Waiting for wave", ref C.ShowWaitingForWaveText, C.WaitingForWaveText);
            DrawDisplayTextSetting("Active instruction", ref C.ShowActiveInstructionText, C.ActiveInstructionText);
            DrawDisplayTextSetting("Inactive instruction", ref C.ShowInactiveInstructionText, C.InactiveInstructionText);
            DrawDisplayTextSetting("Destination overlay", ref C.ShowDestinationOverlayText, C.DestinationOverlayText);
            DrawDisplayTextSetting("Head stack debuff", ref C.ShowHeadStackDebuffText, C.HeadStackDebuffText);
            DrawDisplayTextSetting("Circle debuff", ref C.ShowCircleDebuffText, C.CircleDebuffText);
            DrawDisplayTextSetting("Fan debuff", ref C.ShowFanDebuffText, C.FanDebuffText);
            DrawDisplayTextSetting("No debuff", ref C.ShowNoDebuffText, C.NoDebuffText);
            DrawDisplayTextSetting("Reference tower preview", ref C.ShowReferenceTowerPreviewText, C.ReferenceTowerPreviewText);
            DrawDisplayTextSetting("Paired tower preview", ref C.ShowPairedTowerPreviewText, C.PairedTowerPreviewText);
            ImGui.Unindent();
        }

        DrawDebugSettings();
    }

    private void DrawPairSettings()
    {
        ImGui.TextWrapped(PairSettingsDescriptionText.Get());
        ImGui.Checkbox(SplitHeadStackPairsText.Get(), ref C.SplitHeadStackPairs);
        ImGui.TextWrapped(SplitHeadStackPairsDescriptionText.Get());
        var initialHeadStackRankMode = (int)C.InitialHeadStackRankMode;
        ImGui.SetNextItemWidth(260f);
        if (ImGui.Combo(InitialHeadStackRankModeText.Get(), ref initialHeadStackRankMode,
                BuildInitialHeadStackRankModeLabels(), InitialHeadStackRankModeLabelTexts.Length))
            C.InitialHeadStackRankMode = (InitialHeadStackRankMode)Math.Clamp(initialHeadStackRankMode, 0,
                InitialHeadStackRankModeLabelTexts.Length - 1);
        ImGui.TextWrapped(InitialHeadStackRankModeDescriptionText.Get());
        ImGui.Spacing();
        for (var i = 0; i < PairCount; i++)
        {
            ImGui.PushID($"ExplicitPair{i}");
            ImGui.TextUnformatted(FormatText(PairHeaderText, i + 1));
            C.Pairs[i].Draw();
            ImGui.PopID();
        }

        DrawPairValidation();
    }

    private void DrawPairValidation()
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextUnformatted(PairValidationHeaderText.Get());

        foreach (var line in ValidatePairSettingsForCurrentParty())
        {
            ImGui.PushStyleColor(ImGuiCol.Text, PairValidationColor(line.Severity));
            ImGui.TextWrapped($"{PairValidationPrefix(line.Severity)} {line.Get()}");
            ImGui.PopStyleColor();
        }
    }

    private List<PairValidationLine> ValidatePairSettingsForCurrentParty()
    {
        var lines = new List<PairValidationLine>();
        if (C.Pairs == null || C.Pairs.Length != PairCount)
        {
            lines.Add(PairValidationLine.Error(
                "Pair settings are not initialized. Reopen the script settings or reload the script.",
                "ペア設定が初期化されていません。設定を開き直すか、スクリプトを再読み込みしてください。"));
            return lines;
        }

        var configured = C.Pairs.Select(PriorityDataHasConfiguredEntries).ToArray();
        var configuredCount = configured.Count(item => item);
        if (configuredCount == 0)
        {
            if (C.SplitHeadStackPairs)
            {
                lines.Add(PairValidationLine.Warning(
                    "Head-stack pair splitting is enabled, but no explicit pairs are configured. The script will still use fallback priority pairs 1+3, 2+4, 5+7, and 6+8.",
                    "頭割りペア分割がONですが、明示ペアが未設定です。このままだと優先順位の1+3、2+4、5+7、6+8をペアとして使用します。"));
            }
            else
            {
                lines.Add(PairValidationLine.Info(
                    "No explicit pairs are configured. The script will use priority pairs 1+3, 2+4, 5+7, and 6+8.",
                    "明示ペアは未設定です。優先順位の1+3、2+4、5+7、6+8をペアとして使用します。"));
            }

            return lines;
        }

        if (configuredCount != PairCount)
        {
            lines.Add(PairValidationLine.Error(
                $"Only {configuredCount}/{PairCount} pairs are configured. When any pair is configured, all four pairs must be valid or assignment will wait.",
                $"{PairCount}ペア中{configuredCount}ペアだけが設定されています。ペア設定を使う場合は4ペアすべてが有効でないと、割当は待機します。"));
        }

        var party = Controller.GetPartyMembers()
            .OfType<IPlayerCharacter>()
            .ToList();
        if (party.Count < 8)
        {
            lines.Add(PairValidationLine.Warning(
                $"Current party has {party.Count} members. Full pair validation needs the current 8-player party or replay party.",
                $"現在PTが{party.Count}人です。完全なペア検証には現在の8人PT、またはリプレイPTが必要です。"));
        }

        var used = new Dictionary<uint, (int PairIndex, IPlayerCharacter Player)>();
        var pairs = new List<(int Index, IPlayerCharacter First, IPlayerCharacter Second)>();
        var hasStaticError = lines.Any(line => line.Severity == PairValidationSeverity.Error);

        for (var i = 0; i < PairCount; i++)
        {
            if (!configured[i])
            {
                lines.Add(PairValidationLine.Error(
                    $"Pair {i + 1} is not configured.",
                    $"ペア{i + 1}が未設定です。"));
                hasStaticError = true;
                continue;
            }

            var pairPlayers = GetConfiguredGroup(C.Pairs[i], party);
            if (pairPlayers.Count != 2)
            {
                lines.Add(PairValidationLine.Error(
                    $"Pair {i + 1} resolves to {pairPlayers.Count} current party members. It must resolve to exactly two.",
                    $"ペア{i + 1}は現在PT内で{pairPlayers.Count}人として解決されています。必ず2人に解決される必要があります。"));
                hasStaticError = true;
                continue;
            }

            foreach (var player in pairPlayers)
            {
                if (used.TryGetValue(player.EntityId, out var previous))
                {
                    lines.Add(PairValidationLine.Error(
                        $"Pair {i + 1} overlaps with pair {previous.PairIndex + 1}: {player.Name}.",
                        $"ペア{i + 1}がペア{previous.PairIndex + 1}と重複しています: {player.Name}。"));
                    hasStaticError = true;
                    continue;
                }

                used[player.EntityId] = (i, player);
            }

            pairs.Add((i, pairPlayers[0], pairPlayers[1]));
        }

        if (used.Count != 0 && used.Count != 8)
        {
            lines.Add(PairValidationLine.Error(
                $"Explicit pairs cover {used.Count}/8 unique current party members.",
                $"明示ペアが現在PTの一意なメンバー{used.Count}/8人しか覆っていません。"));
            hasStaticError = true;
        }

        if (pairs.Count != PairCount || used.Count != 8)
            return lines;

        if (!hasStaticError)
        {
            lines.Add(PairValidationLine.Info(
                "Explicit pairs cover all eight current party members without overlap.",
                "明示ペアは現在PT8人を重複なく覆っています。"));
        }

        ValidateCurrentDebuffPairSplit(lines, pairs, C.SplitHeadStackPairs);
        return lines;
    }

    private void ValidateCurrentDebuffPairSplit(
        List<PairValidationLine> lines,
        IReadOnlyList<(int Index, IPlayerCharacter First, IPlayerCharacter Second)> pairs,
        bool splitHeadPairs)
    {
        var debuffs = pairs
            .SelectMany(pair => new[] { CurrentDebuff(pair.First), CurrentDebuff(pair.Second) })
            .ToList();
        var visibleDebuffs = debuffs.Count(debuff => debuff is LiveDebuffKind.HeadStack or LiveDebuffKind.Circle or LiveDebuffKind.Fan);
        if (visibleDebuffs == 0)
        {
            lines.Add(PairValidationLine.Info(
                "Current Missing debuffs are not visible, so only the static pair settings were checked.",
                "現在ミッシングのデバフが見えていないため、静的なペア設定のみを検証しました。"));
            return;
        }

        if (visibleDebuffs < 8)
        {
            lines.Add(PairValidationLine.Warning(
                $"Only {visibleDebuffs}/8 current Missing role debuffs are visible. Dynamic set validation may be incomplete.",
                $"現在見えているミッシング役割デバフは{visibleDebuffs}/8個です。動的なセット検証は不完全な可能性があります。"));
        }

        var dynamicErrors = 0;
        foreach (var pair in pairs)
        {
            var firstDebuff = CurrentDebuff(pair.First);
            var secondDebuff = CurrentDebuff(pair.Second);
            var firstIsHead = firstDebuff == LiveDebuffKind.HeadStack;
            var secondIsHead = secondDebuff == LiveDebuffKind.HeadStack;

            if (firstIsHead && secondIsHead)
            {
                lines.Add(PairValidationLine.Error(
                    $"Pair {pair.Index + 1} has two head-stack players right now: {pair.First.Name} / {pair.Second.Name}. It cannot determine first/second set assignment.",
                    $"ペア{pair.Index + 1}は現在、頭割り2人です: {pair.First.Name} / {pair.Second.Name}。前半/後半セットを決定できません。"));
                dynamicErrors++;
                continue;
            }

            if (firstIsHead || secondIsHead)
            {
                var partnerDebuff = firstIsHead ? secondDebuff : firstDebuff;
                if (partnerDebuff is not (LiveDebuffKind.Circle or LiveDebuffKind.Fan))
                {
                    lines.Add(PairValidationLine.Warning(
                        $"Pair {pair.Index + 1} has a head-stack player, but the partner is currently {partnerDebuff}. The script will wait until the partner is circle/fan.",
                        $"ペア{pair.Index + 1}は頭割り持ちを含みますが、相方の現在デバフは{partnerDebuff}です。相方が円/扇になるまでスクリプトは待機します。"));
                }

                continue;
            }

            if (firstDebuff is not (LiveDebuffKind.Circle or LiveDebuffKind.Fan) ||
                secondDebuff is not (LiveDebuffKind.Circle or LiveDebuffKind.Fan))
            {
                lines.Add(PairValidationLine.Warning(
                    $"Pair {pair.Index + 1} has no head-stack player and is currently {firstDebuff}/{secondDebuff}. The script needs both players to be circle/fan to classify the pair.",
                    $"ペア{pair.Index + 1}は頭割りを含まず、現在デバフは{firstDebuff}/{secondDebuff}です。ペアを分類するには2人とも円/扇である必要があります。"));
            }
        }

        var headCount = debuffs.Count(debuff => debuff == LiveDebuffKind.HeadStack);
        if (visibleDebuffs == 8 && headCount != 2)
        {
            lines.Add(PairValidationLine.Warning(
                $"Current snapshot has {headCount} head-stack debuffs. Initial Forsaken grouping normally has 2; this may be a later wave or an incomplete replay state.",
                $"現在のスナップショットでは頭割りデバフが{headCount}個です。ミッシング初期割当は通常2個なので、後続Waveまたは不完全なリプレイ状態の可能性があります。"));
        }

        if (visibleDebuffs == 8 && dynamicErrors == 0)
        {
            lines.Add(PairValidationLine.Info(
                splitHeadPairs
                    ? "Current debuffs can determine first/second set assignment by the split head-stack pair rule."
                    : "Current debuffs can determine first/second set assignment by the whole-pair rule.",
                splitHeadPairs
                    ? "現在デバフは頭割りペア分割ルールで前半/後半セットを決定可能です。"
                    : "現在デバフはペアごと処理ルールで前半/後半セットを決定可能です。"));
        }
    }

    private static Vector4 PairValidationColor(PairValidationSeverity severity)
    {
        return severity switch
        {
            PairValidationSeverity.Error => new Vector4(1f, 0.25f, 0.2f, 1f),
            PairValidationSeverity.Warning => new Vector4(1f, 0.78f, 0.25f, 1f),
            _ => new Vector4(0.7f, 0.9f, 1f, 1f)
        };
    }

    private static string PairValidationPrefix(PairValidationSeverity severity)
    {
        return severity switch
        {
            PairValidationSeverity.Error => "[Error]",
            PairValidationSeverity.Warning => "[Warning]",
            _ => "[OK]"
        };
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
            DrawDisplayTextSetting("Text", ref C.ShowPastFixedText, C.PastFixedText);
            DrawPositionRule(C.PastFixedPosition);
            ImGui.PopID();

            ImGui.PushID("FutureFixed");
            ImGui.TextUnformatted(C.FutureStageLabelText.Get());
            DrawDisplayTextSetting("Text", ref C.ShowFutureFixedText, C.FutureFixedText);
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
                DrawOpenPatternPreviewElements(pattern);
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

    private static void DrawDisplayTextSetting(string label, ref bool show, InternationalString text)
    {
        ImGui.PushID(label);
        ImGui.Checkbox("##show", ref show);
        ImGui.SameLine();
        DrawInternationalString(label, text);
        ImGui.PopID();
    }

    private void DrawDebugSettings()
    {
        if (!ImGui.CollapsingHeader("Debug")) return;

        ImGui.Indent();
        ImGui.TextUnformatted($"Active: {_active}");
        ImGui.TextUnformatted($"Current wave: {_currentWave}");
        ImGui.TextUnformatted($"Observed tower wave: {_observedTowerWave}");
        ImGui.TextUnformatted($"Current stage: {(_hasStage ? _currentStage.ToString() : "none")}");
        ImGui.TextUnformatted($"Live context refresh: {_allowLiveContextRefresh}");
        ImGui.TextUnformatted($"Reference tower: {FormatMapPosition(_referenceMapPosition)}");
        ImGui.TextUnformatted($"Pair tower: {FormatMapPosition(IsTowerMapPosition(_referenceMapPosition) ? AddMapSteps(_referenceMapPosition, 2) : 0)}");
        ImGui.TextUnformatted($"Past/Future reference: {FormatMapPosition(FixedStageReference())}");
        ImGui.TextUnformatted(_hasPendingTowerDisplay
            ? $"Pending tower display: wave {_pendingTowerDisplayWave} ref {FormatMapPosition(_pendingTowerDisplayReference)}"
            : "Pending tower display: none");
        var priorityParty = GetPriorityOrderedParty();
        ImGui.TextUnformatted(TryGetExplicitPairs(priorityParty, out _, out var explicitPairFailure)
            ? "Explicit pairs: ready"
            : $"Explicit pairs: {(HasAnyPairConfiguration() ? explicitPairFailure : "not configured")}");
        ImGui.TextUnformatted($"Pair split rule: {(C.SplitHeadStackPairs ? "head-stack-player-first" : "whole-head-pair-first")}");
        ImGui.TextUnformatted($"First set: {FormatResolvingSet(_firstSetIds)}");
        ImGui.TextUnformatted($"Second set: {FormatResolvingSet(_secondSetIds)}");
        ImGui.TextUnformatted($"Initial head partners: {FormatInitialHeadPartnerDebuffs()}");
        ImGui.TextUnformatted($"Pending spawns: {FormatMapPositionList(_pendingTowerSpawnPositions)}");
        ImGui.TextUnformatted($"Pending clears: {FormatMapPositionList(_pendingTowerClearPositions)}");
        ImGui.TextUnformatted($"Last pattern: H{_lastPattern.Head}/C{_lastPattern.Circle}/F{_lastPattern.Fan}");
        ImGui.TextUnformatted($"Last side: {_lastSide}");
        ImGui.TextUnformatted($"Last debuff/rank: {_lastDebuff} #{_lastDebuffRank}");
        ImGui.TextUnformatted($"Last support rank: {_lastSupportRank}");
        ImGui.TextUnformatted($"Last selector: {(_lastSelectorLabel.Length == 0 ? "none" : _lastSelectorLabel)}");
        ImGui.TextUnformatted($"Last matched rule: {_lastRuleLabel}");
        ImGui.TextUnformatted($"Context cache: {_stageContexts.Count} players");

        var overrideName = global::Splatoon.Splatoon.BasePlayerOverride;
        var local = global::ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        ImGui.TextUnformatted($"BasePlayer override: {(string.IsNullOrEmpty(overrideName) ? "none" : overrideName)}");
        ImGui.TextUnformatted(local != null
            ? $"LocalPlayer: {DebugIdentity(local)}"
            : "LocalPlayer: null");

        var me = BasePlayer;
        if (me != null)
        {
            ImGui.TextUnformatted($"BasePlayer: {DebugIdentity(me)}");
            ImGui.TextUnformatted($"BasePlayer source: {(local != null && me.AddressEquals(local) ? "LocalPlayer/fallback" : "override")}");
            ImGui.TextUnformatted($"BasePlayer current debuff: {CurrentDebuff(me)}");
            ImGui.TextUnformatted(_stageContexts.TryGetValue(me.EntityId, out var cachedContext)
                ? $"BasePlayer cached context: {cachedContext.Side} {cachedContext.Debuff} #{cachedContext.DebuffRank} support#{cachedContext.SupportRank}"
                : "BasePlayer cached context: none");
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
            foreach (var player in priorityParty)
                ImGui.TextUnformatted($"{player.Name}: 0x{player.EntityId:X8} {CurrentDebuff(player)}");
            ImGui.TreePop();
        }

        ImGui.Unindent();
    }

    private void AddTowerSpawnPosition(uint position)
    {
        AddUniquePairPosition(_pendingTowerSpawnPositions, position);
        if (_pendingTowerSpawnPositions.Count != 2) return;

        if (!TryGetPairReference(_pendingTowerSpawnPositions[0], _pendingTowerSpawnPositions[1],
                out var reference))
        {
            DebugLog($"TOWER_SPAWN pair rejected positions={FormatMapPositionList(_pendingTowerSpawnPositions)}");
            _pendingTowerSpawnPositions.Clear();
            return;
        }

        _pendingTowerSpawnPositions.Clear();
        _pendingTowerClearPositions.Clear();
        if (_observedTowerWave >= WaveCount)
            return;

        _observedTowerWave++;
        DebugLog($"TOWER_SPAWN wave={_observedTowerWave} reference={FormatMapPosition(reference)} pair={FormatMapPosition(AddMapSteps(reference, 2))}");
        if (_observedTowerWave <= 2)
        {
            ActivateTowerDisplay(_observedTowerWave, reference, StageKind.Tower);
            return;
        }

        _hasPendingTowerDisplay = true;
        _pendingTowerDisplayWave = _observedTowerWave;
        _pendingTowerDisplayReference = reference;
        DebugLog($"TOWER_PENDING wave={_pendingTowerDisplayWave} reference={FormatMapPosition(_pendingTowerDisplayReference)}");
    }

    private void AddTowerClearPosition(uint position)
    {
        AddUniquePairPosition(_pendingTowerClearPositions, position);
        if (_pendingTowerClearPositions.Count != 2) return;

        _pendingTowerClearPositions.Clear();
        _allowLiveContextRefresh = false;
        var activatedPending = TryActivatePendingTowerDisplayFromClear();
        DebugLog($"TOWER_CLEAR currentWave={_currentWave} stage={_currentStage} liveRefresh={_allowLiveContextRefresh} activatedPending={activatedPending}");
        if (!activatedPending)
            ApplyDisplay();
    }

    private void SetStage(StageKind stage)
    {
        _active = true;
        _currentStage = stage;
        _hasStage = true;
        _stageContexts.Clear();
        _allowLiveContextRefresh = StageUsesLiveContext(stage);
        _waitingForLiveDebuffRefresh = false;
        _waitingForLiveDebuffRefreshWave = 0;

        DebugLog($"SET_STAGE wave={_currentWave} stage={stage} reference={FormatMapPosition(_referenceMapPosition)} liveRefresh={_allowLiveContextRefresh}");
        UpdateStageInstruction();
        ApplyDisplay();
    }

    private bool TryActivatePendingTowerDisplay(StageKind stage, Func<int, bool> waveFilter)
    {
        if (!_hasPendingTowerDisplay || !waveFilter(_pendingTowerDisplayWave))
        {
            DebugLog($"PENDING_TOWER not activated stage={stage} hasPending={_hasPendingTowerDisplay} pendingWave={_pendingTowerDisplayWave} currentWave={_currentWave}");
            return false;
        }

        DebugLog($"PENDING_TOWER activate stage={stage} wave={_pendingTowerDisplayWave} reference={FormatMapPosition(_pendingTowerDisplayReference)}");
        ActivateTowerDisplay(_pendingTowerDisplayWave, _pendingTowerDisplayReference, stage);
        _hasPendingTowerDisplay = false;
        _pendingTowerDisplayWave = 0;
        _pendingTowerDisplayReference = 0;
        return true;
    }

    private bool TryActivatePendingTowerDisplayFromClear()
    {
        if (!_hasPendingTowerDisplay || _pendingTowerDisplayWave is < 3 or > WaveCount)
            return false;

        var stage = _pendingTowerDisplayWave % 2 == 0
            ? StageKind.Tower
            : StageKind.AllThingsEnding;
        DebugLog($"PENDING_TOWER clear fallback stage={stage} wave={_pendingTowerDisplayWave} reference={FormatMapPosition(_pendingTowerDisplayReference)}");
        return TryActivatePendingTowerDisplay(stage, _ => true);
    }

    private void ActivateTowerDisplay(int wave, uint reference, StageKind stage)
    {
        _active = true;
        _currentWave = wave;
        _referenceMapPosition = reference;
        _hasTowerReference = true;
        _currentStage = stage;
        _hasStage = true;
        _stageContexts.Clear();
        _allowLiveContextRefresh = StageUsesLiveContext(stage);
        _waitingForLiveDebuffRefresh = ShouldWaitForLiveDebuffRefresh(wave, stage);
        _waitingForLiveDebuffRefreshWave = _waitingForLiveDebuffRefresh ? wave : 0;

        DebugLog($"ACTIVATE_TOWER wave={wave} stage={stage} reference={FormatMapPosition(reference)} group={C.Waves[Math.Clamp(wave, 1, WaveCount) - 1].ResolvingGroup} liveRefresh={_allowLiveContextRefresh} waitDebuffRefresh={_waitingForLiveDebuffRefresh}");
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

        var debuff = CurrentDebuff(me);
        ClearDestination();
        _currentInstruction = debuff == LiveDebuffKind.None
            ? DisplayText(C.ShowWaitingForAssignmentText, C.WaitingForAssignmentText)
            : FormatDisplayText(C.ShowWaitingForWaveText, C.WaitingForWaveText, DisplayDebuffLabel(debuff));
    }

    private void UpdateStageInstruction()
    {
        var me = BasePlayer;
        if (me == null) return;

        if (!_hasTowerReference || !_hasStage || _currentWave is < 1 or > WaveCount)
        {
            _currentInstruction = FormatDisplayText(
                C.ShowWaitingForWaveText,
                C.WaitingForWaveText,
                DisplayDebuffLabel(CurrentDebuff(me)));
            ClearDestination();
            return;
        }

        if (ShouldHoldForLiveDebuffRefresh())
        {
            _currentInstruction = FormatDisplayText(
                C.ShowWaitingForWaveText,
                C.WaitingForWaveText,
                DisplayDebuffLabel(CurrentDebuff(me)));
            ClearDestination();
            return;
        }

        if (_currentStage == StageKind.Past)
        {
            ApplyFixedStageInstruction(C.PastFixedText, C.ShowPastFixedText, C.PastFixedPosition);
            return;
        }

        if (_currentStage == StageKind.Future)
        {
            ApplyFixedStageInstruction(C.FutureFixedText, C.ShowFutureFixedText, C.FutureFixedPosition);
            return;
        }

        if (StageUsesLiveContext(_currentStage)
            && (_allowLiveContextRefresh || !_stageContexts.ContainsKey(me.EntityId)))
        {
            if (TryRefreshLiveContexts(me, out var failureReason))
            {
                if (_stageContexts.TryGetValue(me.EntityId, out var freshContext))
                    LogContextResolved("fresh", freshContext);
            }
            else if (!_stageContexts.ContainsKey(me.EntityId))
            {
                LogContextFailure(failureReason);
                _currentInstruction = DisplayText(C.ShowWaitingForAssignmentText, C.WaitingForAssignmentText);
                ClearDestination();
                return;
            }
        }

        if (!_stageContexts.TryGetValue(me.EntityId, out var liveContext))
        {
            LogContextFailure($"context empty player=0x{me.EntityId:X8} wave={_currentWave} stage={_currentStage} cache={_stageContexts.Count}");
            _currentInstruction = DisplayText(C.ShowWaitingForAssignmentText, C.WaitingForAssignmentText);
            ClearDestination();
            return;
        }

        _lastPattern = liveContext.Pattern;
        _lastSide = liveContext.Side;
        _lastDebuff = liveContext.Debuff;
        _lastDebuffRank = liveContext.DebuffRank;
        _lastSupportRank = liveContext.SupportRank;

        if (TryApplyBasicInstruction(liveContext))
            return;

        _lastRuleLabel = "";
        _lastSelectorLabel = "";
        _currentInstruction = FormatDisplayText(
            C.ShowInactiveInstructionText,
            C.InactiveInstructionText,
            _currentWave,
            DisplayDebuffLabel(liveContext.Debuff),
            StageLabel(_currentStage));
        ClearDestination();
    }

    private bool TryApplyBasicInstruction(LiveContext context)
    {
        var stageKind = BasicStageFromStage(_currentStage);
        var stage = C.BasicStages.FirstOrDefault(item => item.Kind == stageKind);
        if (stage == null) return false;

        var pattern = stage.Patterns.FirstOrDefault(item => item.Pattern.Matches(context.Pattern));
        if (pattern == null) return false;

        var placement = pattern.Placements.FirstOrDefault(item => item.Matches(context));
        if (placement == null)
        {
            LogContextFailure($"no placement wave={_currentWave} stage={_currentStage} pattern={FormatPattern(context.Pattern)} side={context.Side} debuff={context.Debuff} debuffRank={context.DebuffRank} supportRank={context.SupportRank}");
            return false;
        }

        _lastRuleLabel = placement.Text.Get();
        _lastSelectorLabel = SelectorLabel(placement.Selector);
        SetDestination(ResolvePosition(placement.Position, _referenceMapPosition));
        _currentInstruction = FormatDisplayText(
            C.ShowActiveInstructionText,
            C.ActiveInstructionText,
            _currentWave,
            StageLabel(_currentStage),
            placement.Text.Get());
        DebugLogOnce(ref _lastInstructionLog,
            $"{_currentWave}|{_currentStage}|{FormatPattern(context.Pattern)}|{context.Side}|{context.Debuff}|{context.DebuffRank}|{context.SupportRank}|{_lastRuleLabel}|{FormatVector3(_myDestination)}",
            $"INSTRUCTION wave={_currentWave} stage={_currentStage} pattern={FormatPattern(context.Pattern)} side={context.Side} debuff={context.Debuff} debuffRank={context.DebuffRank} supportRank={context.SupportRank} rule=\"{_lastRuleLabel}\" selector={_lastSelectorLabel} destination={FormatVector3(_myDestination)}");
        return true;
    }

    private bool HasConfiguredPattern(PatternInfo pattern)
    {
        var stageKind = BasicStageFromStage(_currentStage);
        var stage = C.BasicStages.FirstOrDefault(item => item.Kind == stageKind);
        return stage?.Patterns.Any(item => item.Pattern.Matches(pattern)) == true;
    }

    private bool ApplyFixedStageInstruction(InternationalString text, bool showText, PositionRule position)
    {
        _lastRuleLabel = text.Get();
        _lastSelectorLabel = "";
        var reference = FixedStageReference();
        if (IsTowerMapPosition(reference))
            SetDestination(ResolvePosition(position, reference));
        else
            ClearDestination();

        _currentInstruction = FormatDisplayText(
            C.ShowActiveInstructionText,
            C.ActiveInstructionText,
            _currentWave,
            StageLabel(_currentStage),
            DisplayText(showText, text));
        return true;
    }

    private static bool StageUsesLiveContext(StageKind stage)
    {
        return stage is StageKind.Tower or StageKind.AllThingsEnding;
    }

    private static bool ShouldWaitForLiveDebuffRefresh(int wave, StageKind stage)
    {
        return wave == 2 && stage == StageKind.Tower;
    }

    private bool ShouldHoldForLiveDebuffRefresh()
    {
        return _waitingForLiveDebuffRefresh
            && _waitingForLiveDebuffRefreshWave == _currentWave
            && StageUsesLiveContext(_currentStage);
    }

    private uint FixedStageReference()
    {
        if (_hasPendingTowerDisplay && IsTowerMapPosition(_pendingTowerDisplayReference))
            return _pendingTowerDisplayReference;

        return 0;
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

    private bool TryRefreshLiveContexts(IPlayerCharacter currentPlayer, out string failureReason)
    {
        failureReason = "";
        var wave = C.Waves[_currentWave - 1];
        var party = GetPriorityOrderedParty();
        if (party.Count == 0)
        {
            failureReason = $"party empty wave={_currentWave} stage={_currentStage}";
            return false;
        }

        var resolvingGroup = GetGroupPlayers(wave.ResolvingGroup, party);
        if (resolvingGroup.Count == 0)
        {
            failureReason =
                $"resolving set empty wave={_currentWave} stage={_currentStage} configuredSet={wave.ResolvingGroup} priority=[{FormatPlayers(party)}] first=[{FormatResolvingSet(_firstSetIds)}] second=[{FormatResolvingSet(_secondSetIds)}]";
            return false;
        }

        var supportGroup = GetSupportGroup(wave.ResolvingGroup, party, resolvingGroup);
        var pattern = PatternFromPlayers(resolvingGroup);
        if (!HasConfiguredPattern(pattern))
        {
            failureReason = $"no configured pattern {FormatPattern(pattern)} wave={_currentWave} stage={_currentStage}";
            return false;
        }

        _stageContexts.Clear();
        foreach (var player in party)
        {
            var context = BuildLiveContext(player, resolvingGroup, supportGroup, pattern);
            _stageContexts[player.EntityId] = context;
        }

        if (!_stageContexts.ContainsKey(currentPlayer.EntityId))
            _stageContexts[currentPlayer.EntityId] = BuildLiveContext(currentPlayer, resolvingGroup, supportGroup, pattern);

        return _stageContexts.Count > 0;
    }

    private PatternInfo PatternFromPlayers(IEnumerable<IPlayerCharacter> players)
    {
        var info = new PatternInfo(0, 0, 0);
        foreach (var player in players)
        {
            switch (CurrentDebuff(player))
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

    private LiveContext BuildLiveContext(
        IPlayerCharacter me,
        IReadOnlyList<IPlayerCharacter> resolvingGroup,
        IReadOnlyList<IPlayerCharacter> supportGroup,
        PatternInfo pattern)
    {
        var side = resolvingGroup.Any(p => p.EntityId == me.EntityId)
            ? ParticipantSide.ResolvingGroup
            : supportGroup.Any(p => p.EntityId == me.EntityId)
                ? ParticipantSide.SupportGroup
                : ParticipantSide.Any;

        var sideGroup = side == ParticipantSide.SupportGroup ? supportGroup : resolvingGroup;
        var debuff = CurrentDebuff(me);
        var sameDebuffPlayers = sideGroup
            .Where(player => CurrentDebuff(player) == debuff)
            .ToList();
        var debuffIndex = sameDebuffPlayers.FindIndex(player => player.EntityId == me.EntityId);
        var debuffRank = debuff == LiveDebuffKind.None || debuffIndex < 0 ? 0 : debuffIndex + 1;
        debuffRank = AdjustInitialHeadRankFromExplicitPair(me, side, debuff, debuffRank);
        var supportRank = side == ParticipantSide.SupportGroup
            ? IndexOfPlayer(supportGroup, me.EntityId) + 1
            : 0;

        return new LiveContext(
            side,
            debuff,
            debuffRank,
            supportRank,
            pattern);
    }

    private static int IndexOfPlayer(IReadOnlyList<IPlayerCharacter> players, uint entityId)
    {
        for (var i = 0; i < players.Count; i++)
            if (players[i].EntityId == entityId)
                return i;

        return -1;
    }

    private int AdjustInitialHeadRankFromExplicitPair(
        IPlayerCharacter player,
        ParticipantSide side,
        LiveDebuffKind debuff,
        int currentRank)
    {
        if (_currentWave != 1 || side != ParticipantSide.ResolvingGroup || debuff != LiveDebuffKind.HeadStack)
            return currentRank;

        if (C.InitialHeadStackRankMode == InitialHeadStackRankMode.PriorityOrder)
            return currentRank;

        if (C.InitialHeadStackRankMode == InitialHeadStackRankMode.RoleSide)
            return InitialHeadRankFromPrioritySide(player, currentRank);

        if (!_initialHeadPartnerDebuffs.TryGetValue(player.EntityId, out var partnerDebuff))
            return currentRank;

        return partnerDebuff switch
        {
            LiveDebuffKind.Fan => 1,
            LiveDebuffKind.Circle => 2,
            _ => currentRank
        };
    }

    private int InitialHeadRankFromPrioritySide(IPlayerCharacter player, int currentRank)
    {
        var priorityParty = GetPriorityOrderedParty();
        var priorityIndex = IndexOfPlayer(priorityParty, player.EntityId);
        if (priorityIndex < 0)
            return currentRank;

        return priorityIndex < 4 ? 1 : 2;
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

        var autoIds = group == WaveGroupKind.GroupA ? _firstSetIds : _secondSetIds;
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

    private void TryCaptureResolvingSets()
    {
        if (_firstSetIds.Count == 4 && _secondSetIds.Count == 4) return;

        var party = GetPriorityOrderedParty();
        if (party.Count < 8)
        {
            DebugLogOnce(ref _lastCaptureBlockLog, $"party-count-{party.Count}",
                $"SET_ASSIGN waiting: party count {party.Count} priority=[{FormatPlayers(party)}]");
            return;
        }

        if (TryGetExplicitPairs(party, out var explicitPairs, out _))
        {
            TryCapturePairSets(explicitPairs, "explicit-pairs", party, true);
            return;
        }

        if (HasAnyPairConfiguration())
        {
            TryGetExplicitPairs(party, out _, out var pairFailureReason);
            DebugLogOnce(ref _lastCaptureBlockLog, $"explicit-pair-wait-{pairFailureReason}",
                $"SET_ASSIGN waiting explicit pairs: {pairFailureReason} priority=[{FormatPlayers(party)}]");
            return;
        }

        TryCapturePriorityIndexPairSets(party);
    }

    private void TryCapturePriorityIndexPairSets(IReadOnlyList<IPlayerCharacter> party)
    {
        if (party.Count < 8)
        {
            DebugLogOnce(ref _lastCaptureBlockLog, $"pair-party-count-{party.Count}",
                $"SET_ASSIGN waiting priority pairs: party count {party.Count} priority=[{FormatPlayers(party)}]");
            return;
        }

        var pairs = PriorityIndexPairSlots1238_4567
            .Select(pair => (First: party[pair.A], Second: party[pair.B]))
            .ToList();
        TryCapturePairSets(pairs, "priority-index-pairs", party, false);
    }

    private bool TryGetExplicitPairs(
        IReadOnlyList<IPlayerCharacter> party,
        out List<(IPlayerCharacter First, IPlayerCharacter Second)> pairs,
        out string failureReason)
    {
        pairs = [];
        failureReason = "";

        if (C.Pairs == null || C.Pairs.Length != PairCount)
        {
            failureReason = "pair settings are not initialized";
            return false;
        }

        var usedIds = new HashSet<uint>();
        for (var i = 0; i < PairCount; i++)
        {
            var pairPlayers = GetConfiguredGroup(C.Pairs[i], party);
            if (pairPlayers.Count != 2)
            {
                failureReason = $"pair {i + 1} has {pairPlayers.Count} matching players";
                return false;
            }

            if (!usedIds.Add(pairPlayers[0].EntityId) || !usedIds.Add(pairPlayers[1].EntityId))
            {
                failureReason = $"pair {i + 1} overlaps with another pair";
                return false;
            }

            pairs.Add((pairPlayers[0], pairPlayers[1]));
        }

        if (usedIds.Count != 8)
        {
            failureReason = $"explicit pairs resolved {usedIds.Count} unique players";
            return false;
        }

        return true;
    }

    private bool HasAnyPairConfiguration()
    {
        if (C.Pairs == null) return false;
        return C.Pairs.Any(PriorityDataHasConfiguredEntries);
    }

    private static bool PriorityDataHasConfiguredEntries(PriorityData priorityData)
    {
        return priorityData?.PriorityLists?.Any(list =>
            list?.List?.Any(player =>
                !string.IsNullOrWhiteSpace(player.Name) ||
                player.Jobs.Count > 0 ||
                player.Role != RolePosition.Not_Selected) == true) == true;
    }

    private void TryCapturePairSets(
        IReadOnlyList<(IPlayerCharacter First, IPlayerCharacter Second)> pairs,
        string debugSource,
        IReadOnlyList<IPlayerCharacter> party,
        bool trackHeadPartnerDebuffs)
    {
        if (C.SplitHeadStackPairs)
        {
            TryCaptureSplitHeadStackPairSets(pairs, debugSource, party, trackHeadPartnerDebuffs);
            return;
        }

        var firstSet = new List<IPlayerCharacter>();
        var secondSet = new List<IPlayerCharacter>();
        var headPartnerDebuffs = new Dictionary<uint, LiveDebuffKind>();
        foreach (var (first, second) in pairs)
        {
            var firstDebuff = CurrentDebuff(first);
            var secondDebuff = CurrentDebuff(second);
            var firstIsHead = firstDebuff == LiveDebuffKind.HeadStack;
            var secondIsHead = secondDebuff == LiveDebuffKind.HeadStack;

            if (firstIsHead && secondIsHead)
            {
                DebugLogOnce(ref _lastCaptureBlockLog, $"pair-both-head-{first.EntityId:X8}-{second.EntityId:X8}",
                    $"SET_ASSIGN waiting {debugSource}: pair has two heads pair={DebugPlayer(first)} / {DebugPlayer(second)} priority=[{FormatPlayers(party)}]");
                return;
            }

            if (firstIsHead || secondIsHead)
            {
                var partnerDebuff = firstIsHead ? secondDebuff : firstDebuff;
                if (partnerDebuff is not (LiveDebuffKind.Circle or LiveDebuffKind.Fan))
                {
                    DebugLogOnce(ref _lastCaptureBlockLog, $"pair-head-no-partner-{first.EntityId:X8}-{second.EntityId:X8}-{partnerDebuff}",
                        $"SET_ASSIGN waiting {debugSource}: head pair partner debuff not ready pair={DebugPlayer(first)} / {DebugPlayer(second)} priority=[{FormatPlayers(party)}]");
                    return;
                }

                firstSet.Add(first);
                firstSet.Add(second);

                if (trackHeadPartnerDebuffs)
                    headPartnerDebuffs[firstIsHead ? first.EntityId : second.EntityId] = partnerDebuff;
                continue;
            }

            if (firstDebuff is not (LiveDebuffKind.Circle or LiveDebuffKind.Fan) ||
                secondDebuff is not (LiveDebuffKind.Circle or LiveDebuffKind.Fan))
            {
                DebugLogOnce(ref _lastCaptureBlockLog, $"pair-non-head-not-ready-{first.EntityId:X8}-{second.EntityId:X8}-{firstDebuff}-{secondDebuff}",
                    $"SET_ASSIGN waiting {debugSource}: non-head pair debuffs not ready pair={DebugPlayer(first)} / {DebugPlayer(second)} priority=[{FormatPlayers(party)}]");
                return;
            }

            secondSet.Add(first);
            secondSet.Add(second);
        }

        if (firstSet.Count != 4 || secondSet.Count != 4)
        {
            DebugLogOnce(ref _lastCaptureBlockLog, $"pair-counts-{firstSet.Count}-{secondSet.Count}",
                $"SET_ASSIGN waiting {debugSource}: set counts invalid first={firstSet.Count} second={secondSet.Count} priority=[{FormatPlayers(party)}]");
            return;
        }

        DebugLog($"SET_ASSIGN {debugSource} captured first=[{FormatPlayers(firstSet)}] second=[{FormatPlayers(secondSet)}]");
        SetResolvingSets(firstSet, secondSet);
        _initialHeadPartnerDebuffs.Clear();
        foreach (var (id, debuff) in headPartnerDebuffs)
            _initialHeadPartnerDebuffs[id] = debuff;
    }

    private void TryCaptureSplitHeadStackPairSets(
        IReadOnlyList<(IPlayerCharacter First, IPlayerCharacter Second)> pairs,
        string debugSource,
        IReadOnlyList<IPlayerCharacter> party,
        bool trackHeadPartnerDebuffs)
    {
        var firstSet = new List<IPlayerCharacter>();
        var secondSet = new List<IPlayerCharacter>();
        var headPartnerDebuffs = new Dictionary<uint, LiveDebuffKind>();
        foreach (var (first, second) in pairs)
        {
            var firstDebuff = CurrentDebuff(first);
            var secondDebuff = CurrentDebuff(second);
            var firstIsHead = firstDebuff == LiveDebuffKind.HeadStack;
            var secondIsHead = secondDebuff == LiveDebuffKind.HeadStack;

            if (firstIsHead && secondIsHead)
            {
                DebugLogOnce(ref _lastCaptureBlockLog, $"split-head-pair-both-head-{first.EntityId:X8}-{second.EntityId:X8}",
                    $"SET_ASSIGN waiting {debugSource} split head-stack pairs: pair has two heads pair={DebugPlayer(first)} / {DebugPlayer(second)} priority=[{FormatPlayers(party)}]");
                return;
            }

            if (firstIsHead || secondIsHead)
            {
                var head = firstIsHead ? first : second;
                var partner = firstIsHead ? second : first;
                var partnerDebuff = firstIsHead ? secondDebuff : firstDebuff;
                if (partnerDebuff is not (LiveDebuffKind.Circle or LiveDebuffKind.Fan))
                {
                    DebugLogOnce(ref _lastCaptureBlockLog, $"split-head-pair-head-no-partner-{head.EntityId:X8}-{partner.EntityId:X8}-{partnerDebuff}",
                        $"SET_ASSIGN waiting {debugSource} split head-stack pairs: head pair partner debuff not ready pair={DebugPlayer(first)} / {DebugPlayer(second)} priority=[{FormatPlayers(party)}]");
                    return;
                }

                firstSet.Add(head);
                secondSet.Add(partner);

                if (trackHeadPartnerDebuffs)
                    headPartnerDebuffs[head.EntityId] = partnerDebuff;
                continue;
            }

            if (firstDebuff is not (LiveDebuffKind.Circle or LiveDebuffKind.Fan) ||
                secondDebuff is not (LiveDebuffKind.Circle or LiveDebuffKind.Fan))
            {
                DebugLogOnce(ref _lastCaptureBlockLog, $"split-head-pair-non-head-not-ready-{first.EntityId:X8}-{second.EntityId:X8}-{firstDebuff}-{secondDebuff}",
                    $"SET_ASSIGN waiting {debugSource} split head-stack pairs: non-head pair debuffs not ready pair={DebugPlayer(first)} / {DebugPlayer(second)} priority=[{FormatPlayers(party)}]");
                return;
            }

            var firstPriority = IndexOfPlayer(party, first.EntityId);
            var secondPriority = IndexOfPlayer(party, second.EntityId);
            var firstGoesFirst = firstPriority >= 0 && (secondPriority < 0 || firstPriority <= secondPriority);
            firstSet.Add(firstGoesFirst ? first : second);
            secondSet.Add(firstGoesFirst ? second : first);
        }

        if (firstSet.Count != 4 || secondSet.Count != 4)
        {
            DebugLogOnce(ref _lastCaptureBlockLog, $"split-head-pair-counts-{firstSet.Count}-{secondSet.Count}",
                $"SET_ASSIGN waiting {debugSource} split head-stack pairs: set counts invalid first={firstSet.Count} second={secondSet.Count} priority=[{FormatPlayers(party)}]");
            return;
        }

        DebugLog($"SET_ASSIGN {debugSource} split head-stack pairs captured first=[{FormatPlayers(firstSet)}] second=[{FormatPlayers(secondSet)}]");
        SetResolvingSets(firstSet, secondSet);
        _initialHeadPartnerDebuffs.Clear();
        foreach (var (id, debuff) in headPartnerDebuffs)
            _initialHeadPartnerDebuffs[id] = debuff;
    }

    private void SetResolvingSets(IReadOnlyList<IPlayerCharacter> firstSet, IReadOnlyList<IPlayerCharacter> secondSet)
    {
        var firstSetIds = firstSet.Select(player => player.EntityId).ToHashSet();
        var secondSetIds = secondSet.Select(player => player.EntityId).ToHashSet();
        if (firstSetIds.Count != 4 || secondSetIds.Count != 4 || firstSetIds.Overlaps(secondSetIds))
            return;

        _firstSetIds.Clear();
        _firstSetIds.AddRange(firstSet.Select(player => player.EntityId));
        _secondSetIds.Clear();
        _secondSetIds.AddRange(secondSet.Select(player => player.EntityId));
        _lastCaptureBlockLog = "";
        DebugLog($"SET_ASSIGN set first=[{FormatPlayers(firstSet)}] second=[{FormatPlayers(secondSet)}]");
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
            destination.overlayText = FormatDisplayText(
                C.ShowDestinationOverlayText,
                C.DestinationOverlayText,
                _currentWave,
                DisplayDebuffLabel(CurrentDebuff(me)),
                StageLabel(_currentStage));
        }
    }

    private void DrawPreviewElements(int wave, StageKind stageKind)
    {
        var reference = GetPreviewReference(wave);
        if (!IsTowerMapPosition(reference)) return;

        var pair = AddMapSteps(reference, 2);
        DrawPreviewElement(0, TowerPosition(reference), DisplayText(C.ShowReferenceTowerPreviewText, C.ReferenceTowerPreviewText), 0xC8FFFFFF, 0.95f);
        DrawPreviewElement(1, TowerPosition(pair), DisplayText(C.ShowPairedTowerPreviewText, C.PairedTowerPreviewText), 0xC840FF40, 0.95f);

        var index = 2;

        if (stageKind == StageKind.Past)
        {
            DrawPreviewElement(index, ResolvePosition(C.PastFixedPosition, AddMapSteps(reference, 1)), DisplayText(C.ShowPastFixedText, C.PastFixedText), 0xC8FFD040, 0.6f);
            return;
        }

        if (stageKind == StageKind.Future)
        {
            DrawPreviewElement(index, ResolvePosition(C.FutureFixedPosition, AddMapSteps(reference, 1)), DisplayText(C.ShowFutureFixedText, C.FutureFixedText), 0xC840C0FF, 0.6f);
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

    private void DrawOpenPatternPreviewElements(PatternPlacementConfig pattern)
    {
        if (!IsEnabled) return;
        if (pattern.Placements.Length == 0) return;

        var reference = GetSettingsPreviewReference();
        if (!IsTowerMapPosition(reference)) return;

        if (!_settingsPreviewDrewReference)
        {
            DrawSettingsPreviewElement(TowerPosition(reference), DisplayText(C.ShowReferenceTowerPreviewText, C.ReferenceTowerPreviewText), 0xC8FFFFFF, 0.95f);
            DrawSettingsPreviewElement(TowerPosition(AddMapSteps(reference, 2)), DisplayText(C.ShowPairedTowerPreviewText, C.PairedTowerPreviewText), 0xC840FF40, 0.95f);
            _settingsPreviewDrewReference = true;
        }

        var wave = _currentWave is >= 1 and <= WaveCount ? _currentWave : C.PreviewWave;
        foreach (var placement in pattern.Placements.Where(placement => placement.Enabled))
        {
            var position = ResolvePosition(placement.Position, reference);
            var label = $"{wave}: {placement.Text.Get()}";
            DrawSettingsPreviewElement(position, label, SelectorColor(placement.Selector), 0.6f);
        }
    }

    private uint GetSettingsPreviewReference()
    {
        if (IsTowerMapPosition(_referenceMapPosition))
            return _referenceMapPosition;

        if (_hasPendingTowerDisplay && IsTowerMapPosition(_pendingTowerDisplayReference))
            return _pendingTowerDisplayReference;

        return GetPreviewReference(C.PreviewWave);
    }

    private void DrawSettingsPreviewElement(Vector3 position, string text, uint color, float radius)
    {
        if (_settingsPreviewIndex >= PreviewElementCount) return;

        DrawPreviewElement(_settingsPreviewIndex, position, text, color, radius);
        _settingsPreviewIndex++;
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

    private LiveDebuffKind CurrentDebuff(IPlayerCharacter player)
    {
        return _observedDebuffs.TryGetValue(player.EntityId, out var debuff) && debuff != LiveDebuffKind.None
            ? debuff
            : CurrentDebuffFromPlayer(player);
    }

    private static LiveDebuffKind CurrentDebuffFromPlayer(IPlayerCharacter player)
    {
        var current = player.StatusList
            .Select(status => (Debuff: DebuffFromStatus(status.StatusId), status.RemainingTime))
            .Where(item => item.Debuff != LiveDebuffKind.None)
            .OrderByDescending(item => item.RemainingTime)
            .ToArray();
        return current.Length > 0 ? current[0].Debuff : LiveDebuffKind.None;
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

    private string DisplayDebuffLabel(LiveDebuffKind debuff)
    {
        return debuff switch
        {
            LiveDebuffKind.HeadStack => DisplayText(C.ShowHeadStackDebuffText, C.HeadStackDebuffText),
            LiveDebuffKind.Circle => DisplayText(C.ShowCircleDebuffText, C.CircleDebuffText),
            LiveDebuffKind.Fan => DisplayText(C.ShowFanDebuffText, C.FanDebuffText),
            LiveDebuffKind.None => DisplayText(C.ShowNoDebuffText, C.NoDebuffText),
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

    private static string[] BuildInitialHeadStackRankModeLabels()
    {
        return InitialHeadStackRankModeLabelTexts.Select(item => item.Get()).ToArray();
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

    private string FormatResolvingSet(IEnumerable<uint> entityIds)
    {
        var ids = entityIds.ToList();
        if (ids.Count == 0) return "none";

        var players = Controller.GetPartyMembers()
            .OfType<IPlayerCharacter>()
            .ToDictionary(player => player.EntityId, player => player.Name.ToString());
        return string.Join(", ", ids.Select(id => players.GetValueOrDefault(id, $"0x{id:X8}")));
    }

    private void DebugLog(string message)
    {
        PluginLog.Information($"[DMU P2 Forsaken beta] {message}");
    }

    private void DebugLogOnce(ref string lastKey, string key, string message)
    {
        if (lastKey == key) return;
        lastKey = key;
        DebugLog(message);
    }

    private void LogContextFailure(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            reason = $"unknown wave={_currentWave} stage={_currentStage}";

        DebugLogOnce(ref _lastContextFailureLog, reason, $"CONTEXT_FAIL {reason}");
    }

    private void LogContextResolved(string source, LiveContext context)
    {
        var key = $"{source}|{_currentWave}|{_currentStage}|{FormatPattern(context.Pattern)}|{context.Side}|{context.Debuff}|{context.DebuffRank}|{context.SupportRank}";
        DebugLogOnce(ref _lastContextResolvedLog, key,
            $"CONTEXT_OK source={source} wave={_currentWave} stage={_currentStage} reference={FormatMapPosition(_referenceMapPosition)} pattern={FormatPattern(context.Pattern)} side={context.Side} debuff={context.Debuff} debuffRank={context.DebuffRank} supportRank={context.SupportRank} first=[{FormatResolvingSet(_firstSetIds)}] second=[{FormatResolvingSet(_secondSetIds)}]");
    }

    private string FormatPlayers(IEnumerable<IPlayerCharacter> players)
    {
        var list = players.Select(DebugPlayer).ToList();
        return list.Count == 0 ? "none" : string.Join(", ", list);
    }

    private string DebugPlayer(IPlayerCharacter player)
    {
        return $"{player.Name}(0x{player.EntityId:X8},{CurrentDebuff(player)})";
    }

    private static string DebugIdentity(IPlayerCharacter player)
    {
        var world = player.HomeWorld.ValueNullable?.Name.ToString();
        return string.IsNullOrWhiteSpace(world)
            ? $"{player.Name} 0x{player.EntityId:X8}"
            : $"{player.Name}@{world} 0x{player.EntityId:X8}";
    }

    private string FormatInitialHeadPartnerDebuffs()
    {
        if (_initialHeadPartnerDebuffs.Count == 0) return "none";

        var players = Controller.GetPartyMembers()
            .OfType<IPlayerCharacter>()
            .ToDictionary(player => player.EntityId, player => player.Name.ToString());
        return string.Join(", ", _initialHeadPartnerDebuffs.Select(item =>
            $"{players.GetValueOrDefault(item.Key, $"0x{item.Key:X8}")}:{item.Value}"));
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

    private static string DisplayText(bool show, InternationalString text) => show ? text.Get() ?? "" : "";

    private static string FormatDisplayText(bool show, InternationalString text, params object[] args)
    {
        return show ? FormatText(text, args) : "";
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
        _observedTowerWave = 0;
        _referenceMapPosition = 0;
        _hasPendingTowerDisplay = false;
        _pendingTowerDisplayWave = 0;
        _pendingTowerDisplayReference = 0;
        _currentStage = StageKind.Tower;
        _stageContexts.Clear();
        _allowLiveContextRefresh = false;
        _waitingForLiveDebuffRefresh = false;
        _waitingForLiveDebuffRefreshWave = 0;
        _currentInstruction = "";
        _hasDestination = false;
        _myDestination = Vector3.Zero;
        _pendingTowerSpawnPositions.Clear();
        _pendingTowerClearPositions.Clear();
        _firstSetIds.Clear();
        _secondSetIds.Clear();
        _initialHeadPartnerDebuffs.Clear();
        _observedDebuffs.Clear();
        _lastPattern = new PatternInfo();
        _lastRuleLabel = "";
        _lastDebuff = LiveDebuffKind.Any;
        _lastDebuffRank = 0;
        _lastSupportRank = 0;
        _lastSide = ParticipantSide.Any;
        _lastSelectorLabel = "";
        _lastCaptureBlockLog = "";
        _lastContextFailureLog = "";
        _lastContextResolvedLog = "";
        _lastInstructionLog = "";
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
        "First set",
        "Second set",
        "All",
    ];

    private enum PairValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    private readonly record struct PairValidationLine(PairValidationSeverity Severity, string En, string Jp)
    {
        public static PairValidationLine Info(string en, string jp) => new(PairValidationSeverity.Info, en, jp);
        public static PairValidationLine Warning(string en, string jp) => new(PairValidationSeverity.Warning, en, jp);
        public static PairValidationLine Error(string en, string jp) => new(PairValidationSeverity.Error, en, jp);

        public string Get()
        {
            return new InternationalString { En = En, Jp = Jp }.Get() ?? En;
        }
    }

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

    public enum InitialHeadStackRankMode
    {
        PartnerDebuff,
        PriorityOrder,
        RoleSide
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
            Role = RoleFromSelector(side, debuff, rank);
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

        private static RoleKind RoleFromSelector(ParticipantSide side, LiveDebuffKind debuff, int rank)
        {
            if (side == ParticipantSide.SupportGroup)
            {
                return rank switch
                {
                    1 => RoleKind.Support1,
                    2 => RoleKind.Support2,
                    3 => RoleKind.Support3,
                    4 => RoleKind.Support4,
                    _ => RoleKind.Support1
                };
            }

            return debuff switch
            {
                LiveDebuffKind.HeadStack => rank switch
                {
                    1 => RoleKind.Head1,
                    2 => RoleKind.Head2,
                    3 => RoleKind.Head3,
                    4 => RoleKind.Head4,
                    _ => RoleKind.Head1
                },
                LiveDebuffKind.Circle => rank switch
                {
                    1 => RoleKind.Circle1,
                    2 => RoleKind.Circle2,
                    3 => RoleKind.Circle3,
                    4 => RoleKind.Circle4,
                    _ => RoleKind.Circle1
                },
                LiveDebuffKind.Fan => rank switch
                {
                    1 => RoleKind.Fan1,
                    2 => RoleKind.Fan2,
                    3 => RoleKind.Fan3,
                    4 => RoleKind.Fan4,
                    _ => RoleKind.Fan1
                },
                _ => RoleKind.Support1
            };
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

    public sealed class PriorityData2 : PriorityData
    {
        public override int GetNumPlayers() => 2;
    }

    void ApplyScaleData()
    {
        foreach(var x in Controller.GetRegisteredElements())
        {
            if(x.Value.Enabled && Controller.OriginalElements.TryGetValue(x.Key, out var value))
            {
                x.Value.overlayFScale = value.overlayFScale * C.ScaleMod;
                x.Value.overlayVOffset = value.overlayVOffset + C.VOffsetMod;
            }
        }
    }

    public sealed class Config 
    {
        public float VOffsetMod = 0f;
        public float ScaleMod = 1f;

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

        public bool ShowCollectingAssignmentsText = true;
        public bool ShowWaitingForAssignmentText = true;
        public bool ShowWaitingForWaveText = true;
        public bool ShowActiveInstructionText = true;
        public bool ShowInactiveInstructionText = true;
        public bool ShowDestinationOverlayText = true;
        public bool ShowHeadStackDebuffText = true;
        public bool ShowCircleDebuffText = true;
        public bool ShowFanDebuffText = true;
        public bool ShowNoDebuffText = true;
        public bool ShowReferenceTowerPreviewText = true;
        public bool ShowPairedTowerPreviewText = true;
        public bool ShowPastFixedText = true;
        public bool ShowFutureFixedText = true;

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

        public PositionRule PastFixedPosition = new(PositionBasis.ArenaCenter, 45f, 4f);

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

        public PositionRule FutureFixedPosition = new(PositionBasis.ArenaCenter, 225f, 4f);

        public int PreviewWave = 1;
        public StageKind PreviewStage = StageKind.Tower;

        public bool SplitHeadStackPairs;
        public P2_Forsaken_beta.InitialHeadStackRankMode InitialHeadStackRankMode =
            P2_Forsaken_beta.InitialHeadStackRankMode.PartnerDebuff;
        public int AssignmentMode;

        public PriorityData2[] Pairs = CreateEmptyPairSettings();

        public PriorityData PriorityData = new()
        {
            Name = "Forsaken beta global priority",
            Description = "Used for priority pairs 1+3, 2+4, 5+7, 6+8 and dynamic same-debuff rank ordering. Default: H2 H1 OT MT M1 M2 R1 R2.",
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

        public int DefaultsVersion = CurrentDefaultsVersion;

        public InternationalString WestLabelText = new()
        {
            En = "W",
            Jp = "西"
        };

        public void EnsureDefaults()
        {
            PriorityData ??= new PriorityData();
            EnsurePairSettings();
            NormalizePriorityData(PriorityData, true);
            foreach (var pair in Pairs)
                NormalizePriorityData(pair, false);
            if ((int)InitialHeadStackRankMode < 0 ||
                (int)InitialHeadStackRankMode >= InitialHeadStackRankModeLabelTexts.Length)
                InitialHeadStackRankMode = P2_Forsaken_beta.InitialHeadStackRankMode.PartnerDebuff;
            MigrateUiTerminology();
            PastFixedText ??= new InternationalString { En = "Tower gap", Jp = "塔間" };
            FutureFixedText ??= new InternationalString { En = "Opposite side", Jp = "反対側" };
            PastFixedPosition ??= new PositionRule(PositionBasis.ArenaCenter, 45f, 4f);
            FutureFixedPosition ??= new PositionRule(PositionBasis.ArenaCenter, 225f, 4f);
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

            if (DefaultsVersion < 8)
            {
                MigrateMirageCoordinateDefaults();
                DefaultsVersion = 8;
            }

            if (DefaultsVersion < 9)
            {
                MigratePriorityJobMode();
                DefaultsVersion = 9;
            }

            if (DefaultsVersion < 10)
            {
                DefaultsVersion = 10;
            }

            if (DefaultsVersion < 11)
            {
                MigrateIncompletePriorityOrder();
                DefaultsVersion = 11;
            }

            if (DefaultsVersion < 12)
            {
                EnableDisplayTextDefaults();
                DefaultsVersion = 12;
            }

            if (DefaultsVersion < 13)
            {
                if (AssignmentMode == 2)
                    SplitHeadStackPairs = true;
                DefaultsVersion = 13;
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

        private void MigrateUiTerminology()
        {
            var oldPriorityDescription =
                "Used for " + "R" + "inon-compatible fallback pairs 1+3, 2+4, 5+7, 6+8 and dynamic same-debuff rank ordering. Default: H2 H1 OT MT M1 M2 R1 R2.";
            const string newPriorityDescription =
                "Used for priority pairs 1+3, 2+4, 5+7, 6+8 and dynamic same-debuff rank ordering. Default: H2 H1 OT MT M1 M2 R1 R2.";

            if (PriorityData.Description == oldPriorityDescription)
                PriorityData.Description = newPriorityDescription;
        }

        private void EnsurePairSettings()
        {
            if (Pairs == null || Pairs.Length != PairCount)
            {
                var old = Pairs ?? [];
                Pairs = CreateEmptyPairSettings();
                for (var i = 0; i < Math.Min(old.Length, Pairs.Length); i++)
                    if (old[i] != null)
                        Pairs[i] = old[i];
            }

            for (var i = 0; i < Pairs.Length; i++)
            {
                Pairs[i] ??= CreateEmptyPairSettings()[i];
                Pairs[i].Name = $"Forsaken beta Pair {i + 1}";
                Pairs[i].Description =
                    "Explicit Forsaken pair. Pairs containing one head-stack become the first set; pairs with two fan/circle players become the second set.";
            }
        }

        private void EnableDisplayTextDefaults()
        {
            ShowCollectingAssignmentsText = true;
            ShowWaitingForAssignmentText = true;
            ShowWaitingForWaveText = true;
            ShowActiveInstructionText = true;
            ShowInactiveInstructionText = true;
            ShowDestinationOverlayText = true;
            ShowHeadStackDebuffText = true;
            ShowCircleDebuffText = true;
            ShowFanDebuffText = true;
            ShowNoDebuffText = true;
            ShowReferenceTowerPreviewText = true;
            ShowPairedTowerPreviewText = true;
            ShowPastFixedText = true;
            ShowFutureFixedText = true;
        }

        private static void NormalizePriorityData(PriorityData priorityData, bool createDefaultList)
        {
            if (priorityData.PriorityLists == null || priorityData.PriorityLists.Count == 0)
            {
                if (!createDefaultList) return;

                priorityData.PriorityLists =
                [
                    new PriorityList
                    {
                        IsRole = true,
                        List = CreateDefaultPriorityList()
                    }
                ];
                return;
            }

            foreach (var list in priorityData.PriorityLists)
            {
                if (list == null) continue;
                list.List ??= [];
                var hasNameOrJob = list.List.Any(player =>
                    !string.IsNullOrWhiteSpace(player.Name) || player.Jobs.Count > 0);
                var looksLikePureRoleList = !hasNameOrJob &&
                                            list.List.Any(player => player.Role != RolePosition.Not_Selected);
                if (looksLikePureRoleList)
                    list.IsRole = true;
            }
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

        private void MigrateIncompletePriorityOrder()
        {
            var list = PriorityData.PriorityLists?.FirstOrDefault(item => item.IsRole);
            if (list?.List == null) return;

            var currentRoles = list.List.Select(item => item.Role).ToArray();
            if (currentRoles.Length != LegacyIncompleteRolePriority.Length)
                return;

            for (var i = 0; i < LegacyIncompleteRolePriority.Length; i++)
                if (currentRoles[i] != LegacyIncompleteRolePriority[i])
                    return;

            list.List = CreateDefaultPriorityList();
        }

        private void MigrateMirageCoordinateDefaults()
        {
            UpdatePositionIfOldDefault(PastFixedPosition, PositionBasis.TowerPairCenter, 0f, 0f, PositionBasis.ArenaCenter, 45f, 4f);
            UpdatePositionIfOldDefault(FutureFixedPosition, PositionBasis.OppositeTowerPairCenter, 0f, 0f, PositionBasis.ArenaCenter, 225f, 4f);

            var stage = BasicStages?.FirstOrDefault(item => item.Kind == BasicStageKind.Tower);
            if (stage == null) return;

            var h2c1f1 = stage.Patterns.FirstOrDefault(item => item.Pattern.Matches(new PatternInfo(2, 1, 1)));
            if (h2c1f1 != null)
            {
                UpdateIfOldDefault(h2c1f1, RoleKind.Head1, PositionBasis.LeftTower, 0f, 2.2f, PositionBasis.LeftTower, 60f, 3.25f);
                UpdateIfOldDefault(h2c1f1, RoleKind.Head2, PositionBasis.RightTower, 10f, 3.2f, PositionBasis.RightTower, 270f, 3.25f);
                UpdateIfOldDefault(h2c1f1, RoleKind.Fan1, PositionBasis.LeftTower, 180f, 1.9f, PositionBasis.LeftTower, 330f, 3.25f);
                UpdateIfOldDefault(h2c1f1, RoleKind.Circle1, PositionBasis.RightTower, 187f, 3.4f, PositionBasis.RightTower, 90f, 3.25f);
                UpdateSupportIfOldDefault(h2c1f1, RoleKind.Support1, PositionBasis.LeftTower, 180f, 4.60f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Any, 1, "Left stack support", "左塔頭補助", PositionBasis.LeftTower, 60f, 4.75f);
                UpdateSupportIfOldDefault(h2c1f1, RoleKind.Support2, PositionBasis.LeftTower, 358f, 4.59f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Any, 2, "Bait fan", "扇誘導", PositionBasis.LeftTower, 330f, 4.75f);
                UpdateSupportIfOldDefault(h2c1f1, RoleKind.Support3, PositionBasis.RightTower, 9f, 4.71f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Any, 3, "Right stack support", "右塔頭補助", PositionBasis.RightTower, 270f, 4.75f);
                UpdateSupportIfOldDefault(h2c1f1, RoleKind.Support4, PositionBasis.RightTower, 9f, 4.71f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Any, 4, "Right stack support", "右塔頭補助", PositionBasis.RightTower, 270f, 4.75f);
            }

            var h0c2f2 = stage.Patterns.FirstOrDefault(item => item.Pattern.Matches(new PatternInfo(0, 2, 2)));
            if (h0c2f2 != null)
            {
                UpdateIfOldDefault(h0c2f2, RoleKind.Fan1, PositionBasis.LeftTower, 182f, 3.54f, PositionBasis.LeftTower, 30f, 3.25f);
                UpdateIfOldDefault(h0c2f2, RoleKind.Fan2, PositionBasis.LeftTower, 205f, 3.46f, PositionBasis.LeftTower, 0f, 3.25f);
                UpdateIfOldDefault(h0c2f2, RoleKind.Circle1, PositionBasis.RightTower, 278f, 3.55f, PositionBasis.RightTower, 90f, 3.25f);
                UpdateIfOldDefault(h0c2f2, RoleKind.Circle2, PositionBasis.RightTower, 104f, 3.5f, PositionBasis.RightTower, 270f, 3.25f);
                UpdateSupportIfOldDefault(h0c2f2, RoleKind.Support1, PositionBasis.ArenaCenter, 271f, 6.14f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Any, 1, "Demise NW", "消滅NW", PositionBasis.ArenaCenter, 315f, 3.5f);
                UpdateSupportIfOldDefault(h0c2f2, RoleKind.Support2, PositionBasis.ArenaCenter, 350f, 3.55f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Any, 2, "Demise SW", "消滅SW", PositionBasis.ArenaCenter, 225f, 3.5f);
                UpdateSupportIfOldDefault(h0c2f2, RoleKind.Support3, PositionBasis.ArenaCenter, 201f, 4.56f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Any, 3, "Demise NE", "消滅NE", PositionBasis.ArenaCenter, 45f, 3.55f);
                UpdateSupportIfOldDefault(h0c2f2, RoleKind.Support4, PositionBasis.ArenaCenter, 96f, 3.58f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Any, 4, "Demise SE", "消滅SE", PositionBasis.ArenaCenter, 135f, 3.5f);
            }

            var h4c0f0 = stage.Patterns.FirstOrDefault(item => item.Pattern.Matches(new PatternInfo(4, 0, 0)));
            if (h4c0f0 != null)
            {
                UpdateIfOldDefault(h4c0f0, RoleKind.Head1, PositionBasis.LeftTower, 90f, 3.25f, PositionBasis.RightTower, 90f, 3.25f);
                UpdateIfOldDefault(h4c0f0, RoleKind.Head2, PositionBasis.LeftTower, 270f, 3.25f, PositionBasis.RightTower, 270f, 3.25f);
                UpdateIfOldDefault(h4c0f0, RoleKind.Head3, PositionBasis.RightTower, 90f, 3.25f, PositionBasis.LeftTower, 90f, 3.25f);
                UpdateIfOldDefault(h4c0f0, RoleKind.Head4, PositionBasis.RightTower, 270f, 3.25f, PositionBasis.LeftTower, 270f, 3.25f);
                UpdateSupportIfOldDefault(h4c0f0, RoleKind.Support1, PositionBasis.ArenaCenter, 0f, 0f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Any, 1, "Demise NW", "消滅NW", PositionBasis.ArenaCenter, 315f, 3.5f);
                UpdateSupportIfOldDefault(h4c0f0, RoleKind.Support2, PositionBasis.ArenaCenter, 0f, 5f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Any, 2, "Demise SW", "消滅SW", PositionBasis.ArenaCenter, 225f, 3.5f);
                UpdateSupportIfOldDefault(h4c0f0, RoleKind.Support3, PositionBasis.ArenaCenter, 45f, 5f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Any, 3, "Demise NE", "消滅NE", PositionBasis.ArenaCenter, 45f, 3.55f);
                UpdateSupportIfOldDefault(h4c0f0, RoleKind.Support4, PositionBasis.ArenaCenter, 315f, 5f,
                    ParticipantSide.SupportGroup, LiveDebuffKind.Any, 4, "Demise SE", "消滅SE", PositionBasis.ArenaCenter, 135f, 3.5f);
            }
        }

        private void MigratePriorityJobMode()
        {
            var list = PriorityData.PriorityLists?.FirstOrDefault();
            if (list?.List == null) return;
            if (!list.IsRole) return;
            if (!list.List.Any(player => player.Jobs.Count > 0)) return;
            if (list.List.Any(player => !string.IsNullOrWhiteSpace(player.Name))) return;

            list.IsRole = false;
        }

        private static void UpdatePositionIfOldDefault(
            PositionRule position,
            PositionBasis oldBasis,
            float oldAngle,
            float oldRange,
            PositionBasis newBasis,
            float newAngle,
            float newRange)
        {
            if (position == null) return;
            if (position.Basis != oldBasis) return;
            if (!NearlyEqual(position.AngleDeg, oldAngle)) return;
            if (!NearlyEqual(position.Range, oldRange)) return;

            position.Basis = newBasis;
            position.AngleDeg = newAngle;
            position.Range = newRange;
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

    private static PriorityData2[] CreateEmptyPairSettings()
    {
        return Enumerable.Range(0, PairCount)
            .Select(index => new PriorityData2
            {
                Name = $"Forsaken beta Pair {index + 1}",
                Description =
                    "Explicit Forsaken pair. Pairs containing one head-stack become the first set; pairs with two fan/circle players become the second set."
            })
            .ToArray();
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
                Role(RoleKind.Head1, "Debuff stack 1", "デバフ頭1", PositionBasis.LeftTower, 60f, 3.25f),
                Role(RoleKind.Head2, "Debuff stack 2", "デバフ頭2", PositionBasis.RightTower, 270f, 3.25f),
                Role(RoleKind.Fan1, "Debuff fan", "デバフ扇", PositionBasis.LeftTower, 330f, 3.25f),
                Role(RoleKind.Circle1, "Debuff circle", "デバフ円", PositionBasis.RightTower, 90f, 3.25f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Any, 1, "Left stack support", "左塔頭補助", PositionBasis.LeftTower, 60f, 4.75f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Any, 2, "Bait fan", "扇誘導", PositionBasis.LeftTower, 330f, 4.75f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Any, 3, "Right stack support", "右塔頭補助", PositionBasis.RightTower, 270f, 4.75f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Any, 4, "Right stack support", "右塔頭補助", PositionBasis.RightTower, 270f, 4.75f)),
            new PatternPlacementConfig(
                new PatternInfo(0, 2, 2),
                Role(RoleKind.Fan1, "Debuff fan 1", "デバフ扇1", PositionBasis.LeftTower, 30f, 3.25f),
                Role(RoleKind.Fan2, "Debuff fan 2", "デバフ扇2", PositionBasis.LeftTower, 0f, 3.25f),
                Role(RoleKind.Circle1, "Debuff circle 1", "デバフ円1", PositionBasis.RightTower, 90f, 3.25f),
                Role(RoleKind.Circle2, "Debuff circle 2", "デバフ円2", PositionBasis.RightTower, 270f, 3.25f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Any, 1, "Demise NW", "消滅NW", PositionBasis.ArenaCenter, 315f, 3.5f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Any, 2, "Demise SW", "消滅SW", PositionBasis.ArenaCenter, 225f, 3.5f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Any, 3, "Demise NE", "消滅NE", PositionBasis.ArenaCenter, 45f, 3.55f),
                Role(ParticipantSide.SupportGroup, LiveDebuffKind.Any, 4, "Demise SE", "消滅SE", PositionBasis.ArenaCenter, 135f, 3.5f)),
            new PatternPlacementConfig(
                new PatternInfo(4, 0, 0),
                Role(RoleKind.Head1, "Head pattern 4-1", "頭4処理1", PositionBasis.RightTower, 90f, 3.25f),
                Role(RoleKind.Head2, "Head pattern 4-2", "頭4処理2", PositionBasis.RightTower, 270f, 3.25f),
                Role(RoleKind.Head3, "Head pattern 4-3", "頭4処理3", PositionBasis.LeftTower, 90f, 3.25f),
                Role(RoleKind.Head4, "Head pattern 4-4", "頭4処理4", PositionBasis.LeftTower, 270f, 3.25f),
                Role(RoleKind.Support1, "Demise NW", "消滅NW", PositionBasis.ArenaCenter, 315f, 3.5f),
                Role(RoleKind.Support2, "Demise SW", "消滅SW", PositionBasis.ArenaCenter, 225f, 3.5f),
                Role(RoleKind.Support3, "Demise NE", "消滅NE", PositionBasis.ArenaCenter, 45f, 3.55f),
                Role(RoleKind.Support4, "Demise SE", "消滅SE", PositionBasis.ArenaCenter, 135f, 3.5f)));
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
