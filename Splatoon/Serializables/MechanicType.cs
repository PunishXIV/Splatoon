using ECommons.LanguageHelpers;

namespace Splatoon.Serializables;

public enum MechanicType
{
    // These names can be changed.
    // These number values are used for serialization and must not be changed.
    Unspecified = 0,
    Danger = 1,
    Safe = 2,
    Soak = 3,
    Gaze = 4,
    Knockback = 5,
    Information = 6,
}


public static class MechanicTypes
{
    public static readonly string[] Names =
    [
        "Unspecified".Loc(),
        "Danger".Loc(),
        "Safe".Loc(),
        "Soak".Loc(),
        "Gaze".Loc(),
        "Knockback".Loc(),
        "Information".Loc(),
    ];

    public static readonly string[] Tooltips =
    [
        "The default type for new elements. Use this if none of the other types make sense.",
        "Danger zones. Typically this is avoidable AOE damage.",
        "Safe zones. This zone should be safe from avoidable damage.",
        "Soakable damage zones. Typically this is unavoidable AOE damage such as stacks, towers, or defamations.",
        "Gaze zones. When in the zone or tethered you must look away from the origin.",
        "Knockback zones. When in the zone you will be pushed. Should be paired with a Knockback tether.",
        "Useful information without an associated hazard.",
    ];

    public static readonly MechanicType[] Values = (MechanicType[])Enum.GetValues(typeof(MechanicType));

    public static readonly Dictionary<MechanicType, DisplayStyle> DefaultMechanicColors = new()
    {
         { MechanicType.Unspecified, new(0xC8006FFF, 2f, 0.3f, 0x45006FFF, 0x45006FFF) },
         { MechanicType.Danger, new(0xC80000FF, 2f, 0.3f, 0x450000FF, 0x450000FF) },
         { MechanicType.Safe, new(0xC800D114, 2f, 0.3f, 0x4500D114, 0x4500D114) },
         { MechanicType.Soak, new(0xC8FF9000, 2f, 0.3f, 0x45FF9000, 0x45FF9000) },
         { MechanicType.Gaze, new(0xC8A000D6, 2f, 0.3f, 0x45A000D6, 0x45A000D6) },
         { MechanicType.Knockback, new(0xC800F7FF, 2f, 0.3f, 0x4500F7FF, 0x4500F7FF) },
    };

    public static bool CanOverride(MechanicType type)
    {
        // Information elements cannot have their style overridden
        return type != MechanicType.Information;
    }
}
