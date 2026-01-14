using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.GameHelpers.LegacyPlayer;
using ECommons.LanguageHelpers;
using ECommons.MathHelpers;
using NightmareUI;
using NightmareUI.PrimaryUI;
using Splatoon.Utility;

namespace Splatoon.ConfigGui.CGuiLayouts.LayoutDrawHeader.Subcommands;

internal static class JlockSelector
{
    internal static string jobFilter = "";
    internal static void DrawJlockSelector(this Layout layout)
    {
        if(BasePlayer != null)
        {
            if(layout.JobLockH.Count == 0 || layout.JobLockH.Contains(BasePlayer.GetJob()))
            {
                ImGuiEx.HelpMarker("Player's job matches this selection".Loc(), EColor.GreenBright, FontAwesomeIcon.Check.ToIconString(), false);
            }
            else
            {
                ImGuiEx.HelpMarker("Player's job does not matches this selection".Loc(), EColor.RedBright, FontAwesomeIcon.Times.ToIconString(), false);
            }
            ImGui.SameLine();
        }
        ImGuiEx.SetNextItemFullWidth();
        ImGuiEx.JobSelector("##jobSelector", layout.JobLockH, [ImGuiEx.JobSelectorOption.BulkSelectors, ImGuiEx.JobSelectorOption.IncludeBase], 7, "All Jobs");
    }
}
