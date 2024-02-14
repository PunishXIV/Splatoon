using Dalamud.Interface.Colors;
using ECommons.LanguageHelpers;
using Splatoon.Serializables;

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

    public static void EnumCombo<T>(string name, ref T refConfigField, string[] overrideNames = null, string[] tooltips = null) where T : IConvertible
    {
        var values = overrideNames ?? Enum.GetValues(typeof(T)).Cast<T>().Select(x => x.ToString().Replace("_", " ")).ToArray();
        var selectedNum = Convert.ToInt32(refConfigField);
        if (ImGui.BeginCombo(name, values[selectedNum]))
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (ImGui.Selectable(values[i], i == selectedNum))
                {
                    selectedNum = i;
                }
                if (i == selectedNum)
                {
                    ImGui.SetItemDefaultFocus();
                }
                if (tooltips != null)
                {
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(tooltips[i]);
                    }
                }
            }
            ImGui.EndCombo();
        }
        refConfigField = Enum.GetValues(typeof(T)).Cast<T>().ToArray()[selectedNum];
    }

    public static bool StyleEdit(string name, ref DisplayStyle style)
    {
        bool edited = false;
        SImGuiEx.SizedText("Stroke:".Loc(), CGui.WidthElement);
        ImGui.SameLine();
        var v4 = ImGui.ColorConvertU32ToFloat4(style.strokeColor);
        if (ImGui.ColorEdit4("##strokecolorbutton" + name, ref v4, ImGuiColorEditFlags.NoInputs))
        {
            style.strokeColor = ImGui.ColorConvertFloat4ToU32(v4);
            edited = true;
        }
        ImGui.SameLine();
        ImGuiEx.Text("Thickness:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(60f);
        if (ImGui.DragFloat("##strokeThiccness" + name, ref style.strokeThickness, 0.1f, 0f, float.MaxValue))
        {
            edited = true;
        }

        if (ImGui.IsItemHovered()) ImGui.SetTooltip("This value is also used for tether".Loc());

        SImGuiEx.SizedText("Fill:".Loc(), CGui.WidthElement);
        ImGui.SameLine();
        if (ImGui.Checkbox("Enabled".Loc() + "##name" + name, ref style.filled))
        {
            edited = true;
        }
        if (!style.filled) ImGui.BeginDisabled();
        {
            ImGui.SameLine();
            if (style.overrideFillColor) ImGui.BeginDisabled();
            ImGuiEx.Text("Intensity:".Loc());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200f);
            if (ImGui.SliderFloat("##fillintensity" + name, ref style.fillIntensity, 0f, 1f))
            {
                edited = true;
            }
            if (style.overrideFillColor) ImGui.EndDisabled();
            ImGui.Indent(CGui.WidthElement + 15f);
            {
                if (ImGui.Checkbox("Override Colors".Loc() + "##name" + name, ref style.overrideFillColor))
                {
                    edited = true;
                }
                if (!style.overrideFillColor) ImGui.BeginDisabled();
                ImGui.SameLine();
                ImGuiEx.Text("Origin:".Loc());
                ImGui.SameLine();
                v4 = ImGui.ColorConvertU32ToFloat4(style.originFillColor);
                if (ImGui.ColorEdit4("##fillorigincolorbutton" + name, ref v4, ImGuiColorEditFlags.NoInputs))
                {
                    style.originFillColor = ImGui.ColorConvertFloat4ToU32(v4);
                    edited = true;
                }
                ImGui.SameLine();
                ImGuiEx.Text("End:".Loc());
                ImGui.SameLine();
                v4 = ImGui.ColorConvertU32ToFloat4(style.endFillColor);
                if (ImGui.ColorEdit4("##fillendcolorbutton" + name, ref v4, ImGuiColorEditFlags.NoInputs))
                {
                    style.endFillColor = ImGui.ColorConvertFloat4ToU32(v4);
                    edited = true;
                }
                if (!style.overrideFillColor) ImGui.EndDisabled();
            }
            ImGui.Unindent(CGui.WidthElement + 15f);
        }
        if (!style.filled) ImGui.EndDisabled();
        return edited;
    }
}
