using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Structures;

public readonly record struct RecordedStatus
{
    public readonly uint StatusId;
    public readonly ushort Param;

    public RecordedStatus(uint statusId, ushort param)
    {
        StatusId = statusId;
        Param = param;
    }

    public override string ToString()
    {
        return $"{StatusId},{Param}";
    }
}