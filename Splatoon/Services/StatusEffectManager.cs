using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Status = Lumina.Excel.GeneratedSheets.Status;

namespace Splatoon.Services;
public unsafe class StatusEffectManager
{
    EzHook<StatusManager.Delegates.AddStatus> AddStatusHook;
    EzHook<StatusManager.Delegates.RemoveStatus> RemoveStatusHook;
    private StatusEffectManager()
    {
        AddStatusHook = new((nint)StatusManager.MemberFunctionPointers.AddStatus, AddStatusDetour);
        RemoveStatusHook = new((nint)StatusManager.MemberFunctionPointers.AddStatus, RemoveStatusDetour);
    }

    private void RemoveStatusDetour(StatusManager* thisPtr, int statusIndex, byte u2)
    {
        try
        {
            var name = "unk";
            if(thisPtr->Owner != null) name = $"{thisPtr->Owner->NameString}/{thisPtr->Owner->NameId}";
            var status = thisPtr->Status[statusIndex];
            PluginLog.Debug($"RemoveStatusDetour on: {name}, status={status.StatusId}/{status.Param} {Svc.Data.GetExcelSheet<Status>().GetRow(status.StatusId)?.Name}");
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
            PluginLog.Debug($"AddStatusDetour on: {name}, status={statusId}/{param} {Svc.Data.GetExcelSheet<Status>().GetRow(statusId)?.Name}");
        }
        catch(Exception e)
        {
            e.Log();
        }
        AddStatusHook.Original(thisPtr, statusId, param, u3);
    }
}
