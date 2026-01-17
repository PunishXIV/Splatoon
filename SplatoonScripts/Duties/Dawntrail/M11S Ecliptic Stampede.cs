#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public unsafe class M11S_Ecliptic_Stampede : SplatoonScript
{
    /*****************************************************************************************************************/
    /* Constants and Types                                                                                           */
    /*****************************************************************************************************************/

    #region Constants and Types

    private enum State
    {
        Inactive,
        Casting,
        RoleGimmick,
        Wait,
        Tether,
        Stack,
    }

    private enum StateMajesticMeteorRole
    {
        Inactive,
        MajesticMeteor0,
        MajesticMeteor1,
        MajesticMeteor2,
        MajesticMeteor3,
        MajesticMeteor4,
        MajesticMeteor5,
    }

    private enum StateOtherRoles
    {
        Inactive,
        MajesticMeteor0,
        MajesticMeteor1,
        MajesticMeteor2,
        MajesticMeteor3,
        MajesticMeteor4,
        MajesticMeteor5,
        WaitTower,
        TowerIn,
        WaitTether,
        Tether,
        Stack,
    }

    #endregion

    /*****************************************************************************************************************/
    /* Public Fields                                                                                                 */
    /*****************************************************************************************************************/

    #region Public Fields

    public override HashSet<uint>? ValidTerritories => [1325,];
    public override Metadata? Metadata => new(3, "Redmoon");

    #endregion

    /*****************************************************************************************************************/
    /* Private Fields                                                                                                */
    /*****************************************************************************************************************/

    #region Private Fields

    private State _state = State.Inactive;
    private StateMajesticMeteorRole _majesticMeteorRoleState = StateMajesticMeteorRole.Inactive;
    private StateOtherRoles _otherRolesState = StateOtherRoles.Inactive;
    private int _damageCount = 0;
    private uint _stackCastId = 0;
    private Dictionary<string, Vector3> _majesticMeteorPositions;
    private List<IPlayerCharacter> _lockonPlayers = [];
    private List<(string, IPlayerCharacter)> _tetherInfo = [];
    private List<(string, IPlayerCharacter?)> _stackPositions = [];

    private List<(string, string, Vector3, IGameObject?, IGameObject?)>
        _towerPositions = []; // (Tower Type, N/E/S/W, Position, Assign Player, Assign Player)

    private Config C => Controller.GetConfig<Config>();

    private IPlayerCharacter BasePlayer
    {
        get
        {
            if (C.BasePlayerOverride == "" ||
                !Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback])
                return Player.Object;
            return Svc.Objects.OfType<IPlayerCharacter>()
                .FirstOrDefault(x => x.Name.ToString().EqualsIgnoreCase(C.BasePlayerOverride)) ?? Player.Object;
        }
    }

    #endregion

    /*****************************************************************************************************************/
    /* Public Methods                                                                                                */
    /*****************************************************************************************************************/

    #region Public Methods

    public override void OnSetup()
    {
        Controller.RegisterElement("First AOE", new Element(0)
        {
            refX = 100,
            refY = 100,
            radius = 5,
        });
        for (var i = 1; i <= 6; i++)
        {
            Controller.RegisterElement($"Majestic Meteor N{i}", new Element(0)
            {
                radius = 5f,
                Filled = true,
            });
            Controller.RegisterElement($"Majestic Meteor S{i}", new Element(0)
            {
                radius = 5f,
                Filled = true,
            });
        }

        Controller.RegisterElement($"tether", new Element(0)
        {
            radius = 0.3f,
            tether = true,
            thicc = 10f,
        });
    }

    public override void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        //Ecliptic Stampede
        if (packet->ActionID == 46162) _state = State.Casting;

        if (_state == State.Inactive) return;

        // Mammoth Meteor
        if (packet->ActionID == 46163)
        {
            if (sourceId.TryGetObject(out var o) && o.Position.Z < 100)
            {
                // 北西
                if (o.Position.X > 100) GenerateMajesticMeteorPositionsNE();
                // 北東
                else GenerateMajesticMeteorPositionsNW();
            }
        }

        // Majestic Meteor
        else if (packet->ActionID == 46165 && _otherRolesState != StateOtherRoles.Inactive)
        {
            if (_damageCount == 0)
            {
                HideAllElements();
                _otherRolesState++;
            }

            _damageCount++;
            if (_damageCount >= 6) _damageCount = 0;
        }
        // Cosmic Kiss (Tank Tower)
        else if (packet->ActionID == 46166)
        {
            if (sourceId.TryGetObject(out var o))
            {
                var ns = o.Position.Z < 100 ? "N" : "S";
                var ew = o.Position.X < 100 ? "W" : "E";
                _towerPositions.Add((
                        "T",
                        $"{ns}{ew}",
                        o.Position,
                        null,
                        null)
                );

                if (_towerPositions.Count == 4)
                {
                    ResolveTowerAssignments();
                    _otherRolesState = StateOtherRoles.TowerIn;
                }
            }
        }
        // Weighty Impact (DPS/Healer Tower)
        else if (packet->ActionID == 46167)
        {
            if (sourceId.TryGetObject(out var o))
            {
                var ns = o.Position.Z < 100 ? "N" : "S";
                var ew = o.Position.X < 100 ? "W" : "E";
                _towerPositions.Add((
                        "DH",
                        $"{ns}{ew}",
                        o.Position,
                        null,
                        null)
                );
            }

            if (_towerPositions.Count == 4)
            {
                // 北から時計回りに並べ直す
                var posStr = new[] { "NE", "SE", "SW", "NW", };
                _towerPositions = posStr.Select(x =>
                    _towerPositions.First(y => y.Item2 == x)).ToList();
                ResolveTowerAssignments();
                _otherRolesState = StateOtherRoles.TowerIn;
            }
        }
        else if (packet->ActionID is 46170 or 47037) _stackCastId = packet->ActionID;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_state == State.Inactive || set.Action == null) return;
        var castId = set.Action.Value.RowId;

        // Atomic Impact
        if (castId == 46164 && _majesticMeteorRoleState != StateMajesticMeteorRole.Inactive)
        {
            if (_damageCount == 0)
            {
                HideAllElements();
                if (_majesticMeteorRoleState != StateMajesticMeteorRole.MajesticMeteor5)
                    _majesticMeteorRoleState++;
                else
                {
                    _majesticMeteorRoleState = StateMajesticMeteorRole.Inactive;
                    _state = State.Wait;
                }
            }

            _damageCount++;
            if (_damageCount >= 2) _damageCount = 0;
        }

        if (castId == 46169 && _otherRolesState == StateOtherRoles.Tether) _otherRolesState = StateOtherRoles.Stack;
        if (castId == 46169 && _state is State.Wait or State.Tether) _state = State.Stack;
        else if (castId is 46170 or 47037) OnReset();
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_state == State.Inactive) return;
        if (!(data2 == 0 && data3 == 57 && data5 == 15)) return;
        if (!source.TryGetBattleNpc(out var npc) || !target.TryGetPlayer(out var player)) return;
        // 重複している場合は無視
        if (_tetherInfo.Any(x => x.Item2.EntityId == player.EntityId)) return;
        // x, zがどちらか99 ~ 101以内のはずなのでその座標とは別の座標がNSEWかを確認する
        if (npc.Position.X is >= 99 and <= 101)
        {
            if (npc.Position.Z < 100)
                _tetherInfo.Add(("N", player));
            else
                _tetherInfo.Add(("S", player));
        }
        else if (npc.Position.Z is >= 99 and <= 101)
        {
            if (npc.Position.X < 100)
                _tetherInfo.Add(("W", player));
            else
                _tetherInfo.Add(("E", player));
        }

        if (_tetherInfo.Count != 4) return;

        // Tether情報が揃ったらスタック位置を決定する
        _stackPositions.Add(("NE", null));
        _stackPositions.Add(("NW", null));
        _stackPositions.Add(("SE", null));
        _stackPositions.Add(("SW", null));
        var priority = C.DiversionTowerNotUseStackPriority ? C.TowerAssignmentsPriority : C.StackPriority;
        var nonTetherPlayers = priority.GetPlayers(x =>
            _tetherInfo.All(y => y.Item2.EntityId != x.IGameObject.EntityId));

        // nonTetherPlayersでtowerPositionsにも存在するプレイヤーを抽出してstackPositionsに追加
        foreach (var tower in _towerPositions)
        {
            var player1 = nonTetherPlayers.FirstOrDefault(x => x.IGameObject.EntityId == tower.Item4?.EntityId);
            var player2 = nonTetherPlayers.FirstOrDefault(x => x.IGameObject.EntityId == tower.Item5?.EntityId);
            if (player1 == null && player2 == null) continue;
            if (player1 is { IGameObject: IPlayerCharacter pc1, })
            {
                var idx = _stackPositions.FindIndex(x => x.Item1 == tower.Item2);
                _stackPositions[idx] = (_stackPositions[idx].Item1, pc1);
            }
            else if (player2 is { IGameObject: IPlayerCharacter pc2, })
            {
                var idx = _stackPositions.FindIndex(x => x.Item1 == tower.Item2);
                _stackPositions[idx] = (_stackPositions[idx].Item1, pc2);
            }
        }

        // nonTetherPlayersでstackPositionsにも存在しないプレイヤーを抽出してstackPositionsに追加
        var meteorPlayers = priority.GetPlayers(x =>
            _lockonPlayers.Any(y => y.EntityId == x.IGameObject.EntityId));
        var meteorIndex = 0;
        for (var i = 0; i < _stackPositions.Count; i++)
            if (_stackPositions[i].Item2 == null)
            {
                if (meteorIndex >= meteorPlayers.Count)
                {
                    PluginLog.Error("Could not find enough players for stack positions.");
                    //OnReset();
                    return;
                }

                if (meteorPlayers[meteorIndex].IGameObject is IPlayerCharacter pc)
                    _stackPositions[i] = (_stackPositions[i].Item1, pc);
                meteorIndex++;
            }

        if (_stackPositions.Any(x => x.Item2 == null))
        {
            PluginLog.Error("Could not find enough players for stack positions.");
            //OnReset();
            return;
        }

        // ステート遷移
        if (_tetherInfo.Any(x => x.Item2.EntityId == BasePlayer.EntityId))
        {
            if (_otherRolesState == StateOtherRoles.Inactive) _state = State.Tether;
            else _otherRolesState = StateOtherRoles.Tether;
        }
        else
        {
            _otherRolesState = StateOtherRoles.Inactive;
            _state = State.Wait;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (vfxPath.Contains("vfx/lockon/eff/m0017trg_a0c.avfx"))
        {
            var pc = FakeParty.Get().FirstOrDefault(x => x.EntityId == target);
            if (pc != null) _lockonPlayers.Add(pc);

            if (_lockonPlayers.Count != 2) return;

            if (_lockonPlayers.Any(x => x.EntityId == BasePlayer.EntityId))
                _majesticMeteorRoleState = StateMajesticMeteorRole.MajesticMeteor0;
            else
                _otherRolesState = StateOtherRoles.MajesticMeteor0;
        }
    }

    public override void OnUpdate()
    {
        if (_state == State.Inactive) return;
        if (Controller.TryGetElementByName("tether", out var el))
        {
            if (el.Enabled)
                el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }

        switch (_state)
        {
            default:
            case State.Inactive:
                break;
            case State.Casting:
            {
                HandleCasting();
                if ((_majesticMeteorRoleState != StateMajesticMeteorRole.Inactive ||
                     _otherRolesState != StateOtherRoles.Inactive) &&
                    _majesticMeteorPositions != null)
                {
                    HideAllElements();
                    _state = State.RoleGimmick;
                }

                break;
            }
            case State.RoleGimmick:
                HandleRoleGimmick();
                break;

            case State.Wait:
                if (Controller.TryGetElementByName("tether", out var tetherElement))
                {
                    tetherElement.SetRefPosition(new Vector3(100f, 0f, 100f));
                    tetherElement.Enabled = true;
                }

                break;
            case State.Tether:
                HandleTether();
                break;
            case State.Stack:
                HandleStack();
                break;
        }
    }

    public override void OnReset()
    {
        _state = State.Inactive;
        _majesticMeteorRoleState = StateMajesticMeteorRole.Inactive;
        _otherRolesState = StateOtherRoles.Inactive;
        _stackCastId = 0;
        _majesticMeteorPositions = null;
        _lockonPlayers.Clear();
        _towerPositions.Clear();
        _tetherInfo.Clear();
        _stackPositions.Clear();
        _damageCount = 0;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public class PriorityData4 : PriorityData
    {
        public override int GetNumPlayers()
        {
            return 4;
        }
    }

    public class Config : IEzConfig
    {
        public string BasePlayerOverride = "";
        public bool IsGuidingMajesticMeteor = false;

        public PriorityData TowerAssignmentsPriority = new();
        public bool DiversionTowerNotUseMeteors = false;
        public PriorityData4 MeteorsPriority = new();
        public bool DiversionTowerNotUseStackPriority = false;
        public PriorityData StackPriority = new();
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("# Tower Assignments Priority");
        C.TowerAssignmentsPriority.Draw();

        ImGui.NewLine();
        ImGui.Separator();
        ImGui.NewLine();
        ImGui.Text("# Meteors Priority");
        ImGui.Checkbox(
            "Diversing Tether Guides: Do not use Meteors Priority##diversionTetherGuidesNotUseMeteors",
            ref C.DiversionTowerNotUseMeteors);
        if (!C.DiversionTowerNotUseMeteors) C.MeteorsPriority.Draw();

        ImGui.Separator();
        ImGui.NewLine();
        ImGui.Text("# Stack Priority");
        ImGui.Checkbox(
            "Diversing Tether Guides: Do not use Stack Priority##diversionTetherGuidesNotUseStackPriority",
            ref C.DiversionTowerNotUseStackPriority);
        if (!C.DiversionTowerNotUseStackPriority) C.StackPriority.Draw();

        ImGui.NewLine();
        ImGui.Checkbox("Ill Guiding Meteor##isGuidingMajesticMeteor", ref C.IsGuidingMajesticMeteor);

        ImGui.NewLine();
        // Debug
        if (!ImGuiEx.CollapsingHeader("Debug")) return;
        ImGui.SetNextItemWidth(200);
        ImGui.InputText("Player override", ref C.BasePlayerOverride, 50);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        if (ImGui.BeginCombo("Select..", "Select..."))
        {
            foreach (var x in Svc.Objects.OfType<IPlayerCharacter>())
                if (ImGui.Selectable(x.GetNameWithWorld()))
                    C.BasePlayerOverride = x.Name.ToString();
            ImGui.EndCombo();
        }

        ImGui.Text($"Current State: {_state}");
        ImGui.Text($"Majestic Meteor Role State: {_majesticMeteorRoleState}");
        ImGui.Text($"Other Roles State: {_otherRolesState}");
        ImGui.Text($"Damage Count: {_damageCount}");
        ImGui.Text($"Stack Cast ID: {_stackCastId}");
        ImGui.Text("Lockon Players:");
        foreach (var player in _lockonPlayers) ImGui.Text($"- {player.Name}");
        ImGui.Text($"Tether Info:");
        foreach (var info in _tetherInfo) ImGui.Text($"- {info.Item1} tethered to {info.Item2.Name}");
        ImGui.Text($"Tower Positions:");
        foreach (var pos in _towerPositions)
            ImGui.Text(
                $"- {pos.Item1} {pos.Item2}: ({pos.Item3.X}, {pos.Item3.Y}, {pos.Item3.Z}) assigned to {pos.Item4?.Name ?? "-"}, {pos.Item5?.Name ?? "-"}");
        ImGui.Text("Stack Positions:");
        foreach (var pos in _stackPositions)
            ImGui.Text($"- {pos.Item1} stack position assigned to {pos.Item2?.Name ?? "-"}");
        ImGui.NewLine();
        ImGui.Separator();

        ImGui.Text("Majestic Meteor Positions:");
        if (_majesticMeteorPositions != null)
        {
            foreach (var kvp in _majesticMeteorPositions)
                ImGui.Text($"- {kvp.Key}: ({kvp.Value.X}, {kvp.Value.Y}, {kvp.Value.Z})");
        }
        else
            ImGui.Text("N/A");

        ImGui.NewLine();
        ImGui.Separator();

        ImGui.Text("Element Monitoring:");
        foreach (var el in Controller.GetRegisteredElements().ToList())
            ImGui.Text(
                $"- {el.Value.Name}: Enabled={el.Value.Enabled}, RefPos=({el.Value.refX}, {el.Value.refY}), Radius={el.Value.radius}");
    }

    #endregion

    /*****************************************************************************************************************/
    /* Private Methods                                                                                               */
    /*****************************************************************************************************************/

    #region Private Methods

    private void HandleCasting()
    {
        if (Controller.TryGetElementByName("First AOE", out var firstAoe))
        {
            firstAoe.radius = C.IsGuidingMajesticMeteor ? 8f : 5f;
            firstAoe.Donut = C.IsGuidingMajesticMeteor ? 0f : 20f;
            firstAoe.Enabled = true;
        }
    }

    private void HandleRoleGimmick()
    {
        if (_majesticMeteorRoleState != StateMajesticMeteorRole.Inactive) HandleMajesticMeteorRole();
        else HandleOtherRoles();
    }

    private void HandleTether()
    {
        var posStr = _tetherInfo.FirstOrDefault(x => x.Item2.EntityId == BasePlayer.EntityId).Item1;
        var pos = posStr switch
        {
            "N" => new Vector3(81f, 0f, 119f),
            "E" => new Vector3(81f, 0f, 81f),
            "S" => new Vector3(119f, 0f, 81f),
            "W" => new Vector3(119f, 0f, 119f),
            _ => Vector3.Zero,
        };

        if (pos == Vector3.Zero)
        {
            PluginLog.Error("Invalid tether position.");
            OnReset();
            return;
        }

        if (Controller.TryGetElementByName("tether", out var tetherElement))
        {
            tetherElement.SetRefPosition(pos);
            tetherElement.Enabled = true;
        }
    }

    private void HandleStack()
    {
        var posStr = _stackPositions.FirstOrDefault(x => x.Item2.EntityId == BasePlayer.EntityId).Item1;
        var pos = Vector3.Zero;
        if (_stackCastId == 46170) // 2 pairs 4 stacks
        {
            pos = posStr switch
            {
                "NE" => new Vector3(103f, 0f, 97f),
                "NW" => new Vector3(97f, 0f, 97f),
                "SE" => new Vector3(103f, 0f, 103f),
                "SW" => new Vector3(97f, 0f, 103f),
                _ => Vector3.Zero,
            };
        }
        else if (_stackCastId == 47037) // 4 Pairs 2stacks
        {
            pos = posStr switch
            {
                "NE" => new Vector3(100f, 0f, 97f),
                "NW" => new Vector3(100f, 0f, 94f),
                "SE" => new Vector3(100f, 0f, 103f),
                "SW" => new Vector3(100f, 0f, 106f),
                _ => Vector3.Zero,
            };
        }
        else return;

        if (pos == Vector3.Zero)
        {
            PluginLog.Error("Invalid stack position.");
            OnReset();
            return;
        }

        if (Controller.TryGetElementByName("tether", out var tetherElement))
        {
            tetherElement.SetRefPosition(pos);
            tetherElement.Enabled = true;
        }
    }

    // Sub States Handlers
    private void HandleMajesticMeteorRole()
    {
        var priority = C.DiversionTowerNotUseMeteors ? C.TowerAssignmentsPriority : C.MeteorsPriority;
        if (priority.GetFirstValidList() == null)
        {
            PluginLog.Error("Meteors priority list is empty.");
            OnReset();
            return;
        }

        var lockonPlayers = priority.GetPlayers(x =>
            _lockonPlayers.Any(y => y.EntityId == x.IGameObject.EntityId));
        var otherPlayer = lockonPlayers.FirstOrDefault(x => x.IGameObject.EntityId != BasePlayer.EntityId);
        var baseIndex = priority.GetPlayers(_ => true).IndexOf(x => x.IGameObject.EntityId == BasePlayer.EntityId);
        if (baseIndex == -1)
        {
            PluginLog.Error("Could not find base player in meteors priority list.");
            OnReset();
            return;
        }

        var otherIndex = priority.GetPlayers(_ => true)
            .IndexOf(x => x.IGameObject.EntityId == otherPlayer.IGameObject.EntityId);
        if (otherIndex == -1)
        {
            PluginLog.Error("Could not find other player in meteors priority list.");
            OnReset();
            return;
        }

        var i = _majesticMeteorRoleState switch
        {
            StateMajesticMeteorRole.MajesticMeteor0 => 0,
            StateMajesticMeteorRole.MajesticMeteor1 => 1,
            StateMajesticMeteorRole.MajesticMeteor2 => 2,
            StateMajesticMeteorRole.MajesticMeteor3 => 3,
            StateMajesticMeteorRole.MajesticMeteor4 => 4,
            StateMajesticMeteorRole.MajesticMeteor5 => 5,
            _ => -1,
        };
        if (i == -1)
        {
            PluginLog.Error("Invalid majestic meteor role state.");
            OnReset();
            return;
        }

        // 北
        if (baseIndex < otherIndex)
        {
            var positionKey = $"N{i + 1}";
            if (_majesticMeteorPositions.TryGetValue(positionKey, out var position) &&
                Controller.TryGetElementByName("tether", out var tetherElement))
            {
                tetherElement.SetRefPosition(position);
                tetherElement.Enabled = true;
            }
            else
                PluginLog.Error($"Could not find tether meteor position {positionKey}");
        }
        // 南
        else
        {
            var positionKey = $"S{i + 1}";
            if (_majesticMeteorPositions.TryGetValue(positionKey, out var position) &&
                Controller.TryGetElementByName("tether", out var tetherElement))
            {
                tetherElement.SetRefPosition(position);
                tetherElement.Enabled = true;
            }
            else
                PluginLog.Error($"Could not find tether meteor position {positionKey}");
        }
    }

    private void HandleOtherRoles()
    {
        var pos = _otherRolesState switch
        {
            StateOtherRoles.MajesticMeteor0 => new Vector3(100f, 0f, 100f),
            StateOtherRoles.MajesticMeteor1 => new Vector3(100f, 0f, 93f),
            StateOtherRoles.MajesticMeteor2 => new Vector3(106f, 0f, 96f),
            StateOtherRoles.MajesticMeteor3 => new Vector3(106f, 0f, 103f),
            StateOtherRoles.MajesticMeteor4 => new Vector3(100f, 0f, 107f),
            StateOtherRoles.MajesticMeteor5 => new Vector3(94f, 0f, 103f),
            StateOtherRoles.WaitTower => new Vector3(100f, 0f, 100f),
            StateOtherRoles.TowerIn => GetTowerInPosition(),
            StateOtherRoles.Tether => GetTetherPosition(),
            StateOtherRoles.Stack => GetStackPosition(),
            _ => Vector3.Zero,
        };

        if (_otherRolesState == StateOtherRoles.Stack && _stackCastId == 0) return;

        if (pos == Vector3.Zero)
        {
            //PluginLog.Error("Invalid other roles state.");
            //OnReset();
            return;
        }

        if (Controller.TryGetElementByName("tether", out var tetherElement))
        {
            tetherElement.SetRefPosition(pos);
            tetherElement.Enabled = true;
        }
    }

    private Vector3 GetTowerInPosition()
    {
        var myTower = _towerPositions.FirstOrDefault(x =>
            (x.Item4 != null && x.Item4.EntityId == BasePlayer.EntityId) ||
            (x.Item5 != null && x.Item5.EntityId == BasePlayer.EntityId));
        if (myTower == default)
        {
            PluginLog.Error("Could not find tether meteor position.");
            OnReset();
            return Vector3.Zero;
        }

        // C.MeterGuidingMajesticMeteorがfalseの場合は100, 0, 100寄りにする 約 + 3m
        // そうでない場合は -3m
        if (!C.IsGuidingMajesticMeteor)
        {
            var direction = Vector3.Normalize(new Vector3(100f, 0f,
                100f) - myTower.Item3);
            return myTower.Item3 + direction * 3f;
        }

        var directionAway = Vector3.Normalize(myTower.Item3 - new Vector3(100f, 0f,
            100f));
        return myTower.Item3 + directionAway * 3f;
    }

    private Vector3 GetTetherPosition()
    {
        var myTether = _tetherInfo.FirstOrDefault(x => x.Item2.EntityId == BasePlayer.EntityId);
        if (myTether == default)
        {
            PluginLog.Error("Could not find tether meteor position.");
            OnReset();
            return Vector3.Zero;
        }

        return myTether.Item1 switch
        {
            "N" => new Vector3(81f, 0f, 119f),
            "E" => new Vector3(81f, 0f, 81f),
            "S" => new Vector3(119f, 0f, 81f),
            "W" => new Vector3(119f, 0f, 119f),
            _ => Vector3.Zero,
        };
    }

    private Vector3 GetStackPosition()
    {
        var myTether = _tetherInfo.FirstOrDefault(x => x.Item2.EntityId == BasePlayer.EntityId);
        if (myTether == default)
        {
            PluginLog.Error("Could not find tether meteor position.");
            OnReset();
            return Vector3.Zero;
        }

        if (_stackCastId == 46170) // 2 pairs 4 stacks
        {
            return myTether.Item1 switch
            {
                "N" => new Vector3(94f, 0f, 106f),
                "E" => new Vector3(94f, 0f, 94f),
                "S" => new Vector3(106f, 0f, 94f),
                "W" => new Vector3(106f, 0f, 106f),
                _ => Vector3.Zero,
            };
        }
        else if (_stackCastId == 47037) // 4 Pairs 2stacks
        {
            return myTether.Item1 switch
            {
                "N" or "E" => new Vector3(100f, 0f, 106f),
                "S" or "W" => new Vector3(100f, 0f, 94f),
                _ => Vector3.Zero,
            };
        }

        return Vector3.Zero;
    }

    private void GenerateMajesticMeteorPositionsNE()
    {
        var centor = new Vector3(100, 0, 100);

        _majesticMeteorPositions = new Dictionary<string, Vector3>
        {
            { "S1", new Vector3(centor.X + 9, 0, centor.Z + 19) },
            { "S2", new Vector3(centor.X + 3, 0, centor.Z + 19) },
            { "S3", new Vector3(centor.X - 3, 0, centor.Z + 19) },
            { "S4", new Vector3(centor.X - 9, 0, centor.Z + 19) },
            { "S5", new Vector3(centor.X - 6, 0, centor.Z + 14) },
            { "S6", new Vector3(centor.X + 6, 0, centor.Z + 14) },
            { "N1", new Vector3(centor.X - 9, 0, centor.Z - 19) },
            { "N2", new Vector3(centor.X - 3, 0, centor.Z - 19) },
            { "N3", new Vector3(centor.X + 3, 0, centor.Z - 19) },
            { "N4", new Vector3(centor.X + 9, 0, centor.Z - 19) },
            { "N5", new Vector3(centor.X + 6, 0, centor.Z - 14) },
            { "N6", new Vector3(centor.X - 6, 0, centor.Z - 14) },
        };
    }

    private void GenerateMajesticMeteorPositionsNW()
    {
        // 北西、南東にAOEがいるので、北東、南西開始
        var centor = new Vector3(100, 0, 100);

        _majesticMeteorPositions = new Dictionary<string, Vector3>
        {
            { "S1", new Vector3(centor.X - 9, 0, centor.Z + 19) },
            { "S2", new Vector3(centor.X - 3, 0, centor.Z + 19) },
            { "S3", new Vector3(centor.X + 3, 0, centor.Z + 19) },
            { "S4", new Vector3(centor.X + 9, 0, centor.Z + 19) },
            { "S5", new Vector3(centor.X + 6, 0, centor.Z + 14) },
            { "S6", new Vector3(centor.X - 6, 0, centor.Z + 14) },
            { "N1", new Vector3(centor.X + 9, 0, centor.Z - 19) },
            { "N2", new Vector3(centor.X + 3, 0, centor.Z - 19) },
            { "N3", new Vector3(centor.X - 3, 0, centor.Z - 19) },
            { "N4", new Vector3(centor.X - 9, 0, centor.Z - 19) },
            { "N5", new Vector3(centor.X - 6, 0, centor.Z - 14) },
            { "N6", new Vector3(centor.X + 6, 0, centor.Z - 14) },
        };
    }

    private void ResolveTowerAssignments()
    {
        // メテオではない6人を取得
        var nonMeteorPlayers =
            C.TowerAssignmentsPriority.GetPlayers(x => _lockonPlayers.All(y => y.EntityId != x.IGameObject.EntityId));

        var rhJobs = new[]
        {
            Job.BRD,
            Job.MCH,
            Job.DNC,
            Job.WHM,
            Job.SCH,
            Job.AST,
            Job.SGE,
        };

        if (nonMeteorPlayers.Count != 6)
        {
            PluginLog.Error("Could not find 6 non-meteor players in tower assignments.");
            OnReset();
            return;
        }

        var rhNonMeteorPlayers = nonMeteorPlayers
            .Where(X => X.IGameObject is IPlayerCharacter pc && rhJobs.Contains((Job)pc.Struct()->ClassJob)).ToList();
        var sortedRhNonMeteorPlayers = rhNonMeteorPlayers
            .OrderBy(x => C.TowerAssignmentsPriority.GetPlayers(_ => true)!
                .IndexOf(y => y.IGameObject.EntityId == x.IGameObject.EntityId)).ToList();

        var meleeNonMeteorPlayers = nonMeteorPlayers
            .Where(X => X.IGameObject is IPlayerCharacter pc && !rhJobs.Contains((Job)pc.Struct()->ClassJob) &&
                        pc.GetRole() != CombatRole.Tank).ToList();
        var sortedMeleeNonMeteorPlayers = meleeNonMeteorPlayers
            .OrderBy(x => C.TowerAssignmentsPriority.GetPlayers(_ => true)!
                .IndexOf(y => y.IGameObject.EntityId == x.IGameObject.EntityId)).ToList();

        if (sortedRhNonMeteorPlayers.Count != 2 || sortedMeleeNonMeteorPlayers.Count != 2)
        {
            PluginLog.Error("Could not find 2 ranged/healer players for dps/healer towers.");
            OnReset();
            return;
        }


        // タンク塔抽出
        var tankTowers = _towerPositions.Where(x => x.Item1 == "T").ToList();
        // DPS/ヒーラー塔抽出
        var dhTowers = _towerPositions.Where(x => x.Item1 == "DH").ToList();

        var tankNonMeteorPlayers = nonMeteorPlayers
            .Where(X => X.IGameObject is IPlayerCharacter pc && pc.GetRole() == CombatRole.Tank).ToList();

        if (tankNonMeteorPlayers.Count < tankTowers.Count)
        {
            PluginLog.Error("Could not find enough tank players for tank towers.");
            OnReset();
            return;
        }

        var dhNonMeteorPlayers = nonMeteorPlayers
            .Where(X => X.IGameObject is IPlayerCharacter pc && pc.GetRole() != CombatRole.Tank).ToList();
        if (dhNonMeteorPlayers.Count < dhTowers.Count)
        {
            PluginLog.Error("Could not find enough dps/healer players for dps/healer towers.");
            OnReset();
            return;
        }

        // タンク塔割り当て
        for (var i = 0; i < tankTowers.Count; i++)
        {
            var player = tankNonMeteorPlayers[i];
            var idx = _towerPositions.FindIndex(x => x.Item1 == tankTowers[i].Item1 &&
                                                     x.Item2 == tankTowers[i].Item2);
            _towerPositions[idx] = (tankTowers[i].Item1, tankTowers[i].Item2, tankTowers[i].Item3, player.IGameObject,
                null);
        }

        var assignedCount = 0;

        // DPS/ヒーラー塔割り当て
        for (var i = 0; i < dhTowers.Count; i++)
        {
            var player = sortedRhNonMeteorPlayers[assignedCount];
            var player2 = sortedMeleeNonMeteorPlayers[assignedCount];
            var idx = _towerPositions.FindIndex(x => x.Item1 == dhTowers[i].Item1 &&
                                                     x.Item2 == dhTowers[i].Item2);
            _towerPositions[idx] = (dhTowers[i].Item1, dhTowers[i].Item2, dhTowers[i].Item3,
                player.IGameObject, player2.IGameObject);
            assignedCount++;
        }
    }

    private void HideAllElements()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    #endregion
}