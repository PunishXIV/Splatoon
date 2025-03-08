using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class ShowInfo : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [0];
    public override void OnSettingsDraw()
    {
        var a = LayoutWorld.Instance()->ActiveLayout;
        if(a != null)
        {
            foreach(var f in a->ActiveFestivals)
            {
                ImGuiEx.Text($"{f.Id}/{f.Phase}");
            }
        }
    }
}
