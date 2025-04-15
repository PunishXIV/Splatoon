using Dalamud.Game.ClientState.Objects.Types;
using ECommons.ExcelServices;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests
{
    public class AbilityIdTest : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => [];
        public override void OnActionEffect(uint ActionID, ushort animationID, ActionEffectType type, uint sourceID, ulong targetOID, uint damage)
        {
            if(sourceID.GetObject() is IBattleNpc b)
            {
                PluginLog.Debug($"Action {ActionID} from {b.Name} on {((uint)(targetOID)).GetObject()?.Name}");
            }
        }
    }
}
