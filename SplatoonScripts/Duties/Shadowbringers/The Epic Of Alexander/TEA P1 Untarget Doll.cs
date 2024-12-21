using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Shadowbringers.The_Epic_Of_Alexander;
public class TEA_P1_Untarget_Doll : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [Raids.The_Epic_of_Alexander_Ultimate];

    public override void OnUpdate()
    {
        if(Svc.Targets.Target is IBattleNpc b && b.NameId.EqualsAny<uint>(3759, 9214) && ((float)b.CurrentHp / (float)b.MaxHp) < 0.24f)
        {
            Svc.Targets.Target = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.IsTargetable && !x.IsDead && x.NameId.EqualsAny<uint>(3765, 9211, 9212));
        }
    }
}
