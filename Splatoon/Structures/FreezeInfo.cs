namespace Splatoon.Structures;

public class FreezeInfo
{
    public List<FreezeState> States = new();
    public long AllowRefreezeAt;

    public bool CanDisplay()
    {
        return Environment.TickCount64 > AllowRefreezeAt;
    }
}

public class FreezeState
{
    public HashSet<DisplayObject> Objects;
    public long ShowUntil;
    public long ShowAt = 0;

    public bool IsActive()
    {
        return ShowUntil > Environment.TickCount64 && Environment.TickCount64 >= ShowAt;
    }

    public bool IsExpired()
    {
        return ShowUntil < Environment.TickCount64;
    }
}
