using Dalamud.Game.ClientState.Statuses;
using ECommons.DalamudServices;
using ECommons.Logging;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
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
        public uint[] StatusIds;  // statuses.Lengthに基づいて長さを決定
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
        var player = Svc.ClientState.LocalPlayer;
        if(player == null)
            return;

        var objectID = player.EntityId;
        var statuses = player.StatusList;

        // 状態リストの比較
        CompareStatusList(objectID, statuses, out var changeStatuses);
        LogChanges(changeStatuses);

        // 現在のステータスリストを保存
        CopyStatusList(objectID, statuses);
    }

    public override void OnSettingsDraw()
    {
        var statuses = Svc.ClientState.LocalPlayer?.StatusList;
        if(statuses == null || statuses.Length == 0)
        {
            ImGui.Text("StatusList is null or empty");
            return;
        }

        for(int i = 0; i < statuses.Length; i++)
        {
            ImGui.Text($"Status {i}: {statuses[i]?.StatusId}");
        }
    }
    #endregion

    #region private
    private void CopyStatusList(uint objectID, StatusList statuses)
    {
        // ステータスIDを取得
        var newStatusIds = GetStatusIds(statuses);

        // 既存の情報がある場合は更新、なければ新規追加
        if(_charactorStatusInfos.TryGetValue(objectID, out var existingInfo))
        {
            if(!ArraysEqual(existingInfo.StatusIds, newStatusIds))
            {
                _charactorStatusInfos[objectID] = new CharactorStatusInfo
                {
                    ObjectID = objectID,
                    StatusIds = newStatusIds
                };
            }
        }
        else
        {
            _charactorStatusInfos.Add(objectID, new CharactorStatusInfo
            {
                ObjectID = objectID,
                StatusIds = newStatusIds
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

        // gain確認
        CheckGains(currentStatusIds, existingInfo.StatusIds, changeStatuses);

        // remove確認
        CheckRemovals(currentStatusIds, existingInfo.StatusIds, changeStatuses);

        return changeStatuses.Count > 0 ? StatusChangeResult.Change : StatusChangeResult.NoChange;
    }

    private uint[] GetStatusIds(StatusList statuses)
    {
        // statuses.Lengthに基づいて配列の長さを決定
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
            if(Array.IndexOf(oldStatusIds, currentStatusIds[i]) < 0)
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
            if(Array.IndexOf(currentStatusIds, oldStatusIds[i]) < 0)
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
            {
                return false;
            }
        }
        return true;
    }

    private void LogChanges(List<CharactorStatusDiffResult> changeStatuses)
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
            PluginLog.Information($"Gained statuses: {string.Join(", ", gainStatusIds)}");
        }

        if(removeStatusIds.Count > 0)
        {
            PluginLog.Information($"Removed statuses: {string.Join(", ", removeStatusIds)}");
        }
    }
    #endregion
}
