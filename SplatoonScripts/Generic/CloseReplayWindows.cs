using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class CloseReplayWindows : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [];
    public override Metadata? Metadata => new(2, "NightmareXIV");

    private string[] StandardAddons = [
        "ShopExchangeItem",
        "TelepotTown",
        "Talk",
        "MKDTowerEntry",
        "ShopExchangeCurrency",
        ];
    private (string Name, Action<Pointer<AtkUnitBase>> Action)[] SpecialAddons = [];

    public override void OnUpdate()
    {
        if(!Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback]) return;
        foreach(var x in StandardAddons)
        {
            if(GenericHelpers.TryGetAddonByName<AtkUnitBase>(x, out var addon) && addon->IsReady())
            {
                Callback.Fire(addon, true, -1);
            }
        }
        foreach(var x in SpecialAddons)
        {
            if(GenericHelpers.TryGetAddonByName<AtkUnitBase>(x.Name, out var addon) && addon->IsReady())
            {
                x.Action(addon);
            }
        }
    }
}
