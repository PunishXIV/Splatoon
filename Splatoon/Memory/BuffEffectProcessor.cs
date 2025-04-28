using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Splatoon.SplatoonScripting;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Splatoon.Memory;
internal unsafe class BuffEffectProcessor : IDisposable
{
    #region types
    private enum StatusChangeType : byte
    {
        NoChange,
        Remove,
        Gain
    }

    private enum StatusChangeResult : byte
    {
        NoChange,
        Change
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct CharacterStatusInfo
    {
        [FieldOffset(0)]
        public uint ObjectID;

        [FieldOffset(8)]
        public Status* StatusPtr;
    }

    #endregion

    #region privateDefine
    private const int MAX_STATUS_NUM = 60;
    // There are 629 object slots in total, but only 299 objects up to EventObject are needed.
    private const int MAX_OBJECT_NUM = 299;
    private const uint INVALID_OBJECTID = 0xE0000000;

    private static CharacterStatusInfo* _CharacterStatusInfoPtr = null;
    private static bool _IsRunning = false;
    #endregion

    #region public
    public BuffEffectProcessor()
    {
        try
        {
            _CharacterStatusInfoPtr = (CharacterStatusInfo*)Marshal.AllocHGlobal(sizeof(CharacterStatusInfo) * MAX_OBJECT_NUM);
            Unsafe.InitBlock(_CharacterStatusInfoPtr, 0, (uint)sizeof(CharacterStatusInfo) * MAX_OBJECT_NUM);
            for(var i = 0; i < MAX_OBJECT_NUM; ++i)
            {
                _CharacterStatusInfoPtr[i].StatusPtr = (FFXIVClientStructs.FFXIV.Client.Game.Status*)Marshal.AllocHGlobal(sizeof(FFXIVClientStructs.FFXIV.Client.Game.Status) * MAX_STATUS_NUM);
            }

            _IsRunning = true;
        }
        catch(Exception ex)
        {
            DuoLog.Error($"[Splatoon]: {ex.Message}");
            _IsRunning = false;
        }
    }

    ~BuffEffectProcessor()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        for(var i = 0; i < MAX_OBJECT_NUM; ++i)
        {
            if(_CharacterStatusInfoPtr[i].StatusPtr != null)
            {
                Marshal.FreeHGlobal((IntPtr)_CharacterStatusInfoPtr[i].StatusPtr);
                _CharacterStatusInfoPtr[i].StatusPtr = null;
            }
        }
        if(_CharacterStatusInfoPtr != null)
        {
            Marshal.FreeHGlobal((IntPtr)_CharacterStatusInfoPtr);
            _CharacterStatusInfoPtr = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void ActorEffectUpdate()
    {
        if(!_IsRunning) return;
        try
        {
            Update();
        }
        catch(Exception ex)
        {
            DuoLog.Error($"[Splatoon]: {ex.Message}");
            Dispose();
            _IsRunning = false;
        }
    }
    #endregion
    private void Update()
    {
        var gameObjectManagerPtr = GameObjectManager.Instance();

        for(var i = 0; i < MAX_OBJECT_NUM; ++i)
        {
            var gameObject = gameObjectManagerPtr->Objects.IndexSorted[i].Value;
            if(gameObject == null) continue;
            if(!gameObject->IsCharacter()) continue;
            if(gameObject->EntityId == INVALID_OBJECTID) continue;
            var character = (Character*)gameObject;
            var sm = (StatusManager*)character->GetStatusManager();
            var statusArray = (Status*)((byte*)sm + 0x08);
            if(sm == null) continue;
            if(statusArray == null) continue;

            // New object
            if(_CharacterStatusInfoPtr[i].ObjectID != gameObject->EntityId)
            {
                Unsafe.InitBlock(_CharacterStatusInfoPtr[i].StatusPtr, 0, (uint)sizeof(CharacterStatusInfo));
                _CharacterStatusInfoPtr[i].ObjectID = character->EntityId;
                Unsafe.CopyBlock(&_CharacterStatusInfoPtr[i].StatusPtr[0], &statusArray[0], (uint)sizeof(FFXIVClientStructs.FFXIV.Client.Game.Status) * sm->NumValidStatuses);
                continue;
            }

            // Existing object
            // Check status change
            Status status;
            var isChange = false;
            for(var j = 0; j < sm->NumValidStatuses; ++j)
            {
                if(_CharacterStatusInfoPtr[i].StatusPtr[j].StatusId != statusArray[j].StatusId)
                {
                    if(_CharacterStatusInfoPtr[i].StatusPtr[j].StatusId == 0)
                    {
                        // Gain
                        status = statusArray[j];
                        AddStatusLog(&statusArray[j], character);
                        ScriptingProcessor.OnGainBuffEffect(character->EntityId, status);
                        isChange = true;
                    }
                    else
                    {
                        // Remove
                        status = _CharacterStatusInfoPtr[i].StatusPtr[j];
                        RemoveStatusLog(&_CharacterStatusInfoPtr[i].StatusPtr[j], character);
                        ScriptingProcessor.OnRemoveBuffEffect(character->EntityId, status);

                        if(statusArray[j].StatusId != 0)
                        {
                            // Gain
                            status = statusArray[j];
                            AddStatusLog(&statusArray[j], character);
                            ScriptingProcessor.OnGainBuffEffect(character->EntityId, status);
                        }
                        isChange = true;
                    }
                }

                if(_CharacterStatusInfoPtr[i].StatusPtr[j].Param != statusArray[j].Param && !isChange)
                {
                    // Update
                    status = statusArray[j];
                    UpdateStatusLog(&statusArray[j], character);
                    ScriptingProcessor.OnUpdateBuffEffect(character->EntityId, status);
                }
            }

            // Update status
            Unsafe.CopyBlock(&_CharacterStatusInfoPtr[i].StatusPtr[0], &statusArray[0], (uint)sizeof(FFXIVClientStructs.FFXIV.Client.Game.Status) * sm->NumValidStatuses);
        }
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
        var PositionString = P.Config.LogPosition == true ? $"({gameObjectCharactor->Position.ToString()})" : "";
        var ElementTrigger = "";
        if(gameObjectCharactor->ObjectKind == ObjectKind.Pc)
        {
            ElementTrigger = $"[{prefix}]PC:{data->StatusId}:{gameObjectCharactor->ClassJob.ToString()}";
        }
        else
        {
            ElementTrigger = $"[{prefix}]{gameObjectCharactor->NameId}:{data->StatusId}";
        }

        if(gameObjectCharactor->EntityId == Svc.ClientState.LocalPlayer?.EntityId)
        {
            text = $"You gains the effect of {data->StatusId} Param: {data->Param} ([{prefix}]You:{data->StatusId}:{Svc.ClientState.LocalPlayer.GetJob().ToString()})";
            P.ChatMessageQueue.Enqueue(text);
        }

        text = $"{gameObjectCharactor->NameString} ({PositionString}) gains the effect of {data->StatusId} Param: {data->Param} ({ElementTrigger})";

        P.ChatMessageQueue.Enqueue(text);
    }
    #endregion
}

