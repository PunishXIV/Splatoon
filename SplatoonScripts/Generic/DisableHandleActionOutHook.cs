using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Reflection;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class DisableHandleActionOutHook : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => null;

    public override Metadata? Metadata => new(2, "NightmareXIV");

    public override void OnEnable()
    {
        Disable();
    }

    public override void OnDisable()
    {
        Enable();
    }

    void Disable()
    {
        DalamudReflector.GetService("Dalamud.Game.Gui.GameGui").GetFoP("handleActionOutHook").Call("Disable", []);
    }
    void Enable()
    {
        DalamudReflector.GetService("Dalamud.Game.Gui.GameGui").GetFoP("handleActionOutHook").Call("Enable", []);
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"Hook enabled: {DalamudReflector.GetService("Dalamud.Game.Gui.GameGui").GetFoP("handleActionOutHook").GetFoP("IsEnabled")}");
        if(ImGui.Button("Disable")) Disable();
        if(ImGui.Button("Enable")) Enable();
    }
}
