using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests;
internal class CastStartingTest : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => [];

    public override void OnStartingCast(uint target, uint castId)
    {
        if(target.TryGetObject(out var obj) && obj is IBattleChara battleChar)
        {
            PluginLog.Information($"Starting cast test: {battleChar.Name} ({battleChar.NameId}) is casting {castId}");
        }
    }
}
