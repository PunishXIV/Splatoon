using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.PartyFunctions;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P2_Light_Rampant_JP : SplatoonScript
{
    public enum Direction
    {
        None = -1,
        North = 270,
        NorthEast = 315 + 15,
        SouthEast = 45 - 15,
        South = 90,
        SouthWest = 135 + 15,
        NorthWest = 225 - 15
    }

    public enum State
    {
        None,
        Start,
        Split,
        End
    }

    private readonly HashSet<string> _aoeTargets = [];
    private bool _PlayerHasAoE;

    private State _state = State.None;
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(4, "Garume, Lusaca");

    public Config C => Controller.GetConfig<Config>();

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5,
        uint p6, uint p7, uint p8, ulong targetId,
        byte replaying)
    {
        if(_state == State.Start)
        {
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _aoeTargets.Clear();
        _PlayerHasAoE = false;
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(_state is (State.Start or State.Split) && vfxPath == "vfx/lockon/eff/target_ae_s7k1.avfx")
        {
            if(target.GetObject() is IPlayerCharacter player)
            {
                _aoeTargets.Add(player.Name.ToString());

                if(player.Name.ToString().Equals(Svc.ClientState.LocalPlayer.Name.ToString()))
                    _PlayerHasAoE = true;

            }

            var count = 0;
            foreach(var aoeTarget in _aoeTargets)
            {
                if(C.PlayersCount == 1 && C.PriorityData1.GetPlayer(x => x.Name == aoeTarget) is not null) count++;
                if(C.PlayersCount == 2 && C.PriorityData2.GetPlayer(x => x.Name == aoeTarget) is not null) count++;
                if(C.PlayersCount == 3 && C.PriorityData3.GetPlayer(x => x.Name == aoeTarget) is not null) count++;
            }

            var direction = C.Directions[count];

            PluginLog.Warning($"Direction: {direction} Count: {count}");
            const float radius = 16f;
            var center = new Vector2(100f, 100f);
            var angle = (int)direction;
            var x = center.X + radius * MathF.Cos(angle * MathF.PI / 180);
            var y = center.Y + radius * MathF.Sin(angle * MathF.PI / 180);

            if(Controller.TryGetElementByName("Bait", out var bait))
            {
                bait.Enabled = true;
                bait.SetOffPosition(new Vector3(x, 0, y));
            }

            _state = State.Split;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action is { RowId: 40213 }) _state = State.End;
    }

    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0)
        {
            radius = 4f,
            thicc = 6f,
            tether = true,
            overlayText = "<< Go Here >>",
            overlayFScale = 3f,
            overlayVOffset = 3f
        });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 40212) _state = State.Start;
    }

    public override void OnUpdate()
    {

        if(_state == State.Split && !_PlayerHasAoE)
            Controller.GetRegisteredElements()
                .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
        else
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnSettingsDraw()
    {
        ImGui.SliderInt("Players Count", ref C.PlayersCount, 0, 3);

        switch(C.PlayersCount)
        {
            case 1:
                C.PriorityData1.Draw();
                break;
            case 2:
                C.PriorityData2.Draw();
                break;
            case 3:
                C.PriorityData3.Draw();
                break;
        }

        foreach(var direction in C.Directions)
        {
            var dir = direction.Value;
            ImGui.SetCursorPosX(30);
            ImGui.Text(direction.Key.ToString());
            ImGui.SameLine();
            ImGuiEx.EnumCombo("##" + direction.Key, ref dir);
            C.Directions[direction.Key] = dir;
        }

        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"State: {_state}");
            ImGuiEx.Text($"AOE Targets: {_aoeTargets.Print()}");
        }

    }


    public class PriorityData1 : PriorityData
    {
        public override int GetNumPlayers()
        {
            return 1;
        }
    }

    public class PriorityData2 : PriorityData
    {
        public override int GetNumPlayers()
        {
            return 2;
        }
    }

    public class PriorityData3 : PriorityData
    {
        public override int GetNumPlayers()
        {
            return 3;
        }
    }

    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();

        public Dictionary<int, Direction> Directions = new()
        {
            [0] = Direction.None,
            [1] = Direction.None,
            [2] = Direction.None
        };

        public PriorityData1 PriorityData1 = new();
        public PriorityData2 PriorityData2 = new();
        public PriorityData3 PriorityData3 = new();

        public int PlayersCount = 2;
    }
}
