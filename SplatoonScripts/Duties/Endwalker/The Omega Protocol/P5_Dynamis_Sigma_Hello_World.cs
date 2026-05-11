using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ECommons.Hooks.ActionEffectTypes;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;

public class P5_Dynamis_Sigma_Hello_World : SplatoonScript
{
    #region Metadata
    public override Metadata Metadata { get; } = new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryTop];
    #endregion

    #region Constant
    private const uint TerritoryTop = 1122;
    private const uint SceneId = 6;
    private const uint DataIdOmegaFemale = 0x3D68;
    private const uint ActionCodeDynamisSigma = 32788;
    private const uint ActionLearRazor = 31631;
    private const uint ActionOmegaFemaleFoot = 31530;
    private const uint ActionOmegaFemaleStaff = 31533;
    private const uint ActionHelloWorldNearThird = 31626;
    private const uint StatusHelloNear = 3442;
    private const uint StatusHelloFar = 3443;
    private const string VfxClockwise = "vfx/lockon/eff/m0515_turning_right01c.avfx";
    private const string VfxCounterClockwise = "vfx/lockon/eff/m0515_turning_left01c.avfx";
    private const ushort TransformOmegaFemaleFoot = 4;
    private const ushort TransformOmegaFemaleStaff = 11;
    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);
    private const float SpreadConfigDegreesMin = 0f;
    private const float SpreadConfigDegreesMax = 355.9f;
    private const float SpreadRadiusMin = 0f;
    private const float SettingsCellWidth = 150f;
    private const string NaviElement = "navi";
    private const string NaviSubElement = "navi_sub";
    private const float DefaultHelloSpreadRadius = 9.75f;
    private const float DefaultOtherSpreadRadius = 19f;
    private const uint MarkerP1Unset = uint.MaxValue;

    // Marker presets for settings / debug combos (Attack1–6, Bind1–2, Stop1–2).
    private static readonly MarkerType[] MarkerPresetOrder =
    [
        MarkerType.Attack1,
        MarkerType.Attack2,
        MarkerType.Attack3,
        MarkerType.Attack4,
        MarkerType.Attack5,
        MarkerType.Attack6,
        MarkerType.Bind1,
        MarkerType.Bind2,
        MarkerType.Stop1,
        MarkerType.Stop2
    ];

    private static readonly string[] MarkerPresetComboLabels =
    [
        "Attack1",
        "Attack2",
        "Attack3",
        "Attack4",
        "Attack5",
        "Attack6",
        "Bind1",
        "Bind2",
        "Stop1",
        "Stop2"
    ];

    private static readonly string[] DebugMarkerComboLabelsWithUnset =
    [
        "(unset)",
        "Attack1",
        "Attack2",
        "Attack3",
        "Attack4",
        "Attack5",
        "Attack6",
        "Bind1",
        "Bind2",
        "Stop1",
        "Stop2"
    ];

    #endregion

    #region Config

    public sealed class Config : IEzConfig
    {
        public MarkerType BaitArm1Marker = MarkerType.Attack1;
        public MarkerType BaitArm2Marker = MarkerType.Attack2;
        public MarkerType BaitFar1Marker = MarkerType.Attack3;
        public MarkerType BaitFar2Marker = MarkerType.Attack4;
        public MarkerType BaitNear1Marker = MarkerType.Attack5;
        public MarkerType BaitNear2Marker = MarkerType.Attack6;

        public bool ResolveBaitNearWithoutMarker = false;

        public float DegSpreadHelloNear = 180f;
        public float DegSpreadHelloNearCcw = 180f;
        public float DegSpreadHelloFar = 270f;
        public float DegSpreadHelloFarCcw = 90f;
        public float RadiusHelloNear = DefaultHelloSpreadRadius;
        public float RadiusHelloFar = DefaultHelloSpreadRadius;

        public Group SpreadGroupHelloNear = Group.South;
        public Group SpreadGroupHelloFar = Group.South;
        public Group SpreadGroupBaitArm1 = Group.North;
        public Group SpreadGroupBaitArm2 = Group.North;
        public Group SpreadGroupBaitFar1 = Group.North;
        public Group SpreadGroupBaitFar2 = Group.South;
        public Group SpreadGroupBaitNear1 = Group.South;
        public Group SpreadGroupBaitNear2 = Group.South;

        public float DegSpreadBaitArm1 = 317.5f;
        public float DegSpreadBaitArm1Ccw = 317.5f;
        public float DegSpreadBaitArm2 = 42.5f;
        public float DegSpreadBaitArm2Ccw = 42.5f;
        public float RadiusBaitArm1 = DefaultOtherSpreadRadius;
        public float RadiusBaitArm2 = DefaultOtherSpreadRadius;
        public float DegSpreadBaitFar1 = 90f;
        public float DegSpreadBaitFar1Ccw = 270f;
        public float DegSpreadBaitFar2 = 270f;
        public float DegSpreadBaitFar2Ccw = 90f;
        public float RadiusBaitFar1 = DefaultOtherSpreadRadius;
        public float RadiusBaitFar2 = DefaultOtherSpreadRadius;

        public float DegSpreadBaitNear1 = 192.5f;
        public float DegSpreadBaitNear1Ccw = 192.5f;
        public float DegSpreadBaitNear2 = 167.5f;
        public float DegSpreadBaitNear2Ccw = 167.5f;
        public float RadiusBaitNear1 = DefaultOtherSpreadRadius;
        public float RadiusBaitNear2 = DefaultOtherSpreadRadius;
    }

    private Config C => Controller.GetConfig<Config>();

    #endregion

    #region State
    private readonly Dictionary<ulong, PlayerData> _players = [];
    private State _state = State.Wait;
    private bool _isClockwise;
    private float _initAngle;
    #endregion

    #region Private Class

    private enum State
    {
        Wait,
        Calc,
        AvoidRazor,
        AvoidOmegaFAction,
        SpreadHelloWorld
    }

    private enum Role
    {
        None,
        BaitArm1,
        BaitArm2,
        BaitFar1,
        BaitFar2,
        BaitNear1,
        BaitNear2,
        HelloNear,
        HelloFar
    }

    public enum Group
    {
        North,
        South
    }

    // Values match MarkingController marker indices (see Splatoon.Memory.Marking).
    public enum MarkerType : uint
    {
        None = 999,
        Attack1 = 0,
        Attack2 = 1,
        Attack3 = 2,
        Attack4 = 3,
        Attack5 = 4,
        Bind1 = 5,
        Bind2 = 6,
        Bind3 = 7,
        Stop1 = 8,
        Stop2 = 9,
        Square = 10,
        Circle = 11,
        Cross = 12,
        Triangle = 13,
        Attack6 = 14,
        Attack7 = 15,
        Attack8 = 16
    }

    private sealed class PlayerData
    {
        public ulong ObjectId;
        public string Name = string.Empty;
        public uint MarkerP1 = MarkerP1Unset;
        public Role Role = Role.None;
    }

    #endregion

    #region LifeCycle
    public override void OnSetup()
    {
        Controller.RegisterElementFromCode(NaviElement, """{"Name":"navi","refX":100.0,"refY":100.0,"radius":0.5,"fillIntensity":0.5,"thicc":5.0,"tether":true}""", overwrite: true);
        Controller.RegisterElementFromCode(NaviSubElement, """{"Name":"navi_sub","refX":100.0,"refY":100.0,"radius":0.5,"fillIntensity":0.5,"thicc":5.0,"tether":true}""", overwrite: true);
    }

    public override void OnUpdate()
    {
        DisableNavigationElements();

        if(_players.Count < 7)
            BuildPartySnapshot();

        if(!IsPhaseFive() || _state == State.Wait || BasePlayer == null || _players.Count != 8) return;

        if(!_players.TryGetValue(BasePlayer.GameObjectId, out var me)) return;
        
        RecomputeDerivedRoles();

        if(_state == State.Calc) return;

        if(_state == State.AvoidRazor)
        {
            var pos = GetRazorSafePosition(me.Role);
            UpdateNavigation(NaviElement, pos, true);
            DisableElement(NaviSubElement);
            return;
        }

        if(_state == State.AvoidOmegaFAction)
        {
            var omegaFemale = FindNpcByDataId(DataIdOmegaFemale);
            if(omegaFemale == null)
            {
                DisableNavigationElements();
                return;
            }

            var transformId = omegaFemale.GetTransformationID();
            if(transformId == TransformOmegaFemaleStaff)
            {
                var pos = GetRazorSafePosition(me.Role);
                UpdateNavigation(NaviElement, pos, true);
                DisableElement(NaviSubElement);
            }
            else if(transformId == TransformOmegaFemaleFoot)
            {
                var pos = CalculatePointFromCenterByDegree(ArenaCenter, DefaultOtherSpreadRadius, GetGroup(me.Role) == Group.North ? _initAngle : _initAngle + 180f);
                UpdateNavigation(NaviElement, pos, true);
                DisableElement(NaviSubElement);
            }
            else
            {
                DisableNavigationElements();
            }
            return;
        }

        if(_state == State.SpreadHelloWorld)
            UpdateSpreadNavigation(me.Role);
    }

    public override void OnReset()
    {
        _players.Clear();
        _state = State.Wait;
        _isClockwise = false;
        _initAngle = 0f;
        DisableNavigationElements();
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!IsPhaseFive() || set.Action == null) return;
        var actionId = set.Action.Value.RowId;

        if(actionId == ActionCodeDynamisSigma)
        {
            _state = State.Calc;
            return;
        }

        if(_state == State.AvoidRazor && actionId == ActionLearRazor)
        {
            _state = State.AvoidOmegaFAction;
            return;
        }

        if(_state == State.AvoidOmegaFAction && (actionId == ActionOmegaFemaleFoot || actionId == ActionOmegaFemaleStaff))
        {
            _state = State.SpreadHelloWorld;
            return;
        }

        if(_state == State.SpreadHelloWorld && actionId == ActionHelloWorldNearThird)
        {
            OnReset();
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(!IsPhaseFive() || _state != State.Calc || _players.Count != 8) return;
        if(!vfxPath.Equals(VfxClockwise, StringComparison.OrdinalIgnoreCase)
           && !vfxPath.Equals(VfxCounterClockwise, StringComparison.OrdinalIgnoreCase))
            return;

        _isClockwise = vfxPath.Equals(VfxClockwise, StringComparison.OrdinalIgnoreCase);

        var omegaFemale = FindNpcByDataId(DataIdOmegaFemale);
        if(omegaFemale != null)
            _initAngle = NormalizeDegrees(GetRelativeAngleFromArenaCenter(omegaFemale.Position));

        _state = State.AvoidRazor;
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, uint p7, uint p8, ulong targetId, byte replaying)
    {
        if(!IsPhaseFive()) return;
        if(command != 502) return;

        _ = sourceId;

        ulong markedGoId = 0;
        PlayerData? data = null;
        if(targetId != 0 && _players.TryGetValue(targetId, out var byTarget))
        {
            markedGoId = targetId;
            data = byTarget;
        }
        else if(p2 != 0 && _players.TryGetValue(p2, out var byP2))
        {
            markedGoId = p2;
            data = byP2;
        }

        if(data == null)
            return;

        data.MarkerP1 = p1;
        if(data.Name.Length == 0)
            data.Name = Svc.Objects.FirstOrDefault(x => x.GameObjectId == markedGoId)?.Name.ToString() ?? string.Empty;
            
        RecomputeDerivedRoles();
    }

    public override void OnSettingsDraw()
    {
        ImGui.TextWrapped("This Script guides next 3 steps:");
        ImGui.Indent();
        ImGui.TextWrapped("Step1: Avoid Razor.");
        ImGui.TextWrapped("Step2: Avoid Omega-F actions (Foot or Staff).");
        ImGui.TextWrapped("Step3: Spread Hello World.");
        ImGui.Unindent();
        ImGui.NewLine();

        if(ImGuiEx.BeginDefaultTable("P5SigmaHelloWorldSettings", ["Role", "Marker", "Group", "Spread Angle (Cw)", "Spread Angle (Ccw)", "Range from Center"]))
        {
            DrawRoleSettingsRowHelloDual("HelloNear", ref C.SpreadGroupHelloNear, ref C.DegSpreadHelloNear, ref C.DegSpreadHelloNearCcw, ref C.RadiusHelloNear);
            DrawRoleSettingsRowHelloDual("HelloFar", ref C.SpreadGroupHelloFar, ref C.DegSpreadHelloFar, ref C.DegSpreadHelloFarCcw, ref C.RadiusHelloFar);
            DrawRoleSettingsRowMarkerSpreadDual("#baitArm1", ref C.BaitArm1Marker, ref C.SpreadGroupBaitArm1, ref C.DegSpreadBaitArm1, ref C.DegSpreadBaitArm1Ccw, ref C.RadiusBaitArm1);
            DrawRoleSettingsRowMarkerSpreadDual("#baitArm2", ref C.BaitArm2Marker, ref C.SpreadGroupBaitArm2, ref C.DegSpreadBaitArm2, ref C.DegSpreadBaitArm2Ccw, ref C.RadiusBaitArm2);
            DrawRoleSettingsRowMarkerSpreadDual("#baitFar1", ref C.BaitFar1Marker, ref C.SpreadGroupBaitFar1, ref C.DegSpreadBaitFar1, ref C.DegSpreadBaitFar1Ccw, ref C.RadiusBaitFar1);
            DrawRoleSettingsRowMarkerSpreadDual("#baitFar2", ref C.BaitFar2Marker, ref C.SpreadGroupBaitFar2, ref C.DegSpreadBaitFar2, ref C.DegSpreadBaitFar2Ccw, ref C.RadiusBaitFar2);
            if(C.ResolveBaitNearWithoutMarker)
            {
                DrawRoleSettingsRowSpreadHintDual("#baitNear1", "(remaining)", ref C.SpreadGroupBaitNear1, ref C.DegSpreadBaitNear1, ref C.DegSpreadBaitNear1Ccw, ref C.RadiusBaitNear1);
                DrawRoleSettingsRowSpreadHintDual("#baitNear2", "(remaining)", ref C.SpreadGroupBaitNear2, ref C.DegSpreadBaitNear2, ref C.DegSpreadBaitNear2Ccw, ref C.RadiusBaitNear2);
            }
            else
            {
                DrawRoleSettingsRowMarkerSpreadDual("#baitNear1", ref C.BaitNear1Marker, ref C.SpreadGroupBaitNear1, ref C.DegSpreadBaitNear1, ref C.DegSpreadBaitNear1Ccw, ref C.RadiusBaitNear1);
                DrawRoleSettingsRowMarkerSpreadDual("#baitNear2", ref C.BaitNear2Marker, ref C.SpreadGroupBaitNear2, ref C.DegSpreadBaitNear2, ref C.DegSpreadBaitNear2Ccw, ref C.RadiusBaitNear2);
            }
            ImGui.EndTable();
        }
        ImGui.Checkbox("Resolve BaitNear without Marker. Tether to 2 BaitNear Positions##sigma", ref C.ResolveBaitNearWithoutMarker);
        ImGui.TextDisabled("Cw: Clockwise, Ccw: Counter-Clockwise. Angle is relative to Omega-F, which is true north.");
        ImGui.NewLine();

        DrawImportButtons();
        ImGui.TextWrapped("If other configurations are needed, please adjust the settings manually.");
        ImGui.NewLine();

        if(ImGui.CollapsingHeader("Debug"))
            DrawDebugSection();
    }

    #endregion

    #region Private Method
    // Settings button that fills config from a fixed JP-style strat preset.
    private void DrawImportButtons()
    {
        if(ImGui.Button("Import Japanese Strat"))
            ApplyJapaneseStrat(C);
        ImGuiEx.HelpMarker("Macro: https://jp.finalfantasyxiv.com/lodestone/character/34120564/blog/5178791/\nRaidPlan: https://raidplan.io/plan/u98293e225836jcy");
    }

    // Debug table: state, rotation, and per-player marker / role.
    private void DrawDebugSection()
    {
        ImGui.Text($"State: {_state}");
        ImGui.Text($"IsClockwise: {_isClockwise}");
        ImGui.Text($"InitAngle (ΩF, north=0°): {_initAngle:0.00}");
        if(!ImGuiEx.BeginDefaultTable("P5SigmaHelloWorldRoles", ["Name", "GameObjectId", "Marker (debug)", "Role"]))
            return;

        foreach(var player in _players.Values.OrderBy(x => x.Name).ThenBy(x => x.ObjectId))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(player.Name);
            ImGui.TableNextColumn();
            ImGui.Text($"0x{player.ObjectId:X8}");
            ImGui.TableNextColumn();
            DrawDebugMarkerCombo(player);
            ImGui.TableNextColumn();
            ImGui.Text(player.Role.ToString());
        }

        ImGui.EndTable();
    }

    // One settings row: marker column shows a hint only (no raid marker combo).
    private static void DrawRoleSettingsRowSpreadHintDual(string roleLabel, string markerHint, ref Group spreadGroup, ref float spreadCwDeg, ref float spreadCcwDeg, ref float radius)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(roleLabel);
        ImGui.TableNextColumn();
        ImGui.TextDisabled(markerHint);
        DrawSpreadCells(roleLabel, ref spreadGroup, ref spreadCwDeg, ref spreadCcwDeg, ref radius);
    }

    // One settings row for hello roles (spread only, debuff-assigned).
    private static void DrawRoleSettingsRowHelloDual(string roleLabel, ref Group spreadGroup, ref float spreadCwDeg, ref float spreadCcwDeg, ref float radius)
        => DrawRoleSettingsRowSpreadHintDual(roleLabel, "(debuff assigned)", ref spreadGroup, ref spreadCwDeg, ref spreadCcwDeg, ref radius);

    // One settings row: raid marker combo plus spread group and angles.
    private static void DrawRoleSettingsRowMarkerSpreadDual(string roleLabel, ref MarkerType marker, ref Group spreadGroup, ref float spreadCwDeg, ref float spreadCcwDeg, ref float radius)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(roleLabel);
        ImGui.TableNextColumn();
        DrawMarkerCell(roleLabel + "_mk", ref marker);
        DrawSpreadCells(roleLabel, ref spreadGroup, ref spreadCwDeg, ref spreadCcwDeg, ref radius);
    }

    // Narrow combo: Attack1–6, Bind1–2, Stop1–2 only (invalid saved values coerced to Attack1).
    private static void DrawMarkerCell(string id, ref MarkerType marker)
    {
        var idx = Array.IndexOf(MarkerPresetOrder, marker);
        if(idx < 0)
        {
            marker = MarkerPresetOrder[0];
            idx = 0;
        }

        ImGui.PushID(id);
        ImGui.SetNextItemWidth(SettingsCellWidth);
        if(ImGui.Combo("##marker", ref idx, MarkerPresetComboLabels, MarkerPresetComboLabels.Length))
            marker = MarkerPresetOrder[idx];
        ImGui.PopID();
    }

    // Table cells: spread hemisphere, cw angle, ccw angle, radius for one role row.
    private static void DrawSpreadCells(string idPrefix, ref Group spreadGroup, ref float spreadCwDeg, ref float spreadCcwDeg, ref float radius)
    {
        ImGui.TableNextColumn();
        ImGui.PushID(idPrefix + "_grp");
        ImGui.SetNextItemWidth(SettingsCellWidth);
        ImGuiEx.EnumCombo("##group", ref spreadGroup);
        ImGui.PopID();

        ImGui.TableNextColumn();
        ImGui.PushID(idPrefix + "_cw");
        DrawSpreadConfigDegreesInput(ref spreadCwDeg);
        ImGui.PopID();

        ImGui.TableNextColumn();
        ImGui.PushID(idPrefix + "_ccw");
        DrawSpreadConfigDegreesInput(ref spreadCcwDeg);
        ImGui.PopID();

        ImGui.TableNextColumn();
        ImGui.PushID(idPrefix + "_radius");
        DrawSpreadRadiusInput(ref radius);
        ImGui.PopID();
    }

    // ImGui float input for a spread angle in degrees with clamping.
    private static void DrawSpreadConfigDegreesInput(ref float degrees)
    {
        degrees = ClampSpreadConfigDegrees(degrees);
        ImGui.SetNextItemWidth(SettingsCellWidth);
        ImGui.InputFloat("##spread", ref degrees, 1f, 5f, "%.2f°");
        degrees = ClampSpreadConfigDegrees(degrees);
    }

    // Clamps configured spread angle to the allowed editor range.
    private static float ClampSpreadConfigDegrees(float degrees)
        => Math.Clamp(degrees, SpreadConfigDegreesMin, SpreadConfigDegreesMax);

    // ImGui float input for spread radius with clamping.
    private static void DrawSpreadRadiusInput(ref float radius)
    {
        radius = ClampSpreadRadius(radius);
        ImGui.SetNextItemWidth(SettingsCellWidth);
        ImGui.InputFloat("##radius", ref radius, 0.1f, 1f, "%.2f");
        radius = ClampSpreadRadius(radius);
    }

    // Ensures spread radius is not below the configured minimum.
    private static float ClampSpreadRadius(float radius)
        => Math.Max(SpreadRadiusMin, radius);

    // Applies default JP strat markers, angles, and shared spread groups to config.
    private static void ApplyJapaneseStrat(Config c)
    {
        SetMarkers(c, MarkerType.Attack1, MarkerType.Attack2, MarkerType.Attack3, MarkerType.Attack4);
        SetSpread(ref c.DegSpreadHelloNear, ref c.DegSpreadHelloNearCcw, 180f, 180f);
        SetSpread(ref c.DegSpreadHelloFar, ref c.DegSpreadHelloFarCcw, 270f, 90f);
        SetSpread(ref c.DegSpreadBaitArm1, ref c.DegSpreadBaitArm1Ccw, 317.5f, 317.5f);
        SetSpread(ref c.DegSpreadBaitArm2, ref c.DegSpreadBaitArm2Ccw, 42.5f, 42.5f);
        SetSpread(ref c.DegSpreadBaitFar1, ref c.DegSpreadBaitFar1Ccw, 90f, 270f);
        SetSpread(ref c.DegSpreadBaitFar2, ref c.DegSpreadBaitFar2Ccw, 270f, 90f);
        SetSpread(ref c.DegSpreadBaitNear1, ref c.DegSpreadBaitNear1Ccw, 192.5f, 192.5f);
        SetSpread(ref c.DegSpreadBaitNear2, ref c.DegSpreadBaitNear2Ccw, 167.5f, 167.5f);
        ApplySharedSpreadGroups(c);
        c.ResolveBaitNearWithoutMarker = true;
    }

    // Writes bait marker enum fields on config in one call.
    private static void SetMarkers(Config c, MarkerType arm1, MarkerType arm2, MarkerType far1, MarkerType far2)
    {
        c.BaitArm1Marker = arm1;
        c.BaitArm2Marker = arm2;
        c.BaitFar1Marker = far1;
        c.BaitFar2Marker = far2;
        c.BaitNear1Marker = MarkerType.Bind1;
        c.BaitNear2Marker = MarkerType.Bind2;
    }

    // Sets both cw and ccw spread degree fields to fixed preset values.
    private static void SetSpread(ref float cw, ref float ccw, float cwValue, float ccwValue)
    {
        cw = cwValue;
        ccw = ccwValue;
    }

    // Default north/south group picks for each bait spread slot (JP preset).
    private static void ApplySharedSpreadGroups(Config c)
    {
        c.SpreadGroupHelloNear = Group.South;
        c.SpreadGroupHelloFar = Group.South;
        c.SpreadGroupBaitArm1 = Group.North;
        c.SpreadGroupBaitArm2 = Group.North;
        c.SpreadGroupBaitFar1 = Group.North;
        c.SpreadGroupBaitFar2 = Group.South;
        c.SpreadGroupBaitNear1 = Group.South;
        c.SpreadGroupBaitNear2 = Group.South;
    }

    // Re-runs marker roles, hello debuffs, then assigns the two remaining players as bait-near.
    private void RecomputeDerivedRoles()
    {
        ApplyMarkerRoles();
        ApplyNearFarRoles();
        ResolveRemainingNearRoles();
    }

    // Debug UI: change raid marker P1 for a row and optionally recompute roles when party is full.
    private void DrawDebugMarkerCombo(PlayerData player)
    {
        ImGui.PushID($"m{player.ObjectId:X16}");
        var choice = ToEditableMarkerType(player.MarkerP1);
        var before = player.MarkerP1;
        ImGui.SetNextItemWidth(MathF.Min(240f, ImGui.GetContentRegionAvail().X));
        DrawPresetMarkerComboUnset("##dbgMarker", ref choice);
        var after = FromEditableMarkerType(choice);
        if(after != before)
        {
            player.MarkerP1 = after;
            if(_players.Count == 8)
                RecomputeDerivedRoles();
        }

        if(player.MarkerP1 != MarkerP1Unset && !Enum.IsDefined(typeof(MarkerType), player.MarkerP1))
            ImGui.TextDisabled($"non-enum p1={player.MarkerP1}");
        ImGui.PopID();
    }

    // Debug marker combo: (unset) plus the same presets as settings.
    private static void DrawPresetMarkerComboUnset(string label, ref MarkerType marker)
    {
        int idx;
        if(marker == MarkerType.None)
            idx = 0;
        else
        {
            var p = Array.IndexOf(MarkerPresetOrder, marker);
            idx = p >= 0 ? p + 1 : 0;
        }

        if(ImGui.Combo(label, ref idx, DebugMarkerComboLabelsWithUnset, DebugMarkerComboLabelsWithUnset.Length))
            marker = idx == 0 ? MarkerType.None : MarkerPresetOrder[idx - 1];
    }

    // Maps raw marker P1 to editor enum (unset or unknown become None).
    private static MarkerType ToEditableMarkerType(uint p1)
    {
        if(p1 == MarkerP1Unset)
            return MarkerType.None;
        return Enum.IsDefined(typeof(MarkerType), p1) ? (MarkerType)p1 : MarkerType.None;
    }

    // Maps editor marker choice back to game P1 value (None → unset sentinel).
    private static uint FromEditableMarkerType(MarkerType m)
        => m == MarkerType.None ? MarkerP1Unset : (uint)m;

    // True when duty scene is P5 Dynamis.
    private bool IsPhaseFive() => Controller.Scene == SceneId;

    // Rebuilds the eight-player snapshot from the current party list.
    private void BuildPartySnapshot()
    {
        _players.Clear();
        foreach(var pc in Controller.GetPartyMembers().OfType<IPlayerCharacter>())
        {
            _players[pc.GameObjectId] = new PlayerData
            {
                ObjectId = pc.GameObjectId,
                Name = pc.Name.ToString(),
                MarkerP1 = MarkerP1Unset,
                Role = Role.None
            };
        }
    }

    // Assigns bait arm/far roles from configured marker ids on each player row.
    private void ApplyMarkerRoles()
    {
        foreach(var player in _players.Values)
        {
            player.Role = player.MarkerP1 switch
            {
                var x when x == (uint)C.BaitArm1Marker => Role.BaitArm1,
                var x when x == (uint)C.BaitArm2Marker => Role.BaitArm2,
                var x when x == (uint)C.BaitFar1Marker => Role.BaitFar1,
                var x when x == (uint)C.BaitFar2Marker => Role.BaitFar2,
                var x when !C.ResolveBaitNearWithoutMarker && x == (uint)C.BaitNear1Marker => Role.BaitNear1,
                var x when !C.ResolveBaitNearWithoutMarker && x == (uint)C.BaitNear2Marker => Role.BaitNear2,
                _ => Role.None
            };
        }
    }

    // Overwrites roles from hello near/far status on live characters.
    private void ApplyNearFarRoles()
    {
        foreach(var (objectId, player) in _players.ToArray())
        {
            var obj = Svc.Objects.FirstOrDefault(x => x.GameObjectId == objectId) as IPlayerCharacter;
            if(obj == null) continue;
            if(obj.StatusList.Any(x => x.StatusId == StatusHelloNear))
                player.Role = Role.HelloNear;
            else if(obj.StatusList.Any(x => x.StatusId == StatusHelloFar))
                player.Role = Role.HelloFar;
        }
    }

    // When resolving near without markers, assigns BaitNear1/2 to the two remaining players (sorted by object id).
    private void ResolveRemainingNearRoles()
    {
        if(!C.ResolveBaitNearWithoutMarker)
            return;

        var remaining = _players.Values.Where(x => x.Role == Role.None).ToList();
        if(remaining.Count != 2) return;

        remaining.Sort((a, b) => a.ObjectId.CompareTo(b.ObjectId));
        remaining[0].Role = Role.BaitNear1;
        remaining[1].Role = Role.BaitNear2;
    }

    // Stand position for razor dodge using init angle, cw flag, and north/south group.
    private Vector3 GetRazorSafePosition(Role role)
    {
        var group = GetGroup(role);
        var angle = _isClockwise
            ? (group == Group.North ? _initAngle - 22.5f : _initAngle - 22.5f + 180f)
            : (group == Group.North ? _initAngle + 22.5f : _initAngle + 22.5f + 180f);
        return CalculatePointFromCenterByDegree(ArenaCenter, DefaultOtherSpreadRadius, angle);
    }

    // Updates main (and optional sub) nav elements for hello spread from role config.
    private void UpdateSpreadNavigation(Role role)
    {
        DisableElement(NaviSubElement);
        if(!TryGetSpreadAngles(role, out var angle, out var subAngle))
        {
            DisableElement(NaviElement);
            return;
        }

        UpdateNavigation(NaviElement, CalculatePointFromCenterByDegree(ArenaCenter, GetSpreadRadius(role), angle), true);
        if(subAngle != null)
            UpdateNavigation(NaviSubElement, CalculatePointFromCenterByDegree(ArenaCenter, GetSpreadSubRadius(role), subAngle.Value), true);
    }

    // Computes final spread angles from init facing plus configured cw/ccw offsets.
    private bool TryGetSpreadAngles(Role role, out float angle, out float? subAngle)
    {
        if(!TryGetSpreadOffsets(role, out var primaryOffset, out var secondaryOffset))
        {
            angle = default;
            subAngle = null;
            return false;
        }

        angle = _initAngle + primaryOffset;
        subAngle = secondaryOffset != null ? _initAngle + secondaryOffset.Value : null;
        return true;
    }

    // Primary (and optional secondary) degree offset from config for a resolved role.
    private bool TryGetSpreadOffsets(Role role, out float primaryOffset, out float? secondaryOffset)
    {
        secondaryOffset = null;
        switch(role)
        {
            case Role.HelloNear:
                primaryOffset = SpreadOffsetByDirection(C.DegSpreadHelloNear, C.DegSpreadHelloNearCcw);
                return true;
            case Role.HelloFar:
                primaryOffset = SpreadOffsetByDirection(C.DegSpreadHelloFar, C.DegSpreadHelloFarCcw);
                return true;
            case Role.BaitArm1:
                primaryOffset = SpreadOffsetByDirection(C.DegSpreadBaitArm1, C.DegSpreadBaitArm1Ccw);
                return true;
            case Role.BaitArm2:
                primaryOffset = SpreadOffsetByDirection(C.DegSpreadBaitArm2, C.DegSpreadBaitArm2Ccw);
                return true;
            case Role.BaitFar1:
                primaryOffset = SpreadOffsetByDirection(C.DegSpreadBaitFar1, C.DegSpreadBaitFar1Ccw);
                return true;
            case Role.BaitFar2:
                primaryOffset = SpreadOffsetByDirection(C.DegSpreadBaitFar2, C.DegSpreadBaitFar2Ccw);
                return true;
            case Role.BaitNear1:
                primaryOffset = SpreadOffsetByDirection(C.DegSpreadBaitNear1, C.DegSpreadBaitNear1Ccw);
                secondaryOffset = C.ResolveBaitNearWithoutMarker
                    ? SpreadOffsetByDirection(C.DegSpreadBaitNear2, C.DegSpreadBaitNear2Ccw)
                    : null;
                return true;
            case Role.BaitNear2:
                primaryOffset = SpreadOffsetByDirection(C.DegSpreadBaitNear2, C.DegSpreadBaitNear2Ccw);
                secondaryOffset = C.ResolveBaitNearWithoutMarker
                    ? SpreadOffsetByDirection(C.DegSpreadBaitNear1, C.DegSpreadBaitNear1Ccw)
                    : null;
                return true;
            default:
                primaryOffset = default;
                return false;
        }
    }

    // Picks cw vs ccw configured angle based on current rotation sense flag.
    private float SpreadOffsetByDirection(float clockwiseDegrees, float counterClockwiseDegrees)
        => _isClockwise ? ClampSpreadConfigDegrees(clockwiseDegrees) : ClampSpreadConfigDegrees(counterClockwiseDegrees);

    // North/south spread group from config for a role.
    private Group GetGroup(Role role)
        => role switch
        {
            Role.HelloNear => C.SpreadGroupHelloNear,
            Role.HelloFar => C.SpreadGroupHelloFar,
            Role.BaitArm1 => C.SpreadGroupBaitArm1,
            Role.BaitArm2 => C.SpreadGroupBaitArm2,
            Role.BaitFar1 => C.SpreadGroupBaitFar1,
            Role.BaitFar2 => C.SpreadGroupBaitFar2,
            Role.BaitNear1 => C.SpreadGroupBaitNear1,
            Role.BaitNear2 => C.SpreadGroupBaitNear2,
            _ => Group.South
        };

    // World XZ from arena center, radius, and compass angle in degrees.
    private static Vector3 CalculatePointFromCenterByDegree(Vector3 center, float radius, float degree)
    {
        var rad = degree.DegToRad();
        return new Vector3(
            center.X + MathF.Sin(rad) * radius,
            center.Y,
            center.Z - MathF.Cos(rad) * radius
        );
    }

    // First battle NPC on the object table matching data id.
    private IBattleNpc? FindNpcByDataId(uint dataId)
        => Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.DataId == dataId);

    // Enables a nav element at position with rainbow tint and optional tether.
    private void UpdateNavigation(string elementName, Vector3 position, bool tether)
    {
        if(!Controller.TryGetElementByName(elementName, out var element)) return;
        element.color = ImGui.ColorConvertFloat4ToU32(GetRainbowColor(4d));
        element.SetRefPosition(position);
        element.tether = tether;
        element.Enabled = true;
    }

    // Configured spread radius for the role (hello, bait, bait-near).
    private float GetSpreadRadius(Role role)
        => role switch
        {
            Role.HelloNear => ClampSpreadRadius(C.RadiusHelloNear),
            Role.HelloFar => ClampSpreadRadius(C.RadiusHelloFar),
            Role.BaitArm1 => ClampSpreadRadius(C.RadiusBaitArm1),
            Role.BaitArm2 => ClampSpreadRadius(C.RadiusBaitArm2),
            Role.BaitFar1 => ClampSpreadRadius(C.RadiusBaitFar1),
            Role.BaitFar2 => ClampSpreadRadius(C.RadiusBaitFar2),
            Role.BaitNear1 => ClampSpreadRadius(C.RadiusBaitNear1),
            Role.BaitNear2 => ClampSpreadRadius(C.RadiusBaitNear2),
            _ => DefaultOtherSpreadRadius
        };

    // Secondary nav radius; for bait-near roles, shows the paired near slot on nav_sub.
    private float GetSpreadSubRadius(Role role)
        => role switch
        {
            Role.BaitNear1 => ClampSpreadRadius(C.RadiusBaitNear2),
            Role.BaitNear2 => ClampSpreadRadius(C.RadiusBaitNear1),
            _ => GetSpreadRadius(role)
        };

    // Hides both registered nav elements.
    private void DisableNavigationElements()
    {
        DisableElement(NaviElement);
        DisableElement(NaviSubElement);
    }

    // Turns off a registered element by name if it exists.
    private void DisableElement(string elementName)
    {
        if(Controller.TryGetElementByName(elementName, out var element))
            element.Enabled = false;
    }

    // Compass degrees from arena center to a world position (north-based).
    private static float GetRelativeAngleFromArenaCenter(Vector3 position)
        => MathHelper.GetRelativeAngle(ArenaCenter, position);

    // Wraps degrees into [0, 360).
    private static float NormalizeDegrees(float degrees)
    {
        var n = degrees % 360f;
        return n < 0f ? n + 360f : n;
    }

    // Full-saturation RGBA that cycles hue over wall-clock time.
    private Vector4 GetRainbowColor(double cycleSeconds)
    {
        if(cycleSeconds <= 0d) cycleSeconds = 1d;
        var normalizedTime = Environment.TickCount64 / 1000d / cycleSeconds;
        var hue = normalizedTime % 1f;
        return HsvToVector4(hue, 1f, 1f);
    }

    // Converts HSV in [0,1] to linear RGB with alpha 1.
    private static Vector4 HsvToVector4(double h, double s, double v)
    {
        double r = 0d;
        double g = 0d;
        double b = 0d;
        var i = (int)(h * 6d);
        var f = h * 6d - i;
        var p = v * (1d - s);
        var q = v * (1d - f * s);
        var t = v * (1d - (1d - f) * s);

        switch(i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new Vector4((float)r, (float)g, (float)b, 1f);
    }
    #endregion
}
