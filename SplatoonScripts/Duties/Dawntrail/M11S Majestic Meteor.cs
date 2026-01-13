using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M11S_Majestic_Meteor : SplatoonScript
{
    private readonly uint _actionFinalResolve = 46146;
    private readonly uint _actionPuddleTick = 46145;
    private readonly uint _actionTowerResolve = 46148;
    private readonly uint _castChampionsMeteorStart = 46144;
    private readonly HashSet<uint> _castArcadianCrash = new() { 46154, 46156, 46158, 46160 };
    private static readonly Dictionary<uint, Vector2> MapEffectToPos = new()
    {
        { 22, new(79f, 75f) },
        { 23, new(89f, 75f) },
        { 24, new(111f, 75f) },
        { 25, new(121f, 75f) }
    };

    private readonly Vector2 _center = new(100f, 100f);
    private readonly float _finalLateralXOffset = 1.5f;
    private readonly float _finalLineDistance = 18.0f;
    private readonly float _finalNoLineDistance = 14.0f;
    private readonly HashSet<(uint, Direction)> _linePlayers = new();
    private readonly float _markerLineDistance = 16.0f;
    private readonly float _markerNoLineDistance = 8.0f;
    private readonly float _puddleStep1Offset = 0.0f;
    private readonly float _puddleStep2Offset = 6.0f;
    private readonly float _puddleStep3Offset = 12.0f;
    private readonly Vector2 _towerNWBase = new(84f, 89f);
    private readonly float _towerStandOff = 2f;
    private readonly string _vfxPuddleStartPrefix = "vfx/lockon/eff/lockon8_t0w.avfx";

    private string _basePlayerOverride = "";

    private Element? _eGuide;
    private bool _flipLR;
    private int _gimmicCount;


    private bool _latchedTethers;
    private List<(uint, Direction)> _lineOrder = new();
    private List<uint> _noLineOrder = new();
    private int _puddleCount;
    private readonly Dictionary<uint, Direction> _finalSafeTowerDir = new();
    private bool _showFinalTowerBait;
    private readonly List<Vector2> _mapEffectPositions = new();

    private State _state = State.Idle;
    public override Metadata Metadata => new(6, "Garume");
    public override HashSet<uint>? ValidTerritories { get; } = [1325];
    private Config C => Controller.GetConfig<Config>();

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

    public override void OnSetup()
    {
        Controller.RegisterElement("Guide", new Element(0)
        {
            radius = 1.5f,
            thicc = 10f,
            overlayVOffset = 3f,
            overlayFScale = 3f,
            tether = true
        });

        _eGuide ??= Controller.GetElementByName("Guide");
    }

    public override void OnEnable()
    {
        ResetAll();
    }

    public override void OnReset()
    {
        ResetAll();
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe))
            ResetAll();
    }

    private void ResetAll()
    {
        _state = State.Idle;

        _linePlayers.Clear();
        _lineOrder = new List<(uint, Direction)>();
        _noLineOrder = new List<uint>();

        _latchedTethers = false;
        _flipLR = false;
        _gimmicCount = 0;
        _finalSafeTowerDir.Clear();
        _showFinalTowerBait = false;
        _mapEffectPositions.Clear();

        if (_eGuide != null)
            _eGuide.Enabled = false;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == _castChampionsMeteorStart && _state == State.Idle)
        {
            _state = State.WaitTethers;
            _linePlayers.Clear();
            _lineOrder = new List<(uint, Direction)>();
            _noLineOrder = new List<uint>();
            _latchedTethers = false;
            _flipLR = false;
            _mapEffectPositions.Clear();
        }
        else if (castId == _actionPuddleTick && _state is State.Puddles1 or State.Puddles2 or State.Puddles3)
        {
            _puddleCount++;
            if (_puddleCount >= 8)
            {
                _puddleCount = 0;
                    _state = _state switch
                    {
                        State.Puddles1 => State.Puddles2,
                        State.Puddles2 => State.Puddles3,
                        State.Puddles3 => State.FinalSafe,
                        _ => _state
                    };
            }
        }
        else if (_state == State.FinalTowerBait && _castArcadianCrash.Contains(castId))
        {
            _showFinalTowerBait = true;
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_state != State.WaitTethers) return;
        if (_latchedTethers) return;
        if (data3 != 57 || data5 != 15) return;

        var srcObj = source.GetObject();
        var tgtObj = target.GetObject();
        if (srcObj == null || tgtObj == null) return;

        Direction dir;
        if (srcObj.Position.X > _center.X)
            dir = srcObj.Position.Z > _center.Y ? Direction.SouthEast : Direction.NorthEast;
        else
            dir = srcObj.Position.Z > _center.Y ? Direction.SouthWest : Direction.NorthWest;
        _linePlayers.Add((target, dir));

        if (_linePlayers.Count >= 4)
        {
            _latchedTethers = true;

            BuildRoleOrders();
            _state = State.TowerBait;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (_state != State.MarkerBait) return;
        if (string.IsNullOrEmpty(_vfxPuddleStartPrefix)) return;

        if (vfxPath.StartsWith(_vfxPuddleStartPrefix, StringComparison.OrdinalIgnoreCase)) _state = State.Puddles1;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action == null) return;
        var actionId = set.Action.Value.RowId;

        if (_state == State.TowerBait && actionId == _actionTowerResolve)
        {
            _state = State.MarkerBait;
            return;
        }

        if (_state == State.FinalSafe && actionId == _actionFinalResolve)
        {
            _gimmicCount++;
            if (_gimmicCount == 1)
            {
                _state = State.WaitTethers;
                _linePlayers.Clear();
                _lineOrder.Clear();
                _noLineOrder.Clear();
                _latchedTethers = false;
                _flipLR = false;
                _mapEffectPositions.Clear();
            }
            else
            {
                _state = State.FinalTowerBait;
                _showFinalTowerBait = false;
            }
        }
        else if (_state == State.FinalTowerBait && _castArcadianCrash.Contains(actionId))
        {
            _state = State.Done;
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if (_state is State.Idle or State.Done) return;
        if (position is < 22 or > 25) return;
        if (data1 != 1) return;
        if (!MapEffectToPos.TryGetValue(position, out var pos)) return;

        _mapEffectPositions.Add(pos);
        _flipLR = _mapEffectPositions.Any(p => Math.Abs(p.X - 79f) < 0.01f);
    }

    public override void OnUpdate()
    {
        if (_eGuide == null) return;
        _eGuide.Enabled = false;

        if (_state == State.Idle || _state == State.Done) return;

        var grad = GradientColor.Get(C.GradientA, C.GradientB, 333).ToUint();
        _eGuide.color = grad;
        _eGuide.overlayBGColor = 0xFF000000;

        var myId = BasePlayer.EntityId;

        var isLine = _linePlayers.Any(x => x.Item1 == myId);
        var isNoLine = _noLineOrder.Contains(myId);
        var isEast = BasePlayer.Position.X > _center.X;
        if (_state == State.TowerBait)
        {
            if (!isLine && !isNoLine) return;
            
            var originalSideIsEast =
                _linePlayers.FirstOrDefault(x => x.Item1 == myId).Item2 == Direction.NorthEast ||
                _linePlayers.FirstOrDefault(x => x.Item1 == myId).Item2 == Direction.SouthEast;
            var same = isEast == originalSideIsEast;
            
            if (C.RealtimeTowerBaitSecond && _gimmicCount == 1)
            {
                var dir = GetDirectionFromWorld(BasePlayer.Position);
                var tower = dir switch
                {
                    Direction.NorthEast => TowerNE(),
                    Direction.SouthEast => TowerSE(),
                    Direction.SouthWest => TowerSW(),
                    _ => TowerNW()
                };

                var isNorthTower = dir is Direction.NorthEast or Direction.NorthWest;
                Vector2 stand;
                if (isLine)
                {
                    if (same)
                    {
                        var offset = (tower.X > _center.X) ? -_towerStandOff : _towerStandOff;
                        stand = tower with { X = tower.X + offset };
                    }
                    else
                    {
                        stand = tower with { Y = tower.Y + (isNorthTower ? _towerStandOff : -_towerStandOff) };
                    }
                }
                else
                {
                    stand = tower with { Y = tower.Y + (isNorthTower ? _towerStandOff : -_towerStandOff) };
                }

                SetGuide(stand);
                return;
            }

            if (C.TetherShouldGoNorth)
            {
                if (isLine)
                {
                    var offset = isEast ? _towerStandOff * -1f : _towerStandOff;
                    var tower = isEast ? TowerNE() : TowerNW();
                    var stand = same
                        ? tower with { X = tower.X + offset }
                        : tower with { Y = tower.Y + _towerStandOff };
                    SetGuide(stand);
                }
                else
                {
                    var tower = isEast ? TowerSE() : TowerSW();
                    SetGuide(tower with { Y = tower.Y - _towerStandOff });
                }
            }
            else
            {
                Direction myTowerDir;
                if (isEast)
                {
                    var myIndex = C.PlayerData.GetPlayers(x => x.IGameObject.Position.X > _center.X)
                        .IndexOf(x => x.IGameObject.EntityId == myId);
                    myTowerDir = myIndex is 0 or 1 ? Direction.NorthEast : Direction.SouthEast;
                }
                else
                {
                    var myIndex = C.PlayerData.GetPlayers(x => x.IGameObject.Position.X < _center.X)
                        .IndexOf(x => x.IGameObject.EntityId == myId);
                    myTowerDir = myIndex is 0 or 1 ? Direction.NorthWest : Direction.SouthWest;
                }

                Vector2 stand;
                switch (myTowerDir)
                {
                    case Direction.NorthEast:
                    {
                        if (isLine && same)
                            stand = TowerNE() with { X = TowerNE().X - _towerStandOff };
                        else
                            stand = TowerNE() with { Y = TowerNE().Y + _towerStandOff };

                        break;
                    }
                    case Direction.NorthWest:
                    {
                        if (isLine && same)
                            stand = TowerNW() with { X = TowerNW().X + _towerStandOff };
                        else
                            stand = TowerNW() with { Y = TowerNW().Y + _towerStandOff };

                        break;
                    }
                    case Direction.SouthEast:
                    {
                        if (isLine && same)
                            stand = TowerSE() with { X = TowerSE().X - _towerStandOff };
                        else
                            stand = TowerSE() with { Y = TowerSE().Y - _towerStandOff };

                        break;
                    }
                    default:
                    {
                        if (isLine && same)
                            stand = TowerSW() with { X = TowerSW().X + _towerStandOff };
                        else
                            stand = TowerSW() with { Y = TowerSW().Y - _towerStandOff };

                        break;
                    }
                }

                SetGuide(stand);
            }

            return;
        }

        if (_state == State.MarkerBait)
        {
            if (!isLine && !isNoLine) return;

            var dist = isLine ? _markerLineDistance : _markerNoLineDistance;
            var offsetX = BasePlayer.Position.X > _center.X ? dist : -dist;
            var pos = _center with { X = _center.X + offsetX };

            SetGuide(pos);
            return;
        }

        if (_state is State.Puddles1 or State.Puddles2 or State.Puddles3)
        {
            if (!isLine && !isNoLine) return;
            bool isUp;
            if (isLine)
            {
                var myDir = _lineOrder.FirstOrDefault(x => x.Item1 == myId).Item2;
                isUp = myDir is Direction.SouthEast or Direction.SouthWest;
            }
            else
            {
                var myIndex = _noLineOrder.Where(x => x.GetObject()!.Position.X > _center.X == isEast).ToList()
                    .IndexOf(myId);
                isUp = myIndex == 0;
            }

            var posX = isEast ? 108f : 92f;
            var stepOffset = _state switch
            {
                State.Puddles1 => _puddleStep1Offset,
                State.Puddles2 => _puddleStep2Offset,
                _ => _puddleStep3Offset
            };

            stepOffset = isUp ? -stepOffset : +stepOffset;

            var pos = new Vector2(posX, 100f + stepOffset);
            var text = isUp ? "Up" : "Down";

            SetGuide(pos, text);
            return;
        }

        if (_state == State.FinalSafe)
        {
            if (!isLine && !isNoLine) return;
            var posX = isEast ? 116f : 84f;
            posX += _flipLR ? _finalLateralXOffset : -_finalLateralXOffset;
            Vector2 pos;
            if (isLine)
            {
                var myDir = _lineOrder.FirstOrDefault(x => x.Item1 == myId).Item2;
                var isUp = myDir is Direction.SouthEast or Direction.SouthWest;
                var posY = isUp ? 81f : 119f;
                pos = new Vector2(posX, posY);
            }
            else
            {
                var myIndex = _noLineOrder.Where(x => x.GetObject()!.Position.X > _center.X == isEast).ToList()
                    .IndexOf(myId);
                var posY = myIndex == 0 ? 89f : 111f;
                pos = new Vector2(posX, posY);
            }

            SetGuide(pos);
            _finalSafeTowerDir[myId] = GetDirectionFromPosition(pos);
            return;
        }

        if (_state == State.FinalTowerBait)
        {
            if (!C.ShowFinalTowerBait) return;
            if (!_showFinalTowerBait) return;
            if (!_finalSafeTowerDir.TryGetValue(myId, out var dir)) return;

            var pos = dir switch
            {
                Direction.NorthEast => TowerNE(),
                Direction.SouthEast => TowerSE(),
                Direction.SouthWest => TowerSW(),
                _ => TowerNW()
            };

            SetGuide(pos);
        }
    }

    private void SetGuide(Vector2 xz, string text = "")
    {
        if (_eGuide == null) return;

        _eGuide.refX = xz.X;
        _eGuide.refY = xz.Y;
        _eGuide.refZ = 0f;

        _eGuide.overlayText = !string.IsNullOrEmpty(text) ? text : "";

        _eGuide.overlayTextColor = 0xFFFFFFFF;
        _eGuide.Enabled = true;
    }

    private void BuildRoleOrders()
    {
        var orderedParty = GetPriorityPartyIds();
        if (orderedParty.Count == 0)
        {
            _lineOrder = _linePlayers.Take(4).ToList();
            _noLineOrder = FakeParty.Get().Select(x => x.EntityId).Where(id => _linePlayers.All(y => y.Item1 != id))
                .Take(4).ToList();
            return;
        }

        _lineOrder = orderedParty.Where(id => _linePlayers.Any(x => x.Item1 == id)).Take(4).Select(id =>
            _linePlayers.First(x => x.Item1 == id)).ToList();
        _noLineOrder = orderedParty.Where(id => _linePlayers.All(x => x.Item1 != id)).Take(4).ToList();
    }

    private List<uint> GetPriorityPartyIds()
    {
        var result = new List<uint>();

        try
        {
            var prios = C.PlayerData.GetPlayers(_ => true);
            if (prios != null)
                foreach (var p in prios)
                {
                    var obj = p.IGameObject;
                    if (obj is { ObjectKind: ObjectKind.Player })
                        result.Add(obj.EntityId);
                }
        }
        catch
        {
            // ignore
        }

        if (result.Count == 0) result = FakeParty.Get().Select(x => x.EntityId).ToList();

        var baseName = (_basePlayerOverride ?? "").Trim();
        if (!string.IsNullOrEmpty(baseName) && result.Count > 0)
        {
            var idx = result.FindIndex(id => NameMatches(id, baseName));
            if (idx > 0)
            {
                var rotated = result.Skip(idx).Concat(result.Take(idx)).ToList();
                result = rotated;
            }
        }

        return result;
    }

    private bool NameMatches(uint entityId, string baseName)
    {
        var obj = entityId.GetObject();
        if (obj == null) return false;

        var n = obj.Name.ToString();
        if (string.Equals(n, baseName, StringComparison.OrdinalIgnoreCase)) return true;

        if (baseName.Contains('@'))
        {
            var prefix = baseName.Split('@')[0];
            return string.Equals(n, prefix, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private Vector2 TowerNW()
    {
        return _towerNWBase;
    }

    private Vector2 TowerNE()
    {
        return MirrorX(_towerNWBase);
    }

    private Vector2 TowerSW()
    {
        return MirrorZ(_towerNWBase);
    }

    private Vector2 TowerSE()
    {
        return MirrorXZ(_towerNWBase);
    }

    private Vector2 MirrorX(Vector2 p)
    {
        return new Vector2(2f * _center.X - p.X, p.Y);
    }

    private Vector2 MirrorZ(Vector2 p)
    {
        return new Vector2(p.X, 2f * _center.Y - p.Y);
    }

    private Vector2 MirrorXZ(Vector2 p)
    {
        return new Vector2(2f * _center.X - p.X, 2f * _center.Y - p.Y);
    }
    
    private Direction GetDirectionFromWorld(Vector3 pos)
    {
        var isEast = pos.X > _center.X;
        var isSouth = pos.Z > _center.Y;
        if (isEast && !isSouth) return Direction.NorthEast;
        if (isEast && isSouth) return Direction.SouthEast;
        if (!isEast && isSouth) return Direction.SouthWest;
        return Direction.NorthWest;
    }

    private Direction GetDirectionFromPosition(Vector2 pos)
    {
        var isEast = pos.X > _center.X;
        var isSouth = pos.Y > _center.Y;
        if (isEast && !isSouth) return Direction.NorthEast;
        if (isEast && isSouth) return Direction.SouthEast;
        if (!isEast && isSouth) return Direction.SouthWest;
        return Direction.NorthWest;
    }


    public override void OnSettingsDraw()
    {
        ImGuiEx.Text("■ 設定 / Settings");
        ImGui.Checkbox("Tether should go North", ref C.TetherShouldGoNorth);
        ImGui.Checkbox("2nd TowerBait: Realtime by direction", ref C.RealtimeTowerBaitSecond);
        ImGui.SameLine();
        ImGuiEx.HelpMarker("""
            2回目のTowerBaitのみ
            自分が今立っている方角（北東/南東/南西/北西）に応じて、ガイド先の塔をリアルタイムで切り替えます。
            1回目のTowerBaitは従来どおり（優先順位）です。

            Second TowerBait only
            Switches the guided tower in real time based on your current direction (NE/SE/SW/NW).
            The first TowerBait remains unchanged (priority).
        """);
        ImGui.Checkbox("Show Final Tower bait (after Arcadian Crash cast)", ref C.ShowFinalTowerBait);
        ImGui.SameLine();
        ImGuiEx.HelpMarker("""
            アルカディアンクラッシュの詠唱後
            2回目ギミック終了時に立っていた方角の塔の位置にガイドを出します。
            オフにするとこの再表示を行いません。
            
            After Arcadian Crash starts casting,
            shows a marker on the same tower side where you stood when the second tower mechanic ended.
            Turn off to disable this re-display.
        """);

        ImGuiEx.Text("Gradient (2 colors)");
        ImGui.ColorEdit4("Color A", ref C.GradientA, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Color B", ref C.GradientB, ImGuiColorEditFlags.NoInputs);

        ImGui.Separator();
        C.PlayerData.Draw();

        if (ImGui.CollapsingHeader("Guide (JP)"))
        {
            ImGui.BulletText("優先順位は MT組 → ST組 の順に入力してください。");
            ImGui.BulletText("西側を北から MT / H1 / M1 / R1、東側を北から ST / H2 / M2 / R2 で並ぶ場合、");
            ImGui.BulletText("優先順位リスト には 「MT H1 M1 R1 ST H2 M2 R2」 の順で入力します。");
            ImGui.BulletText("塔はこの優先順位に従って割り当てられます。");
            ImGui.BulletText("たとえば西側に MT / M1 / ST / R2 がいる場合、優先順位が高い MT と M1 が北側の塔に入ります。");
            ImGui.BulletText("また、テザー持ちを必ず北側の塔に入れる運用にしたい場合は、「Tether should go North」 にチェックを入れてください。");
        }

        if (ImGui.CollapsingHeader("Guide (EN)"))
        {
            ImGui.BulletText("Please fill in the priority order as MT group first → ST group second.");
            ImGui.BulletText(
                "If your lineup is West side (from North): MT / H1 / M1 / R1, and East side (from North): ST / H2 / M2 / R2,");
            ImGui.BulletText("enter the PriorityData as: “MT H1 M1 R1 ST H2 M2 R2”.");
            ImGui.BulletText("Tower assignments follow this priority order.");
            ImGui.BulletText(
                "For example, if the West side has MT / M1 / ST / R2, the two highest-priority players—MT and M1—will take the North tower.");
            ImGui.BulletText(
                "If you want tethered players to always take the North towers, enable “Tether should go North.”");
        }

        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text(
                $"Line players: {string.Join(", ", _linePlayers.Select(x => x.Item1.GetObject()?.Name + x.Item2.ToString()))}");
            ImGui.Text($"Line order: {string.Join(", ", _lineOrder.Select(x => x.Item1.GetObject()!.Name))}");
            ImGui.Text($"NoLine order: {string.Join(", ", _noLineOrder.Select(x => x.GetObject()!.Name))}");
            ImGui.Text($"FlipLR: {_flipLR}");

            ImGui.SetNextItemWidth(200);
            ImGui.InputText("Player override", ref _basePlayerOverride, 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.BeginCombo("Select..", "Select..."))
            {
                foreach (var x in Svc.Objects.OfType<IPlayerCharacter>())
                    if (ImGui.Selectable(x.GetNameWithWorld()))
                        _basePlayerOverride = x.Name.ToString();
                ImGui.EndCombo();
            }
        }
    }

    private enum Direction
    {
        NorthEast,
        SouthEast,
        SouthWest,
        NorthWest
    }

    public class Config : IEzConfig
    {
        public Vector4 GradientA = ImGuiColors.DalamudYellow;
        public Vector4 GradientB = ImGuiColors.DalamudRed;
        public PriorityData PlayerData = new();
        public bool TetherShouldGoNorth;
        public bool ShowFinalTowerBait = true;
        public bool RealtimeTowerBaitSecond = false;
    }


    private enum State
    {
        Idle = 0,
        WaitTethers,
        TowerBait,
        MarkerBait,
        Puddles1,
        Puddles2,
        Puddles3,
        FinalSafe,
        FinalTowerBait,
        Done
    }
}

