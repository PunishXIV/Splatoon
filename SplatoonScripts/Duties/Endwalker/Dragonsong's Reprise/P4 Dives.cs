using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;
public sealed class P4_Dives : SplatoonScript
{
    public override Metadata Metadata { get; } = new(2, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [Raids.Dragonsongs_Reprise_Ultimate];

    int DiveCnt;
    bool? IsCWBait = null;
    List<uint> DiveTargets = [];

    IPlayerCharacter BasePlayer
    {
        get
        {
            return Player.Object;
        }
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Hint", """{"Name":"","radius":1.0,"color":3359309568,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
    }

    public override void OnReset()
    {
        DiveCnt = 0;
        IsCWBait = null;
        DiveTargets.Clear();
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    /// <summary>
    /// Sorts a list of objects with Position property clockwise relative to center point (90, 100).
    /// Accounts for inverted Y-axis where higher Y values are visually lower.
    /// </summary>
    /// <typeparam name="T">Type that has a Position property</typeparam>
    /// <param name="points">List of 4 points to sort</param>
    /// <returns>New list sorted clockwise starting from the top (12 o'clock position)</returns>
    public static List<T> SortClockwise<T>(List<T> points) where T : IGameObject
    {
        const double centerX = 90.0;
        const double centerY = 100.0;

        if(points == null || points.Count != 4)
            throw new ArgumentException("Exactly 4 points must be provided");

        return points
            .Select(point => new { Point = point, Angle = GetClockwiseAngle(point, centerX, centerY) })
            .OrderByDescending(item => item.Angle)
            .Select(item => item.Point)
            .ToList();
    }

    /// <summary>
    /// Calculates the clockwise angle from the center point, accounting for inverted Y-axis.
    /// Returns angle in degrees where 0° is at 12 o'clock (top), increasing clockwise.
    /// </summary>
    private static double GetClockwiseAngle<T>(T point, double centerX, double centerY) where T : IGameObject
    {
        // Get Position property using reflection
        var position = point.Position.ToVector2();

        // Extract X and Y coordinates (assuming Position has X and Y properties)
        double x = (double)position.X;
        double y = (double)position.Y;

        // Calculate relative position from center
        double deltaX = x - centerX;
        double deltaY = y - centerY;

        // Since Y-axis is inverted, we need to flip deltaY for correct angle calculation
        // In normal coordinates: positive Y is up, negative Y is down
        // In inverted coordinates: positive Y is down, negative Y is up
        // So we negate deltaY to convert to normal coordinate system
        deltaY = -deltaY;

        // Calculate angle using atan2 (returns angle in radians from -π to π)
        // atan2(deltaY, deltaX) gives angle from positive X-axis (3 o'clock position)
        double angleRadians = Math.Atan2(deltaY, deltaX);

        // Convert to degrees
        double angleDegrees = angleRadians * (180.0 / Math.PI);

        // atan2 returns angle from 3 o'clock position (positive X-axis)
        // We want 0° to be at 12 o'clock (top), so we need to rotate by -90°
        angleDegrees -= 90.0;

        // Normalize angle to 0-360° range
        if(angleDegrees < 0)
            angleDegrees += 360.0;

        return angleDegrees;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Source is not IPlayerCharacter && set.Action != null)
        {
            //PluginLog.Information($"Action: {ExcelActionHelper.GetActionName(set.Action.Value.RowId, true)} target: {set.TargetEffects.Select(x => ((uint)x.TargetID).GetObject()).Print()}");

            if(set.Action?.RowId == 26820)
            {
                DiveCnt++;
                this.DiveTargets.Add(set.TargetEffects.Select(x => (uint)x.TargetID).ToArray());
                if(DiveTargets.Count == 2)
                {
                    if(DiveTargets.Contains(BasePlayer.EntityId))
                    {
                        var players = Controller.GetPartyMembers().Where(x => x.StatusList.Any(s => s.StatusId == 2775)).ToList();
                        var orderedPlayers = SortClockwise(players).Where(x => x.EntityId.EqualsAny(DiveTargets));
                        PluginLog.Information($"Ordered players: {orderedPlayers.Print()}");
                        IsCWBait = orderedPlayers.First().AddressEquals(BasePlayer);
                        PluginLog.Information($"Determined 3rd bait: {(IsCWBait == true ? "Clockwise" : "CounterClockwise")}");
                    }
                }
                if(DiveTargets.Count == 6 && IsCWBait != null)
                {
                    var players = Controller.GetPartyMembers().Where(x => x.StatusList.Any(s => s.StatusId == 2775)).ToList();
                    var orderedPlayers = SortClockwise(players).Where(x => x.EntityId.EqualsAny(DiveTargets[4..6])).ToList();
                    PluginLog.Information($"Ordered players: {orderedPlayers.Print()}");
                    Controller.GetElementByName("Hint").SetRefPosition(orderedPlayers[IsCWBait.Value ? 0 : 1].Position);
                    Controller.GetElementByName("Hint").Enabled = true;
                    Controller.Schedule(() => Controller.GetElementByName("Hint").Enabled = false, 5000);
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        var players = Controller.GetPartyMembers().Where(x => x.StatusList.Any(s => s.StatusId == 2775)).ToList();
        var orderedPlayers = SortClockwise(players);
        ImGuiEx.Text($"{orderedPlayers.Print("\n")}");
    }
}