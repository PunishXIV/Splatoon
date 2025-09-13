using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests;
public unsafe class ActionEffectTest : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [];
    public override Metadata Metadata => new(1, "NightmareXIV");

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Source is not IPlayerCharacter)
        {
            PluginLog.Information($"Cast {set.Action?.RowId} on {set.Source}");
        }
        if(set.Target?.Address == Player.Object.Address)
        {
            foreach(var effect in set.TargetEffects)
            {
                for(var i = 0; i < set.Header.TargetCount; i++)
                {
                    //PluginLog.Information($"{effect[i]}={effect[i].Damage}/{effect[i].mult}");
                }
            }
        }
    }
}
