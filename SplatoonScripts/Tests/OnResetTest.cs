using ECommons.Hooks;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests;
public class OnResetTest : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;
    public override void OnReset()
    {
        PluginLog.Information($"OnReset has been called");
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        PluginLog.Information($"Director update: {category}");
    }
}
