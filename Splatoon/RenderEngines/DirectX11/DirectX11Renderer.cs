using ECommons.GameFunctions;
using ECommons.ObjectLifeTracker;
using Splatoon.Structures;

namespace Splatoon.RenderEngines.DirectX11;
public sealed unsafe class DirectX11Renderer : RenderEngine
{
    private DirectX11Scene DirectX11Scene;

    internal DirectX11Renderer()
    {
        try
        {
            DirectX11Scene = new(this);
        }
        catch (Exception e)
        {
            LoadError = e;
            e.Log();
        }
    }

    public override void Dispose()
    {
        Safe(() => DirectX11Scene?.Dispose());
    }

    internal override void DrawCircle(Element e, float x, float y, float z, float r, float angle, IGameObject go = null)
    {
        var cx = x + e.offX;
        var cy = y + e.offY;
        if (e.includeRotation)
        {
            var rotatedPoint = Utils.RotatePoint(x, y, -angle + e.AdditionalRotation, new Vector3(x - e.offX, y + e.offY, z));
            cx = rotatedPoint.X;
            cy = rotatedPoint.Y;
        }
        if (e.tether)
        {
            Vector3 origin = new(cx, z, cy);
            var end = Utils.XZY(Utils.GetPlayerPositionXZY());
            if (e.ExtraTetherLength > 0)
            {
                end += Vector3.Normalize(end - origin) * e.ExtraTetherLength;
            }
            DisplayObjects.Add(new DisplayObjectLine(origin, end, 0, e.GetDisplayStyleWithOverride(), e.LineEndA, e.LineEndB));
        }
        if (!P.ShouldDraw(cx, Utils.GetPlayerPositionXZY().X, cy, Utils.GetPlayerPositionXZY().Y)) return;
        if (e.thicc > 0)
        {
            if (r > 0)
            {
                if (e.Donut > 0)
                {
                    DisplayObjects.Add(new DisplayObjectDonut(new(cx, z + e.offZ, cy), r, e.Donut, e.GetDisplayStyleWithOverride()));
                }
                else
                {
                    var style = e.GetDisplayStyleWithOverride();
                    DisplayObjects.Add(new DisplayObjectCircle(new(cx, z + e.offZ, cy), r, style));
                }
            }
            else
            {
                DisplayObjects.Add(new DisplayObjectDot(cx, z + e.offZ, cy, e.thicc, e.color));
            }
        }
        if (e.overlayText.Length > 0)
        {
            var text = e.overlayText;
            if (go != null)
            {
                text = text
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
            DisplayObjects.Add(new DisplayObjectText(cx, cy, z + e.offZ + e.overlayVOffset, text, e.overlayBGColor, e.overlayTextColor, e.overlayFScale));
        }
    }

    internal override void DrawCone(Element e, Vector3 origin, float? radius = null, float baseAngle = 0f)
    {
        if (e.coneAngleMax > e.coneAngleMin)
        {
            var angleMin = -baseAngle + e.AdditionalRotation + e.coneAngleMin.DegreesToRadians();
            var angleMax = -baseAngle + e.AdditionalRotation + e.coneAngleMax.DegreesToRadians();
            var totalAngle = angleMax - angleMin;
            if (totalAngle >= 2 * MathF.PI)
            {
                angleMin = 0;
                angleMax = 2 * MathF.PI;
            }

            var center = Utils.XZY(Utils.RotatePoint(origin.X, origin.Y, -baseAngle, origin + new Vector3(-e.offX, e.offY, e.offZ)));
            float innerRadius = 0;
            var outerRadius = radius ?? e.radius;
            if (e.Donut > 0)
            {
                innerRadius = outerRadius;
                outerRadius = innerRadius + e.Donut;
            }
            if (e.tether)
            {
                var end = Utils.XZY(Utils.GetPlayerPositionXZY());
                if (e.ExtraTetherLength > 0)
                {
                    end += Vector3.Normalize(end - center) * e.ExtraTetherLength;
                }
                DisplayObjects.Add(new DisplayObjectLine(center, end, 0, e.GetDisplayStyleWithOverride(), e.LineEndA, e.LineEndB));
            }
            DisplayObjects.Add(new DisplayObjectFan(center, innerRadius, outerRadius, angleMin, angleMax, e.GetDisplayStyleWithOverride()));
        }
    }

    internal override void AddRotatedLine(Vector3 tPos, float angle, Element e, float aradius, float hitboxRadius)
    {
        if (e.includeRotation)
        {
            if (aradius == 0f)
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
                DisplayObjects.Add(new DisplayObjectLine(pointA.X, pointA.Y, pointA.Z,
                    pointB.X, pointB.Y, pointB.Z,
                    e.thicc, e.color, e.LineEndA, e.LineEndB));
            }
            else
            {
                var start = Utils.RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.refX,
                    tPos.Y + e.refY,
                    tPos.Z + e.refZ));
                var stop = Utils.RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.offX,
                    tPos.Y + e.offY,
                    tPos.Z + e.offZ));

                var line = new DisplayObjectLine(Utils.XZY(start), Utils.XZY(stop), aradius, e.GetDisplayStyleWithOverride(), e.LineEndA, e.LineEndB);
                DisplayObjects.Add(line);
            }
        }
        else
        {
            var pointA = new Vector3(
                tPos.X + e.refX,
                tPos.Y + e.refY,
                tPos.Z + e.refZ) + new Vector3(e.LineAddHitboxLengthXA ? hitboxRadius : 0f, e.LineAddHitboxLengthYA ? hitboxRadius : 0f, e.LineAddHitboxLengthZA ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthXA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthYA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f);
            var pointB = new Vector3(
                tPos.X + e.offX,
                tPos.Y + e.offY,
                tPos.Z + e.offZ) + new Vector3(e.LineAddHitboxLengthX ? hitboxRadius : 0f, e.LineAddHitboxLengthY ? hitboxRadius : 0f, e.LineAddHitboxLengthZ ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthX ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthY ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZ ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f);
            DisplayObjects.Add(new DisplayObjectLine(pointA.X, pointA.Y, pointA.Z,
                pointB.X, pointB.Y, pointB.Z,
                e.thicc, e.color, e.LineEndA, e.LineEndB));
        }
    }
}
