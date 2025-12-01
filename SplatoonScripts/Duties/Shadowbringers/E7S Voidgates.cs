using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace SplatoonScriptsOfficial.Duties.Shadowbringers;

public class E7S_Voidgates : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [908];

    Dictionary<uint, (Vector2 Blue, Vector2 Red)> PortalsDestination = new()
    {
        [1] = (new(80, 95), new(80, 85)),
        [2] = (new(80, 115), new(80, 105)),
        [3] = (new(120, 85), new(120, 95)),
        [4] = (new(120, 105), new(120, 115)),
        [5] = (new(95, 80), new(105, 80)),
        [11] = (new(105, 120), new(95, 120)),
        [13] = (new(105,100), new(95, 100)),
    };
    Dictionary<uint, (Vector2 Blue, Vector2 Red)> PortalsSource = new()
    {
        [6] = (new(80, 85), new(80, 95)),
        [7] = (new(80, 105), new(80, 115)),
        [8] = (new(120, 95), new(120, 85)),
        [9] = (new(120, 115), new(120, 105)),
        [10] = (new(95, 120), new(105, 120)),
    };

    Dictionary<uint, bool> ActivePortalDestinations = [];
    Dictionary<uint, bool> ActivePortalSources = [];
    List<bool?> NextAttackRed = [];

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Vertical1", """
            {"Name":"","type":2,"refX":85.0,"refY":80.0,"offX":85.0,"offY":120.0,"radius":5.0}
            """);
        Controller.RegisterElementFromCode("Vertical2", """
            {"Name":"","type":2,"refX":95.0,"refY":80.0,"offX":95.0,"offY":120.0,"radius":5.0}
            """);
        Controller.RegisterElementFromCode("Vertical3", """
            {"Name":"","type":2,"refX":105.0,"refY":80.0,"offX":105.0,"offY":120.0,"radius":5.0}
            """);
        Controller.RegisterElementFromCode("Vertical4", """
            {"Name":"","type":2,"refX":115.0,"refY":80.0,"offX":115.0,"offY":120.0,"radius":5.0}
            """);
        Controller.RegisterElementFromCode("Horizontal1", """
            {"Name":"","type":2,"refX":100.0,"refY":80.0,"offX":100.0,"offY":90.0,"radius":20.0}
            """);
        Controller.RegisterElementFromCode("Horizontal2", """
            {"Name":"","type":2,"refX":100.0,"refY":90.0,"offX":100.0,"offY":100.0,"radius":20.0}
            """);
        Controller.RegisterElementFromCode("Horizontal3", """
            {"Name":"","type":2,"refX":100.0,"refY":100.0,"offX":100.0,"offY":110.0,"radius":20.0}
            """);
        Controller.RegisterElementFromCode("Horizontal4", """
            {"Name":"","type":2,"refX":100.0,"refY":120.0,"offX":100.0,"offY":110.0,"radius":20.0}
            """);
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        {
            if(PortalsSource.TryGetValue(position, out var v))
            {
                if(data1 == 0x1)
                {
                    ActivePortalSources[position] = false;
                }
                else if(data1 == 0x10)
                {
                    ActivePortalSources[position] = true;
                }
                else
                {
                    ActivePortalSources.Remove(position);
                }
            }
        }
        {
            if(PortalsDestination.TryGetValue(position, out var v))
            {
                if(data1 == 0x1)
                {
                    ActivePortalDestinations[position] = false;
                }
                else if(data1 == 0x10)
                {
                    ActivePortalDestinations[position] = true;
                }
                else
                {
                    ActivePortalDestinations.Remove(position);
                }
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Source is IBattleNpc npc)
        {
            if(set.Action?.RowId.EqualsAny(19554u, 19555u, 19556u) == true)
            {
                if(EzThrottler.Throttle("RePortalE7s", 150))
                {
                    float distance = 999f;
                    bool? isRed = null;
                    foreach(var x in ActivePortalDestinations)
                    {
                        var dist = DistancePointToLine(set.Source.Position.ToVector2(), set.Source.Rotation, PortalsDestination[x.Key].Blue);
                        var dist2 = DistancePointToLine(set.Source.Position.ToVector2(), set.Source.Rotation, PortalsDestination[x.Key].Red);
                        if(dist < distance && dist < 0.5f)
                        {
                            distance = dist;
                            isRed = x.Value;
                        }
                        if(dist2 < distance && dist2 < 0.5f)
                        {
                            distance = dist;
                            isRed = !x.Value;
                        }
                    }
                    NextAttackRed.Add(isRed);
                }
            }
        }
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(this.NextAttackRed.Count > 1)
        {
            var attacks = this.NextAttackRed.TakeLast(2).ToArray();
            if(attacks[0] != null)
            {
                foreach(var x in ActivePortalSources)
                {
                    var redPortal = x.Value ? PortalsSource[x.Key].Blue : PortalsSource[x.Key].Red;
                    var bluePortal = x.Value ? PortalsSource[x.Key].Red : PortalsSource[x.Key].Blue;
                    if(Controller.TryGetElementByName(GetLineByCoord(attacks[0].Value?redPortal:bluePortal), out var e))
                    {
                        e.Enabled = true;
                    }
                }
            }
        }
    }

    string GetLineByCoord(Vector2 coord)
    {
        var x = (int)Math.Floor(coord.X);
        var y = (int)Math.Floor(coord.Y);
        if(y == 85) return "Horizontal1";
        if(y == 95) return "Horizontal2";
        if(y == 105) return "Horizontal3";
        if(y == 115) return "Horizontal4";
        if(x == 85) return "Vertical1";
        if(x == 95) return "Vertical2";
        if(x == 105) return "Vertical3";
        if(x == 115) return "Vertical4";
        return null;
    }

    public override void OnReset()
    {
        this.ActivePortalDestinations.Clear();
        this.ActivePortalSources.Clear();
        this.NextAttackRed.Clear();
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"Active source portals:\n{ActivePortalSources.Select(x => $"{x.Key}={x.Value}").Print("\n")}");
        ImGuiEx.Text($"Active destination portals:\n{ActivePortalDestinations.Select(x => $"{x.Key}={x.Value}").Print("\n")}");
        ImGuiEx.Text($"{this.NextAttackRed.Print(" | ")}");
    }

    /// <summary>
    /// Calculates the perpendicular distance from a point to a line defined by an origin and rotation.
    /// </summary>
    /// <param name="lineOrigin">The position where the line starts</param>
    /// <param name="lineRotationRadians">The rotation of the line in radians (0 = north/-Y direction)</param>
    /// <param name="point">The point to measure distance from</param>
    /// <returns>The perpendicular distance from the point to the line</returns>
    public static float DistancePointToLine(Vector2 lineOrigin, float lineRotationRadians, Vector2 point)
    {
        // Build direction vector from rotation
        // In game coords: 0 radians = north (-Y), PI/2 = east (+X), PI = south (+Y), 3PI/2 = west (-X)
        Vector2 lineDirection = new Vector2(
            MathF.Sin(lineRotationRadians),
            -MathF.Cos(lineRotationRadians)
        );

        // Vector from line origin to the point
        Vector2 pointOffset = point - lineOrigin;

        // Project pointOffset onto lineDirection to find the closest point on the line
        float projection = Vector2.Dot(pointOffset, lineDirection);
        Vector2 closestPointOnLine = lineOrigin + lineDirection * projection;

        // Distance is the magnitude of the difference
        return Vector2.Distance(point, closestPointOnLine);
    }

    /// <summary>
    /// Alternative version that also returns the closest point on the line.
    /// </summary>
    public static float DistancePointToLine(Vector2 lineOrigin, float lineRotationRadians, Vector2 point, out Vector2 closestPoint)
    {
        Vector2 lineDirection = new Vector2(
            MathF.Sin(lineRotationRadians),
            -MathF.Cos(lineRotationRadians)
        );

        Vector2 pointOffset = point - lineOrigin;
        float projection = Vector2.Dot(pointOffset, lineDirection);
        closestPoint = lineOrigin + lineDirection * projection;

        return Vector2.Distance(point, closestPoint);
    }
}
