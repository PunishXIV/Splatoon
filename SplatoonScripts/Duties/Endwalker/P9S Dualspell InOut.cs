using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Schedulers;
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
        TickScheduler? sched = null;

        public override void OnSetup()
        {
            Controller.RegisterElementFromCode("In", "{\"Name\":\"In\",\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":8.0,\"Donut\":12.0,\"color\":3372220160,\"thicc\":4.0}");
            Controller.RegisterElementFromCode("Out", "{\"Name\":\"Out\",\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":14.0,\"Donut\":6.0,\"color\":3372220160,\"thicc\":4.0}");

        }

        public override void OnActionEffect(uint ActionID, ushort animationID, ActionEffectType type, uint sourceID, ulong targetOID, uint damage)
        {
            if(ActionID == 33058 || ActionID == 33116) //flame/thunder
            {
                DisplayHide("Out");
            }
            else if(ActionID == 33059) //ice
            {
                DisplayHide("In");
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category.EqualsAny(DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
            {
                DisplayHide(null);
            }
        }

        void DisplayHide(string? which)
        {
            foreach (var x in Controller.GetRegisteredElements()) x.Value.Enabled = false;
            sched?.Dispose();
            if (which != null)
            {
                Controller.GetElementByName(which).Enabled = true;
                sched = new TickScheduler(() => DisplayHide(null), 5000);
            }
        }
    }
}
