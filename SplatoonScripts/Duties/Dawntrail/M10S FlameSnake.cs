using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace SplatoonScriptsOfficial.Duties.Dawntrail;

internal class M10S_FlameSnake :SplatoonScript
{
    /*
    * Constants and Types
    */
    #region Constants and Types
    private enum State
    {
        None,
        WaitData,
        WaitAoeCast1,
        SetAoe1,
        WaitAoeCast2,
        SetAoe2,
        SetForAoe,
        SetStack,
    }

    List<Vector3> SetAoe1Dpos = new List<Vector3>()
    {
        new Vector3(81f,0f,97f),
        new Vector3(81f,0f,103f),
        new Vector3(87f,0f,97f),
        new Vector3(87f,0f,103f),
    };

    List<Vector3> SetAoe1BPos = new List<Vector3>()
    {
        new Vector3(119f,0f,97f),
        new Vector3(119f,0f,103f),
        new Vector3(113f,0f,97f),
        new Vector3(113f,0f,103f),
    };

    List<Vector3> SetAoe2ABPos = new List<Vector3>()
    {
        new Vector3(119f,0f,81f),
        new Vector3(119f,0f,87f),
        new Vector3(113f,0f,81f),
        new Vector3(106f,0f,81f),
    };

    List<Vector3> SetAoe2ADPos = new List<Vector3>()
    {
        new Vector3(81f,0f,81f),
        new Vector3(81f,0f,87f),
        new Vector3(87f,0f,81f),
        new Vector3(94f,0f,81f),
    };

    List<Vector3> SetAoe2BCPos = new List<Vector3>()
    {
        new Vector3(119f,0f,119f),
        new Vector3(119f,0f,113f),
        new Vector3(113f,0f,119f),
        new Vector3(106f,0f,119f),
    };

    List<Vector3> SetAoe2CDPos = new List<Vector3>()
    {
        new Vector3(81f,0f,119f),
        new Vector3(81f,0f,113f),
        new Vector3(87f,0f,119f),
        new Vector3(94f,0f,119f),
    };

    List<Vector3> SetForAoe3Pos = new List<Vector3>()
    {
        new Vector3(87f,0f,87f),
        new Vector3(87f,0f,113f),
        new Vector3(113f,0f,87f),
        new Vector3(113f,0f,113f),
    };
    #endregion

    /*
     * Public Fields
     */
    #region Public Fields
    public override HashSet<uint>? ValidTerritories => [1323];
    public override Metadata? Metadata => new(6, "Redmoon");
    #endregion

    /*
     * Private Fields
     */
    #region Private Fields
    private State _state = State.None;
    private List<IPlayerCharacter> _flamers = new List<IPlayerCharacter>();
    private bool _attackedForAoe = false;
    private int _attackedCount = 0;
    private string _redHotPos = "";
    private string _firstWavePos = "";

    private Config C => Controller.GetConfig<Config>();

    private IPlayerCharacter BasePlayer
    {
        get
        {
            if (C.basePlayerOverride == "") return Player.Object;
            if (!Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback]) return Player.Object;
            return Svc.Objects.OfType<IPlayerCharacter>()
                .FirstOrDefault(x => x.Name.ToString().EqualsIgnoreCase(C.basePlayerOverride)) ?? Player.Object;
        }
    }
    #endregion

    /*
     * Public Methods
     */
    #region Public Methods
    public override void OnSetup()
    {
        Controller.RegisterElement($"tether", new Element(0)
        {
            radius = 0.3f,
            tether = true,
            thicc = 10f,
        });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 45953)
        {
            _state = State.WaitData;
            DuoLog.Information("FlameSnake: OnStartingCast: Waiting for data...");
        }
        if (castId == 46529 && _state == State.WaitAoeCast1)
        {
            if (source.TryGetObject(out IGameObject obj))
            {
                if (obj == null)
                {
                    DuoLog.Error($"FlameSnake: OnStartingCast: Object not found for sourceId {source}");
                    return;
                }
                if (obj.Position.X > 100f) _redHotPos = "B";
                else _redHotPos = "D";

                _state = State.SetAoe1;
                var index = _flamers.IndexOf(BasePlayer);
                var tetherPos = _redHotPos == "B" ? SetAoe1BPos[index] : SetAoe1Dpos[index];
                if (Controller.TryGetElementByName("tether", out var element))
                {
                    element.SetRefPosition(tetherPos);
                    element.Enabled = true;
                }
            }
            else
            {
                DuoLog.Error("E2");
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_state == State.None) return;
        if (set.Action.Value.RowId == 46529 && _state == State.SetAoe1)
        {
            _state = State.WaitAoeCast2;
            if (Controller.TryGetElementByName("tether", out var element))
            {
                element.Enabled = false;
            }
        }
        else if (set.Action.Value.RowId == 46529 && _state == State.SetAoe2)
        {
            _state = State.SetForAoe;
            var index = _flamers.IndexOf(BasePlayer);
            var tetherPos = SetForAoe3Pos[index];
            if (Controller.TryGetElementByName("tether", out var element))
            {
                element.SetRefPosition(tetherPos);
                element.Enabled = true;
            }
        }
        else if (set.Action.Value.RowId == 47389)
        {
            _attackedCount++;
            if (_attackedCount == 4)
            {
                _state = State.SetStack;
                if (Controller.TryGetElementByName("tether", out var element))
                {
                    if (_firstWavePos == "A") element.SetRefPosition(new Vector3(100, 0, 95));
                    else element.SetRefPosition(new Vector3(100, 0, 105));
                    element.Enabled = true;
                }
            }
            else
            {
                if (BasePlayer.EntityId == set.Target.EntityId || _attackedForAoe)
                {
                    _attackedForAoe = true;
                    if (Controller.TryGetElementByName("tether", out var element2))
                    {
                        element2.SetRefPosition(new Vector3(100, 0, 100));
                        element2.Enabled = true;
                    }
                }
                else
                {
                    var index = _flamers.IndexOf(BasePlayer);
                    var tetherPos = SetForAoe3Pos[index];
                    if (Controller.TryGetElementByName("tether", out var element))
                    {
                        element.SetRefPosition(tetherPos);
                        element.Enabled = true;
                    }
                }
            }
        }
        else if (set.Action.Value.RowId == 46519)
        {
            this.OnReset(); 
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if (_state != State.WaitAoeCast2) return;
        if (data1 is 64 or 1024 && data2 is 128 or 2048)
        {
            // A
            if (position == 2)
            {
                _firstWavePos = "A";
                var index = _flamers.IndexOf(BasePlayer);
                var tetherPos = _redHotPos == "B" ? SetAoe2ABPos[index] : SetAoe2ADPos[index];
                if (Controller.TryGetElementByName("tether", out var element))
                {
                    element.SetRefPosition(tetherPos);
                    element.Enabled = true;
                }
            }
            else
            {
                _firstWavePos = "C";
                var index = _flamers.IndexOf(BasePlayer);
                var tetherPos = _redHotPos == "B" ? SetAoe2BCPos[index] : SetAoe2CDPos[index];
                if (Controller.TryGetElementByName("tether", out var element))
                {
                    element.SetRefPosition(tetherPos);
                    element.Enabled = true;
                }
            }
            _state = State.SetAoe2;
        }
    }

    public override void OnGainBuffEffect(uint sourceId, Status Status)
    {
        if (_state == State.None) return;
        if (Status.StatusId == 4975 && sourceId == BasePlayer.EntityId) _state = State.None;
        if (Status.StatusId == 4974)
        {
            if (sourceId.TryGetObject(out IGameObject obj) && obj is IPlayerCharacter flamer)
            {
                _flamers.Add(flamer);
                // 4人揃ったら遷移
                if (_flamers.Count == 4)
                {
                    // joborder順に並び替え
                    var list = _flamers.OrderBy(x => jobOrder.IndexOf((Job)x.ClassJob.RowId)).ToList();
                    _flamers = list;
                    _state = State.WaitAoeCast1;
                }
            }
        }
    }

    public override void OnUpdate()
    {
        if (Controller.TryGetElementByName("tether", out var el))
        {
            if (el.Enabled) el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }
    }

    public class Config :IEzConfig
    {
        public string basePlayerOverride;
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("Player override", ref C.basePlayerOverride, 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.BeginCombo("Select..", "Select..."))
            {
                foreach (var x in Svc.Objects.OfType<IPlayerCharacter>())
                    if (ImGui.Selectable(x.GetNameWithWorld()))
                        C.basePlayerOverride = x.Name.ToString();
                ImGui.EndCombo();
            }

            ImGui.Text($"_state = {_state}");
            ImGui.Text($"_redHotPos = {_redHotPos}");
            ImGui.Text($"_attckedCount = {_attackedCount}");
            ImGui.Text($"_flamers = {string.Join(", ", _flamers.Select(x => x.GetNameWithWorld()))}");
            ImGui.Text($"BasePlayer = {BasePlayer.GetNameWithWorld()}");
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _flamers.Clear();
        _attackedForAoe = false;
        _attackedCount = 0;
        _redHotPos = "";
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }
    #endregion

    /*
     * Private Methods
     */
    #region Private Methods
    private static readonly List<Job> jobOrder = new List<Job>()
    {
        Job.DRK,
        Job.WAR,
        Job.GNB,
        Job.PLD,
        Job.WHM,
        Job.AST,
        Job.SCH,
        Job.SGE,
        Job.DRG,
        Job.VPR,
        Job.SAM,
        Job.MNK,
        Job.RPR,
        Job.RDM,
        Job.SMN,
        Job.PCT,
        Job.NIN,
        Job.BRD,
        Job.MCH,
        Job.DNC,
        Job.BLM,
    };
    #endregion
}
