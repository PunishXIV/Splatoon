using Dalamud.Game.ClientState.Statuses;
using ECommons.Hooks;
using Splatoon.SplatoonScripting;

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

    private struct CharactorStatusInfo
    {
        public uint ObjectID;
        public uint[] StatusIds;
        public int NoUpdateCount;
    }

    private struct CharactorStatusDiffResult
    {
        public uint StatusId;
        public StatusChangeType ChangeType;
    }
    #endregion

    #region privateDefine
    private Dictionary<uint, CharactorStatusInfo> _charactorStatusInfos = new Dictionary<uint, CharactorStatusInfo>();
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

        // すべてのオブジェクトのNoUpdateCountをインクリメント
        IncrementNoUpdateCount();

        // Svc.Objects 内のすべてのオブジェクトをループ
        foreach(var gameObject in Svc.Objects)
        {
            if(gameObject == null)
                continue;
            // IBattleChara にキャストできるか確認
            if(gameObject is IBattleChara battleChara)
            {
                var objectID = battleChara.EntityId;

                // オブジェクトが存在するのでカウンタをリセット
                if(_charactorStatusInfos.TryGetValue(objectID, out var statusInfo))
                {
                    statusInfo.NoUpdateCount = 0; // 存在が確認されたのでカウンタをリセット
                    _charactorStatusInfos[objectID] = statusInfo;
                }

                var statuses = battleChara.StatusList;

                // 状態リストの比較
                CompareStatusList(objectID, statuses, out var changeStatuses);

                // gameObject.Name を含めてログに表示
                LogChanges(battleChara, changeStatuses);

                // 現在のステータスリストを保存
                CopyStatusList(objectID, statuses);
            }
        }

        // 10周期以上更新がないオブジェクトを削除
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
        // すべてのオブジェクトのNoUpdateCountをインクリメント
        foreach(var key in _charactorStatusInfos.Keys)
        {
            var statusInfo = _charactorStatusInfos[key];
            statusInfo.NoUpdateCount++;
            _charactorStatusInfos[key] = statusInfo;
        }
    }

    private void RemoveInactiveObjects()
    {
        // NoUpdateCountが10以上のオブジェクトをリストに格納して後で削除
        List<uint> toRemove = new List<uint>();
        foreach(var kvp in _charactorStatusInfos)
        {
            if(kvp.Value.NoUpdateCount >= 10)
            {
                toRemove.Add(kvp.Key);
            }
        }

        // 実際に削除する
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
                    NoUpdateCount = 0 // 更新されたのでカウンタをリセット
                };
            }
        }
        else
        {
            _charactorStatusInfos.Add(objectID, new CharactorStatusInfo
            {
                ObjectID = objectID,
                StatusIds = newStatusIds,
                NoUpdateCount = 0 // 新規に追加する際にカウンタをリセット
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

    // LogChanges メソッドを更新
    private void LogChanges(IBattleChara battleChara, List<CharactorStatusDiffResult> changeStatuses)
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
            string text;
            if(P.Config.LogPosition)
            {
                foreach(var statusId in gainStatusIds)
                {
                    text = $"{battleChara.Name} ({battleChara.Position.ToString()}) gains the effect of {statusId} ({battleChara.NameId}:+{statusId})";
                    P.ChatMessageQueue.Enqueue(text);
                }
            }
            else
            {
                foreach(var statusId in gainStatusIds)
                {
                    text = $"{battleChara.Name} gains the effect of {statusId} ({battleChara.NameId}:+{statusId})";
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
                    text = $"{battleChara.Name} ({battleChara.Position.ToString()}) loses the effect of {statusId} ({battleChara.NameId}:-{statusId})";
                    P.ChatMessageQueue.Enqueue(text);
                }
            }
            else
            {
                foreach(var statusId in removeStatusIds)
                {
                    text = $"{battleChara.Name} loses the effect of {statusId} ({battleChara.NameId}:-{statusId})";
                    P.ChatMessageQueue.Enqueue(text);
                }
            }
            ScriptingProcessor.OnRemoveBuffEffect(battleChara.EntityId, removeStatusIds);
        }
    }
    #endregion
}
