using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using Splatoon.Structures;
using System;
using System.Collections.Generic;
using System.Text;

using ECommons.DalamudServices.Legacy;

namespace SplatoonScriptsOfficial.Tests;

public class TrapTest : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [];

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action?.RowId == 6275)
        {
            Splatoon.Splatoon.P.AddDynamicElements($"Trap{Environment.TickCount64}", [new(0) {
                refX = set.Position.X,
                refY = set.Position.Z,
                refZ = set.Position.Y,
                overlayText = "You're busted sir",
            }], [Environment.TickCount64+ 10000, (long)DestroyCondition.TERRITORY_CHANGE]);
        }
    }
}
