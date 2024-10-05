namespace Splatoon.Structures;

public readonly record struct RecordedStatus
{
    public readonly string StatusName;
    public readonly uint StatusId;
    public readonly ushort Param;

    public RecordedStatus(string statusName, uint statusId, byte stackCount, ushort param)
    {
        StatusName = statusName;
        StatusId = statusId;
        Param = param;
    }

    public override string ToString()
    {
        return $"{StatusId},{Param}";
    }

    public string ToStringWithName()
    {
        return $"{StatusName}({StatusId}),{Param}";
    }
}