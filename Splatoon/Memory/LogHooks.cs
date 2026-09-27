using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Memory;
using ECommons.ExcelServices;
using ECommons.EzHookManager;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.Network;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Splatoon.Memory;

public unsafe class LogHooks
{
    private LogHooks()
    {
        EzSignatureHelper.Initialize(this);
        ActorCastHook = new(PacketDispatcher.Addresses.HandleActorCastPacket.Value, ActorCastDetour);
    }

    private EzHook<PacketDispatcher.Delegates.HandleActorCastPacket> ActorCastHook;
    private void ActorCastDetour(uint sourceId, ActorCastPacket* packetPtr)
    {
        try
        {
            var packet = (PacketActorCast*)packetPtr;
            /*PluginLog.Debug($"""
                ActorCast:
                {ExcelActionHelper.GetActionName(packet->ActionID, true)}
                Rotation: {packet->RotationRadians} {packet->RotationRadians.RadToDeg()}
                {MemoryHelper.ReadRaw(packetPtr, sizeof(PacketActorCast)).ToHexString()}
                """);*/
            S.Projection.LastCast.GetOrCreate(sourceId)[packet->ActionDescriptor] = *packet;
            ScriptingProcessor.OnStartingCast(sourceId, packet);
        }
        catch(Exception e)
        {
            e.Log();
        }
        ActorCastHook.Original(sourceId, packetPtr);
    }
}
