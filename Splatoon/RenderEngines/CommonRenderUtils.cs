using Dalamud.Game.ClientState.Objects.Types;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.MathHelpers;
using ECommons.ObjectLifeTracker;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Splatoon.RenderEngines;
/// <summary>
/// This class contains render utils that are commonly used across all engines
/// </summary>
public static unsafe class CommonRenderUtils
{
    internal static string ProcessPlaceholders(this string s, IGameObject go)
    {
        var ret = s
        .Replace("$OBJECTID", $"{go.EntityId.Format()}")
        .Replace("$DATAID", $"{go.DataId.Format()}")
        .Replace("$HITBOXR", $"{go.HitboxRadius:F1}")
        .Replace("$KIND", $"{go.ObjectKind}")
        .Replace("$NPCID", $"{go.Struct()->GetNameId().Format()}")
        .Replace("$LIFE", $"{go.GetLifeTimeSeconds():F1}")
        .Replace("$DISTANCE", $"{Vector3.Distance((BasePlayer?.Position ?? Vector3.Zero), go.Position):F1}")
        .Replace("\\n", "\n")
        .Replace("$MSTATUS", $"{(*(int*)(go.Address + 0x104)).Format()}");
        if(go is IBattleChara chr)
        {
            ret = ret
            .Replace("$MODELID", $"{chr.ModelId.Format()}")
            .Replace("$NAMEID", $"{chr.NameId.Format()}")
            .Replace("$STLP", $"{chr.StatusLoop.Format()}")
            .Replace("$TETHER", $"{chr.Struct()->Vfx.Tethers.ToArray().Where(x => x.Id != 0).Select(x => $"{x.Id}").Print(",")}")
            .Replace("$TRANSFORM", $"{((int)chr.GetTransformationID()).Format()}");
            if(ret.Contains("$STREM:"))
            {
                try
                {
                    var match = Regex.Match(ret, @"\$STREM:(\d+):(.*?)\$");
                    if(match.Success && int.TryParse(match.Groups[1].Value, out var statusId) && chr.StatusList.TryGetFirst(s => s.StatusId == statusId, out var status))
                    {
                        ret = ret.Replace(match.Groups[0].Value, $"{status.RemainingTime.ToString(match.Groups[2].Value)}");
                    }
                }
                catch(Exception e)
                {
                    e.Log();
                }
            }
            if(ret.Contains("$CAST:"))
            {
                try
                {
                    var match = Regex.Match(ret, @"\$CAST:(.*?)\$");
                    if(match.Success)
                    {
                        if(chr.IsCasting())
                        {
                            ret = ret.Replace(match.Groups[0].Value, $"{(chr.TotalCastTime - chr.CurrentCastTime).ToString(match.Groups[1].Value)}")
                                .Replace("$CASTNAME", ExcelActionHelper.GetActionName(chr.CastActionId));
                        }
                        else
                        {
                            ret = ret.Replace(match.Groups[0].Value, "").Replace("$CASTNAME", ExcelActionHelper.GetActionName(chr.CastActionId));
                        }
                    }
                    else
                    {
                        castFallback();
                    }
                }
                catch(Exception e)
                {
                    e.Log();
                    castFallback();
                }
            }
            else
            {
                castFallback();
            }
            void castFallback()
            {
                ret = ret.Replace("$CAST", chr.Struct()->GetCastInfo() != null ? $"[{chr.CastActionId.Format()}] {chr.CurrentCastTime}/{chr.TotalCastTime}" : "");
            }
        }
        ret = ret
            .Replace("$NAME", go.Name.ToString());
        return ret;
    }

    /// <summary>
    /// Accepts: Z-height vector. Returns: Z-height vector.
    /// </summary>
    /// <param name="tPos"></param>
    /// <param name="e"></param>
    /// <param name="hitboxRadius"></param>
    /// <param name="angle"></param>
    /// <returns></returns>
    internal static (Vector3 pointA, Vector3 pointB) GetRotatedPointsForZeroRadius(Vector3 tPos, Element e, float hitboxRadius, float angle)
    {
        var pointA = Utils.RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.refX,
                    tPos.Y + e.refY,
                    tPos.Z + e.refZ) + new Vector3(e.LineAddHitboxLengthXA ? hitboxRadius : 0f, e.LineAddHitboxLengthYA ? hitboxRadius : 0f, e.LineAddHitboxLengthZA ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthXA ? BasePlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthYA ? BasePlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZA ? BasePlayer.HitboxRadius : 0f));
        var pointB = Utils.RotatePoint(tPos.X, tPos.Y,
            -angle + e.AdditionalRotation, new Vector3(
            tPos.X + -e.offX,
            tPos.Y + e.offY,
            tPos.Z + e.offZ) + new Vector3(e.LineAddHitboxLengthX ? hitboxRadius : 0f, e.LineAddHitboxLengthY ? hitboxRadius : 0f, e.LineAddHitboxLengthZ ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthX ? BasePlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthY ? BasePlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZ ? BasePlayer.HitboxRadius : 0f));
        return (pointA, pointB);
    }

    internal static (Vector3 pointA, Vector3 pointB) GetNonRotatedPointsForZeroRadius(Vector3 tPos, Element e, float hitboxRadius, float angle)
    {
        var pointA = new Vector3(
                tPos.X + e.refX,
                tPos.Y + e.refY,
                tPos.Z + e.refZ) + new Vector3(e.LineAddHitboxLengthXA ? hitboxRadius : 0f, e.LineAddHitboxLengthYA ? hitboxRadius : 0f, e.LineAddHitboxLengthZA ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthXA ? BasePlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthYA ? BasePlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZA ? BasePlayer.HitboxRadius : 0f);
        var pointB = new Vector3(
            tPos.X + e.offX,
            tPos.Y + e.offY,
            tPos.Z + e.offZ) + new Vector3(e.LineAddHitboxLengthX ? hitboxRadius : 0f, e.LineAddHitboxLengthY ? hitboxRadius : 0f, e.LineAddHitboxLengthZ ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthX ? BasePlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthY ? BasePlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZ ? BasePlayer.HitboxRadius : 0f);
        return (pointA, pointB);
    }

    internal static bool IsElementObjectMatches(Layout layout, Element element, bool isTargetable, IGameObject gameObject)
    {
        return 
            (!element.onlyTargetable || isTargetable)
            && (!element.onlyUnTargetable || !isTargetable)
            && (!element.LimitRotation || (gameObject.Rotation >= element.RotationMax && gameObject.Rotation <= element.RotationMin))
            && (!element.UseHitboxRadius || (gameObject.HitboxRadius >= element.HitboxRadiusMin && gameObject.HitboxRadius <= element.HitboxRadiusMax))
            && (!element.refTargetYou || LayoutUtils.CheckTargetingOption(element, gameObject))
            && (!element.refActorObjectLife || gameObject.GetLifeTimeSeconds().InRange(element.refActorLifetimeMin, element.refActorLifetimeMax))
            && (!element.LimitDistance || IsDistanceMatches(layout, element, gameObject))
            && (element.ObjectKinds.Count == 0 || element.ObjectKinds.Contains(gameObject.ObjectKind))
            && LayoutUtils.CheckCharacterAttributes(element, gameObject);
    }

    internal static bool IsDistanceMatches(Layout layout, Element element, IGameObject go)
    {
        if(element.UseDistanceSourcePlaceholder)
        {
            foreach(var p in element.DistanceSourcePlaceholder) 
            {
                var pos = Utils.GetFacePositions(layout, element, go, p);
                foreach(var x in pos)
                {
                    if(Vector3.Distance(go.Position, x).InRange(element.DistanceMin, element.DistanceMax).Invert(element.LimitDistanceInvert)) return true;
                }
            }
            return false;
        }
        else
        {
            return Vector3.Distance(go.GetPositionXZY(), new(element.DistanceSourceX, element.DistanceSourceY, element.DistanceSourceZ)).InRange(element.DistanceMin, element.DistanceMax).Invert(element.LimitDistanceInvert);
        }
    }
}
