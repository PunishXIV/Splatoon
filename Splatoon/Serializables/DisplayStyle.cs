namespace Splatoon.Serializables
{
    public struct DisplayStyle(
        uint strokeColor,
        float strokeThickness,
        float fillIntensity,
        uint originFillColor,
        uint endFillColor,
        bool filled = true,
        bool overrideFillColor = false)
    {
        public uint strokeColor = strokeColor;
        public float strokeThickness = strokeThickness;
        public float fillIntensity = fillIntensity;
        public uint originFillColor = originFillColor;
        public uint endFillColor = endFillColor;
        public bool filled = filled;
        public bool overrideFillColor = overrideFillColor;
    }
}
