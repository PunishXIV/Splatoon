using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.SplatoonScripting;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public class P1_DoubleTroubleTrap_AutoMarker : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryDmad];

    #endregion

    #region Constant

    private const uint TerritoryDmad = 1363;
    private const uint StatusDoubleTroubleTrap = 5078;

    private const float DelaySliderMinSeconds = 0f;
    private const float DelaySliderMaxSeconds = 3f;

    private const string DefaultMarkCommand = "/mk bind <me>";
    private const string ClearCommand = "/mk off <me>";

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();

    #endregion

    #region State

    private long _markAtMs;
    private bool _trapActive;
    private bool _markCommandSent;

    #endregion

    #region Private Class

    private sealed class Config : IEzConfig
    {
        public string Command = DefaultMarkCommand;
        public float MarkDelayMinSeconds = 0.5f;
        public float MarkDelayMaxSeconds = 1.5f;
    }

    #endregion

    #region LifeCycle

    public override void OnCombatStart() => ResetState();

    public override void OnReset() => ResetState();

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe))
            ResetState();
    }

    public override void OnUpdate()
    {
        if (!_trapActive || _markAtMs <= 0 || Environment.TickCount64 < _markAtMs) return;

        _markAtMs = 0;
        RunMarkCommand();
    }

    public override void OnGainBuffEffect(uint sourceId, Status status)
    {
        if (!IsTrapStatusEvent(sourceId, status)) return;
        if (_trapActive) return;
        if (C.Command.Trim().Length == 0) return;

        _trapActive = true;
        _markCommandSent = false;
        _markAtMs = Environment.TickCount64 + ToRandomDelayMs(C.MarkDelayMinSeconds, C.MarkDelayMaxSeconds);
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status status)
    {
        if (!IsTrapStatusEvent(sourceId, status)) return;
        if (!_trapActive) return;

        _trapActive = false;
        _markAtMs = 0;

        if (_markCommandSent)
            RunCommand(ClearCommand);
    }

    public override void OnSettingsDraw()
    {
        ImGui.TextWrapped(
            "When you gain status DoubleTroubleTrap (5078), runs the mark command after a random delay. When it expires, runs /mk off <me>.");

        ImGui.Spacing();

        ImGui.Text("Command");
        ImGui.SetNextItemWidth(300);
        ImGui.InputText("##command", ref C.Command, 256);

        ImGui.Text("Mark delay min (s)");
        ImGui.SetNextItemWidth(300);
        ImGui.SliderFloat("##markDelayMin", ref C.MarkDelayMinSeconds, DelaySliderMinSeconds, DelaySliderMaxSeconds,
            "%.1f");

        ImGui.Text("Mark delay max (s)");
        ImGui.SetNextItemWidth(300);
        ImGui.SliderFloat("##markDelayMax", ref C.MarkDelayMaxSeconds, DelaySliderMinSeconds, DelaySliderMaxSeconds,
            "%.1f");
    }

    #endregion

    #region Private Method

    // Returns true when the buff event is status 5078 on the local player.
    private bool IsTrapStatusEvent(uint sourceId, Status status)
        => status.StatusId == StatusDoubleTroubleTrap && sourceId == BasePlayer.EntityId;

    // Runs the configured mark command and records that this trap wave was marked.
    private void RunMarkCommand()
    {
        _markCommandSent = true;
        RunCommand(C.Command.Trim());
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

    // Clears runtime state on reset, wipe, or combat start.
    private void ResetState()
    {
        _trapActive = false;
        _markCommandSent = false;
        _markAtMs = 0;
    }

    #endregion
}
