

namespace Splatoon.Structures;

[Serializable]
public class LegacyPreset
{
    public string Name = null;
    public int MapID = 0;
    public Marker A;
    public Marker B;
    public Marker C;
    public Marker D;
    public Marker One;
    public Marker Two;
    public Marker Three;
    public Marker Four;

    [Serializable]
    public class Marker
    {
        public float X;
        public float Y;
        public float Z;
        public int ID;
        public bool Active = false;

        public Element ToElement(string text, uint col)
        {
            /*var bgcol = ImGui.ColorConvertU32ToFloat4(col);
            bgcol.W = 112f / 255f;
            if (bgcol.X == 1) bgcol.X = 112f / 255f;
            if (bgcol.Y == 1) bgcol.Y = 112f / 255f;
            if (bgcol.Z == 1) bgcol.Z = 112f / 255f;*/
            //Svc.Chat.Print(ImGui.ColorConvertFloat4ToU32(bgcol).ToString("X"));
            return new Element(0)
            {
                refX = X,
                refY = Z,
                refZ = Y,
                overlayText = text,
                overlayTextColor = col,
                radius = 0.7f,
                color = col,
            };
        }
    }
}
