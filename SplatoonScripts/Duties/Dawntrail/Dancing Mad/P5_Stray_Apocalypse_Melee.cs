using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using Splatoon.Data;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public class P5_Stray_Apocalypse_Melee : SplatoonScript<P5_Stray_Apocalypse_Melee.Config>
{
    public override Metadata Metadata { get; } = new(3, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];

    public enum ExaType { Left, Mid, Right }
    public List<(ExaType, long)> ObservedExasNw = [];
    public List<(ExaType, long)> ObservedExasNe = [];

    Dictionary<ExaType, Vector2> NwPositions = new()
    {
        [ExaType.Left] = new(85, 80),
        [ExaType.Mid] = new(75, 90),
        [ExaType.Right] = new(95, 70),
    };

    Dictionary<ExaType, Vector2> NePositions = new()
    {
        [ExaType.Left] = new(120, 85),
        [ExaType.Mid] = new(110, 75),
        [ExaType.Right] = new(115, 80),
    };

    public override void OnSetup()
    {
        Controller.RegisterElementsFromMultilineCode("""
                        {"Name":"nw mid + ne left","refX":100.0,"refY":95.0,"radius":0.5,"color":3356884736,"fillIntensity":0.5,"thicc":4.0}
            {"Name":"nw mid + ne mid","refX":105.0,"refY":100.0,"radius":0.5,"color":3356884736,"fillIntensity":0.5,"thicc":4.0}
            {"Name":"nw mid + ne right","refX":105.0,"refY":100.0,"radius":0.5,"color":3356884736,"fillIntensity":0.5,"thicc":4.0}
            {"Name":"nw left + ne left","refX":95.0,"refY":100.0,"radius":0.5,"color":3356884736,"fillIntensity":0.5,"thicc":4.0}
            {"Name":"nw left + ne mid","refX":95.0,"refY":100.0,"radius":0.5,"color":3356884736,"fillIntensity":0.5,"thicc":4.0}
            {"Name":"nw left + ne right","refX":100.0,"refY":105.0,"radius":0.5,"color":3356884736,"fillIntensity":0.5,"thicc":4.0}
            {"Name":"nw right + ne left","refX":100.0,"refY":95.0,"radius":0.5,"color":3356884736,"fillIntensity":0.5,"thicc":4.0}
            {"Name":"nw right + ne mid","refX":105.0,"refY":100.0,"radius":0.5,"color":3356884736,"fillIntensity":0.5,"thicc":4.0}
            {"Name":"nw right + ne right","refX":105.0,"refY":100.0,"radius":0.5,"color":3356884736,"fillIntensity":0.5,"thicc":4.0}
            {"Name":"nw mid","type":2,"Enabled":false,"refX":80.0,"refY":80.0,"offX":120.0,"offY":120.0,"radius":4.5,"Filled":false,"fillIntensity":0.345,"thicc":6.0}
            {"Name":"nw left","type":2,"Enabled":false,"refX":80.0,"refY":90.0,"offX":110.0,"offY":120.0,"radius":4.5,"Filled":false,"fillIntensity":0.345,"thicc":6.0}
            {"Name":"nw right","type":2,"Enabled":false,"refX":90.0,"refY":80.0,"offX":120.0,"offY":110.0,"radius":4.5,"Filled":false,"fillIntensity":0.345,"thicc":6.0}
            {"Name":"ne right","type":2,"Enabled":false,"refX":120.0,"refY":90.0,"offX":90.0,"offY":120.0,"radius":4.5,"Filled":false,"fillIntensity":0.345,"thicc":6.0}
            {"Name":"ne left","type":2,"Enabled":false,"refX":110.0,"refY":80.0,"offX":80.0,"offY":110.0,"radius":4.5,"Filled":false,"fillIntensity":0.345,"thicc":6.0}
            {"Name":"ne mid","type":2,"Enabled":false,"refX":120.0,"refY":80.0,"refZ":3.8146973E-06,"offX":80.0,"offY":120.0,"offZ":3.8146973E-06,"radius":4.5,"Filled":false,"fillIntensity":0.345,"thicc":6.0}
            
            
            """);
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        var primaryShown = false;
        var secondaryShown = false;
        (ExaType, long)? ne = null;
        (ExaType, long)? nw = null;
        if(C.OneMode)
        {
            for(int i = 0; i < ObservedExasNe.Count; i++)
            {
                var cand = ObservedExasNe[i];
                if(Environment.TickCount64 - cand.Item2 < C.TimeStart) continue;
                if(Environment.TickCount64 - cand.Item2 > C.TimeEnd) continue;
                ne = cand;
                break;
            }
            for(int i = 0; i < ObservedExasNw.Count; i++)
            {
                var cand = ObservedExasNw[i];
                if(Environment.TickCount64 - cand.Item2 < C.TimeStart) continue;
                if(Environment.TickCount64 - cand.Item2 > C.TimeEnd) continue;
                nw = cand;
                break;
            }
        }
        else
        {
            for(int i = 0; i < Math.Min(ObservedExasNe.Count, ObservedExasNw.Count); i++)
            {
                var neCandidate = ObservedExasNe[i];
                var nwCandidate = ObservedExasNw[i];
                if(Environment.TickCount64 - neCandidate.Item2 < C.TimeStart || Environment.TickCount64 - nwCandidate.Item2 < C.TimeStart) continue;
                if(Environment.TickCount64 - neCandidate.Item2 > C.TimeEnd || Environment.TickCount64 - nwCandidate.Item2 > C.TimeEnd) continue;
                ne = neCandidate;
                nw = nwCandidate;
                break;
            }
        }
        if(C.DisplayLines)
        {
            if(nw != null)
            {
                if(Controller.TryGetElementByName($"nw {nw.Value.Item1.ToString().ToLower()}", out var e))
                {
                    e.Enabled = true;
                    e.color = (nw.Value.Item2 > (ne?.Item2 ?? 0) ? EColor.YellowBright : EColor.RedBright).ToUint();
                }
            }
            if(ne != null)
            {
                if(Controller.TryGetElementByName($"ne {ne.Value.Item1.ToString().ToLower()}", out var e))
                {
                    e.Enabled = true;
                    e.color = (ne.Value.Item2 > (nw?.Item2 ?? 0) ? EColor.YellowBright : EColor.RedBright).ToUint();
                }
            }
        }
        if(ne != null && nw != null)
        {
            if(C.DisplaySpots)
            {
                var name = $"nw {nw.Value.Item1.ToString().ToLower()} + ne {ne.Value.Item1.ToString().ToLower()}";
                if(!primaryShown)
                {
                    primaryShown = true;
                    if(Controller.TryGetElementByName(name, out var e))
                    {
                        e.Enabled = true;
                        e.color = Controller.AttentionColor;
                        e.tether = true;
                    }
                    else
                    {
                        PluginLog.Debug($"No element {name}");
                    }
                }
                else if(!secondaryShown)
                {
                    secondaryShown = true;
                    if(Controller.TryGetElementByName(name, out var e))
                    {
                        e.Enabled = true;
                        e.color = EColor.GreenBright.ToUint();
                        e.tether = false;
                    }
                    else
                    {
                        PluginLog.Debug($"No element {name}");
                    }
                }
            }
        }
    }

    public override void OnReset()
    {
        ObservedExasNw.Clear();
        ObservedExasNe.Clear();
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(200);
        ImGuiEx.SliderIntAsFloat("Start displaying at, seconds", ref C.TimeStart, 0, 3000);
        ImGui.SetNextItemWidth(200);
        ImGuiEx.SliderIntAsFloat("Stop displaying at, seconds", ref C.TimeEnd, 5000, 10000);
        ImGui.Checkbox("Swap at every exaflare (otherwise at every 2)", ref C.OneMode);
        ImGui.Checkbox("Display safe spots", ref C.DisplaySpots);
        ImGui.Checkbox("Display safe corridors", ref C.DisplayLines);
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"{ObservedExasNe.Print()}");
            ImGuiEx.Text($"{ObservedExasNw.Print()}");
        }
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if(packet->ActionDescriptor == new ActionDescriptor(ActionType.Action, 47932) && sourceId.TryGetBattleNpc(out var b))
        {
            foreach(var x in NePositions)
            {
                if(Vector2.Distance(b.Position2, x.Value) < 1)
                {
                    ObservedExasNe.Add((x.Key, Environment.TickCount64));
                }
            }
            foreach(var x in NwPositions)
            {
                if(Vector2.Distance(b.Position2, x.Value) < 1)
                {
                    ObservedExasNw.Add((x.Key, Environment.TickCount64));
                }
            }
        }
    }

    public class Config
    {
        public int TimeStart = 0;
        public int TimeEnd = 7000;
        public bool DisplayLines = false;
        public bool DisplaySpots = true;
        public bool OneMode = false;
    }
}
