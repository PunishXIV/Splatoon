using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Status = Lumina.Excel.Sheets.Status;

namespace Splatoon.Services;
public unsafe class StatusEffectManager
{
    private delegate byte StatusManager_SetStatus(nint a1, uint a2, ushort a3, float a4, ushort a5, uint a6, byte a7);
    [EzHook("40 53 56 41 56 48 83 EC 70 45 32 F6")]
    private EzHook<StatusManager_SetStatus> StatusManager_SetStatusHook;

    private EzHook<StatusManager.Delegates.AddStatus> AddStatusHook;
    private EzHook<StatusManager.Delegates.RemoveStatus> RemoveStatusHook;
    private StatusEffectManager()
    {
        EzSignatureHelper.Initialize(this);
        AddStatusHook = new((nint)StatusManager.MemberFunctionPointers.AddStatus, AddStatusDetour);
        RemoveStatusHook = new((nint)StatusManager.MemberFunctionPointers.AddStatus, RemoveStatusDetour);
    }

    private byte StatusManager_SetStatusDetour(nint a1, uint a2, ushort a3, float a4, ushort a5, uint a6, byte a7)
    {
        try
        {
            PluginLog.Information($"SetStatus: {a1:X}, {a2}, {a3}, {a4}, {a5}, {a6}, {a7}");
        }
        catch(Exception e)
        {
            e.Log();
        }
        return StatusManager_SetStatusHook.Original(a1, a2, a3, a4, a5, a6, a7);
    }

    private void RemoveStatusDetour(StatusManager* thisPtr, int statusIndex, byte u2)
    {
        try
        {
            var name = "unk";
            if(thisPtr->Owner != null) name = $"{thisPtr->Owner->NameString}/{thisPtr->Owner->NameId}";
            var status = thisPtr->Status[statusIndex];
            PluginLog.Debug($"RemoveStatusDetour on: {name}, status={status.StatusId}/{status.Param} {Svc.Data.GetExcelSheet<Status>().GetRowOrDefault(status.StatusId)?.Name}");
        }
        catch(Exception e)
        {
            e.Log();
        }
        RemoveStatusHook.Original(thisPtr, statusIndex, u2);
    }

    private void AddStatusDetour(StatusManager* thisPtr, ushort statusId, ushort param, void* u3)
    {
        try
        {
            var name = "unk";
            if(thisPtr->Owner != null) name = $"{thisPtr->Owner->NameString}/{thisPtr->Owner->NameId}";
            PluginLog.Debug($"AddStatusDetour on: {name}, status={statusId}/{param} {Svc.Data.GetExcelSheet<Status>().GetRowOrDefault(statusId)?.Name}");
        }
        catch(Exception e)
        {
            e.Log();
        }
        AddStatusHook.Original(thisPtr, statusId, param, u3);
    }
}
