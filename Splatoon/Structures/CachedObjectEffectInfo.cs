namespace Splatoon.Structures;

public record struct CachedObjectEffectInfo
{
    public long StartTime;
    public uint data1;
    public uint data2;

    public CachedObjectEffectInfo(long startTime, uint data1, uint data2)
    {
        StartTime = startTime;
        this.data1 = data1;
        this.data2 = data2;
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
