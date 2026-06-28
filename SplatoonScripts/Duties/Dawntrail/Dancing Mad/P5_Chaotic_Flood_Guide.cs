using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Hooks.ActionEffectTypes;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public class P5_Chaotic_Flood_Guide : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1363];
    public override Metadata? Metadata => new(2, "Poneglyph");

    private class Config : IEzConfig
    {
        public bool UseMarkers = false;
    }

    private Config C => Controller.GetConfig<Config>();

    private const uint KefkaNameId = 7131;
    private const uint TelegraphCast = 49539;
    private const uint WaveResolveAction = 47951;
    private const long SpreadDurationMs = 3000;

    private const float HitHalfWidth = 5f;
    private const float SegmentHalfLength = 26f;
    private const long GroupWindowMs = 700;
    private const long WaveWindowMs = 400;
    private const long ResetGapMs = 8000;

    private static readonly Vector3[] Cardinals =
    [
        new(100f, 0f, 98f),
        new(102f, 0f, 100f),
        new(100f, 0f, 102f),
        new(98f, 0f, 100f),
    ];
    private static readonly string[] CardinalNames = ["N", "E", "S", "W"];
    private static readonly string[] MarkerNames   = ["A", "B", "C", "D"];

    private string GetName(int idx) => C.UseMarkers ? MarkerNames[idx] : CardinalNames[idx];

    private sealed class LineSet
    {
        public long FirstSeen;
        public readonly HashSet<int> Hit = [];
    }

    private bool _active;
    private long _lastTelegraphTick;
    private readonly List<LineSet> _sets = [];
    private bool _computed;
    private readonly int[] _sequence = [0, 1, 2, 3];
    private int _wavesResolved;
    private long _lastWaveTick;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("SafeSpot",
            "{\"Name\":\"\",\"refX\":100.0,\"refY\":100.0,\"refZ\":0.0,\"radius\":1.0,\"color\":3355508503,\"Filled\":true,\"fillIntensity\":0.3,\"thicc\":6.0,\"tether\":true,\"overlayBGColor\":3221225472,\"overlayTextColor\":4294967295,\"overlayVOffset\":1.5,\"overlayFScale\":2.0}");
        Controller.RegisterElementFromCode("Spread",
            "{\"Name\":\"\",\"refX\":110.0,\"refY\":106.0,\"refZ\":0.0,\"radius\":1.0,\"color\":3355508503,\"Filled\":true,\"fillIntensity\":0.3,\"thicc\":6.0,\"tether\":true,\"overlayBGColor\":3221225472,\"overlayTextColor\":4294967295,\"overlayVOffset\":1.5,\"overlayFScale\":2.0,\"overlayText\":\"Spread\"}");
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Text Only: Use Markers (A/B/C/D) instead of Cardinals (N/E/S/W)", ref C.UseMarkers);
    }

    public override void OnReset()
    {
        ResetSequence();
        _active = false;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId != TelegraphCast) return;

        var obj = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.EntityId == source);
        if(obj == null || obj.NameId != KefkaNameId) return;

        var now = Environment.TickCount64;

        if(!_active || now - _lastTelegraphTick > ResetGapMs)
        {
            ResetSequence();
            _active = true;
        }
        _lastTelegraphTick = now;

        var hit = TestLine(obj.Position, obj.Rotation);

        if(_sets.Count == 0 || now - _sets[^1].FirstSeen > GroupWindowMs)
            _sets.Add(new LineSet { FirstSeen = now });

        foreach(var h in hit) _sets[^1].Hit.Add(h);

        if(!_computed) TryCompute();
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!_active || !_computed) return;
        if((set.Action?.RowId ?? 0) != WaveResolveAction) return;

        var now = Environment.TickCount64;
        if(now - _lastWaveTick <= WaveWindowMs) return;
        _lastWaveTick = now;
        _wavesResolved = Math.Min(_wavesResolved + 1, 4);
    }

    public override void OnUpdate()
    {
        var safe = Controller.GetElementByName("SafeSpot")!;
        var spread = Controller.GetElementByName("Spread")!;
        safe.Enabled = false;

        var showSpread = false;

        if(_active && _computed)
        {
            if(_wavesResolved >= 4)
            {
                if(Environment.TickCount64 - _lastWaveTick <= SpreadDurationMs)
                    showSpread = true;
            }
            else
            {
                var step = Math.Min(_wavesResolved, 2);
                var idx = _sequence[step];
                safe.Enabled = true;
                safe.SetRefPosition(Cardinals[idx]);

                if(step == 0)
                {
                    var remaining = Enumerable.Range(0, 3)
                        .Select(s => GetName(_sequence[s]));
                    safe.overlayText = string.Join(" > ", remaining);
                }
                else
                {
                    safe.overlayText = "";
                }
            }
        }

        spread.Enabled = showSpread;
    }

    private void TryCompute()
    {
        if(_sets.Count < 2) return;
        var pair1 = _sets[0].Hit;
        var pair2 = _sets[1].Hit;
        if(pair1.Count < 2 || pair2.Count < 2) return;

        var safe1 = Enumerable.Range(0, 4).Where(i => !pair1.Contains(i)).ToHashSet();
        var safe2 = Enumerable.Range(0, 4).Where(i => !pair2.Contains(i)).ToHashSet();
        var common = safe1.Where(safe2.Contains).ToList();
        if(common.Count != 1) return;

        var start = common[0];
        var direction = safe2.Contains((start + 1) % 4) ? 1 : -1;

        for(var k = 0; k < 4; k++)
            _sequence[k] = ((start + direction * k) % 4 + 4) % 4;

        _computed = true;
    }

    private static HashSet<int> TestLine(Vector3 casterPos, float rot)
    {
        var p = new Vector2(casterPos.X, casterPos.Z);
        var facing = new Vector2(MathF.Cos(rot), -MathF.Sin(rot));
        var along = new Vector2(-facing.Y, facing.X);
        if(along.LengthSquared() > 0.0001f) along = Vector2.Normalize(along);

        var hit = new HashSet<int>();
        for(var i = 0; i < 4; i++)
        {
            var d = new Vector2(Cardinals[i].X, Cardinals[i].Z) - p;
            var t = Vector2.Dot(d, along);
            if(Math.Abs(t) > SegmentHalfLength) continue;
            if((d - t * along).Length() <= HitHalfWidth) hit.Add(i);
        }
        return hit;
    }

    private void ResetSequence()
    {
        _sets.Clear();
        _computed = false;
        _wavesResolved = 0;
        _lastWaveTick = 0;
    }
}
