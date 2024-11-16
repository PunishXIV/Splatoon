using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ImGuiNET;
using Microsoft.VisualBasic.ApplicationServices;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class DSR_Dooms : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 968 };
        public override Metadata? Metadata => new(6, "Enthusiastus");

        List<Element> DoomElements = new();
        List<Element> NoDoomElements = new();
        Dictionary<double, IPlayerCharacter> plrs = new();

        Element? Circle1Element;
        Element? Circle2Element;
        Element? DoomSquareElement;
        Element? DoomTriangleElement;
        Element? NoDoomSquareElement;
        Element? NoDoomTriangleElement;
        Element? X1Element;
        Element? X2Element;

        int count = 0;
        bool active = false;

        const uint GuerriqueDataId = 12637;
        bool positionDynamic = true;

        //IBattleNpc? Thordan => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == ThordanDataId) as IBattleNpc;
        string TestOverride = "";

        IPlayerCharacter PC => TestOverride != "" && FakeParty.Get().FirstOrDefault(x => x.Name.ToString() == TestOverride) is IPlayerCharacter pc ? pc : Svc.ClientState.LocalPlayer!;
        Vector2 Center = new(100, 100);

        public override void OnSetup()
        {
            var circle1 = "{\"Name\":\"Doom Circle 1\",\"Enabled\":false,\"type\":1,\"offX\":2.5,\"offY\":9.0,\"radius\":0.5,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"W\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            var circle2 = "{\"Name\":\"Doom Circle 2\",\"Enabled\":false,\"type\":1,\"offX\":-2.5,\"offY\":9.0,\"radius\":0.5,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"E\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            var doomsquare = "{\"Name\":\"Doom Square\",\"Enabled\":false,\"type\":1,\"offX\":1.95,\"offY\":10.9,\"radius\":0.5,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            var doomtriangle = "{\"Name\":\"Doom Triangle\",\"Enabled\":false,\"type\":1,\"offX\":-1.95,\"offY\":10.9,\"radius\":0.5,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            var nodoomsquare = "{ \"Name\":\"Non doom Square\",\"Enabled\":false,\"type\":1,\"offX\":-1.92,\"offY\":7.15,\"radius\":0.5,\"color\":3372154884,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            var nodoomtriangle = "{\"Name\":\"Non doom Triangle\",\"Enabled\":false,\"type\":1,\"offX\":1.95,\"offY\":7.15,\"radius\":0.5,\"color\":3372154884,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            var nodoomx1 = "{\"Name\":\"Non doom X 1\",\"Enabled\":false,\"type\":1,\"offY\":6.12,\"radius\":0.5,\"color\":3372154884,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"N\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            var nodoomx2 = "{\"Name\":\"Non doom X 2\",\"Enabled\":false,\"type\":1,\"offY\":11.94,\"radius\":0.5,\"color\":3372154884,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"S\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            Circle1Element = Controller.RegisterElementFromCode($"circle1", circle1);
            Circle2Element = Controller.RegisterElementFromCode($"circle2", circle2);
            DoomSquareElement = Controller.RegisterElementFromCode($"doomsquare", doomsquare);
            DoomTriangleElement = Controller.RegisterElementFromCode($"doomtriangle", doomtriangle);
            NoDoomSquareElement = Controller.RegisterElementFromCode($"nodoomsquare", nodoomsquare);
            NoDoomTriangleElement = Controller.RegisterElementFromCode($"nodoomtriangle", nodoomtriangle);
            X1Element = Controller.RegisterElementFromCode($"x1", nodoomx1);
            X2Element = Controller.RegisterElementFromCode($"x2", nodoomx2);

            var doom = "{\"Name\":\"\",\"radius\":0.0,\"overlayBGColor\":1879048447,\"overlayVOffset\":0,\"overlayFScale\":7.0,\"thicc\":0.0,\"overlayText\":\"1\",\"refActorType\":1}";
            var nodoom = "{\"Name\":\"\",\"radius\":0.0,\"overlayBGColor\":1895761920,\"overlayVOffset\":0,\"overlayFScale\":7.0,\"thicc\":0.0,\"overlayText\":\"1\",\"refActorType\":1}";
            for (var i = 0; i < 4; i++)
            {
                var e = Controller.RegisterElementFromCode($"doom{i}", doom);
                e.overlayText = $"{i + 1}";
                e.overlayBGColor = Conf.ColDoom.ToUint();
                e.offZ = Conf.offZ;
                e.overlayFScale = Conf.tScale;
                e.Enabled = false;
                DoomElements.Add(e);
            }
            for (var i = 0; i < 4; i++)
            {
                var e = Controller.RegisterElementFromCode($"nodoom{i}", nodoom);
                e.overlayText = $"{i + 1}";
                e.overlayBGColor = Conf.ColNoDoom.ToUint();
                e.offZ = Conf.offZ;
                e.overlayFScale = Conf.tScale;
                e.Enabled = false;
                NoDoomElements.Add(e);
            }
        }

        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
        }
        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            // Circle
            if (vfxPath == "vfx/lockon/eff/r1fz_firechain_01x.avfx")
            {
                DeactivateDoomMarkers();
                if (target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc)
                {
                    //DuoLog.Information($"{pvc.Name} has circle");
                    if (pvc != PC)
                        return;
                    Circle1Element.Enabled = true;
                    Circle2Element.Enabled = true;
                }
            // Triangle
            } else if (vfxPath == "vfx/lockon/eff/r1fz_firechain_02x.avfx")
            {
                if (target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc)
                {
                    //DuoLog.Information($"{pvc.Name} has triangle");
                    if (pvc != PC)
                        return;
                    var doom = PC.StatusList.Where(z => z.StatusId == 2976);
                    if (doom.Count() > 0)
                    {
                        DoomTriangleElement.Enabled = true;
                    } else
                    {
                        NoDoomTriangleElement.Enabled = true;
                    }
                }
            // Square
            } else if (vfxPath == "vfx/lockon/eff/r1fz_firechain_03x.avfx")
            {
                if (target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc)
                {
                    //DuoLog.Information($"{pvc.Name} has square");
                    if (pvc != PC)
                        return;
                    var doom = PC.StatusList.Where(z => z.StatusId == 2976);
                    if (doom.Count() > 0)
                    {
                        DoomSquareElement.Enabled = true;
                    }
                    else
                    {
                        NoDoomSquareElement.Enabled = true;
                    }
                }
            // X
            } else if (vfxPath == "vfx/lockon/eff/r1fz_firechain_04x.avfx")
            {
                if (target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc)
                {
                    //DuoLog.Information($"{pvc.Name} has x");
                    if (pvc != PC)
                        return;
                    X1Element.Enabled = true;
                    X2Element.Enabled = true;
                }
            }
        }

        public override void OnMessage(string Message)
        {
            if (Message.Contains("(3641>25557)"))
            {
                if (count == 0)
                {
                    count++;
                    return;
                }
                //DuoLog.Information($"Congaline should be complete now.");
                var guerrique = Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == GuerriqueDataId) as IBattleNpc;
                //DuoLog.Information($"Guerrique is at {guerrique.Position.X}/{guerrique.Position.Z}/{guerrique.Position.Y}, need to rotate {-guerrique.Rotation}");

                var players = FakeParty.Get();
                int blue = 0;
                int red = 0;
                foreach (var p in players)
                {
                    //DuoLog.Information($"{p.Name} is @ {p.Position.X}/{p.Position.Z}/{p.Position.Y}");
                    plrs.Add(p.Position.X * Math.Cos(-guerrique.Rotation) + p.Position.Z * Math.Sin(-guerrique.Rotation), p);
                    var doom = p.StatusList.Where(z => z.StatusId == 2976);
                    //DuoLog.Information($"has {doom.Count()} dooms ");
                }
                foreach (var p in plrs.OrderBy(x => x.Key))
                {
                    //DuoLog.Information($"{p.Value.Name}");
                    var doom = p.Value.StatusList.Where(z => z.StatusId == 2976);
                    if (doom.Count() > 0)
                    {
                        var e = DoomElements[red];
                        e.SetRefPosition(p.Value.Position);
                        e.Enabled = true;
                        red++;
                    }
                    else
                    {
                        var e = NoDoomElements[blue];
                        e.SetRefPosition(p.Value.Position);
                        e.Enabled = true;
                        blue++;
                    }
                }
                active = true;
            }
            if(Message.Contains("Ser Grinnaux uses Faith Unmoving."))
            {
                DeactivateKnockbackMarkers();
            }
        }

        private void DeactivateDoomMarkers()
        {
            NoDoomElements.Each(x => x.Enabled = false);
            DoomElements.Each(x => x.Enabled = false);
            active = false;
        }

        private void DeactivateKnockbackMarkers()
        {
            Circle1Element.Enabled = false;
            Circle2Element.Enabled = false;
            DoomSquareElement.Enabled = false;
            DoomTriangleElement.Enabled = false;
            NoDoomSquareElement.Enabled = false;
            NoDoomTriangleElement.Enabled = false;
            X1Element.Enabled = false;
            X2Element.Enabled = false;
        }

        private void ActionEffect_ActionEffectEvent(ActionEffectSet set)
        {
            /*
            if (set.Action == null) return;
            if (set.Action.Value.RowId == 25544)
            {
                //DuoLog.Information($"Position locked!");
                positionDynamic = false;
                for (var i = 0; i < Cones.Count; ++i)
                {
                    var c = Cones[i];
                    var e = ConeElements[i];
                    e.color = C.Col2.ToUint();
                    c.DelTime = Environment.TickCount64 + 2*1000;
                }
                //DuoLog.Information($"Thordan is @ {Thordan.Position.X}/{Thordan.Position.Z}/{Thordan.Position.Y}");
            }*/
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
            plrs.Clear();
            count = 0;
            DeactivateDoomMarkers();
        }

        public override void OnUpdate()
        {
            if (!active)
                return;
            int blue = 0;
            int red = 0;
            foreach (var p in plrs.OrderBy(x => x.Key))
            {
                var doom = p.Value.StatusList.Where(z => z.StatusId == 2976);
                if (doom.Count() > 0)
                {
                    var e = DoomElements[red];
                    e.SetRefPosition(p.Value.Position);
                    red++;
                }
                else
                {
                    var e = NoDoomElements[blue];
                    e.SetRefPosition(p.Value.Position);
                    blue++;
                }
            }
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
            public Vector4 ColNoDoom = Vector4FromRGBA(0xFF0000C8);
            public Vector4 ColDoom = Vector4FromRGBA(0x0000ffC8);
            public float offZ = 1.8f;
            public float tScale = 7f;
        }

        public override void OnSettingsDraw()
        {
            ImGui.ColorEdit4("Non Doom Color", ref Conf.ColNoDoom, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Doom Color", ref Conf.ColDoom, ImGuiColorEditFlags.NoInputs);
            ImGui.Separator();
            ImGui.SetNextItemWidth(150);
            ImGui.DragFloat("Number vertical offset", ref Conf.offZ.ValidateRange(-5f, 5f), 0.1f);
            ImGui.SetNextItemWidth(150);
            ImGui.DragFloat("Number scale", ref Conf.tScale.ValidateRange(0.1f, 10f), 0.1f);
        }

        public unsafe static Vector4 Vector4FromRGBA(uint col)
        {
            byte* bytes = (byte*)&col;
            return new Vector4((float)bytes[3] / 255f, (float)bytes[2] / 255f, (float)bytes[1] / 255f, (float)bytes[0] / 255f);
        }
    }
}
