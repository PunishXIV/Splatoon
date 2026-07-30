using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.Data;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public class P5_Stray_Apocalypse_Melee : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
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
            
            """);
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        var primaryShown = false;
        var secondaryShown = false;
        for(int i = 0; i < Math.Min(ObservedExasNe.Count, ObservedExasNw.Count); i++)
        {
            var ne = ObservedExasNe[i];
            var nw = ObservedExasNw[i];
            if(Environment.TickCount64 - ne.Item2 > 7000 || Environment.TickCount64 - nw.Item2 > 7000) continue;
            var name = $"nw {nw.Item1.ToString().ToLower()} + ne {ne.Item1.ToString().ToLower()}";
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

    public override void OnReset()
    {
        ObservedExasNw.Clear();
        ObservedExasNe.Clear();
    }

    public override void OnSettingsDraw()
    {
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
}
