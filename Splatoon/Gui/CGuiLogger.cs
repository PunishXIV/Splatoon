using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ECommons.LanguageHelpers;
using Splatoon.Gui;
using Splatoon.Utils;

namespace Splatoon;

internal partial class CGui
{
    string LoggerSearch = "";
    bool IsViewer = false;
    void DisplayLogger()
    {
        ImGui.Checkbox("Enable logger".Loc(), ref p.LogObjects);
        ImGui.SameLine();
        ImGui.Checkbox("Viewer mode".Loc(), ref IsViewer);
        ImGuiComponents.HelpMarker("When enabled, only currently present objects are displayed".Loc());
        ImGui.SameLine();
        if(ImGui.Button("Clear list".Loc()))
        {
            p.loggedObjectList.Clear();
        }
        ImGui.SameLine();
        ImGuiEx.Text("Filter:".Loc());
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputText("##filterLog", ref LoggerSearch, 100);
        ImGui.BeginTable("##logObjects", 14, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        ImGui.TableSetupColumn("Object name".Loc(), ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Type".Loc());
        ImGui.TableSetupColumn("Object ID".Loc());
        ImGui.TableSetupColumn("OID Long".Loc());
        ImGui.TableSetupColumn("Data ID".Loc());
        ImGui.TableSetupColumn("Model ID".Loc());
        ImGui.TableSetupColumn("NPC ID".Loc());
        ImGui.TableSetupColumn("Name ID".Loc());
        ImGui.TableSetupColumn("Tar. %".Loc());
        ImGui.TableSetupColumn("Vis. %".Loc());
        ImGui.TableSetupColumn("Exist".Loc());
        ImGui.TableSetupColumn("Dist.".Loc());
        ImGui.TableSetupColumn("Hibox".Loc());
        ImGui.TableSetupColumn("Life".Loc());
        ImGui.TableHeadersRow();
        var i = 0;
        foreach (var x in p.loggedObjectList)
        {
            i++;
            var mid = !x.Value.IsChar ? "--" : $"{x.Key.ModelID.Format()}";
            var oid = x.Key.ObjectID == 0xE0000000 ? "--" : $"{x.Key.ObjectID.Format()}";
            var oidl = $"{x.Key.ObjectIDLong.Format()}";
            var did = x.Key.DataID == 0 ? "--" : $"{x.Key.DataID.Format()}";
            var npcid = $"{x.Key.NPCID.Format()}";
            var nameid = !x.Value.IsChar ? "--" : $"{x.Key.NameID.Format()}";
            if (LoggerSearch != "")
            {
                if (!x.Key.Name.ToString().Contains(LoggerSearch, StringComparison.OrdinalIgnoreCase)
                    && !x.Key.type.ToString().Contains(LoggerSearch, StringComparison.OrdinalIgnoreCase)
                    && !oid.Contains(LoggerSearch, StringComparison.OrdinalIgnoreCase)
                    && !oidl.Contains(LoggerSearch, StringComparison.OrdinalIgnoreCase)
                    && !did.Contains(LoggerSearch, StringComparison.OrdinalIgnoreCase)
                    && !mid.Contains(LoggerSearch, StringComparison.OrdinalIgnoreCase)
                    && !nameid.Contains(LoggerSearch, StringComparison.OrdinalIgnoreCase)) continue;
            }
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiEx.TextCopy(x.Key.Name);
            ImGui.TableNextColumn();
            ImGuiEx.Text($"{x.Key.type}");
            ImGui.TableNextColumn();
            ImGuiEx.TextCopy(oid);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                if(Svc.Objects.TryGetFirst(z => z.ObjectId == x.Key.ObjectID, out var go))
                {
                    Explorer.Ptr = go.Address;
                }
            }
            ImGui.SameLine();
            if (ImGui.SmallButton("Find".Loc()+"##"+i))
            {
                p.SFind.Clear();
                p.SFind.Add(new()
                {
                    includeUntargetable = true,
                    oid = x.Key.ObjectID,
                    SearchAttribute = 2
                });
            }
            ImGui.TableNextColumn();
            ImGuiEx.TextCopy(oidl);
            ImGui.TableNextColumn();
            ImGuiEx.TextCopy(did);
            ImGui.TableNextColumn();
            ImGuiEx.TextCopy(mid);
            ImGui.TableNextColumn();
            ImGuiEx.TextCopy(npcid);
            ImGui.TableNextColumn();
            ImGuiEx.TextCopy(nameid);
            ImGui.TableNextColumn();
            if (x.Value.Targetable) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
            ImGuiEx.Text($"{(int)(((double)x.Value.TargetableTicks / (double)x.Value.ExistenceTicks) * 100)}%");
            if (x.Value.Targetable) ImGui.PopStyleColor();
            ImGui.TableNextColumn();
            if (x.Value.Visible) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
            ImGuiEx.Text(!x.Value.IsChar ? "--":$"{(int)(((double)x.Value.VisibleTicks / (double)x.Value.ExistenceTicks) * 100)}%");
            if (x.Value.Visible) ImGui.PopStyleColor();
            ImGui.TableNextColumn();
            ImGuiEx.Text($"{x.Value.ExistenceTicks}");
            ImGui.TableNextColumn();
            ImGuiEx.Text($"{x.Value.Distance:F1}");
            ImGui.TableNextColumn();
            ImGuiEx.Text($"{x.Value.HitboxRadius:F1}");
            ImGui.TableNextColumn();
            ImGuiEx.Text($"{x.Value.Life:F1}");
        }
        ImGui.EndTable();
        if (IsViewer)
        {
            p.loggedObjectList.Clear();
        }
    }
}
