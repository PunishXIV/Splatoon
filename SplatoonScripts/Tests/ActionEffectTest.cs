using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests;
public class ActionEffectTest : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = new();

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Target.Address == Player.Object.Address)
        {
            foreach (var effect in set.TargetEffects)
            {
                for (int i = 0; i < set.Header.TargetCount; i++)
                {
                    DuoLog.Information($"{effect[i]}={effect[i].Damage}/{effect[i].mult}");
                }
            }
        }
    }
}
