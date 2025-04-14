using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M8S_Lone_Wolfs_Lament : SplatoonScript
{
    public enum Direction
    {
        NorthEast = 306,
        East = 18,
        South = 90,
        West = 162,
        NorthWest = 234
    }

    public enum TetherColor
    {
        Green,
        Blue
    }

    private readonly string _basePlayerOverride = "";

    private readonly List<PlayerData> _players = [];

    private bool _northEastTowerIsOne;

    private State _state = State.None;
    public override HashSet<uint>? ValidTerritories => [1263];
    public override Metadata? Metadata => new(2, "Garume");

    private IPlayerCharacter BasePlayer
    {
        get
        {
            if (_basePlayerOverride == "")
                return Player.Object;
            return Svc.Objects.OfType<IPlayerCharacter>()
                .FirstOrDefault(x => x.Name.ToString().EqualsIgnoreCase(_basePlayerOverride)) ?? Player.Object;
        }
    }

    public Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            radius = 1.5f,
            thicc = 15f,
            tether = true,
            Donut = 0.3f
        };
        Controller.RegisterElement("Bait", element);
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (_state == State.None && castId == 43052) _state = State.Casting;

        if (_state == State.Split && source.GetObject() is IBattleNpc tower &&
            Vector3.Distance(tower.Position, new Vector3(110.3f, -150f, 85.8f)) < 1f)
        {
            _northEastTowerIsOne = castId == 42118;
            _state = State.Tower;

            foreach (var playerData in _players)
                if (_northEastTowerIsOne)
                {
                    if (playerData is { TetherColor: TetherColor.Green, Direction: Direction.NorthEast })
                        playerData.Direction = Direction.NorthWest;
                }
                else
                {
                    if (playerData is { TetherColor: TetherColor.Blue, Direction: Direction.NorthEast })
                        playerData.Direction = Direction.NorthWest;
                }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_state is State.Tower && set.Action is { RowId: 42118 }) _state = State.End;
    }

    public override void OnReset()
    {
        _state = State.None;
        _players.Clear();
        _northEastTowerIsOne = false;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("""
                   It automatically detects roles.
                   If the party's role setup is different from normal, it may not work correctly.
                   """);

        ImGui.Checkbox("X Mirror", ref C.XMirror);

        ImGui.Text("Bait Color:");
        ImGuiComponents.HelpMarker(
            "Change the color of the bait and the text that will be displayed on your bait.\nSetting different values makes it rainbow.");
        ImGui.Indent();
        ImGui.ColorEdit4("Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.SameLine();
        ImGui.ColorEdit4("Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
        ImGui.Unindent();


        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"NorthEast Tower is One: {_northEastTowerIsOne}");
            ImGui.Text($"Players Count: {_players.Count}");
            ImGuiEx.EzTable(
                _players.SelectMany(x => new[]
                {
                    new ImGuiEx.EzTableEntry("Name", () => ImGui.Text(x.Name)),
                    new ImGuiEx.EzTableEntry("Tether Color", () => ImGui.Text(x.TetherColor.ToString())),
                    new ImGuiEx.EzTableEntry("My Role", () => ImGui.Text(x.MyRole.ToString())),
                    new ImGuiEx.EzTableEntry("Target Role", () => ImGui.Text(x.TargetRole.ToString())),
                    new ImGuiEx.EzTableEntry("Direction", () => ImGui.Text(x.Direction.ToString()))
                })
            );
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_state == State.Casting &&
            source.GetObject() is IPlayerCharacter sourcePlayer &&
            target.GetObject() is IPlayerCharacter targetPlayer &&
            data2 == 0 && data5 == 15)
        {
            var color = data3 == 318 ? TetherColor.Blue : TetherColor.Green;
            var myRole = targetPlayer.GetRole();
            var targetRole = sourcePlayer.GetRole();

            var direction1 = myRole switch
            {
                CombatRole.Tank when color == TetherColor.Blue => Direction.West,
                CombatRole.Tank => Direction.NorthEast,
                CombatRole.DPS when targetRole == CombatRole.Tank && color == TetherColor.Blue => Direction.East,
                CombatRole.DPS when targetRole == CombatRole.Healer && color == TetherColor.Green => Direction.South,
                CombatRole.DPS => Direction.NorthEast,
                _ => Direction.South
            };

            var direction2 = targetRole switch
            {
                CombatRole.Tank when color == TetherColor.Blue => Direction.West,
                CombatRole.Tank => Direction.NorthEast,
                CombatRole.DPS when myRole == CombatRole.Tank && color == TetherColor.Blue => Direction.East,
                CombatRole.DPS when myRole == CombatRole.Healer && color == TetherColor.Green => Direction.South,
                CombatRole.DPS => Direction.NorthEast,
                _ => Direction.South
            };

            var playerData1 = new PlayerData
            {
                Address = targetPlayer.Address,
                Name = targetPlayer.Name.ToString(),
                TetherColor = color,
                MyRole = myRole,
                TargetRole = targetRole,
                Direction = direction1
            };
            var playerData2 = new PlayerData
            {
                Address = sourcePlayer.Address,
                Name = sourcePlayer.Name.ToString(),
                TetherColor = color,
                MyRole = targetRole,
                TargetRole = myRole,
                Direction = direction2
            };
            _players.Add(playerData1);
            _players.Add(playerData2);

            if (_players.Count == 8) _state = State.Split;
        }
    }

    public override void OnUpdate()
    {
        if (_state is State.None or State.Casting or State.End)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }
        else
        {
            var player = _players.FirstOrDefault(x => x.Address == BasePlayer.Address);
            if (Controller.TryGetElementByName("Bait", out var bait) && player != null)
            {
                bait.Enabled = true;
                bait.SetRefPosition(player.Position(C.XMirror));
                bait.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
            }
        }
    }

    private enum State
    {
        None,
        Casting,
        Split,
        Tower,
        End
    }

    public class PlayerData
    {
        public IntPtr Address;
        public Direction Direction;
        public CombatRole MyRole;
        public string Name;
        public CombatRole TargetRole;
        public TetherColor? TetherColor;

        public Vector3 Position(bool xMirror = false)
        {
            const float radius = 17f;
            var direction = Direction;
            if (xMirror)
                direction = Direction switch
                {
                    Direction.NorthEast => Direction.NorthEast,
                    Direction.East => Direction.West,
                    Direction.South => Direction.South,
                    Direction.West => Direction.East,
                    Direction.NorthWest => Direction.NorthWest,
                    _ => direction
                };

            var angle = (int)direction;
            var center = new Vector2(100f, 100f);
            var x = center.X + radius * MathF.Cos(angle * MathF.PI / 180);
            var y = center.Y + radius * MathF.Sin(angle * MathF.PI / 180);
            return new Vector3(x, -150f, y);
        }
    }

    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public bool XMirror;
    }
}