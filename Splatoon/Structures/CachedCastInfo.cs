namespace Splatoon.Structures;

public record struct CachedCastInfo
{
    public uint ID;
    public long StartTime;

    public CachedCastInfo(uint iD, long startTime)
    {
        ID = iD;
        StartTime = startTime;
    }

    public float StartTimeF
    {
        get
        {
            return (float)StartTime / 1000f;
        }
    }

    public long Age
    {
        get
        {
            return Environment.TickCount64 - StartTime;
        }
    }

    public float AgeF
    {
        get
        {
            return (float)Age / 1000f;
        }
    }
}
