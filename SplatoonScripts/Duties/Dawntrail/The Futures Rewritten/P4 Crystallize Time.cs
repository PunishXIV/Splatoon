using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
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
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P4_Crystallize_Time : SplatoonScript
{
    public enum Debuff : uint
    {
        Red = 0xCBF,
        Blue = 0xCC0,
        Holy = 0x996,
        Eruption = 0x99C,
        Water = 0x99D,
        Blizzard = 0x99E,
        Aero = 0x99F,
        Quietus = 0x104E,
        Return = 0x1070
    }

    public enum Direction
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


    public enum HitTiming
    {
        Early,
        Late
    }

    public enum MarkerType : uint
    {
        Attack1 = 0,
        Attack2 = 1,
        Attack3 = 2,
        Attack4 = 3
    }

    public enum MoveType
    {
        RedBlizzardWest,
        RedBlizzardEast,
        RedAeroWest,
        RedAeroEast,
        BlueBlizzard,
        BlueHoly,
        BlueWater,
        BlueEruption
    }

    public enum State
    {
        None,
        Start,
        PreSplit,
        BurnYellowHourglass,
        HitDragonAndRed,
        DebuffExpire,
        BurnHourglass,
        BurnPurpleHourglass,
        HitDragonAndAero,
        CorrectCleanse,
        PlaceReturn,
        Split,
        End
    }

    public enum WaveStack
    {
        WestTank,
        EastTank,
        West,
        East
    }

    private readonly Vector2 _center = new(100, 100);

    private readonly List<IBattleChara> _earlyHourglassList = [];
    private readonly List<IBattleChara> _lateHourglassList = [];

    private readonly Dictionary<ulong, PlayerData> _players = new();

    public readonly IEnumerable<uint> AllDebuffIds = Enum.GetValues<Debuff>().Cast<uint>();

    private Direction? _baseDirection = Direction.North;

    private int _burnHourglassCount;

    private Direction _debugDirection1 = Direction.North;
    private Direction _debugDirection2 = Direction.North;

    private Direction? _firstWaveDirection;

    private Direction? _lateHourglassDirection;
    private Direction? _secondWaveDirection;

    private State _state = State.None;
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(7, "Garume");

    private Config C => Controller.GetConfig<Config>();

    public static IBattleNpc? WestDragon => Svc.Objects.Where(x => x is { DataId: 0x45AC, Position.X: <= 100 })
        .Select(x => x as IBattleNpc).First();

    public static IBattleNpc? EastDragon => Svc.Objects.Where(x => x is { DataId: 0x45AC, Position.X: > 100 })
        .Select(x => x as IBattleNpc).First();

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 40240) _state = State.Start;

        if (_state is State.None or State.End) return;
        if (castId == 40251 && source.GetObject() is { } sourceObject)
        {
            var direction = GetDirection(sourceObject.Position);
            if (_firstWaveDirection == null)
                _firstWaveDirection = direction;
            else
                _secondWaveDirection = direction;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_state is State.None or State.End) return;
        if (set.Action is null) return;
        switch (set.Action.Value.RowId)
        {
            case 40299:
            {
                _burnHourglassCount++;
                _state = _burnHourglassCount switch
                {
                    2 => State.HitDragonAndRed,
                    4 => C.HitTiming == HitTiming.Late ? State.BurnPurpleHourglass : State.HitDragonAndAero,
                    6 => C.HitTiming == HitTiming.Late ? State.HitDragonAndAero : _state,
                    _ => _state
                };
                break;
            }
            case 40280:
            {
                if (_state == State.HitDragonAndRed && Player.Status.Any(x => x.StatusId != (uint)Debuff.Red))
                    _state = State.BurnHourglass;
                if (_state == State.HitDragonAndAero && Player.Status.Any(x => x.StatusId != (uint)Debuff.Blue))
                    _state = State.BurnPurpleHourglass;
                break;
            }
            case 40289:
            {
                if (_state == State.Split)
                    _state = State.End;
                break;
            }
        }
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status Status)
    {
        if (sourceId.GetObject() is not IPlayerCharacter player) return;

        if (_state == State.HitDragonAndAero)
            if (FakeParty.Get().All(x => x.StatusList.All(y => y.StatusId != (uint)Debuff.Red)))
                _state = State.CorrectCleanse;

        if (_state == State.CorrectCleanse)
            if (FakeParty.Get().All(x => x.StatusList.All(y => y.StatusId != (uint)Debuff.Blue)))
                _state = State.PlaceReturn;

        if (_state == State.PlaceReturn)
            if (FakeParty.Get().All(x => x.StatusList.All(y => y.StatusId != (uint)Debuff.Return)))
                _state = State.Split;
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (_state is State.None or State.End) return;
        if (vfxPath == "vfx/common/eff/dk02ht_zan0m.avfx" &&
            target.GetObject() is IBattleNpc piece &&
            _baseDirection == null)
        {
            _baseDirection = GetDirection(piece.Position);
            if (_state == State.PreSplit) _state = State.BurnYellowHourglass;
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_state is State.None or State.End) return;
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

            if (_lateHourglassList.Count == 2 && _earlyHourglassList.Count == 2)
                _lateHourglassDirection = GetDirection(_lateHourglassList[0].Position);
        }
    }

    public Direction GetDirection(Vector3 position)
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

    public override void OnGainBuffEffect(uint sourceId, Status Status)
    {
        if (_state == State.Start && sourceId.GetObject() is IPlayerCharacter player)
        {
            var debuffs = player.StatusList.Where(x => AllDebuffIds.Contains(x.StatusId));

            if (_players.All(x => x.Key != player.GameObjectId))
                _players[player.GameObjectId] = new PlayerData
                {
                    PlayerName = player.Name.ToString()
                };

            foreach (var debuff in debuffs)
                switch (debuff.StatusId)
                {
                    case (uint)Debuff.Red:
                        _players[player.GameObjectId].Color = Debuff.Red;
                        break;
                    case (uint)Debuff.Blue:
                        _players[player.GameObjectId].Color = Debuff.Blue;
                        break;
                    case (uint)Debuff.Quietus:
                        _players[player.GameObjectId].HasQuietus = true;
                        break;
                    case (uint)Debuff.Return:
                        break;
                    default:
                        _players[player.GameObjectId].Debuff = (Debuff)debuff.StatusId;
                        break;
                }


            if (_players.All(x => x.Value.HasDebuff))
            {
                var redBlizzards = C.PriorityData
                    .GetPlayers(x => _players.First(y => y.Value.PlayerName == x.Name).Value is
                        { Color: Debuff.Red, Debuff: Debuff.Blizzard }
                    );

                if (redBlizzards != null)
                {
                    _players[redBlizzards[0].IGameObject.GameObjectId].MoveType = MoveType.RedBlizzardWest;
                    _players[redBlizzards[1].IGameObject.GameObjectId].MoveType = MoveType.RedBlizzardEast;
                }

                var redAeros = C.PriorityData
                    .GetPlayers(x => _players.First(y => y.Value.PlayerName == x.Name).Value is
                        { Color: Debuff.Red, Debuff: Debuff.Aero }
                    );

                if (redAeros != null)
                {
                    _players[redAeros[0].IGameObject.GameObjectId].MoveType = MoveType.RedAeroWest;
                    _players[redAeros[1].IGameObject.GameObjectId].MoveType = MoveType.RedAeroEast;
                }

                foreach (var otherPlayer in _players.Where(x => x.Value.MoveType == null))
                    _players[otherPlayer.Key].MoveType = otherPlayer.Value.Debuff switch
                    {
                        Debuff.Holy => MoveType.BlueHoly,
                        Debuff.Blizzard => MoveType.BlueBlizzard,
                        Debuff.Water => MoveType.BlueWater,
                        Debuff.Eruption => MoveType.BlueEruption,
                        _ => _players[otherPlayer.Key].MoveType
                    };


                if (!string.IsNullOrEmpty(C.CommandWhenBlueDebuff) &&
                    Player.Status.Any(x => x.StatusId == (uint)Debuff.Blue))
                {
                    var random = 0;
                    if (C.ShouldUseRandomWait)
                        random = new Random().Next((int)(C.WaitRange.X * 1000), (int)(C.WaitRange.Y * 1000));
                    Controller.Schedule(() => { Chat.Instance.ExecuteCommand(C.CommandWhenBlueDebuff); }, random);
                }

                _state = State.PreSplit;
            }
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _baseDirection = null;
        _lateHourglassDirection = null;
        _firstWaveDirection = null;
        _secondWaveDirection = null;
        _players.Clear();
        _burnHourglassCount = 0;
        _earlyHourglassList.Clear();
        _lateHourglassList.Clear();
    }


    public Vector2 SwapXIfNecessary(Vector2 position)
    {
        if (_lateHourglassDirection is Direction.NorthEast or Direction.SouthWest)
            return position;
        var swapX = _center.X * 2 - position.X;
        return new Vector2(swapX, position.Y);
    }

    public override void OnSetup()
    {
        foreach (var move in Enum.GetValues<MoveType>())
            Controller.RegisterElement(move.ToString(), new Element(0)
            {
                radius = 1f,
                thicc = 6f
            });

        foreach (var stack in Enum.GetValues<WaveStack>())
            Controller.RegisterElement(stack + nameof(WaveStack), new Element(0)
            {
                radius = 1f,
                thicc = 6f
            });

        Controller.RegisterElement("Alert", new Element(1)
        {
            radius = 0f,
            overlayText = "Alert",
            overlayFScale = 1f,
            overlayVOffset = 1f,
            refActorComparisonType = 5,
            refActorPlaceholder = ["<1>"]
        });

        Controller.RegisterElementFromCode("SplitPosition", "{\"Name\":\"\",\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":1.0,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":4.0,\"overlayText\":\"Spread!\",\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public void Alert(string text)
    {
        if (Controller.TryGetElementByName("Alert", out var element))
        {
            element.Enabled = true;
            element.overlayText = text;
        }
    }

    public void HideAlert()
    {
        if (Controller.TryGetElementByName("Alert", out var element))
            element.Enabled = false;
    }


    public override void OnUpdate()
    {
        if (_state is State.None or State.End)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            return;
        }


        var myMove = _players[Player.Object.GameObjectId].MoveType.ToString();
        foreach (var move in Enum.GetValues<MoveType>())
            if (Controller.TryGetElementByName(move.ToString(), out var element))
            {
                if (_state == State.PlaceReturn)
                {
                    element.Enabled = false;
                    continue;
                }

                element.Enabled = C.ShowOther;
                element.color = EColor.Red.ToUint();

                if (myMove == move.ToString())
                {
                    element.Enabled = true;
                    element.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
                    element.tether = true;
                }
            }

        switch (_state)
        {
            case State.PreSplit:
            case State.BurnYellowHourglass:
                BurnYellowHourglass();
                break;
            case State.HitDragonAndRed:
                HitDragonAndRed();
                break;
            case State.DebuffExpire:
                DebuffExpire();
                break;
            case State.BurnHourglass:
                BurnHourglass();
                break;
            case State.BurnPurpleHourglass:
                BurnPurpleHourglass();
                break;
            case State.HitDragonAndAero:
                HitDragonAndAero();
                break;
            case State.CorrectCleanse:
                CorrectCleanse();
                break;
            case State.PlaceReturn:
                PlaceReturn();
                break;
            case State.Split:
                Split();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    public void BurnYellowHourglass()
    {
        foreach (var player in Enum.GetValues<MoveType>())
        {
            var position = player switch
            {
                MoveType.RedBlizzardWest => new Vector2(87, 100),
                MoveType.RedBlizzardEast => new Vector2(113, 100),
                MoveType.RedAeroWest => new Vector2(88, 115),
                MoveType.RedAeroEast => new Vector2(112, 115),
                MoveType.BlueBlizzard => new Vector2(88, 115),
                MoveType.BlueHoly => new Vector2(88, 115),
                MoveType.BlueWater => new Vector2(88, 115),
                MoveType.BlueEruption => new Vector2(112, 85),
                _ => throw new InvalidOperationException()
            };

            position = SwapXIfNecessary(position);
            if (Controller.TryGetElementByName(SwapIfNecessary(player), out var element))
            {
                element.radius = 0.5f;
                element.SetOffPosition(position.ToVector3(0));
            }
        }
    }

    public void HitDragonAndRed()
    {
        foreach (var player in Enum.GetValues<MoveType>())
        {
            var position = player switch
            {
                MoveType.RedBlizzardWest => WestDragon?.Position.ToVector2() ?? new Vector2(87, 100),
                MoveType.RedBlizzardEast => EastDragon?.Position.ToVector2() ?? new Vector2(113, 100),
                MoveType.RedAeroWest => new Vector2(90, 117),
                MoveType.RedAeroEast => new Vector2(107, 118),
                MoveType.BlueBlizzard => new Vector2(91, 115),
                MoveType.BlueHoly => new Vector2(91, 115),
                MoveType.BlueWater => new Vector2(91, 115),
                MoveType.BlueEruption => new Vector2(112, 85),
                _ => throw new InvalidOperationException()
            };

            position = SwapXIfNecessary(position);
            if (Controller.TryGetElementByName(SwapIfNecessary(player), out var element))
            {
                element.radius = 0.5f;
                element.SetOffPosition(position.ToVector3(0));
            }
        }

        if (_players[Player.Object.GameObjectId].MoveType == MoveType.RedBlizzardEast ||
            _players[Player.Object.GameObjectId].MoveType == MoveType.RedBlizzardWest)
            Alert(C.HitDragonText.Get());
    }

    public void DebuffExpire()
    {
        HitDragonAndRed();
    }

    public void BurnHourglass()
    {
        foreach (var player in Enum.GetValues<MoveType>())
        {
            var position = player switch
            {
                MoveType.RedBlizzardWest => new Vector2(112, 86),
                MoveType.RedBlizzardEast => new Vector2(112, 86),
                MoveType.RedAeroWest => new Vector2(100, 115),
                MoveType.RedAeroEast => new Vector2(107, 118),
                MoveType.BlueBlizzard => new Vector2(112, 86),
                MoveType.BlueHoly => new Vector2(112, 86),
                MoveType.BlueWater => new Vector2(112, 86),
                MoveType.BlueEruption => new Vector2(112, 86),
                _ => throw new InvalidOperationException()
            };

            position = SwapXIfNecessary(position);
            if (Controller.TryGetElementByName(SwapIfNecessary(player), out var element))
            {
                element.radius = 1f;
                element.SetOffPosition(position.ToVector3(0));
            }
        }
    }

    public void BurnPurpleHourglass()
    {
        foreach (var player in Enum.GetValues<MoveType>())
        {
            var position = player switch
            {
                MoveType.RedBlizzardWest => new Vector2(100, 85),
                MoveType.RedBlizzardEast => new Vector2(100, 85),
                MoveType.RedAeroWest => new Vector2(100, 118),
                MoveType.RedAeroEast => new Vector2(110, 110),
                MoveType.BlueBlizzard => new Vector2(100, 85),
                MoveType.BlueHoly => new Vector2(100, 85),
                MoveType.BlueWater => new Vector2(100, 85),
                MoveType.BlueEruption => new Vector2(100, 85),
                _ => throw new InvalidOperationException()
            };

            position = SwapXIfNecessary(position);
            if (Controller.TryGetElementByName(SwapIfNecessary(player), out var element))
            {
                element.radius = 1f;
                element.SetOffPosition(position.ToVector3(0));
            }
        }

        Alert(C.AvoidWaveText.Get());
    }

    public void HitDragonAndAero()
    {
        foreach (var player in Enum.GetValues<MoveType>())
        {
            Direction? returnDirection = (_firstWaveDirection, _secondWaveDirection) switch
            {
                (Direction.North, Direction.East) => Direction.NorthEast,
                (Direction.East, Direction.South) => Direction.SouthEast,
                (Direction.South, Direction.West) => Direction.SouthWest,
                (Direction.West, Direction.North) => Direction.NorthWest,
                (Direction.North, Direction.West) => Direction.NorthWest,
                (Direction.West, Direction.South) => Direction.SouthWest,
                (Direction.South, Direction.East) => Direction.SouthEast,
                (Direction.East, Direction.North) => Direction.NorthEast,
                _ => null
            };

            var returnPosition = returnDirection switch
            {
                Direction.NorthEast => new Vector2(115, 85),
                Direction.SouthEast => new Vector2(115, 115),
                Direction.SouthWest => new Vector2(85, 115),
                Direction.NorthWest => new Vector2(85, 85),
                _ => new Vector2(100f, 85f)
            };

            var position = player switch
            {
                MoveType.RedBlizzardWest => returnPosition,
                MoveType.RedBlizzardEast => returnPosition,
                MoveType.RedAeroWest => WestDragon?.Position.ToVector2() ?? new Vector2(87, 108),
                MoveType.RedAeroEast => EastDragon?.Position.ToVector2() ?? new Vector2(113, 108),
                MoveType.BlueBlizzard => new Vector2(100, 100),
                MoveType.BlueHoly => new Vector2(100, 100),
                MoveType.BlueWater => new Vector2(100, 100),
                MoveType.BlueEruption => new Vector2(100, 100),
                _ => throw new InvalidOperationException()
            };

            position = SwapXIfNecessary(position);
            if (Controller.TryGetElementByName(SwapIfNecessary(player), out var element))
            {
                element.radius = 2f;
                element.SetOffPosition(position.ToVector3(0));
            }
        }

        if (_players[Player.Object.GameObjectId].MoveType == MoveType.RedAeroEast ||
            _players[Player.Object.GameObjectId].MoveType == MoveType.RedAeroWest)
            Alert(C.HitDragonText.Get());
    }

    public string SwapIfNecessary(MoveType move)
    {
        if (_lateHourglassDirection is Direction.NorthEast or Direction.SouthWest)
            return move.ToString();
        return move switch
        {
            MoveType.RedBlizzardWest => MoveType.RedBlizzardEast.ToString(),
            MoveType.RedBlizzardEast => MoveType.RedBlizzardWest.ToString(),
            MoveType.RedAeroWest => MoveType.RedAeroEast.ToString(),
            MoveType.RedAeroEast => MoveType.RedAeroWest.ToString(),
            _ => move.ToString()
        };
    }

    public void CorrectCleanse()
    {
        foreach (var player in Enum.GetValues<MoveType>())
        {
            if (_burnHourglassCount < 5)
            {
                var pos = player switch
                {
                    MoveType.RedBlizzardWest => new Vector2(100, 85),
                    MoveType.RedBlizzardEast => new Vector2(100, 85),
                    MoveType.RedAeroWest => new Vector2(100, 115),
                    MoveType.RedAeroEast => new Vector2(100, 115),
                    MoveType.BlueBlizzard => new Vector2(100, 85),
                    MoveType.BlueHoly => new Vector2(100, 85),
                    MoveType.BlueWater => new Vector2(100, 85),
                    MoveType.BlueEruption => new Vector2(100, 85),
                    _ => throw new InvalidOperationException()
                };

                if (Controller.TryGetElementByName(SwapIfNecessary(player), out var e))
                {
                    e.radius = 1f;
                    e.SetOffPosition(pos.ToVector3(0));
                }

                continue;
            }


            Direction? returnDirection = (_firstWaveDirection, _secondWaveDirection) switch
            {
                (Direction.North, Direction.East) => Direction.NorthEast,
                (Direction.East, Direction.South) => Direction.SouthEast,
                (Direction.South, Direction.West) => Direction.SouthWest,
                (Direction.West, Direction.North) => Direction.NorthWest,
                (Direction.North, Direction.West) => Direction.NorthWest,
                (Direction.West, Direction.South) => Direction.SouthWest,
                (Direction.South, Direction.East) => Direction.SouthEast,
                (Direction.East, Direction.North) => Direction.NorthEast,
                _ => null
            };

            Vector2 returnPosition;
            if(!C.KBIRewind) 
            {
                returnPosition = returnDirection switch
                {
                    Direction.NorthEast => new Vector2(112, 88),
                    Direction.SouthEast => new Vector2(112, 112),
                    Direction.SouthWest => new Vector2(88, 112),
                    Direction.NorthWest => new Vector2(88, 88),
                    _ => new Vector2(100f, 100f)
                };
            }
            else
            {
                returnPosition = returnDirection switch
                {
                    Direction.NorthEast => new Vector2(100, 95),
                    Direction.SouthEast => new Vector2(100, 105),
                    Direction.SouthWest => new Vector2(100, 105),
                    Direction.NorthWest => new Vector2(100, 95),
                    _ => new Vector2(100f, 100f)
                };
            }

            var position = player switch
            {
                MoveType.RedBlizzardWest => returnPosition,
                MoveType.RedBlizzardEast => returnPosition,
                MoveType.RedAeroWest => returnPosition,
                MoveType.RedAeroEast => returnPosition,
                _ => Vector2.Zero
            };

            if (position == Vector2.Zero)
            {
                var direction = Direction.West;
                if (C.PrioritizeMarker &&
                    _players.FirstOrDefault(x => x.Value.PlayerName == Player.Name).Value?.Marker is { } marker)
                {
                    direction = marker switch
                    {
                        MarkerType.Attack1 => C.WhenAttack1,
                        MarkerType.Attack2 => C.WhenAttack2,
                        MarkerType.Attack3 => C.WhenAttack3,
                        MarkerType.Attack4 => C.WhenAttack4,
                        _ => direction
                    };
                }
                else
                {
                    if (player == C.WestSentence)
                        direction = Direction.West;
                    else if (player == C.SouthWestSentence)
                        direction = Direction.SouthWest;
                    else if (player == C.SouthEastSentence)
                        direction = Direction.SouthEast;
                    else if (player == C.EastSentence)
                        direction = Direction.East;
                }

                position = direction switch
                {
                    Direction.West => new Vector2(87, 100),
                    Direction.SouthWest => new Vector2(92, 110),
                    Direction.SouthEast => new Vector2(108, 110),
                    Direction.East => new Vector2(113, 100),
                    _ => position
                };
            }

            if (Controller.TryGetElementByName(SwapIfNecessary(player), out var element))
            {
                element.radius = 2f;
                element.SetOffPosition(position.ToVector3(0));
            }
        }

        if (Player.Status.Any(x => x.StatusId == (uint)Debuff.Blue))
            Alert(C.CleanseText.Get());
        else
            HideAlert();
    }

    public void PlaceReturn()
    {
        if(!C.KBIRewind)
        {
            var returnDirection = (_firstWaveDirection, _secondWaveDirection) switch
            {
                (Direction.North, Direction.East) => Direction.NorthEast,
                (Direction.East, Direction.South) => Direction.SouthEast,
                (Direction.South, Direction.West) => Direction.SouthWest,
                (Direction.West, Direction.North) => Direction.NorthWest,
                (Direction.North, Direction.West) => Direction.NorthWest,
                (Direction.West, Direction.South) => Direction.SouthWest,
                (Direction.South, Direction.East) => Direction.SouthEast,
                (Direction.East, Direction.North) => Direction.NorthEast,
                _ => throw new InvalidOperationException()
            };

            var basePosition = returnDirection switch
            {
                Direction.NorthEast => new Vector2(113, 87),
                Direction.SouthEast => new Vector2(113, 113),
                Direction.SouthWest => new Vector2(87, 113),
                Direction.NorthWest => new Vector2(87, 87),
                _ => throw new InvalidOperationException()
            };

            var isWest = returnDirection switch
            {
                Direction.NorthEast => C.IsWestWhenNorthEastWave,
                Direction.SouthEast => C.IsWestWhenSouthEastWave,
                Direction.SouthWest => C.IsWestWhenSouthWestWave,
                Direction.NorthWest => C.IsWestWhenNorthWestWave,
                _ => throw new InvalidOperationException()
            };

            var myStack = (isWest, C.IsTank) switch
            {
                (true, true) => WaveStack.WestTank,
                (false, true) => WaveStack.EastTank,
                (true, false) => WaveStack.West,
                (false, false) => WaveStack.East
            };

            var westTankPosition = basePosition;
            var eastTankPosition = basePosition;
            var westPosition = basePosition;
            var eastPosition = basePosition;

            switch(returnDirection)
            {
                case Direction.NorthEast:
                    westTankPosition += new Vector2(-3f, -0.5f);
                    eastTankPosition += new Vector2(0.5f, 3f);
                    westPosition += new Vector2(-3f, 1f);
                    eastPosition += new Vector2(-1f, 3f);
                    break;
                case Direction.SouthEast:
                    westTankPosition += new Vector2(-3f, 0.5f);
                    eastTankPosition += new Vector2(0.5f, -3f);
                    westPosition += new Vector2(-3f, -1f);
                    eastPosition += new Vector2(-1f, -3f);
                    break;
                case Direction.SouthWest:
                    westTankPosition += new Vector2(-0.5f, -3f);
                    eastTankPosition += new Vector2(3f, 0.5f);
                    westPosition += new Vector2(1f, -3f);
                    eastPosition += new Vector2(3f, -1f);
                    break;
                default:
                    westTankPosition += new Vector2(-0.5f, 3f);
                    eastTankPosition += new Vector2(3f, -0.5f);
                    westPosition += new Vector2(1f, 3f);
                    eastPosition += new Vector2(3f, -1f);
                    break;
            }

            foreach(var stack in Enum.GetValues<WaveStack>())
                if(Controller.TryGetElementByName(stack + nameof(WaveStack), out var element))
                {
                    element.Enabled = C.ShowOther;
                    element.radius = stack is WaveStack.WestTank or WaveStack.EastTank ? 0.5f : 1.2f;
                    element.SetOffPosition(stack switch
                    {
                        WaveStack.WestTank => westTankPosition.ToVector3(0),
                        WaveStack.EastTank => eastTankPosition.ToVector3(0),
                        WaveStack.West => westPosition.ToVector3(0),
                        WaveStack.East => eastPosition.ToVector3(0),
                        _ => throw new InvalidOperationException()
                    });
                }

            if(Controller.TryGetElementByName(myStack + nameof(WaveStack), out var myElement))
            {
                myElement.Enabled = true;
                myElement.tether = true;
                myElement.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
            }
        }
        else
        {
            var returnDirection = (_firstWaveDirection, _secondWaveDirection) switch
            {
                (Direction.North, Direction.East) => Direction.North,
                (Direction.East, Direction.South) => Direction.South,
                (Direction.South, Direction.West) => Direction.South,
                (Direction.West, Direction.North) => Direction.North,
                (Direction.North, Direction.West) => Direction.North,
                (Direction.West, Direction.South) => Direction.South,
                (Direction.South, Direction.East) => Direction.South,
                (Direction.East, Direction.North) => Direction.North,
                _ => throw new InvalidOperationException()
            };
            if(Controller.TryGetElementByName(WaveStack.West.ToString() + nameof(WaveStack), out var myElement))
            {
                myElement.Enabled = true;
                myElement.tether = true;
                myElement.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
                myElement.SetOffPosition(Vector3.Zero);
                myElement.SetRefPosition(new(100, 0, 100 + (returnDirection == Direction.North?-5:5)));
            }
        }

        Alert(C.PlaceReturnText.Get());
    }

    public void Split()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(C.HighlightSplitPosition && Controller.TryGetElementByName("SplitPosition", out var myElement))
        {
            myElement.Enabled = true;
            myElement.tether = true;
            myElement.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
        }
        Alert(C.SplitText.Get());
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
            ImGuiEx.Text("Priority");
            ImGui.Indent();
            ImGui.Text("West");
            C.PriorityData.Draw();
            ImGui.Text("East");
            ImGui.Unindent();
            ImGui.Separator();

            ImGuiEx.EnumCombo("Hit Timing", ref C.HitTiming);

            ImGui.Separator();
            ImGuiEx.Text("Sentence Moves");
            ImGui.Indent();
            ImGui.Checkbox("PrioritizeMarker", ref C.PrioritizeMarker);
            if (C.PrioritizeMarker)
            {
                ImGui.Indent();
                ImGui.InputText("Execute Command When Blue Debuff Gained", ref C.CommandWhenBlueDebuff, 30);
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
                ImGuiEx.EnumCombo("When Attack 1", ref C.WhenAttack1);
                ImGuiEx.EnumCombo("When Attack 2", ref C.WhenAttack2);
                ImGuiEx.EnumCombo("When Attack 3", ref C.WhenAttack3);
                ImGuiEx.EnumCombo("When Attack 4", ref C.WhenAttack4);
                ImGui.Unindent();
            }

            ImGuiEx.EnumCombo("West Sentence", ref C.WestSentence);
            ImGuiEx.EnumCombo("South West Sentence", ref C.SouthWestSentence);
            ImGuiEx.EnumCombo("South East Sentence", ref C.SouthEastSentence);
            ImGuiEx.EnumCombo("East Sentence", ref C.EastSentence);
            ImGui.Unindent();
            ImGui.Separator();

            ImGui.Checkbox("Highlight static Spirit taker position. ", ref C.HighlightSplitPosition);
            ImGuiEx.TextWrapped(EColor.RedBright, "You must go to Registered Elements section and put \"SplitPosition\" element to where you want it to be. Go to Eden's Promise: Eternity undersized for a preview, if necessary.");

            ImGui.Separator();

            ImGuiEx.Text("Place Return Moves");
            ImGui.Indent();

            ImGui.Checkbox($"Knockback immunity return positions (beta)", ref C.KBIRewind);

            if(!C.KBIRewind)
            {
                ImGui.Checkbox("Is Tank", ref C.IsTank);

                ImGui.Text("When North East Wave:");
                ImGui.SameLine();
                ImGuiEx.RadioButtonBool($"West##{nameof(C.IsWestWhenNorthEastWave)}",
                    $"East##{nameof(C.IsWestWhenNorthEastWave)}", ref C.IsWestWhenNorthEastWave, true);
                ImGui.Text("When South East Wave:");
                ImGui.SameLine();
                ImGuiEx.RadioButtonBool($"West##{nameof(C.IsWestWhenSouthEastWave)}",
                    $"East##{nameof(C.IsWestWhenSouthEastWave)}", ref C.IsWestWhenSouthEastWave, true);
                ImGui.Text("When South West Wave:");
                ImGui.SameLine();
                ImGuiEx.RadioButtonBool($"West##{nameof(C.IsWestWhenSouthWestWave)}",
                    $"East##{nameof(C.IsWestWhenSouthWestWave)}", ref C.IsWestWhenSouthWestWave, true);
                ImGui.Text("When North West Wave:");
                ImGui.SameLine();
                ImGuiEx.RadioButtonBool($"West##{nameof(C.IsWestWhenNorthWestWave)}",
                    $"East##{nameof(C.IsWestWhenNorthWestWave)}", ref C.IsWestWhenNorthWestWave, true);
            }

            ImGui.Unindent();

            ImGui.Separator();

            ImGui.Text("Dialogue Text:");
            ImGui.Indent();
            var splitText = C.SplitText.Get();
            ImGui.Text("Split Text:");
            ImGui.SameLine();
            C.SplitText.ImGuiEdit(ref splitText);

            var hitDragonText = C.HitDragonText.Get();
            ImGui.Text("Hit Dragon Text:");
            ImGui.SameLine();
            C.HitDragonText.ImGuiEdit(ref hitDragonText);

            var avoidWaveText = C.AvoidWaveText.Get();
            ImGui.Text("Avoid Wave Text:");
            ImGui.SameLine();
            C.AvoidWaveText.ImGuiEdit(ref avoidWaveText);

            var cleanseText = C.CleanseText.Get();
            ImGui.Text("Cleanse Text:");
            ImGui.SameLine();
            C.CleanseText.ImGuiEdit(ref cleanseText);

            var placeReturnText = C.PlaceReturnText.Get();
            ImGui.Text("Place Return Text:");
            ImGui.SameLine();
            C.PlaceReturnText.ImGuiEdit(ref placeReturnText);

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
        }

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"State: {_state}");

            ImGui.Text($"Base Direction: {_baseDirection.ToString()}");
            ImGui.Text($"Late Hourglass Direction: {_lateHourglassDirection.ToString()}");
            ImGui.Text($"First Wave Direction: {_firstWaveDirection.ToString()}");
            ImGui.Text($"Second Wave Direction: {_secondWaveDirection.ToString()}");
            ImGui.Text($"Burn Hourglass Count: {_burnHourglassCount}");

            ImGuiEx.EzTable("Player Data", _players.SelectMany(x => new ImGuiEx.EzTableEntry[]
            {
                new("Player Name", () => ImGuiEx.Text(x.Value.PlayerName)),
                new("Color", () => ImGuiEx.Text(x.Value.Color.ToString())),
                new("Debuff", () => ImGuiEx.Text(x.Value.Debuff.ToString())),
                new("Has Quietus", () => ImGuiEx.Text(x.Value.HasQuietus.ToString())),
                new("Move Type", () => ImGuiEx.Text(x.Value.MoveType.ToString()))
            }));

            ImGuiEx.EnumCombo("First Wave Direction", ref _debugDirection1);
            ImGuiEx.EnumCombo("Second Wave Direction", ref _debugDirection2);
            if (ImGui.Button("Show Return Placement"))
            {
                _firstWaveDirection = _debugDirection1;
                _secondWaveDirection = _debugDirection2;
                _state = State.PlaceReturn;
            }
        }
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5,
        uint p6, ulong targetId,
        byte replaying)
    {
        if (_state is not (State.None or State.End))
            if (command == 502)
                try
                {
                    _players[p2].Marker = (MarkerType)p1;
                }
                catch
                {
                    PluginLog.Warning($"GameObjectId:{p2} was not found");
                }
    }

    /*public class CoordinateTransformer(Direction baseDirection, Direction targetDirection, Vector2 center = default)
    {
        public Direction TransformedTargetDirection
        {
            get
            {
                var baseAngle = (int)baseDirection;
                var targetAngle = (int)targetDirection;

                var relativeAngle = (targetAngle - baseAngle + 360) % 360;

                return (Direction)relativeAngle;
            }
        }

        private static Vector2 RotatePoint(Vector2 pos, float angle)
        {
            var rad = Math.PI * angle / 180.0;
            var newX = pos.X * Math.Cos(rad) - pos.Y * Math.Sin(rad);
            var newY = pos.X * Math.Sin(rad) + pos.Y * Math.Cos(rad);
            return new Vector2((float)newX, (float)newY);
        }

        public Vector2 Transform(Vector2 targetCoords)
        {
            var baseAngle = (int)baseDirection;

            var relativePosition = targetCoords - center;
            var rotated = RotatePoint(relativePosition, -baseAngle);

            if (TransformedTargetDirection is Direction.NorthEast or Direction.SouthWest) return rotated + center;

            var axisDirection = RotatePoint(Vector2.UnitY, baseAngle); // 基準方向の軸ベクトル
            rotated = ReflectAcrossAxis(rotated, Vector2.Zero, axisDirection); // 左上原点で反転
            return rotated + center;
        }

        private static Vector2 ReflectAcrossAxis(Vector2 point, Vector2 axisOrigin, Vector2 axisDirection)
        {
            var axis = Vector2.Normalize(axisDirection - axisOrigin);

            var relativePoint = point - axisOrigin;
            var projection = Vector2.Dot(relativePoint, axis) * axis;

            var reflection = 2 * projection - relativePoint;

            return axisOrigin + reflection;
        }
    }*/

    public record PlayerData
    {
        public Debuff? Color;
        public Debuff? Debuff;
        public bool HasQuietus;
        public MarkerType? Marker;
        public MoveType? MoveType;
        public string PlayerName;

        public bool HasDebuff => Debuff != null && Color != null;
    }

    public class Config : IEzConfig
    {
        public InternationalString AvoidWaveText = new() { En = "Avoid Wave", Jp = "波をよけろ！" };
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public InternationalString CleanseText = new() { En = "Get Cleanse", Jp = "白を取れ！" };
        public string CommandWhenBlueDebuff = "";
        public MoveType EastSentence = MoveType.BlueBlizzard;

        public InternationalString HitDragonText = new() { En = "Hit Dragon", Jp = "竜に当たれ！" };

        public HitTiming HitTiming = HitTiming.Late;

        public bool IsTank;
        public bool IsWestWhenNorthEastWave;
        public bool IsWestWhenNorthWestWave;
        public bool IsWestWhenSouthEastWave;
        public bool IsWestWhenSouthWestWave;
        public InternationalString PlaceReturnText = new() { En = "Place Return", Jp = "リターンを置け！" };

        public bool PrioritizeMarker;

        public PriorityData PriorityData = new();

        public bool ShouldUseRandomWait = true;


        public bool ShowOther;
        public MoveType SouthEastSentence = MoveType.BlueHoly;
        public MoveType SouthWestSentence = MoveType.BlueWater;
        public InternationalString SplitText = new() { En = "Split", Jp = "散開！" };
        public Vector2 WaitRange = new(0.5f, 1.5f);
        public MoveType WestSentence = MoveType.BlueEruption;
        public Direction WhenAttack1 = Direction.East;
        public Direction WhenAttack2 = Direction.SouthEast;
        public Direction WhenAttack3 = Direction.SouthWest;
        public Direction WhenAttack4 = Direction.West;

        public bool HighlightSplitPosition = false;

        public bool KBIRewind = false;
    }
}
