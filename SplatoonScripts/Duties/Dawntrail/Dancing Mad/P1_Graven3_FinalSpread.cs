using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

internal class P1_Graven3_FinalSpread : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(5, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryDmad];

    #endregion

    #region Constant

    private const uint TerritoryDmad = 1363;
    private const int SceneGraven3 = 5;

    private const uint KefkaDataId = 0x233C;
    private const uint CastIdTrueThunder = 47775;
    private const uint CastIdFalseThunder = 47777;

    private const string VfxKefkaTrue = "vfx/lockon/eff/m0462trg_c02c.avfx";
    private const string VfxKefkaFalse = "vfx/lockon/eff/m0462trg_c01c.avfx";
    private const string VfxPlayerSpread = "vfx/lockon/eff/m0462trg_a0c.avfx";
    private const string VfxPlayerStack = "vfx/lockon/eff/m0462trg_b0c.avfx";

    private const float DiagonalPosEpsilon = 0.05f;
    private static readonly float[] NwToSePosX = [96.46f, 75.25f];
    private static readonly float[] NeToSwPosX = [103.54f, 124.75f];

    private const string ElNwToSePreview1 = "NWtoSE_Preview1";
    private const string ElNwToSePreview2 = "NWtoSE_Preview2";
    private const string ElNeToSwPreview1 = "NEtoSW_Preview1";
    private const string ElNeToSwPreview2 = "NEtoSW_Preview2";
    private const string ElFinalSpreadLabel = "FinalSpreadLabel";
    private const string ElFinalStackLabel = "FinalStackLabel";

    private const string SpotElementTemplate =
        """{{"Name":"","Enabled":false,"refX":{0},"refY":{1},"radius":2.5,"Donut":0.2,"color":3355443455,"Filled":true,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"thicc":5.0,"FillStep":0.5}}""";

    private const string JsonNwToSePreview1 =
        """{"Name":"NWtoSE_Preview1","type":2,"Enabled":false,"refX":87.0,"refY":80.0,"refZ":1.9073486E-06,"offX":120.0,"offY":113.0,"radius":5.0,"color":3369009407,"fillIntensity":0.342,"thicc":15.0}""";

    private const string JsonNwToSePreview2 =
        """{"Name":"NWtoSE_Preview2","type":2,"Enabled":false,"refX":68.75,"refY":90.0,"offX":110.0,"offY":131.25,"radius":5.0,"color":3369009407,"fillIntensity":0.342,"thicc":15.0}""";

    private const string JsonNeToSwPreview1 =
        """{"Name":"NEtoSW_Preview1","type":2,"Enabled":false,"refX":111.058,"refY":81.875,"refZ":-1.9073486E-06,"offX":81.317,"offY":111.666,"offZ":-9.536743E-07,"radius":5.0,"color":3369009407,"fillIntensity":0.342,"thicc":15.0}""";

    private const string JsonNeToSwPreview2 =
        """{"Name":"NEtoSW_Preview2","type":2,"Enabled":false,"refX":125.482,"refY":95.716,"offX":91.959,"offY":129.332,"radius":5.0,"color":3369009407,"fillIntensity":0.342,"thicc":15.0}""";

    private const string JsonFinalSpreadLabel =
        """{"Name":"","type":1,"Enabled":false,"radius":0.0,"overlayTextColor":4244635647,"overlayVOffset":4.0,"overlayFScale":2.0,"overlayText":"Spread","refActorType":1}""";

    private const string JsonFinalStackLabel =
        """{"Name":"","type":1,"Enabled":false,"radius":0.0,"overlayTextColor":4244635647,"overlayVOffset":4.0,"overlayFScale":2.0,"overlayText":"Stack","refActorType":1}""";

    private static readonly Dictionary<DiagonalPattern, Dictionary<PartyRole, Vector2>> Spots = new()
    {
        [DiagonalPattern.NwToSe] = new()
        {
            [PartyRole.T1] = new(95.84355f, 95.92145f),
            [PartyRole.T2] = new(103.438614f, 89.30285f),
            [PartyRole.H1] = new(102.67517f, 117.08952f),
            [PartyRole.H2] = new(117.225464f, 103.184326f),
            [PartyRole.M1] = new(103.90559f, 104.109024f),
            [PartyRole.M2] = new(111.02388f, 96.948944f),
            [PartyRole.R1] = new(83.237434f, 96.709076f),
            [PartyRole.R2] = new(97.19673f, 82.95755f),
        },
        [DiagonalPattern.NeToSw] = new()
        {
            [PartyRole.T1] = new(89.63431f, 96.18922f),
            [PartyRole.T2] = new(95.97002f, 90.00216f),
            [PartyRole.H1] = new(97.74788f, 116.67111f),
            [PartyRole.H2] = new(116.792206f, 97.56387f),
            [PartyRole.M1] = new(96.91941f, 102.94722f),
            [PartyRole.M2] = new(102.55876f, 97.12093f),
            [PartyRole.R1] = new(83.207825f, 102.35189f),
            [PartyRole.R2] = new(102.67212f, 83.19874f),
        },
    };

    // v3 stack spots (plan.md L76-79).
    private static readonly Dictionary<DiagonalPattern, Dictionary<RoleGroup, Vector2>> StackSpots = new()
    {
        [DiagonalPattern.NwToSe] = new()
        {
            [RoleGroup.Dps] = new(95.78495f, 95.69587f),
            [RoleGroup.TankHealer] = new(104.37233f, 103.95186f),
        },
        [DiagonalPattern.NeToSw] = new()
        {
            [RoleGroup.Dps] = new(103.052155f, 96.77839f),
            [RoleGroup.TankHealer] = new(96.55574f, 103.384964f),
        },
    };

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();

    #endregion

    #region State

    private KefkaVFX _kefkaVfx = KefkaVFX.None;
    private PlayerVfxState _playerVfx = PlayerVfxState.None;
    private DiagonalPattern _pattern = DiagonalPattern.None;
    private FinalSolution _final = FinalSolution.Unsolved;

    #endregion

    #region Private Class

    private enum DiagonalPattern
    {
        None,
        NwToSe,
        NeToSw,
    }

    private enum PartyRole
    {
        T1,
        T2,
        H1,
        H2,
        M1,
        M2,
        R1,
        R2,
    }

    private enum FinalSolution
    {
        Unsolved,
        Spread,
        Stack,
    }

    private enum KefkaVFX
    {
        None,
        True,
        False,
    }

    private enum PlayerVfxState
    {
        None,
        Spread,
        Stack,
    }

    private enum RoleGroup
    {
        Dps,
        TankHealer,
    }

    private sealed class Config : IEzConfig
    {
        public PartyRole Role = PartyRole.T1;
        public bool IgnoreKefkaVfx;
        public bool ShowPreview;
        public DiagonalPattern PreviewPattern = DiagonalPattern.NwToSe;
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        foreach(var (pattern, roles) in Spots)
        {
            foreach(var (role, spot) in roles)
            {
                Controller.RegisterElementFromCode(
                    GetSpotElementName(pattern, role),
                    string.Format(CultureInfo.InvariantCulture, SpotElementTemplate, spot.X, spot.Y),
                    overwrite: true);
            }
        }

        foreach(var (pattern, groups) in StackSpots)
        {
            foreach(var (group, spot) in groups)
            {
                Controller.RegisterElementFromCode(
                    GetStackElementName(pattern, group),
                    string.Format(CultureInfo.InvariantCulture, SpotElementTemplate, spot.X, spot.Y),
                    overwrite: true);
            }
        }

        Controller.RegisterElementFromCode(ElNwToSePreview1, JsonNwToSePreview1, overwrite: true);
        Controller.RegisterElementFromCode(ElNwToSePreview2, JsonNwToSePreview2, overwrite: true);
        Controller.RegisterElementFromCode(ElNeToSwPreview1, JsonNeToSwPreview1, overwrite: true);
        Controller.RegisterElementFromCode(ElNeToSwPreview2, JsonNeToSwPreview2, overwrite: true);
        Controller.RegisterElementFromCode(ElFinalSpreadLabel, JsonFinalSpreadLabel, overwrite: true);
        Controller.RegisterElementFromCode(ElFinalStackLabel, JsonFinalStackLabel, overwrite: true);
    }

    public override void OnUpdate()
    {
        DisableAllElements();

        if(C.ShowPreview)
        {
            ApplyPreviewDisplay(C.PreviewPattern);
            return;
        }

        if(!IsPhaseActive())
        {
            ResetState();
            return;
        }

        if(_kefkaVfx == KefkaVFX.None || _playerVfx == PlayerVfxState.None)
        {
            _final = FinalSolution.Unsolved;
        }
        else
        {
            _final = ResolveFinalSolution(_playerVfx, _kefkaVfx);
        }

        if(_final == FinalSolution.Unsolved || _pattern == DiagonalPattern.None)
        {
            return;
        }

        ApplyMechanicDisplay(_final, _pattern);
    }

    public override void OnReset() => ResetState();

    public override void OnStartingCast(uint source, uint castId)
    {
        if(!IsPhaseActive() || _pattern != DiagonalPattern.None)
        {
            return;
        }

        if(castId is not (CastIdTrueThunder or CastIdFalseThunder))
        {
            return;
        }

        if(TryDetectPattern(out var pattern))
        {
            _pattern = pattern;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(!IsPhaseActive())
        {
            return;
        }

        if(_playerVfx == PlayerVfxState.None && TryMapPlayerVfxPath(vfxPath, out var playerVfx))
        {
            _playerVfx = playerVfx;
        }

        if(_kefkaVfx == KefkaVFX.None && TryMapKefkaVfxPath(vfxPath, out var kefkaVfx))
        {
            _kefkaVfx = kefkaVfx;
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.TextDisabled("Configuration");
        ImGui.Separator();
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Role", ref C.Role);
        ImGui.Checkbox("Ignore KefkaVFX (always true)", ref C.IgnoreKefkaVfx);

        ImGui.Spacing();
        ImGui.TextDisabled("Preview");
        ImGui.Separator();
        ImGui.Checkbox("Show all spots for preview", ref C.ShowPreview);
        if(C.ShowPreview)
        {
            var idx = C.PreviewPattern == DiagonalPattern.NwToSe ? 0 : 1;
            ImGui.SetNextItemWidth(200f);
            if(ImGui.Combo("Pattern", ref idx, "NWtoSE\0NEtoSW\0"))
            {
                C.PreviewPattern = idx == 0 ? DiagonalPattern.NwToSe : DiagonalPattern.NeToSw;
            }
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Debug");
        ImGui.Separator();

        if(C.ShowPreview)
        {
            ImGui.TextUnformatted($"Pattern: {GetPatternLabel(C.PreviewPattern)} (preview)");
        }
        else
        {
            ImGui.TextUnformatted(_pattern == DiagonalPattern.None ? "Pattern: (none)" : $"Pattern: {_pattern}");
        }

        ImGui.TextUnformatted(_kefkaVfx == KefkaVFX.None ? "KefkaVFX: (none)" : $"KefkaVFX: {_kefkaVfx}");
        ImGui.TextUnformatted(_playerVfx == PlayerVfxState.None ? "Player VFX: (none)" : $"Player VFX: {_playerVfx}");
        ImGui.TextUnformatted(_final == FinalSolution.Unsolved ? "FinalSolution: (unsolved)" : $"FinalSolution: {_final}");
    }

    #endregion

    #region Private Method

    // Clear overlays and latched mechanic state.
    private void ResetState()
    {
        _kefkaVfx = KefkaVFX.None;
        _playerVfx = PlayerVfxState.None;
        _pattern = DiagonalPattern.None;
        _final = FinalSolution.Unsolved;
        DisableAllElements();
        Controller.Hide();
    }

    // Disable every registered element before applying this frame's display.
    private void DisableAllElements()
    {
        foreach(var element in Controller.GetRegisteredElements().Values)
        {
            element.Enabled = false;
        }
    }

    // Preview: diagonal lines, all spread spots, and all stack spots.
    private void ApplyPreviewDisplay(DiagonalPattern pattern)
    {
        EnablePreviewElements(pattern);
        EnablePatternSpots(pattern, allRoles: true);
        EnableStackSpots(pattern, allGroups: true);
    }

    // Live mechanic: head-up label plus spread spot or stack spot for configured role.
    private void ApplyMechanicDisplay(FinalSolution final, DiagonalPattern pattern)
    {
        EnableElement(final == FinalSolution.Spread ? ElFinalSpreadLabel : ElFinalStackLabel);

        if(final == FinalSolution.Spread)
        {
            EnablePatternSpots(pattern, allRoles: false);
            return;
        }

        EnableStackSpot(pattern, GetRoleGroup(C.Role));
    }

    // Enable preview diagonal lines for the selected pattern.
    private void EnablePreviewElements(DiagonalPattern pattern)
    {
        switch(pattern)
        {
            case DiagonalPattern.NwToSe:
                EnableElement(ElNwToSePreview1);
                EnableElement(ElNwToSePreview2);
                break;
            case DiagonalPattern.NeToSw:
                EnableElement(ElNeToSwPreview1);
                EnableElement(ElNeToSwPreview2);
                break;
        }
    }

    // Enable spot circles for one role or all roles in preview mode.
    private void EnablePatternSpots(DiagonalPattern pattern, bool allRoles)
    {
        if(allRoles)
        {
            foreach(var role in Spots[pattern].Keys)
            {
                EnableSpotElement(GetSpotElementName(pattern, role));
            }

            return;
        }

        EnableSpotElement(GetSpotElementName(pattern, C.Role));
    }

    // Enable stack spot circles for one role group or both groups in preview mode.
    private void EnableStackSpots(DiagonalPattern pattern, bool allGroups)
    {
        if(allGroups)
        {
            foreach(var group in StackSpots[pattern].Keys)
            {
                EnableSpotElement(GetStackElementName(pattern, group));
            }

            return;
        }

        EnableStackSpot(pattern, GetRoleGroup(C.Role));
    }

    // Enable a single stack spot for the resolved pattern and role group.
    private void EnableStackSpot(DiagonalPattern pattern, RoleGroup group)
        => EnableSpotElement(GetStackElementName(pattern, group));

    // Enable a single registered element by name.
    private void EnableElement(string name)
    {
        if(Controller.TryGetElementByName(name, out var element))
        {
            element.Enabled = true;
        }
    }

    // Enable a spread/stack spot with rainbow tether when Attention Color is configured.
    private void EnableSpotElement(string name)
    {
        if(!Controller.TryGetElementByName(name, out var element))
        {
            return;
        }

        element.Enabled = true;
        element.tether = true;
        element.color = Controller.AttentionColor;
    }

    // Return whether Graven 3 scene 5 is active.
    private bool IsPhaseActive() => Controller.Scene == SceneGraven3;

    // Map spawned player spread/stack VFX path to state.
    private static bool TryMapPlayerVfxPath(string vfxPath, out PlayerVfxState playerVfx)
    {
        playerVfx = vfxPath switch
        {
            VfxPlayerSpread => PlayerVfxState.Spread,
            VfxPlayerStack => PlayerVfxState.Stack,
            _ => PlayerVfxState.None,
        };
        return playerVfx != PlayerVfxState.None;
    }

    // Map spawned Kefka true/false VFX path to state.
    private static bool TryMapKefkaVfxPath(string vfxPath, out KefkaVFX kefkaVfx)
    {
        kefkaVfx = vfxPath switch
        {
            VfxKefkaTrue => KefkaVFX.True,
            VfxKefkaFalse => KefkaVFX.False,
            _ => KefkaVFX.None,
        };
        return kefkaVfx != KefkaVFX.None;
    }

    // Enumerate Kefka actors casting true or false thunder.
    private static IEnumerable<IBattleNpc> FindKefkaThunderCasters()
        => Svc.Objects.OfType<IBattleNpc>()
            .Where(npc => npc.DataId == KefkaDataId
                && npc.IsCasting
                && (npc.CastActionId == CastIdTrueThunder || npc.CastActionId == CastIdFalseThunder));

    // Detect NWtoSE or NEtoSW from thunder caster X positions at cast start.
    private static bool TryDetectPattern(out DiagonalPattern pattern)
    {
        pattern = DiagonalPattern.None;
        var casters = FindKefkaThunderCasters().ToList();
        if(casters.Count == 0)
        {
            return false;
        }

        if(AnyCasterAtAnyX(casters, NwToSePosX))
        {
            pattern = DiagonalPattern.NwToSe;
            return true;
        }

        if(AnyCasterAtAnyX(casters, NeToSwPosX))
        {
            pattern = DiagonalPattern.NeToSw;
            return true;
        }

        return false;
    }

    // Map player spread/stack VFX to spread or stack per Kefka VFX true/false.
    private FinalSolution ResolveFinalSolution(PlayerVfxState playerVfx, KefkaVFX kefkaVfx)
    {
        if(C.IgnoreKefkaVfx)
        {
            kefkaVfx = KefkaVFX.True;
        }

        if(playerVfx == PlayerVfxState.Spread)
        {
            return kefkaVfx == KefkaVFX.True ? FinalSolution.Spread : FinalSolution.Stack;
        }

        return kefkaVfx == KefkaVFX.True ? FinalSolution.Stack : FinalSolution.Spread;
    }

    // Return whether any caster X is within epsilon of a pattern anchor.
    private static bool AnyCasterAtAnyX(IEnumerable<IBattleNpc> casters, float[] xs)
        => casters.Any(npc => xs.Any(x => MathF.Abs(npc.Position.X - x) < DiagonalPosEpsilon));

    // Build the registered spot element name for a pattern and role.
    private static string GetSpotElementName(DiagonalPattern pattern, PartyRole role)
        => $"{GetPatternLabel(pattern)}_{role}";

    // Build the registered stack element name for a pattern and role group.
    private static string GetStackElementName(DiagonalPattern pattern, RoleGroup group)
        => $"{GetPatternLabel(pattern)}_{GetRoleGroupLabel(group)}";

    // Map a configured party slot to DPS or tank/healer stack group.
    private static RoleGroup GetRoleGroup(PartyRole role)
        => role is PartyRole.M1 or PartyRole.M2 or PartyRole.R1 or PartyRole.R2
            ? RoleGroup.Dps
            : RoleGroup.TankHealer;

    // Return the display label for a stack role group.
    private static string GetRoleGroupLabel(RoleGroup group)
        => group switch
        {
            RoleGroup.Dps => "DPS",
            RoleGroup.TankHealer => "TankHealer",
            _ => "?",
        };

    // Return the display label for a diagonal pattern.
    private static string GetPatternLabel(DiagonalPattern pattern)
        => pattern switch
        {
            DiagonalPattern.NwToSe => "NWtoSE",
            DiagonalPattern.NeToSw => "NEtoSW",
            _ => "?",
        };

    #endregion
}
