using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.Hooks;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;

public class P3_Oversampled_Wave_Cannon_Cross : SplatoonScript
{
    #region Metadata

    public override Metadata Metadata { get; } = new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryTop];

    #endregion

    #region Constant

    private const uint TerritoryTop = 1122;
    private const int SceneOversampledWaveCannon = 4;

    private const uint CastCannonRight = 31595;
    private const uint CastCannonLeft = 31596;

    private const uint StatusCannonRight = 3452;
    private const uint StatusCannonLeft = 3453;

    private const int PartySize = 8;
    private const int CannonCountRequired = 3;

    private const string LayoutNavi = "Navi";
    private const string LayoutCone = "Cone";
    private const string ElSwapHint = "SwapHint";

    private static readonly int[] Group03 = [0, 1, 2, 3];
    private static readonly int[] Group47 = [4, 5, 6, 7];

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();

    public sealed class Config : IEzConfig
    {
        public PriorityData PriorityData = new();
        public bool ShowMonitorCone = true;
    }

    #endregion

    #region State

    private OmegaCannonSide? _omegaCannonSide;
    private string? _activeNaviName;
    private bool _showSwapHint;
    private int _debugHomeIndex = -1;
    private int _debugFinalSlot = -1;
    private string _debugInitialCannons = string.Empty;
    private string _debugFinalCannons = string.Empty;
    private string _debugSwaps = string.Empty;

    #endregion

    #region Private Class

    private enum OmegaCannonSide
    {
        Right,
        Left,
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        Controller.TryRegisterLayoutFromCode(
            LayoutNavi,
            """~Lv2~{"Name":"Navi","ZoneLockH":[1122],"ElementsL":[{"Name":"N IN NoCannon Right","type":1,"offX":1.0,"offY":9.0,"refActorDataID":15717,"refActorRequireBuffsInvert":true,"refActorComparisonType":3,"includeRotation":true},{"Name":"N IN NoCannon Left","type":1,"offX":-1.0,"offY":9.0,"refActorDataID":15717,"refActorRequireBuffsInvert":true,"refActorComparisonType":3,"includeRotation":true},{"Name":"N IN IsCannon Right","type":1,"offX":3.0,"offY":9.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"N IN IsCannon Left","type":1,"offX":-3.0,"offY":9.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"N OUT NoCannon Right","type":1,"offX":1.0,"offY":19.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"N OUT NoCannon Left","type":1,"offX":-1.0,"offY":19.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"N OUT IsCannon Right","type":1,"offX":3.0,"offY":19.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"N OUT IsCannon Left","type":1,"offX":-3.0,"offY":19.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"S IN NoCannon Right","type":1,"offX":1.0,"offY":-9.0,"refActorDataID":15717,"refActorRequireBuffsInvert":true,"refActorComparisonType":3,"includeRotation":true},{"Name":"S IN NoCannon Left","type":1,"offX":-1.0,"offY":-9.0,"refActorDataID":15717,"refActorRequireBuffsInvert":true,"refActorComparisonType":3,"includeRotation":true},{"Name":"S IN IsCannon Right","type":1,"offX":3.0,"offY":-9.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"S IN IsCannon Left","type":1,"offX":-3.0,"offY":-9.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"S OUT NoCannon Right","type":1,"offX":1.0,"offY":-19.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"S OUT NoCannon Left","type":1,"offX":-1.0,"offY":-19.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"S OUT IsCannon Right","type":1,"offX":3.0,"offY":-19.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"S OUT IsCannon Left","type":1,"offX":-3.0,"offY":-19.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"E IN IsCannon Top","type":1,"offX":9.0,"offY":3.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"E IN IsCannon Bottom","type":1,"offX":9.0,"offY":-3.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"E IN NoCannon Center","type":1,"offX":9.0,"refActorDataID":15717,"refActorRequireBuffsInvert":true,"refActorComparisonType":3,"includeRotation":true},{"Name":"E OUT IsCannon Top","type":1,"offX":19.0,"offY":3.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"E OUT IsCannon Bottom","type":1,"offX":19.0,"offY":-3.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"E OUT NoCannon Center","type":1,"offX":19.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"W IN IsCannon Top","type":1,"offX":-9.0,"offY":3.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"W IN IsCannon Bottom","type":1,"offX":-9.0,"offY":-3.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"W IN NoCannon Center","type":1,"offX":-9.0,"refActorDataID":15717,"refActorRequireBuffsInvert":true,"refActorComparisonType":3,"includeRotation":true},{"Name":"W OUT IsCannon Top","type":1,"offX":-19.0,"offY":3.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"W OUT IsCannon Bottom","type":1,"offX":-19.0,"offY":-3.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"W OUT NoCannon Center","type":1,"offX":-19.0,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true}]}""",
            out _,
            overwrite: true);

        Controller.TryRegisterLayoutFromCode(
            LayoutCone,
            """~Lv2~{"Name":"Cone","ZoneLockH":[1122],"Enabled":false,"ElementsL":[{"Name":"N IN IsCannon Right","type":4,"offX":3.0,"offY":9.0,"radius":3.0,"coneAngleMin":0,"coneAngleMax":180,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"N IN IsCannon Left","type":4,"offX":-3.0,"offY":9.0,"radius":3.0,"coneAngleMin":180,"coneAngleMax":360,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"N OUT IsCannon Right","type":4,"offX":3.0,"offY":19.0,"radius":3.0,"coneAngleMin":0,"coneAngleMax":180,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"N OUT IsCannon Left","type":4,"offX":-3.0,"offY":19.0,"radius":3.0,"coneAngleMin":180,"coneAngleMax":360,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"S IN IsCannon Right","type":4,"offX":3.0,"offY":-9.0,"radius":3.0,"coneAngleMin":0,"coneAngleMax":180,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"S IN IsCannon Left","type":4,"offX":-3.0,"offY":-9.0,"radius":3.0,"coneAngleMin":180,"coneAngleMax":360,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"S OUT IsCannon Right","type":4,"offX":3.0,"offY":-19.0,"radius":3.0,"coneAngleMin":0,"coneAngleMax":180,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"S OUT IsCannon Left","type":4,"offX":-3.0,"offY":-19.0,"radius":3.0,"coneAngleMin":180,"coneAngleMax":360,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"E IN IsCannon Top","type":4,"offX":9.0,"offY":3.0,"radius":3.0,"coneAngleMin":-90,"coneAngleMax":90,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"E IN IsCannon Bottom","type":4,"offX":9.0,"offY":-3.0,"radius":3.0,"coneAngleMin":90,"coneAngleMax":270,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"E OUT IsCannon Top","type":4,"offX":19.0,"offY":3.0,"radius":3.0,"coneAngleMin":-90,"coneAngleMax":90,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"E OUT IsCannon Bottom","type":4,"offX":19.0,"offY":-3.0,"radius":3.0,"coneAngleMin":90,"coneAngleMax":270,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"W IN IsCannon Top","type":4,"offX":-9.0,"offY":3.0,"radius":3.0,"coneAngleMin":-90,"coneAngleMax":90,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"W IN IsCannon Bottom","type":4,"offX":-9.0,"offY":-3.0,"radius":3.0,"coneAngleMin":90,"coneAngleMax":270,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"W OUT IsCannon Top","type":4,"offX":-19.0,"offY":3.0,"radius":3.0,"coneAngleMin":-90,"coneAngleMax":90,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true},{"Name":"W OUT IsCannon Bottom","type":4,"offX":-19.0,"offY":-3.0,"radius":3.0,"coneAngleMin":90,"coneAngleMax":270,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true}]}""",
            out _,
            overwrite: true);

        Controller.RegisterElementFromCode(
            ElSwapHint,
            """{"Name":"SwapHint","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"Swap","overlayTextIntl":{"Jp":"入れ替え"},"refActorType":1}""",
            overwrite: true);

        DisableAllGuides();
    }

    public override void OnUpdate()
    {
        if(!IsActiveScene())
        {
            DisableAllGuides();
            return;
        }

        if(_omegaCannonSide is null)
        {
            DisableAllGuides();
            return;
        }

        if(!TryResolveAssignment(out var homeIndex, out var initialCannons, out var finalSlot, out var finalCannons, out var myPairSwaps))
        {
            DisableAllGuides();
            return;
        }

        var naviName = ResolveNaviElementName(finalSlot, finalCannons, _omegaCannonSide.Value);
        if(naviName is null)
        {
            DisableAllGuides();
            return;
        }

        _debugHomeIndex = homeIndex;
        _debugFinalSlot = finalSlot;
        _debugInitialCannons = string.Join(",", initialCannons.OrderBy(x => x));
        _debugFinalCannons = string.Join(",", finalCannons.OrderBy(x => x));
        _debugSwaps = string.Join(",", myPairSwaps.Select(p => $"{p.A}<->{p.B}"));
        _showSwapHint = myPairSwaps.Count > 0;
        _activeNaviName = naviName;

        ShowNavi(naviName, _showSwapHint);
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(!IsActiveScene()) return;

        if(castId == CastCannonRight)
        {
            _omegaCannonSide = OmegaCannonSide.Right;
        }
        else if(castId == CastCannonLeft)
        {
            _omegaCannonSide = OmegaCannonSide.Left;
        }
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
        {
            OnReset();
        }
    }

    public override void OnReset()
    {
        _omegaCannonSide = null;
        _activeNaviName = null;
        _showSwapHint = false;
        _debugHomeIndex = -1;
        _debugFinalSlot = -1;
        _debugInitialCannons = string.Empty;
        _debugFinalCannons = string.Empty;
        _debugSwaps = string.Empty;
        DisableAllGuides();
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Priority: T1 T2 H1 H2 M1 M2 R1 R2 (indices 0-7)");
        C.PriorityData.Draw();
        ImGui.Checkbox("Show monitor cone (IsCannon)", ref C.ShowMonitorCone);

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text($"Scene: {Controller.Scene} (need {SceneOversampledWaveCannon})");
            ImGui.Text($"Omega Cannon Side: {_omegaCannonSide?.ToString() ?? "null"}");
            ImGui.Text($"Home index: {_debugHomeIndex}");
            ImGui.Text($"Final slot: {_debugFinalSlot}");
            ImGui.Text($"Initial cannons: {_debugInitialCannons}");
            ImGui.Text($"Final cannons: {_debugFinalCannons}");
            ImGui.Text($"Swaps: {_debugSwaps}");
            ImGui.Text($"Show SwapHint: {_showSwapHint}");
            ImGui.Text($"Active navi: {_activeNaviName ?? "none"}");
            ImGui.Text($"BasePlayer: {BasePlayer?.Name.ToString() ?? "null"}");
        }
    }

    #endregion

    #region Private Method

    // True when zone scene is P3 oversampled wave cannon.
    private bool IsActiveScene() => Controller.Scene == SceneOversampledWaveCannon;

    // Resolves home index, cannons, final slot after swaps for local player.
    private bool TryResolveAssignment(
        out int homeIndex,
        out HashSet<int> initialCannons,
        out int finalSlot,
        out HashSet<int> finalCannons,
        out List<(int A, int B)> myPairSwaps)
    {
        homeIndex = -1;
        finalSlot = -1;
        initialCannons = [];
        finalCannons = [];
        myPairSwaps = [];

        homeIndex = C.PriorityData.GetOwnIndex(_ => true);
        if(homeIndex < 0) return false;

        var priority = C.PriorityData.GetPlayers(_ => true);
        if(priority is null || priority.Count < PartySize) return false;

        for(var i = 0; i < PartySize; i++)
        {
            if(priority[i].IGameObject is not IPlayerCharacter pc) continue;
            if(HasCannonStatus(pc))
            {
                initialCannons.Add(i);
            }
        }

        if(initialCannons.Count != CannonCountRequired) return false;

        var swapPairs = ResolveSwapPairs(initialCannons);
        finalCannons = ApplyCannonSwaps(initialCannons, swapPairs);
        finalSlot = ApplySlotSwaps(homeIndex, swapPairs);
        var resolvedHomeIndex = homeIndex;
        myPairSwaps = swapPairs.Where(p => p.A == resolvedHomeIndex || p.B == resolvedHomeIndex).ToList();
        return true;
    }

    // Returns pairs that must swap for the given pre-swap cannon index set.
    private static List<(int A, int B)> ResolveSwapPairs(HashSet<int> cannons)
    {
        var result = new List<(int A, int B)>();
        if(ShouldSwapPair01(cannons)) result.Add((0, 1));
        if(ShouldSwapPair23(cannons)) result.Add((2, 3));
        if(ShouldSwapPair45(cannons)) result.Add((4, 5));
        if(ShouldSwapPair67(cannons)) result.Add((6, 7));
        return result;
    }

    // Pair 0↔1 swap conditions from priority group 0-3.
    private static bool ShouldSwapPair01(HashSet<int> cannons)
    {
        var g = GroupIndices(cannons, Group03);
        return g.Count switch
        {
            1 => g.SetEquals([0]),
            2 => g.SetEquals([0, 2]) || g.SetEquals([1, 3]),
            3 => g.SetEquals([0, 2, 3]),
            _ => false,
        };
    }

    // Pair 2↔3 swap conditions from priority group 0-3.
    private static bool ShouldSwapPair23(HashSet<int> cannons)
    {
        var g = GroupIndices(cannons, Group03);
        return g.Count switch
        {
            1 => g.SetEquals([2]),
            2 => false,
            3 => g.SetEquals([0, 1, 2]),
            _ => false,
        };
    }

    // Pair 4↔5 swap conditions from priority group 4-7.
    private static bool ShouldSwapPair45(HashSet<int> cannons)
    {
        var g = GroupIndices(cannons, Group47);
        return g.Count switch
        {
            1 => g.SetEquals([5]),
            2 => g.SetEquals([4, 6]) || g.SetEquals([5, 7]),
            3 => g.SetEquals([5, 6, 7]),
            _ => false,
        };
    }

    // Pair 6↔7 swap conditions from priority group 4-7.
    private static bool ShouldSwapPair67(HashSet<int> cannons)
    {
        var g = GroupIndices(cannons, Group47);
        return g.Count switch
        {
            1 => g.SetEquals([7]),
            2 => false,
            3 => g.SetEquals([4, 5, 7]),
            _ => false,
        };
    }

    // Intersects cannon indices with a priority group.
    private static HashSet<int> GroupIndices(HashSet<int> cannons, int[] group)
        => cannons.Where(group.Contains).ToHashSet();

    // Moves cannon flags across swap pairs (simultaneous).
    private static HashSet<int> ApplyCannonSwaps(HashSet<int> initialCannons, List<(int A, int B)> swapPairs)
    {
        var result = new HashSet<int>(initialCannons);
        foreach(var (a, b) in swapPairs)
        {
            var aHas = initialCannons.Contains(a);
            var bHas = initialCannons.Contains(b);
            if(aHas == bHas) continue;
            if(aHas)
            {
                result.Remove(a);
                result.Add(b);
            }
            else
            {
                result.Remove(b);
                result.Add(a);
            }
        }

        return result;
    }

    // Maps home index to final slot after simultaneous pair swaps.
    private static int ApplySlotSwaps(int homeIndex, List<(int A, int B)> swapPairs)
    {
        foreach(var (a, b) in swapPairs)
        {
            if(homeIndex == a) return b;
            if(homeIndex == b) return a;
        }

        return homeIndex;
    }

    // Picks navi element for final slot, post-swap cannons, and Omega side.
    private static string? ResolveNaviElementName(int finalSlot, HashSet<int> finalCannons, OmegaCannonSide omegaSide)
    {
        var invertRight = omegaSide == OmegaCannonSide.Left;
        return finalSlot switch
        {
            0 or 2 => ResolveNorth(finalSlot, finalCannons, invertRight),
            5 or 7 => ResolveSouth(finalSlot, finalCannons, invertRight),
            1 or 3 => ResolveEast(finalSlot, finalCannons),
            4 or 6 => ResolveWest(finalSlot, finalCannons),
            _ => null,
        };
    }

    // North [0=IN, 2=OUT]: invert Omega Left/Right.
    private static string ResolveNorth(int slot, HashSet<int> cannons, bool invertRight)
    {
        var hasIn = cannons.Contains(0);
        var hasOut = cannons.Contains(2);
        var isIn = slot == 0;
        var isCannon = isIn ? hasIn : hasOut;
        var side = invertRight ? "Right" : "Left";
        var depth = isIn ? "IN" : "OUT";
        var role = isCannon ? "IsCannon" : "NoCannon";
        return $"N {depth} {role} {side}";
    }

    // South [5=IN, 7=OUT]: invert Omega Left/Right.
    private static string ResolveSouth(int slot, HashSet<int> cannons, bool invertRight)
    {
        var hasIn = cannons.Contains(5);
        var hasOut = cannons.Contains(7);
        var isIn = slot == 5;
        var isCannon = isIn ? hasIn : hasOut;
        var side = invertRight ? "Right" : "Left";
        var depth = isIn ? "IN" : "OUT";
        var role = isCannon ? "IsCannon" : "NoCannon";
        return $"S {depth} {role} {side}";
    }

    // East [1=IN, 3=OUT]: Top/Bottom pair rule.
    private static string ResolveEast(int slot, HashSet<int> cannons)
    {
        var hasIn = cannons.Contains(1);
        var hasOut = cannons.Contains(3);
        var isIn = slot == 1;
        var isCannon = isIn ? hasIn : hasOut;
        var depth = isIn ? "IN" : "OUT";
        var role = isCannon ? "IsCannon" : "NoCannon";

        string offset;
        if(hasIn && hasOut)
        {
            // Both have cannon: IN Top / OUT Bottom.
            offset = isIn ? "Top" : "Bottom";
        }
        else if(isCannon)
        {
            // Single cannon on this arm always uses Bottom.
            offset = "Bottom";
        }
        else
        {
            offset = "Center";
        }

        return $"E {depth} {role} {offset}";
    }

    // West [4=IN, 6=OUT]: Top/Bottom pair rule.
    private static string ResolveWest(int slot, HashSet<int> cannons)
    {
        var hasIn = cannons.Contains(4);
        var hasOut = cannons.Contains(6);
        var isIn = slot == 4;
        var isCannon = isIn ? hasIn : hasOut;
        var depth = isIn ? "IN" : "OUT";
        var role = isCannon ? "IsCannon" : "NoCannon";

        string offset;
        if(hasIn && hasOut)
        {
            // Both have cannon: IN Bottom / OUT Top.
            offset = isIn ? "Bottom" : "Top";
        }
        else if(isCannon)
        {
            // Single cannon on this arm always uses Top.
            offset = "Top";
        }
        else
        {
            offset = "Center";
        }

        return $"W {depth} {role} {offset}";
    }

    // Enables Navi circle, optional Cone half, and SwapHint when swap is required.
    private void ShowNavi(string naviName, bool showSwapHint)
    {
        DisableAllGuides();

        if(Controller.TryGetLayoutByName(LayoutNavi, out var naviLayout))
        {
            naviLayout.Enabled = true;
            foreach(var element in naviLayout.ElementsL)
            {
                if(element.Name != naviName)
                {
                    element.Enabled = false;
                    element.tether = false;
                    continue;
                }

                element.Enabled = true;
                element.tether = true;
                element.thicc = 5f;
                element.radius = 0.5f;
                element.color = Controller.AttentionColor;
            }
        }

        // Cone layout: IsCannon names only, no tether, toggled by config.
        if(C.ShowMonitorCone
            && naviName.Contains("IsCannon")
            && Controller.TryGetLayoutByName(LayoutCone, out var coneLayout))
        {
            coneLayout.Enabled = true;
            foreach(var element in coneLayout.ElementsL)
            {
                if(element.Name != naviName)
                {
                    element.Enabled = false;
                    element.tether = false;
                    continue;
                }

                element.Enabled = true;
                element.tether = false;
                element.color = Controller.AttentionColor;
            }
        }

        if(showSwapHint)
        {
            EnableSwapHint();
        }
        else
        {
            DisableSwapHint();
        }
    }

    // Enables the self-tethered SwapHint overlay.
    private void EnableSwapHint()
    {
        if(Controller.TryGetElementByName(ElSwapHint, out var swapHint))
        {
            swapHint.Enabled = true;
        }
    }

    // Turns SwapHint off.
    private void DisableSwapHint()
    {
        if(Controller.TryGetElementByName(ElSwapHint, out var swapHint))
        {
            swapHint.Enabled = false;
        }
    }

    // Disables Navi / Cone layouts and SwapHint.
    private void DisableAllGuides()
    {
        foreach(var layoutName in new[] { LayoutNavi, LayoutCone })
        {
            if(!Controller.TryGetLayoutByName(layoutName, out var layout)) continue;
            layout.Enabled = false;
            foreach(var element in layout.ElementsL)
            {
                element.Enabled = false;
                element.tether = false;
            }
        }

        DisableSwapHint();
    }

    // True when the player has either cannon loading status.
    private static bool HasCannonStatus(IPlayerCharacter player)
        => player.StatusList.Any(x => x.StatusId is StatusCannonRight or StatusCannonLeft);

    #endregion
}
