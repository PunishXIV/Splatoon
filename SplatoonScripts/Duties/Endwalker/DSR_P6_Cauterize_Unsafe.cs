using Dalamud.Logging;
using ECommons;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Endwalker;

public class DSR_P6_Cauterize_Unsafe : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => new() { Raids.Dragonsongs_Reprise_Ultimate };

    public override Metadata? Metadata => new(2, "NightmareXIV");

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Nidhogg", "{\"Name\":\"\",\"type\":3,\"Enabled\":false,\"refY\":60.0,\"radius\":11.0,\"color\":2013266175,\"thicc\":1.0,\"refActorNPCNameID\":3458,\"refActorRequireCast\":true,\"refActorCastId\":[27966],\"refActorComparisonType\":6,\"includeRotation\":true}");
        Controller.RegisterElementFromCode("Hraesvelgr", "{\"Name\":\"\",\"type\":3,\"Enabled\":false,\"refY\":60.0,\"radius\":11.0,\"color\":2013266175,\"thicc\":1.0,\"refActorNPCNameID\":4954,\"refActorRequireCast\":true,\"refActorCastId\":[27967],\"refActorComparisonType\":6,\"includeRotation\":true}");
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        {
            //  Freezing (2899), Remains = 3.0, Param = 0, Count = 0
            if (Player.Object.StatusList.Any(x => x.StatusId == 2899) && Controller.TryGetElementByName("Hraesvelgr", out var e))
            {
                e.Enabled = true;
            }
        }
        {
            //  Boiling (2898), Remains = 3.0, Param = 0, Count = 0
            if (Player.Object.StatusList.Any(x => x.StatusId == 2898) && Controller.TryGetElementByName("Nidhogg", out var e))
            {
                e.Enabled = true;
            }
        }
    }
}
