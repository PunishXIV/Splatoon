using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P4_Darklit : SplatoonScript
{
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

    public enum Mode
    {
        Vertical,
        Horizontal
    }

    public enum MoveType
    {
        Straight,
        Clockwise,
        Swap
    }

    public enum State
    {
        None,
        Start,
        Split,
        Stack,
        End
    }

    private Dictionary<ulong, PlayerData> _players = new();

    private State _state = State.None;

    public uint WaterId = 0x99D;
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(1, "Garume");
    public Config C => Controller.GetConfig<Config>();

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 00) _state = State.Start;
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_state == State.Start && source.GetObject() is IPlayerCharacter sourcePlayer &&
            target.GetObject() is IPlayerCharacter targetPlayer)
        {
            var direction = Direction.North;
            var priority = C.PriorityData.GetPlayers(x => true).IndexOf(x => x.Name == sourcePlayer.Name.ToString());
            if (priority < 4)
                direction = C.Mode == Mode.Vertical ? Direction.North : Direction.West;
            else
                direction = C.Mode == Mode.Vertical ? Direction.South : Direction.East;
            _players[source] = new PlayerData
            {
                Name = sourcePlayer.Name.ToString(),
                Position = sourcePlayer.Position,
                Direction = direction,
                IsInkling = true,
                LinkToRole = targetPlayer.GetRole(),
                Id = source,
                LinkTo = target,
                Role = sourcePlayer.GetRole(),
                Priority = priority
            };

            if (_players.Count == 4)
            {
                MoveType? moveType = null;
                if (C.Mode == Mode.Vertical)
                {
                    var isSameRole = _players.Any(player => player.Value.Role == player.Value.LinkToRole);
                    if (!isSameRole) moveType = MoveType.Clockwise;
                    var left = _players.Values.Where(x => x is { Priority: < 4, IsInkling: true })
                        .OrderBy(x => x.Priority).ToList();
                    var right = _players.Values.Where(x => x is { Priority: >= 4, IsInkling: true })
                        .OrderBy(x => x.Priority).ToList();
                    if (_players[left[0].LinkTo].Name == right[0].Name ||
                        _players[right[0].LinkTo].Name == left[0].Name)
                        moveType = MoveType.Swap;
                    else
                        moveType = MoveType.Straight;

                    if (moveType == MoveType.Straight)
                    {
                        _players[left[0].Id].Direction = Direction.North;
                        _players[left[1].Id].Direction = Direction.South;
                        _players[right[0].Id].Direction = Direction.North;
                        _players[right[1].Id].Direction = Direction.South;
                    }
                    else if (moveType == MoveType.Clockwise)
                    {
                        _players[left[0].Id].Direction = Direction.North;
                        _players[left[1].Id].Direction = Direction.North;
                        _players[right[0].Id].Direction = Direction.South;
                        _players[right[1].Id].Direction = Direction.South;
                    }
                    else if (moveType == MoveType.Swap)
                    {
                        if (C.IsDpsSwap)
                        {
                            _players[left[0].Id].Direction = Direction.North;
                            _players[left[1].Id].Direction = Direction.South;
                            _players[right[0].Id].Direction = Direction.South;
                            _players[right[1].Id].Direction = Direction.North;
                        }
                        else
                        {
                            _players[left[0].Id].Direction = Direction.North;
                            _players[left[1].Id].Direction = Direction.North;
                            _players[right[0].Id].Direction = Direction.South;
                            _players[right[1].Id].Direction = Direction.South;
                        }
                    }

                    var otherLeft = _players.Values.Where(x => x is { Priority: < 4, IsInkling: false })
                        .OrderBy(x => x.Priority).ToList();
                    var otherRight = _players.Values.Where(x => x is { Priority: >= 4, IsInkling: false })
                        .OrderBy(x => x.Priority).ToList();
                    _players[otherLeft[0].Id].Direction = Direction.NorthWest;
                    _players[otherRight[0].Id].Direction = Direction.SouthWest;
                    _players[otherLeft[1].Id].Direction = Direction.NorthEast;
                    _players[otherRight[1].Id].Direction = Direction.SouthEast;
                }
                else
                {
                    throw new NotImplementedException();
                }


                _state = State.Split;
            }
        }
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("General"))
        {
            ImGuiEx.EnumCombo("Mode", ref C.Mode);
            ImGuiEx.Text("Priority");
            ImGuiEx.Text(C.Mode == Mode.Vertical
                ? "NorthWest -> SouthWest -> NorthEast -> SouthWest"
                : "NorthWest -> NorthEast -> SouthWest -> SouthEast");
            C.PriorityData.Draw();
            ImGui.Checkbox("Dps Swap When Box", ref C.IsDpsSwap);
            ImGuiEx.HelpMarker("If disabled, TH will swap when BOX.");
        }

        if (ImGuiEx.CollapsingHeader("Debug")) ImGui.Text($"State: {_state}");
    }

    public record PlayerData
    {
        public Direction Direction;
        public ulong Id;
        public bool IsInkling;
        public ulong LinkTo;
        public CombatRole LinkToRole;
        public string Name;
        public Vector3 Position;
        public int Priority = -1;
        public CombatRole Role;
        public bool SwapStack;
    }

    public class Config : IEzConfig
    {
        public bool IsDpsSwap = true;
        public Mode Mode = Mode.Vertical;
        public PriorityData PriorityData = new();
    }
}