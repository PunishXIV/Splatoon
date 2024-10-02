namespace Splatoon.Structures;

public readonly record struct RecordedStatus
{
    public readonly string StatusName;
    public readonly uint StatusId;
    public readonly byte StackCount;
    public readonly ushort Param;

    public RecordedStatus(string statusName, uint statusId, byte stackCount, ushort param)
    {
        StatusName = statusName;
        StatusId = statusId;
        StackCount = stackCount;
        Param = param;
    }

    public override string ToString()
    {
        return $"{StatusId},{StackCount},{Param}";
    }

    public string ToStringWithName()
    {
        return $"{StatusName}({StatusId}),{StackCount},{Param}";
    }
}