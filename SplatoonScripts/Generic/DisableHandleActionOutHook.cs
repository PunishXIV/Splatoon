using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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

    private void Disable()
    {
        DalamudReflector.GetService("Dalamud.Game.Gui.GameGui").GetFoP("handleActionOutHook").Call("Disable", []);
    }
    private void Enable()
    {
        DalamudReflector.GetService("Dalamud.Game.Gui.GameGui").GetFoP("handleActionOutHook").Call("Enable", []);
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"Hook enabled: {DalamudReflector.GetService("Dalamud.Game.Gui.GameGui").GetFoP("handleActionOutHook").GetFoP("IsEnabled")}");
        if(ImGui.Button("Disable")) Disable();
        if(ImGui.Button("Enable")) Enable();
        if(ImGui.Button("Load bunch of C# libraries"))
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher", "runtime");
            var path2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher", "addon", "hooks", "dev");
            foreach(var x in (string[])[.. Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories), .. Directory.GetFiles(path2, "*.dll", SearchOption.AllDirectories)])
            {
                try
                {
                    var f = new FileInfo(x);
                    PluginLog.Information($"Loading {f.Name.Replace(".dll", "")}");
                    Assembly.Load(f.Name.Replace(".dll", ""));
                }
                catch(Exception e)
                {
                    e.Log();
                }
            }
        }
        ImGuiEx.TextCopy($"Delegate: {((nint)AgentActionDetail.StaticVirtualTablePointer->ReceiveEvent) - Process.GetCurrentProcess().MainModule.BaseAddress:X16}");
        ImGuiEx.TextCopy($"ptr: {*(nint*)((nint)AgentActionDetail.Addresses.StaticVirtualTable.Value + 24) - Process.GetCurrentProcess().MainModule.BaseAddress:X16}");
    }
}
