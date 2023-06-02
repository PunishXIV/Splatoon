using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P12S_Limit_Cut : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1154 };
        public override Metadata? Metadata => new(1, "NightmareXIV");
        const uint Puddle = 33527;
        const uint Laser = 33520;
        bool mechanicActive = false;
        int puddleNum = 0;
        int laserNum = 0;

        Element EPuddle = null!;
        Element ELaser = null!;

        public override void OnSetup()
        {
            EPuddle = Controller.RegisterElementFromCode("Puddle", "{\"Name\":\"Puddle\",\"type\":1,\"Enabled\":false,\"radius\":4.5,\"Donut\":0.5,\"overlayBGColor\":4278190335,\"overlayTextColor\":4294967295,\"overlayVOffset\":1.5,\"thicc\":3.0,\"overlayText\":\"Puddles!\",\"refActorType\":1}");
            ELaser = Controller.RegisterElementFromCode("Laser", "{\"Name\":\"Laser\",\"type\":1,\"Enabled\":false,\"overlayBGColor\":4278252031,\"overlayTextColor\":4278190080,\"overlayFScale\":2.0,\"thicc\":0.0,\"overlayText\":\"BAIT LASER\",\"refActorType\":1}");
        }

        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
        }

        public override void OnDisable()
        {
            ActionEffect.ActionEffectEvent -= ActionEffect_ActionEffectEvent;
        }

        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            if (vfxPath.StartsWith("vfx/lockon/eff/sph_lockon2_num0"))
            {
                mechanicActive = true;
                puddleNum = 0;
                laserNum = 0;
                //DuoLog.Information($"Mechanic starts");
            }
        }

        public override void OnUpdate()
        {
            EPuddle.Enabled = false;
            ELaser.Enabled = false;
            if (mechanicActive)
            {
                var myNum = GetMyNumber();
                if (myNum.EqualsAny(1, 2, 3, 4))
                {
                    if (puddleNum < 4)
                    {
                        //take puddle
                        EPuddle.Enabled = true;
                    }
                    else
                    {
                        //puddle complete
                    }
                }
                else
                {
                    if (puddleNum >= 4)
                    {
                        //take puddle
                        EPuddle.Enabled = true;
                    }
                    else
                    {
                        //puddle not yet started
                    }
                }
                //57681324
                if (myNum.EqualsAny(5, 7) && laserNum == 0)
                {
                    //laser
                    ELaser.Enabled = true;
                }
                if (myNum.EqualsAny(6, 8) && laserNum == 2)
                {
                    //laser
                    ELaser.Enabled = true;
                }
                if (myNum.EqualsAny(1, 3) && laserNum == 4)
                {
                    //laser
                    ELaser.Enabled = true;
                }
                if (myNum.EqualsAny(2, 4) && laserNum == 6)
                {
                    //laser
                    ELaser.Enabled = true;
                }
                if (ELaser.Enabled)
                {
                    ELaser.overlayBGColor = GradientColor.Get(ImGuiColors.DalamudRed, ImGuiColors.DalamudYellow).ToUint();
                }
                if ((puddleNum >= 8 && laserNum >= 8))
                {
                    mechanicActive = false;
                    //DuoLog.Information($"Mechanic ends {puddleNum} {laserNum}");
                }
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category.EqualsAny(DirectorUpdateCategory.Wipe, DirectorUpdateCategory.Recommence))
            {
                mechanicActive = false;
            }
        }

        int GetMyNumber()
        {
            if(AttachedInfo.VFXInfos.TryGetValue(Svc.ClientState.LocalPlayer.Address, out var info))
            {
                if(info.OrderBy(x => x.Value.Age).TryGetFirst(x => x.Key.StartsWith("vfx/lockon/eff/sph_lockon2_num0"), out var effect))
                {
                    return int.Parse(effect.Key.Replace("vfx/lockon/eff/sph_lockon2_num0", "")[0].ToString());
                }
            }
            return 0;
        }

        private void ActionEffect_ActionEffectEvent(ActionEffectSet set)
        {
            if (!mechanicActive) return;
            if(set.Source?.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
            {
                if(set.Action.RowId == Puddle)
                {
                    //DuoLog.Information($"Puddle on {set.Target?.Name}");
                    puddleNum++;
                }
                if(set.Action.RowId == Laser)
                {
                    //DuoLog.Information($"Laser");
                    laserNum++;
                }
                //DuoLog.Information($"Cast: {set.Action.RowId} {set.Action.Name} on {set.Target}");
            }
        }
    }
}
