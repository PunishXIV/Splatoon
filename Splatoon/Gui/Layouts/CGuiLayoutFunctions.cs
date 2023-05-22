using ECommons.LanguageHelpers;
using Splatoon.Utils;

namespace Splatoon;

internal partial class CGui
{
    internal static bool AddEmptyLayout(out Layout l)
    {
        if (NewLayoytName.Contains("~"))
        {
            Notify.Error("Name can't contain reserved characters: ~".Loc());
        }
        else if (NewLayoytName.Contains(","))
        {
            Notify.Error("Name can't contain reserved characters: ,".Loc());
        }
        else
        {
            l = new Layout()
            {
                Name = CGui.NewLayoytName
            };
            if (Svc.ClientState != null) l.ZoneLockH.Add(Svc.ClientState.TerritoryType);
            P.Config.LayoutsL.Add(l);
            CGui.NewLayoytName = "";
            return true;
        }
        l = default;
        return false;
    }

    static void DrawRotationSelector(Element el, string i, string k)
    {
        ImGui.SameLine();
        ImGuiEx.Text("Add angle:".Loc());
        ImGui.SameLine();
        var angleDegrees = el.AdditionalRotation.RadiansToDegrees();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(50f);
        ImGui.DragFloat("##ExtraAngle" + i + k, ref angleDegrees, 0.1f, 0f, 360f);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hold shift for faster changing;\ndouble-click to enter manually.".Loc());
        if (angleDegrees < 0f || angleDegrees > 360f) angleDegrees = 0f;
        el.AdditionalRotation = angleDegrees.DegreesToRadians();
        if (el.type != 1)
        {
            ImGui.SameLine();
            ImGui.Checkbox("Face me##" + i + k, ref el.FaceMe);
        }
    }
}
