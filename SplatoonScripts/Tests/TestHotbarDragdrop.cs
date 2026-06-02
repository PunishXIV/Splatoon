using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.DalamudServices;
using ECommons.EzHookManager;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using Action = Lumina.Excel.Sheets.Action;

namespace SplatoonScriptsOfficial.Tests;

public unsafe class TestHotbarDragdrop : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;

    private (int Hotbar, int Slot)? HoveredSlot = null;

    [EzHook("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 20 48 8B 7C 24 ?? 48 8B D9", false)]
    private EzHook<AddonActionBarBase.Delegates.ReceiveEvent> AddonActionBarBase_ReceiveEventHook;

    private void AddonActionBarBase_ReceiveEventDetour(AddonActionBarBase* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        try
        {
            if(eventType == AtkEventType.DragDropRollOver)
            {
                HoveredSlot = ((int Hotbar, int Slot)?)(thisPtr->RaptureHotbarId, eventParam);
            }
            if(eventType == AtkEventType.DragDropRollOut)
            {
                HoveredSlot = null;
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        AddonActionBarBase_ReceiveEventHook.Original(thisPtr, eventType, eventParam, atkEvent, atkEventData);
    }

    public override void OnSetup()
    {
        EzSignatureHelper.Initialize(this);
    }

    public override void OnEnable()
    {
        AddonActionBarBase_ReceiveEventHook?.Enable();
    }

    public override void OnDisable()
    {
        AddonActionBarBase_ReceiveEventHook?.Disable();
    }

    private int DragDropFrame;
    private uint CurrentAction;
    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"{HoveredSlot}");
        drawAction(15991);
        drawAction(15992);
        drawAction(15993);
        drawAction(15994);

        void drawAction(uint actionId) 
        { 
            var action = Svc.Data.GetExcelSheet<Action>().GetRow(actionId);
            if(ThreadLoadImageHandler.TryGetIconTextureWrap(action.Icon, false, out var texture))
            {
                ImGui.ImageButton(texture.Handle, new(50));
                if(ImGui.BeginDragDropSource())
                {
                    CurrentAction = actionId;
                    ImGui.Image(texture.Handle, new(50));
                    ImGui.EndDragDropSource();
                }
                if(actionId == this.CurrentAction && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && HoveredSlot != null && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow))
                {
                    RaptureHotbarModule.Instance()->Hotbars[HoveredSlot.Value.Hotbar].Slots[HoveredSlot.Value.Slot].Set(RaptureHotbarModule.HotbarSlotType.Action, actionId);
                }
            }
        }
    }
}
