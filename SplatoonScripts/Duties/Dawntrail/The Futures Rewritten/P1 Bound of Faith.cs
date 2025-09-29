using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;
public sealed class P1_Bound_of_Faith : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [Raids.Futures_Rewritten_Ultimate];

    uint Debuff = 4165;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("NorthEast", """
            {"Name":"east non adj","refX":108.0,"refY":95.5,"radius":3.0,"color":3356425984,"Filled":false,"fillIntensity":0.5,"tether":true}
            """);
        Controller.RegisterElementFromCode("NorthWest", """
            {"Name":"west non adj","refX":92.0,"refY":95.5,"radius":3.0,"color":3356425984,"Filled":false,"fillIntensity":0.5,"tether":true}
            """);
        Controller.RegisterElementFromCode("SouthEast", """
            {"Name":"east adj","refX":108.0,"refY":104.5,"radius":3.0,"Filled":false,"fillIntensity":0.5,"tether":true}
            """);
        Controller.RegisterElementFromCode("SouthWest", """
            {"Name":"west adj","refX":92.0,"refY":104.5,"radius":3.0,"Filled":false,"fillIntensity":0.5,"tether":true}
            """);
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        var players = (C.Override.All(x => x.Length > 0)?Svc.Objects.OfType<IPlayerCharacter>().Where(x => x.GetNameWithWorld().EqualsAny(C.Override)):Svc.Objects.OfType<IPlayerCharacter>().Where(x => x.StatusList.Any(s => s.StatusId == Debuff && s.RemainingTime < 12))).ToArray();
        if(players.Length == 2)
        {
            var meAdjusting = false;
            var n1 = C.List.GetPlayers(x => true)!;
            var indexes = players.Select(x => n1.IndexOf(p => p.IGameObject.AddressEquals(x))).Order().ToArray(); //marked
            var myIndex = n1.IndexOf(x => C.BasePlayer == ""?x.IGameObject.AddressEquals(Player.Object):x.NameWithWorld == C.BasePlayer);
            //PluginLog.Debug($"List: {n1.Select(x => x.NameWithWorld).Print()}");
            //PluginLog.Debug($"Candidates: {indexes.Print()}, my={myIndex}");
            //PluginLog.Debug($"Candidates: {n1[indexes[0]].NameWithWorld}, {n1[indexes[1]].NameWithWorld}");
            var dir = Player.Position.X > 100 ? "East" : "West";
            if(indexes[0] < 4 == indexes[1] < 4) //potential adjust situation
            {
                if(myIndex == indexes[0])
                {
                    meAdjusting = true;
                    //PluginLog.Debug("I adjust");
                    Controller.GetElementByName((myIndex >= 4 ? "North" : "South") + dir).Enabled = true;
                }
                else if(myIndex.EqualsAny(indexes[0] - 4, indexes[0] + 4))
                {
                    //own counterparty adjusts
                    meAdjusting = true;
                    //PluginLog.Debug("I adjust (2)");
                    Controller.GetElementByName((myIndex >= 4 ? "North" : "South") + dir).Enabled = true;
                }
                else
                {
                    //PluginLog.Debug("No adju");
                    Controller.GetElementByName((myIndex < 4 ? "North" : "South") + dir).Enabled = true;
                }
            }
            else
            {
                Controller.GetElementByName((myIndex < 4 ? "North" : "South") + dir).Enabled = true;
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.TextWrapped($"""Order players so that North group is first 4, South group is second 4. Within each group of 4, order players in a way that 1st and 5th people are ones that adjust earliest, 4th and 8th ones that never adjust. For example, for classic G1/G2 spread and TMRH prio, you have to set T1 M1 R1 H1 T2 M2 R2 H2""");
        C.List.Draw();

        if(ImGuiEx.CollapsingHeader("Artificially override debuffed players (testing)"))
        {
            ImGui.InputText($"Player1", ref C.Override[0]);
            ImGui.InputText($"Player2", ref C.Override[1]);
            ImGui.InputText($"Me", ref C.BasePlayer);
        }
    }

    Config C => Controller.GetConfig<Config>();
    public class Config: IEzConfig
    {
        public string[] Override = ["", ""];
        public string BasePlayer = "";
        public PriorityData List = new();
    }
}