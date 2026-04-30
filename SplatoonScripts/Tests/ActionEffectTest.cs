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

using ECommons.DalamudServices.Legacy;

namespace SplatoonScriptsOfficial.Tests;
public unsafe class ActionEffectTest : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [];
    public override Metadata Metadata => new(1, "NightmareXIV");

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Source is IPlayerCharacter pc)
        {
            PluginLog.Information($"""
                Cast
                {set.Header.ActionType} @ {set.Header.ActionID}
                """);
        }
    }
}
