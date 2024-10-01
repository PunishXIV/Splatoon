using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Structures;

public readonly record struct RecordedStatus
{
    public readonly uint StatusId;
    public readonly byte StackCount;
    public readonly ushort Param;

    public RecordedStatus(uint statusId, byte stackCount, ushort param)
    {
        StatusId = statusId;
        StackCount = stackCount;
        Param = param;
    }

    public override string ToString()
    {
        return $"{StatusId},{StackCount},{Param}";
    }
}