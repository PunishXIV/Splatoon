using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;

public unsafe class P5_Dynamis_Sigma_Relative_Tower_Finder : SplatoonScript
{
    #region Metadata
    public override Metadata Metadata { get; } = new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryTop];
    #endregion

    #region Constant
    private const uint TerritoryTop = 1122;
    public const uint SceneId = 6;

    private const uint CastCodeDynamisSigma = 32788;
    private const uint CastWaveCannon = 31603;
    private const uint ActionIdSingleTower = 31492;
    private const uint ActionIdDualTower = 31493;

    public const uint TowerSingle = 2013245;
    public const uint TowerDual = 2013246;

    public const uint GlitchFar = 3428;
    public const uint GlitchMiddle = 3427;

    public const uint TowerCountGlitchFar = 5;
    public const uint TowerCountGlitchMiddle = 6;

    public const float AngleDiff = 45f;
    public const float PlayerRotateAngle = 22.5f;
    private const float AngleSnapStep = 22.5f;
    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);

    private const double RainbowHueCycleSeconds = 4d;
    #endregion

    #region Config
    // No IEZConfig in this script.
    #endregion

    #region State
    private bool _isSigma = false;
    private uint _currentGlitchStatus = 0;
    private float _localPlayerAngle = PlayerRotateAngle;
    private List<ObjectInfo> _allPlayerInfos = [];
    private List<ObjectInfo> _allTowerInfos = [];
    private List<ObjectInfo> _candidateTowerInfos = [];
    private ObjectInfo? _selectedTowerInfo = null;
    #endregion

    #region Private Class
    private readonly record struct ObjectInfo(IGameObject Object, float Angle);
    #endregion

    #region LifeCycle
    public override void OnSetup()
        => Controller.RegisterElementFromCode("navigation", """{"Name":"navigation","Enabled":false,"radius":3.0,"thicc":6.0,"fillIntensity":0.1,"tether":true}""", overwrite: true);

    public override void OnUpdate()
    {
        if(!IsPhaseFive()) return;
        if(!_isSigma)
        {
            DisableElement("navigation");
            return;
        }

        _currentGlitchStatus = GetCurrentGlitchStatus();
        if(_currentGlitchStatus == 0)
        {
            DisableElement("navigation");
            return;
        }

        if(!TryGetLocalPlayerInfo(out var localPlayerInfo))
        {
            DisableElement("navigation");
            return;
        }
        _localPlayerAngle = localPlayerInfo.Angle;

        _allTowerInfos = GetTowerInfos();
        if(!IsExpectedTowerCount(_currentGlitchStatus, _allTowerInfos.Count))
        {
            DisableElement("navigation");
            return;
        }

        var minAngle = NormalizeAngle(_localPlayerAngle - AngleDiff);
        var maxAngle = NormalizeAngle(_localPlayerAngle + AngleDiff);

        _candidateTowerInfos = _allTowerInfos
            .Where(info => IsAngleInRange(info.Angle, minAngle, maxAngle))
            .Where(info => info.Angle != _localPlayerAngle)
            .ToList();

        if(_candidateTowerInfos.Count == 1)
        {
            _selectedTowerInfo = _candidateTowerInfos[0];
        }
        else if(_candidateTowerInfos.Count == 2)
        {
            _selectedTowerInfo = _candidateTowerInfos.FirstOrDefault(x => x.Object.DataId == TowerDual);
        }
        else
        {
            _selectedTowerInfo = null;
        }

        UpdateNavigationElement(_selectedTowerInfo);
    }

    public override void OnReset()
    {
        _isSigma = false;
        _currentGlitchStatus = 0;
        _localPlayerAngle = PlayerRotateAngle;
        _allPlayerInfos = [];
        _allTowerInfos = [];
        _candidateTowerInfos = [];
        _selectedTowerInfo = null;
        DisableElement("navigation");
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!IsPhaseFive()) return;
        if(set.Action == null) return;

        var actionId = set.Action.Value.RowId;
        if(actionId == CastCodeDynamisSigma)
        {
            _isSigma = true;
            return;
        }

        if(!_isSigma) return;

        if(actionId == CastWaveCannon)
        {
            _allPlayerInfos = GetPlayerInfos();
            return;
        }

        if(actionId.EqualsAny(ActionIdSingleTower, ActionIdDualTower))
            OnReset();
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text($"This Strategy is called 「マクロ押さない式」 on JP server.");
        ImGui.Text($"This script find towers relative to the direction of the Wave Cannon Spread.");
        ImGui.Text($"Warning: This script not supports Playstation Alignment and Wave Cannon Spread.");

        ImGui.Separator();
        ImGui.Text($"BasePlayer: {BasePlayer?.Name.ToString() ?? "null"}");
        ImGui.Text($"Scene: {Controller.Scene}");
        ImGui.Text($"Is Sigma: {_isSigma}");
        ImGui.Text($"Glitch Status: {FormatGlitchStatus(_currentGlitchStatus)}");
        ImGui.Text($"Player Angle: {(_localPlayerAngle - PlayerRotateAngle)} + {PlayerRotateAngle} => {_localPlayerAngle}");
        ImGui.Text($"Tower Range: {(_localPlayerAngle - AngleDiff)} to {(_localPlayerAngle + AngleDiff)}");
        ImGui.Text($"Player Infos: {FormatObjectInfoList(_allPlayerInfos)}");
        ImGui.Text($"All Tower Infos: {FormatObjectInfoList(_allTowerInfos)}");
        ImGui.Text($"Candidate Tower Infos: {FormatObjectInfoList(_candidateTowerInfos)}");
        ImGui.Text($"Selected Tower Info: {_selectedTowerInfo?.Angle ?? 0f}");
    }
    #endregion

    #region Private Method
    // True when scene id is P5 Dynamis.
    private bool IsPhaseFive()
        => Controller.Scene == SceneId;

    // Checks tower count matches glitch debuff (far vs middle).
    private static bool IsExpectedTowerCount(uint glitchStatus, int towerCount)
        => (glitchStatus == GlitchFar && towerCount == TowerCountGlitchFar)
           || (glitchStatus == GlitchMiddle && towerCount == TowerCountGlitchMiddle);

    // Finds this player in the cached wave-cannon angle list.
    private bool TryGetLocalPlayerInfo(out ObjectInfo localPlayerInfo)
    {
        localPlayerInfo = default;
        if(BasePlayer == null) return false;

        foreach(var info in _allPlayerInfos)
        {
            if(info.Object.ObjectId == BasePlayer.ObjectId)
            {
                localPlayerInfo = info;
                return true;
            }
        }
        return false;
    }

    // True if angle lies on the shorter arc between min and max (handles wrap).
    private static bool IsAngleInRange(float angle, float minAngle, float maxAngle)
    {
        if(minAngle <= maxAngle)
            return minAngle <= angle && angle <= maxAngle;
        return minAngle <= angle || angle <= maxAngle;
    }

    // Normalizes to [0,360) and snaps to 22.5° steps.
    private static float NormalizeAngle(float angle)
    {
        var normalized = (angle + 360f) % 360f;
        return (float)(Math.Round(normalized / AngleSnapStep) * AngleSnapStep);
    }

    // Towers on field with compass angle from arena center.
    private List<ObjectInfo> GetTowerInfos()
        => Svc.Objects
            .Where(x => x.DataId.EqualsAny(TowerSingle, TowerDual))
            .Select(x => new ObjectInfo(x, NormalizeAngle(GetRelativeAngleFromArenaCenter(x.Position))))
            .OrderBy(x => x.Angle)
            .ToList();

    // Party PCs with wave-spread facing angle (player rotate offset applied).
    private List<ObjectInfo> GetPlayerInfos()
        => Svc.Objects.OfType<IPlayerCharacter>()
            .Select(x => new ObjectInfo(x, NormalizeAngle(GetRelativeAngleFromArenaCenter(x.Position) + PlayerRotateAngle)))
            .OrderBy(x => x.Angle)
            .Distinct()
            .ToList();

    // Glitch far/middle status id on BasePlayer, or 0.
    private uint GetCurrentGlitchStatus()
        => BasePlayer?.StatusList.FirstOrDefault(x => x.StatusId == GlitchFar || x.StatusId == GlitchMiddle)?.StatusId ?? 0;

    // Compass degrees from arena center toward position.
    private static float GetRelativeAngleFromArenaCenter(Vector3 position)
        => MathHelper.GetRelativeAngle(ArenaCenter, position);

    // Hides element when missing.
    private void DisableElement(string name)
    {
        if(Controller.TryGetElementByName(name, out var element))
            element.Enabled = false;
    }

    // Points navigation tether at chosen tower with rainbow color.
    private void UpdateNavigationElement(ObjectInfo? targetTowerInfo)
    {
        if(!Controller.TryGetElementByName("navigation", out var element))
            return;

        if(targetTowerInfo == null)
        {
            element.Enabled = false;
            return;
        }

        element.SetRefPosition(targetTowerInfo.Value.Object.Position);
        element.color = ImGui.ColorConvertFloat4ToU32(GetRainbowColor(RainbowHueCycleSeconds));
        element.Enabled = true;
    }

    // Short glitch label for debug UI.
    private static string FormatGlitchStatus(uint glitchStatus)
        => glitchStatus == GlitchFar ? "Far" : glitchStatus == GlitchMiddle ? "Middle" : "None";

    // One-line list of tower angles and single/dual type.
    private static string FormatObjectInfoList(List<ObjectInfo> infos)
        => $"[{string.Join(", ", infos.Select(x => $"{x.Angle} ({(x.Object.DataId == TowerSingle ? "Single" : "Dual")})"))}]";

    // Full-saturation hue cycle for highlight color.
    private Vector4 GetRainbowColor(double cycleSeconds)
    {
        if(cycleSeconds <= 0d) cycleSeconds = 1d;
        var normalizedTime = Environment.TickCount64 / 1000d / cycleSeconds;
        var hue = normalizedTime % 1f;
        return HsvToVector4(hue, 1f, 1f);
    }

    // HSV in 0–1 to opaque RGBA for ImGui.
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
