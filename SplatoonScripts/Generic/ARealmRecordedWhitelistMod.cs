using ECommons;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.Reflection;
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
    public override Metadata Metadata => new(2, "lillylilim");

    public override void OnEnable()
    {
        Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        Svc.ClientState.Login += ClientState_Login;
    }

    private void ClientState_Login()
    {
        ClientState_TerritoryChanged(0);
    }

    private void ClientState_TerritoryChanged(ushort obj)
    {
        if(DalamudReflector.TryGetDalamudPlugin("ARealmRecorded", out var plugin, true, true))
        {
            var whitelist = plugin.GetStaticFoP<HashSet<uint>>("ARealmRecorded.Game", "whitelistedContentTypes");

            whitelist.Add(21); // deep dungeon
            whitelist.Add(39); // new deep dungeon?

            foreach(var x in whitelist)
            {
                PluginLog.Verbose($"OnEnable(): ARealmRecorded whitelist: {x}");
            }
        }
    }

    public override void OnDisable()
    {
        Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        Svc.ClientState.Login -= ClientState_Login;
    }
}