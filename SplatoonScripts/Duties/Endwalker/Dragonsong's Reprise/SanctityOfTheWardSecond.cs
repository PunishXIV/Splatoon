using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public class SanctityOfTheWardSecond : SplatoonScript
{
    public enum SpreadDirection
    {
        North,
        East,
        South,
        West
    }

    private static readonly Vector2 InnerNorth = new(100, 90);
    private static readonly Vector2 InnerEast = new(110, 100);
    private static readonly Vector2 InnerSouth = new(100, 110);
    private static readonly Vector2 InnerWest = new(90, 100);
    private static readonly Vector2 OuterNorth = new(100, 80);
    private static readonly Vector2 OuterEast = new(120, 100);
    private static readonly Vector2 OuterSouth = new(100, 120);
    private static readonly Vector2 OuterWest = new(80, 100);
    private static readonly Vector2 Center = new(100, 100);

    private readonly List<IGameObject> _innerTowers = new();
    private readonly List<IGameObject> _outerEastTowers = new();
    private readonly List<IGameObject> _outerNorthTowers = new();
    private readonly List<IGameObject> _outerSouthTowers = new();
    private readonly List<IGameObject> _outerWestTowers = new();

    private SpreadDirection _fixedSpreadDirection;

    private bool _isStart;

    private bool _shouldInduceCommet;

    private bool _shouldPrioritizeOuterTower;

    public List<IGameObject> MyTowers = new();
    public override HashSet<uint>? ValidTerritories => [968];

    public override Metadata? Metadata => new(1, "Garume");

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 29563)
        {
            PluginLog.Log("Starting cast: " + source);
            _isStart = true;
            var towers = Svc.Objects.Where(x => x is IBattleNpc { NameId: 3640, DataId: 9020 })
                .OrderBy(x => x.Position.X)
                .ThenBy(y => y.Position.Z);

            foreach (var tower in towers)
            {
                var centerDistance = Vector2.Distance(tower.Position.ToVector2(), Center);
                var northDistance = Vector2.Distance(tower.Position.ToVector2(), OuterNorth);
                var eastDistance = Vector2.Distance(tower.Position.ToVector2(), OuterEast);
                var southDistance = Vector2.Distance(tower.Position.ToVector2(), OuterSouth);
                var westDistance = Vector2.Distance(tower.Position.ToVector2(), OuterWest);

                if (centerDistance < 8f)
                    _innerTowers.Add(tower);
                else if (northDistance < 12f)
                    _outerNorthTowers.Add(tower);
                else if (eastDistance < 12f)
                    _outerEastTowers.Add(tower);
                else if (southDistance < 12f)
                    _outerSouthTowers.Add(tower);
                else if (westDistance < 12f) _outerWestTowers.Add(tower);
            }

            Controller.Schedule(() =>
            {
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
                _isStart = false;
            }, 40 * 1000);
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (vfxPath == "vfx/lockon/eff/r1fz_holymeteo_s12x.avfx")
        {
            if (target.GetObject().Name.ToString() == Player.Name) _shouldInduceCommet = true;

            if (target.GetObject() is IPlayerCharacter character)
            {
                if (character.GetRole() == CombatRole.DPS && Player.Object.GetRole() == CombatRole.DPS)
                    _shouldPrioritizeOuterTower = true;
                else if ((character.GetRole() == CombatRole.Healer || character.GetRole() == CombatRole.Tank) &&
                         (Player.Object.GetRole() == CombatRole.Healer || Player.Object.GetRole() == CombatRole.Tank))
                    _shouldPrioritizeOuterTower = true;
            }
        }
    }

    public override void OnReset()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        _isStart = false;
        _shouldInduceCommet = false;
        _shouldPrioritizeOuterTower = false;
        _innerTowers.Clear();
        _outerNorthTowers.Clear();
        _outerEastTowers.Clear();
        _outerSouthTowers.Clear();
        _outerWestTowers.Clear();
        MyTowers.Clear();
    }

    public override void OnSetup()
    {
        var baitElement = new Element(0);
        var baitElement2 = new Element(0);
        var baitElement3 = new Element(0);
        Controller.TryRegisterElement("bait1", baitElement);
        Controller.TryRegisterElement("bait2", baitElement2);
        Controller.TryRegisterElement("bait3", baitElement3);
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!_isStart)
            return;

        if (set.Action.RowId == 25575)
        {
            var position = Player.Position.ToVector2();

            if (Vector2.Distance(position, InnerNorth) < 10f)
                _fixedSpreadDirection = SpreadDirection.North;
            else if (Vector2.Distance(position, InnerEast) < 10f)
                _fixedSpreadDirection = SpreadDirection.East;
            else if (Vector2.Distance(position, InnerSouth) < 10f)
                _fixedSpreadDirection = SpreadDirection.South;
            else if (Vector2.Distance(position, InnerWest) < 10f) _fixedSpreadDirection = SpreadDirection.West;


            if (_shouldPrioritizeOuterTower)
                switch (_fixedSpreadDirection)
                {
                    case SpreadDirection.North:
                        MyTowers = _outerNorthTowers
                            .Where(x => Vector2.Distance(x.Position.ToVector2(), OuterNorth) < 1f).ToList();
                        if (MyTowers.Count == 0)
                            MyTowers = _outerNorthTowers.ToList();
                        break;
                    case SpreadDirection.East:
                        MyTowers = _outerEastTowers.Where(x => Vector2.Distance(x.Position.ToVector2(), OuterEast) < 1f)
                            .ToList();
                        if (MyTowers.Count == 0)
                            MyTowers = _outerEastTowers.ToList();
                        break;
                    case SpreadDirection.South:
                        MyTowers = _outerSouthTowers
                            .Where(x => Vector2.Distance(x.Position.ToVector2(), OuterSouth) < 1f).ToList();
                        if (MyTowers.Count == 0)
                            MyTowers = _outerSouthTowers.ToList();
                        break;
                    case SpreadDirection.West:
                        MyTowers = _outerWestTowers.Where(x => Vector2.Distance(x.Position.ToVector2(), OuterWest) < 1f)
                            .ToList();
                        if (MyTowers.Count == 0)
                            MyTowers = _outerWestTowers.ToList();
                        break;
                    default:
                        MyTowers = _innerTowers.ToList();
                        break;
                }
            else
                switch (_fixedSpreadDirection)
                {
                    case SpreadDirection.North:
                        if (_outerNorthTowers.Count > 1)
                            MyTowers = _outerNorthTowers
                                .Where(x => Vector2.Distance(x.Position.ToVector2(), OuterNorth) > 5f).ToList();
                        else
                            MyTowers = _innerTowers.ToList();
                        break;
                    case SpreadDirection.East:
                        if (_outerEastTowers.Count > 1)
                            MyTowers = _outerEastTowers
                                .Where(x => Vector2.Distance(x.Position.ToVector2(), OuterEast) > 5f).ToList();
                        else
                            MyTowers = _innerTowers.ToList();
                        break;
                    case SpreadDirection.South:
                        if (_outerSouthTowers.Count > 1)
                            MyTowers = _outerSouthTowers
                                .Where(x => Vector2.Distance(x.Position.ToVector2(), OuterSouth) > 5f).ToList();
                        else
                            MyTowers = _innerTowers.ToList();
                        break;
                    case SpreadDirection.West:
                        if (_outerWestTowers.Count > 1)
                            MyTowers = _outerWestTowers
                                .Where(x => Vector2.Distance(x.Position.ToVector2(), OuterWest) > 5f).ToList();
                        else
                            MyTowers = _innerTowers.ToList();
                        break;
                    default:
                        MyTowers = _innerTowers.ToList();
                        break;
                }

            for (var i = 0; i < MyTowers.Count; i++)
            {
                var towerPosition = MyTowers[i].Position;
                if (Controller.TryGetElementByName($"bait{i + 1}", out var element))
                {
                    element.Enabled = true;
                    element.tether = true;
                    element.SetOffPosition(towerPosition);
                }
            }
        }

        if (set.Action.RowId == 29564)
            if (!_shouldPrioritizeOuterTower)
                switch (_fixedSpreadDirection)
                {
                    case SpreadDirection.East:
                        if (Controller.TryGetElementByName("bait1", out var eastElement1))
                        {
                            eastElement1.Enabled = true;
                            eastElement1.tether = true;
                            eastElement1.SetOffPosition(new Vector3(104, 0f, 104f));
                        }

                        if (Controller.TryGetElementByName("bait2", out var eastElement2))
                        {
                            eastElement2.Enabled = true;
                            eastElement2.tether = true;
                            eastElement2.SetOffPosition(new Vector3(115, 0f, 115f));
                        }

                        break;

                    case SpreadDirection.South:
                        if (Controller.TryGetElementByName("bait1", out var southElement1))
                        {
                            southElement1.Enabled = true;
                            southElement1.tether = true;
                            southElement1.SetOffPosition(new Vector3(96f, 0f, 104f));
                        }

                        if (Controller.TryGetElementByName("bait2", out var element2))
                        {
                            element2.Enabled = true;
                            element2.tether = true;
                            element2.SetOffPosition(new Vector3(85, 0f, 115f));
                        }

                        break;

                    case SpreadDirection.West:
                        if (Controller.TryGetElementByName("bait1", out var westElement1))
                        {
                            westElement1.Enabled = true;
                            westElement1.tether = true;
                            westElement1.SetOffPosition(new Vector3(96f, 0f, 96f));
                        }

                        if (Controller.TryGetElementByName("bait2", out var westElement2))
                        {
                            westElement2.Enabled = true;
                            westElement2.tether = true;
                            westElement2.SetOffPosition(new Vector3(85, 0f, 85f));
                        }

                        break;

                    case SpreadDirection.North:
                        if (Controller.TryGetElementByName("bait1", out var northElement1))
                        {
                            northElement1.Enabled = true;
                            northElement1.tether = true;
                            northElement1.SetOffPosition(new Vector3(104f, 0f, 96f));
                        }

                        if (Controller.TryGetElementByName("bait2", out var northElement2))
                        {
                            northElement2.Enabled = true;
                            northElement2.tether = true;
                            northElement2.SetOffPosition(new Vector3(115, 0f, 85f));
                        }

                        break;
                }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Inner");
        foreach (var tower in _innerTowers)
        {
            ImGui.Text(tower.Name.ToString());
            ImGui.SameLine();
            ImGui.Text(tower.Position.ToString());
        }

        ImGui.Text("Outer North");
        foreach (var tower in _outerNorthTowers)
        {
            ImGui.Text(tower.Name.ToString());
            ImGui.SameLine();
            ImGui.Text(tower.Position.ToString());
        }

        ImGui.Text("Outer East");
        foreach (var tower in _outerEastTowers)
        {
            ImGui.Text(tower.Name.ToString());
            ImGui.SameLine();
            ImGui.Text(tower.Position.ToString());
        }

        ImGui.Text("Outer South");
        foreach (var tower in _outerSouthTowers)
        {
            ImGui.Text(tower.Name.ToString());
            ImGui.SameLine();
            ImGui.Text(tower.Position.ToString());
        }

        ImGui.Text("Outer West");
        foreach (var tower in _outerWestTowers)
        {
            ImGui.Text(tower.Name.ToString());
            ImGui.SameLine();
            ImGui.Text(tower.Position.ToString());
        }

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Text("My Towers");
        foreach (var tower in MyTowers)
        {
            ImGui.Text(tower.Name.ToString());
            ImGui.SameLine();
            ImGui.Text(tower.Position.ToString());
        }
    }
}