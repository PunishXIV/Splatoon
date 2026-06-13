using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
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
    private const uint KefkaNameId = 7131;
    private const uint ChaosDataId = 19508;
    private const uint DecisiveBattleChaos = 49890;
    private const uint DecisiveBattleExdeath = 49891;
    private const uint BlackHoleDataId = 19512;
    private const uint KefkaDashStart = 47843;
    private const uint KefkaDashEnd = 47844;
    private const uint BlackHoleCast = 47867;
    private const uint BlackHoleHit = 47868;
    private const uint BintaStackCast = 47846;
    private const uint BintaSpreadCast = 47847;
    private const uint BintaStackHit = 47850;
    private const uint BintaSpreadHit = 47851;
    private const uint DubbingEdict = 47873;
    private const uint AsIsFirst = 47852;
    private const uint AsIsSecond = 47853;
    private const uint AsIsHit = 47854;
    private const uint WhiteHole = 48486;
    private const uint LongitudinalImplosion = 47869;
    private const uint LatitudinalImplosion = 47870;
    private const uint ImplosionHit = 47871;
    private const uint UltimateEmbrace = 49740;
    private const uint BowelsOfAgony = 47858;
    private const uint LateP3Blizzaga = 47887;
    private const uint DondokoCast = 47855;
    private const uint DondokoHit = 47856;
    private const uint LandingCast = 47874;
    private const uint LandingHit = 47875;
    private const uint TowerImpact = 47857;
    private const uint EnhancedBlizzaga = 47889;
    private const uint Protrude = 47877;
    private const uint TargetIconCommand = 34;
    private const uint TetherCreateCommand = 35;
    private const uint TetherRemoveCommand = 47;
    private const uint FinalStackMarker = 161;
    private const uint BlackHoleTetherData3 = 84;
    private const uint BlackHoleTetherData5 = 15;
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
    private const float BaitOffset = 4.5f;
    private const float StackRadius = 10.8f;
    private const float SpreadRadius = 10.8f;
    private const float SafeRadius = 12.0f;
    private const float FinalPairRadius = 9.8f;
    private const string DestinationElement = "Destination";
    private const string InstructionElement = "Instruction";
    private const string TetherLineElement = "BlackHoleToSelf";
    private const uint CorrectLineColor = 0xC800FF00;
    private const uint WrongLineColor = 0xC8FF2020;
    private const uint UnknownLineColor = 0xC8FFD000;

    private static readonly Vector3 Center = new(100f, 0f, 100f);
    private static readonly int[] ExpectedSourcesByWindow = [1, 2, 3, 3, 3, 3, 3, 3, 2, 1];
    private static readonly int[] SelectableMarkerIds = [0, 1, 2, 5, 6, 7, 8, 9];
    private static readonly string[] SelectableMarkerNames = ["Attack1", "Attack2", "Attack3", "Bind1", "Bind2", "Bind3", "Stop1", "Stop2"];
    private static readonly int[] DefaultMarkerLineOrders = [0, 1, 2, 0, 1, 2, 0, 1];
    private static readonly string[] BlackHoleOrderNames = ["1st", "2nd", "3rd"];
    private static readonly string[] AssignmentModeNames =
        ["Party marker", "Priority", "Marker + priority fallback", "PF role/accretion", "Fixed role/accretion spots"];
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
        En = "P3 Earthquake helper. It resolves your First/Second/Third line order from the debuff plus party markers or priority, then follows live Black Hole tether changes. When the line is on you, it shows the Black Hole-to-player line and your bait position. The line is green when the current active Black Hole order matches your slot, red when it does not.",
        Jp = "P3地震用です。デバフとマーカーまたは優先順位から自分の第一/第二/第三対象内の線取り順を決め、黒穴テザーの付け替わりを追ってナビします。自分に線が付いた時は、黒穴から自分への線と誘導先を表示します。現在線が出ている黒穴の並びと自分のスロットが一致する場合は緑、一致しない場合は赤になります。"
    };
    private static readonly InternationalString AssignmentModeDescription = new()
    {
        En = "Party marker: uses only the marker line-order table below. Priority: ignores markers and orders players with the priority list inside each First/Second/Third group. Marker + priority fallback: uses markers first; if no valid marker result is available after Black Hole starts and all groups are known, it falls back to priority. PF role/accretion: resolves order inside your group as DPS first, support second, Accretion third. Fixed role/accretion spots: support=A, DPS=B, Accretion=C; if that preferred spot has no active Black Hole, it uses D.",
        Jp = "Party marker: 下のマーカー別線取り順だけで判定します。Priority: マーカーを無視し、第一/第二/第三対象ごとに優先順位で並べます。Marker + priority fallback: まずマーカーで判定し、黒穴開始後も有効なマーカー判定ができず、全員のグループが揃っている場合だけ優先順位へフォールバックします。PF role/accretion: 自分のグループ内でDPSを1番目、タンク/ヒラを2番目、Accretion持ちを3番目として判定します。Fixed role/accretion spots: タンク/ヒラ=A、DPS=B、Accretion=C として扱い、担当spotに黒穴が無い場合はDを使います。"
    };
    private static readonly InternationalString LineBaitDirectionDescription = new()
    {
        En = "Line bait direction controls where your bait marker is placed from the Black Hole that is currently tethered to you. Clockwise and Counterclockwise are relative to the arena center; only your bait offset changes, not the Black Hole detection order.",
        Jp = "Line bait direction は、自分に付いた黒穴を基準に誘導先を時計回り/反時計回りのどちらへずらすかを決めます。方向はフィールド中央基準です。黒穴の検出順は変わらず、自分の線を引っ張る位置だけが変わります。"
    };
    private static readonly InternationalString BlackHoleSourceOrderDescription = new()
    {
        En = "Black Hole source order sorts only the Black Holes that currently have active tethers. The anchor decides where 1st starts; the order decides clockwise or counterclockwise from that anchor.",
        Jp = "Black Hole source order は、現在線が出ている黒穴だけを並べ替える設定です。anchor で 1番目を数え始める基準を決め、order でそこから時計回り/反時計回りのどちらに数えるかを決めます。"
    };
    private static readonly InternationalString MarkerLineOrderDescription = new()
    {
        En = "Set which line order each party marker means. The debuff decides the group: First Target, Second Target, or Third Target. The marker decides the order inside that group. Example: Attack1 = 1st means First Target + Attack1 becomes First1, while Second Target + Attack1 becomes Second1. Third Target has only two players, so do not assign 3rd to markers used by Third Target players.",
        Jp = "各マーカーが何番目の線取りを意味するかを設定します。第一/第二/第三対象のどのグループかはデバフで決まり、グループ内の何番目かをマーカーで決めます。例: Attack1 = 1st の場合、第一対象+Attack1 は First1、第二対象+Attack1 は Second1 になります。第三対象は2人だけなので、第三対象に使うマーカーへ 3rd は割り当てないでください。"
    };
    private static readonly InternationalString MarkerCommandDescription = new()
    {
        En = "Optional self-marker commands. First/Second/Third command means the debuff group, not the line order. When enabled, the script queues the matching command once when you receive the debuff, waits a random delay between min and max seconds, then executes it. These commands are not executed during replay playback.",
        Jp = "任意の自分用マーカーコマンドです。First/Second/Third command は線取り順ではなく、第一/第二/第三対象のデバフグループに対応します。有効にすると、デバフが付いた時に対応するコマンドを1回だけ予約し、minからmax秒のランダムディレイ後に実行します。リプレイ再生中は実行しません。"
    };
    private static readonly InternationalString DisplayTextDescription = new()
    {
        En = "These fields change only the text shown on Splatoon overlays. Turning a text off hides that text only; it does not disable the marker, tether line, assignment logic, marker commands, or Black Hole detection.",
        Jp = "ここはSplatoon上に表示する文言だけを変更します。チェックをOFFにすると文字だけ非表示になります。マーカー、線、割り当てロジック、マーカーコマンド、黒穴検出は無効になりません。"
    };
    private static readonly InternationalString FinalRolePositionDescription = new()
    {
        En = "The late fixed spread/tower guide uses these role positions even when Earthquake line assignment mode is Party marker.",
        Jp = "終盤の固定散開/塔ナビは、地震線取りの Assignment mode が Party marker の場合でもこのロール設定を使用します。"
    };

    private readonly Dictionary<uint, TargetGroup> _groups = [];
    private readonly Dictionary<uint, uint> _activeTethers = [];
    private readonly Dictionary<uint, uint> _blackHoleByEndpoint = [];
    private readonly Dictionary<uint, int> _tetherBucketsBySource = [];
    private readonly Dictionary<int, uint> _tetherTargets = [];
    private readonly Dictionary<int, Vector3> _tetherSources = [];
    private readonly Dictionary<uint, (uint Source, uint Data2, uint Data3, uint Data5)> _pendingTetherRemovals = [];
    private readonly List<uint> _liveBlackHoleIds = [];
    private readonly Dictionary<uint, LiveTetherEntry> _liveBlackHoleTethers = [];
    private readonly HashSet<int> _hitSources = [];
    private readonly HashSet<uint> _earthPlayers = [];
    private readonly HashSet<uint> _accretionPlayers = [];

    private State _state;
    private uint _selfPlayerId;
    private Slot _selfSlot;
    private AssignmentQuality _quality;
    private Vector3? _selfDestination;
    private Vector3? _selfTetherSource;
    private Vector3? _guideDestination;
    private Vector3? _dubbingDestination;
    private (Vector3 Destination, string Text, GuidanceKind Kind, uint ActionId, float Rotation, float Offset)? _pendingPostDubbingGuide;
    private GuidanceKind _guideKind;
    private uint _guideActionId;
    private string _guideText = "";
    private string _guideInstruction = "";
    private string _guideDebug = "";
    private string _kefkaAnchorDebug = "";
    private string _pendingMarkerCommand = "";
    private long _markerCommandAtMs;
    private uint _kefkaId;
    private Vector3? _kefkaPosition;
    private uint _selfTetherTarget;
    private uint _lastMissingLineTarget;
    private int _currentWindow = -1;
    private int _selfTetherBucket = -1;
    private int _earthMaxCount;
    private FinalStage _finalStage;
    private FinalStackRole _firstFinalStackRole;
    private FinalStackRole _currentFinalStackRole;
    private int _landingCount;
    private bool _sentMarkerCommand;
    private string _instruction = "";

    public override HashSet<uint>? ValidTerritories { get; } = [TerritoryDancingMadUltimate];
    public override Metadata Metadata => new(34, "Garume");

    public override void OnSetup()
    {
        C.EnsureDefaults();
        Controller.RegisterElement(DestinationElement, new Element(0)
        {
            Enabled = false,
            radius = 1.25f,
            thicc = 5.0f,
            fillIntensity = 0.25f,
            color = 0xC800FFFF,
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
        Controller.RegisterElement(TetherLineElement, new Element(2)
        {
            Enabled = false,
            radius = 0,
            thicc = 5.0f,
            color = 0xC800FFFF
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
        if (ShouldSkipAttachedInfoCast(origin, castId)) return;
        UpdateKefkaAnchor(source, castId, packetRotation);
        LogKefkaCastProbe(origin, source, castId, packet);
        LogKefkaCandidateEvent("CAST", source, castId);
        LogFinalCast(origin, source, castId, packetRotation);

        if (castId is UltimateEmbrace or BowelsOfAgony)
            ResetAll();
        else if (castId is DecisiveBattleChaos or DecisiveBattleExdeath)
            StartCollection();
        else if (castId == BlackHoleCast)
            StartBlackHole();
        else if (!HandleMiddleAoeCast(source, castId, packetRotation))
            HandleFinalCast(castId);
    }

    private bool HandleMiddleAoeCast(uint source, uint castId, float? packetRotation)
    {
        if (castId is BintaStackCast or BintaSpreadCast)
        {
            StartCollection();
            if (C.ShowInterBlackHoleAoeGuides)
                SetBintaGuide(source, packetRotation, castId == BintaStackCast);
            return true;
        }

        if (castId == DubbingEdict)
        {
            StartCollection();
            if (C.ShowInterBlackHoleAoeGuides)
                SetDubbingGuide(source);
            return true;
        }

        if (castId is AsIsFirst or AsIsSecond)
        {
            StartCollection();
            if (C.ShowInterBlackHoleAoeGuides)
                SetDirectionalGuide(source, castId, TextOrEmpty(C.ShowAsIsText, C.AsIsText), GuidanceKind.AsIs,
                    castId == AsIsFirst ? 0.0f : MathF.PI, SafeRadius, packetRotation, delayDuringDubbing: true);
            return true;
        }

        if (castId == WhiteHole)
        {
            StartCollection();
            SetInstruction(TextOrEmpty(C.ShowWhiteHoleText, C.WhiteHoleText), GuidanceKind.WhiteHole);
            return true;
        }

        if (castId is LongitudinalImplosion or LatitudinalImplosion)
        {
            StartCollection();
            if (C.ShowInterBlackHoleAoeGuides)
                SetImplosionGuide(source, castId, castId == LongitudinalImplosion, packetRotation);
            return true;
        }

        return false;
    }

    private bool HandleFinalCast(uint castId)
    {
        if (castId == LateP3Blizzaga)
        {
            SetFinalCenterBait();
            return true;
        }
        if (castId == DondokoCast)
        {
            SetFinalRoleSpread();
            return true;
        }
        if (castId == LandingCast)
        {
            SetKnownFinalLanding();
            return true;
        }
        if (castId is EnhancedBlizzaga or Protrude)
        {
            SetFinalMove();
            return true;
        }

        return false;
    }

    private bool ShouldSkipAttachedInfoCast(string origin, uint castId)
    {
        var skip = origin == "attached-info" && castId == DubbingEdict &&
            (_dubbingDestination.HasValue || _guideActionId == DubbingEdict);
        if (skip && C.EnableBlackHoleDebugLogs)
            DebugLog("CAST_SKIP attached-info action=47873 reason=packet-guide-already-set");
        return skip;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        RefreshBasePlayerState();

        var actionId = set.Action?.RowId ?? 0;
        LogKefkaCandidateEvent("ACTION", set.Source?.EntityId ?? 0, actionId);
        LogKefkaActionProbe(set, actionId);
        LogFinalAction(set, actionId);
        if (actionId == BlackHoleHit && TryBucket(set.Source?.Position ?? set.Position, out var bucket))
        {
            AdvanceWindow(bucket);
        }
        else if (!HandleMiddleAoeAction(actionId) && !HandleFinalAction(actionId) &&
            actionId is UltimateEmbrace or BowelsOfAgony)
        {
            Complete();
        }
    }

    private bool HandleMiddleAoeAction(uint actionId)
    {
        if (actionId == DubbingEdict)
        {
            _dubbingDestination = null;
            ClearGuide();
            if (C.ShowInterBlackHoleAoeGuides)
                RestorePendingPostDubbingGuide();
            else
                ClearPendingPostDubbingGuide();
            return true;
        }

        if (actionId is BintaStackHit or BintaSpreadHit or AsIsHit or ImplosionHit)
        {
            if (actionId is BintaStackHit or BintaSpreadHit or AsIsHit)
                ClearPendingPostDubbingGuide();
            ClearGuide();
            return true;
        }

        return false;
    }

    private bool HandleFinalAction(uint actionId)
    {
        if (actionId == DondokoCast)
        {
            SetFinalRoleSpread();
            return true;
        }
        if (actionId == LandingHit)
        {
            AdvanceFinalLanding();
            return true;
        }
        if (actionId is DondokoHit or TowerImpact)
        {
            SetKnownFinalLanding();
            return true;
        }
        if (actionId is EnhancedBlizzaga)
        {
            SetFinalMove();
            return true;
        }
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
        LogKefkaActorControlProbe(sourceId, command, p1, p2, p3, p4, p5, p6, p7, p8, targetId, replaying);
        LogFinalActorControl(sourceId, command, p1, p2, p3, p4, p5, p6, p7, p8, targetId, replaying);

        if (command == TetherCreateCommand && p2 == BlackHoleTetherData3 && p4 == BlackHoleTetherData5)
        {
            LogKefkaSnapshot($"actor-tether-create {Describe(sourceId)}->{Describe(p3)}");
            HandleBlackHoleTetherCreate(sourceId, p3, p1, p2, p4, "actor-control");
            return;
        }
        if (command == TetherRemoveCommand)
        {
            HandleBlackHoleTetherRemoval(sourceId, p1, p2, p4, "actor-control");
            return;
        }

        if (command == TargetIconCommand && p1 == FinalStackMarker &&
            _state is not (State.Idle or State.Completed) &&
            TryFinalStackRole(sourceId, out var role))
            SetFinalLanding(role);
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
                ClearSelfResolution();
            return;
        }
        if (isSelf && status.StatusId == LineDoneStatus)
            return;

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

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        RefreshBasePlayerState();

        if (IsBlackHoleTether(data3, data5))
            LogKefkaSnapshot($"tether-create {Describe(source)}->{Describe(target)}");
        HandleBlackHoleTetherCreate(source, target, data2, data3, data5, "tether");
    }

    private void HandleBlackHoleTetherCreate(uint source, uint target, uint data2, uint data3, uint data5, string origin)
    {
        if (_state != State.BlackHoleActive)
            return;
        if (!IsBlackHoleTether(data3, data5))
            return;
        if (!TryResolveBlackHoleTether(source, target, out var blackHoleId, out var tetherTarget, out var blackHolePosition, out var bucket))
        {
            DebugLog($"TETHER_CREATE rejected origin={origin} source={Describe(source)} target={Describe(target)} data={data2}/{data3}/{data5} slot={_selfSlot} expected={ExpectedBucket(_selfSlot)} active=[{ActiveBucketText()}]");
            return;
        }

        CacheBlackHoleTether(source, target, blackHoleId, tetherTarget, blackHolePosition, bucket, origin,
            $"{data2}/{data3}/{data5}");
    }

    private void CacheBlackHoleTether(uint source, uint target, uint blackHoleId, uint tetherTarget,
        Vector3 blackHolePosition, int bucket, string origin, string dataText)
    {
        if (_activeTethers.TryGetValue(blackHoleId, out var oldTarget) && oldTarget != tetherTarget &&
            _blackHoleByEndpoint.GetValueOrDefault(oldTarget) == blackHoleId)
            _blackHoleByEndpoint.Remove(oldTarget);
        _pendingTetherRemovals.Remove(blackHoleId);
        _activeTethers[blackHoleId] = tetherTarget;
        _blackHoleByEndpoint[source] = blackHoleId;
        _blackHoleByEndpoint[target] = blackHoleId;
        _tetherBucketsBySource[blackHoleId] = bucket;
        if (origin == "live-vfx" || !_tetherTargets.ContainsKey(bucket))
        {
            _tetherTargets[bucket] = tetherTarget;
            _tetherSources[bucket] = blackHolePosition;
        }
        DebugLog($"TETHER_CREATE origin={origin} bh={Describe(blackHoleId)} bucket={DirectionName(bucket)} currentTarget={Describe(tetherTarget)} displayTarget={Describe(_tetherTargets.GetValueOrDefault(bucket))} raw={Describe(source)}->{Describe(target)} data={dataText} slot={_selfSlot} expectedBefore={ExpectedBucket(_selfSlot)} active=[{ActiveBucketText()}] targetSelf={tetherTarget == BasePlayer?.EntityId}");
        RefreshExpectedTether("create");
    }

    public override void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
    {
        RefreshBasePlayerState();

        HandleBlackHoleTetherRemoval(source, data2, data3, data5, "tether");
    }

    private void HandleBlackHoleTetherRemoval(uint source, uint data2, uint data3, uint data5, string origin)
    {
        if (_state != State.BlackHoleActive)
            return;
        if (!_blackHoleByEndpoint.TryGetValue(source, out var blackHoleId))
        {
            if ((data2 == 0 && data3 == 0 && data5 == 0 || IsBlackHoleTether(data3, data5)) &&
                TryResolveBlackHoleEndpoint(source, out _, out _))
                blackHoleId = source;
            else
            {
                DebugLog($"TETHER_REMOVE ignored origin={origin} source={Describe(source)} data={data2}/{data3}/{data5} slot={_selfSlot} expected={ExpectedBucket(_selfSlot)} active=[{ActiveBucketText()}]");
                return;
            }
        }

        _pendingTetherRemovals[blackHoleId] = (source, data2, data3, data5);
        DebugLog($"TETHER_REMOVE queued origin={origin} bh={Describe(blackHoleId)} rawSource={Describe(source)} data={data2}/{data3}/{data5} slot={_selfSlot} selfBucket={DirectionName(_selfTetherBucket)} active=[{ActiveBucketText()}]");
        RefreshExpectedTether("remove-queued");
    }

    public override void OnUpdate()
    {
        RefreshBasePlayerState();
        ExecutePendingMarkerCommand();

        HideElements();
        RefreshKefkaAnchorFromObject();
        ResolveSelfSlot();
        ResolveActiveTethers();
        if (_state == State.BlackHoleActive)
            PollLiveBlackHoleTethers();
        if (BasePlayer == null || _state is State.Idle or State.Completed) return;

        ShowGuidance();
        ApplyPendingTetherRemovals();
    }

    private void ShowGuidance()
    {
        ShowSelfTetherLine();
        if (_selfDestination is { } destination)
            ShowDestination(destination, TextOrEmpty(C.ShowOverlayText, C.OverlayText, SlotName(_selfSlot)));
        else if (_guideDestination is { } guide)
            ShowDestination(guide, _guideText);
        else if (!string.IsNullOrWhiteSpace(_guideInstruction))
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
        DrawBlackHoleSettings();
        if (C.AssignmentMode is AssignmentMode.PartyMarker or AssignmentMode.MarkerThenPriority)
            DrawMarkerAssignmentSettings();
        DrawMarkerCommandSettings();
        DrawDisplayTextSettings();
        DrawDebugStatus();
    }

    private void DrawAssignmentSettings()
    {
        ImGui.TextUnformatted("Assignment");
        ImGui.Indent();
        var mode = (int)C.AssignmentMode;
        if (DrawCombo("Assignment mode", ref mode, AssignmentModeNames, 260f))
            C.AssignmentMode = (AssignmentMode)Math.Clamp(mode, 0, AssignmentModeNames.Length - 1);
        ImGui.TextWrapped(AssignmentModeDescription.Get());

        if (C.AssignmentMode is AssignmentMode.FixedRoleAccretion)
		{
            ImGui.TextUnformatted("DPS/Support/Accretion spot assignment");
            ImGui.Indent();
            var dps = (int)C.DpsMarker;
            if (DrawCombo("DPS", ref dps, ["A", "B", "C", "D"], 180f))
                C.DpsMarker = (MapMarker)dps;
            var support = (int)C.SupportMarker;
            if (DrawCombo("Support", ref support, ["A", "B", "C", "D"], 180f))
                C.SupportMarker = (MapMarker)support;
            var accretion = (int)C.AccretionMarker;
            if (DrawCombo("Accretion", ref accretion, ["A", "B", "C", "D"], 180f))
                C.AccretionMarker = (MapMarker)accretion;
            var fallback = (int)C.FallbackMarker;
            if (DrawCombo("Fallback", ref fallback, ["A", "B", "C", "D"], 180f))
                C.FallbackMarker = (MapMarker)fallback;

            if (C.DpsMarker == C.SupportMarker || C.DpsMarker == C.AccretionMarker || C.DpsMarker == C.FallbackMarker ||
                C.SupportMarker == C.AccretionMarker || C.SupportMarker == C.FallbackMarker ||
                C.AccretionMarker == C.FallbackMarker)
                ImGui.TextWrapped("Warning: DPS, Support, Accretion, and Fallback markers should be unique.");

            ImGui.Unindent();
		}

        if (C.AssignmentMode is AssignmentMode.Priority or AssignmentMode.MarkerThenPriority)
        {
            if (ImGui.Button("Apply Meow static TN priority"))
                C.PriorityData = CreatePriorityData("P3 Earthquake Meow static TN priority",
                    "M1 - M2 - R1 - R2 - MT - OT - H1 - H2.", MeowStaticTrueNorthPriority);
            C.PriorityData.Draw();
        }
        else if (ImGui.CollapsingHeader("Final role positions"))
        {
            ImGui.TextWrapped(FinalRolePositionDescription.Get());
            C.PriorityData.Draw();
        }
        ImGui.Unindent();
    }

    private void DrawBlackHoleSettings()
    {
        ImGui.Separator();
        ImGui.TextUnformatted("Black Hole");
        ImGui.Indent();
        var direction = (int)C.LineBaitDirection;
        if (DrawCombo("Line bait direction", ref direction, ["Clockwise", "Counterclockwise"], 180f))
            C.LineBaitDirection = (LineBaitDirection)Math.Clamp(direction, 0, 1);
        ImGui.TextWrapped(LineBaitDirectionDescription.Get());

        var sourceOrder = (int)C.BlackHoleSourceOrder;
        if (DrawCombo("Black Hole source order", ref sourceOrder, ["Clockwise", "Counterclockwise"], 180f))
            C.BlackHoleSourceOrder = (BlackHoleSourceOrder)Math.Clamp(sourceOrder, 0, 1);
        var anchor = (int)C.BlackHoleOrderAnchor;
        if (DrawCombo("Black Hole order anchor", ref anchor, ["Kefka position", "Arena north"], 260f))
            C.BlackHoleOrderAnchor = (BlackHoleOrderAnchor)Math.Clamp(anchor, 0, 1);
        ImGui.TextWrapped(BlackHoleSourceOrderDescription.Get());
        ImGui.Checkbox("Show inter-Black Hole AOE guides", ref C.ShowInterBlackHoleAoeGuides);
        ImGui.Checkbox("Debug Black Hole tether logs", ref C.EnableBlackHoleDebugLogs);
        ImGui.Unindent();
    }

    private void DrawMarkerAssignmentSettings()
    {
        ImGui.Separator();
        if (ImGui.CollapsingHeader("Party marker assignment"))
        {
            ImGui.Indent();
            DrawMarkerLineOrders();
            ImGui.Unindent();
        }
    }

    private void DrawMarkerCommandSettings()
    {
        ImGui.Separator();
        if (!ImGui.CollapsingHeader("Marker command")) return;

        ImGui.Indent();
        ImGui.TextWrapped(MarkerCommandDescription.Get());
        ImGui.Spacing();
        ImGui.Checkbox("Execute self marker command", ref C.ExecuteMarkerCommand);
        DrawFloat("Marker delay min (s)", ref C.MarkerDelayMinSeconds);
        DrawFloat("Marker delay max (s)", ref C.MarkerDelayMaxSeconds);
        ImGui.Spacing();
        DrawCommand("First Target command", ref C.FirstTargetCommand);
        DrawCommand("Second Target command", ref C.SecondTargetCommand);
        DrawCommand("Third Target command", ref C.ThirdTargetCommand);
        ImGui.Unindent();
    }

    private void DrawDisplayTextSettings()
    {
        if (!ImGui.CollapsingHeader("Display text")) return;

        ImGui.Indent();
        ImGui.TextWrapped(DisplayTextDescription.Get());

        DrawSubsection("Line navigation");
        DrawText("Take line", C.TakeLineText, ref C.ShowTakeLineText);
        DrawText("Waiting", C.WaitingText, ref C.ShowWaitingText);
        DrawText("Unknown slot", C.UnknownSlotText, ref C.ShowUnknownSlotText);
        DrawText("Overlay", C.OverlayText, ref C.ShowOverlayText);
        ImGui.Unindent();

        DrawSubsection("Middle mechanics");
        DrawText("Head stack", C.HeadStackText, ref C.ShowHeadStackText);
        DrawText("Role spread", C.RoleSpreadText, ref C.ShowRoleSpreadText);
        DrawText("Dubbing", C.DubbingText, ref C.ShowDubbingText);
        DrawText("As-Is", C.AsIsText, ref C.ShowAsIsText);
        DrawText("White Hole", C.WhiteHoleText, ref C.ShowWhiteHoleText);
        DrawText("Implosion", C.ImplosionText, ref C.ShowImplosionText);
        ImGui.Unindent();

        DrawSubsection("Final sequence");
        DrawText("Final center", C.FinalCenterText, ref C.ShowFinalCenterText);
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
        ImGui.TextWrapped($"KefkaCandidates={KefkaCandidateText()}");
        ImGui.TextUnformatted($"Final={_finalStage} Landing={_landingCount} FirstStack={_firstFinalStackRole} CurrentStack={_currentFinalStackRole}");
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
        _liveBlackHoleTethers.Clear();
        CacheLiveBlackHoleActors();
        ClearCurrentWindowTethers();
        ClearSelfTether(true, "start-blackhole");
        ClearGuide();
        LogKefkaSnapshot("black-hole-start");
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
    }

    private void ResolveSelfSlot()
    {
        var me = BasePlayer;
        if (me == null || _selfSlot != Slot.None || !_groups.ContainsKey(me.EntityId)) return;
        if (TryResolveSlot(me, out _selfSlot, out _quality))
            _instruction = TextOrEmpty(C.ShowWaitingText, C.WaitingText, SlotName(_selfSlot));
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
        _selfDestination = null;
        _selfTetherSource = null;
        _selfTetherTarget = 0;
        _lastMissingLineTarget = 0;
        _selfTetherBucket = -1;
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

        if (C.AssignmentMode is AssignmentMode.RoleAccretion or AssignmentMode.FixedRoleAccretion)
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

        if (C.AssignmentMode is AssignmentMode.PartyMarker or AssignmentMode.MarkerThenPriority &&
            TryMarkerSlot(player, group, out slot))
        {
            quality = AssignmentQuality.Marker;
            return true;
        }

        if (C.AssignmentMode == AssignmentMode.MarkerThenPriority && _state != State.BlackHoleActive)
        {
            slot = Slot.None;
            return false;
        }

        if (C.AssignmentMode is AssignmentMode.Priority or AssignmentMode.MarkerThenPriority && HasCompleteGroups() &&
            TryPrioritySlot(player, group, out slot))
        {
            quality = C.AssignmentMode == AssignmentMode.Priority ? AssignmentQuality.Priority : AssignmentQuality.Fallback;
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
        var rank = fixedSpots
            ? isAccretion ? (int)C.AccretionMarker : isSupport ? (int)C.SupportMarker : (int)C.DpsMarker
            : isAccretion ? 2 : isSupport ? 1 : 0;
        slot = SlotFromRank(group, rank);
        return slot != Slot.None;
    }

    private void ResolveActiveTethers()
    {
        if (_state != State.BlackHoleActive) return;

        RefreshExpectedTether("update");
    }

    private void RefreshExpectedTether(string reason)
    {
        var expected = ExpectedBucket(_selfSlot);
        if (expected < 0 ||
            !_tetherSources.TryGetValue(expected, out var source) ||
            !_tetherTargets.TryGetValue(expected, out var target))
        {
            if (_selfTetherBucket >= 0)
                ClearSelfTether(true, $"{reason}:missing expected={DirectionName(expected)} active=[{ActiveBucketText()}]");
            return;
        }

        if (!_selfDestination.HasValue || _selfTetherBucket != expected || _selfTetherTarget != target)
            SetSelfTether(source, expected, target, reason);
    }

    private bool HasCompleteGroups()
    {
        return _groups.Count(x => x.Value == TargetGroup.Attack) == 3 &&
               _groups.Count(x => x.Value == TargetGroup.Bind) == 3 &&
               _groups.Count(x => x.Value == TargetGroup.Stop) == 2;
    }

    private void ClearCurrentWindowTethers()
    {
        _activeTethers.Clear();
        _blackHoleByEndpoint.Clear();
        _tetherBucketsBySource.Clear();
        _tetherTargets.Clear();
        _tetherSources.Clear();
        _pendingTetherRemovals.Clear();
    }

    private void ApplyPendingTetherRemovals()
    {
        if (_pendingTetherRemovals.Count == 0) return;

        var removals = _pendingTetherRemovals.ToArray();
        _pendingTetherRemovals.Clear();
        foreach (var (blackHoleId, removal) in removals)
        {
            _activeTethers.TryGetValue(blackHoleId, out var latestTarget);
            _tetherBucketsBySource.TryGetValue(blackHoleId, out var bucket);
            DebugLog($"TETHER_REMOVE observed bh={Describe(blackHoleId)} bucket={DirectionName(bucket)} latestTarget={Describe(latestTarget)} rawSource={Describe(removal.Source)} data={removal.Data2}/{removal.Data3}/{removal.Data5} slot={_selfSlot} selfBucket={DirectionName(_selfTetherBucket)} active=[{ActiveBucketText()}]");
        }
        RefreshExpectedTether("remove-observed");
    }

    private void PollLiveBlackHoleTethers()
    {
        if (_liveBlackHoleIds.Count < ExpectedBlackHoleActors)
            CacheLiveBlackHoleActors();

        for (var i = _liveBlackHoleIds.Count - 1; i >= 0; i--)
        {
            var blackHoleId = _liveBlackHoleIds[i];
            if (Svc.Objects.SearchById(blackHoleId) is not ICharacter blackHole || !IsBlackHoleObject(blackHole))
            {
                _liveBlackHoleIds.RemoveAt(i);
                _liveBlackHoleTethers.Remove(blackHoleId);
                continue;
            }

            if (!TryGetLiveTetherEntry(blackHole, out var entry))
            {
                if (_liveBlackHoleTethers.Remove(blackHoleId, out var old))
                    DebugLog($"LIVE_TETHER_CLEAR bh={Describe(blackHoleId)} old=[{LiveTetherText(old)}] slot={_selfSlot} selfBucket={DirectionName(_selfTetherBucket)} active=[{ActiveBucketText()}]");
                continue;
            }

            if (_liveBlackHoleTethers.TryGetValue(blackHoleId, out var oldEntry) && oldEntry == entry)
                continue;

            DebugLog($"LIVE_TETHER_CHANGE bh={Describe(blackHoleId)} old=[{(_liveBlackHoleTethers.TryGetValue(blackHoleId, out oldEntry) ? LiveTetherText(oldEntry) : "")}] new=[{LiveTetherText(entry)}] slot={_selfSlot} expected={DirectionName(ExpectedBucket(_selfSlot))} active=[{ActiveBucketText()}]");
            _liveBlackHoleTethers[blackHoleId] = entry;
            HandleLiveBlackHoleTether(blackHoleId, entry.Target, entry.Id);
        }
    }

    private void CacheLiveBlackHoleActors()
    {
        _liveBlackHoleIds.Clear();
        foreach (var obj in Svc.Objects)
            if (obj is ICharacter character && IsBlackHoleObject(character))
                _liveBlackHoleIds.Add(character.EntityId);
    }

    private static bool TryGetLiveTetherEntry(ICharacter source, out LiveTetherEntry entry)
    {
        var chr = source.Struct();
        for (var i = 0; i < chr->Vfx.Tethers.Length; i++)
        {
            var tether = chr->Vfx.Tethers[i];
            if (tether.Id == 0) continue;

            var target = Svc.Objects.FirstOrDefault(x => x.GameObjectId == tether.TargetId);
            var targetId = target?.EntityId ?? tether.TargetId.ObjectId;
            if (targetId == 0) continue;
            entry = new LiveTetherEntry(tether.Id, tether.Progress, targetId);
            return true;
        }
        entry = default;
        return false;
    }

    private void HandleLiveBlackHoleTether(uint blackHoleId, uint target, ushort tetherId)
    {
        if (_state != State.BlackHoleActive || tetherId != BlackHoleTetherData3)
            return;
        if (!TryResolveBlackHoleTether(blackHoleId, target, out var resolvedBlackHoleId, out var tetherTarget,
                out var blackHolePosition, out var bucket))
        {
            DebugLog($"LIVE_TETHER rejected bh={Describe(blackHoleId)} target={Describe(target)} id={tetherId} slot={_selfSlot} expected={ExpectedBucket(_selfSlot)} active=[{ActiveBucketText()}]");
            return;
        }

        CacheBlackHoleTether(blackHoleId, target, resolvedBlackHoleId, tetherTarget, blackHolePosition, bucket,
            "live-vfx", $"live-id={tetherId}");
    }

    private void ClearSelfTether(bool keepWaitingText, string reason = "")
    {
        if (_selfTetherBucket >= 0 || _selfDestination.HasValue || _selfTetherSource.HasValue)
            DebugLog($"DISPLAY_CLEAR reason={reason} previousBucket={DirectionName(_selfTetherBucket)} previousTarget={Describe(_selfTetherTarget)} slot={_selfSlot} active=[{ActiveBucketText()}]");
        _selfDestination = null;
        _selfTetherSource = null;
        _selfTetherTarget = 0;
        _lastMissingLineTarget = 0;
        _selfTetherBucket = -1;
        _instruction = keepWaitingText && _selfSlot != Slot.None
            ? TextOrEmpty(C.ShowWaitingText, C.WaitingText, SlotName(_selfSlot))
            : "";
    }

    private void RunMarkerCommand(TargetGroup group)
    {
        if (_sentMarkerCommand) return;
        _sentMarkerCommand = true;
        if (!C.ExecuteMarkerCommand || Svc.Condition[ConditionFlag.DutyRecorderPlayback]) return;
        _pendingMarkerCommand = group switch
        {
            TargetGroup.Attack => C.FirstTargetCommand,
            TargetGroup.Bind => C.SecondTargetCommand,
            TargetGroup.Stop => C.ThirdTargetCommand,
            _ => ""
        };
        if (string.IsNullOrWhiteSpace(_pendingMarkerCommand)) return;

        _markerCommandAtMs = Environment.TickCount64 + ToRandomDelayMs(C.MarkerDelayMinSeconds, C.MarkerDelayMaxSeconds);
    }

    private void ExecutePendingMarkerCommand()
    {
        if (_markerCommandAtMs <= 0 || Environment.TickCount64 < _markerCommandAtMs) return;

        var command = _pendingMarkerCommand;
        _pendingMarkerCommand = "";
        _markerCommandAtMs = 0;
        if (!string.IsNullOrWhiteSpace(command) && !Svc.Condition[ConditionFlag.DutyRecorderPlayback])
            Chat.ExecuteCommand(command);
    }

    private void SetSelfTether(Vector3 source, int bucket, uint target, string reason)
    {
        var changed = _selfTetherBucket != bucket || _selfTetherTarget != target || !_selfDestination.HasValue;
        _selfTetherSource = new Vector3(source.X, 0.0f, source.Z);
        _selfTetherTarget = target;
        _selfTetherBucket = bucket;
        _selfDestination = BaitPosition(source);
        _instruction = TextOrEmpty(C.ShowTakeLineText, C.TakeLineText, SlotName(_selfSlot), DirectionName(bucket));
        if (changed)
            DebugLog($"DISPLAY_SET reason={reason} bucket={DirectionName(bucket)} target={Describe(target)} targetSelf={target == BasePlayer?.EntityId} destination=({ _selfDestination.Value.X:F2},{_selfDestination.Value.Z:F2}) slot={_selfSlot} rank={ExpectedRank(_selfSlot, _currentWindow)} active=[{ActiveBucketText()}]");
    }

    private void AdvanceWindow(int sourceBucket)
    {
        if (_state != State.BlackHoleActive || _currentWindow is < 0 or > 9) return;
        _hitSources.Add(sourceBucket);
        DebugLog($"WINDOW_HIT window={_currentWindow} bucket={DirectionName(sourceBucket)} hits={_hitSources.Count}/{ExpectedSourcesByWindow[_currentWindow]} selfBucket={DirectionName(_selfTetherBucket)} active=[{ActiveBucketText()}]");
        if (_hitSources.Count < ExpectedSourcesByWindow[_currentWindow]) return;

        if (_selfTetherBucket >= 0 && _hitSources.Contains(_selfTetherBucket))
            ClearSelfTether(true, "window-hit");
        _hitSources.Clear();
        ClearCurrentWindowTethers();
        DebugLog($"WINDOW_ADVANCE next={_currentWindow + 1} cleared active tethers");
        _currentWindow++;
        if (_currentWindow > 9)
            EnterFinalSequence();
    }

    private void EnterFinalSequence()
    {
        if (_state == State.Completed) return;

        var previous = _state;
        _state = State.FinalSequence;
        if (_finalStage == FinalStage.None)
            _finalStage = FinalStage.AwaitingBlizzaga;
        _currentWindow = Math.Max(_currentWindow, 10);
        _instruction = "";
        ClearSelfTether(false, "enter-final");
        ClearBlackHoleState();
        DebugLog($"FINAL_ENTER previousState={previous} {FinalStateText()}");
    }

    private void SetFinalCenterBait()
    {
        EnterFinalSequence();
        _finalStage = FinalStage.CenterBait;
        DebugLog($"FINAL_CENTER action={LateP3Blizzaga} {FinalStateText()}");
        SetGuide(Center, TextOrEmpty(C.ShowFinalCenterText, C.FinalCenterText), GuidanceKind.FinalCenter,
            LateP3Blizzaga, 0.0f, 0.0f);
    }

    private void SetFinalRoleSpread()
    {
        EnterFinalSequence();
        _finalStage = FinalStage.RoleSpread;
        DebugLog($"FINAL_SPREAD action={DondokoCast} {FinalStateText()}");
        SetFinalRoleGuide(C.ShowFinalSpreadText, C.FinalSpreadText, GuidanceKind.FinalSpread, DondokoCast);
    }

    private void SetKnownFinalLanding()
    {
        DebugLog($"FINAL_LANDING_RESOLVE knownCurrent={_currentFinalStackRole} first={_firstFinalStackRole} landing={_landingCount} {FinalStateText()}");
        if (_currentFinalStackRole != FinalStackRole.Unknown)
            SetFinalLanding(_currentFinalStackRole);
        else if (_landingCount == 1 && _firstFinalStackRole != FinalStackRole.Unknown)
            SetFinalLanding(Opposite(_firstFinalStackRole));
        else
            DebugLog($"FINAL_LANDING_RESOLVE skipped reason=unknown-stack {FinalStateText()}");
    }

    private void SetFinalLanding(FinalStackRole stackRole)
    {
        if (stackRole == FinalStackRole.Unknown)
        {
            DebugLog($"FINAL_LANDING skipped reason=unknown-stack {FinalStateText()}");
            return;
        }

        EnterFinalSequence();
        _finalStage = _landingCount == 0 ? FinalStage.Landing1 : FinalStage.Landing2;
        _currentFinalStackRole = stackRole;
        if (_firstFinalStackRole == FinalStackRole.Unknown)
            _firstFinalStackRole = stackRole;

        var ownStackRole = OwnFinalStackRole();
        DebugLog($"FINAL_LANDING stack={stackRole} ownStack={ownStackRole} {FinalStateText()}");
        if (ownStackRole == stackRole)
        {
            DebugLog($"FINAL_GUIDE mode=stack destination={PositionText(Center)} {FinalStateText()}");
            SetGuide(Center, TextOrEmpty(C.ShowFinalStackText, C.FinalStackText), GuidanceKind.FinalLanding,
                LandingCast, 0.0f, 0.0f);
            return;
        }

        if (TryGetOwnRolePosition(out var role))
        {
            var destination = FinalPairPosition(role);
            DebugLog($"FINAL_GUIDE mode=tower role={role} destination={PositionText(destination)} {FinalStateText()}");
            SetGuide(destination, TextOrEmpty(C.ShowFinalTowerText, C.FinalTowerText, PairName(role)),
                GuidanceKind.FinalLanding, LandingCast, 0.0f, 0.0f);
        }
        else
        {
            DebugLog($"FINAL_GUIDE mode=tower role=unknown instructionOnly {FinalStateText()}");
            SetInstruction(TextOrEmpty(C.ShowFinalTowerText, C.FinalTowerText, "?"), GuidanceKind.FinalLanding);
        }
    }

    private void AdvanceFinalLanding()
    {
        if (_state != State.FinalSequence)
            EnterFinalSequence();

        DebugLog($"FINAL_LANDING_ADVANCE before landing={_landingCount} first={_firstFinalStackRole} current={_currentFinalStackRole} {FinalStateText()}");
        _landingCount++;
        _currentFinalStackRole = FinalStackRole.Unknown;
        if (_landingCount >= 2)
            SetFinalMove();
        else if (_firstFinalStackRole != FinalStackRole.Unknown)
            SetFinalLanding(Opposite(_firstFinalStackRole));
        else
            DebugLog($"FINAL_LANDING_ADVANCE waiting reason=missing-first-stack {FinalStateText()}");
    }

    private void SetFinalMove()
    {
        EnterFinalSequence();
        _finalStage = FinalStage.ProtrudeMove;
        DebugLog($"FINAL_MOVE action={Protrude} {FinalStateText()}");
        SetFinalRoleGuide(C.ShowFinalMoveText, C.FinalMoveText, GuidanceKind.FinalMove, Protrude);
    }

    private void SetFinalRoleGuide(bool show, InternationalString text, GuidanceKind kind, uint actionId)
    {
        if (TryGetOwnRolePosition(out var role))
        {
            var destination = FinalPairPosition(role);
            DebugLog($"FINAL_ROLE_GUIDE kind={kind} action={actionId} role={role} destination={PositionText(destination)} {FinalStateText()}");
            SetGuide(destination, TextOrEmpty(show, text, PairName(role)), kind, actionId, 0.0f, 0.0f);
        }
        else
        {
            DebugLog($"FINAL_ROLE_GUIDE kind={kind} action={actionId} role=unknown instructionOnly {FinalStateText()}");
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
        _markerCommandAtMs = 0;
        ClearMechanicState(clearSlot: true);
        HideElements();
        _state = State.Idle;
    }

    private void ClearMechanicState(bool clearSlot)
    {
        _earthPlayers.Clear();
        _accretionPlayers.Clear();
        _kefkaId = 0;
        _kefkaPosition = null;
        _kefkaAnchorDebug = "";
        ClearPendingPostDubbingGuide();
        _dubbingDestination = null;
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
        _currentFinalStackRole = FinalStackRole.Unknown;
        _landingCount = 0;
    }

    private void ClearBlackHoleState()
    {
        ClearCurrentWindowTethers();
        _liveBlackHoleIds.Clear();
        _liveBlackHoleTethers.Clear();
        _hitSources.Clear();
    }

    private void ShowDestination(Vector3 destination, string text)
    {
        if (!Controller.TryGetElementByName(DestinationElement, out var element)) return;
        element.Enabled = true;
        element.color = RainbowColor();
        element.SetRefPosition(destination);
        element.overlayText = text;
    }

    private void ShowSelfTetherLine()
    {
        if (BasePlayer == null || _selfTetherSource is not { } source ||
            !Controller.TryGetElementByName(TetherLineElement, out var element))
            return;
        if (_selfTetherTarget.GetObject() is not { } target)
        {
            if (_lastMissingLineTarget != _selfTetherTarget)
            {
                _lastMissingLineTarget = _selfTetherTarget;
                DebugLog($"DISPLAY_LINE_TARGET_MISSING target={Describe(_selfTetherTarget)} bucket={DirectionName(_selfTetherBucket)} slot={_selfSlot}");
            }
            return;
        }
        _lastMissingLineTarget = 0;

        element.Enabled = true;
        element.color = TetherLineColor();
        element.SetRefPosition(source);
        element.SetOffPosition(target.Position);
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
        if (actorId.GetObject() is { } obj && TryBucket(obj.Position, out bucket))
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
            if (!TryCaptureKefkaFromMatchingClone(rotation, $"anchor-rotation-match-{actionId}"))
                CaptureKefkaFromRotation(rotation, offset, $"anchor-rotation-fallback-{actionId}");
        }
        else if (actionId == DecisiveBattleChaos)
            CaptureKefka(actorId, force: true, "decisive-battle");
        else if (actorId != 0 && actorId == _kefkaId)
            CaptureKefka(actorId, force: false, $"cast-{actionId}");
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

    private void CaptureKefkaFromRotation(float rotation, float offset, string reason)
    {
        var angle = NormalizeAngle(rotation + offset);
        var position = PositionFromDirectionAngle(angle, KefkaVirtualAnchorRadius);
        var changed = _kefkaPosition is not { } old || Vector3.DistanceSquared(old, position) > 0.25f;
        _kefkaId = 0;
        _kefkaPosition = position;
        _kefkaAnchorDebug = $"cast-rotation {Deg(rotation):F1}{SignedDeg(offset)}";
        if (changed)
            DebugLog($"KEFKA_ANCHOR reason={reason} rot={Deg(rotation):F1} off={Deg(offset):F1} dir={Deg(angle):F1} pos=({position.X:F1},{position.Z:F1}) angle={Deg(DirectionAngle(position)):F1}");
    }

    private bool TryCaptureKefkaFromMatchingClone(float rotation, string reason)
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
        var changed = _kefkaPosition is not { } old || Vector3.DistanceSquared(old, position) > 0.25f;
        _kefkaId = 0;
        _kefkaPosition = position;
        _kefkaAnchorDebug = $"rotation-match {Deg(rotation):F1}->{Describe(best.EntityId)}";
        if (changed)
            DebugLog($"KEFKA_ANCHOR reason={reason} rot={Deg(rotation):F1} matchRot={Deg(best.Rotation):F1} diff={Deg(bestDiff):F1} pos=({position.X:F1},{position.Z:F1}) angle={Deg(DirectionAngle(position)):F1}");
        return true;
    }

    private void RefreshKefkaAnchorFromObject()
    {
        if (_kefkaId != 0)
            CaptureKefka(_kefkaId, force: false, "object");
    }

    private void CaptureKefka(uint actorId, bool force, string reason)
    {
        if (actorId.GetObject() is { } obj)
            CaptureKefka(obj, force, reason);
    }

    private void LogFinalCast(string origin, uint source, uint actionId, float? packetRotation)
    {
        if (!C.EnableBlackHoleDebugLogs || !IsFinalProbeAction(actionId)) return;
        var packetText = packetRotation.HasValue ? $" packetRot={Deg(packetRotation.Value):F1}" : "";
        DebugLog($"FINAL_CAST origin={origin} action={actionId} source={DescribeObjectDetail(source)}{packetText} {FinalStateText()}");
    }

    private void LogFinalAction(ActionEffectSet set, uint actionId)
    {
        if (!C.EnableBlackHoleDebugLogs || !IsFinalProbeAction(actionId)) return;
        var targets = string.Join(",",
            set.TargetEffects.Select(target => DescribeTarget((ulong)target.TargetID)).Take(8));
        DebugLog($"FINAL_ACTION action={actionId} source={DescribeObjectDetail(set.Source?.EntityId ?? 0)} target={Describe(set.Target?.EntityId ?? 0)} eventPos=({set.Position.X:F2},{set.Position.Z:F2}) rot={Deg(set.Header.RealRotation):F1} targets=[{targets}] {FinalStateText()}");
    }

    private void LogFinalActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5,
        uint p6, uint p7, uint p8, ulong targetId, byte replaying)
    {
        if (!C.EnableBlackHoleDebugLogs || command != TargetIconCommand || p1 != FinalStackMarker) return;
        var hasRole = TryFinalStackRole(sourceId, out var role);
        DebugLog($"FINAL_MARKER command={command} marker={p1} source={DescribeObjectDetail(sourceId)} role={(hasRole ? role : FinalStackRole.Unknown)} target={DescribeTarget(targetId)} replaying={replaying} p={p1}/{p2}/{p3}/{p4}/{p5}/{p6}/{p7}/{p8} {FinalStateText()}");
    }

    private static bool IsFinalProbeAction(uint actionId) =>
        actionId is LateP3Blizzaga or DondokoCast or DondokoHit or LandingCast or LandingHit or TowerImpact
            or EnhancedBlizzaga or Protrude;

    private string FinalStateText()
    {
        var ownRole = TryGetOwnRolePosition(out var role) ? role.ToString() : "unknown";
        var ownStack = OwnFinalStackRole();
        return $"state={_state} final={_finalStage} landing={_landingCount} first={_firstFinalStackRole} current={_currentFinalStackRole} ownRole={ownRole} ownStack={ownStack}";
    }

    private static string PositionText(Vector3 position) => $"({position.X:F2},{position.Z:F2})";

    private void LogKefkaCandidateEvent(string kind, uint actorId, uint actionId)
    {
        if (!C.EnableBlackHoleDebugLogs || actorId.GetObject() is not { } obj || !IsKefkaAnchorObject(obj))
            return;

        DebugLog($"KEFKA_CANDIDATE_{kind} action={actionId} {KefkaCandidateLine(obj)} current={(_kefkaId == obj.EntityId)}");
    }

    private unsafe void LogKefkaCastProbe(string origin, uint source, uint castId, PacketActorCast* packet)
    {
        if (!C.EnableBlackHoleDebugLogs || !IsKefkaProbeCast(castId))
            return;

        var sourceText = source.GetObject() is { } obj
            ? KefkaCandidateDetailLine(obj, ObjectListIndex(obj.EntityId))
            : Describe(source);
        var packetText = packet == null
            ? ""
            : $" packetTarget={Describe(packet->TargetID)} packetPos=({packet->Position.X:F2},{packet->Position.Y:F2},{packet->Position.Z:F2}) packetRot={packet->Rotation * 180.0f / MathF.PI:F1}";
        DebugLog($"KEFKA_CAST origin={origin} action={castId} source={sourceText} candidate={(source.GetObject() is { } candidate && IsKefkaAnchorObject(candidate))}{packetText}");
        LogActiveKefkaCasts(castId);
        LogKefkaSnapshot($"cast {origin} {castId}");
    }

    private void LogActiveKefkaCasts(uint castId)
    {
        var candidates = Svc.Objects
            .Select((obj, index) => (obj, index))
            .Where(x => IsKefkaRelatedObject(x.obj))
            .ToList();

        DebugLog($"KEFKA_ACTIVE_CAST_SCAN action={castId} count={candidates.Count}");
        foreach (var (obj, index) in candidates)
        {
            var battle = obj as IBattleChara;
            DebugLog($"KEFKA_ACTIVE_CAST action={castId} match={battle?.CastActionId == castId} {KefkaCandidateDetailLine(obj, index)}");
        }
    }

    private void LogKefkaActorControlProbe(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5,
        uint p6, uint p7, uint p8, ulong targetId, byte replaying)
    {
        if (!C.EnableBlackHoleDebugLogs || command is not (98 or 417) ||
            !HasKefkaRelatedReference(sourceId, p1, p2, p3, p4, p5, p6, p7, p8))
            return;

        DebugLog($"KEFKA_ACTOR_CONTROL command={command} replaying={replaying} target={DescribeTarget(targetId)} source={DescribeObjectDetail(sourceId)} p={p1}/{p2}/{p3}/{p4}/{p5}/{p6}/{p7}/{p8} p1Obj={DescribeObjectDetail(p1)} p2Obj={DescribeObjectDetail(p2)} p3Obj={DescribeObjectDetail(p3)} p4Obj={DescribeObjectDetail(p4)}");
        LogKefkaSnapshot($"actor-control {command}");
    }

    private void LogKefkaActionProbe(ActionEffectSet set, uint actionId)
    {
        if (!C.EnableBlackHoleDebugLogs ||
            actionId is not (KefkaDashStart or KefkaDashEnd) ||
            set.Source is not { } source ||
            !IsKefkaAnchorObject(source))
            return;

        DebugLog($"KEFKA_ACTION action={actionId} source={KefkaCandidateDetailLine(source, ObjectListIndex(source.EntityId))} target={Describe(set.Target?.EntityId ?? 0)} eventPos=({set.Position.X:F2},{set.Position.Y:F2},{set.Position.Z:F2}) rotation={set.Header.RealRotation:F3} targets={set.TargetEffects.Count()}");
        foreach (var target in set.TargetEffects)
            DebugLog($"KEFKA_ACTION_TARGET action={actionId} source={source.EntityId:X8} target={DescribeTarget((ulong)target.TargetID)} {DescribeActionEffectEntries(target)}");
        LogKefkaSnapshot($"action {actionId}");
    }

    private void LogKefkaSnapshot(string reason)
    {
        if (!C.EnableBlackHoleDebugLogs) return;

        var candidates = Svc.Objects
            .Select((obj, index) => (obj, index))
            .Where(x => IsKefkaAnchorObject(x.obj))
            .ToList();

        DebugLog($"KEFKA_SNAPSHOT reason=\"{reason}\" count={candidates.Count} selected={Describe(_kefkaId)} anchor={OrderAnchorDebugText()}");
        foreach (var (obj, index) in candidates)
            DebugLog($"KEFKA_OBJECT reason=\"{reason}\" {KefkaCandidateDetailLine(obj, index)} current={(_kefkaId == obj.EntityId)}");
    }

    private void CaptureKefka(IGameObject? obj, bool force, string reason)
    {
        if (obj == null) return;
        if (!force && _kefkaId != 0 && obj.EntityId != _kefkaId) return;
        var position = FlatPosition(obj.Position);
        if (!IsKefkaAnchorPosition(position)) return;
        var changed = _kefkaPosition is not { } old || Vector3.DistanceSquared(old, position) > 0.25f;
        _kefkaId = obj.EntityId;
        _kefkaPosition = position;
        _kefkaAnchorDebug = $"object {Describe(obj.EntityId)}";
        if (changed)
            DebugLog($"KEFKA_ANCHOR reason={reason} pos=({position.X:F1},{position.Z:F1})");
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

    private static bool IsKefkaProbeCast(uint actionId) =>
        actionId is DecisiveBattleChaos or DecisiveBattleExdeath or BlackHoleCast or BintaStackCast or BintaSpreadCast
            or DubbingEdict or AsIsFirst or AsIsSecond or LongitudinalImplosion or LatitudinalImplosion;

    private static bool IsKefkaRelatedObject(IGameObject obj) =>
        obj.DataId == KefkaDataId ||
        obj.Struct()->GetNameId() == KefkaNameId ||
        obj is ICharacter character && character.NameId == KefkaNameId;

    private static bool HasKefkaRelatedReference(params uint[] actorIds) =>
        actorIds.Any(actorId => actorId == KefkaNameId || actorId.GetObject() is { } obj && IsKefkaRelatedObject(obj));

    private float OrderAnchorAngle() => C.BlackHoleOrderAnchor switch
    {
        BlackHoleOrderAnchor.KefkaPosition when TryGetKefkaPosition(out var kefka) => DirectionAngle(kefka),
        _ => 0.0f
    };

    private float SourceAngle(int bucket) =>
        _tetherSources.TryGetValue(bucket, out var source) ? DirectionAngle(source) : BucketAngle(bucket);

    private float OrderedAngleDistance(float sourceAngle, float anchorAngle) =>
        C.BlackHoleSourceOrder == BlackHoleSourceOrder.ClockwiseFromNorth
            ? NormalizeAngle(sourceAngle - anchorAngle)
            : NormalizeAngle(anchorAngle - sourceAngle);

    private string OrderAnchorDebugText() => C.BlackHoleOrderAnchor switch
    {
        BlackHoleOrderAnchor.KefkaPosition when TryGetKefkaPosition(out var pos) =>
            $"{(string.IsNullOrWhiteSpace(_kefkaAnchorDebug) ? "Kefka" : _kefkaAnchorDebug)}({pos.X:F1},{pos.Z:F1})",
        _ => "N"
    };

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

    private static string KefkaCandidateDetailLine(IGameObject obj, int objectListIndex)
    {
        var pos = FlatPosition(obj.Position);
        var distance = Vector2.Distance(new Vector2(pos.X, pos.Z), new Vector2(Center.X, Center.Z));
        var angle = DirectionAngle(pos) * 180.0f / MathF.PI;
        var rotation = obj.Rotation * 180.0f / MathF.PI;
        var ptr = obj.Struct();
        var goid = (ulong)ptr->GetGameObjectId();
        var npc = obj as IBattleNpc;
        var battle = obj as IBattleChara;
        var character = obj as ICharacter;
        var visible = character?.IsCharacterVisible() == true;
        var characterLine = character == null
            ? ""
            : $" nameId={character.NameId} model={character.Struct()->ModelContainer.ModelCharaId} target={DescribeTarget(character.TargetObjectId)} eventState={character.Struct()->EventState} statusLoop={character.Struct()->StatusLoopVfxId} tethers=[{DescribeTethers(character)}]";
        var battleLine = DescribeBattleState(battle);
        var npcLine = npc == null ? "" : $" npcKind={npc.BattleNpcKind}";

        return $"idx={objectListIndex} id={obj.EntityId:X8} go={goid:X} data={obj.DataId} base={obj.BaseId} kind={obj.ObjectKind} sub={ptr->SubKind}{npcLine} owner={obj.OwnerId:X} npcId={ptr->GetNameId()} namePlate={ptr->NamePlateIconId} layout={ptr->LayoutId} dead={ptr->IsDead()} pos=({pos.X:F2},{pos.Z:F2}) r={distance:F2} angle={angle:F1} rot={rotation:F1} hitbox={obj.HitboxRadius:F1} tar={ptr->GetIsTargetable()} vis={visible} targetStatus={ptr->TargetStatus} render={ptr->RenderFlags} vfxScale={ptr->VfxScale:F2} draw={(nint)ptr->DrawObject:X}{characterLine}{battleLine}";
    }

    private static int ObjectListIndex(uint entityId)
    {
        var index = 0;
        foreach (var obj in Svc.Objects)
        {
            if (obj.EntityId == entityId)
                return index;
            index++;
        }

        return -1;
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

    private static string DescribeBattleState(IBattleChara? battle)
    {
        if (battle == null)
            return "";
        try
        {
            return $" cast={battle.IsCasting}/{battle.CastActionId}/{battle.CurrentCastTime:F2}/{battle.TotalCastTime:F2} statuses=[{DescribeStatuses(battle)}]";
        }
        catch (Exception e)
        {
            return $" cast=<unavailable:{e.GetType().Name}>";
        }
    }

    private static string DescribeStatuses(IBattleChara battle)
    {
        var entries = battle.StatusList
            .Where(x => x.StatusId != 0)
            .Take(16)
            .Select(x => $"{x.StatusId}({x.RemainingTime:F1})")
            .ToList();
        return entries.Count == 0 ? "none" : string.Join(",", entries);
    }

    private static string DescribeActionEffectEntries(TargetEffect target)
    {
        var entries = new List<string>();
        for (var i = 0; i < 8; i++)
        {
            var entry = target[i];
            if (entry.type == ActionEffectType.Nothing)
                continue;
            entries.Add($"{i}:{entry.type} p={entry.param0}/{entry.param1}/{entry.param2} mult={entry.mult} flags={entry.flags} value={entry.value} damage={entry.Damage}");
        }

        return entries.Count == 0 ? "entries=none" : $"entries=[{string.Join("; ", entries)}]";
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

    private Vector3 BaitPosition(Vector3 source)
    {
        var radial = Vector3.Normalize(new Vector3(source.X - Center.X, 0, source.Z - Center.Z));
        var tangent = new Vector3(-radial.Z, 0, radial.X);
        if (C.LineBaitDirection == LineBaitDirection.Counterclockwise)
            tangent = -tangent;
        return new Vector3(source.X, 0, source.Z) + tangent * BaitOffset;
    }

    private uint TetherLineColor()
    {
        var expected = ExpectedBucket(_selfSlot);
        if (expected < 0 || _selfTetherBucket < 0)
            return UnknownLineColor;
        if (expected != _selfTetherBucket)
            return WrongLineColor;
        return _selfTetherTarget == BasePlayer?.EntityId ? CorrectLineColor : WrongLineColor;
    }

    private int ExpectedBucket(Slot slot)
    {
        var rank = ExpectedRank(slot, _currentWindow);
        if (C.AssignmentMode == AssignmentMode.FixedRoleAccretion)
            return ExpectedFixedSpotBucket(rank);
        var buckets = OrderedActiveBuckets();
        return rank >= 0 && rank < buckets.Count ? buckets[rank] : -1;
    }

    private int ExpectedFixedSpotBucket(int rank)
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

    private static int ExpectedRank(Slot slot, int window) => window switch
    {
        0 => slot == Slot.Attack1 ? 0 : -1,
        1 => slot switch { Slot.Attack1 => 0, Slot.Attack2 => 1, _ => -1 },
        2 => slot switch { Slot.Attack1 => 0, Slot.Attack2 => 1, Slot.Attack3 => 2, _ => -1 },
        3 => slot switch { Slot.Bind1 => 0, Slot.Attack2 => 1, Slot.Attack3 => 2, _ => -1 },
        4 => slot switch { Slot.Bind1 => 0, Slot.Bind2 => 1, Slot.Attack3 => 2, _ => -1 },
        5 => slot switch { Slot.Bind1 => 0, Slot.Bind2 => 1, Slot.Bind3 => 2, _ => -1 },
        6 => slot switch { Slot.Stop1 => 0, Slot.Bind2 => 1, Slot.Bind3 => 2, _ => -1 },
        7 => slot switch { Slot.Stop1 => 0, Slot.Stop2 => 1, Slot.Bind3 => 2, _ => -1 },
        8 => slot switch { Slot.Stop1 => 0, Slot.Stop2 => 1, _ => -1 },
        9 => slot == Slot.Stop2 ? 0 : -1,
        _ => -1
    };

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

    private void SetBintaGuide(uint source, float? eventRotation, bool stack)
    {
        if (!TryGetKefkaAnchorRotation(source, eventRotation, out var rotation)) return;
        if (stack)
        {
            const float offset = MathF.PI / 2.0f;
            var kefkaRelative = TryGetKefkaRelativePosition(offset, StackRadius, out var position, out var anchorAngle);
            var destination = kefkaRelative ? position : RadialFromFacing(rotation, offset, StackRadius);
            SavePostDubbingGuide(destination, TextOrEmpty(C.ShowHeadStackText, C.HeadStackText), GuidanceKind.HeadStack,
                BintaStackCast, kefkaRelative ? anchorAngle : rotation, offset);
        }
        else
        {
            var offset = RoleSpreadOffset();
            SavePostDubbingGuide(RadialFromFacing(rotation, offset, SpreadRadius),
                TextOrEmpty(C.ShowRoleSpreadText, C.RoleSpreadText), GuidanceKind.RoleSpread, BintaSpreadCast, rotation, offset);
        }
    }

    private void SetDirectionalGuide(uint source, uint actionId, string text, GuidanceKind kind, float offset, float radius,
        float? eventRotation, bool delayDuringDubbing = false)
    {
        if (!TryGetKefkaAnchorRotation(source, eventRotation, out var rotation)) return;

        var destination = RadialFromFacing(rotation, offset, radius);
        if (actionId == DubbingEdict)
            _dubbingDestination = destination;

        if (delayDuringDubbing)
        {
            SavePostDubbingGuide(destination, text, kind, actionId, rotation, offset);
            return;
        }

        SetGuide(destination, text, kind, actionId, rotation, offset);
    }

    private void SetDubbingGuide(uint source)
    {
        if (!TryGetChaosRotation(source, out var rotation)) return;
        const float offset = MathF.PI;
        var destination = RadialFromFacing(rotation, offset, SafeRadius);
        _dubbingDestination = destination;
        SetGuide(destination, TextOrEmpty(C.ShowDubbingText, C.DubbingText), GuidanceKind.Dubbing, DubbingEdict, rotation, offset);
    }

    private bool TryGetChaosRotation(uint source, out float rotation)
    {
        if (source.GetObject() is { DataId: ChaosDataId } sourceObj)
        {
            rotation = sourceObj.Rotation;
            return true;
        }

        var chaos = Svc.Objects
            .Where(obj => obj.DataId == ChaosDataId)
            .OrderByDescending(obj => obj is IBattleChara battle && battle.IsCasting && battle.CastActionId == DubbingEdict)
            .ThenByDescending(obj => obj is ICharacter character && character.IsCharacterVisible())
            .ThenByDescending(obj => obj.Struct()->GetIsTargetable())
            .FirstOrDefault();

        if (chaos != null)
        {
            rotation = chaos.Rotation;
            return true;
        }

        rotation = 0.0f;
        if (C.EnableBlackHoleDebugLogs)
            DebugLog("DUBBING_SKIP reason=missing-dataid-19508");
        return false;
    }

    private void SavePostDubbingGuide(Vector3 destination, string text, GuidanceKind kind, uint actionId, float rotation, float offset)
    {
        _pendingPostDubbingGuide = (destination, text, kind, actionId, rotation, offset);
        if (_dubbingDestination.HasValue || _guideActionId == DubbingEdict) return;
        RestorePendingPostDubbingGuide();
    }

    private void RestorePendingPostDubbingGuide()
    {
        if (_pendingPostDubbingGuide is not { } guide) return;
        SetGuide(guide.Destination, guide.Text, guide.Kind, guide.ActionId, guide.Rotation, guide.Offset);
    }

    private void ClearPendingPostDubbingGuide()
    {
        _pendingPostDubbingGuide = null;
    }

    private void SetImplosionGuide(uint source, uint actionId, bool longitudinal, float? eventRotation)
    {
        if (!TryGetKefkaAnchorRotation(source, eventRotation, out var rotation)) return;
        var offset = longitudinal ? MathF.PI / 2.0f : 0.0f;
        var a = RadialFromFacing(rotation, offset, SafeRadius);
        var b = RadialFromFacing(rotation, offset + MathF.PI, SafeRadius);
        SetGuide(NearestToSelf(a, b), TextOrEmpty(C.ShowImplosionText, C.ImplosionText), GuidanceKind.Implosion,
            actionId, rotation, offset);
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
        if (C.EnableBlackHoleDebugLogs)
            DebugLog($"GUIDE_SET kind={kind} action={actionId} rot={Deg(rotation):F1} off={Deg(offset):F1} ref=({destination.X:F2},{destination.Z:F2}) anchor={OrderAnchorDebugText()}");
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

    private float RoleSpreadOffset()
    {
        return BasePlayer?.GetRole() switch
        {
            CombatRole.Tank => 3.0f * MathF.PI / 4.0f,
            CombatRole.Healer => MathF.PI / 2.0f,
            _ => MathF.PI / 4.0f
        };
    }

    private Vector3 NearestToSelf(Vector3 a, Vector3 b)
    {
        var me = BasePlayer;
        if (me == null) return a;
        return Vector3.DistanceSquared(me.Position, a) <= Vector3.DistanceSquared(me.Position, b) ? a : b;
    }

    private bool TryGetKefkaRelativePosition(float offset, float radius, out Vector3 position, out float anchorAngle)
    {
        if (TryGetKefkaPosition(out var kefka))
        {
            anchorAngle = DirectionAngle(kefka);
            position = PositionFromDirectionAngle(NormalizeAngle(anchorAngle + offset), radius);
            return true;
        }

        anchorAngle = 0.0f;
        position = default;
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

    private static Vector3 FinalPairPosition(RolePosition role) => role switch
    {
        RolePosition.T1 or RolePosition.H1 => Center + new Vector3(-FinalPairRadius, 0.0f, -FinalPairRadius),
        RolePosition.T2 or RolePosition.H2 => Center + new Vector3(FinalPairRadius, 0.0f, -FinalPairRadius),
        RolePosition.M1 or RolePosition.R1 => Center + new Vector3(-FinalPairRadius, 0.0f, FinalPairRadius),
        RolePosition.M2 or RolePosition.R2 => Center + new Vector3(FinalPairRadius, 0.0f, FinalPairRadius),
        _ => Center
    };

    private static string PairName(RolePosition role) => role switch
    {
        RolePosition.T1 or RolePosition.H1 => "NW MT/H1",
        RolePosition.T2 or RolePosition.H2 => "NE ST/H2",
        RolePosition.M1 or RolePosition.R1 => "SW D1/D3",
        RolePosition.M2 or RolePosition.R2 => "SE D2/D4",
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

    private static uint RainbowColor()
    {
        var t = Environment.TickCount64 % 2400 / 2400f * MathF.PI * 2.0f;
        uint r = (uint)((MathF.Sin(t) * 0.5f + 0.5f) * 255.0f);
        uint g = (uint)((MathF.Sin(t + MathF.PI * 2.0f / 3.0f) * 0.5f + 0.5f) * 255.0f);
        uint b = (uint)((MathF.Sin(t + MathF.PI * 4.0f / 3.0f) * 0.5f + 0.5f) * 255.0f);
        return 0xC8000000u | (r << 16) | (g << 8) | b;
    }

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

    private static string LiveTetherText(LiveTetherEntry entry) =>
        $"id={entry.Id} progress={entry.Progress} target={Describe(entry.Target)}";

    private static string Describe(uint actorId)
    {
        if (actorId == 0)
            return "none";
        if (actorId.GetObject() is { } obj)
            return $"{obj.Name}(0x{actorId:X8})@({obj.Position.X:F1},{obj.Position.Z:F1})";
        return $"0x{actorId:X8}";
    }

    private static string DescribeObjectDetail(uint actorId)
    {
        if (actorId == 0)
            return "none";
        if (actorId == KefkaNameId)
            return $"{actorId}(KefkaNameId)";
        if (actorId.GetObject() is { } obj)
            return KefkaCandidateDetailLine(obj, ObjectListIndex(obj.EntityId));
        return $"0x{actorId:X8}";
    }

    private static string DescribeTarget(ulong actorId)
    {
        if (actorId <= uint.MaxValue)
            return Describe((uint)actorId);
        return $"0x{actorId:X16}";
    }

    private void DebugLog(string message)
    {
        if (C.EnableBlackHoleDebugLogs)
            PluginLog.Information($"[DMU P3 Earthquake] {message}");
    }

    private static bool IsBlackHoleTether(uint data3, uint data5) =>
        data3 == BlackHoleTetherData3 && data5 == BlackHoleTetherData5;

    private static bool IsBlackHoleObject(IGameObject obj) =>
        obj.DataId == BlackHoleDataId && TryBucket(obj.Position, out _);

    private static bool DrawCombo(string label, ref int selected, string[] items, float width)
    {
        ImGui.SetNextItemWidth(width);
        return ImGui.Combo(label, ref selected, items, items.Length);
    }

    private static void DrawFloat(string label, ref float value)
    {
        ImGui.SetNextItemWidth(120f);
        ImGui.InputFloat(label, ref value, 0.05f, 0.5f, "%.2f");
    }

    private static void DrawSubsection(string label)
    {
        ImGui.Spacing();
        ImGui.TextUnformatted(label);
        ImGui.Indent();
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

    private enum State { Idle, CollectingAssignments, BlackHoleActive, FinalSequence, Completed }
    public enum AssignmentMode { PartyMarker, Priority, MarkerThenPriority, RoleAccretion, FixedRoleAccretion }
    private enum AssignmentQuality { Unknown, Marker, Priority, Fallback, RoleAccretion }
    private enum GuidanceKind { None, HeadStack, RoleSpread, Dubbing, AsIs, WhiteHole, Implosion, FinalCenter, FinalSpread, FinalLanding, FinalMove }
    private enum FinalStage { None, AwaitingBlizzaga, CenterBait, RoleSpread, Landing1, Landing2, ProtrudeMove }
    private enum FinalStackRole { Unknown, Support, Dps }
    private enum TargetGroup { None, Attack, Bind, Stop }
    private enum Slot { None, Attack1, Attack2, Attack3, Bind1, Bind2, Bind3, Stop1, Stop2 }
    public enum LineBaitDirection { Clockwise, Counterclockwise }
    public enum BlackHoleSourceOrder { ClockwiseFromNorth = 0, CounterclockwiseFromNorth = 1 }
    public enum BlackHoleOrderAnchor { KefkaPosition = 0, ArenaNorth = 1 }
    public enum MapMarker { A = 0, B = 1, C = 2, D = 3 }

    private readonly record struct LiveTetherEntry(ushort Id, byte Progress, uint Target);

    public sealed class Config
    {
        public AssignmentMode AssignmentMode = AssignmentMode.MarkerThenPriority;
        public LineBaitDirection LineBaitDirection = LineBaitDirection.Clockwise;
        public BlackHoleSourceOrder BlackHoleSourceOrder = BlackHoleSourceOrder.ClockwiseFromNorth;
        public BlackHoleOrderAnchor BlackHoleOrderAnchor = BlackHoleOrderAnchor.KefkaPosition;
        public bool ShowInterBlackHoleAoeGuides;
        public bool EnableBlackHoleDebugLogs = true;
        public int[] MarkerLineOrders = [0, 1, 2, 0, 1, 2, 0, 1];
        public bool ExecuteMarkerCommand;
        public float MarkerDelayMinSeconds = 0.1f;
        public float MarkerDelayMaxSeconds = 0.8f;
        public string FirstTargetCommand = "/mk attack <me>";
        public string SecondTargetCommand = "/mk bind <me>";
        public string ThirdTargetCommand = "/mk stop <me>";
        public PriorityData PriorityData = CreatePriorityData("P3 Earthquake priority",
            "Used when assignment mode is Priority or marker fallback.", DefaultRolePriority);

        public InternationalString TakeLineText = new() { En = "{0}: take {1} line", Jp = "{0}: {1}の線を取る" };
        public bool ShowTakeLineText = true;
        public InternationalString WaitingText = new() { En = "{0}: wait for Earthquake line", Jp = "{0}: 地震の線待ち" };
        public bool ShowWaitingText = true;
        public InternationalString UnknownSlotText = new() { En = "Earthquake slot unknown", Jp = "地震スロット未確定" };
        public bool ShowUnknownSlotText = true;
        public InternationalString OverlayText = new() { En = "{0}", Jp = "{0}" };
        public bool ShowOverlayText = true;
        public InternationalString HeadStackText = new() { En = "Stack", Jp = "頭割り" };
        public bool ShowHeadStackText = true;
        public InternationalString RoleSpreadText = new() { En = "Role spread", Jp = "ロール散開" };
        public bool ShowRoleSpreadText = true;
        public InternationalString DubbingText = new() { En = "Dodge Dubbing", Jp = "ダビング回避" };
        public bool ShowDubbingText = true;
        public InternationalString AsIsText = new() { En = "Dodge As-Is", Jp = "ありのまま回避" };
        public bool ShowAsIsText = true;
        public InternationalString WhiteHoleText = new() { En = "Full HP", Jp = "HP全快" };
        public bool ShowWhiteHoleText = true;
        public InternationalString ImplosionText = new() { En = "Dodge Implosion", Jp = "インプロージョン回避" };
        public bool ShowImplosionText = true;
        public InternationalString FinalCenterText = new() { En = "Center bait", Jp = "中央で誘導" };
        public bool ShowFinalCenterText = true;
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

        public void EnsureDefaults()
        {
            AssignmentMode = (AssignmentMode)Math.Clamp((int)AssignmentMode, 0, AssignmentModeNames.Length - 1);
            LineBaitDirection = (LineBaitDirection)Math.Clamp((int)LineBaitDirection, 0, 1);
            BlackHoleSourceOrder = (BlackHoleSourceOrder)Math.Clamp((int)BlackHoleSourceOrder, 0, 1);
            BlackHoleOrderAnchor = (BlackHoleOrderAnchor)Math.Clamp((int)BlackHoleOrderAnchor, 0, 1);
            PriorityData ??= CreatePriorityData("P3 Earthquake priority",
                "Used when assignment mode is Priority or marker fallback.", DefaultRolePriority);
            if (MarkerLineOrders == null || MarkerLineOrders.Length != SelectableMarkerIds.Length)
                MarkerLineOrders = DefaultMarkerLineOrders.ToArray();
            for (var i = 0; i < MarkerLineOrders.Length; i++)
                MarkerLineOrders[i] = Math.Clamp(MarkerLineOrders[i], 0, BlackHoleOrderNames.Length - 1);
            MarkerDelayMinSeconds = Math.Max(0.0f, MarkerDelayMinSeconds);
            MarkerDelayMaxSeconds = Math.Max(0.0f, MarkerDelayMaxSeconds);
            FirstTargetCommand ??= "/mk attack <me>";
            SecondTargetCommand ??= "/mk bind <me>";
            ThirdTargetCommand ??= "/mk stop <me>";
            TakeLineText ??= new InternationalString { En = "{0}: take {1} line", Jp = "{0}: {1}の線を取る" };
            WaitingText ??= new InternationalString { En = "{0}: wait for Earthquake line", Jp = "{0}: 地震の線待ち" };
            UnknownSlotText ??= new InternationalString { En = "Earthquake slot unknown", Jp = "地震スロット未確定" };
            OverlayText ??= new InternationalString { En = "{0}", Jp = "{0}" };
            HeadStackText ??= new InternationalString { En = "Stack", Jp = "頭割り" };
            RoleSpreadText ??= new InternationalString { En = "Role spread", Jp = "ロール散開" };
            DubbingText ??= new InternationalString { En = "Dodge Dubbing", Jp = "ダビング回避" };
            AsIsText ??= new InternationalString { En = "Dodge As-Is", Jp = "ありのまま回避" };
            WhiteHoleText ??= new InternationalString { En = "Full HP", Jp = "HP全快" };
            ImplosionText ??= new InternationalString { En = "Dodge Implosion", Jp = "インプロージョン回避" };
            FinalCenterText ??= new InternationalString { En = "Center bait", Jp = "中央で誘導" };
            FinalSpreadText ??= new InternationalString { En = "{0}: spread", Jp = "{0}: 散開" };
            FinalStackText ??= new InternationalString { En = "Stack center", Jp = "中央で頭割り" };
            FinalTowerText ??= new InternationalString { En = "{0}: tower", Jp = "{0}: 塔" };
            FinalMoveText ??= new InternationalString { En = "{0}: spread and keep moving", Jp = "{0}: 散開して動く" };
        }
    }
}
