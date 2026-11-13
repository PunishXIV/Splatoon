using ECommons.EzHookManager;
using ECommons.GameHelpers;
using ECommons.GameHelpers.LegacyPlayer;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Splatoon.SplatoonScripting;
using System.Runtime.CompilerServices;

namespace Splatoon.Memory;

/// <summary>
/// Raises OnGainBuffEffect / OnRemoveBuffEffect / OnUpdateBuffEffect from the functions that write status slots,
/// instead of comparing every slot of every object each frame.
/// Every status slot write goes through one of:
///  - StatusManager.SetStatus: compares the slot before and after. Id changed = remove old + gain new, same id with different param = update.
///  - the "status removed" notification (called after RemoveStatus and the other removal paths have cleared the slot).
///    Calls made from inside SetStatus are skipped because SetStatus already reported them.
///  - the function that only writes the param of a slot (from ActorControl).
/// Events are raised synchronously from the hooks, right after the game has written the slot.
/// </summary>
internal unsafe class StatusHookProcessor : IDisposable
{
    // There are 629 object slots in total, but only 299 objects up to EventObject are needed.
    private const int MAX_OBJECT_NUM = 299;
    private const uint INVALID_OBJECTID = 0xE0000000;

    private delegate byte SetStatusDelegate(StatusManager* sm, int index, ushort statusId, float remaining, ushort param, ulong source, byte refresh);
    private delegate void StatusRemovedDelegate(StatusManager* sm, ushort statusId, ushort param, ulong source, byte a5);
    private delegate void SetStatusParamDelegate(StatusManager* sm, int index, ushort statusId, ushort param);

    private EzHook<SetStatusDelegate> SetStatusHook;
    private EzHook<StatusRemovedDelegate> StatusRemovedHook;
    private EzHook<SetStatusParamDelegate> SetStatusParamHook;

    private int InSetStatus = 0;

    public StatusHookProcessor()
    {
        try
        {
            SetStatusHook = new((nint)StatusManager.MemberFunctionPointers.SetStatus, SetStatusDetour);
            StatusRemovedHook = new("40 55 57 41 55 41 57 48 83 EC 68 48 83 39 00 49 8B E9 45 0F B7 F8 48 8B F9 44 0F B7 EA", StatusRemovedDetour);
            SetStatusParamHook = new("8B C2 48 03 C0 66 44 39 44 C1 08 75 06 66 44 89 4C C1 0A C3", SetStatusParamDetour);
        }
        catch(Exception ex)
        {
            PluginLog.Error($"Could not create status hooks.");
            ex.Log();
        }
    }

    public void Dispose()
    {
        // EzHook.Disable disposes the underlying hook, so nothing stays patched or referenced after a plugin reload
        SetStatusHook?.Disable();
        StatusRemovedHook?.Disable();
        SetStatusParamHook?.Disable();
        SetStatusHook = null;
        StatusRemovedHook = null;
        SetStatusParamHook = null;
    }

    private byte SetStatusDetour(StatusManager* sm, int index, ushort statusId, float remaining, ushort param, ulong source, byte refresh)
    {
        // Status managers we don't report on (no owner, objects past the first 299, copies) go straight through.
        // Removal notifications from inside them are filtered by the same owner check, so InSetStatus is not needed there.
        var owner = sm != null && (uint)index < (uint)sm->Status.Length ? GetOwner(sm) : null;
        if(owner == null) return SetStatusHook.Original(sm, index, statusId, remaining, param, source, refresh);
        var before = sm->Status[index];
        byte ret;
        InSetStatus++;
        try
        {
            ret = SetStatusHook.Original(sm, index, statusId, remaining, param, source, refresh);
        }
        finally
        {
            InSetStatus--;
        }
        try
        {
            var after = sm->Status[index];
            if(before.StatusId != after.StatusId)
            {
                if(before.StatusId != 0) Remove(owner, before);
                if(after.StatusId != 0) Gain(owner, after);
            }
            else if(after.StatusId != 0 && before.Param != after.Param)
            {
                Update(owner, after);
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        return ret;
    }

    private void StatusRemovedDetour(StatusManager* sm, ushort statusId, ushort param, ulong source, byte a5)
    {
        try
        {
            if(InSetStatus == 0 && statusId != 0 && sm != null)
            {
                var owner = GetOwner(sm);
                if(owner != null)
                {
                    // The slot is already cleared at this point, so the remaining time is not known.
                    Remove(owner, new Status() { StatusId = statusId, Param = param, SourceObject = source });
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        StatusRemovedHook.Original(sm, statusId, param, source, a5);
    }

    private void SetStatusParamDetour(StatusManager* sm, int index, ushort statusId, ushort param)
    {
        var owner = sm != null && (uint)index < (uint)sm->Status.Length ? GetOwner(sm) : null;
        if(owner == null)
        {
            SetStatusParamHook.Original(sm, index, statusId, param);
            return;
        }
        var before = sm->Status[index];
        SetStatusParamHook.Original(sm, index, statusId, param);
        try
        {
            var after = sm->Status[index];
            if(after.StatusId != 0 && before.StatusId == after.StatusId && before.Param != after.Param)
            {
                Update(owner, after);
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    /// <summary>
    /// The character this status manager belongs to, limited to the same objects the frame comparison used to cover.
    /// </summary>
    private static Character* GetOwner(StatusManager* sm)
    {
        var owner = sm->Owner;
        if(owner == null) return null;
        var go = (GameObject*)owner;
        if(go->ObjectIndex >= MAX_OBJECT_NUM) return null;
        if(!go->IsCharacter()) return null;
        if(go->EntityId == INVALID_OBJECTID) return null;
        if(owner->GetStatusManager() != sm) return null;
        return owner;
    }

    private void Gain(Character* character, Status status)
    {
        AddStatusLog(&status, character);
        ScriptingProcessor.OnGainBuffEffect(character->EntityId, status);
    }

    private void Remove(Character* character, Status status)
    {
        RemoveStatusLog(&status, character);
        ScriptingProcessor.OnRemoveBuffEffect(character->EntityId, status);
    }

    private void Update(Character* character, Status status)
    {
        UpdateStatusLog(&status, character);
        ScriptingProcessor.OnUpdateBuffEffect(character->EntityId, status);
    }

    #region private
    private void AddStatusLog(Status* data, Character* gameObjectCharactor) => StatusLog("buff+", data, gameObjectCharactor);
    private void RemoveStatusLog(Status* data, Character* gameObjectCharactor) => StatusLog("buff-", data, gameObjectCharactor);
    private void UpdateStatusLog(Status* data, Character* gameObjectCharactor) => StatusLog("buff*", data, gameObjectCharactor);

    // Updated LogChanges method
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void StatusLog(string prefix, Status* data, Character* gameObjectCharactor)
    {
        var text = "";
        var PositionString = P.Config.LogPosition == true ? $"({gameObjectCharactor->Position})" : "";
        var ElementTrigger = "";
        if(gameObjectCharactor->ObjectKind == ObjectKind.Pc)
        {
            ElementTrigger = $"[{prefix}]PC:{data->StatusId}:{gameObjectCharactor->ClassJob}";
        }
        else
        {
            ElementTrigger = $"[{prefix}]{gameObjectCharactor->NameId}:{data->StatusId}";
        }

        if(gameObjectCharactor->EntityId == BasePlayer?.EntityId)
        {

            text = $"You gains the effect of {data->StatusId} Param: {data->Param} ([{prefix}]You:{data->StatusId}:{BasePlayer.GetJob()})";
            P.ChatMessageQueue.Enqueue(text);
        }

        text = $"{gameObjectCharactor->NameString} ({PositionString}) gains the effect of {data->StatusId} Param: {data->Param} ({ElementTrigger})";

        P.ChatMessageQueue.Enqueue(text);
    }
    #endregion
}
