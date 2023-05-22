using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using ECommons.Hooks.ActionEffectTypes;
using Lumina.Excel.GeneratedSheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests
{
    public class Hooktest : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new();

        delegate byte IsRecipeCompletedDelegate(uint recipeID);
        [Signature("40 53 48 83 EC 20 8B D9 81 F9")]
        IsRecipeCompletedDelegate IsRecipeCompleted;

        byte Detour(uint a1)
        {
            var ret = hook.Original(a1);
            try
            {
                PluginLog.Information($"{Svc.Data.GetExcelSheet<Recipe>()?.GetRow(a1)?.ItemResult.Value?.Name}: {ret}");
            }
            catch(Exception ex) { }
            return ret;
        }

        public override void OnEnable()
        {
            SignatureHelper.Initialise(this);
            hook?.Enable();
        }

        public override void OnDisable()
        {
            hook?.Disable();
            hook?.Dispose();
        }
    }
}
