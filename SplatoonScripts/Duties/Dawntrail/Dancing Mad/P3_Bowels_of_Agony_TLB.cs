using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

internal class P3_Bowels_of_Agony_TLB : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(2, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryDmad];

    #endregion

    #region Constant

    private const uint TerritoryDmad = 1363;
    private const int SceneP3 = 8;
    private const int PartyPlayerCount = 8;

    private const uint ChaosDataId = 19508;
    private const uint ExdeathDataId = 19509;
    private const uint ChaosNameId = 7691;
    private const uint FireCrystalDataId = 0x001EC03A;
    private const uint WaterCrystalDataId = 0x001EC03B;
    private const uint WindCrystalDataId = 0x001EC03C;

    private const uint ThunderCircle = 47890;
    private const uint ThunderBuster = 47881;
    private const uint ThunderBusterSecond = 47884;
    private const uint BowelsOfAgony = 47858;
    private const uint ImplosionEffect = 47871;
    private const uint VerticalImplosion = 47869;
    private const uint HorizontalImplosion = 47870;
    private const uint Cyclone = 47864;
    private const uint VacuumWave = 47891;

    private const ushort StatusEntropy = 1600;
    private const ushort StatusDynamicFluid = 1601;

    private const uint StatusBattleChaos = 4192;
    private const uint StatusBattleExdeath = 4194;

    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);
    private static readonly Regex LimitCutVfxRegex =
        new(@"^vfx/lockon/eff/(?:m0361trg_[ab][1-8]t|sph_lockon2_num0[1-8]_s8[pt])\.avfx$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private const string ElNavi = "BowelsNavi";
    private const string ElImplosionLine1 = "implosion_line1";
    private const string ElImplosionLine2 = "implosion_line2";

    private const string JsonImplosionLine1 =
        """{"Name":"implosion_line1","type":3,"refY":30.0,"offY":-30.0,"radius":0.0,"fillIntensity":0.345,"thicc":7.0,"refActorDataID":19508,"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":0.7853982}""";

    private const string JsonImplosionLine2 =
        """{"Name":"implosion_line2","type":3,"refY":30.0,"offY":-30.0,"radius":0.0,"fillIntensity":0.345,"thicc":7.0,"refActorDataID":19508,"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":2.3561945,"DistanceSourceX":100.67891,"DistanceSourceY":100.679085,"DistanceSourceZ":1.9073486E-06,"DistanceMax":20.2}""";

    private static readonly string[] RoleColumnLabels = ["T1", "T2", "H1", "H2", "M1", "M2", "R1", "R2"];

    private static readonly string[] SpreadComboLabels =
        ["windinside", "windoutside", "fireleft", "fireright", "water"];

    private static readonly string[] KnockbackComboLabels =
        ["near", "far", "left", "right"];

    private static readonly string[] BaitSmashRoleLabels = ["R1", "R2"];

    private static readonly string[] BaitBusterRuleLabels =
        ["First Baiter", "T1", "T2"];

    // Spread/Knockback settings table column widths (fixed to prevent layout shift).
    private const float SpreadKnockbackTableLabelColumnWidth = 80f;
    private const float SpreadKnockbackTableRoleColumnWidth = 102f;
    private const float SpreadKnockbackTableComboWidth = 96f;

    private static readonly int PositionSpotCount = Enum.GetValues<PositionSpot>().Length;

    // Position rules table column widths (fixed to prevent layout shift).
    private const float PositionRulesTableLabelColumnWidth = 220f;
    private const float PositionRulesTableBasisColumnWidth = 88f;
    private const float PositionRulesTableAngleColumnWidth = 72f;
    private const float PositionRulesTableRangeColumnWidth = 72f;

    private static readonly string[] PositionSpotLabels =
    [
        "wind inside",
        "wind outside",
        "fire left",
        "fire right",
        "wind inside avoid vertical",
        "wind outside avoid vertical",
        "fire left avoid vertical",
        "fire right avoid vertical",
        "wind inside avoid horizontal",
        "wind outside avoid horizontal",
        "fire left avoid horizontal",
        "fire right avoid horizontal",
        "far",
        "near",
        "left",
        "right",
        "far stack",
        "near stack",
        "left stack",
        "right stack",
        "water",
        "water avoid vertical",
        "water avoid horizontal",
    ];

    private static readonly string[] PositionBasisLabels =
        ["Wind", "Water", "Chaos", "Exdeath", "Fire"];

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();
    private IPlayerCharacter BasePlayer => Controller.BasePlayer;

    private sealed class Config : IEzConfig
    {
        public PriorityData PriorityData = CreateDefaultPriorityData();
        public SpreadCombo[] SpreadByIndex = CreateDefaultSpread();
        public KnockbackCombo[] KnockbackByIndex = CreateDefaultKnockback();
        public BaitBusterCombo BaitBusterRule = BaitBusterCombo.FirstBaiter;
        public BaitSmashCombo BaitSmashRole = BaitSmashCombo.R1;
        public bool DebugPreviewAllDestinations;
        public bool FixFireLeftRightByWaterSideFromWind;
        public PositionRuleSettings[] PositionRules = CreateDefaultPositionRules();

        public void EnsureDefaults()
        {
            PriorityData ??= CreateDefaultPriorityData();
            SpreadByIndex = EnsureComboArray(SpreadByIndex, CreateDefaultSpread());
            KnockbackByIndex = EnsureComboArray(KnockbackByIndex, CreateDefaultKnockback());
            PositionRules = EnsurePositionRules(PositionRules);
            if(BaitBusterRule is not (BaitBusterCombo.FirstBaiter or BaitBusterCombo.T1 or BaitBusterCombo.T2))
                BaitBusterRule = BaitBusterCombo.FirstBaiter;
            if(BaitSmashRole is not (BaitSmashCombo.R1 or BaitSmashCombo.R2))
                BaitSmashRole = BaitSmashCombo.R1;
        }

        private static T[] EnsureComboArray<T>(T[]? current, T[] defaults)
        {
            if(current == null || current.Length != PartyPlayerCount)
                return defaults;

            return current;
        }

        private static PriorityData CreateDefaultPriorityData()
            => new()
            {
                Name = "Bowels of Agony TLB priority",
                Description = "Default: T1 T2 H1 H2 M1 M2 R1 R2",
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
                            new JobbedPlayer { Role = RolePosition.R2 },
                        ],
                    },
                ],
            };

        private static SpreadCombo[] CreateDefaultSpread()
            =>
            [
                SpreadCombo.WindInside,
                SpreadCombo.WindInside,
                SpreadCombo.WindInside,
                SpreadCombo.WindInside,
                SpreadCombo.WindOutside,
                SpreadCombo.WindOutside,
                SpreadCombo.FireRight,
                SpreadCombo.FireLeft,
            ];

        private static KnockbackCombo[] CreateDefaultKnockback()
            =>
            [
                KnockbackCombo.Far,
                KnockbackCombo.Far,
                KnockbackCombo.Left,
                KnockbackCombo.Left,
                KnockbackCombo.Near,
                KnockbackCombo.Near,
                KnockbackCombo.Right,
                KnockbackCombo.Left,
            ];

        private static PositionRuleSettings[] EnsurePositionRules(PositionRuleSettings[]? current)
        {
            var defaults = CreateDefaultPositionRules();
            if(current == null)
                return defaults;

            for(var i = 0; i < Math.Min(current.Length, defaults.Length); i++)
            {
                if(current[i] == null)
                    continue;

                if(!IsValidPositionBasis(current[i].Basis))
                    current[i].Basis = defaults[i].Basis;
                if(float.IsNaN(current[i].AngleDeg) || float.IsInfinity(current[i].AngleDeg))
                    current[i].AngleDeg = defaults[i].AngleDeg;
                if(float.IsNaN(current[i].Range) || float.IsInfinity(current[i].Range) || current[i].Range < 0f)
                    current[i].Range = defaults[i].Range;

                defaults[i] = current[i];
            }

            return defaults;
        }

        private static bool IsValidPositionBasis(PositionBasis basis)
            => basis is PositionBasis.WindCrystal or PositionBasis.WaterCrystal or PositionBasis.Chaos
                or PositionBasis.Exdeath or PositionBasis.FireCrystal;

        private static PositionRuleSettings[] CreateDefaultPositionRules()
            =>
            [
                Rule(PositionBasis.WindCrystal, 0f, 5f),
                Rule(PositionBasis.WindCrystal, 180f, 5f),
                Rule(PositionBasis.WaterCrystal, 225f, 5f),
                Rule(PositionBasis.WaterCrystal, 135f, 5f),
                Rule(PositionBasis.Chaos, 215f, 5f),
                Rule(PositionBasis.Chaos, 30f, 5f),
                Rule(PositionBasis.Chaos, 215f, 10f),
                Rule(PositionBasis.Chaos, 215f, 10f),
                Rule(PositionBasis.Chaos, 235f, 5f),
                Rule(PositionBasis.Chaos, 55f, 5f),
                Rule(PositionBasis.Chaos, 235f, 10f),
                Rule(PositionBasis.Chaos, 235f, 10f),
                Rule(PositionBasis.Exdeath, 0f, 5f),
                Rule(PositionBasis.Exdeath, 0f, 5f),
                Rule(PositionBasis.Exdeath, 45f, 5f),
                Rule(PositionBasis.Exdeath, 315f, 5f),
                Rule(PositionBasis.Exdeath, 0f, 10f),
                Rule(PositionBasis.Exdeath, 0f, 5f),
                Rule(PositionBasis.Exdeath, 45f, 10f),
                Rule(PositionBasis.Exdeath, 315f, 10f),
                Rule(PositionBasis.FireCrystal, 0f, 0f),
                Rule(PositionBasis.FireCrystal, 0f, 0f),
                Rule(PositionBasis.FireCrystal, 0f, 0f),
            ];

        private static PositionRuleSettings Rule(PositionBasis basis, float angleDeg, float range)
            => new() { Basis = basis, AngleDeg = angleDeg, Range = range };
    }

    #endregion

    #region State

    private State _state = State.Wait;
    private bool _isAgony;
    private int _basterCount;
    private int _implosionCount;
    private PositionsCache _positions;
    private string _naviBlockReason = "Wait";
    private Vector3? _cachedWindCrystalPosition;
    private Vector3? _cachedWaterCrystalPosition;
    private Vector3? _cachedFireCrystalPosition;
    private HashSet<string> _battleChaosPlayerNames = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _battleExdeathPlayerNames = new(StringComparer.OrdinalIgnoreCase);

    #endregion

    #region Private Class

    private enum State
    {
        Wait,
        BaitBoss,
        SpreadFirst,
        Buster,
        AvoidVerticalImplosion,
        AvoidHorizontalImplosion,
        SpreadSecond,
        BaitSmash,
        Knockback,
        PairStack,
    }

    private enum SpreadCombo
    {
        WindInside,
        WindOutside,
        FireLeft,
        FireRight,
        Water,
    }

    private enum KnockbackCombo
    {
        Near,
        Far,
        Left,
        Right,
    }

    private enum BaitSmashCombo
    {
        R1,
        R2,
    }

    private enum BaitBusterCombo
    {
        FirstBaiter,
        T1,
        T2,
    }

    private enum PositionBasis
    {
        WindCrystal,
        WaterCrystal,
        Chaos,
        Exdeath,
        FireCrystal,
    }

    private enum PositionSpot
    {
        WindInside,
        WindOutside,
        FireLeft,
        FireRight,
        WindInsideAvoidVertical,
        WindOutsideAvoidVertical,
        FireLeftAvoidVertical,
        FireRightAvoidVertical,
        WindInsideAvoidHorizontal,
        WindOutsideAvoidHorizontal,
        FireLeftAvoidHorizontal,
        FireRightAvoidHorizontal,
        Far,
        Near,
        Left,
        Right,
        FarStack,
        NearStack,
        LeftStack,
        RightStack,
        Water,
        WaterAvoidVertical,
        WaterAvoidHorizontal,
    }

    private sealed class PositionRuleSettings
    {
        public PositionBasis Basis = PositionBasis.WindCrystal;
        public float AngleDeg;
        public float Range = 5f;
    }

    private sealed class PositionsCache
    {
        public bool IsValid;
        public Vector3 ExdeathPosition;
        public Vector3 ChaosBaitPosition;
        public Vector3 ExdeathBaitPosition;
        public Vector3 SmashBaitPosition;
        public Vector3 WindInsidePosition;
        public Vector3 WindOutsidePosition;
        public Vector3 FireLeftPosition;
        public Vector3 FireRightPosition;
        public Vector3 WaterPosition;
        public Vector3 WindInsideAvoidVerticalPosition;
        public Vector3 WindOutsideAvoidVerticalPosition;
        public Vector3 FireLeftAvoidVerticalPosition;
        public Vector3 FireRightAvoidVerticalPosition;
        public Vector3 WaterAvoidVerticalPosition;
        public Vector3 WindInsideAvoidHorizontalPosition;
        public Vector3 WindOutsideAvoidHorizontalPosition;
        public Vector3 FireLeftAvoidHorizontalPosition;
        public Vector3 FireRightAvoidHorizontalPosition;
        public Vector3 WaterAvoidHorizontalPosition;
        public Vector3 FarPosition;
        public Vector3 NearPosition;
        public Vector3 LeftPosition;
        public Vector3 RightPosition;
        public Vector3 FarStackPosition;
        public Vector3 NearStackPosition;
        public Vector3 LeftStackPosition;
        public Vector3 RightStackPosition;
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        C.EnsureDefaults();

        Controller.RegisterElement(ElNavi, new Element(0)
        {
            Enabled = false,
            radius = 0.8f,
            thicc = 6f,
            fillIntensity = 0.25f,
            tether = true,
            overlayFScale = 1.6f,
        }, overwrite: true);

        for(var i = 0; i < PartyPlayerCount; i++)
        {
            Controller.RegisterElement(GetRolePreviewElementName(i), new Element(0)
            {
                Enabled = false,
                radius = 0.25f,
                Donut = 0.1f,
                fillIntensity = 0.544f,
            }, overwrite: true);
        }

        Controller.RegisterElementFromCode(ElImplosionLine1, JsonImplosionLine1, overwrite: true);
        Controller.RegisterElementFromCode(ElImplosionLine2, JsonImplosionLine2, overwrite: true);
    }

    public override void OnReset()
        => ResetState();

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe))
            ResetState();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(!IsPhaseActive())
            return;

        if(castId == BowelsOfAgony)
        {
            _isAgony = true;
            return;
        }

        if(!_isAgony)
            return;

        switch(castId)
        {
            case ThunderCircle:
                _state = State.SpreadFirst;
                break;
            case ThunderBuster:
                _basterCount = 0;
                _state = State.Buster;
                break;
            case VerticalImplosion:
                _state = State.AvoidVerticalImplosion;
                break;
            case HorizontalImplosion:
                _state = State.AvoidHorizontalImplosion;
                break;
            case VacuumWave:
                _state = State.Knockback;
                break;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!IsPhaseActive())
            return;

        var actionId = set.Action?.RowId ?? 0;

        if(!_isAgony)
            return;

        switch(actionId)
        {
            case BowelsOfAgony:
                _basterCount = 0;
                _implosionCount = 0;
                CaptureBattleStatusPlayers();
                _state = State.BaitBoss;
                break;

            case Cyclone:
                switch(_state)
                {
                    case State.SpreadSecond:
                        _state = State.BaitSmash;
                        break;
                    case State.PairStack:
                        _state = State.Wait;
                        break;
                }

                break;

            case ThunderBusterSecond:
                _basterCount++;
                if(_basterCount == 2)
                    _state = State.SpreadFirst;
                break;

            case ImplosionEffect:
                _implosionCount++;
                if(_implosionCount == 2)
                {
                    if(_state == State.AvoidVerticalImplosion)
                        _state = State.AvoidHorizontalImplosion;
                    else if(_state == State.AvoidHorizontalImplosion)
                        _state = State.AvoidVerticalImplosion;
                }

                if(_implosionCount == 4)
                    _state = State.SpreadSecond;
                break;

            case VacuumWave:
                _state = State.PairStack;
                break;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(IsPhaseActive() && LimitCutVfxRegex.IsMatch(vfxPath))
            ResetState();
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status status)
    {
        if(!_isAgony)
            return;

        if(_state == State.SpreadSecond && status.StatusId is StatusEntropy or StatusDynamicFluid)
            _state = State.BaitSmash;
    }

    public override void OnUpdate()
    {
        if(!IsPhaseActive())
        {
            ResetState();
            _naviBlockReason = "Scene != 8";
            return;
        }

        if(!_isAgony)
        {
            ResetState();
            _naviBlockReason = "Agony not active";
            return;
        }

        UpdateDebugPreviewMarkers();
        UpdateImplosionLineMarkers();

        if(_state == State.Wait)
        {
            _naviBlockReason = "State = Wait";
            DisableNavi();
            return;
        }

        var priorityIndex = GetPriorityIndex(BasePlayer);
        if(priorityIndex is < 0 or >= PartyPlayerCount)
        {
            _naviBlockReason = "Priority index not in 0-7";
            DisableNavi();
            return;
        }

        if(!UpdateCachedPositions())
        {
            _naviBlockReason = "Object positions not ready";
            DisableNavi();
            return;
        }

        if(TryResolveDestination(priorityIndex) is not { } destination)
        {
            _naviBlockReason = "No destination for current state/role";
            DisableNavi();
            return;
        }

        _naviBlockReason = "Showing nav";
        EnableNavi(destination);
    }

    public override void OnSettingsDraw()
    {
        C.EnsureDefaults();

        if(ImGui.BeginTabBar("##P3BowelsTlbSettings"))
        {
            if(ImGui.BeginTabItem("Main###tabMain"))
            {
                DrawMainTab();
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

    // Draws priority, spread/knockback rules, and bait smash settings.
    private void DrawMainTab()
    {
        ImGui.TextDisabled("Priority (T1, T2, H1, H2, M1, M2, R1, R2)");
        ImGui.Separator();
        C.PriorityData.Draw();

        ImGui.Spacing();
        DrawSpreadKnockbackRulesTable();

        ImGui.Spacing();
        ImGui.TextDisabled("Bait Buster");
        ImGui.Separator();
        ImGui.SetNextItemWidth(200f);
        var baitBusterRule = (int)C.BaitBusterRule;
        if(ImGui.Combo("Bait Buster rule", ref baitBusterRule, BaitBusterRuleLabels, BaitBusterRuleLabels.Length))
            C.BaitBusterRule = (BaitBusterCombo)baitBusterRule;

        ImGui.Spacing();
        ImGui.TextDisabled("Bait Smash");
        ImGui.Separator();
        ImGui.SetNextItemWidth(200f);
        var baitSmashRole = (int)C.BaitSmashRole;
        if(ImGui.Combo("Bait Smash role", ref baitSmashRole, BaitSmashRoleLabels, BaitSmashRoleLabels.Length))
            C.BaitSmashRole = (BaitSmashCombo)baitSmashRole;

        ImGui.Spacing();
        DrawFireLeftRightSettings();

        ImGui.Spacing();
        DrawPositionRulesTable();
    }

    // Draws fire left/right side correction option.
    private void DrawFireLeftRightSettings()
    {
        ImGui.TextDisabled("Fire left / right");
        ImGui.Separator();
        ImGui.Checkbox(
            "Fix FireLeft / FireRight position at Water, by Side seen from Wind.###fixFireLeftRightByWaterSide",
            ref C.FixFireLeftRightByWaterSideFromWind);
    }

    // Draws live runtime state and local player resolution info.
    private void DrawDebugTab()
    {
        DrawDebugStateSection();
        ImGui.Spacing();
        DrawDebugPreviewSection();
        ImGui.Spacing();
        DrawDebugLocalPlayerSection();
        ImGui.Spacing();
        DrawDebugBattleStatusSection();
        ImGui.Spacing();
        DrawDebugPositionsSection();
    }

    // Draws debug field preview toggle.
    private void DrawDebugPreviewSection()
    {
        ImGui.TextUnformatted("Field preview");
        ImGui.Separator();
        ImGui.Checkbox("Preview all destinations on field", ref C.DebugPreviewAllDestinations);
    }

    // Draws scene, state machine, and counters.
    private void DrawDebugStateSection()
    {
        ImGui.TextUnformatted("State");
        ImGui.Separator();
        ImGui.TextUnformatted($"Scene: {Controller.Scene} (P3 active: {IsPhaseActive()})");
        ImGui.TextUnformatted($"Agony active: {_isAgony}");
        ImGui.TextUnformatted($"State: {_state}");
        ImGui.TextUnformatted($"Buster count: {_basterCount}");
        ImGui.TextUnformatted($"Implosion count: {_implosionCount}");
        ImGui.TextUnformatted($"Navi block reason: {_naviBlockReason}");
    }

    // Draws local player priority index, role, and resolved rules.
    private void DrawDebugLocalPlayerSection()
    {
        ImGui.TextUnformatted("Local player");
        ImGui.Separator();

        var player = BasePlayer;
        if(player == null)
        {
            ImGui.TextUnformatted("BasePlayer: —");
            return;
        }

        var priorityIndex = GetPriorityIndex(player);
        ImGui.TextUnformatted($"Name: {player.Name}");
        ImGui.TextUnformatted($"Priority index: {(priorityIndex == int.MaxValue ? "—" : priorityIndex.ToString())}");

        if(priorityIndex is >= 0 and < PartyPlayerCount)
        {
            ImGui.TextUnformatted($"Role: {GetRoleAtPriorityIndex(priorityIndex)}");
            ImGui.TextUnformatted($"Spread rule: {C.SpreadByIndex[priorityIndex]}");
            ImGui.TextUnformatted($"Knockback rule: {C.KnockbackByIndex[priorityIndex]}");
            ImGui.TextUnformatted($"Bait Buster rule: {C.BaitBusterRule}");
        }

        if(player.StatusList.Any(s => s.StatusId == StatusBattleChaos))
            ImGui.TextUnformatted("Status: Battle Chaos (4192)");
        else if(player.StatusList.Any(s => s.StatusId == StatusBattleExdeath))
            ImGui.TextUnformatted("Status: Battle Exdeath (4194)");
        else
            ImGui.TextUnformatted("Status: —");
    }

    // Draws party members with Battle Chaos / Battle Exdeath status.
    private void DrawDebugBattleStatusSection()
    {
        ImGui.TextUnformatted("Battle status players (saved at BowelsOfAgony)");
        ImGui.Separator();
        ImGui.TextUnformatted($"Battle Chaos (4192): {FormatPlayerNameList(_battleChaosPlayerNames)}");
        ImGui.TextUnformatted($"Battle Exdeath (4194): {FormatPlayerNameList(_battleExdeathPlayerNames)}");
    }

    // Draws position cache status and current nav destination.
    private void DrawDebugPositionsSection()
    {
        ImGui.TextUnformatted("Positions");
        ImGui.Separator();

        var positionsReady = UpdateCachedPositions();
        ImGui.TextUnformatted($"Object positions ready: {positionsReady}");
        ImGui.TextUnformatted($"Wind crystal (live): {FormatObjectLookup(WindCrystalDataId)}");
        ImGui.TextUnformatted($"Wind crystal (cached): {FormatCachedVector3(_cachedWindCrystalPosition)}");
        ImGui.TextUnformatted($"Water crystal (live): {FormatObjectLookup(WaterCrystalDataId)}");
        ImGui.TextUnformatted($"Water crystal (cached): {FormatCachedVector3(_cachedWaterCrystalPosition)}");
        ImGui.TextUnformatted($"Fire crystal (live): {FormatObjectLookup(FireCrystalDataId)}");
        ImGui.TextUnformatted($"Fire crystal (cached): {FormatCachedVector3(_cachedFireCrystalPosition)}");
        ImGui.TextUnformatted($"Chaos: {FormatObjectLookup(ChaosDataId, ChaosNameId)}");
        ImGui.TextUnformatted($"Exdeath: {FormatObjectLookup(ExdeathDataId, 0)}");

        if(positionsReady)
        {
            ImGui.TextUnformatted($"Chaos bait: {FormatVector3(_positions.ChaosBaitPosition)}");
            ImGui.TextUnformatted($"Exdeath bait: {FormatVector3(_positions.ExdeathBaitPosition)}");
            ImGui.TextUnformatted($"Exdeath position: {FormatVector3(_positions.ExdeathPosition)}");
            ImGui.TextUnformatted($"Smash bait: {FormatVector3(_positions.SmashBaitPosition)}");
        }

        var player = BasePlayer;
        if(player != null)
        {
            var priorityIndex = GetPriorityIndex(player);
            if(positionsReady &&
               priorityIndex is >= 0 and < PartyPlayerCount &&
               TryResolveDestination(priorityIndex) is { } destination)
            {
                ImGui.TextUnformatted($"Resolved destination: {FormatVector3(destination)}");
            }
            else
            {
                ImGui.TextUnformatted("Resolved destination: —");
            }
        }

        if(Controller.TryGetElementByName(ElNavi, out var navi))
            ImGui.TextUnformatted($"Navi enabled: {navi.Enabled}");
    }

    // Formats a Vector3 for debug display.
    private static string FormatVector3(Vector3 v)
        => $"({v.X:F2}, {v.Y:F2}, {v.Z:F2})";

    // Formats object lookup result for debug display.
    private static string FormatObjectLookup(uint dataId, uint nameId = 0)
        => TryGetObjectPosition(dataId, nameId) is { } position
            ? FormatVector3(FlattenPosition(position))
            : "—";

    // Formats a cached Vector3 for debug display.
    private static string FormatCachedVector3(Vector3? position)
        => position.HasValue ? FormatVector3(position.Value) : "—";

    // Formats saved player names for debug display.
    private static string FormatPlayerNameList(IEnumerable<string> names)
    {
        var list = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        return list.Count > 0 ? string.Join(", ", list) : "—";
    }

    // Draws spread and knockback rule table (rows: Spread/Knockback, columns: T1-R2).
    private void DrawSpreadKnockbackRulesTable()
    {
        ImGui.TextDisabled("Spread & Knockback rules");
        ImGui.Separator();

        if(!ImGui.BeginTable("##SpreadKnockbackRules", RoleColumnLabels.Length + 1,
               ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit |
               ImGuiTableFlags.NoHostExtendX))
            return;

        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, SpreadKnockbackTableLabelColumnWidth);
        foreach(var label in RoleColumnLabels)
        {
            ImGui.TableSetupColumn(label, ImGuiTableColumnFlags.WidthFixed, SpreadKnockbackTableRoleColumnWidth);
        }
        ImGui.TableHeadersRow();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Spread");
        for(var i = 0; i < PartyPlayerCount; i++)
        {
            ImGui.TableNextColumn();
            ImGui.PushID($"spreadRule_{i}");
            ImGui.SetNextItemWidth(SpreadKnockbackTableComboWidth);
            var spread = (int)C.SpreadByIndex[i];
            if(ImGui.Combo("##spread", ref spread, SpreadComboLabels, SpreadComboLabels.Length))
                C.SpreadByIndex[i] = (SpreadCombo)spread;
            ImGui.PopID();
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Knockback");
        for(var i = 0; i < PartyPlayerCount; i++)
        {
            ImGui.TableNextColumn();
            ImGui.PushID($"knockbackRule_{i}");
            ImGui.SetNextItemWidth(SpreadKnockbackTableComboWidth);
            var knockback = (int)C.KnockbackByIndex[i];
            if(ImGui.Combo("##knockback", ref knockback, KnockbackComboLabels, KnockbackComboLabels.Length))
                C.KnockbackByIndex[i] = (KnockbackCombo)knockback;
            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    // Draws configurable position rules (basis, angle, range) for each spot.
    private void DrawPositionRulesTable()
    {
        ImGui.TextDisabled("Position rules");
        ImGui.Separator();
        ImGui.TextDisabled("Center reference is fixed (100, 100).");

        if(!ImGui.BeginTable("##PositionRules", 5,
               ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit |
               ImGuiTableFlags.NoHostExtendX))
            return;

        ImGui.TableSetupColumn("Spot", ImGuiTableColumnFlags.WidthFixed, PositionRulesTableLabelColumnWidth);
        ImGui.TableSetupColumn("Basis", ImGuiTableColumnFlags.WidthFixed, PositionRulesTableBasisColumnWidth);
        ImGui.TableSetupColumn("Angle", ImGuiTableColumnFlags.WidthFixed, PositionRulesTableAngleColumnWidth);
        ImGui.TableSetupColumn("Range", ImGuiTableColumnFlags.WidthFixed, PositionRulesTableRangeColumnWidth);
        ImGui.TableSetupColumn("Position", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        var objectPositionsReady = TryGetObjectBasePositions(
            out var windCrystalPosition,
            out var waterCrystalPosition,
            out var chaosPosition,
            out var exdeathPosition);

        for(var i = 0; i < PositionSpotCount; i++)
        {
            var settings = C.PositionRules[i];
            var basis = (int)settings.Basis;
            var angleDeg = settings.AngleDeg;
            var range = settings.Range;
            var changed = false;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(PositionSpotLabels[i]);

            ImGui.PushID($"posRule_{i}");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1f);
            if(ImGui.Combo("##basis", ref basis, PositionBasisLabels, PositionBasisLabels.Length))
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
                settings.Basis = ClampPositionBasis(basis);
                settings.AngleDeg = NormalizeAngle(angleDeg);
                settings.Range = MathF.Max(0f, range);
            }

            ImGui.TableNextColumn();
            if(objectPositionsReady &&
               ResolvePositionFromRule(settings, (PositionSpot)i, windCrystalPosition, waterCrystalPosition,
                   chaosPosition, exdeathPosition, GetChaosFacingDegree()) is { } position)
            {
                ImGui.TextUnformatted(FormatVector3(position));
            }
            else
            {
                ImGui.TextUnformatted(objectPositionsReady ? "(unresolved)" : "(objects unavailable)");
            }
        }

        ImGui.EndTable();
    }

    #endregion

    #region Private Method

    // Returns whether P3 scene is active.
    private bool IsPhaseActive()
        => Controller.Scene == SceneP3;

    // Clears state, counters, and nav display.
    private void ResetState()
    {
        _state = State.Wait;
        _isAgony = false;
        _basterCount = 0;
        _implosionCount = 0;
        _positions = new PositionsCache();
        _cachedWindCrystalPosition = null;
        _cachedWaterCrystalPosition = null;
        _cachedFireCrystalPosition = null;
        _battleChaosPlayerNames.Clear();
        _battleExdeathPlayerNames.Clear();
        DisableNavi();
        DisableImplosionLineMarkers();
        DisableAllRolePreviewMarkers();
    }

    // Resolves the local player's priority index from PriorityData.
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

    // Returns the configured role at a priority index.
    private RolePosition GetRoleAtPriorityIndex(int priorityIndex)
    {
        var list = C.PriorityData.GetFirstValidList()?.List;
        if(list == null || priorityIndex < 0 || priorityIndex >= list.Count)
            return RolePosition.Not_Selected;

        return list[priorityIndex].Role;
    }

    // Returns spread combo for priority index 0-7.
    private SpreadCombo GetSpreadRule(int priorityIndex)
        => C.SpreadByIndex[priorityIndex];

    // Returns knockback combo for priority index 0-7.
    private KnockbackCombo GetKnockbackRule(int priorityIndex)
        => C.KnockbackByIndex[priorityIndex];

    // Returns the configured bait smash role as RolePosition.
    private RolePosition GetConfiguredBaitSmashRole()
        => C.BaitSmashRole == BaitSmashCombo.R1 ? RolePosition.R1 : RolePosition.R2;

    // Returns party members who have the given battle status.
    private IEnumerable<IPlayerCharacter> GetBattleStatusPlayers(uint statusId)
        => Controller.GetPartyMembers()
            .Where(p => p.StatusList.Any(s => s.StatusId == statusId));

    // Saves Battle Chaos / Exdeath players when Bowels of Agony resolves.
    private void CaptureBattleStatusPlayers()
    {
        _battleChaosPlayerNames.Clear();
        foreach(var player in GetBattleStatusPlayers(StatusBattleChaos))
        {
            var name = player.Name.ToString();
            if(!string.IsNullOrWhiteSpace(name))
                _battleChaosPlayerNames.Add(name);
        }

        _battleExdeathPlayerNames.Clear();
        foreach(var player in GetBattleStatusPlayers(StatusBattleExdeath))
        {
            var name = player.Name.ToString();
            if(!string.IsNullOrWhiteSpace(name))
                _battleExdeathPlayerNames.Add(name);
        }
    }

    // Returns the player name at a priority index from PriorityData.
    private string? GetPlayerNameAtPriorityIndex(int priorityIndex)
    {
        var priorityList = C.PriorityData.GetPlayers(_ => true);
        if(priorityList == null || priorityIndex < 0 || priorityIndex >= priorityList.Count)
            return null;

        return priorityList[priorityIndex].Name;
    }

    // Returns whether the player was saved as Battle Chaos at Bowels of Agony.
    private bool IsSavedBattleChaosPlayer(IPlayerCharacter player)
        => IsSavedBattleChaosPlayer(player.Name.ToString());

    // Returns whether the saved player name has Battle Chaos status.
    private bool IsSavedBattleChaosPlayer(string playerName)
        => !string.IsNullOrWhiteSpace(playerName) && _battleChaosPlayerNames.Contains(playerName);

    // Returns whether the player was saved as Battle Exdeath at Bowels of Agony.
    private bool IsSavedBattleExdeathPlayer(IPlayerCharacter player)
        => IsSavedBattleExdeathPlayer(player.Name.ToString());

    // Returns whether the saved player name has Battle Exdeath status.
    private bool IsSavedBattleExdeathPlayer(string playerName)
        => !string.IsNullOrWhiteSpace(playerName) && _battleExdeathPlayerNames.Contains(playerName);

    // Finds object position by DataId, then optional NameId fallback.
    private static Vector3? TryGetObjectPosition(uint dataId, uint nameId = 0)
    {
        foreach(var obj in Svc.Objects)
        {
            if(obj.DataId == dataId)
                return obj.Position;
        }

        if(nameId == 0)
            return null;

        foreach(var obj in Svc.Objects)
        {
            if(obj is ICharacter character && character.NameId == nameId)
                return obj.Position;
        }

        return null;
    }

    // Finds object rotation by DataId, then optional NameId fallback.
    private static float? TryGetObjectRotation(uint dataId, uint nameId = 0)
        => Svc.Objects.FirstOrDefault(x => x.DataId == dataId)?.Rotation
           ?? (nameId == 0 ? null : Svc.Objects.OfType<ICharacter>().FirstOrDefault(x => x.NameId == nameId)?.Rotation);

    // Flattens a world position onto the arena floor.
    private static Vector3 FlattenPosition(Vector3 position)
        => new(position.X, 0f, position.Z);

    // Mirrors a position through the arena center on the XZ plane.
    private static Vector3 MirrorThroughArenaCenter(Vector3 position)
        => new(
            ArenaCenter.X + (position.X - ArenaCenter.X) * -1f,
            0f,
            ArenaCenter.Z + (position.Z - ArenaCenter.Z) * -1f);

    // Computes offset from base using north as 0° reference.
    private static Vector3 GetRelativePosition(Vector3 basePos, Vector3 northRef, float angleDeg, float range)
    {
        var refAngle = MathHelper.GetRelativeAngle(basePos, northRef);
        return CalculatePointCircle(basePos, range, NormalizeAngle(refAngle + angleDeg));
    }

    // Calculates a point on a circle from center, radius, and degree.
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

    // Normalizes an angle to 0°–360°.
    private static float NormalizeAngle(float degree)
        => (degree % 360f + 360f) % 360f;

    // Rounds an angle to the nearest step and normalizes to 0°–360°.
    private static float RoundAngleByStep(float degree, float step)
        => NormalizeAngle((float)Math.Round(degree / step) * step);
    // Returns angle from arena center with wind crystal treated as 0° north.
    private static float GetAngleFromWindNorth(Vector3 windCrystalPosition, Vector3 targetPosition)
    {
        var windAngle = MathHelper.GetRelativeAngle(ArenaCenter, windCrystalPosition);
        var targetAngle = MathHelper.GetRelativeAngle(ArenaCenter, targetPosition);
        return NormalizeAngle(targetAngle - windAngle);
    }
    // Returns whether fire left/right should be swapped when water is at 90° from wind north.
    private static bool ShouldSwapFireLeftRightAtWater(Vector3 windCrystalPosition, Vector3 waterCrystalPosition)
        => RoundAngleByStep(GetAngleFromWindNorth(windCrystalPosition, waterCrystalPosition), 90f) == 90f;
    // Swaps resolved fire left/right position caches.
    private void SwapFireLeftRightPositions()
    {
        (_positions.FireLeftPosition, _positions.FireRightPosition) =
            (_positions.FireRightPosition, _positions.FireLeftPosition);
        (_positions.FireLeftAvoidVerticalPosition, _positions.FireRightAvoidVerticalPosition) =
            (_positions.FireRightAvoidVerticalPosition, _positions.FireLeftAvoidVerticalPosition);
        (_positions.FireLeftAvoidHorizontalPosition, _positions.FireRightAvoidHorizontalPosition) =
            (_positions.FireRightAvoidHorizontalPosition, _positions.FireLeftAvoidHorizontalPosition);
    }

    // Converts actor rotation radians to the script's world-degree convention.
    private static float ActorRotationToWorldDegree(float rotationRad)
        => NormalizeAngle(180f - rotationRad * 180f / MathF.PI);

    // Stores live crystal position in cache when the object is present.
    private void TryCacheCrystalPosition(uint dataId, ref Vector3? cache)
    {
        if(TryGetObjectPosition(dataId) is { } position)
            cache = FlattenPosition(position);
    }

    // Rebuilds cached mechanic positions from live object positions.
    private bool UpdateCachedPositions()
    {
        TryCacheCrystalPosition(WindCrystalDataId, ref _cachedWindCrystalPosition);
        TryCacheCrystalPosition(WaterCrystalDataId, ref _cachedWaterCrystalPosition);
        TryCacheCrystalPosition(FireCrystalDataId, ref _cachedFireCrystalPosition);

        if(_cachedWindCrystalPosition is not { } windCrystalPosition ||
           _cachedWaterCrystalPosition is not { } waterCrystalPosition ||
           _cachedFireCrystalPosition is not { })
            return false;

        if(TryGetObjectPosition(ChaosDataId, ChaosNameId) is not { } chaosPosition)
            return false;

        if(TryGetObjectPosition(ExdeathDataId) is not { } exdeathPosition)
            return false;

        chaosPosition = FlattenPosition(chaosPosition);
        exdeathPosition = FlattenPosition(exdeathPosition);

        _positions.ChaosBaitPosition = windCrystalPosition;
        _positions.ExdeathBaitPosition = MirrorThroughArenaCenter(windCrystalPosition);
        _positions.SmashBaitPosition = MirrorThroughArenaCenter(windCrystalPosition);
        _positions.ExdeathPosition = exdeathPosition;

        _positions.WindInsidePosition = ResolveConfiguredPosition(PositionSpot.WindInside, windCrystalPosition,
            waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.WindOutsidePosition = ResolveConfiguredPosition(PositionSpot.WindOutside, windCrystalPosition,
            waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.FireLeftPosition = ResolveConfiguredPosition(PositionSpot.FireLeft, windCrystalPosition,
            waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.FireRightPosition = ResolveConfiguredPosition(PositionSpot.FireRight, windCrystalPosition,
            waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.WaterPosition = ResolveConfiguredPosition(PositionSpot.Water, windCrystalPosition,
            waterCrystalPosition, chaosPosition, exdeathPosition);

        _positions.WindInsideAvoidVerticalPosition = ResolveConfiguredPosition(PositionSpot.WindInsideAvoidVertical,
            windCrystalPosition, waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.WindOutsideAvoidVerticalPosition = ResolveConfiguredPosition(PositionSpot.WindOutsideAvoidVertical,
            windCrystalPosition, waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.FireLeftAvoidVerticalPosition = ResolveConfiguredPosition(PositionSpot.FireLeftAvoidVertical,
            windCrystalPosition, waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.FireRightAvoidVerticalPosition = ResolveConfiguredPosition(PositionSpot.FireRightAvoidVertical,
            windCrystalPosition, waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.WaterAvoidVerticalPosition = ResolveConfiguredPosition(PositionSpot.WaterAvoidVertical,
            windCrystalPosition, waterCrystalPosition, chaosPosition, exdeathPosition);

        _positions.WindInsideAvoidHorizontalPosition = ResolveConfiguredPosition(PositionSpot.WindInsideAvoidHorizontal,
            windCrystalPosition, waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.WindOutsideAvoidHorizontalPosition = ResolveConfiguredPosition(
            PositionSpot.WindOutsideAvoidHorizontal, windCrystalPosition, waterCrystalPosition, chaosPosition,
            exdeathPosition);
        _positions.FireLeftAvoidHorizontalPosition = ResolveConfiguredPosition(PositionSpot.FireLeftAvoidHorizontal,
            windCrystalPosition, waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.FireRightAvoidHorizontalPosition = ResolveConfiguredPosition(PositionSpot.FireRightAvoidHorizontal,
            windCrystalPosition, waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.WaterAvoidHorizontalPosition = ResolveConfiguredPosition(PositionSpot.WaterAvoidHorizontal,
            windCrystalPosition, waterCrystalPosition, chaosPosition, exdeathPosition);

        if(C.FixFireLeftRightByWaterSideFromWind
           && ShouldSwapFireLeftRightAtWater(windCrystalPosition, waterCrystalPosition))
            SwapFireLeftRightPositions();

        _positions.FarPosition = ResolveConfiguredPosition(PositionSpot.Far, windCrystalPosition, waterCrystalPosition,
            chaosPosition, exdeathPosition);
        _positions.NearPosition = ResolveConfiguredPosition(PositionSpot.Near, windCrystalPosition,
            waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.LeftPosition = ResolveConfiguredPosition(PositionSpot.Left, windCrystalPosition,
            waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.RightPosition = ResolveConfiguredPosition(PositionSpot.Right, windCrystalPosition,
            waterCrystalPosition, chaosPosition, exdeathPosition);

        _positions.FarStackPosition = ResolveConfiguredPosition(PositionSpot.FarStack, windCrystalPosition,
            waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.NearStackPosition = ResolveConfiguredPosition(PositionSpot.NearStack, windCrystalPosition,
            waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.LeftStackPosition = ResolveConfiguredPosition(PositionSpot.LeftStack, windCrystalPosition,
            waterCrystalPosition, chaosPosition, exdeathPosition);
        _positions.RightStackPosition = ResolveConfiguredPosition(PositionSpot.RightStack, windCrystalPosition,
            waterCrystalPosition, chaosPosition, exdeathPosition);

        _positions.IsValid = true;
        return true;
    }

    // Returns the configured rule for a position spot.
    private PositionRuleSettings GetPositionRule(PositionSpot spot)
        => C.PositionRules[(int)spot];

    // Resolves a configured position from base object positions.
    private Vector3 ResolveConfiguredPosition(
        PositionSpot spot,
        Vector3 windCrystalPosition,
        Vector3 waterCrystalPosition,
        Vector3 chaosPosition,
        Vector3 exdeathPosition)
        => ResolvePositionFromRule(GetPositionRule(spot), spot, windCrystalPosition, waterCrystalPosition,
            chaosPosition, exdeathPosition, GetChaosFacingDegree());

    // Resolves a position from rule settings and live base object positions.
    private Vector3 ResolvePositionFromRule(
        PositionRuleSettings rule,
        PositionSpot spot,
        Vector3 windCrystalPosition,
        Vector3 waterCrystalPosition,
        Vector3 chaosPosition,
        Vector3 exdeathPosition,
        float? chaosFacingDegree)
    {
        var basePosition = rule.Basis switch
        {
            PositionBasis.WindCrystal => windCrystalPosition,
            PositionBasis.WaterCrystal => waterCrystalPosition,
            PositionBasis.FireCrystal => _cachedFireCrystalPosition ?? windCrystalPosition,
            PositionBasis.Chaos => chaosPosition,
            PositionBasis.Exdeath => exdeathPosition,
            _ => windCrystalPosition,
        };

        if(rule.Basis == PositionBasis.Chaos && IsAvoidImplosionSpot(spot) && chaosFacingDegree.HasValue)
            return CalculatePointCircle(basePosition, rule.Range, NormalizeAngle(chaosFacingDegree.Value + rule.AngleDeg));

        return GetRelativePosition(basePosition, ArenaCenter, rule.AngleDeg, rule.Range);
    }

    // Returns whether a configurable spot is for Horizontal/Vertical Implosion dodging.
    private static bool IsAvoidImplosionSpot(PositionSpot spot)
        => spot is >= PositionSpot.WindInsideAvoidVertical and <= PositionSpot.FireRightAvoidHorizontal
            or PositionSpot.WaterAvoidVertical or PositionSpot.WaterAvoidHorizontal;

    // Returns live Chaos facing for Horizontal/Vertical Implosion positioning.
    private float? GetChaosFacingDegree()
    {
        return TryGetObjectRotation(ChaosDataId, ChaosNameId) is { } chaosRotation
            ? ActorRotationToWorldDegree(chaosRotation)
            : null;
    }

    // Returns whether live base object positions are available for preview.
    private bool TryGetObjectBasePositions(
        out Vector3 windCrystalPosition,
        out Vector3 waterCrystalPosition,
        out Vector3 chaosPosition,
        out Vector3 exdeathPosition)
    {
        TryCacheCrystalPosition(WindCrystalDataId, ref _cachedWindCrystalPosition);
        TryCacheCrystalPosition(WaterCrystalDataId, ref _cachedWaterCrystalPosition);
        TryCacheCrystalPosition(FireCrystalDataId, ref _cachedFireCrystalPosition);

        windCrystalPosition = default;
        waterCrystalPosition = default;
        chaosPosition = default;
        exdeathPosition = default;

        if(_cachedWindCrystalPosition is not { } wind ||
           _cachedWaterCrystalPosition is not { } water ||
           _cachedFireCrystalPosition is not { })
            return false;

        if(TryGetObjectPosition(ChaosDataId, ChaosNameId) is not { } chaosRaw)
            return false;

        if(TryGetObjectPosition(ExdeathDataId) is not { } exdeathRaw)
            return false;

        windCrystalPosition = wind;
        waterCrystalPosition = water;
        chaosPosition = FlattenPosition(chaosRaw);
        exdeathPosition = FlattenPosition(exdeathRaw);
        return true;
    }

    // Clamps a position basis combo index to valid values.
    private static PositionBasis ClampPositionBasis(int basis)
        => basis switch
        {
            (int)PositionBasis.WaterCrystal => PositionBasis.WaterCrystal,
            (int)PositionBasis.Chaos => PositionBasis.Chaos,
            (int)PositionBasis.Exdeath => PositionBasis.Exdeath,
            (int)PositionBasis.FireCrystal => PositionBasis.FireCrystal,
            _ => PositionBasis.WindCrystal,
        };

    // Resolves destination for the current state and priority index.
    private Vector3? TryResolveDestination(int priorityIndex)
    {
        var role = GetRoleAtPriorityIndex(priorityIndex);

        switch(_state)
        {
            case State.BaitBoss:
                if(role is RolePosition.T1 or RolePosition.T2)
                {
                    var playerName = GetPlayerNameAtPriorityIndex(priorityIndex);
                    if(playerName == null)
                        return null;

                    if(IsSavedBattleChaosPlayer(playerName))
                        return _positions.ChaosBaitPosition;

                    if(IsSavedBattleExdeathPlayer(playerName))
                        return _positions.ExdeathBaitPosition;

                    return null;
                }

                return ResolveSpreadPosition(GetSpreadRule(priorityIndex));

            case State.Buster:
                return TryResolveBusterDestination(role, priorityIndex);

            case State.SpreadFirst:
            case State.SpreadSecond:
                return ResolveSpreadPosition(GetSpreadRule(priorityIndex));

            case State.AvoidVerticalImplosion:
                return ResolveAvoidVerticalPosition(GetSpreadRule(priorityIndex));

            case State.AvoidHorizontalImplosion:
                return ResolveAvoidHorizontalPosition(GetSpreadRule(priorityIndex));

            case State.BaitSmash:
                if(role != GetConfiguredBaitSmashRole())
                    return null;

                return _positions.SmashBaitPosition;

            case State.Knockback:
                return ResolveKnockbackPosition(GetKnockbackRule(priorityIndex), stack: false);

            case State.PairStack:
                return ResolveKnockbackPosition(GetKnockbackRule(priorityIndex), stack: true);

            default:
                return null;
        }
    }

    // Resolves buster bait or spread destination per Bait Buster rule.
    private Vector3? TryResolveBusterDestination(RolePosition role, int priorityIndex)
    {
        if(role is RolePosition.T1 or RolePosition.T2)
        {
            if(IsBusterBaiter(role, priorityIndex))
                return _positions.ExdeathPosition;

            return null;
        }

        return ResolveSpreadPosition(GetSpreadRule(priorityIndex));
    }

    // Returns whether the T1/T2 player at the priority index baits Thunder Buster.
    private bool IsBusterBaiter(RolePosition role, int priorityIndex)
    {
        if(role is not (RolePosition.T1 or RolePosition.T2))
            return false;

        return C.BaitBusterRule switch
        {
            BaitBusterCombo.FirstBaiter =>
                IsSavedBattleExdeathPlayer(GetPlayerNameAtPriorityIndex(priorityIndex) ?? string.Empty),
            BaitBusterCombo.T1 => role == RolePosition.T1,
            BaitBusterCombo.T2 => role == RolePosition.T2,
            _ => false,
        };
    }

    // Maps spread combo to spread position.
    private Vector3 ResolveSpreadPosition(SpreadCombo combo)
        => combo switch
        {
            SpreadCombo.WindInside => _positions.WindInsidePosition,
            SpreadCombo.WindOutside => _positions.WindOutsidePosition,
            SpreadCombo.FireLeft => _positions.FireLeftPosition,
            SpreadCombo.FireRight => _positions.FireRightPosition,
            SpreadCombo.Water => _positions.WaterPosition,
            _ => _positions.WindInsidePosition,
        };

    // Maps spread combo to vertical implosion avoid position.
    private Vector3 ResolveAvoidVerticalPosition(SpreadCombo combo)
        => combo switch
        {
            SpreadCombo.WindInside => _positions.WindInsideAvoidVerticalPosition,
            SpreadCombo.WindOutside => _positions.WindOutsideAvoidVerticalPosition,
            SpreadCombo.FireLeft => _positions.FireLeftAvoidVerticalPosition,
            SpreadCombo.FireRight => _positions.FireRightAvoidVerticalPosition,
            SpreadCombo.Water => _positions.WaterAvoidVerticalPosition,
            _ => _positions.WindInsideAvoidVerticalPosition,
        };

    // Maps spread combo to horizontal implosion avoid position.
    private Vector3 ResolveAvoidHorizontalPosition(SpreadCombo combo)
        => combo switch
        {
            SpreadCombo.WindInside => _positions.WindInsideAvoidHorizontalPosition,
            SpreadCombo.WindOutside => _positions.WindOutsideAvoidHorizontalPosition,
            SpreadCombo.FireLeft => _positions.FireLeftAvoidHorizontalPosition,
            SpreadCombo.FireRight => _positions.FireRightAvoidHorizontalPosition,
            SpreadCombo.Water => _positions.WaterAvoidHorizontalPosition,
            _ => _positions.WindInsideAvoidHorizontalPosition,
        };

    // Maps knockback combo to dodge or stack position.
    private Vector3 ResolveKnockbackPosition(KnockbackCombo combo, bool stack)
    {
        if(stack)
        {
            return combo switch
            {
                KnockbackCombo.Far => _positions.FarStackPosition,
                KnockbackCombo.Near => _positions.NearStackPosition,
                KnockbackCombo.Left => _positions.LeftStackPosition,
                KnockbackCombo.Right => _positions.RightStackPosition,
                _ => _positions.NearStackPosition,
            };
        }

        return combo switch
        {
            KnockbackCombo.Far => _positions.FarPosition,
            KnockbackCombo.Near => _positions.NearPosition,
            KnockbackCombo.Left => _positions.LeftPosition,
            KnockbackCombo.Right => _positions.RightPosition,
            _ => _positions.NearPosition,
        };
    }

    // Enables nav element at destination with attention color.
    private void EnableNavi(Vector3 destination)
    {
        if(!Controller.TryGetElementByName(ElNavi, out var element))
            return;

        element.SetRefPosition(destination);
        element.color = Controller.AttentionColor;
        element.tether = true;
        element.Enabled = true;
    }

    // Disables the nav element.
    private void DisableNavi()
    {
        if(!Controller.TryGetElementByName(ElNavi, out var element))
            return;

        element.Enabled = false;
        element.tether = false;
    }

    // Returns whether the state machine is in an avoid implosion phase.
    private bool IsAvoidImplosionState()
        => _state is State.AvoidVerticalImplosion or State.AvoidHorizontalImplosion;

    // Enables or disables implosion guide lines for the current state.
    private void UpdateImplosionLineMarkers()
    {
        if(IsAvoidImplosionState())
            EnableImplosionLineMarkers();
        else
            DisableImplosionLineMarkers();
    }

    // Enables Chaos-relative implosion guide lines.
    private void EnableImplosionLineMarkers()
    {
        if(Controller.TryGetElementByName(ElImplosionLine1, out var line1))
            line1.Enabled = true;

        if(Controller.TryGetElementByName(ElImplosionLine2, out var line2))
            line2.Enabled = true;
    }

    // Disables implosion guide lines.
    private void DisableImplosionLineMarkers()
    {
        if(Controller.TryGetElementByName(ElImplosionLine1, out var line1))
            line1.Enabled = false;

        if(Controller.TryGetElementByName(ElImplosionLine2, out var line2))
            line2.Enabled = false;
    }

    // Returns the debug role preview element name for a priority index.
    private static string GetRolePreviewElementName(int index)
        => $"RolePreview_{index}";

    // Updates all role preview markers when debug preview is enabled.
    private void UpdateDebugPreviewMarkers()
    {
        if(!C.DebugPreviewAllDestinations || _state == State.Wait)
        {
            DisableAllRolePreviewMarkers();
            return;
        }

        if(!UpdateCachedPositions())
        {
            DisableAllRolePreviewMarkers();
            return;
        }

        for(var i = 0; i < PartyPlayerCount; i++)
        {
            if(TryResolveDestination(i) is { } destination)
                EnableRolePreviewMarker(i, destination, RoleColumnLabels[i]);
            else
                DisableRolePreviewMarker(i);
        }
    }

    // Enables a role preview marker at the given position.
    private void EnableRolePreviewMarker(int index, Vector3 position, string label)
    {
        if(!Controller.TryGetElementByName(GetRolePreviewElementName(index), out var element))
            return;

        element.SetRefPosition(position);
        element.color = Controller.AttentionColor;
        element.overlayText = label;
        element.tether = false;
        element.Enabled = true;
    }

    // Disables a single role preview marker.
    private void DisableRolePreviewMarker(int index)
    {
        if(!Controller.TryGetElementByName(GetRolePreviewElementName(index), out var element))
            return;

        element.Enabled = false;
        element.tether = false;
    }

    // Disables all role preview markers.
    private void DisableAllRolePreviewMarkers()
    {
        for(var i = 0; i < PartyPlayerCount; i++)
            DisableRolePreviewMarker(i);
    }

    #endregion
}