using ECommons.LanguageHelpers;

namespace Splatoon.Serializables;

public enum AlphaBlendMode
{
    None = 0,
    Add = 1,
    Max = 2,
}

public static class AlphaBlendModes
{
    public static readonly string[] Names =
    [
        "None".Loc(),
        "Add".Loc(),
        "Maximum".Loc(),
    ];

    public static readonly string[] Tooltips =
    [
        "Overlapping elements are not blended.\nWhatever is drawn last overwrites anything it overlaps.",
        "Overlapping elements are blended by adding their alpha values.\nThis makes it easier to see areas where elements overlap.",
        "Overlapping elements are blended using the most opaque alpha.\nThis makes it easier to see through overlapping areas.",
    ];

    public static readonly AlphaBlendMode[] Values = (AlphaBlendMode[])Enum.GetValues(typeof(AlphaBlendMode));
}
