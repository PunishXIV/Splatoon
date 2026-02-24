using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers.LegacyPlayer;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TerraFX.Interop.Windows;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M11S_Fixed_Stampede : SplatoonScript
{
    public override Metadata Metadata { get; } = new(3, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1325];

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Tower1", """{"Name":"Ranged Dps tower highlight - 1st cw","type":1,"radius":1.0,"Donut":3.0,"color":3356425984,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":14305,"refActorRequireCast":true,"refActorCastId":[46167],"refActorComparisonType":6,"tether":true,"Enumeration":1,"EnumerationOrder":[1],"EnumerationCenter":{"X":100.0,"Y":100.0},"EnumerationStart":{"X":100.0,"Y":90.0}}""");
        Controller.RegisterElementFromCode("Tower2", """{"Name":"Ranged Dps tower highlight - 1st ccw","type":1,"radius":1.0,"Donut":3.0,"color":3356425984,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":14305,"refActorRequireCast":true,"refActorCastId":[46167],"refActorComparisonType":6,"tether":true,"Enumeration":2,"EnumerationOrder":[1],"EnumerationCenter":{"X":100.0,"Y":100.0},"EnumerationStart":{"X":100.0,"Y":90.0}}""");
        Controller.RegisterElementFromCode("Melee1", """{"Name":"Melee Dps tower highlight - 1st ccw","type":1,"radius":2.5,"fillIntensity":0.833,"thicc":4.0,"refActorNPCNameID":14305,"refActorRequireCast":true,"refActorCastId":[46167],"refActorComparisonType":6,"Enumeration":1,"EnumerationOrder":[1],"EnumerationCenter":{"X":100.0,"Y":100.0},"EnumerationStart":{"X":100.0,"Y":90.0}}""");
        Controller.RegisterElementFromCode("Melee1h", """{"Name":"Melee Dps tower highlight - 1st ccw","type":1,"radius":2.5,"Donut":1.5,"color":3356425984,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":14305,"refActorRequireCast":true,"refActorCastId":[46167],"refActorComparisonType":6,"tether":true,"Enumeration":1,"EnumerationOrder":[1],"EnumerationCenter":{"X":100.0,"Y":100.0},"EnumerationStart":{"X":100.0,"Y":90.0}}""");
        Controller.RegisterElementFromCode("Melee2", """{"Name":"Melee Dps tower highlight - 1st ccw","type":1,"radius":2.5,"fillIntensity":0.833,"thicc":4.0,"refActorNPCNameID":14305,"refActorRequireCast":true,"refActorCastId":[46167],"refActorComparisonType":6,"Enumeration":2,"EnumerationOrder":[1],"EnumerationCenter":{"X":100.0,"Y":100.0},"EnumerationStart":{"X":100.0,"Y":90.0}}""");
        Controller.RegisterElementFromCode("Melee2h", """{"Name":"Melee Dps tower highlight - 1st ccw","type":1,"radius":2.5,"Donut":1.5,"color":3356425984,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":14305,"refActorRequireCast":true,"refActorCastId":[46167],"refActorComparisonType":6,"tether":true,"Enumeration":2,"EnumerationOrder":[1],"EnumerationCenter":{"X":100.0,"Y":100.0},"EnumerationStart":{"X":100.0,"Y":90.0}}""");
        Controller.RegisterElementFromCode("Tank1", """{"Name":"Tank tower highlight - 1st cw","type":1,"radius":3.0,"Donut":1.0,"color":3369795328,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":14305,"refActorRequireCast":true,"refActorCastId":[46166],"refActorComparisonType":6,"tether":true,"Enumeration":1,"EnumerationOrder":[1],"EnumerationCenter":{"X":100.0,"Y":100.0},"EnumerationStart":{"X":100.0,"Y":90.0}}""");
        Controller.RegisterElementFromCode("Tank2", """{"Name":"Tank tower highlight - 1st ccw","type":1,"radius":3.0,"Donut":1.0,"color":3369795328,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":14305,"refActorRequireCast":true,"refActorCastId":[46166],"refActorComparisonType":6,"tether":true,"Enumeration":2,"EnumerationOrder":[1],"EnumerationCenter":{"X":100.0,"Y":100.0},"EnumerationStart":{"X":100.0,"Y":90.0}}""");

        Controller.RegisterElementFromCode("4NW", """{"Name":"","refX":97.5,"refY":97.5,"radius":1.0,"Donut":0.5,"color":3355639552,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("4NE", """{"Name":"","refX":102.5,"refY":97.5,"radius":1.0,"Donut":0.5,"color":3355639552,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("4SE", """{"Name":"","refX":102.5,"refY":102.5,"radius":1.0,"Donut":0.5,"color":3355639552,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("4SW", """{"Name":"","refX":97.5,"refY":102.5,"radius":1.0,"Donut":0.5,"color":3355639552,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");

        Controller.RegisterElementFromCode("4NWo", """{"Name":"","refX":95,"refY":95,"radius":1.0,"Donut":0.5,"color":3355639552,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("4NEo", """{"Name":"","refX":105,"refY":95,"radius":1.0,"Donut":0.5,"color":3355639552,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("4SEo", """{"Name":"","refX":105,"refY":105,"radius":1.0,"Donut":0.5,"color":3355639552,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("4SWo", """{"Name":"","refX":95,"refY":105,"radius":1.0,"Donut":0.5,"color":3355639552,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");

        Controller.RegisterElementFromCode($"2{TwoWayDirection.W_Inside}", """{"Name":"","refX":96.0,"refY":100.0,"radius":1.0,"Donut":0.5,"color":3355639552,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode($"2{TwoWayDirection.E_Inside}", """{"Name":"","refX":104.0,"refY":100.0,"radius":1.0,"Donut":0.5,"color":3355639552,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode($"2{TwoWayDirection.E_Outside}", """{"Name":"","refX":108.0,"refY":100.0,"radius":1.0,"Donut":0.5,"color":3355639552,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode($"2{TwoWayDirection.W_Outside}", """{"Name":"","refX":92.0,"refY":100.0,"radius":1.0,"Donut":0.5,"color":3355639552,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
    }

    bool HaveTether = false;
    Vector3 TetherPos;

    public override void OnSettingsDraw()
    {
        ImGuiEx.TextWrapped($"This script supports HHRR or RRHH priority. It does not supports HRHR or RHRH. This is done to avoid having to setup priority list. Should it be needed, please contact NightmareXIV via discord.\nAlso this script assumes tower enumeration starts North clockwise, however, that can be changed via Registered Elements tab.");
        ImGui.Separator();
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("Priority type", ref C.PrioType);
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("My role", ref C.MyRole);

        ImGuiEx.Text(EColor.YellowBright, "Setup 4-way bait directions for meteor baiters:");
        ImGui.Indent();
        for(int i = 0; i < C.BaitFourWay.Count; i++)
        {
            FourWayDirection x = C.BaitFourWay[i];
            ImGui.SetNextItemWidth(150f);
            if(ImGuiEx.EnumCombo($"{(CardinalDirection)i}", ref x)) C.BaitFourWay[i] = x;
        }
        ImGui.Unindent();

        ImGuiEx.Text(EColor.YellowBright, "Setup 2-way bait directions for meteor baiters:");
        ImGui.Indent();
        for(int i = 0; i < C.BaitTwoWay.Count; i++)
        {
            TwoWayDirection x = C.BaitTwoWay[i];
            ImGui.SetNextItemWidth(150f);
            if(ImGuiEx.EnumCombo($"{(CardinalDirection)i}", ref x)) C.BaitTwoWay[i] = x;
        }
        ImGui.Unindent();

        if(ImGui.CollapsingHeader("Debug"))
        {
            foreach(var x in Controller.GetPartyMembers())
            {
                ImGuiEx.CollectionCheckbox(x.GetNameWithWorld(), x.ObjectId, this.Baiters);
            }
            ImGuiEx.Text($"Tethers: {BasePlayer.GetTethers().Print()}");
        }
    }

    List<uint> Baiters = [];
    Dictionary<uint, CardinalDirection> BaiterDirections = [];

    public override void OnReset()
    {
        BaiterDirections.Clear();
        Baiters.Clear();
        HaveTether = false;
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(vfxPath == "vfx/lockon/eff/m0017trg_a0c.avfx")
        {
            Baiters.Add(target);
        }
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        if(Baiters.Count == 2 && !Baiters.Contains(BasePlayer.ObjectId))
        {
            if(C.MyRole.EqualsAny(MyRole.H1, MyRole.H2, MyRole.R1, MyRole.R2))
            {
                if(Controller.TryGetElementByName($"Tower{GetDpsTowerNumber()}", out var e))
                {
                    e.Enabled = true;
                }
            }
            if(C.MyRole == MyRole.Melee_CW)
            {
                if(Controller.TryGetElementByName("Melee1", out var e) && Controller.TryGetElementByName("Melee1h", out var e2))
                {
                    e.Enabled = true;
                    e2.Enabled = true;
                }
            }
            if(C.MyRole == MyRole.Melee_CCW)
            {
                if(Controller.TryGetElementByName("Melee2", out var e) && Controller.TryGetElementByName("Melee2h", out var e2))
                {
                    e.Enabled = true;
                    e2.Enabled = true;
                }
            }
            if(C.MyRole.EqualsAny(MyRole.Tank_CW, MyRole.Tank_CCW))
            {
                if(Controller.TryGetElementByName($"Tank{GetTankTowerNumber()}", out var e))
                {
                    e.Enabled = true;
                }
            }
        }
        foreach(var x in Svc.Objects)
        {
            if(x is IBattleNpc n)
            {
                if(n.IsCasting(46170))
                {
                    //4-way
                    if(this.BaiterDirections.TryGetValue(BasePlayer.ObjectId, out var d))
                    {
                        var dir = C.BaitFourWay.SafeSelect((int)d);
                        if(Controller.TryGetElementByName($"4{dir}", out var e))
                        {
                            e.Enabled = true;
                        }
                    }
                    if(BasePlayer.GetTethers().Where(x => x.Pair is ICharacter c && c.NameId == 14305).Any())
                    {
                        HaveTether = true;
                        TetherPos = BasePlayer.Position;
                    }
                    else if(HaveTether)
                    {
                        string direction;
                        if(TetherPos.Z < 100)
                        {
                            direction = TetherPos.X < 100 ? "NW" : "NE";
                        }
                        else
                        {
                            direction = TetherPos.X < 100 ? "SW" : "SE";
                        }
                        if(Controller.TryGetElementByName($"4{direction}o", out var e))
                        {
                            e.Enabled = true;
                        }
                    }
                }
                if(n.IsCasting(47037))
                {
                    //2-way
                    if(this.BaiterDirections.TryGetValue(BasePlayer.ObjectId, out var d))
                    {
                        var dir = C.BaitTwoWay.SafeSelect((int)d);
                        if(Controller.TryGetElementByName($"2{dir}", out var e))
                        {
                            e.Enabled = true;
                        }
                    }
                    if(BasePlayer.GetTethers().Where(x => x.Pair is ICharacter c && c.NameId == 14305).Any())
                    {
                        HaveTether = true;
                        TetherPos = BasePlayer.Position;
                    }
                    else if(HaveTether && Controller.TryGetElementByName($"2{(TetherPos.X < 100 ? "W" : "E")}_Outside", out var e))
                    {
                        e.Enabled = true;
                    }
                }
            }
        }
    }

    int GetTankTowerNumber()
    {
        if(C.MyRole == MyRole.Tank_CW) return 1;
        if(C.MyRole == MyRole.Tank_CCW) return 2;
        return default;
    }

    int GetDpsTowerNumber()
    {
        if(C.MyRole == MyRole.Melee_CW) return 1;
        if(C.MyRole == MyRole.Melee_CCW) return 2;
        if(C.PrioType == PrioType.H1_H2_R1_R2)
        {
            if(C.MyRole == MyRole.H1) return 1;
            if(C.MyRole == MyRole.R2) return 2;
            if(C.MyRole == MyRole.H2)
            {
                if(Baiters.All(x => x.TryGetPlayer(out var pc) && pc.GetJob().IsDps()))
                {
                    //all baiters are dps, splitting tower with other healer
                    return 2;
                }
                else
                {
                    //baiters are one dps and one healer, taking first tower
                    return 1;
                }
            }
            if(C.MyRole == MyRole.R1)
            {
                if(Baiters.All(x => x.TryGetPlayer(out var pc) && !pc.GetJob().IsDps()))
                {
                    //all baiters are non-dps, splitting tower with other dps
                    return 1;
                }
                else
                {
                    //baiters are one dps and one healer, taking 2nd tower
                    return 2;
                }
            }
        }
        if(C.PrioType == PrioType.R1_R2_H1_H2)
        {
            if(C.MyRole == MyRole.R1) return 1;
            if(C.MyRole == MyRole.H2) return 2;
            if(C.MyRole == MyRole.H1)
            {
                if(Baiters.All(x => x.TryGetPlayer(out var pc) && pc.GetJob().IsDps()))
                {
                    //all baiters are dps, splitting tower with other healer
                    return 1;
                }
                else
                {
                    //baiters are one dps and one healer, taking 2nd tower
                    return 2;
                }
            }
            if(C.MyRole == MyRole.R2)
            {
                if(Baiters.All(x => x.TryGetPlayer(out var pc) && !pc.GetJob().IsDps()))
                {
                    //all baiters are non-dps, splitting tower with other dps
                    return 2;
                }
                else
                {
                    //baiters are one dps and one healer, taking 1st tower
                    return 1;
                }
            }
        }
        return default;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action?.RowId == 46164 && set.Target is IPlayerCharacter pc)
        {
            var targetDirection = MathHelper.GetCardinalDirection(new(100,0,100), pc.Position);
            if(!this.BaiterDirections.ContainsKey(pc.ObjectId))
            {
                this.BaiterDirections[pc.ObjectId] = targetDirection;
                PluginLog.Information($"Determined direction for {pc.GetNameWithWorld()}: {targetDirection}");
            }
        }
    }

    public enum PrioType { H1_H2_R1_R2, R1_R2_H1_H2 }
    public enum MyRole { Disabled, H1, H2, R1, R2, Melee_CW, Melee_CCW, Tank_CW, Tank_CCW }

    public enum FourWayDirection { NE, SE, SW, NW }
    public enum TwoWayDirection { W_Inside, W_Outside, E_Inside, E_Outside }

    public Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public bool ReadTos = false;
        public PrioType PrioType = PrioType.H1_H2_R1_R2;
        public MyRole MyRole = MyRole.Disabled;
        public List<FourWayDirection> BaitFourWay = [FourWayDirection.NW, FourWayDirection.NE, FourWayDirection.NW, FourWayDirection.NE];
        public List<TwoWayDirection> BaitTwoWay = [TwoWayDirection.W_Outside, TwoWayDirection.E_Outside, TwoWayDirection.W_Outside, TwoWayDirection.E_Outside];
    }
}
