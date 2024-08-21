using ECommons.LanguageHelpers;

namespace Splatoon.Serializables;

public enum CastAnimationKind
{
    // These names can be changed.
    // These number values are used for serialization and must not be changed.
    Unspecified = 0,
    Pulse = 1,
    ColorShift = 2,
    Fill = 3,
}


public static class CastAnimations
{
    public static readonly string[] Names =
    [
        "Unspecified".Loc(),
        "Pulse".Loc(),
        "ColorShift".Loc(),
        "Fill".Loc(),
    ];

    public static readonly string[] Tooltips =
    [
        "The default type for new elements. No cast animation is specified.".Loc(),
        "Pulse with some frequency. This looks similar to the game's default VFX.".Loc(),
        "Change the element's color based on the cast progress.".Loc(),
        "Fill the element from start to end based on the cast progress.".Loc(),
    ];
}
