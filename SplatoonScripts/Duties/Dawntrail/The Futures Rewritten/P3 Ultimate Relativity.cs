using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Statuses;
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
        GainBuff,
        FirstFire,
        BaitEarlyHourglass,
        SecondFire,
        BaitNoTetherHourglass,
        ThirdFire,
        BaitLateHourglass,
        BurnReturn,
        End
    }

    private readonly Dictionary<ulong,PlayerData> _playerDatas = [];

    private State _state = State.None;

    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(2, "Garume");

    public Config C => Controller.GetConfig<Config>();

    public override void OnStartingCast(uint source, uint castId)
    {
        if (_state == State.End) return;
        if (castId == 40266) _state = State.Start;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_state == State.End) return;
        if (set.Action is { RowId: 40276 })
        {
            var fireStatuses = FakeParty.Get().SelectMany(x => x.StatusList)
                .Where(x => x.StatusId == (uint)Debuff.Fire)
                .ToList();
            if (fireStatuses.Any(x => x.RemainingTime > 15f))
                _state = State.BaitEarlyHourglass;
            else
                _state = State.BaitNoTetherHourglass;
        }
    }

    public override void OnGainBuffEffect(uint sourceId, Status Status)
    {
        if (_state == State.Start)
            if (FakeParty.Get()
                .All(x => Enum.GetValues<Debuff>().Any(y => x.StatusList.Any(z => z.StatusId == (uint)y))))
            {
                _state = State.GainBuff;
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
    }

    public void BaitHourglass(Direction direction)
    {
    }

    public void GoCenter(Direction direction)
    {
        var center = new Vector2(100f, 100f);
        if (Controller.TryGetElementByName(direction.ToString(), out var element))
        {
            element.SetOffPosition(center.ToVector3(0f));
            element.Enabled = true;
        }
    }

    public void GoOutside(Direction direction)
    {
        var radius = 18f;
        var angle = (int)direction;
        var center = new Vector2(100, 100);
        var position = new Vector2(
            center.X + radius * MathF.Cos(MathF.PI * angle / 180),
            center.Y + radius * MathF.Sin(MathF.PI * angle / 180)
        );
        if (Controller.TryGetElementByName(direction.ToString(), out var element))
        {
            element.SetOffPosition(position.ToVector3(0f));
            element.Enabled = true;
        }
    }

    public void PlaceReturnToHourglass(Direction direction)
    {
    }
    
    public void PlaceReturnToNearCenter(Direction direction)
    {
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
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("General"))
        {
            ImGuiEx.EnumCombo("Mode", ref C.Mode);
            if (C.Mode == Mode.Marker)
            {
                ImGui.InputText("Early Fire Command", ref C.EarlyFireCommand, 100);
                ImGui.InputText("Middle Fire Command", ref C.MiddleFireCommand, 100);
                ImGui.InputText("Late Fire Command", ref C.LateFireCommand, 100);
                ImGui.InputText("Blizzard Command", ref C.BlizzardCommand, 100);
            }
            else if (C.Mode == Mode.Priority)
            {
                C.PriorityList.Draw();
            }
        }

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGuiEx.EzTable("Player Data", _playerDatas.SelectMany(x => new ImGuiEx.EzTableEntry[]
            {
                new("Player Name", () => ImGuiEx.Text(x.Value.PlayerName)),
                new("Kind Fire", () => ImGuiEx.Text(x.Value.KindFire.ToString())),
                new("Number", () => ImGuiEx.Text(x.Value.Number.ToString()))
            }));
        }
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5,
        uint p6, ulong targetId,
        byte replaying)
    {
        if (_state == State.GainBuff && _playerDatas.Count < 8)
            if (command == 502)
            {
                var player = Svc.Objects.FirstOrDefault(x => x.GameObjectId == p2);
                if (player == null) PluginLog.Warning("Error: Cannot find player");
                switch (p1)
                {
                    case (uint)MarkerType.Attack1 or (uint)MarkerType.Attack2 or (uint)MarkerType.Attack3:
                        _playerDatas.Add(p2,new PlayerData
                            { PlayerName = player?.Name.ToString(), KindFire = KindFire.Early, Number = (int)p1 + 1 });
                        break;
                    case (uint)MarkerType.Bind1 or (uint)MarkerType.Bind2 or (uint)MarkerType.Bind3:
                        _playerDatas.Add(p2,new PlayerData
                            { PlayerName = player?.Name.ToString(), KindFire = KindFire.Late, Number = (int)p1 - 4 });
                        break;
                    case (uint)MarkerType.Ignore1 or (uint)MarkerType.Ignore2:
                        _playerDatas.Add(p2,new PlayerData
                            { PlayerName = player?.Name.ToString(), KindFire = KindFire.Middle, Number = (int)p1 - 7 });
                        break;
                }

                if (_playerDatas.Count == 8)
                    _state = State.FirstFire;
            }
    }

    public override void OnUpdate()
    {
        if (_state == State.End) Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
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
            PlaceReturnToHourglass(Direction.East);
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
            
        }
        else if (_state == State.BurnReturn)
        {
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

    public record PlayerData
    {
        public string? PlayerName { init; get; }
        public KindFire KindFire { init; get; }
        public int Number { init; get; }
        public override int GetHashCode()
        {
            return PlayerName?.GetHashCode() ?? 0;
        }
    }

    public class Config : IEzConfig
    {
        public string BlizzardCommand = "";
        public string EarlyFireCommand = "";
        public string LateFireCommand = "";
        public string MiddleFireCommand = "";
        public Mode Mode = Mode.Marker;
        public PriorityData PriorityList = new();
    }
}