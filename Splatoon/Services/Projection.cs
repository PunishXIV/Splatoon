using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using Splatoon.Data;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using TerraFX.Interop.Windows;
using Action = Lumina.Excel.Sheets.Action;

namespace Splatoon.Services;

//parts of the code are from Veyn's Boss Mod
internal class Projection : IDisposable
{
    public static readonly float RaidwideSize = 30f;
    public static readonly float HalfWidth = 0.5f;
    public Dictionary<uint, Dictionary<ActionDescriptor, PacketActorCast>> LastCast = [];
    public bool Blacklist = false;
    public List<ProjectionItemDescriptor> ProjectingItems = [];
    Stopwatch Stopwatch = new();
    public long LastSw = 0;
    private Projection()
    {
        Svc.Framework.Update += Framework_Update;
    }

    List<Element> RentedElements = [];

    Element RentElement(int index, ShapeData shape, IBattleNpc caster)
    {
        Element ret;
        if(index < RentedElements.Count)
        {
            ret = RentedElements[index];
        }
        else
        {
            var element = new Element(default);
            RentedElements.Add(element);
            ret = element;
            PluginLog.Debug($"[Projection] Rented element count is now {RentedElements.Count}");
        }
        ret.type = shape.Shape switch
        {
            Shape.Circle => 1,
            Shape.Donut => 1,
            Shape.Rect => 3,
            Shape.Cross => 3,
            Shape.Cone => 4,
            _ => 1,
        };
        ret.refActorComparisonType = 2;
        ret.refActorObjectID = caster.ObjectId;
        ret.Filled = true;
        ret.fillIntensity = 0.4f;
        ret.includeRotation = true;
        ret.AdditionalRotation = 0f;
        ret.SetRefPosition(Vector3.Zero);
        ret.SetOffPosition(Vector3.Zero);
        ret.color = GradientColor.Get(P.Config.ProjectionColor1, P.Config.ProjectionColor2, P.Config.ProjectionPulseTime).ToUint();
        ret.castAnimation = P.Config.ProjectionCastAnimation;
        ret.animationColor = GradientColor.Get(P.Config.AnimationColor1, P.Config.AnimationColor2, P.Config.ProjectionPulseTime).ToUint();
        ret.faceplayer = "";
        ret.RotationOverrideAngleOnlyMode = false;
        ret.RotationOverride = false;
        ret.CastFractionOverride = caster.CurrentCastTime / caster.TotalCastTime;
        ret.fillIntensity = P.Config.ProjectionFillIntensity;
        return ret;
    }

    private unsafe void Framework_Update(Dalamud.Plugin.Services.IFramework framework)
    {
        Stopwatch.Reset();
        Stopwatch.Start();
        if(P.ConfigGui.Open)
        {
            ProjectingItems.Clear();
        }
        int elementIndex = 0;
        List<(IBattleNpc obj, Element element)> injectedElements = [];
        foreach(var x in Svc.Objects)
        {
            if(x is IBattleNpc b && b.GetNameplateKind() != NameplateKind.FriendlyBattleNPC && b.IsCasting() && b.CastActionType == (int)ActionType.Action && Svc.Data.GetExcelSheet<Action>().TryGetRow(b.CastActionId, out var data))
            {
                var shape = GuessShapeAndSize(data, b);
                var info = b.Struct()->GetCastInfo();
                uint targetObjectId = (info->TargetId.ObjectId != 0xE000_0000) ? info->TargetId.ObjectId : b.ObjectId;
                if(shape.Range > 0f && (data.EffectRange < RaidwideSize || shape.Shape != Shape.Circle))
                {
                    var blacklisted = false;
                    foreach(var a in P.Config.ProjectionBlacklistedActions)
                    {
                        if(a.Action == data.RowId)
                        {
                            blacklisted = true;
                        }
                    }
                    ProjectionItemDescriptor descriptor = P.ConfigGui.Open ? new(new(b.CastActionType, b.CastActionId), b.ObjectId, blacklisted) : null;
                    this.ProjectingItems.Add(descriptor);
                    bool? showOverride = null;
                    bool isAlreadyProcessed = false;
                    foreach(var layout in P.Config.LayoutsL)
                    {
                        if(LayoutUtils.IsLayoutEnabled(layout))
                        {
                            if(layout.ForcedProjectorActions.Contains(b.CastActionId))
                            {
                                descriptor?.WhitelistingLayouts.Add(layout.InternationalName.Get(layout.Name));
                                showOverride = true;
                            }
                            if(layout.BlacklistedProjectorActions.Contains(b.CastActionId))
                            {
                                descriptor?.BlacklistingLayouts.Add(layout.InternationalName.Get(layout.Name));
                                showOverride = false;
                            }
                            if(!isAlreadyProcessed) 
                            {
                                foreach(var layoutElement in layout.ElementsL)
                                {
                                    if(layoutElement.Enabled
                                        && layoutElement.type.EqualsAny(1, 3, 4)
                                        && layoutElement.refActorRequireCast
                                        && layoutElement.refActorCastId.Contains(b.CastActionId)
                                        && LayoutUtils.IsAttributeMatches(layoutElement, b)
                                        )
                                    {
                                        isAlreadyProcessed = true;
                                        descriptor?.SuppressingLayouts.Add(layout.InternationalName.Get(layout.Name));
                                    }
                                }
                            }
                        }
                    }

                    var shouldHide = showOverride == false || isAlreadyProcessed || (blacklisted && showOverride != true) || (showOverride != true && !P.Config.EnableProjection);

                    if(shouldHide)
                    {
                        goto Next;
                    }
                    descriptor?.Rendered = true;
                    var element = RentElement(elementIndex++, shape, b);
                    var rotation = 0f;
                    if(LastCast.TryGetValue(b.ObjectId, out var list) && list.TryGetValue(new(b.CastActionType, b.CastActionId), out var packet) && packet.ActionType == b.CastActionType)
                    {
                        rotation = 180 + packet.RotationRadians.RadToDeg();
                        element.RotationOverrideAddAngle = rotation;
                        element.RotationOverrideAngleOnlyMode = true;
                        element.RotationOverride = true;
                        //PluginLog.Information($"{JsonConvert.SerializeObject(element)}");
                    }
                    if(Blacklist)
                    {
                        if(!P.Config.ProjectionBlacklistedActions.Any(a => a.Action == data.RowId))
                        {
                            P.Config.ProjectionBlacklistedActions.Add(new()
                            {
                                Action = data.RowId,
                                DataId = b.DataId,
                                NameId = b.NameId,
                                ModelId = (uint)b.Struct()->ModelContainer.ModelCharaId,
                                Territory = Player.Territory
                            });
                        }
                    }
                    if(shape.Shape == Shape.Circle)
                    {
                        element.refActorObjectID = targetObjectId;
                        element.radius = shape.Range;
                        element.Donut = 0;
                        if(info->TargetId.ObjectId == 0xE000_0000)
                        {
                            element.type = 0;
                            element.SetRefPosition(info->TargetLocation);
                        }
                    }
                    else if(shape.Shape == Shape.Donut)
                    {
                        element.refActorObjectID = targetObjectId;
                        element.radius = shape.AngleOrWidth;
                        element.Donut = shape.Range;
                        if(info->TargetId.ObjectId == 0xE000_0000)
                        {
                            element.type = 0;
                            element.SetRefPosition(info->TargetLocation);
                        }
                    }
                    else if(shape.Shape == Shape.Rect)
                    {
                        element.radius = shape.AngleOrWidth;
                        element.offY = shape.Range;
                        if(targetObjectId != b.ObjectId)
                        {
                            element.faceplayer = $"<objectid:{targetObjectId}>";
                        }
                    }
                    else if(shape.Shape == Shape.Cross)
                    {
                        element.radius = shape.AngleOrWidth;
                        element.offY = shape.Range;
                        element.refY = -shape.Range;
                        var next = RentElement(elementIndex++, shape, b);
                        next.radius = shape.AngleOrWidth;
                        next.offY = shape.Range;
                        next.refY = -shape.Range;
                        next.RotationOverrideAddAngle = rotation;
                        next.RotationOverrideAngleOnlyMode = true;
                        next.RotationOverride = true;
                        next.AdditionalRotation = 90f.DegToRad();
                        injectedElements.Add((b, next));
                    }
                    else if(shape.Shape == Shape.Cone)
                    {
                        element.radius = shape.Range;
                        element.coneAngleMin = (int)(-shape.AngleOrWidth.RadToDeg());
                        element.coneAngleMax = (int)(shape.AngleOrWidth.RadToDeg());

                        if(targetObjectId != b.ObjectId)
                        {
                            element.faceplayer = $"<objectid:{targetObjectId}>";
                        }
                    }
                    injectedElements.Add((b, element));
                }
            }
        Next:;
        }
        List<Guid> skip = [];
        foreach(var x in injectedElements)
        {
            if(x.element.type == 4 || x.element.type == 3) //find opposing cones and lines
            {
                foreach(var y in injectedElements)
                {
                    //now find if there are also cones that are the same but "look" in opposite direction
                    if(y.element.type == x.element.type)
                    {
                        var angle1 = Utils.GetRotationWithOverride(x.obj, x.element);
                        var angle2 = Utils.GetRotationWithOverride(y.obj, y.element);
                        if(
                            Math.Abs((angle1 - angle2).RadToDeg()).ApproximatelyEquals(180, 1)
                            )
                        {
                            if(x.obj.RemainingCastTime - y.obj.RemainingCastTime > 0.2f)
                            {
                                skip.Add(x.element.GUID);
                            }
                            else if(!x.obj.RemainingCastTime.ApproximatelyEquals(y.obj.RemainingCastTime, 0.2f))
                            {
                                skip.Add(y.element.GUID);
                            }
                        }
                    }
                }
            }
        }
        foreach(var x in injectedElements)
        {
            if(!skip.Contains(x.element.GUID)) P.InjectElement(x.element);
        }
        Blacklist = false;
        Stopwatch.Stop();
        LastSw = Stopwatch.ElapsedTicks;
    }

    public ShapeData GuessShapeAndSize(Action data, IGameObject actor)
    {
        return data.CastType switch
        {
            2 => new(Shape.Circle, data.EffectRange),
            3 => new(Shape.Cone , data.EffectRange + actor.HitboxRadius, DetermineConeAngle(data).Rad * HalfWidth),
            4 => new(Shape.Rect, data.EffectRange + actor.HitboxRadius, data.XAxisModifier * HalfWidth),
            5 => new(Shape.Circle, data.EffectRange + actor.HitboxRadius),
            //6 => custom shapes
            //7 => new AOEShapeCircle(data.EffectRange), - used for player ground-targeted circles a-la asylum
            //8 => new(Shape.Rect, default, data.XAxisModifier * HalfWidth), // charges
            10 => new(Shape.Donut, DetermineDonutRange(data)?.Outer ?? 0, DetermineDonutRange(data)?.Inner ?? 0),
            11 => new(Shape.Cross, data.EffectRange, data.XAxisModifier * HalfWidth),
            12 => new(Shape.Rect, data.EffectRange, data.XAxisModifier * HalfWidth),
            13 => new(Shape.Cone, data.EffectRange, DetermineConeAngle(data).Rad * HalfWidth),
            _ => default
        };
    }

    Dictionary<string, (float Inner, float Outer)?> DonutCache = [];
    public (float Inner, float Outer)? DetermineDonutRange(Action data)
    {
        var path = data.Omen.Value.Path.ToString();
        if(DonutCache.TryGetValue(path, out var result))
        {
            return result;
        }
        var regex = Regex.Match(path, @"sircle_([0-9]{2})([0-9]{2})");
        if(regex.Success && int.TryParse(regex.Groups[1].Value, out var outer) && int.TryParse(regex.Groups[2].Value, out var inner))
        {
            result = (inner, outer - inner);
            DonutCache[path] = result;
            PluginLog.Debug($"Omen {path} donut inner radius {inner} outer {outer}");
            return result;
        }
        else
        {
            PluginLog.Debug($"Omen {path} failed to parse donut");
            DonutCache[path] = null;
            return null;
        }
    }

    private static Angle DetermineConeAngle(Action data)
    {
        if(data.Omen.ValueNullable == null)
        {
            var text = $"[Projection] No omen data for {ExcelActionHelper.GetActionName(data.RowId, true)}";
            if(EzThrottler.Throttle($"DisplayLog{text}", 10000)) Svc.Log.Debug(text);
            return 180f.Degrees();
        }
        var path = data.Omen.Value.Path.ToString();
        var pos = data.Omen.Value.Path.ToString().IndexOf("fan", StringComparison.Ordinal);
        if(pos < 0 || pos + 6 > path.Length || !int.TryParse(path.AsSpan(pos + 3, 3), out var angle))
        {
            var text = $"[Projection] Can't determine angle from omen ({path}/{data.Omen.Value.PathAlly}) for {ExcelActionHelper.GetActionName(data.RowId, true)}";
            if(EzThrottler.Throttle($"DisplayLog{text}", 10000)) Svc.Log.Debug(text);
            return 180.Degrees();
        }
        return angle.Degrees();
    }

    public void Dispose()
    {
        Svc.Framework.Update -= Framework_Update;
    }

    public readonly record struct ShapeData(Shape Shape, float Range, float AngleOrWidth = 0);

    public enum Shape { Circle, Rect, Donut, Cone, Cross}
}
