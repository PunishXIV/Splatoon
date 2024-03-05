using ECommons.LanguageHelpers;

namespace Splatoon.Serializables;

public enum LineEnd
{
    None = 0,
    Arrow = 1,
}

public static class LineEnds
{
    public static readonly string[] Names =
    [
        "None".Loc(),
        "Arrow".Loc(),
    ];

    public static readonly string[] Tooltips =
    [
        "No line end caps.".Loc(),
        "Arrow pointing out.".Loc(),
    ];

    public static readonly LineEnd[] Values = (LineEnd[])Enum.GetValues(typeof(LineEnd));
}
