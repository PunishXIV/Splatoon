using Dalamud.Hooking;
using ECommons.Logging;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommons.GameFunctions;

namespace SplatoonScriptsOfficial.Tests
{
    public class ActorControlTest : SplatoonScript
    {
        delegate void ProcessActorControlPacket(uint a1, uint a2, uint a3, uint a4, uint a5, uint a6, int a7, uint a8, long a9, byte a10);
        [Signature("40 55 53 41 55 41 56 41 57 48 8D AC 24", DetourName =nameof(ProcessActorControlPacketDetour))]
        Hook<ProcessActorControlPacket> ProcessActorControlPacketHook;


        public override HashSet<uint> ValidTerritories => new();

        public override void OnEnable()
        {
            SignatureHelper.Initialise(this);
            ProcessActorControlPacketHook.Enable();
            Svc.Chat.Print($"ProcessActorControlPacketHook.Address: {ProcessActorControlPacketHook.Address:X16}");
        }

        public override void OnDisable()
        {
            ProcessActorControlPacketHook.Disable();
            ProcessActorControlPacketHook.Dispose();
        }

        void ProcessActorControlPacketDetour(uint a1, uint a2, uint a3, uint a4, uint a5, uint a6, int a7, uint a8, long a9, byte a10)
        {
            try
            {
                var o = a1.GetObject();
                if (o != null && !o.IsTargetable() && o.Name.ToString().Contains("Omega"))
                {
                    PluginLog.Information($"ActorControlPacket: {a1:X8}, {a2:X8}, {a3:X8}, {a4:X8}, {a5:X8}, {a6:X8}, {a7:X8}, {a8:X8}, {a9:X16}, {a10:X2}");
                }
            }
            catch(Exception e) { }
            ProcessActorControlPacketHook.Original(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10);
        }
    }
}
