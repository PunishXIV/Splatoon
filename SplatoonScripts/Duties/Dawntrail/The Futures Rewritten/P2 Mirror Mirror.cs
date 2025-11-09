using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P2_Mirror_Mirror : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata Metadata => new(5, "Garume, NightmareXIV");
    public enum Action
    {
        OppositeBlueMirror,
        BlueMirror
    }

    public enum Clockwise
    {
        Clockwise,
        Counter_Clockwise,
        Do_not_display,
        Clockwise_from_your_position,
        Counter_Clockwise_from_your_position,
        Try_to_guess,
    }

    public enum Direction
    {
        North = 1,
        NorthEast = 2,
        East = 3,
        SouthEast = 4,
        South = 5,
        SouthWest = 6,
        West = 7,
        NorthWest = 8
    }

    public enum State
    {
        None,
        Casting,
        FirstAction,
        SecondAction,
        End
    }

    private readonly List<Direction> _redMirrorDirections = [];

    private Direction? _blueMirrorDirection;

    private Direction _firstActionDirection;

    private State _state = State.None;

    public Config C => Controller.GetConfig<Config>();

    public override void OnStartingCast(uint source, uint castId)
    {
        if(_state == State.End) return;
        if(castId == 40179) _state = State.Casting;
        if(_state == State.FirstAction)
        {
            if(castId == 40205)
            {
                var closestDirection = _redMirrorDirections
                    .OrderBy(dir => Math.Min(
                        Math.Abs((int)dir - (int)_firstActionDirection),
                        8 - Math.Abs((int)dir - (int)_firstActionDirection)
                    ))
                    .ThenBy(dir => C.Clockwise == Clockwise.Clockwise ? (int)dir : -(int)dir)
                    .First();
                ApplyElement(closestDirection);
                _state = State.SecondAction;
            }
        }
    }

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            tether = true,
            radius = 4f,
            Donut = 10f,
            fillIntensity = 0.4f,
            thicc = 6f,
        };

        Controller.RegisterElement("Bait", element);

        Controller.RegisterElementFromCode("SpotLeft", """{"Name":"","type":1,"offX":-3.0,"offY":0.5,"radius":0.5,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorObjectID":1073786597,"refActorComparisonType":2,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}""");
        Controller.RegisterElementFromCode("SpotRight", """{"Name":"","type":1,"offX":3.0,"offY":0.5,"radius":0.5,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorObjectID":1073786597,"refActorComparisonType":2,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}""");
    }

    public override void OnReset()
    {
        _state = State.None;
        _redMirrorDirections.Clear();
        _blueMirrorDirection = null;
        ReflectedPosition = null;
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(_state != State.Casting) return;
        if(data1 == 256 && data2 == 512)
            _redMirrorDirections.Add((Direction)position);
        else if(data1 == 1 && data2 == 2) _blueMirrorDirection = (Direction)position;

        if(_redMirrorDirections.Count == 2 && _blueMirrorDirection != null)
        {
            _state = State.FirstAction;
            _firstActionDirection = C.FirstAction == Action.BlueMirror
                ? _blueMirrorDirection.Value
                : (Direction)(((int)_blueMirrorDirection.Value + 4) % 8);
            if(C.FirstAction == Action.BlueMirror)
            { 
                ApplyElement(_firstActionDirection); 
            }
            else if(C.FirstAction == Action.OppositeBlueMirror)
            {
                ApplyElement(_firstActionDirection); 
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(_state == State.SecondAction)
        {
            if(set.Action.Value.RowId == 40205) _state = State.End;
        }
    }

    Vector3? ReflectedPosition = null;
    public override void OnUpdate()
    {
        if(_state.EqualsAny(State.End, State.None) || (_state == State.SecondAction && C.Clockwise >= Clockwise.Do_not_display)) { Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false); }
        Controller.GetRegisteredElements().Where(x => x.Key.StartsWith("Spot")).Each(x => x.Value.Enabled = false);

        var castingMirrors = Svc.Objects.OfType<IBattleNpc>().Where(x => x.DataId == 17825 && x.IsCasting(40205));
        if(castingMirrors.Count() == 2)
        {
            if(C.Clockwise == Clockwise.Try_to_guess)
            {
                var p1 = GetTankDistanceToPoint(castingMirrors.ElementAt(0).Position);
                var p2 = GetTankDistanceToPoint(castingMirrors.ElementAt(1).Position);
                if(p1 != null && p2 != null)
                {
                    var diff = p1.Value - p2.Value;
                    if(Math.Abs(diff) > 3f)
                    {
                        if(Player.Job.IsMeleeDps() || Player.Job.IsTank()) diff = -diff;
                        ReflectedPosition = castingMirrors.ElementAt(diff > 0 ? 0:1).Position;
                    }
                }
            } 
            else if(ReflectedPosition == null)
            {
                if(C.Clockwise == Clockwise.Clockwise_from_your_position)
                {
                    ReflectedPosition = GetClockwisePoint(Player.Position.ToVector2(), castingMirrors.Select(x => x.Position.ToVector2())).Value.ToVector3(0);
                }
                if(C.Clockwise == Clockwise.Counter_Clockwise_from_your_position)
                {
                    ReflectedPosition = castingMirrors.Where(x => x.Position.ToVector2() != GetClockwisePoint(Player.Position.ToVector2(), castingMirrors.Select(x => x.Position.ToVector2()))).First().Position;
                }
            }
            if(ReflectedPosition != null && Controller.TryGetElementByName("Bait", out var el))
            {
                el.Enabled = true;
                el.SetOffPosition(ReflectedPosition.Value);
            }
        }

        if(Controller.TryGetElementByName("Bait", out var e))
        {
            if(C.PreciseSpot && e.Enabled)
            {
                if(Svc.Objects.TryGetFirst(x => x is IBattleNpc b
                && b.NameId == 9317
                && Vector2.Distance(b.Position.ToVector2(), new(e.refX + e.offX, e.refY + e.offY)) < 1, out var obj))
                {
                    var element = Controller.GetElementByName($"Spot{C.BaitPosition}");
                    element.SetRefPosition(obj.Position);
                    element.refActorObjectID = obj.EntityId;
                    element.Enabled = true;
                }
                e.tether = false;
            }
            else
            {
                e.tether = true;
            }
        }
    }

    public float? GetTankDistanceToPoint(Vector3 point)
    {
        //Data ID: 17823
        var tank = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.IsTargetable && x.DataId == 17823)?.TargetObject;
        if(tank != null)
        {
            return Vector3.Distance(tank.Position, point);
        }
        return null;
    }

    public void ApplyElement(Direction direction)
    {
        var angle = ((int)direction - 3) % 8 * 45f;
        var position = new Vector3(100f, 0f, 100f);
        var radius = 20f;
        position += new Vector3((float)Math.Cos(MathF.PI * angle / 180f) * radius, 0f,
            (float)Math.Sin(MathF.PI * angle / 180f) * radius);
        if(Controller.TryGetElementByName("Bait", out var element))
        {
            element.Enabled = true;
            element.SetOffPosition(position);
        }
    }

    public override void OnSettingsDraw()
    {
        if(ImGuiEx.CollapsingHeader("General"))
        {
            ImGui.SetNextItemWidth(150f.Scale());
            ImGuiEx.EnumCombo("First Action", ref C.FirstAction);
            ImGui.SetNextItemWidth(150f.Scale());
            ImGuiEx.EnumCombo("Clockwise", ref C.Clockwise);
            ImGuiComponents.HelpMarker("When the red mirrors distance is equal.");

            ImGui.Checkbox("Show precise spot (left/right only)", ref C.PreciseSpot);
            if(C.PreciseSpot)
            {
                ImGui.Indent();
                ImGui.SetNextItemWidth(150f.Scale());
                ImGuiEx.EnumCombo("Spot", ref C.BaitPosition);
                ImGui.Unindent();
            }
        }

        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"First Action Direction: {_firstActionDirection}");
            ImGui.Text($"Blue Mirror Direction: {_blueMirrorDirection}");
            ImGui.Text($"Red Mirror Directions: {string.Join(", ", _redMirrorDirections)}");
        }
    }

    public class Config : IEzConfig
    {
        public Clockwise Clockwise = Clockwise.Clockwise_from_your_position;
        public Action FirstAction = Action.OppositeBlueMirror;
        public bool PreciseSpot = false;
        public BaitPosition BaitPosition = BaitPosition.Left;
    }

    public enum BaitPosition { Left, Right }

    private static readonly Vector2 Center = new Vector2(100.0f, 100.0f);

    /// <summary>
    /// Finds the point that is immediately clockwise from your position.
    /// </summary>
    /// <param name="myPosition">Your position</param>
    /// <param name="points">Collection of points to check</param>
    /// <returns>The point that is immediately clockwise, or null if no points provided</returns>
    public static Vector2? GetClockwisePoint(Vector2 myPosition, IEnumerable<Vector2> points)
    {
        Vector2? closestPoint = null;
        double smallestDiff = double.MaxValue;

        // Translate my position to center origin
        Vector2 myRel = myPosition - Center;
        myRel.Y = -myRel.Y; // Invert Y for proper angle calculation

        double myAngle = Math.Atan2(myRel.Y, myRel.X);

        foreach(var point in points)
        {
            // Translate to center origin
            Vector2 pRel = point - Center;
            pRel.Y = -pRel.Y; // Invert Y for proper angle calculation

            // Calculate angle
            double pAngle = Math.Atan2(pRel.Y, pRel.X);

            // Calculate clockwise difference
            double diff = NormalizeAngle(myAngle - pAngle);

            // Find the smallest positive difference (immediately clockwise)
            if(diff < smallestDiff)
            {
                smallestDiff = diff;
                closestPoint = point;
            }
        }

        return closestPoint;
    }

    /// <summary>
    /// Normalizes angle to range [0, 2π]
    /// </summary>
    private static double NormalizeAngle(double angle)
    {
        double normalized = angle % (2 * Math.PI);
        if(normalized < 0)
            normalized += 2 * Math.PI;
        return normalized;
    }
}