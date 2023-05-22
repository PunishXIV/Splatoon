

namespace Splatoon.Structures;

public class DynamicElement
{
    public string Name;
    public Layout[] Layouts;
    public Element[] Elements;
    public long[] DestroyTime; //-1 combat exit / -2 terr change
}

public enum DestroyCondition : long
{
    NEVER = 0,
    COMBAT_EXIT = -1,
    TERRITORY_CHANGE = -2,
}
