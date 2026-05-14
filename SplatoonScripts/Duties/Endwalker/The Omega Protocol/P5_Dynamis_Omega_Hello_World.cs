using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ECommons.Hooks.ActionEffectTypes;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;

public class P5_Dynamis_Omega_Hello_World : SplatoonScript
{
    #region Metadata
    public override Metadata Metadata { get; } = new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryTop];
    #endregion

    #region Constant
    private const uint TerritoryTop = 1122;
    private const uint SceneId = 6;
    private const uint CastCodeDynamisOmega = 32789;
    private const uint CastMonitorWaveCannonLeft = 31638;
    private const uint CastMonitorWaveCannonRight = 31639;
    private const uint ActionHelloWorldNear = 31625;
    private const uint StatusHelloNear = 3442;
    private const uint StatusHelloFar = 3443;
    private const uint StatusFirstTarget = 3004;
    private const uint StatusSecondTarget = 3005;
    private const uint StatusDynamis = 3444;
    private const byte StatusDynamisTetherParam = 3;
    private const uint MarkerP1Unset = uint.MaxValue;
    private const float SettingsCellWidth = 150f;
    private const double RainbowHueCycleSeconds = 4d;

    private const string ElSep = "Spread1_Separator";
    private const string ElS1C38_BaitFar1 = "S1_C38_BaitFar1";
    private const string ElS1C38_BaitFar2 = "S1_C38_BaitFar2";
    private const string ElS1C38_BaitMon1 = "S1_C38_BaitMon1";
    private const string ElS1C38_BaitMon2 = "S1_C38_BaitMon2";
    private const string ElS1C39_BaitFar1 = "S1_C39_BaitFar1";
    private const string ElS1C39_BaitFar2 = "S1_C39_BaitFar2";
    private const string ElS1C39_BaitMon1 = "S1_C39_BaitMon1";
    private const string ElS1C39_BaitMon2 = "S1_C39_BaitMon2";
    private const string ElS2_BaitFar1 = "S2_BaitFar1";
    private const string ElS2_BaitFar2 = "S2_BaitFar2";
    private const string ElS2_Tether1 = "S2_Tether1";
    private const string ElS2_Tether2 = "S2_Tether2";

    private const string ElS1C38_FarSource = "S1_C38_FarSource";
    private const string ElS1C38_CloseSource = "S1_C38_CloseSource";
    private const string ElS1C38_NearA = "S1_C38_NearA";
    private const string ElS1C38_NearB = "S1_C38_NearB";
    private const string ElS1C39_FarSource = "S1_C39_FarSource";
    private const string ElS1C39_CloseSource = "S1_C39_CloseSource";
    private const string ElS1C39_NearA = "S1_C39_NearA";
    private const string ElS1C39_NearB = "S1_C39_NearB";
    private const string ElS2_FarSource = "S2_FarSource";
    private const string ElS2_NearSource = "S2_NearSource";
    private const string ElS2_NearA = "S2_NearA";
    private const string ElS2_NearB = "S2_NearB";

    private static readonly string[] AllSpreadElementNames =
    [
        ElSep,
        ElS1C38_FarSource,
        ElS1C38_BaitFar1,
        ElS1C38_CloseSource,
        ElS1C38_BaitFar2,
        ElS1C38_NearA,
        ElS1C38_NearB,
        ElS1C38_BaitMon1,
        ElS1C38_BaitMon2,
        ElS1C39_FarSource,
        ElS1C39_BaitFar1,
        ElS1C39_BaitFar2,
        ElS1C39_NearA,
        ElS1C39_NearB,
        ElS1C39_BaitMon1,
        ElS1C39_BaitMon2,
        ElS1C39_CloseSource,
        ElS2_FarSource,
        ElS2_BaitFar1,
        ElS2_NearSource,
        ElS2_BaitFar2,
        ElS2_NearA,
        ElS2_NearB,
        ElS2_Tether1,
        ElS2_Tether2
    ];

    private static readonly MarkerType[] MarkerPresetOrder =
    [
        MarkerType.Attack1,
        MarkerType.Attack2,
        MarkerType.Attack3,
        MarkerType.Attack4,
        MarkerType.Attack5,
        MarkerType.Attack6,
        MarkerType.Bind1,
        MarkerType.Bind2,
        MarkerType.Stop1,
        MarkerType.Stop2
    ];

    private static readonly string[] MarkerPresetComboLabels =
    [
        "Attack1",
        "Attack2",
        "Attack3",
        "Attack4",
        "Attack5",
        "Attack6",
        "Bind1",
        "Bind2",
        "Stop1",
        "Stop2"
    ];

    #endregion

    #region Config

    public sealed class Config : IEzConfig
    {
        public MarkerType Spread1BaitMonitor1Marker = MarkerType.Bind1;
        public MarkerType Spread1BaitMonitor2Marker = MarkerType.Bind2;
        public MarkerType Spread1BaitFar1Marker = MarkerType.Attack1;
        public MarkerType Spread1BaitFar2Marker = MarkerType.Attack2;
        public MarkerType Spread1BaitNear1Marker = MarkerType.Attack3;
        public MarkerType Spread1BaitNear2Marker = MarkerType.Attack4;

        public MarkerType Spread2BaitTether1Marker = MarkerType.Bind1;
        public MarkerType Spread2BaitTether2Marker = MarkerType.Bind2;
        public MarkerType Spread2BaitFar1Marker = MarkerType.Attack1;
        public MarkerType Spread2BaitFar2Marker = MarkerType.Attack2;
        public MarkerType Spread2BaitNear1Marker = MarkerType.Attack3;
        public MarkerType Spread2BaitNear2Marker = MarkerType.Attack4;

        public bool Spread1ResolveBaitNearWithoutMarker = false;
        public bool Spread2ResolveBaitNearWithoutMarker = false;

        public bool Spread2ResolveBaitTetherByStatus = false;
    }

    private Config C => Controller.GetConfig<Config>();

    #endregion

    #region State
    private readonly Dictionary<ulong, PlayerData> _players = [];
    private ScriptState _state = ScriptState.None;
    private bool _monitorIsLeft;
    #endregion

    #region Private Class

    private enum ScriptState
    {
        None,
        Wait,
        Spread1,
        Spread2
    }

    private enum Role
    {
        None,
        BaitFar1,
        BaitFar2,
        BaitMonitor1,
        BaitMonitor2,
        HelloNear,
        HelloFar,
        BaitNear1,
        BaitNear2
    }

    public enum MarkerType : uint
    {
        None = 999,
        Attack1 = 0,
        Attack2 = 1,
        Attack3 = 2,
        Attack4 = 3,
        Attack5 = 4,
        Bind1 = 5,
        Bind2 = 6,
        Bind3 = 7,
        Stop1 = 8,
        Stop2 = 9,
        Square = 10,
        Circle = 11,
        Cross = 12,
        Triangle = 13,
        Attack6 = 14,
        Attack7 = 15,
        Attack8 = 16
    }

    private sealed class PlayerData
    {
        public ulong ObjectId;
        public string Name = string.Empty;
        public uint MarkerP1 = MarkerP1Unset;
        public Role Role = Role.None;
    }

    private readonly record struct Spread1Pack(
        string Mon1,
        string Mon2,
        string NearA,
        string NearB,
        string BaitFar1,
        string BaitFar2,
        string FarSource,
        string HelloNearSource);

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode(ElSep, """{"Name":"Separator","Enabled":false,"type":3,"refY":20.0,"offY":-20.0,"radius":0.0,"color":4294967295,"overlayBGColor":4278190335,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"MONITOR BAITER","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31639,31638],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);

        Controller.RegisterElementFromCode(ElS1C38_FarSource, """{"Name":"Monitor right - HelloWorldFar","Enabled":false,"type":1,"offX":-2.0,"offY":-11.0,"radius":0.5,"color":4288326400,"overlayBGColor":4285363712,"overlayTextColor":4294967295,"thicc":5.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31638],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C38_CloseSource, """{"Name":"Monitor right - HelloWorldNear","Enabled":false,"type":1,"offX":-10.96,"radius":0.5,"color":4278225677,"overlayBGColor":4278220288,"overlayTextColor":4294967295,"thicc":5.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31638],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C38_BaitMon1, """{"Name":"Monitor right - BaitMonitor1","Enabled":false,"type":1,"offX":9.0,"offY":9.0,"radius":0.5,"color":4278190335,"overlayBGColor":4278190335,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31638],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C38_BaitMon2, """{"Name":"Monitor right - BaitMonitor2","Enabled":false,"type":1,"offX":9.0,"offY":-9.0,"radius":0.5,"color":4278190335,"overlayBGColor":4278190335,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31638],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C38_BaitFar1, """{"Name":"Monitor right - BaitFar1","Enabled":false,"type":1,"offX":-2.0,"offY":19.0,"radius":0.5,"color":3355508503,"overlayBGColor":2617245696,"overlayTextColor":4278255360,"thicc":3.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31638],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C38_BaitFar2, """{"Name":"Monitor right - BaitFar2","Enabled":false,"type":1,"offX":-2.0,"offY":-18.5,"radius":0.5,"color":3355508503,"overlayBGColor":2617245696,"overlayTextColor":4278255360,"thicc":3.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31638],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C38_NearA, """{"Name":"Monitor right - BaitNear1","Enabled":false,"type":1,"offX":-18.0,"offY":2.56,"radius":0.5,"color":4278237622,"overlayBGColor":4278236333,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31638],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C38_NearB, """{"Name":"Monitor right - BaitNear2","Enabled":false,"type":1,"offX":-18.18,"offY":-3.68,"radius":0.5,"color":4278237622,"overlayBGColor":4278236333,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31638],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);

        Controller.RegisterElementFromCode(ElS1C39_FarSource, """{"Name":"Monitor left - HelloWorldFar","Enabled":false,"type":1,"offX":2.0,"offY":-11.0,"radius":0.5,"color":4288326400,"overlayBGColor":4285363712,"overlayTextColor":4294967295,"thicc":5.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31639],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C39_CloseSource, """{"Name":"Monitor left - HelloWorldNear","Enabled":false,"type":1,"offX":10.96,"radius":0.5,"color":4278225677,"overlayBGColor":4278220288,"overlayTextColor":4294967295,"thicc":5.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31639],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C39_BaitMon1, """{"Name":"Monitor left - BaitMonitor1","Enabled":false,"type":1,"offX":-9.0,"offY":-9.0,"radius":0.5,"color":4278190335,"overlayBGColor":4278190335,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31639],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C39_BaitMon2, """{"Name":"Monitor left - BaitMonitor2","Enabled":false,"type":1,"offX":-9.0,"offY":9.0,"radius":0.5,"color":4278190335,"overlayBGColor":4278190335,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31639],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C39_BaitFar2, """{"Name":"Monitor left - BaitFar2", "Enabled":false,"type":1,"offX":2.0,"offY":19.0,"radius":0.5,"color":3355508503,"overlayBGColor":2617245696,"overlayTextColor":4278255360,"thicc":3.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31639],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C39_BaitFar1, """{"Name":"Monitor left - BaitFar1", "Enabled":false,"type":1,"offX":2.0,"offY":-18.5,"radius":0.5,"color":3355508503,"overlayBGColor":2617245696,"overlayTextColor":4278255360,"thicc":3.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31639],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C39_NearA, """{"Name":"Monitor left - BaitNear1","Enabled":false,"type":1,"offX":18.18,"offY":-3.68,"radius":0.5,"color":4278237622,"overlayBGColor":4278236333,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31639],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS1C39_NearB, """{"Name":"Monitor left - BaitNear2","Enabled":false,"type":1,"offX":18.0,"offY":2.56,"radius":0.5,"color":4278237622,"overlayBGColor":4278236333,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"","refActorNPCNameID":7636,"refActorRequireCast":true,"refActorCastId":[31639],"refActorComparisonType":6,"includeRotation":true}""", overwrite: true);

        Controller.RegisterElementFromCode(ElS2_FarSource, """{"Name":"HelloWorldFar","Enabled":false,"type":1,"offX":-8.5,"offY":20.0,"radius":0.5,"color":4288326400,"overlayBGColor":4285363712,"overlayTextColor":4294967295,"thicc":5.0,"overlayText":"","refActorNPCNameID":7695,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS2_NearSource, """{"Name":"HelloWorldNear","Enabled":false,"type":1,"offY":30.34,"radius":0.5,"color":4278225677,"overlayBGColor":4278220288,"overlayTextColor":4294967295,"thicc":5.0,"overlayText":"","refActorNPCNameID":7695,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS2_Tether1, """{"Name":"BaitTether1","Enabled":false,"type":1,"offX":11.0,"offY":5.25,"radius":0.5,"color":4278190335,"overlayBGColor":4278190335,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"","refActorNPCID":7695,"refActorComparisonType":4,"includeRotation":true,"onlyVisible":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS2_Tether2, """{"Name":"BaitTether2","Enabled":false,"type":1,"offX":-11.0,"offY":5.25,"radius":0.5,"color":4278190335,"overlayBGColor":4278190335,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"","refActorNPCID":7695,"refActorComparisonType":4,"includeRotation":true,"onlyVisible":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS2_BaitFar1, """{"Name":"BaitFar1","Enabled":false,"type":1,"offX":19.0,"offY":20.0,"radius":0.5,"color":3355508503,"overlayBGColor":2617245696,"overlayTextColor":4278255360,"thicc":3.0,"overlayText":"","refActorNPCNameID":7695,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS2_BaitFar2, """{"Name":"BaitFar2","Enabled":false,"type":1,"offX":-19.0,"offY":20.0,"radius":0.5,"color":3355508503,"overlayBGColor":2617245696,"overlayTextColor":4278255360,"thicc":3.0,"overlayText":"","refActorNPCNameID":7695,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS2_NearA, """{"Name":"BaitNear1","Enabled":false,"type":1,"offX":4.76,"offY":38.0,"radius":0.5,"color":4278237622,"overlayBGColor":4278236333,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"","refActorNPCNameID":7695,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true}""", overwrite: true);
        Controller.RegisterElementFromCode(ElS2_NearB, """{"Name":"BaitNear2","Enabled":false,"type":1,"offX":-2.86,"offY":38.22,"radius":0.5,"color":4278237622,"overlayBGColor":4278236333,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"","refActorNPCNameID":7695,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true}""", overwrite: true);
    }

    public override void OnUpdate()
    {
        DisableAllSpreadElements();

        if(Controller.Scene != SceneId)
            return;

        if(_players.Count < 8)
            BuildPartySnapshot();

        if(BasePlayer == null || _players.Count != 8)
            return;

        if(!_players.TryGetValue(BasePlayer.GameObjectId, out var me))
            return;

        RecomputeDerivedRoles();

        if(me.Role == Role.None)
            return;

        if(_state == ScriptState.Spread1)
        {
            EnableElement(ElSep, false);
            EnableSpread1Element(me.Role, _monitorIsLeft);
            return;
        }

        if(_state == ScriptState.Spread2)
            EnableSpread2Element(me.Role);
    }

    public override void OnReset()
    {
        _players.Clear();
        _state = ScriptState.None;
        _monitorIsLeft = false;
        DisableAllSpreadElements();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(!IsPhaseFive())
            return;

        if(_state == ScriptState.None && castId == CastCodeDynamisOmega)
        {
            _state = ScriptState.Wait;
            return;
        }

        if(_state == ScriptState.Wait && castId == CastMonitorWaveCannonLeft)
        {
            _state = ScriptState.Spread1;
            _monitorIsLeft = true;
            return;
        }

        if(_state == ScriptState.Wait && castId == CastMonitorWaveCannonRight)
        {
            _state = ScriptState.Spread1;
            _monitorIsLeft = false;
            return;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!IsPhaseFive() || set.Action == null)
            return;

        var actionId = set.Action.Value.RowId;
        if(actionId != ActionHelloWorldNear)
            return;

        if(_state == ScriptState.Spread1)
            _state = ScriptState.Spread2;
        else if(_state == ScriptState.Spread2)
            OnReset();
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, uint p7, uint p8, ulong targetId, byte replaying)
    {
        if(!IsPhaseFive() || command != 502)
            return;

        if(!TryGetMarkedPlayer(targetId, p2, out var markedGoId, out var data))
            return;

        data.MarkerP1 = p1;
        if(data.Name.Length == 0)
            data.Name = TryGetObjectName(markedGoId);

        RecomputeDerivedRoles();
    }

    public override void OnSettingsDraw()
    {
        ImGui.TextWrapped("This Script Resolve Spread Roles from marker and status.");
        ImGui.Spacing();

        ImGui.Text("Spread1 (North: Monitor)");
        if(ImGuiEx.BeginDefaultTable("P5OmegaHelloWorldS1", ["Role", "Marker"]))
        {
            DrawHintOnlyRow("HelloNear", "Status: HelloWorldNear & FirstTarget");
            DrawHintOnlyRow("HelloFar", "Status: HelloWorldFar & FirstTarget");
            DrawMarkerOnlyRow("BaitMonitor1", ref C.Spread1BaitMonitor1Marker);
            DrawMarkerOnlyRow("BaitMonitor2", ref C.Spread1BaitMonitor2Marker);
            DrawMarkerOnlyRow("BaitFar1", ref C.Spread1BaitFar1Marker);
            DrawMarkerOnlyRow("BaitFar2", ref C.Spread1BaitFar2Marker);
            if(C.Spread1ResolveBaitNearWithoutMarker)
            {
                DrawHintOnlyRow("BaitNear1", "Remaining");
                DrawHintOnlyRow("BaitNear2", "Remaining");
            }
            else
            {
                DrawMarkerOnlyRow("BaitNear1", ref C.Spread1BaitNear1Marker);
                DrawMarkerOnlyRow("BaitNear2", ref C.Spread1BaitNear2Marker);
            }
            ImGui.EndTable();
        }

        ImGui.Checkbox("Resolve BaitNear without Marker. Tether to 2 BaitNear Positions##s1", ref C.Spread1ResolveBaitNearWithoutMarker);

        ImGui.Spacing();
        ImGui.Text("Spread2 (North: Omega Beetle)");
        if(ImGuiEx.BeginDefaultTable("P5OmegaHelloWorldS2", ["Role", "Marker"]))
        {
            DrawHintOnlyRow("HelloNear", "Status: HelloWorldNear & SecondTarget");
            DrawHintOnlyRow("HelloFar", "Status: HelloWorldFar & SecondTarget");
            if(C.Spread2ResolveBaitTetherByStatus)
            {
                DrawHintOnlyRow("BaitTether1", "Status: Dynamis 3 stack");
                DrawHintOnlyRow("BaitTether2", "Status: Dynamis 3 stack");
            }
            else
            {
                DrawMarkerOnlyRow("BaitTether1", ref C.Spread2BaitTether1Marker);
                DrawMarkerOnlyRow("BaitTether2", ref C.Spread2BaitTether2Marker);
            }
            DrawMarkerOnlyRow("BaitFar1", ref C.Spread2BaitFar1Marker);
            DrawMarkerOnlyRow("BaitFar2", ref C.Spread2BaitFar2Marker);
            if(C.Spread2ResolveBaitNearWithoutMarker)
            {
                DrawHintOnlyRow("BaitNear1", "Remaining");
                DrawHintOnlyRow("BaitNear2", "Remaining");
            }
            else
            {
                DrawMarkerOnlyRow("BaitNear1", ref C.Spread2BaitNear1Marker);
                DrawMarkerOnlyRow("BaitNear2", ref C.Spread2BaitNear2Marker);
            }
            ImGui.EndTable();
        }

        ImGui.Checkbox("Resolve BaitNear without Marker. Tether to 2 BaitNear Positions##s2", ref C.Spread2ResolveBaitNearWithoutMarker);
        ImGui.Checkbox("Resolve BaitTether by status. (Dynamis 3 stack). Tether to 2 BaitTether Positions.", ref C.Spread2ResolveBaitTetherByStatus);

        ImGui.Spacing();
        if(ImGui.CollapsingHeader("Debug"))
            DrawDebugStateSection();
    }

    #endregion

    #region Private Method

    private bool TryGetMarkedPlayer(ulong targetId, ulong p2, out ulong markedGoId, out PlayerData? data)
    {
        markedGoId = 0;
        data = null;
        if(targetId != 0 && _players.TryGetValue(targetId, out var byTarget))
        {
            markedGoId = targetId;
            data = byTarget;
            return true;
        }

        if(p2 != 0 && _players.TryGetValue(p2, out var byP2))
        {
            markedGoId = p2;
            data = byP2;
            return true;
        }

        return false;
    }

    private static string TryGetObjectName(ulong gameObjectId)
        => Svc.Objects.FirstOrDefault(x => x.GameObjectId == gameObjectId)?.Name.ToString() ?? string.Empty;

    private static IPlayerCharacter? FindPartyCharacter(ulong gameObjectId)
        => Svc.Objects.FirstOrDefault(x => x.GameObjectId == gameObjectId) as IPlayerCharacter;

    private static bool HasStatus(IPlayerCharacter obj, uint statusId)
        => obj.StatusList.Any(s => s != null && s.StatusId == statusId);

    private static bool HasStatus(IPlayerCharacter obj, uint statusId, byte param)
        => obj.StatusList.Any(s => s != null && s.StatusId == statusId && s.Param == param);

    private static readonly Comparison<PlayerData> ByObjectId = (a, b) => a.ObjectId.CompareTo(b.ObjectId);

    private static Spread1Pack Spread1PackForSide(bool monitorIsLeft)
        => monitorIsLeft
            ? new Spread1Pack(
                ElS1C38_BaitMon1,
                ElS1C38_BaitMon2,
                ElS1C38_NearA,
                ElS1C38_NearB,
                ElS1C38_BaitFar1,
                ElS1C38_BaitFar2,
                ElS1C38_FarSource,
                ElS1C38_CloseSource)
            : new Spread1Pack(
                ElS1C39_BaitMon1,
                ElS1C39_BaitMon2,
                ElS1C39_NearA,
                ElS1C39_NearB,
                ElS1C39_BaitFar1,
                ElS1C39_BaitFar2,
                ElS1C39_FarSource,
                ElS1C39_CloseSource);

    // Spread1: one monitor spot per Bind role (Mon1 / Mon2). BaitNear follows Spread1 near option.
    private void EnableSpread1Element(Role role, bool monitorIsLeft)
    {
        var pack = Spread1PackForSide(monitorIsLeft);

        if(role == Role.BaitMonitor1)
        {
            EnableElement(pack.Mon1);
            return;
        }

        if(role == Role.BaitMonitor2)
        {
            EnableElement(pack.Mon2);
            return;
        }

        if(role is Role.BaitNear1 or Role.BaitNear2)
        {
            if(C.Spread1ResolveBaitNearWithoutMarker)
                EnableElements(pack.NearA, pack.NearB);
            else if(role == Role.BaitNear1)
                EnableElement(pack.NearA);
            else
                EnableElement(pack.NearB);
            return;
        }

        var name = role switch
        {
            Role.BaitFar1 => pack.BaitFar1,
            Role.BaitFar2 => pack.BaitFar2,
            Role.HelloFar => pack.FarSource,
            Role.HelloNear => pack.HelloNearSource,
            _ => null
        };

        if(name != null)
            EnableElement(name);
    }

    // Spread2: marker tether → one spot per Bind (Tether1 / Tether2). Status tether → both spots with tethers.
    // BaitNear follows Spread2 near option.
    private void EnableSpread2Element(Role role)
    {
        if(role is Role.BaitMonitor1 or Role.BaitMonitor2)
        {
            if(C.Spread2ResolveBaitTetherByStatus)
                EnableElements(ElS2_Tether1, ElS2_Tether2);
            else if(role == Role.BaitMonitor1)
                EnableElement(ElS2_Tether1);
            else
                EnableElement(ElS2_Tether2);
            return;
        }

        if(role is Role.BaitNear1 or Role.BaitNear2)
        {
            if(C.Spread2ResolveBaitNearWithoutMarker)
                EnableElements(ElS2_NearA, ElS2_NearB);
            else if(role == Role.BaitNear1)
                EnableElement(ElS2_NearA);
            else
                EnableElement(ElS2_NearB);
            return;
        }

        var name = role switch
        {
            Role.BaitFar1 => ElS2_BaitFar1,
            Role.BaitFar2 => ElS2_BaitFar2,
            Role.HelloFar => ElS2_FarSource,
            Role.HelloNear => ElS2_NearSource,
            _ => null
        };

        if(name != null)
            EnableElement(name);
    }

    private void EnableElements(params string[] names)
    {
        foreach(var n in names)
            EnableElement(n, true);
    }

    // Turns on a registered layout element with tether and cycling rainbow tint (Sigma-style nav).
    private void EnableElement(string elementName, bool isRainbow = true)
    {
        if(!Controller.TryGetElementByName(elementName, out var element))
            return;
        if(isRainbow)
            element.color = ImGui.ColorConvertFloat4ToU32(GetRainbowColor(RainbowHueCycleSeconds));
        element.tether = true;
        element.Enabled = true;
    }

    // Full-saturation RGBA that cycles hue over wall-clock time.
    private Vector4 GetRainbowColor(double cycleSeconds)
    {
        if(cycleSeconds <= 0d)
            cycleSeconds = 1d;
        var normalizedTime = Environment.TickCount64 / 1000d / cycleSeconds;
        var hue = normalizedTime % 1f;
        return HsvToVector4(hue, 1f, 1f);
    }

    // Converts HSV in [0,1] to linear RGB with alpha 1.
    private static Vector4 HsvToVector4(double h, double s, double v)
    {
        double r = 0d;
        double g = 0d;
        double b = 0d;
        var i = (int)(h * 6d);
        var f = h * 6d - i;
        var p = v * (1d - s);
        var q = v * (1d - f * s);
        var t = v * (1d - (1d - f) * s);

        switch(i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new Vector4((float)r, (float)g, (float)b, 1f);
    }

    // Disables every registered spread layout element.
    private void DisableAllSpreadElements()
    {
        foreach(var n in AllSpreadElementNames)
            DisableElement(n);
    }

    // Disables one element by name if present.
    private void DisableElement(string elementName)
    {
        if(Controller.TryGetElementByName(elementName, out var element))
            element.Enabled = false;
    }

    // One settings row with marker combo only.
    private static void DrawMarkerOnlyRow(string roleLabel, ref MarkerType marker)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(roleLabel);
        ImGui.TableNextColumn();
        DrawMarkerCell(roleLabel + "_mk", ref marker);
    }

    // One settings row: fixed hint text in the marker column (no combo).
    private static void DrawHintOnlyRow(string roleLabel, string markerColumnHint)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(roleLabel);
        ImGui.TableNextColumn();
        ImGui.TextDisabled(markerColumnHint);
    }

    // Narrow combo: Attack1–6, Bind1–2, Stop1–2 only.
    private static void DrawMarkerCell(string id, ref MarkerType marker)
    {
        var idx = Array.IndexOf(MarkerPresetOrder, marker);
        if(idx < 0)
        {
            marker = MarkerPresetOrder[0];
            idx = 0;
        }

        ImGui.PushID(id);
        ImGui.SetNextItemWidth(SettingsCellWidth);
        if(ImGui.Combo("##marker", ref idx, MarkerPresetComboLabels, MarkerPresetComboLabels.Length))
            marker = MarkerPresetOrder[idx];
        ImGui.PopID();
    }

    // Debug panel: state machine and scene for troubleshooting.
    private void DrawDebugStateSection()
    {
        ImGui.Text($"State: {_state}");
        ImGui.Text($"MonitorIsLeft: {_monitorIsLeft}");
        ImGui.Text($"Scene: {Controller.Scene} (expected P5: {SceneId})");
        ImGui.Text($"Party snapshot: {_players.Count} / 8");
        ImGui.Text(_state == ScriptState.Spread2
            ? C.Spread2ResolveBaitTetherByStatus
                ? "Marker mapping: Spread2 columns (tether: Status 3444 param 3)"
                : "Marker mapping: Spread2 columns"
            : "Marker mapping: Spread1 columns");
        ImGui.TextDisabled($"IsPhaseFive (scene == {SceneId}): {IsPhaseFive()}");

        ImGui.Separator();
        ImGui.TextUnformatted("BasePlayer:");
        if(BasePlayer == null)
        {
            ImGui.TextDisabled("(null)");
            return;
        }

        if(!_players.TryGetValue(BasePlayer.GameObjectId, out var me))
        {
            ImGui.TextDisabled("Not in party snapshot.");
            return;
        }

        RecomputeDerivedRoles();

        var displayName = me.Name.Length > 0 ? me.Name : BasePlayer.Name.ToString();
        ImGui.Text($"Name: {displayName}");
        ImGui.Text($"Role: {me.Role}");
        ImGui.Text($"Marker P1: {FormatMarkerP1(me.MarkerP1)}");
    }

    // Raid marker P1 for debug label (unset vs enum name vs raw id).
    private static string FormatMarkerP1(uint p1)
    {
        if(p1 == MarkerP1Unset)
            return "(unset)";
        return Enum.IsDefined(typeof(MarkerType), p1)
            ? $"{(MarkerType)p1} ({p1})"
            : $"{p1}";
    }

    // True when duty scene is P5 Dynamis (Omega section).
    private bool IsPhaseFive()
        => Controller.Scene == SceneId;

    // Rebuilds the eight-player snapshot from the current party list.
    private void BuildPartySnapshot()
    {
        _players.Clear();
        foreach(var pc in Controller.GetPartyMembers().OfType<IPlayerCharacter>())
        {
            _players[pc.GameObjectId] = new PlayerData
            {
                ObjectId = pc.GameObjectId,
                Name = pc.Name.ToString(),
                MarkerP1 = MarkerP1Unset,
                Role = Role.None
            };
        }
    }

    // Assigns bait roles from raid markers using Spread1 mapping in Wait/Spread1, Spread2 mapping in Spread2.
    private void ApplyMarkerRoles()
    {
        GetMarkerSlotIds(out var far1, out var far2, out var sec1, out var sec2, out var skipSpread2TetherMarkers);

        var useNearMarkers = _state == ScriptState.Spread2
            ? !C.Spread2ResolveBaitNearWithoutMarker
            : !C.Spread1ResolveBaitNearWithoutMarker;
        uint nearM1 = 0;
        uint nearM2 = 0;
        if(useNearMarkers)
        {
            if(_state == ScriptState.Spread2)
            {
                nearM1 = (uint)C.Spread2BaitNear1Marker;
                nearM2 = (uint)C.Spread2BaitNear2Marker;
            }
            else
            {
                nearM1 = (uint)C.Spread1BaitNear1Marker;
                nearM2 = (uint)C.Spread1BaitNear2Marker;
            }
        }

        foreach(var player in _players.Values)
        {
            Role role = Role.None;
            if(player.MarkerP1 == far1)
                role = Role.BaitFar1;
            else if(player.MarkerP1 == far2)
                role = Role.BaitFar2;
            else if(!skipSpread2TetherMarkers && player.MarkerP1 == sec1)
                role = Role.BaitMonitor1;
            else if(!skipSpread2TetherMarkers && player.MarkerP1 == sec2)
                role = Role.BaitMonitor2;
            else if(useNearMarkers && player.MarkerP1 == nearM1)
                role = Role.BaitNear1;
            else if(useNearMarkers && player.MarkerP1 == nearM2)
                role = Role.BaitNear2;
            player.Role = role;
        }
    }

    private void GetMarkerSlotIds(out uint far1, out uint far2, out uint sec1, out uint sec2, out bool skipSpread2TetherMarkers)
    {
        skipSpread2TetherMarkers = _state == ScriptState.Spread2 && C.Spread2ResolveBaitTetherByStatus;
        if(_state == ScriptState.Spread2)
        {
            far1 = (uint)C.Spread2BaitFar1Marker;
            far2 = (uint)C.Spread2BaitFar2Marker;
            sec1 = (uint)C.Spread2BaitTether1Marker;
            sec2 = (uint)C.Spread2BaitTether2Marker;
            return;
        }

        far1 = (uint)C.Spread1BaitFar1Marker;
        far2 = (uint)C.Spread1BaitFar2Marker;
        sec1 = (uint)C.Spread1BaitMonitor1Marker;
        sec2 = (uint)C.Spread1BaitMonitor2Marker;
    }

    // Spread2 + option: two tether baiters from Dynamis (3444) param 3 (order arbitrary; slots fixed by ObjectId sort).
    private void ApplySpread2TetherRolesFromStatus()
    {
        if(_state != ScriptState.Spread2 || !C.Spread2ResolveBaitTetherByStatus)
            return;

        var tetherPlayers = new List<PlayerData>();
        foreach(var (objectId, player) in _players.ToArray())
        {
            var obj = FindPartyCharacter(objectId);
            if(obj == null)
                continue;
            if(HasStatus(obj, StatusDynamis, StatusDynamisTetherParam))
                tetherPlayers.Add(player);
        }

        if(tetherPlayers.Count != 2)
            return;

        tetherPlayers.Sort(ByObjectId);
        tetherPlayers[0].Role = Role.BaitMonitor1;
        tetherPlayers[1].Role = Role.BaitMonitor2;
    }

    // Spread1: Hello near/far only when FirstTarget (3004) is present; Spread2: only when SecondTarget (3005).
    private bool PassesNearFarTargetGate(IPlayerCharacter obj)
        => _state switch
        {
            ScriptState.Spread1 => HasStatus(obj, StatusFirstTarget),
            ScriptState.Spread2 => HasStatus(obj, StatusSecondTarget),
            _ => false
        };

    // Overwrites roles from hello near/far debuffs on live characters.
    private void ApplyNearFarRoles()
    {
        foreach(var (objectId, player) in _players.ToArray())
        {
            var obj = FindPartyCharacter(objectId);
            if(obj == null)
                continue;
            if(!PassesNearFarTargetGate(obj))
                continue;
            if(HasStatus(obj, StatusHelloNear))
                player.Role = Role.HelloNear;
            else if(HasStatus(obj, StatusHelloFar))
                player.Role = Role.HelloFar;
        }
    }

    // Assigns near-bait roles to the two remaining players when not using BaitNear markers.
    private void ResolveRemainingNearRoles()
    {
        var useRemaining = _state == ScriptState.Spread2
            ? C.Spread2ResolveBaitNearWithoutMarker
            : C.Spread1ResolveBaitNearWithoutMarker;
        if(!useRemaining)
            return;

        var remaining = _players.Values.Where(x => x.Role == Role.None).ToList();
        if(remaining.Count != 2)
            return;

        remaining.Sort(ByObjectId);
        remaining[0].Role = Role.BaitNear1;
        remaining[1].Role = Role.BaitNear2;
    }

    // Marker roles, hello debuffs, then remaining near bait assignment.
    private void RecomputeDerivedRoles()
    {
        ApplyMarkerRoles();
        ApplyNearFarRoles();
        ApplySpread2TetherRolesFromStatus();
        ResolveRemainingNearRoles();
    }

    #endregion
}
