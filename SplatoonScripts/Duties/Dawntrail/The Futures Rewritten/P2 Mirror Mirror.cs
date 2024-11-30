using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Configuration;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P2_Mirror_Mirror : SplatoonScript
{
    public enum Action
    {
        OppositeBlueMirror,
        BlueMirror
    }

    public enum Clockwise
    {
        Clockwise,
        CounterClockwise
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

    private readonly List<Direction> _redMirrorDirections = new();

    private Direction? _blueMirrorDirection;

    private Direction _firstActionDirection;

    private State _state = State.None;
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(2, "Garume");

    public Config C => Controller.GetConfig<Config>();

    public override void OnStartingCast(uint source, uint castId)
    {
        if (_state == State.End) return;
        if (castId == 40179) _state = State.Casting;
        if (_state == State.FirstAction)
        {
            if (castId == 40205)
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
            radius = 2f,
            thicc = 6f
        };

        Controller.RegisterElement("Bait", element);
    }

    public override void OnReset()
    {
        _state = State.None;
        _redMirrorDirections.Clear();
        _blueMirrorDirection = null;
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if (_state != State.Casting) return;
        if (data1 == 256 && data2 == 512)
            _redMirrorDirections.Add((Direction)position);
        else if (data1 == 1 && data2 == 2) _blueMirrorDirection = (Direction)position;

        if (_redMirrorDirections.Count == 2 && _blueMirrorDirection != null)
        {
            _state = State.FirstAction;
            _firstActionDirection = C.FirstAction == Action.BlueMirror
                ? _blueMirrorDirection.Value
                : (Direction)(((int)_blueMirrorDirection.Value + 4) % 8);
            if (C.FirstAction == Action.BlueMirror)
                ApplyElement(_firstActionDirection);
            else if (C.FirstAction == Action.OppositeBlueMirror)
                ApplyElement(_firstActionDirection);
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_state == State.SecondAction)
        {
            if (set.Action.Value.RowId == 40205) _state = State.End;
        }
    }

    public override void OnUpdate()
    {
        if (_state is State.End or State.None) Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public void ApplyElement(Direction direction)
    {
        var angle = ((int)direction - 3) % 8 * 45f;
        var position = new Vector3(100f, 0f, 100f);
        var radius = 18f;
        position += new Vector3((float)Math.Cos(MathF.PI * angle / 180f) * radius, 0f,
            (float)Math.Sin(MathF.PI * angle / 180f) * radius);
        if (Controller.TryGetElementByName("Bait", out var element))
        {
            element.Enabled = true;
            element.SetOffPosition(position);
        }
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("General"))
        {
            ImGuiEx.EnumCombo("First Action", ref C.FirstAction);
            ImGuiEx.EnumCombo("Clockwise", ref C.Clockwise);
            ImGuiComponents.HelpMarker(
                "When the red mirrors distance is equal, choose by clockwise or counterclockwise.");
        }

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"First Action Direction: {_firstActionDirection}");
            ImGui.Text($"Blue Mirror Direction: {_blueMirrorDirection}");
            ImGui.Text($"Red Mirror Directions: {string.Join(", ", _redMirrorDirections)}");
        }
    }

    public class Config : IEzConfig
    {
        public Clockwise Clockwise = Clockwise.Clockwise;
        public Action FirstAction = Action.OppositeBlueMirror;
    }
}