using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Config;
using Dalamud.Utility;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets2;
using Microsoft.VisualBasic.ApplicationServices;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Windows.Forms.VisualStyles;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class DSR_Towers : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 968 };
        public override Metadata? Metadata => new(1, "Enthusiastus");

        Element? SolutionElement;

        List<Element> TowerElements = new();
        List<Element> NorthTowers = new();
        List<Element> EastTowers = new();
        List<Element> SouthTowers = new();
        List<Element> WestTowers = new();

        bool takeMeteorTower=false;

        string solutionText = "";
 
        //BattleNpc? Thordan => Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.DataId == ThordanDataId) as BattleNpc;
        string TestOverride = "";

        PlayerCharacter PC => TestOverride != "" && FakeParty.Get().FirstOrDefault(x => x.Name.ToString() == TestOverride) is PlayerCharacter pc ? pc : Svc.ClientState.LocalPlayer!;
        Vector2 Center = new(100, 100);

        public override void OnSetup()
        {
            var solution = "{\"Name\":\"solution\",\"type\":1,\"radius\":0.0,\"overlayBGColor\":4278190335,\"overlayVOffset\":3.0,\"overlayFScale\":3.0,\"thicc\":0.0,\"overlayText\":\"no solution found\",\"refActorType\":1}";
            SolutionElement = Controller.RegisterElementFromCode($"solution", solution);
            SolutionElement.offZ = Conf.offZ;
            SolutionElement.overlayFScale = Conf.tScale;
            SolutionElement.Enabled = false;
            var tower = "{\"Name\":\"tower\",\"refX\":100,\"refY\":100,\"radius\":3.0,\"color\":3355508735,\"thicc\":5.0,\"Filled\":true}";
            for (var i = 0; i < 8; i++)
            {
                var e = Controller.RegisterElementFromCode($"tower{i}", tower);
                e.color = Conf.ColNoMeteorTower.ToUint();
                e.Enabled = false;
                TowerElements.Add(e);
            }
        }

        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
        }
        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            if (vfxPath == "vfx/lockon/eff/r1fz_holymeteo_s12x.avfx")
            {

                if (target.TryGetObject(out var pv) && pv is PlayerCharacter pvc)
                {
                    if(pvc.GetRole().ToString() == "Tank" || pvc.GetRole().ToString() == "Healer")
                    {
                        solutionText = $"Supp Met,";
                    } else
                    {
                        solutionText = $"{pvc.GetRole().ToString()} Met,";
                    }
                    if(pvc == PC)
                    {
                        takeMeteorTower = true;
                    }
                }
            }
           
        }

        public override void OnMessage(string Message)
        {
            if (Message.Contains("(3640>29563)"))
            {
                var towers = Svc.Objects.Where(x => x is BattleNpc b && b.NameId == 3640 && b.DataId == 9020).OrderBy(x => x.Position.X).ThenBy(y => y.Position.Z);
                int i = 0;
                foreach (var x in towers)
                {
                    var cur = i;
                    i++;
                    TowerElements[cur].SetRefPosition(x.Position);
                    TowerElements[cur].Enabled = true;
                    if (x.Position.Z < 107 && x.Position.Z > 93 && x.Position.X > 93 && x.Position.X < 107)
                    {
                        //DuoLog.Information($"Skip inner Tower @{x.Position}");
                        TowerElements[cur].color = Conf.ColInnerTower.ToUint();
                        continue;
                    }
                    //DuoLog.Information($"Found Tower #{cur} {x.Name}({x.ObjectId}) @{x.Position}");
                    TowerElements[cur].color = Conf.ColNoMeteorTower.ToUint();
                    //TowerElements[cur].overlayText = $"{x.Position} || {Math.Round(2 - 2 * Math.Atan2(x.Position.X-100, x.Position.Z-100) / Math.PI) % 4}";
                    // Coordinate -Center because Center is @ 100/100 and formula needs it to be at 0
                    var quadrant = Math.Round(2 - 2 * Math.Atan2(x.Position.X - Center.X, x.Position.Z - Center.Y) / Math.PI) % 4;
                    switch(quadrant)
                    {
                        case 0:
                            NorthTowers.Add(TowerElements[cur]);
                            break;
                        case 1:
                            EastTowers.Add(TowerElements[cur]);
                            break;
                        case 2:
                            SouthTowers.Add(TowerElements[cur]);
                            break;
                        case 3:
                            WestTowers.Add(TowerElements[cur]);
                            break;
                        default:
                            DuoLog.Information($"Wait a tower is in no circle section? How did you do this? Pos: {x.Position}");
                            break;
                    }
                }
                //DuoLog.Information($"Towers: {towers.Count()} | North: {NorthTowers.Count()} | East: {EastTowers.Count()} | South: {SouthTowers.Count()} | West: {WestTowers.Count()}");
                findTowerStrategy();
            }
            if(Message.Contains("Ser Noudenet casts Holy Comet")) {
                SolutionElement.Enabled = false;
                foreach (var e in TowerElements)
                {
                    e.Enabled = false;
                }
            }
        }

        private void findTowerStrategy()
        {
            /*
            DuoLog.Information($"North:");
            foreach (var t in NorthTowers)
            {
                DuoLog.Information($"{t.refX} / {t.refY}");
            }
            DuoLog.Information($"South:");
            foreach (var t in SouthTowers)
            {
                DuoLog.Information($"{t.refX} / {t.refY}");
            }
            */

            Element? nt = null;
            Element? st = null;

            // Try straight towers first
            if(NorthTowers.Any(n => n.refX > 99 && n.refX < 101) && SouthTowers.Any(n => n.refX > 99 && n.refX < 101)) {
                solutionText += " Both straight, perfect";
                nt = NorthTowers.FirstOrDefault(n => n.refX > 99 && n.refX < 101);
                st = SouthTowers.FirstOrDefault(n => n.refX > 99 && n.refX < 101);

            } else if (NorthTowers.Any(n => n.refX < 99) && SouthTowers.Any(n => n.refX > 101)) {
                solutionText += " Both CCW, perfect";
                nt = NorthTowers.FirstOrDefault(n => n.refX < 99);
                st = SouthTowers.FirstOrDefault(n => n.refX > 101);

            } else if (NorthTowers.Any(n => n.refX > 101) && SouthTowers.Any(n => n.refX < 99)) {
                solutionText += " Both CW, perfect";
                nt = NorthTowers.FirstOrDefault(n => n.refX > 101);
                st = SouthTowers.FirstOrDefault(n => n.refX < 99);

            // no perfect solution, prefer north short
            } else if (NorthTowers.Any(n => n.refX > 101) && SouthTowers.Any(n => n.refX > 99 && n.refX < 101)) {
                solutionText += " North CW, South straight, North short";
                nt = NorthTowers.FirstOrDefault(n => n.refX > 101);
                st = SouthTowers.FirstOrDefault(n => n.refX > 99 && n.refX < 101);

            } else if(NorthTowers.Any(n => n.refX > 99 && n.refX < 101) && SouthTowers.Any(n => n.refX > 101)) {
                solutionText += " North straight, South CCW, North short";
                nt = NorthTowers.FirstOrDefault(n => n.refX > 99 && n.refX < 101);
                st = SouthTowers.FirstOrDefault(n => n.refX > 101);

            } else if(SouthTowers.Any(n => n.refX < 99) && NorthTowers.Any(n => n.refX > 99 && n.refX < 101)) {
                solutionText += " North straight, South CW, South short";
                st = SouthTowers.FirstOrDefault(n => n.refX < 99);
                nt = NorthTowers.FirstOrDefault(n => n.refX > 99 && n.refX < 101);

            } else if(SouthTowers.Any(n => n.refX > 99 && n.refX < 101) && NorthTowers.Any(n => n.refX < 99)) {
                solutionText += " North CCW, South straight, South short";
                st = SouthTowers.FirstOrDefault(n => n.refX > 99 && n.refX < 101);
                nt = NorthTowers.FirstOrDefault(n => n.refX < 99);

            // no short solution either... switch to e/w?
            } else if (NorthTowers.Any(n => n.refX > 101) && SouthTowers.Any(n => n.refX > 101)) {
                solutionText += " North CW, South CCW, North dbl short";
                nt = NorthTowers.FirstOrDefault(n => n.refX > 101);
                st = SouthTowers.FirstOrDefault(n => n.refX > 101);

            } else if(SouthTowers.Any(n => n.refX < 99) && NorthTowers.Any(n => n.refX < 99)) {
                solutionText += " North CCW, South CCW, South dbl short";
                st = SouthTowers.FirstOrDefault(n => n.refX < 99);
                nt = NorthTowers.FirstOrDefault(n => n.refX < 99);
            }
            if(takeMeteorTower)
            {
                if (nt != null)
                {
                    nt.color = Conf.ColMeteorTower.ToUint();
                    nt.Enabled = true;
                }
                if (st != null)
                {
                    st.color = Conf.ColMeteorTower.ToUint();
                    st.Enabled = true;
                }
            } else
            {
                if (nt != null)
                {
                    nt.color = Conf.ColDontMeteorTower.ToUint();
                    nt.Enabled = true;
                }
                if (st != null)
                {
                    st.color = Conf.ColDontMeteorTower.ToUint();
                    st.Enabled = true;
                }
            }
            
            SolutionElement.overlayText = solutionText;
            solutionText = "";
            if(Conf.raidCaller)
                SolutionElement.Enabled = true;
        }

        private void ActionEffect_ActionEffectEvent(ActionEffectSet set)
        {
        }

        public override void OnDisable()
        {
            ActionEffect.ActionEffectEvent -= ActionEffect_ActionEffectEvent;
        }

        void Hide()
        {
        }

        void Off()
        {
            SolutionElement.Enabled = false;
            foreach(var e in TowerElements)
            {
                e.Enabled = false;
            }
            NorthTowers.Clear();
            EastTowers.Clear();
            SouthTowers.Clear();
            WestTowers.Clear();
            SouthTowers.Clear();
        }

        public override void OnUpdate()
        {

        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
            {
                Off();
            }
        }

        Config Conf => Controller.GetConfig<Config>();
        public class Config : IEzConfig
        {
            public bool raidCaller = false;
            public Vector4 ColNoMeteorTower = Vector4FromRGBA(0xFFFF0064);
            public Vector4 ColMeteorTower = Vector4FromRGBA(0x00FF0064);
            public Vector4 ColDontMeteorTower = Vector4FromRGBA(0xFF000064);
            public Vector4 ColInnerTower = Vector4FromRGBA(0x0000FF64);
            public float offZ = 0.0f;
            public float tScale = 3.5f;
        }

        public override void OnSettingsDraw()
        {
            ImGui.ColorEdit4("Non Meteor Tower", ref Conf.ColNoMeteorTower, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Meteor Tower", ref Conf.ColMeteorTower, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Do not take Meteor Tower", ref Conf.ColDontMeteorTower, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Inner Tower", ref Conf.ColInnerTower, ImGuiColorEditFlags.NoInputs);
            ImGui.Separator();
            ImGui.Checkbox($"Make Raid calls?", ref Conf.raidCaller);
            ImGui.Separator();
            ImGui.SetNextItemWidth(150);
            ImGui.DragFloat("Solution vertical offset", ref Conf.offZ.ValidateRange(-15f, 15f), 0.1f);
            ImGui.SetNextItemWidth(150);
            ImGui.DragFloat("Solution text scale", ref Conf.tScale.ValidateRange(0.1f, 10f), 0.1f);
        }

        public unsafe static Vector4 Vector4FromRGBA(uint col)
        {
            byte* bytes = (byte*)&col;
            return new Vector4((float)bytes[3] / 255f, (float)bytes[2] / 255f, (float)bytes[1] / 255f, (float)bytes[0] / 255f);
        }
    }
}
