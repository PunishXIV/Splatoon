using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Throttlers;
using ImGuiNET;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public class P6_AutoTargetSwitcher : SplatoonScript
{
    private enum Timings
    {
        Start,
        FirstWyrmsbreathEnd,
        FirstAkhAfahEnd,
        WrothFlames,
        SecondAkhAfahEnd,
        SecondWyrmsbreathEnd
    }

    private readonly List<float> _percentages = [];
    private readonly Random _random = new();
    private readonly List<IBattleChara> _targets = [];
    private int _akhAfahCount;

    private int _breathCount;
    private IBattleChara? _currentTarget;

    private Timings _currentTiming = Timings.Start;
    private float _lastMinPercentage;
    public override HashSet<uint>? ValidTerritories => [968];

    public override Metadata? Metadata => new(1, "Garume");
    private Config C => Controller.GetConfig<Config>();

    private IBattleChara? Nidhogg => Svc.Objects.FirstOrDefault(o => o.DataId == 0x3144) as IBattleChara;
    private IBattleChara? Hraesvelgr => Svc.Objects.FirstOrDefault(o => o.DataId == 0x3145) as IBattleChara;

    private bool IsActive => !C.TimingMode ||
                             (C.EnableTimings.Contains(_currentTiming) && !C.DisableTimings.Contains(_currentTiming));


    private static void DrawReorderbleList<T>(IList<T> list) where T : struct, Enum
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

        ImGui.Checkbox("Timing Mode", ref C.TimingMode);
        if (C.TimingMode)
        {
            ImGui.Indent();
            ImGui.PushID("EnableTimings");
            ImGui.Text("Enable Timings");
            ImGui.Indent();
            DrawReorderbleList(C.EnableTimings);
            ImGui.Unindent();
            ImGui.PopID();
            
            ImGui.Spacing();
            
            ImGui.PushID("DisableTimings");
            ImGui.Text("Disable Timings");
            ImGui.Indent();
            DrawReorderbleList(C.DisableTimings);
            ImGui.Unindent();
            ImGui.PopID();
            ImGui.Unindent();
        }


        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Checkbox("DebugMode", ref C.DebugMode);
            ImGui.Text($"Timings: {C.TimingMode}");
            ImGui.Text($"IsActive: {IsActive}");
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

        switch (set.Action.RowId)
        {
            case 27954 or 27955 or 27956 or 27957:
                _breathCount++;
                switch (_breathCount)
                {
                    case 2:
                        _currentTiming = Timings.FirstWyrmsbreathEnd;
                        Alert("First Wyrmsbreath ended");
                        break;
                    case 4:
                        _currentTiming = Timings.SecondWyrmsbreathEnd;
                        Alert("Second Akh Afah ended");
                        break;
                }

                break;
            case 27969 or 27971:
                _akhAfahCount++;
                switch (_akhAfahCount)
                {
                    case 2:
                        _currentTiming = Timings.FirstAkhAfahEnd;
                        Alert("First Akh Afah ended");
                        break;
                    case 4:
                        _currentTiming = Timings.SecondAkhAfahEnd;
                        Alert("Second Akh Afah ended");
                        break;
                }

                break;
            case 27973:
                _currentTiming = Timings.WrothFlames;
                DuoLog.Information("Wroth Flames Started");
                break;
        }
    }

    public override void OnUpdate()
    {
        if (!IsActive) return;
        if (EzThrottler.Throttle("AutoTargetSwitcher", C.Interval))
        {
            var nidhogg = Nidhogg;
            var hraesvelgr = Hraesvelgr;
            if (nidhogg == null || hraesvelgr == null) return;

            _targets.Clear();
            _targets.Add(nidhogg);
            _targets.Add(hraesvelgr);

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
        _lastMinPercentage = 0f;
        _targets.Clear();
        _percentages.Clear();
        _breathCount = 0;
        _akhAfahCount = 0;
    }

    private class Config : IEzConfig
    {
        public readonly List<Timings> DisableTimings = [];
        public readonly List<Timings> EnableTimings = [];
        public float AcceptablePercentage = 3f;
        public bool DebugMode = false;
        public int Interval = 300;
        public bool TimingMode = false;
    }
}