using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Reflection;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public sealed class ARealmRecordedWhitelistMod : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => null;
    public override Metadata Metadata => new(4, "lillylilim, NightmareXIV");

    public override void OnEnable()
    {
        Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        Svc.ClientState.Login += ClientState_Login;
        if(Player.Available)
        {
            ClientState_Login();
        }
    }

    private void ClientState_Login()
    {
        ClientState_TerritoryChanged(0);
    }

    public override void OnUpdate()
    {
        if(!Svc.ClientState.IsLoggedIn)
        {
            if(EzThrottler.Throttle("PeriodicARRCheck"))
            {
                ClientState_TerritoryChanged(0);
            }
        }
    }

    private void ClientState_TerritoryChanged(ushort obj)
    {
        if(Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == "ARealmRecorded" && x.IsLoaded))
        {
            try
            {
                if(DalamudReflector.TryGetDalamudPlugin("ARealmRecorded", out var plugin, true, true))
                {
                    var whitelist = plugin.GetStaticFoP<HashSet<uint>>("ARealmRecorded.Game", "whitelistedContentTypes");

                    whitelist.Add(21); // deep dungeon
                    whitelist.Add(39); // new deep dungeon?
                    whitelist.Add((uint)TerritoryIntendedUseEnum.Seasonal_Event_Duty); // new deep dungeon?

                    foreach(var x in whitelist)
                    {
                        //PluginLog.Debug($"ARealmRecorded whitelist: {x}");
                    }
                }
            }
            catch(Exception e)
            {
                e.LogDebug();
            }
        }
    }

    public override void OnDisable()
    {
        Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        Svc.ClientState.Login -= ClientState_Login;
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"{Player.TerritoryIntendedUse.RowId}");
    }
}