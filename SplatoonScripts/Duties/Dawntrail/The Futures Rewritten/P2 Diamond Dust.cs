using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P2_Diamond_Dust : SplatoonScript
{
    public enum Direction
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    private enum IcicleImpactDirection
    {
        None,
        North,
        NorthEast,
        East,
        SouthEast
    }

    private enum State
    {
        None,
        Start,
        Split,
        Knockback,
        Move,
        End
    }

    private const uint DonutCastId = 40203;
    private const uint CircleCastId = 40202;

    private IcicleImpactDirection _firstIcicleImpactDirection = IcicleImpactDirection.None;
    private bool _hasAoe;

    private State _state = State.None;
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(1, "Garume");

    private Config C => Controller.GetConfig<Config>();

    public override void OnReset()
    {
        _state = State.None;
        _firstIcicleImpactDirection = IcicleImpactDirection.None;
        _hasAoe = false;
    }


    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (vfxPath == "vfx/lockon/eff/tag_ae5m_8s_0v.avfx")
            if (target.GetObject() is IPlayerCharacter player)
                _hasAoe = (player.GetRole() == CombatRole.DPS && Player.Object.GetRole() == CombatRole.DPS) ||
                          ((player.GetRole() == CombatRole.Healer || player.GetRole() == CombatRole.Tank) &&
                           (Player.Object.GetRole() == CombatRole.Healer ||
                            Player.Object.GetRole() == CombatRole.Tank));
    }

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            tether = true,
            radius = 3f,
            thicc = 6f
        };

        Controller.RegisterElement("Bait", element);
    }

    public override void OnUpdate()
    {
        if (_state is State.None or State.End)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            return;
        }

        Controller.GetRegisteredElements()
            .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("General"))
        {
            ImGui.Indent();
            if (ImGuiEx.CollapsingHeader("Circle"))
            {
                ImGui.Indent();
                ImGuiEx.Text("No AOE");
                ImGuiEx.EnumCombo($"East-West##{nameof(C.MoveDirectionWhenEastIcicleImpactCircleHasNoAoe)}",
                    ref C.MoveDirectionWhenEastIcicleImpactCircleHasNoAoe);
                ImGuiEx.EnumCombo(
                    $"NorthEast-SouthWest##{nameof(C.MoveDirectionWhenNorthEastIcicleImpactCircleHasNoAoe)}",
                    ref C.MoveDirectionWhenNorthEastIcicleImpactCircleHasNoAoe);
                ImGuiEx.EnumCombo($"North-South##{nameof(C.MoveDirectionWhenNorthIcicleImpactCircleHasNoAoe)}",
                    ref C.MoveDirectionWhenNorthIcicleImpactCircleHasNoAoe);
                ImGuiEx.EnumCombo(
                    $"SouthEast-NorthWest##{nameof(C.MoveDirectionWhenSouthEastIcicleImpactCircleHasNoAoe)}",
                    ref C.MoveDirectionWhenSouthEastIcicleImpactCircleHasNoAoe);
                ImGui.Separator();
                ImGuiEx.Text("Has AOE");
                ImGuiEx.EnumCombo($"East-West##{nameof(C.MoveDirectionWhenEastIcicleImpactCircleHasAoe)}",
                    ref C.MoveDirectionWhenEastIcicleImpactCircleHasAoe);
                ImGuiEx.EnumCombo(
                    $"NorthEast-SouthWest##{nameof(C.MoveDirectionWhenNorthEastIcicleImpactCircleHasAoe)}",
                    ref C.MoveDirectionWhenNorthEastIcicleImpactCircleHasAoe);
                ImGuiEx.EnumCombo($"North-South##{nameof(C.MoveDirectionWhenNorthIcicleImpactCircleHasAoe)}",
                    ref C.MoveDirectionWhenNorthIcicleImpactCircleHasAoe);
                ImGuiEx.EnumCombo(
                    $"SouthEast-NorthWest##{nameof(C.MoveDirectionWhenSouthEastIcicleImpactCircleHasAoe)}",
                    ref C.MoveDirectionWhenSouthEastIcicleImpactCircleHasAoe);
                ImGui.Unindent();
            }

            if (ImGuiEx.CollapsingHeader("Donut"))
            {
                ImGui.Indent();
                ImGuiEx.Text("No AOE");
                ImGuiEx.EnumCombo($"East-West##{nameof(C.MoveDirectionWhenEastIcicleImpactDonutHasNoAoe)}",
                    ref C.MoveDirectionWhenEastIcicleImpactDonutHasNoAoe);
                ImGuiEx.EnumCombo(
                    $"NorthEast-SouthWest##{nameof(C.MoveDirectionWhenNorthEastIcicleImpactDonutHasNoAoe)}",
                    ref C.MoveDirectionWhenNorthEastIcicleImpactDonutHasNoAoe);
                ImGuiEx.EnumCombo($"North-South##{nameof(C.MoveDirectionWhenNorthIcicleImpactDonutHasNoAoe)}",
                    ref C.MoveDirectionWhenNorthIcicleImpactDonutHasNoAoe);
                ImGuiEx.EnumCombo(
                    $"SouthEast-NorthWest##{nameof(C.MoveDirectionWhenSouthEastIcicleImpactDonutHasNoAoe)}",
                    ref C.MoveDirectionWhenSouthEastIcicleImpactDonutHasNoAoe);
                ImGui.Separator();
                ImGuiEx.Text("Has AOE");
                ImGuiEx.EnumCombo($"East-West##{nameof(C.MoveDirectionWhenEastIcicleImpactDonutHasAoe)}",
                    ref C.MoveDirectionWhenEastIcicleImpactDonutHasAoe);
                ImGuiEx.EnumCombo($"NorthEast-SouthWest##{nameof(C.MoveDirectionWhenNorthEastIcicleImpactDonutHasAoe)}",
                    ref C.MoveDirectionWhenNorthEastIcicleImpactDonutHasAoe);
                ImGuiEx.EnumCombo($"North-South##{nameof(C.MoveDirectionWhenNorthIcicleImpactDonutHasAoe)}",
                    ref C.MoveDirectionWhenNorthIcicleImpactDonutHasAoe);
                ImGuiEx.EnumCombo($"SouthEast-NorthWest##{nameof(C.MoveDirectionWhenSouthEastIcicleImpactDonutHasAoe)}",
                    ref C.MoveDirectionWhenSouthEastIcicleImpactDonutHasAoe);
                ImGui.Unindent();
            }

            if (ImGuiEx.CollapsingHeader("Knockback"))
            {
                ImGui.Indent();
                ImGuiEx.Text("Knockback Directions");
                ImGui.Indent();
                ImGuiEx.CollectionCheckbox("North", Direction.North, C.KnockbackDirections);
                ImGuiEx.CollectionCheckbox("NorthEast", Direction.NorthEast, C.KnockbackDirections);
                ImGuiEx.CollectionCheckbox("East", Direction.East, C.KnockbackDirections);
                ImGuiEx.CollectionCheckbox("SouthEast", Direction.SouthEast, C.KnockbackDirections);
                ImGuiEx.CollectionCheckbox("South", Direction.South, C.KnockbackDirections);
                ImGuiEx.CollectionCheckbox("SouthWest", Direction.SouthWest, C.KnockbackDirections);
                ImGuiEx.CollectionCheckbox("West", Direction.West, C.KnockbackDirections);
                ImGuiEx.CollectionCheckbox("NorthWest", Direction.NorthWest, C.KnockbackDirections);
                ImGui.Unindent();
                ImGui.Unindent();
            }

            ImGui.Text("Bait Color:");
            ImGuiComponents.HelpMarker(
                "Change the color of the bait and the text that will be displayed on your bait.\nSetting different values makes it rainbow.");
            ImGui.Indent();
            ImGui.ColorEdit4("Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
            ImGui.SameLine();
            ImGui.ColorEdit4("Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
            ImGui.Unindent();

            ImGui.Unindent();
        }

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Indent();
            ImGuiEx.Text($"First Icicle Impact Direction: {_firstIcicleImpactDirection}");
            ImGuiEx.Text($"Has AOE: {_hasAoe}");
            ImGui.Unindent();
        }
    }


    public override void OnStartingCast(uint source, uint castId)
    {
        if (_state == State.End) return;
        if (castId is 40197) _state = State.Start;

        if (_firstIcicleImpactDirection == IcicleImpactDirection.None)
            if (castId is 40198)
            {
                var icicleObject = source.GetObject();
                var isEast = icicleObject?.Position.X > 105;
                var isWest = icicleObject?.Position.X < 95;
                var isNorth = icicleObject?.Position.Y < 95;

                if (isNorth && isEast)
                    _firstIcicleImpactDirection = IcicleImpactDirection.NorthEast;
                else if (isNorth && isWest)
                    _firstIcicleImpactDirection = IcicleImpactDirection.SouthEast;
                else if (isEast)
                    _firstIcicleImpactDirection = IcicleImpactDirection.East;
                else if (isNorth) _firstIcicleImpactDirection = IcicleImpactDirection.North;
            }

        switch (castId)
        {
            case CircleCastId:
            {
                _state = State.Split;
                var radius = 18f;
                if (_hasAoe)
                {
                    if (_firstIcicleImpactDirection == IcicleImpactDirection.North)
                        ApplyElement("Bait", C.MoveDirectionWhenNorthIcicleImpactCircleHasAoe, radius);
                    else if (_firstIcicleImpactDirection == IcicleImpactDirection.NorthEast)
                        ApplyElement("Bait", C.MoveDirectionWhenNorthEastIcicleImpactCircleHasAoe, radius);
                    else if (_firstIcicleImpactDirection == IcicleImpactDirection.East)
                        ApplyElement("Bait", C.MoveDirectionWhenEastIcicleImpactCircleHasAoe, radius);
                    else if (_firstIcicleImpactDirection == IcicleImpactDirection.SouthEast)
                        ApplyElement("Bait", C.MoveDirectionWhenSouthEastIcicleImpactCircleHasAoe, radius);
                }

                else
                {
                    if (_firstIcicleImpactDirection == IcicleImpactDirection.North)
                        ApplyElement("Bait", C.MoveDirectionWhenNorthIcicleImpactCircleHasNoAoe, radius);
                    else if (_firstIcicleImpactDirection == IcicleImpactDirection.NorthEast)
                        ApplyElement("Bait", C.MoveDirectionWhenNorthEastIcicleImpactCircleHasNoAoe, radius);
                    else if (_firstIcicleImpactDirection == IcicleImpactDirection.East)
                        ApplyElement("Bait", C.MoveDirectionWhenEastIcicleImpactCircleHasNoAoe, radius);
                    else if (_firstIcicleImpactDirection == IcicleImpactDirection.SouthEast)
                        ApplyElement("Bait", C.MoveDirectionWhenSouthEastIcicleImpactCircleHasNoAoe, radius);
                }

                break;
            }
            case DonutCastId:
            {
                _state = State.Split;
                var radius = 5f;
                if (_hasAoe)
                {
                    if (_firstIcicleImpactDirection == IcicleImpactDirection.North)
                        ApplyElement("Bait", C.MoveDirectionWhenNorthIcicleImpactDonutHasAoe, radius);
                    else if (_firstIcicleImpactDirection == IcicleImpactDirection.NorthEast)
                        ApplyElement("Bait", C.MoveDirectionWhenNorthEastIcicleImpactDonutHasAoe, radius);
                    else if (_firstIcicleImpactDirection == IcicleImpactDirection.East)
                        ApplyElement("Bait", C.MoveDirectionWhenEastIcicleImpactDonutHasAoe, radius);
                    else if (_firstIcicleImpactDirection == IcicleImpactDirection.SouthEast)
                        ApplyElement("Bait", C.MoveDirectionWhenSouthEastIcicleImpactDonutHasAoe, radius);
                }

                else
                {
                    if (_firstIcicleImpactDirection == IcicleImpactDirection.North)
                        ApplyElement("Bait", C.MoveDirectionWhenNorthIcicleImpactDonutHasNoAoe, radius);
                    else if (_firstIcicleImpactDirection == IcicleImpactDirection.NorthEast)
                        ApplyElement("Bait", C.MoveDirectionWhenNorthEastIcicleImpactDonutHasNoAoe, radius);
                    else if (_firstIcicleImpactDirection == IcicleImpactDirection.East)
                        ApplyElement("Bait", C.MoveDirectionWhenEastIcicleImpactDonutHasNoAoe, radius);
                    else if (_firstIcicleImpactDirection == IcicleImpactDirection.SouthEast)
                        ApplyElement("Bait", C.MoveDirectionWhenSouthEastIcicleImpactDonutHasNoAoe, radius);
                }

                break;
            }
        }
    }

    private void ApplyElement(string elementName, Direction direction, float radius, float elementRadius = 3f)
    {
        var position = new Vector3(100, 0, 100);
        var angle = GetAngle(direction);
        position += radius * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            element.Enabled = true;
            element.radius = elementRadius;
            element.SetOffPosition(position);
        }
    }


    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_state is State.None or State.End) return;
        if (!set.Action.HasValue) return;
        if (set.Action.Value.RowId is CircleCastId or DonutCastId)
        {
            _state = State.Knockback;
            if (_firstIcicleImpactDirection == IcicleImpactDirection.SouthEast)
            {
                if (C.KnockbackDirections.Contains(Direction.SouthEast))
                    ApplyElement("Bait", Direction.SouthEast, 5f, 1f);
                else if (C.KnockbackDirections.Contains(Direction.NorthWest))
                    ApplyElement("Bait", Direction.NorthWest, 5f, 1f);
            }
            else if (_firstIcicleImpactDirection == IcicleImpactDirection.North)
            {
                if (C.KnockbackDirections.Contains(Direction.North))
                    ApplyElement("Bait", Direction.North, 5f, 1f);
                else if (C.KnockbackDirections.Contains(Direction.South))
                    ApplyElement("Bait", Direction.South, 5f, 1f);
            }
            else if (_firstIcicleImpactDirection == IcicleImpactDirection.NorthEast)
            {
                if (C.KnockbackDirections.Contains(Direction.NorthEast))
                    ApplyElement("Bait", Direction.NorthEast, 5f, 1f);
                else if (C.KnockbackDirections.Contains(Direction.SouthWest))
                    ApplyElement("Bait", Direction.SouthWest, 5f, 1f);
            }
            else if (_firstIcicleImpactDirection == IcicleImpactDirection.East)
            {
                if (C.KnockbackDirections.Contains(Direction.East))
                    ApplyElement("Bait", Direction.East, 5f, 1f);
                else if (C.KnockbackDirections.Contains(Direction.West))
                    ApplyElement("Bait", Direction.West, 5f, 1f);
            }
        }

        if (set.Action.Value.RowId is 40208)
        {
            _state = State.End;
            if (Controller.TryGetElementByName("Bait", out var element)) element.Enabled = false;
        }
    }

    private float GetAngle(Direction direction)
    {
        return direction switch
        {
            Direction.North => 270,
            Direction.NorthEast => 315,
            Direction.East => 0,
            Direction.SouthEast => 45,
            Direction.South => 90,
            Direction.SouthWest => 135,
            Direction.West => 180,
            Direction.NorthWest => 225,
            _ => 0
        };
    }

    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();

        public List<Direction> KnockbackDirections =
        [
            Direction.North,
            Direction.NorthEast,
            Direction.East,
            Direction.SouthEast
        ];

        public Direction MoveDirectionWhenEastIcicleImpactCircleHasAoe = Direction.North;

        public Direction MoveDirectionWhenEastIcicleImpactCircleHasNoAoe = Direction.North;
        public Direction MoveDirectionWhenEastIcicleImpactDonutHasAoe = Direction.North;

        public Direction MoveDirectionWhenEastIcicleImpactDonutHasNoAoe = Direction.North;
        public Direction MoveDirectionWhenNorthEastIcicleImpactCircleHasAoe = Direction.North;
        public Direction MoveDirectionWhenNorthEastIcicleImpactCircleHasNoAoe = Direction.North;
        public Direction MoveDirectionWhenNorthEastIcicleImpactDonutHasAoe = Direction.North;
        public Direction MoveDirectionWhenNorthEastIcicleImpactDonutHasNoAoe = Direction.North;
        public Direction MoveDirectionWhenNorthIcicleImpactCircleHasAoe = Direction.North;
        public Direction MoveDirectionWhenNorthIcicleImpactCircleHasNoAoe = Direction.North;
        public Direction MoveDirectionWhenNorthIcicleImpactDonutHasAoe = Direction.North;
        public Direction MoveDirectionWhenNorthIcicleImpactDonutHasNoAoe = Direction.North;
        public Direction MoveDirectionWhenSouthEastIcicleImpactCircleHasAoe = Direction.North;
        public Direction MoveDirectionWhenSouthEastIcicleImpactCircleHasNoAoe = Direction.North;
        public Direction MoveDirectionWhenSouthEastIcicleImpactDonutHasAoe = Direction.North;
        public Direction MoveDirectionWhenSouthEastIcicleImpactDonutHasNoAoe = Direction.North;
    }
}