using ECommons;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Modules;
internal static class PhaseUpdater
{
    public static void UpdatePhaseIfNeeded()
    {
        if(Svc.ClientState.TerritoryType == Raids.Dragonsongs_Reprise_Ultimate)
        {
            foreach(var x in Svc.Objects)
            {
                if((x.DataId == 12604) && x.IsTargetable)
                {
                    if(P.Phase != 2)
                    {
                        P.Phase = 2;
                        PluginLog.Debug($"Forcing phase to phase 2 (framework update)");
                    }
                }
            }
        }
    }

    public static void UpdateFromDirector(DirectorUpdateCategory cat)
    {
        if(Svc.ClientState.TerritoryType == Raids.Dragonsongs_Reprise_Ultimate)
        {
            if(cat == DirectorUpdateCategory.Commence || cat == DirectorUpdateCategory.Recommence)
            {
                if(Svc.Objects.Any(x => x.DataId.EqualsAny(12601u, 12602u, 12603u) && x.IsTargetable))
                {
                    P.Phase = 1;
                    PluginLog.Debug($"Forcing phase to phase 1 (director update)");
                }
            }
        }
    }
}