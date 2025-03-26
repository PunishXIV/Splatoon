using Dalamud.Memory;
using ECommons;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class ExportBlacklist : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [9999];

    public override void OnSettingsDraw()
    {
        if(ImGui.Button("Export blacklist"))
        {
            var s = "";
            foreach(var x in InfoProxyBlacklist.Instance()->BlockedCharacters)
            {
                s += $"{BlockedCharaToString(x)}\n==========================\n";
            }
            GenericHelpers.Copy(s);
        }
    }

    string BlockedCharaToString(InfoProxyBlacklist.BlockedCharacter c)
    {
        return $"""
            Name: {MemoryHelper.ReadStringNullTerminated((nint)c.Name.Value)},
            ID: {c.Id}
            Flag: {c.Flag}
            """;
    }
}
