using System;
using System.Collections.Generic;
using System.Text;

namespace Splatoon.Utility;

[AttributeUsage(AttributeTargets.Enum)]
public class MapEffectNamesAttribute : Attribute
{
    public string Bg;

    public MapEffectNamesAttribute(string bg)
    {
        Bg = bg ?? throw new ArgumentNullException(nameof(bg));
    }
}
