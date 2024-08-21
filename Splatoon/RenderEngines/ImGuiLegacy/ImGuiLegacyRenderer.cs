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
            this.LoadError = e;
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
                    //Svc.Chat.Print(Svc.ClientState.LocalPlayer.Rotation.ToString());
                }
                else if (e.type == 4)
                {
                    if (e.coneAngleMax > e.coneAngleMin)
                    {
                        for (var x = e.coneAngleMin; x < e.coneAngleMax; x += GetFillStepCone(e.FillStep))
                        {
                            AddConeLine(Utils.GetPlayerPositionXZY(), Svc.ClientState.LocalPlayer.Rotation, (Svc.ClientState.LocalPlayer.Rotation.RadiansToDegrees() - x.Float()).DegreesToRadians(), e, radius, x == e.coneAngleMin?1f:e.fillIntensity ?? Utils.DefaultFillIntensity, false);
                        }
                        AddConeLine(Utils.GetPlayerPositionXZY(), Svc.ClientState.LocalPlayer.Rotation, (Svc.ClientState.LocalPlayer.Rotation.RadiansToDegrees() - e.coneAngleMax.Float()).DegreesToRadians(), e, radius, 1f, true);
                    }
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
                        if (e.coneAngleMax > e.coneAngleMin)
                        {
                            for (var x = e.coneAngleMin; x < e.coneAngleMax; x += GetFillStepCone(e.FillStep))
                            {
                                var angle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()) - x.Float())).DegreesToRadians()
                                            : (Svc.Targets.Target.Rotation.RadiansToDegrees() - x.Float()).DegreesToRadians();
                                var baseAngle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                                            : Svc.Targets.Target.Rotation;
                                AddConeLine(Svc.Targets.Target.GetPositionXZY(), baseAngle, angle, e, radius, x == e.coneAngleMin?1f:e.fillIntensity ?? Utils.DefaultFillIntensity, false);
                            }
                            {
                                var angle = e.FaceMe ?
                                                (180 - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()) - e.coneAngleMax.Float())).DegreesToRadians()
                                                : (Svc.Targets.Target.Rotation.RadiansToDegrees() - e.coneAngleMax.Float()).DegreesToRadians();
                                var baseAngle = e.FaceMe ?
                                                (180 - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                                                : (Svc.Targets.Target.Rotation);
                                AddConeLine(Svc.Targets.Target.GetPositionXZY(), baseAngle, angle, e, radius, 1f, true);
                            }
                        }
                        //DisplayObjects.Add(new DisplayObjectCone(e, Svc.Targets.Target.Position, Svc.Targets.Target.Rotation, radius));
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
                                if (e.coneAngleMax > e.coneAngleMin)
                                {
                                    for (var x = e.coneAngleMin; x < e.coneAngleMax; x += GetFillStepCone(e.FillStep))
                                    {
                                        var angle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(a.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()) - x.Float())).DegreesToRadians()
                                            : (a.Rotation.RadiansToDegrees() - x.Float()).DegreesToRadians();
                                        var baseAngle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(a.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                                            : (a.Rotation);
                                        AddConeLine(a.GetPositionXZY(), baseAngle, angle, e, aradius, x == e.coneAngleMin?1f:e.fillIntensity ?? Utils.DefaultFillIntensity, false);
                                    }
                                    {
                                        var angle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(a.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()) - e.coneAngleMax.Float())).DegreesToRadians()
                                            : (a.Rotation.RadiansToDegrees() - e.coneAngleMax.Float()).DegreesToRadians();
                                        var baseAngle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(a.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                                            : (a.Rotation);
                                        AddConeLine(a.GetPositionXZY(), baseAngle, angle, e, aradius, 1f, true);
                                    }
                                }
                                //DisplayObjects.Add(new DisplayObjectCone(e, a.Position, a.Rotation, aradius));
                            }
                        }
                    }
                }
            }

        }
        else if (e.type == 2)
        {
            if (e.radius > 0)
            {
                if(!LayoutUtils.ShouldDraw(e.refX, Utils.GetPlayerPositionXZY().X, e.refY, Utils.GetPlayerPositionXZY().Y) 
                    && !LayoutUtils.ShouldDraw(e.offX, Utils.GetPlayerPositionXZY().X, e.offY, Utils.GetPlayerPositionXZY().Y)) return;
                Utils.PerpOffset(new Vector2(e.refX, e.refY), new Vector2(e.offX, e.offY), 0f, e.radius, out _, out var p1);
                Utils.PerpOffset(new Vector2(e.refX, e.refY), new Vector2(e.offX, e.offY), 0f, -e.radius, out _, out var p2);
                Utils.PerpOffset(new Vector2(e.refX, e.refY), new Vector2(e.offX, e.offY), 1f, e.radius, out _, out var p3);
                Utils.PerpOffset(new Vector2(e.refX, e.refY), new Vector2(e.offX, e.offY), 1f, -e.radius, out _, out var p4);
                var rect = new DisplayObjectRect()
                {
                    l1 = new DisplayObjectLine(p1.X, p1.Y, e.refZ,
                    p2.X, p2.Y, e.refZ,
                    e.thicc, e.color),
                    l2 = new DisplayObjectLine(p3.X, p3.Y, e.offZ,
                    p4.X, p4.Y, e.offZ,
                    e.thicc, e.color)
                };
                if (P.Config.AltRectFill)
                {
                    AddAlternativeFillingRect(rect, GetFillStepRect(e.FillStep), e.fillIntensity ?? Utils.DefaultFillIntensity, e.Filled);
                }
            }
            else
            {
                if (
                    (
                        i == null || !i.UseDistanceLimit || LayoutUtils.CheckDistanceToLineCondition(i, e)
                    ) &&
                    (
                LayoutUtils.ShouldDraw(e.offX, Utils.GetPlayerPositionXZY().X, e.offY, Utils.GetPlayerPositionXZY().Y)
                    || LayoutUtils.ShouldDraw(e.refX, Utils.GetPlayerPositionXZY().X, e.refY, Utils.GetPlayerPositionXZY().Y)
                    )
                    )
                    DisplayObjects.Add(new DisplayObjectLine(e.refX, e.refY, e.refZ, e.offX, e.offY, e.offZ, e.thicc, e.color));
            }
        }
        else if (e.type == 5)
        {
            if (e.coneAngleMax > e.coneAngleMin)
            {
                var pos = new Vector3(e.refX + e.offX, e.refY + e.offY, e.refZ + e.offZ);
                if(!LayoutUtils.ShouldDraw(pos.X, Utils.GetPlayerPositionXZY().X, pos.Y, Utils.GetPlayerPositionXZY().Y)) return;
                if (P.Config.FillCone)
                {
                    var baseAngle = e.FaceMe ? MathHelper.GetRelativeAngle(new Vector2(e.refX + e.offX, e.refY + e.offY), Marking.GetPlayer(e.faceplayer).Position.ToVector2()).DegreesToRadians() + MathF.PI : 0;
                    var startRad = baseAngle + e.coneAngleMin.Float().DegreesToRadians() + MathF.PI / 2;
                    var endRad = baseAngle + e.coneAngleMax.Float().DegreesToRadians() + MathF.PI / 2;
                    AddCone(pos, startRad, endRad, e, e.radius);
                }
                else
                {
                    for (var x = e.coneAngleMin; x < e.coneAngleMax; x += GetFillStepCone(e.FillStep))
                    {
                        var angle = e.FaceMe ?
                            (180 - (MathHelper.GetRelativeAngle(new Vector2(e.refX + e.offX, e.refY + e.offY), Marking.GetPlayer(e.faceplayer).Position.ToVector2()) - x.Float())).DegreesToRadians()
                            : (-x.Float()).DegreesToRadians();
                        var baseAngle = e.FaceMe ?
                            (180 - (MathHelper.GetRelativeAngle(new Vector2(e.refX + e.offX, e.refY + e.offY), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                            : 0;
                        AddConeLine(pos, baseAngle, angle, e, e.radius, x == e.coneAngleMin ? 1f : e.fillIntensity ?? Utils.DefaultFillIntensity, false);
                    }
                    {
                        var angle = e.FaceMe ?
                            (180 - (MathHelper.GetRelativeAngle(new Vector2(e.refX + e.offX, e.refY + e.offY), Marking.GetPlayer(e.faceplayer).Position.ToVector2()) - e.coneAngleMax.Float())).DegreesToRadians()
                            : (-e.coneAngleMax.Float()).DegreesToRadians();
                        var baseAngle = e.FaceMe ?
                            (180 - (MathHelper.GetRelativeAngle(new Vector2(e.refX + e.offX, e.refY + e.offY), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                            : 0;
                        AddConeLine(pos, baseAngle, angle, e, e.radius, 1f, true);
                    }
                }
            }
        }
    }

    private void DrawCircle(Element e, float x, float y, float z, float r, float angle, IGameObject go = null)
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
            DisplayObjects.Add(new DisplayObjectLine(cx,
                cy,
                z,
                Utils.GetPlayerPositionXZY().X, Utils.GetPlayerPositionXZY().Y, Utils.GetPlayerPositionXZY().Z,
                e.thicc, e.color));
        }
        if (!LayoutUtils.ShouldDraw(cx, Utils.GetPlayerPositionXZY().X, cy, Utils.GetPlayerPositionXZY().Y)) return;
        if (e.thicc > 0)
        {
            if (r > 0)
            {
                if (P.Config.UseFullDonutFill && e != null && e.Donut > 0 && !e.LegacyFill)
                {
                    DisplayObjects.Add(new DisplayObjectCircle(cx, cy, z + e.offZ, r, e.thicc, e.color, false, 1f));
                    DisplayObjects.Add(new DisplayObjectCircle(cx, cy, z + e.offZ, r + e.Donut, e.thicc, e.color, false, 1f));
                    DisplayObjects.Add(new DisplayObjectDonut(cx, cy, z + e.offZ, r, e.Donut, e.color, e.fillIntensity ?? Utils.DefaultFillIntensity));
                }
                else
                {
                    var filled = e.Filled && e.Donut == 0;
                    DisplayObjects.Add(new DisplayObjectCircle(cx, cy, z + e.offZ, r, e.thicc, filled?Utils.TransformAlpha(e.color, e.fillIntensity ?? Utils.DefaultFillIntensity):e.color, filled, e.fillIntensity ?? Utils.DefaultFillIntensity));
                    if(filled)
                    {
                        DisplayObjects.Add(new DisplayObjectCircle(cx, cy, z + e.offZ, r, e.thicc, e.color, false, 1f));
                    }
                    if (e != null && e.Donut > 0)
                    {
                        var donutR = GetFillStepDonut(e.FillStep);
                        while (donutR < e.Donut)
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

    private void AddRotatedLine(Vector3 tPos, float angle, Element e, float aradius, float hitboxRadius)
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
                if (P.Config.AltRectFill)
                {
                    AddAlternativeFillingRect(rect, GetFillStepRect(e.FillStep), e.fillIntensity ?? Utils.DefaultFillIntensity, e.Filled);
                }
            }
        }
        else
        {
            var (pointA, pointB) = CommonRenderUtils.GetNonRotatedPointsForZeroRadius(tPos, e, hitboxRadius, angle);
            if(!LayoutUtils.ShouldDraw(pointA.X, Utils.GetPlayerPositionXZY().X, pointA.Y, Utils.GetPlayerPositionXZY().Y)
                && !LayoutUtils.ShouldDraw(pointB.X, Utils.GetPlayerPositionXZY().X, pointB.Y, Utils.GetPlayerPositionXZY().Y)) return;
            DisplayObjects.Add(new DisplayObjectLine(pointA.X, pointA.Y, pointA.Z,
                pointB.X, pointB.Y, pointB.Z,
                e.thicc, e.color));
        }
    }

    private void AddCone(Vector3 center, float startRad, float endRad, Element e, float radius)
    {
        //PluginLog.Debug($"[addcone] {center}, {startRad} -> {endRad}"); 
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
        if (P.Config.AltRectStepOverride || original < P.Config.AltRectStep)
        {
            return P.Config.AltRectStep;
        }
        return original;
    }

    internal float GetFillStepDonut(float original)
    {
        if (P.Config.AltDonutStepOverride || original < P.Config.AltDonutStep)
        {
            return P.Config.AltDonutStep;
        }
        return original;
    }

    internal int GetFillStepCone(float original)
    {
        if (P.Config.AltConeStepOverride || original < P.Config.AltConeStep)
        {
            return P.Config.AltConeStep;
        }
        return (int)Math.Max(1f, original);
    }
}
