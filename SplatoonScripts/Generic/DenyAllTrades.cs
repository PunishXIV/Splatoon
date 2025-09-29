using ECommons;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe sealed class DenyAllTrades : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; }

    public override void OnUpdate()
    {
        if(GenericHelpers.TryGetAddonByName<AtkUnitBase>("Trade", out var trade))
        {
            Callback.Fire(trade, true, -1);
        }
    }
}