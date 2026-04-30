using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.Schedulers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ECommons.DalamudServices.Legacy;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.AnotherMerchantTale;

public class Darya_Serenade_Script : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1317, 1316];
    public override Metadata? Metadata => new(1, "Poneglyph");

    private bool isWaitingForVFX = false;
    private List<string> capturedElements = new();
    private List<TickScheduler> activeSchedulers = new();

    private const string VFX_CHOCOBO = "vfx/common/eff/m0941_chocobo_c0h.avfx";
    private const string VFX_SEAHORSE = "vfx/common/eff/m0941_seahorse_c0h.avfx";
    private const string VFX_PUFFER = "vfx/common/eff/m0941_puffer_c0h.avfx";
    private const string VFX_CRAB = "vfx/common/eff/m0941_crab_c0h.avfx";
    private const string VFX_TURTLE = "vfx/common/eff/m0941_turtle_c0h.avfx";

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Chocobo", "{\"Name\":\"Chocobo\",\"type\":4,\"refY\":40.0,\"radius\":45.0,\"coneAngleMin\":-30,\"coneAngleMax\":30,\"color\":3355462399,\"fillIntensity\":0.8,\"thicc\":0.0,\"refActorModelID\":4777,\"refActorComparisonType\":1,\"includeRotation\":true,\"DistanceSourceX\":375.0,\"DistanceSourceY\":522.0,\"DistanceSourceZ\":-29.5,\"DistanceMax\":13.3}");
        Controller.RegisterElementFromCode("Seahorse", "{\"Name\":\"Seahorse\",\"type\":3,\"refY\":40.0,\"radius\":4.0,\"color\":3355508719,\"fillIntensity\":0.8,\"thicc\":0.0,\"refActorModelID\":4773,\"refActorComparisonType\":1,\"includeRotation\":true,\"DistanceSourceX\":375.0,\"DistanceSourceY\":522.0,\"DistanceSourceZ\":-29.5,\"DistanceMax\":13.3}");
        Controller.RegisterElementFromCode("Puffer", "{\"Name\":\"Puffer\",\"type\":4,\"refY\":40.0,\"radius\":20.0,\"coneAngleMin\":-90,\"coneAngleMax\":90,\"color\":3355479807,\"fillIntensity\":0.8,\"thicc\":0.0,\"refActorModelID\":4778,\"refActorComparisonType\":1,\"includeRotation\":true,\"DistanceSourceX\":375.0,\"DistanceSourceY\":522.0,\"DistanceSourceZ\":-29.5,\"DistanceMax\":13.3}");
        Controller.RegisterElementFromCode("Crab", "{\"Name\":\"Crab\",\"type\":3,\"refY\":40.0,\"radius\":4.0,\"fillIntensity\":0.8,\"thicc\":0.0,\"refActorModelID\":4776,\"refActorComparisonType\":1,\"includeRotation\":true}");
        Controller.RegisterElementFromCode("Turtle", "{\"Name\":\"Turtle\",\"type\":3,\"refY\":40.0,\"radius\":4.0,\"color\":3355508509,\"fillIntensity\":0.8,\"thicc\":0.0,\"refActorModelID\":4775,\"refActorComparisonType\":1,\"includeRotation\":true}");
        OnReset();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 45773)
        {
            isWaitingForVFX = true;
            capturedElements.Clear();
        }

        if(castId == 45844)
        {
            if(capturedElements.Count == 4)
            {
                ExecuteDisplay();
            }
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(!isWaitingForVFX) return;

        string? detected = vfxPath switch
        {
            VFX_CHOCOBO => "Chocobo",
            VFX_SEAHORSE => "Seahorse",
            VFX_PUFFER => "Puffer",
            VFX_CRAB => "Crab",
            VFX_TURTLE => "Turtle",
            _ => null
        };

        if(detected != null)
        {
            capturedElements.Add(detected);

            if(capturedElements.Count == 4)
            {
                isWaitingForVFX = false;
                ExecuteDisplay();
            }
        }
    }

    private void ExecuteDisplay()
    {
        var timings = new (uint StartAt, uint Duration)[]
        {
            (1000, 7000),
            (8000, 3000),
            (10500, 3500),
            (14000, 3000)
        };

        for(int i = 0; i < capturedElements.Count && i < timings.Length; i++)
        {
            string elementName = capturedElements[i];
            var (startAt, duration) = timings[i];
            int stepNum = i + 1;

            activeSchedulers.Add(new TickScheduler(() =>
            {
                if(Controller.TryGetElementByName(elementName, out var element))
                {
                    element.Enabled = true;

                    activeSchedulers.Add(new TickScheduler(() =>
                    {
                        element.Enabled = false;
                    }, duration));
                }
            }, startAt));
        }
    }

    public override void OnReset()
    {
        isWaitingForVFX = false;
        capturedElements.Clear();
        foreach(var sched in activeSchedulers) sched?.Dispose();
        activeSchedulers.Clear();
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }
}