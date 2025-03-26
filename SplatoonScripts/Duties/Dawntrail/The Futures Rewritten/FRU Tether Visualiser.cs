using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Schedulers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;
public unsafe class FRU_Tether_Visualiser : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(1, "NightmareXIV");
    List<(uint P1, uint P2, long Time)> Tethers = [];

    public override void OnSetup()
    {
        for(int i = 0; i < 6; i++)
        {
            Controller.RegisterElementFromCode($"Tether{i}", "{\"Name\":\"\",\"type\":2,\"radius\":0.0,\"color\":3357277952,\"fillIntensity\":0.345,\"thicc\":8.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }
    }

    bool IsPhase2 => WeatherManager.Instance()->WeatherId == 35;

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        var i = 0;
        foreach(var x in Tethers)
        {
            if(Environment.TickCount64 >  x.Time + (IsPhase2?C.TimeP2:C.TimeP4))
            {
                new TickScheduler(() => Tethers.Remove(x));
            }
            else
            {
                if(x.P1.TryGetObject(out var p1) && x.P2.TryGetObject(out var p2) && Controller.TryGetElementByName($"Tether{i++}", out var e))
                {
                    e.Enabled = true;
                    e.SetRefPosition(p1.Position);
                    e.SetOffPosition(p2.Position);
                }
            }
        }
    }

    public override void OnReset()
    {
        Tethers.Clear();
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(data3 == 110 && data5 == 15)
        {
            Tethers.Insert(0, (source, target, Environment.TickCount64));
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.SliderIntAsFloat("Phase 2 visualisation duration", ref C.TimeP2, -1, 20000);
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.SliderIntAsFloat("Phase 4 visualisation duration", ref C.TimeP4, -1, 20000);
    }

    public Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public int TimeP2 = 10000;
        public int TimeP4 = 10000;
    }
}
