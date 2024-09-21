using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using ECommons.DalamudServices;
using ECommons.Logging;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Tests;
internal class StatusListMonitoring :SplatoonScript
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

    private struct CharactorStatusInfo
    {
        public uint ObjectID;
        public uint[] StatusIds;
        public int NoUpdateCount;  // Changed to int, and object will be removed after 10 cycles without updates
    }

    private struct CharactorStatusDiffResult
    {
        public uint StatusId;
        public StatusChangeType ChangeType;
    }
    #endregion

    public override HashSet<uint>? ValidTerritories => null;

    private Dictionary<uint, CharactorStatusInfo> _charactorStatusInfos = new Dictionary<uint, CharactorStatusInfo>();

    #region public
    public override void OnUpdate()
    {
        // Increment NoUpdateCount for all objects
        IncrementNoUpdateCount();

        // Loop through all objects in Svc.Objects
        foreach(var gameObject in Svc.Objects)
        {
            // Check if the object can be cast to IBattleChara
            if(gameObject is IBattleChara battleChara)
            {
                var objectID = battleChara.EntityId;

                // Reset the counter if the object exists
                if(_charactorStatusInfos.TryGetValue(objectID, out var statusInfo))
                {
                    statusInfo.NoUpdateCount = 0; // Reset the counter as the object is confirmed to exist
                    _charactorStatusInfos[objectID] = statusInfo;
                }

                var statuses = battleChara.StatusList;

                // Compare the current and previous status lists
                CompareStatusList(objectID, statuses, out var changeStatuses);

                // Log changes, including gameObject.Name
                LogChanges(gameObject, changeStatuses);

                // Save the current status list
                CopyStatusList(objectID, statuses);
            }
        }

        // Remove objects that have not been updated for 10 cycles
        RemoveInactiveObjects();
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text($"Active objects: {_charactorStatusInfos.Count}");
    }
    #endregion

    #region private
    private void IncrementNoUpdateCount()
    {
        // Increment NoUpdateCount for all objects
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
        List<uint> toRemove = new List<uint>();
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
            PluginLog.Information($"Removed object with ID: {objectID} (inactive for 10 cycles)");
        }
    }

    private void CopyStatusList(uint objectID, StatusList statuses)
    {
        var newStatusIds = GetStatusIds(statuses);

        if(_charactorStatusInfos.TryGetValue(objectID, out var existingInfo))
        {
            if(!ArraysEqual(existingInfo.StatusIds, newStatusIds))
            {
                _charactorStatusInfos[objectID] = new CharactorStatusInfo
                {
                    ObjectID = objectID,
                    StatusIds = newStatusIds,
                    NoUpdateCount = 0 // Reset the counter as it has been updated
                };
            }
        }
        else
        {
            _charactorStatusInfos.Add(objectID, new CharactorStatusInfo
            {
                ObjectID = objectID,
                StatusIds = newStatusIds,
                NoUpdateCount = 0 // Set the counter to 0 when adding a new object
            });
        }
    }

    private StatusChangeResult CompareStatusList(uint objectID, StatusList statuses, out List<CharactorStatusDiffResult> changeStatuses)
    {
        changeStatuses = new List<CharactorStatusDiffResult>();

        if(!_charactorStatusInfos.TryGetValue(objectID, out var existingInfo))
        {
            return StatusChangeResult.NoChange;
        }

        var currentStatusIds = GetStatusIds(statuses);

        CheckGains(currentStatusIds, existingInfo.StatusIds, changeStatuses);
        CheckRemovals(currentStatusIds, existingInfo.StatusIds, changeStatuses);

        return changeStatuses.Count > 0 ? StatusChangeResult.Change : StatusChangeResult.NoChange;
    }

    private uint[] GetStatusIds(StatusList statuses)
    {
        var statusIds = new uint[statuses.Length];
        for(int i = 0; i < statuses.Length; i++)
        {
            statusIds[i] = statuses[i]?.StatusId ?? 0;
        }
        return statusIds;
    }

    private void CheckGains(uint[] currentStatusIds, uint[] oldStatusIds, List<CharactorStatusDiffResult> changeStatuses)
    {
        for(int i = 0; i < currentStatusIds.Length; i++)
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

    private void CheckRemovals(uint[] currentStatusIds, uint[] oldStatusIds, List<CharactorStatusDiffResult> changeStatuses)
    {
        for(int i = 0; i < oldStatusIds.Length; i++)
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

    private bool ArraysEqual(uint[] array1, uint[] array2)
    {
        if(array1.Length != array2.Length)
            return false;

        for(int i = 0; i < array1.Length; i++)
        {
            if(array1[i] != array2[i])
                return false;
        }
        return true;
    }

    // Updated LogChanges method
    private void LogChanges(IGameObject gameObject, List<CharactorStatusDiffResult> changeStatuses)
    {
        List<uint> gainStatusIds = new List<uint>();
        List<uint> removeStatusIds = new List<uint>();

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
            PluginLog.Information($"[{gameObject.Name}({gameObject.EntityId})] Gained statuses: {string.Join(", ", gainStatusIds)}");
        }

        if(removeStatusIds.Count > 0)
        {
            PluginLog.Information($"[{gameObject.Name}({gameObject.EntityId})] Removed statuses: {string.Join(", ", removeStatusIds)}");
        }
    }
    #endregion
}
