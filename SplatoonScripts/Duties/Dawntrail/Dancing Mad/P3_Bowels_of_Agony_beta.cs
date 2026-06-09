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
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using Splatoon.Utility;
using DalamudBattleNpc = Dalamud.Game.ClientState.Objects.Types.IBattleNpc;
using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.IGameObject;

namespace SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate;

public class P3_Bowels_of_Agony_beta : SplatoonScript
{
    private const int MaxDisplayElements = 8;
    private const string DestinationElementPrefix = "Destination";

    private const uint TerritoryDancingMadUltimate = 1363;
    private const uint TheDecisiveBattleChaos = 49890;
    private const uint TheDecisiveBattleExdeath = 49891;
    private const uint BowelsOfAgony = 47858;
    private const uint Cyclone = 47864;
    private const uint ThunderIIILateCast = 47881;
    private const uint ThunderIIIHit = 47884;
    private const uint ThunderIIICast = 47890;
    private const uint VerticalImplosionCast = 47869;
    private const uint HorizontalImplosionCast = 47870;
    private const uint HvImplosionHit = 47871;
    private const uint UmbralSmashCast = 47872;
    private const uint VacuumWave = 47891;
    private const uint UltimateEmbrace = 49740;

    private const ushort EntropyStatus = 1600;
    private const ushort DynamicFluidStatus = 1601;
    private const ushort HeadwindStatus = 1602;
    private const ushort TailwindStatus = 1603;
    private const float ShortElementThresholdSeconds = 30.0f;
    private const float HvCorrectionAngleDegrees = 10.0f;
    private const int HvBaseSpotCount = 8;

    private const uint FireCrystalDataId = 0x001EC03A;
    private const uint WaterCrystalDataId = 0x001EC03B;
    private const uint WindCrystalDataId = 0x001EC03C;
    private const float ArenaCenterX = 100.0f;
    private const float ArenaCenterZ = 100.0f;

    private const uint MarkerColor = 0xC8FFFFFF;

    private static readonly Vector3 BaseShortCrystal = new(109.9f, 0.0f, 90.1f);
    private static readonly Vector3 BaseLongCrystal = new(90.1f, 0.0f, 109.9f);
    private static readonly Vector3 BaseWindCrystal = new(109.9f, 0.0f, 109.9f);

    private static readonly InternationalString DescriptionText = new()
    {
        En = "P3 Bowels of Agony beta coordinate helper. It detects fire/water/wind crystals and the local player's debuffs, then shows only the selected BasePlayer destination. Priority is T1/T2/H1/H2/M1/M2/R1/R2; T2 is the first Thunder III bait, and R1 is the late D3 bait.",
        Jp = "P3 バウル・オブ・アゴニー beta 座標ナビです。火/水/風クリスタルと自分のデバフを検出し、BasePlayer の行き先だけを表示します。優先順位は T1/T2/H1/H2/M1/M2/R1/R2 で、T2が最初のThunder III誘導、R1が後半D3誘導です。"
    };

    private static readonly InternationalString CoordinateText = new()
    {
        En = "Coordinates are edited in the base layout where short crystal is NE, long crystal is SW, and wind crystal is SE. H/V uses one shared base coordinate set and rotates each spot by the configured direction based on Chaos facing.",
        Jp = "座標は「短デバフ側クリスタルNE、遅デバフ側クリスタルSW、風クリスタルSE」の基準配置で編集します。H/V中は共通の基準座標を使い、各spotの設定方向へChaos基準で固定角度だけ回転補正します。"
    };

    private static readonly string[] HvCorrectionDirectionLabels = ["Clockwise", "Counterclockwise"];

    private static readonly InternationalString DisplayOptionsText = new() { En = "Display options", Jp = "表示オプション" };
    private static readonly InternationalString ShowAllCurrentStateSpotsText = new() { En = "Show all current-state spots", Jp = "現在Stateの全spotを表示する" };

    private static readonly SpotDefinition[] Spots =
    [
        new(SpotId.InitialSTThunder, "Initial.STThunder", "Initial / ST Thunder bait", new(88.33f, 111.67f)),
        new(SpotId.InitialShortTH, "Initial.ShortTH", "Initial / short element TH", new(108.49f, 91.51f)),
        new(SpotId.InitialShortDPS, "Initial.ShortDPS", "Initial / short element DPS", new(111.31f, 88.69f)),
        new(SpotId.InitialLongTH, "Initial.LongTH", "Initial / long element TH", new(104.81f, 92.08f)),
        new(SpotId.InitialLongDPS, "Initial.LongDPS", "Initial / long element DPS", new(107.92f, 95.19f)),
        new(SpotId.InitialWindTH1, "Initial.WindTH1", "Initial / wind-only TH1", new(109.10f, 114.30f)),
        new(SpotId.InitialWindTH2, "Initial.WindTH2", "Initial / wind-only TH2", new(113.20f, 110.70f)),
        new(SpotId.InitialWindDPS1, "Initial.WindDPS1", "Initial / wind-only DPS1", new(110.70f, 113.20f)),
        new(SpotId.InitialWindDPS2, "Initial.WindDPS2", "Initial / wind-only DPS2", new(114.30f, 109.10f)),
        new(SpotId.SecondShortTH, "Second.ShortTH", "Second / old short TH", new(107.29f, 95.26f)),
        new(SpotId.SecondShortDPS, "Second.ShortDPS", "Second / old short DPS", new(111.44f, 88.64f)),
        new(SpotId.SecondLongTH, "Second.LongTH", "Second / long element TH", new(114.06f, 109.00f)),
        new(SpotId.SecondLongDPS, "Second.LongDPS", "Second / long element DPS", new(112.38f, 101.70f)),
        new(SpotId.SecondWindTH1, "Second.WindTH1", "Second / wind-only TH1", new(117.33f, 105.39f)),
        new(SpotId.SecondWindTH2, "Second.WindTH2", "Second / wind-only TH2", new(110.43f, 113.66f)),
        new(SpotId.SecondWindDPS1, "Second.WindDPS1", "Second / wind-only DPS1", new(105.03f, 110.86f)),
        new(SpotId.SecondWindDPS2, "Second.WindDPS2", "Second / wind-only DPS2", new(110.38f, 100.99f)),
        new(SpotId.FirstWindMTChaos, "FirstWind.MTChaos", "First wind resolved / MT Chaos", new(100.00f, 100.00f)),
        new(SpotId.FirstWindSTExdeath, "FirstWind.STExdeath", "First wind resolved / ST Exdeath", new(111.67f, 111.67f)),
        new(SpotId.FinalBaitWindDebuff1, "FinalBait.WindDebuff1", "Final bait / wind debuff 1", new(108.90f, 115.00f)),
        new(SpotId.FinalBaitWindDebuff2, "FinalBait.WindDebuff2", "Final bait / wind debuff 2", new(103.60f, 108.80f)),
        new(SpotId.FinalBaitWindDebuff3, "FinalBait.WindDebuff3", "Final bait / wind debuff 3", new(114.80f, 108.70f)),
        new(SpotId.FinalBaitWindDebuff4, "FinalBait.WindDebuff4", "Final bait / wind debuff 4", new(108.90f, 103.50f)),
        new(SpotId.FinalBaitNoDebuff1, "FinalBait.NoDebuff1", "Final bait / no wind debuff 1", new(104.20f, 117.15f)),
        new(SpotId.FinalBaitNoDebuff2, "FinalBait.NoDebuff2", "Final bait / no wind debuff 2", new(98.55f, 108.70f)),
        new(SpotId.FinalBaitNoDebuff3, "FinalBait.NoDebuff3", "Final bait / no wind debuff 3", new(117.25f, 103.60f)),
        new(SpotId.FinalBaitNoDebuff4, "FinalBait.NoDebuff4", "Final bait / no wind debuff 4", new(113.80f, 109.25f)),
        new(SpotId.HvShortTH, "HV.ShortTH", "H/V base / old short TH", new(94.34f, 106.39f)),
        new(SpotId.HvShortDPS, "HV.ShortDPS", "H/V base / old short DPS", new(94.34f, 106.39f)),
        new(SpotId.HvLongTH, "HV.LongTH", "H/V base / long element TH", new(94.34f, 106.39f)),
        new(SpotId.HvLongDPS, "HV.LongDPS", "H/V base / long element DPS", new(94.34f, 106.39f)),
        new(SpotId.HvWindTH1, "HV.WindTH1", "H/V base / wind-only TH1", new(105.66f, 105.66f)),
        new(SpotId.HvWindTH2, "HV.WindTH2", "H/V base / wind-only TH2", new(105.66f, 105.66f)),
        new(SpotId.HvWindDPS1, "HV.WindDPS1", "H/V base / wind-only DPS1", new(105.66f, 105.66f)),
        new(SpotId.HvWindDPS2, "HV.WindDPS2", "H/V base / wind-only DPS2", new(105.66f, 105.66f)),
        new(SpotId.FinalSetupSTExdeath, "FinalSetup.STExdeath", "Final setup / ST Exdeath", new(113.00f, 113.00f)),
        new(SpotId.FinalSetupMTChaos, "FinalSetup.MTChaos", "Final setup / MT Chaos", new(113.00f, 113.00f)),
        new(SpotId.FinalSetupD3Bait, "FinalSetup.D3Bait", "Final setup / D3 bait", new(88.80f, 88.80f)),
        new(SpotId.FinalSetupParty, "FinalSetup.Party", "Final setup / party", new(105.60f, 105.40f)),
        new(SpotId.AfterLateThunderMTCenter, "AfterLateThunder.MTCenter", "After late Thunder / MT center", new(100.00f, 100.00f)),
        new(SpotId.AfterLateThunderSTCenter, "AfterLateThunder.STCenter", "After late Thunder / ST center", new(100.00f, 100.00f)),
        new(SpotId.FinalPair1, "FinalPair.Pair1", "Final pair stack / pair 1", new(104.20f, 117.15f)),
        new(SpotId.FinalPair2, "FinalPair.Pair2", "Final pair stack / pair 2", new(98.55f, 108.70f)),
        new(SpotId.FinalPair3, "FinalPair.Pair3", "Final pair stack / pair 3", new(117.25f, 103.60f)),
        new(SpotId.FinalPair4, "FinalPair.Pair4", "Final pair stack / pair 4", new(113.80f, 109.25f))
    ];

    private static readonly (string Label, SpotId First, SpotId Last)[] CoordinateGroups =
    [
        ("Initial element", SpotId.InitialSTThunder, SpotId.InitialWindDPS2),
        ("Second element", SpotId.SecondShortTH, SpotId.SecondWindDPS2),
        ("H/V base", SpotId.HvShortTH, SpotId.HvWindDPS2),
        ("First wind resolved", SpotId.FirstWindMTChaos, SpotId.FirstWindSTExdeath),
        ("After late Thunder", SpotId.AfterLateThunderMTCenter, SpotId.AfterLateThunderSTCenter),
        ("Final setup", SpotId.FinalSetupSTExdeath, SpotId.FinalSetupParty),
        ("Final bait", SpotId.FinalBaitWindDebuff1, SpotId.FinalBaitNoDebuff4),
        ("Final pair stack", SpotId.FinalPair1, SpotId.FinalPair4)
    ];

    private readonly Dictionary<uint, PlayerDebuffs> _debuffsByEntityId = [];
    private readonly Dictionary<uint, int> _finalPairIndexByEntityId = [];
    private readonly Dictionary<CrystalKind, Vector3> _crystals = [];
    private readonly HashSet<ElementTiming> _resolvedElementTimings = [];

    private bool _active;
    private bool _windResolved;
    private bool _initialThunderSeen;
    private bool _initialThunderResolved;
    private bool _lateThunderStarted;
    private int _lateThunderStHits;
    private bool _hvStarted;
    private bool _hvFirstHitResolved;
    private bool _hvSecondHitResolved;
    private bool _finalBaitStarted;
    private bool _vacuumWaveResolved;
    private bool _finalWindDebuffsCleared;
    private bool _finalCycloneResolved;
    private BowelsStep _step = BowelsStep.None;
    private ElementKind _firstRemovedElement = ElementKind.None;
    private uint _chaosEntityId;
    private uint _exdeathEntityId;
    private Vector3? _chaosPosition;
    private Vector3? _exdeathPosition;
    private float? _chaosRotation;
    private HvAxis _hvAvoidAxis = HvAxis.Unknown;
    private HvAxis _firstHvHitAxis = HvAxis.Unknown;

    public override HashSet<uint>? ValidTerritories { get; } = [TerritoryDancingMadUltimate];
    public override Metadata Metadata => new(1, "Garume");

    private Config C
    {
        get
        {
            var config = Controller.GetConfig<Config>();
            config.EnsureDefaults();
            return config;
        }
    }

    private IPlayerCharacter BasePlayer => Controller.BasePlayer;

    public override void OnSetup()
    {
        for (var i = 0; i < MaxDisplayElements; i++)
        {
            Controller.RegisterElement($"{DestinationElementPrefix}{i}", new Element(0)
            {
                Enabled = false,
                radius = 1.15f,
                thicc = 5.0f,
                fillIntensity = 0.25f,
                color = MarkerColor,
                tether = false,
                overlayText = ""
            });
        }
    }

    public override void OnCombatStart() => ResetState();
    public override void OnCombatEnd() => ResetState();
    public override void OnReset() => ResetState();

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category is DirectorUpdateCategory.Commence or DirectorUpdateCategory.Recommence or DirectorUpdateCategory.Wipe)
            ResetState();
    }

    public override void OnStartingCast(uint source, uint castId) => HandleAction(castId, source, false);

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        var actionId = set.Action?.RowId ?? 0;

        if (actionId == ThunderIIIHit)
        {
            RecordLateThunderHit(set);
            return;
        }

        if (actionId == HvImplosionHit)
        {
            AdvanceHvHit(set);
            return;
        }

        HandleAction(actionId, set.Source, true, set.Position, set.Header.RealRotation);
    }

    private void HandleAction(uint actionId, uint source, bool isEffect, Vector3? position = null, float? rotation = null)
    {
        if (actionId == UltimateEmbrace)
        {
            ResetState();
            return;
        }

        if (actionId == BowelsOfAgony)
            Change();

        if (actionId == ThunderIIICast)
        {
            CaptureExdeathFrame(source, position);
            Change(() =>
            {
                _initialThunderSeen = true;
                _initialThunderResolved |= isEffect;
            });
            return;
        }

        if (actionId == TheDecisiveBattleChaos)
        {
            CaptureChaosFrame(source, position, rotation);
            Change();
        }

        if (actionId == TheDecisiveBattleExdeath)
        {
            CaptureExdeathFrame(source, position);
            Change();
        }

        if (!_active)
            return;

        if (actionId == ThunderIIILateCast)
        {
            CaptureExdeathFrame(source, position);
            Change(() => _lateThunderStarted = true);
            return;
        }

        if (actionId == VacuumWave)
        {
            CaptureExdeathFrame(source, position);
            Change(() =>
            {
                _finalBaitStarted = true;
                _vacuumWaveResolved |= isEffect;
            });
            return;
        }

        if (actionId == UmbralSmashCast)
        {
            CaptureChaosFrame(source, position, rotation);
            Change(() => _finalBaitStarted = true);
            return;
        }

        if (TryGetHvDodgeAxis(actionId, out var hvAvoidAxis))
        {
            StartHvAvoid(source, hvAvoidAxis, position, rotation);
            return;
        }

        if (actionId == Cyclone && _finalWindDebuffsCleared && _step >= BowelsStep.FinalWindResolved)
        {
            Change(() => _finalCycloneResolved = true);
            return;
        }

        if (actionId == Cyclone)
            Change(() => _windResolved = true);
    }

    public override void OnGainBuffEffect(uint sourceId, Status status) => RecordBuff(sourceId, status);
    public override void OnUpdateBuffEffect(uint sourceId, Status status) => RecordBuff(sourceId, status);

    private void RecordBuff(uint sourceId, Status status)
    {
        if (IsBowelsStatus(status.StatusId))
            Change(() => RecordStatus(sourceId, status.StatusId, status.RemainingTime));
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status status)
    {
        if (!IsBowelsStatus(status.StatusId)) return;

        if (!_debuffsByEntityId.TryGetValue(sourceId, out var debuffs))
            return;

        var removedElement = status.StatusId switch
        {
            EntropyStatus => ElementKind.Fire,
            DynamicFluidStatus => ElementKind.Water,
            _ => ElementKind.None
        };
        var removedElementTiming = removedElement == ElementKind.None
            ? ElementTiming.Unknown
            : GetElementTiming(removedElement, debuffs);

        debuffs.Remove(status.StatusId);

        if (removedElement != ElementKind.None)
            AdvanceResolvedElement(removedElement, removedElementTiming);

        if (status.StatusId is HeadwindStatus or TailwindStatus &&
            _vacuumWaveResolved &&
            _active &&
            !_finalWindDebuffsCleared &&
            !AnyWindLeft())
            Change(() => _finalWindDebuffsCleared = true);
    }

    public override void OnObjectCreation(nint newObjectPtr)
    {
        if (_active)
            ScanCrystalObjects();
    }

    public override void OnUpdate()
    {
        ScanActiveHvCast();

        if (_active)
        {
            CapturePartyStatuses();
            ScanCrystalObjects();
            CaptureExdeathFrame();
        }

        ApplyDisplay();
    }

    public override void OnSettingsDraw()
    {
        C.EnsureDefaults();
        ImGui.TextWrapped(DescriptionText.Get());
        ImGui.Separator();
        C.PriorityData.Draw();
        ImGui.Separator();

        if (ImGui.CollapsingHeader(DisplayOptionsText.Get(), ImGuiTreeNodeFlags.DefaultOpen))
            ImGui.Checkbox(ShowAllCurrentStateSpotsText.Get(), ref C.ShowAllCurrentStateSpots);

        ImGui.Separator();

        var coordinateChanged = false;
        if (ImGui.CollapsingHeader("Coordinate templates", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextWrapped(CoordinateText.Get());
            if (ImGui.Button("Reset coordinate templates"))
            {
                C.ResetCoordinateTemplates();
                coordinateChanged = true;
            }

            foreach (var group in CoordinateGroups[..3])
                coordinateChanged |= DrawCoordinateGroup(group);

            coordinateChanged |= DrawHvCorrectionDirectionSettings();

            foreach (var group in CoordinateGroups[3..])
                coordinateChanged |= DrawCoordinateGroup(group);
        }

        if (coordinateChanged)
            ApplyDisplay();
    }

    private bool DrawCoordinateGroup((string Label, SpotId First, SpotId Last) group)
    {
        if (!ImGui.TreeNode(group.Label)) return false;

        var changed = false;
        for (var i = (int)group.First; i <= (int)group.Last; i++)
        {
            var spot = Spots[i];
            var value = C.GetPosition(spot.Id);
            ImGui.SetNextItemWidth(220f);
            if (ImGui.DragFloat2(spot.SettingLabel, ref value, 0.05f, 70f, 130f))
            {
                C.SetPosition(spot.Id, value);
                changed = true;
            }
        }

        ImGui.TreePop();
        return changed;
    }

    private bool DrawHvCorrectionDirectionSettings()
    {
        if (!ImGui.TreeNode("H/V correction direction")) return false;

        var changed = false;
        for (var i = 0; i < HvBaseSpotCount; i++)
        {
            var spot = Spots[(int)SpotId.HvShortTH + i];
            ImGui.TextUnformatted(spot.SettingLabel);

            var verticalDirection = C.GetHvCorrectionDirectionIndex(HvAxis.LeftRight, i);
            ImGui.SetNextItemWidth(150f);
            if (ImGui.Combo($"Vertical##hv-v-{i}", ref verticalDirection,
                    HvCorrectionDirectionLabels, HvCorrectionDirectionLabels.Length))
            {
                C.SetHvCorrectionDirectionIndex(HvAxis.LeftRight, i, verticalDirection);
                changed = true;
            }

            ImGui.SameLine();

            var horizontalDirection = C.GetHvCorrectionDirectionIndex(HvAxis.ForwardBack, i);
            ImGui.SetNextItemWidth(150f);
            if (ImGui.Combo($"Horizontal##hv-h-{i}", ref horizontalDirection,
                    HvCorrectionDirectionLabels, HvCorrectionDirectionLabels.Length))
            {
                C.SetHvCorrectionDirectionIndex(HvAxis.ForwardBack, i, horizontalDirection);
                changed = true;
            }
        }

        ImGui.TreePop();
        return changed;
    }

    private void Change(Action? update = null)
    {
        _active = true;
        update?.Invoke();
        RecomputeStep();
    }

    private void RecordLateThunderHit(ActionEffectSet set)
    {
        Change(() =>
        {
            _lateThunderStarted = true;
            if (TargetsPriorityPlayer(set, 1))
                _lateThunderStHits++;
        });
    }

    private void AdvanceResolvedElement(ElementKind element, ElementTiming timing = ElementTiming.Unknown)
    {
        if (element == ElementKind.None)
            return;

        if (_firstRemovedElement == ElementKind.None)
            _firstRemovedElement = element;

        if (timing == ElementTiming.Unknown)
            timing = _resolvedElementTimings.Contains(ElementTiming.Short) ? ElementTiming.Long : ElementTiming.Short;

        if (!_resolvedElementTimings.Add(timing))
            return;

        RecomputeStep();
    }

    private void StartHvAvoid(uint source, HvAxis dodgeAxis, Vector3? position = null, float? rotation = null)
    {
        if (_step >= BowelsStep.HVAvoidFirst)
            return;

        if (position.HasValue && rotation.HasValue)
            CaptureChaosFrame(source, position.Value, rotation.Value);
        else
            CaptureChaosFrame(source);

        Change(() =>
        {
            _hvStarted = true;
            _hvAvoidAxis = dodgeAxis;
            _firstHvHitAxis = HvAxis.Unknown;
        });
    }

    private void AdvanceHvHit(ActionEffectSet set)
    {
        if (!TryClassifyHvHitAxis(set.Header.RealRotation, out var axis))
            return;

        if (_step == BowelsStep.HVAvoidFirst)
        {
            _firstHvHitAxis = axis;
            _hvFirstHitResolved = true;
            RecomputeStep();
            return;
        }

        if (_step == BowelsStep.HVAvoidSecond && _firstHvHitAxis != HvAxis.Unknown && axis != _firstHvHitAxis)
        {
            _firstHvHitAxis = axis;
            _hvSecondHitResolved = true;
            RecomputeStep();
        }
    }

    private void RecomputeStep()
    {
        if (!_active || _step == BowelsStep.Done)
            return;

        var next = DetermineStep();
        if (next == _step)
            return;

        _step = next;
    }

    private BowelsStep DetermineStep()
    {
        if (!_active)
            return BowelsStep.None;

        if (_finalCycloneResolved)
            return BowelsStep.Done;

        if (_finalBaitStarted)
            return _finalWindDebuffsCleared ? BowelsStep.FinalWindResolved :
                _vacuumWaveResolved ? BowelsStep.AfterVacuumWave :
                BowelsStep.FinalBait;

        var shortElementResolved = _resolvedElementTimings.Contains(ElementTiming.Short);
        var longElementResolved = _resolvedElementTimings.Contains(ElementTiming.Long);

        var lateThunderResolved = IsLateThunderResolved();
        if (longElementResolved && _hvSecondHitResolved && lateThunderResolved)
            return BowelsStep.PostSecondElement;

        if (_hvSecondHitResolved && lateThunderResolved)
            return BowelsStep.SecondElementResolve;

        if (_hvFirstHitResolved && lateThunderResolved)
            return BowelsStep.HVAvoidSecond;

        if (_hvStarted && lateThunderResolved)
            return BowelsStep.HVAvoidFirst;

        if (lateThunderResolved)
            return BowelsStep.AfterLateThunder;

        if (_lateThunderStarted || (_windResolved && shortElementResolved))
            return BowelsStep.FirstWindResolved;

        if (_initialThunderSeen && !_initialThunderResolved)
            return BowelsStep.InitialThunderCasting;

        if (shortElementResolved)
            return BowelsStep.FirstElementResolved;

        if (_initialThunderResolved)
            return BowelsStep.AfterThunder;

        return BowelsStep.InitialElement;
    }

    private bool IsLateThunderResolved()
    {
        return _lateThunderStarted && _lateThunderStHits >= 2;
    }

    private void CapturePartyStatuses()
    {
        foreach (var player in Controller.GetPartyMembers().OfType<IPlayerCharacter>())
        {
            foreach (var status in player.StatusList)
            {
                if (IsBowelsStatus(status.StatusId))
                    RecordStatus(player.EntityId, status.StatusId, status.RemainingTime);
            }
        }
    }

    private void RecordStatus(uint playerId, uint statusId, float remainingTime)
    {
        if (!_debuffsByEntityId.TryGetValue(playerId, out var debuffs))
        {
            debuffs = new PlayerDebuffs();
            _debuffsByEntityId[playerId] = debuffs;
        }

        debuffs.Record(statusId, remainingTime);
    }

    private void ScanCrystalObjects()
    {
        foreach (var gameObject in global::ECommons.DalamudServices.Svc.Objects)
            TryCaptureCrystal(gameObject);
    }

    private void TryCaptureCrystal(DalamudGameObject gameObject)
    {
        if (!TryGetCrystalKind(gameObject.DataId, out var kind))
            return;

        _crystals[kind] = gameObject.Position;
    }

    private void ScanActiveHvCast()
    {
        if (_step >= BowelsStep.HVAvoidFirst)
            return;

        foreach (var npc in global::ECommons.DalamudServices.Svc.Objects.OfType<DalamudBattleNpc>())
        {
            uint actionId;
            try
            {
                if (!npc.IsCasting)
                    continue;

                actionId = npc.CastInfo.ActionId != 0 ? npc.CastInfo.ActionId : npc.CastActionId;
            }
            catch (NullReferenceException)
            {
                continue;
            }

            if (!TryGetHvDodgeAxis(actionId, out var dodgeAxis))
                continue;

            StartHvAvoid(npc.EntityId, dodgeAxis);
            return;
        }
    }

    private List<UniversalPartyMember> GetPriorityPlayers()
    {
        return C.PriorityData.GetPlayers(_ => true)?
            .Where(member => member.IGameObject is IPlayerCharacter)
            .ToList() ?? [];
    }

    private bool TargetsPriorityPlayer(ActionEffectSet set, int priorityIndex)
    {
        var priorityMembers = GetPriorityPlayers();
        if (priorityIndex >= 0 &&
            priorityIndex < priorityMembers.Count &&
            priorityMembers[priorityIndex].IGameObject is IPlayerCharacter priorityPlayer)
            return set.TargetEffects.Any(target => (uint)target.TargetID == priorityPlayer.EntityId);

        return false;
    }

    private bool TryResolveSpot(
        IPlayerCharacter player,
        PlayerDebuffs debuffs,
        IReadOnlyList<UniversalPartyMember> priorityMembers,
        out ResolvedSpot resolved)
    {
        resolved = default;
        var priorityIndex = GetPriorityIndex(player, priorityMembers);

        switch (_step)
        {
            case BowelsStep.InitialElement when priorityIndex == 1:
                return Resolve(SpotId.InitialSTThunder, out resolved);
            case BowelsStep.InitialElement:
            case BowelsStep.InitialThunderCasting:
            case BowelsStep.AfterThunder:
            case BowelsStep.FirstElementResolved:
                return TryResolveElementOrWindSpot(player, debuffs, priorityMembers, true, out resolved);
            case BowelsStep.FirstWindResolved:
                return TryResolveBossControlSpot(priorityIndex, out resolved) ||
                    TryResolveElementOrWindSpot(player, debuffs, priorityMembers, false, out resolved);
            case BowelsStep.AfterLateThunder when priorityIndex == 0:
                return Resolve(SpotId.AfterLateThunderMTCenter, out resolved);
            case BowelsStep.AfterLateThunder when priorityIndex == 1:
                return Resolve(SpotId.AfterLateThunderSTCenter, out resolved);
            case BowelsStep.AfterLateThunder:
                return TryResolveElementOrWindSpot(player, debuffs, priorityMembers, false, out resolved);
            case BowelsStep.HVAvoidFirst:
            case BowelsStep.HVAvoidSecond:
                return TryResolveHvSpot(player, debuffs, priorityMembers, GetCurrentHvAxis(), out resolved);
            case BowelsStep.SecondElementResolve:
                return TryResolveElementOrWindSpot(player, debuffs, priorityMembers, false, out resolved);
            case BowelsStep.PostSecondElement:
                return Resolve(GetFinalSetupSpot(priorityIndex), out resolved);
            case BowelsStep.FinalBait:
                return TryResolveFinalBaitSpot(player, debuffs, priorityMembers, out resolved);
            case BowelsStep.AfterVacuumWave:
            case BowelsStep.FinalWindResolved:
                return TryResolveFinalPairSpot(player, debuffs, priorityMembers, out resolved);
            default:
                return false;
        }
    }

    private bool TryResolveElementOrWindSpot(
        IPlayerCharacter player,
        PlayerDebuffs debuffs,
        IReadOnlyList<UniversalPartyMember> priorityMembers,
        bool initial,
        out ResolvedSpot resolved)
    {
        resolved = default;
        return TryGetElementOrWindSpot(player, debuffs, priorityMembers, initial, out var spot) &&
            Resolve(spot, out resolved);
    }

    private bool TryGetElementOrWindSpot(
        IPlayerCharacter player,
        PlayerDebuffs debuffs,
        IReadOnlyList<UniversalPartyMember> priorityMembers,
        bool initial,
        out SpotId spot)
    {
        if (debuffs.Element != ElementKind.None)
        {
            var timing = GetElementTiming(debuffs.Element, debuffs);
            if (timing == ElementTiming.Unknown)
            {
                spot = default;
                return false;
            }

            var isShort = timing == ElementTiming.Short;
            var isDps = player.GetRole() == CombatRole.DPS;
            var firstSpot = initial ? SpotId.InitialShortTH : SpotId.SecondShortTH;
            spot = (SpotId)((int)firstSpot + (isShort ? 0 : 2) + (isDps ? 1 : 0));
            return true;
        }

        return TryGetWindOnlySpot(player, priorityMembers, initial, out spot);
    }

    private static SpotId GetFinalSetupSpot(int priorityIndex)
    {
        return priorityIndex switch
        {
            0 => SpotId.FinalSetupMTChaos,
            1 => SpotId.FinalSetupSTExdeath,
            6 => SpotId.FinalSetupD3Bait,
            _ => SpotId.FinalSetupParty
        };
    }

    private bool TryResolveFinalBaitSpot(
        IPlayerCharacter player,
        PlayerDebuffs debuffs,
        IReadOnlyList<UniversalPartyMember> priorityMembers,
        out ResolvedSpot resolved)
    {
        var hasWind = debuffs.HasWind;
        var groupIndex = GetFinalBaitGroupIndex(player, hasWind, priorityMembers);
        if (groupIndex < 0 || groupIndex >= 4)
        {
            resolved = default;
            return false;
        }

        CacheFinalPairIndex(player, groupIndex);
        var firstSpot = hasWind ? SpotId.FinalBaitWindDebuff1 : SpotId.FinalBaitNoDebuff1;
        return Resolve((SpotId)((int)firstSpot + groupIndex), out resolved);
    }

    private int GetFinalBaitGroupIndex(
        IPlayerCharacter player,
        bool hasWind,
        IReadOnlyList<UniversalPartyMember> priorityMembers)
    {
        var index = 0;
        foreach (var member in priorityMembers)
        {
            if (member.IGameObject is not IPlayerCharacter priorityPlayer)
                continue;

            if (!TryGetDebuffs(priorityPlayer, out var priorityDebuffs))
                return -1;

            if (priorityDebuffs.HasWind != hasWind)
                continue;

            if (priorityPlayer.EntityId == player.EntityId)
                return index;

            index++;
        }

        return -1;
    }

    private bool TryResolveFinalPairSpot(
        IPlayerCharacter player,
        PlayerDebuffs debuffs,
        IReadOnlyList<UniversalPartyMember> priorityMembers,
        out ResolvedSpot resolved)
    {
        if (!TryGetFinalPairIndex(player, out var pairIndex))
        {
            if (_finalWindDebuffsCleared)
            {
                resolved = default;
                return false;
            }

            pairIndex = GetFinalBaitGroupIndex(player, debuffs.HasWind, priorityMembers);
            if (pairIndex >= 0 && pairIndex < 4)
                CacheFinalPairIndex(player, pairIndex);
        }

        if (pairIndex < 0 || pairIndex >= 4)
        {
            resolved = default;
            return false;
        }

        return Resolve((SpotId)((int)SpotId.FinalPair1 + pairIndex), out resolved);
    }

    private void CacheFinalPairIndex(IPlayerCharacter player, int pairIndex)
    {
        _finalPairIndexByEntityId[player.EntityId] = pairIndex;
    }

    private bool TryGetFinalPairIndex(IPlayerCharacter player, out int pairIndex)
    {
        return _finalPairIndexByEntityId.TryGetValue(player.EntityId, out pairIndex);
    }

    private bool TryResolveBossControlSpot(int priorityIndex, out ResolvedSpot resolved)
    {
        if (priorityIndex == 0)
            return Resolve(SpotId.FirstWindMTChaos, out resolved, new Vector3(ArenaCenterX, 0.0f, ArenaCenterZ));

        if (priorityIndex == 1)
        {
            if (TryGetExdeathPosition(out var exdeathPosition))
                return Resolve(SpotId.FirstWindSTExdeath, out resolved, exdeathPosition);

            return Resolve(SpotId.FirstWindSTExdeath, out resolved);
        }

        resolved = default;
        return false;
    }

    private bool TryResolveHvSpot(
        IPlayerCharacter player,
        PlayerDebuffs debuffs,
        IReadOnlyList<UniversalPartyMember> priorityMembers,
        HvAxis axis,
        out ResolvedSpot resolved)
    {
        resolved = default;
        if (axis == HvAxis.Unknown)
            return false;

        if (!TryGetElementOrWindSpot(player, debuffs, priorityMembers, false, out var spot))
            return false;

        var hvIndex = (int)spot - (int)SpotId.SecondShortTH;
        if (hvIndex is < 0 or >= HvBaseSpotCount)
            return false;

        var hvSpot = (SpotId)((int)SpotId.HvShortTH + hvIndex);
        var definition = Spots[(int)hvSpot];
        var basePosition = TransformFromCurrentCrystals(C.GetPosition(hvSpot));
        var direction = C.GetHvCorrectionDirection(axis, hvIndex);
        var position = ApplyHvCorrection(basePosition, direction);
        resolved = new ResolvedSpot($"{definition.Key}.{axis}.{direction}", position);
        return true;
    }

    private bool TryGetWindOnlySpot(
        IPlayerCharacter player,
        IReadOnlyList<UniversalPartyMember> priorityMembers,
        bool initial,
        out SpotId spot)
    {
        var isDps = player.GetRole() == CombatRole.DPS;
        var rank = priorityMembers
            .Where(member => member.IGameObject is IPlayerCharacter pc && (pc.GetRole() == CombatRole.DPS) == isDps)
            .Where(member => member.IGameObject is IPlayerCharacter pc &&
                TryGetDebuffs(pc, out var debuffs) && debuffs.Element == ElementKind.None)
            .Select(member => ((IPlayerCharacter)member.IGameObject).EntityId)
            .ToList()
            .IndexOf(player.EntityId);

        if (rank is >= 0 and < 2)
        {
            var firstSpot = initial ? SpotId.InitialWindTH1 : SpotId.SecondWindTH1;
            spot = (SpotId)((int)firstSpot + (isDps ? 2 : 0) + rank);
            return true;
        }

        spot = default;
        return false;
    }

    private bool Resolve(SpotId spotId, out ResolvedSpot resolved, Vector3? directPosition = null)
    {
        var definition = Spots[(int)spotId];
        var position = directPosition ?? TransformFromCurrentCrystals(C.GetPosition(spotId));
        resolved = new ResolvedSpot(definition.Key, position);
        return true;
    }

    private Vector3 ApplyHvCorrection(Vector3 basePosition, HvCorrectionDirection direction)
    {
        if (!TryGetChaosFrame(out var chaosPosition, out var rotation))
            return basePosition;

        var relativeToChaos = new Vector2(basePosition.X - chaosPosition.X, basePosition.Z - chaosPosition.Z);
        var radius = relativeToChaos.Length();
        if (radius < 0.01f)
            return basePosition;

        var forward = ForwardFromRotation(rotation);
        var right = RightFromForward(forward);
        var currentAngle = MathF.Atan2(
            Vector2.Dot(relativeToChaos, right),
            Vector2.Dot(relativeToChaos, forward));
        var correctedAngle = ApplyHvAngleOffset(currentAngle, direction);

        if (float.IsNaN(correctedAngle))
            return basePosition;

        var correctedRelative =
            right * (MathF.Sin(correctedAngle) * radius) +
            forward * (MathF.Cos(correctedAngle) * radius);
        var corrected = new Vector2(chaosPosition.X, chaosPosition.Z) + correctedRelative;
        return new Vector3(corrected.X, basePosition.Y, corrected.Y);
    }

    private static float ApplyHvAngleOffset(float angle, HvCorrectionDirection direction)
    {
        var offset = DegreesToRadians(HvCorrectionAngleDegrees);
        return NormalizeRadians(direction == HvCorrectionDirection.Clockwise
            ? angle + offset
            : angle - offset);
    }

    private static float DegreesToRadians(float degrees)
    {
        return degrees * MathF.PI / 180.0f;
    }

    private static float NormalizeRadians(float angle)
    {
        const float twoPi = MathF.PI * 2.0f;
        while (angle <= -MathF.PI)
            angle += twoPi;
        while (angle > MathF.PI)
            angle -= twoPi;
        return angle;
    }

    private HvAxis GetCurrentHvAxis()
    {
        return _step switch
        {
            BowelsStep.HVAvoidFirst => _hvAvoidAxis,
            BowelsStep.HVAvoidSecond => OppositeAxis(_hvAvoidAxis),
            _ => HvAxis.Unknown
        };
    }

    private ElementTiming GetElementTiming(ElementKind element, PlayerDebuffs debuffs)
    {
        var fireMax = _debuffsByEntityId.Values.Where(x => x.EntropyRemaining > 0f)
            .Select(x => x.EntropyRemaining).DefaultIfEmpty(0f).Max();
        var waterMax = _debuffsByEntityId.Values.Where(x => x.DynamicFluidRemaining > 0f)
            .Select(x => x.DynamicFluidRemaining).DefaultIfEmpty(0f).Max();

        if (fireMax > 0f && waterMax > 0f && Math.Abs(fireMax - waterMax) > 5f)
        {
            return element switch
            {
                ElementKind.Fire => fireMax < waterMax ? ElementTiming.Short : ElementTiming.Long,
                ElementKind.Water => waterMax < fireMax ? ElementTiming.Short : ElementTiming.Long,
                _ => ElementTiming.Unknown
            };
        }

        if (_firstRemovedElement != ElementKind.None)
            return element == _firstRemovedElement ? ElementTiming.Short : ElementTiming.Long;

        var remaining = element == ElementKind.Fire ? debuffs.EntropyRemaining : debuffs.DynamicFluidRemaining;
        if (remaining <= 0f) return ElementTiming.Unknown;
        return remaining < ShortElementThresholdSeconds ? ElementTiming.Short : ElementTiming.Long;
    }

    private Vector3 TransformFromCurrentCrystals(Vector2 basePosition)
    {
        var position = new Vector3(basePosition.X, 0.0f, basePosition.Y);
        return TryGetCurrentCrystalBasis(out var shortCrystal, out var longCrystal, out var windCrystal) &&
            TryTransform(position, shortCrystal, longCrystal, windCrystal, out var transformed)
            ? transformed
            : position;
    }

    private bool TryGetChaosFrame(out Vector3 position, out float rotation)
    {
        if (TryGetTrackedObject(_chaosEntityId, out var chaos))
        {
            CaptureChaosFrame(chaos.EntityId, chaos.Position, chaos.Rotation);
            position = _chaosPosition!.Value;
            rotation = _chaosRotation!.Value;
            return true;
        }

        if (_chaosPosition.HasValue && _chaosRotation.HasValue)
        {
            position = _chaosPosition.Value;
            rotation = _chaosRotation.Value;
            return true;
        }

        position = default;
        rotation = default;
        return false;
    }

    private void CaptureChaosFrame(uint source, Vector3? position = null, float? rotation = null)
    {
        if (!position.HasValue || !rotation.HasValue)
        {
            if (!TryGetTrackedObject(source, out var obj))
            {
                if (source != 0)
                    _chaosEntityId = source;
                return;
            }

            source = obj.EntityId;
            position = obj.Position;
            rotation = obj.Rotation;
        }

        _chaosEntityId = source;
        _chaosPosition = position.Value;
        _chaosRotation = rotation.Value;
    }

    private bool TryGetExdeathPosition(out Vector3 position)
    {
        CaptureExdeathFrame();

        if (_exdeathPosition.HasValue)
        {
            position = _exdeathPosition.Value;
            return true;
        }

        position = default;
        return false;
    }

    private bool CaptureExdeathFrame(uint source = 0, Vector3? position = null)
    {
        if (!position.HasValue)
        {
            if ((source == 0 || !TryGetTrackedObject(source, out var obj)) &&
                !TryGetTrackedObject(_exdeathEntityId, out obj) &&
                !TryFindExdeathObject(out obj))
            {
                if (source != 0)
                    _exdeathEntityId = source;
                return false;
            }

            source = obj.EntityId;
            position = obj.Position;
        }

        if (source != 0)
            _exdeathEntityId = source;

        _exdeathPosition = position.Value;
        return true;
    }

    private bool TryClassifyHvHitAxis(float hitRotation, out HvAxis axis)
    {
        axis = HvAxis.Unknown;
        if (!TryGetChaosFrame(out _, out var chaosRotation))
            return false;

        var hit = ForwardFromRotation(hitRotation);
        var chaosForward = ForwardFromRotation(chaosRotation);
        var chaosRight = RightFromForward(chaosForward);
        var forwardDot = MathF.Abs(Vector2.Dot(hit, chaosForward));
        var rightDot = MathF.Abs(Vector2.Dot(hit, chaosRight));
        axis = forwardDot >= rightDot ? HvAxis.ForwardBack : HvAxis.LeftRight;
        return true;
    }

    private static Vector2 ForwardFromRotation(float rotation)
    {
        var vector = new Vector2(MathF.Sin(rotation), MathF.Cos(rotation));
        return Vector2.Normalize(vector);
    }

    private static Vector2 RightFromForward(Vector2 forward)
    {
        return new Vector2(forward.Y, -forward.X);
    }

    private bool TryGetCurrentCrystalBasis(out Vector3 shortCrystal, out Vector3 longCrystal, out Vector3 windCrystal)
    {
        shortCrystal = default;
        longCrystal = default;
        windCrystal = default;

        var shortElement = GetShortElement();
        if (shortElement == ElementKind.None)
            return false;

        var longElement = shortElement == ElementKind.Fire ? ElementKind.Water : ElementKind.Fire;
        if (!_crystals.TryGetValue((CrystalKind)shortElement, out shortCrystal) ||
            !_crystals.TryGetValue((CrystalKind)longElement, out longCrystal) ||
            !_crystals.TryGetValue(CrystalKind.Wind, out windCrystal))
            return false;

        return Math.Abs(Determinant(
            new Vector2(longCrystal.X - shortCrystal.X, longCrystal.Z - shortCrystal.Z),
            new Vector2(windCrystal.X - shortCrystal.X, windCrystal.Z - shortCrystal.Z))) > 0.001f;
    }

    private ElementKind GetShortElement()
    {
        var fire = _debuffsByEntityId.Values.FirstOrDefault(x => x.Element == ElementKind.Fire);
        if (fire != null && GetElementTiming(ElementKind.Fire, fire) == ElementTiming.Short)
            return ElementKind.Fire;

        var water = _debuffsByEntityId.Values.FirstOrDefault(x => x.Element == ElementKind.Water);
        return water != null && GetElementTiming(ElementKind.Water, water) == ElementTiming.Short
            ? ElementKind.Water
            : ElementKind.None;
    }

    private static bool TryTransform(Vector3 basePosition, Vector3 targetShort, Vector3 targetLong,
        Vector3 targetWind, out Vector3 transformed)
    {
        transformed = default;
        var baseLong = new Vector2(BaseLongCrystal.X - BaseShortCrystal.X, BaseLongCrystal.Z - BaseShortCrystal.Z);
        var baseWind = new Vector2(BaseWindCrystal.X - BaseShortCrystal.X, BaseWindCrystal.Z - BaseShortCrystal.Z);
        var basePoint = new Vector2(basePosition.X - BaseShortCrystal.X, basePosition.Z - BaseShortCrystal.Z);
        var determinant = Determinant(baseLong, baseWind);
        if (Math.Abs(determinant) < 0.001f)
            return false;

        var longWeight = Determinant(basePoint, baseWind) / determinant;
        var windWeight = Determinant(baseLong, basePoint) / determinant;
        var target = new Vector2(targetShort.X, targetShort.Z) +
            new Vector2(targetLong.X - targetShort.X, targetLong.Z - targetShort.Z) * longWeight +
            new Vector2(targetWind.X - targetShort.X, targetWind.Z - targetShort.Z) * windWeight;

        transformed = new Vector3(target.X, basePosition.Y, target.Y);
        return true;
    }

    private void ApplyDisplay()
    {
        for (var i = 0; i < MaxDisplayElements; i++)
        {
            if (Controller.TryGetElementByName($"{DestinationElementPrefix}{i}", out var marker))
                marker.Enabled = false;
        }

        if (!_active)
            return;

        var displayIndex = 0;
        var basePlayer = BasePlayer;
        var highlightedLabel = "";
        if (basePlayer != null && TryResolvePlayerSpot(basePlayer, out var baseSpot))
        {
            highlightedLabel = baseSpot.Label;
            ApplySpot(ref displayIndex, baseSpot, true);
        }

        if (!C.ShowAllCurrentStateSpots)
            return;

        var shown = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(highlightedLabel))
            shown.Add(highlightedLabel);

        foreach (var member in GetPriorityPlayers())
        {
            if (member.IGameObject is not IPlayerCharacter player ||
                basePlayer != null && player.EntityId == basePlayer.EntityId ||
                !TryResolvePlayerSpot(player, out var spot) ||
                !shown.Add(spot.Label))
                continue;

            ApplySpot(ref displayIndex, spot, false);
        }
    }

    private void ApplySpot(ref int displayIndex, ResolvedSpot spot, bool highlighted)
    {
        if (displayIndex >= MaxDisplayElements ||
            !Controller.TryGetElementByName($"{DestinationElementPrefix}{displayIndex++}", out var marker))
            return;

        marker.Enabled = true;
        marker.SetRefPosition(spot.Position);
        marker.color = highlighted ? RainbowColor() : MarkerColor;
        marker.tether = highlighted;
        marker.overlayText = C.ShowAllCurrentStateSpots ? spot.Label : "";
    }

    private bool TryResolvePlayerSpot(IPlayerCharacter player, out ResolvedSpot resolved)
    {
        resolved = default;
        if (_step is BowelsStep.None or BowelsStep.Done)
            return false;

        var priorityMembers = GetPriorityPlayers();
        return priorityMembers.Count > 0 &&
            TryGetDebuffs(player, out var debuffs) &&
            TryResolveSpot(player, debuffs, priorityMembers, out resolved);
    }

    private bool TryGetDebuffs(IPlayerCharacter player, out PlayerDebuffs debuffs)
    {
        return _debuffsByEntityId.TryGetValue(player.EntityId, out debuffs!);
    }

    private bool AnyWindLeft()
    {
        CapturePartyStatuses();
        return _debuffsByEntityId.Values.Any(debuffs => debuffs.HasWind);
    }

    private void ResetState()
    {
        _active = false;
        _windResolved = false;
        _initialThunderSeen = false;
        _initialThunderResolved = false;
        _lateThunderStarted = false;
        _lateThunderStHits = 0;
        _hvStarted = false;
        _hvFirstHitResolved = false;
        _hvSecondHitResolved = false;
        _finalBaitStarted = false;
        _vacuumWaveResolved = false;
        _finalWindDebuffsCleared = false;
        _finalCycloneResolved = false;
        _step = BowelsStep.None;
        _firstRemovedElement = ElementKind.None;
        _chaosEntityId = 0;
        _exdeathEntityId = 0;
        _chaosPosition = null;
        _exdeathPosition = null;
        _chaosRotation = null;
        _hvAvoidAxis = HvAxis.Unknown;
        _firstHvHitAxis = HvAxis.Unknown;
        _debuffsByEntityId.Clear();
        _finalPairIndexByEntityId.Clear();
        _crystals.Clear();
        _resolvedElementTimings.Clear();

        foreach (var registered in Controller.GetRegisteredElements())
            registered.Value.Enabled = false;
    }

    private static bool IsBowelsStatus(uint statusId)
    {
        return statusId is EntropyStatus or DynamicFluidStatus or HeadwindStatus or TailwindStatus;
    }

    private static int GetPriorityIndex(
        IPlayerCharacter player,
        IReadOnlyList<UniversalPartyMember> priorityMembers)
    {
        for (var i = 0; i < priorityMembers.Count; i++)
        {
            if (priorityMembers[i].IGameObject is IPlayerCharacter priorityPlayer &&
                priorityPlayer.EntityId == player.EntityId)
                return i;
        }

        return -1;
    }

    private static bool TryGetCrystalKind(uint dataId, out CrystalKind kind)
    {
        kind = dataId switch
        {
            FireCrystalDataId => CrystalKind.Fire,
            WaterCrystalDataId => CrystalKind.Water,
            WindCrystalDataId => CrystalKind.Wind,
            _ => CrystalKind.None
        };

        return kind != CrystalKind.None;
    }

    private static bool TryGetHvDodgeAxis(uint actionId, out HvAxis axis)
    {
        axis = actionId switch
        {
            VerticalImplosionCast => HvAxis.LeftRight,
            HorizontalImplosionCast => HvAxis.ForwardBack,
            _ => HvAxis.Unknown
        };

        return axis != HvAxis.Unknown;
    }

    private static HvAxis OppositeAxis(HvAxis axis)
    {
        return axis switch
        {
            HvAxis.ForwardBack => HvAxis.LeftRight,
            HvAxis.LeftRight => HvAxis.ForwardBack,
            _ => HvAxis.Unknown
        };
    }

    private static bool TryFindExdeathObject(out DalamudGameObject obj)
    {
        foreach (var gameObject in global::ECommons.DalamudServices.Svc.Objects)
        {
            var name = gameObject.Name.ToString();
            if (name.Contains("Exdeath", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("エクスデス", StringComparison.OrdinalIgnoreCase))
            {
                obj = gameObject;
                return true;
            }
        }

        obj = null!;
        return false;
    }

    private static bool TryGetTrackedObject(uint entityId, out DalamudGameObject obj)
    {
        if (entityId != 0 && entityId.GetObject() is { } gameObject)
        {
            obj = gameObject;
            return true;
        }

        obj = null!;
        return false;
    }

    private static float Determinant(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }

    private static uint RainbowColor()
    {
        return Utils.HsvToVector4(ImGui.GetTime() * 0.35 % 1.0, 0.85, 1.0).ToUint();
    }

    private enum BowelsStep { None, InitialElement, InitialThunderCasting, AfterThunder, FirstElementResolved, FirstWindResolved, AfterLateThunder, HVAvoidFirst, HVAvoidSecond, SecondElementResolve, PostSecondElement, FinalBait, AfterVacuumWave, FinalWindResolved, Done }

    public enum SpotId
    {
        InitialSTThunder, InitialShortTH, InitialShortDPS, InitialLongTH, InitialLongDPS,
        InitialWindTH1, InitialWindTH2, InitialWindDPS1, InitialWindDPS2,
        SecondShortTH, SecondShortDPS, SecondLongTH, SecondLongDPS,
        SecondWindTH1, SecondWindTH2, SecondWindDPS1, SecondWindDPS2,
        FirstWindMTChaos, FirstWindSTExdeath,
        FinalBaitWindDebuff1, FinalBaitWindDebuff2, FinalBaitWindDebuff3, FinalBaitWindDebuff4,
        FinalBaitNoDebuff1, FinalBaitNoDebuff2, FinalBaitNoDebuff3, FinalBaitNoDebuff4,
        HvShortTH, HvShortDPS, HvLongTH, HvLongDPS, HvWindTH1, HvWindTH2, HvWindDPS1, HvWindDPS2,
        FinalSetupSTExdeath, FinalSetupMTChaos, FinalSetupD3Bait, FinalSetupParty,
        AfterLateThunderMTCenter, AfterLateThunderSTCenter,
        FinalPair1, FinalPair2, FinalPair3, FinalPair4
    }

    private enum ElementKind { None, Fire, Water }
    private enum ElementTiming { Unknown, Short, Long }
    private enum CrystalKind { None, Fire, Water, Wind }
    public enum HvAxis { Unknown, ForwardBack, LeftRight }
    public enum HvCorrectionDirection { Clockwise, Counterclockwise }

    private readonly record struct SpotDefinition(SpotId Id, string Key, string SettingLabel, Vector2 DefaultPosition);
    private readonly record struct ResolvedSpot(string Label, Vector3 Position);
    private sealed class PlayerDebuffs
    {
        public bool Entropy { get; private set; }
        public bool DynamicFluid { get; private set; }
        public bool Headwind { get; private set; }
        public bool Tailwind { get; private set; }
        public float EntropyRemaining { get; private set; }
        public float DynamicFluidRemaining { get; private set; }
        public bool HasWind => Headwind || Tailwind;
        public ElementKind Element => Entropy ? ElementKind.Fire : DynamicFluid ? ElementKind.Water : ElementKind.None;

        public void Record(uint statusId, float remaining)
        {
            switch (statusId)
            {
                case EntropyStatus:
                    Entropy = true;
                    EntropyRemaining = Math.Max(EntropyRemaining, remaining);
                    break;
                case DynamicFluidStatus:
                    DynamicFluid = true;
                    DynamicFluidRemaining = Math.Max(DynamicFluidRemaining, remaining);
                    break;
                case HeadwindStatus:
                    Headwind = true;
                    break;
                case TailwindStatus:
                    Tailwind = true;
                    break;
            }
        }

        public void Remove(uint statusId)
        {
            switch (statusId)
            {
                case EntropyStatus:
                    EntropyRemaining = 0.0f;
                    break;
                case DynamicFluidStatus:
                    DynamicFluidRemaining = 0.0f;
                    break;
                case HeadwindStatus:
                    Headwind = false;
                    break;
                case TailwindStatus:
                    Tailwind = false;
                    break;
            }
        }
    }

    public sealed class Config : IEzConfig
    {
        public PriorityData PriorityData = new()
        {
            Name = "P3 Bowels priority",
            Description = "T1/T2/H1/H2/M1/M2/R1/R2. T2 baits first Thunder III, R1 handles late D3 bait.",
            PriorityLists =
            [
                new PriorityList
                {
                    IsRole = true,
                    List =
                    [
                        new JobbedPlayer { Role = RolePosition.T1 },
                        new JobbedPlayer { Role = RolePosition.T2 },
                        new JobbedPlayer { Role = RolePosition.H1 },
                        new JobbedPlayer { Role = RolePosition.H2 },
                        new JobbedPlayer { Role = RolePosition.M1 },
                        new JobbedPlayer { Role = RolePosition.M2 },
                        new JobbedPlayer { Role = RolePosition.R1 },
                        new JobbedPlayer { Role = RolePosition.R2 }
                    ]
                }
            ]
        };

        public Vector2[] Positions = CreateDefaultPositions();
        public int[] VerticalHvCorrectionDirections = CreateDefaultHvCorrectionDirections(HvAxis.LeftRight);
        public int[] HorizontalHvCorrectionDirections = CreateDefaultHvCorrectionDirections(HvAxis.ForwardBack);
        public bool ShowAllCurrentStateSpots;

        public void EnsureDefaults()
        {
            PriorityData ??= new PriorityData();
            if (Positions == null || Positions.Length != Spots.Length)
                Positions = CreateDefaultPositions();

            EnsureHvCorrectionDirections();
        }

        public Vector2 GetPosition(SpotId id)
        {
            EnsureDefaults();
            return Positions[(int)id];
        }

        public void SetPosition(SpotId id, Vector2 value)
        {
            EnsureDefaults();
            Positions[(int)id] = value;
        }

        public void ResetCoordinateTemplates()
        {
            Positions = CreateDefaultPositions();
            ResetHvCorrectionDirections();
        }

        public HvCorrectionDirection GetHvCorrectionDirection(HvAxis axis, int index)
        {
            var directionIndex = GetHvCorrectionDirectionIndex(axis, index);
            return directionIndex == (int)HvCorrectionDirection.Counterclockwise
                ? HvCorrectionDirection.Counterclockwise
                : HvCorrectionDirection.Clockwise;
        }

        public int GetHvCorrectionDirectionIndex(HvAxis axis, int index)
        {
            EnsureHvCorrectionDirections();
            var directions = axis == HvAxis.LeftRight
                ? VerticalHvCorrectionDirections
                : HorizontalHvCorrectionDirections;
            return directions[Math.Clamp(index, 0, HvBaseSpotCount - 1)];
        }

        public void SetHvCorrectionDirectionIndex(HvAxis axis, int index, int value)
        {
            EnsureHvCorrectionDirections();
            var directions = axis == HvAxis.LeftRight
                ? VerticalHvCorrectionDirections
                : HorizontalHvCorrectionDirections;
            directions[Math.Clamp(index, 0, HvBaseSpotCount - 1)] =
                Math.Clamp(value, 0, HvCorrectionDirectionLabels.Length - 1);
        }

        private void EnsureHvCorrectionDirections()
        {
            if (VerticalHvCorrectionDirections == null ||
                VerticalHvCorrectionDirections.Length != HvBaseSpotCount)
                VerticalHvCorrectionDirections = CreateDefaultHvCorrectionDirections(HvAxis.LeftRight);

            if (HorizontalHvCorrectionDirections == null ||
                HorizontalHvCorrectionDirections.Length != HvBaseSpotCount)
                HorizontalHvCorrectionDirections = CreateDefaultHvCorrectionDirections(HvAxis.ForwardBack);
        }

        private void ResetHvCorrectionDirections()
        {
            VerticalHvCorrectionDirections = CreateDefaultHvCorrectionDirections(HvAxis.LeftRight);
            HorizontalHvCorrectionDirections = CreateDefaultHvCorrectionDirections(HvAxis.ForwardBack);
        }

        private static Vector2[] CreateDefaultPositions()
        {
            return Spots.Select(spot => spot.DefaultPosition).ToArray();
        }

        private static int[] CreateDefaultHvCorrectionDirections(HvAxis axis)
        {
            var first = axis == HvAxis.LeftRight ? (int)HvCorrectionDirection.Clockwise : (int)HvCorrectionDirection.Counterclockwise;
            var second = axis == HvAxis.LeftRight ? (int)HvCorrectionDirection.Counterclockwise : (int)HvCorrectionDirection.Clockwise;
            return [first, first, first, first, second, second, second, second];
        }
    }
}
