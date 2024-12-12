using ECommons.DalamudServices;
using ECommons.Hooks;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;
public unsafe class P4_Delete_Intermission_Ice : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1238];
    public override Metadata? Metadata => new(1, "NightmareXIV");

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(position == 24 && data1 == 1 && data2 == 2)
        {
            Svc.Framework.RunOnTick(() =>
            {
                MapEffect.Delegate(*(nint*)(((nint)EventFramework.Instance()) + 344), 24, 4, 8);
            });
        }
    }
}
