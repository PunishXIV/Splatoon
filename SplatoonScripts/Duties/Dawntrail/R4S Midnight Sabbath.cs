using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe class R4S_Midnight_Sabbath : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1232];
    public override Metadata? Metadata => new(1, "NightmareXIV");

    uint TowerID = 13562;

    public override void OnUpdate()
    {
        foreach(var x in Svc.Objects.OfType<IBattleNpc>().Where(x => x.NameId == 13562))
        {
            //PluginLog.Information($"{x.Struct()->OrnamentData.OrnamentId}");
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Source is IBattleNpc b && b.NameId == TowerID)
        {
            PluginLog.Information($"Cast: {ExcelActionHelper.GetActionName(set.Action, true)}");
        }
    }
}
