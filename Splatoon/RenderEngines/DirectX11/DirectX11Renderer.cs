using ECommons.GameFunctions;
using ECommons.MathHelpers;
using ECommons.ObjectLifeTracker;
using Splatoon.Memory;
using Splatoon.Serializables;
using static Splatoon.RenderEngines.DirectX11.DirectX11DisplayObjects;

namespace Splatoon.RenderEngines.DirectX11;
public sealed unsafe class DirectX11Renderer : RenderEngine
{
    private DirectX11Scene DirectX11Scene;

    internal override RenderEngineKind RenderEngineKind { get; } = RenderEngineKind.DirectX11;

    internal DirectX11Renderer()
    {
        if(!Enabled)
        {
            this.LoadError = new RenderEngineDisabledException();
            return;
        }
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

    internal void DrawCircle(Element e, float x, float y, float z, float r, float angle, IGameObject go = null)
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
        if (!LayoutUtils.ShouldDraw(cx, Utils.GetPlayerPositionXZY().X, cy, Utils.GetPlayerPositionXZY().Y)) return;
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
        DrawText(e, go, cx, cy, z);
    }

    void DrawText(Element e, IGameObject go, float cx, float cy, float z) 
    {
        if(e.overlayText.Length > 0)
        {
            var text = e.overlayText;
            if(go != null)
            {
                text = text.ProcessPlaceholders(go);
            }
            DisplayObjects.Add(new DisplayObjectText(cx, cy, z + e.offZ + e.overlayVOffset, text, e.overlayBGColor, e.overlayTextColor, e.overlayFScale));
        }
    }

    internal void DrawCone(Element e, Vector3 origin, float? radius = null, float baseAngle = 0f)
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
            if(!LayoutUtils.ShouldDraw(center.X, Utils.GetPlayerPositionXZY().X, center.Z, Utils.GetPlayerPositionXZY().Y)) return;
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
            DrawText(e, null, center.X, center.Z, center.Y);
        }
    }

    internal void AddRotatedLine(Vector3 tPos, float angle, Element e, float aradius, float hitboxRadius)
    {
        if (e.includeRotation)
        {
            if (aradius == 0f)
            {
                var (pointA, pointB) = CommonRenderUtils.GetRotatedPointsForZeroRadius(tPos, e, hitboxRadius, angle);
                if(!LayoutUtils.ShouldDraw(pointA.X, Utils.GetPlayerPositionXZY().X, pointA.Y, Utils.GetPlayerPositionXZY().Y)
                    && !LayoutUtils.ShouldDraw(pointB.X, Utils.GetPlayerPositionXZY().X, pointB.Y, Utils.GetPlayerPositionXZY().Y)) return;
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

                if(!LayoutUtils.ShouldDraw(start.X, Utils.GetPlayerPositionXZY().X, start.Y, Utils.GetPlayerPositionXZY().Y)
                    && !LayoutUtils.ShouldDraw(stop.X, Utils.GetPlayerPositionXZY().X, stop.Y, Utils.GetPlayerPositionXZY().Y)) return;

                var line = new DisplayObjectLine(Utils.XZY(start), Utils.XZY(stop), aradius, e.GetDisplayStyleWithOverride(), e.LineEndA, e.LineEndB);
                DisplayObjects.Add(line);
            }
        }
        else
        {
            var (pointA, pointB) = CommonRenderUtils.GetNonRotatedPointsForZeroRadius(tPos, e, hitboxRadius, angle);
            if(!LayoutUtils.ShouldDraw(pointA.X, Utils.GetPlayerPositionXZY().X, pointA.Y, Utils.GetPlayerPositionXZY().Y)
                && !LayoutUtils.ShouldDraw(pointB.X, Utils.GetPlayerPositionXZY().X, pointB.Y, Utils.GetPlayerPositionXZY().Y)) return;
            DisplayObjects.Add(new DisplayObjectLine(pointA.X, pointA.Y, pointA.Z,
                pointB.X, pointB.Y, pointB.Z,
                e.thicc, e.color, e.LineEndA, e.LineEndB));
        }
    }

    internal override void ProcessElement(Element e, Layout i = null, bool forceEnable = false)
    {
        if (!e.Enabled && !forceEnable) return;
        P.ElementAmount++;
        var radius = e.radius;
        if (e.type == 0)
        {
            if (i == null || !i.UseDistanceLimit || LayoutUtils.CheckDistanceCondition(i, e.refX, e.refY, e.refZ))
            {
                DrawCircle(e, e.refX, e.refY, e.refZ, radius, 0f);
            }
        }
        else if (e.type == 1 || e.type == 3 || e.type == 4)
        {
            if (e.includeOwnHitbox) radius += Svc.ClientState.LocalPlayer.HitboxRadius;
            if (e.refActorType == 1 && LayoutUtils.CheckCharacterAttributes(e, Svc.ClientState.LocalPlayer, true))
            {
                if (e.type == 1)
                {
                    var pointPos = Utils.GetPlayerPositionXZY();
                    DrawCircle(e, pointPos.X, pointPos.Y, pointPos.Z, radius, e.includeRotation ? Svc.ClientState.LocalPlayer.Rotation : 0f,
                        e.overlayPlaceholders ? Svc.ClientState.LocalPlayer : null);
                }
                else if (e.type == 3)
                {
                    AddRotatedLine(Utils.GetPlayerPositionXZY(), Svc.ClientState.LocalPlayer.Rotation, e, radius, 0f);
                }
                else if (e.type == 4)
                {
                    DrawCone(e, Utils.GetPlayerPositionXZY(), radius, Svc.ClientState.LocalPlayer.Rotation);
                }
            }
            else if (e.refActorType == 2 && Svc.Targets.Target != null
                && Svc.Targets.Target is IBattleNpc && LayoutUtils.CheckCharacterAttributes(e, Svc.Targets.Target, true))
            {
                if (i == null || !i.UseDistanceLimit || LayoutUtils.CheckDistanceCondition(i, Svc.Targets.Target.GetPositionXZY()))
                {
                    if (e.includeHitbox) radius += Svc.Targets.Target.HitboxRadius;
                    if (e.type == 1)
                    {
                        DrawCircle(e, Svc.Targets.Target.GetPositionXZY().X, Svc.Targets.Target.GetPositionXZY().Y,
                            Svc.Targets.Target.GetPositionXZY().Z, radius, e.includeRotation ? Svc.Targets.Target.Rotation : 0f,
                            e.overlayPlaceholders ? Svc.Targets.Target : null);
                    }
                    else if (e.type == 3)
                    {
                        var angle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                                            : Svc.Targets.Target.Rotation;
                        AddRotatedLine(Svc.Targets.Target.GetPositionXZY(), angle, e, radius, Svc.Targets.Target.HitboxRadius);
                    }
                    else if (e.type == 4)
                    {
                        var baseAngle = e.FaceMe ? (180 - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians() : Svc.Targets.Target.Rotation;
                        DrawCone(e, Svc.Targets.Target.GetPositionXZY(), radius, baseAngle);
                    }
                }
            }
            else if (e.refActorType == 0)
            {
                foreach (var a in Svc.Objects)
                {
                    var targetable = a.Struct()->GetIsTargetable();
                    if (LayoutUtils.IsAttributeMatches(e, a)
                            && CommonRenderUtils.IsElementObjectMatches(e, targetable, a))
                    {
                        if (i == null || !i.UseDistanceLimit || LayoutUtils.CheckDistanceCondition(i, a.GetPositionXZY()))
                        {
                            var aradius = radius;
                            if (e.includeHitbox) aradius += a.HitboxRadius;
                            if (e.type == 1)
                            {
                                DrawCircle(e, a.GetPositionXZY().X, a.GetPositionXZY().Y, a.GetPositionXZY().Z, aradius,
                                    e.includeRotation ? a.Rotation : 0f,
                                    e.overlayPlaceholders ? a : null);
                            }
                            else if (e.type == 3)
                            {
                                var angle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(a.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                                            : a.Rotation;
                                AddRotatedLine(a.GetPositionXZY(), angle, e, aradius, a.HitboxRadius);
                            }
                            else if (e.type == 4)
                            {
                                var baseAngle = e.FaceMe ?
                                    (180 - (MathHelper.GetRelativeAngle(a.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                                    : (a.Rotation);
                                DrawCone(e, a.GetPositionXZY(), aradius, baseAngle);
                            }
                        }
                    }
                }
            }

        }
        else if (e.type == 2)
        {
            if(i == null || !i.UseDistanceLimit || LayoutUtils.CheckDistanceToLineCondition(i, e))
            {
                if(!LayoutUtils.ShouldDraw(e.refX, Utils.GetPlayerPositionXZY().X, e.refY, Utils.GetPlayerPositionXZY().Y)
    && !LayoutUtils.ShouldDraw(e.offX, Utils.GetPlayerPositionXZY().X, e.offY, Utils.GetPlayerPositionXZY().Y)) return;
                AddLine(new Vector3(e.refX, e.refZ, e.refY), new Vector3(e.offX, e.offZ, e.offY), e.radius, e.GetDisplayStyleWithOverride(), e.LineEndA, e.LineEndB);
            }
        }
        else if (e.type == 5)
        {
            var baseAngle = e.FaceMe ?
                (180 - (MathHelper.GetRelativeAngle(new Vector2(e.refX + e.offX, e.refY + e.offY), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                : 0;
            var pos = new Vector3(e.refX + e.offX, e.refY + e.offY, e.refZ + e.offZ);
            DrawCone(e, pos, radius, baseAngle);
        }
    }

    internal override void AddLine(float ax, float ay, float az, float bx, float by, float bz, float thickness, uint color, LineEnd startStyle = LineEnd.None, LineEnd endStyle = LineEnd.None)
    {
        DisplayObjects.Add(new DisplayObjectLine(ax, ay, az, bx, by, bz, thickness, color, startStyle, endStyle));
    }

    internal void AddLine(Vector3 start, Vector3 stop, float radius, DisplayStyle style, LineEnd startStyle = LineEnd.None, LineEnd endStyle = LineEnd.None)
    {
        DisplayObjects.Add(new DisplayObjectLine(start, stop, radius, style, startStyle, endStyle));
    }
}
