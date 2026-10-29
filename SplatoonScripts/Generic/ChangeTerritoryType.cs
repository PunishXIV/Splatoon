using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.ImGuiMethods.TerritorySelection;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace SplatoonScriptsOfficial.Generic;

#pragma warning disable RS0030
public class ChangeTerritoryType : SplatoonScript
{
    public override Metadata Metadata => new(1, "NightmareXIV");

    public override HashSet<uint>? ValidTerritories => [9999];

    public unsafe override void OnSettingsDraw()
    {
        ImGuiEx.Text($"Current territory: {ExcelTerritoryHelper.GetName(Player.Territory.RowId)}");

        var m = LayoutWorld.Instance()->ActiveLayout;
        if(m != null)
        {
            if(Player.Territory.RowId != m->TerritoryTypeId)
            {
                ImGuiEx.Text(EColor.RedBright, $"Territory is not synchronized with current layout.\nCurrent layout: {ExcelTerritoryHelper.GetName(m->TerritoryTypeId)}\nReported territory: {ExcelTerritoryHelper.GetName(Player.Territory.RowId)}");
                if(ImGuiEx.IconButtonWithText(Dalamud.Interface.FontAwesomeIcon.Sync, $"Synchronize territory with current layout"))
                {
                    ChangeTerritory(m->TerritoryTypeId);
                }
            }
            else
            {
                ImGuiEx.Text(EColor.GreenBright, $"Territory is synchronized with current layout.\nCurrent layout: {ExcelTerritoryHelper.GetName(m->TerritoryTypeId)}\nReported territory: {ExcelTerritoryHelper.GetName(Player.Territory.RowId)}");
            }
        }
        if(ImGuiEx.IconButtonWithText(Dalamud.Interface.FontAwesomeIcon.Pen, "Change territory manually"))
        {
            uint ttid = Player.Territory.RowId;
            new TerritorySelector(ttid, (sel, ter) =>
            {
                sel.Close();
                ChangeTerritory(ter);
            })
            {
                SelectedCategory = TerritorySelector.Category.All,
            };
        }
    }

    void ChangeTerritory(uint territory)
    {
        try
        {
            var cs = DalamudReflector.GetService("Dalamud.Game.ClientState.ClientState");
            cs.SetFoP("TerritoryType", (ushort)territory);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }
}
