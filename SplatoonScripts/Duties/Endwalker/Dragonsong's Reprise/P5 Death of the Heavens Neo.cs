using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;
public sealed class P5_Death_of_the_Heavens_Neo : SplatoonScript
{
    public override Metadata Metadata { get; } = new(5, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [Raids.Dragonsongs_Reprise_Ultimate];

    IPlayerCharacter BasePlayer
    {
        get
        {
            if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback] && C.BPO != "" && Players.TryGetFirst(x => x.GetNameWithWorld() == C.BPO, out var p))
            {
                return p;
            }
            return Player.Object;
        }
    }
    IEnumerable<IPlayerCharacter> Players => Controller.GetPartyMembers();
    IPlayerCharacter[] Dooms => Players.Where(x => x.StatusList.Any(s => s.StatusId == 2976)).ToArray();
    IPlayerCharacter[] NonDooms => Players.Where(x => !x.StatusList.Any(s => s.StatusId == 2976)).ToArray();

    (int Num, float Confidence) MyPosition = default;
    CardinalDirection RelNorth;
    Dictionary<string, Vector3> OriginalPositions;

    public enum Shape { RedCircle, BlueCross, PurpleSquare, GreenTriangle }
    Dictionary<uint, Shape> Shapes = [];

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Doom1", """{"Name":"","refX":100.0,"refY":112.0,"radius":1.0,"color":3358850816,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"overlayText":"Doom 1","tether":true}""");
        Controller.RegisterElementFromCode("Doom2", """{"Name":"","refX":84.5,"refY":112.0,"radius":1.0,"color":3358850816,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"overlayText":"Doom 2","tether":true}""");
        Controller.RegisterElementFromCode("Doom3", """{"Name":"","refX":84.5,"refY":87.5,"radius":1.0,"color":3358850816,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"overlayText":"Doom 3","tether":true}""");
        Controller.RegisterElementFromCode("Doom4", """{"Name":"","refX":100.0,"refY":88.0,"radius":1.0,"color":3358850816,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"overlayText":"Doom 4","tether":true}""");
        Controller.RegisterElementFromCode("Clean1", """{"Name":"","refX":100.0,"refY":120.5,"radius":1.0,"color":3358850816,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"overlayText":"Clean 1","tether":true}""");
        Controller.RegisterElementFromCode("Clean2", """{"Name":"","refX":116.0,"refY":112.0,"radius":1.0,"color":3358850816,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"overlayText":"Clean 2","tether":true}""");
        Controller.RegisterElementFromCode("Clean3", """{"Name":"","refX":115.5,"refY":88.0,"radius":1.0,"color":3358850816,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"overlayText":"Clean 3","tether":true}""");
        Controller.RegisterElementFromCode("Clean4", """{"Name":"","refX":99.5,"refY":79.5,"radius":1.0,"color":3358850816,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"overlayText":"Clean 4","tether":true}""");

        Controller.RegisterElementFromCode("BaitDoom1", """{"Name":"","refX":100.0,"refY":112.0,"radius":1.0,"color":3358850816,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"overlayText":"Bait Red 1","tether":true}""");
        Controller.RegisterElementFromCode("BaitDoom4", """{"Name":"","refX":100.0,"refY":88.0,"radius":1.0,"color":3358850816,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"overlayText":"Bait Red 2","tether":true}""");

        Controller.RegisterElementFromCode("BaitNoDoom", "{\"Name\":\"\",\"refX\":96.5,\"refY\":99.5,\"radius\":4.0,\"color\":3358850816,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":4.0,\"overlayText\":\"Prepare to adjust\",\"tether\":true}");

        Controller.RegisterElementFromCode("BaitDoom2", """{"Name":"","refX":103.0,"refY":102.5,"radius":1.0,"color":3358850816,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"overlayText":"Doom 2","tether":true}""");
        Controller.RegisterElementFromCode("BaitDoom3", """{"Name":"","refX":103.0,"refY":97.5,"radius":1.0,"color":3358850816,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"overlayText":"Doom 3","tether":true}""");

        Controller.RegisterElementFromCode("LineDoomRedLeft", """{"Name":"","type":2,"refX":100.0,"refY":120.0,"offX":100.0,"offY":100.0,"radius":0.0,"Filled":false,"fillIntensity":0.345,"thicc":4.0}""");
        Controller.RegisterElementFromCode("LineDoomPurple", """{"Name":"","type":2,"refX":116.5,"refY":112.5,"offX":100.0,"offY":100.0,"radius":0.0,"Filled":false,"fillIntensity":0.345,"thicc":4.0}""");
        Controller.RegisterElementFromCode("LineDoomGreen", """{"Name":"","type":2,"refX":116.5,"refY":87.0,"offX":100.0,"offY":100.0,"radius":0.0,"Filled":false,"fillIntensity":0.345,"thicc":4.0}""");
        Controller.RegisterElementFromCode("LineDoomRedRight", """{"Name":"","type":2,"refX":100.0,"refY":80.0,"offX":100.0,"offY":100.0,"radius":0.0,"Filled":false,"fillIntensity":0.345,"thicc":4.0}""");
        Controller.RegisterElementFromCode("LineCleanBlueNorth", """{"Name":"","type":2,"refX":80.0,"refY":100.0,"offX":100.0,"offY":100.0,"radius":0.0,"Filled":false,"fillIntensity":0.345,"thicc":4.0}""");
        Controller.RegisterElementFromCode("LineCleanBlueSouth", """{"Name":"","type":2,"refX":120.0,"refY":100.0,"offX":100.0,"offY":100.0,"radius":0.0,"Filled":false,"fillIntensity":0.345,"thicc":4.0}""");
        Controller.RegisterElementFromCode("LineCleanPurple", """{"Name":"","type":2,"refX":83.5,"refY":87.5,"offX":100.0,"offY":100.0,"radius":0.0,"Filled":false,"fillIntensity":0.345,"thicc":4.0}""");
        Controller.RegisterElementFromCode("LineCleanGreen", """{"Name":"","type":2,"refX":83.5,"refY":112.5,"offX":100.0,"offY":100.0,"radius":0.0,"Filled":false,"fillIntensity":0.345,"thicc":4.0}""");

        OriginalPositions = Controller.GetRegisteredElements().ToDictionary(x => x.Key, x => new Vector3(x.Value.refX, x.Value.refZ, x.Value.refY));

        Controller.RegisterElementFromCode("KBHelper", """{"Name":"","type":1,"radius":0.0,"color":4294901992,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":3639,"refActorComparisonType":6,"onlyVisible":true,"tether":true,"ExtraTetherLength":17.0,"LineEndB":1}""");
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        //red: >  Message: VFX vfx/lockon/eff/r1fz_firechain_01x.avfx spawned on  npc id=0, model id=0, name npc id=0, position=<100.11438, -3.8146973E-06, 109.391846>, name=White Mage
        //green: > Message: VFX vfx/lockon/eff/r1fz_firechain_02x.avfx spawned on  npc id=0, model id=0, name npc id=0, position=<101.548706, -3.8146973E-06, 98.649536>, name=Black Mage
        //purple: > Message: VFX vfx/lockon/eff/r1fz_firechain_03x.avfx spawned on  npc id=0, model id=0, name npc id=0, position=<101.426636, -3.8146973E-06, 101.304565>, name=Gunbreaker
        //blue: >  Message: VFX vfx/lockon/eff/r1fz_firechain_04x.avfx spawned on  npc id=0, model id=0, name npc id=0, position=<98.64926, -0.012660894, 98.40637>, name=Paladin
        if(vfxPath == "vfx/lockon/eff/r1fz_firechain_01x.avfx")
        {
            Shapes[target] = Shape.RedCircle;
        }
        if(vfxPath == "vfx/lockon/eff/r1fz_firechain_02x.avfx")
        {
            Shapes[target] = Shape.GreenTriangle;
        }
        if(vfxPath == "vfx/lockon/eff/r1fz_firechain_03x.avfx")
        {
            Shapes[target] = Shape.PurpleSquare;
        }
        if(vfxPath == "vfx/lockon/eff/r1fz_firechain_04x.avfx")
        {
            Shapes[target] = Shape.BlueCross;
        }
    }

    public override void OnReset()
    {
        MyPosition = default;
        this.Shapes.Clear();
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x =>
        {
            x.Value.Enabled = false;
        });
        if(Controller.Scene != 5) return;
        if(Shapes.TryGetValue(BasePlayer.EntityId, out var myShape) 
            && Shapes.TryGetFirst(x => x.Value == myShape 
            && x.Key != BasePlayer.EntityId, out var myPartnerId) && myPartnerId.Key.TryGetObject(out var myPartner)
            && Controller.GetPartyMembers().Any(x => x.StatusList.Any(s => s.StatusId == 2976)))
        {
            var doomOnMe = BasePlayer.StatusList.Any(x => x.StatusId == 2976);
            if(myShape == Shape.RedCircle)
            {
                if(MyPosition.Num == 1)
                {
                    Controller.GetElementByName("LineDoomRedLeft").Enabled = true;
                }
                else
                {
                    Controller.GetElementByName("LineDoomRedRight").Enabled = true;
                }
            }
            if(myShape == Shape.BlueCross)
            {
                var northElement = Controller.GetElementByName("LineCleanBlueNorth");
                var northPos = new Vector2(northElement.refX, northElement.refY);
                if(Vector2.Distance(northPos, BasePlayer.Position.ToVector2()) < Vector2.Distance(northPos, myPartner.Position.ToVector2()))
                {
                    northElement.Enabled = true;
                }
                else
                {
                    Controller.GetElementByName("LineCleanBlueSouth").Enabled = true;
                }
            }
            if(myShape == Shape.PurpleSquare)
            {
                Controller.GetElementByName(doomOnMe ? "LineDoomPurple" : "LineCleanPurple").Enabled = true;
            }
            if(myShape == Shape.GreenTriangle)
            {
                Controller.GetElementByName(doomOnMe ? "LineDoomGreen" : "LineCleanGreen").Enabled = true;
            }
            var col = GetRainbowColor(1).ToUint();
            Controller.GetRegisteredElements().Where(x => x.Key.StartsWith("LineDoom") || x.Key.StartsWith("LineClean")).Each(x => x.Value.color = col);
            Controller.GetElementByName("KBHelper").Enabled = true;
        }
        if(Dooms.Count(x => x.StatusList.Any(s => s.StatusId == 2976 && s.RemainingTime >= 23.5)) == 4)
        {
            Shapes.Clear();
            var iHaveDoom = Dooms.Select(x => x.Address).Contains(BasePlayer.Address);
            var newPosition = GetCharacterNumberAndConfidence(iHaveDoom ? Dooms : NonDooms, BasePlayer);
            if(newPosition.Confidence > MyPosition.Confidence)
            {
                MyPosition = newPosition; 
            }
            var guer = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.DataId == 12637);
            if(guer.Position.Z > 105)
            {
                var newNorth = C.ForceDirection ?? CardinalDirection.South;
                if(newNorth != RelNorth)
                {
                    MyPosition = MyPosition with { Confidence = 0 };
                    RelNorth = newNorth;
                }
            }
            else if(guer.Position.Z < 95)
            {
                var newNorth = C.ForceDirection ?? CardinalDirection.North;
                if(newNorth != RelNorth)
                {
                    MyPosition = MyPosition with { Confidence = 0 };
                    RelNorth = newNorth;
                }
            }
            else if(guer.Position.X > 105)
            {
                var newNorth = C.ForceDirection ?? CardinalDirection.East;
                if(newNorth != RelNorth)
                {
                    MyPosition = MyPosition with { Confidence = 0 };
                    RelNorth = newNorth;
                }
            }
            else if(guer.Position.X < 95)
            {
                var newNorth = C.ForceDirection ?? CardinalDirection.West;
                if(newNorth != RelNorth)
                {
                    MyPosition = MyPosition with { Confidence = 0 };
                    RelNorth = newNorth;
                }
            }
            UpdatePositions();
            var str = (iHaveDoom ? "Doom" : "Clean") + (MyPosition.Num).ToString();
            if(EzThrottler.Throttle("doomeam", 200)) DuoLog.Information(str+ $"{MyPosition}");
        }
        else if(Dooms.Count(x => x.StatusList.Any(s => s.StatusId == 2976 && s.RemainingTime >= 17f)) == 4)
        {
            var iHaveDoom = Dooms.Select(x => x.Address).Contains(BasePlayer.Address);
            Controller.GetElementByName((iHaveDoom ? "Doom" : "Clean") + (MyPosition.Num).ToString()).Enabled = true;
        }
        else if(Dooms.Count(x => x.StatusList.Any(s => s.StatusId == 2976 && s.RemainingTime >= 8f)) == 4)
        {
            var iHaveDoom = Dooms.Select(x => x.Address).Contains(BasePlayer.Address);
            Controller.GetElementByName((iHaveDoom ? ("BaitDoom" + (MyPosition.Num).ToString()) : "BaitNoDoom")).Enabled = true;
        }
        else if(Dooms.Any(x => x.StatusList.Any(s => s.StatusId == 2976)))
        {
            var iHaveDoom = Dooms.Select(x => x.Address).Contains(BasePlayer.Address);
            if(iHaveDoom)
            {
                var e = Controller.GetElementByName("Clean" + (MyPosition.Num).ToString());
                //e.Enabled = true;
            }
        }
    }

    void UpdatePositions()
    {
        foreach(var x in OriginalPositions)
        {
            var angle = RelNorth switch
            {
                CardinalDirection.West => 0,
                CardinalDirection.North => 90,
                CardinalDirection.East => 180,
                CardinalDirection.South => 270,
            };
            Controller.GetElementByName(x.Key).SetRefPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), x.Value));
        }
    }

    (int Number, float Confidence) GetCharacterNumberAndConfidence(ICollection<IPlayerCharacter> characters, IPlayerCharacter target)
    {
        var angle = RelNorth switch
        {
            CardinalDirection.West => 90,
            CardinalDirection.North => 0,
            CardinalDirection.East => 270,
            CardinalDirection.South => 180,
        };
        var ordered = characters.OrderBy(c => MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), c.Position).X).ToList();
        //PluginLog.Information($"{RelNorth} {ordered.Select(x => x.GetNameWithWorld()).Print("\n")}");
        int number = -1;
        for(int i = 0; i < ordered.Count; i++)
        {
            if(ordered[i].AddressEquals(target))
            {
                number = i + 1;
                break;
            }
        }

        float confidence = 100f;
        foreach(var c in ordered)
        {
            if(!c.AddressEquals(target))
            {
                confidence -= Math.Abs(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), c.Position).Z - MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), target.Position).Z);
            }
        }

        PluginLog.Information($"Ret: {(number, confidence)}");
        return (number, confidence);
    }


    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public string BPO = "";
        public CardinalDirection? ForceDirection = null;
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.EnumCombo("Override position", ref C.ForceDirection);
        ImGui.InputText("BPO", ref C.BPO);
        if(ImGui.BeginCombo("##sel", "Select base player"))
        {
            foreach(var x in Players)
            {
                if(ImGuiEx.Selectable($"{x.GetNameWithWorld()}"))
                {
                    C.BPO = x.GetNameWithWorld();
                    MyPosition = default;
                }
            }
            ImGui.EndCombo();
        }
    }

    public static Vector4 GetRainbowColor(double cycleSeconds)
    {
        if(cycleSeconds <= 0d)
        {
            cycleSeconds = 1d;
        }

        var ms = Environment.TickCount64;
        var t = (ms / 1000d) / cycleSeconds;
        var hue = t % 1f;
        return HsvToVector4(hue, 1f, 1f);
    }

    public static Vector4 HsvToVector4(double h, double s, double v)
    {
        double r = 0f, g = 0f, b = 0f;
        var i = (int)(h * 6f);
        var f = h * 6f - i;
        var p = v * (1f - s);
        var q = v * (1f - f * s);
        var t = v * (1f - (1f - f) * s);

        switch(i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new Vector4((float)r, (float)g, (float)b, 1f);
    }
}