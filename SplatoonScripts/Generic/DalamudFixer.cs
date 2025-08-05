using ECommons.Reflection;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public class DalamudFixer : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [0];

    public override void OnSettingsDraw()
    {
        if(ImGui.Button("Fix DTR bar errors"))
        {
            DalamudReflector.GetService("Dalamud.Game.Gui.Dtr.DtrBar").SetFoP("entriesLock", new ReaderWriterLockSlim());
        }
    }
}
