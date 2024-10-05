using Dalamud.Memory;
using ECommons;
using ECommons.Automation;
using ECommons.EzHookManager;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Splatoon.SplatoonScripting;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Tests;
public unsafe class GenericTest7 : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; }

    public override Metadata? Metadata => new(5);

    

    public override void OnEnable()
    {
        EzSignatureHelper.Initialize(this);
    }

    public override void OnUpdate()
    {
        var trade = AgentModule.Instance()->GetAgentByInternalId(AgentId.Trade);
        if(trade->IsAgentActive() && GenericHelpers.TryGetAddonByName<AtkUnitBase>("Trade", out var addon))
        {
            if(FrameThrottler.Throttle("AntiTrade", 2))
            {
                Callback.Fire(addon, true, -1);
                DuoLog.Information("Prevent trade");
            }
        }
    }

    public override void OnSettingsDraw()
    {
    }
}
