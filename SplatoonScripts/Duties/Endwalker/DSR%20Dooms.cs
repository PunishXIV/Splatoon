using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ImGuiNET;
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
        public override Metadata? Metadata => new(2, "Enthusiastus");

        List<Element> DoomElements = new();
        List<Element> NoDoomElements = new();
        Dictionary<double, PlayerCharacter> plrs = new();

        int count = 0;
        bool active = false;

        const uint GuerriqueDataId = 12637;
        bool positionDynamic = true;

        //BattleNpc? Thordan => Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.DataId == ThordanDataId) as BattleNpc;
        Vector2 Center = new(100, 100);

        public override void OnSetup()
        {
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
                var guerrique = Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.DataId == GuerriqueDataId) as BattleNpc;
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
            if (Message.Contains("Ser Grinnaux uses Faith Unmoving."))
            {
                NoDoomElements.Each(x => x.Enabled = false);
                DoomElements.Each(x => x.Enabled = false);
                active = false;
            }
        }

        private void ActionEffect_ActionEffectEvent(ActionEffectSet set)
        {
            /*
            if (set.Action == null) return;
            if (set.Action.RowId == 25544)
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
            DoomElements.Each(x => x.Enabled = false);
            NoDoomElements.Each(x => x.Enabled = false);
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
