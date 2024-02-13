namespace Splatoon.Serializables
{
    public struct DisplayStyle(uint strokeColor, float strokeThickness, uint originFillColor, uint endFillColor, bool filled = true)
    {
        public uint strokeColor = strokeColor;
        public float strokeThickness = strokeThickness;
        public uint originFillColor = originFillColor;
        public uint endFillColor = endFillColor;
        public bool filled = filled;
    }
}
