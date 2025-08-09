using Dalamud.Memory;
using ECommons;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class ExportBlacklist : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [9999];
    public override Metadata? Metadata => new(1, "NightmareXIV");

    public override void OnSettingsDraw()
    {
        if(ImGui.Button("Export blacklist"))
        {
            var s = "";
            var array = InfoProxyBlacklist.Instance()->BlockedCharacters;
            for(var i = 0; i < array.Length; i++)
            {
                var x = array[i];
                if(BlackListStringArray.Instance()->PlayerNames[i].ToString() != "")
                {
                    s += $"{BlockedCharaToString(x, i)}\n==========================\n";
                }
            }
            GenericHelpers.Copy(s);
        }
    }

    private string BlockedCharaToString(InfoProxyBlacklist.BlockedCharacter c, int index)
    {
        return $"""
            Name: {MemoryHelper.ReadStringNullTerminated((nint)c.Name.Value)},
            ID: {c.Id}
            Comment: {BlackListStringArray.Instance()->Notes[index]}
            Flag: {c.Flag}
            """;
    }
}
