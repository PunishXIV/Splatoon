using System;
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
using ECommons.EzIpcManager;
using ECommons.GameFunctions;
using ECommons.MathHelpers;
using ECommons.Reflection;
using Splatoon.Gui.Priority;
using Splatoon.Memory;
using Splatoon.SplatoonScripting.Priority;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

internal class M12S_Grotesquerie1 : SplatoonScript
{
    /*
     * Constants and Types
     */

    #region Constants and Types

    private enum State
    {
        None,
        Casting,
        AOE1,
        AOE2,
        AOE3,
        SpreadOrStack,
    }

    private (State, Vector3)[] AoePositions =
    [
        (State.Casting, new Vector3(83f, 0f, 86f)),
        (State.AOE1, new Vector3(87f, 0f, 90f)),
        (State.AOE2, new Vector3(92f, 0f, 94f)),
        (State.AOE3, new Vector3(100f, 0f, 100f)),
    ];

    private struct DebugInfo
    {
        public bool isTHStack;
        public int PositionIndex;
        public float DailyRoutinesRadians;
    }

    #endregion

    /*
     * Public Fields
     */

    #region Public Fields

    public override HashSet<uint>? ValidTerritories => [1327,];
    public override Metadata? Metadata => new(2, "Redmoon");

    #endregion

    /*
     * Private Fields
     */

    #region Private Fields

    private State _state = State.None;
    private string _eastOrWest = "";
    private int _phagocyteCount = 0;
    private DebugInfo _debugInfo = new();

    //private DailyRoutines _dr = new();
    private Config _c => Controller.GetConfig<Config>();

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

        Controller.RegisterElementFromCode(
            "cone",
            "{\"Name\":\"\",\"type\":5,\"refX\":-1.3535173,\"refY\":-0.85854375,\"radius\":20.0,\"coneAngleMin\":-1,\"coneAngleMax\":1,\"color\":3372155392,\"fillIntensity\":0.5,\"refActorComparisonType\":4,\"includeRotation\":true,\"AdditionalRotation\":4.712389}");
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        var castId = packet->ActionID;
        // Grotesquerie
        if (castId == 48829) _state = State.Casting;

        if (_state == State.None) return;
        // Ravenous
        if (castId == 46237)
        {
            var deg = packet->Rotation.RadToDeg();
            _eastOrWest = deg switch
            {
                >= 265 and <= 275 => "West",
                >= 85 and <= 95 => "East",
                _ => "",
            };
        }
        // Phagocyte
        else if (castId == 46238)
        {
            if (_phagocyteCount == 0) _state++;
            _phagocyteCount++;
            if (_phagocyteCount >= 8) _phagocyteCount = 0;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_state == State.None) return;
        var castId = set.Action.Value.RowId;
        // Dramatic
        if (castId == 46250) OnReset();
    }

    public override void OnUpdate()
    {
        if (_state == State.None) return;
        Element? el;
        switch (_state)
        {
            case State.Casting:
            case State.AOE1:
            case State.AOE2:
            case State.AOE3:
            {
                var pos = AoePositions.First(x => x.Item1 == _state).Item2;
                if (Controller.TryGetElementByName("tether", out el))
                {
                    el.SetRefPosition(pos);
                    el.Enabled = true;
                }

                break;
            }
            case State.SpreadOrStack:
            {
                if (_eastOrWest == "")
                {
                    OnReset();
                    throw new Exception("East or West side not determined.");
                }

                var safePos = GetSafePosition();
                if (Controller.TryGetElementByName("tether", out el))
                {
                    el.SetRefPosition(safePos);
                    el.Enabled = true;
                }

                // var deg = GetShouldCharaRotation();
                // if (Controller.TryGetElementByName("cone", out el))
                // {
                //     el.SetRefPosition(safePos);
                //     el.AdditionalRotation = deg.DegToRad();
                //     el.Enabled = true;
                // }
                //
                // if (_c.UseDailyRoutines)
                // {
                //     var status = FakeParty.Get().FirstOrDefault(x => x.StatusList.Any(x => x.StatusId is 4762))
                //         ?.StatusList.FirstOrDefault(x => x.StatusId is 4762);
                //     // AlwaysOnがtrueの場合は常にDailyRoutinesで向き固定
                //     // それ以外はステータスの残り時間が1.5秒以下の場合にDailyRoutinesで向き固定
                //     if (_c.AlwaysOn || status == null || status.RemainingTime <= 1.5f)
                //     {
                //         if (!_dr.IsModuleEnabled("AutoFaceCameraDirection"))
                //             _dr.LoadModule("AutoFaceCameraDirection", true);
                //         var drDeg = ConvertDailyRoutinesDegrees(deg);
                //         _dr.LockOnChara(drDeg.DegToRad());
                //         _debugInfo.DailyRoutinesRadians = drDeg.DegToRad();
                //     }
                // }

                break;
            }
            case State.None:
                break;
            default:
                OnReset();
                throw new ArgumentOutOfRangeException();
        }

        if (Controller.TryGetElementByName("tether", out el))
        {
            if (el.Enabled)
                el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }
    }

    public class Config : IEzConfig
    {
        public int stackPosition = 1;
        public int spreadPosition = 1;
        public bool UseDailyRoutines = false;
        public bool AlwaysOn = false;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Stack Positions:");
        ImGui.SameLine();
        if (ImGui.BeginCombo("##stackPosition", _c.stackPosition.ToString()))
        {
            for (var i = 1; i < 5; i++)
                if (ImGui.Selectable(i.ToString()))
                    _c.stackPosition = i;
            ImGui.EndCombo();
        }

        ImGui.Text("Spread Positions:");
        ImGui.SameLine();
        if (ImGui.BeginCombo("##spreadPosition", _c.spreadPosition.ToString()))
        {
            for (var i = 1; i < 5; i++)
                if (ImGui.Selectable(i.ToString()))
                    _c.spreadPosition = i;
            ImGui.EndCombo();
        }

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"_state = {_state}");
            ImGui.Text($"_eastOrWest = {_eastOrWest}");
            ImGui.Text($"_phagocyteCount = {_phagocyteCount}");
            ImGui.Text($"IsTHStack = {_debugInfo.isTHStack}");
            ImGui.Text($"PositionIndex = {_debugInfo.PositionIndex}");
            //ImGui.Text($"DailyRoutines LoadedLockFace Module = {_dr.IsModuleEnabled("AutoFaceCameraDirection")}");
            ImGui.Text($"DailyRoutines Radians = {_debugInfo.DailyRoutinesRadians}");

            ImGui.Checkbox("Use Daily Routines API", ref _c.UseDailyRoutines);
            ImGui.Checkbox("AlwaysOn", ref _c.AlwaysOn);
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _eastOrWest = "";
        _phagocyteCount = 0;
        _debugInfo = new DebugInfo();
        // if (_dr.IsModuleEnabled("AutoFaceCameraDirection"))
        // {
        //     _dr.CancelLockOn();
        //     _dr.UnloadModule("AutoFaceCameraDirection", true, false);
        // }

        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    #endregion

/*
 * Private Methods
 */

    #region Private Methods

    private Vector3 GetSafePosition()
    {
        var isTHStack = FakeParty.Get()
            .Any(x => x.GetRole() != CombatRole.DPS && x.StatusList.Any(y => y.StatusId == 4762));
        _debugInfo.isTHStack = isTHStack;
        var statusId =
            Splatoon.Splatoon.BasePlayer.StatusList.FirstOrDefault(x => x.StatusId is 4761 or 4762)?.StatusId ?? 0;
        if (statusId == 0)
        {
            if ((isTHStack && Splatoon.Splatoon.BasePlayer.GetRole() != CombatRole.DPS) ||
                (!isTHStack && Splatoon.Splatoon.BasePlayer.GetRole() == CombatRole.DPS)) statusId = 4762;
            else
                throw new Exception("No status found.");
        }

        var index = statusId == 4761 ? _c.spreadPosition : _c.stackPosition + 10;
        _debugInfo.PositionIndex = index;
        var pos = index switch
        {
            // spread positions
            1 => new Vector3(102.5f, 0f, 85.5f),
            2 => new Vector3(107.0f, 0f, 93.5f),
            3 => new Vector3(114.0f, 0f, 100.0f),
            4 => new Vector3(107.0f, 0f, 106.5f),
            // stack positions
            11 => new Vector3(113.5f, 0f, 85.5f),
            12 => new Vector3(116.5f, 0f, 85.5f),
            13 => new Vector3(119.5f, 0f, 85.5f),
            14 => new Vector3(119.5f, 0f, 88.5f),
            _ => throw new Exception("Invalid position index."),
        };

        // if west side, mirror x-axis
        if (_eastOrWest != "West") pos.X = 200f - pos.X;

        return pos;
    }

    private float GetShouldCharaRotation()
    {
        var isTHStack = FakeParty.Get()
            .Any(x => x.GetRole() != CombatRole.DPS && x.StatusList.Any(y => y.StatusId == 4762));
        var statusId =
            Splatoon.Splatoon.BasePlayer.StatusList.FirstOrDefault(x => x.StatusId is 4761 or 4762)?.StatusId ?? 0;
        if (statusId == 0)
        {
            if ((isTHStack && Splatoon.Splatoon.BasePlayer.GetRole() != CombatRole.DPS) ||
                (!isTHStack && Splatoon.Splatoon.BasePlayer.GetRole() == CombatRole.DPS)) statusId = 4762;
            else
                throw new Exception("No status found.");
        }

        var index = statusId == 4761 ? _c.spreadPosition : _c.stackPosition + 10;
        var deg = index switch
        {
            // East side is default
            // spread positions
            1 => 180f,
            2 => 90f,
            3 => 90f,
            4 => 0f,
            // stack positions
            11 => 180f,
            12 => 180f,
            13 => 270f,
            14 => 270f,
            _ => throw new Exception("Invalid position index."),
        };

        // if west side, mirror x-axis
        if (_eastOrWest != "West") deg = (360f - deg) % 360f;

        return deg;
    }

    private float ConvertDailyRoutinesDegrees(float degrees)
    {
        // DRは南から反時計周り 既に南からを引数で渡すので反時計回りに変換
        return (360f - degrees) % 360f;
    }

    #endregion

    #region APIs

    internal class DailyRoutines
    {
        private EzIPCDisposalToken[] _ipcDisposalTokens;

        internal DailyRoutines()
        {
            _ipcDisposalTokens = EzIPC.Init(this, "DailyRoutines", SafeWrapper.IPCException);
        }

        internal void Dispose()
        {
            foreach (var token in _ipcDisposalTokens) token.Dispose();
        }

        internal bool IsInitialized()
        {
            return _ipcDisposalTokens.Length > 0 && _ipcDisposalTokens[0].IsDisposed == false;
        }

        [EzIPC] public readonly Func<string, bool> IsModuleEnabled;
        [EzIPC] public readonly Func<float> Version;
        [EzIPC] public readonly Func<string, bool, bool> LoadModule;
        [EzIPC] public readonly Func<string, bool, bool, bool> UnloadModule;

        [EzIPC("Modules.AutoFaceCameraDirection.%m")]
        public readonly Action<bool> SetWorkMode;

        [EzIPC("Modules.AutoFaceCameraDirection.%m")]
        public readonly Func<string, bool> LockOnGround;

        [EzIPC("Modules.AutoFaceCameraDirection.%m")]
        public readonly Action<float> LockOnChara;

        [EzIPC("Modules.AutoFaceCameraDirection.%m")]
        public readonly Action<float> LockOnCamera;

        [EzIPC("Modules.AutoFaceCameraDirection.%m")]
        public readonly Action CancelLockOn;
    }

    #endregion
}