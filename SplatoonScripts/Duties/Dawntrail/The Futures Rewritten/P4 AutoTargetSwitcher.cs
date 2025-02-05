using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Throttlers;
using ImGuiNET;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P4_AutoTargetSwitcher : SplatoonScript
{
    private readonly List<float> _percentages = [];
    private readonly Random _random = new();
    private readonly List<IBattleChara> _targets = [];

    private int _akhMornCount;
    private IBattleChara? _currentTarget;

    private Timings _currentTiming = Timings.Start;
    private float _lastMinPercentage;
    private int _mornAfahCount;
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(8, "Garume");

    private Config C => Controller.GetConfig<Config>();

    private IBattleChara? DarkGirl => Svc.Objects.OfType<IBattleChara>().FirstOrDefault(o => (!C.LimitDistance || Player.DistanceTo(o) < 3f + o.HitboxRadius) && o.IsTargetable && o.DataId == 0x45AB);
    private IBattleChara? LightGirl => Svc.Objects.OfType<IBattleChara>().FirstOrDefault(o => (!C.LimitDistance || Player.DistanceTo(o) < 3f + o.HitboxRadius) && o.IsTargetable && o.DataId == 0x45A9);

    private bool IsActive => !C.TimingMode ||
                             (C.EnableTimings.Contains(_currentTiming) && !C.DisableTimings.Contains(_currentTiming));


    private static void DrawReorderableList<T>(IList<T> list) where T : struct, Enum
    {
        var toRemoveIndex = -1;
        for (var i = 0; i < list.Count; i++)
        {
            ImGui.Text($"Item: {list[i]}");
            ImGui.SameLine();
            if (ImGui.SmallButton($"Remove##{i}")) toRemoveIndex = i;
        }

        if (toRemoveIndex != -1) list.RemoveAt(toRemoveIndex);

        if (ImGui.BeginCombo("##partysel", "Add Item"))
        {
            Enum.GetValues<T>().Each(x =>
            {
                if (ImGui.Selectable(x.ToString())) list.Add(x);
            });
            ImGui.EndCombo();
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Auto Target Switcher");
        ImGui.Text("Switches target object automatically");

        ImGui.SliderFloat("Acceptable Percentage", ref C.AcceptablePercentage, 0f, 100f);
        ImGui.SliderInt("Interval", ref C.Interval, 100, 1000);

        ImGui.Checkbox("Should Disable When Low Hp", ref C.ShouldDisableWhenLowHp);
        if (C.ShouldDisableWhenLowHp) ImGui.SliderFloat("Low Hp Percentage", ref C.LowHpPercentage, 0f, 100f);

        ImGui.Checkbox("Do not switch if target is not in melee range", ref C.LimitDistance);

        ImGui.Checkbox("Timing Mode", ref C.TimingMode);
        if (C.TimingMode)
        {
            ImGui.Indent();
            ImGui.PushID("EnableTimings");
            ImGui.Text("Enable Timings");
            ImGui.Indent();
            DrawReorderableList(C.EnableTimings);
            ImGui.Unindent();
            ImGui.PopID();

            ImGui.Spacing();

            ImGui.PushID("DisableTimings");
            ImGui.Text("Disable Timings");
            ImGui.Indent();
            DrawReorderableList(C.DisableTimings);
            ImGui.Unindent();
            ImGui.PopID();
            ImGui.Unindent();
        }


        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Checkbox("DebugMode", ref C.DebugMode);
            ImGui.Text($"Timings: {C.TimingMode}");
            ImGui.Text($"IsActive: {IsActive}");
            ImGui.Text($"Current Timing: {_currentTiming}");
            ImGui.Text($"Current Target: {_currentTarget?.Name}");

            ImGui.Separator();
            var darkGirl = DarkGirl;
            var lightGirl = LightGirl;
            if (darkGirl == null || lightGirl == null) return;
            var darkGirlHpPercent = (float)darkGirl.CurrentHp / darkGirl.MaxHp * 100f;
            var lightGirlHpPercent = (float)lightGirl.CurrentHp / lightGirl.MaxHp * 100f;
            ImGui.Text($"DarkGirl Hp Percent: {darkGirlHpPercent}");
            ImGui.Text($"LightGirl Hp Percent: {lightGirlHpPercent}");
            ImGui.Text($"Difference: {Math.Abs(darkGirlHpPercent - lightGirlHpPercent)}");
        }
    }

    private void Alert(string message)
    {
        if (C.DebugMode)
            DuoLog.Information(message);
    }


    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action is null) return;

        switch (set.Action.Value.RowId)
        {
            case 40285:
                _currentTiming = Timings.SomberDanceEnd;
                break;
            case 40249:
                _mornAfahCount++;
                _currentTiming = _mornAfahCount == 1 ? Timings.FirstMornAfahEnd : Timings.SecondMornAfahEnd;
                break;
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 40247)
        {
            _akhMornCount++;
            _currentTiming = _akhMornCount == 1 ? Timings.FirstAkhMorn : Timings.SecondAkhMorn;
        }
    }

    public override void OnUpdate()
    {
        if (!IsActive) return;
        if (Svc.Condition[ConditionFlag.DutyRecorderPlayback]) return;
        if (EzThrottler.Throttle("AutoTargetSwitcher", C.Interval))
        {
            var darkGirl = DarkGirl;
            var lightGirl = LightGirl;

            if (darkGirl == null && lightGirl == null)
            {
                Alert("No targets found");
                return;
            }

            if (C.ShouldDisableWhenLowHp && darkGirl != null && lightGirl != null)
                if ((float)darkGirl.CurrentHp / darkGirl.MaxHp * 100f < C.LowHpPercentage ||
                    (float)lightGirl.CurrentHp / lightGirl.MaxHp * 100f < C.LowHpPercentage)
                {
                    Alert("Disabling due to low hp");
                    return;
                }

            if (darkGirl == null && lightGirl != null)
            {
                Svc.Targets.SetTarget(lightGirl);
                _currentTarget = lightGirl;
                return;
            }

            if (darkGirl != null && lightGirl == null)
            {
                Svc.Targets.SetTarget(darkGirl);
                _currentTarget = darkGirl;
                return;
            }

            _targets.Clear();
            if (darkGirl != null) _targets.Add(darkGirl);
            if (lightGirl != null) _targets.Add(lightGirl);

            _percentages.Clear();
            foreach (var percentage in _targets.Select(target => (float)target.CurrentHp / target.MaxHp * 100f))
                _percentages.Add(percentage);

            var minPercentage = _percentages.Min();
            var maxPercentage = _percentages.Max();

            if (_currentTarget == null || maxPercentage - minPercentage > C.AcceptablePercentage ||
                Math.Abs(minPercentage - _lastMinPercentage) > 0.1f)
            {
                _lastMinPercentage = minPercentage;

                if (maxPercentage - minPercentage > C.AcceptablePercentage)
                {
                    var maxTarget = _targets[_percentages.IndexOf(maxPercentage)];
                    Alert($"Switching to target with max percentage: {maxTarget.Name}");
                    Svc.Targets.SetTarget(maxTarget);
                    _currentTarget = maxTarget;
                }
                else
                {
                    var randomIndex = _random.Next(_targets.Count);
                    var randomTarget = _targets[randomIndex];
                    Alert($"Switching to random target: {randomTarget.Name}");
                    Svc.Targets.SetTarget(randomTarget);
                    _currentTarget = randomTarget;
                }
            }
            else
            {
                Alert($"Maintaining current target: {_currentTarget.Name}");
            }
        }
    }


    public override void OnReset()
    {
        _currentTarget = null;
        _currentTiming = Timings.Start;
        _lastMinPercentage = 0f;
        _akhMornCount = 0;
        _mornAfahCount = 0;
        _targets.Clear();
        _percentages.Clear();
    }

    private enum Timings
    {
        Start,
        SomberDanceEnd,
        FirstAkhMorn,
        FirstMornAfahEnd,
        SecondAkhMorn,
        SecondMornAfahEnd
    }

    private class Config : IEzConfig
    {
        public List<Timings> DisableTimings =
        [
        ];

        public List<Timings> EnableTimings =
        [
        ];

        public float AcceptablePercentage = 3f;
        public bool DebugMode;

        public int Interval = 300;
        public float LowHpPercentage = 1f;
        public bool ShouldDisableWhenLowHp;
        public bool TimingMode;
        public bool LimitDistance = false;
    }
}