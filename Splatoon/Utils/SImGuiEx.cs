using Dalamud.Interface.Colors;

namespace Splatoon.Utils;

public static class SImGuiEx //came here to laugh on how scuffed it is? let's do so together.
{
    public static void DrawLine(Vector2 curpos, float contRegion)
    {
        ImGui.GetForegroundDrawList().PathLineTo(curpos);
        ImGui.GetForegroundDrawList().PathLineTo(curpos with { X = curpos.X + contRegion });
        ImGui.GetForegroundDrawList().PathStroke((Environment.TickCount % 600 > 300 ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudRed).ToUint(), ImDrawFlags.None, 2f);

    }

    public static void InputUintDynamic(string id, ref uint u)
    {
        if (P.Config.Hexadecimal)
        {
            ImGuiEx.Text("0x");
            ImGui.SameLine(0, 1);
            ImGui.SetNextItemWidth(200f);
            ImGuiEx.InputHex(id, ref u);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Hexadecimal input");
            }
        }
        else
        {
            ImGui.SetNextItemWidth(200f);
            ImGuiEx.InputUint(id, ref u);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Decimal input");
            }
        }
    }

    public static void SizedText(string text, float width)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, Colors.Transparent);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Colors.Transparent);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Colors.Transparent);
        var s = ImGui.CalcTextSize(text);
        ImGuiEx.Text(text);
        if (width > s.X)
        {
            ImGui.SameLine();
            ImGui.Button("", new Vector2(width - s.X, 1f));
        }
        ImGui.PopStyleColor(3);
    }

    public static void TextCentered(string text)
    {
        /*ImGui.PushStyleColor(ImGuiCol.Button, Colors.Transparent);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Colors.Transparent);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Colors.Transparent);*/
        var s = ImGui.CalcTextSize(text);
        //ImGui.Button("", new Vector2(ImGui.GetColumnWidth()/2f - s.X/2f, 1f));
        ImGui.SetCursorPosX(ImGui.GetColumnWidth() / 2f - s.X / 2f);
        //ImGui.PopStyleColor(3);
        //ImGui.SameLine();
        ImGuiEx.Text(text);
    }

    static int StyleColors = 0;
    public static void ColorButton(uint color)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);
        StyleColors += 3;
    }

    public static void UncolorButton()
    {
        if (StyleColors == 0) return;
        ImGui.PopStyleColor(StyleColors);
        StyleColors = 0;
    }

    public static void DisplayColor(uint col)
    {
        var a = col.ToVector4();
        ImGui.ColorEdit4("", ref a, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoPicker);
    }

    public static void GSameLine(Action a, out float cursorPosX, bool end = false)
    {
        var upperTextCursor = ImGui.GetCursorPos();
        a();
        if (!end)
        {
            ImGui.SameLine();
            upperTextCursor.X = ImGui.GetCursorPosX();
            ImGuiEx.Text("");
            ImGui.SetCursorPos(upperTextCursor);
        }
        cursorPosX = upperTextCursor.X;
    }

    static public void EnumCombo<T>(string name, ref T refConfigField, string[] overrideNames = null) where T : IConvertible
    {
        var values = overrideNames ?? Enum.GetValues(typeof(T)).Cast<T>().Select(x => x.ToString().Replace("_", " ")).ToArray();
        var num = Convert.ToInt32(refConfigField);
        ImGui.Combo(name, ref num, values, values.Length);
        refConfigField = Enum.GetValues(typeof(T)).Cast<T>().ToArray()[num];
    }
}
