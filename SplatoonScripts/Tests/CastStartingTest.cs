using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests;
internal class CastStartingTest :SplatoonScript
{
    public override HashSet<uint> ValidTerritories => new();

    public override void OnStartingCast(IBattleChara battleChar, uint castId)
    {
        if (battleChar.NameId != 0)
        {
            PluginLog.Information($"Starting cast test: {battleChar.Name} ({battleChar.NameId}) is casting {castId}");
        }
    }
}
