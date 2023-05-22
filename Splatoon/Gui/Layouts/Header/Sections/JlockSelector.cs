using ECommons.LanguageHelpers;
using ECommons.MathHelpers;
using Splatoon.Utils;

namespace Splatoon.ConfigGui.CGuiLayouts.LayoutDrawHeader.Subcommands;

internal static class JlockSelector
{
    internal static string jobFilter = "";
    internal static void DrawJlockSelector(this Layout layout)
    {
        var jprev = new List<string>();
        if (layout.JobLock == 0)
        {
            jprev.Add("All jobs".Loc());
        }
        else
        {
            foreach (var k in P.Jobs)
            {
                if (Bitmask.IsBitSet(layout.JobLock, k.Key))
                {
                    jprev.Add(k.Value);
                }
            }
        }
        var colorJLock = Svc.ClientState?.LocalPlayer?.ClassJob != null
            && layout.JobLock != 0
            && !Bitmask.IsBitSet(layout.JobLock, (int)Svc.ClientState.LocalPlayer.ClassJob.Id)
            && Environment.TickCount64 % 1000 < 500;
        if (colorJLock) ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red);
        ImGuiEx.SetNextItemFullWidth();
        if (ImGui.BeginCombo("##joblock", jprev.Count < 3 ? string.Join(", ", jprev) : "?? jobs".Loc(jprev.Count)))
        {
            if (colorJLock) ImGui.PopStyleColor();
            ImGui.InputTextWithHint("##joblockfltr", "Filter".Loc(), ref jobFilter, 100);
            foreach (var k in P.Jobs)
            {
                if (!k.Key.ToString().Contains(jobFilter) && !k.Value.Contains(jobFilter)) continue;
                if (k.Key == 0) continue;
                var col = false;
                if (Bitmask.IsBitSet(layout.JobLock, k.Key))
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, Colors.Red);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Colors.Red);
                    col = true;
                }
                if (ImGui.SmallButton(k.Key + " / " + k.Value + "##selectjob"))
                {
                    if (Bitmask.IsBitSet(layout.JobLock, k.Key))
                    {
                        Bitmask.ResetBit(ref layout.JobLock, k.Key);
                    }
                    else
                    {
                        Bitmask.SetBit(ref layout.JobLock, k.Key);
                    }
                }
                if (col) ImGui.PopStyleColor(2);
            }
            ImGui.EndCombo();
        }
        else
        {
            if (colorJLock) ImGui.PopStyleColor();
        }
    }
}
