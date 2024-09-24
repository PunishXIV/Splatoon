using ECommons;
using ECommons.ImGuiMethods;
using ECommons.Reflection;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests;
public class GenericTest7 : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; }

    public override void OnSettingsDraw()
    {
        if(ImGui.Button("Try add"))
        {
            try
            {
                DalamudReflector.AddRepo("https://127.0.0.1/", true);
            }
            catch(Exception ex)
            {
                ex.Log();
            }
        }
        ImGuiEx.Text(Loc(en: "Mechanic in English", jp: "Mechanic in Japanese"));
        ImGuiEx.Text(Loc(jp: "Only JP text"));
    }
}
