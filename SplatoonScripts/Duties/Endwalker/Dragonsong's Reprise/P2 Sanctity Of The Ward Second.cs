using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using Player = ECommons.GameHelpers.LegacyPlayer.Player;
using ECommons.GameHelpers.LegacyPlayer;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Ipc.Exceptions;
using ECommons.ExcelServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ECommons.Automation;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public class P2_Sanctity_Of_The_Ward_Second : SplatoonScript
{
    private enum SpreadDirection
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

    private readonly List<IGameObject> _innerTowers = [];
    private readonly List<IGameObject> _outerEastTowers = [];
    private readonly List<IGameObject> _outerNorthTowers = [];
    private readonly List<IGameObject> _outerSouthTowers = [];
    private readonly List<IGameObject> _outerWestTowers = [];

    private SpreadDirection _fixedSpreadDirection;

    private bool _isFirstTowerPhase;
    private bool _isSecondTowerPhase;

    private bool _isStart;

    private Vector2 _lastPlayerPosition;

    private bool _shouldInduceCommet;

    private bool _shouldPrioritizeOuterTower;

    public List<IGameObject> MyTowers = [];
    public override HashSet<uint>? ValidTerritories => [968];

    public override Metadata? Metadata => new(4, "Garume");

    private Config C => Controller.GetConfig<Config>();

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 29563)
        {
            PluginLog.Log("Starting cast: " + source);
            _isStart = true;
            var towers = Svc.Objects.Where(x => x is IBattleNpc { NameId: 3640, DataId: 9020 })
                .OrderBy(x => x.Position.X)
                .ThenBy(y => y.Position.Z);

            foreach(var tower in towers)
            {
                var centerDistance = Vector2.Distance(tower.Position.ToVector2(), Center);
                var northDistance = Vector2.Distance(tower.Position.ToVector2(), OuterNorth);
                var eastDistance = Vector2.Distance(tower.Position.ToVector2(), OuterEast);
                var southDistance = Vector2.Distance(tower.Position.ToVector2(), OuterSouth);
                var westDistance = Vector2.Distance(tower.Position.ToVector2(), OuterWest);

                if(centerDistance < 8f)
                    _innerTowers.Add(tower);
                else if(northDistance < 12f)
                    _outerNorthTowers.Add(tower);
                else if(eastDistance < 12f)
                    _outerEastTowers.Add(tower);
                else if(southDistance < 12f)
                    _outerSouthTowers.Add(tower);
                else if(westDistance < 12f) _outerWestTowers.Add(tower);
            }

            Controller.Schedule(() =>
            {
                Controller.GetRegisteredElements().Where(x => x.Key.StartsWith("bait")).Each(x => x.Value.Enabled = false);
                _isStart = false;
            }, 40 * 1000);
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(vfxPath == "vfx/lockon/eff/r1fz_holymeteo_s12x.avfx")
        {
            if(target.GetObject().Name.ToString() == Player.Name) _shouldInduceCommet = true;

            if(target.GetObject() is IPlayerCharacter character)
            {
                if(character.GetRole() == CombatRole.DPS && Player.Object.GetRole() == CombatRole.DPS)
                    _shouldPrioritizeOuterTower = true;
                else if((character.GetRole() == CombatRole.Healer || character.GetRole() == CombatRole.Tank) &&
                         (Player.Object.GetRole() == CombatRole.Healer || Player.Object.GetRole() == CombatRole.Tank))
                    _shouldPrioritizeOuterTower = true;
            }
        }
    }

    public override void OnReset()
    {
        Controller.GetRegisteredElements().Where(x => x.Key.StartsWith("bait")).Each(x => x.Value.Enabled = false);
        _isStart = false;
        _isFirstTowerPhase = false;
        _isSecondTowerPhase = false;
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
        for(var i = 0; i < 3; i++)
        {
            var element = new Element(0);
            Controller.TryRegisterElement($"bait{i + 1}", element, true);
        }

        Controller.RegisterElementFromCode("AdjustCall", """{"Name":"","refX":100.0,"refY":100.0,"radius":3.0,"color":3356884736,"Filled":false,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"thicc":8.0,"overlayText":"ADJUST","tether":true}""");
    }

    private void SetTowers(Vector2 playerPosition)
    {
        if(Vector2.Distance(playerPosition, InnerNorth) < 10f)
            _fixedSpreadDirection = SpreadDirection.North;
        else if(Vector2.Distance(playerPosition, InnerEast) < 10f)
            _fixedSpreadDirection = SpreadDirection.East;
        else if(Vector2.Distance(playerPosition, InnerSouth) < 10f)
            _fixedSpreadDirection = SpreadDirection.South;
        else if(Vector2.Distance(playerPosition, InnerWest) < 10f) _fixedSpreadDirection = SpreadDirection.West;


        if(_shouldPrioritizeOuterTower)
            switch(_fixedSpreadDirection)
            {
                case SpreadDirection.North:
                    MyTowers = _outerNorthTowers
                        .Where(x => Vector2.Distance(x.Position.ToVector2(), OuterNorth) < 3f).ToList();
                    if(MyTowers.Count == 0)
                        MyTowers = _outerNorthTowers.ToList();
                    break;
                case SpreadDirection.East:
                    MyTowers = _outerEastTowers.Where(x => Vector2.Distance(x.Position.ToVector2(), OuterEast) < 3f)
                        .ToList();
                    if(MyTowers.Count == 0)
                        MyTowers = _outerEastTowers.ToList();
                    break;
                case SpreadDirection.South:
                    MyTowers = _outerSouthTowers
                        .Where(x => Vector2.Distance(x.Position.ToVector2(), OuterSouth) < 3f).ToList();
                    if(MyTowers.Count == 0)
                        MyTowers = _outerSouthTowers.ToList();
                    break;
                case SpreadDirection.West:
                    MyTowers = _outerWestTowers.Where(x => Vector2.Distance(x.Position.ToVector2(), OuterWest) < 3f)
                        .ToList();
                    if(MyTowers.Count == 0)
                        MyTowers = _outerWestTowers.ToList();
                    break;
                default:
                    MyTowers = _innerTowers.ToList();
                    break;
            }
        else
            switch(_fixedSpreadDirection)
            {
                case SpreadDirection.North:
                    if(_outerNorthTowers.Count > 1)
                        MyTowers = _outerNorthTowers
                            .Where(x => Vector2.Distance(x.Position.ToVector2(), OuterNorth) > 5f).ToList();
                    else
                        MyTowers = _innerTowers.ToList();
                    break;
                case SpreadDirection.East:
                    if(_outerEastTowers.Count > 1)
                        MyTowers = _outerEastTowers
                            .Where(x => Vector2.Distance(x.Position.ToVector2(), OuterEast) > 5f).ToList();
                    else
                        MyTowers = _innerTowers.ToList();
                    break;
                case SpreadDirection.South:
                    if(_outerSouthTowers.Count > 1)
                        MyTowers = _outerSouthTowers
                            .Where(x => Vector2.Distance(x.Position.ToVector2(), OuterSouth) > 5f).ToList();
                    else
                        MyTowers = _innerTowers.ToList();
                    break;
                case SpreadDirection.West:
                    if(_outerWestTowers.Count > 1)
                        MyTowers = _outerWestTowers
                            .Where(x => Vector2.Distance(x.Position.ToVector2(), OuterWest) > 5f).ToList();
                    else
                        MyTowers = _innerTowers.ToList();
                    break;
                default:
                    MyTowers = _innerTowers.ToList();
                    break;
            }
    }


    public override void OnUpdate()
    {
        ProcessSwap();
        if(!_isStart)
            return;
        if(!_isFirstTowerPhase && !_isSecondTowerPhase)
        {
            var playerPosition = Player.Position.ToVector2();
            if(playerPosition != _lastPlayerPosition)
            {
                SetTowers(playerPosition);
                Controller.GetRegisteredElements().Where(x => x.Key.StartsWith("bait")).Each(x => { x.Value.Enabled = false; });
                for(var i = 0; i < MyTowers.Count; i++)
                    if(Controller.TryGetElementByName($"bait{i + 1}", out var element))
                    {
                        element.Enabled = true;
                        element.color = C.PredictBaitColor.ToUint();
                        element.thicc = 4f; 
                        element.tether = true;
                        element.SetOffPosition(MyTowers[i].Position);
                    }
            }

            _lastPlayerPosition = playerPosition;
        }

        if(_isFirstTowerPhase || _isSecondTowerPhase)
            Controller.GetRegisteredElements().Where(x => x.Key.StartsWith("bait"))
                .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!_isStart)
            return;

        if(set.Action == null) return;

        if(set.Action.Value.RowId == 25575)
        {
            _isFirstTowerPhase = true;
            var position = Player.Position.ToVector2();
            SetTowers(position);

            Controller.GetRegisteredElements().Where(x => x.Key.StartsWith("bait")).Each(x => x.Value.Enabled = false);
            for(var i = 0; i < MyTowers.Count; i++) SetOffPosition($"bait{i + 1}", MyTowers[i].Position);
        }

        if(set.Action.Value.RowId == 29564)
        {
            _isFirstTowerPhase = false;
            _isSecondTowerPhase = true;
            Controller.GetRegisteredElements().Where(x => x.Key.StartsWith("bait")).Each(x => x.Value.Enabled = false);
            if(!_shouldPrioritizeOuterTower)
            {
                const float innerOffset = 3f;
                const float outerOffset = 14f;

                var innerOffsetPosition = _fixedSpreadDirection switch
                {
                    SpreadDirection.East => new Vector3(100 + innerOffset, 0f, 100f + innerOffset),
                    SpreadDirection.North => new Vector3(100f + innerOffset, 0f, 100f - innerOffset),
                    SpreadDirection.South => new Vector3(100f - innerOffset, 0f, 100f + innerOffset),
                    SpreadDirection.West => new Vector3(100 - innerOffset, 0f, 100f - innerOffset),
                    _ => Vector3.Zero
                };

                var outerOffsetPosition = _fixedSpreadDirection switch
                {
                    SpreadDirection.East => new Vector3(100 + outerOffset, 0f, 100f + outerOffset),
                    SpreadDirection.North => new Vector3(100f + outerOffset, 0f, 100f - outerOffset),
                    SpreadDirection.South => new Vector3(100f - outerOffset, 0f, 100f + outerOffset),
                    SpreadDirection.West => new Vector3(100 - outerOffset, 0f, 100f - outerOffset),
                    _ => Vector3.Zero
                };

                SetOffPosition("bait1", innerOffsetPosition);
                SetOffPosition("bait2", outerOffsetPosition);
            }
            else
            {
                var offsetPosition = _fixedSpreadDirection switch
                {
                    SpreadDirection.East => new Vector3(119f, 0f, 100f),
                    SpreadDirection.North => new Vector3(100f, 0f, 81f),
                    SpreadDirection.South => new Vector3(100f, 0f, 119f),
                    SpreadDirection.West => new Vector3(81f, 0f, 100f),
                    _ => Vector3.Zero
                };

                SetOffPosition("bait1", offsetPosition);
            }
        }
    }

    private Element? SetOffPosition(string name, Vector3 position)
    {
        if(Controller.TryGetElementByName(name, out var element))
        {
            element.Enabled = true;
            element.tether = true;
            element.thicc = 5f;
            element.SetOffPosition(position);
            return element;
        }

        return null;
    }

    unsafe void ProcessSwap()
    {
        var e = Controller.GetElementByName("AdjustCall")!;
        e.Enabled = false;
        if(!C.ResolveSwaps) return;
        var remTime = Controller.GetPartyMembers().Select(x => x.StatusList.FirstOrDefault(s => s.StatusId == 562)).Where(x => x != null).Select(x => x.RemainingTime).FirstOrDefault();
        
        if(Controller.Scene == 4 && Player.DistanceTo(new Vector3(100,0,100)) > 12f)
        {
            if(Svc.Objects.OfType<IBattleNpc>().Any(x => x.IsCasting(25308) && x.CurrentCastTime > 0.5f))
            {
                if(EzThrottler.Throttle(GenericHelpers.GetCallStackID(), 200))
                {
                    var action = Player.Job.IsDom() ? 7559u : 7548u;
                    if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
                    {
                        if(ActionManager.Instance()->GetActionStatus(ActionType.Action, action) == 0)
                        {
                            Chat.ExecuteAction(action);
                        }
                    }
                    else
                    {
                        DuoLog.Information($"Would use {ExcelActionHelper.GetActionName(action)}");
                    }
                }
            }
        }

        if(Controller.Scene == 4 && remTime.InRange(16f, 25f))
        {
            var north = new Vector3(100.000f, 0, 90.500f); //radius = 7
            var south = new Vector3(100.000f, 0, 109.500f);
            var east = new Vector3(109.500f, 0, 100f);
            var west = new Vector3(90.500f, 0, 100f);
            var isMeDps = Player.Job.IsDps();
            var isMeteorDps = Controller.GetPartyMembers().Any(x => x.GetJob().IsDps() == isMeDps && x.StatusList.Any(s => s.StatusId == 562));

            if(isMeDps == isMeteorDps && !Player.Status.Any(s => s.StatusId == 562))
            {
                int countPlayersWithMeteor(Vector3 where)
                {
                    return Controller.GetPartyMembers().Where(x => x.GetJob().IsDps() == isMeDps && Vector3.Distance(where, x.Position) < 7f && x.StatusList.Any(s => s.StatusId == 562)).Count();
                }
                int countPlayersWithoutMeteor(Vector3 where)
                {
                    return Controller.GetPartyMembers().Where(x => x.GetJob().IsDps() == isMeDps && Vector3.Distance(where, x.Position) < 7f && !x.StatusList.Any(s => s.StatusId == 562)).Count();
                }
                int countPlayers(Vector3 where)
                {
                    return Controller.GetPartyMembers().Where(x => x.GetJob().IsDps() == isMeDps && Vector3.Distance(where, x.Position) < 7f).Count();
                }

                if(Player.DistanceTo(north) < 7f)
                {
                    if(countPlayersWithoutMeteor(west) == 0)
                    {
                        e.Enabled = true;
                        e.SetRefPosition(west);
                    }
                    else if(countPlayersWithoutMeteor(east) == 0)
                    {
                        e.Enabled = true;
                        e.SetRefPosition(east);
                    }
                }
            }
        }
    }


    public unsafe override void OnSettingsDraw()
    {
        ImGui.Text("Bait Color:");
        ImGuiComponents.HelpMarker(
            "Change the color of the bait and the text that will be displayed on your bait.\nSetting different values makes it rainbow.");
        ImGui.Indent();
        ImGui.ColorEdit4("Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.SameLine();
        ImGui.ColorEdit4("Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
        ImGui.Unindent();
        ImGui.Text("Predict Bait Color:");
        ImGui.Indent();
        ImGui.ColorEdit4("Color", ref C.PredictBaitColor, ImGuiColorEditFlags.NoInputs);
        ImGui.Unindent();

        ImGui.Checkbox("Resolve swaps (only works for north position right now)", ref C.ResolveSwaps);

        if(ImGui.CollapsingHeader("Debug"))
        {
            var action = Player.Job.IsDom() ? 7559u : 7548u;
            ImGuiEx.Text($"{ActionManager.Instance()->GetActionStatus(ActionType.Action, action)}");
            ImGui.Text("Inner");
            foreach(var tower in _innerTowers)
            {
                ImGui.Text(tower.Name.ToString());
                ImGui.SameLine();
                ImGui.Text(tower.Position.ToString());
            }

            ImGui.Text("Outer North");
            foreach(var tower in _outerNorthTowers)
            {
                ImGui.Text(tower.Name.ToString());
                ImGui.SameLine();
                ImGui.Text(tower.Position.ToString());
            }

            ImGui.Text("Outer East");
            foreach(var tower in _outerEastTowers)
            {
                ImGui.Text(tower.Name.ToString());
                ImGui.SameLine();
                ImGui.Text(tower.Position.ToString());
            }

            ImGui.Text("Outer South");
            foreach(var tower in _outerSouthTowers)
            {
                ImGui.Text(tower.Name.ToString());
                ImGui.SameLine();
                ImGui.Text(tower.Position.ToString());
            }

            ImGui.Text("Outer West");
            foreach(var tower in _outerWestTowers)
            {
                ImGui.Text(tower.Name.ToString());
                ImGui.SameLine();
                ImGui.Text(tower.Position.ToString());
            }

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Text("My Towers");
            foreach(var tower in MyTowers)
            {
                ImGui.Text(tower.Name.ToString());
                ImGui.SameLine();
                ImGui.Text(tower.Position.ToString());
            }
        }
    }

    private class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public Vector4 PredictBaitColor = EColor.Red;
        public bool ResolveSwaps = false;
    }
}
