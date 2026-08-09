using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;

public class P5_Dynamis_Delta_Guide : SplatoonScript
{
    #region Metadata
    public override Metadata Metadata { get; } = new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryTop];
    #endregion

    #region Constant
    private const uint TerritoryTop = 1122;
    private const uint SceneId = 6;

    private const uint CastDynamisDelta = 31624;
    private const uint CastOpticalLaser = 31628;
    private const uint CastEnbug = 31587;
    private const uint CastRocketPunch = 31482;
    private const uint CastShieldCombo = 31528;
    private const uint CastOverSampledRight = 31638;
    private const uint CastOverSampledLeft = 31639;
    private const uint CastRazorLeft = 31636;
    private const uint CastRazorRight = 31637;
    private const uint CastHelloNear = 31625;
    private const uint CastHelloFar = 33040;

    private const ushort StatusTetherGreen = 0xD70;
    private const ushort StatusTetherBlue = 0xDB0;
    private const ushort StatusHelloNear = 0xD72;
    private const ushort StatusHelloFar = 0xD73;
    private const ushort StatusCannonLeft = 3452;
    private const ushort StatusCannonRight = 3453;
    private const ushort StatusDoomMark2 = 0x9E6;
    private const ushort StatusMagicVulnUp = 0xB7D;
    private const ushort StatusEnbugNear = 0x688;

    private const uint DataIdFinal = 0x394D;
    private const uint DataIdBeetle = 0x3D6C;
    private const uint DataIdRocketBlue = 0x3D5D;
    private const uint DataIdRocketYellow = 0x3D5E;

    private const uint TetherParam2 = 0;
    private const uint TetherParamGreen = 200;
    private const uint TetherParamBlue = 201;
    private const uint TetherParam5 = 15;
    private const int RocketCount = 8;

    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);

    private const string LayoutStep0 = "step0";
    private const string LayoutStep1 = "step1";
    private const string LayoutStep2 = "step2";
    private const string LayoutStep3 = "step3";
    private const string LayoutStep4WaveRight = "step4_waveright";
    private const string LayoutStep4WaveLeft = "step4_waveleft";
    private const string LayoutStep5Left = "step5_left";
    private const string LayoutStep5Right = "step5_right";
    private const string LayoutHints = "Hints";
    private const string ElHint = "Hint";
    private const string ElPartnerTether = "PartnerTether";
    private const string ElBaitArm = "BaitArm";

    private const string VfxTurningCw = "vfx/lockon/eff/m0515_turning_right01c.avfx";
    private const string VfxTurningCcw = "vfx/lockon/eff/m0515_turning_left01c.avfx";
    private const float BaitArmRadius = 20f;
    private const float BaitArmAngleOffset = 10f;
    private const int ArmCount = 6;

    private static readonly string[] AllGuideLayoutNames =
    [
        LayoutStep0,
        LayoutStep1,
        LayoutStep2,
        LayoutStep3,
        LayoutStep4WaveRight,
        LayoutStep4WaveLeft,
        LayoutStep5Left,
        LayoutStep5Right
    ];
    #endregion

    #region Config
    private enum FarTakerMode
    {
        GreenInside,
        GreenOutside
    }

    private enum RocketSwapSideMode
    {
        Inside,
        Outside
    }

    private enum BashedPlayerSideMode
    {
        BeetleOmega,
        FinalOmega
    }

    private sealed class Config : IEzConfig
    {
        public FarTakerMode Green_FarTaker = FarTakerMode.GreenInside;
        public RocketSwapSideMode BlueRocketSwapSide = RocketSwapSideMode.Inside;
        public RocketSwapSideMode GreenRocketSwapSide = RocketSwapSideMode.Inside;
        public BashedPlayerSideMode BashedPlayerSide = BashedPlayerSideMode.FinalOmega;
    }

    private Config C => Controller.GetConfig<Config>();
    #endregion

    #region State
    private readonly Dictionary<uint, PlayerInfo> _players = [];
    private readonly Dictionary<uint, ArmInfo> _armsById = [];
    private List<ArmInfo> _armsSorted = [];
    private bool _isDelta;
    private bool _rocketsAssigned;
    private bool _armsReady;
    private int _step;
    private WaveKind _wave;
    private RazorKind _razor;
    private float _beetleWorldAngle;
    #endregion

    #region Private Class
    private enum TetherKind
    {
        None,
        Green,
        Blue
    }

    private enum HelloKind
    {
        None,
        Near,
        Far
    }

    private enum RoleKind
    {
        None,
        GreenIn,
        GreenOut,
        BlueIn,
        BlueOut
    }

    private enum SideKind
    {
        None,
        East,
        West
    }

    private enum WaveKind
    {
        None,
        Right,
        Left
    }

    private enum RazorKind
    {
        None,
        Left,
        Right
    }

    private sealed class PlayerInfo
    {
        public uint EntityId;
        public string Name = string.Empty;
        public TetherKind Tether = TetherKind.None;
        public HelloKind Hello = HelloKind.None;
        public uint PartnerId;
        public string PartnerName = string.Empty;
        public RoleKind Role = RoleKind.None;
        public Vector3? SnapshotPosition;
        public string NearestRocket = string.Empty;
        public string BaseRocketPair = string.Empty;
        public string FixedRocketPair = string.Empty;
        public SideKind Side = SideKind.None;
        public bool Swapped;
        public bool IsShield;
        public bool IsCannon;
        public bool IsStep6Target;
        public string[] GuideElements = [];
    }

    private sealed class ArmInfo
    {
        public uint EntityId;
        public float BeetleAngle;
        public bool IsCw;
    }
    #endregion

    #region LifeCycle
    public override void OnSetup()
    {
        Controller.TryRegisterLayoutFromCode(LayoutStep0, """
            ~Lv2~{"Enabled":false,"Name":"step0","ZoneLockH":[1122],"ElementsL":[{"Name":"Blue1","type":1,"Enabled":false,"offX":-7.5,"offY":6.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"thicc":5.0,"radius":0.5},{"Name":"Blue2","type":1,"Enabled":false,"offX":7.5,"offY":6.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"thicc":5.0,"radius":0.5},{"Name":"Blue3","type":1,"Enabled":false,"offX":-13.0,"offY":10.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"thicc":5.0,"radius":0.5},{"Name":"Blue4","type":1,"Enabled":false,"offX":13.0,"offY":10.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"thicc":5.0,"radius":0.5},{"Name":"Green1","type":1,"Enabled":false,"offX":10.0,"offY":8.0,"fillIntensity":0.5,"refActorDataID":14669,"refActorComparisonType":3,"includeRotation":true,"thicc":5.0,"radius":0.5},{"Name":"Green2","type":1,"Enabled":false,"offX":-10.0,"offY":8.0,"fillIntensity":0.5,"refActorDataID":14669,"refActorComparisonType":3,"includeRotation":true,"thicc":5.0,"radius":0.5},{"Name":"Green3","type":1,"Enabled":false,"offX":11.0,"offY":12.0,"fillIntensity":0.5,"refActorDataID":14669,"refActorComparisonType":3,"includeRotation":true,"thicc":5.0,"radius":0.5},{"Name":"Green4","type":1,"Enabled":false,"offX":-11.0,"offY":12.0,"fillIntensity":0.5,"refActorDataID":14669,"refActorComparisonType":3,"includeRotation":true,"thicc":5.0,"radius":0.5},{"Name":"BlueLine1","type":3,"Enabled":false,"refX":7.5,"refY":6.0,"offX":-7.5,"offY":6.0,"radius":0.0,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true},{"Name":"BlueLine2","type":3,"Enabled":false,"refX":13.0,"refY":10.0,"offX":-13.0,"offY":10.0,"radius":0.0,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true},{"Name":"GreenLine1","type":3,"Enabled":false,"refX":10.0,"refY":8.0,"offX":-10.0,"offY":8.0,"radius":0.0,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":14669,"refActorComparisonType":3,"includeRotation":true},{"Name":"GreenLine2","type":3,"Enabled":false,"refX":11.0,"refY":12.0,"offX":-11.0,"offY":12.0,"radius":0.0,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":14669,"refActorComparisonType":3,"includeRotation":true}]}
            """, out _, overwrite: true);

        Controller.TryRegisterLayoutFromCode(LayoutStep1, """
            ~Lv2~{"Enabled":false,"Name":"step1","ZoneLockH":[1122],"ElementsL":[{"Name":"BlueOutEast","type":1,"Enabled":false,"offX":-5.0,"offY":10.0,"radius":0.5,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true},{"Name":"BlueOutWest","type":1,"Enabled":false,"offX":5.0,"offY":10.0,"radius":0.5,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true},{"Name":"BlueInEast","type":1,"Enabled":false,"offX":-10.0,"offY":10.0,"radius":0.5,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true},{"Name":"BlueInWest","type":1,"offX":10.0,"offY":10.0,"radius":0.5,"color":4280811264,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true},{"Name":"GreenOutEast","type":1,"Enabled":false,"offX":11.0,"offY":10.0,"radius":0.5,"color":4293067007,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":14669,"refActorComparisonType":3,"includeRotation":true,"tether":true},{"Name":"GreenOutWest","type":1,"Enabled":false,"offX":-11.0,"offY":10.0,"radius":0.5,"color":4293394176,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":14669,"refActorComparisonType":3,"includeRotation":true,"tether":true},{"Name":"GreenInEast","type":1,"Enabled":false,"offX":11.0,"offY":10.0,"radius":0.5,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":14669,"refActorComparisonType":3,"includeRotation":true,"tether":true},{"Name":"GreenInWest","type":1,"Enabled":false,"offX":-11.0,"offY":10.0,"radius":0.5,"color":4294921216,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":14669,"refActorComparisonType":3,"includeRotation":true,"tether":true}]}
            """, out _, overwrite: true);

        Controller.TryRegisterLayoutFromCode(LayoutStep2, """
            ~Lv2~{"Enabled":false,"Name":"step2","ZoneLockH":[1122],"ElementsL":[{"Name":"BlueOutEast","type":1,"Enabled":false,"offX":-5.0,"offY":10.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"thicc":5.0,"radius":0.5},{"Name":"BlueOutWest","type":1,"Enabled":false,"offX":5.0,"offY":10.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"thicc":5.0,"radius":0.5},{"Name":"BlueInEast","type":1,"Enabled":false,"fillIntensity":0.5,"refActorObjectID":0,"refActorComparisonType":2,"tether":true,"thicc":5.0,"radius":0.5},{"Name":"BlueInWest","type":1,"Enabled":false,"fillIntensity":0.5,"refActorObjectID":0,"refActorComparisonType":2,"tether":true,"thicc":5.0,"radius":0.5},{"Name":"GreenOutEast","type":1,"Enabled":false,"fillIntensity":0.5,"refActorObjectID":0,"refActorComparisonType":2,"tether":true,"thicc":5.0,"radius":0.5},{"Name":"GreenOutWest","type":1,"Enabled":false,"fillIntensity":0.5,"refActorObjectID":0,"refActorComparisonType":2,"tether":true,"thicc":5.0,"radius":0.5},{"Name":"GreenInEast","type":1,"Enabled":false,"offX":11.0,"offY":10.0,"fillIntensity":0.5,"refActorDataID":14669,"refActorComparisonType":3,"includeRotation":true,"tether":true,"thicc":5.0,"radius":0.5},{"Name":"GreenInWest","type":1,"Enabled":false,"offX":-11.0,"offY":10.0,"fillIntensity":0.5,"refActorDataID":14669,"refActorComparisonType":3,"includeRotation":true,"tether":true,"thicc":5.0,"radius":0.5}]}
            """, out _, overwrite: true);

        Controller.TryRegisterLayoutFromCode(LayoutStep3, """
            ~Lv2~{"Enabled":false,"Name":"step3","ZoneLockH":[1122],"ElementsL":[{"Name":"BaitArm","Enabled":false,"refX":100.0,"refY":100.0,"fillIntensity":0.5,"tether":true,"thicc":5.0,"radius":0.5},{"Name":"BaitShield_E","type":1,"Enabled":false,"offX":-5.0,"offY":20.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"thicc":5.0,"radius":0.5},{"Name":"BaitShield_W","type":1,"Enabled":false,"offX":5.0,"offY":20.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"thicc":5.0,"radius":0.5}]}
            """, out _, overwrite: true);

        Controller.TryRegisterLayoutFromCode(LayoutStep4WaveRight, """
            ~Lv2~{"Enabled":false,"Name":"step4_waveright","ZoneLockH":[1122],"ElementsL":[{"Name":"BaitCannon_NE","type":1,"Enabled":false,"offX":-12.42,"offY":15.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"BaitCannon_NW","type":1,"Enabled":false,"offX":12.42,"offY":15.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"BaitCannon_SE","type":1,"Enabled":false,"offX":-12.4,"offY":25.42,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"BaitCannon_SW","type":1,"Enabled":false,"offX":12.4,"offY":25.4,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"IsShield_and_IsCannon_FinalSide","type":1,"Enabled":false,"offX":5.0,"offY":31.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"IsShiled_and_NotCannon_FinalSide","type":1,"Enabled":false,"offX":1.0,"offY":31.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"NotShield_and_IsCannon","type":1,"Enabled":false,"offX":5.0,"offY":20.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"NotShiled_and_NotCannon","type":1,"Enabled":false,"offX":1.0,"offY":20.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"IsShield_and_IsCannon_BeetleSide","type":1,"Enabled":false,"offX":5.0,"offY":9.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"IsShiled_and_NotCannon_BeetleSide","type":1,"Enabled":false,"offX":1.0,"offY":9.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5}]}
            """, out _, overwrite: true);

        Controller.TryRegisterLayoutFromCode(LayoutStep4WaveLeft, """
            ~Lv2~{"Enabled":false,"Name":"step4_waveleft","ZoneLockH":[1122],"ElementsL":[{"Name":"BaitCannon_NE","type":1,"Enabled":false,"offX":-12.42,"offY":15.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"BaitCannon_NW","type":1,"Enabled":false,"offX":12.42,"offY":15.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"BaitCannon_SE","type":1,"Enabled":false,"offX":-12.4,"offY":25.42,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"BaitCannon_SW","type":1,"Enabled":false,"offX":12.4,"offY":25.4,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"IsShield_and_IsCannon_FinalSide","type":1,"Enabled":false,"offX":-5.0,"offY":31.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"IsShiled_and_NotCannon_FinalSide","type":1,"Enabled":false,"offX":-1.0,"offY":31.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"NotShield_and_IsCannon","type":1,"Enabled":false,"offX":-5.0,"offY":20.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"NotShiled_and_NotCannon","type":1,"Enabled":false,"offX":-1.0,"offY":20.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"IsShield_and_IsCannon_BeetleSide","type":1,"Enabled":false,"offX":-5.0,"offY":9.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5},{"Name":"IsShiled_and_NotCannon_BeetleSide","type":1,"Enabled":false,"offX":-1.0,"offY":9.0,"fillIntensity":0.5,"refActorDataID":15724,"refActorComparisonType":3,"includeRotation":true,"tether":true,"radius":0.5}]}
            """, out _, overwrite: true);

        Controller.TryRegisterLayoutFromCode(LayoutStep5Left, """
            ~Lv2~{"Enabled":false,"Name":"step5_left","ZoneLockH":[1122],"ElementsL":[{"Name":"Blue_NearWorld","type":1,"Enabled":false,"offY":21.02,"radius":0.5,"color":4292346111,"overlayBGColor":4278220288,"overlayTextColor":4294967295,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":6.0213859,"fillIntensity":0.5},{"Name":"Blue_FarWorld","type":1,"Enabled":false,"offY":15.0,"radius":0.5,"color":4288326400,"overlayBGColor":4285363712,"overlayTextColor":4294967295,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":5.1399947,"fillIntensity":0.5},{"Name":"Green_FarTaker1","type":1,"Enabled":false,"offY":1.78,"radius":0.5,"color":4294901875,"overlayBGColor":2617245696,"overlayTextColor":4278255360,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":6.0213859,"fillIntensity":0.5},{"Name":"Green_FarTaker2","type":1,"Enabled":false,"offY":37.1,"radius":0.5,"color":4294901875,"overlayBGColor":2617245696,"overlayTextColor":4278255360,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":6.0213859,"fillIntensity":0.5},{"Name":"Blue_Nothing1","type":1,"Enabled":false,"offY":25.38,"radius":0.5,"color":4278255392,"overlayBGColor":4278236333,"overlayTextColor":4278190080,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":5.6234509,"fillIntensity":0.5},{"Name":"Blue_Nothing2","type":1,"Enabled":false,"offY":25.24,"radius":0.5,"color":4278255392,"overlayBGColor":4278236333,"overlayTextColor":4278190080,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":5.4157567,"fillIntensity":0.5},{"Name":"Green_Nothing1","type":1,"Enabled":false,"offY":34.7,"radius":0.5,"color":4294901888,"overlayBGColor":4294967295,"overlayTextColor":4278190080,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":5.7595865,"fillIntensity":0.5},{"Name":"Green_Nothing2","type":1,"Enabled":false,"offY":34.7,"radius":0.5,"color":4294901888,"overlayBGColor":4294967295,"overlayTextColor":4278190080,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":5.7595865,"fillIntensity":0.5}]}
            """, out _, overwrite: true);

        Controller.TryRegisterLayoutFromCode(LayoutStep5Right, """
            ~Lv2~{"Enabled":false,"Name":"step5_right","ZoneLockH":[1122],"ElementsL":[{"Name":"Blue_NearWorld","type":1,"Enabled":false,"offY":21.02,"radius":0.5,"color":4292346111,"overlayBGColor":4278220288,"overlayTextColor":4294967295,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":0.2617994,"fillIntensity":0.5},{"Name":"Blue_FarWorld","type":1,"Enabled":false,"offY":15.0,"radius":0.5,"color":4288326400,"overlayBGColor":4285363712,"overlayTextColor":4294967295,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":1.1431906,"fillIntensity":0.5},{"Name":"Green_FarTaker1","type":1,"Enabled":false,"offY":1.78,"radius":0.5,"color":4294901875,"overlayBGColor":2617245696,"overlayTextColor":4278255360,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":0.2617994,"fillIntensity":0.5},{"Name":"Green_FarTaker2","type":1,"Enabled":false,"offY":37.1,"radius":0.5,"color":4294901875,"overlayBGColor":2617245696,"overlayTextColor":4278255360,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":0.2617994,"fillIntensity":0.5},{"Name":"Blue_Nothing1","type":1,"Enabled":false,"offY":25.38,"radius":0.5,"color":4278255392,"overlayBGColor":4278236333,"overlayTextColor":4278190080,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":0.6597344,"fillIntensity":0.5},{"Name":"Blue_Nothing2","type":1,"Enabled":false,"offY":25.24,"radius":0.5,"color":4278255392,"overlayBGColor":4278236333,"overlayTextColor":4278190080,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":0.86742866,"fillIntensity":0.5},{"Name":"Green_Nothing1","type":1,"Enabled":false,"offY":34.7,"radius":0.5,"color":4294901888,"overlayBGColor":4294967295,"overlayTextColor":4278190080,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":0.5235988,"fillIntensity":0.5},{"Name":"Green_Nothing2","type":1,"Enabled":false,"offY":34.7,"radius":0.5,"color":4294901888,"overlayBGColor":4294967295,"overlayTextColor":4278190080,"thicc":5.0,"refActorModelID":3771,"refActorComparisonType":1,"includeRotation":true,"tether":true,"AdditionalRotation":0.5235988,"fillIntensity":0.5}]}
            """, out _, overwrite: true);

        Controller.RegisterElementFromCode("RasorLeft",
            "{\"Name\":\"RasorLeft\",\"type\":3,\"refX\":15.0,\"refY\":43.0,\"offX\":15.0,\"offY\":-10.0,\"radius\":15.0,\"color\":1342242815,\"overlayBGColor\":4294967295,\"overlayTextColor\":4278190080,\"thicc\":5.0,\"overlayText\":\"Broken green\",\"refActorModelID\":3771,\"refActorRequireCast\":true,\"refActorCastId\":[31636],\"refActorComparisonType\":1,\"includeRotation\":true,\"AdditionalRotation\":6.021386}",
            overwrite: true);

        Controller.RegisterElementFromCode("RasorRight",
            "{\"Name\":\"RasorRight\",\"type\":3,\"refX\":15.0,\"refY\":10.0,\"offX\":15.0,\"offY\":-43.0,\"radius\":15.0,\"color\":1342242815,\"overlayBGColor\":4294967295,\"overlayTextColor\":4278190080,\"thicc\":5.0,\"overlayText\":\"Broken green\",\"refActorModelID\":3771,\"refActorRequireCast\":true,\"refActorCastId\":[31637],\"refActorComparisonType\":1,\"includeRotation\":true,\"AdditionalRotation\":3.403392}",
            overwrite: true);

        Controller.RegisterElementFromCode(ElHint,
            "{\"Name\":\"Hint\",\"type\":1,\"radius\":0.0,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"overlayVOffset\":2.0,\"overlayFScale\":2.0,\"overlayText\":\"hint\",\"refActorType\":1}",
            overwrite: true);

        Controller.RegisterElementFromCode(ElPartnerTether,
            "{\"Name\":\"PartnerTether\",\"type\":1,\"Enabled\":false,\"fillIntensity\":0.5,\"refActorObjectID\":0,\"refActorComparisonType\":2,\"tether\":true,\"thicc\":5.0,\"radius\":1.0}",
            overwrite: true);

        Controller.TryRegisterLayoutFromCode(LayoutHints, """
            ~Lv2~{"Enabled":false,"Name":"Hints","ZoneLockH":[1122],"ElementsL":[{"Name":"step0_Blue","overlayText":"ちびオメガへ"},{"Name":"step0_Green","overlayText":"でかオメガへ"},{"Name":"step1_BlueOut","overlayText":""},{"Name":"step1_swap_BlueOut","overlayText":"入れ替え"},{"Name":"step1_BlueIn","overlayText":""},{"Name":"step1_swap_BlueIn","overlayText":"入れ替え"},{"Name":"step1_GreenOut","overlayText":""},{"Name":"step1_swap_GreenOut","overlayText":"入れ替え"},{"Name":"step1_GreenIn","overlayText":""},{"Name":"step1_swap_GreenIn","overlayText":"入れ替え"},{"Name":"step2_BlueOut","overlayText":""},{"Name":"step2_BlueIn","overlayText":"パンチをかさねる"},{"Name":"step2_GreenOut","overlayText":""},{"Name":"step2_GreenIn","overlayText":""},{"Name":"step3_BaitArm","overlayText":"アーム誘導"},{"Name":"step3_BaitShield","overlayText":"バッシュ誘導"},{"Name":"step4_BaitCannon","overlayText":"検知誘導"},{"Name":"step4_IsShield_and_IsCannon","overlayText":"離れて検知"},{"Name":"step4_IsShiled_and_NotCannon","overlayText":"離れて"},{"Name":"step4_NotShield_and_IsCannon","overlayText":"検知＆頭割り"},{"Name":"step4_NotShiled_and_NotCannon","overlayText":"頭割り"},{"Name":"step5_Blue_NearWorld","overlayText":"ニアデバフ"},{"Name":"step5_Blue_FarWorld","overlayText":"ファーデバフ"},{"Name":"step5_Green_FarTaker1","overlayText":"時計回りに移動"},{"Name":"step5_Green_FarTaker2","overlayText":"時計回りに移動"},{"Name":"step5_Blue_Nothing1","overlayText":""},{"Name":"step5_Blue_Nothing2","overlayText":""},{"Name":"step5_Green_Nothing1","overlayText":""},{"Name":"step5_Green_Nothing2","overlayText":""},{"Name":"step5_Green_Nothing1_Waiting","overlayText":"線切り待機中"},{"Name":"step5_Green_Nothing1_GetClose","overlayText":"線を切れ"},{"Name":"step5_Green_Nothing2_Waiting","overlayText":"線切り待機中"},{"Name":"step5_Green_Nothing2_GetClose","overlayText":"線を切れ"},{"Name":"step6_Waiting","overlayText":"線切り待機中"},{"Name":"step6_GetClose","overlayText":"線を切れ"}]}
            """, out _, overwrite: true);
    }

    public override void OnUpdate()
    {
        if(_isDelta && Controller.Scene == SceneId)
        {
            TryAssignNearestRockets();
            TryAdvanceFromStep0();
        }

        DisableAllGuides();
        UpdateGuide();
    }

    public override void OnReset()
    {
        _players.Clear();
        ResetDeltaRuntime();
        DisableAllGuides();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(Controller.Scene != SceneId)
            return;

        if(castId == CastDynamisDelta)
        {
            BuildPartySnapshot();
            ResetDeltaRuntime();
            DisableAllGuides();
            return;
        }

        if(!_isDelta)
            return;

        if(castId == CastOverSampledRight)
        {
            _wave = WaveKind.Right;
            return;
        }

        if(castId == CastOverSampledLeft)
        {
            _wave = WaveKind.Left;
            return;
        }

        if(castId == CastRocketPunch && _step == 2)
        {
            SetStep(3);
            return;
        }

        if(castId == CastRazorLeft && _step == 4)
        {
            _razor = RazorKind.Left;
            SetStep(5);
            return;
        }

        if(castId == CastRazorRight && _step == 4)
        {
            _razor = RazorKind.Right;
            SetStep(5);
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(Controller.Scene != SceneId || set.Action == null)
            return;

        var actionId = set.Action.Value.RowId;

        if(actionId == CastDynamisDelta)
        {
            _isDelta = true;
            return;
        }

        if(!_isDelta)
            return;

        if(actionId == CastOpticalLaser)
        {
            AssignSnapshot();
            return;
        }

        if(actionId == CastEnbug)
        {
            if(_step == 1)
            {
                SetStep(2);
                return;
            }

            if(_step == 6)
                EndDelta();
            return;
        }

        if(actionId == CastShieldCombo && _step == 3)
        {
            if(set.Target is IPlayerCharacter shieldTarget
                && _players.TryGetValue(shieldTarget.EntityId, out var shieldInfo))
            {
                shieldInfo.IsShield = true;
            }

            SetStep(4);
            return;
        }

        if(actionId is CastHelloNear or CastHelloFar)
        {
            if(_step == 5)
                EnterStep6();
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(!_isDelta)
            return;

        if(data2 != TetherParam2 || data5 != TetherParam5)
            return;

        if(data3 != TetherParamGreen && data3 != TetherParamBlue)
            return;

        if(!_players.TryGetValue(source, out var sourceInfo) || !_players.TryGetValue(target, out var targetInfo))
            return;

        sourceInfo.PartnerId = target;
        sourceInfo.PartnerName = targetInfo.Name;
        targetInfo.PartnerId = source;
        targetInfo.PartnerName = sourceInfo.Name;
    }

    public override void OnGainBuffEffect(uint sourceId, Status Status)
    {
        if(!_isDelta)
            return;

        if(!_players.TryGetValue(sourceId, out var info))
            return;

        switch(Status.StatusId)
        {
            case StatusTetherGreen:
                info.Tether = TetherKind.Green;
                break;
            case StatusTetherBlue:
                info.Tether = TetherKind.Blue;
                break;
            case StatusHelloNear:
                info.Hello = HelloKind.Near;
                break;
            case StatusHelloFar:
                info.Hello = HelloKind.Far;
                break;
            case StatusCannonLeft:
            case StatusCannonRight:
                info.IsCannon = true;
                if(_step >= 4)
                    RefreshGuideElements();
                break;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(!_isDelta)
            return;

        var isCw = vfxPath.Equals(VfxTurningCw, StringComparison.OrdinalIgnoreCase) ? true
            : vfxPath.Equals(VfxTurningCcw, StringComparison.OrdinalIgnoreCase) ? false
            : (bool?)null;
        if(isCw == null)
            return;

        var obj = target.GetObject();
        if(obj == null)
            return;

        var beetle = Svc.Objects.FirstOrDefault(x => x.DataId == DataIdBeetle);
        if(beetle == null)
            return;

        _beetleWorldAngle = GetWorldAngle(beetle.Position);
        _armsById[target] = new ArmInfo
        {
            EntityId = target,
            BeetleAngle = NormalizeAngle(GetWorldAngle(obj.Position) - _beetleWorldAngle),
            IsCw = isCw.Value
        };

        if(_armsById.Count < ArmCount)
            return;

        _armsSorted = _armsById.Values.OrderBy(x => x.BeetleAngle).ToList();
        _armsReady = true;
    }

    public override void OnSettingsDraw()
    {
        if(!ImGui.BeginTabBar("##P5DeltaGuideSettings"))
            return;

        if(ImGui.BeginTabItem("Main###tabMain"))
        {
            DrawMainSettings();
            ImGui.EndTabItem();
        }

        if(ImGui.BeginTabItem("Debug###tabDebug"))
        {
            DrawDebugSettings();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    // Draws configuration options.
    private void DrawMainSettings()
    {
        ImGui.Text("Far World Taker");
        ImGui.Indent();
        if(ImGui.RadioButton("Green Inside##FarTaker", C.Green_FarTaker == FarTakerMode.GreenInside))
            C.Green_FarTaker = FarTakerMode.GreenInside;
        ImGui.SameLine();
        if(ImGui.RadioButton("Green Outside##FarTaker", C.Green_FarTaker == FarTakerMode.GreenOutside))
            C.Green_FarTaker = FarTakerMode.GreenOutside;
        ImGui.Unindent();

        ImGui.Text("Rocket Swap Side");
        ImGui.Indent();
        if(ImGui.RadioButton("Blue Inside##BlueSwap", C.BlueRocketSwapSide == RocketSwapSideMode.Inside))
            C.BlueRocketSwapSide = RocketSwapSideMode.Inside;
        ImGui.SameLine();
        if(ImGui.RadioButton("Blue Outside##BlueSwap", C.BlueRocketSwapSide == RocketSwapSideMode.Outside))
            C.BlueRocketSwapSide = RocketSwapSideMode.Outside;

        if(ImGui.RadioButton("Green Inside##GreenSwap", C.GreenRocketSwapSide == RocketSwapSideMode.Inside))
            C.GreenRocketSwapSide = RocketSwapSideMode.Inside;
        ImGui.SameLine();
        if(ImGui.RadioButton("Green Outside##GreenSwap", C.GreenRocketSwapSide == RocketSwapSideMode.Outside))
            C.GreenRocketSwapSide = RocketSwapSideMode.Outside;
        ImGui.Unindent();

        ImGui.Text("Bashed Player Side");
        ImGui.Indent();
        if(ImGui.RadioButton("Final Omega", C.BashedPlayerSide == BashedPlayerSideMode.FinalOmega))
            C.BashedPlayerSide = BashedPlayerSideMode.FinalOmega;
        ImGui.SameLine();
        if(ImGui.RadioButton("Beetle Omega", C.BashedPlayerSide == BashedPlayerSideMode.BeetleOmega))
            C.BashedPlayerSide = BashedPlayerSideMode.BeetleOmega;
        ImGui.Unindent();
    }

    // Draws runtime state and PlayerInfo table.
    private void DrawDebugSettings()
    {
        ImGui.Text($"IsDelta: {_isDelta}");
        ImGui.Text($"Step: {_step}");
        ImGui.Text($"Wave: {_wave}");
        ImGui.Text($"Razor: {_razor}");
        ImGui.Text($"BasePlayer: {BasePlayer?.Name.ToString() ?? "null"}");
        ImGui.Text($"ArmsReady: {_armsReady} ({_armsById.Count}/{ArmCount})");
        ImGui.Text($"BeetleWorldAngle: {_beetleWorldAngle:F1}");
        if(_armsSorted.Count > 0)
        {
            for(var i = 0; i < _armsSorted.Count; i++)
            {
                var arm = _armsSorted[i];
                ImGui.Text($"Arm[{i}]: Angle={arm.BeetleAngle:F1} {(arm.IsCw ? "CW" : "CCW")} Id={arm.EntityId}");
            }
        }
        ImGui.Separator();
        ImGui.Text("PlayerInfo");

        if(_players.Count == 0)
        {
            ImGui.Text("No PlayerInfo");
            return;
        }

        List<ImGuiEx.EzTableEntry> entries = [];
        foreach(var player in _players.Values.OrderBy(x => x.EntityId))
        {
            var row = player;
            entries.Add(new ImGuiEx.EzTableEntry("EntityId", () => ImGui.Text(row.EntityId.ToString())));
            entries.Add(new ImGuiEx.EzTableEntry("Name", () => ImGui.Text(row.Name)));
            entries.Add(new ImGuiEx.EzTableEntry("Tether", () => ImGui.Text(row.Tether.ToString())));
            entries.Add(new ImGuiEx.EzTableEntry("Hello", () => ImGui.Text(row.Hello.ToString())));
            entries.Add(new ImGuiEx.EzTableEntry("Partner", () => ImGui.Text(row.PartnerName)));
            entries.Add(new ImGuiEx.EzTableEntry("Role", () => ImGui.Text(row.Role.ToString())));
            entries.Add(new ImGuiEx.EzTableEntry("NearestRocket", () => ImGui.Text(row.NearestRocket)));
            entries.Add(new ImGuiEx.EzTableEntry("BaseRocketPair", () => ImGui.Text(row.BaseRocketPair)));
            entries.Add(new ImGuiEx.EzTableEntry("FixedRocketPair", () => ImGui.Text(row.FixedRocketPair)));
            entries.Add(new ImGuiEx.EzTableEntry("Side", () => ImGui.Text(row.Side.ToString())));
            entries.Add(new ImGuiEx.EzTableEntry("Swapped", () => ImGui.Text(row.Swapped.ToString())));
            entries.Add(new ImGuiEx.EzTableEntry("IsShield", () => ImGui.Text(row.IsShield.ToString())));
            entries.Add(new ImGuiEx.EzTableEntry("IsCannon", () => ImGui.Text(row.IsCannon.ToString())));
            entries.Add(new ImGuiEx.EzTableEntry("IsStep6Target", () => ImGui.Text(row.IsStep6Target.ToString())));
            entries.Add(new ImGuiEx.EzTableEntry("Guide", () => ImGui.Text(string.Join(", ", row.GuideElements))));
        }

        ImGuiEx.EzTable(entries);
    }
    #endregion

    #region Private Method
    // Clears runtime flags without touching PlayerInfo rows.
    private void ResetDeltaRuntime()
    {
        _isDelta = false;
        _rocketsAssigned = false;
        _step = 0;
        _wave = WaveKind.None;
        _razor = RazorKind.None;
        _armsById.Clear();
        _armsSorted = [];
        _armsReady = false;
        _beetleWorldAngle = 0f;
    }

    // Ends the mechanic and turns all guides off.
    private void EndDelta()
    {
        _isDelta = false;
        DisableAllGuides();
    }

    // Disables every step0-5 guide layout and its elements.
    private void DisableAllGuides()
    {
        foreach(var name in AllGuideLayoutNames)
        {
            if(!Controller.TryGetLayoutByName(name, out var layout))
                continue;

            layout.Enabled = false;
            foreach(var element in layout.ElementsL)
                element.Enabled = false;
        }

        DisableHint();
        DisablePartnerTether();
    }

    // Builds eight-player rows keyed by EntityId.
    private void BuildPartySnapshot()
    {
        _players.Clear();
        foreach(var pc in Controller.GetPartyMembers().OfType<IPlayerCharacter>())
        {
            _players[pc.EntityId] = new PlayerInfo
            {
                EntityId = pc.EntityId,
                Name = pc.Name.ToString()
            };
        }
    }

    // Snapshots Role / nearest rocket / nearest player / Side once at Optical Laser.
    private void AssignSnapshot()
    {
        foreach(var player in _players.Values)
        {
            player.Role = RoleKind.None;
            player.SnapshotPosition = null;
            player.NearestRocket = string.Empty;
            player.BaseRocketPair = string.Empty;
            player.FixedRocketPair = string.Empty;
            player.Side = SideKind.None;
            player.Swapped = false;
            player.IsShield = false;
            player.IsCannon = false;
            player.IsStep6Target = false;
            player.GuideElements = [];
        }

        _rocketsAssigned = false;
        _step = 0;

        var party = Controller.GetPartyMembers().OfType<IPlayerCharacter>().ToList();
        foreach(var info in _players.Values)
        {
            var pc = party.FirstOrDefault(p => p.EntityId == info.EntityId);
            if(pc != null)
                info.SnapshotPosition = pc.Position;
        }

        // Role / Side first; BaseRocketPair; rockets then may swap In sides and set FixedRocketPair.
        AssignRolesByPairMidpoint(party);
        AssignSides(party);
        AssignSameSideRocketPairs(fixedAfterSwap: false);
        TryAssignNearestRockets();
    }

    // Assigns GreenIn/Out and BlueIn/Out from pair midpoint distance to arena center.
    private void AssignRolesByPairMidpoint(List<IPlayerCharacter> party)
    {
        var visited = new HashSet<uint>();
        var greenPairs = new List<(PlayerInfo A, PlayerInfo B, float Dist)>();
        var bluePairs = new List<(PlayerInfo A, PlayerInfo B, float Dist)>();

        foreach(var info in _players.Values)
        {
            if(info.PartnerId == 0 || !visited.Add(info.EntityId))
                continue;

            if(!_players.TryGetValue(info.PartnerId, out var partner))
                continue;

            visited.Add(partner.EntityId);

            var pcA = party.FirstOrDefault(p => p.EntityId == info.EntityId);
            var pcB = party.FirstOrDefault(p => p.EntityId == partner.EntityId);
            if(pcA == null || pcB == null)
                continue;

            var mid = (pcA.Position + pcB.Position) / 2f;
            var dist = Vector3.Distance(mid, ArenaCenter);
            var color = info.Tether != TetherKind.None ? info.Tether : partner.Tether;

            if(color == TetherKind.Green)
                greenPairs.Add((info, partner, dist));
            else if(color == TetherKind.Blue)
                bluePairs.Add((info, partner, dist));
        }

        ApplyInOutRoles(greenPairs, RoleKind.GreenIn, RoleKind.GreenOut);
        ApplyInOutRoles(bluePairs, RoleKind.BlueIn, RoleKind.BlueOut);
    }

    // Marks closer pair In and farther pair Out for one tether color.
    private static void ApplyInOutRoles(List<(PlayerInfo A, PlayerInfo B, float Dist)> pairs, RoleKind inRole, RoleKind outRole)
    {
        if(pairs.Count == 0)
            return;

        var ordered = pairs.OrderBy(x => x.Dist).ToList();
        ordered[0].A.Role = inRole;
        ordered[0].B.Role = inRole;

        if(ordered.Count < 2)
            return;

        ordered[^1].A.Role = outRole;
        ordered[^1].B.Role = outRole;
    }

    // Assigns rockets once when all 8 exist: each rocket claims nearest SnapshotPosition (unique match).
    private void TryAssignNearestRockets()
    {
        if(_rocketsAssigned || _players.Count == 0)
            return;

        if(_players.Values.All(x => x.SnapshotPosition == null))
            return;

        var rockets = Svc.Objects
            .Where(x => x.DataId == DataIdRocketBlue || x.DataId == DataIdRocketYellow)
            .ToList();

        if(rockets.Count < RocketCount)
            return;

        var snapshots = _players.Values
            .Where(x => x.SnapshotPosition != null)
            .ToList();

        var pairs = new List<(float Dist, uint RocketId, PlayerInfo Info, string Label)>();
        foreach(var rocket in rockets)
        {
            var label = rocket.DataId == DataIdRocketBlue ? "RocketBlue" : "RocketYellow";
            foreach(var info in snapshots)
            {
                pairs.Add((
                    Vector3.Distance(rocket.Position, info.SnapshotPosition!.Value),
                    rocket.EntityId,
                    info,
                    label));
            }
        }

        foreach(var info in snapshots)
            info.NearestRocket = string.Empty;

        var usedRockets = new HashSet<uint>();
        var usedPlayers = new HashSet<uint>();
        foreach(var pair in pairs.OrderBy(x => x.Dist))
        {
            if(usedRockets.Contains(pair.RocketId) || usedPlayers.Contains(pair.Info.EntityId))
                continue;

            usedRockets.Add(pair.RocketId);
            usedPlayers.Add(pair.Info.EntityId);
            pair.Info.NearestRocket = pair.Label;
            if(usedPlayers.Count >= snapshots.Count)
                break;
        }

        _rocketsAssigned = true;
        ResolveInSideSwap();
        AssignSameSideRocketPairs(fixedAfterSwap: true);
        TryAdvanceFromStep0();
    }

    // Advances to step1 once all 8 rockets are assigned.
    private void TryAdvanceFromStep0()
    {
        if(_step != 0 || !_rocketsAssigned || _players.Count == 0)
            return;

        SetStep(1);
    }

    // Sets step and refreshes guide element names.
    private void SetStep(int step)
    {
        _step = step;
        RefreshGuideElements();
    }

    // Fills GuideElements for every player from the current step.
    private void RefreshGuideElements()
    {
        foreach(var info in _players.Values)
            info.GuideElements = ResolveGuideElements(info);
    }

    // Maps PlayerInfo to step-specific layout element names.
    private string[] ResolveGuideElements(PlayerInfo info)
    {
        if(_step == 0)
            return ResolveStep0Elements(info);

        if(_step is 1 or 2)
        {
            if(info.Role == RoleKind.None || info.Side == SideKind.None)
                return [];
            return [$"{info.Role}{info.Side}"];
        }

        if(_step == 3)
            return WrapGuideName(ResolveStep3Role(info));

        if(_step == 4)
            return WrapGuideName(ResolveStep4Role(info));

        if(_step == 5)
            return ResolveStep5Elements(info);

        return [];
    }

    // Wraps a single element name, or empty when unresolved.
    private static string[] WrapGuideName(string name)
        => name.Length == 0 ? [] : [name];

    // Maps tether color to Blue/Green spots plus connecting lines; none when unresolved.
    private static string[] ResolveStep0Elements(PlayerInfo info)
        => info.Tether switch
        {
            TetherKind.Blue => ["Blue1", "Blue2", "Blue3", "Blue4", "BlueLine1", "BlueLine2"],
            TetherKind.Green => ["Green1", "Green2", "Green3", "Green4", "GreenLine1", "GreenLine2"],
            _ => []
        };

    // Maps Role+Side to BaitArm (shared) / BaitShield_E|W layout labels.
    private static string ResolveStep3Role(PlayerInfo info)
    {
        if(info.Side == SideKind.None)
            return string.Empty;

        if(info.Role is RoleKind.BlueOut or RoleKind.GreenIn or RoleKind.GreenOut)
            return "BaitArm";

        return info.Role switch
        {
            RoleKind.BlueIn when info.Side == SideKind.East => "BaitShield_E",
            RoleKind.BlueIn when info.Side == SideKind.West => "BaitShield_W",
            _ => string.Empty
        };
    }

    // Maps Role+Side to sorted arm list index; -1 when not a BaitArm role.
    private static int ResolveStep3ArmIndex(PlayerInfo info)
        => (info.Role, info.Side) switch
        {
            (RoleKind.BlueOut, SideKind.East) => 0,
            (RoleKind.GreenIn, SideKind.East) => 1,
            (RoleKind.GreenOut, SideKind.East) => 2,
            (RoleKind.GreenOut, SideKind.West) => 3,
            (RoleKind.GreenIn, SideKind.West) => 4,
            (RoleKind.BlueOut, SideKind.West) => 5,
            _ => -1
        };

    // Maps Green Role+Side to BaitCannon, Blue to IsShield/IsCannon labels.
    private string ResolveStep4Role(PlayerInfo info)
    {
        if(info.Tether == TetherKind.Green)
        {
            return (info.Role, info.Side) switch
            {
                (RoleKind.GreenIn, SideKind.East) => "BaitCannon_NE",
                (RoleKind.GreenIn, SideKind.West) => "BaitCannon_NW",
                (RoleKind.GreenOut, SideKind.East) => "BaitCannon_SE",
                (RoleKind.GreenOut, SideKind.West) => "BaitCannon_SW",
                _ => string.Empty
            };
        }

        if(info.Tether != TetherKind.Blue)
            return string.Empty;

        if(!info.IsShield)
        {
            return info.IsCannon
                ? "NotShield_and_IsCannon"
                : "NotShiled_and_NotCannon";
        }

        var sideSuffix = C.BashedPlayerSide == BashedPlayerSideMode.BeetleOmega
            ? "_BeetleSide"
            : "_FinalSide";

        return info.IsCannon
            ? $"IsShield_and_IsCannon{sideSuffix}"
            : $"IsShiled_and_NotCannon{sideSuffix}";
    }

    // Maps Hello / Green_FarTaker config to step5 element names; Green_Nothing waits/cuts skip layout.
    private string[] ResolveStep5Elements(PlayerInfo info)
    {
        if(info.Tether == TetherKind.Blue)
        {
            if(info.Hello == HelloKind.Near)
                return ["Blue_NearWorld"];
            if(info.Hello == HelloKind.Far)
                return ["Blue_FarWorld"];
            return ["Blue_Nothing1", "Blue_Nothing2"];
        }

        if(info.Tether != TetherKind.Green)
            return [];

        if(IsGreenFarTaker(info))
            return ["Green_FarTaker1", "Green_FarTaker2"];

        if(!IsGreenNothing(info) || IsCutWaitPhase() || PlayerHasStatus(info.EntityId, StatusEnbugNear))
            return [];

        return ["Green_Nothing1", "Green_Nothing2"];
    }

    // True when Green and assigned FarTaker by config.
    private bool IsGreenFarTaker(PlayerInfo info)
    {
        if(info.Tether != TetherKind.Green)
            return false;

        var farTakerRole = C.Green_FarTaker == FarTakerMode.GreenOutside
            ? RoleKind.GreenOut
            : RoleKind.GreenIn;
        return info.Role == farTakerRole;
    }

    // True when Green In/Out and not FarTaker.
    private bool IsGreenNothing(PlayerInfo info)
        => info.Tether == TetherKind.Green
            && info.Role is RoleKind.GreenIn or RoleKind.GreenOut
            && !IsGreenFarTaker(info);

    // Marks Enbug Near holders and advances to step6 on Hello Near/Far Effected.
    private void EnterStep6()
    {
        foreach(var info in _players.Values)
            info.IsStep6Target = PlayerHasStatus(info.EntityId, StatusEnbugNear);

        SetStep(6);
    }

    // Swaps configured Inside/Outside tether partners East/West when same-side In/Out share a rocket.
    private void ResolveInSideSwap()
    {
        var greenRole = C.GreenRocketSwapSide == RocketSwapSideMode.Outside
            ? RoleKind.GreenOut
            : RoleKind.GreenIn;
        var blueRole = C.BlueRocketSwapSide == RocketSwapSideMode.Outside
            ? RoleKind.BlueOut
            : RoleKind.BlueIn;

        TrySwapSidesForColor(TetherKind.Green, greenRole);
        TrySwapSidesForColor(TetherKind.Blue, blueRole);
    }

    // Exchanges Side between the given role pair when any same-side In/Out pair shares NearestRocket.
    private void TrySwapSidesForColor(TetherKind color, RoleKind swapRole)
    {
        var members = _players.Values.Where(x => x.Tether == color).ToList();
        if(members.Count == 0)
            return;

        var shouldSwap = members.Any(info =>
        {
            if(info.NearestRocket.Length == 0 || info.Side == SideKind.None)
                return false;

            var counterpart = members.FirstOrDefault(x =>
                x.EntityId != info.EntityId
                && x.Side == info.Side
                && x.NearestRocket.Length > 0);
            return counterpart != null && counterpart.NearestRocket == info.NearestRocket;
        });

        if(!shouldSwap)
            return;

        var swapPlayers = members.Where(x => x.Role == swapRole).ToList();
        if(swapPlayers.Count != 2)
            return;

        // Same-role partners: exchange East/West.
        (swapPlayers[0].Side, swapPlayers[1].Side) = (swapPlayers[1].Side, swapPlayers[0].Side);
        swapPlayers[0].Swapped = true;
        swapPlayers[1].Swapped = true;
    }

    // Sets BaseRocketPair or FixedRocketPair to the other same-tether same-side player (In↔Out).
    private void AssignSameSideRocketPairs(bool fixedAfterSwap)
    {
        foreach(var info in _players.Values)
        {
            var name = string.Empty;
            if(info.Tether != TetherKind.None && info.Side != SideKind.None)
            {
                var counterpart = _players.Values.FirstOrDefault(x =>
                    x.EntityId != info.EntityId
                    && x.Tether == info.Tether
                    && x.Side == info.Side);
                if(counterpart != null)
                    name = counterpart.Name;
            }

            if(fixedAfterSwap)
                info.FixedRocketPair = name;
            else
                info.BaseRocketPair = name;
        }
    }

    // Stores East/West from Final/Beetle looking toward arena center.
    private void AssignSides(List<IPlayerCharacter> party)
    {
        var final = Svc.Objects.FirstOrDefault(x => x.DataId == DataIdFinal);
        var beetle = Svc.Objects.FirstOrDefault(x => x.DataId == DataIdBeetle);

        foreach(var info in _players.Values)
        {
            var position = info.SnapshotPosition
                ?? party.FirstOrDefault(p => p.EntityId == info.EntityId)?.Position;
            if(position == null)
                continue;

            var boss = info.Tether switch
            {
                TetherKind.Green => final,
                TetherKind.Blue => beetle,
                _ => null
            };
            if(boss == null)
                continue;

            var isLeft = IsLeftLookingAtCenter(boss.Position, position.Value);
            // Beetle=north: Blue Left→East Right→West; Final: Green Left→West Right→East.
            info.Side = info.Tether switch
            {
                TetherKind.Blue => isLeft ? SideKind.East : SideKind.West,
                TetherKind.Green => isLeft ? SideKind.West : SideKind.East,
                _ => SideKind.None
            };
        }
    }

    // True when player is on the left of boss looking at arena center.
    private static bool IsLeftLookingAtCenter(Vector3 boss, Vector3 player)
    {
        var toCenter = new Vector2(ArenaCenter.X - boss.X, ArenaCenter.Z - boss.Z);
        var toPlayer = new Vector2(player.X - boss.X, player.Z - boss.Z);
        return toCenter.X * toPlayer.Y - toCenter.Y * toPlayer.X < 0f;
    }

    // Resolves guide layout name from current step, wave, and razor side.
    private string? GetGuideLayoutName()
        => _step switch
        {
            0 => LayoutStep0,
            1 => LayoutStep1,
            2 => LayoutStep2,
            3 => LayoutStep3,
            4 when _wave == WaveKind.Right => LayoutStep4WaveRight,
            4 when _wave == WaveKind.Left => LayoutStep4WaveLeft,
            5 when _razor == RazorKind.Left => LayoutStep5Left,
            5 when _razor == RazorKind.Right => LayoutStep5Right,
            _ => null
        };

    // Enables the given element names on a layout and tints with Attention Color.
    private void EnableLayoutElements(Layout layout, params string[] names)
    {
        foreach(var name in names)
        {
            var element = layout.ElementsL.FirstOrDefault(x => x.Name == name);
            if(element == null)
                continue;

            element.Enabled = true;
            element.color = Controller.AttentionColor;
        }
    }

    // Enables the current step layout and BasePlayer GuideElements.
    private void UpdateGuide()
    {
        if(!_isDelta || Controller.Scene != SceneId || BasePlayer == null)
            return;

        if(!_players.TryGetValue(BasePlayer.EntityId, out var me))
            return;

        // Keep tether (step0) / BashedPlayerSide (step4) / Green cut phases (step5-6) reflected.
        if(_step is 0 or 4 or 5 or 6)
            RefreshGuideElements();

        var layoutName = GetGuideLayoutName();
        if(layoutName != null && Controller.TryGetLayoutByName(layoutName, out var layout))
        {
            layout.Enabled = true;
            var baitArmHandled = _step == 3 && TryEnableBaitArmNavi(layout, me);
            if(!baitArmHandled && me.GuideElements.Length > 0)
            {
                EnableLayoutElements(layout, me.GuideElements);
                BindFixedRocketPairTether(layout, me);
            }
        }

        if(NeedsPartnerTether(me))
            BindPartnerTether(me.PartnerId);

        UpdateHint(me);
    }

    // Places step3 BaitArm at VFX-derived spot; true when player is a BaitArm role.
    private bool TryEnableBaitArmNavi(Layout layout, PlayerInfo me)
    {
        var index = ResolveStep3ArmIndex(me);
        if(index < 0)
            return false;

        if(!_armsReady || index >= _armsSorted.Count)
            return true;

        var arm = _armsSorted[index];
        var naviAngle = NormalizeAngle(arm.BeetleAngle + (arm.IsCw ? -BaitArmAngleOffset : BaitArmAngleOffset));
        var worldAngle = NormalizeAngle(_beetleWorldAngle + naviAngle);
        var pos = CalculatePointCircle(ArenaCenter, BaitArmRadius, worldAngle);

        var element = layout.ElementsL.FirstOrDefault(x => x.Name == ElBaitArm);
        if(element == null)
            return true;

        element.SetRefPosition(pos);
        element.color = Controller.AttentionColor;
        element.Enabled = true;
        return true;
    }

    // Compass degrees from arena center (0 = -Z), matching CalculatePointCircle.
    private static float GetWorldAngle(Vector3 position)
    {
        var dx = position.X - ArenaCenter.X;
        var dz = position.Z - ArenaCenter.Z;
        return NormalizeAngle(MathF.Atan2(dx, -dz).RadToDeg());
    }

    // Wraps angle to [0, 360).
    private static float NormalizeAngle(float degree)
        => (degree % 360f + 360f) % 360f;

    // World XZ from center, radius, and compass angle in degrees (0 = -Z).
    private static Vector3 CalculatePointCircle(Vector3 center, float radius, float degree)
    {
        var radian = degree.DegToRad();
        var sin = MathF.Sin(radian);
        var cos = MathF.Cos(radian);
        return new Vector3(
            center.X + sin * radius,
            center.Y,
            center.Z - cos * radius
        );
    }

    // Binds step1-2 GreenOut / step2 BlueIn guide tether to FixedRocketPair player ObjectId.
    private void BindFixedRocketPairTether(Layout layout, PlayerInfo me)
    {
        var needsPlayerTether = (me.Role == RoleKind.GreenOut && _step is 1 or 2)
            || (_step == 2 && me.Role == RoleKind.BlueIn);
        if(!needsPlayerTether || me.FixedRocketPair.Length == 0)
            return;

        var target = _players.Values.FirstOrDefault(x => x.Name == me.FixedRocketPair);
        if(target == null)
            return;

        foreach(var name in me.GuideElements)
        {
            var element = layout.ElementsL.FirstOrDefault(x => x.Name == name);
            if(element == null)
                continue;
            element.refActorObjectID = target.EntityId;
        }
    }

    // True when Green_Nothing / step6 target should tether to Partner during enbug cut.
    private bool NeedsPartnerTether(PlayerInfo me)
    {
        if(me.PartnerId == 0)
            return false;

        var isCutActor = (_step == 5 && IsGreenNothing(me))
            || (_step == 6 && me.IsStep6Target);
        if(!isCutActor || IsCutWaitPhase())
            return false;

        return PlayerHasStatus(me.EntityId, StatusEnbugNear);
    }

    // Binds PartnerTether to the given ObjectId with Attention Color.
    private void BindPartnerTether(uint partnerId)
    {
        if(!Controller.TryGetElementByName(ElPartnerTether, out var element))
            return;

        element.Enabled = true;
        element.refActorObjectID = partnerId;
        element.color = Controller.AttentionColor;
    }

    // Turns PartnerTether off.
    private void DisablePartnerTether()
    {
        if(Controller.TryGetElementByName(ElPartnerTether, out var element))
            element.Enabled = false;
    }

    // True while doom mark / magic vuln is still on anyone (cut wait).
    private bool IsCutWaitPhase()
        => AnyPartyHasStatus(StatusDoomMark2, StatusMagicVulnUp);

    // True when any party member has one of the given statuses.
    private bool AnyPartyHasStatus(params ushort[] statusIds)
    {
        foreach(var pc in Controller.GetPartyMembers().OfType<IPlayerCharacter>())
        {
            foreach(var statusId in statusIds)
            {
                if(pc.StatusList.Any(s => s != null && s.StatusId == statusId))
                    return true;
            }
        }

        return false;
    }

    // True when the player EntityId currently has the status.
    private bool PlayerHasStatus(uint entityId, ushort statusId)
    {
        var pc = Controller.GetPartyMembers().OfType<IPlayerCharacter>()
            .FirstOrDefault(x => x.EntityId == entityId);
        return pc != null && pc.StatusList.Any(s => s != null && s.StatusId == statusId);
    }

    // Resolves phase-priority Hints layout key for cut wait / cut now.
    private string? ResolvePhaseHintKey(PlayerInfo info)
    {
        string? waitingKey;
        string? cutKey;
        if(_step == 5 && IsGreenNothing(info))
        {
            waitingKey = "step5_Green_Nothing1_Waiting";
            cutKey = "step5_Green_Nothing1_GetClose";
        }
        else if(_step == 6 && info.IsStep6Target)
        {
            waitingKey = "step6_Waiting";
            cutKey = "step6_GetClose";
        }
        else
        {
            return null;
        }

        if(IsCutWaitPhase())
            return waitingKey;
        if(PlayerHasStatus(info.EntityId, StatusEnbugNear))
            return cutKey;
        return null;
    }

    // Resolves Hints layout element name from step, GuideElements, and swap state.
    private string? ResolveHintKey(PlayerInfo info)
    {
        if(info.GuideElements.Length == 0)
            return null;

        var name = info.GuideElements[0];

        if(_step == 0)
        {
            if(name.StartsWith("Blue", StringComparison.Ordinal))
                return "step0_Blue";
            if(name.StartsWith("Green", StringComparison.Ordinal))
                return "step0_Green";
            return null;
        }

        if(_step is 1 or 2)
        {
            var role = StripEastWest(name);
            if(_step == 1 && info.Swapped)
                return $"step1_swap_{role}";
            return $"step{_step}_{role}";
        }

        if(_step == 3)
        {
            if(name.StartsWith("BaitArm", StringComparison.Ordinal))
                return "step3_BaitArm";
            if(name.StartsWith("BaitShield", StringComparison.Ordinal))
                return "step3_BaitShield";
            return null;
        }

        if(_step == 4)
        {
            if(name.StartsWith("BaitCannon", StringComparison.Ordinal))
                return "step4_BaitCannon";
            return $"step4_{StripFinalBeetleSide(name)}";
        }

        return $"step{_step}_{name}";
    }

    // Removes trailing East/West from role guide names.
    private static string StripEastWest(string name)
    {
        if(name.EndsWith("East", StringComparison.Ordinal))
            return name[..^4];
        if(name.EndsWith("West", StringComparison.Ordinal))
            return name[..^4];
        return name;
    }

    // Removes _FinalSide / _BeetleSide from step4 shield labels.
    private static string StripFinalBeetleSide(string name)
    {
        if(name.EndsWith("_FinalSide", StringComparison.Ordinal))
            return name[..^"_FinalSide".Length];
        if(name.EndsWith("_BeetleSide", StringComparison.Ordinal))
            return name[..^"_BeetleSide".Length];
        return name;
    }

    // Copies overlayText from Hints layout onto the display Hint element.
    private void UpdateHint(PlayerInfo me)
    {
        var key = ResolvePhaseHintKey(me) ?? ResolveHintKey(me);
        if(key == null)
        {
            DisableHint();
            return;
        }

        if(!Controller.TryGetLayoutByName(LayoutHints, out var hintLayout))
        {
            DisableHint();
            return;
        }

        var source = hintLayout.ElementsL.FirstOrDefault(x => x.Name == key);
        if(source == null
            || string.IsNullOrEmpty(source.overlayText)
            || !Controller.TryGetElementByName(ElHint, out var hint))
        {
            DisableHint();
            return;
        }

        hint.Enabled = true;
        hint.overlayText = source.overlayText;
    }

    // Turns the display Hint element off.
    private void DisableHint()
    {
        if(Controller.TryGetElementByName(ElHint, out var hint))
            hint.Enabled = false;
    }
    #endregion
}
