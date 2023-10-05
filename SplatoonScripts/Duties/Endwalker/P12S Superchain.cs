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
using Splatoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P12S_Superchain : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1154 };
        public override Metadata? Metadata => new(6, "NightmareXIV");

        enum Spheres : uint 
        {
            Mastersphere = 16176,
            AOEBall = 16177,
            Protean = 16179,
            Donut = 16178,
            Pairs = 16180,
        }

        const uint AOEDebuff = 3578;

        public override void OnSetup()
        {
            var donut = "{\"Name\":\"Donut\",\"type\":1,\"radius\":6.0,\"Donut\":30.0,\"thicc\":3.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2}";
            Controller.RegisterElementFromCode("Donut1", donut);
            Controller.RegisterElementFromCode("Donut2", donut);

            var AOE = "{\"Name\":\"AOE\",\"type\":1,\"radius\":7.0,\"color\":2013266175,\"thicc\":3.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"Filled\":true}";
            Controller.RegisterElementFromCode("AOEBall1", AOE);
            Controller.RegisterElementFromCode("AOEBall2", AOE);

            Controller.TryRegisterLayoutFromCode("Protean", "~Lv2~{\"Name\":\"P12S Protean\",\"Group\":\"P12S\",\"ZoneLockH\":[1154],\"ElementsL\":[{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":0.7853982,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":1.5707964,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":2.3561945,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":3.1415927,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":3.7524579,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":4.7996554,\"Filled\":true},{\"Name\":\"Protean\",\"type\":3,\"refY\":7.16,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":5.497787,\"Filled\":true}]}", out _);

            Controller.TryRegisterLayoutFromCode("Pairs", "~Lv2~{\"Name\":\"P12S Pairs\",\"Group\":\"P12S\",\"ZoneLockH\":[1154],\"ElementsL\":[{\"Name\":\"Pair\",\"type\":3,\"refX\":-0.5,\"refY\":7.16,\"offX\":-0.5,\"radius\":0.0,\"color\":4294902005,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":0.5,\"refY\":7.16,\"offX\":0.5,\"radius\":0.0,\"color\":4294902011,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":-0.5,\"refY\":7.16,\"offX\":-0.5,\"radius\":0.0,\"color\":4294902005,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":1.5707964,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":0.5,\"refY\":7.16,\"offX\":0.5,\"radius\":0.0,\"color\":4294902011,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":1.5707964,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":0.5,\"refY\":7.16,\"offX\":0.5,\"radius\":0.0,\"color\":4294902011,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":3.1415927,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":-0.5,\"refY\":7.16,\"offX\":-0.5,\"radius\":0.0,\"color\":4294902011,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":3.1415927,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":0.5,\"refY\":7.16,\"offX\":0.5,\"radius\":0.0,\"color\":4294902011,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":4.712389,\"Filled\":true},{\"Name\":\"Pair\",\"type\":3,\"refX\":-0.5,\"refY\":7.16,\"offX\":-0.5,\"radius\":0.0,\"color\":4294902011,\"thicc\":7.0,\"refActorObjectID\":0,\"FillStep\":0.25,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":4.712389,\"Filled\":true}]}", out _);

            Controller.TryRegisterLayoutFromCode("DebuffAOESelf", "~Lv2~{\"Enabled\":false,\"Name\":\"P12S Spread AOE\",\"Group\":\"P12S\",\"ZoneLockH\":[1154],\"ElementsL\":[{\"Name\":\"self\",\"type\":1,\"radius\":7.0,\"color\":1258356223,\"refActorType\":1,\"Filled\":true},{\"Name\":\"party\",\"type\":1,\"radius\":7.0,\"color\":4278255586,\"thicc\":5.0,\"refActorPlaceholder\":[\"<2>\",\"<3>\",\"<4>\",\"<5>\",\"<6>\",\"<7>\",\"<8>\"],\"refActorComparisonType\":5}],\"MaxDistance\":7.0,\"UseDistanceLimit\":true,\"DistanceLimitType\":1}", out _);

            Controller.TryRegisterLayoutFromCode("DebuffAOEOther", "~Lv2~{\"Enabled\":false,\"Name\":\"P12S Spread AOE other\",\"Group\":\"P12S\",\"ZoneLockH\":[1154],\"ElementsL\":[{\"Name\":\"party\",\"type\":1,\"radius\":7.0,\"color\":3355508706,\"thicc\":5.0,\"refActorPlaceholder\":[\"<2>\",\"<3>\",\"<4>\",\"<5>\",\"<6>\",\"<7>\",\"<8>\"],\"refActorRequireBuff\":true,\"refActorBuffId\":[3578],\"refActorUseBuffTime\":true,\"refActorBuffTimeMax\":3.5,\"refActorComparisonType\":5}]}", out _);
        }

        Dictionary<uint, List<uint>> Attachments = new();

        public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
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
            if(Controller.TryGetLayoutByName("DebuffAOESelf", out var self) && Controller.TryGetLayoutByName("DebuffAOEOther", out var other))
            {
                if (C.EnableAOEChecking)
                {
                    if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == AOEDebuff && x.RemainingTime < 3.5f))
                    {
                        self.Enabled = true;
                        other.Enabled = false;
                    }
                    else
                    {
                        self.Enabled = false;
                        other.Enabled = true;
                    }
                }
                else
                {
                    self.Enabled = false;
                    other.Enabled = false;
                }
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
                            e.color = C.AoeColor.ToUint();
                        }
                    }
                    else if (x.type == Spheres.Donut)
                    {
                        donut++;
                        if (Controller.TryGetElementByName($"Donut{donut}", out var e))
                        {
                            e.Enabled = true;
                            e.refActorObjectID = x.obj.ObjectId;
                            e.Donut = C.DonutRadius;
                            e.color = C.DonutColor.ToUint();
                            //e.color = TransformColorBasedOnDistance(e.color, x.dist);
                        }
                    }
                    else
                    {
                        if (Controller.TryGetLayoutByName($"{x.type}", out var e))
                        {
                            e.Enabled = true;
                            e.ElementsL.Each(z => z.refActorObjectID = x.obj.ObjectId);
                            if (x.type == Spheres.Protean) e.ElementsL.Each(z => z.color = C.ProteanLineColor.ToUint());
                            if (x.type == Spheres.Pairs) e.ElementsL.Each(z => z.color = C.PairLineColor.ToUint());
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

        public unsafe static Vector4 Vector4FromABGR(uint col)
        {
            byte* bytes = (byte*)&col;
            return new Vector4((float)bytes[0] / 255f, (float)bytes[1] / 255f, (float)bytes[2] / 255f, (float)bytes[3] / 255f);
        }

        public class Config : IEzConfig
        {
            public float DonutRadius = 25.0f;
            public bool EnableAOEChecking = true;
            public Vector4 ProteanLineColor = Vector4FromABGR(4278190335);
            public Vector4 PairLineColor = Vector4FromABGR(4294902011);
            public Vector4 AoeColor = Vector4FromABGR(0x780000FF);
            public Vector4 DonutColor = Vector4FromABGR(0x660000FF);
            //public Vector4 AssistColorSelf = Vector4FromABGR(1258356223);
            //public Vector4 AssistColorOther = Vector4FromABGR(3355508706);
        }

        Config C => Controller.GetConfig<Config>();

        public override void OnSettingsDraw()
        {
            ImGuiEx.TextV("Dount radius:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f);
            ImGui.DragFloat("", ref C.DonutRadius.ValidateRange(5f, 50f), 0.1f, 5f, 30f);
            ImGui.ColorEdit4("Dount color", ref C.DonutColor, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("AOE color", ref C.AoeColor, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Pair line color", ref C.PairLineColor, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Protean line color", ref C.ProteanLineColor, ImGuiColorEditFlags.NoInputs);
            ImGui.Checkbox($"Enable AOE debuff assist", ref C.EnableAOEChecking);
            /*ImGuiEx.Text($"       ");
            ImGui.SameLine();
            ImGui.ColorEdit4("Self color (filled)", ref C.AssistColorSelf, ImGuiColorEditFlags.NoInputs);
            ImGui.SameLine();
            ImGui.ColorEdit4("Others (radius)", ref C.AssistColorOther, ImGuiColorEditFlags.NoInputs);*/

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
