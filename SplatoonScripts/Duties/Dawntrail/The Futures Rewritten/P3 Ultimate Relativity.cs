using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
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

    public enum Debuff : uint
    {
        Holy = 0x996,
        Fire = 0x997,
        ShadowEye = 0x998,
        Eruption = 0x99C,
        DarkWater = 0x99D,
        Blizzard = 0x99E,
        Return = 0x9A0
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

    public enum MarkerType : uint
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

    public enum Mode
    {
        Marker,
        Priority
    }

    public enum State
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

    private readonly List<IBattleChara> _earlyHourglassList = [];

    private readonly Dictionary<Direction, Hourglass> _hourglasses = new();
    private readonly List<IBattleChara> _lateHourglassList = [];

    private readonly Dictionary<ulong, PlayerData> _playerDatas = [];

    private readonly Dictionary<Direction, Vector2> _positions = new();

    private Direction? _baseDirection;

    private State _state = State.None;

    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(2, "Garume");

    public Config C => Controller.GetConfig<Config>();

    public override void OnStartingCast(uint source, uint castId)
    {
        if (_state == State.End) return;
        if (castId == 40266) _state = State.Start;
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
                _baseDirection = (Direction)((int)GetDirection(baseOrientationHourglass.Position) + 90);
            }
        }
    }

    public Direction GetDirection(Vector3 position)
    {
        var center = new Vector3(100, 0, 100);
        var isNorth = position.Z < center.Z;
        var isEast = position.X > center.X;
        var isSouth = position.Z > center.Z;
        var isWest = position.X < center.X;

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
                }
            }

        if (_state == State.End) return;
        if (_baseDirection == null) return;
        if (Status.StatusId == 2970)
        {
            var clockwise = Status.Param == 348 ? Clockwise.Clockwise : Clockwise.CounterClockwise;
            var position = sourceId.GetObject()?.Position ?? throw new InvalidOperationException();
            var basedDirection = ((int)GetDirection(position) - (int)_baseDirection.Value + 360) % 360;

            _hourglasses[(Direction)basedDirection] = new Hourglass { Clockwise = clockwise };

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

    public void BaitHourglass(Direction direction)
    {
        if (_baseDirection == null) return;
        var basedDirection = (Direction)(((int)direction + (int)_baseDirection.Value) % 360);
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
            element.SetOffPosition(position.ToVector3(0f));
    }

    public void GoCenter(Direction direction)
    {
        if (_baseDirection == null) return;
        var center = new Vector2(100f, 100f);
        _positions[direction] = center;
        if (Controller.TryGetElementByName(direction.ToString(), out var element))
            element.SetOffPosition(center.ToVector3(0f));
    }

    public void GoOutside(Direction direction)
    {
        var radius = 18f;
        if (_baseDirection == null) return;
        var basedDirection = (Direction)(((int)direction + (int)_baseDirection.Value) % 360);
        var center = new Vector2(100, 100);
        var position = new Vector2(
            center.X + radius * MathF.Cos(MathF.PI * (int)basedDirection / 180),
            center.Y + radius * MathF.Sin(MathF.PI * (int)basedDirection / 180)
        );
        _positions[direction] = position;
        if (Controller.TryGetElementByName(direction.ToString(), out var element))
            element.SetOffPosition(position.ToVector3(0f));
    }

    public void PlaceReturnToHourglass(Direction direction)
    {
        if (_baseDirection == null) return;
        var basedDirection = (Direction)(((int)direction + (int)_baseDirection.Value) % 360);
        var radius = 10f;
        var center = new Vector2(100, 100);
        var position = new Vector2(
            center.X + radius * MathF.Cos(MathF.PI * (int)basedDirection / 180),
            center.Y + radius * MathF.Sin(MathF.PI * (int)basedDirection / 180)
        );
        _positions[direction] = position;
        if (Controller.TryGetElementByName(direction.ToString(), out var element))
            element.SetOffPosition(position.ToVector3(0f));
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
            overlayFScale = 5f,
            overlayVOffset = 5f,
            radius = 0f
        });
    }

    public override void OnReset()
    {
        _state = State.None;
        _playerDatas.Clear();
        _hourglasses.Clear();
        _baseDirection = null;
        _earlyHourglassList.Clear();
        _lateHourglassList.Clear();

        if (Controller.TryGetElementByName("Text", out var element)) element.Enabled = false;
        Controller.GetRegisteredElements().Each(x => x.Value.tether = false);
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("General"))
        {
            ImGui.Indent();
            ImGuiEx.EnumCombo("Mode", ref C.Mode);
            ImGui.Indent();
            if (C.Mode == Mode.Marker)
            {
                ImGui.InputText("Early Fire Command", ref C.EarlyFireCommand, 100);
                ImGui.InputText("Middle Fire Command", ref C.MiddleFireCommand, 100);
                ImGui.InputText("Late Fire Command", ref C.LateFireCommand, 100);
                ImGui.InputText("Blizzard Command", ref C.BlizzardCommand, 100);
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
                ImGuiEx.Text(EColor.RedBright,"No implementation yet");
                ImGuiEx.Text(EColor.RedBright,"Please use Marker mode for now");
                // C.PriorityList.Draw();
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
                new("Clockwise", () => ImGuiEx.Text(x.Value.Clockwise.ToString()))
            }));
        }
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5,
        uint p6, ulong targetId,
        byte replaying)
    {
        if (_state is not (State.None or State.End) && _playerDatas.Count < 8)
            if (command == 502)
            {
                var player = Svc.Objects.FirstOrDefault(x => x.GameObjectId == p2);
                if (player == null) PluginLog.Warning("Error: Cannot find player");
                switch (p1)
                {
                    case (uint)MarkerType.Attack1:
                        _playerDatas.Add(p2, new PlayerData
                        {
                            PlayerName = player?.Name.ToString(), KindFire = KindFire.Early, Number = 1,
                            Direction = C.Attack1Direction
                        });
                        break;
                    case (uint)MarkerType.Attack2:
                        _playerDatas.Add(p2, new PlayerData
                        {
                            PlayerName = player?.Name.ToString(), KindFire = KindFire.Early, Number = 2,
                            Direction = C.Attack2Direction
                        });
                        break;
                    case (uint)MarkerType.Attack3:
                        _playerDatas.Add(p2, new PlayerData
                        {
                            PlayerName = player?.Name.ToString(), KindFire = KindFire.Early, Number = 3,
                            Direction = C.Attack3Direction
                        });
                        break;
                    case (uint)MarkerType.Bind1:
                        _playerDatas.Add(p2, new PlayerData
                        {
                            PlayerName = player?.Name.ToString(), KindFire = KindFire.Late, Number = 1,
                            Direction = C.Bind1Direction
                        });
                        break;
                    case (uint)MarkerType.Bind2:
                        _playerDatas.Add(p2, new PlayerData
                        {
                            PlayerName = player?.Name.ToString(), KindFire = KindFire.Late, Number = 2,
                            Direction = C.Bind2Direction
                        });
                        break;
                    case (uint)MarkerType.Bind3:
                        _playerDatas.Add(p2, new PlayerData
                        {
                            PlayerName = player?.Name.ToString(), KindFire = KindFire.Late, Number = 3,
                            Direction = C.Bind3Direction
                        });
                        break;
                    case (uint)MarkerType.Ignore1:
                        _playerDatas.Add(p2, new PlayerData
                        {
                            PlayerName = player?.Name.ToString(), KindFire = KindFire.Middle, Number = 1,
                            Direction = C.Ignore1Direction
                        });
                        break;
                    case (uint)MarkerType.Ignore2:
                        _playerDatas.Add(p2, new PlayerData
                        {
                            PlayerName = player?.Name.ToString(), KindFire = KindFire.Middle, Number = 2,
                            Direction = C.Ignore2Direction
                        });
                        break;
                }
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
            x.Value.Enabled = C.ShowOther;
        });

        var myDirection = _playerDatas.First(x => x.Value.PlayerName == Player.Name).Value.Direction.ToString();
        if (myDirection == null) return;
        if (Controller.TryGetElementByName(myDirection!, out var myElement) && myElement.offX != 0f)
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
            GoOutside(Direction.South);
            GoCenter(Direction.SouthWest);
            GoCenter(Direction.West);
            GoOutside(Direction.NorthWest);
        }
        else if (_state == State.BaitEarlyHourglass)
        {
            BaitHourglass(Direction.North);
            PlaceReturnToHourglass(Direction.NorthEast);
            GoCenter(Direction.East);
            BaitHourglass(Direction.SouthEast);
            PlaceReturnToHourglass(Direction.South);
            BaitHourglass(Direction.SouthWest);
            PlaceReturnToHourglass(Direction.West);
            PlaceReturnToHourglass(Direction.NorthWest);
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
            GoCenter(Direction.North);
            BaitHourglass(Direction.NorthEast);
            GoCenter(Direction.East);
            GoCenter(Direction.SouthEast);
            BaitHourglass(Direction.South);
            GoCenter(Direction.SouthWest);
            GoCenter(Direction.West);
            BaitHourglass(Direction.NorthWest);
        }
        else if (_state == State.ThirdFire)
        {
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
    }

    public KindFire GetKindFire(StatusList statuses)
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

    public record Hourglass
    {
        public Clockwise Clockwise { get; init; }
    }

    public record PlayerData
    {
        public string? PlayerName { init; get; }
        public KindFire KindFire { init; get; }
        public int Number { init; get; }
        public Direction? Direction { init; get; }

        public override int GetHashCode()
        {
            return PlayerName?.GetHashCode() ?? 0;
        }
    }

    public class Config : IEzConfig
    {
        public Direction Attack1Direction = Direction.NorthWest;
        public Direction Attack2Direction = Direction.NorthEast;
        public Direction Attack3Direction = Direction.South;

        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
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

        public Mode Mode = Mode.Marker;

        public Vector4 OtherColor = 0xFF0000FF.ToVector4();
        public PriorityData PriorityList = new();
        public bool ShowOther;
    }
}