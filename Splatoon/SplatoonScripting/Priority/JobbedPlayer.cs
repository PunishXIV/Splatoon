﻿using ECommons.ExcelServices;
using ECommons.PartyFunctions;
using Splatoon.Gui.Priority;
using System.Diagnostics.CodeAnalysis;
#nullable enable

namespace Splatoon.SplatoonScripting.Priority;
public class JobbedPlayer
{
    internal string ID = GetTemporaryId();
    public string Name = "";
    public HashSet<Job> Jobs = [];
    public RolePosition Role = RolePosition.Not_Selected;

    internal void DrawSelector(bool isRole)
    {
        if(isRole)
        {
            ImGui.SetNextItemWidth(150f);
            ImGuiEx.EnumCombo("##selRole", ref Role);
        }
        else
        {
            var hint = Jobs.Count == 0 ? "Unused slot..." : "Any name";
            ImGui.SetNextItemWidth(150f);
            ImGui.InputTextWithHint("##input", hint, ref Name, 100);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f);
            ImGuiEx.JobSelector("##selJobs", Jobs, noJobSelectedPreview: "Any job");
            ImGui.SameLine();
            if(ImGuiEx.IconButton(FontAwesomeIcon.Users))
            {
                ImGui.OpenPopup("SelectParty");
            }
            if(ImGui.BeginPopup("SelectParty"))
            {
                foreach(var x in UniversalParty.MembersPlayback)
                {
                    if(ThreadLoadImageHandler.TryGetIconTextureWrap(x.ClassJob.GetIcon(), true, out var tex))
                    {
                        ImGui.Image(tex.ImGuiHandle, new(ImGui.GetFontSize()));
                        ImGui.SameLine(0, 1);
                    }
                    if(ImGui.Selectable($"{x.NameWithWorld}"))
                    {
                        Name = x.NameWithWorld;
                    }
                    if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        Name = x.NameWithWorld;
                        Jobs = [x.ClassJob];
                        ImGui.CloseCurrentPopup();
                    }
                    ImGuiEx.Tooltip("Left-click - set player name. \nRight-click - set player name and job.");
                }
                ImGui.EndPopup();
            }
        }
    }

    public bool IsPlayerEmpty()
    {
        return Name == "" && Jobs.Count == 0;
    }

    public UniversalPartyMember? ResolveByRole(RolePosition role)
    {
        var player = P.PriorityPopupWindow.Assignments.SafeSelect(PriorityPopupWindow.RolePositions.IndexOf(role));
        if(player == null) return null;
        if(player.IsInParty(false, out var ret))
        {
            return ret;
        }
        return null;
    }

    public bool IsInParty(bool byRole, [NotNullWhen(true)] out UniversalPartyMember? member)
    {
        if(byRole)
        {
            member = ResolveByRole(Role);
            return member != null;
        }
        foreach(var x in UniversalParty.MembersPlayback)
        {
            if(Name != "")
            {
                if(Name.EqualsIgnoreCase(x.Name) || Name.EqualsIgnoreCase(x.NameWithWorld))
                {
                    if(Jobs.Count == 0 || Jobs.Contains(x.ClassJob))
                    {
                        member = x;
                        return true;
                    }
                }
            }
            else
            {
                if(Jobs.Contains(x.ClassJob))
                {
                    member = x;
                    return true;
                }
            }
        }
        member = null;
        return false;
    }

    public string GetNameAndJob()
    {
        return $"{Name} - {(Jobs.Count > 0 ? Jobs.Print() : "Any job")}";
    }
}
