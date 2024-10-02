using ECommons.Logging;
using Splatoon.SplatoonScripting;
using Splatoon.Structures;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Tests;
internal class OnBuffEffectTest :SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => null;

    public override void OnGainBuffEffect(uint sourceId, IReadOnlyList<RecordedStatus> gainStatusInfos)
    {
        var gameObject = sourceId.GetObject();
        PluginLog.Information($"OnGainBuffEffect: [{gameObject.Name}({sourceId})] {string.Join(", ", gainStatusInfos)}");
    }

    public override void OnRemoveBuffEffect(uint sourceId, IReadOnlyList<RecordedStatus> removeStatusInfos)
    {
        var gameObject = sourceId.GetObject();
        PluginLog.Information($"OnRemoveBuffEffect: [{gameObject.Name}({sourceId})] {string.Join(", ", removeStatusInfos)}");
    }
}
