using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using ECommons.Throttlers;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M12S_P1_Mortal_Slayer : SplatoonScript
{
    public enum BallKind
    {
        Green,
        Purple
    }

    public enum Direction
    {
        East,
        West
    }

    public enum State
    {
        Idle,
        Start,
        Active1,
        Active2,
        Active3,
        Active4,
        End
    }

    private int _actiondBallCount;
    private Direction _myDir;
    private bool _myFirst;
    private float _myOffset;
    private string _myText = "";
    private int _myWave;
    private List<PlayerData> _playerOrderForBalls = [];

    private List<BallData> _spawnedBalls = [];

    private State _state = State.Idle;
    public override Metadata Metadata => new(1, "Garume");
    public override HashSet<uint>? ValidTerritories => [1327];

    public Config C => Controller.GetConfig<Config>();

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if (packet->ActionID == 46229)
        {
            _state = State.Start;
            _spawnedBalls.Clear();
            _actiondBallCount = 0;
            _playerOrderForBalls.Clear();
            _myWave = 0;
            _myText = "";
            _myOffset = 0f;
        }
    }

    public override void OnSetup()
    {
        Controller.RegisterElement("Guide",
            new Element(0)
            {
                radius = 2f, thicc = 10f, tether = true, overlayBGColor = 0xFF000000, overlayTextColor = 0xFFFFFFFF,
                overlayVOffset = 2f, overlayFScale = 2f
            });
    }

    public override void OnUpdate()
    {
        if (_spawnedBalls.Count == 8 && EzThrottler.Throttle("M12S_Mortal_Slayer_TryBuildPlayerOrder", 100))
            TryBuildPlayerOrder();

        var e = Controller.GetElementByName("Guide");
        e.Enabled = false;
        e.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
        e.overlayText = "";
        
        if (_state is State.Idle or State.End) return;

        var currentWave = _state is >= State.Active1 and <= State.Active4 ? (int)_state - (int)State.Active1 + 1 : 0;
        Vector2 pos = default;
        var show = false;
        var text = "";
        var myTurn = false;
        if (_myWave > 0)
        {
            myTurn = currentWave == _myWave;
            if (myTurn) pos = CalcPos(_myDir, _myFirst, _myOffset);
            text = _myText;
            show = true;
        }
        else if (currentWave > 0)
        {
            var myIndex = _playerOrderForBalls.IndexOf(x => x.ObjectId == Controller.BasePlayer.EntityId);
            var baseIndex = (currentWave - 1) * 2;
            if (myIndex == baseIndex || myIndex == baseIndex + 1)
            {
                var offset = _playerOrderForBalls[baseIndex].Direction == _playerOrderForBalls[baseIndex + 1].Direction
                    ? 3f
                    : 0f;
                var dir = _playerOrderForBalls[myIndex].Direction;
                var first = myIndex == baseIndex;
                pos = CalcPos(dir, first, offset);
                text = $"{(dir == Direction.East ? "E" : "W")} Wave:{currentWave}";
                myTurn = true;
                show = true;
            }
        }

        if (show)
        {
            e.Enabled = true;
            e.overlayText = text;
            if (myTurn)
            {
                e.radius = 2f;
                e.thicc = 10f;
                e.tether = true;
                e.SetRefPosition(pos.ToVector3());
            }
            else
            {
                e.radius = 0f;
                e.thicc = 0f;
                e.tether = false;
                e.SetRefPosition(Controller.BasePlayer.Position);
            }
        }
    }

    private static Vector2 CalcPos(Direction dir, bool first, float offset)
    {
        var dx = first ? -1f : 1f;
        var east = dir == Direction.East;
        var pos = new Vector2(east ? 107f : 93f, 90f);
        pos.X += dx * offset;
        pos.Y += (east ? -dx : dx) * offset;
        return pos;
    }

    private bool TryBuildPlayerOrder()
    {
        if (_spawnedBalls.Count != 8) return false;

        var purpleCount = _spawnedBalls.Count(b => b.Kind == BallKind.Purple);
        if (purpleCount != 2)
            DuoLog.Warning($"Expected 2 purple balls, but got {purpleCount}.");

        var order = BuildPlayerOrder(_spawnedBalls);
        if (order == null) return false;
        _playerOrderForBalls = order;
        SetMyOrderFromList(order);
        return true;
    }

    private List<PlayerData>? BuildPlayerOrder(List<BallData> balls)
    {
        var prio = C.PriorityData.GetPlayers(_ => true)?.ToList();
        if (prio?.Count < 8)
        {
            DuoLog.Warning($"PriorityData.GetPlayers() returned insufficient players. Count={prio?.Count ?? 0}");
            return null;
        }

        var ordered = balls.Chunk(2).SelectMany(chunk => chunk.OrderBy(b => b.Direction == Direction.West ? 0 : 1))
            .ToList();
        var ret = new List<PlayerData>(ordered.Count);
        var purpleSlot = 0;
        var greenSlot = 2;
        foreach (var b in ordered)
        {
            var idx = b.Kind == BallKind.Purple ? purpleSlot++ : greenSlot++;
            if ((uint)idx >= (uint)prio.Count)
            {
                DuoLog.Warning($"Priority index out of range. idx={idx}, prioCount={prio.Count}");
                return null;
            }

            var p = prio[idx];
            ret.Add(new PlayerData
            {
                Wave = b.Wave, Direction = b.Direction, Kind = b.Kind, ObjectId = p.IGameObject.EntityId,
                Name = p.IGameObject.Name.ToString()
            });
        }

        return ret;
    }

    private void SetMyOrderFromList(List<PlayerData> order)
    {
        if (_myWave != 0) return;
        var myIndex = order.FindIndex(x => x.ObjectId == Controller.BasePlayer.EntityId);
        if (myIndex < 0) return;
        var wave = myIndex / 2 + 1;
        var baseIndex = (wave - 1) * 2;
        if (order.Count < baseIndex + 2) return;
        var first = myIndex == baseIndex;
        var dir = order[myIndex].Direction;
        _myWave = wave;
        _myFirst = first;
        _myDir = dir;
        _myOffset = order[baseIndex].Direction == order[baseIndex + 1].Direction ? 3f : 0f;
        _myText = $"{(dir == Direction.East ? "E" : "W")} Wave:{wave}";
    }

    private void TrySetMyText(int wave)
    {
        if (_myWave != 0) return;
        var order = BuildPlayerOrder(_spawnedBalls);
        if (order == null) return;
        var baseIndex = (wave - 1) * 2;
        if (order.Count < baseIndex + 2) return;
        var a = order[baseIndex];
        var b = order[baseIndex + 1];
        var myId = Controller.BasePlayer.EntityId;
        if (a.ObjectId != myId && b.ObjectId != myId) return;
        var first = a.ObjectId == myId;
        var dir = first ? a.Direction : b.Direction;
        _myWave = wave;
        _myFirst = first;
        _myDir = dir;
        _myOffset = a.Direction == b.Direction ? 3f : 0f;
        _myText = $"{(dir == Direction.East ? "E" : "W")}{(first ? 1 : 2)} Wave:{wave}";
    }

    public override void OnObjectCreation(IntPtr newObjectPtr)
    {
        if (_state != State.Start) return;
        _ = new TickScheduler(() =>
        {
            var gameObject = Svc.Objects.FirstOrDefault(o => o.Address == newObjectPtr);
            var id = gameObject?.DataId ?? 0;
            if (id is not (19200 or 19201)) return;
            _spawnedBalls.Add(new BallData
            {
                Kind = id == 19200 ? BallKind.Purple : BallKind.Green,
                Wave = _spawnedBalls.Count / 2 + 1,
                Direction = gameObject.Position.X > 100 ? Direction.East : Direction.West
            });
            if (_spawnedBalls.Count % 2 == 0) TrySetMyText(_spawnedBalls.Count / 2);
            if (_spawnedBalls.Count < 8) return;
            _state = State.Active1;
            _spawnedBalls = _spawnedBalls.Chunk(2)
                .SelectMany(chunk => chunk.OrderBy(b => b.Direction == Direction.West ? 0 : 1)).ToList();
        });
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action.Value.RowId is 46232 or 46230 && ++_actiondBallCount % 2 == 0 &&
            _state is >= State.Active1 and <= State.Active4)
        {
            _state = _state == State.Active4 ? State.End : (State)((int)_state + 1);
            var currentWave = _state is >= State.Active1 and <= State.Active4
                ? (int)_state - (int)State.Active1 + 1
                : 0;
            if (_myWave > 0 && (_state == State.End || currentWave > _myWave))
            {
                _myWave = 0;
                _myText = "";
                _myOffset = 0f;
            }
        }
    }

    public override void OnSettingsDraw()
    {
        C.PriorityData.Draw();
        ImGui.ColorEdit4("Color1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Color2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGuiEx.EzTable("Balls Information", _spawnedBalls.SelectMany(x => new[]
            {
                new ImGuiEx.EzTableEntry("Wave", () => ImGuiEx.Text(x.Wave.ToString())),
                new ImGuiEx.EzTableEntry("Kind", () => ImGuiEx.Text(x.Kind.ToString())),
                new ImGuiEx.EzTableEntry("Direction", () => ImGuiEx.Text(x.Direction.ToString()))
            }));

            ImGuiEx.EzTable("Player Order For Balls", _playerOrderForBalls.SelectMany(x => new[]
            {
                new ImGuiEx.EzTableEntry("Wave", () => ImGuiEx.Text(x.Wave.ToString())),
                new ImGuiEx.EzTableEntry("Kind", () => ImGuiEx.Text(x.Kind.ToString())),
                new ImGuiEx.EzTableEntry("Direction", () => ImGuiEx.Text(x.Direction.ToString())),
                new ImGuiEx.EzTableEntry("ObjectId", () => ImGuiEx.Text(x.ObjectId.ToString())),
                new ImGuiEx.EzTableEntry("Name", () => ImGuiEx.Text(x.Name))
            }));

            ImGui.Text($"My index: {_playerOrderForBalls.IndexOf(x => x.ObjectId == Controller.BasePlayer.EntityId)}");
        }
    }

    public class BallData
    {
        public Direction Direction;
        public BallKind Kind;
        public int Wave;
    }

    public class PlayerData
    {
        public Direction Direction;
        public BallKind Kind;
        public string Name = "";
        public uint ObjectId;
        public int Wave;
    }

    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public PriorityData PriorityData = new();
    }
}
