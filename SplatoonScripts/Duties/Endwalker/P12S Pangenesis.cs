using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Components;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PluginLog = ECommons.Logging.PluginLog;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P12S_Pangenesis : SplatoonScript
    {
        public enum DebuffType { None, Short_2, Long_2, One }
        DebuffType MyDebuff = DebuffType.None;

        public override HashSet<uint> ValidTerritories => new() { 1154 };
        public override Metadata? Metadata => new(4, "tatad2");

        private string ElementNamePrefix = "P12SSC";
        private int towerCount = 0;

        private int whiteTowerCast = 33603;
        private int blackTowerCast = 33604;

        private int whiteDebuff = 3576;
        private int blackDebuff = 3577; 
        private int DNABuff = 3593;

        private Element? Indicator;
        private bool directionRight = false; // 0=>left, 1=>right
        private bool lastTowerBlack = false; // 0=>white, 1=>black

        string TestOverride = "";

        PlayerCharacter PC => TestOverride != "" && FakeParty.Get().FirstOrDefault(x => x.Name.ToString() == TestOverride) is PlayerCharacter pc ? pc : Svc.ClientState.LocalPlayer!;

        public override void OnEnable()
        {
            Reset();
        }

        private void Reset()
        {
            PluginLog.Information("pangenesis RESET");
            towerCount = 0;
            Indicator.Enabled = false;
            MyDebuff = DebuffType.None;
        }

        public override void OnSetup()
        {
            // {"Name":"","refX":100.0,"refY":95.0,"refActorDataID":16182,"refActorComparisonType":3,"tether":true}

            Element e = new Element(0);
            e.tether = true;
            e.Enabled = false;
            Controller.RegisterElement(ElementNamePrefix + "Indicator", e, true);
            Indicator = Controller.GetElementByName(ElementNamePrefix + "Indicator"); 
        }

        private void FirstTower()
        {
            BattleChara whiteTower = (BattleChara)Svc.Objects.First(x => x is BattleChara o && o.IsCasting == true && o.CastActionId == whiteTowerCast && o.CurrentCastTime < 1);
            BattleChara blackTower = (BattleChara)Svc.Objects.First(x => x is BattleChara o && o.IsCasting == true && o.CastActionId == blackTowerCast && o.CurrentCastTime < 1);

            PluginLog.Information($"tower casting time: {blackTower.CurrentCastTime}"); 

            Vector2 whitePos = whiteTower.Position.ToVector2(); 
            Vector2 blackPos = blackTower.Position.ToVector2(); 

            PluginLog.Information($"wtower: {whiteTower.ObjectId}, blacktower: {blackTower.ObjectId}, casttime: {whiteTower.CurrentCastTime}, {blackTower.CurrentCastTime}, position: {whiteTower.Position.ToVector2().ToString()}, {blackTower.Position.ToVector2().ToString()}");

            StatusList statusList = PC.StatusList;
            if (statusList.Any(x => x.StatusId == whiteDebuff && x.RemainingTime <= 8))
            {
                // short white, go black tower 
                Indicator.refX = blackPos.X;
                Indicator.refY = blackPos.Y;
                lastTowerBlack = true;
                MyDebuff = DebuffType.Short_2;
            }
            else if (statusList.Any(x => x.StatusId == whiteDebuff && x.RemainingTime > 8))
            {
                // long white, wait
                int biasX = blackPos.X < 100 ? 5 : -5;
                Indicator.refX = blackPos.X + biasX;
                Indicator.refY = blackPos.Y;
                lastTowerBlack = true;
                MyDebuff = DebuffType.Long_2;
            }
            else if (statusList.Any(x => x.StatusId == blackDebuff && x.RemainingTime <= 8))
            {
                // short black, go white tower 
                Indicator.refX = whitePos.X;
                Indicator.refY = whitePos.Y;
                lastTowerBlack = false;
                MyDebuff = DebuffType.Short_2;
            }
            else if (statusList.Any(x => x.StatusId == blackDebuff && x.RemainingTime > 8))
            {
                // long black, wait
                int biasX = whitePos.X < 100 ? 5 : -5;
                Indicator.refX = whitePos.X + biasX;
                Indicator.refY = whitePos.Y;
                lastTowerBlack = false;
                MyDebuff = DebuffType.Long_2;
            }
            else
            {
                if (C.Strat == Strat.First_2_1)
                {
                    if (statusList.Any(x => x.StatusId == DNABuff))
                    {
                        // 1 buff, go first tower
                        Indicator.refX = PC.Position.ToVector2().X < 100 ? 85 : 115;
                        Indicator.refY = 91;
                        lastTowerBlack = (Indicator.refX < 100) == (blackPos.X < 100);
                        MyDebuff = DebuffType.One;
                    }
                    else
                    {
                        // 0 buff, wait;
                        Indicator.refX = PC.Position.ToVector2().X < 100 ? 90 : 110;
                        Indicator.refY = 91;
                        lastTowerBlack = (Indicator.refX < 100) != (blackPos.X < 100);
                        MyDebuff = DebuffType.None;
                    }
                }
                else if(C.Strat == Strat.First_2_0)
                {
                    if (statusList.Any(x => x.StatusId == DNABuff))
                    {
                        // 1 buff, wait
                        Indicator.refX = PC.Position.ToVector2().X < 100 ? 90 : 110;
                        Indicator.refY = 91;
                        lastTowerBlack = (Indicator.refX < 100) == (blackPos.X < 100);
                        MyDebuff = DebuffType.One;
                    }
                    else
                    {
                        // 0 buff, go first tower;
                        Indicator.refX = PC.Position.ToVector2().X < 100 ? 85 : 115;
                        Indicator.refY = 91;
                        lastTowerBlack = (Indicator.refX < 100) != (blackPos.X < 100);
                        MyDebuff = DebuffType.None;
                    }
                }
            }

            if(C.Strat == Strat.First_2_0 && MyDebuff == DebuffType.Short_2)
            {
                lastTowerBlack = !lastTowerBlack;
            }

            directionRight = (int)Indicator.refX < 100 ? false : true;
            Indicator.Enabled = true;
            PluginLog.Information($"first tower, {Indicator.refX}, {Indicator.refY}, colorBlack?: {lastTowerBlack}"); 
        }

        private void SecondTower()
        {
            BattleChara whiteTower; 
            BattleChara blackTower; 
            if (directionRight)
            {
                // right
                whiteTower = (BattleChara)Svc.Objects.First(x => x is BattleChara o && o.IsCasting == true && o.CastActionId == whiteTowerCast && o.Position.ToVector2().X > 100 && o.CurrentCastTime < 1);
                blackTower = (BattleChara)Svc.Objects.First(x => x is BattleChara o && o.IsCasting == true && o.CastActionId == blackTowerCast && o.Position.ToVector2().X > 100 && o.CurrentCastTime < 1);
            }
            else
            {
                // left
                whiteTower = (BattleChara)Svc.Objects.First(x => x is BattleChara o && o.IsCasting == true && o.CastActionId == whiteTowerCast && o.Position.ToVector2().X < 100 && o.CurrentCastTime < 1);
                blackTower = (BattleChara)Svc.Objects.First(x => x is BattleChara o && o.IsCasting == true && o.CastActionId == blackTowerCast && o.Position.ToVector2().X < 100 && o.CurrentCastTime < 1);
            }

            Vector2 whitePos = whiteTower.Position.ToVector2();
            Vector2 blackPos = blackTower.Position.ToVector2();

            new TickScheduler(() =>
            {
                StatusList statusList = PC.StatusList;
                if (statusList.Any(x => x.StatusId == whiteDebuff))
                {
                    //  white, go black
                    Indicator.refX = blackPos.X;
                    Indicator.refY = blackPos.Y;
                    lastTowerBlack = true;
                }
                else if (statusList.Any(x => x.StatusId == blackDebuff))
                {
                    // black, go white
                    Indicator.refX = whitePos.X;
                    Indicator.refY = whitePos.Y;
                    lastTowerBlack = false;
                }
                else
                {
                    Indicator.refX = lastTowerBlack ? blackPos.X : whitePos.X;
                    Indicator.refY = lastTowerBlack ? blackPos.Y : whitePos.Y;
                }
                PluginLog.Information($"second/third tower, {Indicator.refX}, {Indicator.refY}");
            }, 1500); 
        }

        private void ThirdTower()
        {
            SecondTower();
            new TickScheduler(() =>
            {
                Indicator.Enabled = false;
            }, 6000); 
        }

        public override void OnMessage(string Message)
        {
            if (Message.Contains("(12383>33603)") || Message.Contains("(12383>33604)"))
            {
                // tower appear
                PluginLog.Information($"tower appear!");
                towerCount++;
                if (towerCount == 2)
                    FirstTower();
                if (towerCount == 6)
                    SecondTower();
                if (towerCount == 10)
                    ThirdTower(); 
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if (category == DirectorUpdateCategory.Commence || category == DirectorUpdateCategory.Recommence)
                Reset();
        }

        
        public override void OnSettingsDraw()
        {
            ImGui.SetNextItemWidth(200f);
            ImGuiEx.EnumCombo("Select strat", ref C.Strat);
            if (ImGui.CollapsingHeader("Debug"))
            {
                ImGuiEx.Text($"LastTowerBlack: {lastTowerBlack}");
                ImGuiEx.Text($"towerCount: {towerCount}");
                ImGuiEx.Text($"MyDebuff: {MyDebuff}");
                if (ImGui.Button("Reset")) Reset();
                ImGui.SetNextItemWidth(200f);
                ImGui.InputText("TestOverride", ref TestOverride, 50);
                ImGuiEx.Text($"{PC}");
            }
        }
        
        public enum Strat { First_2_1, First_2_0}

        Config C => Controller.GetConfig<Config>();
        public class Config : IEzConfig
        {
            public Strat Strat = Strat.First_2_1;
        }
    }
}
