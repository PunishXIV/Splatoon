using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Tests
{
    public unsafe class GenericTest2 : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => [];

        public override void OnEnable()
        {
            Svc.GameNetwork.NetworkMessage += GameNetwork_NetworkMessage;
            SignatureHelper.Initialise(this);
        }
        public override void OnDisable()
        {
            Svc.GameNetwork.NetworkMessage -= GameNetwork_NetworkMessage;
        }

        private void GameNetwork_NetworkMessage(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, Dalamud.Game.Network.NetworkMessageDirection direction)
        {
            PluginLog.Information($"opCode: {opCode}, dir: {direction}");
        }
    }
}
