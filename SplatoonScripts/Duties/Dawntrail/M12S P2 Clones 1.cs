using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using static Splatoon.Splatoon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using ECommons.DalamudServices.Legacy;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public unsafe class M12S_P2_Clones_1 : SplatoonScript
{
    public override Metadata Metadata { get; } = new(4, "NightmareXIV, Leo");
    public override HashSet<uint>? ValidTerritories { get; } = [1327];

    public override void OnSetup()
    {
        for(int i = 0; i < 4; i++)
        {
            Controller.RegisterElementFromCode($"Dark{i}", """{"Name":"CloneDark","type":3,"refZ":5.0,"radius":0.0,"color":3372155094,"Filled":false,"fillIntensity":0.345,"thicc":20.0,"refActorNPCNameID":14380,"refActorComparisonType":2,"includeRotation":true}""");
            Controller.RegisterElementFromCode($"Fire{i}", """{"Name":"CloneFire","type":3,"refZ":5.0,"radius":0.0,"color":3355492351,"Filled":false,"fillIntensity":0.345,"thicc":20.0,"refActorNPCNameID":14380,"refActorComparisonType":2,"includeRotation":true}""");

        }
        Controller.RegisterElementFromCode($"BaitPosition", """{"Name":"","radius":0.5,"Donut":0.5,"fillIntensity":0.5,"thicc":5.0,"tether":true}""");
    }

    int RentedDark = 0;
    int RentedFire = 0;

    int Phase = 0;

    uint DarknessCast = 46303;
    uint FireCast = 46301;

    uint MasterFireClone = 0;
    uint MasterDarknessClone = 0;

    uint RN = 0;

    List<IBattleNpc> battleNpcs = [];
    IBattleNpc RelativeNorthClone = null;
    Direction relativeNorth = Direction.None;

    List<uint> FireClones = [];
    List<uint> DarknessClones = [];

    enum Direction { None, NE, SE, SW, NW }
    Dictionary<Direction, Vector2> CloneDirections = new()
    {
        [Direction.NE] = new(109.88013f, 90.073975f),
        [Direction.SE] = new(109.88013f, 109.88013f),
        [Direction.SW] = new(90.073975f, 109.88013f),
        [Direction.NW] = new(90.073975f, 90.073975f),
    };

    public enum Strat { Static, DN, Relative }
    public Dictionary<Strat, string> Strats = new()
    {
        {Strat.Static,  "Static/JP"},
        {Strat.DN,      "DN/NA"},
        {Strat.Relative,"Clone Relative"}
    };
    public enum Role { Tank, Melee, Healer, Ranged }

    public override void OnReset()
    {
        Phase = 0;
        MasterFireClone = 0;
        MasterDarknessClone = 0;
        FireClones.Clear();
        DarknessClones.Clear();
        battleNpcs.Clear();
        RelativeNorthClone = null;
        RN = 0;
        relativeNorth = Direction.None;
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if(packet->ActionID == 46368)
        {
            this.Controller.Reset();
        }
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        //BasePlayer(≈LocalPlayer) will always be null in loading screens
        //if(BasePlayer.StatusList.Any(x => x.StatusId == 3323))
        if (BasePlayer != null && (BasePlayer.StatusList?.Any(x => x.StatusId == 3323) ?? false))
        {
            C.HasDarkResistanceDown = true;
        }
        else
        {
            C.HasDarkResistanceDown = false;
        }
        if(DarknessClones.Count != 2)
        {
            var e = Controller.GetElementByName($"Dark0");
            e.Enabled = true;
            e.refActorObjectID = MasterDarknessClone;

        }
        if(FireClones.Count != 2)
        {
            var e = Controller.GetElementByName($"Fire0");
            e.Enabled = true;
            e.refActorObjectID = MasterFireClone;
        }
        foreach(var x in Svc.Objects.OfType<IBattleNpc>())
        {
            if(DarknessClones.Count != 2 && x.IsCasting(DarknessCast))
            {
                MasterDarknessClone = x.ObjectId;
                var e = Controller.GetElementByName($"Dark0");
                e.Enabled = true;
                e.refActorObjectID = MasterDarknessClone;
            }
            if(FireClones.Count != 2 && x.IsCasting(FireCast))
            {
                MasterFireClone = x.ObjectId;
                var e = Controller.GetElementByName($"Fire0");
                e.Enabled = true;
                e.refActorObjectID = MasterFireClone;
            }
            if(DarknessClones.Count < 2 && MasterDarknessClone != 0 && MasterDarknessClone.TryGetBattleNpc(out var darkness))
            {
                DarknessClones = Svc.Objects.OfType<IBattleNpc>().Where(x => x.DataId == 19204 && Vector3.Distance(darkness.Position, x.Position).ApproximatelyEquals(5f, 0.1f)).Select(x => x.ObjectId).ToList();
            }
            if(FireClones.Count < 2 && MasterFireClone != 0 && MasterFireClone.TryGetBattleNpc(out var fire))
            {
                FireClones = Svc.Objects.OfType<IBattleNpc>().Where(x => x.DataId == 19204 && Vector3.Distance(fire.Position, x.Position).ApproximatelyEquals(5f, 0.1f)).Select(x => x.ObjectId).ToList();
            }
            if(DarknessClones.Count == 2)
            {
                for(int i = 0; i < DarknessClones.Count; i++)
                {
                    var e = Controller.GetElementByName($"Dark{i}");
                    e.Enabled = true;
                    e.refActorObjectID = DarknessClones[i];
                }
            }
            if(FireClones.Count == 2)
            {
                for(int i = 0; i < FireClones.Count; i++)
                {
                    var e = Controller.GetElementByName($"Fire{i}");
                    e.Enabled = true;
                    e.refActorObjectID = FireClones[i];
                }
            }
        }
        if(DarknessClones.Count == 2 && Phase == 0)
        {
            battleNpcs = Svc.Objects.OfType<IBattleNpc>().ToList();

            // result (IBattleNpc Npc, Direction Dir)
            var result = DarknessClones
            .Select(id => battleNpcs.FirstOrDefault(n => n.ObjectId == id))
            .Where(npc => npc != null)
            .Select(npc => new
            {
                Npc = npc,
                //Dir = CloneDirections.FirstOrDefault(kvp => kvp.Value == npc.Position.ToVector2()).Key
                Dir = CloneDirections.FirstOrDefault(kvp => Vector2.Distance(kvp.Value, npc.Position.ToVector2()) < 0.1f).Key
            })
            .FirstOrDefault(x => x.Dir != Direction.None);

            if(result != null)
            {
                RelativeNorthClone = result.Npc;
                relativeNorth = result.Dir;
                RN = RelativeNorthClone.ObjectId;

                var e = Controller.GetElementByName($"BaitPosition");
                e.refActorObjectID = RelativeNorthClone.ObjectId;

                switch(C.SelectedStrat)
                {
                    case Strat.Static:
                    case Strat.Relative:

                        if(C.HasDarkResistanceDown)
                        {
                            (e.offX, e.offY) = (relativeNorth, C.IsMelee, C.FirePositionIsLeft) switch
                            {
                                (Direction.NE,  true,  true) => (101.365f,  98.635f),
                                (Direction.NE,  true, false) => (101.365f,  98.635f),
                                (Direction.NE, false,  true) => ( 86.350f, 100.000f),
                                (Direction.NE, false, false) => (100.000f, 113.650f),

                                (Direction.SE,  true,  true) => (101.365f, 101.365f),
                                (Direction.SE,  true, false) => (101.365f, 101.365f),
                                (Direction.SE, false,  true) => (100.000f,  86.350f),
                                (Direction.SE, false, false) => ( 86.350f, 100.000f),
 
                                (Direction.SW,  true,  true) => ( 98.635f, 101.365f),
                                (Direction.SW,  true, false) => ( 98.635f, 101.365f),
                                (Direction.SW, false,  true) => (113.650f, 100.000f),
                                (Direction.SW, false, false) => (100.000f,  86.350f),

                                (Direction.NW,  true,  true) => ( 98.635f,  98.635f),
                                (Direction.NW,  true, false) => ( 98.635f,  98.635f),
                                (Direction.NW, false,  true) => (100.000f, 113.650f),
                                (Direction.NW, false, false) => (113.650f, 100.000f),

                                _ => (0.0f, 0.0f),
                            };
                        }
                        else
                        {
                            (e.offX, e.offY) = (relativeNorth, C.IsMelee, C.DarkPositionIsLeft) switch
                            {
                                (Direction.NE,  true,  true) => ( 93.175f,  98.635f),
                                (Direction.NE,  true, false) => (101.365f, 106.825f),
                                (Direction.NE, false,  true) => (100.000f,  86.350f),
                                (Direction.NE, false, false) => (113.650f, 100.000f),

                                (Direction.SE,  true,  true) => (101.365f,  93.175f),
                                (Direction.SE,  true, false) => ( 93.175f, 101.365f),
                                (Direction.SE, false,  true) => (113.650f, 100.000f),
                                (Direction.SE, false, false) => (100.000f, 113.650f),

                                (Direction.SW,  true,  true) => (106.825f, 101.365f),
                                (Direction.SW,  true, false) => ( 98.635f,  93.175f),
                                (Direction.SW, false,  true) => (100.000f, 113.650f),
                                (Direction.SW, false, false) => ( 86.350f, 100.000f),

                                (Direction.NW,  true,  true) => ( 98.635f, 106.825f),
                                (Direction.NW,  true, false) => (106.825f,  98.635f),
                                (Direction.NW, false,  true) => ( 86.350f, 100.000f),
                                (Direction.NW, false, false) => (100.000f,  86.350f),

                                _ => (0.0f, 0.0f),
                            };
                        }
                        e.color = GetRainbowColor(1f).ToUint();
                        e.radius = C.IsMelee ? 0.5f : 1.45f;
                        e.Enabled = !C.SkipMechs;
                        //e.Enabled = true;
                        break;

                    case Strat.DN:
                        (e.offX, e.offY) = (relativeNorth, C.role, C.HasDarkResistanceDown) switch
                        {
                            //------------------------ Fire --------------------------
                            (Direction.NE, Role.Tank  ,  true) => (101.365f,  98.635f),
                            (Direction.NE, Role.Melee ,  true) => (101.365f,  98.635f),
                            (Direction.NE, Role.Healer,  true) => (100.000f, 113.650f),
                            (Direction.NE, Role.Ranged,  true) => (100.000f, 113.650f),

                            (Direction.SE, Role.Tank  ,  true) => (101.365f, 101.365f),
                            (Direction.SE, Role.Melee ,  true) => (101.365f, 101.365f),
                            (Direction.SE, Role.Healer,  true) => (100.000f,  86.350f),
                            (Direction.SE, Role.Ranged,  true) => (100.000f,  86.350f),

                            (Direction.SW, Role.Tank  ,  true) => ( 98.635f, 101.365f),
                            (Direction.SW, Role.Melee ,  true) => ( 98.635f, 101.365f),
                            (Direction.SW, Role.Healer,  true) => (100.000f,  86.350f),
                            (Direction.SW, Role.Ranged,  true) => (100.000f,  86.350f),

                            (Direction.NW, Role.Tank  ,  true) => ( 98.635f,  98.635f),
                            (Direction.NW, Role.Melee ,  true) => ( 98.635f,  98.635f),
                            (Direction.NW, Role.Healer,  true) => (113.650f, 100.000f),
                            (Direction.NW, Role.Ranged,  true) => (113.650f, 100.000f),

                            //------------------------ Dark --------------------------
                            (Direction.NE, Role.Tank  , false) => (101.365f, 106.825f),
                            (Direction.NE, Role.Melee , false) => ( 93.175f,  98.635f),
                            (Direction.NE, Role.Healer, false) => (100.000f,  86.350f),
                            (Direction.NE, Role.Ranged, false) => (113.650f, 100.000f),

                            (Direction.SE, Role.Tank  , false) => (101.365f,  93.175f),
                            (Direction.SE, Role.Melee , false) => ( 93.175f, 101.365f),
                            (Direction.SE, Role.Healer, false) => (113.650f, 100.000f),
                            (Direction.SE, Role.Ranged, false) => (100.000f, 113.650f),

                            (Direction.SW, Role.Tank  , false) => ( 98.635f,  93.175f),
                            (Direction.SW, Role.Melee , false) => (106.825f, 101.365f),
                            (Direction.SW, Role.Healer, false) => (100.000f, 113.650f),
                            (Direction.SW, Role.Ranged, false) => ( 86.350f, 100.000f),

                            (Direction.NW, Role.Tank  , false) => (106.825f,  98.635f),
                            (Direction.NW, Role.Melee , false) => ( 98.635f, 106.825f),
                            (Direction.NW, Role.Healer, false) => (100.000f,  86.350f),
                            (Direction.NW, Role.Ranged, false) => ( 86.350f, 100.000f),

                            _ => (0.0f, 0.0f),

                        };
                        e.color = GetRainbowColor(1f).ToUint();
                        e.radius = C.role is Role.Tank or Role.Melee? 0.5f : 1.45f;
                        e.Enabled = !C.SkipMechs;
                        //e.Enabled = true;
                        break;

                    default:
                        break;

                }
                
            }
        }
        if(Phase == 1)
        {
            var e = Controller.GetElementByName($"BaitPosition");
            e.Enabled = false;
        }
    }
    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(Phase == 0 && DarknessClones.Count == 2 && set.Action?.RowId == 46304)
        {
            Phase = 1;
        }
    }
    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"Script Mode:");
        ImGuiEx.RadioButtonBool("Highlight only", "Resolve mechanic", ref C.SkipMechs);

        if(!C.SkipMechs)
        {
            ImGui.Checkbox("Disable rainbow coloring", ref C.NoRainbow);
            if(C.NoRainbow)
            {
                ImGui.ColorEdit4("Alternative color", ref C.FixedColor, ImGuiColorEditFlags.NoInputs);
            }

            ImGui.NewLine();
            ImGuiEx.TextV("Strategy:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            ImGuiEx.EnumCombo("##Strategy", ref C.SelectedStrat, names: Strats);
            switch (C.SelectedStrat)
            {
                case Strat.Static:

                    ImGuiEx.TextV("Your Role:");
                    ImGui.SameLine();
                    ImGuiEx.RadioButtonBool("Tank/Melee", "Healer/Ranged", ref C.IsMelee, sameLine: true);

                    ImGuiEx.TextV("Dark Baiting Position, Looking at the Relative North Clone:");
                    ImGui.SameLine();
                    ImGuiEx.RadioButtonBool("Left##DarkL", "Right##DarkR", ref C.DarkPositionIsLeft, sameLine: true);

                    if(!C.IsMelee)
                    {
                        ImGuiEx.TextV("Fire Baiting Position, Looking at the Relative North Clone:");
                        ImGui.SameLine();
                        ImGuiEx.RadioButtonBool("Left##FireL", "Right##FireR", ref C.FirePositionIsLeft, sameLine: true);
                    }
                    break;

                case Strat.Relative:

                    ImGuiEx.TextV("Your Role:");
                    ImGui.SameLine();
                    ImGuiEx.RadioButtonBool("Tank/Melee", "Healer/Ranged", ref C.IsMelee, sameLine: true);

                    if(C.IsMelee)
                    {
                        ImGuiEx.TextV("Dark Baiting Position, Looking at your Clone:");
                        ImGui.SameLine();
                        //Intentionally swapped labels due to inverted = true.
                        ImGuiEx.RadioButtonBool("Right##DarkR", "Left##DarkL", ref C.DarkPositionIsLeft, sameLine: true, inverted: C.IsMelee);
                    }
                    else
                    {
                        ImGuiEx.TextV("Dark Baiting Position, Looking at your Clone:");
                        ImGui.SameLine();
                        ImGuiEx.RadioButtonBool("Left##DarkL", "Right##DarkR", ref C.DarkPositionIsLeft, sameLine: true, inverted: C.IsMelee);

                        ImGuiEx.TextV("Fire Baiting Position, Looking at your Clone:");
                        ImGui.SameLine();
                        //Intentionally swapped labels due to inverted = true.
                        ImGuiEx.RadioButtonBool("Right##FireR", "Left##FireL", ref C.FirePositionIsLeft, sameLine: true, inverted: true);
                    }
                    break;

                case Strat.DN:

                    ImGuiEx.TextV("Your Role:");
                    ImGui.SameLine();
                    ImGuiEx.EnumRadio(ref C.role, sameLine: true);
                    break;

                default:
                    break;

            }
        }
        ImGui.NewLine();
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Indent();
            ImGuiEx.Text($"Relative North Clone OID: {RN} / 0x{RN.ToString("X")}");
            ImGuiEx.Text($"Relative North : {relativeNorth.ToString()}");
            ImGuiEx.Text($"Phase : {Phase}");
            ImGuiEx.Text($"Has Dark Resistance Down?: {C.HasDarkResistanceDown}");
        }
        ;
    }

    public class Config : IEzConfig
    {
        public bool NoRainbow = false;
        public Vector4 FixedColor = EColor.RedBright;
        public bool SkipMechs = true;
        public Strat SelectedStrat = Strat.Static;
        public bool HasDarkResistanceDown = false;
        public bool IsMelee = true;
        public Role role = Role.Tank;
        public bool DarkPositionIsLeft = true;
        public bool FirePositionIsLeft = true;
    }
    Config C => Controller.GetConfig<Config>();

    public Vector4 GetRainbowColor(double cycleSeconds)
    {
        if(C.NoRainbow) return C.FixedColor;
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