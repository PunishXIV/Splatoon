using ECommons.Configuration;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.SplatoonAPI;
using ECommons.Throttlers;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;
internal class Pantokrator_invincible_Reminder : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => [1122];
    public override Metadata? Metadata => new(1, "Redmoon");

    private const uint kPantokrator = 31499;
    private const uint kAtomicRay = 31480;

    private bool _phaseStart = false;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("invincible_count", "{\"Name\":\"Player dot\",\"type\":1,\"Enabled\":false,\"radius\":0.0,\"color\":3355508540,\"Filled\":false,\"fillIntensity\":0.5,\"overlayTextColor\":3355508496,\"overlayVOffset\":1.78,\"overlayFScale\":2.5,\"overlayText\":\"3\",\"refActorType\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"mechanicType\":2,\"RenderEngineKind\":1}");
        var invincibleCountControllerElement = Controller.GetElementByName("invincible_count");
        if(invincibleCountControllerElement == null)
            return;
        invincibleCountControllerElement.overlayText = "4";
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        var sourceObj = source.GetObject();
        if(sourceObj == null)
            return;
        if(sourceObj.DataId != 15708)
            return;

        if(castId == kPantokrator)
        {
            _phaseStart = true;
        }
        if(castId == kAtomicRay)
        {
            _phaseStart = false;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        var invincibleCountControllerElement = Controller.GetElementByName("invincible_count");
        if(invincibleCountControllerElement == null)
            return;
        if(!_phaseStart || invincibleCountControllerElement.Enabled)
            return;

        if(vfxPath == "vfx/lockon/eff/lockon5_t0h.avfx")
        {
            invincibleCountControllerElement.overlayText = "4";
            invincibleCountControllerElement.Enabled = true;
            EzThrottler.Throttle("NextElementTimer", C.NextElementTimer);
        }
    }

    public override void OnUpdate()
    {
        var invincibleCountControllerElement = Controller.GetElementByName("invincible_count");
        if(invincibleCountControllerElement == null)
            return;
        if(!invincibleCountControllerElement.Enabled)
            return;

        if(EzThrottler.Check("NextElementTimer"))
        {
            PluginLog.Log("Pantokrator invincible reminder End");
            invincibleCountControllerElement.Enabled = false;
            var count = int.Parse(invincibleCountControllerElement.overlayText);
            count--;
            invincibleCountControllerElement.overlayText = count.ToString();
            if(count > -1)
            {
                invincibleCountControllerElement.Enabled = true;
                EzThrottler.Throttle("NextElementTimer", C.NextElementTimer);
            }
            else
            {
                invincibleCountControllerElement.overlayText = "4";
                EzThrottler.Reset("NextElementTimer");
                _phaseStart = false;
            }
        }
    }

    public override void OnReset()
    {
        EzThrottler.Reset("NextElementTimer");
        _phaseStart = false;
        var invincibleCountControllerElement = Controller.GetElementByName("invincible_count");
        if(invincibleCountControllerElement == null)
            return;
        invincibleCountControllerElement.Enabled = false;
        invincibleCountControllerElement.overlayText = "4";
    }

    private Config C => Controller.GetConfig<Config>();
    public override void OnSettingsDraw()
    {
        ImGui.InputInt("Count Interval", ref C.NextElementTimer);
        ImGuiEx.HelpMarker("The interval between each count down. Unit:[ms] default: 950[ms]");
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Indent();
            var invincibleCountControllerElement = Controller.GetElementByName("invincible_count");
            if(invincibleCountControllerElement == null)
                return;
            ImGui.Text($"Phase Start: {_phaseStart}");
            ImGui.Text($"invincibleCountControllerElement: {invincibleCountControllerElement.Enabled}");
            ImGui.Text($"Invincible Count: {Controller.GetElementByName("invincible_count")?.overlayText}");
        }
    }

    public class Config : IEzConfig
    {
        public bool Debug = false;
        public int NextElementTimer = 950;
    }
}
