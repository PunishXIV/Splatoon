using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.PartyFinder.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class EX6_Arcane_Revelation : SplatoonScript
{
    public override Metadata Metadata { get; } = new(2, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1308];

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Danger", """{"Name":"","radius":16.0,"fillIntensity":0.5}""");
    }


    public Dictionary<CardinalDirection, Vector2> RawDirections = new()
    {
        [CardinalDirection.North] = new(100f, 240f),
        [CardinalDirection.West] = new(95.17041f, 249.98853f),
        [CardinalDirection.South] = new(100f, 260f),
        [CardinalDirection.East] = new(104.78369f, 249.98853f),
    };

    public Dictionary<CardinalDirection, Vector2> Directions
    {
        get
        {
            if(Svc.Objects.TryGetFirst(x => x.DataId == DataId, out var obj))
            {
                if(Vector2.Distance(obj.Position.ToVector2(), new(100f, 350f)) < 16f)
                {
                    return RawDirections.ToDictionary(x => x.Key, x => x.Value with { Y = x.Value.Y + 100 });
                }
            }
            return RawDirections;
        }
    }

    public uint DataId = 18998;

    public uint Move4 = 45658;
    public uint Move3 = 45657;
    public uint Move2 = 45656;

    public bool DirectionDetermined = false;
    public CardinalDirection CurrentDirection = 0;
    public int NumMoves = 0;

    public override void OnReset()
    {
        NumMoves = 0;
        CurrentDirection = (CardinalDirection)(-1);
        DirectionDetermined = false;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action != null && set.Source is IBattleNpc bnpc && bnpc.Name.ToString() != "Automaton Queen")
        {
            //PluginLog.Information($"Cast from: {bnpc}, {ExcelActionHelper.GetActionName(set.Action?.RowId ?? 0, true)}");
            if(set.Action?.RowId.EqualsAny(47527u, 45713u, 45714u) == true)
            {
                DirectionDetermined = false;
                CurrentDirection = (CardinalDirection)(-1);
                NumMoves = 0;
                PluginLog.Information($"Initial: {CurrentDirection}, {NumMoves}, {DirectionDetermined}");
            }
            if(set.Action?.RowId == Move2)
            {
                NumMoves = 2;
                PluginLog.Information($"2 moves");
            }
            if(set.Action?.RowId == Move3)
            {
                NumMoves = 3;
                PluginLog.Information($"3 moves");
            }
            if(set.Action?.RowId == Move4)
            {
                NumMoves = 4;
                PluginLog.Information($"4 moves");
            }
        }
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(Svc.Objects.TryGetFirst(x => x.DataId == DataId, out var obj))
        {
            if(DirectionDetermined)
            {
                if(Controller.TryGetElementByName("Danger", out var e))
                {
                    e.SetRefPosition(Directions[CurrentDirection].ToVector3());
                    e.Enabled = true;
                }
            }
            if(CurrentDirection == (CardinalDirection)(-1))
            {
                CurrentDirection = GetCurrentDirection();
                if(CurrentDirection != (CardinalDirection)(-1))PluginLog.Information($"Current direction set: {CurrentDirection}, {DirectionDetermined}, {NumMoves}");
            }
            else if(!DirectionDetermined && NumMoves != 0)
            {
                if(GetCurrentDirection() == (CardinalDirection)(-1))
                {
                    CurrentDirection = GetNextDirection();
                    PluginLog.Information($"Determined next direction: {CurrentDirection}, {DirectionDetermined}, {NumMoves}");
                    DirectionDetermined = true;
                    NumMoves = 0;
                }
            }
        }
    }

    public CardinalDirection GetNextDirection()
    {
        CardinalDirection[] cw = [CardinalDirection.North, CardinalDirection.East, CardinalDirection.South, CardinalDirection.West];
        var currentIndex = cw.IndexOf(CurrentDirection);
        var nextCwPoint = cw.CircularSelect(currentIndex + 1);
        var isCw = IsPointGoingTowards(nextCwPoint);
        PluginLog.Information($"Num moves: {(isCw ? this.NumMoves : -this.NumMoves)}");
        return cw.CircularSelect(currentIndex + (isCw ? this.NumMoves : -this.NumMoves));
    }

    public CardinalDirection GetCurrentDirection()
    {
        if(Svc.Objects.TryGetFirst(x => x.DataId == DataId, out var obj))
        {
            foreach(var p in Directions)
            {
                if(Vector2.Distance(p.Value, obj.Position.ToVector2()) < 1)
                {
                    return p.Key;
                }
            }
        }
        return (CardinalDirection)(-1);
    }

    public bool IsPointGoingTowards(CardinalDirection direction)
    {
        return IsPointOnLine(Svc.Objects.First(x => x.DataId == DataId).Position.ToVector2(), Directions[CurrentDirection], Directions[direction]);
    }

    public bool IsPointOnLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd, float tolerance = 0.5f)
    {
        // Calculate the distance from point to the line segment
        Vector2 lineVector = lineEnd - lineStart;
        Vector2 pointVector = point - lineStart;

        // Calculate line length manually
        float lineLength = (float)Math.Sqrt(lineVector.X * lineVector.X + lineVector.Y * lineVector.Y);

        // Handle degenerate case where start and end are the same point
        if(lineLength < tolerance)
        {
            float distToStart = (float)Math.Sqrt(
                (point.X - lineStart.X) * (point.X - lineStart.X) +
                (point.Y - lineStart.Y) * (point.Y - lineStart.Y)
            );
            return distToStart <= tolerance;
        }

        // Normalize the line vector
        Vector2 lineDirection = new Vector2(lineVector.X / lineLength, lineVector.Y / lineLength);

        // Project the point onto the line
        float projectionLength = pointVector.X * lineDirection.X + pointVector.Y * lineDirection.Y
            ;

        // Check if the projection is within the line segment bounds
        if(projectionLength < -tolerance || projectionLength > lineLength + tolerance)
        {
            return false;
        }

        // Calculate the perpendicular distance from point to line
        Vector2 projectedPoint = new Vector2(
            lineStart.X + lineDirection.X * projectionLength,
            lineStart.Y + lineDirection.Y * projectionLength
        );

        float distance = (float)Math.Sqrt(
            (point.X - projectedPoint.X) * (point.X - projectedPoint.X) +
            (point.Y - projectedPoint.Y) * (point.Y - projectedPoint.Y)
        );

        return distance <= tolerance;
    }
}
