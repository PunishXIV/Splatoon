using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Schedulers;
using ImGuiNET;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P9S_Dualspell_InOut : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1148 };
        public override Metadata? Metadata => new(2, "NightmareXIV");
        TickScheduler? sched = null;
        BattleNpc? Kokytos => Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.DataId == 16087 && b.IsTargetable()) as BattleNpc;



        public override void OnSetup()
        {
            
            Controller.RegisterElementFromCode("In", "{\"Name\":\"In\",\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":8.0,\"Donut\":12.0,\"color\":0x70FFFF00,\"thicc\":4.0}");
            Controller.RegisterElementFromCode("Out", "{\"Name\":\"Out\",\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":14.0,\"Donut\":6.0,\"color\":0x70FFFF00,\"thicc\":4.0}");
            Controller.RegisterElementFromCode("SmallLaser", "{\"Name\":\"Small\",\"type\":3,\"Enabled\":false,\"refY\":20.0,\"radius\":4.0,\"color\":1275002890,\"refActorDataID\":16087,\"refActorUseCastTime\":true,\"refActorCastTimeMax\":8.0,\"refActorUseOvercast\":true,\"refActorComparisonType\":3,\"includeRotation\":true,\"FaceMe\":true}");
            Controller.RegisterElementFromCode("BigLaser", "{\"Name\":\"Large\",\"type\":3,\"Enabled\":false,\"refY\":20.0,\"radius\":8.0,\"color\":1275002890,\"refActorDataID\":16087,\"refActorUseCastTime\":true,\"refActorCastTimeMax\":8.0,\"refActorUseOvercast\":true,\"refActorComparisonType\":3,\"includeRotation\":true,\"FaceMe\":true}");
        }

        public override void OnActionEffect(uint ActionID, ushort animationID, ActionEffectType type, uint sourceID, ulong targetOID, uint damage)
        {
            if(ActionID == 33058 || ActionID == 33116) //flame/thunder
            {
                if(GetColor(Kokytos) == Color.LightningIce && Conf.ShowProtean)
                {
                    DisplayHide("Out", true);
                }
                else
                {
                    DisplayHide("Out");
                }
            }
            else if(ActionID == 33059) //ice
            {
                if (GetColor(Kokytos) == Color.LightningIce && Conf.ShowProtean)
                {
                    DisplayHide("In", false);
                }
                else
                {
                    DisplayHide("In");
                }
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category.EqualsAny(DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
            {
                DisplayHide(null);
            }
        }

        void DisplayHide(string? which, bool? laser = null)
        {
            foreach (var x in Controller.GetRegisteredElements()) x.Value.Enabled = false;
            sched?.Dispose();
            if (which != null)
            {
                Controller.GetElementByName(which).Enabled = true;
                if (laser == true)
                {
                    Controller.GetElementByName("BigLaser").Enabled = true;
                }
                if (laser == false)
                {
                    Controller.GetElementByName("SmallLaser").Enabled = true;
                }

                sched = new TickScheduler(() => DisplayHide(null), 5000);
            }
        }

        /*vfx/common/eff/m0842cast03c0g.avfx firewind
            vfx/common/eff/m0842cast01c0g.avfx icefire
            vfx/common/eff/m0842cast02c0g.avfx icethunder
            */
        enum Color { Unknown, FireIce, LightningIce, FireWind };
        Color GetColor(GameObject obj)
        {
            Color col = Color.Unknown;
            long age = long.MaxValue;
            if (AttachedInfo.TryGetVfx(obj, out var info))
            {
                foreach (var x in info)
                {
                    if (x.Value.Age < age)
                    {
                        if (x.Key == "vfx/common/eff/m0842cast01c0g.avfx")
                        {
                            col = Color.FireIce;
                            age = x.Value.Age;
                        }
                        else if (x.Key == "vfx/common/eff/m0842cast02c0g.avfx")
                        {
                            col = Color.LightningIce;
                            age = x.Value.Age;
                        }
                        else if (x.Key == "vfx/common/eff/m0842cast03c0g.avfx")
                        {
                            col = Color.FireWind;
                            age = x.Value.Age;
                        }
                    }
                }
            }
            return col;
        }

        Config Conf => Controller.GetConfig<Config>();
        public class Config : IEzConfig
        {
            public bool ShowProtean = true;
        }

        public override void OnSettingsDraw()
        {
            ImGui.Checkbox($"Show protean for self", ref Conf.ShowProtean);
            if (ImGui.CollapsingHeader("Debug"))
            {
                if(Kokytos != null)
                {
                    ImGuiEx.Text($"Kokytos color: {GetColor(Kokytos)}");
                }
            }
        }
    }
}
