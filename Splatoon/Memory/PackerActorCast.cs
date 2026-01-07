using FFXIVClientStructs;
using Splatoon.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Splatoon.Memory;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct PacketActorCast
{
    [FieldOffset(0)]
    public ushort ActionID;

    [FieldOffset(2)]
    public byte ActionType;

    public readonly ActionDescriptor ActionDescriptor => new(this);

    [FieldOffset(3)]
    [Obsolete("Unknown")]
    public byte Unknown;

    [FieldOffset(4)]
    [Obsolete("Unknown")]
    public uint Unknown1; // also action ID

    [FieldOffset(8)]
    public float CastTime;

    [FieldOffset(12)]
    public uint TargetID;

    [FieldOffset(16)]
    public ushort Rotation;

    public readonly float RotationRadians => ((Rotation * 0.0095875263f) * 0.0099999998f) - MathF.PI;

    [FieldOffset(20)]
    [Obsolete("Unknown")]
    public uint Unknown2;

    [FieldOffset(24)]
    public ushort PosX;

    [FieldOffset(26)]
    public ushort PosY;

    [FieldOffset(28)]
    public ushort PosZ;

    [FieldOffset(30)]
    [Obsolete("Unknown")]
    public ushort Unknown3;
}
