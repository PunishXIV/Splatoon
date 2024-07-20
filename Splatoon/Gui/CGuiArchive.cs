using Dalamud.Plugin.Ipc.Exceptions;
using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon;
internal unsafe partial class CGui
{
    internal void DrawArchive()
    {
        ImGuiEx.TextWrapped($"""
            You may archive layouts that you are no longer using. Archived layouts:
            - Are not processed and do not consume any resources;
            - Are included into backups;
            - Can not be edited, viewed, reordered;
            - Can be exported to clipboard or restored at any time.
            """);
        var groups = P.Archive.LayoutsL.Select(x => x.Group).Distinct().Order();

        foreach (var group in groups)
        {
            if (group == "") continue;
            if (ImGuiEx.TreeNode(group))
            {
                var grp = P.Archive.LayoutsL.Where(x => x.Group == group);
                if (ImGuiEx.IconButtonWithText(FontAwesomeIcon.Copy, "Copy group"))
                {
                    Copy(grp.Select(x => EzConfig.DefaultSerializationFactory.Serialize(x, false)).Join("\n"));
                }
                ImGui.SameLine();
                if (ImGuiEx.IconButtonWithText(FontAwesomeIcon.ArrowCircleLeft, "Restore group"))
                {
                    foreach (var x in grp)
                    {
                        P.Config.LayoutsL.Add(x.JSONClone());
                        new TickScheduler(() => P.Archive.LayoutsL.Remove(x));
                    }
                }
                ImGui.SameLine();
                if (ImGuiEx.IconButtonWithText(FontAwesomeIcon.Trash, "Delete group", ImGuiEx.Ctrl))
                {
                    foreach (var x in grp)
                    {
                        new TickScheduler(() => P.Archive.LayoutsL.Remove(x));
                    }
                }
                ImGui.PushID(group);
                DrawArchiveEntries(grp);
                ImGui.PopID();
                ImGui.TreePop();
            }
        }
        var nogrp = P.Archive.LayoutsL.Where(x => x.Group == "");
        if(nogrp.Any()) DrawArchiveEntries(nogrp);
    }

    void DrawArchiveEntries(IEnumerable<Layout> layouts)
    {
        if (ImGui.BeginTable("EntryArchive", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Info");
            ImGui.TableSetupColumn("Control");

            foreach(var x in layouts)
            {
                ImGui.PushID(x.GUID);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGuiEx.TextV($"{x.Name}");

                ImGui.TableNextColumn();

                ImGuiEx.TextV($"{x.ElementsL.Count} elements");

                ImGui.TableNextColumn();

                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Copy, "Copy"))
                {
                    Copy(EzConfig.DefaultSerializationFactory.Serialize(x, false));
                }
                ImGui.SameLine();
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.ArrowCircleLeft, "Restore"))
                {
                    P.Config.LayoutsL.Add(x.JSONClone());
                    new TickScheduler(() => P.Archive.LayoutsL.Remove(x));
                }
                ImGui.SameLine();
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Trash, "Delete", ImGuiEx.Ctrl))
                {
                    new TickScheduler(() => P.Archive.LayoutsL.Remove(x));
                }
                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }
}
