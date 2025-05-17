using Dalamud.Game.ClientState.Conditions;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using Splatoon.Memory;
using Splatoon.Structures;
using System.Text.RegularExpressions;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.VfxContainer;

namespace Splatoon.Utility;
public static unsafe class LayoutUtils
{
    public static bool IsAttributeMatches(Element e, IGameObject o)
    {
        if(e.refActorComparisonAnd)
        {
            return (e.refActorNameIntl.Get(e.refActorName) == String.Empty || IsNameMatches(e, o)) &&
             (e.refActorModelID == 0 || (o is ICharacter c && c.Struct()->ModelContainer.ModelCharaId == e.refActorModelID)) &&
             (e.refActorObjectID == 0 || o.EntityId == e.refActorObjectID) &&
             (e.refActorDataID == 0 || o.DataId == e.refActorDataID) &&
             (e.refActorNPCID == 0 || o.Struct()->GetNameId() == e.refActorNPCID) &&
             (e.refActorPlaceholder.Count == 0 || e.refActorPlaceholder.Any(x => ResolvePlaceholder(x) == o.Address)) &&
             (e.refActorNPCNameID == 0 || (o is ICharacter c2 && c2.NameId == e.refActorNPCNameID)) &&
             (e.refActorVFXPath == "" || (AttachedInfo.TryGetSpecificVfxInfo(o, e.refActorVFXPath, out var info) && info.Age.InRange(e.refActorVFXMin, e.refActorVFXMax))) &&
             ((e.refActorObjectEffectData1 == 0 && e.refActorObjectEffectData2 == 0) || (AttachedInfo.ObjectEffectInfos.TryGetValue(o.Address, out var einfo) && IsObjectEffectMatches(e, o, einfo)) &&
             (e.refActorNamePlateIconID == 0 || o.Struct()->NamePlateIconId == e.refActorNamePlateIconID));
        }
        else
        {
            if(e.refActorComparisonType == 0 && IsNameMatches(e, o)) return true;
            if(e.refActorComparisonType == 1 && o is ICharacter c && c.Struct()->ModelContainer.ModelCharaId == e.refActorModelID) return true;
            if(e.refActorComparisonType == 2 && o.EntityId == e.refActorObjectID) return true;
            if(e.refActorComparisonType == 3 && o.DataId == e.refActorDataID) return true;
            if(e.refActorComparisonType == 4 && o.Struct()->GetNameId() == e.refActorNPCID) return true;
            if(e.refActorComparisonType == 5 && e.refActorPlaceholder.Any(x => ResolvePlaceholder(x) == o.Address)) return true;
            if(e.refActorComparisonType == 6 && o is ICharacter c2 && c2.NameId == e.refActorNPCNameID) return true;
            if(e.refActorComparisonType == 7 && AttachedInfo.TryGetSpecificVfxInfo(o, e.refActorVFXPath, out var info) && info.Age.InRange(e.refActorVFXMin, e.refActorVFXMax)) return true;
            if(e.refActorComparisonType == 8 && AttachedInfo.ObjectEffectInfos.TryGetValue(o.Address, out var einfo) && IsObjectEffectMatches(e, o, einfo)) return true;
            if(e.refActorComparisonType == 9 && o.Struct()->NamePlateIconId == e.refActorNamePlateIconID) return true;
            return false;
        }
    }

    public static bool IsObjectEffectMatches(Element e, IGameObject o, List<CachedObjectEffectInfo> info)
    {
        if(e.refActorObjectEffectLastOnly)
        {
            if(info.Count > 0)
            {
                var last = info[info.Count - 1];
                return last.data1 == e.refActorObjectEffectData1 && last.data2 == e.refActorObjectEffectData2;
            }
            return false;
        }
        else
        {
            return info.Any(last => last.data1 == e.refActorObjectEffectData1 && last.data2 == e.refActorObjectEffectData2 && last.Age.InRange(e.refActorObjectEffectMin, e.refActorObjectEffectMax));
        }
    }

    public static bool IsNameMatches(Element e, IGameObject o)
    {
        return !string.IsNullOrEmpty(e.refActorNameIntl.Get(e.refActorName)) && (e.refActorNameIntl.Get(e.refActorName) == "*" || o.Name.ToString().ContainsIgnoreCase(e.refActorNameIntl.Get(e.refActorName)));
    }

    public static nint ResolvePlaceholder(string ph)
    {
        if(PlaceholderCache.TryGetValue(ph, out var val))
        {
            return val;
        }
        else
        {
            var result = (nint)FakePronoun.Resolve(ph);
            PlaceholderCache[ph] = result;
            return result;
        }
    }

    public static bool CheckTargetingOption(Element e, IGameObject a)
    {
        if(e.refTargetYou)
        {
            return ((e.refActorTargetingYou == 1 && a.TargetObjectId != Svc.ClientState.LocalPlayer.EntityId) || (e.refActorTargetingYou == 2 && a.TargetObjectId == Svc.ClientState.LocalPlayer.EntityId));
        }

        return false;
    }

    public static bool CheckCharacterAttributes(Element e, IGameObject a, bool ignoreVisibility = false)
    {
        return
            (ignoreVisibility || !e.onlyVisible || (a is ICharacter chr && chr.IsCharacterVisible()))
            && (!e.refActorRequireCast || (e.refActorCastId.Count > 0 && a is IBattleChara chr2 && IsCastingMatches(e, chr2) != e.refActorCastReverse))
            && (!e.refActorRequireBuff || (e.refActorBuffId.Count > 0 && a is IBattleChara chr3 && CheckEffect(e, chr3)))
            && (!e.refActorUseTransformation || (a is IBattleChara chr4 && CheckTransformationID(e, chr4)))
            && (!e.refMark || (a is IBattleChara chr5 && Marking.HaveMark(chr5, (uint)e.refMarkID)))
            && (!e.LimitRotation || (a.Rotation >= e.RotationMax && a.Rotation <= e.RotationMin))
            && (!e.refActorTether || IsTetherMatches(e, a) == !e.refActorIsTetherInvert);
    }

    public static bool IsTetherMatches(Element e, IGameObject obj)
    {
        if(e.refActorIsTetherSource == null || e.refActorIsTetherSource == true)
        {
            if(AttachedInfo.TetherInfos.TryGetValue(obj.Address, out var tethers))
            {
                foreach(var t in tethers)
                {
                    if(t.AgeF >= e.refActorTetherTimeMin && t.AgeF <= e.refActorTetherTimeMax
                        && (e.refActorTetherParam1 == null || e.refActorTetherParam1 == t.Param1)
                        && (e.refActorTetherParam2 == null || e.refActorTetherParam2 == t.Param2)
                        && (e.refActorTetherParam3 == null || e.refActorTetherParam3 == t.Param3)
                        )
                    {
                        if(e.refActorTetherConnectedWithPlayer.Count == 0)
                        {
                            return true;
                        }
                        else
                        {
                            foreach(var p in e.refActorTetherConnectedWithPlayer)
                            {
                                var tar = FakePronoun.Resolve(p);
                                if(tar != null)
                                {
                                    if(t.Target == tar->EntityId) return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        if(e.refActorIsTetherSource == null || e.refActorIsTetherSource == false)
        {
            //reverse lookup goes brrrrr
            foreach(var x in AttachedInfo.TetherInfos)
            {
                if(x.Key == obj.Address) continue;
                foreach(var t in x.Value)
                {
                    if(t.AgeF >= e.refActorTetherTimeMin && t.AgeF <= e.refActorTetherTimeMax
                        && (e.refActorTetherParam1 == null || e.refActorTetherParam1 == t.Param1)
                        && (e.refActorTetherParam2 == null || e.refActorTetherParam2 == t.Param2)
                        && (e.refActorTetherParam3 == null || e.refActorTetherParam3 == t.Param3)
                        && t.Target == obj.EntityId
                        )
                    {
                        if(e.refActorTetherConnectedWithPlayer.Count == 0)
                        {
                            return true;
                        }
                        else
                        {
                            foreach(var p in e.refActorTetherConnectedWithPlayer)
                            {
                                var tar = FakePronoun.Resolve(p);
                                if(tar != null)
                                {
                                    if(x.Key == (nint)tar) return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    public static bool CheckTransformationID(Element e, ICharacter c)
    {
        return e.refActorTransformationID == c.GetTransformationID();
    }

    public static bool IsCastingMatches(Element e, IBattleChara chr)
    {
        if(chr == null) return false;
        if(chr.IsCasting(e.refActorCastId))
        {
            if(e.refActorUseCastTime)
            {
                return chr.IsCastInRange(e.refActorCastTimeMin, e.refActorCastTimeMax);
            }
            else
            {
                return true;
            }
        }
        else
        {
            if(e.refActorUseOvercast)
            {
                if(AttachedInfo.TryGetCastTime(chr.Address, e.refActorCastId, out var castTime))
                {
                    return castTime.InRange(e.refActorCastTimeMin, e.refActorCastTimeMax);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }

    public static float CastFraction(Element e, IGameObject go)
    {
        if(go is IBattleChara chr)
        {
            float castTime = -1;
            float totalCastTime = 1;
            if(chr.IsCasting(e.refActorCastId))
            {
                castTime = chr.CurrentCastTime;
                totalCastTime = chr.TotalCastTime;
            }
            else if(!(e.refActorUseOvercast && AttachedInfo.TryGetCastTime(chr.Address, e.refActorCastId, out castTime)))
            {
                return 0;
            }

            if(e.refActorUseCastTime)
            {
                castTime -= e.refActorCastTimeMin;
                totalCastTime = e.refActorCastTimeMax - e.refActorCastTimeMin;
            }
            if(castTime <= 0 || totalCastTime <= 0) return 0;
            if(castTime > totalCastTime) return 1;

            return castTime / totalCastTime;
        }
        return 0;
    }

    public static bool CheckEffect(Element e, IBattleChara c)
    {
        if(e.refActorRequireAllBuffs)
        {
            if(e.refActorUseBuffTime)
            {
                return c.StatusList.Where(x => x.RemainingTime.InRange(e.refActorBuffTimeMin, e.refActorBuffTimeMax) && (!e.refActorUseBuffParam || x.Param == e.refActorBuffParam)).Select(x => x.StatusId).ContainsAll(e.refActorBuffId).Invert(e.refActorRequireBuffsInvert);
            }
            else
            {
                return c.StatusList.Where(x => !e.refActorUseBuffParam || x.Param == e.refActorBuffParam).Select(x => x.StatusId).ContainsAll(e.refActorBuffId).Invert(e.refActorRequireBuffsInvert);
            }
        }
        else
        {
            if(e.refActorUseBuffTime)
            {
                return c.StatusList.Where(x => x.RemainingTime.InRange(e.refActorBuffTimeMin, e.refActorBuffTimeMax) && (!e.refActorUseBuffParam || x.Param == e.refActorBuffParam)).Select(x => x.StatusId).ContainsAny(e.refActorBuffId).Invert(e.refActorRequireBuffsInvert);
            }
            else
            {
                return c.StatusList.Where(x => !e.refActorUseBuffParam || x.Param == e.refActorBuffParam).Select(x => x.StatusId).ContainsAny(e.refActorBuffId).Invert(e.refActorRequireBuffsInvert);
            }
        }
    }

    public static bool IsLayoutVisible(Layout layout)
    {
        if(!layout.Enabled) return false;
        if(!layout.Group.IsNullOrEmpty() && P.Config.DisabledGroups.Contains(layout.Group)) return false;
        if(layout.DisableInDuty && Svc.Condition[ConditionFlag.BoundByDuty]) return false;
        if((layout.ZoneLockH.Count > 0 && !layout.ZoneLockH.Contains(Svc.ClientState.TerritoryType)).Invert(layout.IsZoneBlacklist)) return false;
        if(layout.Scenes.Count > 0 && !layout.Scenes.Contains(*Scene.ActiveScene)) return false;
        if(layout.Phase != 0 && layout.Phase != P.Phase) return false;
        if(layout.JobLockH.Count > 0 && !layout.JobLockH.Contains(Player.Job)) return false;
        if((layout.DCond == 1 || layout.DCond == 3) && !Svc.Condition[ConditionFlag.InCombat]) return false;
        if((layout.DCond == 2 || layout.DCond == 3) && !Svc.Condition[ConditionFlag.BoundByDuty]) return false;
        if(layout.DCond == 4 && !(Svc.Condition[ConditionFlag.InCombat]
            || Svc.Condition[ConditionFlag.BoundByDuty])) return false;
        if(layout.UseDistanceLimit && layout.DistanceLimitType == 0)
        {
            if(Svc.Targets.Target != null)
            {
                var dist = Vector3.Distance(Svc.Targets.Target.GetPositionXZY(), Utils.GetPlayerPositionXZY()) - (layout.DistanceLimitTargetHitbox ? Svc.Targets.Target.HitboxRadius : 0) - (layout.DistanceLimitMyHitbox ? Svc.ClientState.LocalPlayer.HitboxRadius : 0);
                if(!(dist >= layout.MinDistance && dist < layout.MaxDistance)) return false;
            }
            else
            {
                return false;
            }
        }
        if(layout.UseTriggers)
        {
            foreach(var t in layout.Triggers)
            {
                if(t.FiredState == 2) continue;
                if((t.Type == 2 || t.Type == 3) && !t.Disabled)
                {
                    foreach(var CurrentChatMessage in P.CurrentChatMessages)
                    {
                        var trg = t.MatchIntl.Get(t.Match);
                        if(trg != string.Empty &&
                            (t.IsRegex ? Regex.IsMatch(CurrentChatMessage, trg) : CurrentChatMessage.ContainsIgnoreCase(trg))
                            )
                        {
                            if(t.Duration == 0)
                            {
                                t.FiredState = 0;
                            }
                            else
                            {
                                t.FiredState = 1;
                                t.DisableAt.Add(Environment.TickCount64 + (int)(t.Duration * 1000) + (int)(t.MatchDelay * 1000));
                            }
                            if(t.MatchDelay != 0)
                            {
                                t.EnableAt.Add(Environment.TickCount64 + (int)(t.MatchDelay * 1000));
                            }
                            else
                            {
                                layout.TriggerCondition = t.Type == 2 ? 1 : -1;
                            }
                            if(t.FireOnce)
                            {
                                t.Disabled = true;
                            }
                        }
                    }
                }
                if(t.FiredState == 0 && (t.Type == 0 || t.Type == 1))
                {
                    if(P.CombatStarted != 0 && Environment.TickCount64 - P.CombatStarted > t.TimeBegin * 1000)
                    {
                        if(t.Duration == 0)
                        {
                            t.FiredState = 2;
                        }
                        else
                        {
                            t.FiredState = 1;
                            t.DisableAt.Add(Environment.TickCount64 + (int)(t.Duration * 1000));
                        }
                        layout.TriggerCondition = t.Type == 0 ? 1 : -1;
                    }
                }
                for(var e = 0; e < t.EnableAt.Count; e++)
                {
                    if(Environment.TickCount64 > t.EnableAt[e])
                    {
                        layout.TriggerCondition = t.Type == 2 ? 1 : -1;
                        t.EnableAt.RemoveAt(e);
                        break;
                    }
                }
                for(var e = 0; e < t.DisableAt.Count; e++)
                {
                    if(Environment.TickCount64 > t.DisableAt[e])
                    {
                        t.FiredState = (t.Type == 2 || t.Type == 3) ? 0 : 2;
                        t.DisableAt.RemoveAt(e);
                        layout.TriggerCondition = 0;
                        break;
                    }
                }

            }
            if(layout.TriggerCondition == -1 || (layout.TriggerCondition == 0 && layout.DCond == 5)) return false;
        }
        return true;
    }

    public static bool CheckDistanceCondition(Layout i, float x, float y, float z)
    {
        return CheckDistanceCondition(i, new Vector3(x, y, z));
    }

    public static bool CheckDistanceCondition(Layout i, Vector3 v)
    {
        if(i.DistanceLimitType != 1) return true;
        var dist = Vector3.Distance(v, Utils.GetPlayerPositionXZY());
        if(!(dist >= i.MinDistance && dist < i.MaxDistance)) return false;
        return true;
    }

    public static bool CheckDistanceToLineCondition(Layout i, Element e)
    {
        if(i.DistanceLimitType != 1) return true;
        var dist = Vector3.Distance(Utils.FindClosestPointOnLine(Utils.GetPlayerPositionXZY(), new Vector3(e.refX, e.refY, e.refZ), new Vector3(e.offX, e.offY, e.offZ)), Utils.GetPlayerPositionXZY());
        if(!(dist >= i.MinDistance && dist < i.MaxDistance)) return false;
        return true;
    }

    public static bool ShouldDraw(float x1, float x2, float y1, float y2)
    {
        return ((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)) < P.Config.maxdistance * P.Config.maxdistance;
    }
}
