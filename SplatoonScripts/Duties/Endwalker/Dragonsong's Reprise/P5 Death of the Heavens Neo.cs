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
    public override Metadata Metadata { get; } = new(4, "NightmareXIV");
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

        OriginalPositions = Controller.GetRegisteredElements().ToDictionary(x => x.Key, x => new Vector3(x.Value.refX, x.Value.refZ, x.Value.refY));
    }

    public override void OnReset()
    {
        MyPosition = default;
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x =>
        {
            x.Value.Enabled = false;
        });
        if(Controller.Scene != 5) return;
        if(Dooms.Count(x => x.StatusList.Any(s => s.StatusId == 2976 && s.RemainingTime >= 23.5)) == 4)
        {
            var iHaveDoom = Dooms.Select(x => x.Address).Contains(BasePlayer.Address);
            var newPosition = GetCharacterNumberAndConfidence(iHaveDoom ? Dooms : NonDooms, BasePlayer);
            if(newPosition.Confidence > MyPosition.Confidence)
            {
                MyPosition = newPosition; 
            }
            var guer = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.DataId == 12637);
            if(guer.Position.Z > 105)
            {
                var newNorth = CardinalDirection.South;
                if(newNorth != RelNorth)
                {
                    MyPosition = MyPosition with { Confidence = 0 };
                    RelNorth = newNorth;
                }
            }
            else if(guer.Position.Z < 95)
            {
                var newNorth = CardinalDirection.North;
                if(newNorth != RelNorth)
                {
                    MyPosition = MyPosition with { Confidence = 0 };
                    RelNorth = newNorth;
                }
            }
            else if(guer.Position.X > 105)
            {
                var newNorth = CardinalDirection.East;
                if(newNorth != RelNorth)
                {
                    MyPosition = MyPosition with { Confidence = 0 };
                    RelNorth = newNorth;
                }
            }
            else if(guer.Position.X < 95)
            {
                var newNorth = CardinalDirection.West;
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
                e.Enabled = true;
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
    }

    public override void OnSettingsDraw()
    {
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
}