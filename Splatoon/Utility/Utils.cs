using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Statuses;
using ECommons.GameFunctions;
using ECommons.MathHelpers;
using Newtonsoft.Json;
using Splatoon.RenderEngines;
using Splatoon.Structures;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Splatoon.Utility;

public static unsafe class Utils
{
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
        for (var I = 0; I < source.Length; ++I)
        {
            if (Equals(source, separator, I))
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
            for (int i = 0; i < separator.Length; ++i)
                if (index + i >= source.Length || source[index + i] != separator[i])
                    return false;
            return true;
        }
    }

    public static bool IsCastInRange(this IBattleChara c, float min, float max)
    {
        if (c.CurrentCastTime.InRange(min, max))
        {
            return true;
        }
        return false;
    }

    public static bool IsInRange(this Status buff, float min, float max)
    {
        if (buff.RemainingTime.InRange(min, max))
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
        foreach (var str in strings)
        {
            try
            {
                if (str.StartsWith("~Lv2~"))
                {
                    var s = str[5..];
                    var l = JsonConvert.DeserializeObject<Layout>(s);
                    l.Name = l.Name.SanitizeName();
                    var lname = l.Name;
                    if (P.Config.LayoutsL.Any(x => x.Name == lname) && !ImGui.GetIO().KeyCtrl)
                    {
                        throw new Exception("Error: this name already exists.\nTo override, hold CTRL.");
                    }
                    P.Config.LayoutsL.Add(l);
                    CGui.ScrollTo = l;
                    if (!silent) Notify.Success($"Layout version 2\n{l.GetName()}");
                    layouts.Add(l);
                }
                else
                {
                    if (!silent) Notify.Info("Attempting to perform legacy import");
                    var l = DeserializeLegacyLayout(str);
                    P.Config.LayoutsL.Add(l);
                    CGui.ScrollTo = l;
                    layouts.Add(l);
                }
            }
            catch (Exception e)
            {
                if (!silent) Notify.Error($"Error parsing layout: {e.Message}");
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
        if (import.Contains('~'))
        {
            var name = import.Split('~')[0];
            var json = import.Substring(name.Length + 1);
            try
            {
                json = Encoding.UTF8.GetString(Convert.FromBase64String(json));
                Notify.Info("Import type: Base64");
            }
            catch (Exception)
            {
                Notify.Info("Import type: JSON");
            }
            if (P.Config.LayoutsL.Any(x => x.Name == name) && !ImGui.GetIO().KeyCtrl)
            {
                throw new Exception("Error: this name already exists.\nTo override, hold CTRL.");
            }
            else if (name.Length == 0 && !ImGui.GetIO().KeyCtrl)
            {
                throw new Exception("Error: name not present.\nTo override, hold CTRL.");
            }
            else if (name.Contains(","))
            {
                throw new Exception("Name can't contain reserved characters: ,");
            }
            else
            {
                var layout = JsonConvert.DeserializeObject<Layout>(json);
                layout.Name = name;
#pragma warning disable CS0612 // Type or member is obsolete
                foreach (var x in layout.Elements)
                {
                    x.Value.Name = x.Key;
                    layout.ElementsL.Add(x.Value);
                }
                layout.Elements.Clear();
#pragma warning restore CS0612 // Type or member is obsolete
                return layout;
            }
        }
        else
        {
            Notify.Info("Import type: Legacy/Paisley Park/Waymark preset plugin");
            var lp = JsonConvert.DeserializeObject<LegacyPreset>(import);
            if (lp.Name == null || lp.Name == "") lp.Name = DateTimeOffset.Now.ToLocalTime().ToString().Replace(",", ".");
            if (lp.A == null && lp.B == null && lp.C == null && lp.D == null &&
                lp.One == null && lp.Two == null && lp.Three == null && lp.Four == null)
            {
                throw new Exception("Error importing: invalid data");
            }
            else if (P.Config.LayoutsL.Any(x => x.Name == "Legacy preset: " + lp.Name))
            {
                throw new Exception("Error: this name already exists");
            }
            else if (lp.Name.Contains(",") || lp.Name.Contains("~"))
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
                    ZoneLockH = new HashSet<ushort>() { Svc.ClientState.TerritoryType },
                    Name = "Legacy preset: " + lp.Name
                };
                if (lp.A != null && lp.A.Active) AddLegacyElement(l, "A", lp.A.ToElement("A", 0xff00ff00));
                if (lp.B != null && lp.B.Active) AddLegacyElement(l, "B", lp.B.ToElement("B", 0xff00ffff));
                if (lp.C != null && lp.C.Active) AddLegacyElement(l, "C", lp.C.ToElement("C", 0xffffff00));
                if (lp.D != null && lp.D.Active) AddLegacyElement(l, "D", lp.D.ToElement("D", 0xffff00ff));
                if (lp.One != null && lp.One.Active) AddLegacyElement(l, "1", lp.One.ToElement("1", 0xff00ff00));
                if (lp.Two != null && lp.Two.Active) AddLegacyElement(l, "2", lp.Two.ToElement("2", 0xff00ffff));
                if (lp.Three != null && lp.Three.Active) AddLegacyElement(l, "3", lp.Three.ToElement("3", 0xffffff00));
                if (lp.Four != null && lp.Four.Active) AddLegacyElement(l, "4", lp.Four.ToElement("4", 0xffff00ff));
                return l;
            }
        }
    }

    public static void ExportToClipboard(this Layout l)
    {
        ImGui.SetClipboardText(l.Serialize());
        Notify.Success($"{l.GetName()} copied to clipboard.");
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
        return "~Ev2~" + JsonConvert.SerializeObject(l, Formatting.None, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
    }

    public static string GetName(this Layout l)
    {
        if (l.Name.IsNullOrEmpty())
        {
            var index = P.Config.LayoutsL.IndexOf(l);
            if (index >= 0)
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
            return l.Name;
        }
    }

    public static string GetName(this Element e)
    {
        if (e.Name.IsNullOrEmpty())
        {
            if (P.Config.LayoutsL.TryGetFirst(x => x.ElementsL.Contains(e), out var l))
            {
                var index = l.ElementsL.IndexOf(e);
                if (index >= 0)
                {
                    return $"Unnamed element {index}";
                }
            }
            return $"Unnamed element {e.GUID}";
        }
        else
        {
            return e.Name;
        }
    }

    public static IPlayerCharacter GetRolePlaceholder(CombatRole role, int num)
    {
        int curIndex = 1;
        for (var i = 1; i <= 8; i++)
        {
            var result = (nint)FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule()->GetPronounModule()->ResolvePlaceholder($"<{i}>", 0, 0);
            if (result == nint.Zero) return null;
            var go = Svc.Objects.CreateObjectReference(result);
            if (go is IPlayerCharacter pc)
            {
                if (pc.GetRole() == role)
                {
                    if (num == curIndex)
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
        if (!e.FaceMe) return e.AdditionalRotation + angle;
        return (e.AdditionalRotation.RadiansToDegrees() + MathHelper.GetRelativeAngle(new Vector2(cx, cy), Svc.ClientState.LocalPlayer.Position.ToVector2())).DegreesToRadians();
    }

    public static bool EqualsIgnoreCase(this string a, string b)
    {
        return a.Equals(b, StringComparison.OrdinalIgnoreCase);
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
        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        {
            using (var gs = new GZipStream(mso, CompressionLevel.Optimal))
            {
                msi.CopyTo(gs);
            }
            return Convert.ToBase64String(mso.ToArray()).Replace('+', '-').Replace('/', '_');
        }
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
        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        {
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                gs.CopyTo(mso);
            }
            return Encoding.Unicode.GetString(mso.ToArray());
        }
    }

    //because Dalamud changed Y and Z in actor positions I have to do emulate old behavior to not break old presets
    public static Vector3 GetPlayerPositionXZY()
    {
        if (Svc.ClientState.LocalPlayer != null)
        {
            if (PlayerPosCache == null)
            {
                PlayerPosCache = XZY(Svc.ClientState.LocalPlayer.Position);
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
        catch (Exception e)
        {
            Svc.Chat.Print("Error: " + e.Message + "\n" + e.StackTrace);
        }
    }

    public static string NotNull(this string s)
    {
        return s ?? "";
    }
    public static float AngleBetweenVectors(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
    {
        return MathF.Acos(((x2 - x1) * (x4 - x3) + (y2 - y1) * (y4 - y3)) /
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
            if (angleA == angleB)
            {
                var radiusA = MathF.Sqrt(a.X * a.X + a.Y * a.Y);
                var radiusB = MathF.Sqrt(b.X * b.X + b.Y * b.Y);
                return radiusA > radiusB ? 1 : -1;
            }
            return angleA > angleB ? 1 : -1;
        });
        foreach (var x in array) yield return (x + medium, MathF.Atan2(x.Y, x.X));
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
        if (angle == 0f) return point;
        var s = (float)Math.Sin(angle);
        var c = (float)Math.Cos(angle);

        // translate point back to origin:
        point -= origin;

        // rotate point
        float xnew = point.X * c - point.Y * s;
        float ynew = point.X * s + point.Y * c;
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
        if (angle == 0f) return p;
        var s = (float)Math.Sin(angle);
        var c = (float)Math.Cos(angle);

        // translate point back to origin:
        p.X -= cx;
        p.Y -= cy;

        // rotate point
        float xnew = p.X * c - p.Y * s;
        float ynew = p.X * s + p.Y * c;

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
        foreach (var r in deletions) s = s.Replace(r, "");
        return s;
    }

    public static void RemoveSymbols(this InternationalString s, IEnumerable<string> deletions)
    {
        foreach (var r in deletions)
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
    static public void PerpOffset(Vector2 a, Vector2 b, float position, float offset, out Vector2 c, out Vector2 d)
    {
        //p3 is located at the x or y delta * position + p1x or p1y original.
        var p3 = new Vector2((b.X - a.X) * position + a.X, (b.Y - a.Y) * position + a.Y);

        //returns an angle in radians between p1 and p2 + 1.5708 (90degress).
        var angleRadians = MathF.Atan2(a.Y - b.Y, a.X - b.X) + 1.5708f;

        //locates p4 at the given angle and distance from p3.
        var p4 = new Vector2(p3.X + MathF.Cos(angleRadians) * offset, p3.Y + MathF.Sin(angleRadians) * offset);

        //send out the calculated points
        c = p3;
        d = p4;
    }
}
