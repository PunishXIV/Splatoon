namespace Splatoon.Structures;

public record struct VFXInfo
{
    public long SpawnTime;

    public long Age
    {
        get
        {
            return Environment.TickCount64 - SpawnTime;
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
