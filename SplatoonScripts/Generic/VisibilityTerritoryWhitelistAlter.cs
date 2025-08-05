using Dalamud.Interface;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.ImGuiMethods.TerritorySelection;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class VisibilityTerritoryWhitelistAlter : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [];

    public override void OnEnable()
    {
        Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        if(Svc.ClientState.IsLoggedIn)
        {
            ClientState_TerritoryChanged((ushort)Player.Territory);
        }
    }

    private void ClientState_TerritoryChanged(ushort obj)
    {
        try
        {
            if(DalamudReflector.TryGetDalamudPlugin("Visibility", out var plugin, true, true))
            {
                var whitelist = plugin.GetFoP("Configuration").GetFoP<HashSet<ushort>>("TerritoryTypeWhitelist");
                foreach(var x in C.Territories)
                {
                    whitelist.Add((ushort)x);
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public override void OnDisable()
    {
        Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text($"{C.Territories.Count} zones selected");
        if(ImGui.Button("Select zones..."))
        {
            new TerritorySelector(C.Territories, (sel, x) =>
            {
                C.Territories = x;
            }, "Select force whitelisted territories");
        }
        if(ImGui.Button("Apply settings"))
        {
            if(Player.Available) ClientState_TerritoryChanged((ushort)Player.Territory);
        }
        ImGuiEx.HelpMarker("", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString(), false);
        ImGui.SameLine();
        ImGuiEx.TextWrapped(EColor.RedBright, "To reset zone whitelist, reload Visibility plugin or restart the game");
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public HashSet<uint> Territories = [];
    }
}
