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
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class DSR_Wrath : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 968 };
        public override Metadata? Metadata => new(2, "Enthusiastus");

        Element? SkydiveTargetElement;
        Element? NoSkydiveTargetElement;
        Element? BahamutDiveTargetElement;
        Element? IgnasseTargetElement;
        Element? IgnasseHitboxElement;
        IPlayerCharacter? IgnassePlayer;
        Element? VellguineTargetElement;
        Element? VellguineHitboxElement;
        IPlayerCharacter? VellguinePlayer;

        bool active = false;
        bool gottether = false;

        uint IgnasseDataId = 12635;
        IBattleNpc? Ignasse => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == IgnasseDataId) as IBattleNpc;
        uint VellguineDataId = 12633;
        IBattleNpc? Vellguine => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == VellguineDataId) as IBattleNpc;

        string TestOverride = "";

        IPlayerCharacter PC => TestOverride != "" && FakeParty.Get().FirstOrDefault(x => x.Name.ToString() == TestOverride) is IPlayerCharacter pc ? pc : Svc.ClientState.LocalPlayer!;

        Vector2 Center = new(100, 100);

        public override void OnSetup()
        {
            var skydiveTargetTether = "{\"Name\":\"markerTargetTether\",\"type\":1,\"offX\":17.42,\"offY\":12.22,\"radius\":0.6,\"color\":4294901787,\"thicc\":7.6,\"refActorNPCNameID\":3984,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}";
            var noSkydiveTargetTether = "{\"Name\":\"nomarkerTargetTether\",\"type\":1,\"offX\":-19.5,\"offY\":23.0,\"radius\":0.6,\"color\":4294901787,\"thicc\":7.6,\"refActorNPCNameID\":3984,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}";
            var bahamutDiveTargetTether = "{\"Name\":\"bahamutDiveTargetTether\",\"type\":1,\"offY\":28.0,\"radius\":0.6,\"color\":4294901787,\"thicc\":7.6,\"refActorNPCNameID\":3639,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}";
            var ignasseTargetTether = "{\"Name\":\"ignasseTargetTether\",\"type\":1,\"offX\":-2.7,\"offY\":41.7,\"radius\":0.6,\"color\":4294901787,\"thicc\":7.6,\"refActorDataID\":12635,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}";
            var ignasseHitbox = "{\"Name\":\"ignasseHitbox\",\"type\":2,\"radius\":7.0,\"color\":1258291455,\"thicc\":7.0,\"FillStep\":1.5}";
            var vellguineTargetTether = "{\"Name\":\"vellguineTargetTether\",\"type\":1,\"offX\":4.7,\"offY\":41.7,\"radius\":0.6,\"color\":4294901787,\"thicc\":7.6,\"refActorDataID\":12633,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}";
            var vellguineHitbox = "{\"Name\":\"vellguineHitbox\",\"type\":2,\"radius\":7.0,\"color\":1258291455,\"thicc\":7.0,\"FillStep\":1.5}";
            SkydiveTargetElement = Controller.RegisterElementFromCode($"skydivetether", skydiveTargetTether);
            SkydiveTargetElement.Enabled = false;
            NoSkydiveTargetElement = Controller.RegisterElementFromCode($"noskydivetether", noSkydiveTargetTether);
            NoSkydiveTargetElement.Enabled = false;
            BahamutDiveTargetElement = Controller.RegisterElementFromCode($"bahamuttether", bahamutDiveTargetTether);
            BahamutDiveTargetElement.Enabled = false;
            IgnasseTargetElement = Controller.RegisterElementFromCode($"ignassetether", ignasseTargetTether);
            IgnasseTargetElement.Enabled = false;
            IgnasseHitboxElement = Controller.RegisterElementFromCode($"ignassehitbox", ignasseHitbox);
            IgnasseHitboxElement.Enabled = false;
            VellguineTargetElement = Controller.RegisterElementFromCode($"vellgunietether", vellguineTargetTether);
            VellguineTargetElement.Enabled = false;
            VellguineHitboxElement = Controller.RegisterElementFromCode($"vellguinehitbox", vellguineHitbox);
            VellguineHitboxElement.Enabled = false;
        }

        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
        }

        public override void OnMessage(string Message)
        {
            if (Message.Contains("King Thordan readies Wrath of the Heavens"))
            {
                active = true;
            }
            if (Message.Contains("King Thordan readies Death of the Heavens"))
            {
                active = false;
            }
        }

        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            if (vfxPath == "vfx/lockon/eff/m0005sp_19o0t.avfx")
            {
                if (target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc)
                {
                    //DuoLog.Information($"Local player is {PC.Name}");
                    if (PC == pvc)
                    {
                        //DuoLog.Information($"Skyward Leap is on me, tether other side");
                        SkydiveTargetElement.Enabled = true;
                    }
                    else
                    {
                        //DuoLog.Information($"Skyward Leap is on someone else tether side");
                        if (gottether)
                            return;
                        NoSkydiveTargetElement.Enabled = true;
                    }
                    Task.Delay(8000).ContinueWith(_ =>
                    {
                        SkydiveTargetElement.Enabled = false;
                        NoSkydiveTargetElement.Enabled = false;
                    });
                }
            }
            if (vfxPath == "vfx/lockon/eff/bahamut_wyvn_glider_target_02tm.avfx")
            {
                if (target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc && pvc == PC)
                {
                    //DuoLog.Information($"Oh no BahamutWYVNGLIDER on {pvc}");
                    BahamutDiveTargetElement.Enabled = true;
                    Task.Delay(10000).ContinueWith(_ =>
                    {
                        BahamutDiveTargetElement.Enabled = false;
                    });
                }
            }
        }

        public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
        {
            // Look for tethers only in p5 wrath (see OnMessage)
            if (!active) return;
            if (source.TryGetObject(out var ignasse) && ignasse is IBattleChara ig && ig.NameId == 3638 && target.TryGetObject(out var pi) && pi is IPlayerCharacter pic)
            {
                IgnassePlayer = pic;
                //DuoLog.Information($"Ignasse tether from {ignasse.Name} to {IgnassePlayer.Name} data {data2} || {data3} || {data5}");
                if (PC == pic)
                {
                    gottether = true;
                    NoSkydiveTargetElement.Enabled = false;
                    SkydiveTargetElement.Enabled = false;
                    IgnasseTargetElement.Enabled = true;
                    Task.Delay(6200).ContinueWith(_ =>
                    {
                        IgnasseTargetElement.Enabled = false;
                    });
                } else
                {
                    IgnasseHitboxElement.SetRefPosition(ignasse.Position);
                    IgnasseHitboxElement.SetOffPosition(IgnassePlayer.Position);
                    IgnasseHitboxElement.Enabled = true;
                    Task.Delay(7000).ContinueWith(_ =>
                    {
                        IgnasseHitboxElement.Enabled = false;

                    });
                }
            }
            else if (source.TryGetObject(out var vellguine) && vellguine is IBattleChara vg && vg.NameId == 3636 && target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc)
            {
                VellguinePlayer = pvc;
                //DuoLog.Information($"Vellguine tether from {vellguine.Name} to {VellguinePlayer.Name} data {data2} || {data3} || {data5}");
                if (PC == pvc)
                {
                    gottether = true;
                    NoSkydiveTargetElement.Enabled = false;
                    SkydiveTargetElement.Enabled = false;
                    VellguineTargetElement.Enabled = true;
                    Task.Delay(6200).ContinueWith(_ =>
                    {
                        VellguineTargetElement.Enabled = false;
                    });
                } else
                {
                    VellguineHitboxElement.SetRefPosition(vellguine.Position);
                    VellguineHitboxElement.SetOffPosition(VellguinePlayer.Position);
                    VellguineHitboxElement.Enabled = true;
                    Task.Delay(7000).ContinueWith(_ =>
                    {
                        VellguineHitboxElement.Enabled = false;

                    });
                }
                
            }
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
            active = false;
            gottether = false;
            if (SkydiveTargetElement != null)
                SkydiveTargetElement.Enabled = false;
            if (NoSkydiveTargetElement != null)
                NoSkydiveTargetElement.Enabled = false;
            if (BahamutDiveTargetElement != null)
                BahamutDiveTargetElement.Enabled = false;
            if (IgnasseTargetElement != null)
                IgnasseTargetElement.Enabled = false;
            if (VellguineTargetElement != null)
                VellguineTargetElement.Enabled = false;
            if (IgnasseHitboxElement != null)
                IgnasseHitboxElement.Enabled = false;
            if (VellguineHitboxElement != null)
                VellguineHitboxElement.Enabled = false;
        }

        public override void OnUpdate()
        {
            if(IgnasseHitboxElement.Enabled)
            {
                IgnasseHitboxElement.SetRefPosition(Ignasse.Position);
                IgnasseHitboxElement.SetOffPosition(IgnassePlayer.Position);
            }
            if(VellguineHitboxElement.Enabled)
            {
                VellguineHitboxElement.SetRefPosition(Vellguine.Position);
                VellguineHitboxElement.SetOffPosition(VellguinePlayer.Position);
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
