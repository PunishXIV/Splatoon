using ECommons;
using ECommons.Automation.UIInput;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class AutoRetainerCreation : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [];
    public string SymbolsA = "qwrtpsdfghjklzxcvbnm";
    public string SymbolsB = "eyuioa";

    string GenerateRandomName()
    {
        var len = Random.Shared.Next(4, 21);
        StringBuilder sb = new();
        var start = Random.Shared.Next(0, 2);
        for(int i = 0; i < len; i++)
        {
            sb.Append((i % 2 == start ? SymbolsA : SymbolsB).GetRandom());
        }
        return sb.ToString();
    }

    public override void OnUpdate()
    {
        if(!Svc.ClientState.IsLoggedIn) return;
        Process("_CharaMakeRaceGender", 18);
        Process("_CharaMakeTribe", 18);
        Process("_CharaMakeFeature", 37);
        if(GenericHelpers.TryGetAddonMaster<AddonMaster.SelectString>(out var m) && m.IsAddonReady)
        {
            if(m.EntryCount > 0 && m.Entries[0].Text.Equals("Polite.") && EzThrottler.Throttle(this.InternalData.FullName + "SelectString"))
            {
                m.Entries[0].Select();
                var name = GenerateRandomName();
                Svc.Toasts.ShowQuest($"Suggested name: \"{name}\" copied to clipboard");
                GenericHelpers.Copy(name);
            } 
        }
        if(EzThrottler.Throttle(this.InternalData.FullName + "PeriodicNotify", 60000))
        {
            DuoLog.Warning($"{this.InternalData.Name} is running. Disable the script if you don't need it anymore.");
        }
    }

    void Process(string addonName, uint buttonId)
    {
        if(GenericHelpers.TryGetAddonByName<AtkUnitBase>(addonName, out var addon) && addon->IsReady())
        {
            if(EzThrottler.Throttle(this.InternalData.FullName + addonName))
            {
                if(addon->GetButtonNodeById(buttonId)->IsEnabled && addon->GetButtonNodeById(buttonId)->AtkResNode->IsVisible())
                    addon->GetButtonNodeById(buttonId)->ClickAddonButton(addon);
            }
        }
    }
}
