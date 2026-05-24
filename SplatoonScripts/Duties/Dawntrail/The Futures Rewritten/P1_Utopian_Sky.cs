using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public sealed class P1_Utopian_Sky : SplatoonScript
{
    #region Metadata

    public override Metadata Metadata { get; } = new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryFru];

    #endregion

    #region Constant

    private const uint TerritoryFru = 1238;
    private const int RequiredPhantomCount = 3;
    private const int PlayersPerCircleGroup = 4;

    // casts and action effects
    private const uint CastPrismaticDeceptionRed = 40154;
    private const uint CastPrismaticDeceptionBlue = 40155;
    private const uint CastLightCircleRed = 40150;
    private const uint CastLightCircleBlue = 40151;
    private const uint EffectBlastingZone = 40158;
    private const uint EffectBurnOut = 40164;
    private const uint EffectFloatingRestraint = 40171;
    private const uint EffectSinFlame = 40167;

    // actors and debuffs
    private const uint DataIdPhantom = 17820;
    private const byte PhantomTransformId = 4;
    private const uint DataIdLightCircleRed = 17821;
    private const uint DebuffStackMark = 4165;

    // Geometry (not from RegisterElementFromCode JSON)
    private const float PrismaticPreSpreadRadius = 19f;
    private const float PrismaticSpreadRadiusNear = 12.5f;
    private const float PrismaticSpreadRadiusFar = 19f;
    private const float PrismaticBlueSpreadSideOffsetDegrees = 18f;
    private const float LightCircleSafeRadius = 10f;
    private const float AngleStepDegrees = 45f;
    private const float LightCircleSafeNorthEast = 60f;
    private const float LightCircleSafeSouthEast = 120f;
    private const float LightCircleSafeSouthWest = 240f;
    private const float LightCircleSafeNorthWest = 300f;
    private const float CardinalEast = 90f;

    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);
    private static readonly float[] AllCardinalDirections = [0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f];

    private const string ElementNavi = "navi";

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();

    public sealed class Config : IEzConfig
    {
        public Direction PrismaticSpreadDirection = Direction.North;
        public Direction PrismaticGroupDirection1 = Direction.NorthEast;
        public Direction PrismaticGroupDirection2 = Direction.East;
        public Direction PrismaticGroupDirection3 = Direction.SouthEast;
        public Direction PrismaticGroupDirection4 = Direction.South;
        public PrismaticBlueSpread PrismaticBlueSpread = PrismaticBlueSpread.Front;

        public CircleNorthGroupPriority CircleNorthGroup = new();
        public CircleSouthGroupPriority CircleSouthGroup = new();
        public CircleNorthGroupAdjusterPriority CircleNorthGroupAdjuster = new();
        public CircleSouthGroupAdjusterPriority CircleSouthGroupAdjuster = new();
    }

    #endregion

    #region State

    private MechanicState _state = MechanicState.Wait;
    private readonly List<float> _phantomAngles = [];
    private uint _prismatic;
    private uint _circle;
    private LightCircleGroup _lightCircleGroup;
    private List<IPlayerCharacter> _northPlayers = [];
    private List<IPlayerCharacter> _southPlayers = [];
    private IPlayerCharacter? _northMarkedPlayer;
    private IPlayerCharacter? _southMarkedPlayer;

    #endregion

    #region Private Class

    public enum Direction
    {
        North = 0,
        NorthEast = 45,
        East = 90,
        SouthEast = 135,
        South = 180,
        SouthWest = 225,
        West = 270,
        NorthWest = 315,
    }

    public enum PrismaticBlueSpread
    {
        Front,
        Back,
        Left,
        Right,
    }

    private enum LightCircleGroup
    {
        North,
        South,
    }

    private enum MechanicState
    {
        Wait,
        PrismaticDeception,
        WaitCircle,
        ResolveCircle,
        FinalStack,
    }

    private readonly record struct LightCircleSafeAngles(float North, float South);

    public sealed class CircleNorthGroupPriority : PriorityData
    {
        public override int GetNumPlayers() => PlayersPerCircleGroup;
    }

    public sealed class CircleSouthGroupPriority : PriorityData
    {
        public override int GetNumPlayers() => PlayersPerCircleGroup;
    }

    public sealed class CircleNorthGroupAdjusterPriority : PriorityData
    {
        public override int GetNumPlayers() => 1;
    }

    public sealed class CircleSouthGroupAdjusterPriority : PriorityData
    {
        public override int GetNumPlayers() => 1;
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode(
            ElementNavi,
            """{"Name":"navi","Enabled":false,"refX":96.75298,"refY":94.58399,"radius":0.5,"fillIntensity":0.25,"thicc":5.0,"tether":true}""",
            overwrite: true);
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
        {
            ResetAll();
        }
    }

    public override void OnReset() => ResetAll();

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId is CastPrismaticDeceptionRed or CastPrismaticDeceptionBlue)
        {
            _state = MechanicState.PrismaticDeception;
            _prismatic = castId;
            _phantomAngles.Clear();
            return;
        }

        if(castId is CastLightCircleRed or CastLightCircleBlue)
        {
            _state = MechanicState.WaitCircle;
            _circle = castId;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null)
        {
            return;
        }

        var effectId = set.Action.Value.RowId;

        if(_state == MechanicState.PrismaticDeception && effectId == EffectBlastingZone)
        {
            ResetAll();
            return;
        }

        if(_state == MechanicState.WaitCircle && effectId == EffectBurnOut)
        {
            _state = MechanicState.ResolveCircle;
            return;
        }

        if(_state == MechanicState.ResolveCircle && effectId == EffectFloatingRestraint)
        {
            _state = MechanicState.FinalStack;
            return;
        }

        if(effectId == EffectSinFlame)
        {
            ResetAll();
        }
    }

    public override void OnUpdate()
    {
        DisableNaviElement();

        if(_state is MechanicState.Wait or MechanicState.WaitCircle)
        {
            return;
        }

        Vector3? naviPosition = _state switch
        {
            MechanicState.PrismaticDeception => UpdatePrismaticNavi(),
            MechanicState.ResolveCircle => UpdateResolveCircleNavi(),
            MechanicState.FinalStack => UpdateFinalStackNavi(),
            _ => null,
        };

        if(naviPosition is { } position)
        {
            ApplyNaviElementAt(position);
        }
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text(EColor.YellowBright, $"Prismatic Deception Settings:");
        ImGui.Text("\nSpread direction");
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("Spread direction", ref C.PrismaticSpreadDirection);
        
        ImGui.Text("\nAvoid Blasting Zone directions");
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("Avoid Blasting Zone direction 1", ref C.PrismaticGroupDirection1);
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("Avoid Blasting Zone direction 2", ref C.PrismaticGroupDirection2);
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("Avoid Blasting Zone direction 3", ref C.PrismaticGroupDirection3);  
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("Avoid Blasting Zone direction 4", ref C.PrismaticGroupDirection4);
        
        ImGui.Text("\nThunder spread Position");
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("Thunder spread Position", ref C.PrismaticBlueSpread);

        ImGui.Text("\n");
        ImGui.Separator();
        ImGuiEx.Text(EColor.YellowBright, $"Light Circle Settings:");

        ImGui.Text("\nNorth group priority");
        C.CircleNorthGroup.Draw();
        ImGui.Text("\nSouth group priority");
        C.CircleSouthGroup.Draw();

        ImGui.Text("\nNorth adjuster");
        C.CircleNorthGroupAdjuster.Draw();
        ImGui.Text("\nSouth adjuster");
        C.CircleSouthGroupAdjuster.Draw();

        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            DrawDebugInfo();
        }
    }

    #endregion

    #region Private Method

    // Clears mechanic state and disables navi on wipe or reset.
    private void ResetAll()
    {
        _state = MechanicState.Wait;
        _phantomAngles.Clear();
        _prismatic = 0;
        _circle = 0;
        _northPlayers = [];
        _southPlayers = [];
        _northMarkedPlayer = null;
        _southMarkedPlayer = null;
        DisableNaviElement();
    }

    // Disables the navi element at the start of each tick.
    private void DisableNaviElement()
    {
        if(Controller.TryGetElementByName(ElementNavi, out var el))
        {
            el.Enabled = false;
        }
    }

    // Places tether navi at arena XZ; color follows Splatoon Attention Color (rainbow when configured).
    private void ApplyNaviElementAt(Vector3 world)
    {
        if(!Controller.TryGetElementByName(ElementNavi, out var el))
        {
            return;
        }

        el.refX = world.X;
        el.refY = world.Z;
        el.refZ = world.Y;
        el.color = Controller.AttentionColor;
        el.tether = true;
        el.Enabled = true;
    }

    // PrismaticDeception — collect phantoms, pre-spread or resolved group spread.
    private Vector3? UpdatePrismaticNavi()
    {
        CollectPhantomAngles();

        if(_phantomAngles.Count < RequiredPhantomCount)
        {
            return CalculatePointCircle(ArenaCenter, PrismaticPreSpreadRadius, (float)C.PrismaticSpreadDirection);
        }

        if(!TryResolvePrismaticSpreadDegrees(out var spreadDegrees))
        {
            return null;
        }

        var (angleOffset, radius) = GetPrismaticSpreadOffsetAndRadius();
        return CalculatePointCircle(ArenaCenter, radius, NormalizeAngle(spreadDegrees + angleOffset));
    }

    // ResolveCircle — party groups, safe spot from red circles.
    private Vector3? UpdateResolveCircleNavi()
    {
        if(!TryUpdatePartyState())
        {
            return null;
        }

        var safeAngles = GetLightCircleSafeAngles();
        var angle = _lightCircleGroup == LightCircleGroup.North ? safeAngles.North : safeAngles.South;
        return CalculatePointCircle(ArenaCenter, LightCircleSafeRadius, angle);
    }

    // FinalStack — tether to marked player in own group.
    private Vector3? UpdateFinalStackNavi()
    {
        if(BasePlayer == null)
        {
            return null;
        }

        _lightCircleGroup = GetSelfLightCircleGroup();
        var target = _lightCircleGroup == LightCircleGroup.North ? _northMarkedPlayer : _southMarkedPlayer;
        return target?.Position;
    }

    // accumulates phantom slot angles (DataId 17820, transform 4).
    private void CollectPhantomAngles()
    {
        if(_phantomAngles.Count >= RequiredPhantomCount)
        {
            return;
        }

        foreach(var npc in Svc.Objects.OfType<IBattleNpc>())
        {
            if(npc.DataId != DataIdPhantom || npc.GetTransformationID() != PhantomTransformId)
            {
                continue;
            }

            var angle = GetArenaRelativeAngle(npc.Position);
            if(_phantomAngles.Contains(angle))
            {
                continue;
            }

            _phantomAngles.Add(angle);
        }
    }

    // exclude phantom directions (+180) and match one PrismaticGroupDirection.
    private bool TryResolvePrismaticSpreadDegrees(out float spreadDegrees)
    {
        spreadDegrees = 0f;

        if(_phantomAngles.Count != RequiredPhantomCount)
        {
            return false;
        }

        var excluded = new HashSet<float>();
        foreach(var angle in _phantomAngles)
        {
            excluded.Add(NormalizeAngle(angle));
            excluded.Add(NormalizeAngle(angle + 180f));
        }

        var remaining = AllCardinalDirections.Where(d => !excluded.Contains(d)).ToList();
        if(remaining.Count != 2)
        {
            return false;
        }

        var matches = remaining.Where(GetConfiguredGroupDirections().Contains).ToList();
        if(matches.Count != 1)
        {
            return false;
        }

        spreadDegrees = matches[0];
        return true;
    }

    // Blue spread offset/radius; Red uses near radius only.
    private (float AngleOffset, float Radius) GetPrismaticSpreadOffsetAndRadius()
    {
        if(_prismatic == CastPrismaticDeceptionRed)
        {
            return (0f, PrismaticSpreadRadiusNear);
        }

        return C.PrismaticBlueSpread switch
        {
            PrismaticBlueSpread.Front => (0f, PrismaticSpreadRadiusNear),
            PrismaticBlueSpread.Back => (0f, PrismaticSpreadRadiusFar),
            PrismaticBlueSpread.Left => (PrismaticBlueSpreadSideOffsetDegrees, PrismaticSpreadRadiusFar),
            PrismaticBlueSpread.Right => (-PrismaticBlueSpreadSideOffsetDegrees, PrismaticSpreadRadiusFar),
            _ => (0f, PrismaticSpreadRadiusNear),
        };
    }

    // resolves and caches north/south groups, marked players, and self group.
    private bool TryUpdatePartyState()
    {
        if(!TryResolvePartyGroups(out var northGroup, out var southGroup))
        {
            return false;
        }

        _northPlayers = northGroup;
        _southPlayers = southGroup;
        _northMarkedPlayer = _northPlayers.FirstOrDefault(HasStackMark);
        _southMarkedPlayer = _southPlayers.FirstOrDefault(HasStackMark);

        if(BasePlayer == null || _northMarkedPlayer == null || _southMarkedPlayer == null)
        {
            return false;
        }

        _lightCircleGroup = GetSelfLightCircleGroup();
        return true;
    }

    // Returns whether BasePlayer is in the north or south resolved group.
    private LightCircleGroup GetSelfLightCircleGroup()
    {
        var self = BasePlayer!;
        return _northPlayers.Any(x => x.EntityId == self.EntityId)
            ? LightCircleGroup.North
            : LightCircleGroup.South;
    }

    // priority lists, stack-mark adjusters, north/south groups of four.
    private bool TryResolvePartyGroups(
        out List<IPlayerCharacter> northGroup,
        out List<IPlayerCharacter> southGroup)
    {
        northGroup = [];
        southGroup = [];

        var northPlayers = GetPlayersFromPriority(C.CircleNorthGroup);
        var southPlayers = GetPlayersFromPriority(C.CircleSouthGroup);
        if(northPlayers == null || southPlayers == null)
        {
            return false;
        }

        northGroup = OrderByPriority(northPlayers, C.CircleNorthGroup).ToList();
        southGroup = OrderByPriority(southPlayers, C.CircleSouthGroup).ToList();

        ApplyNorthDoubleMarkAdjustment(ref northGroup, ref southGroup);
        ApplySouthDoubleMarkAdjustment(ref northGroup, ref southGroup);

        return northGroup.Count == PlayersPerCircleGroup && southGroup.Count == PlayersPerCircleGroup;
    }

    // two north marks — south adjuster to north, last north mark to south.
    private void ApplyNorthDoubleMarkAdjustment(
        ref List<IPlayerCharacter> northGroup,
        ref List<IPlayerCharacter> southGroup)
    {
        var northMarked = northGroup.Where(HasStackMark).ToList();
        if(northMarked.Count != 2)
        {
            return;
        }

        var toNorth = GetFirstPlayerFromPriority(C.CircleSouthGroupAdjuster);
        var toSouth = northMarked.Last();
        if(toNorth == null)
        {
            return;
        }

        SwapPlayersBetweenGroups(ref northGroup, ref southGroup, toNorth, toSouth);
    }

    // two south marks — first south mark to north, north adjuster to south.
    private void ApplySouthDoubleMarkAdjustment(
        ref List<IPlayerCharacter> northGroup,
        ref List<IPlayerCharacter> southGroup)
    {
        var southMarked = southGroup.Where(HasStackMark).ToList();
        if(southMarked.Count != 2)
        {
            return;
        }

        var toNorth = southMarked.First();
        var toSouth = GetFirstPlayerFromPriority(C.CircleNorthGroupAdjuster);
        if(toSouth == null)
        {
            return;
        }

        SwapPlayersBetweenGroups(ref northGroup, ref southGroup, toNorth, toSouth);
    }

    // Removes toSouth from north and toNorth from south, then swaps them across groups.
    private static void SwapPlayersBetweenGroups(
        ref List<IPlayerCharacter> northGroup,
        ref List<IPlayerCharacter> southGroup,
        IPlayerCharacter toNorth,
        IPlayerCharacter toSouth)
    {
        northGroup = northGroup.Where(x => x.EntityId != toSouth.EntityId).ToList();
        northGroup.Add(toNorth);
        southGroup = southGroup.Where(x => x.EntityId != toNorth.EntityId).ToList();
        southGroup.Add(toSouth);
    }

    // safe north/south angles from red circle layout and circle cast color.
    private LightCircleSafeAngles GetLightCircleSafeAngles()
    {
        var hasEast = GetLightCircleRedAngles().Count(a => a == CardinalEast) == 1;

        if(_circle == CastLightCircleBlue)
        {
            return hasEast
                ? new LightCircleSafeAngles(LightCircleSafeNorthEast, LightCircleSafeSouthEast)
                : new LightCircleSafeAngles(LightCircleSafeNorthWest, LightCircleSafeSouthWest);
        }

        return hasEast
            ? new LightCircleSafeAngles(LightCircleSafeNorthWest, LightCircleSafeSouthWest)
            : new LightCircleSafeAngles(LightCircleSafeNorthEast, LightCircleSafeSouthEast);
    }

    // Collects snapped arena-relative angles of visible red light circle objects.
    private List<float> GetLightCircleRedAngles()
        => Svc.Objects.OfType<IBattleNpc>()
            .Where(x => x.DataId == DataIdLightCircleRed && x.IsCharacterVisible())
            .Select(x => GetArenaRelativeAngle(x.Position))
            .ToList();

    // Returns configured PrismaticGroupDirection values as degrees.
    private float[] GetConfiguredGroupDirections()
        =>
        [
            (float)C.PrismaticGroupDirection1,
            (float)C.PrismaticGroupDirection2,
            (float)C.PrismaticGroupDirection3,
            (float)C.PrismaticGroupDirection4,
        ];

    // Maps a configured priority list to in-party player characters.
    private static List<IPlayerCharacter>? GetPlayersFromPriority(PriorityData priority)
    {
        var members = priority.GetPlayers(_ => true);
        if(members == null)
        {
            return null;
        }

        var players = new List<IPlayerCharacter>();
        foreach(var member in members)
        {
            if(member.IGameObject is IPlayerCharacter pc)
            {
                players.Add(pc);
            }
        }

        return players.Count == priority.GetNumPlayers() ? players : null;
    }

    // Returns the first player from a priority list (adjuster lists).
    private static IPlayerCharacter? GetFirstPlayerFromPriority(PriorityData priority)
        => GetPlayersFromPriority(priority)?.FirstOrDefault();

    // Orders players by configured priority index, then entity id.
    private static IEnumerable<IPlayerCharacter> OrderByPriority(
        IEnumerable<IPlayerCharacter> players,
        PriorityData priority)
        => players.OrderBy(p => GetPriorityIndex(p, priority)).ThenBy(p => p.EntityId);

    // Zero-based index in priority config, or max when unknown.
    private static int GetPriorityIndex(IPlayerCharacter player, PriorityData priority)
    {
        var priorityList = priority.GetPlayers(_ => true);
        if(priorityList == null)
        {
            return int.MaxValue;
        }

        var name = player.Name.ToString();
        for(var index = 0; index < priorityList.Count; index++)
        {
            if(priorityList[index].Name == name)
            {
                return index;
            }
        }

        return int.MaxValue;
    }

    // stack mark debuff 4165.
    private static bool HasStackMark(IPlayerCharacter player)
        => player.StatusList.Any(s => s.StatusId == DebuffStackMark);

    // Snaps arena-relative angle from center to the 45-degree grid.
    private static float GetArenaRelativeAngle(Vector3 position)
        => RoundAngleByStep(MathHelper.GetRelativeAngle(ArenaCenter, position), AngleStepDegrees);

    // Renders debug fields in script settings.
    private void DrawDebugInfo()
    {
        ImGui.Text($"State: {_state}");
        ImGui.Text($"Prismatic cast: {_prismatic}");
        ImGui.Text($"Circle cast: {_circle}");
        ImGui.Text($"Phantom angles: {_phantomAngles.Print()}");
        ImGui.Text($"Light circle group: {_lightCircleGroup}");
        ImGui.Text($"North players: {FormatPlayerList(_northPlayers)}");
        ImGui.Text($"South players: {FormatPlayerList(_southPlayers)}");
        ImGui.Text($"North marked: {_northMarkedPlayer?.Name}");
        ImGui.Text($"South marked: {_southMarkedPlayer?.Name}");
        ImGui.Text($"Red circle angles: {GetLightCircleRedAngles().Print()}");
    }

    // Formats a player list for debug output (name and stack mark).
    private static string FormatPlayerList(IEnumerable<IPlayerCharacter> players)
    {
        var list = players.ToList();
        if(list.Count == 0)
        {
            return "(empty)";
        }

        return string.Join(", ", list.Select(p => $"{p.Name}{(HasStackMark(p) ? "*" : "")}"));
    }

    // World XZ on circle from center, radius, and compass angle in degrees.
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

    // Normalizes degrees to [0, 360).
    private static float NormalizeAngle(float degree)
        => (degree % 360f + 360f) % 360f;

    // Snaps angle to step and normalizes to [0, 360).
    private static float RoundAngleByStep(float degree, float step)
        => NormalizeAngle((float)Math.Round(degree / step) * step);

    #endregion
}
