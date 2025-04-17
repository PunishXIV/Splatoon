using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using Microsoft.VisualBasic;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;


namespace SplatoonScriptsOfficial.Tests;
internal unsafe class RedmoonTest1 : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => null;

    public override void OnSettingsDraw()
    {
        var gom = GameObjectManager.Instance();
        var eventFrameworkPtr = EventFramework.Instance();

        if(eventFrameworkPtr == null)
        {
            ImGui.Text("EventFramework is null");
            return;
        }
        if(gom == null || gom->Objects.IndexSorted.Length == 0)
        {
            ImGui.Text("GameObjectManager is null or has no objects");
            return;
        }

        if(ImGuiEx.CollapsingHeader("Sorted list"))
        {
            for(var objectIndex = 0; objectIndex < gom->Objects.IndexSorted.Length; ++objectIndex)
            {
                var obj = gom->Objects.IndexSorted[objectIndex].Value;
                if(obj == null)
                {
                    ImGuiNET.ImGui.Text($"Object {objectIndex}: null");
                    continue;
                }
                ImGuiNET.ImGui.Text($"Object {objectIndex}: 0x{Conversion.Hex(obj->EntityId)} {obj->NameString.ToString()}");
            }
        }
        if(ImGuiEx.CollapsingHeader("SortEntityId list"))
        {
            for(var objectIndex = 0; objectIndex < gom->Objects.EntityIdSorted.Length; ++objectIndex)
            {
                var obj = gom->Objects.EntityIdSorted[objectIndex].Value;
                if(obj == null)
                {
                    ImGuiNET.ImGui.Text($"Object {objectIndex}: null");
                    continue;
                }
                ImGuiNET.ImGui.Text($"Object {objectIndex}: 0x{Conversion.Hex(obj->EntityId)} {obj->NameString.ToString()}");
            }
        }
        if(ImGuiEx.CollapsingHeader("MyStatusList"))
        {
            var sm = (StatusManager*)Svc.ClientState.LocalPlayer.StatusList.Address;
            var statusArray = (Status*)((byte*)sm + 0x08);
            if(sm == null)
            {
                ImGui.Text("StatusManager is null");
                return;
            }
            if(statusArray == null)
            {
                ImGui.Text("StatusArray is null");
                return;
            }
            for(var statusIndex = 0; statusIndex < sm->NumValidStatuses; ++statusIndex)
            {
                ImGui.Text($"Status {statusIndex}: 0x{Conversion.Hex((&statusArray[statusIndex])->StatusId)}");
            }
        }
        if(ImGuiEx.CollapsingHeader("EventFramework"))
        {
            ImGui.Text(eventFrameworkPtr->GetContentDirector() == null ? "CurrentDirector is null" : "CurrentDirector is not null");
            ImGui.Text(eventFrameworkPtr->GetInstanceContentDirector() == null ? "InstanceContentDirector is null" : "InstanceContentDirector is not null");
            ImGui.Text(eventFrameworkPtr->GetPublicContentDirector() == null ? "PublicContentDirector is null" : "PublicContentDirector is not null");
        }
    }
}
