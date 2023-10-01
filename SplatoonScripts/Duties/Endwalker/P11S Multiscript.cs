using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Reflection;
using ECommons.Schedulers;
using ImGuiNET;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P11S_Multiscript : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1152 };
        public override Metadata? Metadata => new(3, "NightmareXIV");

        const string DarkVFX = "vfx/common/eff/m0830_dark_castloopc0k1.avfx";
        const string LightVFX = "vfx/common/eff/m0830_light_castloopc0k1.avfx";
        enum Color { Unknown, Light, Dark };
        BattleNpc? Themis => Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.DataId == 16114 && b.IsTargetable()) as BattleNpc;
        IEnumerable<BattleNpc> IllusoryThemises => Svc.Objects.Where(x => x is BattleNpc b && b.DataId == 16115).Cast<BattleNpc>();
        TickScheduler? DonutScheduler;
        TickScheduler? TowerScheduler;

        Dictionary<TowerDirection, Vector2> Towers = new()
        {
            { (TowerDirection)7,  new(108.152f, 91.834f) },
            { (TowerDirection)8, new(108.085f, 108.149f) },
            { (TowerDirection)9, new(91.889f, 108.137f) },
            { (TowerDirection)10, new(91.871f, 91.960f) },
            {(TowerDirection)15, new(100f, 92f) },
            {(TowerDirection)16, new(108f, 100f) },
            {(TowerDirection)17, new(100f, 108f) },
            {(TowerDirection)18, new(92f, 100f) }
        };

        public enum TowerDirection : uint
        {
            North = 15,
            West = 18,
            East = 16,
            South = 17,
            NorthWest = 10,
            NorthEast = 7,
            SouthWest = 9,
            SouthEast = 8,
        }

        TowerDirection[] Clock = new TowerDirection[] { TowerDirection.North, TowerDirection.NorthEast, TowerDirection.East, TowerDirection.SouthEast, TowerDirection.South, TowerDirection.SouthWest, TowerDirection.West, TowerDirection.NorthWest };

        TowerDirection GetNextSpot(TowerDirection md, MoveDirection d)
        {
            if(d == MoveDirection.Fixed_position)
            {
                return C.SafeSpotFixedPos;
            }
            if(d == MoveDirection.Unused_Tower_Position)
            {
                if (md == C.Tower1) return C.Tower2;
                if (md == C.Tower2) return C.Tower1;
            }
            var index = Array.IndexOf(Clock, md);
            if (d == MoveDirection.Clockwise) index++;
            if (d == MoveDirection.CounterClockwise) index--;
            if(index < 0) index = Clock.Length - 1;
            if (index >= Clock.Length) index = 0;
            return Clock[index];
        }

        public enum MoveDirection { Disable, Clockwise, CounterClockwise, Unused_Tower_Position, Fixed_position}

        public override void OnSetup()
        {
            for(var i = 0; i < 8; i++)
            {
                Controller.RegisterElementFromCode($"PairDonut{i}", "{\"Name\":\"\",\"Enabled\":false,\"refX\":93.386154,\"refY\":89.96649,\"radius\":2.0,\"Donut\":7.0,\"color\":0x70BD00CE,\"thicc\":3.0,\"refActorPlaceholder\":[],\"FillStep\":0.25,\"refActorComparisonType\":5}");
                Controller.RegisterElementFromCode($"LingerAOE{i}", "{\"Name\":\"\",\"Enabled\":false,\"refX\":89.57288,\"refY\":89.32873,\"refZ\":-9.536743E-07,\"radius\":5.0,\"color\":1358954495,\"Filled\":true}");
            }
            Controller.RegisterElementFromCode("Tower", "{\"Name\":\"\",\"Enabled\":false,\"refX\":107.98778,\"refY\":100.025696,\"radius\":1.0,\"Donut\":3.0,\"color\":4278255401,\"thicc\":5.0,\"FillStep\":1.0,\"tether\":true}");
            Controller.RegisterElementFromCode($"Spot{TowerDirection.North}", "{\"Name\":\"\",\"refX\":100.0,\"refY\":83.0,\"radius\":2.0,\"color\":4294963968,\"thicc\":5.0,\"FillStep\":1.0,\"tether\":true,\"LegacyFill\":true}");
            Controller.RegisterElementFromCode($"Spot{TowerDirection.East}", "{\"Name\":\"\",\"refX\":117.0,\"refY\":100.0,\"radius\":2.0,\"color\":4294963968,\"thicc\":5.0,\"FillStep\":1.0,\"tether\":true,\"LegacyFill\":true}");
            Controller.RegisterElementFromCode($"Spot{TowerDirection.West}", "{\"Name\":\"\",\"refX\":83.0,\"refY\":100.0,\"refZ\":1.9073486E-06,\"radius\":2.0,\"color\":4294963968,\"thicc\":5.0,\"FillStep\":1.0,\"tether\":true,\"LegacyFill\":true}");
            Controller.RegisterElementFromCode($"Spot{TowerDirection.South}", "{\"Name\":\"\",\"refX\":100.0,\"refY\":117.0,\"radius\":2.0,\"color\":4294963968,\"thicc\":5.0,\"FillStep\":1.0,\"tether\":true,\"LegacyFill\":true}");
            Controller.RegisterElementFromCode($"Spot{TowerDirection.SouthEast}", "{\"Name\":\"\",\"refX\":112.0,\"refY\":112.0,\"refZ\":1.9073486E-06,\"radius\":2.0,\"color\":4294963968,\"thicc\":5.0,\"FillStep\":1.0,\"tether\":true,\"LegacyFill\":true}");
            Controller.RegisterElementFromCode($"Spot{TowerDirection.SouthWest}", "{\"Name\":\"\",\"refX\":88.0,\"refY\":112.0,\"refZ\":1.9073486E-06,\"radius\":2.0,\"color\":4294963968,\"thicc\":5.0,\"FillStep\":1.0,\"tether\":true,\"LegacyFill\":true}");
            Controller.RegisterElementFromCode($"Spot{TowerDirection.NorthEast}", "{\"Name\":\"\",\"refX\":112.0,\"refY\":88.0,\"refZ\":1.9073486E-06,\"radius\":2.0,\"color\":4294963968,\"thicc\":5.0,\"FillStep\":1.0,\"tether\":true,\"LegacyFill\":true}");
            Controller.RegisterElementFromCode($"Spot{TowerDirection.NorthWest}", "{\"Name\":\"\",\"refX\":88.0,\"refY\":88.0,\"refZ\":1.9073486E-06,\"radius\":2.0,\"color\":4294963968,\"thicc\":5.0,\"FillStep\":1.0,\"tether\":true,\"LegacyFill\":true}");
        }

        public override void OnMapEffect(uint position, ushort data1, ushort data2)
        {
            if (C.HighlightTower)
            {
                var pos = (TowerDirection)position;
                if(Towers.TryGetValue(pos, out var tower) && pos.EqualsAny(C.Tower1, C.Tower2))
                {
                    //my tower
                    if (data1 == 1)
                    {
                        TowerScheduler?.Dispose();
                        TowerScheduler = new(delegate
                        {
                            if (Controller.TryGetElementByName("Tower", out var e))
                            {
                                e.Enabled = true;
                                e.SetRefPosition(tower.ToVector3(0));
                            }
                        }, C.TowerDelay);
                    }
                    if (data1 == 4)
                    {
                        if (Controller.TryGetElementByName("Tower", out var e))
                        {
                            e.Enabled = false;
                        }
                        if (C.MoveDirection != MoveDirection.Disable) 
                        {
                            var next = GetNextSpot(pos, C.MoveDirection);
                            //now draw path
                            if(Controller.TryGetElementByName($"Spot{next}", out var t))
                            {
                                t.Enabled = true;
                                new TickScheduler(() => t.Enabled = false, 3000);
                            }
                        }
                    }
                }
            }
        }

        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
            ResetAll();
        }

        public override void OnDisable()
        {
            ActionEffect.ActionEffectEvent -= ActionEffect_ActionEffectEvent;
            ResetAll();
        }

        private void ActionEffect_ActionEffectEvent(ActionEffectSet set)
        {
            if(set.Source != null && set.Source is BattleNpc b)
            {
                //DuoLog.Information($"{set.Action.RowId} - {set.Action.Name} ({b.Name})");
                if(C.EnableProteanLinger && set.Action.RowId.EqualsAny<uint>(33257, 33256)) //protean
                {
                    new TickScheduler(() =>
                    {
                        var col = GetColor(Themis);
                        var name = col == Color.Dark ? "PairDonut" : "LingerAOE";
                        int i = 0;
                        foreach (var x in FakeParty.Get())
                        {
                            if (Controller.TryGetElementByName($"{name}{i}", out var e))
                            {
                                e.Enabled = true;
                                e.SetRefPosition(x.Position);
                            }
                            i++;
                        }
                    }, 500);
                    DonutScheduler?.Dispose();
                    DonutScheduler = new(() => Controller.GetRegisteredElements().Where(x => x.Key.StartsWithAny("PairDonut", "LingerAOE")).Each(z => z.Value.Enabled = false), 3000);
                }
            }
        }

        Color GetColor(GameObject obj)
        {
            Color col = Color.Unknown;
            long age = long.MaxValue;
            if(AttachedInfo.TryGetVfx(obj, out var info))
            {
                foreach(var x in info)
                {
                    if (x.Value.Age < age)
                    {
                        if (x.Key == LightVFX)
                        {
                            col = Color.Light;
                            age = x.Value.Age;
                        }
                        else if(x.Key == DarkVFX)
                        {
                            col = Color.Dark;
                            age = x.Value.Age;
                        }
                    }
                }
            }
            return col;
        }

        public override void OnSettingsDraw()
        {
            ImGui.Checkbox("Display post-protean lingering effects (AOE/donut)", ref C.EnableProteanLinger);

            ImGui.Checkbox("Highlight your designated letter of the law tower", ref C.HighlightTower);
            if (C.HighlightTower)
            {
                ImGuiEx.TextV("Your towers:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                ImGuiEx.EnumCombo("##t1", ref C.Tower1);
                ImGui.SameLine();
                ImGuiEx.Text("+");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                ImGuiEx.EnumCombo("##t2", ref C.Tower2);
                ImGui.SetNextItemWidth(150f);
                ImGuiEx.SliderIntAsFloat("Delay tower highlight after spawning, seconds", ref C.TowerDelay, 0, 5000);
                ImGui.SetNextItemWidth(150f);
                ImGuiEx.EnumCombo("Highlight move direction after taking tower", ref C.MoveDirection);
                if(C.MoveDirection == MoveDirection.Fixed_position)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150f);
                    ImGuiEx.EnumCombo("##fixedpos", ref C.SafeSpotFixedPos);
                }


                if (ImGui.CollapsingHeader("Debug"))
                {
                    var next1 = GetNextSpot(C.Tower1, C.MoveDirection);
                    var next2 = GetNextSpot(C.Tower2, C.MoveDirection);
                    ImGuiEx.Text($"Next: 1: {C.Tower1}->{next1}, 2: {C.Tower2}->{next2}");

                    var t = Themis;
                    if (t != null)
                    {
                        ImGuiEx.Text($"Themis color: {GetColor(t)} / {t}");
                    }
                    foreach (var x in IllusoryThemises)
                    {
                        ImGuiEx.Text($"{x} color: {GetColor(x)}");
                    }
                }
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
            {
                ResetAll();
            }
        }

        void ResetAll()
        {
            DonutScheduler?.Dispose();
            TowerScheduler?.Dispose();
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }

        Config C => Controller.GetConfig<Config>();
        public class Config : IEzConfig
        {
            public bool EnableProteanLinger = true;
            public bool HighlightTower = false;
            public TowerDirection Tower1 = TowerDirection.North;
            public TowerDirection Tower2 = TowerDirection.NorthWest;
            public MoveDirection MoveDirection = MoveDirection.Disable;
            public int TowerDelay = 4500;
            public TowerDirection SafeSpotFixedPos = TowerDirection.North;
        }
    }
}
