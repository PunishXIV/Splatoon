using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Utility;
using ECommons;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.Interop;
using ECommons.LanguageHelpers;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Lua;
using Newtonsoft.Json;
using Splatoon.Memory;
using Splatoon.RenderEngines;
using Splatoon.Serializables;
using Splatoon.Structures;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkTimer.Delegates;
using S = Splatoon.Services.S;
#nullable enable

namespace Splatoon.Utility;

public static unsafe class Utils
{
    public static uint BlendColors(uint bottom, uint top)
    {
        float br = (bottom & 0xFF) / 255f;
        float bg = ((bottom >> 8) & 0xFF) / 255f;
        float bb = ((bottom >> 16) & 0xFF) / 255f;
        float ba = ((bottom >> 24) & 0xFF) / 255f;

        float tr = (top & 0xFF) / 255f;
        float tg = ((top >> 8) & 0xFF) / 255f;
        float tb = ((top >> 16) & 0xFF) / 255f;
        float ta = ((top >> 24) & 0xFF) / 255f;

        float outA = ta + ba * (1f - ta);
        if(outA == 0f) return 0;

        float outR = (tr * ta + br * ba * (1f - ta)) / outA;
        float outG = (tg * ta + bg * ba * (1f - ta)) / outA;
        float outB = (tb * ta + bb * ba * (1f - ta)) / outA;

        return ((uint)(outR * 255f) & 0xFF)
             | (((uint)(outG * 255f) & 0xFF) << 8)
             | (((uint)(outB * 255f) & 0xFF) << 16)
             | (((uint)(outA * 255f) & 0xFF) << 24);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Start">XYZ</param>
    /// <param name="End">XYZ</param>
    /// <param name="Thickness"></param>
    /// <param name="Color"></param>
    public readonly record struct PointerLineSegment(Vector3 Start, Vector3 End, float Thickness, uint Color);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="start">XYZ</param>
    /// <param name="end">XYZ</param>
    /// <param name="style"></param>
    /// <returns></returns>
    public static List<PointerLineSegment> PreparePointerLine(Vector3 start, Vector3 end, PointerLineStyle style)
    {
        var result = Utils.SplitLine(start, end, style.ChunkLength!.Value, style.IntervalLength!.Value, (float)((Environment.TickCount64 / (double)style.AnimationDuration!.Value) % 1d));
        List<PointerLineSegment> ret = [with(result.Count * 3)];
        for(var i = 0; i < result.Count; i++)
        {
            var x = result[i];
            var col = Utils.GetSegmentColor(style.Background!.Value, style.Accent!.Value, style.TotalSegments!.Value, style.GradientStrength!.Value, result.Count - 1 - i, style.AccentLength!.Value, (int)((Environment.TickCount64 / style.ColorShiftDuration!.Value) % style.TotalSegments!.Value));
            if(x.Start != null && x.End != null)
            {
                ret.Add(new(x.Start.Value, x.End.Value, style.Thickness!.Value, col));
            }
            if(x.End != null) 
            {
                var total = style.ChunkLength.Value == 0?style.IntervalLength.Value:style.ChunkLength.Value;
                var tip = total * style.TipLength!.Value;
                var perp1 = Utils.GetPerpendicularPointUnitY(start, x.End.Value, -style.Width!.Value, -tip);
                var perp2 = Utils.GetPerpendicularPointUnitY(start, x.End.Value, style.Width!.Value, -tip);
                ret.Add(new(perp1.Point, perp1.ShiftedB, style.Thickness!.Value, col));
                ret.Add(new(perp2.Point, perp2.ShiftedB, style.Thickness!.Value, col));
            }
        }
        return ret;
    }

    public readonly record struct LineSegment(Vector3? Start, Vector3? End, Vector3 IntervalEnd);

    public static (uint Background, uint Accent) GetSpreadColors(uint color, float spread)
    {
        spread = Math.Clamp(spread, 0f, 1f);
        var half = spread / 2f;
        var alpha = ((color >> 24) & 0xFF) / 255f;
        var bgAlpha = alpha - half;
        var accentAlpha = alpha + half;
        if(bgAlpha < 0f)
        {
            bgAlpha = 0f;
            accentAlpha = spread;
        }
        else if(accentAlpha > 1f)
        {
            accentAlpha = 1f;
            bgAlpha = 1f - spread;
        }
        var rgb = color & 0x00FFFFFF;
        var background = rgb | ((uint)MathF.Round(bgAlpha * 255f) << 24);
        var accent = rgb | ((uint)MathF.Round(accentAlpha * 255f) << 24);
        return (background, accent);
    }

    public static List<LineSegment> SplitLine(Vector3 a, Vector3 b, float chunkLength, float interval = 0f, float shift = 0f)
    {
        var delta = b - a;
        var totalLength = delta.Length();

        if(totalLength == 0f)
        {
            return [new LineSegment { Start = a, End = b, IntervalEnd = b }];
        }

        var dir = delta / totalLength;

        var stride = chunkLength + interval;
        var shiftDist = shift * stride;

        if(chunkLength == 0f)
        {
            var effectiveInterval = interval <= 0f ? 0f : interval;
            if(effectiveInterval <= 0f)
            {
                return [new LineSegment { Start = null, End = b, IntervalEnd = b }];
            }

            var shiftedLength = totalLength + shiftDist;
            var count = (int)MathF.Ceiling(shiftedLength / effectiveInterval) + 1;
            var points = new List<LineSegment>();

            for(var i = 0; i < count; i++)
            {
                var distFromB = (i * effectiveInterval) - shiftDist;
                if(distFromB > totalLength)
                {
                    break;
                }

                var nextDistFromB = ((i + 1) * effectiveInterval) - shiftDist;
                var clampedNext = MathF.Min(MathF.Max(nextDistFromB, 0f), totalLength);

                var clampedDist = MathF.Max(distFromB, 0f);
                var end = b - (dir * MathF.Min(clampedDist, totalLength));
                var intervalEnd = b - (dir * clampedNext);

                if(distFromB < 0f)
                {
                    if(nextDistFromB < 0f)
                    {
                        continue;
                    }

                    points.Add(new LineSegment { Start = null, End = b, IntervalEnd = intervalEnd });
                }
                else
                {
                    points.Add(new LineSegment { Start = null, End = end, IntervalEnd = intervalEnd });
                }
            }

            return points;
        }

        var shiftedLength2 = totalLength + shiftDist;
        var segCount = (int)MathF.Ceiling(shiftedLength2 / stride);
        var segments = new List<LineSegment>();

        for(var i = 0; i < segCount; i++)
        {
            var chunkEnd = (i * stride) - shiftDist;
            var chunkStart = chunkEnd + chunkLength;
            var intervalEnd = chunkStart + interval;

            if(chunkEnd > totalLength)
            {
                break;
            }

            var chunkVisible = chunkStart >= 0f;
            var intervalVisible = intervalEnd > 0f && interval > 0f;

            if(!chunkVisible && !intervalVisible)
            {
                continue;
            }

            var clampedEnd = chunkVisible ? MathF.Max(chunkEnd, 0f) : (float?)null;
            var clampedStart = chunkVisible ? MathF.Min(chunkStart, totalLength) : (float?)null;
            var intervalStart = MathF.Min(MathF.Max(chunkEnd, 0f), totalLength);
            var clampedInterval = MathF.Min(MathF.Max(intervalEnd, 0f), totalLength);

            segments.Add(new LineSegment
            {
                Start = clampedStart.HasValue ? b - (dir * clampedStart.Value) : null,
                End = chunkVisible ? b - (dir * clampedEnd!.Value) : b - (dir * intervalStart),
                IntervalEnd = b - (dir * clampedInterval),
            });
        }

        return segments;
    }

    public static uint GetSegmentColor(uint background, uint accent, int totalSegments, int gradientStrength, int segmentIndex, int accentLength = 1, int offset = 0)
    {
        var normalized = (((segmentIndex - offset) % totalSegments) + totalSegments) % totalSegments;
        var dist = Math.Min(normalized, totalSegments - normalized);
        if(dist < accentLength)
        {
            return accent;
        }

        var gradDist = dist - accentLength;
        if(gradientStrength == 0 || gradDist >= gradientStrength)
        {
            return background;
        }

        var t = (gradDist + 1f) / (gradientStrength + 1f);
        return LerpColor(accent, background, t);
    }

    private static byte Lerp(byte a, byte b, float t) => (byte)(a + ((b - a) * t));
    private static uint LerpColor(uint colorA, uint colorB, float t)
    {
        var rA = (byte)(colorA & 0xFF);
        var gA = (byte)((colorA >> 8) & 0xFF);
        var bA = (byte)((colorA >> 16) & 0xFF);
        var aA = (byte)((colorA >> 24) & 0xFF);

        var rB = (byte)(colorB & 0xFF);
        var gB = (byte)((colorB >> 8) & 0xFF);
        var bB = (byte)((colorB >> 16) & 0xFF);
        var aB = (byte)((colorB >> 24) & 0xFF);

        return (uint)(Lerp(rA, rB, t) | (Lerp(gA, gB, t) << 8) | (Lerp(bA, bB, t) << 16) | (Lerp(aA, aB, t) << 24));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Point">XYZ</param>
    /// <param name="ShiftedB">XYZ</param>
    public readonly record struct PerpendicularPoint(Vector3 Point, Vector3 ShiftedB);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="a">XYZ</param>
    /// <param name="b">XYZ</param>
    /// <param name="perpendicularOffset"></param>
    /// <param name="lineOffset"></param>
    /// <param name="bShift"></param>
    /// <returns></returns>
    public static PerpendicularPoint GetPerpendicularPointUnitY(Vector3 a, Vector3 b, float perpendicularOffset, float lineOffset = 0f, float bShift = 0f)
    {
        Vector3 forward = Vector3.Normalize(b - a);
        var up = Vector3.UnitY;
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, up));
        var shiftedB = b + (forward * bShift);
        var point = shiftedB + (forward * lineOffset) + (right * perpendicularOffset);
        return new(point, shiftedB);
    }

    public static void SetCursorTo(float refX, float refZ, float refY)
    {
        if(Utils.WorldToScreen(new Vector3(refX, refZ, refY), out var screenPos) && WindowFunctions.TryFindGameWindow(out var handle))
        {
            var point = new POINT() { x = (int)screenPos.X, y = (int)screenPos.Y };
            //Chat.Print(point.X + "/" + point.Y);
            if(TerraFX.Interop.Windows.Windows.ClientToScreen(handle, &point))
            {
                //Chat.Print(point.X + "/" + point.Y);
                TerraFX.Interop.Windows.Windows.SetCursorPos(point.x, point.y);
            }
        }
    }

    internal static void Reset()
    {
        var phase = Splatoon.P.Phase;
        Splatoon.P.TerritoryChangedEvent(0);
        foreach(var x in P.Config.LayoutsL)
        {
            x.FreezeInfo = new();
        }
        Notify.Success("Reset");
        if(Splatoon.P.Phase != phase)
        {
            Splatoon.P.Phase = phase;
            Notify.Info($"Returned to phase {phase}");
        }
        AttachedInfo.CastInfos.Clear();
        AttachedInfo.VFXInfos.Clear();
        AttachedInfo.TetherInfos.Clear();
        SplatoonScripting.ScriptingProcessor.Scripts.Where(x => x.IsEnabled).Each(x => x.Controller.Reset());
    }

    /// <summary>
    /// Returns element from layout by name, or null, if absent. If multiple elements of the same name are present, will return first only.
    /// </summary>
    /// <param name="layout"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Element? GetElement(this Layout layout, string name)
    {
        return layout.ElementsL.FirstOrDefault(x => x.Name == name);
    }

    /// <summary>
    /// Returns element from layout by index, or null, if absent. Does not throws on index out of bounds.
    /// </summary>
    /// <param name="layout"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static Element? GetElement(this Layout layout, int index)
    {
        return layout.ElementsL.SafeSelect(index);
    }

    public static IEnumerable<Element> GetElements(this Layout layout, string name)
    {
        foreach(var x in layout.ElementsL)
        {
            if(x.Name == name)
            {
                yield return x;
            }
        }
    }

    /// <summary>
    /// Disables all elements of layout
    /// </summary>
    /// <param name="layout"></param>
    public static void Hide(this Layout layout)
    {
        foreach(var x in layout.ElementsL)
        {
            x.Enabled = false;
        }
    }

    /// <summary>
    /// Enables all elements of layout
    /// </summary>
    /// <param name="layout"></param>
    public static void Show(this Layout layout)
    {
        foreach(var x in layout.ElementsL)
        {
            x.Enabled = true;
        }
    }

    public static void SetElementsColor(this Layout v, uint col)
    {
        foreach(var x in v.ElementsL)
        {
            x.color = col;
        }
    }

    public static Vector4 GetAttentionColor()
    {
        var cycleSeconds = Math.Max(P.Config.AttentionColorCycle, 0.1f);
        if(P.Config.AttentionColorType == AttentionColorType.Rainbow)
        {

            var ms = Environment.TickCount64;
            var t = ms / 1000d / cycleSeconds;
            var hue = t % 1f;
            return HsvToVector4(hue, 1f, 1f);
        }
        else if(P.Config.AttentionColorType == AttentionColorType.Gradient)
        {
            return GradientColor.Get(P.Config.AttentionColor1, P.Config.AttentionColor2, (int)(cycleSeconds * 500));
        }
        else
        {
            return P.Config.AttentionColor1;
        }
    }

    public static Vector4 HsvToVector4(double h, double s, double v)
    {
        double r = 0f, g = 0f, b = 0f;
        var i = (int)(h * 6f);
        var f = (h * 6f) - i;
        var p = v * (1f - s);
        var q = v * (1f - (f * s));
        var t = v * (1f - ((1f - f) * s));

        switch(i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new Vector4((float)r, (float)g, (float)b, 1f);
    }

    public static GameObject* ResolvePronounBPO(string p)
    {
        var ret = ExtendedPronoun.Resolve(p);
        if(Svc.Condition[ConditionFlag.DutyRecorderPlayback] && BasePlayerOverride != "")
        {
            if(p == "<me>")
            {
                return (GameObject*)BasePlayer.Struct();
            }
            else if(p == "<target>")
            {
                return (GameObject*)(BasePlayer.TargetObject?.Address);
            }
        }
        return ret;
    }

    public static List<IGameObject> AlterTargetIfNeeded(Element element, IGameObject go)
    {
        List<IGameObject> ret = [go];
        if(element.TargetAlteration == TargetAlteration.Tethered)
        {
            ret.Clear();
            {
                if(go is ICharacter chr)
                {
                    var c = chr.Struct();
                    for(var i = 0; i < c->Vfx.Tethers.Length; i++)
                    {
                        var t = c->Vfx.Tethers[i];
                        if(t.Id != 0)
                        {
                            var target = Svc.Objects.FirstOrDefault(x => x.GameObjectId == t.TargetId);
                            if(target != null)
                            {
                                ret.Add(target);
                            }
                        }
                    }
                }
            }

            foreach(var x in Svc.Objects)
            {
                if(x is ICharacter chr)
                {
                    var c = chr.Struct();
                    for(var i = 0; i < c->Vfx.Tethers.Length; i++)
                    {
                        var t = c->Vfx.Tethers[i];
                        if(t.Id != 0 && t.TargetId == go.GameObjectId)
                        {
                            ret.Add(x);
                        }
                    }
                }
            }
        }
        else if(element.TargetAlteration == TargetAlteration.Targeted)
        {
            ret.Clear();
            if(go is ICharacter chr)
            {
                var t = chr.TargetObject;
                if(t != null)
                {
                    ret.Add(t);
                }
            }
        }
        else if((int)element.TargetAlteration >= 1100 && (int)element.TargetAlteration <= 1200)
        {
            ret.Clear();
            if(go != null)
            {
                var index = (int)element.TargetAlteration - 1100;
                var i = 0;
                foreach(var x in Svc.Objects.OfType<IPlayerCharacter>().Where(x => !x.IsDead).OrderBy(o => Vector3.DistanceSquared(o.Position, go.Position)))
                {
                    if(index == i)
                    {
                        ret.Add(x);
                        break;
                    }
                    i++;
                }
            }
        }
        else if((int)element.TargetAlteration >= 2100 && (int)element.TargetAlteration <= 2200)
        {
            ret.Clear();
            if(go != null)
            {
                var index = (int)element.TargetAlteration - 2100;
                var i = 0;
                foreach(var x in Svc.Objects.OfType<IPlayerCharacter>().Where(x => !x.IsDead).OrderByDescending(o => Vector3.DistanceSquared(o.Position, go.Position)))
                {
                    if(index == i)
                    {
                        ret.Add(x);
                        break;
                    }
                    i++;
                }
            }
        }
        return ret;
    }

    /// <summary>
    /// XYZ format
    /// </summary>
    /// <param name="layout"></param>
    /// <param name="go"></param>
    /// <param name="placeholder"></param>
    /// <returns></returns>
    public static List<Vector3> GetFacePositions(Layout layout, IGameObject go, string placeholder)
    {
        if(placeholder.StartsWith("<element:"))
        {
            var details = placeholder[1..^1].Split(":");
            var list = details.Length == 2
                ? Splatoon.CapturedPositions.SafeSelect(layout?.GetName())?.SafeSelect(details[1])
                : Splatoon.CapturedPositions.SafeSelect(details[1])?.SafeSelect(details[2]);
            return list ?? [];
        }
        if(placeholder.StartsWith("<objectid:"))
        {
            var details = placeholder[1..^1].Split(":");
            if(uint.TryParse(details[1], details[1].StartsWith("0x") ? NumberStyles.HexNumber : default, null, out var result))
            {
                return Svc.Objects.Where(x => x.ObjectId == result).Select(x => x.Position).ToList();
            }
        }
        if(placeholder == "<tethered>")
        {
            var ret = new List<Vector3>();
            {
                if(go is ICharacter chr)
                {
                    var c = chr.Struct();
                    for(var i = 0; i < c->Vfx.Tethers.Length; i++)
                    {
                        var t = c->Vfx.Tethers[i];
                        if(t.Id != 0)
                        {
                            var target = Svc.Objects.FirstOrDefault(x => x.GameObjectId == t.TargetId);
                            if(target != null)
                            {
                                ret.Add(target.Position);
                            }
                        }
                    }
                }
            }

            foreach(var x in Svc.Objects)
            {
                if(x is ICharacter chr)
                {
                    var c = chr.Struct();
                    for(var i = 0; i < c->Vfx.Tethers.Length; i++)
                    {
                        var t = c->Vfx.Tethers[i];
                        if(t.Id != 0 && t.TargetId == go?.GameObjectId)
                        {
                            ret.Add(x.Position);
                        }
                    }
                }
            }
            return ret;
        }
        var obj = Utils.ResolvePronounBPO(placeholder);
        if(obj != null)
        {
            return [obj->Position];
        }
        return [];
    }

    public static string GetShortName(this Expansion ex)
    {
        return ex switch
        {
            Expansion.A_Realm_Reborn => "ARR".Loc(),
            Expansion.Heavensward => "HW".Loc(),
            Expansion.Stormblood => "SB".Loc(),
            Expansion.Shadowbringers => "ShB".Loc(),
            Expansion.Endwalker => "EW".Loc(),
            Expansion.Dawntrail => "DT".Loc(),
            _ => ex.ToString().Replace('_', ' '),
        };
    }

    public static Expansion DetermineExpansion(this Layout l)
    {
        if(l.ZoneLockH.Count == 0)
        {
            return Expansion.Mixed;
        }
        else
        {
            var enumerator = l.ZoneLockH.GetEnumerator();
            enumerator.MoveNext();
            var initial = DetermineExpansion(enumerator.Current);
            while(enumerator.MoveNext())
            {
                if(DetermineExpansion(enumerator.Current) != initial)
                {
                    return Expansion.Mixed;
                }
            }
            return initial;
        }
    }

    private static Dictionary<uint, Expansion> ExpansionCache = [];
    public static Expansion DetermineExpansion(uint territoryType)
    {
        if(!ExpansionCache.TryGetValue(territoryType, out var ret))
        {
            ret = Expansion.A_Realm_Reborn;
            var data = ExcelTerritoryHelper.Get(territoryType);
            if(data != null)
            {
                var bg = data.Value.Bg.GetText();
                for(var i = 1; i <= 5; i++)
                {
                    if(bg.StartsWith($"ex{i}/"))
                    {
                        ret = (Expansion)i;
                        break;
                    }
                }
            }
            ExpansionCache[territoryType] = ret;
        }
        return ret;
    }

    public static ContentCategory DetermineContentCategory(this Layout l)
    {
        if(l.ZoneLockH.Count == 0)
        {
            return ContentCategory.Mixed;
        }
        else
        {
            var enumerator = l.ZoneLockH.GetEnumerator();
            enumerator.MoveNext();
            var initial = DetermineContentCategory(enumerator.Current);
            while(enumerator.MoveNext())
            {
                if(DetermineContentCategory(enumerator.Current) != initial)
                {
                    return ContentCategory.Mixed;
                }
            }
            return initial;
        }
    }
    private static Dictionary<uint, ContentCategory> ContentCategoryCache = [];
    public static ContentCategory DetermineContentCategory(uint territoryType)
    {
        if(!ContentCategoryCache.TryGetValue(territoryType, out var ret))
        {
            ret = ContentCategory.Other;
            var data = ExcelTerritoryHelper.Get(territoryType);
            if(data != null)
            {
                var use = data.Value.GetTerritoryIntendedUse();
                if(use.EqualsAny([
                    TerritoryIntendedUseEnum.City_Area,
                    TerritoryIntendedUseEnum.Open_World,
                    TerritoryIntendedUseEnum.Residential_Area,
                    TerritoryIntendedUseEnum.Housing_Instances,
                    TerritoryIntendedUseEnum.Inn,
                    TerritoryIntendedUseEnum.Gold_Saucer,
                    TerritoryIntendedUseEnum.Diadem,
                    TerritoryIntendedUseEnum.Barracks,
                    TerritoryIntendedUseEnum.Island_Sanctuary,
                    TerritoryIntendedUseEnum.Diadem_2,
                    TerritoryIntendedUseEnum.Diadem_3,
                    ]))
                {
                    ret = ContentCategory.World;
                }
                else if(use.EqualsAny([
                    TerritoryIntendedUseEnum.Dungeon,
                    TerritoryIntendedUseEnum.Variant_Dungeon,
                    TerritoryIntendedUseEnum.Deep_Dungeon,
                    TerritoryIntendedUseEnum.Criterion_Duty,
                    TerritoryIntendedUseEnum.Criterion_Savage_Duty,
                    ]))
                {
                    ret = ContentCategory.Dungeon;
                }
                else if(use.EqualsAny([
                    TerritoryIntendedUseEnum.Trial,
                    ]))
                {
                    ret = ContentCategory.Trial;
                }
                else if(use.EqualsAny([
                    TerritoryIntendedUseEnum.Raid,
                    TerritoryIntendedUseEnum.Raid_2,
                    ]))
                {
                    ret = ContentCategory.Raid;
                }
                else if(use.EqualsAny([
                    TerritoryIntendedUseEnum.Alliance_Raid,
                    TerritoryIntendedUseEnum.Large_Scale_Raid,
                    TerritoryIntendedUseEnum.Large_Scale_Savage_Raid,
                    ]))
                {
                    ret = ContentCategory.Alliance;
                }
                else if(use.EqualsAny([
                    TerritoryIntendedUseEnum.Bozja,
                    TerritoryIntendedUseEnum.Eureka,
                    TerritoryIntendedUseEnum.Occult_Crescent,
                    ]))
                {
                    ret = ContentCategory.Foray;
                }
            }
            ContentCategoryCache[territoryType] = ret;
        }
        return ret;
    }

    public static bool IsActorNameUsed(this Element e)
    {
        if(e.refActorType != 0)
        {
            return false;
        }

        if(!e.refActorComparisonAnd && e.refActorComparisonType != 0)
        {
            return false;
        }

        if(e.refActorName.EqualsAny("", "*") && e.refActorNameIntl.IsEmpty())
        {
            return false;
        }

        return true;
    }

    public static bool IsValid(this Layout l)
    {
        if(l == null)
        {
            return false;
        }

        if(l.Name == null || !l.InternationalName.IsValid())
        {
            return false;
        }

        if(l.Description == null || !l.InternationalDescription.IsValid())
        {
            return false;
        }

        if(l.ZoneLockH == null)
        {
            return false;
        }

        if(l.Group == null)
        {
            return false;
        }

        if(l.Scenes == null)
        {
            return false;
        }

        if(l.Subconfigurations == null)
        {
            return false;
        }

        if(l.JobLockH == null)
        {
            return false;
        }

        if(l.Triggers == null || !l.Triggers.All(IsValid))
        {
            return false;
        }

        if(l.ElementsL == null || !l.ElementsL.All(IsValid))
        {
            return false;
        }

        return true;
    }

    public static bool IsValid(this Trigger t)
    {
        if(t == null)
        {
            return false;
        }

        if(t.Match == null || !t.MatchIntl.IsValid())
        {
            return false;
        }

        if(t.EnableAt == null || t.DisableAt == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks whether element is free of fields set to null.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static bool IsValid(this Element e)
    {
        if(e == null)
        {
            return false;
        }

        if(e.Name == null || !e.InternationalName.IsValid())
        {
            return false;
        }

        if(e.overlayText == null || !e.overlayTextIntl.IsValid())
        {
            return false;
        }

        if(e.refActorName == null || !e.refActorNameIntl.IsValid())
        {
            return false;
        }

        if(e.refActorPlaceholder == null || e.refActorPlaceholder.Contains(null))
        {
            return false;
        }

        if(e.refActorCastId == null)
        {
            return false;
        }

        if(e.refActorBuffId == null)
        {
            return false;
        }

        if(e.refActorTetherConnectedWithPlayer == null || e.refActorTetherConnectedWithPlayer.Contains(null))
        {
            return false;
        }

        if(e.faceplayer == null)
        {
            return false;
        }

        if(e.RotationOverridePoint == null)
        {
            return false;
        }

        if(e.ObjectKinds == null)
        {
            return false;
        }

        return true;
    }

    public static bool IsValid(this InternationalString s)
    {
        if(s == null)
        {
            return false;
        }

        if(s.En == null || s.Fr == null || s.Other == null || s.Jp == null || s.De == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="conf">Can pass null</param>
    /// <param name="layout"></param>
    /// <returns></returns>
    public static string GetName(this LayoutSubconfiguration conf, Layout layout)
    {
        var name = conf?.Name ?? layout.DefaultConfigurationName.NullWhenEmpty() ?? "Default Configration";
        if(name == "")
        {
            var index = layout.Subconfigurations.IndexOf(conf);
            name = $"Unnamed configuration {(index == -1 ? conf.Guid : index + 1)}";
        }
        return name;
    }

    private static bool IsNullOrEmpty(this string s) => GenericHelpers.IsNullOrEmpty(s);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="e"></param>
    /// <returns>Radians</returns>
    public static float GetRotationWithOverride(this IGameObject obj, Layout l, Element e)
    {
        if(!e.RotationOverride)
        {
            if(e.UseCastRotation && obj.IsBattleChara(out var b))
            {
                if(b.IsCasting() && b.CastInfo.ActionId.EqualsAny(e.refActorCastId))
                {
                    if(S.Projection.LastCast.TryGetValue(obj.ObjectId, out var casts) && casts.TryGetValue(new(ActionType.Action, b.CastInfo.ActionId), out var packet))
                    {
                        return packet.Rotation;
                    }
                }
                else
                {
                    foreach(var castId in e.refActorCastId)
                    {
                        if(S.Projection.LastCast.TryGetValue(obj.ObjectId, out var casts))
                        {
                            if(casts.TryGetValue(new(ActionType.Action, castId), out var packet))
                            {
                                if(AttachedInfo.TryGetCastTime(b.Address, castId, out var castTime))
                                {
                                    if(castTime.InRange(e.refActorCastTimeMin, e.refActorCastTimeMax))
                                    {
                                        return packet.Rotation;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return obj.Rotation;
        }
        if(e.RotationOverrideAngleOnlyMode)
        {
            return (180f + e.RotationOverrideAddAngle).DegToRad();
        }
        Vector2 position;
        if(e.RotationOverrideFaceMode)
        {
            var pos = CommonRenderUtils.GetPointsFromPlaceholderList(l, obj, e.RotationOverrideFaceModePlaceholders);
            if(pos.Count == 0)
            {
                position = obj.Position.ToVector2();
            }
            else if(pos.Count == 1)
            {
                position = pos[0].ToVector2();
            }
            else
            {
                position = Vector2.Zero;
                foreach(var x in pos)
                {
                    position += x.ToVector2();
                }
                position /= pos.Count;
            }
        }
        else
        {
            position = e.RotationOverridePoint.ToVector2();
        }
        return (180f - MathHelper.GetRelativeAngle(obj.Position.ToVector2(), position) + e.RotationOverrideAddAngle).DegToRad();
    }

    public static void Migrate(this Layout l)
    {
        DataMigrator.MigrateJobs(l);
#pragma warning disable CS0612 // Type or member is obsolete
        foreach(var x in l.Elements)
        {
            x.Value.Name = x.Key;
            l.ElementsL.Add(x.Value);
        }
        l.Elements.Clear();
#pragma warning restore CS0612 // Type or member is obsolete
    }

    public static bool IsLinux()
    {
        //return true;
        return Util.GetHostPlatform().EqualsAny(OSPlatform.Linux, OSPlatform.OSX);
    }

    public static string FancySymbols(this string n)
    {
        return n.ToString().ReplaceByChar("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ", "");
    }

    public static bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos)
    {
        //return S.VbmCamera.WorldToScreen(worldPos, out screenPos);
        /*worldPos = default;
        screenPos = default;
        var cam = CameraManager.Instance();
        if(cam != null)
        {
            var current = cam->CurrentCamera;
            if(current != null)
            {
                var result = current->WorldToScreen(worldPos, out var screenPosCs);
                screenPos = screenPosCs;
                return result;
            }
        }
        return false;*/
        return Svc.GameGui.WorldToScreen(worldPos, out screenPos);
    }

    public static byte[] BrotliCompress(byte[] bytes)
    {
        using var memoryStream = new MemoryStream();
        using(var brotliStream = new BrotliStream(memoryStream, CompressionLevel.SmallestSize))
        {
            brotliStream.Write(bytes, 0, bytes.Length);
        }
        return memoryStream.ToArray();
    }
    public static byte[] BrotliDecompress(byte[] bytes)
    {
        using var memoryStream = new MemoryStream(bytes);
        using var outputStream = new MemoryStream();
        using(var decompressStream = new BrotliStream(memoryStream, CompressionMode.Decompress))
        {
            decompressStream.CopyTo(outputStream);
        }
        return outputStream.ToArray();
    }

    public static string GetScriptConfigurationName(string scriptFullName, string configKey)
    {
        if(P.Config.ScriptConfigurationNames.TryGetValue(scriptFullName, out var d))
        {
            if(d.TryGetValue(configKey, out var name))
            {
                return name;
            }
        }
        return null;
    }

    public static uint[] BlacklistedMessages = new uint[] { 4777, 4139, 4398, 2091, 2218, 2350, 4397, 2224, 4270, 4269, 2729, 4400, 10537, 10409, 10543, 2222, 4401, 2874, 4905, 12585, 4783, 4140 };

    public static string[] BlacklistedVFX = new string[]
    {
        "vfx/common/eff/dk04ht_canc0h.avfx",
        "vfx/common/eff/dk02ht_totu0y.avfx",
        "vfx/common/eff/dk05th_stup0t.avfx",
        "vfx/common/eff/dk10ht_wra0c.avfx",
        "vfx/common/eff/cmat_ligct0c.avfx",
        "vfx/common/eff/dk07ht_da00c.avfx",
        "vfx/common/eff/cmat_icect0c.avfx",
        "vfx/common/eff/dk10ht_ice2c.avfx",
        "vfx/common/eff/combo_001f.avfx",
        "vfx/common/eff/dk02ht_da00c.avfx",
        "vfx/common/eff/dk06gd_par0h.avfx",
        "vfx/common/eff/dk04ht_fir0h.avfx",
        "vfx/common/eff/dk05th_stdn0t.avfx",
        "vfx/common/eff/dk06mg_mab0h.avfx",
        "vfx/common/eff/mgc_2kt001c1t.avfx",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
    };

    public static uint TransformAlpha(uint sourceColor, float? fillIntensity)
    {
        var alpha = Math.Clamp(fillIntensity ?? DefaultFillIntensity, 0f, 1f);
        var colPtr = (byte*)&sourceColor;
        colPtr[3] = (byte)((float)colPtr[3] * alpha);
        return sourceColor;
    }

    public const float DefaultFillIntensity = 0.5f;

    public static byte[][] Separate(byte[] source, byte[] separator)
    {
        var Parts = new List<byte[]>();
        var Index = 0;
        byte[] Part;
        for(var I = 0; I < source.Length; ++I)
        {
            if(Equals(source, separator, I))
            {
                Part = new byte[I - Index];
                Array.Copy(source, Index, Part, 0, Part.Length);
                Parts.Add(Part);
                Index = I + separator.Length;
                I += separator.Length - 1;
            }
        }
        Part = new byte[source.Length - Index];
        Array.Copy(source, Index, Part, 0, Part.Length);
        Parts.Add(Part);
        return Parts.ToArray();

        static bool Equals(byte[] source, byte[] separator, int index)
        {
            for(var i = 0; i < separator.Length; ++i)
            {
                if(index + i >= source.Length || source[index + i] != separator[i])
                {
                    return false;
                }
            }

            return true;
        }
    }

    public static bool IsCastInRange(this IBattleChara c, float min, float max)
    {
        if(c.CastInfo.CurrentCastTime.InRange(min, max))
        {
            return true;
        }
        return false;
    }

    public static bool IsInRange(this IStatus buff, float min, float max)
    {
        if(buff.RemainingTime.InRange(min, max))
        {
            return true;
        }
        return false;
    }

    public static string SanitizeName(this string s)
    {
        return s.Replace(",", "_").Replace("~", "_");
    }

    public static List<Layout> ImportLayouts(string ss, bool silent = false)
    {
        var layouts = new List<Layout>();
        var strings = ss.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach(var str in strings)
        {
            try
            {
                if(str.StartsWith("~Lv2~"))
                {
                    var s = str[5..];
                    var l = JsonConvert.DeserializeObject<Layout>(s);
                    l.Name = l.Name.SanitizeName();
                    var lname = l.Name;
                    if(P.Config.LayoutsL.Any(x => x.Name == lname) && !ImGui.GetIO().KeyCtrl)
                    {
                        throw new Exception("Error: this name already exists.\nTo override, hold CTRL.");
                    }
                    P.Config.LayoutsL.Add(l);
                    CGui.ScrollTo = l;
                    if(!silent)
                    {
                        Notify.Success($"Layout version 2\n{l.GetDisplayName()}");
                    }

                    layouts.Add(l);
                }
                else
                {
                    if(!silent)
                    {
                        Notify.Info("Attempting to perform legacy import");
                    }

                    var l = DeserializeLegacyLayout(str);
                    P.Config.LayoutsL.Add(l);
                    CGui.ScrollTo = l;
                    layouts.Add(l);
                }
            }
            catch(Exception e)
            {
                if(!silent)
                {
                    Notify.Error($"Error parsing layout: {e.Message}");
                }
            }
        }
        if(layouts.Count > 0)
        {
            Notify.Success($"Imported {layouts.Count} layouts");
        }
        else
        {
            Notify.Error($"No layouts detected in clipboard");
        }
        return layouts;
    }

    public static Layout DeserializeLegacyLayout(string import)
    {
        if(import.Contains('~'))
        {
            var name = import.Split('~')[0];
            var json = import[(name.Length + 1)..];
            try
            {
                json = Encoding.UTF8.GetString(Convert.FromBase64String(json));
                Notify.Info("Import type: Base64");
            }
            catch(Exception)
            {
                Notify.Info("Import type: JSON");
            }
            if(P.Config.LayoutsL.Any(x => x.Name == name) && !ImGui.GetIO().KeyCtrl)
            {
                throw new Exception("Error: this name already exists.\nTo override, hold CTRL.");
            }
            else if(name.Length == 0 && !ImGui.GetIO().KeyCtrl)
            {
                throw new Exception("Error: name not present.\nTo override, hold CTRL.");
            }
            else if(name.Contains(","))
            {
                throw new Exception("Name can't contain reserved characters: ,");
            }
            else
            {
                var layout = JsonConvert.DeserializeObject<Layout>(json);
                layout.Name = name;
                layout.Migrate();
                return layout;
            }
        }
        else
        {
            Notify.Info("Import type: Legacy/Paisley Park/Waymark preset plugin");
            var lp = JsonConvert.DeserializeObject<LegacyPreset>(import);
            if(lp.Name == null || lp.Name == "")
            {
                lp.Name = DateTimeOffset.Now.ToLocalTime().ToString().Replace(",", ".");
            }

            if(lp.A == null && lp.B == null && lp.C == null && lp.D == null &&
                lp.One == null && lp.Two == null && lp.Three == null && lp.Four == null)
            {
                throw new Exception("Error importing: invalid data");
            }
            else if(P.Config.LayoutsL.Any(x => x.Name == "Legacy preset: " + lp.Name))
            {
                throw new Exception("Error: this name already exists");
            }
            else if(lp.Name.Contains(",") || lp.Name.Contains("~"))
            {
                throw new Exception("Name can't contain reserved characters: , and ~");
            }
            else
            {
                static void AddLegacyElement(Layout layout, string name, Element element)
                {
                    element.Name = name;
                    layout.ElementsL.Add(element);
                }
                Layout l = new()
                {
                    ZoneLockH = [(ushort)Svc.ClientState.TerritoryType],
                    Name = "Legacy preset: " + lp.Name
                };
                if(lp.A != null && lp.A.Active)
                {
                    AddLegacyElement(l, "A", lp.A.ToElement("A", 0xff00ff00));
                }

                if(lp.B != null && lp.B.Active)
                {
                    AddLegacyElement(l, "B", lp.B.ToElement("B", 0xff00ffff));
                }

                if(lp.C != null && lp.C.Active)
                {
                    AddLegacyElement(l, "C", lp.C.ToElement("C", 0xffffff00));
                }

                if(lp.D != null && lp.D.Active)
                {
                    AddLegacyElement(l, "D", lp.D.ToElement("D", 0xffff00ff));
                }

                if(lp.One != null && lp.One.Active)
                {
                    AddLegacyElement(l, "1", lp.One.ToElement("1", 0xff00ff00));
                }

                if(lp.Two != null && lp.Two.Active)
                {
                    AddLegacyElement(l, "2", lp.Two.ToElement("2", 0xff00ffff));
                }

                if(lp.Three != null && lp.Three.Active)
                {
                    AddLegacyElement(l, "3", lp.Three.ToElement("3", 0xffffff00));
                }

                if(lp.Four != null && lp.Four.Active)
                {
                    AddLegacyElement(l, "4", lp.Four.ToElement("4", 0xffff00ff));
                }

                return l;
            }
        }
    }

    public static void ExportToClipboard(this Layout l)
    {
        ImGui.SetClipboardText(l.Serialize());
        Notify.Success($"{l.GetDisplayName()} copied to clipboard.");
    }

    public static string Serialize(this Layout l)
    {
        return "~Lv2~" + JsonConvert.SerializeObject(l, Formatting.None, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
    }

    public static void ExportToClipboard(this Element l)
    {
        ImGui.SetClipboardText(l.Serialize());
        Notify.Success($"{l.GetName()} copied to clipboard.");
    }

    public static string Serialize(this Element l)
    {
        return JsonConvert.SerializeObject(l, Formatting.None, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
    }

    public static string GetDisplayName(this Layout l)
    {
        return $"{(l.Nodraw ? "Ø" : "")}{l.GetName()}";
    }

    public static bool ShouldSkipDraw(this Element e, Layout l)
    {
        if(l != null)
        {
            return l.Nodraw || e.Nodraw;
        }
        return e.Nodraw;
    }

    public static string GetName(this Layout l)
    {
        if(l.InternationalName.Get(l.Name).IsNullOrEmpty())
        {
            var index = P.Config.LayoutsL.IndexOf(l);
            if(index >= 0)
            {
                return $"Unnamed layout {index}";
            }
            else
            {
                return $"Unnamed layout {l.GUID}";
            }
        }
        else
        {
            return l.InternationalName.Get(l.Name);
        }
    }

    public static string GetName(this Element e, Layout owner = null)
    {
        if(e.InternationalName.Get(e.Name).IsNullOrEmpty())
        {
            owner ??= P.Config.LayoutsL.FirstOrDefault(x => x.GetElementsWithSubconfiguration().Contains(e));
            if(owner != null)
            {
                var index = owner.GetElementsWithSubconfiguration().IndexOf(e);
                if(index >= 0)
                {
                    return $"Unnamed element {index}";
                }
            }
            return $"Unnamed element {e.GUID}";
        }
        else
        {
            return e.InternationalName.Get(e.Name);
        }
    }

    public static IPlayerCharacter GetRolePlaceholder(CombatRole role, int num)
    {
        var curIndex = 1;
        for(var i = 1; i <= 8; i++)
        {
            var result = (nint)FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule()->GetPronounModule()->ResolvePlaceholder($"<{i}>", 0, 0);
            if(result == nint.Zero)
            {
                return null;
            }

            var go = Svc.Objects.CreateObjectReference(result);
            if(go is IPlayerCharacter pc)
            {
                if(pc.GetRole() == role)
                {
                    if(num == curIndex)
                    {
                        return pc;
                    }
                    curIndex++;
                }
            }
        }
        return null;
    }

    public static string Format(this ushort num)
    {
        return P.Config.Hexadecimal ? $"0x{num:X}" : $"{num}";
    }

    public static string Format(this uint num)
    {
        return P.Config.Hexadecimal ? $"0x{num:X}" : $"{num}";
    }

    public static string Format(this ulong num)
    {
        return P.Config.Hexadecimal ? $"0x{num:X}" : $"{num}";
    }

    public static string Format(this int num)
    {
        return P.Config.Hexadecimal ? $"0x{num:X}" : $"{num}";
    }

    public static string Format(this long num)
    {
        return P.Config.Hexadecimal ? $"0x{num:X}" : $"{num}";
    }

    public static float GetAdditionalRotation(this Element e, float cx, float cy, float angle)
    {
        if(!e.FaceMe)
        {
            return e.AdditionalRotation + angle;
        }

        return (e.AdditionalRotation.RadiansToDegrees() + MathHelper.GetRelativeAngle(new Vector2(cx, cy), BasePlayer.Position.ToVector2())).DegreesToRadians();
    }

    public static bool StartsWithIgnoreCase(this string a, string b)
    {
        return a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
    }

    public static bool ContainsIgnoreCase(this string a, string b)
    {
        return a.Contains(b, StringComparison.OrdinalIgnoreCase);
    }

    public static string Compress(this string s)
    {
        var bytes = Encoding.Unicode.GetBytes(s);
        using var msi = new MemoryStream(bytes);
        using var mso = new MemoryStream();
        using(var gs = new GZipStream(mso, CompressionLevel.Optimal))
        {
            msi.CopyTo(gs);
        }
        return Convert.ToBase64String(mso.ToArray()).Replace('+', '-').Replace('/', '_');
    }

    public static string ToBase64UrlSafe(this string s)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).Replace('+', '-').Replace('/', '_');
    }

    public static string FromBase64UrlSafe(this string s)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/')));
    }

    public static string Decompress(this string s)
    {
        var bytes = Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/'));
        using var msi = new MemoryStream(bytes);
        using var mso = new MemoryStream();
        using(var gs = new GZipStream(msi, CompressionMode.Decompress))
        {
            gs.CopyTo(mso);
        }
        return Encoding.Unicode.GetString(mso.ToArray());
    }

    //because Dalamud changed Y and Z in actor positions I have to do emulate old behavior to not break old presets
    public static Vector3 GetPlayerPositionXZY()
    {
        if(BasePlayer != null)
        {
            if(PlayerPosCache == null)
            {
                PlayerPosCache = XZY(BasePlayer.Position);
            }
            return PlayerPosCache.Value;
        }
        return Vector3.Zero;
    }

    public static Vector3 GetPositionXZY(this IGameObject a)
    {
        return XZY(a.Position);
    }

    /// <summary>
    /// Swaps Y and Z coordinates in vector
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static Vector3 XZY(Vector3 point)
    {
        return new Vector3(point.X, point.Z, point.Y);
    }

    public static void ProcessStart(string s)
    {
        try
        {
            Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = s
            });
        }
        catch(Exception e)
        {
            Svc.Chat.Print("Error: " + e.ToStringFull());
        }
    }

    public static string NotNull(this string s)
    {
        return s ?? "";
    }
    public static float AngleBetweenVectors(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
    {
        return MathF.Acos((((x2 - x1) * (x4 - x3)) + ((y2 - y1) * (y4 - y3))) /
            (MathF.Sqrt(Square(x2 - x1) + Square(y2 - y1)) * MathF.Sqrt(Square(x4 - x3) + Square(y4 - y3))));
    }

    public static IEnumerable<(Vector2 v2, float angle)> GetPolygon(List<Vector2> coords)
    {
        var medium = new Vector2(coords.Average(x => x.X), coords.Average(x => x.Y));
        var array = coords.Select(x => x - medium).ToArray();
        Array.Sort(array, delegate (Vector2 a, Vector2 b)
        {
            var angleA = MathF.Atan2(a.Y, a.X);
            var angleB = MathF.Atan2(b.Y, b.X);
            if(angleA == angleB)
            {
                var radiusA = MathF.Sqrt((a.X * a.X) + (a.Y * a.Y));
                var radiusB = MathF.Sqrt((b.X * b.X) + (b.Y * b.Y));
                return radiusA > radiusB ? 1 : -1;
            }
            return angleA > angleB ? 1 : -1;
        });
        foreach(var x in array)
        {
            yield return (x + medium, MathF.Atan2(x.Y, x.X));
        }
    }

    public static float Square(float x)
    {
        return x * x;
    }

    public static float RadToDeg(float radian)
    {
        return radian * (180 / MathF.PI);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="origin">XZY</param>
    /// <param name="angle"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static Vector3 RotatePoint(Vector3 origin, float angle, Vector3 point)
    {
        if(angle == 0f)
        {
            return point;
        }

        var s = (float)Math.Sin(angle);
        var c = (float)Math.Cos(angle);

        // translate point back to origin:
        point -= origin;

        // rotate point
        var xnew = (point.X * c) - (point.Y * s);
        var ynew = (point.X * s) + (point.Y * c);
        point.X = xnew;
        point.Y = ynew;

        // translate point back:
        point += origin;
        return point;
    }

    /// <summary>
    /// Accepts: Z-height vector. Returns: Z-height vector.
    /// </summary>
    /// <param name="cx"></param>
    /// <param name="cy"></param>
    /// <param name="angle"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public static Vector3 RotatePoint(float cx, float cy, float angle, Vector3 p)
    {
        if(angle == 0f)
        {
            return p;
        }

        var s = (float)Math.Sin(angle);
        var c = (float)Math.Cos(angle);

        // translate point back to origin:
        p.X -= cx;
        p.Y -= cy;

        // rotate point
        var xnew = (p.X * c) - (p.Y * s);
        var ynew = (p.X * s) + (p.Y * c);

        // translate point back:
        p.X = xnew + cx;
        p.Y = ynew + cy;
        return p;
    }

    // Calculate the distance between
    // point pt and the segment p1 --> p2.
    public static Vector3 FindClosestPointOnLine(Vector3 P, Vector3 A, Vector3 B)
    {
        var D = Vector3.Normalize(B - A);
        var d = Vector3.Dot(P - A, D);
        return A + Vector3.Multiply(D, d);
    }

    public static float DegreesToRadians(this int val)
    {
        return (float)(Math.PI / 180f * val);
    }

    public static float DegreesToRadians(this float val)
    {
        return (float)(Math.PI / 180 * val);
    }
    public static float RadiansToDegrees(this float radians)
    {
        return (float)(180 / Math.PI * radians);
    }
    public static Vector4 Column1(this Matrix4x4 value)
    {
        return new Vector4(value.M11, value.M21, value.M31, value.M41);
    }
    public static Vector4 Column2(this Matrix4x4 value)
    {
        return new Vector4(value.M12, value.M22, value.M32, value.M42);
    }
    public static Vector4 Column3(this Matrix4x4 value)
    {
        return new Vector4(value.M13, value.M23, value.M33, value.M43);
    }
    public static Vector4 Column4(this Matrix4x4 value)
    {
        return new Vector4(value.M14, value.M24, value.M34, value.M44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TransformCoordinate(in Vector3 coordinate, in Matrix4x4 transform, out Vector3 result)
    {
        result.X = (coordinate.X * transform.M11) + (coordinate.Y * transform.M21) + (coordinate.Z * transform.M31) + transform.M41;
        result.Y = (coordinate.X * transform.M12) + (coordinate.Y * transform.M22) + (coordinate.Z * transform.M32) + transform.M42;
        result.Z = (coordinate.X * transform.M13) + (coordinate.Y * transform.M23) + (coordinate.Z * transform.M33) + transform.M43;
        var w = 1f / ((coordinate.X * transform.M14) + (coordinate.Y * transform.M24) + (coordinate.Z * transform.M34) + transform.M44);
        result *= w;
    }

    public static string RemoveSymbols(this string s, IEnumerable<string> deletions)
    {
        foreach(var r in deletions)
        {
            s = s.Replace(r, "");
        }

        return s;
    }

    public static void RemoveSymbols(this InternationalString s, IEnumerable<string> deletions)
    {
        foreach(var r in deletions)
        {
            s.En = s.En.Replace(r, "");
            s.Jp = s.Jp.Replace(r, "");
            s.De = s.De.Replace(r, "");
            s.Fr = s.Fr.Replace(r, "");
            s.Other = s.Other.Replace(r, "");
        }
    }

    /// <summary>
    /// Create a perpendicular offset point at a position located along a line segment.
    /// </summary>
    /// <param name="a">Input. PointD(x,y) of p1.</param>
    /// <param name="b">Input. PointD(x,y) of p2.</param>
    /// <param name="position">Distance between p1(0.0) and p2 (1.0) in a percentage.</param>
    /// <param name="offset">Distance from position at 90degrees to p1 and p2- non-percetange based.</param>
    /// <param name="c">Output of the calculated point along p1 and p2. might not be necessary for the ultimate output.</param>
    /// <param name="d">Output of the calculated offset point.</param>
    public static void PerpOffset(Vector2 a, Vector2 b, float position, float offset, out Vector2 c, out Vector2 d)
    {
        //p3 is located at the x or y delta * position + p1x or p1y original.
        var p3 = new Vector2(((b.X - a.X) * position) + a.X, ((b.Y - a.Y) * position) + a.Y);

        //returns an angle in radians between p1 and p2 + 1.5708 (90degress).
        var angleRadians = MathF.Atan2(a.Y - b.Y, a.X - b.X) + 1.5708f;

        //locates p4 at the given angle and distance from p3.
        var p4 = new Vector2(p3.X + (MathF.Cos(angleRadians) * offset), p3.Y + (MathF.Sin(angleRadians) * offset));

        //send out the calculated points
        c = p3;
        d = p4;
    }
}
