using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Statuses;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks;
using Splatoon.SplatoonScripting;
using Splatoon.Structures;

namespace Splatoon.Memory;
internal class BuffEffectProcessor
{
    #region types
    private enum StatusChangeType
    {
        NoChange,
        Remove,
        Gain
    }

    private enum StatusChangeResult
    {
        NoChange,
        Change
    }

    private struct CharacterStatusInfo
    {
        public uint ObjectID;
        public RecordedStatus[] Statuses;
        public int NoUpdateCount;
    }

    private record struct CharactorStatusDiffResult
    {
        public RecordedStatus StatusId;
        public StatusChangeType ChangeType;
    }

    #endregion

    #region privateDefine
    private Dictionary<uint, CharacterStatusInfo> _charactorStatusInfos = [];
    private static bool _isClearRequest = false;
    #endregion

    #region public
    public void ActorEffectUpdate()
    {
        if(_isClearRequest)
        {
            _charactorStatusInfos.Clear();
            _isClearRequest = false;
        }

        // Increment the NoUpdateCount for all objects
        IncrementNoUpdateCount();

        // Loop through all objects in Svc.Objects
        foreach(var gameObject in Svc.Objects)
        {
            if(gameObject == null)
                continue;

            // Check if it can be cast to IBattleChara
            if(gameObject is IBattleChara battleChara)
            {
                var objectID = battleChara.EntityId;

                // If the object exists, reset the counter
                if(_charactorStatusInfos.TryGetValue(objectID, out var statusInfo))
                {
                    statusInfo.NoUpdateCount = 0; // Reset the counter as the object is confirmed to exist
                    _charactorStatusInfos[objectID] = statusInfo;
                }

                var statuses = battleChara.StatusList;

                // Compare the current and previous status lists
                CompareStatusList(objectID, statuses, out var changeStatuses);

                // Log the changes, including gameObject.Name
                LogChanges(battleChara, changeStatuses);

                // Save the current status list
                CopyStatusList(objectID, statuses);
            }
        }

        // Remove objects that have not been updated for 10 cycles
        RemoveInactiveObjects();
    }

    public static void DirectorCheck(DirectorUpdateCategory category)
    {
        if(category == DirectorUpdateCategory.Commence ||
            category == DirectorUpdateCategory.Wipe)
        {
            _isClearRequest = true;
        }
    }
    #endregion

    #region private
    private void IncrementNoUpdateCount()
    {
        // Increment the NoUpdateCount for all objects
        foreach(var key in _charactorStatusInfos.Keys)
        {
            var statusInfo = _charactorStatusInfos[key];
            statusInfo.NoUpdateCount++;
            _charactorStatusInfos[key] = statusInfo;
        }
    }

    private void RemoveInactiveObjects()
    {
        // Add objects with NoUpdateCount >= 10 to the removal list
        List<uint> toRemove = [];
        foreach(var kvp in _charactorStatusInfos)
        {
            if(kvp.Value.NoUpdateCount >= 10)
            {
                toRemove.Add(kvp.Key);
            }
        }

        // Actually remove the objects
        foreach(var objectID in toRemove)
        {
            _charactorStatusInfos.Remove(objectID);
        }
    }

    private void CopyStatusList(uint objectID, StatusList statuses)
    {
        var newStatusIds = GetStatusIds(statuses);

        if(_charactorStatusInfos.TryGetValue(objectID, out var existingInfo))
        {
            if(!ArraysEqual(existingInfo.Statuses, newStatusIds))
            {
                _charactorStatusInfos[objectID] = new CharacterStatusInfo
                {
                    ObjectID = objectID,
                    Statuses = newStatusIds,
                    NoUpdateCount = 0 // Reset the counter as it has been updated
                };
            }
        }
        else
        {
            _charactorStatusInfos.Add(objectID, new CharacterStatusInfo
            {
                ObjectID = objectID,
                Statuses = newStatusIds,
                NoUpdateCount = 0 // Set the counter to 0 when adding a new object
            });
        }
    }

    private StatusChangeResult CompareStatusList(uint objectID, StatusList statuses, out List<CharactorStatusDiffResult> changeStatuses)
    {
        changeStatuses = [];

        if(!_charactorStatusInfos.TryGetValue(objectID, out var existingInfo))
        {
            return StatusChangeResult.NoChange;
        }

        var currentStatusIds = GetStatusIds(statuses);

        CheckGains(currentStatusIds, existingInfo.Statuses, changeStatuses);
        CheckRemovals(currentStatusIds, existingInfo.Statuses, changeStatuses);

        return changeStatuses.Count > 0 ? StatusChangeResult.Change : StatusChangeResult.NoChange;
    }

    private RecordedStatus[] GetStatusIds(StatusList statuses)
    {
        var statusIds = new RecordedStatus[statuses.Length];
        for(var i = 0; i < statuses.Length; i++)
        {
            var status = statuses[i];
            statusIds[i] = status == null?default:new RecordedStatus(status.StatusId, status.StackCount);
        }
        return statusIds;
    }

    private void CheckGains(RecordedStatus[] currentStatusIds, RecordedStatus[] oldStatusIds, List<CharactorStatusDiffResult> changeStatuses)
    {
        for(var i = 0; i < currentStatusIds.Length; i++)
        {
            if(System.Array.IndexOf(oldStatusIds, currentStatusIds[i]) < 0)
            {
                changeStatuses.Add(new CharactorStatusDiffResult
                {
                    StatusId = currentStatusIds[i],
                    ChangeType = StatusChangeType.Gain
                });
            }
        }
    }

    private void CheckRemovals(RecordedStatus[] currentStatusIds, RecordedStatus[] oldStatusIds, List<CharactorStatusDiffResult> changeStatuses)
    {
        for(var i = 0; i < oldStatusIds.Length; i++)
        {
            if(System.Array.IndexOf(currentStatusIds, oldStatusIds[i]) < 0)
            {
                changeStatuses.Add(new CharactorStatusDiffResult
                {
                    StatusId = oldStatusIds[i],
                    ChangeType = StatusChangeType.Remove
                });
            }
        }
    }

    private bool ArraysEqual(RecordedStatus[] array1, RecordedStatus[] array2)
    {
        if(array1.Length != array2.Length)
            return false;

        for(var i = 0; i < array1.Length; i++)
        {
            if(array1[i] != array2[i])
                return false;
        }
        return true;
    }

    // Updated LogChanges method
    private void LogChanges(IBattleChara battleChara, List<CharactorStatusDiffResult> changeStatuses)
    {
        List<RecordedStatus> gainStatusIds = [];
        List<RecordedStatus> removeStatusIds = [];
        var isPlayer = battleChara is IPlayerCharacter;
        var pc = battleChara as IPlayerCharacter;

        foreach(var changeStatus in changeStatuses)
        {
            switch(changeStatus.ChangeType)
            {
                case StatusChangeType.Gain:
                    gainStatusIds.Add(changeStatus.StatusId);
                    break;
                case StatusChangeType.Remove:
                    removeStatusIds.Add(changeStatus.StatusId);
                    break;
            }
        }

        if(gainStatusIds.Count > 0)
        {
            string text;

            if(P.Config.LogPosition)
            {
                foreach(var statusId in gainStatusIds)
                {
                    if(isPlayer && Svc.ClientState.LocalPlayer != null && Svc.ClientState.LocalPlayer.Address == pc.Address)
                    {
                        text = $"You ({battleChara.Position.ToString()}) gain the effect of {statusId} ([buff+]You:{statusId}:{pc.GetJob().ToString()})";
                        P.ChatMessageQueue.Enqueue(text);
                    }

                    if(isPlayer)
                    {
                        text = $"{battleChara.Name} ({battleChara.Position.ToString()}) gains the effect of {statusId} ([buff+]{ObjectFunctions.GetNameplateKind(pc).ToString()}:{statusId}:{pc.GetJob().ToString()})";
                    }
                    else
                    {
                        text = $"{battleChara.Name} ({battleChara.Position.ToString()}) gains the effect of {statusId} ([buff+]{battleChara.NameId}:{statusId})";
                    }
                    P.ChatMessageQueue.Enqueue(text);
                }
            }
            else
            {
                foreach(var statusId in gainStatusIds)
                {
                    if(isPlayer && Svc.ClientState.LocalPlayer != null && Svc.ClientState.LocalPlayer.Address == pc.Address)
                    {
                        text = $"You gain the effect of {statusId} ([buff+]You:{statusId}:{pc.GetJob().ToString()})";
                        P.ChatMessageQueue.Enqueue(text);
                    }

                    if(isPlayer)
                    {
                        text = $"{battleChara.Name} gains the effect of {statusId} ([buff+]{ObjectFunctions.GetNameplateKind(pc).ToString()}:{statusId}:{pc.GetJob().ToString()})";
                    }
                    else
                    {
                        text = $"{battleChara.Name} gains the effect of {statusId} ([buff+]{battleChara.NameId}:{statusId})";
                    }
                    P.ChatMessageQueue.Enqueue(text);
                }
            }
            ScriptingProcessor.OnGainBuffEffect(battleChara.EntityId, gainStatusIds);
        }

        if(removeStatusIds.Count > 0)
        {
            string text;
            if(P.Config.LogPosition)
            {
                foreach(var statusId in removeStatusIds)
                {
                    if(isPlayer && Svc.ClientState.LocalPlayer != null && Svc.ClientState.LocalPlayer.Address == pc.Address)
                    {
                        text = $"You ({battleChara.Position.ToString()}) loses the effect of {statusId} ([buff-]You:{statusId}:{pc.GetJob().ToString()})";
                        P.ChatMessageQueue.Enqueue(text);
                    }

                    if(isPlayer)
                    {
                        text = $"{battleChara.Name} ({battleChara.Position.ToString()}) loses the effect of {statusId} ([buff-]{ObjectFunctions.GetNameplateKind(pc).ToString()}:{statusId}:{pc.GetJob().ToString()})";
                    }
                    else
                    {
                        text = $"{battleChara.Name} ({battleChara.Position.ToString()}) loses the effect of {statusId} ([buff-]{battleChara.NameId}:{statusId})";
                    }
                    P.ChatMessageQueue.Enqueue(text);
                }
            }
            else
            {
                foreach(var statusId in removeStatusIds)
                {
                    if(isPlayer && Svc.ClientState.LocalPlayer != null && Svc.ClientState.LocalPlayer.Address == pc.Address)
                    {
                        text = $"You lose the effect of {statusId} ([buff-]You:{statusId}:{pc.GetJob().ToString()})";
                        P.ChatMessageQueue.Enqueue(text);
                    }

                    if(isPlayer)
                    {
                        text = $"{battleChara.Name} loses the effect of {statusId} ([buff-]{ObjectFunctions.GetNameplateKind(pc).ToString()}:{statusId}:{pc.GetJob().ToString()})";
                    }
                    else
                    {
                        text = $"{battleChara.Name} loses the effect of {statusId} ([buff-]{battleChara.NameId}:{statusId})";
                    }
                    P.ChatMessageQueue.Enqueue(text);
                }
            }
            ScriptingProcessor.OnRemoveBuffEffect(battleChara.EntityId, removeStatusIds);
        }
    }
    #endregion
}
