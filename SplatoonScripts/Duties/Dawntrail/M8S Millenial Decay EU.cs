using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe sealed class M8S_Millenial_Decay_EU : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1263];
    public override Metadata Metadata => new(1, "NightmareXIV");

    bool? IsCW = null;

    public List<Vector2> CCWSet = [new(100, 88), new(93, 90), new(100,112), new(107,110)];
    public List<Vector2> CWSet = [new(100, 88), new(107, 90), new(100,112), new(93,110)];

    public bool PlayerDoesFirst = false;
    public bool CanDisplay = true;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("North", """{"Name":"","refX":100.0,"refY":90.0,"radius":2.0,"color":3360882432,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("South", """{"Name":"","refX":100.0,"refY":110.0,"radius":2.0,"color":3360882432,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("West", """{"Name":"","refX":90.0,"refY":100.0,"radius":2.0,"color":3360882432,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("East", """{"Name":"","refX":110.0,"refY":100.0,"radius":2.0,"color":3360882432,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("NorthEast", """{"Name":"","refX":107.0,"refY":93.0,"radius":2.0,"color":3360882432,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("NorthWest", """{"Name":"","refX":93.0,"refY":93.0,"radius":2.0,"color":3360882432,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("SouthEast", """{"Name":"","refX":107.0,"refY":107.0,"radius":2.0,"color":3360882432,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("SouthWest", """{"Name":"","refX":93.0,"refY":107.0,"radius":2.0,"color":3360882432,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
    }

    public override void OnUpdate()
    { 
        if(IsCW == null)
        {
            //Data ID: 18218
            //Casting: True, Action ID = 41908, Type = 1, Cast time: 2.8/7.7
            var d = Svc.Objects.OfType<IBattleNpc>().Where(x => x.DataId == 18218 && x.IsCasting(41908));
            //DuoLog.Information($"Count is {d.Count()}");
            if(d.Count() == 2)
            {
                if(d.All(x => CCWSet.Any(v => Vector2.Distance(x.Position.ToVector2(), v) < 1))) 
                {
                    DuoLog.Information("Determined: Counter Clockwise");
                    IsCW = false; 
                }
                if(d.All(x => CWSet.Any(v => Vector2.Distance(x.Position.ToVector2(), v) < 1)))
                {
                    DuoLog.Information("Determined: Clockwise");
                    IsCW = true;
                }
                EzThrottler.Throttle(this.InternalData.FullName + "1", 6000, true);
                EzThrottler.Throttle(this.InternalData.FullName + "2", 12000, true);
            }
        }
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(CanDisplay && IsCW != null && !EzThrottler.Check(this.InternalData.FullName + "2"))
        {
            if(PlayerDoesFirst)
            {
                if(!EzThrottler.Check(this.InternalData.FullName + "1"))
                {
                    Controller.GetElementByName($"{(IsCW.Value ? C.CWFirst : C.CCWFirst)}")!.Enabled = true;
                }
            } 
            else
            {
                if(!EzThrottler.Check(this.InternalData.FullName + "1"))
                {
                    Controller.GetElementByName($"{(IsCW.Value ? C.CWSecondIdle : C.CCWSecondIdle)}")!.Enabled = true;
                } 
                else
                {
                    Controller.GetElementByName($"{(IsCW.Value ? C.CWSecond : C.CCWSecond)}")!.Enabled = true;
                }
            }
        }
    }

    public override void OnReset()
    {
        IsCW = null;
        PlayerDoesFirst = false;
        CanDisplay = false;
    } 

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        //Casting: True, Action ID = 41911, Type = 1, Cast time: 2.9/4.7
        if(vfxPath == "vfx/lockon/eff/loc05sp_05a_se_p.avfx" && Svc.Objects.OfType<IBattleNpc>().Any(x => x.IsCasting(41911)))
        {
            if(Player.Object.EntityId == target)
            {
                DuoLog.Information("Player does first spread");
                PlayerDoesFirst = true;
            }
            CanDisplay = true;
        }
    }
    public enum Direction
    {
        North = 1,
        NorthEast = 2,
        East = 3,
        SouthEast = 4,
        South = 5,
        SouthWest = 6,
        West = 7,
        NorthWest = 8
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text("When AOE goes clockwise (True North):");
        ImGui.Indent();
        ImGui.SetNextItemWidth(150f.Scale());
        ImGuiEx.EnumCombo("Drop position if you have 1st aoe", ref C.CWFirst);
        ImGui.SetNextItemWidth(150f.Scale());
        ImGuiEx.EnumCombo("Initial position if you have 2nd aoe", ref C.CWSecondIdle);
        ImGui.SetNextItemWidth(150f.Scale());
        ImGuiEx.EnumCombo("Drop position if you have 2nd aoe", ref C.CWSecond);
        ImGui.Unindent();
        ImGuiEx.Text("When AOE goes counter-clockwise (True North):");
        ImGui.PushID("2");
        ImGui.Indent();
        ImGui.SetNextItemWidth(150f.Scale());
        ImGuiEx.EnumCombo("Drop position if you have 1st aoe", ref C.CCWFirst);
        ImGui.SetNextItemWidth(150f.Scale());
        ImGuiEx.EnumCombo("Initial position if you have 2nd aoe", ref C.CCWSecondIdle);
        ImGui.SetNextItemWidth(150f.Scale());
        ImGuiEx.EnumCombo("Drop position if you have 2nd aoe", ref C.CCWSecond);
        ImGui.Unindent();
        ImGui.PopID(); 
    }

    Config C => Controller.GetConfig<Config>();

    public class Config : IEzConfig
    {
        public Direction CWFirst = Direction.NorthEast;
        public Direction CWSecond = Direction.East;
        public Direction CWSecondIdle = Direction.West;
        public Direction CCWFirst = Direction.NorthWest;
        public Direction CCWSecond = Direction.SouthWest;
        public Direction CCWSecondIdle = Direction.West;
    }
}