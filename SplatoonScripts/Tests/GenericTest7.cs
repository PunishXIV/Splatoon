using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using Newtonsoft.Json;
using Splatoon.SplatoonScripting;
using SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Tests;
public unsafe class GenericTest7 : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; }

    public override Metadata? Metadata => new(5);

    

    public override void OnSettingsDraw()
    {
        var rm = RetainerManager.Instance();
        for(int i = 0; i < rm->Retainers.Length; i++)
        {
            ImGuiEx.Text($"Retainer {rm->Retainers[i].NameString} order {rm->DisplayOrder[i]}");
        }
        ImGuiEx.Text($"Ret:\n{rm->Retainers.ToArray().Select(x => x.NameString).Print("\n")}\n\nOrd:\n{rm->DisplayOrder.ToArray().Print("\n")}");
    }
}
