using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Tests;
internal unsafe class RedmoonTest3 :SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;
    public override Metadata? Metadata => new(1, "Redmoon");

    IBattleChara?[] GetEnemyList()
    {
        var list = new IBattleChara?[8];
        var array = AtkStage.Instance()->AtkArrayDataHolder->NumberArrays[21];
        var characters = Svc.Objects.OfType<IBattleChara>().ToArray();
        for (int i = 0; i < 8; i++)
        {
            var id = *(uint*)&array->IntArray[8 + (i * 6)];
            if (id != 0xE0000000)
            {
                list[i] = characters.FirstOrDefault(x => x.EntityId == id);
            }
        }
        return list;
    }

    public override void OnSettingsDraw()
    {
        var list = new IBattleChara?[8];
        var array = AtkStage.Instance()->AtkArrayDataHolder->NumberArrays[21];
        var characters = Svc.Objects.OfType<IBattleChara>().ToArray();
        for (int i = 0; i < 8; i++)
        {
            var ptr = (uint*)&array->IntArray[8 + (i * 6)];
            var id = *ptr;
            ImGuiEx.Text($"Address: 0x{((IntPtr)ptr).ToString("X8")}, ID: {id}");
            if (id != 0xE0000000)
            {
                list[i] = characters.FirstOrDefault(x => x.EntityId == id);
            }
        }
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] == null)
            {
                ImGuiEx.Text($"Enemy {i}: NULL");
                continue;
            }
            ImGuiEx.Text($"Enemy {i}: {list[i]?.Name} HP:{list[i].CurrentHp}");
        }
    }
}
