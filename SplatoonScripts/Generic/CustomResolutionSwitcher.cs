using Dalamud.Bindings.ImGui;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace SplatoonScriptsOfficial.Generic;

public class CustomResolutionSwitcher : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;

    bool PreviousState = false;

    public unsafe override void OnUpdate()
    {
        var newState = Framework.Instance()->WindowInactive;
        if(newState != PreviousState)
        {
            PreviousState = newState;
            if(newState)
            {
                On();
            }
            else
            {
                Off();
            }
        }
    }

    void On()
    {
        Svc.Commands.ProcessCommand($"/gres {C.Resolution}");
        Svc.Commands.ProcessCommand("/gres on");
    }

    void Off()
    {
        Svc.Commands.ProcessCommand("/gres off");
    }

    public override void OnDisable()
    {
        Off();
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(200f);
        ImGui.InputFloat("Resolution when minimized", ref C.Resolution);
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public float Resolution = 0.25f;
    }
}
