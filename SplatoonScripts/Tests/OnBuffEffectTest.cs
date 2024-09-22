using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests;
internal class OnBuffEffectTest : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => null;

    public override void OnGainBuffEffect(uint sourceId, List<uint> gainBuffIds)
    {
        var gameObject = sourceId.GetObject();
        PluginLog.Information($"OnGainBuffEffect: [{gameObject.Name}({sourceId})] {string.Join(", ", gainBuffIds)}");
    }

    public override void OnRemoveBuffEffect(uint sourceId, List<uint> removeBuffIds)
    {
        var gameObject = sourceId.GetObject();
        PluginLog.Information($"OnRemoveBuffEffect: [{gameObject.Name}({sourceId})] {string.Join(", ", removeBuffIds)}");
    }
}
