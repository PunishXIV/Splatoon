using ECommons;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Events;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Generic;
public class NoSproutIcon : SplatoonScript
{
    public override Metadata? Metadata { get; } = new(2, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;

    public Config C => this.Controller.GetConfig<Config>();

    public override void OnEnable()
    {
        ProperOnLogin.RegisterAvailable(OnLogin);
    }

    public override void OnDisable()
    {
        ProperOnLogin.Unregister(OnLogin);
    }

    void OnLogin()
    {
        C.PlayerNames[Player.CID] = Player.NameWithWorld;
    }

    public override void OnUpdate()
    {
        if(Svc.ClientState.LocalPlayer != null && Svc.ClientState.LocalPlayer.OnlineStatus.RowId == 32 && C.EnabledCIDs.Contains(Svc.ClientState.LocalContentId))
        {
            if(GenericHelpers.IsScreenReady() && Player.Interactable && EzThrottler.Throttle("NaStatusOff", 10000))
            {
                Chat.Instance.ExecuteCommand("/nastatus off");
            }
        }
    }

    public override void OnSettingsDraw()
    {
        if(Player.Available) C.PlayerNames[Player.CID] = Player.NameWithWorld;
        foreach(var x in C.EnabledCIDs.Union(C.PlayerNames.Keys))
        {
            ImGuiEx.CollectionCheckbox(GetName(x), x, C.EnabledCIDs);
        }
    }

    string GetName(ulong id)
    {
        return C.PlayerNames.SafeSelect(id) ?? $"{id:X16}";
    }

    public class Config : IEzConfig
    {
        public Dictionary<ulong, string> PlayerNames = [];
        public List<ulong> EnabledCIDs = [];
    }
}
