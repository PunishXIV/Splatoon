using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.SimpleGui;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests;
public class GenericTest6 : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;

    public override Metadata? Metadata => new(2);

    public override void OnScriptUpdated(uint previousVersion)
    {
        if(previousVersion < 2)
        {
            PluginLog.Information("Updated");
            new PopupWindow(() =>
            {
                ImGuiEx.Text($"""
                    Warning: Splatoon Script 
                    {InternalData.Name}
                    was updated.
                    If you were using Sidewise Spark related functions,
                    you must reconfigure the script.
                    """);
            });
        }
    }
}
