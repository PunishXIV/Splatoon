using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;

public class P5_Dynamis_Omega_Safe_Guide : SplatoonScript
{
    #region Metadata
    public override Metadata Metadata { get; } = new(3, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryTop];
    #endregion

    #region Constant
    private const uint TerritoryTop = 1122;
    private const uint SceneId = 6;
    private const uint CastWaveFirstHorizontal = 31644;
    private const uint CastWaveFirstVertical = 31643;
    private const uint CastWaveSecondHorizontal = 31608;
    private const uint CastWaveSecondVertical = 31607;
    private const uint OmegaMaleDataId = 0x3D69;
    private const uint OmegaFemaleDataId = 0x3D6A;

    private const uint TransformSword = 0;
    private const uint TransformShield = 4;
    private const uint TransformStaff = 0;
    private const uint TransformFoot = 4;

    private const float AngleSnap45 = 45f;
    private const float OuterSafeRadius = 10f;
    private const float InnerSafeRadius = 5f;
    private const float SwordStaffFarRadius = 19f;
    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);

    private const int StepAwaitingFirstWave = 0;
    private const int StepAwaitingSecondWave = 1;
    private const int StepComplete = 2;

    private const int FirstOmegaPairVisibleCount = 2;
    private const int SecondOmegaPairMinVisibleCount = 4;

    private const float SwordStaffQuadrantOffsetDegrees = 67.5f;
    private const float SwordStaffFarOffsetDegrees = 225f;
    private const string LayoutLanguage = "Language";
    private static readonly string[] FieldMarkerNames = ["A", "B", "C", "D", "1", "2", "3", "4"];
    private const float SwordFootQuadrantOffsetDegrees = 32.5f;
    private const float ShieldStaffQuadrantOffsetDegrees = 45f;
    private const float ShieldFootQuadrantOffsetDegrees = 32.5f;

    private const float NeSwDiagonalAngle1 = 45f;
    private const float NeSwDiagonalAngle2 = 225f;

    // Not from RegisterElementFromCode JSON; used as tower line offZ in UpdateTowerNavigation.
    private const float TowerVerticalOffset = 10f;
    #endregion

    #region Config
    private Config C => Controller.GetConfig<Config>();
    #endregion

    #region State
    private readonly OmegaPairInfo _firstInfo = new();
    private readonly OmegaPairInfo _secondInfo = new();
    private int _step = StepAwaitingFirstWave;
    #endregion

    #region Private Class
    private enum SwordStaffSpotMode
    {
        Inner,
        Outer
    }

    private sealed class Config : IEzConfig
    {
        public SwordStaffSpotMode SwordStaffSpot = SwordStaffSpotMode.Inner;
        public float PillarHeight = TowerVerticalOffset;
        public bool ShowCallout = true;
        public bool CalloutOnHead = true;
        public bool CalloutOnCenter;
    }

    private sealed class OmegaPairInfo
    {
        public uint MaleObjectId;
        public uint FemaleObjectId;
        public uint OmegaCastId;

        public bool HasObjects => MaleObjectId != 0 && FemaleObjectId != 0;
        public bool IsReady => HasObjects && OmegaCastId != 0;
    }
    #endregion

    #region LifeCycle
    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("first_navi", """{"Name":"first_navi","radius":0,"fillIntensity":0.5,"thicc":25.0,"refActorDataID":14669,"refActorComparisonType":3}""", overwrite: true);
        Controller.RegisterElementFromCode("second_navi", """{"Name":"second_navi","radius":0,"fillIntensity":0.5,"thicc":25.0,"refActorDataID":14669,"refActorComparisonType":3}""", overwrite: true);
        Controller.RegisterElementFromCode("first_navi_tower", """{"Name":"first_navi_tower","type":2,"refX":100.0,"refY":100.0,"offX":100.0,"offY":100.0,"offZ":10.0,"radius":0.0,"Filled":false,"fillIntensity":0.5,"thicc":50.0,"refActorDataID":15708,"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":3.3684855}""", overwrite: true);
        Controller.RegisterElementFromCode("second_navi_tower", """{"Name":"second_navi_tower","type":2,"refX":100.0,"refY":100.0,"offX":100.0,"offY":100.0,"offZ":10.0,"radius":0.0,"Filled":false,"fillIntensity":0.5,"thicc":50.0,"refActorDataID":15708,"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":3.3684855}""", overwrite: true);
        Controller.RegisterElementFromCode("callout_head", """{"Name":"callout_head","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":2516582400,"overlayTextColor":4294967295,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"refActorType":1}""", overwrite: true);
        Controller.RegisterElementFromCode("callout_center", """{"Name":"callout_center","refX":100.0,"refY":100.0,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayVOffset":1.0,"overlayFScale":3.0,"thicc":0.0}""", overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutLanguage, """
            ~Lv2~{"Name":"Language","ElementsL":[{"Name":"Guide","radius":0.0,"Filled":false,"fillIntensity":0.5,"thicc":0.0,"overlayText":"{0} → {1}","overlayTextIntl":{"Jp":"{0} から {1}"}},{"Name":"Inside","radius":0.0,"Filled":false,"fillIntensity":0.5,"thicc":0.0,"overlayText":" Inside","overlayTextIntl":{"Jp":"内"}},{"Name":"Middle","radius":0.0,"Filled":false,"fillIntensity":0.5,"thicc":0.0},{"Name":"Outside","radius":0.0,"Filled":false,"fillIntensity":0.5,"thicc":0.0,"overlayText":" Outside","overlayTextIntl":{"Jp":"外"}}]}
            """, out _, overwrite: true);
    }

    public override void OnUpdate()
    {
        if(!IsPhaseFive())
        {
            DisableElement("first_navi");
            DisableElement("second_navi");
            DisableElement("first_navi_tower");
            DisableElement("second_navi_tower");
            DisableElement("callout_head");
            DisableElement("callout_center");
            return;
        }

        PopulatePairObjectIds();
        var firstReady = TryGetSafePosition(_firstInfo, out var firstPos);
        var secondReady = TryGetSafePosition(_secondInfo, out var secondPos);

        if(_step == StepAwaitingFirstWave)
        {
            UpdateNavigation("first_navi", firstReady, firstPos, tether: true, rainbow: true);
            UpdateNavigation("second_navi", secondReady, secondPos, tether: false, rainbow: true);
            UpdateTowerNavigation("first_navi_tower", firstReady, firstPos, rainbow: true);
            UpdateTowerNavigation("second_navi_tower", secondReady, secondPos, rainbow: true);
        }
        else if(_step == StepAwaitingSecondWave)
        {
            UpdateNavigation("first_navi", shouldEnable: false, firstPos, tether: false, rainbow: false);
            UpdateNavigation("second_navi", secondReady, secondPos, tether: true, rainbow: true);
            UpdateTowerNavigation("first_navi_tower", shouldEnable: false, firstPos, rainbow: false);
            UpdateTowerNavigation("second_navi_tower", secondReady, secondPos, rainbow: true);
        }
        else
        {
            UpdateNavigation("first_navi", shouldEnable: false, firstPos, tether: false, rainbow: false);
            UpdateNavigation("second_navi", shouldEnable: false, secondPos, tether: false, rainbow: false);
            UpdateTowerNavigation("first_navi_tower", shouldEnable: false, firstPos, rainbow: false);
            UpdateTowerNavigation("second_navi_tower", shouldEnable: false, secondPos, rainbow: false);
        }

        UpdateCallout(firstReady, firstPos, secondReady, secondPos);
    }

    public override void OnReset()
    {
        _firstInfo.MaleObjectId = 0;
        _firstInfo.FemaleObjectId = 0;
        _firstInfo.OmegaCastId = 0;
        _secondInfo.MaleObjectId = 0;
        _secondInfo.FemaleObjectId = 0;
        _secondInfo.OmegaCastId = 0;
        _step = StepAwaitingFirstWave;
        DisableElement("first_navi");
        DisableElement("second_navi");
        DisableElement("first_navi_tower");
        DisableElement("second_navi_tower");
        DisableElement("callout_head");
        DisableElement("callout_center");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(!IsPhaseFive()) return;
        if(castId != CastWaveFirstHorizontal && castId != CastWaveFirstVertical) return;
        if(_firstInfo.OmegaCastId != 0 && _secondInfo.OmegaCastId != 0) return;

        _firstInfo.OmegaCastId = castId;
        _secondInfo.OmegaCastId = castId == CastWaveFirstHorizontal ? CastWaveSecondVertical : CastWaveSecondHorizontal;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!IsPhaseFive()) return;
        if(set.Action == null) return;

        var castId = set.Action.Value.RowId;
        if(castId == _firstInfo.OmegaCastId)
            _step = StepAwaitingSecondWave;
        if(castId == _secondInfo.OmegaCastId)
            _step = StepComplete;
    }

    public override void OnSettingsDraw()
    {
        ImGui.TextDisabled("Settings");
        ImGui.Separator();
        ImGui.Text("Sword + Staff Guide");
        ImGui.Indent();
        if(ImGui.RadioButton("Inner##SwordStaff", C.SwordStaffSpot == SwordStaffSpotMode.Inner))
            C.SwordStaffSpot = SwordStaffSpotMode.Inner;
        ImGui.SameLine();
        if(ImGui.RadioButton("Outer##SwordStaff", C.SwordStaffSpot == SwordStaffSpotMode.Outer))
            C.SwordStaffSpot = SwordStaffSpotMode.Outer;
        ImGui.Unindent();

        ImGui.Spacing();
        ImGui.TextDisabled("Visualizer");
        ImGui.Separator();
        ImGui.SetNextItemWidth(200f);
        ImGui.SliderFloat("Pillar Height", ref C.PillarHeight, 0f, 30f, "%.1f");
        ImGui.Checkbox("Show Callout", ref C.ShowCallout);
        ImGui.Indent();
        ImGui.BeginDisabled(!C.ShowCallout);
        ImGui.Checkbox("On Your Head", ref C.CalloutOnHead);
        ImGui.Checkbox("On Center", ref C.CalloutOnCenter);
        ImGui.EndDisabled();
        ImGui.Unindent();
    }
    #endregion

    #region Private Method
    // True when the duty scene id matches P5 Dynamis.
    private bool IsPhaseFive()
        => Controller.Scene == SceneId;

    // Fills first/second pair object ids from visible Omega NPCs when still unknown.
    private void PopulatePairObjectIds()
    {
        var omegaObjects = Svc.Objects
            .OfType<IBattleNpc>()
            .Where(x => x.DataId == OmegaMaleDataId || x.DataId == OmegaFemaleDataId)
            .Where(x => x.IsCharacterVisible())
            .ToList();

        if(!_firstInfo.HasObjects && omegaObjects.Count == FirstOmegaPairVisibleCount)
        {
            _firstInfo.MaleObjectId = omegaObjects.FirstOrDefault(x => x.DataId == OmegaMaleDataId)?.ObjectId ?? 0;
            _firstInfo.FemaleObjectId = omegaObjects.FirstOrDefault(x => x.DataId == OmegaFemaleDataId)?.ObjectId ?? 0;
        }

        if(!_secondInfo.HasObjects && omegaObjects.Count >= SecondOmegaPairMinVisibleCount)
        {
            _secondInfo.MaleObjectId = omegaObjects
                .FirstOrDefault(x => x.DataId == OmegaMaleDataId && x.ObjectId != _firstInfo.MaleObjectId)
                ?.ObjectId ?? 0;
            _secondInfo.FemaleObjectId = omegaObjects
                .FirstOrDefault(x => x.DataId == OmegaFemaleDataId && x.ObjectId != _firstInfo.FemaleObjectId)
                ?.ObjectId ?? 0;
        }
    }

    // Resolves safe world position for a pair from current transforms and cast id.
    private bool TryGetSafePosition(OmegaPairInfo info, out Vector3 safePosition)
    {
        safePosition = default;
        if(!info.IsReady) return false;

        var male = Svc.Objects.FirstOrDefault(x => x.ObjectId == info.MaleObjectId) as IBattleNpc;
        var female = Svc.Objects.FirstOrDefault(x => x.ObjectId == info.FemaleObjectId) as IBattleNpc;
        if(male == null || female == null || !male.IsCharacterVisible() || !female.IsCharacterVisible())
            return false;

        var pos = GetSafePosition(male, female, info.OmegaCastId);
        if(pos == null) return false;

        safePosition = pos.Value;
        return true;
    }

    // Updates point nav element: position, tether, optional Attention Color tint, enable state.
    private void UpdateNavigation(string elementName, bool shouldEnable, Vector3 safePosition, bool tether, bool rainbow)
    {
        if(!Controller.TryGetElementByName(elementName, out var element)) return;
        if(!shouldEnable)
        {
            element.Enabled = false;
            return;
        }

        element.SetRefPosition(safePosition);
        element.tether = tether;
        if(rainbow)
            element.color = Controller.AttentionColor;
        element.Enabled = true;
    }

    // Updates tower line element ref position, XZ offsets, height, Attention Color, enable state.
    private void UpdateTowerNavigation(string elementName, bool shouldEnable, Vector3 safePosition, bool rainbow)
    {
        if(!Controller.TryGetElementByName(elementName, out var element)) return;
        if(!shouldEnable)
        {
            element.Enabled = false;
            return;
        }

        element.SetRefPosition(safePosition);
        element.offX = safePosition.X;
        element.offY = safePosition.Z;
        element.offZ = C.PillarHeight;
        if(rainbow)
            element.color = Controller.AttentionColor;
        element.Enabled = true;
    }

    // Shows the hint in the attention window, and on the head and arena center when enabled.
    private void UpdateCallout(bool firstReady, Vector3 firstPos, bool secondReady, Vector3 secondPos)
    {
        var text = _step == StepComplete ? "" : BuildCalloutText(firstReady, firstPos, secondReady, secondPos);
        if(text.Length > 0)
            Controller.DisplayAttentionWindowLine(text);

        SetCalloutElement("callout_head", C.ShowCallout && C.CalloutOnHead, text);
        SetCalloutElement("callout_center", C.ShowCallout && C.CalloutOnCenter, text);
    }

    // Enables one world callout element when it is selected and there is text.
    private void SetCalloutElement(string elementName, bool show, string text)
    {
        if(!Controller.TryGetElementByName(elementName, out var element)) return;
        if(!show || text.Length == 0)
        {
            element.Enabled = false;
            element.overlayText = "";
            return;
        }

        element.overlayText = text;
        element.Enabled = true;
    }

    // Localized "{0} → {1}" from the two safe spots, or one side when the other is not ready.
    private string BuildCalloutText(bool firstReady, Vector3 firstPos, bool secondReady, Vector3 secondPos)
    {
        if(!firstReady && !secondReady) return "";

        var markerByAngle = BuildMarkerByAngle();
        var first = firstReady ? FormatGuideSpot(firstPos, markerByAngle) : "";
        var second = secondReady ? FormatGuideSpot(secondPos, markerByAngle) : "";
        if(firstReady && secondReady) return string.Format(LayoutText("Guide"), first, second);
        return firstReady ? first : second;
    }

    // Marker name at the safe spot's 45° angle, plus the localized inside or outside suffix.
    private string FormatGuideSpot(Vector3 position, Dictionary<int, string> markerByAngle)
    {
        var snapped = SnapTo45Int(GetRelativeAngleFromArenaCenter(position));
        var name = markerByAngle.TryGetValue(snapped, out var marker) ? marker : "?";
        var dx = position.X - ArenaCenter.X;
        var dz = position.Z - ArenaCenter.Z;
        var radius = System.MathF.Sqrt(dx * dx + dz * dz);
        var band = "Middle";
        if(radius < (InnerSafeRadius + OuterSafeRadius) * 0.5f)
            band = "Inside";
        else if(radius > (OuterSafeRadius + SwordStaffFarRadius) * 0.5f)
            band = "Outside";
        return name + LayoutText(band);
    }

    // Localized overlay text from the Language layout. Empty when the element has no text.
    private string LayoutText(string elementName)
    {
        var element = Controller.GetRegisteredLayouts().SafeSelect(LayoutLanguage)?.GetElement(elementName);
        if(element == null) return "";
        return element.overlayTextIntl.Get(element.overlayText);
    }

    // Active field markers keyed by angle snapped to 45°, north = 0. Closest marker wins a shared slot.
    private static unsafe Dictionary<int, string> BuildMarkerByAngle()
    {
        var markerByAngle = new Dictionary<int, string>();
        var bestDistance = new Dictionary<int, float>();
        var markers = MarkingController.Instance()->FieldMarkers;
        for(var i = 0; i < FieldMarkerNames.Length; i++)
        {
            var marker = markers[i];
            if(!marker.Active) continue;

            var angle = GetRelativeAngleFromArenaCenter(marker.Position);
            var snapped = SnapTo45Int(angle);
            var distance = System.MathF.Abs(NormalizeAngle(angle - snapped));
            if(distance > 180f)
                distance = 360f - distance;
            if(bestDistance.TryGetValue(snapped, out var previous) && previous <= distance)
                continue;

            bestDistance[snapped] = distance;
            markerByAngle[snapped] = FieldMarkerNames[i];
        }

        return markerByAngle;
    }

    // Safe spot from male/female transformation pair and wave cast id; null if unsupported.
    private Vector3? GetSafePosition(IBattleNpc male, IBattleNpc female, uint omegaCastId)
    {
        var maleTransform = male.GetTransformationID();
        var femaleTransform = female.GetTransformationID();
        var maleAngle = SnapTo45(GetRelativeAngleFromArenaCenter(male.Position));
        var femaleAngle = SnapTo45(GetRelativeAngleFromArenaCenter(female.Position));
        float angle;
        var radius = OuterSafeRadius;

        if(maleTransform == TransformSword && femaleTransform == TransformStaff)
        {
            var amount = SwordStaffQuadrantOffsetDegrees;
            if(C.SwordStaffSpot == SwordStaffSpotMode.Outer)
            {
                amount = SwordStaffFarOffsetDegrees;
                radius = SwordStaffFarRadius;
            }

            var offset = GetQuadrantOffset(maleAngle, omegaCastId, amount, invertHorizontal: true);
            angle = femaleAngle + offset;
        }
        else if(maleTransform == TransformSword && femaleTransform == TransformFoot)
        {
            var offset = GetQuadrantOffset(femaleAngle, omegaCastId, SwordFootQuadrantOffsetDegrees, invertHorizontal: true);
            angle = femaleAngle + offset;
            radius = InnerSafeRadius;
        }
        else if(maleTransform == TransformShield && femaleTransform == TransformStaff)
        {
            var offset = GetQuadrantOffset(maleAngle, omegaCastId, ShieldStaffQuadrantOffsetDegrees, invertHorizontal: true);
            angle = maleAngle + offset;
        }
        else if(maleTransform == TransformShield && femaleTransform == TransformFoot)
        {
            var offset = GetQuadrantOffset(maleAngle, omegaCastId, ShieldFootQuadrantOffsetDegrees, invertHorizontal: true);
            angle = maleAngle + offset;
            radius = InnerSafeRadius;
        }
        else
        {
            return null;
        }

        return CalculatePointFromCenterByDegree(ArenaCenter, radius, angle);
    }

    // Signed angle offset (degrees) from horizontal vs vertical wave and NE/SW diagonal stance.
    private static float GetQuadrantOffset(float angle, uint omegaCastId, float amount, bool invertHorizontal = false)
    {
        var isNeSw = angle == NeSwDiagonalAngle1 || angle == NeSwDiagonalAngle2;
        var horizontalSign = isNeSw ? 1f : -1f;
        if(invertHorizontal) horizontalSign *= -1f;
        var sign = 0f;
        if(omegaCastId == CastWaveFirstHorizontal || omegaCastId == CastWaveFirstVertical)
            sign = omegaCastId == CastWaveFirstHorizontal ? horizontalSign : -horizontalSign;
        if(omegaCastId == CastWaveSecondHorizontal || omegaCastId == CastWaveSecondVertical)
            sign = omegaCastId == CastWaveSecondHorizontal ? horizontalSign : -horizontalSign;
        return amount * sign;
    }

    // Rounds angle to nearest 45° step in 0–360 range.
    private static float SnapTo45(float angle)
    {
        var normalized = NormalizeAngle(angle);
        return (float)(System.Math.Round(normalized / AngleSnap45) * AngleSnap45) % 360f;
    }

    // SnapTo45 as an integer degree in 0–359, for marker lookup.
    private static int SnapTo45Int(float angle)
        => (int)System.Math.Round(SnapTo45(angle)) % 360;

    // Wraps angle to [0, 360).
    private static float NormalizeAngle(float angle)
        => (angle % 360f + 360f) % 360f;

    // World XZ from center, radius, and compass angle in degrees (game convention).
    private static Vector3 CalculatePointFromCenterByDegree(Vector3 center, float radius, float degree)
    {
        var rad = degree.DegToRad();
        return new Vector3(
            center.X + System.MathF.Sin(rad) * radius,
            center.Y,
            center.Z - System.MathF.Cos(rad) * radius
        );
    }

    // Degrees from arena center toward position (same basis as safe spot math).
    private static float GetRelativeAngleFromArenaCenter(Vector3 position)
        => MathHelper.GetRelativeAngle(ArenaCenter, position);

    // Turns off a layout element by name if it exists.
    private void DisableElement(string name)
    {
        if(Controller.TryGetElementByName(name, out var element))
            element.Enabled = false;
    }
    #endregion
}
