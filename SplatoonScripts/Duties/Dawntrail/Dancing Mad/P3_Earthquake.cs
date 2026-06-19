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
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate;

public unsafe class P3_Earthquake : SplatoonScript<P3_Earthquake.Config>
{
    private const uint TerritoryDancingMadUltimate = 1363;
    private const uint KefkaDataId = 19451;
    private const uint FinalDondokoDataId = 19504;
    private const uint DecisiveBattleChaos = 49890;
    private const uint DecisiveBattleExdeath = 49891;
    private const uint BlackHoleDataId = 19512;
    private const uint BlackHoleCast = 47867;
    private const uint BlackHoleHit = 47868;
    private const uint BintaStackCast = 47846;
    private const uint BintaSpreadCast = 47847;
    private const uint AsIsFirst = 47852;
    private const uint AsIsSecond = 47853;
    private const uint UltimateEmbrace = 49740;
    private const uint BowelsOfAgony = 47858;
    private const uint LateP3Blizzaga = 47887;
    private const uint DondokoCast = 47855;
    private const uint DondokoHit = 47856;
    private const uint LandingCast = 47874;
    private const uint FinalLandingSwitch = 47885;
    private const uint TowerImpact = 47857;
    private const uint Protrude = 47877;
    private const uint TargetIconCommand = 34;
    private const uint FinalStackMarker = 161;
    private const ushort FirstTarget = 3004;
    private const ushort SecondTarget = 3005;
    private const ushort ThirdTarget = 3006;
    private const ushort AccretionStatus = 1604;
    private const ushort EarthStatus = 5454;
    private const ushort LineDoneStatus = 5453;
    private const float BlackHoleRadiusMin = 11.0f;
    private const float BlackHoleRadiusMax = 23.0f;
    private const float KefkaAnchorRadiusMin = 5.0f;
    private const float KefkaVirtualAnchorRadius = 20.0f;
    private const float KefkaRotationMatchMax = MathF.PI / 4.0f;
    private const int ExpectedBlackHoleActors = 12;
    private const float BlackHoleGuideRadius = 9.021f;
    private const float LastBlackHoleGuideRadius = 19.0f;
    private const float BlackHoleAvoidRadius = 3.0f;
    private const float BlackHoleAvoidRadiusSq = BlackHoleAvoidRadius * BlackHoleAvoidRadius;
    private const float BlackHoleAvoidMinRadius = 6.0f;
    private const float BlackHoleAvoidInwardStep = 1.0f;
    private const int BlackHoleAvoidInwardSteps = 6;
    private const float BlackHoleAvoidAngleStep = MathF.PI / 24.0f;
    private const int BlackHoleAvoidAngleSteps = 4;
    private const float FinalPairRadius = 9.8f;
    private const float FinalTowerRadius = 10.0f;
    private const float FinalInitialSplitRadius = 5.5f;
    private const string DestinationElement = "Destination";
    private const string InstructionElement = "Instruction";
    private const string BlackHoleLineElement = "BlackHoleLine";
    private const float DefaultColorAlpha = 200.0f / 255.0f;

    private static readonly Vector3 Center = new(100f, 0f, 100f);
    private static readonly Slot[][] BlackHoleWindowSlots =
    [
        [Slot.Attack1],
        [Slot.Attack1, Slot.Attack2],
        [Slot.Attack1, Slot.Attack2, Slot.Attack3],
        [Slot.Bind1, Slot.Attack2, Slot.Attack3],
        [Slot.Bind1, Slot.Bind2, Slot.Attack3],
        [Slot.Bind1, Slot.Bind2, Slot.Bind3],
        [Slot.Stop1, Slot.Bind2, Slot.Bind3],
        [Slot.Stop1, Slot.Stop2, Slot.Bind3],
        [Slot.Stop1, Slot.Stop2],
        [Slot.Stop2]
    ];
    private static readonly int[] ExpectedSourcesByWindow = BlackHoleWindowSlots.Select(x => x.Length).ToArray();
    private static readonly int[] SelectableMarkerIds = [0, 1, 2, 5, 6, 7, 8, 9];
    private static readonly string[] SelectableMarkerNames = ["Attack1", "Attack2", "Attack3", "Bind1", "Bind2", "Bind3", "Stop1", "Stop2"];
    private static readonly int[] DefaultMarkerLineOrders = [0, 1, 2, 0, 1, 2, 0, 1];
    private static readonly string[] BlackHoleOrderNames = ["1st", "2nd", "3rd"];
    private static readonly string[] FinalInitialBaitModeNames = ["Center", "Kefka-relative N/S"];
    private static readonly string[] FinalNorthRoleNames = ["Support", "DPS"];
    private static readonly string[] MarkerCommandSourceNames = ["Target debuff", "Accretion debuff"];
    private static readonly AssignmentMode[] AssignmentModeValues =
    [
        AssignmentMode.PartyMarker,
        AssignmentMode.Priority,
        AssignmentMode.RoleAccretion,
        AssignmentMode.FixedRoleAccretion,
        AssignmentMode.FixedMarkerLanes
    ];
    private static readonly string[] AssignmentModeNames =
        ["Party marker", "Priority", "PF role/accretion", "Fixed role/accretion spots", "Fixed marker lanes"];
    private static readonly string[] MapMarkerNames = ["A", "B", "C", "D"];
    private static readonly string[] LineBaitDirectionNames = ["Clockwise", "Counterclockwise"];
    private static readonly string[] FirstWindowBaitDirectionNames = ["Same as line bait direction", "Clockwise", "Counterclockwise"];
    private static readonly string[] FirstPairAssignmentNames = ["Source order", "First slot nearest"];
    private static readonly string[] FirstOrbRoleNames = ["DPS", "Support"];
    private static readonly RolePosition[] DefaultRolePriority =
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
    private static readonly RolePosition[] MeowStaticTrueNorthPriority =
    [
        RolePosition.M1,
        RolePosition.M2,
        RolePosition.R1,
        RolePosition.R2,
        RolePosition.T1,
        RolePosition.T2,
        RolePosition.H1,
        RolePosition.H2
    ];

    private static readonly InternationalString Description = new()
    {
        En = "P3 Earthquake helper. It resolves your First/Second/Third line order from the debuff plus party markers or priority, then follows live Black Hole tether changes. When the line is on you, it shows the Black Hole-to-player line and your bait position. The tether uses the configured correct/wrong/unknown colors depending on whether the current active Black Hole order matches your slot.",
        Jp = "P3地震用です。デバフとマーカーまたは優先順位から自分の第一/第二/第三対象内の線取り順を決め、ブラックホールテザーの付け替わりを追ってナビします。自分に線が付いた時は、ブラックホールから自分への線と誘導先を表示します。現在線が出ているブラックホールの並びと自分のスロットが一致するかどうかで、設定した正解/不一致/不明の色を使います。"
    };
    private static readonly InternationalString AssignmentModeDescription = new()
    {
        En = "Party marker: uses only the marker line-order table below. Priority: ignores markers and orders players with the priority list inside each First/Second/Third group. PF role/accretion: resolves order inside your group from First orb role plus Accretion. Fixed role/accretion spots: support=A, DPS=B, Accretion=C; if that preferred spot has no active Black Hole, it uses D. Fixed marker lanes: resolves your lane from role/accretion, then searches from configured markers and directions.",
        Jp = "Party marker: 下のマーカー別線取り順だけで判定します。Priority: マーカーを無視し、第一/第二/第三対象ごとに優先順位で並べます。PF role/accretion: First orb role と Accretion から自分のグループ内の順番を判定します。Fixed role/accretion spots: タンク/ヒラ=A、DPS=B、Accretion=C として扱い、担当spotにブラックホールが無い場合はDを使います。Fixed marker lanes: ロール/Accretionから担当レーンを決め、設定したマーカーと方向から取る線を探します。"
    };
    private static readonly InternationalString LineBaitDirectionDescription = new()
    {
        En = "Line bait direction controls where your bait marker is placed from the Black Hole that is currently tethered to you. Clockwise and Counterclockwise are relative to the arena center. In Fixed marker lanes, DPS/Support directions choose the source search order; this setting still controls the final bait offset except when the first-window override is used.",
        Jp = "Line bait direction は、自分に付いたブラックホールを基準に誘導先を時計回り/反時計回りのどちらへずらすかを決めます。方向はフィールド中央基準です。Fixed marker lanes では DPS/Support direction は線の探索順にだけ使います。First window bait direction で別の方向を選んでいる場合を除き、実際に線を引っ張る位置はこの設定で決まります。"
    };
    private static readonly InternationalString FirstWindowBaitDirectionDescription = new()
    {
        En = "First window bait direction overrides only the bait position for the first Black Hole window. It does not change which Black Hole is selected.",
        Jp = "最初のブラックホール window だけ、線を引っ張る誘導先方向を上書きします。取るブラックホール自体は変えません。"
    };
    private static readonly InternationalString FirstPairAssignmentDescription = new()
    {
        En = "First pair assignment controls how the first two-line Black Hole window selects sources. Source order uses the configured Black Hole source order. First slot nearest makes the first slot take the visible Black Hole closest to that player, and the second slot takes the other visible Black Hole.",
        Jp = "First pair assignment は、最初に2本出るブラックホール window の線選択を決めます。Source order は設定した Black Hole source order を使います。First slot nearest は、1番目のスロットがそのプレイヤーに最も近いブラックホールを取り、2番目のスロットがもう片方を取ります。"
    };
    private static readonly InternationalString BlackHoleSourceOrderDescription = new()
    {
        En = "Black Hole source order sorts only the Black Holes that currently have active tethers. The anchor decides where 1st starts; the order decides clockwise or counterclockwise from that anchor.",
        Jp = "Black Hole source order は、現在線が出ているブラックホールだけを並べ替える設定です。anchor で 1番目を数え始める基準を決め、order でそこから時計回り/反時計回りのどちらに数えるかを決めます。"
    };
    private static readonly InternationalString PostBlackHoleNavigationDescription = new()
    {
        En = "Show or hide the final-sequence navigation after the Black Hole windows. Black Hole tether tracking and assignments still run even when this is disabled.",
        Jp = "ブラックホール後の最終ギミック用ナビを表示するかを切り替えます。OFFでもブラックホールの線追跡と割り当て処理は動作します。"
    };
    private static readonly InternationalString BlackHoleTetherOnlyDescription = new()
    {
        En = "When enabled, Black Hole windows show only the Black Hole tether line. Destination circles and waiting text are hidden during Black Hole.",
        Jp = "有効にすると、ブラックホール中はブラックホールのテザー線だけを表示します。誘導先の円と待機テキストは非表示になります。"
    };
    private static readonly InternationalString MarkerLineOrderDescription = new()
    {
        En = "Set which line order each party marker means. The debuff decides the group: First Target, Second Target, or Third Target. The marker decides the order inside that group. Example: Attack1 = 1st means First Target + Attack1 becomes First1, while Second Target + Attack1 becomes Second1. Third Target has only two players, so do not assign 3rd to markers used by Third Target players.",
        Jp = "各マーカーが何番目の線取りを意味するかを設定します。第一/第二/第三対象のどのグループかはデバフで決まり、グループ内の何番目かをマーカーで決めます。例: Attack1 = 1st の場合、第一対象+Attack1 は First1、第二対象+Attack1 は Second1 になります。第三対象は2人だけなので、第三対象に使うマーカーへ 3rd は割り当てないでください。"
    };
    private static readonly InternationalString MarkerCommandDescription = new()
    {
        En = "Optional self-marker commands. Target debuff source uses the First/Second/Third group, not the line order. It can also skip or cancel the target marker when you have Accretion or Faded Accretion. Accretion debuff source queues the Accretion command when you receive Accretion. The script waits a random delay between min and max seconds, then executes the queued command. Commands are not executed during replay playback.",
        Jp = "任意の自分用マーカーコマンドです。Target debuff を選ぶと線取り順ではなく第一/第二/第三対象のデバフグループで実行します。AccretionまたはFaded Accretion持ちの時だけ、target markerをスキップ/キャンセルする設定も使えます。Accretion debuff を選ぶと自分にAccretionが付いた時にAccretion commandを予約します。minからmax秒のランダムディレイ後に実行し、リプレイ再生中は実行しません。"
    };
    private static readonly InternationalString VisualSettingsDescription = new()
    {
        En = "Navigation color 1/2 are the gradient colors used by navigation markers and their tether. Set both colors to the same value for a solid color. Tether colors are used for the Black Hole tether line.",
        Jp = "Navigation color 1/2 はナビ表示とナビから出るテザーに使うグラデーションの色です。同じ色を2つ設定すると単色表示になります。Tether color はブラックホールのテザー線に使います。"
    };
    private static readonly InternationalString DisplayTextDescription = new()
    {
        En = "These fields change only the text shown on Splatoon overlays. Turning a text off hides that text only; it does not disable the marker, tether line, assignment logic, marker commands, or Black Hole detection.",
        Jp = "ここはSplatoon上に表示する文言だけを変更します。チェックをOFFにすると文字だけ非表示になります。マーカー、線、割り当てロジック、マーカーコマンド、ブラックホール検出は無効になりません。"
    };
    private static readonly InternationalString FinalRolePositionDescription = new()
    {
        En = "The late fixed spread/tower guide uses these role positions even when Earthquake line assignment mode is Party marker.",
        Jp = "終盤の固定散開/塔ナビは、地震線取りの Assignment mode が Party marker の場合でもこのロール設定を使用します。"
    };
    private static readonly InternationalString FinalInitialBaitDescription = new()
    {
        En = "Center keeps the existing all-center bait. Kefka-relative N/S splits the first bait by combat role using the frozen Kefka-foot direction: the selected north role goes Kefka-relative north, the other role goes south.",
        Jp = "Center は既存の全員中央誘導です。Kefka-relative N/S は、固定したケフカ足元方向を基準に、選択したロールをケフカ基準北、もう片方を南へ誘導します。"
    };

    private readonly Dictionary<uint, TargetGroup> _groups = [];
    private readonly Dictionary<int, uint> _tetherTargets = [];
    private readonly Dictionary<int, Vector3> _tetherSources = [];
    private readonly List<Vector3> _blackHolePositions = [];
    private readonly int[] _fixedLaneSetBuckets = [-1, -1, -1];
    private readonly HashSet<int> _hitSources = [];
    private readonly HashSet<uint> _earthPlayers = [];
    private readonly HashSet<uint> _accretionPlayers = [];

    private State _state;
    private uint _selfPlayerId;
    private Slot _selfSlot;
    private AssignmentQuality _quality;
    private BlackHoleTask? _selfBlackHoleTask;
    private Vector3? _selfDestination => _selfBlackHoleTask?.StandPosition;
    private Vector3? _selfTetherSource => _selfBlackHoleTask?.Source;
    private Vector3? _guideDestination;
    private GuidanceKind _guideKind;
    private uint _guideActionId;
    private string _guideText = "";
    private string _guideInstruction = "";
    private string _guideDebug = "";
    private string _kefkaAnchorDebug = "";
    private string _pendingMarkerCommand = "";
    private long _markerCommandAtMs;
    private bool _pendingTargetMarkerCommand;
    private uint _kefkaId;
    private Vector3? _kefkaPosition;
    private readonly List<Vector3> _finalTowerPositions = [];
    private uint _selfTetherTarget => _selfBlackHoleTask?.Target ?? 0;
    private int _currentWindow = -1;
    private int _fixedLaneSetStartWindow = -1;
    private int _selfTetherBucket => _selfBlackHoleTask?.Bucket ?? -1;
    private int _selfCompletedWindow = -1;
    private int _earthMaxCount;
    private FinalStage _finalStage;
    private FinalStackRole _firstFinalStackRole;
    private FinalStackRole _secondFinalStackRole;
    private FinalStackRole _currentFinalStackRole;
    private int _landingCount;
    private int _finalStackMarkerCount;
    private int _finalDondokoHitCount;
    private bool _sentMarkerCommand;
    private bool _selfHadAccretionMarkerBlock;
    private string _instruction = "";

    public override HashSet<uint>? ValidTerritories { get; } = [TerritoryDancingMadUltimate];
    public override Metadata Metadata => new(40, "Garume");

    public override void OnSetup()
    {
        C.EnsureDefaults();
        Controller.RegisterElement(DestinationElement, new Element(0)
        {
            Enabled = false,
            radius = 1.25f,
            thicc = 5.0f,
            fillIntensity = 0.25f,
            color = C.RainbowNavigationColor1,
            tether = true,
            overlayBGColor = 0xC8000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 2.4f,
            overlayFScale = 1.5f
        });
        Controller.RegisterElement(InstructionElement, new Element(0)
        {
            Enabled = false,
            radius = 0,
            thicc = 0,
            overlayBGColor = 0xC8000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 3.0f,
            overlayFScale = 1.7f
        });
        Controller.RegisterElement(BlackHoleLineElement, new Element(2)
        {
            Enabled = false,
            radius = 0,
            thicc = 5.0f,
            color = C.WrongTetherColor
        });
    }

    public override void OnCombatStart() => ResetAll();
    public override void OnCombatEnd() => ResetAll();
    public override void OnReset() => ResetAll();

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category is DirectorUpdateCategory.Commence or DirectorUpdateCategory.Recommence or DirectorUpdateCategory.Wipe)
            ResetAll();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        HandleStartingCast(source, castId, "attached-info", null);
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        HandleStartingCast(sourceId, packet->ActionID, "packet", packet);
    }

    private unsafe void HandleStartingCast(uint source, uint castId, string origin, PacketActorCast* packet)
    {
        RefreshBasePlayerState();
        var packetRotation = packet == null ? (float?)null : packet->Rotation;
        UpdateKefkaAnchor(source, castId, packetRotation);
        ObserveFinalTowerSource(source.GetObject(), null, castId, origin);

        if (castId is UltimateEmbrace or BowelsOfAgony)
            ResetAll();
        else if (castId is DecisiveBattleChaos or DecisiveBattleExdeath)
            StartCollection();
        else if (castId == BlackHoleCast)
            StartBlackHole();
        else
            HandleFinalCast(castId);
    }

    private bool HandleFinalCast(uint castId)
    {
        if (castId == LateP3Blizzaga)
        {
            SetFinalCenterBait();
            return true;
        }
        if (castId is DondokoCast or LandingCast)
            return true;
        if (castId == Protrude)
        {
            SetFinalMove();
            return true;
        }

        return false;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        RefreshBasePlayerState();

        var actionId = set.Action?.RowId ?? 0;
        ObserveFinalTowerSource(set.Source, set.Position, actionId, "action");
        if (actionId == BlackHoleHit && TryBucket(set.Source?.Position ?? set.Position, out var bucket))
        {
            AdvanceWindow(bucket);
        }
        else if (!HandleFinalAction(set, actionId) &&
            actionId is UltimateEmbrace or BowelsOfAgony)
        {
            Complete();
        }
    }

    private bool HandleFinalAction(ActionEffectSet set, uint actionId)
    {
        if (actionId == LateP3Blizzaga)
        {
            SetFinalRoleSpread(LateP3Blizzaga);
            return true;
        }
        if (actionId == FinalLandingSwitch)
        {
            SetKnownFinalLanding();
            return true;
        }
        if (actionId == DondokoHit)
        {
            _finalDondokoHitCount++;
            if (_finalDondokoHitCount == 2)
                SetSecondFinalLanding();
            return true;
        }
        if (actionId == TowerImpact)
            return true;
        if (actionId is Protrude)
        {
            Complete();
            return true;
        }

        return false;
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5,
        uint p6, uint p7, uint p8, ulong targetId, byte replaying)
    {
        RefreshBasePlayerState();

        if (command == TargetIconCommand && p1 == FinalStackMarker &&
            _state is not (State.Idle or State.Completed) &&
            TryFinalStackRole(sourceId, out var role))
            RecordFinalStackRole(role);
    }

    public override void OnGainBuffEffect(uint sourceId, Status status)
    {
        RefreshBasePlayerState();
        var isSelf = sourceId == BasePlayer?.EntityId;

        if (status.StatusId == EarthStatus)
        {
            StartCollection();
            _earthPlayers.Add(sourceId);
            _earthMaxCount = Math.Max(_earthMaxCount, _earthPlayers.Count);
            return;
        }
        if (status.StatusId == AccretionStatus)
        {
            StartCollection();
            _accretionPlayers.Add(sourceId);
            if (isSelf)
            {
                _selfHadAccretionMarkerBlock = true;
                CancelPendingTargetMarkerCommandForAccretion();
                ClearSelfResolution();
                RunAccretionMarkerCommand();
            }
            return;
        }
        if (isSelf && status.StatusId == LineDoneStatus)
        {
            _selfHadAccretionMarkerBlock = true;
            CancelPendingTargetMarkerCommandForAccretion();
            return;
        }

        if (GroupFromStatus(status.StatusId) is not { } group) return;
        StartCollection();
        _groups[sourceId] = group;

        if (isSelf)
        {
            ClearSelfResolution();
            RunMarkerCommand(group);
        }
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status status)
    {
        RefreshBasePlayerState();

        if (status.StatusId == EarthStatus)
        {
            var hadEarth = _earthPlayers.Remove(sourceId);
            if (hadEarth && _state == State.BlackHoleActive && _earthMaxCount >= 8 && _earthPlayers.Count == 0)
                EnterFinalSequence();
            return;
        }
        if (status.StatusId == AccretionStatus ||
            sourceId == BasePlayer?.EntityId && status.StatusId == LineDoneStatus)
            return;
    }

    public override void OnUpdate()
    {
        RefreshBasePlayerState();
        ExecutePendingMarkerCommand();

        HideElements();
        RefreshKefkaAnchorFromObject();
        ResolveSelfSlot();
        if (_state == State.BlackHoleActive)
            PollLiveBlackHoleTethers();
        if (BasePlayer == null || _state is State.Idle or State.Completed) return;

        ShowGuidance();
    }

    private void ShowGuidance()
    {
        if (_state == State.FinalSequence && !C.ShowPostBlackHoleNavigation)
            return;

        ShowBlackHoleLine();
        if (C.BlackHoleTetherOnly && _state == State.BlackHoleActive)
            return;

        var showedBlackHoleDestination = ShowBlackHoleDestination();
        if (!showedBlackHoleDestination && _guideDestination is { } guide)
            ShowDestination(guide, _guideText);

        if (!string.IsNullOrWhiteSpace(_guideInstruction))
            ShowInstruction(_guideInstruction);
        else if (!string.IsNullOrWhiteSpace(_instruction))
            ShowInstruction(_instruction);
    }

    public override void OnSettingsDraw()
    {
        C.EnsureDefaults();
        ImGui.TextWrapped(Description.Get());
        ImGui.Separator();

        DrawAssignmentSettings();
        DrawMarkerCommandSettings();
        DrawFinalRolePositionSettings();
        DrawVisualSettings();
        DrawDisplayTextSettings();
        DrawDebugStatus();
    }

    private void DrawAssignmentSettings()
    {
        ImGui.TextUnformatted("Assignment");
        ImGui.Indent();
        var mode = AssignmentModeIndex(C.AssignmentMode);
        if (DrawCombo("Assignment mode", ref mode, AssignmentModeNames, 260f))
            C.AssignmentMode = AssignmentModeValues[Math.Clamp(mode, 0, AssignmentModeValues.Length - 1)];
        ImGui.TextWrapped(AssignmentModeDescription.Get());

        if (C.AssignmentMode is AssignmentMode.PartyMarker)
            DrawPartyMarkerSettings();

        if (C.AssignmentMode is AssignmentMode.RoleAccretion or AssignmentMode.FixedMarkerLanes)
        {
            var firstOrbRole = (int)C.FirstOrbRole;
            if (DrawCombo("First orb role", ref firstOrbRole, FirstOrbRoleNames, 180f))
                C.FirstOrbRole = (FirstOrbRole)Math.Clamp(firstOrbRole, 0, FirstOrbRoleNames.Length - 1);
        }

        if (C.AssignmentMode is AssignmentMode.FixedRoleAccretion)
        {
            ImGui.TextUnformatted("DPS/Support/Accretion spot assignment");
            ImGui.Indent();
            DrawMapMarkerCombo("DPS", ref C.DpsMarker);
            DrawMapMarkerCombo("Support", ref C.SupportMarker);
            DrawMapMarkerCombo("Accretion", ref C.AccretionMarker);
            DrawMapMarkerCombo("Fallback", ref C.FallbackMarker);

            if (C.DpsMarker == C.SupportMarker || C.DpsMarker == C.AccretionMarker || C.DpsMarker == C.FallbackMarker ||
                C.SupportMarker == C.AccretionMarker || C.SupportMarker == C.FallbackMarker ||
                C.AccretionMarker == C.FallbackMarker)
                ImGui.TextWrapped("Warning: DPS, Support, Accretion, and Fallback markers should be unique.");

            ImGui.Unindent();
        }
        else if (C.AssignmentMode is AssignmentMode.FixedMarkerLanes)
        {
            ImGui.TextUnformatted("Fixed marker lanes");
            ImGui.Indent();
            if (ImGui.Button("Apply DPS=A / Support=D / Accretion=B / Flex=C"))
                ApplyFixedMarkerLanePreset();
            DrawMapMarkerCombo("DPS marker", ref C.LaneDpsMarker);
            DrawLineBaitDirectionCombo("DPS direction", ref C.DpsLineBaitDirection);
            DrawMapMarkerCombo("Support marker", ref C.LaneSupportMarker);
            DrawLineBaitDirectionCombo("Support direction", ref C.SupportLineBaitDirection);
            DrawMapMarkerCombo("Accretion marker", ref C.LaneAccretionMarker);
            DrawLineBaitDirectionCombo("Accretion direction", ref C.AccretionLineBaitDirection);
            DrawMapMarkerCombo("Flex marker", ref C.LaneFlexMarker);
            if (C.LaneDpsMarker == C.LaneSupportMarker || C.LaneDpsMarker == C.LaneAccretionMarker || C.LaneDpsMarker == C.LaneFlexMarker ||
                C.LaneSupportMarker == C.LaneAccretionMarker || C.LaneSupportMarker == C.LaneFlexMarker ||
                C.LaneAccretionMarker == C.LaneFlexMarker)
                ImGui.TextWrapped("Warning: DPS, Support, Accretion, and Flex markers should be unique.");
            ImGui.Unindent();
        }

        if (C.AssignmentMode is AssignmentMode.Priority)
        {
            if (ImGui.Button("Apply Meow static TN priority"))
                C.PriorityData = CreatePriorityData("P3 Earthquake Meow static TN priority",
                    "M1 - M2 - R1 - R2 - MT - OT - H1 - H2.", MeowStaticTrueNorthPriority);
            C.PriorityData.Draw();
        }

        DrawBlackHoleSettings();
        ImGui.Unindent();
    }

    private void DrawFinalRolePositionSettings()
    {
        if (!ImGui.CollapsingHeader("Final role positions")) return;

        ImGui.TextWrapped(FinalRolePositionDescription.Get());
        if (C.AssignmentMode is not AssignmentMode.Priority)
            C.PriorityData.Draw();
        DrawFinalInitialBaitSettings();
    }

    private void DrawFinalInitialBaitSettings()
    {
        ImGui.Spacing();
        var mode = (int)C.FinalInitialBaitMode;
        if (DrawCombo("Initial bait position", ref mode, FinalInitialBaitModeNames, 220f))
            C.FinalInitialBaitMode = (FinalInitialBaitMode)Math.Clamp(mode, 0, FinalInitialBaitModeNames.Length - 1);
        if (C.FinalInitialBaitMode == FinalInitialBaitMode.KefkaRelativeRoleSplit)
        {
            var northRole = (int)C.FinalInitialNorthRole;
            if (DrawCombo("Kefka-relative north role", ref northRole, FinalNorthRoleNames, 180f))
                C.FinalInitialNorthRole = (FinalInitialNorthRole)Math.Clamp(northRole, 0, FinalNorthRoleNames.Length - 1);
        }
        ImGui.TextWrapped(FinalInitialBaitDescription.Get());
    }

    private void DrawPartyMarkerSettings()
    {
        DrawSubsection("Party marker assignment");
        ImGui.Indent();
        DrawMarkerLineOrders();
        ImGui.Unindent();
    }

    private void ApplyFixedMarkerLanePreset()
    {
        C.FirstOrbRole = FirstOrbRole.Dps;
        C.LaneDpsMarker = MapMarker.A;
        C.LaneSupportMarker = MapMarker.D;
        C.LaneAccretionMarker = MapMarker.B;
        C.LaneFlexMarker = MapMarker.C;
        C.DpsLineBaitDirection = LineBaitDirection.Clockwise;
        C.SupportLineBaitDirection = LineBaitDirection.Counterclockwise;
    }

    private void DrawMapMarkerCombo(string label, ref MapMarker marker)
    {
        var value = (int)marker;
        if (DrawCombo(label, ref value, MapMarkerNames, 180f))
            marker = (MapMarker)Math.Clamp(value, 0, MapMarkerNames.Length - 1);
    }

    private void DrawLineBaitDirectionCombo(string label, ref LineBaitDirection direction)
    {
        var value = (int)direction;
        if (DrawCombo(label, ref value, LineBaitDirectionNames, 180f))
            direction = (LineBaitDirection)Math.Clamp(value, 0, LineBaitDirectionNames.Length - 1);
    }

    private void DrawBlackHoleSettings()
    {
        DrawSubsection("Black Hole");
        ImGui.Indent();
        var direction = (int)C.LineBaitDirection;
        if (DrawCombo("Line bait direction", ref direction, LineBaitDirectionNames, 180f))
            C.LineBaitDirection = (LineBaitDirection)Math.Clamp(direction, 0, 1);
        ImGui.TextWrapped(LineBaitDirectionDescription.Get());
        var firstWindowDirection = (int)C.FirstWindowBaitDirection;
        if (DrawCombo("First window bait direction", ref firstWindowDirection, FirstWindowBaitDirectionNames, 260f))
            C.FirstWindowBaitDirection = (FirstWindowBaitDirection)Math.Clamp(firstWindowDirection, 0, FirstWindowBaitDirectionNames.Length - 1);
        ImGui.TextWrapped(FirstWindowBaitDirectionDescription.Get());
        var firstPairAssignment = (int)C.FirstPairAssignment;
        if (DrawCombo("First pair assignment", ref firstPairAssignment, FirstPairAssignmentNames, 220f))
            C.FirstPairAssignment = (FirstPairAssignment)Math.Clamp(firstPairAssignment, 0, FirstPairAssignmentNames.Length - 1);
        ImGui.TextWrapped(FirstPairAssignmentDescription.Get());

        var sourceOrder = (int)C.BlackHoleSourceOrder;
        if (DrawCombo("Black Hole source order", ref sourceOrder, ["Clockwise", "Counterclockwise"], 180f))
            C.BlackHoleSourceOrder = (BlackHoleSourceOrder)Math.Clamp(sourceOrder, 0, 1);
        var anchor = (int)C.BlackHoleOrderAnchor;
        if (DrawCombo("Black Hole order anchor", ref anchor, ["Kefka position", "Arena north"], 260f))
            C.BlackHoleOrderAnchor = (BlackHoleOrderAnchor)Math.Clamp(anchor, 0, 1);
        ImGui.TextWrapped(BlackHoleSourceOrderDescription.Get());
        ImGui.Checkbox("Black Hole tether only", ref C.BlackHoleTetherOnly);
        ImGui.TextWrapped(BlackHoleTetherOnlyDescription.Get());
        ImGui.Checkbox("Show post-Black-Hole final navigation", ref C.ShowPostBlackHoleNavigation);
        ImGui.TextWrapped(PostBlackHoleNavigationDescription.Get());
        ImGui.Unindent();
    }

    private void DrawMarkerCommandSettings()
    {
        if (!ImGui.CollapsingHeader("Marker command")) return;

        ImGui.Indent();
        ImGui.TextWrapped(MarkerCommandDescription.Get());
        ImGui.Spacing();
        ImGui.Checkbox("Execute self marker command", ref C.ExecuteMarkerCommand);
        var source = (int)C.MarkerCommandSource;
        if (DrawCombo("Marker command source", ref source, MarkerCommandSourceNames, 180f))
            C.MarkerCommandSource = (MarkerCommandSource)Math.Clamp(source, 0, MarkerCommandSourceNames.Length - 1);
        DrawFloat("Marker delay min (s)", ref C.MarkerDelayMinSeconds);
        DrawFloat("Marker delay max (s)", ref C.MarkerDelayMaxSeconds);
        ImGui.Spacing();
        if (C.MarkerCommandSource == MarkerCommandSource.TargetDebuff)
        {
            ImGui.Checkbox("Skip target marker on Accretion/Faded Accretion", ref C.SkipTargetMarkerOnAccretion);
            DrawCommand("First Target command", ref C.FirstTargetCommand);
            DrawCommand("Second Target command", ref C.SecondTargetCommand);
            DrawCommand("Third Target command", ref C.ThirdTargetCommand);
        }
        else
        {
            DrawCommand("Accretion command", ref C.AccretionCommand);
        }
        ImGui.Unindent();
    }

    private void DrawVisualSettings()
    {
        if (!ImGui.CollapsingHeader("Visuals")) return;

        ImGui.Indent();
        ImGui.TextWrapped(VisualSettingsDescription.Get());
        DrawColor("Navigation color 1", ref C.RainbowNavigationColor1);
        DrawColor("Navigation color 2", ref C.RainbowNavigationColor2);
        DrawColor("Correct tether color", ref C.CorrectTetherColor);
        DrawColor("Wrong tether color", ref C.WrongTetherColor);
        DrawColor("Unknown tether color", ref C.UnknownTetherColor);
        ImGui.Unindent();
    }

    private void DrawDisplayTextSettings()
    {
        if (!ImGui.CollapsingHeader("Display text")) return;

        ImGui.Indent();
        ImGui.TextWrapped(DisplayTextDescription.Get());

        DrawSubsection("Line navigation");
        ImGui.Indent();
        DrawText("First line window", C.FirstLineWindowText, ref C.ShowFirstLineWindowText);
        DrawText("Next line", C.NextLineWindowText, ref C.ShowNextLineWindowText);
        DrawText("Take line now", C.TakeLineNowText, ref C.ShowTakeLineNowText);
        DrawText("Unknown slot", C.UnknownSlotText, ref C.ShowUnknownSlotText);
        DrawText("Overlay", C.OverlayText, ref C.ShowOverlayText);
        ImGui.Unindent();

        DrawSubsection("Final sequence");
        ImGui.Indent();
        DrawText("Final center", C.FinalCenterText, ref C.ShowFinalCenterText);
        DrawText("Final role split bait", C.FinalRoleSplitText, ref C.ShowFinalRoleSplitText);
        DrawText("Final role spread", C.FinalSpreadText, ref C.ShowFinalSpreadText);
        DrawText("Final stack", C.FinalStackText, ref C.ShowFinalStackText);
        DrawText("Final tower", C.FinalTowerText, ref C.ShowFinalTowerText);
        DrawText("Final move", C.FinalMoveText, ref C.ShowFinalMoveText);
        ImGui.Unindent();
        ImGui.Unindent();
    }

    private void DrawDebugStatus()
    {
        ImGui.Separator();
        if (!ImGui.CollapsingHeader("Debug status")) return;

        ImGui.Indent();
        ImGui.TextUnformatted($"State={_state} Window={_currentWindow} Slot={_selfSlot} Source={_quality} Guide={_guideKind}");
        ImGui.TextUnformatted($"BlackHoleOrder={C.BlackHoleSourceOrder} Anchor={OrderAnchorDebugText()}");
        ImGui.TextWrapped($"BlackHoleExpected={BlackHoleExpectedDebugText("settings")}");
        ImGui.TextWrapped($"BlackHoleActors={BlackHoleActorDebugText()}");
        ImGui.TextWrapped($"TetherHolders={TetherHolderDebugText()}");
        ImGui.TextWrapped($"KefkaCandidates={KefkaCandidateText()}");
        ImGui.TextUnformatted($"Final={_finalStage} Landing={_landingCount} Markers={_finalStackMarkerCount} FirstStack={_firstFinalStackRole} SecondStack={_secondFinalStackRole} CurrentStack={_currentFinalStackRole}");
        ImGui.TextUnformatted($"FinalDondokoHits={_finalDondokoHitCount}");
        ImGui.TextUnformatted($"FinalPairAnchor={FinalPairAnchorDebugText()} Towers={FinalTowerDebugText()}");
        if (!string.IsNullOrWhiteSpace(_guideDebug))
            ImGui.TextUnformatted(_guideDebug);
        ImGui.Unindent();
    }

    private void StartCollection()
    {
        if (_state == State.Completed)
        {
            ClearMechanicState(clearSlot: true);
            _state = State.Idle;
        }

        if (_state == State.Idle)
            _state = State.CollectingAssignments;
    }

    private void StartBlackHole()
    {
        StartCollection();
        _state = State.BlackHoleActive;
        _currentWindow = 0;
        _hitSources.Clear();
        CacheLiveBlackHoleActors();
        ClearCurrentWindowTethers();
        ClearFixedLaneSetBuckets();
        ClearSelfTether(true);
        ClearGuide();
    }

    private void RefreshBasePlayerState()
    {
        var id = BasePlayer?.EntityId ?? 0;
        if (_selfPlayerId == id) return;

        var previousId = _selfPlayerId;
        _selfPlayerId = id;
        if (previousId == 0) return;

        ClearSelfResolution();
        _pendingMarkerCommand = "";
        _markerCommandAtMs = 0;
        _sentMarkerCommand = false;
        RefreshFinalGuideForBasePlayerChange();
    }

    private void RefreshFinalGuideForBasePlayerChange()
    {
        if (_state != State.FinalSequence)
            return;

        switch (_finalStage)
        {
            case FinalStage.CenterBait:
                if (TryGetFinalInitialBaitGuide(out var destination, out var text))
                    SetGuide(destination, text, GuidanceKind.FinalCenter, LateP3Blizzaga, 0.0f, 0.0f);
                else
                    SetInstruction(TextOrEmpty(C.ShowFinalCenterText, C.FinalCenterText), GuidanceKind.FinalCenter);
                break;
            case FinalStage.RoleSpread:
                SetFinalRoleGuide(C.ShowFinalSpreadText, C.FinalSpreadText, GuidanceKind.FinalSpread, LateP3Blizzaga);
                break;
            case FinalStage.Landing1:
            case FinalStage.Landing2:
                if (_currentFinalStackRole != FinalStackRole.Unknown)
                    SetFinalLanding(_currentFinalStackRole);
                break;
            case FinalStage.ProtrudeMove:
                SetFinalRoleGuide(C.ShowFinalMoveText, C.FinalMoveText, GuidanceKind.FinalMove, Protrude);
                break;
        }
    }

    private void ResolveSelfSlot()
    {
        var me = BasePlayer;
        if (me == null || _selfSlot != Slot.None || !_groups.ContainsKey(me.EntityId)) return;
        if (TryResolveSlot(me, out _selfSlot, out _quality))
            _instruction = "";
        else
            _instruction = TextOrEmpty(C.ShowUnknownSlotText, C.UnknownSlotText);
    }

    private void ClearSelfResolution()
    {
        _selfSlot = Slot.None;
        _quality = AssignmentQuality.Unknown;
        ClearSelfDisplayState();
    }

    private void ClearSelfDisplayState()
    {
        _selfBlackHoleTask = null;
        _selfCompletedWindow = -1;
        _instruction = "";
    }

    private bool TryResolveSlot(IPlayerCharacter player, out Slot slot, out AssignmentQuality quality)
    {
        quality = AssignmentQuality.Unknown;
        var group = _groups.GetValueOrDefault(player.EntityId);
        if (group == TargetGroup.None)
        {
            slot = Slot.None;
            return false;
        }

        if (C.AssignmentMode is AssignmentMode.RoleAccretion or AssignmentMode.FixedRoleAccretion or AssignmentMode.FixedMarkerLanes)
        {
            if (HasCompleteGroups() && _accretionPlayers.Count >= 2 &&
                TryRoleAccretionSlot(player, group, out slot, C.AssignmentMode == AssignmentMode.FixedRoleAccretion))
            {
                quality = AssignmentQuality.RoleAccretion;
                return true;
            }
            slot = Slot.None;
            return false;
        }

        if (C.AssignmentMode == AssignmentMode.PartyMarker && TryMarkerSlot(player, group, out slot))
        {
            quality = AssignmentQuality.Marker;
            return true;
        }

        if (C.AssignmentMode == AssignmentMode.Priority && HasCompleteGroups() && TryPrioritySlot(player, group, out slot))
        {
            quality = AssignmentQuality.Priority;
            return true;
        }

        slot = Slot.None;
        return false;
    }

    private bool TryMarkerSlot(IPlayerCharacter player, TargetGroup group, out Slot slot)
    {
        for (var i = 0; i < SelectableMarkerIds.Length; i++)
            if (Marking.HaveMark(player, (uint)SelectableMarkerIds[i]))
            {
                slot = SlotFromRank(group, C.MarkerLineOrders[i]);
                return slot != Slot.None;
            }
        slot = Slot.None;
        return false;
    }

    private bool TryPrioritySlot(IPlayerCharacter player, TargetGroup group, out Slot slot)
    {
        var players = C.PriorityData.GetPlayers(x =>
            x.IGameObject is IPlayerCharacter pc && _groups.GetValueOrDefault(pc.EntityId) == group);
        var rank = players?.FindIndex(x => x.IGameObject.EntityId == player.EntityId) ?? -1;
        slot = rank < 0 ? Slot.None : SlotFromRank(group, rank);
        return slot != Slot.None;
    }

    private bool TryRoleAccretionSlot(IPlayerCharacter player, TargetGroup group, out Slot slot, bool fixedSpots)
    {
        var isAccretion = _accretionPlayers.Contains(player.EntityId);
        var isSupport = player.GetRole() is CombatRole.Tank or CombatRole.Healer;
        var supportFirst = C.FirstOrbRole == FirstOrbRole.Support;
        var rank = fixedSpots
            ? isAccretion ? (int)C.AccretionMarker : isSupport ? (int)C.SupportMarker : (int)C.DpsMarker
            : isAccretion ? 2 : isSupport == supportFirst ? 0 : 1;
        slot = SlotFromRank(group, rank);
        return slot != Slot.None;
    }

    private void RefreshExpectedTether()
    {
        var expected = ExpectedBucket(_selfSlot);
        if (_state == State.BlackHoleActive && _selfCompletedWindow == _currentWindow)
        {
            if (_selfTetherBucket >= 0)
                ClearSelfTether(false);
            else
                _instruction = "";
            return;
        }

        if (expected >= 0 && _hitSources.Contains(expected))
        {
            _selfCompletedWindow = _currentWindow;
            ClearSelfTether(false);
            return;
        }

        if (expected < 0 ||
            !_tetherSources.TryGetValue(expected, out var source) ||
            !_tetherTargets.TryGetValue(expected, out var target))
        {
            if (_selfTetherBucket >= 0)
                ClearSelfTether(false);
            _instruction = LineWindowInstruction();
            return;
        }

        SetSelfTether(source, expected, target);
    }

    private bool HasCompleteGroups()
    {
        return _groups.Count(x => x.Value == TargetGroup.Attack) == 3 &&
               _groups.Count(x => x.Value == TargetGroup.Bind) == 3 &&
               _groups.Count(x => x.Value == TargetGroup.Stop) == 2;
    }

    private void ClearCurrentWindowTethers()
    {
        _tetherTargets.Clear();
        _tetherSources.Clear();
        _selfCompletedWindow = -1;
    }

    private void PollLiveBlackHoleTethers()
    {
        CacheLiveBlackHoleActors();
        _tetherTargets.Clear();
        _tetherSources.Clear();

        foreach (var obj in Svc.Objects)
        {
            if (obj is not ICharacter character)
                continue;
            CacheLiveBlackHoleTethersFrom(character);
        }

        CacheFixedLaneSetBuckets();
        RefreshExpectedTether();
    }

    private void CacheLiveBlackHoleActors()
    {
        _blackHolePositions.Clear();
        foreach (var obj in Svc.Objects)
            if (obj is ICharacter character && character.DataId == BlackHoleDataId)
                _blackHolePositions.Add(FlatPosition(character.Position));
    }

    private void CacheLiveBlackHoleTethersFrom(ICharacter source)
    {
        var chr = source.Struct();
        for (var i = 0; i < chr->Vfx.Tethers.Length; i++)
        {
            var tether = chr->Vfx.Tethers[i];
            if (tether.Id == 0) continue;

            var target = Svc.Objects.FirstOrDefault(x => x.GameObjectId == tether.TargetId);
            var targetId = target?.EntityId ?? tether.TargetId.ObjectId;
            if (targetId == 0)
                continue;
            if (!TryResolveBlackHoleTether(source.EntityId, targetId, out _, out var tetherTarget,
                    out var blackHolePosition, out var bucket))
                continue;

            _tetherTargets[bucket] = tetherTarget;
            _tetherSources[bucket] = blackHolePosition;
        }
    }

    private void CacheFixedLaneSetBuckets()
    {
        if (C.AssignmentMode is not (AssignmentMode.FixedRoleAccretion or AssignmentMode.FixedMarkerLanes))
            return;

        var startWindow = FixedMarkerSetStartWindow(_currentWindow);
        if (startWindow < 0)
            return;

        if (_fixedLaneSetStartWindow != startWindow)
        {
            _fixedLaneSetStartWindow = startWindow;
            Array.Fill(_fixedLaneSetBuckets, -1);
        }

        if (_currentWindow != startWindow || _tetherTargets.Count < ExpectedSourcesByWindow[startWindow])
            return;

        for (var lane = 0; lane < _fixedLaneSetBuckets.Length; lane++)
            if (_fixedLaneSetBuckets[lane] < 0)
            {
                var bucket = C.AssignmentMode == AssignmentMode.FixedRoleAccretion
                    ? ExpectedFixedSpotBucketUncached(lane)
                    : ExpectedMarkerOrFlexLaneBucket(lane);
                if (bucket >= 0)
                    _fixedLaneSetBuckets[lane] = bucket;
            }
    }

    private void ClearSelfTether(bool keepWindowText)
    {
        _selfBlackHoleTask = null;
        _instruction = keepWindowText ? LineWindowInstruction() : "";
    }

    private string LineWindowInstruction()
    {
        if (_state != State.BlackHoleActive || _selfSlot == Slot.None || _currentWindow is < 0 or > 9 ||
            _selfCompletedWindow == _currentWindow)
            return "";

        var nextWindow = NextSelfWindow(_currentWindow);
        if (nextWindow < 0)
            return "";
        if (nextWindow == _currentWindow)
            return TextOrEmpty(C.ShowTakeLineNowText, C.TakeLineNowText, _currentWindow + 1);
        if (nextWindow == _currentWindow + 1)
            return TextOrEmpty(C.ShowNextLineWindowText, C.NextLineWindowText, _currentWindow + 1);
        return TextOrEmpty(C.ShowFirstLineWindowText, C.FirstLineWindowText, _currentWindow + 1, nextWindow + 1);
    }

    private int NextSelfWindow(int fromWindow)
    {
        for (var window = Math.Max(0, fromWindow); window <= 9; window++)
            if (ExpectedRank(_selfSlot, window) >= 0)
                return window;
        return -1;
    }

    private void RunMarkerCommand(TargetGroup group)
    {
        if (C.MarkerCommandSource != MarkerCommandSource.TargetDebuff)
            return;
        if (ShouldSkipTargetMarkerForAccretion())
        {
            _sentMarkerCommand = true;
            return;
        }

        QueueMarkerCommand(group switch
        {
            TargetGroup.Attack => C.FirstTargetCommand,
            TargetGroup.Bind => C.SecondTargetCommand,
            TargetGroup.Stop => C.ThirdTargetCommand,
            _ => ""
        }, true);
    }

    private void RunAccretionMarkerCommand()
    {
        if (C.MarkerCommandSource == MarkerCommandSource.AccretionDebuff)
            QueueMarkerCommand(C.AccretionCommand);
    }

    private bool ShouldSkipTargetMarkerForAccretion()
    {
        if (!C.SkipTargetMarkerOnAccretion)
            return false;
        if (_selfHadAccretionMarkerBlock || _accretionPlayers.Contains(BasePlayer?.EntityId ?? 0))
            return true;
        return BasePlayer?.StatusList.Any(status => status.StatusId is AccretionStatus or LineDoneStatus) == true;
    }

    private void CancelPendingTargetMarkerCommandForAccretion()
    {
        if (!C.SkipTargetMarkerOnAccretion || C.MarkerCommandSource != MarkerCommandSource.TargetDebuff ||
            !_pendingTargetMarkerCommand)
            return;

        _pendingMarkerCommand = "";
        _markerCommandAtMs = 0;
        _pendingTargetMarkerCommand = false;
    }

    private void QueueMarkerCommand(string command, bool targetDebuffCommand = false)
    {
        if (_sentMarkerCommand) return;
        _sentMarkerCommand = true;
        if (!C.ExecuteMarkerCommand || Svc.Condition[ConditionFlag.DutyRecorderPlayback]) return;
        _pendingMarkerCommand = command ?? "";
        if (string.IsNullOrWhiteSpace(_pendingMarkerCommand))
        {
            _pendingTargetMarkerCommand = false;
            return;
        }
        _pendingTargetMarkerCommand = targetDebuffCommand;

        _markerCommandAtMs = Environment.TickCount64 + ToRandomDelayMs(C.MarkerDelayMinSeconds, C.MarkerDelayMaxSeconds);
    }

    private void ExecutePendingMarkerCommand()
    {
        if (_markerCommandAtMs <= 0 || Environment.TickCount64 < _markerCommandAtMs) return;

        var command = _pendingMarkerCommand;
        _pendingMarkerCommand = "";
        _pendingTargetMarkerCommand = false;
        _markerCommandAtMs = 0;
        if (!string.IsNullOrWhiteSpace(command) && !Svc.Condition[ConditionFlag.DutyRecorderPlayback])
            Chat.ExecuteCommand(command);
    }

    private void SetSelfTether(Vector3 source, int bucket, uint target)
    {
        var flatSource = FlatPosition(source);
        var destination = BlackHoleStandPosition(flatSource);
        _selfBlackHoleTask = new BlackHoleTask(bucket, flatSource, target, destination);
        _instruction = LineWindowInstruction();
    }

    private void AdvanceWindow(int sourceBucket)
    {
        if (_state != State.BlackHoleActive || _currentWindow is < 0 or > 9) return;
        var expected = ExpectedBucket(_selfSlot);
        if (_selfCompletedWindow != _currentWindow &&
            (sourceBucket == _selfTetherBucket || expected >= 0 && sourceBucket == expected))
        {
            _selfCompletedWindow = _currentWindow;
            ClearSelfTether(false);
        }

        _hitSources.Add(sourceBucket);
        if (_hitSources.Count < ExpectedSourcesByWindow[_currentWindow]) return;

        if (_selfTetherBucket >= 0 && _hitSources.Contains(_selfTetherBucket))
            ClearSelfTether(true);
        _hitSources.Clear();
        ClearCurrentWindowTethers();
        _currentWindow++;
        if (_currentWindow > 9)
            EnterFinalSequence();
    }

    private void EnterFinalSequence()
    {
        if (_state == State.Completed) return;

        _state = State.FinalSequence;
        if (_finalStage == FinalStage.None)
            _finalStage = FinalStage.AwaitingBlizzaga;
        _currentWindow = Math.Max(_currentWindow, 10);
        _instruction = "";
        ClearSelfTether(false);
        ClearBlackHoleState();
    }

    private void SetFinalCenterBait()
    {
        EnterFinalSequence();
        _finalStage = FinalStage.CenterBait;
        if (TryGetFinalInitialBaitGuide(out var destination, out var text))
            SetGuide(destination, text, GuidanceKind.FinalCenter, LateP3Blizzaga, 0.0f, 0.0f);
        else
            SetInstruction(TextOrEmpty(C.ShowFinalCenterText, C.FinalCenterText), GuidanceKind.FinalCenter);
    }

    private bool TryGetFinalInitialBaitGuide(out Vector3 destination, out string text)
    {
        if (C.FinalInitialBaitMode == FinalInitialBaitMode.Center)
        {
            destination = Center;
            text = TextOrEmpty(C.ShowFinalCenterText, C.FinalCenterText);
            return true;
        }

        var ownStackRole = OwnFinalStackRole();
        if (ownStackRole == FinalStackRole.Unknown)
        {
            destination = default;
            text = "";
            return false;
        }

        var northRole = C.FinalInitialNorthRole == FinalInitialNorthRole.Support
            ? FinalStackRole.Support
            : FinalStackRole.Dps;
        var goNorth = ownStackRole == northRole;
        var angle = NormalizeAngle(OrderAnchorAngle() + (goNorth ? 0.0f : MathF.PI));
        destination = PositionFromDirectionAngle(angle, FinalInitialSplitRadius);
        text = TextOrEmpty(C.ShowFinalRoleSplitText, C.FinalRoleSplitText, ownStackRole == FinalStackRole.Support ? "Support" : "DPS");
        return true;
    }

    private void SetFinalRoleSpread(uint actionId)
    {
        EnterFinalSequence();
        _finalStage = FinalStage.RoleSpread;
        SetFinalRoleGuide(C.ShowFinalSpreadText, C.FinalSpreadText, GuidanceKind.FinalSpread, actionId);
    }

    private void RecordFinalStackRole(FinalStackRole stackRole)
    {
        if (stackRole == FinalStackRole.Unknown)
            return;
        _finalStackMarkerCount++;
        if (_firstFinalStackRole == FinalStackRole.Unknown)
            _firstFinalStackRole = stackRole;
        else if (_secondFinalStackRole == FinalStackRole.Unknown)
            _secondFinalStackRole = stackRole;
    }

    private void SetKnownFinalLanding()
    {
        if (_firstFinalStackRole != FinalStackRole.Unknown)
            SetFinalLanding(_firstFinalStackRole);
    }

    private void SetSecondFinalLanding()
    {
        var stackRole = _secondFinalStackRole != FinalStackRole.Unknown
            ? _secondFinalStackRole
            : Opposite(_firstFinalStackRole);
        _landingCount = 1;
        SetFinalLanding(stackRole);
    }

    private void SetFinalLanding(FinalStackRole stackRole)
    {
        if (stackRole == FinalStackRole.Unknown)
            return;

        EnterFinalSequence();
        _finalStage = _landingCount == 0 ? FinalStage.Landing1 : FinalStage.Landing2;
        _currentFinalStackRole = stackRole;
        if (_firstFinalStackRole == FinalStackRole.Unknown)
            _firstFinalStackRole = stackRole;

        var ownStackRole = OwnFinalStackRole();
        if (ownStackRole == stackRole)
        {
            SetGuide(Center, TextOrEmpty(C.ShowFinalStackText, C.FinalStackText), GuidanceKind.FinalLanding,
                LandingCast, 0.0f, 0.0f);
            return;
        }

        if (TryGetOwnRolePosition(out var role))
        {
            var destination = FinalTowerPosition(role);
            SetGuide(destination, TextOrEmpty(C.ShowFinalTowerText, C.FinalTowerText, RolePairName(role)),
                GuidanceKind.FinalLanding, LandingCast, 0.0f, 0.0f);
        }
        else
        {
            SetInstruction(TextOrEmpty(C.ShowFinalTowerText, C.FinalTowerText, "?"), GuidanceKind.FinalLanding);
        }
    }

    private void SetFinalMove()
    {
        EnterFinalSequence();
        _finalStage = FinalStage.ProtrudeMove;
        SetFinalRoleGuide(C.ShowFinalMoveText, C.FinalMoveText, GuidanceKind.FinalMove, Protrude);
    }

    private void SetFinalRoleGuide(bool show, InternationalString text, GuidanceKind kind, uint actionId)
    {
        if (TryGetOwnRolePosition(out var role))
        {
            var destination = FinalSpreadPosition(role);
            SetGuide(destination, TextOrEmpty(show, text, RolePairName(role)), kind, actionId, 0.0f, 0.0f);
        }
        else
        {
            SetInstruction(TextOrEmpty(show, text, "?"), kind);
        }
    }

    private void Complete()
    {
        _state = State.Completed;
        ClearMechanicState(clearSlot: false);
        HideElements();
    }

    private void ResetAll()
    {
        _groups.Clear();
        _selfPlayerId = 0;
        _sentMarkerCommand = false;
        _pendingMarkerCommand = "";
        _pendingTargetMarkerCommand = false;
        _markerCommandAtMs = 0;
        ClearMechanicState(clearSlot: true);
        HideElements();
        _state = State.Idle;
    }

    private void ClearMechanicState(bool clearSlot)
    {
        _earthPlayers.Clear();
        _accretionPlayers.Clear();
        _selfHadAccretionMarkerBlock = false;
        _kefkaId = 0;
        _kefkaPosition = null;
        _kefkaAnchorDebug = "";
        _earthMaxCount = 0;
        _currentWindow = -1;
        ClearFinalState();
        if (clearSlot)
            ClearSelfResolution();
        else
            ClearSelfDisplayState();
        ClearGuide();
        ClearBlackHoleState();
    }

    private void ClearFinalState()
    {
        _finalStage = FinalStage.None;
        _firstFinalStackRole = FinalStackRole.Unknown;
        _secondFinalStackRole = FinalStackRole.Unknown;
        _currentFinalStackRole = FinalStackRole.Unknown;
        _landingCount = 0;
        _finalStackMarkerCount = 0;
        _finalDondokoHitCount = 0;
        _finalTowerPositions.Clear();
    }

    private void ClearBlackHoleState()
    {
        ClearCurrentWindowTethers();
        _blackHolePositions.Clear();
        _hitSources.Clear();
        ClearFixedLaneSetBuckets();
    }

    private void ShowDestination(Vector3 destination, string text, uint? color = null)
    {
        if (!Controller.TryGetElementByName(DestinationElement, out var element)) return;
        element.Enabled = true;
        element.color = color ?? NavigationColor();
        element.SetRefPosition(destination);
        element.overlayText = text;
    }

    private bool ShowBlackHoleDestination()
    {
        if (_selfBlackHoleTask is not { } task)
            return false;

        if (IsSelfTetherTarget())
        {
            ShowDestination(task.StandPosition, "");
            return true;
        }

        if (!TryGetSelfTetherTarget(out var target))
            return false;

        ShowDestination(FlatPosition(target.Position), "");
        return true;
    }

    private void ShowBlackHoleLine()
    {
        if (_selfTetherSource is not { } source ||
            _selfTetherTarget.GetObject() is not { } target ||
            !Controller.TryGetElementByName(BlackHoleLineElement, out var element))
            return;

        var expected = ExpectedBucket(_selfSlot);
        element.Enabled = true;
        element.color = expected < 0 || _selfTetherBucket < 0
            ? C.UnknownTetherColor
            : expected != _selfTetherBucket || !IsSelfTetherTarget()
                ? C.WrongTetherColor
                : C.CorrectTetherColor;
        element.SetRefPosition(source);
        element.SetOffPosition(target.Position);
    }

    private bool IsSelfTetherTarget() => _selfTetherTarget != 0 && _selfTetherTarget == BasePlayer?.EntityId;

    private bool TryGetSelfTetherTarget(out IGameObject target)
    {
        if (_selfTetherTarget.GetObject() is { } obj)
        {
            target = obj;
            return true;
        }

        target = null!;
        return false;
    }

    private void ShowInstruction(string text)
    {
        if (!Controller.TryGetElementByName(InstructionElement, out var element)) return;
        if (string.IsNullOrWhiteSpace(text)) return;
        element.Enabled = true;
        element.SetRefPosition(BasePlayer.Position);
        element.overlayText = text;
    }

    private void HideElements()
    {
        foreach (var element in Controller.GetRegisteredElements().Values)
            element.Enabled = false;
    }

    private static bool TryResolveBlackHoleTether(uint source, uint target, out uint blackHoleId,
        out uint tetherTarget, out Vector3 blackHolePosition, out int bucket)
    {
        if (TryResolveBlackHoleEndpoint(source, out blackHolePosition, out bucket))
        {
            blackHoleId = source;
            tetherTarget = target;
            return true;
        }

        if (TryResolveBlackHoleEndpoint(target, out blackHolePosition, out bucket))
        {
            blackHoleId = target;
            tetherTarget = source;
            return true;
        }

        blackHoleId = 0;
        tetherTarget = 0;
        blackHolePosition = default;
        bucket = -1;
        return false;
    }

    private static bool TryResolveBlackHoleEndpoint(uint actorId, out Vector3 position, out int bucket)
    {
        if (actorId.GetObject() is { } obj && IsBlackHoleObject(obj) && TryBucket(obj.Position, out bucket))
        {
            position = new Vector3(obj.Position.X, 0.0f, obj.Position.Z);
            return true;
        }

        position = default;
        bucket = -1;
        return false;
    }

    private static bool TryBucket(Vector3 position, out int bucket)
    {
        var v = new Vector2(position.X - Center.X, position.Z - Center.Z);
        var r = v.Length();
        if (r is < BlackHoleRadiusMin or > BlackHoleRadiusMax)
        {
            bucket = -1;
            return false;
        }

        bucket = Math.Abs(v.X) > Math.Abs(v.Y)
            ? v.X > 0 ? 1 : 3
            : v.Y > 0 ? 2 : 0;
        return true;
    }

    private void UpdateKefkaAnchor(uint actorId, uint actionId, float? eventRotation = null)
    {
        if (TryGetKefkaAnchorOffset(actionId, out var offset) &&
            TryGetKefkaAnchorRotation(actorId, eventRotation, out var rotation))
        {
            if (!TryCaptureKefkaFromMatchingClone(rotation))
                CaptureKefkaFromRotation(rotation, offset);
        }
        else if (actionId == DecisiveBattleChaos)
            CaptureKefka(actorId, true);
        else if (actorId != 0 && actorId == _kefkaId)
            CaptureKefka(actorId, false);
    }

    private bool TryGetKefkaAnchorRotation(uint actorId, float? eventRotation, out float rotation)
    {
        if (eventRotation.HasValue)
        {
            rotation = eventRotation.Value;
            return true;
        }

        return TryGetCastRotation(actorId, out rotation);
    }

    private void CaptureKefkaFromRotation(float rotation, float offset)
    {
        var angle = NormalizeAngle(rotation + offset);
        var position = PositionFromDirectionAngle(angle, KefkaVirtualAnchorRadius);
        _kefkaId = 0;
        _kefkaPosition = position;
        _kefkaAnchorDebug = $"cast-rotation {Deg(rotation):F1}{SignedDeg(offset)}";
    }

    private bool TryCaptureKefkaFromMatchingClone(float rotation)
    {
        IGameObject? best = null;
        var bestDiff = float.MaxValue;
        foreach (var obj in Svc.Objects)
        {
            if (!IsKefkaAnchorObject(obj))
                continue;
            var diff = AngleDistance(obj.Rotation, rotation);
            if (diff < bestDiff)
            {
                best = obj;
                bestDiff = diff;
            }
        }

        if (best == null || bestDiff > KefkaRotationMatchMax)
            return false;

        var position = FlatPosition(best.Position);
        _kefkaId = 0;
        _kefkaPosition = position;
        _kefkaAnchorDebug = $"rotation-match {Deg(rotation):F1}->{Describe(best.EntityId)}";
        return true;
    }

    private void RefreshKefkaAnchorFromObject()
    {
        if (_kefkaId != 0)
            CaptureKefka(_kefkaId, false);
    }

    private void CaptureKefka(uint actorId, bool force)
    {
        if (actorId.GetObject() is { } obj)
            CaptureKefka(obj, force);
    }

    private void ObserveFinalTowerSource(IGameObject? source, Vector3? eventPosition, uint actionId, string origin)
    {
        if (actionId is not (DondokoHit or TowerImpact))
            return;

        var position = source != null ? FlatPosition(source.Position) :
            eventPosition.HasValue ? FlatPosition(eventPosition.Value) : default;
        var distance = Vector2.Distance(new Vector2(position.X, position.Z), new Vector2(Center.X, Center.Z));
        if (distance is < 7.5f or > 13.5f ||
            _finalTowerPositions.Any(x => Vector3.DistanceSquared(x, position) < 1.0f))
            return;

        _finalTowerPositions.Add(position);
        if (_guideKind == GuidanceKind.FinalLanding &&
            _currentFinalStackRole != FinalStackRole.Unknown &&
            TryGetOwnRolePosition(out _) &&
            OwnFinalStackRole() != _currentFinalStackRole)
            SetFinalLanding(_currentFinalStackRole);
    }

    private static string PositionText(Vector3 position) => $"({position.X:F2},{position.Z:F2})";

    private void CaptureKefka(IGameObject? obj, bool force)
    {
        if (obj == null) return;
        if (!force && _kefkaId != 0 && obj.EntityId != _kefkaId) return;
        var position = FlatPosition(obj.Position);
        if (!IsKefkaAnchorPosition(position)) return;
        _kefkaId = obj.EntityId;
        _kefkaPosition = position;
        _kefkaAnchorDebug = $"object {Describe(obj.EntityId)}";
    }

    private bool TryGetKefkaPosition(out Vector3 position)
    {
        if (_kefkaPosition is { } cached)
        {
            position = cached;
            return true;
        }

        if (_kefkaId.GetObject() is { } obj)
        {
            position = FlatPosition(obj.Position);
            return true;
        }

        position = default;
        return false;
    }

    private static bool IsKefkaAnchorPosition(Vector3 position) =>
        Vector2.Distance(new Vector2(position.X, position.Z), new Vector2(Center.X, Center.Z)) >= KefkaAnchorRadiusMin;

    private static bool IsKefkaAnchorObject(IGameObject obj) =>
        obj.DataId == KefkaDataId && IsKefkaAnchorPosition(FlatPosition(obj.Position));

    private static bool TryGetKefkaAnchorOffset(uint actionId, out float offset)
    {
        offset = actionId switch
        {
            BintaStackCast => MathF.PI / 2.0f,
            BintaSpreadCast => -MathF.PI / 2.0f,
            AsIsFirst => 0.0f,
            AsIsSecond => MathF.PI,
            _ => float.NaN
        };
        return !float.IsNaN(offset);
    }

    private float OrderAnchorAngle() =>
        TryGetKefkaPosition(out var kefka) ? DirectionAngle(kefka) : 0.0f;

    private float SourceAngle(int bucket) =>
        _tetherSources.TryGetValue(bucket, out var source) ? DirectionAngle(source) : BucketAngle(bucket);

    private float OrderedAngleDistance(float sourceAngle, float anchorAngle) =>
        C.BlackHoleSourceOrder == BlackHoleSourceOrder.ClockwiseFromNorth
            ? NormalizeAngle(sourceAngle - anchorAngle)
            : NormalizeAngle(anchorAngle - sourceAngle);

    private string OrderAnchorDebugText() => TryGetKefkaPosition(out var pos)
        ? $"{(string.IsNullOrWhiteSpace(_kefkaAnchorDebug) ? "Kefka" : _kefkaAnchorDebug)}({pos.X:F1},{pos.Z:F1})"
        : "N";

    private static Vector3 FlatPosition(Vector3 position) => new(position.X, 0.0f, position.Z);

    private string KefkaCandidateText()
    {
        var items = new List<string>();
        var index = 0;
        foreach (var obj in Svc.Objects)
        {
            if (IsKefkaAnchorObject(obj))
            {
                var selected = obj.EntityId == _kefkaId ? "*" : "";
                items.Add($"{selected}{index}:{KefkaCandidateLine(obj)}");
            }
            index++;
        }

        if (items.Count == 0)
            return "none";

        return items.Count <= 24
            ? string.Join(" ", items)
            : $"{string.Join(" ", items.Take(24))} ... +{items.Count - 24}";
    }

    private static string KefkaCandidateLine(IGameObject obj)
    {
        var pos = FlatPosition(obj.Position);
        var distance = Vector2.Distance(new Vector2(pos.X, pos.Z), new Vector2(Center.X, Center.Z));
        var angle = DirectionAngle(pos) * 180.0f / MathF.PI;
        var visible = obj is ICharacter character && character.IsCharacterVisible();
        var targetable = obj.Struct()->GetIsTargetable();
        var rotation = obj.Rotation * 180.0f / MathF.PI;
        var goid = (ulong)obj.Struct()->GetGameObjectId();
        return $"{obj.EntityId}/{obj.EntityId:X8}/go={goid:X}@({pos.X:F1},{pos.Z:F1}) r={distance:F1} a={angle:F0} rot={rotation:F0} vis={visible} tar={targetable}";
    }

    private static string DescribeTethers(ICharacter character)
    {
        var entries = new List<string>();
        var tethers = character.Struct()->Vfx.Tethers;
        for (var i = 0; i < tethers.Length; i++)
        {
            var tether = tethers[i];
            if (tether.Id == 0) continue;
            entries.Add($"{i}:{tether.Id}/{tether.Progress}/{tether.TargetId.ObjectId:X8}");
        }

        return entries.Count == 0 ? "none" : string.Join(",", entries);
    }

    private static float DirectionAngle(Vector3 position)
    {
        var v = new Vector2(position.X - Center.X, Center.Z - position.Z);
        return v.LengthSquared() < 0.01f ? 0.0f : NormalizeAngle(MathF.Atan2(v.X, v.Y));
    }

    private static float BucketAngle(int bucket) => NormalizeAngle(bucket * MathF.PI / 2.0f);

    private static float NormalizeAngle(float angle)
    {
        const float tau = MathF.PI * 2.0f;
        angle %= tau;
        return angle < 0.0f ? angle + tau : angle;
    }

    private static float AngleDistance(float a, float b)
    {
        var diff = Math.Abs(NormalizeAngle(a) - NormalizeAngle(b));
        return Math.Min(diff, MathF.PI * 2.0f - diff);
    }

    private Vector3 BlackHoleStandPosition(Vector3 source)
    {
        var direction = _currentWindow == 0 && C.FirstWindowBaitDirection != FirstWindowBaitDirection.SameAsLineBaitDirection
            ? C.FirstWindowBaitDirection == FirstWindowBaitDirection.Counterclockwise
                ? LineBaitDirection.Counterclockwise
                : LineBaitDirection.Clockwise
            : C.LineBaitDirection;
        var side = direction == LineBaitDirection.Counterclockwise ? -1.0f : 1.0f;
        var radius = _currentWindow == 9 ? LastBlackHoleGuideRadius : BlackHoleGuideRadius;
        var angle = DirectionAngle(source) + side * MathF.PI / 4.0f;
        var best = PositionFromDirectionAngle(angle, radius);
        var bestDistance = NearestBlackHoleDistanceSq(best);
        if (bestDistance >= BlackHoleAvoidRadiusSq)
            return best;

        for (var inward = 0; inward <= BlackHoleAvoidInwardSteps; inward++)
        {
            var candidateRadius = Math.Max(BlackHoleAvoidMinRadius, radius - inward * BlackHoleAvoidInwardStep);
            for (var step = inward == 0 ? 1 : 0; step <= BlackHoleAvoidAngleSteps; step++)
            {
                if (step == 0)
                {
                    if (Consider(angle, candidateRadius))
                        return best;
                }
                else
                {
                    var offset = step * BlackHoleAvoidAngleStep;
                    if (Consider(angle + side * offset, candidateRadius))
                        return best;
                    if (Consider(angle - side * offset, candidateRadius))
                        return best;
                }
            }
        }

        return best;

        bool Consider(float candidateAngle, float candidateRadius)
        {
            var candidate = PositionFromDirectionAngle(candidateAngle, candidateRadius);
            var distance = NearestBlackHoleDistanceSq(candidate);
            if (distance <= bestDistance)
                return false;
            best = candidate;
            bestDistance = distance;
            return bestDistance >= BlackHoleAvoidRadiusSq;
        }

        float NearestBlackHoleDistanceSq(Vector3 candidate)
        {
            var nearest = float.MaxValue;
            foreach (var position in _blackHolePositions)
                nearest = Math.Min(nearest, Vector3.DistanceSquared(candidate, position));
            return nearest;
        }
    }

    private LineBaitDirection LaneBaitDirection(int lane)
    {
        if (lane == 2) return C.AccretionLineBaitDirection;
        if (lane is not (0 or 1)) return C.LineBaitDirection;

        var supportFirst = C.FirstOrbRole == FirstOrbRole.Support;
        var supportLane = lane == 0 ? supportFirst : !supportFirst;
        return supportLane ? C.SupportLineBaitDirection : C.DpsLineBaitDirection;
    }

    private int ExpectedBucket(Slot slot)
    {
        if (TryFirstPairBucket(slot, out var firstPairBucket))
            return firstPairBucket;

        var rank = ExpectedRank(slot, _currentWindow);
        if (C.AssignmentMode == AssignmentMode.FixedRoleAccretion)
            return ExpectedFixedSpotBucket(rank);
        if (C.AssignmentMode == AssignmentMode.FixedMarkerLanes)
            return ExpectedFixedMarkerLaneBucket(slot, rank);
        var buckets = OrderedActiveBuckets();
        return rank >= 0 && rank < buckets.Count ? buckets[rank] : -1;
    }

    private bool TryFirstPairBucket(Slot slot, out int bucket)
    {
        bucket = -1;
        if (C.FirstPairAssignment != FirstPairAssignment.FirstSlotNearest || _currentWindow != 1 ||
            slot is not (Slot.Attack1 or Slot.Attack2))
            return false;

        var activeBuckets = _tetherTargets.Keys.Where(_tetherSources.ContainsKey).ToList();
        if (activeBuckets.Count != 2)
            return false;

        IPlayerCharacter? firstPlayer = null;
        foreach (var player in Svc.Objects.OfType<IPlayerCharacter>())
        {
            if (!_groups.ContainsKey(player.EntityId)) continue;
            if (TryResolveSlot(player, out var resolved, out _) && resolved == Slot.Attack1)
            {
                firstPlayer = player;
                break;
            }
        }

        if (firstPlayer == null)
            return false;

        var firstPosition = FlatPosition(firstPlayer.Position);
        var firstBucket = activeBuckets
            .OrderBy(activeBucket =>
            {
                var source = _tetherSources[activeBucket];
                return Vector2.DistanceSquared(
                    new Vector2(firstPosition.X, firstPosition.Z),
                    new Vector2(source.X, source.Z));
            })
            .ThenBy(activeBucket => activeBucket)
            .First();
        bucket = slot == Slot.Attack1 ? firstBucket : activeBuckets.First(activeBucket => activeBucket != firstBucket);
        return true;
    }

    private int ExpectedFixedSpotBucket(int rank)
    {
        if (rank < 0) return -1;
        var startWindow = FixedMarkerSetStartWindow(_currentWindow);
        if (startWindow >= 0 && rank < _fixedLaneSetBuckets.Length)
        {
            if (_fixedLaneSetStartWindow != startWindow)
            {
                _fixedLaneSetStartWindow = startWindow;
                Array.Fill(_fixedLaneSetBuckets, -1);
            }

            if (_fixedLaneSetBuckets[rank] < 0 &&
                _currentWindow == startWindow &&
                _tetherTargets.Count >= ExpectedSourcesByWindow[startWindow])
            {
                var bucket = ExpectedFixedSpotBucketUncached(rank);
                if (bucket >= 0)
                    _fixedLaneSetBuckets[rank] = bucket;
            }

            return _fixedLaneSetBuckets[rank];
        }

        return ExpectedFixedSpotBucketUncached(rank);
    }

    private int ExpectedFixedSpotBucketUncached(int rank)
    {
        if (rank < 0) return -1;
        var buckets = OrderedBuckets();
        if (rank >= buckets.Count) return -1;
        var preferred = buckets[rank];
        if (_tetherTargets.ContainsKey(preferred))
            return preferred;
        var fallback = buckets[(int)C.FallbackMarker];
        return _tetherTargets.ContainsKey(fallback) ? fallback : -1;
    }

    private int ExpectedFixedMarkerLaneBucket(Slot slot, int activeRank)
    {
        if (activeRank < 0) return -1;
        var lane = RankFromSlot(slot);
        if (lane < 0) return -1;

        if (IsSnakeSetWindow(_currentWindow) && lane is 0 or 1)
            return TryDirectionalMarkerBucket(LaneMarker(lane), LaneBaitDirection(lane), out var bucket) ? bucket : -1;

        return ExpectedFixedLaneSetBucket(lane);
    }

    private int ExpectedFixedLaneSetBucket(int lane)
    {
        var startWindow = FixedMarkerSetStartWindow(_currentWindow);
        if (startWindow < 0 || lane is < 0 or > 2)
            return -1;

        if (_fixedLaneSetStartWindow != startWindow)
        {
            _fixedLaneSetStartWindow = startWindow;
            Array.Fill(_fixedLaneSetBuckets, -1);
        }

        if (_currentWindow == startWindow &&
            _tetherTargets.Count >= ExpectedSourcesByWindow[startWindow] &&
            _fixedLaneSetBuckets[lane] < 0)
        {
            var bucket = ExpectedMarkerOrFlexLaneBucket(lane);
            if (bucket >= 0)
                _fixedLaneSetBuckets[lane] = bucket;
        }

        return _fixedLaneSetBuckets[lane];
    }

    private int ExpectedMarkerOrFlexLaneBucket(int lane)
    {
        if (TryMarkerBucket(LaneMarker(lane), out var bucket))
            return bucket;

        if (!IsMarkerFlexSetWindow(_currentWindow))
            return -1;

        return TryMarkerBucket(C.LaneFlexMarker, out bucket) ? bucket : -1;
    }

    private void ClearFixedLaneSetBuckets()
    {
        _fixedLaneSetStartWindow = -1;
        Array.Fill(_fixedLaneSetBuckets, -1);
    }

    private MapMarker LaneMarker(int lane)
    {
        if (lane == 2) return C.LaneAccretionMarker;

        var supportFirst = C.FirstOrbRole == FirstOrbRole.Support;
        var supportLane = lane == 0 ? supportFirst : !supportFirst;
        return supportLane ? C.LaneSupportMarker : C.LaneDpsMarker;
    }

    private bool TryMarkerBucket(MapMarker marker, out int bucket)
    {
        var buckets = OrderedBuckets();
        bucket = (int)marker < buckets.Count ? buckets[(int)marker] : -1;
        return bucket >= 0 && _tetherTargets.ContainsKey(bucket);
    }

    private bool TryDirectionalMarkerBucket(MapMarker marker, LineBaitDirection direction, out int bucket)
    {
        var buckets = OrderedBuckets();
        var start = (int)marker;
        if (start < 0 || start >= buckets.Count)
        {
            bucket = -1;
            return false;
        }

        var step = DirectionStep(direction);
        for (var i = 0; i < buckets.Count; i++)
        {
            var index = (start + step * i + buckets.Count) % buckets.Count;
            var candidate = buckets[index];
            if (_tetherTargets.ContainsKey(candidate))
            {
                bucket = candidate;
                return true;
            }
        }

        bucket = -1;
        return false;
    }

    private string BlackHoleExpectedDebugText(string reason)
    {
        var expected = ExpectedBucket(_selfSlot);
        var expectedTarget = expected >= 0 && _tetherTargets.TryGetValue(expected, out var target)
            ? Describe(target)
            : "none";
        var expectedSource = expected >= 0 && _tetherSources.ContainsKey(expected);
        var selfTarget = _selfTetherTarget != 0 && _selfTetherTarget == BasePlayer?.EntityId;

        return $"reason={reason} mode={C.AssignmentMode} window={_currentWindow} slot={_selfSlot} " +
               $"rank={ExpectedRank(_selfSlot, _currentWindow)} expected={DirectionName(expected)} " +
               $"hits={HitSourceDebugText()} liveActors={_blackHolePositions.Count}/{ExpectedBlackHoleActors} " +
               $"source={expectedSource} target={expectedTarget} selfBucket={DirectionName(_selfTetherBucket)} " +
               $"selfTarget={Describe(_selfTetherTarget)} targetSelf={selfTarget} ordered=[{OrderedBucketText()}] " +
               $"active=[{ActiveBucketText()}] decision={ExpectedDecisionText(_selfSlot)}";
    }

    private string HitSourceDebugText()
    {
        var expected = _currentWindow >= 0 && _currentWindow < ExpectedSourcesByWindow.Length
            ? ExpectedSourcesByWindow[_currentWindow]
            : 0;
        var hitText = _hitSources.Count == 0
            ? "none"
            : string.Join(",", _hitSources.OrderBy(x => x).Select(DirectionName));
        return $"{_hitSources.Count}/{expected}[{hitText}]";
    }

    private string ExpectedDecisionText(Slot slot)
    {
        var rank = ExpectedRank(slot, _currentWindow);
        if (slot == Slot.None)
            return "slot=none";
        if (rank < 0)
            return "not-assigned-this-window";
        if (TryFirstPairBucket(slot, out var firstPairBucket))
            return $"first-pair nearest expected={DirectionName(firstPairBucket)}";
        if (C.AssignmentMode == AssignmentMode.FixedMarkerLanes)
            return FixedMarkerLaneDecisionText(slot, rank);
        if (C.AssignmentMode == AssignmentMode.FixedRoleAccretion)
            return FixedRoleDecisionText(rank);

        var active = OrderedActiveBuckets();
        return $"ordered-active rank={rank} count={active.Count}";
    }

    private string FixedRoleDecisionText(int rank)
    {
        var cached = rank is >= 0 and <= 2 ? _fixedLaneSetBuckets[rank] : -1;
        var markerText = MarkerBucketText((MapMarker)Math.Clamp(rank, 0, MapMarkerNames.Length - 1));
        if (!IsMarkerFlexSetWindow(_currentWindow))
            return $"fixed-role rank={rank} markerIndex={rank} marker={markerText} no-cache-window";
        return $"fixed-role rank={rank} markerIndex={rank} set={FixedMarkerSetStartWindow(_currentWindow)} " +
               $"cached={DirectionName(cached)} marker={markerText} fallback={MarkerBucketText(C.FallbackMarker)}";
    }

    private string FixedMarkerLaneDecisionText(Slot slot, int rank)
    {
        var lane = RankFromSlot(slot);
        if (lane < 0)
            return "fixed-lane lane=none";

        var marker = LaneMarker(lane);
        var laneName = LaneName(lane);
        if (IsSnakeSetWindow(_currentWindow) && lane is 0 or 1)
            return $"fixed-lane rank={rank} lane={lane}:{laneName} snake marker={marker} " +
                   $"dir={LaneBaitDirection(lane)} scan=[{DirectionalScanDebugText(marker, LaneBaitDirection(lane))}]";

        var cached = lane is >= 0 and <= 2 ? _fixedLaneSetBuckets[lane] : -1;
        var markerText = MarkerBucketText(marker);
        if (!IsMarkerFlexSetWindow(_currentWindow))
            return $"fixed-lane rank={rank} lane={lane}:{laneName} marker={markerText} no-flex-window";

        return $"fixed-lane rank={rank} lane={lane}:{laneName} set={FixedMarkerSetStartWindow(_currentWindow)} " +
               $"cached={DirectionName(cached)} marker={markerText} flex={MarkerBucketText(C.LaneFlexMarker)}";
    }

    private string LaneName(int lane)
    {
        if (lane == 2)
            return "Accretion";
        if (lane is not (0 or 1))
            return "Unknown";

        var supportFirst = C.FirstOrbRole == FirstOrbRole.Support;
        var supportLane = lane == 0 ? supportFirst : !supportFirst;
        return supportLane ? "Support" : "DPS";
    }

    private string MarkerBucketText(MapMarker marker)
    {
        var buckets = OrderedBuckets();
        var index = (int)marker;
        if (index < 0 || index >= buckets.Count)
            return $"{marker}->invalid";

        var bucket = buckets[index];
        return $"{marker}->{DirectionName(bucket)}:{(_tetherTargets.TryGetValue(bucket, out var target) ? Describe(target) : "none")}";
    }

    private string DirectionalScanDebugText(MapMarker marker, LineBaitDirection direction)
    {
        var buckets = OrderedBuckets();
        var start = (int)marker;
        if (start < 0 || start >= buckets.Count)
            return "invalid-marker";

        var step = DirectionStep(direction);
        var parts = new List<string>();
        for (var i = 0; i < buckets.Count; i++)
        {
            var index = (start + step * i + buckets.Count) % buckets.Count;
            var bucket = buckets[index];
            parts.Add($"{(MapMarker)index}->{DirectionName(bucket)}:{(_tetherTargets.TryGetValue(bucket, out var target) ? Describe(target) : "none")}");
        }
        return string.Join(" ", parts);
    }

    private static int ExpectedRank(Slot slot, int window) =>
        window >= 0 && window < BlackHoleWindowSlots.Length
            ? Array.IndexOf(BlackHoleWindowSlots[window], slot)
            : -1;

    private static bool IsSnakeSetWindow(int window) => window is 0 or 1 or 8 or 9;

    private static bool IsMarkerFlexSetWindow(int window) => window is >= 2 and <= 7;

    private static int FixedMarkerSetStartWindow(int window) => window switch
    {
        >= 2 and <= 4 => 2,
        >= 5 and <= 7 => 5,
        _ => -1
    };

    private int DirectionStep(LineBaitDirection direction)
    {
        var orderedClockwise = C.BlackHoleSourceOrder == BlackHoleSourceOrder.ClockwiseFromNorth;
        return (direction == LineBaitDirection.Clockwise) == orderedClockwise ? 1 : -1;
    }

    private List<int> OrderedActiveBuckets()
    {
        return OrderedBuckets().Where(_tetherTargets.ContainsKey).ToList();
    }

    private List<int> OrderedBuckets()
    {
        var anchor = OrderAnchorAngle();
        return Enumerable.Range(0, 4)
            .OrderBy(bucket => OrderedAngleDistance(SourceAngle(bucket), anchor))
            .ThenBy(bucket => bucket)
            .ToList();
    }

    private void SetInstruction(string text, GuidanceKind kind)
    {
        _guideDestination = null;
        _guideActionId = 0;
        _guideText = "";
        _guideInstruction = text;
        _guideKind = kind;
        _guideDebug = kind.ToString();
    }

    private void SetGuide(Vector3 destination, string text, GuidanceKind kind, uint actionId, float rotation, float offset)
    {
        _guideDestination = destination;
        _guideActionId = actionId;
        _guideText = text;
        _guideInstruction = "";
        _guideKind = kind;
        _guideDebug = $"action={actionId} rot={Deg(rotation):F1} off={Deg(offset):F1} ref=({destination.X:F2},{destination.Z:F2})";
    }

    private void ClearGuide()
    {
        _guideDestination = null;
        _guideActionId = 0;
        _guideText = "";
        _guideInstruction = "";
        _guideDebug = "";
        if (_guideKind != GuidanceKind.None)
            _guideKind = GuidanceKind.None;
    }

    private bool TryGetCastRotation(uint source, out float rotation)
    {
        if (source.GetObject() is { } obj)
        {
            rotation = obj.Rotation;
            return true;
        }

        rotation = 0.0f;
        return false;
    }

    private bool TryFinalStackRole(uint actorId, out FinalStackRole role)
    {
        if (actorId.GetObject() is IPlayerCharacter pc)
        {
            role = StackRoleFromCombat(pc.GetRole());
            return role != FinalStackRole.Unknown;
        }

        if (TryGetConfiguredRole(actorId, out var position))
        {
            role = StackRoleFromRolePosition(position);
            return role != FinalStackRole.Unknown;
        }

        role = FinalStackRole.Unknown;
        return false;
    }

    private FinalStackRole OwnFinalStackRole()
    {
        if (TryGetOwnRolePosition(out var role))
            return StackRoleFromRolePosition(role);
        return BasePlayer == null ? FinalStackRole.Unknown : StackRoleFromCombat(BasePlayer.GetRole());
    }

    private bool TryGetOwnRolePosition(out RolePosition role)
    {
        if (BasePlayer != null && TryGetConfiguredRole(BasePlayer.EntityId, out role))
            return true;

        role = RolePosition.Not_Selected;
        return false;
    }

    private bool TryGetConfiguredRole(uint actorId, out RolePosition role)
    {
        var list = C.PriorityData.GetFirstValidList();
        if (list != null)
            foreach (var entry in list.List)
                if (entry.IsInParty(list.IsRole, out var member) && member.IGameObject.EntityId == actorId &&
                    entry.Role != RolePosition.Not_Selected)
                {
                    role = entry.Role;
                    return true;
                }

        role = RolePosition.Not_Selected;
        return false;
    }

    private static FinalStackRole StackRoleFromCombat(CombatRole role) =>
        role == CombatRole.DPS ? FinalStackRole.Dps :
        role is CombatRole.Tank or CombatRole.Healer ? FinalStackRole.Support : FinalStackRole.Unknown;

    private static FinalStackRole StackRoleFromRolePosition(RolePosition role) => role switch
    {
        RolePosition.T1 or RolePosition.T2 or RolePosition.H1 or RolePosition.H2 => FinalStackRole.Support,
        RolePosition.M1 or RolePosition.M2 or RolePosition.R1 or RolePosition.R2 => FinalStackRole.Dps,
        _ => FinalStackRole.Unknown
    };

    private static FinalStackRole Opposite(FinalStackRole role) => role switch
    {
        FinalStackRole.Support => FinalStackRole.Dps,
        FinalStackRole.Dps => FinalStackRole.Support,
        _ => FinalStackRole.Unknown
    };

    private Vector3 FinalSpreadPosition(RolePosition role) =>
        PositionFromDirectionAngle(NormalizeAngle(FinalPairAnchorAngle() + FinalSpreadOffset(role)), FinalPairRadius);

    private Vector3 FinalTowerPosition(RolePosition role)
    {
        var angle = NormalizeAngle(FinalPairAnchorAngle() + (IsLeftFinalTowerRole(role) ? -MathF.PI / 2.0f : MathF.PI / 2.0f));
        return _finalTowerPositions.Count == 0
            ? PositionFromDirectionAngle(angle, FinalTowerRadius)
            : _finalTowerPositions.OrderBy(position => AngleDistance(DirectionAngle(position), angle)).First();
    }

    private float FinalPairAnchorAngle() => OrderAnchorAngle();

    private string FinalPairAnchorDebugText() => $"blackhole-order {OrderAnchorDebugText()}";

    private string FinalTowerDebugText() => _finalTowerPositions.Count == 0
        ? "none"
        : string.Join(" ", _finalTowerPositions.Select(position => $"{PositionText(position)}@{Deg(DirectionAngle(position)):F0}"));

    private static float FinalSpreadOffset(RolePosition role) => role switch
    {
        RolePosition.T1 or RolePosition.H1 => -MathF.PI / 4.0f,
        RolePosition.T2 or RolePosition.H2 => MathF.PI / 4.0f,
        RolePosition.M1 or RolePosition.R1 => -3.0f * MathF.PI / 4.0f,
        RolePosition.M2 or RolePosition.R2 => 3.0f * MathF.PI / 4.0f,
        _ => 0.0f
    };

    private static bool IsLeftFinalTowerRole(RolePosition role) =>
        role is RolePosition.T1 or RolePosition.H1 or RolePosition.M1 or RolePosition.R1;

    private static string RolePairName(RolePosition role) => role switch
    {
        RolePosition.T1 or RolePosition.H1 => "MT/H1",
        RolePosition.T2 or RolePosition.H2 => "ST/H2",
        RolePosition.M1 or RolePosition.R1 => "D1/D3",
        RolePosition.M2 or RolePosition.R2 => "D2/D4",
        _ => "?"
    };

    private static Vector3 RadialFromFacing(float rotation, float offset, float radius)
    {
        var angle = rotation + offset;
        return Center + new Vector3(MathF.Cos(angle) * radius, 0.0f, MathF.Sin(angle) * radius);
    }

    private static Vector3 PositionFromDirectionAngle(float angle, float radius) =>
        Center + new Vector3(MathF.Sin(angle) * radius, 0.0f, -MathF.Cos(angle) * radius);

    private static float Deg(float radians) => radians * 180.0f / MathF.PI;

    private static string SignedDeg(float radians)
    {
        var deg = Deg(radians);
        return deg switch
        {
            > 0.0f => $"+{deg:F1}",
            < 0.0f => $"{deg:F1}",
            _ => "+0.0"
        };
    }

    private static Vector4 WithDefaultAlpha(Vector4 color) => color with { W = DefaultColorAlpha };

    private uint NavigationColor() => GradientColor.Get(
        C.RainbowNavigationColor1.ToVector4(),
        C.RainbowNavigationColor2.ToVector4()).ToUint();

    private static long ToRandomDelayMs(float minSeconds, float maxSeconds)
    {
        minSeconds = Math.Max(0.0f, minSeconds);
        maxSeconds = Math.Max(0.0f, maxSeconds);
        if (maxSeconds < minSeconds)
            (minSeconds, maxSeconds) = (maxSeconds, minSeconds);

        var seconds = minSeconds + (float)Random.Shared.NextDouble() * (maxSeconds - minSeconds);
        return (long)MathF.Round(seconds * 1000.0f);
    }

    private static TargetGroup? GroupFromStatus(uint statusId) => statusId switch
    {
        FirstTarget => TargetGroup.Attack,
        SecondTarget => TargetGroup.Bind,
        ThirdTarget => TargetGroup.Stop,
        _ => null
    };

    private static Slot SlotFromRank(TargetGroup group, int rank) => (group, rank) switch
    {
        (TargetGroup.Attack, 0) => Slot.Attack1,
        (TargetGroup.Attack, 1) => Slot.Attack2,
        (TargetGroup.Attack, 2) => Slot.Attack3,
        (TargetGroup.Bind, 0) => Slot.Bind1,
        (TargetGroup.Bind, 1) => Slot.Bind2,
        (TargetGroup.Bind, 2) => Slot.Bind3,
        (TargetGroup.Stop, 0) => Slot.Stop1,
        (TargetGroup.Stop, 1) => Slot.Stop2,
        _ => Slot.None
    };

    private static int RankFromSlot(Slot slot) => slot switch
    {
        Slot.Attack1 or Slot.Bind1 or Slot.Stop1 => 0,
        Slot.Attack2 or Slot.Bind2 or Slot.Stop2 => 1,
        Slot.Attack3 or Slot.Bind3 => 2,
        _ => -1
    };

    private static string SlotName(Slot slot) => slot switch
    {
        Slot.Attack1 => "First1",
        Slot.Attack2 => "First2",
        Slot.Attack3 => "First3",
        Slot.Bind1 => "Second1",
        Slot.Bind2 => "Second2",
        Slot.Bind3 => "Second3",
        Slot.Stop1 => "Third1",
        Slot.Stop2 => "Third2",
        _ => "?"
    };

    private static string DirectionName(int bucket) => bucket switch
    {
        0 => "N",
        1 => "E",
        2 => "S",
        3 => "W",
        _ => "?"
    };

    private static string Format(InternationalString text, params object[] args)
    {
        try { return string.Format(text.Get(), args); }
        catch { return text.Get(); }
    }

    private static string TextOrEmpty(bool show, InternationalString text, params object[] args)
    {
        if (!show) return "";
        return args.Length == 0 ? text.Get() : Format(text, args);
    }

    private string ActiveBucketText()
    {
        return string.Join(", ", _tetherTargets
            .OrderBy(x => x.Key)
            .Select(x => $"{DirectionName(x.Key)}->{Describe(x.Value)}"));
    }

    private string BlackHoleActorDebugText()
    {
        var entries = new List<string>();
        foreach (var obj in Svc.Objects)
        {
            if (obj is not ICharacter character || obj.DataId != BlackHoleDataId)
                continue;
            var hasBucket = TryBucket(obj.Position, out var bucket);
            entries.Add($"{DirectionName(hasBucket ? bucket : -1)}:{obj.EntityId:X8}@({obj.Position.X:F1},{obj.Position.Z:F1}) in={hasBucket} tethers=[{DescribeTethers(character)}]");
        }
        return entries.Count == 0 ? "none" : string.Join(" | ", entries);
    }

    private string TetherHolderDebugText()
    {
        var entries = new List<string>();
        foreach (var obj in Svc.Objects)
        {
            if (obj is not ICharacter character)
                continue;
            var tethers = DescribeTethers(character);
            if (tethers == "none")
                continue;
            entries.Add($"{obj.Name}(0x{obj.EntityId:X8}) data={obj.DataId} tethers=[{tethers}]");
        }
        return entries.Count == 0 ? "none" : string.Join(" | ", entries.Take(16));
    }

    private string OrderedBucketText()
    {
        return string.Join(", ", OrderedBuckets()
            .Select((bucket, index) => $"{(MapMarker)index}={DirectionName(bucket)}"));
    }

    private static string Describe(uint actorId)
    {
        if (actorId == 0)
            return "none";
        if (actorId.GetObject() is { } obj)
            return $"{obj.Name}(0x{actorId:X8})@({obj.Position.X:F1},{obj.Position.Z:F1})";
        return $"0x{actorId:X8}";
    }

    private static bool IsBlackHoleObject(IGameObject obj) =>
        obj.DataId == BlackHoleDataId && TryBucket(obj.Position, out _);

    private static bool DrawCombo(string label, ref int selected, string[] items, float width)
    {
        ImGui.SetNextItemWidth(width);
        return ImGui.Combo(label, ref selected, items, items.Length);
    }

    private static int AssignmentModeIndex(AssignmentMode mode)
    {
        var index = Array.IndexOf(AssignmentModeValues, mode);
        return index < 0 ? 0 : index;
    }

    private static AssignmentMode NormalizeAssignmentMode(AssignmentMode mode) =>
        AssignmentModeValues.Contains(mode) ? mode : AssignmentMode.PartyMarker;

    private static void DrawFloat(string label, ref float value)
    {
        ImGui.SetNextItemWidth(120f);
        ImGui.InputFloat(label, ref value, 0.05f, 0.5f, "%.2f");
    }

    private static void DrawSubsection(string label)
    {
        ImGui.Spacing();
        ImGui.TextUnformatted(label);
    }

    private static void DrawCommand(string label, ref string command)
    {
        command ??= "";
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputText(label, ref command, 160);
    }

    private static void DrawText(string label, InternationalString text, ref bool show)
    {
        ImGui.PushID(label);
        ImGui.Checkbox("Show", ref show);
        ImGui.SameLine();
        ImGui.TextUnformatted(label);
        ImGui.SetNextItemWidth(-1f);
        var value = text.Get();
        text.ImGuiEdit(ref value);
        ImGui.Spacing();
        ImGui.PopID();
    }

    private static void DrawColor(string label, ref uint color)
    {
        var value = color.ToVector4();
        ImGui.SetNextItemWidth(220f);
        if (ImGui.ColorEdit4(label, ref value, ImGuiColorEditFlags.NoInputs))
            color = value.ToUint();
    }

    private static PriorityData CreatePriorityData(string name, string description, IReadOnlyList<RolePosition> roles) => new()
    {
        Name = name,
        Description = description,
        PriorityLists =
        [
            new PriorityList
            {
                IsRole = true,
                List = roles.Select(role => new JobbedPlayer { Role = role }).ToList()
            }
        ]
    };

    private void DrawMarkerLineOrders()
    {
        ImGui.TextUnformatted("Black Hole line order for each marker:");
        ImGui.TextWrapped(MarkerLineOrderDescription.Get());
        for (var i = 0; i < SelectableMarkerIds.Length; i++)
        {
            var selected = Math.Clamp(C.MarkerLineOrders[i], 0, BlackHoleOrderNames.Length - 1);
            ImGui.SetNextItemWidth(160f);
            if (ImGui.Combo($"{SelectableMarkerNames[i]} line order", ref selected, BlackHoleOrderNames, BlackHoleOrderNames.Length))
                C.MarkerLineOrders[i] = selected;
        }
    }

    private readonly record struct BlackHoleTask(int Bucket, Vector3 Source, uint Target, Vector3 StandPosition);

    private enum State { Idle, CollectingAssignments, BlackHoleActive, FinalSequence, Completed }
    public enum AssignmentMode
    {
        PartyMarker = 0,
        Priority = 1,
        MarkerThenPriority = 2,
        RoleAccretion = 3,
        FixedRoleAccretion = 4,
        FixedMarkerLanes = 5
    }
    private enum AssignmentQuality { Unknown, Marker, Priority, RoleAccretion }
    private enum GuidanceKind { None, FinalCenter, FinalSpread, FinalLanding, FinalMove }
    private enum FinalStage { None, AwaitingBlizzaga, CenterBait, RoleSpread, Landing1, Landing2, ProtrudeMove }
    private enum FinalStackRole { Unknown, Support, Dps }
    private enum TargetGroup { None, Attack, Bind, Stop }
    private enum Slot { None, Attack1, Attack2, Attack3, Bind1, Bind2, Bind3, Stop1, Stop2 }
    public enum LineBaitDirection { Clockwise, Counterclockwise }
    public enum FirstWindowBaitDirection { SameAsLineBaitDirection = 0, Clockwise = 1, Counterclockwise = 2 }
    public enum FirstPairAssignment { SourceOrder = 0, FirstSlotNearest = 1 }
    public enum BlackHoleSourceOrder { ClockwiseFromNorth = 0, CounterclockwiseFromNorth = 1 }
    public enum BlackHoleOrderAnchor { KefkaPosition = 0, ArenaNorth = 1 }
    public enum FirstOrbRole { Dps = 0, Support = 1 }
    public enum FinalInitialBaitMode { Center = 0, KefkaRelativeRoleSplit = 1 }
    public enum FinalInitialNorthRole { Support = 0, Dps = 1 }
    public enum MapMarker { A = 0, B = 1, C = 2, D = 3 }
    public enum MarkerCommandSource { TargetDebuff = 0, AccretionDebuff = 1 }

    public sealed class Config
    {
        public AssignmentMode AssignmentMode = AssignmentMode.PartyMarker;
        public FirstOrbRole FirstOrbRole = FirstOrbRole.Dps;
        public LineBaitDirection LineBaitDirection = LineBaitDirection.Clockwise;
        public FirstWindowBaitDirection FirstWindowBaitDirection = FirstWindowBaitDirection.SameAsLineBaitDirection;
        public FirstPairAssignment FirstPairAssignment = FirstPairAssignment.SourceOrder;
        public BlackHoleSourceOrder BlackHoleSourceOrder = BlackHoleSourceOrder.ClockwiseFromNorth;
        public BlackHoleOrderAnchor BlackHoleOrderAnchor = BlackHoleOrderAnchor.KefkaPosition;
        public FinalInitialBaitMode FinalInitialBaitMode = FinalInitialBaitMode.Center;
        public FinalInitialNorthRole FinalInitialNorthRole = FinalInitialNorthRole.Support;
        public bool BlackHoleTetherOnly;
        public bool ShowPostBlackHoleNavigation = true;
        public uint RainbowNavigationColor1 = WithDefaultAlpha(EColor.CyanBright).ToUint();
        public uint RainbowNavigationColor2 = WithDefaultAlpha(EColor.VioletBright).ToUint();
        public uint CorrectTetherColor = WithDefaultAlpha(EColor.GreenBright).ToUint();
        public uint WrongTetherColor = WithDefaultAlpha(EColor.RedBright).ToUint();
        public uint UnknownTetherColor = WithDefaultAlpha(EColor.YellowBright).ToUint();
        public int[] MarkerLineOrders = [0, 1, 2, 0, 1, 2, 0, 1];
        public bool ExecuteMarkerCommand;
        public MarkerCommandSource MarkerCommandSource = MarkerCommandSource.TargetDebuff;
        public bool SkipTargetMarkerOnAccretion;
        public float MarkerDelayMinSeconds = 0.1f;
        public float MarkerDelayMaxSeconds = 0.8f;
        public string FirstTargetCommand = "/mk attack <me>";
        public string SecondTargetCommand = "/mk bind <me>";
        public string ThirdTargetCommand = "/mk stop <me>";
        public string AccretionCommand = "/mk bind <me>";
        public PriorityData PriorityData = CreatePriorityData("P3 Earthquake priority",
            "Used when assignment mode is Priority.", DefaultRolePriority);

        public InternationalString FirstLineWindowText = new() { En = "W{0}: take line at W{1}", Jp = "W{0}: W{1}で線取り" };
        public bool ShowFirstLineWindowText = true;
        public InternationalString NextLineWindowText = new() { En = "W{0}: take next line", Jp = "W{0}: 次線を取る" };
        public bool ShowNextLineWindowText = true;
        public InternationalString TakeLineNowText = new() { En = "W{0}: take line", Jp = "W{0}: 線を取れ" };
        public bool ShowTakeLineNowText = true;
        public InternationalString UnknownSlotText = new() { En = "Earthquake slot unknown", Jp = "地震スロット未確定" };
        public bool ShowUnknownSlotText = true;
        public InternationalString OverlayText = new() { En = "{0}", Jp = "{0}" };
        public bool ShowOverlayText = true;
        public InternationalString FinalCenterText = new() { En = "Center bait", Jp = "中央で誘導" };
        public bool ShowFinalCenterText = true;
        public InternationalString FinalRoleSplitText = new() { En = "{0}: bait", Jp = "{0}: 誘導" };
        public bool ShowFinalRoleSplitText = true;
        public InternationalString FinalSpreadText = new() { En = "{0}: spread", Jp = "{0}: 散開" };
        public bool ShowFinalSpreadText = true;
        public InternationalString FinalStackText = new() { En = "Stack center", Jp = "中央で頭割り" };
        public bool ShowFinalStackText = true;
        public InternationalString FinalTowerText = new() { En = "{0}: tower", Jp = "{0}: 塔" };
        public bool ShowFinalTowerText = true;
        public InternationalString FinalMoveText = new() { En = "{0}: spread and keep moving", Jp = "{0}: 散開して動く" };
        public bool ShowFinalMoveText = true;
        public MapMarker DpsMarker = MapMarker.B;
        public MapMarker SupportMarker = MapMarker.A;
        public MapMarker AccretionMarker = MapMarker.C;
        public MapMarker FallbackMarker = MapMarker.D;
        public MapMarker LaneDpsMarker = MapMarker.A;
        public MapMarker LaneSupportMarker = MapMarker.D;
        public MapMarker LaneAccretionMarker = MapMarker.B;
        public MapMarker LaneFlexMarker = MapMarker.C;
        public LineBaitDirection DpsLineBaitDirection = LineBaitDirection.Clockwise;
        public LineBaitDirection SupportLineBaitDirection = LineBaitDirection.Counterclockwise;
        public LineBaitDirection AccretionLineBaitDirection = LineBaitDirection.Clockwise;

        public void EnsureDefaults()
        {
            AssignmentMode = NormalizeAssignmentMode(AssignmentMode);
            FirstOrbRole = (FirstOrbRole)Math.Clamp((int)FirstOrbRole, 0, FirstOrbRoleNames.Length - 1);
            LineBaitDirection = (LineBaitDirection)Math.Clamp((int)LineBaitDirection, 0, 1);
            FirstWindowBaitDirection = (FirstWindowBaitDirection)Math.Clamp((int)FirstWindowBaitDirection, 0, FirstWindowBaitDirectionNames.Length - 1);
            FirstPairAssignment = (FirstPairAssignment)Math.Clamp((int)FirstPairAssignment, 0, FirstPairAssignmentNames.Length - 1);
            DpsLineBaitDirection = ClampLineBaitDirection(DpsLineBaitDirection);
            SupportLineBaitDirection = ClampLineBaitDirection(SupportLineBaitDirection);
            AccretionLineBaitDirection = ClampLineBaitDirection(AccretionLineBaitDirection);
            BlackHoleSourceOrder = (BlackHoleSourceOrder)Math.Clamp((int)BlackHoleSourceOrder, 0, 1);
            BlackHoleOrderAnchor = (BlackHoleOrderAnchor)Math.Clamp((int)BlackHoleOrderAnchor, 0, 1);
            FinalInitialBaitMode = (FinalInitialBaitMode)Math.Clamp((int)FinalInitialBaitMode, 0, FinalInitialBaitModeNames.Length - 1);
            FinalInitialNorthRole = (FinalInitialNorthRole)Math.Clamp((int)FinalInitialNorthRole, 0, FinalNorthRoleNames.Length - 1);
            MarkerCommandSource = (MarkerCommandSource)Math.Clamp((int)MarkerCommandSource, 0, MarkerCommandSourceNames.Length - 1);
            DpsMarker = ClampMarker(DpsMarker);
            SupportMarker = ClampMarker(SupportMarker);
            AccretionMarker = ClampMarker(AccretionMarker);
            FallbackMarker = ClampMarker(FallbackMarker);
            LaneDpsMarker = ClampMarker(LaneDpsMarker);
            LaneSupportMarker = ClampMarker(LaneSupportMarker);
            LaneAccretionMarker = ClampMarker(LaneAccretionMarker);
            LaneFlexMarker = ClampMarker(LaneFlexMarker);
            PriorityData ??= CreatePriorityData("P3 Earthquake priority",
                "Used when assignment mode is Priority.", DefaultRolePriority);
            if (MarkerLineOrders == null || MarkerLineOrders.Length != SelectableMarkerIds.Length)
                MarkerLineOrders = DefaultMarkerLineOrders.ToArray();
            for (var i = 0; i < MarkerLineOrders.Length; i++)
                MarkerLineOrders[i] = Math.Clamp(MarkerLineOrders[i], 0, BlackHoleOrderNames.Length - 1);
            MarkerDelayMinSeconds = Math.Max(0.0f, MarkerDelayMinSeconds);
            MarkerDelayMaxSeconds = Math.Max(0.0f, MarkerDelayMaxSeconds);
            FirstTargetCommand ??= "/mk attack <me>";
            SecondTargetCommand ??= "/mk bind <me>";
            ThirdTargetCommand ??= "/mk stop <me>";
            AccretionCommand ??= "/mk bind <me>";
            FirstLineWindowText ??= new InternationalString { En = "W{0}: take line at W{1}", Jp = "W{0}: W{1}で線取り" };
            NextLineWindowText ??= new InternationalString { En = "W{0}: take next line", Jp = "W{0}: 次線を取る" };
            TakeLineNowText ??= new InternationalString { En = "W{0}: take line", Jp = "W{0}: 線を取れ" };
            UnknownSlotText ??= new InternationalString { En = "Earthquake slot unknown", Jp = "地震スロット未確定" };
            OverlayText ??= new InternationalString { En = "{0}", Jp = "{0}" };
            FinalCenterText ??= new InternationalString { En = "Center bait", Jp = "中央で誘導" };
            FinalRoleSplitText ??= new InternationalString { En = "{0}: bait", Jp = "{0}: 誘導" };
            FinalSpreadText ??= new InternationalString { En = "{0}: spread", Jp = "{0}: 散開" };
            FinalStackText ??= new InternationalString { En = "Stack center", Jp = "中央で頭割り" };
            FinalTowerText ??= new InternationalString { En = "{0}: tower", Jp = "{0}: 塔" };
            FinalMoveText ??= new InternationalString { En = "{0}: spread and keep moving", Jp = "{0}: 散開して動く" };
        }

        private static MapMarker ClampMarker(MapMarker marker) =>
            (MapMarker)Math.Clamp((int)marker, 0, MapMarkerNames.Length - 1);

        private static LineBaitDirection ClampLineBaitDirection(LineBaitDirection direction) =>
            (LineBaitDirection)Math.Clamp((int)direction, 0, LineBaitDirectionNames.Length - 1);
    }
}
