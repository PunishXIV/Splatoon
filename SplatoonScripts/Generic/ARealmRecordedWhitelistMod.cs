using ECommons;
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
    public override HashSet<uint>? ValidTerritories => [];
    public override Metadata Metadata => new(1, "lillylilim");

    public override void OnEnable()
    {
        try
        {
            if(DalamudReflector.TryGetDalamudPlugin("ARealmRecorded", out var plugin, true, true))
            {
                var whitelist = plugin.GetStaticFoP<HashSet<uint>>("ARealmRecorded.Game", "whitelistedContentTypes");

                whitelist.Add(21); // deep dungeon
                whitelist.Add(39); // new deep dungeon?

                foreach(var x in whitelist)
                {
                    PluginLog.Log($"OnEnable(): ARealmRecorded whitelist: {x}");
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public override void OnDisable()
    {
        try
        {
            if(DalamudReflector.TryGetDalamudPlugin("ARealmRecorded", out var plugin, true, true))
            {
                var whitelist = plugin.GetStaticFoP<HashSet<uint>>("ARealmRecorded.Game", "whitelistedContentTypes");

                whitelist.Remove(21); // deep dungeon
                whitelist.Remove(39); // new deep dungeon?

                foreach(var x in whitelist)
                {
                    PluginLog.Log($"OnDisable(): ARealmRecorded whitelist: {x}");
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }
}