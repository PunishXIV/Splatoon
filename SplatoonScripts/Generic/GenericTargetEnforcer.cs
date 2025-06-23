using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public class GenericTargetEnforcer : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [.. ForceTargetDataId.Keys];

    private static Dictionary<uint, uint[]> ForceTargetDataId = new()
    {
        [1045] = [208], //bowl of embers normal - nail
        [1037] = [125], //tam tararam deepcraft - mobs that make boss immune
        [1245] = [1410], //halatali - doctore
        [233] = [717],
    };

    public override void OnUpdate()
    {
        if(ForceTargetDataId.TryGetValue(Player.Territory, out var values))
        {
            if(!values.Contains(Svc.Targets.Target?.DataId ?? 0))
            {
                var obj = Svc.Objects.OrderBy(Player.DistanceTo).FirstOrDefault(x => values.Contains(x.DataId) && x.IsTargetable && !x.IsDead);
                if(obj != null && Player.DistanceTo(obj) < 25f)
                {
                    if(EzThrottler.Throttle("SetTargetForced"))
                    {
                        Svc.Targets.Target = obj;
                    }
                }
            }
        }
    }
}
