using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameFunctions;
using ECommons.GameFunctions.VirtualTableClassifier;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Newtonsoft.Json;
using Pictomancy.ImGuiDraw;
using Splatoon.Data;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SplatoonScriptsOfficial.Duties.Shadowbringers.The_Epic_Of_Alexander;

public class TEA_P3_Wormhole_Formation_Neo : SplatoonScript<TEA_P3_Wormhole_Formation_Neo.Config>
{
    public override Metadata Metadata => new(3, "NightmareXIV");
    public ScriptState State = new();

    public override HashSet<uint>? ValidTerritories => [Raids.The_Epic_of_Alexander_Ultimate];

    public override void OnSetup()
    {
        Controller.RegisterElementsFromMultilineCode("""
            {"Name":"Prepare 1","refX":87.0,"refY":87.0,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Prepare 2","refX":113.0,"refY":87.0,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Prepare 3 Bait","refX":87.0,"refY":113.0,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Prepare 3 Wall","refX":84.09789,"refY":111.68969,"refZ":3.8146973E-06,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Prepare 4 Bait","refX":113.0,"refY":113.0,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Prepare 4 Wall","refX":115.90211,"refY":111.68969,"refZ":3.8146973E-06,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Prepare 5","refX":82.5,"refY":92.5,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Prepare 7","refX":82.5,"refY":92.5,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Prepare 6","refX":117.5,"refY":92.5,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Prepare 8","refX":117.5,"refY":92.5,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Idle Left","refX":81.0,"refY":100.0,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Idle Right","refX":119.0,"refY":100.0,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Soak 1 Left","refX":82.0,"refY":95.0,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Soak 1 Right","refX":118.0,"refY":102.5,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Soak 2 Left","refX":84.0,"refY":95.5,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Soak 2 Right","refX":116.0,"refY":104.0,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Soak 3 Left","refX":90.5,"refY":95.5,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            {"Name":"Soak 3 Right","refX":109.5,"refY":104.0,"radius":0.5,"color":3357671168,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"thicc":4.0,"tether":true}
            """);

    }

    //> [05.11.2026 22:33:29 +03:00] Action #18542 Wormhole Formation cast on 11347/Alexander Prime effect on 11347/Alexander Prime npc id=9220, model id=1583, transform=21 data=11347 name=Alexander Prime ActionEffect|18542|11347|11347|9220|1583|21|11347
    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action?.RowId == 18542)
        {
            this.Controller.Reset();
            State.Phase = Phase.Prepare;
        }
        if(State.Phase != Phase.None)
        {
            //> [05.11.2026 22:51:06 +03:00] Action #18517 Eye of the Chakram cast on 11341/Steam Chakram npc id=9217, model id=1425, transform=0 data=11341 name=Steam Chakram ActionEffect|18517|11341|9217|1425|0|11341
            if(set.Action?.RowId == 18517)
            {
                State.Phase = Phase.Limit_Cut_1;
                var mirroredPuddle = !Svc.Objects.Any(x => x.DataId == 2007519 && Vector2.Distance(x.Position2, new Vector2(89f, 95.5f)) < 3f);
                if(mirroredPuddle)
                {
                    PluginLog.Information("Mirrored puddle detected, swapping soak positions");
                    static Vector3 Mirror(Vector3 p) => new Vector3(200 - p.X, p.Y, p.Z);
                    foreach(var x in Controller.OriginalElements.Where(e => e.Key.StartsWith("Soak ") && e.Key.EndsWith("Left")))
                    {
                        var opposite = x.Key.Substring(0, x.Key.Length - 4) + "Right";
                        Controller.GetElementByName(x.Key).RefPosition = Mirror(Controller.OriginalElements[opposite].RefPosition);
                    }
                    foreach(var x in Controller.OriginalElements.Where(e => e.Key.StartsWith("Soak ") && e.Key.EndsWith("Right")))
                    {
                        var opposite = x.Key.Substring(0, x.Key.Length - 5) + "Left";
                        Controller.GetElementByName(x.Key).RefPosition = Mirror(Controller.OriginalElements[opposite].RefPosition);
                    }
                }
                else
                {
                    PluginLog.Information("No mirrored puddle detected");
                    foreach(var x in Controller.OriginalElements.Where(e => e.Key.StartsWith("Soak ")))
                    {
                        Controller.GetElementByName(x.Key).RefPosition = Controller.OriginalElements[x.Key].RefPosition;
                    }
                }
            }
            //> [05.11.2026 22:51:12 +03:00] Action #18484 Alpha Sword cast on 11342/Cruise Chaser effect on me npc id=9218, model id=1606, transform=0 data=11342 name=Cruise Chaser ActionEffect|18484|11342|me|9218|1606|0|11342
            //> [05.11.2026 22:53:27 +03:00] Action #18883 Super Blassty Charge cast on 11342/Cruise Chaser effect on DRG npc id=9218, model id=1606, transform=0 data=11342 name=Cruise Chaser ActionEffect|18883|11342|DRG|9218|1606|0|11342
            if(set.Action?.RowId == 18484 || set.Action?.RowId == 18883)
            {
                State.Phase++;
            }
        }
    }

    //> [05.11.2026 22:36:43 +03:00] Steam Chakram starts casting 18517 (9217>18517)
    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if(State.Phase != Phase.None)
        {
            if(packet->ActionDescriptor == new ActionDescriptor(ActionType.Action, 18517))
            {
                State.Phase = Phase.Chakram_casting;
            }
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(State.Phase == Phase.Prepare)
        {
            //> [05.11.2026 22:36:32 +03:00] Message: VFX vfx/lockon/eff/m0361trg_a1t.avfx spawned on me npc id=0, model id=0, name npc id=0, position=<99.87024, 0, 100.053345>, name=Sage
            if(vfxPath.StartsWith("vfx/lockon/eff/m0361trg_a") && int.TryParse(vfxPath.AsSpan("vfx/lockon/eff/m0361trg_a".Length, 1), out var lcNum))
            {
                State.Assignments[target] = lcNum;
                if(target == BasePlayer.ObjectId)
                {
                    State.MirroredAlex = Svc.Objects.OfTypeIBattleNpc().Any(x => x.DataId == 11347 && Vector2.Distance(x.Position2, new Vector2(100f, 121.5f)) > 3f);
                    State.FourBaits = Svc.Objects.OfTypeIBattleNpc().Any(x => x.DataId == 11340 && Vector2.Distance(x.Position2, new Vector2(85f, 85f)) < 3f) || Svc.Objects.OfTypeIBattleNpc().Any(x => x.DataId == 11340 && Vector2.Distance(x.Position2, new Vector2(115f, 115f)) < 3f);

                    foreach(var x in Controller.OriginalElements)
                    {
                        Controller.GetElementByName(x.Key)!.RefPosition = !State.MirroredAlex?x.Value.RefPosition:MathHelper.RotateWorldPoint(new(100, 0, 100), 180.DegreesToRadians(), x.Value.RefPosition);
                    }
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        if(ImGui.CollapsingHeader("Debug"))
        {
            if(ImGui.Button("copy state"))
            {
                GenericHelpers.Copy(JsonConvert.SerializeObject(State));
            }
            if(ImGui.Button("paste state"))
            {
                try
                {
                    State = JsonConvert.DeserializeObject<ScriptState>(GenericHelpers.Paste()!) ?? new();
                }
                catch(Exception e)
                {
                    e.Log();
                }
            }
            ImGuiEx.Checkbox("Mirrored alex", ref State.MirroredAlex);
            ImGuiEx.Checkbox("4 baits", ref State.FourBaits);
        }
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        if(State.Phase > Phase.None && State.Phase < Phase.Complete)
        {
            if(State.Assignments.TryGetValue(BasePlayer.ObjectId, out var myNumber))
            {
                Controller.DisplayAttentionWindowLine($"Your number: {myNumber}");
            }
            Controller.DisplayAttentionWindowLine(ImGuiColors.DalamudGrey3, $"Phase: {State.Phase}");
        }
        {
            if(State.Assignments.TryGetValue(BasePlayer.ObjectId, out var myNumber))
            {
                if(State.Phase == Phase.Prepare || State.Phase == Phase.Chakram_casting)
                {
                    if(myNumber == 1) Display("Prepare 1");
                    if(myNumber == 2) Display("Prepare 2");
                    if(myNumber == 3) Display(State.FourBaits ? "Prepare 3 Wall" : "Prepare 3 Bait");
                    if(myNumber == 4) Display(!State.FourBaits ? "Prepare 4 Wall" : "Prepare 4 Bait");
                    if(myNumber == 5) Display("Prepare 5");
                    if(myNumber == 6) Display("Prepare 6");
                    if(myNumber == 7) Display("Prepare 7");
                    if(myNumber == 8) Display("Prepare 8");
                }
                if(State.Phase == Phase.Limit_Cut_1)
                {
                    if(myNumber == 1) Display("Prepare 1");
                    if(myNumber == 2) Display("Prepare 2");
                    if(myNumber == 3) Display(State.FourBaits ? "Prepare 3 Wall" : "Prepare 3 Bait");
                    if(myNumber == 4) Display(!State.FourBaits ? "Prepare 4 Wall" : "Prepare 4 Bait");
                    if(myNumber == 5) Display("Soak 1 Left");
                    if(myNumber == 6) Display("Soak 1 Right");
                    if(myNumber == 7) Display("Idle Left");
                    if(myNumber == 8) Display("Idle Right");
                }
                if(State.Phase == Phase.Limit_Cut_2 || State.Phase == Phase.Limit_Cut_3)
                {
                    if(myNumber == 1) Display("Prepare 1");
                    if(myNumber == 2) Display("Prepare 2");
                    if(myNumber == 3) Display("Prepare 3 Wall");
                    if(myNumber == 4) Display("Prepare 4 Wall");
                    if(myNumber == 5) Display("Soak 1 Left");
                    if(myNumber == 6) Display("Soak 1 Right");
                    if(myNumber == 7) Display("Idle Left");
                    if(myNumber == 8) Display("Idle Right");
                }
                if(State.Phase == Phase.Limit_Cut_4)
                {
                    if(myNumber == 1) Display("Idle Left");
                    if(myNumber == 2) Display("Idle Right");
                    if(myNumber == 3) Display("Prepare 3 Wall");
                    if(myNumber == 4) Display("Prepare 4 Wall");
                    if(myNumber == 5) Display("Soak 1 Left");
                    if(myNumber == 6) Display("Soak 1 Right");
                    if(myNumber == 7) Display("Idle Left");
                    if(myNumber == 8) Display("Idle Right");
                }
                if(State.Phase == Phase.Limit_Cut_5)
                {
                    if(myNumber == 1) Display("Idle Left");
                    if(myNumber == 2) Display("Idle Right");
                    if(myNumber == 3) Display("Prepare 3 Wall");
                    if(myNumber == 4) Display("Prepare 4 Wall");
                    if(myNumber == 5) Display("Prepare 1");
                    if(myNumber == 6) Display("Prepare 2");
                    if(myNumber == 7) Display("Soak 2 Left");
                    if(myNumber == 8) Display("Soak 2 Right");
                }
                if(State.Phase == Phase.Limit_Cut_6)
                {
                    if(myNumber == 1) Display("Idle Left");
                    if(myNumber == 2) Display("Idle Right");
                    if(myNumber == 3) Display("Idle Left");
                    if(myNumber == 4) Display("Idle Right");
                    if(myNumber == 5) Display("Prepare 1");
                    if(myNumber == 6) Display("Prepare 2");
                    if(myNumber == 7) Display("Soak 2 Left");
                    if(myNumber == 8) Display("Soak 2 Right");
                }
                if(State.Phase == Phase.Limit_Cut_7 || State.Phase == Phase.Limit_Cut_8)
                {
                    if(myNumber == 1) Display("Soak 3 Left");
                    if(myNumber == 2) Display("Soak 3 Right");
                    if(myNumber == 3) Display("Idle Left");
                    if(myNumber == 4) Display("Idle Right");
                    if(myNumber == 5) Display("Prepare 1");
                    if(myNumber == 6) Display("Prepare 2");
                    if(myNumber == 7) Display("Prepare 3 Bait");
                    if(myNumber == 8) Display("Prepare 4 Bait");
                }
            }
        }
    }

    void Display(string element)
    {
        if(Controller.TryGetElementByName(element, out var e))
        {
            e.Enabled = true;
            e.color = Controller.AttentionColor;
        }
        else
        {
            PluginLog.Warning($"Invalid element {element}");
        }
    }

    public override void OnReset()
    {
        State = new();
    }

    public class ScriptState
    {
        public Phase Phase;
        public Dictionary<uint, int> Assignments = [];
        public bool MirroredAlex = false;
        public bool FourBaits = false;
    }

    public class Config
    {

    }

    public enum Phase
    {
        None,
        Prepare,
        Chakram_casting,
        Limit_Cut_1,
        Limit_Cut_2,
        Limit_Cut_3,
        Limit_Cut_4,
        Limit_Cut_5,
        Limit_Cut_6,
        Limit_Cut_7,
        Limit_Cut_8,
        Complete,
    }
}
