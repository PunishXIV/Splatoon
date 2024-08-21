using ECommons.GameFunctions;
using ECommons.MathHelpers;
using ECommons.ObjectLifeTracker;
using FFXIVClientStructs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Splatoon.RenderEngines;
/// <summary>
/// This class contains render utils that are commonly used across all engines
/// </summary>
public unsafe static class CommonRenderUtils
{
    internal static string ProcessPlaceholders(this string s, IGameObject go)
    {
        return s
        .Replace("$NAMEID", $"{(go is ICharacter chr2 ? chr2.NameId : 0).Format()}")
        .Replace("$NAME", go.Name.ToString())
        .Replace("$OBJECTID", $"{go.EntityId.Format()}")
        .Replace("$DATAID", $"{go.DataId.Format()}")
        .Replace("$MODELID", $"{(go is ICharacter chr ? chr.Struct()->CharacterData.ModelCharaId : 0).Format()}")
        .Replace("$HITBOXR", $"{go.HitboxRadius:F1}")
        .Replace("$KIND", $"{go.ObjectKind}")
        .Replace("$NPCID", $"{go.Struct()->GetNameId().Format()}")
        .Replace("$LIFE", $"{go.GetLifeTimeSeconds():F1}")
        .Replace("$DISTANCE", $"{Vector3.Distance((Svc.ClientState.LocalPlayer?.Position ?? Vector3.Zero), go.Position):F1}")
        .Replace("$CAST", go is IBattleChara chr3 ? $"[{chr3.CastActionId.Format()}] {chr3.CurrentCastTime}/{chr3.TotalCastTime}" : "")
        .Replace("\\n", "\n")
        .Replace("$TRANSFORM", $"{(go is ICharacter chr5 ? chr5.GetTransformationID() : 0).Format()}")
        .Replace("$MSTATUS", $"{(*(int*)(go.Address + 0x104)).Format()}");
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
                    tPos.Z + e.refZ) + new Vector3(e.LineAddHitboxLengthXA ? hitboxRadius : 0f, e.LineAddHitboxLengthYA ? hitboxRadius : 0f, e.LineAddHitboxLengthZA ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthXA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthYA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f));
        var pointB = Utils.RotatePoint(tPos.X, tPos.Y,
            -angle + e.AdditionalRotation, new Vector3(
            tPos.X + -e.offX,
            tPos.Y + e.offY,
            tPos.Z + e.offZ) + new Vector3(e.LineAddHitboxLengthX ? hitboxRadius : 0f, e.LineAddHitboxLengthY ? hitboxRadius : 0f, e.LineAddHitboxLengthZ ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthX ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthY ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZ ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f));
        return (pointA, pointB);
    }

    internal static (Vector3 pointA, Vector3 pointB) GetNonRotatedPointsForZeroRadius(Vector3 tPos, Element e, float hitboxRadius, float angle)
    {
        var pointA = new Vector3(
                tPos.X + e.refX,
                tPos.Y + e.refY,
                tPos.Z + e.refZ) + new Vector3(e.LineAddHitboxLengthXA ? hitboxRadius : 0f, e.LineAddHitboxLengthYA ? hitboxRadius : 0f, e.LineAddHitboxLengthZA ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthXA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthYA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f);
        var pointB = new Vector3(
            tPos.X + e.offX,
            tPos.Y + e.offY,
            tPos.Z + e.offZ) + new Vector3(e.LineAddHitboxLengthX ? hitboxRadius : 0f, e.LineAddHitboxLengthY ? hitboxRadius : 0f, e.LineAddHitboxLengthZ ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthX ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthY ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZ ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f);
        return (pointA, pointB);
    }

    internal static bool IsElementObjectMatches(Element e, bool targetable, IGameObject a)
    {
        return (!e.onlyTargetable || targetable)
                            && (!e.onlyUnTargetable || !targetable)
                            && LayoutUtils.CheckCharacterAttributes(e, a)
                            && (!e.refTargetYou || LayoutUtils.CheckTargetingOption(e, a))
                            && (!e.refActorObjectLife || a.GetLifeTimeSeconds().InRange(e.refActorLifetimeMin, e.refActorLifetimeMax))
                            && (!e.LimitDistance || Vector3.Distance(a.GetPositionXZY(), new(e.DistanceSourceX, e.DistanceSourceY, e.DistanceSourceZ)).InRange(e.DistanceMin, e.DistanceMax).Invert(e.LimitDistanceInvert));
    }
}
