using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.Commands;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.GameFunctions;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests
{
    public class ActorControlTest : SplatoonScript
    {
        public override HashSet<uint>? ValidTerritories => null;
        public override Metadata Metadata => new(1, "NightmareXIV");

        public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, ulong targetId, byte replaying)
        {
            PluginLog.Information($"Source: {sourceId.GetObject()}\ncmd: {command}\n{p1},{p2},{p3},{p4},{p5},{p6},{((uint)targetId).GetObject()},{replaying}");
        }
    }
}
