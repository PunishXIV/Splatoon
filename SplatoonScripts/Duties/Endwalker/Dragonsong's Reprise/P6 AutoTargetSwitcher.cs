using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.Logging;
using ECommons.Throttlers;
using ImGuiNET;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public class P6_AutoTargetSwitcher : SplatoonScript
{
    private readonly List<float> _percentages = [];
    private readonly Random _random = new();
    private readonly List<IBattleChara> _targets = [];
    private IBattleChara? _currentTarget;
    private float _lastMinPercentage;
    public override HashSet<uint>? ValidTerritories => [968];

    public override Metadata? Metadata => new(1, "Garume");
    private Config C => Controller.GetConfig<Config>();

    private IBattleChara? Nidhogg => Svc.Objects.FirstOrDefault(o => o.DataId == 0x3144) as IBattleChara;
    private IBattleChara? Hraesvelgr => Svc.Objects.FirstOrDefault(o => o.DataId == 0x3145) as IBattleChara;

    public override void OnSettingsDraw()
    {
        ImGui.Text("Auto Target Switcher");
        ImGui.Text("Switches target object automatically");

        ImGui.SliderFloat("Acceptable Percentage", ref C.AcceptablePercentage, 0f, 100f);
        ImGui.SliderInt("Interval", ref C.Interval, 100, 1000);
        ImGui.Checkbox("Debug", ref C.Debug);
    }

    private void Alert(string message)
    {
        if (C.Debug)
            DuoLog.Information(message);
    }

    public override void OnUpdate()
    {
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
    }

    private class Config : IEzConfig
    {
        public float AcceptablePercentage = 3f;
        public bool Debug;
        public int Interval = 300;
    }
}