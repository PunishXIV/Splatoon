using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;

public class P5_Dynamis_Omega_Safe_Guide : SplatoonScript
{
    #region Metadata
    public override Metadata Metadata { get; } = new(1, "mirage");
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
    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);

    private const int StepAwaitingFirstWave = 0;
    private const int StepAwaitingSecondWave = 1;
    private const int StepComplete = 2;

    private const int FirstOmegaPairVisibleCount = 2;
    private const int SecondOmegaPairMinVisibleCount = 4;

    private const float SwordStaffQuadrantOffsetDegrees = 67.5f;
    private const float SwordFootQuadrantOffsetDegrees = 32.5f;
    private const float ShieldStaffQuadrantOffsetDegrees = 45f;
    private const float ShieldFootQuadrantOffsetDegrees = 32.5f;

    private const float NeSwDiagonalAngle1 = 45f;
    private const float NeSwDiagonalAngle2 = 225f;

    // Not from RegisterElementFromCode JSON; used as tower line offZ in UpdateTowerNavigation.
    private const float TowerVerticalOffset = 10f;

    private const double RainbowHueCycleSeconds = 4d;
    #endregion

    #region Config
    // No IEZConfig in this script.
    #endregion

    #region State
    private readonly OmegaPairInfo _firstInfo = new();
    private readonly OmegaPairInfo _secondInfo = new();
    private int _step = StepAwaitingFirstWave;
    #endregion

    #region Private Class
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
    }

    public override void OnUpdate()
    {
        if(!IsPhaseFive())
        {
            DisableElement("first_navi");
            DisableElement("second_navi");
            DisableElement("first_navi_tower");
            DisableElement("second_navi_tower");
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
        ImGui.Text($"BasePlayer: {Controller.BasePlayer?.Name.ToString() ?? "null"}");
        ImGui.Text($"Scene: {Controller.Scene} (P5={SceneId})");
        ImGui.Text($"Step: {_step}");
        ImGui.Text($"First Cast: {FormatCast(_firstInfo.OmegaCastId)}");
        ImGui.Text($"Second Cast: {FormatCast(_secondInfo.OmegaCastId)}");
        ImGui.Separator();
        DrawPairDebug("First", _firstInfo);
        ImGui.Separator();
        DrawPairDebug("Second", _secondInfo);
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

    // Updates point nav element: position, tether, optional rainbow tint, enable state.
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
            element.color = ImGui.ColorConvertFloat4ToU32(GetRainbowColor(RainbowHueCycleSeconds));
        element.Enabled = true;
    }

    // Updates tower line element ref position, XZ offsets, height, rainbow, enable state.
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
        element.offZ = TowerVerticalOffset;
        if(rainbow)
            element.color = ImGui.ColorConvertFloat4ToU32(GetRainbowColor(RainbowHueCycleSeconds));
        element.Enabled = true;
    }

    // Draws ImGui debug lines for one Omega pair in script settings.
    private void DrawPairDebug(string label, OmegaPairInfo info)
    {
        ImGui.Text($"{label} Pair");
        ImGui.Text($"- Male ObjectId: {info.MaleObjectId}");
        ImGui.Text($"- Female ObjectId: {info.FemaleObjectId}");
        ImGui.Text($"- Ready: {info.IsReady}");

        var male = Svc.Objects.FirstOrDefault(x => x.ObjectId == info.MaleObjectId) as IBattleNpc;
        var female = Svc.Objects.FirstOrDefault(x => x.ObjectId == info.FemaleObjectId) as IBattleNpc;
        if(male == null || female == null)
        {
            ImGui.Text("- Objects: missing");
            return;
        }

        var maleTransform = male.GetTransformationID();
        var femaleTransform = female.GetTransformationID();
        var maleAngle = SnapTo45(GetRelativeAngleFromArenaCenter(male.Position));
        var femaleAngle = SnapTo45(GetRelativeAngleFromArenaCenter(female.Position));
        var safePos = GetSafePosition(male, female, info.OmegaCastId);

        ImGui.Text($"- Male Transform/Angle: {maleTransform} ({FormatMaleTransform(maleTransform)}) / {maleAngle:0.##}");
        ImGui.Text($"- Female Transform/Angle: {femaleTransform} ({FormatFemaleTransform(femaleTransform)}) / {femaleAngle:0.##}");
        ImGui.Text($"- Pair Type: {GetPairType(maleTransform, femaleTransform)}");
        ImGui.Text(safePos == null
            ? "- Safe Position: unresolved"
            : $"- Safe Position: X={safePos.Value.X:0.00}, Y={safePos.Value.Y:0.00}, Z={safePos.Value.Z:0.00}");
    }

    // Safe spot from male/female transformation pair and wave cast id; null if unsupported.
    private static Vector3? GetSafePosition(IBattleNpc male, IBattleNpc female, uint omegaCastId)
    {
        var maleTransform = male.GetTransformationID();
        var femaleTransform = female.GetTransformationID();
        var maleAngle = SnapTo45(GetRelativeAngleFromArenaCenter(male.Position));
        var femaleAngle = SnapTo45(GetRelativeAngleFromArenaCenter(female.Position));
        float angle;
        var radius = OuterSafeRadius;

        if(maleTransform == TransformSword && femaleTransform == TransformStaff)
        {
            var offset = GetQuadrantOffset(maleAngle, omegaCastId, SwordStaffQuadrantOffsetDegrees, invertHorizontal: true);
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

    // Short debug label for male+female transformation id pair.
    private static string GetPairType(ushort maleTransform, ushort femaleTransform)
    {
        if(maleTransform == TransformSword && femaleTransform == TransformStaff) return "Sword+Staff";
        if(maleTransform == TransformSword && femaleTransform == TransformFoot) return "Sword+Foot";
        if(maleTransform == TransformShield && femaleTransform == TransformStaff) return "Shield+Staff";
        if(maleTransform == TransformShield && femaleTransform == TransformFoot) return "Shield+Foot";
        return "Unknown";
    }

    // Display name for male Omega transformation id.
    private static string FormatMaleTransform(ushort transform)
    {
        if(transform == TransformSword) return "Sword";
        if(transform == TransformShield) return "Shield";
        return "Unknown";
    }

    // Display name for female Omega transformation id.
    private static string FormatFemaleTransform(ushort transform)
    {
        if(transform == TransformStaff) return "Staff";
        if(transform == TransformFoot) return "Foot";
        return "Unknown";
    }

    // Human-readable text for a tracked P5 wave cast id (settings UI).
    private static string FormatCast(uint castId)
    {
        if(castId == CastWaveFirstHorizontal) return $"{CastWaveFirstHorizontal} (First Horizontal)";
        if(castId == CastWaveFirstVertical) return $"{CastWaveFirstVertical} (First Vertical)";
        if(castId == CastWaveSecondHorizontal) return $"{CastWaveSecondHorizontal} (Second Horizontal)";
        if(castId == CastWaveSecondVertical) return $"{CastWaveSecondVertical} (Second Vertical)";
        return "0 (unset)";
    }

    // Full-saturation RGBA with hue cycling over time (highlight effect).
    private Vector4 GetRainbowColor(double cycleSeconds)
    {
        if(cycleSeconds <= 0d) cycleSeconds = 1d;
        var normalizedTime = Environment.TickCount64 / 1000d / cycleSeconds;
        var hue = normalizedTime % 1f;
        return HsvToVector4(hue, 1f, 1f);
    }

    // Converts HSV in 0–1 space to opaque RGBA for ImGui.ColorConvertFloat4ToU32.
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
            case 3: r = p; g = q; b = t; break;
            case 4: r = t; g = p; b = q; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new Vector4((float)r, (float)g, (float)b, 1f);
    }

    // Turns off a layout element by name if it exists.
    private void DisableElement(string name)
    {
        if(Controller.TryGetElementByName(name, out var element))
            element.Enabled = false;
    }
    #endregion
}
