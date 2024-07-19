using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Common.Lua;
using Splatoon.Serializables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Utility;
public static class ElementExtensions
{
    internal static float GetDefaultFillIntensity(this Element e)
    {
        // Generate a default fill transparency based on the stroke transparency and fillstep relative to their defaults.
        uint strokeAlpha = (e.color >> 24);
        const uint defaultStrokeAlpha = 0xC8;
        float transparencyFromStroke = (float)strokeAlpha / defaultStrokeAlpha;
        float transparencyFromFillStep = 0.5f / e.FillStep;
        if (e.type.EqualsAny(0, 1))
        {
            // Donut
            if (e.Donut > 0)
            {
                transparencyFromFillStep /= 2;
            }
            // Circle
            else
            {
                transparencyFromFillStep *= 2;
            }
        }
        // Cone
        if (e.type.EqualsAny(4, 5))
        {
            transparencyFromFillStep *= 4;
        }
        uint fillAlpha = Math.Clamp((uint)(0x45 * transparencyFromFillStep * transparencyFromStroke), 0x19, 0x64);
        float fillIntensity = (float)fillAlpha / strokeAlpha;
        return Math.Clamp(fillIntensity, 0, 1);
    }

    public static void SetDisplayStyle(this Element e, DisplayStyle value)
    {
        e.color = value.strokeColor;
        e.thicc = value.strokeThickness;
        e.Filled = value.filled;
        e.overrideFillColor = value.overrideFillColor;
        e.fillIntensity = value.fillIntensity;
        e.originFillColor = value.originFillColor;
        e.endFillColor = value.endFillColor;
    }
    
    public static DisplayStyle GetDisplayStyle(this Element e)
    {
        // Most elements used line fill with Filled = false and need fill migration.
        bool needsPolygonalFillMigration = e.fillIntensity == null;
        float fillIntensity = e.fillIntensity ?? e.GetDefaultFillIntensity();
        if (needsPolygonalFillMigration)
        {
            // Non-donut circles are the only shapes that don't need fill migration because they had functioning Fill.
            bool isCircle = e.type.EqualsAny(0, 1) && e.Donut == 0;
            if (!isCircle)
            {
                e.Filled = true;
            }
        }

        uint originFillColor = e.originFillColor ?? Colors.MultiplyAlpha(e.color, fillIntensity);
        uint endFillColor = e.endFillColor ?? Colors.MultiplyAlpha(e.color, fillIntensity);

        return new DisplayStyle(e.color, e.thicc, fillIntensity, originFillColor, endFillColor, e.Filled, e.overrideFillColor);
    }


    public static DisplayStyle GetDisplayStyleWithOverride(this Element e)
    {
        DisplayStyle style = e.GetDisplayStyle();

        if (P.Config.StyleOverrides.TryGetValue(e.mechanicType, out var value))
        {
            (var overrideEnabled, var overrideStyle) = value;
            if (overrideEnabled)
            {
                style = overrideStyle;
            }
        }
        if (!style.overrideFillColor)
        {
            uint defaultColor = Colors.MultiplyAlpha(style.strokeColor, style.fillIntensity);
            style.originFillColor = defaultColor;
            style.endFillColor = defaultColor;
        }

        style.originFillColor = P.Config.ClampFillColorAlpha(style.originFillColor);
        style.endFillColor = P.Config.ClampFillColorAlpha(style.endFillColor);
        return style;
    }

    public static bool IsDangerous(this Element e)
    {
        return e.mechanicType == MechanicType.Danger;
    }
}
