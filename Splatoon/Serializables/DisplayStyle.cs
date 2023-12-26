namespace Splatoon.Serializables
{
    public struct DisplayStyle(uint strokeColor, float strokeThickness, uint originFillColor, uint endFillColor, bool filled = true)
    {
        public uint strokeColor = strokeColor;
        public float strokeThickness = strokeThickness;
        public uint originFillColor = originFillColor;
        public uint endFillColor = endFillColor;
        public bool filled = filled;

        public uint fillColor(float amount)
        {
            return Lerp(originFillColor, endFillColor, amount);
        }

        public uint animatedOriginFillColor(float animatePercent)
        {
            if (animatePercent < 0.5)
            {
                return Lerp(originFillColor, endFillColor, animatePercent / 0.5f);
            }
            return endFillColor;
        }
        public uint animatedEndFillColor(float animatePercent)
        {
            if (animatePercent > 0.5)
            {
                return Lerp(originFillColor, endFillColor, (animatePercent - 0.5f) / 0.5f);
            }
            return originFillColor;
        }
    }
}
