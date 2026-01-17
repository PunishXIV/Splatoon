using Dalamud.Utility;
using ECommons.GameFunctions;
using ECommons.MathHelpers;
using ECommons.ObjectLifeTracker;
using Splatoon.Memory;
using Splatoon.Serializables;
using Splatoon.SplatoonScripting;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
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
            LoadError = new RenderEngineDisabledException();
            return;
        }
        try
        {
            if(Utils.IsLinux())
            {
                if(!P.Config.DX11EnabledOnMacLinux && !P.ForceLoadDX11) throw new InvalidOperationException("DirectX11 renderer was disabled because Mac/Linux was detected and user has not allowed it to be loaded.");
            }
            //throw new NotImplementedException("DirectX renderer is not updated");
            DirectX11Scene = new(this);
        }
        catch(Exception e)
        {
            LoadError = e;
            e.Log();
        }
    }

    public override void Dispose()
    {
        Safe(() => DirectX11Scene?.Dispose());
    }

    internal void DrawCircle(Layout layout, Element e, float x, float y, float z, float r, float angle, IGameObject go = null)
    {
        var cx = x + e.offX;
        var cy = y + e.offY;
        if(e.includeRotation)
        {
            var rotatedPoint = Utils.RotatePoint(x, y, -angle + e.AdditionalRotation, new Vector3(x - e.offX, y + e.offY, z));
            cx = rotatedPoint.X;
            cy = rotatedPoint.Y;
        }
        if(e.tether && !e.Nodraw)
        {
            Vector3 origin = new(cx, z, cy);
            var end = Utils.XZY(Utils.GetPlayerPositionXZY());
            if(e.ExtraTetherLength > 0)
            {
                end += Vector3.Normalize(end - origin) * e.ExtraTetherLength;
            }
            DisplayObjects.Add(new DisplayObjectLine(e.GetUniqueId(go), origin, end, 0, e.GetDisplayStyleWithOverride(), e.LineEndA, e.LineEndB));
        }
        if(layout != null && e.IsCapturing)
        {
            AddCapturedObject(layout, e, new Vector3(cx, z + e.offZ, cy));
        }
        if(e.Nodraw) return;
        if(!LayoutUtils.ShouldDraw(cx, Utils.GetPlayerPositionXZY().X, cy, Utils.GetPlayerPositionXZY().Y)) return;
        if(r > 0)
        {
            if(e.Donut > 0)
            {
                DisplayObjects.Add(new DisplayObjectDonut(e.GetUniqueId(go), new(cx, z + e.offZ, cy), r, e.Donut, e.GetDisplayStyleWithOverride(go)));
            }
            else
            {
                DisplayObjects.Add(new DisplayObjectCircle(e.GetUniqueId(go), new(cx, z + e.offZ, cy), r, e.GetDisplayStyleWithOverride(go)));
            }
        }
        else
        {
            DisplayObjects.Add(new DisplayObjectDot(e.GetUniqueId(go), cx, z + e.offZ, cy, e.thicc, e.color));
        }
        DrawText(e, go, cx, cy, z);
    }

    private void DrawText(Element element, IGameObject associatedGameObject, float cx, float cy, float z)
    {
        if(element.overlayTextIntl.Get(element.overlayText).Length > 0)
        {
            var text = element.overlayTextIntl.Get(element.overlayText);
            if(associatedGameObject != null)
            {
                text = text.ProcessPlaceholders(associatedGameObject);
            }
            DisplayObjects.Add(new DisplayObjectText(element.GetUniqueId(associatedGameObject), cx, cy, z + element.offZ + element.overlayVOffset, text, element.overlayBGColor, element.overlayTextColor, element.overlayFScale));
        }
    }

    internal void DrawCone(Layout layout, Element e, Vector3 origin, float? radius = null, float baseAngle = 0f, IGameObject go = null)
    {
        if(e.coneAngleMax > e.coneAngleMin)
        {
            var angleMin = -baseAngle + e.AdditionalRotation + e.coneAngleMin.DegreesToRadians();
            var angleMax = -baseAngle + e.AdditionalRotation + e.coneAngleMax.DegreesToRadians();
            var totalAngle = angleMax - angleMin;
            if(totalAngle >= 2 * MathF.PI)
            {
                angleMin = 0;
                angleMax = 2 * MathF.PI;
            }

            var center = Utils.XZY(Utils.RotatePoint(origin.X, origin.Y, -baseAngle, origin + new Vector3(-e.offX, e.offY, e.offZ)));
            if(layout != null && e.IsCapturing)
            {
                AddCapturedObject(layout, e, new Vector3(center.X, center.Z, center.Y));
            }
            if(e.Nodraw) return;
            if(!LayoutUtils.ShouldDraw(center.X, Utils.GetPlayerPositionXZY().X, center.Z, Utils.GetPlayerPositionXZY().Y)) return;
            float innerRadius = 0;
            var outerRadius = radius ?? e.radius;
            if(e.Donut > 0)
            {
                innerRadius = outerRadius;
                outerRadius = innerRadius + e.Donut;
            }
            if(e.tether && !e.Nodraw)
            {
                var end = Utils.XZY(Utils.GetPlayerPositionXZY());
                if(e.ExtraTetherLength > 0)
                {
                    end += Vector3.Normalize(end - center) * e.ExtraTetherLength;
                }
                DisplayObjects.Add(new DisplayObjectLine(e.GetUniqueId(go), center, end, 0, e.GetDisplayStyleWithOverride(), e.LineEndA, e.LineEndB));
            }

            DisplayObjects.Add(new DisplayObjectFan(e.GetUniqueId(go), center, innerRadius, outerRadius, angleMin, angleMax, e.GetDisplayStyleWithOverride()));
            DrawText(e, null, center.X, center.Z, center.Y);
        }
    }

    internal void AddRotatedLine(Layout layout, Vector3 tPos, float angle, Element e, float aradius, float hitboxRadius, IGameObject go = null)
    {
        if(e.includeRotation)
        {
            if(aradius == 0f)
            {
                var (pointA, pointB) = CommonRenderUtils.GetRotatedPointsForZeroRadius(tPos, e, hitboxRadius, angle);
                if(layout != null && e.IsCapturing)
                {
                    AddCapturedObject(layout, e, new Vector3(pointA.X, pointA.Z, pointA.X));
                }
                if(e.Nodraw) return;
                if(!LayoutUtils.ShouldDraw(pointA.X, Utils.GetPlayerPositionXZY().X, pointA.Y, Utils.GetPlayerPositionXZY().Y)
                    && !LayoutUtils.ShouldDraw(pointB.X, Utils.GetPlayerPositionXZY().X, pointB.Y, Utils.GetPlayerPositionXZY().Y)) return;
                DisplayObjects.Add(new DisplayObjectLine(e.GetUniqueId(go), pointA.X, pointA.Y, pointA.Z,
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

                if(layout != null && e.IsCapturing)
                {
                    AddCapturedObject(layout, e, new Vector3(start.X, start.Z, start.X));
                }
                if(e.Nodraw) return;

                if(!LayoutUtils.ShouldDraw(start.X, Utils.GetPlayerPositionXZY().X, start.Y, Utils.GetPlayerPositionXZY().Y)
                    && !LayoutUtils.ShouldDraw(stop.X, Utils.GetPlayerPositionXZY().X, stop.Y, Utils.GetPlayerPositionXZY().Y)) return;

                var line = new DisplayObjectLine(e.GetUniqueId(go), Utils.XZY(start), Utils.XZY(stop), aradius, e.GetDisplayStyleWithOverride(go), e.LineEndA, e.LineEndB);

                DisplayObjects.Add(line);
            }
        }
        else
        {
            if(aradius == 0)
            {
                var (pointA, pointB) = CommonRenderUtils.GetNonRotatedPointsForZeroRadius(tPos, e, hitboxRadius, angle);
                if(layout != null && e.IsCapturing)
                {
                    AddCapturedObject(layout, e, new Vector3(pointA.X, pointA.Z, pointA.X));
                }
                if(e.Nodraw) return;
                if(!LayoutUtils.ShouldDraw(pointA.X, Utils.GetPlayerPositionXZY().X, pointA.Y, Utils.GetPlayerPositionXZY().Y)
                    && !LayoutUtils.ShouldDraw(pointB.X, Utils.GetPlayerPositionXZY().X, pointB.Y, Utils.GetPlayerPositionXZY().Y)) return;
                DisplayObjects.Add(new DisplayObjectLine(e.GetUniqueId(go), pointA.X, pointA.Y, pointA.Z,
                    pointB.X, pointB.Y, pointB.Z,
                    e.thicc, e.color, e.LineEndA, e.LineEndB));
            }
            else
            {
                var start = new Vector3(
                    tPos.X + e.refX,
                    tPos.Y + e.refY,
                    tPos.Z + e.refZ);
                var stop = new Vector3(
                    tPos.X + e.offX,
                    tPos.Y + e.offY,
                    tPos.Z + e.offZ);

                if(layout != null && e.IsCapturing)
                {
                    AddCapturedObject(layout, e, new Vector3(start.X, start.Z, start.X));
                }
                if(e.Nodraw) return;

                if(!LayoutUtils.ShouldDraw(start.X, Utils.GetPlayerPositionXZY().X, start.Y, Utils.GetPlayerPositionXZY().Y)
                    && !LayoutUtils.ShouldDraw(stop.X, Utils.GetPlayerPositionXZY().X, stop.Y, Utils.GetPlayerPositionXZY().Y)) return;

                var line = new DisplayObjectLine(e.GetUniqueId(go), Utils.XZY(start), Utils.XZY(stop), aradius, e.GetDisplayStyleWithOverride(go), e.LineEndA, e.LineEndB);

                DisplayObjects.Add(line);
            }
        }
    }

    internal override bool ProcessElement(Element element, Layout layout = null, bool forceEnable = false)
    {
        var ret = false;
        if(!element.Enabled && !forceEnable) return ret;
        P.ElementAmount++;
        var radius = element.radius;
        if(element.type == 0)
        {
            if(layout == null || !layout.UseDistanceLimit || LayoutUtils.CheckDistanceCondition(layout, element.refX, element.refY, element.refZ))
            {
                ret = true;
                DrawCircle(layout, element, element.refX, element.refY, element.refZ, radius, 0f);
            }
        }
        else if(element.type == 1 || element.type == 3 || element.type == 4)
        {
            if(element.includeOwnHitbox) radius += BasePlayer.HitboxRadius;
            if(element.refActorType == 1 && LayoutUtils.CheckCharacterAttributes(element, BasePlayer, true))
            {
                ret = true;
                if(element.type == 1)
                {
                    var pointPos = Utils.GetPlayerPositionXZY();
                    DrawCircle(layout, element, pointPos.X, pointPos.Y, pointPos.Z, radius, element.includeRotation ? BasePlayer.GetRotationWithOverride(element) : 0f,
                        BasePlayer);
                }
                else if(element.type == 3)
                {
                    AddRotatedLine(layout, Utils.GetPlayerPositionXZY(), BasePlayer.GetRotationWithOverride(element), element, radius, 0f, BasePlayer);
                }
                else if(element.type == 4)
                {
                    DrawCone(layout, element, Utils.GetPlayerPositionXZY(), radius, BasePlayer.GetRotationWithOverride(element), BasePlayer);
                }
            }
            else if(element.refActorType == 2 && Svc.Targets.Target != null
                && Svc.Targets.Target is IBattleNpc && LayoutUtils.CheckCharacterAttributes(element, Svc.Targets.Target, true))
            {
                if(layout == null || !layout.UseDistanceLimit || LayoutUtils.CheckDistanceCondition(layout, Svc.Targets.Target.GetPositionXZY()))
                {
                    ret = true;
                    if(element.includeHitbox) radius += Svc.Targets.Target.HitboxRadius;
                    if(element.type == 1)
                    {
                        DrawCircle(layout, element, Svc.Targets.Target.GetPositionXZY().X, Svc.Targets.Target.GetPositionXZY().Y,
                            Svc.Targets.Target.GetPositionXZY().Z, radius, element.includeRotation ? Svc.Targets.Target.GetRotationWithOverride(element) : 0f,
                            Svc.Targets.Target);
                    }
                    else if(element.type == 3)
                    {
                        if(element.FaceMe)
                        {
                            var list = Utils.GetFacePositions(layout, element, Svc.Targets.Target, element.faceplayer);
                            if(list != null)
                            {
                                foreach(var pos in list)
                                {
                                    var angle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), pos.ToVector2()))).DegreesToRadians();
                                    AddRotatedLine(layout, element.FaceInvert ? pos.ToXZY() : Svc.Targets.Target.GetPositionXZY(), angle, element, radius, Svc.Targets.Target.HitboxRadius, Svc.Targets.Target);
                                }
                            }
                        }
                        else
                        {
                            var angle = Svc.Targets.Target.GetRotationWithOverride(element);
                            AddRotatedLine(layout, Svc.Targets.Target.GetPositionXZY(), angle, element, radius, Svc.Targets.Target.HitboxRadius, Svc.Targets.Target);
                        }
                    }
                    else if(element.type == 4)
                    {
                        if(element.FaceMe)
                        {
                            var list = Utils.GetFacePositions(layout, element, Svc.Targets.Target, element.faceplayer);
                            if(list != null)
                            {
                                foreach(var pos in list)
                                {
                                    var baseAngle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), pos.ToVector2()))).DegreesToRadians();
                                    DrawCone(layout, element, element.FaceInvert ? pos.ToXZY() : Svc.Targets.Target.GetPositionXZY(), radius, baseAngle, Svc.Targets.Target);
                                }
                            }
                        }
                        else
                        {
                            var baseAngle = Svc.Targets.Target.GetRotationWithOverride(element);
                            DrawCone(layout, element, Svc.Targets.Target.GetPositionXZY(), radius, baseAngle, Svc.Targets.Target);
                        }
                    }
                }
            }
            else if(element.refActorType == 0)
            {
                List<IGameObject> objectList = [];
                foreach(var a in Svc.Objects)
                {
                    var targetable = a.Struct()->GetIsTargetable();
                    if(LayoutUtils.IsAttributeMatches(element, a)
                            && CommonRenderUtils.IsElementObjectMatches(layout, element, targetable, a))
                    {
                        if(layout == null || !layout.UseDistanceLimit || LayoutUtils.CheckDistanceCondition(layout, a.GetPositionXZY()))
                        {
                            ret = true;
                            objectList.Add(a);
                            
                        }
                    }
                }
                CommonRenderUtils.HandleEnumeration(element, ref objectList);
                foreach(var a in objectList)
                {
                    foreach(var obj in Utils.AlterTargetIfNeeded(element, a))
                    {
                        var aradius = radius;
                        if(element.includeHitbox) aradius += obj.HitboxRadius;
                        if(element.type == 1)
                        {
                            DrawCircle(layout, element, obj.GetPositionXZY().X, obj.GetPositionXZY().Y, obj.GetPositionXZY().Z, aradius,
                                element.includeRotation ? obj.GetRotationWithOverride(element) : 0f,
                                obj);
                        }
                        else if(element.type == 3)
                        {
                            if(element.FaceMe)
                            {
                                var list = Utils.GetFacePositions(layout, element, obj, element.faceplayer);
                                if(list != null)
                                {
                                    foreach(var pos in list)
                                    {
                                        var angle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(obj.Position.ToVector2(), pos.ToVector2()))).DegreesToRadians();
                                        AddRotatedLine(layout, element.FaceInvert ? pos.ToXZY() : obj.GetPositionXZY(), angle, element, aradius, obj.HitboxRadius, obj);
                                    }
                                }
                            }
                            else
                            {
                                var angle = obj.GetRotationWithOverride(element);
                                AddRotatedLine(layout, obj.GetPositionXZY(), angle, element, aradius, obj.HitboxRadius, obj);
                            }

                        }
                        else if(element.type == 4)
                        {
                            if(element.FaceMe)
                            {
                                var list = Utils.GetFacePositions(layout, element, obj, element.faceplayer);
                                if(list != null)
                                {
                                    foreach(var pos in list)
                                    {
                                        var baseAngle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(obj.Position.ToVector2(), pos.ToVector2()))).DegreesToRadians();
                                        DrawCone(layout, element, element.FaceInvert ? pos.ToXZY() : obj.GetPositionXZY(), aradius, baseAngle, obj);
                                    }
                                }
                            }
                            else
                            {
                                var baseAngle = obj.GetRotationWithOverride(element);
                                DrawCone(layout, element, obj.GetPositionXZY(), aradius, baseAngle, obj);
                            }
                        }
                    }
                }
            }

        }
        else if(element.type == 2)
        {
            if(layout == null || !layout.UseDistanceLimit || LayoutUtils.CheckDistanceToLineCondition(layout, element))
            {
                if(!LayoutUtils.ShouldDraw(element.refX, Utils.GetPlayerPositionXZY().X, element.refY, Utils.GetPlayerPositionXZY().Y)
    && !LayoutUtils.ShouldDraw(element.offX, Utils.GetPlayerPositionXZY().X, element.offY, Utils.GetPlayerPositionXZY().Y)) return ret;
                ret = true;
                AddLine(new Vector3(element.refX, element.refZ, element.refY), new Vector3(element.offX, element.offZ, element.offY), element.radius, element.GetDisplayStyleWithOverride(), element.LineEndA, element.LineEndB);
            }
        }
        else if(element.type == 5)
        {
            if(element.FaceMe)
            {
                var list = Utils.GetFacePositions(layout, element, null, element.faceplayer);
                if(list != null)
                {
                    foreach(var fpos in list)
                    {
                        var baseAngle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(new Vector2(element.refX + element.offX, element.refY + element.offY), fpos.ToVector2()))).DegreesToRadians();
                        var pos = new Vector3(element.refX + element.offX, element.refY + element.offY, element.refZ + element.offZ);
                        DrawCone(layout, element, element.FaceInvert ? fpos.ToXZY() : pos, radius, baseAngle);
                    }
                }
            }
            else
            {
                var baseAngle = 0f;
                var pos = new Vector3(element.refX + element.offX, element.refY + element.offY, element.refZ + element.offZ);
                DrawCone(layout, element, pos, radius, baseAngle);
            }

        }
        return ret;
    }

    internal override void AddLine(float ax, float ay, float az, float bx, float by, float bz, float thickness, uint color, LineEnd startStyle = LineEnd.None, LineEnd endStyle = LineEnd.None)
    {
        DisplayObjects.Add(new DisplayObjectLine("", ax, ay, az, bx, by, bz, thickness, color, startStyle, endStyle));
    }

    internal void AddLine(Vector3 start, Vector3 stop, float radius, DisplayStyle style, LineEnd startStyle = LineEnd.None, LineEnd endStyle = LineEnd.None)
    {
        DisplayObjects.Add(new DisplayObjectLine("", start, stop, radius, style, startStyle, endStyle));
    }
}
