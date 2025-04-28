using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using ECommons;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests
{
    public unsafe class TetherProcessor2Test : SplatoonScript
    {
        //__int64 __fastcall sub_14072F1E0(__int64 a1, unsigned __int8 a2, char a3, char a4, char a5)
        private delegate long ProcessActorControlPacket(GameObject* a1, byte a2, byte a3, byte a4, byte a5);
        [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 57 48 83 EC 20 41 0F B6 E9 0F B6 DA", DetourName = nameof(ProcessActorControlPacketDetour))]
        private Hook<ProcessActorControlPacket> ProcessActorControlPacketHook;


        public override HashSet<uint> ValidTerritories => [];

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

        private long ProcessActorControlPacketDetour(GameObject* a1, byte a2, byte a3, byte a4, byte a5)
        {
            PluginLog.Information($"Tether lost: {GenericHelpers.Read(a1->Name)}, {a2:X8}, {a3:X8}, {a4:X8}, {a5:X8}");
            return ProcessActorControlPacketHook.Original(a1, a2, a3, a4, a5);
        }
    }
}
