using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

internal class P1_Graven3_FinalSpread : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryDmad];

    #endregion

    #region Constant

    private const uint TerritoryDmad = 1363;
    private const int SceneGraven3 = 5;

    private const uint KefkaDataId = 0x233C;
    private const uint SpreadActorDataId = 0x4C30;
    private const uint CastIdTrueThunder = 47775;
    private const uint CastIdFalseThunder = 47777;

    private const string VfxBossFireSpread = "vfx/lockon/eff/m0462trg_c02c.avfx";
    private const string VfxBossFireStack = "vfx/lockon/eff/m0462trg_c01c.avfx";
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

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();

    #endregion

    #region State

    // No persistent state.

    #endregion

    #region Private Class

    private enum DiagonalPattern
    {
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

    private enum SpreadStackKind
    {
        Spread,
        Stack,
    }

    private sealed class Config : IEzConfig
    {
        public PartyRole Role = PartyRole.T1;
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
            EnablePreviewElements(C.PreviewPattern);
            EnablePatternSpots(C.PreviewPattern, allRoles: true);
            return;
        }

        if(!TryGetActiveMechanic(out var pattern, out var keftaTrue, out var final))
            return;

        EnableFinalSpreadStackLabel(final);

        if(final == SpreadStackKind.Spread)
            EnablePatternSpots(pattern, allRoles: false);
    }

    public override void OnCombatStart() => ResetState();

    public override void OnReset() => ResetState();

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe))
            ResetState();
    }

    public override void OnSettingsDraw()
    {
        ImGui.TextDisabled("Configuration");
        ImGui.Separator();
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Role", ref C.Role);

        ImGui.Spacing();
        ImGui.TextDisabled("Preview");
        ImGui.Separator();
        ImGui.Checkbox("Show all spots for preview", ref C.ShowPreview);
        if(C.ShowPreview)
        {
            var idx = C.PreviewPattern == DiagonalPattern.NwToSe ? 0 : 1;
            ImGui.SetNextItemWidth(200f);
            if(ImGui.Combo("Pattern", ref idx, "NWtoSE\0NEtoSW\0"))
                C.PreviewPattern = idx == 0 ? DiagonalPattern.NwToSe : DiagonalPattern.NeToSw;
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Debug");
        ImGui.Separator();
        DrawPatternStatus();
        DrawMechanicStatus();
    }

    #endregion

    #region Private Method

    // Disable every registered element before applying this frame's display.
    private void DisableAllElements()
    {
        foreach(var element in Controller.GetRegisteredElements().Values)
            element.Enabled = false;
    }

    // Clear overlays on combat reset events.
    private void ResetState()
    {
        DisableAllElements();
        Controller.Hide();
    }

    // Return whether scene 5 thunder cast, marks, pattern, and Kefta are all ready.
    private bool TryGetActiveMechanic(out DiagonalPattern pattern, out bool keftaTrue, out SpreadStackKind final)
    {
        pattern = default;
        keftaTrue = false;
        final = default;

        if(!IsPhaseActive()
            || !FindKefkaThunderCasters().Any()
            || !TryGetPartyMarkState(out var hasSpread, out var hasStack)
            || (!hasSpread && !hasStack)
            || !TryDetectPattern(out pattern)
            || !TryGetKeftaIsTrue(out keftaTrue)
            || !TryResolveSpreadStackMark(hasSpread, hasStack, C.Role, out var mark))
            return false;

        final = ResolveFinalSpreadStack(mark, keftaTrue);
        return true;
    }

    // Enable a single registered element by name.
    private void EnableElement(string name)
    {
        if(Controller.TryGetElementByName(name, out var element))
            element.Enabled = true;
    }

    // Enable the resolved spread or stack head-up label.
    private void EnableFinalSpreadStackLabel(SpreadStackKind final)
    {
        EnableElement(final == SpreadStackKind.Spread ? ElFinalSpreadLabel : ElFinalStackLabel);
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
                EnableSpotElement(GetSpotElementName(pattern, role));
            return;
        }

        EnableSpotElement(GetSpotElementName(pattern, C.Role));
    }

    // Enable a spread spot with rainbow tether when Attention Color is configured.
    private void EnableSpotElement(string name)
    {
        if(!Controller.TryGetElementByName(name, out var element))
            return;

        element.Enabled = true;
        element.tether = true;
        element.color = Controller.AttentionColor;
    }

    // Draw live or preview diagonal pattern status in settings.
    private void DrawPatternStatus()
    {
        if(C.ShowPreview)
        {
            ImGui.TextUnformatted($"Pattern: {GetPatternLabel(C.PreviewPattern)} (preview)");
            return;
        }

        if(!IsPhaseActive())
        {
            ImGui.TextUnformatted("Pattern: (inactive)");
            return;
        }

        if(!TryDetectPattern(out var pattern))
        {
            ImGui.TextUnformatted("Pattern: (no match)");
            return;
        }

        ImGui.TextUnformatted($"Pattern: {GetPatternLabel(pattern)}");
    }

    // Draw Kefta, party marks, and resolved final spread/stack in settings.
    private void DrawMechanicStatus()
    {
        if(!IsPhaseActive())
        {
            ImGui.TextUnformatted("Kefta: (inactive)");
            ImGui.TextUnformatted("Final: (inactive)");
            return;
        }

        if(!TryGetKeftaIsTrue(out var keftaTrue))
        {
            ImGui.TextUnformatted("Kefta: (unknown)");
            ImGui.TextUnformatted("Final: (unknown)");
            DrawMarksStatus();
            return;
        }

        ImGui.TextUnformatted(keftaTrue ? "Kefta: True" : "Kefta: False");
        DrawMarksStatus();

        if(!TryGetPartyMarkState(out var hasSpread, out var hasStack)
            || !TryResolveSpreadStackMark(hasSpread, hasStack, C.Role, out var mark))
        {
            ImGui.TextUnformatted("Final: (waiting for marks)");
            return;
        }

        ImGui.TextUnformatted($"Final: {ResolveFinalSpreadStack(mark, keftaTrue)}");
    }

    // Draw spread/stack party mark presence in settings.
    private static void DrawMarksStatus()
    {
        if(!TryGetPartyMarkState(out var hasSpread, out var hasStack))
        {
            ImGui.TextUnformatted("Marks: (none)");
            return;
        }

        ImGui.TextUnformatted($"Marks: {FormatPartyMarks(hasSpread, hasStack)}");
    }

    // Return whether Graven 3 scene 5 is active.
    private bool IsPhaseActive() => Controller.Scene == SceneGraven3;

    // Enumerate Kefka actors casting true or false thunder.
    private static IEnumerable<IBattleNpc> FindKefkaThunderCasters()
        => Svc.Objects.OfType<IBattleNpc>()
            .Where(npc => npc.DataId == KefkaDataId
                && npc.IsCasting
                && (npc.CastActionId == CastIdTrueThunder || npc.CastActionId == CastIdFalseThunder));

    // Scan all PCs for spread and stack lock-on VFX.
    private static bool TryGetPartyMarkState(out bool hasSpread, out bool hasStack)
    {
        hasSpread = false;
        hasStack = false;

        foreach(var obj in Svc.Objects.Where(obj => obj.ObjectKind == ObjectKind.Pc))
        {
            if(AttachedInfo.TryGetSpecificVfxInfo(obj, VfxPlayerSpread, out _))
                hasSpread = true;
            if(AttachedInfo.TryGetSpecificVfxInfo(obj, VfxPlayerStack, out _))
                hasStack = true;

            if(hasSpread && hasStack)
                return true;
        }

        return hasSpread || hasStack;
    }

    // Read spread or stack lock-on VFX on one game object.
    private static bool TryGetSpreadStackMark(IGameObject obj, out SpreadStackKind mark)
    {
        mark = default;
        var hasSpread = AttachedInfo.TryGetSpecificVfxInfo(obj, VfxPlayerSpread, out _);
        var hasStack = AttachedInfo.TryGetSpecificVfxInfo(obj, VfxPlayerStack, out _);

        if(hasSpread && !hasStack)
        {
            mark = SpreadStackKind.Spread;
            return true;
        }

        if(hasStack && !hasSpread)
        {
            mark = SpreadStackKind.Stack;
            return true;
        }

        return false;
    }

    // Read spread/stack mark on the party member for the configured role slot.
    private bool TryGetRoleSpreadStackMark(PartyRole role, out SpreadStackKind mark)
    {
        mark = default;
        return TryGetPartyMemberForRole(role, out var player) && TryGetSpreadStackMark(player, out mark);
    }

    // Resolve party-wide marks, or the role slot when both spread and stack exist.
    private bool TryResolveSpreadStackMark(bool hasSpread, bool hasStack, PartyRole role, out SpreadStackKind mark)
    {
        mark = default;

        if(hasSpread && !hasStack)
        {
            mark = SpreadStackKind.Spread;
            return true;
        }

        if(hasStack && !hasSpread)
        {
            mark = SpreadStackKind.Stack;
            return true;
        }

        if(!hasSpread && !hasStack)
            return false;

        return TryGetRoleSpreadStackMark(role, out mark);
    }

    // Map a role slot to a party member by job category order.
    private bool TryGetPartyMemberForRole(PartyRole role, out IPlayerCharacter player)
    {
        player = null!;
        var members = Controller.GetPartyMembers().ToList();
        if(members.Count == 0)
            return false;

        var tanks = members.Where(x => x.GetRole() == CombatRole.Tank).ToList();
        var healers = members.Where(x => x.GetRole() == CombatRole.Healer).ToList();
        var dps = members.Where(x => x.GetRole() == CombatRole.DPS).ToList();

        var resolved = role switch
        {
            PartyRole.T1 => tanks.ElementAtOrDefault(0),
            PartyRole.T2 => tanks.ElementAtOrDefault(1),
            PartyRole.H1 => healers.ElementAtOrDefault(0),
            PartyRole.H2 => healers.ElementAtOrDefault(1),
            PartyRole.M1 => dps.ElementAtOrDefault(0),
            PartyRole.M2 => dps.ElementAtOrDefault(1),
            PartyRole.R1 => dps.ElementAtOrDefault(2),
            PartyRole.R2 => dps.ElementAtOrDefault(3),
            _ => null,
        };

        if(resolved == null)
            return false;

        player = resolved;
        return true;
    }

    // Read Kefta true/false from the spread actor boss-fire VFX.
    private static bool TryGetKeftaIsTrue(out bool isTrue)
    {
        isTrue = false;
        var actor = Svc.Objects.FirstOrDefault(obj => obj.DataId == SpreadActorDataId);
        if(actor == null)
            return false;

        if(AttachedInfo.TryGetSpecificVfxInfo(actor, VfxBossFireSpread, out _))
        {
            isTrue = true;
            return true;
        }

        return AttachedInfo.TryGetSpecificVfxInfo(actor, VfxBossFireStack, out _);
    }

    // Detect NWtoSE or NEtoSW from thunder caster X positions.
    private static bool TryDetectPattern(out DiagonalPattern pattern)
    {
        pattern = default;
        var casters = FindKefkaThunderCasters().ToList();
        if(casters.Count == 0)
            return false;

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

    // Apply Kefta inversion to the resolved spread/stack mark.
    private static SpreadStackKind ResolveFinalSpreadStack(SpreadStackKind mark, bool keftaTrue)
        => keftaTrue ? mark : mark == SpreadStackKind.Spread ? SpreadStackKind.Stack : SpreadStackKind.Spread;

    // Return whether any caster X is within epsilon of a pattern anchor.
    private static bool AnyCasterAtAnyX(IEnumerable<IBattleNpc> casters, float[] xs)
        => casters.Any(npc => xs.Any(x => MathF.Abs(npc.Position.X - x) < DiagonalPosEpsilon));

    // Build the registered spot element name for a pattern and role.
    private static string GetSpotElementName(DiagonalPattern pattern, PartyRole role)
        => $"{GetPatternLabel(pattern)}_{role}";

    // Return the display label for a diagonal pattern.
    private static string GetPatternLabel(DiagonalPattern pattern)
        => pattern switch
        {
            DiagonalPattern.NwToSe => "NWtoSE",
            DiagonalPattern.NeToSw => "NEtoSW",
            _ => "?",
        };

    // Return settings text for spread/stack party mark presence.
    private static string FormatPartyMarks(bool hasSpread, bool hasStack)
        => (hasSpread, hasStack) switch
        {
            (true, true) => "Spread + Stack",
            (true, false) => "Spread",
            (false, true) => "Stack",
            _ => "(none)",
        };

    #endregion
}
