using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Lumina.Data.Parsing.Tex.Buffers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P12S_Superchain : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1154 };
        public override Metadata? Metadata => new(1, "NightmareXIV");

        enum Spheres : uint 
        {
            Mastersphere = 16176,
            AOEBall = 16177,
            Protean = 16179,
            Donut = 16178,
            Pairs = 16180,
        }

        public override void OnSetup()
        {
            var donut = "{\"Name\":\"Donut\",\"type\":1,\"radius\":6.0,\"Donut\":30.0,\"thicc\":3.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2}";
            var e1 = Controller.RegisterElementFromCode("Donut1", donut);
            var e2 = Controller.RegisterElementFromCode("Donut2", donut);
            e1.Donut = Conf.DonutRadius;
            e2.Donut = Conf.DonutRadius;
            e1.color = Conf.DonutColor.ToUint();
            e2.color = Conf.DonutColor.ToUint(); 

            var AOE = "{\"Name\":\"AOE\",\"type\":1,\"radius\":7.0,\"color\":2013266175,\"thicc\":3.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"Filled\":true}";
            Controller.RegisterElementFromCode("AOEBall1", AOE);
            Controller.RegisterElementFromCode("AOEBall2", AOE);

            Controller.TryRegisterLayoutFromCode("Protean", "~Lv2~{\"Name\":\"P12S Protean\",\"Group\":\"P12S\",\"ZoneLockH\":[1154],\"ElementsL\":[{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":0.7853982,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":1.5707964,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":2.3561945,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":3.1415927,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":3.7524579,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":4.7996554,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":5.497787,\"Filled\":true}]}", out _);

            Controller.TryRegisterLayoutFromCode("Pairs", "~Lv2~{\"Name\":\"P12S Pairs\",\"Group\":\"P12S\",\"ZoneLockH\":[1154],\"ElementsL\":[{\"Name\":\"Pair\",\"type\":3,\"refX\":-0.5,\"refY\":7.16,\"offX\":-0.5,\"radius\":0.0,\"color\":4294902005,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":0.5,\"refY\":7.16,\"offX\":0.5,\"radius\":0.0,\"color\":4294902011,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":-0.5,\"refY\":7.16,\"offX\":-0.5,\"radius\":0.0,\"color\":4294902005,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":1.5707964,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":0.5,\"refY\":7.16,\"offX\":0.5,\"radius\":0.0,\"color\":4294902011,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":1.5707964,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":0.5,\"refY\":7.16,\"offX\":0.5,\"radius\":0.0,\"color\":4294902011,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":3.1415927,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":-0.5,\"refY\":7.16,\"offX\":-0.5,\"radius\":0.0,\"color\":4294902011,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":3.1415927,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":0.5,\"refY\":7.16,\"offX\":0.5,\"radius\":0.0,\"color\":4294902011,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":4.712389,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":-0.5,\"refY\":7.16,\"offX\":-0.5,\"radius\":0.0,\"color\":4294902011,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":4.712389,\"Filled\":true}]}", out _);
        }

        Dictionary<uint, List<uint>> Attachments = new();

        public override void OnTetherCreate(uint source, uint target, byte data2, byte data3, byte data5)
        {
            if (!Attachments.ContainsKey(target)) Attachments.Add(target, new());
            //DuoLog.Information($"Attached {source} to {target}");
            Attachments[target].Add(source);
        }

        public override void OnUpdate()
        {
            var list = FindNextMechanic().ToList();
            list.RemoveAll(x => x.dist < 0.1f);
            if(list.Count > 0)
            {
                var toDisplay = list.Where(x => Math.Abs(x.dist - list[0].dist) < 0.5f).Select(x => (x.type, x.obj, x.dist)).ToList();
                if(list.TryGetFirst(x => x.type.EqualsAny(Spheres.Pairs, Spheres.Protean), out var l))
                {
                    toDisplay.Add((l.type, l.obj, l.dist));
                }
                Display(toDisplay);
            }
            else
            {
                Display();
            }
        }

        void Display(IEnumerable<(Spheres type, BattleNpc obj, float dist)>? values = null)
        {
            int aoe = 0;
            int donut = 0;
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            Controller.GetRegisteredLayouts().Each(x => x.Value.Enabled = false);
            if (values != null)
            {
                foreach (var x in values)
                {
                    if (x.type == Spheres.AOEBall)
                    {
                        aoe++;
                        if (Controller.TryGetElementByName($"AOEBall{aoe}", out var e))
                        {
                            e.Enabled = true;
                            e.refActorObjectID = x.obj.ObjectId;
                            //e.color = TransformColorBasedOnDistance(e.color, x.dist);
                        }
                    }
                    else if (x.type == Spheres.Donut)
                    {
                        donut++;
                        if (Controller.TryGetElementByName($"Donut{donut}", out var e))
                        {
                            e.Enabled = true;
                            e.refActorObjectID = x.obj.ObjectId;
                            //e.color = TransformColorBasedOnDistance(e.color, x.dist);
                        }
                    }
                    else
                    {
                        if (Controller.TryGetLayoutByName($"{x.type}", out var e))
                        {
                            e.Enabled = true;
                            e.ElementsL.Each(z => z.refActorObjectID = x.obj.ObjectId);
                            //e.ElementsL.Each(z => z.color = TransformColorBasedOnDistance(z.color, x.dist));
                        }
                    }
                }
            }
        }

        uint TransformColorBasedOnDistance(uint col, float distance)
        {
            distance.ValidateRange(2, 20);
            distance -= 2f;
            var alpha = (1 - distance / 18) * 0.3f + 0.5f;
            return (col.ToVector4() with { W = alpha }).ToUint();
        }

        IEnumerable<(BattleNpc obj, Spheres type, float dist)> FindNextMechanic()
        {
            List<(BattleNpc obj, Spheres type, float dist)> objs = new();
            foreach(var x in Svc.Objects.Where(z => z is BattleNpc b && b.IsCharacterVisible()).Cast<BattleNpc>())
            {
                if(Enum.GetValues<Spheres>().Contains((Spheres)x.DataId) && x.DataId != (uint)Spheres.Mastersphere)
                {
                    var master = GetMasterSphereForObject(x);
                    if(master != null)
                    {
                        objs.Add((master, (Spheres)x.DataId, Vector3.Distance(master.Position, x.Position)));
                    }
                }
            }
            return objs.OrderBy(x => x.dist);
        }

        BattleNpc? GetMasterSphereForObject(BattleNpc obj)
        {
            foreach(var x in Attachments)
            {
                if (x.Value.Contains(obj.ObjectId) && x.Key.GetObject() is BattleNpc b && b.IsCharacterVisible())
                {
                    return b;
                }
            }
            return null;
        }

        public override void OnMessage(string Message)
        {
            if (Message.ContainsAny("(12377>33498)", "(12377>34554)", "(12377>34555)"))
            {
                Attachments.Clear();
            }
        }

        public class Config : IEzConfig
        {
            public float DonutRadius = 6.0f;
            public Vector4 DonutColor = ImGuiColors.DalamudViolet;
        }

        Config Conf => Controller.GetConfig<Config>();

        public override void OnSettingsDraw()
        {
            ImGui.InputFloat("donut radius: ", ref Conf.DonutRadius);
            ImGui.ColorEdit4("donut color: ", ref Conf.DonutColor); 

            if (ImGui.CollapsingHeader("Debug"))
            {
                foreach(var x in Attachments)
                {
                    ImGuiEx.Text($"{x.Key}({x.Key.GetObject()}) <- {x.Value.Select(z => $"{z}({z.GetObject()})").Print()}");
                }
                ImGui.Separator();
                foreach (var x in FindNextMechanic())
                {
                    ImGuiEx.Text($"{x.type} = {x.dist}");
                }
            }
        }
    }
}
