using Dalamud.Plugin.Ipc.Exceptions;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using NotificationMasterAPI;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class InventoryLowNotifier : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [];

    public override Metadata? Metadata => new(1, "NightmareXIV");

    bool IsNotified = false;
    int GetInventoryFreeSlots()
    {
        int num = 0;
        var im = InventoryManager.Instance();
        foreach(var item in (InventoryType[])[InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4])
        {
            var c = im->GetInventoryContainer(item);
            for(int i = 0; i < c->Size; i++)
            {
                if(c->GetInventorySlot(i)->ItemId == 0) num++;
            }
        }
        return num;
    }

    public override void OnEnable()
    {
        Svc.GameInventory.InventoryChanged += GameInventory_InventoryChanged;
        IsNotified = false;
    }

    private void GameInventory_InventoryChanged(IReadOnlyCollection<Dalamud.Game.Inventory.InventoryEventArgTypes.InventoryEventArgs> events)
    {
        if(GetInventoryFreeSlots() <= C.NumSlots)
        {
            if(!IsNotified)
            {
                DuoLog.Warning($"Inventory is almost full!");
                IsNotified = true;
                Splatoon.Splatoon.P.NotificationMasterApi.DisplayTrayNotification("Final Fantasy XIV", "Inventory is almost full!");
                Splatoon.Splatoon.P.NotificationMasterApi.FlashTaskbarIcon();
            }
        }
        else
        {
            IsNotified = false;
        }
    }

    public override void OnDisable()
    {
        Svc.GameInventory.InventoryChanged -= GameInventory_InventoryChanged;
    }

    Config C => Controller.GetConfig<Config>();
    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(150f);
        ImGui.InputInt("Notify when inventory has reached this amount of slots or less", ref C.NumSlots);
        ImGuiEx.Text($"Current: {GetInventoryFreeSlots()}");
    }

    public class Config : IEzConfig
    {
        public int NumSlots = 10;
    }
}
