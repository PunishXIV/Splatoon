using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Memory;
using ECommons.ExcelServices;
using ECommons.EzHookManager;
using ECommons.MathHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Splatoon.Memory;

public unsafe class LogHooks 
{
    private LogHooks()
    {
        EzSignatureHelper.Initialize(this);
    }

    delegate nint ActorCastDelegate(uint sourceId, nint packetPtr);

    [EzHook("40 53 57 48 81 EC ?? ?? ?? ?? 48 8B FA 8B D1")]
    EzHook<ActorCastDelegate> ActorCastHook;
    private nint ActorCastDetour(uint sourceId, nint packetPtr)
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
        }
        catch(Exception e)
        {
            e.Log();
        }
        return ActorCastHook.Original(sourceId, packetPtr);
    }
}
