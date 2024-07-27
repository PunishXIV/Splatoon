namespace Splatoon.Serializables;

public struct DisplayStyle(
    uint strokeColor,
    float strokeThickness,
    float fillIntensity,
    uint originFillColor,
    uint endFillColor,
    bool filled = true,
    bool overrideFillColor = false,
    float castFraction = 0,
    AnimationStyle animation = default)
{
    public uint strokeColor = strokeColor;
    public float strokeThickness = strokeThickness;
    public float fillIntensity = fillIntensity;
    public uint originFillColor = originFillColor;
    public uint endFillColor = endFillColor;
    public bool filled = filled;
    public bool overrideFillColor = overrideFillColor;
    public float castFraction = castFraction;
    public AnimationStyle animation = animation;
}

public struct AnimationStyle(CastAnimationKind kind, uint color, float size, float frequency)
{
    public CastAnimationKind kind = kind;
    public uint color = color;
    public float size = size;
    public float frequency = frequency;
}
