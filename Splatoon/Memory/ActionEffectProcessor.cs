using ECommons.Hooks.ActionEffectTypes;
using Splatoon.Modules;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Memory
{
    internal unsafe static class ActionEffectProcessor
    {
        internal static void ProcessActionEffect(uint ActionID, ushort animationID, ActionEffectType type, uint sourceID, ulong targetOID, uint damage)
        {
            ScriptingProcessor.OnActionEffect(ActionID, animationID, type, sourceID, targetOID, damage);
        }
    }
}
