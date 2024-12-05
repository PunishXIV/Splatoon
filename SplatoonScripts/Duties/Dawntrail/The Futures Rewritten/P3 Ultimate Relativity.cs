using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using Status = FFXIVClientStructs.FFXIV.Client.Game.Status;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P3_Ultimate_Relativity : SplatoonScript
{
    public enum Clockwise
    {
        Clockwise,
        CounterClockwise
    }

    public enum Direction
    {
        East = 0,
        SouthEast = 45,
        South = 90,
        SouthWest = 135,
        West = 180,
        NorthWest = 225,
        North = 270,
        NorthEast = 315
    }

    public enum KindFire
    {
        Early,
        Middle,
        Late,
        Blizzard
    }

    public enum Mode
    {
        Marker,
        Priority
    }

    private readonly List<IBattleChara> _earlyHourglassList = [];

    private readonly Dictionary<Direction, Hourglass> _hourglasses = new();
    private readonly List<IBattleChara> _lateHourglassList = [];

    private readonly Dictionary<ulong, PlayerData> _playerDatas = [];

    private readonly Dictionary<Direction, Vector2> _positions = new();

    private Direction? _baseDirection;

    private bool _showSinboundMeltdown;

    private State _state = State.None;

    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(6, "Garume");

    private Config C => Controller.GetConfig<Config>();

    public override void OnStartingCast(uint source, uint castId)
    {
        if (_state == State.End) return;
        if (castId == 40266) _state = State.Start;
        if (castId == 40291) _showSinboundMeltdown = true;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_state == State.End || set.Action == null) return;
        var castId = set.Action.Value.RowId;
        if (castId is 40291 or 40235) _showSinboundMeltdown = false;
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_state == State.End) return;
        if (source.GetObject() is not IBattleChara sourceObject) return;
        if (data5 == 15)
        {
            switch (data3)
            {
                case 133:
                    _lateHourglassList.Add(sourceObject);
                    break;
                case 134:
                    _earlyHourglassList.Add(sourceObject);
                    break;
            }

            if (_lateHourglassList.Count == 2 && _earlyHourglassList.Count == 3)
            {
                var nonHourglass = _lateHourglassList.Select(lateHourglass =>
                    _earlyHourglassList.MinBy(x => Vector3.Distance(x.Position, lateHourglass.Position)) ??
                    throw new InvalidOperationException()).ToList();

                var baseOrientationHourglass = _earlyHourglassList.Except(nonHourglass).First();
                _baseDirection = (Direction)((int)GetDirection(baseOrientationHourglass.Position) % 360);
            }
        }
    }

    private static Direction GetDirection(Vector3 position)
    {
        var isNorth = position.Z < 95f;
        var isEast = position.X > 105f;
        var isSouth = position.Z > 105f;
        var isWest = position.X < 95f;

        if (isNorth && isEast) return Direction.NorthEast;
        if (isEast && isSouth) return Direction.SouthEast;
        if (isSouth && isWest) return Direction.SouthWest;
        if (isWest && isNorth) return Direction.NorthWest;
        if (isNorth) return Direction.North;
        if (isEast) return Direction.East;
        if (isSouth) return Direction.South;
        if (isWest) return Direction.West;
        throw new InvalidOperationException();
    }

    private Direction SwapAsOrientation(Direction direction)
    {
        if (!C.BaseOrientationIsNorth)
            return direction switch
            {
                Direction.North => Direction.North,
                Direction.NorthEast => Direction.NorthWest,
                Direction.East => Direction.West,
                Direction.SouthEast => Direction.SouthWest,
                Direction.South => Direction.South,
                Direction.SouthWest => Direction.SouthEast,
                Direction.West => Direction.East,
                Direction.NorthWest => Direction.NorthEast,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        return direction;
    }

    public override void OnGainBuffEffect(uint sourceId, Status Status)
    {
        if (_state == State.Start)
            if (FakeParty.Get()
                .All(x => Enum.GetValues<Debuff>().Any(y => x.StatusList.Any(z => z.StatusId == (uint)y))))
            {
                _state = State.FirstFire;
                if (C.Mode == Mode.Marker)
                {
                    var myKindFire = GetKindFire(Player.Status);
                    var random = 0;
                    if (C.ShouldUseRandomWait)
                        random = new Random().Next((int)(C.WaitRange.X * 1000), (int)(C.WaitRange.Y * 1000));
                    Controller.Schedule(() =>
                    {
                        switch (myKindFire)
                        {
                            case KindFire.Early:
                                Chat.Instance.ExecuteCommand(C.EarlyFireCommand);
                                break;
                            case KindFire.Middle:
                                Chat.Instance.ExecuteCommand(C.MiddleFireCommand);
                                break;
                            case KindFire.Late:
                                Chat.Instance.ExecuteCommand(C.LateFireCommand);
                                break;
                            case KindFire.Blizzard:
                                Chat.Instance.ExecuteCommand(C.BlizzardCommand);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }, random);
                }
                else if (C.Mode == Mode.Priority)
                {
                    var earlyPriority = (C.PriorityList.GetPlayers(x =>
                            ((IPlayerCharacter)x.IGameObject).StatusList.Any(y =>
                                y is { StatusId: (uint)Debuff.Fire, RemainingTime: < 15 })) ?? [])
                        .Select(x => (IPlayerCharacter)x.IGameObject);
                    var middlePriority = (C.PriorityList.GetPlayers(x =>
                            ((IPlayerCharacter)x.IGameObject).StatusList.Any(y =>
                                y is { StatusId: (uint)Debuff.Fire, RemainingTime: >= 15 and < 25 })) ?? [])
                        .Select(x => (IPlayerCharacter)x.IGameObject);
                    var latePriority = (C.PriorityList.GetPlayers(x =>
                            ((IPlayerCharacter)x.IGameObject).StatusList.Any(y =>
                                y is { StatusId: (uint)Debuff.Fire, RemainingTime: >= 25 })) ?? [])
                        .Select(x => (IPlayerCharacter)x.IGameObject);

                    var directions = Enum.GetValues<Direction>().ToList();
                    var index = 0;
                    foreach (var player in earlyPriority)
                        if (player.GetRole() != CombatRole.DPS)
                        {
                            directions.Remove(SwapAsOrientation(Direction.South));
                            _playerDatas[player.GameObjectId] = new PlayerData
                            {
                                PlayerName = player.Name.ToString(),
                                KindFire = KindFire.Early,
                                Number = 3,
                                Direction = SwapAsOrientation(Direction.South)
                            };
                        }
                        else
                        {
                            // West
                            if (index == 0)
                            {
                                directions.Remove(SwapAsOrientation(Direction.NorthWest));
                                _playerDatas[player.GameObjectId] = new PlayerData
                                {
                                    PlayerName = player.Name.ToString(),
                                    KindFire = KindFire.Early,
                                    Number = 1,
                                    Direction = SwapAsOrientation(Direction.NorthWest)
                                };
                                index++;
                            }
                            // East
                            else
                            {
                                directions.Remove(SwapAsOrientation(Direction.NorthEast));
                                _playerDatas[player.GameObjectId] = new PlayerData
                                {
                                    PlayerName = player.Name.ToString(),
                                    KindFire = KindFire.Early,
                                    Number = 2,
                                    Direction = SwapAsOrientation(Direction.NorthEast)
                                };
                            }
                        }

                    foreach (var player in middlePriority)
                        if (player.GetRole() != CombatRole.DPS)
                        {
                            directions.Remove(SwapAsOrientation(Direction.West));
                            _playerDatas[player.GameObjectId] = new PlayerData
                            {
                                PlayerName = player.Name.ToString(),
                                KindFire = KindFire.Middle,
                                Number = 1,
                                Direction = SwapAsOrientation(Direction.West)
                            };
                        }
                        else
                        {
                            directions.Remove(SwapAsOrientation(Direction.East));
                            _playerDatas[player.GameObjectId] = new PlayerData
                            {
                                PlayerName = player.Name.ToString(),
                                KindFire = KindFire.Middle,
                                Number = 2,
                                Direction = SwapAsOrientation(Direction.East)
                            };
                        }

                    index = 0;
                    foreach (var player in latePriority)
                        if (player.GetRole() != CombatRole.DPS)
                        {
                            // West
                            if (index == 0)
                            {
                                directions.Remove(SwapAsOrientation(Direction.SouthWest));
                                _playerDatas[player.GameObjectId] = new PlayerData
                                {
                                    PlayerName = player.Name.ToString(),
                                    KindFire = KindFire.Late,
                                    Number = 1,
                                    Direction = SwapAsOrientation(Direction.SouthWest)
                                };
                                index++;
                            }
                            // East
                            else
                            {
                                directions.Remove(SwapAsOrientation(Direction.SouthEast));
                                _playerDatas[player.GameObjectId] = new PlayerData
                                {
                                    PlayerName = player.Name.ToString(),
                                    KindFire = KindFire.Late,
                                    Number = 2,
                                    Direction = SwapAsOrientation(Direction.SouthEast)
                                };
                            }
                        }
                        else
                        {
                            directions.Remove(SwapAsOrientation(Direction.North));
                            _playerDatas[player.GameObjectId] = new PlayerData
                            {
                                PlayerName = player.Name.ToString(),
                                KindFire = KindFire.Late,
                                Number = 3,
                                Direction = SwapAsOrientation(Direction.North)
                            };
                        }

                    var direction = directions.First();
                    var blizzardPlayer = FakeParty
                        .Get()
                        .FirstOrDefault(x => x.StatusList.Any(y => y.StatusId == (uint)Debuff.Blizzard));
                    if (blizzardPlayer != null)
                        _playerDatas[blizzardPlayer.GameObjectId] = new PlayerData
                        {
                            PlayerName = blizzardPlayer.Name.ToString(),
                            KindFire = KindFire.Blizzard,
                            Number = 3,
                            Direction = direction
                        };

                    if (_playerDatas.Count != 8) DuoLog.Warning("Error: Not all players are assigned");
                }
            }

        if (_state == State.End) return;
        if (_baseDirection == null) return;
        if (Status.StatusId == 2970)
        {
            var clockwise = Status.Param == 348 ? Clockwise.Clockwise : Clockwise.CounterClockwise;
            var position = sourceId.GetObject()?.Position ?? throw new InvalidOperationException();
            var direction = GetDirection(position);
            var basedDirection = (Direction)((int)direction - (int)_baseDirection.Value - 90);
            while (basedDirection < 0) basedDirection += 360;

            _hourglasses[basedDirection] = new Hourglass { Clockwise = clockwise, Direction = direction };

            _state = _hourglasses.Count switch
            {
                3 => State.BaitEarlyHourglass,
                6 => State.BaitNoTetherHourglass,
                8 => State.BaitLateHourglass,
                _ => _state
            };
        }
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status Status)
    {
        if (_state == State.End) return;
        if (Status.StatusId == 2970)
            _state = _hourglasses.Count switch
            {
                3 => State.SecondFire,
                6 => State.ThirdFire,
                8 => State.End,
                _ => _state
            };
    }

    private void BaitHourglass(Direction direction)
    {
        if (_baseDirection == null) return;
        var basedDirection = (Direction)(((int)direction + (int)_baseDirection.Value + 90) % 360);
        var hourglass = _hourglasses[direction];
        var radius = 11f;
        var offset = hourglass.Clockwise == Clockwise.Clockwise ? -15 : 15;
        var center = new Vector2(100, 100);
        var position = new Vector2(
            center.X + radius * MathF.Cos(MathF.PI * ((int)basedDirection + offset) / 180),
            center.Y + radius * MathF.Sin(MathF.PI * ((int)basedDirection + offset) / 180)
        );

        _positions[direction] = position;
        if (Controller.TryGetElementByName(direction.ToString(), out var element))
        {
            element.radius = 0.5f;
            element.SetOffPosition(position.ToVector3(0f));
        }
    }

    private void GoCenter(Direction direction, float radius = 2f)
    {
        if (_baseDirection == null) return;
        var center = new Vector2(100f, 100f);
        _positions[direction] = center;
        if (Controller.TryGetElementByName(direction.ToString(), out var element))
        {
            element.radius = radius;
            element.SetOffPosition(center.ToVector3(0f));
        }
    }

    private void GoNearCenter(Direction direction)
    {
        const float radius = 1f;
        if (_baseDirection == null) return;
        var basedDirection = (Direction)(((int)direction + (int)_baseDirection.Value + 90) % 360);
        var center = new Vector2(100, 100);
        var position = new Vector2(
            center.X + radius * MathF.Cos(MathF.PI * (int)basedDirection / 180),
            center.Y + radius * MathF.Sin(MathF.PI * (int)basedDirection / 180)
        );
        _positions[direction] = position;
        if (Controller.TryGetElementByName(direction.ToString(), out var element))
        {
            element.radius = 0.5f;
            element.SetOffPosition(position.ToVector3(0f));
        }
    }

    private void GoOutside(Direction direction)
    {
        const float radius = 18f;
        if (_baseDirection == null) return;
        var basedDirection = (Direction)(((int)direction + (int)_baseDirection.Value + 90) % 360);
        var center = new Vector2(100, 100);
        var position = new Vector2(
            center.X + radius * MathF.Cos(MathF.PI * (int)basedDirection / 180),
            center.Y + radius * MathF.Sin(MathF.PI * (int)basedDirection / 180)
        );
        _positions[direction] = position;
        if (Controller.TryGetElementByName(direction.ToString(), out var element))
        {
            element.radius = 2f;
            element.SetOffPosition(position.ToVector3(0f));
        }
    }

    private void PlaceReturnToHourglass(Direction direction, float radius)
    {
        if (_baseDirection == null) return;
        var basedDirection = (Direction)(((int)direction + (int)_baseDirection.Value + 90) % 360);
        var center = new Vector2(100, 100);
        var position = new Vector2(
            center.X + radius * MathF.Cos(MathF.PI * (int)basedDirection / 180),
            center.Y + radius * MathF.Sin(MathF.PI * (int)basedDirection / 180)
        );
        _positions[direction] = position;
        if (Controller.TryGetElementByName(direction.ToString(), out var element))
        {
            element.radius = 1f;
            element.SetOffPosition(position.ToVector3(0f));
        }
    }

    private void PlaceReturnToHourglass(Direction direction)
    {
        PlaceReturnToHourglass(direction, 10f);
    }

    private void PlaceReturnToHourglassOutside(Direction direction)
    {
        PlaceReturnToHourglass(direction, 11f);
    }

    private void PlaceReturnToHourglassInside(Direction direction)
    {
        PlaceReturnToHourglass(direction, 8f);
    }

    private void ShowSinboundMeltdown()
    {
        var hourGlassesList = Svc.Objects
            .Where(x => x is IBattleNpc npc && npc is { CastActionId: 40291, IsCasting: true }).ToList();
        if (!hourGlassesList.Any()) return;
        PluginLog.Information($"hourGlassesList.Count(): {hourGlassesList.Count()}");

        var pcs = FakeParty.Get().ToList();
        if (pcs.Count != 8) return;

        var i = 0;
        foreach (var hourglass in hourGlassesList)
        {
            // Search for the closest player
            var closestPlayer = pcs.MinBy(x => Vector3.Distance(x.Position, hourglass.Position));

            // Show Element
            if (Controller.TryGetElementByName($"SinboundMeltdown{i}", out var element) && closestPlayer != null)
            {
                var extPos = GetExtendedAndClampedPosition(hourglass.Position, closestPlayer.Position, 20f, 20f);
                element.SetRefPosition(hourglass.Position);
                element.SetOffPosition(extPos);
                element.Enabled = true;
                i++;
            }
        }
    }

    public void HideSinboundMeltdown()
    {
        for (var i = 0; i < 3; ++i)
            if (Controller.TryGetElementByName($"SinboundMeltdown{i}", out var element))
                element.Enabled = false;
    }

    private static Vector3 GetExtendedAndClampedPosition(Vector3 center, Vector3 currentPos, float extensionLength,
        float? limit)
    {
        // Calculate the normalized direction vector from the center to the current position
        var direction = Vector3.Normalize(currentPos - center);

        // Extend the position by the specified length
        var extendedPos = currentPos + direction * extensionLength;

        // If limit is null, return the extended position without clamping
        if (!limit.HasValue) return extendedPos;

        // Calculate the distance from the center to the extended position
        var distanceFromCenter = Vector3.Distance(center, extendedPos);

        // If the extended position exceeds the limit, clamp it within the limit
        if (distanceFromCenter > limit.Value) return center + direction * limit.Value;

        // If within the limit, return the extended position as is
        return extendedPos;
    }

    public override void OnSetup()
    {
        foreach (var direction in Enum.GetNames<Direction>())
        {
            var element = new Element(0)
            {
                thicc = 6f,
                radius = 2f
            };
            Controller.RegisterElement(direction, element);
        }

        Controller.RegisterElement("Text", new Element(0)
        {
            offX = 100f,
            offY = 100f,
            overlayFScale = 5f,
            overlayVOffset = 5f,
            radius = 0f
        });

        for (var i = 0; i < 3; ++i)
        {
            var element = new Element(2)
            {
                thicc = 2f,
                radius = 2.5f,
                fillIntensity = 0.25f,
                Filled = true
            };
            Controller.RegisterElement($"SinboundMeltdown{i}", element);
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _showSinboundMeltdown = false;
        _playerDatas.Clear();
        _hourglasses.Clear();
        _baseDirection = null;
        _earlyHourglassList.Clear();
        _lateHourglassList.Clear();
        _positions.Clear();

        if (Controller.TryGetElementByName("Text", out var element)) element.Enabled = false;
        Controller.GetRegisteredElements().Each(x => x.Value.tether = false);
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text(EColor.RedBright, """
                                       This script has not been thoroughly tested.
                                       It may not work properly.
                                       If you encounter any bugs, please let us know.
                                       """);

        if (ImGuiEx.CollapsingHeader("General"))
        {
            ImGui.Indent();
            ImGuiEx.EnumCombo("Mode", ref C.Mode);
            ImGuiEx.HelpMarker(
                "Marker: Use the marker to determine the direction of the player.\nPriority: Use the priority list to determine the direction of the player.");
            ImGui.Indent();
            if (C.Mode == Mode.Marker)
            {
                ImGui.Text("""
                           Set the command to execute when the Fire debuff is applied.
                           Make sure the command is one that assigns markers.
                           Please note that the functionality may not work correctly if markers are not assigned to all party members.
                           """);
                ImGui.InputText("Early Fire Command", ref C.EarlyFireCommand, 100);
                ImGui.InputText("Middle Fire Command", ref C.MiddleFireCommand, 100);
                ImGui.InputText("Late Fire Command", ref C.LateFireCommand, 100);
                ImGui.InputText("Blizzard Command", ref C.BlizzardCommand, 100);

                ImGui.Checkbox("Random Wait", ref C.ShouldUseRandomWait);
                if (C.ShouldUseRandomWait)
                {
                    var minWait = C.WaitRange.X;
                    var maxWait = C.WaitRange.Y;
                    ImGui.SliderFloat2("Wait Range (sec)", ref C.WaitRange, 0f, 3f, "%.1f");
                    if (Math.Abs(minWait - C.WaitRange.X) > 0.01f)
                    {
                        if (C.WaitRange.X > C.WaitRange.Y)
                            C.WaitRange.Y = C.WaitRange.X;
                    }
                    else if (Math.Abs(maxWait - C.WaitRange.Y) > 0.01f)
                    {
                        if (C.WaitRange.Y < C.WaitRange.X)
                            C.WaitRange.X = C.WaitRange.Y;
                    }
                }


                ImGui.Separator();
                ImGuiEx.EnumCombo("Attack 1 Direction", ref C.Attack1Direction);
                ImGuiEx.EnumCombo("Attack 2 Direction", ref C.Attack2Direction);
                ImGuiEx.EnumCombo("Attack 3 Direction", ref C.Attack3Direction);
                ImGuiEx.EnumCombo("Bind 1 Direction", ref C.Bind1Direction);
                ImGuiEx.EnumCombo("Bind 2 Direction", ref C.Bind2Direction);
                ImGuiEx.EnumCombo("Bind 3 Direction", ref C.Bind3Direction);
                ImGuiEx.EnumCombo("Ignore 1 Direction", ref C.Ignore1Direction);
                ImGuiEx.EnumCombo("Ignore 2 Direction", ref C.Ignore2Direction);
            }
            else if (C.Mode == Mode.Priority)
            {
                ImGui.Text("West");
                C.PriorityList.Draw();
                ImGui.Text("East");
            }

            ImGui.Unindent();

            ImGui.Separator();
            ImGui.Text("Bait Color:");
            ImGuiComponents.HelpMarker(
                "Change the color of the bait and the text that will be displayed on your bait.\nSetting different values makes it rainbow.");
            ImGui.Indent();
            ImGui.ColorEdit4("Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
            ImGui.SameLine();
            ImGui.ColorEdit4("Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
            ImGui.Unindent();

            ImGui.Checkbox("Show Other", ref C.ShowOther);

            ImGui.Text("Display Text:");
            ImGui.SameLine();
            var text = C.LookOutsideText.Get();
            C.LookOutsideText.ImGuiEdit(ref text);

            ImGui.Checkbox("Base Orientation is North", ref C.BaseOrientationIsNorth);
            ImGuiEx.HelpMarker("""
                               Set the reference direction.
                                enabled: The direction in which the shape formed by the yellow hourglass resembles a "Y" is considered north.
                                disabled: The direction in which the shape formed by the yellow hourglass resembles a "Y" is considered south.
                                
                               For developers: Internally, the priority is simply reversed.
                               """);
            ImGui.Unindent();
        }

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"Base Direction: {_baseDirection.ToString()}");
            ImGuiEx.EzTable("Player Data", _playerDatas.SelectMany(x => new ImGuiEx.EzTableEntry[]
            {
                new("Player Name", () => ImGuiEx.Text(x.Value.PlayerName)),
                new("Kind Fire", () => ImGuiEx.Text(x.Value.KindFire.ToString())),
                new("Number", () => ImGuiEx.Text(x.Value.Number.ToString())),
                new("Direction", () => ImGuiEx.Text(x.Value.Direction.ToString()))
            }));

            ImGuiEx.EzTable("Positions", _positions.SelectMany(x => new ImGuiEx.EzTableEntry[]
            {
                new("Direction", () => ImGuiEx.Text(x.Key.ToString())),
                new("Position", () => ImGuiEx.Text(x.Value.ToString()))
            }));

            ImGuiEx.EzTable("Hourglasses", _hourglasses.SelectMany(x => new ImGuiEx.EzTableEntry[]
            {
                new("Direction", () => ImGuiEx.Text(x.Key.ToString())),
                new("Clockwise", () => ImGuiEx.Text(x.Value.Clockwise.ToString())),
                new("OriginalDirection", () => ImGuiEx.Text(x.Value.Direction.ToString()))
            }));
        }
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5,
        uint p6, ulong targetId,
        byte replaying)
    {
        if (_state is State.None or State.End) return;
        if (command == 502)
        {
            var player = Svc.Objects.FirstOrDefault(x => x.GameObjectId == p2);
            if (player == null) PluginLog.Warning("Error: Cannot find player");

            _playerDatas[p2] = p1 switch
            {
                (uint)MarkerType.Attack1 => new PlayerData
                {
                    PlayerName = player?.Name.ToString(),
                    KindFire = KindFire.Early,
                    Number = 1,
                    Direction = SwapAsOrientation(C.Attack1Direction)
                },
                (uint)MarkerType.Attack2 => new PlayerData
                {
                    PlayerName = player?.Name.ToString(),
                    KindFire = KindFire.Early,
                    Number = 2,
                    Direction = SwapAsOrientation(C.Attack2Direction)
                },
                (uint)MarkerType.Attack3 => new PlayerData
                {
                    PlayerName = player?.Name.ToString(),
                    KindFire = KindFire.Early,
                    Number = 3,
                    Direction = SwapAsOrientation(C.Attack3Direction)
                },
                (uint)MarkerType.Bind1 => new PlayerData
                {
                    PlayerName = player?.Name.ToString(),
                    KindFire = KindFire.Late,
                    Number = 1,
                    Direction = SwapAsOrientation(C.Bind1Direction)
                },
                (uint)MarkerType.Bind2 => new PlayerData
                {
                    PlayerName = player?.Name.ToString(),
                    KindFire = KindFire.Late,
                    Number = 2,
                    Direction = SwapAsOrientation(C.Bind2Direction)
                },
                (uint)MarkerType.Bind3 => new PlayerData
                {
                    PlayerName = player?.Name.ToString(),
                    KindFire = KindFire.Late,
                    Number = 3,
                    Direction = SwapAsOrientation(C.Bind3Direction)
                },
                (uint)MarkerType.Ignore1 => new PlayerData
                {
                    PlayerName = player?.Name.ToString(),
                    KindFire = KindFire.Middle,
                    Number = 1,
                    Direction = SwapAsOrientation(C.Ignore1Direction)
                },
                (uint)MarkerType.Ignore2 => new PlayerData
                {
                    PlayerName = player?.Name.ToString(),
                    KindFire = KindFire.Middle,
                    Number = 2,
                    Direction = SwapAsOrientation(C.Ignore2Direction)
                },
                _ => _playerDatas[p2]
            };

            if (player is IPlayerCharacter playerCharacter &&
                playerCharacter.StatusList.Any(x => x.StatusId == (uint)Debuff.Blizzard))
                _playerDatas[p2].KindFire = KindFire.Blizzard;
        }
    }


    public override void OnUpdate()
    {
        if (_state is State.None or State.Start or State.End)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            return;
        }

        Controller.GetRegisteredElements().Where(x => x.Key != "Text").Each(x =>
        {
            x.Value.color = C.OtherColor.ToUint();
            x.Value.tether = false;
            x.Value.Enabled = C.ShowOther;
        });

        var myDirection = _playerDatas.FirstOrDefault(x => x.Value.PlayerName == Player.Name).Value?.Direction
            .ToString();
        if (myDirection == null) return;
        if (Controller.TryGetElementByName(myDirection, out var myElement) && myElement.offX != 0f)
        {
            myElement.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
            myElement.tether = true;
            myElement.Enabled = true;
        }

        if (_state == State.FirstFire)
        {
            GoCenter(Direction.North);
            GoOutside(Direction.NorthEast);
            GoCenter(Direction.East);
            GoCenter(Direction.SouthEast);

            if (_playerDatas.Any(x => x.Value is { KindFire: KindFire.Blizzard, Direction: Direction.South }))
                GoCenter(Direction.South, 1f);
            else
                GoOutside(Direction.South);

            GoCenter(Direction.SouthWest);
            GoCenter(Direction.West);
            GoOutside(Direction.NorthWest);
        }
        else if (_state == State.BaitEarlyHourglass)
        {
            BaitHourglass(Direction.North);

            var northEastPlayer = _playerDatas.FirstOrDefault(x => x.Value.Direction == Direction.East);
            if (northEastPlayer.Value != null && FakeParty.Get().Where(x => x.Name.ToString() == northEastPlayer.Value.PlayerName)
                .Any(x => x.StatusList.Any(y => y.StatusId == (uint)Debuff.Eruption)))
                PlaceReturnToHourglassOutside(Direction.NorthEast);
            else
                PlaceReturnToHourglassInside(Direction.NorthEast);

            var eastPlayer = _playerDatas.FirstOrDefault(x => x.Value.Direction == Direction.East);
            if (eastPlayer.Value != null && FakeParty.Get().Where(x => x.Name.ToString() == eastPlayer.Value.PlayerName)
                .Any(x => x.StatusList.Any(y => y.StatusId == (uint)Debuff.DarkWater)))
                GoNearCenter(Direction.East);
            else
                PlaceReturnToHourglass(Direction.East);
            BaitHourglass(Direction.SouthEast);

            var southPlayer = _playerDatas.FirstOrDefault(x => x.Value.Direction == Direction.East);
            if (southPlayer.Value != null && FakeParty.Get().Where(x => x.Name.ToString() == southPlayer.Value.PlayerName)
                .Any(x => x.StatusList.Any(y => y.StatusId == (uint)Debuff.Eruption)))
                PlaceReturnToHourglassOutside(Direction.South);
            else
                PlaceReturnToHourglassInside(Direction.South);

            BaitHourglass(Direction.SouthWest);

            var westPlayer = _playerDatas.FirstOrDefault(x => x.Value.Direction == Direction.West);
            if (westPlayer.Value != null && FakeParty.Get().Where(x => x.Name.ToString() == westPlayer.Value.PlayerName)
                .Any(x => x.StatusList.Any(y => y.StatusId == (uint)Debuff.DarkWater)))
                GoNearCenter(Direction.West);
            else
                PlaceReturnToHourglass(Direction.West);

            var northWestPlayer = _playerDatas.FirstOrDefault(x => x.Value.Direction == Direction.NorthWest);
            if (northWestPlayer.Value != null &&FakeParty.Get().Where(x => x.Name.ToString() == northWestPlayer.Value.PlayerName)
                .Any(x => x.StatusList.Any(y => y.StatusId == (uint)Debuff.Eruption)))
                PlaceReturnToHourglassOutside(Direction.NorthWest);
            else
                PlaceReturnToHourglassInside(Direction.NorthWest);
        }
        else if (_state == State.SecondFire)
        {
            GoCenter(Direction.North);
            GoCenter(Direction.NorthEast);
            GoOutside(Direction.East);
            GoCenter(Direction.SouthEast);
            GoCenter(Direction.South);
            GoCenter(Direction.SouthWest);
            GoOutside(Direction.West);
            GoCenter(Direction.NorthWest);
        }
        else if (_state == State.BaitNoTetherHourglass)
        {
            GoNearCenter(Direction.North);
            BaitHourglass(Direction.NorthEast);
            GoCenter(Direction.East);
            GoNearCenter(Direction.SouthEast);
            BaitHourglass(Direction.South);
            GoNearCenter(Direction.SouthWest);
            GoCenter(Direction.West);
            BaitHourglass(Direction.NorthWest);
        }
        else if (_state == State.ThirdFire)
        {
            if (_playerDatas.Any(x => x.Value is { KindFire: KindFire.Blizzard, Direction: Direction.North }))
                GoCenter(Direction.North, 1f);
            else
                GoOutside(Direction.North);

            GoCenter(Direction.NorthEast);
            GoCenter(Direction.East);
            GoOutside(Direction.SouthEast);
            GoCenter(Direction.South);
            GoOutside(Direction.SouthWest);
            GoCenter(Direction.West);
            GoCenter(Direction.NorthWest);
        }
        else if (_state == State.BaitLateHourglass)
        {
            GoCenter(Direction.North);
            GoCenter(Direction.NorthEast);
            BaitHourglass(Direction.East);
            GoCenter(Direction.SouthEast);
            GoCenter(Direction.South);
            GoCenter(Direction.SouthWest);
            BaitHourglass(Direction.West);
            GoCenter(Direction.NorthWest);
            if (Controller.TryGetElementByName("Text", out var element))
            {
                element.Enabled = true;
                element.overlayText = C.LookOutsideText.Get();
            }
        }

        if (_showSinboundMeltdown) ShowSinboundMeltdown();
    }

    private static KindFire GetKindFire(StatusList statuses)
    {
        foreach (var status in statuses)
            switch (status.StatusId)
            {
                case (uint)Debuff.Fire:
                {
                    return status.RemainingTime switch
                    {
                        < 15 => KindFire.Early,
                        < 25 => KindFire.Middle,
                        _ => KindFire.Late
                    };
                }
                case (uint)Debuff.Blizzard:
                    return KindFire.Blizzard;
            }

        DuoLog.Warning("Error: Cannot determine fire debuff");
        return KindFire.Early;
    }

    private enum Debuff : uint
    {
        Holy = 0x996,
        Fire = 0x997,
        ShadowEye = 0x998,
        Eruption = 0x99C,
        DarkWater = 0x99D,
        Blizzard = 0x99E,
        Return = 0x9A0
    }

    private enum MarkerType : uint
    {
        Attack1 = 0,
        Attack2 = 1,
        Attack3 = 2,
        Bind1 = 5,
        Bind2 = 6,
        Bind3 = 7,
        Ignore1 = 8,
        Ignore2 = 9
    }

    private enum State
    {
        None,
        Start,
        FirstFire,
        BaitEarlyHourglass,
        SecondFire,
        BaitNoTetherHourglass,
        ThirdFire,
        BaitLateHourglass,
        End
    }

    private record Hourglass
    {
        public Clockwise Clockwise { get; init; }
        public Direction Direction { get; init; }
    }

    private record PlayerData
    {
        public string? PlayerName { init; get; }
        public KindFire KindFire { set; get; }
        public int Number { init; get; }
        public Direction? Direction { init; get; }

        public override int GetHashCode()
        {
            return PlayerName?.GetHashCode() ?? 0;
        }
    }

    private class Config : IEzConfig
    {
        public Direction Attack1Direction = Direction.NorthWest;
        public Direction Attack2Direction = Direction.NorthEast;
        public Direction Attack3Direction = Direction.South;

        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();

        public bool BaseOrientationIsNorth = true;
        public Direction Bind1Direction = Direction.SouthWest;
        public Direction Bind2Direction = Direction.SouthEast;
        public Direction Bind3Direction = Direction.North;
        public string BlizzardCommand = "";
        public string EarlyFireCommand = "";
        public Direction Ignore1Direction = Direction.West;
        public Direction Ignore2Direction = Direction.East;
        public string LateFireCommand = "";
        public InternationalString LookOutsideText = new() { En = "Look Outside", Jp = "外を見ろ" };
        public string MiddleFireCommand = "";

        public Mode Mode = Mode.Priority;

        public Vector4 OtherColor = 0xFF0000FF.ToVector4();
        public PriorityData PriorityList = new();

        public bool ShouldUseRandomWait = true;
        public bool ShowOther;
        public Vector2 WaitRange = new(0.5f, 1.5f);
    }
}
