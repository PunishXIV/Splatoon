using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace SplatoonScriptsOfficial.Generic;

public class ReplaySpeedBoost : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [];

    uint LastValue;
    bool ValueChanged;
    public override void OnUpdate()
    {
        if(Svc.Condition[ConditionFlag.DutyRecorderPlayback] && !GenericHelpers.IsScreenReady())
        {
            var cfg = Svc.GameConfig.System.GetUInt("Fps");
            if(!ValueChanged && cfg != 0)
            {
                RenderDisableManager.PlaceRequest();
                ValueChanged = true;
                LastValue = cfg;
                Svc.GameConfig.System.Set("Fps", 0u);
            }
        }
        else
        {
            if(ValueChanged)
            {
                RenderDisableManager.RemoveRequest();
                Svc.GameConfig.System.Set("Fps", LastValue);
                ValueChanged = false;
            }
        }
    }

    public override void OnDisable()
    {
        if(ValueChanged)
        {
            RenderDisableManager.RemoveRequest();
            Svc.GameConfig.System.Set("Fps", LastValue);
            ValueChanged = false;
        }
    }
}
