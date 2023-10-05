using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using Splatoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P10S_Tethers : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1150 };
        public override Metadata? Metadata => new(2, "NightmareXIV");
        List<TetherData> Tethers = new();

        public class TetherData
        {
            public uint source;
            public uint target;
            public long time = Environment.TickCount64;
            public float Age => (float)(Environment.TickCount64 - time) / 1000f;
        }

        public override void OnSetup()
        {
            var code = "{\"Name\":\"\",\"type\":5,\"refX\":103.03228,\"refY\":99.94743,\"radius\":20.0,\"coneAngleMin\":-61,\"coneAngleMax\":61,\"color\":3355506687,\"FillStep\":2.0,\"includeRotation\":true,\"AdditionalRotation\":3.1415927,\"Filled\":true}";
            Controller.RegisterElementFromCode("Cone1", code);
            Controller.RegisterElementFromCode("Cone2", code);
            Controller.RegisterElementFromCode("Tether", "{\"Name\":\"\",\"Enabled\":false,\"radius\":0.0,\"thicc\":5.0,\"tether\":true}");
        }

        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
            Off();
        }

        private void ActionEffect_ActionEffectEvent(ECommons.Hooks.ActionEffectTypes.ActionEffectSet set)
        {
            if(set.Action.RowId == 33432)
            {
                Off();
            }
        }

        public override void OnDisable()
        {
            ActionEffect.ActionEffectEvent -= ActionEffect_ActionEffectEvent;
            Off();
        }

        public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
        {
            Tethers.Add(new() { source = source, target = target });
            Tethers.RemoveAll(x => x.Age > 30f);
        }

        void Off()
        {
            Tethers.Clear();
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
            {
                Off();
            }
        }

        public override void OnSettingsDraw()
        {
            ImGui.ColorEdit4("Self color", ref C.ColorSelf, ImGuiColorEditFlags.NoInputs);
            ImGui.Checkbox("Highlight own tether", ref C.TetherSelf);
            ImGui.ColorEdit4("Other player color", ref C.Color, ImGuiColorEditFlags.NoInputs);
            ImGui.SetNextItemWidth(150f);
            ImGui.SliderFloat("Cone line thickness", ref C.Thick, 1f, 10f);
            ImGui.SetNextItemWidth(150f);
            ImGui.SliderFloat("Cone fill step", ref C.Interval, 1f, 20f);
            ImGui.SetNextItemWidth(150f);
            ImGui.SliderFloat("Cone length", ref C.radius, 5f, 40f);
            if (ImGui.CollapsingHeader("debug"))
            {
                foreach(var x in Tethers)
                {
                    ImGuiEx.Text($"{x.source.GetObject()}->{x.target.GetObject()}, {x.Age}s");
                }
            }
        }

        public override void OnUpdate()
        {
            int num = 1;
            foreach(var x in Tethers)
            {
                if(x.source.TryGetObject(out var pillar) && pillar is BattleChara p && p.NameId == 12354 && x.target.TryGetObject(out var player) && player is PlayerCharacter pc && Controller.TryGetElementByName($"Cone{num}", out var e))
                {
                    num++;
                    e.Enabled = true;
                    e.AdditionalRotation = (180 + MathHelper.GetRelativeAngle(p.Position, pc.Position)).DegreesToRadians();
                    e.SetRefPosition(p.Position);
                    e.color = C.Color.ToUint();
                    e.thicc = C.Thick;
                    e.FillStep = C.Interval;
                    e.radius = C.radius;
                    if(pc.Address == Player.Object.Address)
                    {
                        e.color = C.ColorSelf.ToUint();
                        if (C.TetherSelf && Controller.TryGetElementByName("Tether", out var t))
                        {
                            t.Enabled = true;
                            t.SetRefPosition(p.Position);
                        }
                    }
                }
            }
        }

        Config C => Controller.GetConfig<Config>();
        public class Config : IEzConfig
        {
            public Vector4 Color = 0xFFF700C8.SwapBytes().ToVector4();
            public Vector4 ColorSelf = 0xFFAD00C8.SwapBytes().ToVector4();
            public float Thick = 4f;
            public float Interval = 10f;
            public bool TetherSelf = true;
            public float radius = 10f;
        }
    }
}
