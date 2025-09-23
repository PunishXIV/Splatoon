using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;
public sealed class P4_Dives : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [Raids.Dragonsongs_Reprise_Ultimate];

    int DiveCnt;

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(Controller.Scene != 2)
        {
            DiveCnt = 0;
        }
    }

    public static List<IGameObject> SortQuad(List<IGameObject> points)
    {
        var sorted = points.OrderBy(p => p.Position.Y).ThenByDescending(p => p.Position.X).ToList();

        var upper = sorted.Take(2).OrderByDescending(p => p.Position.X).ToList();
        var lower = sorted.Skip(2).OrderByDescending(p => p.Position.X).ToList();

        return
        [
            upper[0], // upper right
            lower[0], // lower right
            lower[1], // lower left
            upper[1]  // upper left
        ];
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Source is not IPlayerCharacter && set.Action != null)
        {
            PluginLog.Information($"Action: {ExcelActionHelper.GetActionName(set.Action.Value.RowId, true)}");
        }
    }
}