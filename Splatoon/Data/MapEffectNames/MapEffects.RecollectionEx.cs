using System;
using System.Collections.Generic;
using System.Text;

namespace Splatoon.Data.MapEffectNames;

public static partial class MapEffects
{
    /// <summary>
    /// Note: 1 corresponds to element from North immediately clockwise, and then it goes further clockwise
    /// </summary>
    [MapEffectNames("ex5/01_xkt_x6/fld/x6fd/level/x6fd")]
    public enum RecollectionEx : uint
    {
        Inner_1 = 4,
        Inner_2 = 5,
        Inner_3 = 6,
        Inner_4 = 7,
        Inner_5 = 8,
        Inner_6 = 9,
        Inner_7 = 10,
        Inner_8 = 11,
        Outer_1 = 12,
        Outer_2 = 13,
        Outer_3 = 14,
        Outer_4 = 15,
        Outer_5 = 16,
        Outer_6 = 17,
        Outer_7 = 18,
        Outer_8 = 19,
    }
}