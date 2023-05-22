using ECommons;
using ECommons.LanguageHelpers;
using Splatoon.Utils;

namespace Splatoon.ConfigGui.CGuiLayouts.LayoutDrawHeader.Subcommands;

internal static class ZlockSelector
{
    internal static string zlockf = "";
    internal static bool zlockcur = false;
    internal static void DrawZlockSelector(this Layout layout)
    {
        var colorZLock = Svc.ClientState?.TerritoryType != null
            && (layout.ZoneLockH.Count != 0 && !layout.ZoneLockH.Contains(Svc.ClientState.TerritoryType)).Invert(layout.IsZoneBlacklist)
            && Environment.TickCount64 % 1000 < 500;
        if (colorZLock) ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red);
        layout.ZoneLockH.RemoveWhere(el => !P.Zones.ContainsKey(el));
        ImGuiEx.SetNextItemFullWidth();
        if (ImGui.BeginCombo("##zlk", layout.ZoneLockH.Count == 0 ? "All zones".Loc() :
            layout.ZoneLockH.Count == 1 ? GenericHelpers.GetTerritoryName(layout.ZoneLockH.First()) :
            "?? zones".Loc(layout.ZoneLockH.Count)
            ))
        {
            if (colorZLock) ImGui.PopStyleColor();
            ImGui.SetNextItemWidth(100f);
            ImGui.InputTextWithHint("##zfltr", "Filter".Loc(), ref zlockf, 100);
            ImGui.SameLine();
            ImGui.Checkbox("Only selected".Loc(), ref zlockcur);
            if (P.Zones.ContainsKey(Svc.ClientState.TerritoryType))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Colors.Yellow);
                if (layout.ZoneLockH.Contains(Svc.ClientState.TerritoryType))
                {
                    SImGuiEx.ColorButton(Colors.Red);
                }
                string zcfc = P.Zones[Svc.ClientState.TerritoryType].ContentFinderCondition?.Value.Name?.ToString();
                if (P.Zones.ContainsKey(Svc.ClientState.TerritoryType) && ImGui.SmallButton($"Current zone: ??".Loc(GenericHelpers.GetTerritoryName(Svc.ClientState.TerritoryType))))
                {
                    layout.ZoneLockH.Toggle(Svc.ClientState.TerritoryType);
                }
                SImGuiEx.UncolorButton();
                ImGui.PopStyleColor();
            }
            foreach (var z in P.Zones.Where(x => x.Value?.PlaceName?.Value?.Name?.ToString().IsNullOrEmpty() == false))
            {
                var s = GenericHelpers.GetTerritoryName(z.Key);
                if (!s.ToLower().Contains(zlockf)) continue;
                if (zlockcur && !layout.ZoneLockH.Contains(z.Key)) continue;
                if (layout.ZoneLockH.Contains(z.Key))
                {
                    SImGuiEx.ColorButton(Colors.Red);
                }
                if (ImGui.SmallButton(s))
                {
                    layout.ZoneLockH.Toggle(z.Key);
                }
                SImGuiEx.UncolorButton();
            }
            ImGui.EndCombo();
        }
        else
        {
            if (colorZLock) ImGui.PopStyleColor();
        }
    }
}
