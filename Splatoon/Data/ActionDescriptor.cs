using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace Splatoon.Data;

public readonly record struct ActionDescriptor
{
    public readonly ActionType Type;
    public readonly uint Id;

    public ActionDescriptor()
    {
    }

    public ActionDescriptor(ActionType type, uint id)
    {
        Type = type;
        Id = id;
    }

    public ActionDescriptor(int type, uint id)
    {
        Type = (ActionType)type;
        Id = id;
    }

    public ActionDescriptor(uint id)
    {
        Type = ActionType.Action;
        Id = id;
    }

    public ActionDescriptor(PacketActorCast packet)
    {
        Type = (ActionType)packet.ActionType;
        Id = packet.ActionID;
    }
}
