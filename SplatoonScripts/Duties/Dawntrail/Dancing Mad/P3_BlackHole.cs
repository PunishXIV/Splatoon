using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

internal class P3_BlackHole : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryDmad];

    #endregion

    #region Constant

    private const uint TerritoryDmad = 1363;
    private const int SceneP3 = 8;

    private const uint DataIdBlackHole = 0x4C38;
    private const uint CastBlackHole = 47867;
    private const uint CastNothingness = 47868;
    private const uint NothingnessWaveFrameWidth = 3;

    private const ushort StatusFirstTarget = 0xBBC;
    private const ushort StatusSecondTarget = 0xBBD;
    private const ushort StatusThirdTarget = 0xBBE;

    private const float OutSideBlackHole = 17.5f;
    private const float OutSideBlackHoleTolerance = 1f;

    private const float DelaySliderMinSeconds = 0f;
    private const float DelaySliderMaxSeconds = 3f;

    private const string ClearCommand = "/mk off <me>";
    private const string ElNavi = "BlackHoleNavi";

    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);
    private static readonly Vector3 TrueNorth = new(100f, 0f, 80f);

    private static readonly string[] MarkerResolveKindLabels = ["Attack", "Bind", "Stop"];

    private static readonly Dictionary<string, FloorMark> DefaultBaitRules = CreateDefaultBaitRules();

    private static readonly (int Count, int Wave, int Index)[] BaitRuleKeys =
    [
        (1, 1, 1),
        (1, 2, 1),
        (1, 2, 2),
        (2, 1, 1),
        (2, 1, 2),
        (2, 1, 3),
        (2, 2, 1),
        (2, 2, 2),
        (2, 2, 3),
        (2, 3, 1),
        (2, 3, 2),
        (2, 3, 3),
        (3, 1, 1),
        (3, 1, 2),
        (3, 1, 3),
        (3, 2, 1),
        (3, 2, 2),
        (3, 2, 3),
        (3, 3, 1),
        (3, 3, 2),
        (3, 3, 3),
        (4, 1, 1),
        (4, 1, 2),
        (4, 2, 1),
    ];

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();

    #endregion

    #region State

    private int _nothingnessCount;
    private uint _lastNothingnessWaveFrame;

    private long _markAtMs;
    private bool _targetMarkActive;
    private bool _markCommandSent;
    private ushort _activeTargetStatus;
    private string _pendingMarkCommand = "";

    #endregion

    #region Private Class

    private enum MarkerResolveKind
    {
        Attack = 0,
        Bind = 1,
        Stop = 2,
    }

    private enum FloorMark : uint
    {
        Attack1 = 0,
        Attack2 = 1,
        Attack3 = 2,
        Bind1 = 5,
        Bind2 = 6,
        Bind3 = 7,
        Stop1 = 8,
        Stop2 = 9,
    }

    private sealed class Config : IEzConfig
    {
        public bool EnableSelfMark = true;
        public bool EnableBait = true;
        public MarkerResolveKind FirstTargetMark = MarkerResolveKind.Attack;
        public MarkerResolveKind SecondTargetMark = MarkerResolveKind.Bind;
        public MarkerResolveKind ThirdTargetMark = MarkerResolveKind.Stop;
        public float MarkDelayMinSeconds = 0.5f;
        public float MarkDelayMaxSeconds = 1.5f;
        public Dictionary<string, FloorMark> BaitRules = new(DefaultBaitRules);
    }

    private sealed class OutsideBlackHoleEntry(Vector3 position, float angle, bool hasTether)
    {
        public Vector3 Position { get; } = position;
        public float Angle { get; } = angle;
        public bool HasTether { get; } = hasTether;
    }

    private readonly record struct ResolvedState(int BlackHole, int Wave);

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        Controller.RegisterElement(ElNavi, new Element(0)
        {
            Enabled = false,
            radius = 0.5f,
            Filled = true,
            thicc = 4f,
            tether = true,
        }, overwrite: true);
    }

    public override void OnCombatStart() => ResetState();

    public override void OnReset() => ResetState();

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe))
            ResetState();
    }

    public override void OnGainBuffEffect(uint sourceId, Status status)
    {
        if (!C.EnableSelfMark || BasePlayer == null || sourceId != BasePlayer.EntityId)
            return;

        if (!TryGetTargetMarkKind(status.StatusId, out var markKind))
            return;

        var command = GetMarkCommand(markKind);
        if (command == null)
            return;

        _targetMarkActive = true;
        _markCommandSent = false;
        _activeTargetStatus = status.StatusId;
        _pendingMarkCommand = command;
        _markAtMs = Environment.TickCount64 + ToRandomDelayMs(C.MarkDelayMinSeconds, C.MarkDelayMaxSeconds);
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status status)
    {
        if (BasePlayer == null || sourceId != BasePlayer.EntityId)
            return;

        if (status.StatusId != _activeTargetStatus)
            return;

        _targetMarkActive = false;
        _markAtMs = 0;
        _pendingMarkCommand = "";
        _activeTargetStatus = 0;

        if (_markCommandSent)
            RunCommand(ClearCommand);

        _markCommandSent = false;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!IsPhaseActive())
            return;

        var actionId = set.Action?.RowId ?? 0;

        switch (actionId)
        {
            case CastBlackHole:
                _nothingnessCount = 0;
                _lastNothingnessWaveFrame = 0;
                break;
            case CastNothingness:
                var frame = (uint)Svc.PluginInterface.UiBuilder.FrameCount;
                if (_lastNothingnessWaveFrame != 0
                    && frame - _lastNothingnessWaveFrame < NothingnessWaveFrameWidth)
                    break;

                _lastNothingnessWaveFrame = frame;
                _nothingnessCount++;
                break;
        }
    }

    public override void OnUpdate()
    {
        if (!IsPhaseActive())
        {
            DisableNavi();
            return;
        }

        TryRunPendingMarkCommand();

        if (!C.EnableBait)
        {
            DisableNavi();
            return;
        }

        DisableNavi();

        if (!IsBlackHolePresent() && _nothingnessCount > 0)
            return;

        if (!TryResolveState(_nothingnessCount, out var resolved))
            return;

        if (BasePlayer == null)
            return;

        var sorted = SortOutsideBlackHoles();
        for (var index = 1; index <= sorted.Count; index++)
        {
            if (!TryGetBaitMark(resolved.BlackHole, resolved.Wave, index, out var requiredMark))
                continue;

            if (!PlayerHasFloorMark(BasePlayer, requiredMark))
                continue;

            EnableNavi(sorted[index - 1].Position);
            return;
        }
    }

    public override void OnSettingsDraw()
    {
        if (ImGui.BeginTabBar("##P3BlackHoleSettings"))
        {
            if (ImGui.BeginTabItem("Main###tabMain"))
            {
                DrawMainTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Debug###tabDebug"))
            {
                DrawDebugTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    #endregion

    #region Settings UI

    // Draws main settings grouped by feature section.
    private void DrawMainTab()
    {
        ImGui.TextDisabled("Self Marking");
        ImGui.Separator();

        ImGui.TextWrapped(
            "Auto-marks yourself when gaining First/Second/Third Target debuffs.");

        ImGui.Checkbox("Enable Self Marking", ref C.EnableSelfMark);

        ImGui.BeginDisabled(!C.EnableSelfMark);
        DrawMarkerResolveKindCombo("FirstTarget", ref C.FirstTargetMark);
        DrawMarkerResolveKindCombo("SecondTarget", ref C.SecondTargetMark);
        DrawMarkerResolveKindCombo("ThirdTarget", ref C.ThirdTargetMark);

        ImGui.TextUnformatted("delay (s)");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(140f);
        ImGui.SliderFloat("Min##markDelayMin", ref C.MarkDelayMinSeconds, DelaySliderMinSeconds,
            DelaySliderMaxSeconds, "%.1f");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(140f);
        ImGui.SliderFloat("Max##markDelayMax", ref C.MarkDelayMaxSeconds, DelaySliderMinSeconds,
            DelaySliderMaxSeconds, "%.1f");
        ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.TextDisabled("Bait BlackHole");
        ImGui.Separator();

        ImGui.TextWrapped(
            "Shows black hole bait navi on outside black holes from North Clockwise.");

        ImGui.Checkbox("Enable Bait BlackHole", ref C.EnableBait);
        ImGui.BeginDisabled(!C.EnableBait);
        DrawBaitRulesTable();
        ImGui.EndDisabled();
    }

    // Draws editable bait rule floor mark assignments.
    private void DrawBaitRulesTable()
    {
        if (!ImGui.BeginTable("BaitRules", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            return;

        ImGui.TableSetupColumn("BlackHole", ImGuiTableColumnFlags.WidthFixed, 72f);
        ImGui.TableSetupColumn("Wave", ImGuiTableColumnFlags.WidthFixed, 48f);
        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 48f);
        ImGui.TableSetupColumn("Floor mark", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        foreach (var (count, wave, index) in BaitRuleKeys)
        {
            var key = BaitRuleKey(count, wave, index);
            if (!C.BaitRules.TryGetValue(key, out var floorMark)
                && !DefaultBaitRules.TryGetValue(key, out floorMark))
                continue;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{count}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{wave}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{index}");
            ImGui.TableNextColumn();
            ImGui.PushID(key);
            DrawFloorMarkCombo("##floorMark", ref floorMark);
            C.BaitRules[key] = floorMark;
            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    // Draws debug panels grouped by topic.
    private void DrawDebugTab()
    {
        ImGui.TextDisabled("State");
        ImGui.Separator();
        ImGui.TextUnformatted($"NothingnessCount: {_nothingnessCount}");

        if (TryResolveState(_nothingnessCount, out var resolved))
        {
            ImGui.TextUnformatted($"ResolvedBlackHole: {resolved.BlackHole}");
            ImGui.TextUnformatted($"ResolvedWave: {resolved.Wave}");
        }
        else
        {
            ImGui.TextUnformatted("ResolvedBlackHole: —");
            ImGui.TextUnformatted("ResolvedWave: —");
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Floor marks");
        ImGui.Separator();
        DrawPlayerMarkerTable();

        ImGui.Spacing();
        ImGui.TextDisabled("SortOutSideBlackHole");
        ImGui.Separator();
        DrawSortOutsideBlackHoleTable();
    }

    // Debug table for party member floor marks.
    private void DrawPlayerMarkerTable()
    {
        var players = Controller.GetPartyMembers()
            .Where(x => x != null)
            .OrderBy(x => x.Name.ToString())
            .ToList();

        if (players.Count == 0)
        {
            ImGui.TextUnformatted("(no party members)");
            return;
        }

        if (!ImGui.BeginTable("PlayerFloorMarks", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            return;

        ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Marks", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        foreach (var player in players)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(player.Name.ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(FormatPlayerFloorMarks(player));
        }

        ImGui.EndTable();
    }

    // Formats all floor marks currently assigned to a player.
    private static string FormatPlayerFloorMarks(IPlayerCharacter player)
    {
        var marks = new List<string>(8);

        foreach (FloorMark floorMark in Enum.GetValues<FloorMark>())
        {
            if (Marking.HaveMark(player, (uint)floorMark))
                marks.Add(floorMark.ToString());
        }

        return marks.Count == 0 ? "(none)" : string.Join(", ", marks);
    }

    // Debug table for outside black holes sorted by compass angle.
    private void DrawSortOutsideBlackHoleTable()
    {
        var sorted = SortOutsideBlackHoles();

        if (sorted.Count == 0)
        {
            ImGui.TextUnformatted("(none)");
            return;
        }

        if (!ImGui.BeginTable("SortOutSideBlackHole", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 48f);
        ImGui.TableSetupColumn("Bait mark", ImGuiTableColumnFlags.WidthFixed, 72f);
        ImGui.TableSetupColumn("Position", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Angle", ImGuiTableColumnFlags.WidthFixed, 64f);
        ImGui.TableSetupColumn("Tether", ImGuiTableColumnFlags.WidthFixed, 56f);
        ImGui.TableHeadersRow();

        for (var index = 0; index < sorted.Count; index++)
        {
            var entry = sorted[index];
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{index + 1}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(FormatBaitMarkForIndex(index + 1));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(FormatPositionXz(entry.Position));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(FormatAngle(entry.Angle));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(entry.HasTether ? "True" : "False");
        }

        ImGui.EndTable();
    }

    // Formats the bait floor mark assigned to a sorted outside black hole index.
    private string FormatBaitMarkForIndex(int index)
    {
        if (!TryResolveState(_nothingnessCount, out var resolved)
            || !TryGetBaitMark(resolved.BlackHole, resolved.Wave, index, out var floorMark))
            return "—";

        return floorMark.ToString();
    }

    // Formats a floor position as (x, z).
    private static string FormatPositionXz(Vector3 position)
        => $"({position.X:F1}, {position.Z:F1})";

    // Formats a compass angle in degrees.
    private static string FormatAngle(float angle)
        => $"{angle:F1}°";

    #endregion

    #region Private Method

    // Returns true when the script should run in P3 scene 8.
    private bool IsPhaseActive()
        => Controller.Scene == SceneP3;

    // Returns true when at least one black hole object exists on the field.
    private static bool IsBlackHolePresent()
        => Svc.Objects.Any(x => x.DataId == DataIdBlackHole);

    // Resolves black hole set and wave from nothingness count (exact match only).
    private static bool TryResolveState(int nothingnessCount, out ResolvedState state)
    {
        state = nothingnessCount switch
        {
            0 => new ResolvedState(1, 1),
            1 => new ResolvedState(1, 2),
            2 => new ResolvedState(2, 1),
            3 => new ResolvedState(2, 2),
            4 => new ResolvedState(2, 3),
            5 => new ResolvedState(3, 1),
            6 => new ResolvedState(3, 2),
            7 => new ResolvedState(3, 3),
            8 => new ResolvedState(4, 1),
            9 => new ResolvedState(4, 2),
            _ => default,
        };

        return nothingnessCount is 0 or 1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9;
    }

    // Returns outside-ring black holes sorted by compass angle from true north.
    private static List<OutsideBlackHoleEntry> SortOutsideBlackHoles()
    {
        return Svc.Objects
            .Where(x => x.DataId == DataIdBlackHole && IsOutsideBlackHole(x.Position))
            .Select(x => new OutsideBlackHoleEntry(
                NormalizeY(x.Position),
                GetAngleFromTrueNorth(x.Position),
                IsTetherBlackHole(x)))
            .OrderBy(x => x.Angle)
            .ToList();
    }

    // Returns true when the black hole object has an active tether.
    private static unsafe bool IsTetherBlackHole(IGameObject gameObject)
    {
        if (gameObject is ICharacter character)
        {
            var c = character.Struct();
            for (var i = 0; i < c->Vfx.Tethers.Length; i++)
            {
                if (c->Vfx.Tethers[i].Id != 0)
                    return true;
            }
        }

        foreach (var obj in Svc.Objects)
        {
            if (obj.Address == gameObject.Address || obj is not ICharacter other)
                continue;

            var oc = other.Struct();
            for (var i = 0; i < oc->Vfx.Tethers.Length; i++)
            {
                var t = oc->Vfx.Tethers[i];
                if (t.Id != 0 && t.TargetId == gameObject.GameObjectId)
                    return true;
            }
        }

        return false;
    }

    // Returns true when the position sits on the outside black hole ring.
    private static bool IsOutsideBlackHole(Vector3 position)
    {
        var distance = MathF.Sqrt(DistanceSquaredXz(ArenaCenter, NormalizeY(position)));
        return distance < OutSideBlackHole + OutSideBlackHoleTolerance
               && distance > OutSideBlackHole - OutSideBlackHoleTolerance;
    }

    // Compass angle from arena center with true north reference point.
    private static float GetAngleFromTrueNorth(Vector3 position)
    {
        var posAngle = MathHelper.GetRelativeAngle(ArenaCenter, position);
        return NormalizeAngle(posAngle - GetNorthReferenceAngle());
    }

    // Angle from arena center toward the true north reference point.
    private static float GetNorthReferenceAngle()
        => MathHelper.GetRelativeAngle(ArenaCenter, TrueNorth);

    // Normalizes degree to 0-360.
    private static float NormalizeAngle(float degree)
        => (degree % 360f + 360f) % 360f;

    // Squared XZ distance between two floor positions.
    private static float DistanceSquaredXz(Vector3 a, Vector3 b)
    {
        var deltaX = a.X - b.X;
        var deltaZ = a.Z - b.Z;
        return deltaX * deltaX + deltaZ * deltaZ;
    }

    // Flatten Y for arena-floor geometry.
    private static Vector3 NormalizeY(Vector3 position) => new(position.X, 0f, position.Z);

    // Returns true when the player has the required floor mark.
    private static bool PlayerHasFloorMark(IPlayerCharacter player, FloorMark floorMark)
        => Marking.HaveMark(player, (uint)floorMark);

    // Maps target status id to configured marker resolve kind.
    private bool TryGetTargetMarkKind(ushort statusId, out MarkerResolveKind markKind)
    {
        switch (statusId)
        {
            case StatusFirstTarget:
                markKind = C.FirstTargetMark;
                return true;
            case StatusSecondTarget:
                markKind = C.SecondTargetMark;
                return true;
            case StatusThirdTarget:
                markKind = C.ThirdTargetMark;
                return true;
            default:
                markKind = default;
                return false;
        }
    }

    // Converts marker resolve kind to a self-mark chat command.
    private static string? GetMarkCommand(MarkerResolveKind kind)
        => kind switch
        {
            MarkerResolveKind.Attack => "/mk attack <me>",
            MarkerResolveKind.Bind => "/mk bind <me>",
            MarkerResolveKind.Stop => "/mk stop <me>",
            _ => null,
        };

    // Runs the pending self-mark command when the delay expires.
    private void TryRunPendingMarkCommand()
    {
        if (!_targetMarkActive || _markAtMs <= 0 || Environment.TickCount64 < _markAtMs)
            return;

        _markAtMs = 0;
        if (_pendingMarkCommand.Length == 0)
            return;

        _markCommandSent = true;
        RunCommand(_pendingMarkCommand);
    }

    // Picks a random delay in milliseconds between configured min and max seconds.
    private static long ToRandomDelayMs(float minSeconds, float maxSeconds)
    {
        var min = Math.Clamp(minSeconds, DelaySliderMinSeconds, DelaySliderMaxSeconds);
        var max = Math.Clamp(maxSeconds, DelaySliderMinSeconds, DelaySliderMaxSeconds);
        if (min > max)
            (min, max) = (max, min);

        var delaySeconds = min >= max ? min : min + Random.Shared.NextSingle() * (max - min);
        return (long)(delaySeconds * 1000);
    }

    // Sends a chat command or logs it during duty recorder playback.
    private static void RunCommand(string command)
    {
        if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
            DuoLog.Information(command);
        else
            Chat.Instance.ExecuteCommand(command);
    }

    // Enables nav element at destination with attention color.
    private void EnableNavi(Vector3 destination)
    {
        if (!Controller.TryGetElementByName(ElNavi, out var element))
            return;

        element.SetRefPosition(destination);
        element.color = Controller.AttentionColor;
        element.tether = true;
        element.Enabled = true;
    }

    // Disables the nav element.
    private void DisableNavi()
    {
        if (!Controller.TryGetElementByName(ElNavi, out var element))
            return;

        element.Enabled = false;
        element.tether = false;
    }

    // Builds the default bait rule floor mark map.
    private static Dictionary<string, FloorMark> CreateDefaultBaitRules()
        => new()
        {
            [BaitRuleKey(1, 1, 1)] = FloorMark.Attack1,
            [BaitRuleKey(1, 2, 1)] = FloorMark.Attack1,
            [BaitRuleKey(1, 2, 2)] = FloorMark.Attack2,
            [BaitRuleKey(2, 1, 1)] = FloorMark.Attack1,
            [BaitRuleKey(2, 1, 2)] = FloorMark.Attack2,
            [BaitRuleKey(2, 1, 3)] = FloorMark.Attack3,
            [BaitRuleKey(2, 2, 1)] = FloorMark.Bind1,
            [BaitRuleKey(2, 2, 2)] = FloorMark.Attack2,
            [BaitRuleKey(2, 2, 3)] = FloorMark.Attack3,
            [BaitRuleKey(2, 3, 1)] = FloorMark.Bind1,
            [BaitRuleKey(2, 3, 2)] = FloorMark.Bind2,
            [BaitRuleKey(2, 3, 3)] = FloorMark.Attack3,
            [BaitRuleKey(3, 1, 1)] = FloorMark.Bind1,
            [BaitRuleKey(3, 1, 2)] = FloorMark.Bind2,
            [BaitRuleKey(3, 1, 3)] = FloorMark.Bind3,
            [BaitRuleKey(3, 2, 1)] = FloorMark.Stop1,
            [BaitRuleKey(3, 2, 2)] = FloorMark.Bind2,
            [BaitRuleKey(3, 2, 3)] = FloorMark.Bind3,
            [BaitRuleKey(3, 3, 1)] = FloorMark.Stop1,
            [BaitRuleKey(3, 3, 2)] = FloorMark.Stop2,
            [BaitRuleKey(3, 3, 3)] = FloorMark.Bind3,
            [BaitRuleKey(4, 1, 1)] = FloorMark.Stop1,
            [BaitRuleKey(4, 1, 2)] = FloorMark.Stop2,
            [BaitRuleKey(4, 2, 1)] = FloorMark.Stop2,
        };

    // Returns the config key for a bait rule tuple.
    private static string BaitRuleKey(int count, int wave, int index)
        => $"{count}_{wave}_{index}";

    // Resolves the configured floor mark for a bait rule, falling back to defaults.
    private bool TryGetBaitMark(int blackHole, int wave, int index, out FloorMark floorMark)
    {
        var key = BaitRuleKey(blackHole, wave, index);
        if (C.BaitRules.TryGetValue(key, out floorMark))
            return true;

        return DefaultBaitRules.TryGetValue(key, out floorMark);
    }

    // Draws a combo box for floor mark bait rule settings.
    private static void DrawFloorMarkCombo(string label, ref FloorMark mark)
    {
        var values = Enum.GetValues<FloorMark>();
        var idx = Array.IndexOf(values, mark);
        if (idx < 0)
            idx = 0;

        ImGui.SetNextItemWidth(160f);
        if (ImGui.Combo(label, ref idx, Enum.GetNames<FloorMark>(), values.Length))
            mark = values[idx];
    }

    // Draws a combo box for marker resolve kind settings.
    private static void DrawMarkerResolveKindCombo(string label, ref MarkerResolveKind kind)
    {
        var idx = (int)kind;
        if (idx < 0 || idx >= MarkerResolveKindLabels.Length)
            idx = 0;

        ImGui.SetNextItemWidth(200f);
        if (ImGui.Combo(label, ref idx, MarkerResolveKindLabels, MarkerResolveKindLabels.Length))
            kind = (MarkerResolveKind)idx;
    }

    // Clears runtime state on reset, wipe, or combat start.
    private void ResetState()
    {
        _nothingnessCount = 0;
        _lastNothingnessWaveFrame = 0;
        _markAtMs = 0;
        _targetMarkActive = false;
        _markCommandSent = false;
        _activeTargetStatus = 0;
        _pendingMarkCommand = "";
        DisableNavi();
    }

    #endregion
}
