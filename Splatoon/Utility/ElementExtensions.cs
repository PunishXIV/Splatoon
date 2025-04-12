using Splatoon.RenderEngines;
using Splatoon.Serializables;

namespace Splatoon.Utility;
public static class ElementExtensions
{
    internal static float GetDefaultFillIntensity(this Element e)
    {
        // Generate a default fill transparency based on the stroke transparency and fillstep relative to their defaults.
        var strokeAlpha = (e.color >> 24);
        const uint defaultStrokeAlpha = 0xC8;
        var transparencyFromStroke = (float)strokeAlpha / defaultStrokeAlpha;
        var transparencyFromFillStep = 0.5f / e.FillStep;
        if(e.type.EqualsAny(0, 1))
        {
            // Donut
            if(e.Donut > 0)
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
        if(e.type.EqualsAny(4, 5))
        {
            transparencyFromFillStep *= 4;
        }
        var fillAlpha = Math.Clamp((uint)(0x45 * transparencyFromFillStep * transparencyFromStroke), 0x19, 0x64);
        var fillIntensity = (float)fillAlpha / strokeAlpha;
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
        var needsPolygonalFillMigration = e.fillIntensity == null;
        var fillIntensity = e.fillIntensity ?? e.GetDefaultFillIntensity();
        if(needsPolygonalFillMigration)
        {
            // Non-donut circles are the only shapes that don't need fill migration because they had functioning Fill.
            var isCircle = e.type.EqualsAny(0, 1) && e.Donut == 0;
            if(!isCircle)
            {
                e.Filled = true;
            }
        }

        var originFillColor = e.originFillColor ?? Colors.MultiplyAlpha(e.color, fillIntensity);
        var endFillColor = e.endFillColor ?? Colors.MultiplyAlpha(e.color, fillIntensity);

        return new DisplayStyle(e.color, e.thicc, fillIntensity, originFillColor, endFillColor, e.Filled, e.overrideFillColor, 0, e.GetAnimationStyle());
    }


    public static DisplayStyle GetDisplayStyleWithOverride(this Element e, IGameObject go = null)
    {
        var style = e.GetDisplayStyle();

        if(P.Config.StyleOverrides.TryGetValue(e.mechanicType, out var value))
        {
            (var overrideEnabled, var overrideStyle) = value;
            if(overrideEnabled)
            {
                style = overrideStyle;
            }
        }
        if(!style.overrideFillColor)
        {
            var defaultColor = Colors.MultiplyAlpha(style.strokeColor, style.fillIntensity);
            style.originFillColor = defaultColor;
            style.endFillColor = defaultColor;
        }
        style.animation = e.GetAnimationStyle();
        style.castFraction = e.refActorRequireCast ? LayoutUtils.CastFraction(e, go) : 0f;
        if(style.animation.kind is CastAnimationKind.ColorShift)
        {
            style.originFillColor = P.Config.ClampFillColorAlpha(Colors.Lerp(style.originFillColor, style.animation.color, style.castFraction));
            style.endFillColor = P.Config.ClampFillColorAlpha(Colors.Lerp(style.endFillColor, style.animation.color, style.castFraction));
        }
        else
        {
            style.originFillColor = P.Config.ClampFillColorAlpha(style.originFillColor);
            style.endFillColor = P.Config.ClampFillColorAlpha(style.endFillColor);
        }
        return style;
    }

    public static bool IsStrokeVisible(this DisplayStyle style)
    {
        return style.strokeThickness > 0 && (style.strokeColor & 0xFF000000) > 0;
    }

    public static bool IsDangerous(this Element e)
    {
        return e.mechanicType == MechanicType.Danger;
    }

    public static AnimationStyle GetAnimationStyle(this Element e)
    {
        return new(e.castAnimation, e.animationColor, e.pulseSize, e.pulseFrequency);
    }

    public static float EffectiveLength(this Element e)
    {
        if(e.type.EqualsAny(0, 1))
        {
            if(e.Donut > 0)
                return e.Donut;
            return e.radius;
        }
        if(e.type.EqualsAny(2, 3))
        {
            return (new Vector3(e.refX, e.refY, e.refZ) - new Vector3(e.offX, e.offY, e.offZ)).Length();
        }
        if(e.type.EqualsAny(4, 5))
        {
            return e.radius;
        }
        return 0;
    }

    public static RenderEngineKind ConfiguredRenderEngineKind(this Element e)
    {
        if(e.RenderEngineKind != RenderEngineKind.Unspecified)
            return e.RenderEngineKind;
        if(P.Config.RenderEngineKind != RenderEngineKind.Unspecified)
            return P.Config.RenderEngineKind;
        return RenderEngineKind.DirectX11;
    }
}
