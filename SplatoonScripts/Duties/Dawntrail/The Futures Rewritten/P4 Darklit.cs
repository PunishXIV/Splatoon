using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P4_Darklit : SplatoonScript
{
    public enum BoxSwapType
    {
        NorthWestAndSouthWest,
        NorthEastAndSouthEast
    }

    private enum Direction
    {
        North = 0,
        NorthEast = 45,
        East = 90,
        SouthEast = 135,
        South = 180,
        SouthWest = 225,
        West = 270,
        NorthWest = 315
    }

    public enum HourglassSwapType
    {
        NorthWestAndSouthEast,
        NorthEastAndSouthWest,
        Clockwise,
        CounterClockwise
    }

    public enum Mode
    {
        Vertical,
        Horizontal
    }

    private enum MoveType
    {
        Straight,
        Hourglass,
        Box
    }

    private enum Role
    {
        Dps,
        TankAndHealer
    }

    private enum State
    {
        None,
        Start,
        Tower,
        Split,
        Stack,
        End
    }

    // 40228 : East
    private uint[] _holyWingIds = [40227, 40228];

    private MoveType? _moveType;

    private Dictionary<ulong, PlayerData> _players = new();

    private State _state = State.None;

    private const uint WaterId = 0x99D;
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(2, "Garume");
    private Config C => Controller.GetConfig<Config>();

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 40239) _state = State.Start;
        if (_holyWingIds.Contains(castId))
        {
            var x = castId == _holyWingIds[0] ? 106.5f : 93.5f;

            if (Controller.TryGetElementByName("StackBaitNorth", out var northElement)) northElement.refX = x;

            if (Controller.TryGetElementByName("StackBaitSouth", out var southElement)) southElement.refX = x;

            if (Controller.TryGetElementByName("Split", out var splitElement)) splitElement.Enabled = false;
        }
    }

    public override void OnSetup()
    {
        foreach (var direction in Enum.GetValues<Direction>())
        {
            Vector3? position = direction switch
            {
                Direction.North => new Vector3(100, 0, 92),
                Direction.NorthEast => new Vector3(103, 0, 99),
                Direction.SouthEast => new Vector3(103, 0, 101),
                Direction.South => new Vector3(100, 0, 108),
                Direction.SouthWest => new Vector3(97, 0, 101),
                Direction.NorthWest => new Vector3(97, 0, 99),
                _ => null
            };

            if (position == null) continue;

            Controller.RegisterElement(direction.ToString(), new Element(0)
            {
                thicc = 6f,
                refX = position.Value.X,
                refY = position.Value.Z,
                refZ = position.Value.Y,
                radius = direction is Direction.North or Direction.South ? 4f : 0.5f
            });
        }


        Controller.RegisterElement("Split", new Element(0)
        {
            radius = 0f,
            overlayText = "<< SPLIT >>",
            overlayFScale = 4f,
            overlayVOffset = 3f,
            refActorComparisonType = 5,
            refX = 100f,
            refY = 100f,
            color = EColor.BlueBright.ToUint()
        });

        Controller.RegisterElement("Stack", new Element(0)
        {
            radius = 0f,
            overlayText = "<< STACK >>",
            overlayFScale = 4f,
            overlayVOffset = 3f,
            refActorComparisonType = 5,
            refX = 100f,
            refY = 100f,
            color = EColor.BlueBright.ToUint()
        });

        Controller.RegisterElement("StackBaitNorth", new Element(0)
        {
            radius = 1f,
            thicc = 6f,
            refY = 87f
        });

        Controller.RegisterElement("StackBaitSouth", new Element(0)
        {
            radius = 1f,
            thicc = 6f,
            refY = 113f
        });
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action != null && _state == State.Tower && set.Action.Value.RowId == 40190)
        {
            foreach (var direction in Enum.GetValues<Direction>())
                if (Controller.TryGetElementByName(direction.ToString(), out var element))
                    element.Enabled = false;
            if (Controller.TryGetElementByName("Split", out var splitElement)) splitElement.Enabled = true;
            _state = State.Split;
        }

        if (set.Action != null && _state == State.Split && set.Action.Value.RowId == 40289)
        {
            if (Controller.TryGetElementByName("Split", out var splitElement)) splitElement.Enabled = false;
            if (Controller.TryGetElementByName("Stack", out var stackElement)) stackElement.Enabled = true;
            _state = State.Stack;
        }

        if (set.Action != null && _state == State.Stack && _holyWingIds.Contains(set.Action.Value.RowId)) _state = State.End;
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_state != State.Start || source.GetObject() is not IPlayerCharacter sourcePlayer ||
            target.GetObject() is not IPlayerCharacter targetPlayer) return;
        var priority = C.PriorityData.GetPlayers(x => true).IndexOf(x => x.Name == sourcePlayer.Name.ToString());
        _players[source] = new PlayerData
        {
            Name = sourcePlayer.Name.ToString(),
            IsInkling = true,
            LinkToRole = targetPlayer.GetRole() == CombatRole.DPS ? Role.Dps : Role.TankAndHealer,
            Id = source,
            LinkTo = target,
            Role = sourcePlayer.GetRole() == CombatRole.DPS ? Role.Dps : Role.TankAndHealer,
            Priority = priority,
            HasWater = sourcePlayer.StatusList.Any(x => x.StatusId == WaterId)
        };

        if (_players.Count == 4)
        {
            var otherPlayers = FakeParty.Get().Where(x => !_players.ContainsKey(x.GameObjectId)).ToList();
            foreach (var otherPlayer in otherPlayers)
                _players[otherPlayer.GameObjectId] = new PlayerData
                {
                    Name = otherPlayer.Name.ToString(),
                    IsInkling = false,
                    LinkToRole = otherPlayer.GetRole() == CombatRole.DPS ? Role.Dps : Role.TankAndHealer,
                    Id = otherPlayer.GameObjectId,
                    Role = otherPlayer.GetRole() == CombatRole.DPS ? Role.Dps : Role.TankAndHealer,
                    Priority = C.PriorityData.GetPlayers(x => true)
                        .IndexOf(x => x.Name == otherPlayer.Name.ToString()),
                    HasWater = otherPlayer.StatusList.Any(x => x.StatusId == WaterId)
                };
            if (C.Mode == Mode.Vertical)
            {
                var left = _players.Values.Where(x => x is { Priority: < 4, IsInkling: true })
                    .OrderBy(x => x.Priority).ToList();
                var right = _players.Values.Where(x => x is { Priority: >= 4, IsInkling: true })
                    .OrderBy(x => x.Priority).ToList();
                var isSameRole = _players.Where(x => x.Value.IsInkling)
                    .Any(player => player.Value.Role == player.Value.LinkToRole);

                if ((_players[left[0].LinkTo].Name == right[0].Name ||
                     _players[right[0].LinkTo].Name == left[0].Name) && isSameRole)
                    _moveType = MoveType.Box;
                else if (!isSameRole)
                    _moveType = MoveType.Hourglass;
                else
                    _moveType = MoveType.Straight;


                switch (_moveType)
                {
                    case MoveType.Straight:
                        _players[left[0].Id].Direction = Direction.North;
                        _players[left[1].Id].Direction = Direction.South;
                        _players[right[0].Id].Direction = Direction.North;
                        _players[right[1].Id].Direction = Direction.South;
                        break;
                    case MoveType.Hourglass when C.HourglassSwapType == HourglassSwapType.NorthEastAndSouthWest:
                    case MoveType.Hourglass when C.HourglassSwapType == HourglassSwapType.Clockwise:
                        _players[left[0].Id].Direction = Direction.North;
                        _players[left[1].Id].Direction = Direction.North;
                        _players[right[0].Id].Direction = Direction.South;
                        _players[right[1].Id].Direction = Direction.South;
                        break;
                    case MoveType.Hourglass when C.HourglassSwapType == HourglassSwapType.NorthWestAndSouthEast:
                    case MoveType.Hourglass when C.HourglassSwapType == HourglassSwapType.CounterClockwise:
                        _players[left[0].Id].Direction = Direction.South;
                        _players[left[1].Id].Direction = Direction.South;
                        _players[right[0].Id].Direction = Direction.North;
                        _players[right[1].Id].Direction = Direction.North;
                        break;
                    case MoveType.Box when C.BoxSwapType == BoxSwapType.NorthWestAndSouthWest:
                        _players[left[0].Id].Direction = Direction.South;
                        _players[left[1].Id].Direction = Direction.North;
                        _players[right[0].Id].Direction = Direction.North;
                        _players[right[1].Id].Direction = Direction.South;
                        break;
                    case MoveType.Box when C.BoxSwapType == BoxSwapType.NorthEastAndSouthEast:
                        _players[left[0].Id].Direction = Direction.North;
                        _players[left[1].Id].Direction = Direction.South;
                        _players[right[0].Id].Direction = Direction.South;
                        _players[right[1].Id].Direction = Direction.North;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var otherLeft = _players.Values.Where(x => x is { Priority: < 4, IsInkling: false })
                    .OrderBy(x => x.Priority).ToList();
                var otherRight = _players.Values.Where(x => x is { Priority: >= 4, IsInkling: false })
                    .OrderBy(x => x.Priority).ToList();
                _players[otherLeft[0].Id].Direction = Direction.NorthWest;
                _players[otherRight[0].Id].Direction = Direction.NorthEast;
                _players[otherLeft[1].Id].Direction = Direction.SouthWest;
                _players[otherRight[1].Id].Direction = Direction.SouthEast;
            }
            else
            {
                var up = _players.Values.Where(x => x is { Priority: < 4, IsInkling: true })
                    .OrderBy(x => x.Priority).ToList();
                var down = _players.Values.Where(x => x is { Priority: >= 4, IsInkling: true })
                    .OrderBy(x => x.Priority).ToList();
                var isSameRole = _players.Where(x => x.Value.IsInkling)
                    .Any(player => player.Value.Role == player.Value.LinkToRole);

                if ((_players[up[0].LinkTo].Name == down[0].Name ||
                     _players[down[0].LinkTo].Name == up[0].Name) && isSameRole)
                    _moveType = MoveType.Box;
                else if (isSameRole)
                    _moveType = MoveType.Hourglass;
                else
                    _moveType = MoveType.Straight;


                switch (_moveType)
                {
                    case MoveType.Straight:
                        _players[up[0].Id].Direction = Direction.North;
                        _players[up[1].Id].Direction = Direction.North;
                        _players[down[0].Id].Direction = Direction.South;
                        _players[down[1].Id].Direction = Direction.South;
                        break;
                    case MoveType.Hourglass when C.HourglassSwapType == HourglassSwapType.NorthEastAndSouthWest:
                    case MoveType.Hourglass when C.HourglassSwapType == HourglassSwapType.Clockwise:
                        _players[up[0].Id].Direction = Direction.North;
                        _players[up[1].Id].Direction = Direction.South;
                        _players[down[0].Id].Direction = Direction.North;
                        _players[down[1].Id].Direction = Direction.South;
                        break;
                    case MoveType.Hourglass when C.HourglassSwapType == HourglassSwapType.NorthWestAndSouthEast:
                    case MoveType.Hourglass when C.HourglassSwapType == HourglassSwapType.CounterClockwise:
                        _players[up[0].Id].Direction = Direction.South;
                        _players[up[1].Id].Direction = Direction.North;
                        _players[down[0].Id].Direction = Direction.South;
                        _players[down[1].Id].Direction = Direction.North;
                        break;
                    case MoveType.Box when C.BoxSwapType == BoxSwapType.NorthWestAndSouthWest:
                        _players[up[0].Id].Direction = Direction.South;
                        _players[down[1].Id].Direction = Direction.North;
                        _players[up[0].Id].Direction = Direction.North;
                        _players[down[1].Id].Direction = Direction.South;
                        break;
                    case MoveType.Box when C.BoxSwapType == BoxSwapType.NorthEastAndSouthEast:
                        _players[up[0].Id].Direction = Direction.North;
                        _players[up[1].Id].Direction = Direction.South;
                        _players[down[0].Id].Direction = Direction.South;
                        _players[down[1].Id].Direction = Direction.North;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var otherUp = _players.Values.Where(x => x is { Priority: < 4, IsInkling: false })
                    .OrderBy(x => x.Priority).ToList();
                var otherDown = _players.Values.Where(x => x is { Priority: >= 4, IsInkling: false })
                    .OrderBy(x => x.Priority).ToList();
                _players[otherUp[0].Id].Direction = Direction.NorthWest;
                _players[otherUp[1].Id].Direction = Direction.NorthEast;
                _players[otherDown[0].Id].Direction = Direction.SouthWest;
                _players[otherDown[1].Id].Direction = Direction.SouthEast;
            }


            var noTetherAndHasWater =
                _players.Values.Where(x => x is { IsInkling: false, HasWater: true }).ToList();
            if (noTetherAndHasWater.Count == 1)
            {
                var player = noTetherAndHasWater.First();
                var otherPlayer = _players.Values.First(x => x is { IsInkling: true, HasWater: true });
                var playerAboutDirection = player.Direction switch
                {
                    Direction.North => Direction.North,
                    Direction.NorthEast => Direction.North,
                    Direction.SouthEast => Direction.South,
                    Direction.South => Direction.South,
                    Direction.SouthWest => Direction.South,
                    Direction.NorthWest => Direction.North,
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (playerAboutDirection == otherPlayer.Direction)
                {
                    var otherPlayerDirection = player.Direction switch
                    {
                        Direction.NorthEast => Direction.SouthEast,
                        Direction.SouthEast => Direction.NorthEast,
                        Direction.SouthWest => Direction.NorthWest,
                        Direction.NorthWest => Direction.SouthWest,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    var anotherPlayer = _players.Values.First(x => x.Direction == otherPlayerDirection);
                    _players[anotherPlayer.Id].Direction = player.Direction;
                    _players[player.Id].Direction = otherPlayerDirection;
                }
            }

            _state = State.Tower;
        }
    }


    public override void OnUpdate()
    {
        if (_state is State.None or State.End)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            return;
        }

        if (_state == State.Tower)
        {
            var myDirection = _players.Values.First(x => x.Name == Player.Name).Direction;
            foreach (var direction in Enum.GetValues<Direction>())
                if (Controller.TryGetElementByName(direction.ToString(), out var element))
                {
                    if (myDirection == direction)
                    {
                        element.Enabled = true;
                        element.tether = true;
                        element.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
                    }
                    else
                    {
                        element.tether = false;
                        element.color = EColor.RedBright.ToUint();
                        element.Enabled = C.ShowOther;
                    }
                }
        }
        else if (_state == State.Stack)
        {
            var myDirection = _players.Values.First(x => x.Name == Player.Name).Direction;
            var elementName = myDirection switch
            {
                Direction.North => "StackBaitNorth",
                Direction.NorthEast => "StackBaitNorth",
                Direction.NorthWest => "StackBaitNorth",
                Direction.South => "StackBaitSouth",
                Direction.SouthEast => "StackBaitSouth",
                Direction.SouthWest => "StackBaitSouth",
                _ => throw new ArgumentOutOfRangeException()
            };
            if (Controller.TryGetElementByName(elementName, out var northElement))
            {
                northElement.Enabled = true;
                northElement.tether = true;
                northElement.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
            }
        }
    }


    public override void OnReset()
    {
        _players.Clear();
        _state = State.None;
        _moveType = null;
        Controller.GetRegisteredElements().Each(x => x.Value.tether = false);
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("General"))
        {
            ImGuiEx.EnumCombo("Mode", ref C.Mode);
            ImGuiEx.Text("Priority");
            ImGuiEx.Text(C.Mode == Mode.Vertical
                ? "NorthWest -> SouthWest -> NorthEast -> SouthEast"
                : "NorthWest -> NorthEast -> SouthWest -> SouthEast");
            C.PriorityData.Draw();
            ImGuiEx.EnumCombo("Box Swap Type", ref C.BoxSwapType);
            ImGuiEx.EnumCombo("Hourglass Swap Type", ref C.HourglassSwapType);
            ImGui.Checkbox("Show Other", ref C.ShowOther);
        }

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"Move Type: {_moveType}");
            ImGuiEx.EzTable("Player Data", _players.SelectMany(x => new ImGuiEx.EzTableEntry[]
            {
                new("Player Name", () => ImGuiEx.Text(x.Value.Name)),
                new("Direction", () => ImGuiEx.Text(x.Value.Direction.ToString())),
                new("ID", () => ImGuiEx.Text(x.Value.Id.ToString())),
                new("Is Inkling", () => ImGuiEx.Text(x.Value.IsInkling.ToString())),
                new("Link To", () => ImGuiEx.Text(x.Value.LinkTo.ToString())),
                new("Link To Role", () => ImGuiEx.Text(x.Value.LinkToRole.ToString())),
                new("Role", () => ImGuiEx.Text(x.Value.Role.ToString())),
                new("Priority", () => ImGuiEx.Text(x.Value.Priority.ToString())),
                new("Has Water", () => ImGuiEx.Text(x.Value.HasWater.ToString()))
            }));
        }
    }

    private record PlayerData
    {
        public Direction Direction;
        public bool HasWater;
        public ulong Id;
        public bool IsInkling;
        public ulong LinkTo;
        public Role LinkToRole;
        public string Name;
        public int Priority = -1;
        public Role Role;
    }

    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public BoxSwapType BoxSwapType = BoxSwapType.NorthWestAndSouthWest;
        public HourglassSwapType HourglassSwapType = HourglassSwapType.Clockwise;
        public Mode Mode = Mode.Vertical;
        public PriorityData PriorityData = new();
        public bool ShowOther = true;
    }
}
