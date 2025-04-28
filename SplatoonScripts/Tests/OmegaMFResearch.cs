﻿using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons;
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
    public class OmegaMFResearch : SplatoonScript
    {
        private delegate void ProcessActorControlPacket(uint a1, uint a2, uint a3, uint a4, uint a5, uint a6, int a7, uint a8, long a9, byte a10);
        [Signature("40 55 53 41 55 41 56 41 57 48 8D AC 24", DetourName = nameof(ProcessActorControlPacketDetour))]
        private Hook<ProcessActorControlPacket> ProcessActorControlPacketHook;


        public override HashSet<uint> ValidTerritories => [];
        private Dictionary<uint, List<string>> Values = [];
        private bool Mechanic = false;

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

        public override void OnMessage(string Message)
        {
            if(Message.Contains("(7635>31550)"))
            {
                DuoLog.Information($"Starts casting {Environment.TickCount64}");
                Values.Clear();
                DuoLog.Information($"Init");
                Mechanic = true;
            }
        }

        public override void OnUpdate()
        {
            if(Mechanic)
            {
                var casters = Svc.Objects.Where(x => x is IBattleNpc b && !b.IsTargetable() && b.IsCharacterVisible() && b.IsCasting).Cast<IBattleNpc>();
                foreach(var x in casters)
                {
                    DuoLog.Information($"Cast {x} - {Environment.TickCount64}");
                    if(Values.TryGetValue(x.EntityId, out var coll))
                    {
                        foreach(var z in coll)
                        {
                            DuoLog.Information(z);
                        }
                    }
                    Mechanic = false;
                }

            }
        }

        private void ProcessActorControlPacketDetour(uint a1, uint a2, uint a3, uint a4, uint a5, uint a6, int a7, uint a8, long a9, byte a10)
        {
            try
            {
                if(a2 == 0x3F)
                {
                    DuoLog.Information($"Decided: {Environment.TickCount64}");
                }
                PluginLog.Information($"ActorControlPacket: {a1:X8}, {a2:X8}, {a3:X8}, {a4:X8}, {a5:X8}, {a6:X8}, {a7:X8}, {a8:X8}, {a9:X16}, {a10:X2}");
                if(!Values.ContainsKey(a1))
                {
                    Values[a1] = [];
                }
                Values[a1].Add($"{Environment.TickCount64} - {a1:X8}, {a2:X8}, {a3:X8}, {a4:X8}, {a5:X8}, {a6:X8}, {a7:X8}, {a8:X8}, {a9:X16}, {a10:X2}");
            }
            catch(Exception e) { e.Log(); }
            ProcessActorControlPacketHook.Original(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10);
        }
    }
}
