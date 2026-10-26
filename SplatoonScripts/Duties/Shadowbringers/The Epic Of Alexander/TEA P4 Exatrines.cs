using ECommons;
using ECommons.Hooks.ActionEffectTypes;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Shadowbringers.The_Epic_Of_Alexander;

public class TEA_P4_Exatrines : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [887];

    #endregion

    #region Constants

    private const uint CastExatrines = 18574;
    private const uint CastTrine = 18575;
    private const uint CastTrineAlt = 18576;
    private const uint CastStack = 18580;

    private const int WaveCount = 3;
    private const int StepCount = 4;
    private const long QuietMs = 1000;

    private const string LayoutGuide = "Guide";
    private const string LayoutWave1 = "Exatrines 1";
    private const string LayoutWave2 = "Exatrines 2";
    private const string LayoutWave3 = "Exatrines 3";
    private static readonly string[] WaveLayouts = [LayoutWave1, LayoutWave2, LayoutWave3];

    private static readonly Vector3[] Pockets =
    [
        new(100f, 0f, 100f),
        new(108f, 0f, 100f),
        new(100f, 0f, 108f),
        new(108f, 0f, 108f),
    ];

    #endregion

    #region Config

    // No IEzConfig in this script.

    #endregion

    #region State

    private int _step;
    private bool _stepOpen;
    private long _lastStepAt;
    private long _lastPlaceAt;
    private readonly List<Vector3> _pending = [];
    private readonly List<List<Vector3>> _waves = [];

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        Controller.TryRegisterLayoutFromCode(LayoutGuide, """
            ~Lv2~{"Name":"Guide","ZoneLockH":[887],"ElementsL":[{"Name":"Start","refX":100.0,"refY":100.0,"radius":0.5,"Donut":0.1,"color":3355508480,"fillIntensity":1.0,"IsCapturing":true},{"Name":"Goal","refX":100.0,"refY":108.0,"radius":0.5,"Donut":0.1,"color":3355508480,"fillIntensity":1.0,"IsCapturing":true},{"Name":"Guide Line","type":2,"radius":0.0,"color":4278255360,"fillIntensity":1.0,"thicc":5.0,"UsePlaceholderAsRefPosition":true,"UsePlaceholderAsOffPosition":true,"PlaceholdersRefPosition":["<element:Start>"],"PlaceholdersOffPosition":["<element:Goal>"],"EnablePointerLine":true,"PointerLineStyle":{"Width":0.5}}]}
            """, out _, overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutWave1, """
            ~Lv2~{"Name":"Exatrines 1","ZoneLockH":[887],"ElementsL":[{"Name":"1","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"2","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"3","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"4","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"5","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"6","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"7","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"8","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"9","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"10","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5}]}
            """, out _, overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutWave2, """
            ~Lv2~{"Name":"Exatrines 2","ZoneLockH":[887],"ElementsL":[{"Name":"1","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":3355443200,"fillIntensity":0.5},{"Name":"2","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":3355443200,"fillIntensity":0.5},{"Name":"3","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":3355443200,"fillIntensity":0.5},{"Name":"4","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":3355443200,"fillIntensity":0.5},{"Name":"5","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":3355443200,"fillIntensity":0.5},{"Name":"6","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":3355443200,"fillIntensity":0.5},{"Name":"7","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":3355443200,"fillIntensity":0.5},{"Name":"8","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":3355443200,"fillIntensity":0.5},{"Name":"9","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":3355443200,"fillIntensity":0.5},{"Name":"10","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":3355443200,"fillIntensity":0.5}]}
            """, out _, overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutWave3, """
            ~Lv2~{"Name":"Exatrines 3","ZoneLockH":[887],"ElementsL":[{"Name":"1","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"2","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"3","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"4","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"5","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"6","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"7","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"8","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"9","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5},{"Name":"10","Enabled":false,"refX":100.0,"refY":100.0,"radius":6.0,"color":1711276287,"fillIntensity":0.5}]}
            """, out _, overwrite: true);

        Hide();
    }

    public override void OnUpdate()
    {
        CommitWave();
        if(_stepOpen && Environment.TickCount64 - _lastStepAt >= QuietMs)
            _stepOpen = false;

        if(_waves.Count == 0 || StepEnded)
        {
            Hide();
            return;
        }

        for(var index = 0; index < WaveCount; index++)
            ApplyWave(index);
        ApplyGuide();
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if((uint)packet->ActionID == CastExatrines)
        {
            ClearState();
            return;
        }

        if((uint)packet->ActionID is not (CastTrine or CastTrineAlt) || _waves.Count >= WaveCount)
            return;

        _pending.Add(sourceId.TryGetObject(out var obj) ? obj.Position : packet->Position);
        _lastPlaceAt = Environment.TickCount64;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action?.RowId == CastStack)
        {
            ClearState();
            return;
        }

        if(set.Action?.RowId is not (CastTrine or CastTrineAlt) || _waves.Count < WaveCount || StepEnded)
            return;

        _lastStepAt = Environment.TickCount64;
        if(_stepOpen)
            return;

        _stepOpen = true;
        _step++;
    }

    public override void OnReset()
    {
        ClearState();
        Hide();
    }

    #endregion

    #region Private Method

    // Drops the stored casts and the step.
    private void ClearState()
    {
        _step = 0;
        _stepOpen = false;
        _lastStepAt = 0;
        _lastPlaceAt = 0;
        _pending.Clear();
        _waves.Clear();
    }

    // Closes a wave after a quiet gap without another wave cast.
    private void CommitWave()
    {
        if(_pending.Count == 0 || Environment.TickCount64 - _lastPlaceAt < QuietMs)
            return;

        _waves.Add([.. _pending]);
        _pending.Clear();
    }

    // True after the last visible step.
    private bool StepEnded => _step >= StepCount;

    // True when a cast landed on this pocket.
    private static bool Hits(Vector3 pocket, List<Vector3> casts)
    {
        foreach(var cast in casts)
        {
            if(Distance(cast, pocket) <= 1f)
                return true;
        }

        return false;
    }

    // Horizontal distance, ignoring height.
    private static float Distance(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    // Moves an element onto a ground position.
    private static void Place(Element? element, Vector3 position)
    {
        if(element == null)
            return;
        element.Enabled = true;
        element.refX = position.X;
        element.refY = position.Z;
    }

    // Returns one element from a registered layout.
    private Element? FindElement(string layoutName, string elementName)
        => Controller.GetRegisteredLayouts().SafeSelect(layoutName)?.GetElement(elementName);

    // Disables every guide and wave element.
    private void Hide()
    {
        DisableLayout(LayoutGuide);
        DisableLayout(LayoutWave1);
        DisableLayout(LayoutWave2);
        DisableLayout(LayoutWave3);
    }

    // Disables the elements of one layout.
    private void DisableLayout(string name)
    {
        var layout = Controller.GetRegisteredLayouts().SafeSelect(name);
        if(layout == null)
            return;

        foreach(var element in layout.ElementsL)
            element.Enabled = false;
    }

    // Steps 0 and 1 enable wave 1. Later steps enable wave 3. Wave 2 stays enabled.
    private void ApplyWave(int index)
    {
        var layout = WaveLayouts[index];
        var enabled = index switch
        {
            0 => _step <= 1,
            1 => true,
            _ => _step > 1,
        };
        if(index >= _waves.Count || !enabled)
        {
            DisableLayout(layout);
            return;
        }

        var casts = _waves[index];
        for(var slot = 0; slot < 10; slot++)
        {
            var element = FindElement(layout, $"{slot + 1}");
            if(slot < casts.Count)
                Place(element, casts[slot]);
            else if(element != null)
                element.Enabled = false;
        }
    }

    // Points the guide along the adjacent pocket from start to goal.
    private void ApplyGuide()
    {
        if(!TryFindPath(out var from, out var to))
        {
            DisableLayout(LayoutGuide);
            return;
        }

        Place(FindElement(LayoutGuide, "Start"), from);
        Place(FindElement(LayoutGuide, "Goal"), to);
        var line = FindElement(LayoutGuide, "Guide Line");
        if(line != null)
            line.Enabled = true;
    }

    // Start is a center pocket missed by wave 1 and 2. Goal is a wave 1 pocket 8 away.
    private bool TryFindPath(out Vector3 start, out Vector3 goal)
    {
        start = default;
        goal = default;
        if(_waves.Count < 2)
            return false;

        foreach(var from in Pockets)
        {
            if(Hits(from, _waves[0]) || Hits(from, _waves[1]))
                continue;
            foreach(var to in Pockets)
            {
                if(!Hits(to, _waves[0]) || MathF.Abs(Distance(from, to) - 8f) > 1f)
                    continue;

                start = from;
                goal = to;
                return true;
            }
        }

        return false;
    }

    #endregion
}
