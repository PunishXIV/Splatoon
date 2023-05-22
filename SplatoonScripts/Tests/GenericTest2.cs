using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests
{
    public unsafe class GenericTest2 : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new();

        public override void OnEnable()
        {
            Svc.GameNetwork.NetworkMessage += GameNetwork_NetworkMessage;
            SignatureHelper.Initialise(this);
            *(int*)forceDisableMovementPtr += 1;
        }
        public override void OnDisable()
        {
            Svc.GameNetwork.NetworkMessage -= GameNetwork_NetworkMessage;
            *(int*)forceDisableMovementPtr -= 1;
        }

        private void GameNetwork_NetworkMessage(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, Dalamud.Game.Network.NetworkMessageDirection direction)
        {
            if(direction == Dalamud.Game.Network.NetworkMessageDirection.ZoneUp && opCode == 0x25f)
            {
                DuoLog.Information($"Moving");
                //if (Debugger.IsAttached) Debugger.Break();
            }
        }
        [Signature("F3 0F 10 05 ?? ?? ?? ?? 0F 2E C6 0F 8A", Offset = 4, ScanType = ScanType.StaticAddress, Fallibility = Fallibility.Infallible)]
        private static nint forceDisableMovementPtr;
    }
}
