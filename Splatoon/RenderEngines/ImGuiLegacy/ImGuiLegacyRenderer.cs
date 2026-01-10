using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.MathHelpers;
using ECommons.ObjectLifeTracker;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Splatoon.Memory;
using Splatoon.Serializables;
using Splatoon.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkTimer.Delegates;
using static Splatoon.RenderEngines.ImGuiLegacy.ImGuiLegacyDisplayObjects;

namespace Splatoon.RenderEngines.ImGuiLegacy;
internal sealed unsafe class ImGuiLegacyRenderer : RenderEngine
{
    internal override RenderEngineKind RenderEngineKind { get; } = RenderEngineKind.ImGui_Legacy;
    internal ImGuiLegacyScene Scene;
    internal override bool CanBeDisabled { get; } = false;

    internal ImGuiLegacyRenderer()
    {
        try
        {
            Scene = new(this);
        }
        catch(Exception e)
        {
            LoadError = e;
            e.Log();
        }
    }

    public override void Dispose()
    {
        Scene.Dispose();
    }

    internal override void AddLine(float ax, float ay, float az, float bx, float by, float bz, float thickness, uint color, LineEnd startStyle = LineEnd.None, LineEnd endStyle = LineEnd.None)
    {
        DisplayObjects.Add(new DisplayObjectLine(ax, ay, az, bx, by, bz, thickness, color));
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
                        element.overlayPlaceholders ? BasePlayer : null);
                }
                else if(element.type == 3)
                {
                    AddRotatedLine(layout, Utils.GetPlayerPositionXZY(), BasePlayer.GetRotationWithOverride(element), element, radius, 0f);
                    //Svc.Chat.Print(Svc.ClientState.LocalPlayer.Rotation.ToString());
                }
                else if(element.type == 4)
                {
                    if(element.coneAngleMax > element.coneAngleMin)
                    {
                        for(var x = element.coneAngleMin; x < element.coneAngleMax; x += GetFillStepCone(element.FillStep))
                        {
                            AddConeLine(Utils.GetPlayerPositionXZY(), BasePlayer.GetRotationWithOverride(element), (BasePlayer.GetRotationWithOverride(element).RadiansToDegrees() - x.Float()).DegreesToRadians(), element, radius, x == element.coneAngleMin ? 1f : element.fillIntensity ?? Utils.DefaultFillIntensity, false);
                        }
                        AddConeLine(Utils.GetPlayerPositionXZY(), BasePlayer.GetRotationWithOverride(element), (BasePlayer.GetRotationWithOverride(element).RadiansToDegrees() - element.coneAngleMax.Float()).DegreesToRadians(), element, radius, 1f, true);
                    }
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
                            element.overlayPlaceholders ? Svc.Targets.Target : null);
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
                                    AddRotatedLine(layout, element.FaceInvert ? pos.ToXZY() : Svc.Targets.Target.GetPositionXZY(), angle, element, radius, Svc.Targets.Target.HitboxRadius);
                                }
                            }
                        }
                        else
                        {
                            var angle = Svc.Targets.Target.GetRotationWithOverride(element);
                            AddRotatedLine(layout, Svc.Targets.Target.GetPositionXZY(), angle, element, radius, Svc.Targets.Target.HitboxRadius);
                        }

                    }
                    else if(element.type == 4)
                    {
                        if(element.coneAngleMax > element.coneAngleMin)
                        {
                            for(var x = element.coneAngleMin; x < element.coneAngleMax; x += GetFillStepCone(element.FillStep))
                            {
                                if(element.FaceMe)
                                {
                                    var list = Utils.GetFacePositions(layout, element, Svc.Targets.Target, element.faceplayer);
                                    if(list != null)
                                    {
                                        foreach(var pos in list)
                                        {
                                            var angle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), pos.ToVector2()) - x.Float())).DegreesToRadians();
                                            var baseAngle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), pos.ToVector2()))).DegreesToRadians();
                                            AddConeLine(
                                                element.FaceInvert ? pos.ToXZY() : Svc.Targets.Target.GetPositionXZY(),
                                                baseAngle,
                                                angle,
                                                element,
                                                radius,
                                                x == element.coneAngleMin ? 1f : element.fillIntensity ?? Utils.DefaultFillIntensity,
                                                false
                                            );
                                        }
                                    }
                                }
                                else
                                {
                                    var angle = (Svc.Targets.Target.GetRotationWithOverride(element).RadiansToDegrees() - x.Float()).DegreesToRadians();
                                    var baseAngle = Svc.Targets.Target.GetRotationWithOverride(element);
                                    AddConeLine(
                                        Svc.Targets.Target.GetPositionXZY(),
                                        baseAngle,
                                        angle,
                                        element,
                                        radius,
                                        x == element.coneAngleMin ? 1f : element.fillIntensity ?? Utils.DefaultFillIntensity,
                                        false
                                    );
                                }

                            }
                            {
                                if(element.FaceMe)
                                {
                                    var list = Utils.GetFacePositions(layout, element, Svc.Targets.Target, element.faceplayer);
                                    if(list != null)
                                    {
                                        foreach(var pos in list)
                                        {
                                            var angle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), pos.ToVector2()) - element.coneAngleMax.Float())).DegreesToRadians();
                                            var baseAngle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), pos.ToVector2()))).DegreesToRadians();
                                            AddConeLine(element.FaceInvert ? pos.ToXZY() : Svc.Targets.Target.GetPositionXZY(), baseAngle, angle, element, radius, 1f, true);
                                        }
                                    }
                                }
                                else
                                {
                                    var angle = (Svc.Targets.Target.GetRotationWithOverride(element).RadiansToDegrees() - element.coneAngleMax.Float()).DegreesToRadians();
                                    var baseAngle = Svc.Targets.Target.GetRotationWithOverride(element);
                                    AddConeLine(Svc.Targets.Target.GetPositionXZY(), baseAngle, angle, element, radius, 1f, true);
                                }

                            }
                        }
                        //DisplayObjects.Add(new DisplayObjectCone(e, Svc.Targets.Target.Position, Svc.Targets.Target.Rotation, radius));
                    }
                }
            }
            else if(element.refActorType == 0)
            {
                foreach(var a in Svc.Objects)
                {
                    var targetable = a.Struct()->GetIsTargetable();
                    if(LayoutUtils.IsAttributeMatches(element, a)
                            && CommonRenderUtils.IsElementObjectMatches(layout, element, targetable, a))
                    {
                        if(layout == null || !layout.UseDistanceLimit || LayoutUtils.CheckDistanceCondition(layout, a.GetPositionXZY()))
                        {
                            ret = true;
                            foreach(var obj in Utils.AlterTargetIfNeeded(element, a))
                            {
                                var aradius = radius;
                                if(element.includeHitbox) aradius += obj.HitboxRadius;
                                if(element.type == 1)
                                {
                                    DrawCircle(layout, element, obj.GetPositionXZY().X, obj.GetPositionXZY().Y, obj.GetPositionXZY().Z, aradius,
                                        element.includeRotation ? obj.GetRotationWithOverride(element) : 0f,
                                        element.overlayPlaceholders ? obj : null);
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
                                                AddRotatedLine(layout, element.FaceInvert ? pos.ToXZY() : obj.GetPositionXZY(), angle, element, aradius, obj.HitboxRadius);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var angle = obj.GetRotationWithOverride(element);
                                        AddRotatedLine(layout, obj.GetPositionXZY(), angle, element, aradius, obj.HitboxRadius);
                                    }

                                }
                                else if(element.type == 4)
                                {
                                    if(element.coneAngleMax > element.coneAngleMin)
                                    {
                                        for(var x = element.coneAngleMin; x < element.coneAngleMax; x += GetFillStepCone(element.FillStep))
                                        {
                                            if(element.FaceMe)
                                            {
                                                var list = Utils.GetFacePositions(layout, element, obj, element.faceplayer);
                                                if(list != null)
                                                {
                                                    foreach(var pos in list)
                                                    {
                                                        var angle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(obj.Position.ToVector2(), pos.ToVector2()) - x.Float())).DegreesToRadians();
                                                        var baseAngle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(obj.Position.ToVector2(), pos.ToVector2()))).DegreesToRadians();
                                                        AddConeLine(
                                                            element.FaceInvert ? pos.ToXZY() : obj.GetPositionXZY(),
                                                            baseAngle,
                                                            angle,
                                                            element,
                                                            aradius,
                                                            x == element.coneAngleMin ? 1f : element.fillIntensity ?? Utils.DefaultFillIntensity,
                                                            false
                                                        );
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var angle = (obj.GetRotationWithOverride(element).RadiansToDegrees() - x.Float()).DegreesToRadians();
                                                var baseAngle = obj.GetRotationWithOverride(element);
                                                AddConeLine(
                                                    obj.GetPositionXZY(),
                                                    baseAngle,
                                                    angle,
                                                    element,
                                                    aradius,
                                                    x == element.coneAngleMin ? 1f : element.fillIntensity ?? Utils.DefaultFillIntensity,
                                                    false
                                                );
                                            }

                                        }
                                        {
                                            if(element.FaceMe)
                                            {
                                                var list = Utils.GetFacePositions(layout, element, obj, element.faceplayer);
                                                if(list != null)
                                                {
                                                    foreach(var pos in list)
                                                    {
                                                        var angle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(obj.Position.ToVector2(), pos.ToVector2()) - element.coneAngleMax.Float())).DegreesToRadians();
                                                        var baseAngle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(obj.Position.ToVector2(), pos.ToVector2()))).DegreesToRadians();
                                                        AddConeLine(element.FaceInvert ? pos.ToXZY() : obj.GetPositionXZY(), baseAngle, angle, element, aradius, 1f, true);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var angle = (obj.GetRotationWithOverride(element).RadiansToDegrees() - element.coneAngleMax.Float()).DegreesToRadians();
                                                var baseAngle = obj.GetRotationWithOverride(element);
                                                AddConeLine(obj.GetPositionXZY(), baseAngle, angle, element, aradius, 1f, true);
                                            }

                                        }
                                    }
                                    //DisplayObjects.Add(new DisplayObjectCone(e, a.Position, a.Rotation, aradius));
                                }
                            }
                        }
                    }
                }
            }

        }
        else if(element.type == 2)
        {
            if(element.radius > 0)
            {
                if(!LayoutUtils.ShouldDraw(element.refX, Utils.GetPlayerPositionXZY().X, element.refY, Utils.GetPlayerPositionXZY().Y)
                    && !LayoutUtils.ShouldDraw(element.offX, Utils.GetPlayerPositionXZY().X, element.offY, Utils.GetPlayerPositionXZY().Y)) return ret;
                ret = true;
                Utils.PerpOffset(new Vector2(element.refX, element.refY), new Vector2(element.offX, element.offY), 0f, element.radius, out _, out var p1);
                Utils.PerpOffset(new Vector2(element.refX, element.refY), new Vector2(element.offX, element.offY), 0f, -element.radius, out _, out var p2);
                Utils.PerpOffset(new Vector2(element.refX, element.refY), new Vector2(element.offX, element.offY), 1f, element.radius, out _, out var p3);
                Utils.PerpOffset(new Vector2(element.refX, element.refY), new Vector2(element.offX, element.offY), 1f, -element.radius, out _, out var p4);
                var rect = new DisplayObjectRect()
                {
                    l1 = new DisplayObjectLine(p1.X, p1.Y, element.refZ,
                    p2.X, p2.Y, element.refZ,
                    element.thicc, element.color),
                    l2 = new DisplayObjectLine(p3.X, p3.Y, element.offZ,
                    p4.X, p4.Y, element.offZ,
                    element.thicc, element.color)
                };
                if(P.Config.AltRectFill)
                {
                    AddAlternativeFillingRect(rect, GetFillStepRect(element.FillStep), element.fillIntensity ?? Utils.DefaultFillIntensity, element.Filled);
                }
            }
            else
            {
                if(
                    (
                        layout == null || !layout.UseDistanceLimit || LayoutUtils.CheckDistanceToLineCondition(layout, element)
                    ) &&
                    (
                LayoutUtils.ShouldDraw(element.offX, Utils.GetPlayerPositionXZY().X, element.offY, Utils.GetPlayerPositionXZY().Y)
                    || LayoutUtils.ShouldDraw(element.refX, Utils.GetPlayerPositionXZY().X, element.refY, Utils.GetPlayerPositionXZY().Y)
                    )
                    )
                {
                    ret = true;
                    DisplayObjects.Add(new DisplayObjectLine(element.refX, element.refY, element.refZ, element.offX, element.offY, element.offZ, element.thicc, element.color));
                }
            }
        }
        else if(element.type == 5)
        {
            if(element.coneAngleMax > element.coneAngleMin)
            {
                var pos = new Vector3(element.refX + element.offX, element.refY + element.offY, element.refZ + element.offZ);
                if(!LayoutUtils.ShouldDraw(pos.X, Utils.GetPlayerPositionXZY().X, pos.Y, Utils.GetPlayerPositionXZY().Y)) return ret;
                ret = true;
                if(P.Config.FillCone)
                {
                    if(element.FaceMe)
                    {
                        var list = Utils.GetFacePositions(layout, element, null, element.faceplayer);
                        if(list != null)
                        {
                            foreach(var fpos in list)
                            {
                                var baseAngle = ((element.FaceInvert ? 180 : 0) + MathHelper.GetRelativeAngle(new Vector2(element.refX + element.offX, element.refY + element.offY), fpos.ToVector2())).DegreesToRadians() + MathF.PI;
                                var startRad = baseAngle + element.coneAngleMin.Float().DegreesToRadians() + MathF.PI / 2;
                                var endRad = baseAngle + element.coneAngleMax.Float().DegreesToRadians() + MathF.PI / 2;
                                AddCone(layout, element.FaceInvert ? pos.ToXZY() : pos, startRad, endRad, element, element.radius);
                            }
                        }
                    }
                    else
                    {
                        var baseAngle = 0f;
                        var startRad = baseAngle + element.coneAngleMin.Float().DegreesToRadians() + MathF.PI / 2;
                        var endRad = baseAngle + element.coneAngleMax.Float().DegreesToRadians() + MathF.PI / 2;
                        AddCone(layout, pos, startRad, endRad, element, element.radius);
                    }

                }
                else
                {
                    for(var x = element.coneAngleMin; x < element.coneAngleMax; x += GetFillStepCone(element.FillStep))
                    {
                        if(element.FaceMe)
                        {
                            var list = Utils.GetFacePositions(layout, element, null, element.faceplayer);
                            if(list != null)
                            {
                                foreach(var fpos in list)
                                {
                                    var angle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(
                                        new Vector2(element.refX + element.offX, element.refY + element.offY),
                                        fpos.ToVector2()
                                    ) - x.Float())).DegreesToRadians();

                                    var baseAngle = ((element.FaceInvert ? 0 : 180) - (MathHelper.GetRelativeAngle(
                                        new Vector2(element.refX + element.offX, element.refY + element.offY),
                                        fpos.ToVector2()
                                    ))).DegreesToRadians();

                                    AddConeLine(
                                        element.FaceInvert ? fpos.ToXZY() : pos,
                                        baseAngle,
                                        angle,
                                        element,
                                        element.radius,
                                        x == element.coneAngleMin ? 1f : element.fillIntensity ?? Utils.DefaultFillIntensity,
                                        false
                                    );
                                }
                            }
                        }
                        else
                        {
                            var angle = (-x.Float()).DegreesToRadians();
                            var baseAngle = 0f;
                            AddConeLine(
                                pos,
                                baseAngle,
                                angle,
                                element,
                                element.radius,
                                x == element.coneAngleMin ? 1f : element.fillIntensity ?? Utils.DefaultFillIntensity,
                                false
                            );
                        }

                    }
                    {
                        if(element.FaceMe)
                        {
                            var list = Utils.GetFacePositions(layout, element, null, element.faceplayer);
                            if(list != null)
                            {
                                foreach(var fpos in list)
                                {
                                    var angle = (180 - (MathHelper.GetRelativeAngle(
                                        new Vector2(element.refX + element.offX, element.refY + element.offY),
                                        fpos.ToVector2()
                                    ) - element.coneAngleMax.Float())).DegreesToRadians();

                                    var baseAngle = (180 - (MathHelper.GetRelativeAngle(
                                        new Vector2(element.refX + element.offX, element.refY + element.offY),
                                        fpos.ToVector2()
                                    ))).DegreesToRadians();

                                    AddConeLine(pos, baseAngle, angle, element, element.radius, 1f, true);
                                }
                            }
                        }
                        else
                        {
                            var angle = (-element.coneAngleMax.Float()).DegreesToRadians();
                            var baseAngle = 0f;
                            AddConeLine(pos, baseAngle, angle, element, element.radius, 1f, true);
                        }

                    }
                }
            }
        }
        return ret;
    }

    private void DrawCircle(Layout layout, Element e, float x, float y, float z, float r, float angle, IGameObject go = null)
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
            DisplayObjects.Add(new DisplayObjectLine(cx,
                cy,
                z,
                Utils.GetPlayerPositionXZY().X, Utils.GetPlayerPositionXZY().Y, Utils.GetPlayerPositionXZY().Z,
                e.thicc, e.color));
        }
        if(layout != null && e.IsCapturing)
        {
            AddCapturedObject(layout, e, new Vector3(cx, z + e.offZ, cy));
        }
        if(e.Nodraw) return;
        if(!LayoutUtils.ShouldDraw(cx, Utils.GetPlayerPositionXZY().X, cy, Utils.GetPlayerPositionXZY().Y)) return;
        if(e.thicc > 0)
        {
            if(r > 0)
            {
                if(P.Config.UseFullDonutFill && e != null && e.Donut > 0 && !e.LegacyFill)
                {
                    DisplayObjects.Add(new DisplayObjectCircle(cx, cy, z + e.offZ, r, e.thicc, e.color, false, 1f));
                    DisplayObjects.Add(new DisplayObjectCircle(cx, cy, z + e.offZ, r + e.Donut, e.thicc, e.color, false, 1f));
                    DisplayObjects.Add(new DisplayObjectDonut(cx, cy, z + e.offZ, r, e.Donut, e.color, e.fillIntensity ?? Utils.DefaultFillIntensity));
                }
                else
                {
                    var filled = e.Filled && e.Donut == 0;
                    DisplayObjects.Add(new DisplayObjectCircle(cx, cy, z + e.offZ, r, e.thicc, filled ? Utils.TransformAlpha(e.color, e.fillIntensity ?? Utils.DefaultFillIntensity) : e.color, filled, e.fillIntensity ?? Utils.DefaultFillIntensity));
                    if(filled)
                    {
                        DisplayObjects.Add(new DisplayObjectCircle(cx, cy, z + e.offZ, r, e.thicc, e.color, false, 1f));
                    }
                    if(e != null && e.Donut > 0)
                    {
                        var donutR = GetFillStepDonut(e.FillStep);
                        while(donutR < e.Donut)
                        {
                            DisplayObjects.Add(new DisplayObjectCircle(cx, cy, z + e.offZ, r + donutR, e.thicc, Utils.TransformAlpha(e.color, e.fillIntensity), filled, 1f));
                            donutR += GetFillStepDonut(e.FillStep);
                        }
                        DisplayObjects.Add(new DisplayObjectCircle(cx, cy, z + e.offZ, r + e.Donut, e.thicc, e.color, filled, 1f));
                    }
                }
            }
            else
            {
                DisplayObjects.Add(new DisplayObjectDot(cx, cy, z + e.offZ, e.thicc, e.color));
            }
        }
        DrawText(e, go, cx, cy, z);
    }

    private void DrawText(Element element, IGameObject associatedGameObject, float cx, float cy, float z)
    {
        if(element.Nodraw) return;
        if(element.overlayTextIntl.Get(element.overlayText).Length > 0)
        {
            var text = element.overlayTextIntl.Get(element.overlayText);
            if(associatedGameObject != null)
            {
                text = text.ProcessPlaceholders(associatedGameObject);
            }
            DisplayObjects.Add(new DisplayObjectText(cx, cy, z + element.offZ + element.overlayVOffset, text, element.overlayBGColor, element.overlayTextColor, element.overlayFScale));
        }
    }

    private void AddRotatedLine(Layout layout, Vector3 tPos, float angle, Element e, float aradius, float hitboxRadius)
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
                DisplayObjects.Add(new DisplayObjectLine(pointA.X, pointA.Y, pointA.Z,
                    pointB.X, pointB.Y, pointB.Z,
                    e.thicc, e.color));
            }
            else
            {
                var pointA = Utils.RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.refX - aradius,
                    tPos.Y + e.refY,
                    tPos.Z + e.refZ));
                var pointB = Utils.RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.offX - aradius,
                    tPos.Y + e.offY,
                    tPos.Z + e.offZ));
                var pointA2 = Utils.RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.refX + aradius,
                    tPos.Y + e.refY,
                    tPos.Z + e.refZ));
                var pointB2 = Utils.RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.offX + aradius,
                    tPos.Y + e.offY,
                    tPos.Z + e.offZ));

                if(layout != null && e.IsCapturing)
                {
                    var start = Utils.RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.refX,
                    tPos.Y + e.refY,
                    tPos.Z + e.refZ));
                    AddCapturedObject(layout, e, new Vector3(start.X, start.Z, start.X));
                }
                if(e.Nodraw) return;

                if(!LayoutUtils.ShouldDraw(pointA.X, Utils.GetPlayerPositionXZY().X, pointA.Y, Utils.GetPlayerPositionXZY().Y)
                    && !LayoutUtils.ShouldDraw(pointB.X, Utils.GetPlayerPositionXZY().X, pointB.Y, Utils.GetPlayerPositionXZY().Y)
                    && !LayoutUtils.ShouldDraw(pointA2.X, Utils.GetPlayerPositionXZY().X, pointA2.Y, Utils.GetPlayerPositionXZY().Y)
                    && !LayoutUtils.ShouldDraw(pointB2.X, Utils.GetPlayerPositionXZY().X, pointB2.Y, Utils.GetPlayerPositionXZY().Y)) return;

                var rect = new DisplayObjectRect()
                {
                    l1 = new DisplayObjectLine(pointA.X, pointA.Y, pointA.Z,
                    pointB.X, pointB.Y, pointB.Z,
                    e.thicc, e.color),
                    l2 = new DisplayObjectLine(pointA2.X, pointA2.Y, pointA2.Z,
                    pointB2.X, pointB2.Y, pointB2.Z,
                    e.thicc, e.color)
                };
                if(P.Config.AltRectFill)
                {
                    AddAlternativeFillingRect(rect, GetFillStepRect(e.FillStep), e.fillIntensity ?? Utils.DefaultFillIntensity, e.Filled);
                }
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
                DisplayObjects.Add(new DisplayObjectLine(pointA.X, pointA.Y, pointA.Z,
                    pointB.X, pointB.Y, pointB.Z,
                    e.thicc, e.color));
            }
            else
            {
                var pointA = new Vector3(
                    tPos.X + e.refX - aradius,
                    tPos.Y + e.refY,
                    tPos.Z + e.refZ);
                var pointB = new Vector3(
                    tPos.X + e.offX - aradius,
                    tPos.Y + e.offY,
                    tPos.Z + e.offZ);
                var pointA2 = new Vector3(
                    tPos.X + e.refX + aradius,
                    tPos.Y + e.refY,
                    tPos.Z + e.refZ);
                var pointB2 = new Vector3(
                    tPos.X + e.offX + aradius,
                    tPos.Y + e.offY,
                    tPos.Z + e.offZ);

                if(layout != null && e.IsCapturing)
                {
                    var start = Utils.RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.refX,
                    tPos.Y + e.refY,
                    tPos.Z + e.refZ));
                    AddCapturedObject(layout, e, new Vector3(start.X, start.Z, start.X));
                }
                if(e.Nodraw) return;

                if(!LayoutUtils.ShouldDraw(pointA.X, Utils.GetPlayerPositionXZY().X, pointA.Y, Utils.GetPlayerPositionXZY().Y)
                    && !LayoutUtils.ShouldDraw(pointB.X, Utils.GetPlayerPositionXZY().X, pointB.Y, Utils.GetPlayerPositionXZY().Y)
                    && !LayoutUtils.ShouldDraw(pointA2.X, Utils.GetPlayerPositionXZY().X, pointA2.Y, Utils.GetPlayerPositionXZY().Y)
                    && !LayoutUtils.ShouldDraw(pointB2.X, Utils.GetPlayerPositionXZY().X, pointB2.Y, Utils.GetPlayerPositionXZY().Y)) return;

                var rect = new DisplayObjectRect()
                {
                    l1 = new DisplayObjectLine(pointA.X, pointA.Y, pointA.Z,
                    pointB.X, pointB.Y, pointB.Z,
                    e.thicc, e.color),
                    l2 = new DisplayObjectLine(pointA2.X, pointA2.Y, pointA2.Z,
                    pointB2.X, pointB2.Y, pointB2.Z,
                    e.thicc, e.color)
                };
                if(P.Config.AltRectFill)
                {
                    AddAlternativeFillingRect(rect, GetFillStepRect(e.FillStep), e.fillIntensity ?? Utils.DefaultFillIntensity, e.Filled);
                }
            }
        }
    }

    private void AddCone(Layout layout, Vector3 center, float startRad, float endRad, Element e, float radius)
    {
        //PluginLog.Debug($"[addcone] {center}, {startRad} -> {endRad}"); 
        if(layout != null && e.IsCapturing)
        {
            AddCapturedObject(layout, e, new Vector3(center.X, center.Z, center.Y));
        }
        if(e.Nodraw) return;
        DisplayObjects.Add(new DisplayObjectCone(
            center.X, center.Y, center.Z, radius, startRad, endRad,
            e.thicc, e.color, true
            ));
    }

    private void AddConeLine(Vector3 tPos, float baseAngle, float angle, Element e, float radius, float fillIntensity, bool addText)
    {
        tPos = Utils.RotatePoint(tPos.X, tPos.Y, -baseAngle, tPos + new Vector3(-e.offX, e.offY, e.offZ));
        var pointA = Utils.RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X,
                    tPos.Y + e.Donut,
                    tPos.Z));
        var pointB = Utils.RotatePoint(tPos.X, tPos.Y,
            -angle + e.AdditionalRotation, new Vector3(
            tPos.X,
            tPos.Y + radius + e.Donut,
            tPos.Z));
        DisplayObjects.Add(new DisplayObjectLine(pointA.X, pointA.Y, pointA.Z,
            pointB.X, pointB.Y, pointB.Z,
            e.thicc, Utils.TransformAlpha(e.color, fillIntensity)));
        if(addText)
        {
            DrawText(e, null, pointA.X, pointA.Y, pointA.Z);
        }
    }

    private void AddAlternativeFillingRect(DisplayObjectRect rect, float step, float fillIntensity, bool filled)
    {
        var thc = P.Config.AltRectForceMinLineThickness || rect.l1.thickness < P.Config.AltRectMinLineThickness ? P.Config.AltRectMinLineThickness : rect.l1.thickness;
        var col = P.Config.AltRectHighlightOutline ? (rect.l1.color.ToVector4() with { W = 1f }).ToUint() : rect.l1.color;
        var fl1 = new DisplayObjectLine(rect.l1.ax, rect.l1.ay, rect.l1.az, rect.l2.ax, rect.l2.ay, rect.l2.az, thc, col);
        var fl2 = new DisplayObjectLine(rect.l1.bx, rect.l1.by, rect.l1.bz, rect.l2.bx, rect.l2.by, rect.l2.bz, thc, col);
        var fl3 = new DisplayObjectLine(rect.l1.ax, rect.l1.ay, rect.l1.az, rect.l1.bx, rect.l1.by, rect.l1.bz, thc, col);
        var fl4 = new DisplayObjectLine(rect.l2.ax, rect.l2.ay, rect.l2.az, rect.l2.bx, rect.l2.by, rect.l2.bz, thc, col);
        DisplayObjects.Add(fl1);
        DisplayObjects.Add(fl2);
        DisplayObjects.Add(fl3);
        DisplayObjects.Add(fl4);
        if(filled)
        {
            {
                var v1 = new Vector3(rect.l1.ax, rect.l1.ay, rect.l1.az);
                var v2 = new Vector3(rect.l2.ax, rect.l2.ay, rect.l2.az);
                var v3 = new Vector3(rect.l1.bx, rect.l1.by, rect.l1.bz);
                var v4 = new Vector3(rect.l2.bx, rect.l2.by, rect.l2.bz);
                var dst = Vector3.Distance(v2, v1);
                var stp = dst / step;
                var d1 = (v2 - v1) / stp;
                var d2 = (v4 - v3) / stp;
                for(var i = step; i < dst; i += step)
                {
                    v1 += d1;
                    v3 += d2;
                    DisplayObjects.Add(new DisplayObjectLine(v1.X, v1.Y, v1.Z, v3.X, v3.Y, v3.Z, thc, Utils.TransformAlpha(rect.l1.color, fillIntensity)));
                }
            }
            {
                var v1 = new Vector3(rect.l1.ax, rect.l1.ay, rect.l1.az);
                var v3 = new Vector3(rect.l2.ax, rect.l2.ay, rect.l2.az);
                var v2 = new Vector3(rect.l1.bx, rect.l1.by, rect.l1.bz);
                var v4 = new Vector3(rect.l2.bx, rect.l2.by, rect.l2.bz);
                var dst = Vector3.Distance(v2, v1);
                var stp = dst / step;
                var d1 = (v2 - v1) / stp;
                var d2 = (v4 - v3) / stp;
                for(var i = step; i < dst; i += step)
                {
                    v1 += d1;
                    v3 += d2;
                    DisplayObjects.Add(new DisplayObjectLine(v1.X, v1.Y, v1.Z, v3.X, v3.Y, v3.Z, thc, Utils.TransformAlpha(rect.l1.color, fillIntensity)));
                }
            }
        }
    }

    internal float GetFillStepRect(float original)
    {
        if(P.Config.AltRectStepOverride || original < P.Config.AltRectStep)
        {
            return P.Config.AltRectStep;
        }
        return original;
    }

    internal float GetFillStepDonut(float original)
    {
        if(P.Config.AltDonutStepOverride || original < P.Config.AltDonutStep)
        {
            return P.Config.AltDonutStep;
        }
        return original;
    }

    internal int GetFillStepCone(float original)
    {
        if(P.Config.AltConeStepOverride || original < P.Config.AltConeStep)
        {
            return P.Config.AltConeStep;
        }
        return (int)Math.Max(1f, original);
    }
}
