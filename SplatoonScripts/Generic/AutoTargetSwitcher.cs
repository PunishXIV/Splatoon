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

namespace SplatoonScriptsOfficial.Generic;

public class AutoTargetSwitcher : SplatoonScript
{
    private readonly List<float> _percentages = [];
    private readonly Random _random = new();
    private IBattleChara? _currentTarget;
    private float _lastMinPercentage;

    public override HashSet<uint>? ValidTerritories => [];
    public override Metadata? Metadata => new(1, "Garume");

    private Config C => Controller.GetConfig<Config>();

    public override void OnSettingsDraw()
    {
        ImGui.Text("Auto Target Switcher");
        ImGui.Text("Switches target object automatically");

        for (var i = 0; i < C.TargetObjectIds.Length; i++)
        {
            ImGui.PushID($"TargetObjectIds{i}");
            ImGui.Text($"Target Object {i}");
            ImGui.SameLine();
            InputUlong("##TargetObjectIds", ref C.TargetObjectIds[i]);
            ImGui.SameLine();
            if (ImGui.Button("Remove"))
            {
                for (var j = i; j < C.TargetObjectIds.Length - 1; j++)
                    C.TargetObjectIds[j] = C.TargetObjectIds[j + 1];
                Array.Resize(ref C.TargetObjectIds, C.TargetObjectIds.Length - 1);
            }

            ImGui.PopID();
        }

        if (ImGui.Button("Add Target Object"))
            Array.Resize(ref C.TargetObjectIds, C.TargetObjectIds.Length + 1);

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
            var targets = Svc.Objects
                .Where(o => C.TargetObjectIds.Contains(o.GameObjectId))
                .OfType<IBattleChara>()
                .ToList();

            if (targets.Count == 0)
            {
                Alert("No targets found");
                return;
            }

            Alert($"Found {targets.Count} targets");

            _percentages.Clear();
            foreach (var percentage in targets.Select(target => (float)target.CurrentHp / target.MaxHp * 100f))
                _percentages.Add(percentage);

            var minPercentage = _percentages.Min();
            var maxPercentage = _percentages.Max();

            if (_currentTarget == null || maxPercentage - minPercentage > C.AcceptablePercentage ||
                Math.Abs(minPercentage - _lastMinPercentage) > 0.1f)
            {
                _lastMinPercentage = minPercentage;

                if (maxPercentage - minPercentage > C.AcceptablePercentage)
                {
                    var maxTarget = targets[_percentages.IndexOf(maxPercentage)];
                    Alert($"Switching to target with max percentage: {maxTarget.Name}");
                    Svc.Targets.SetTarget(maxTarget);
                    _currentTarget = maxTarget;
                }
                else
                {
                    var randomIndex = _random.Next(targets.Count);
                    var randomTarget = targets[randomIndex];
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

    private static void InputUlong(string id, ref ulong value)
    {
        var str = value.ToString();
        ImGui.InputText(id, ref str, 20);
        if (ulong.TryParse(str, out var result))
            value = result;
    }

    private class Config : IEzConfig
    {
        public float AcceptablePercentage = 3f;
        public bool Debug;
        public int Interval = 300;
        public ulong[] TargetObjectIds = Array.Empty<ulong>();
    }
}