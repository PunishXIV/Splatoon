using ECommons.DalamudServices;
using ECommons.GameFunctions;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace SplatoonScriptsOfficial.Generic;

public class RenderToggleCmd : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;

    public bool RenderDisabled = false;

    public override void OnEnable()
    {
        Svc.Commands.AddHandler("/rendertoggle", new(delegate 
        { 
            RenderDisabled = !RenderDisabled;
            if(RenderDisabled)
            {
                RenderDisableManager.PlaceRequest();
            }
            else
            {
                RenderDisableManager.RemoveRequest();
            }
        }));
    }

    public override void OnUpdate()
    {
        if(RenderDisabled) Controller.DisplayAttentionWindowLine("Render disabled. Type /rendertoggle to enable.");
    }

    public override void OnDisable()
    {
        RenderDisabled = false;
        Svc.Commands.RemoveHandler("/rendertoggle");
        RenderDisableManager.RemoveRequest();
    }
}
