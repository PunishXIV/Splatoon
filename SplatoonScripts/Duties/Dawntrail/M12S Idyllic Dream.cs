using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers.LegacyPlayer;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M12S_Idyllic_Dream : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1327];

    public override Metadata? Metadata => new(3, "Errer");

    #region 常量 (Constants)

    // Boss和分身ID (Boss and Clone IDs)
    private const uint BossDataId = 19202;      // 主Boss (Main Boss)
    private const uint CloneDataId = 19204;     // 分身 (存储技能) (Clone - stores skills)
    private const uint FirstSpawnId = 19210;    // 先刷新的4个 (First 4 to spawn)

    // 技能ID (Skill IDs)
    private const uint MirrorCastId = 46345;    // 镜中奇梦 - 四运开始 (Mirror Dreams - Four Movements Start)
    private const uint SelfCopyCastId = 46296;  // 自我复制 (Self Copy)
    private const uint ExecuteCastId = 48089;   // Boss执行 - 触发绘制 (Boss Execute - Triggers Drawing)
    private const uint ProjectionCastId = 48098; // 心象投影 - 第4次触发引导 (Mental Projection - 4th triggers guidance)
    private const uint TimeWarpCastId = 46316;  // 时空重现 - 触发第6次大圈绘制 (Time Warp - Triggers 6th circle drawing)

    // 分身技能 (Clone Skills)
    private const uint CloneLeftRight = 46351;  // 面相左右双刀 (Left/Right Cleave)
    private const uint CloneCircle = 46353;     // 脚下圆形AOE (Circle AOE at feet)

    private const uint CloneCircle2 = 48303;     // 脚下圆形AOE (Circle AOE at feet)
    private const uint CloneFrontBack = 46352;  // 面相前后双刀 (Front/Back Cleave)

    // VFX
    private const string VfxShare = "vfx/channeling/eff/chn_x6rc_fr_share01x.avfx";  // 四人分摊 (4-person stack)
    private const string VfxCircle = "vfx/channeling/eff/chn_x6rc_fr_tgae01x.avfx";  // 大圈 (Large circle)
    private const string VfxTether = "vfx/channeling/eff/chn_tergetfix1f.avfx";      // 连线VFX (19210) (Tether VFX)

    // 属性头标 (Attribute markers)
    // 2015013 风+雷 (Wind+Lightning), 2015014 暗+雷 (Dark+Lightning), 2015015 土 (Earth), 2015016 火 (Fire)

    #endregion

    #region 状态变量 (State Variables)

    private record struct CloneInfo(uint ActionId, Vector3 Position, float Rotation);

    private readonly List<string> _eventLog = new();
    private const int MaxLogEntries = 100;

    // 四运状态 (Four Movements state)
    private bool _isMechanicActive = false;
    private readonly List<(uint EntityId, Vector3 Position, string PointType)> _firstSpawnPositions = new();
    private readonly List<CloneInfo> _cloneInfos = new();
    private string _connectionType = ""; // 正点分摊 or 斜点分摊 (Cardinal stack or Intercardinal stack)
    private Vector3 _frontBackClonePos = Vector3.Zero; // 前后刀分身位置 (Front/back cleave clone position)
    private long _executeTime = 0;
    private bool _manualDrawTest = false; // 手动测试绘制 (Manual test drawing)
    private readonly List<string> _drawDebugLog = new(); // 绘制调试日志 (Drawing debug log)
    private string _firstSpawnType = ""; // 先出现的是正点还是斜点 (First to appear is cardinal or intercardinal) ("正点" / "斜点")

    // 心象投影读条计数器 (Mental Projection cast counter)
    private int _projectionCastCount = 0;

    // 玩家连线追踪 (职能, 连线类型) - 保留record struct用于其他用途 (Player tether tracking (Role, Tether type) - Keep record struct for other uses)
    private record struct PlayerTetherInfo(string PlayerName, string Role, string TetherType, string PointType);

    // 第一阶段: 连线检测 - 标点→玩家名 (data3=368) (Phase 1: Tether detection - Waymark→Player name)
    private readonly Dictionary<string, string> _round1Tethers = new();

    // 第二阶段: VFX类型检测 - 标点→VFX类型 (分摊/大圈) - 由VfxShare/VfxCircle填充 (Phase 2: VFX type detection - Waymark→VFX type (stack/spread) - Filled by VfxShare/VfxCircle)
    private readonly Dictionary<string, string> _round2VfxTypes = new();

    // 第二阶段: 连线检测 - 标点→(玩家名, VFX类型) - 由OnTetherCreate (data3=369/373) 填充 (Phase 2: Tether detection - Waymark→(Player name, VFX type) - Filled by OnTetherCreate)
    private readonly Dictionary<string, (string PlayerName, string VfxType)> _round2Tethers = new();

    // 阶段计数器: 0=未开始, 1=第一阶段(VfxTether), 2=第二阶段(VfxShare/VfxCircle), 3=引导中 (Phase counter: 0=Not started, 1=Phase 1, 2=Phase 2, 3=Guiding)
    private int _currentPhase = 0;

    // 第三阶段: 引导相关 (Phase 3: Guidance related)
    private readonly List<string> _guidanceSequence = new(); // 4轮顺序 (分摊/大圈) (4-round sequence (stack/spread))
    private readonly List<string[]> _roundPoints = new(); // 每轮处理的标点对 (Waymark pairs for each round)
    private int _currentGuidanceRound = 0; // 当前轮次 0-3 (Current round 0-3)
    private int _lastLoggedRound = -1; // 上次输出日志的轮次 (防止每帧重复输出) (Last logged round - Prevent duplicate output per frame)
    private long _guidanceStartTime = 0; // 引导开始时间 (Guidance start time)
    private const int FirstRoundDurationMs = 7000; // 第一轮持续5秒 (First round duration 5 sec)
    private const int OtherRoundDurationMs = 5000; // 后续轮持续5秒 (Subsequent rounds duration 5 sec)

    // 玩家分组缓存: 玩家名→组名(右组/左组) (Player group cache: Player name→Group name (Right group/Left group))
    private readonly Dictionary<string, string> _playerGroups = new();

    // 玩家标点缓存: 玩家名→标点名 (Player waymark cache: Player name→Waymark name)
    private readonly Dictionary<string, string> _playerPoints = new();

    // 第二轮AOE: 第5次心象投影后记录，第7次绘制 (Second round AOE: Recorded after 5th Mental Projection, drawn on 7th)
    private readonly List<CloneInfo> _secondRoundCloneInfos = new();
    private bool _recordingSecondRound = false;
    private long _secondRoundDrawTime = 0;

    // 第三轮: 第8次心象投影绘制缺少的刀 (Third round: 8th Mental Projection draws missing cleave)
    private uint _missingBladeType = 0; // CloneLeftRight 或 CloneFrontBack (CloneLeftRight or CloneFrontBack)
    private uint _fifthRoundBladeType = 0; // 第5次记录的刀类型 (Cleave type recorded on 5th)
    private long _thirdRoundDrawTime = 0;
    private static readonly Vector3 ThirdRoundPosition = new(100f, 0f, 92.5f);

    // 第6/8次心象投影: 19210大圈绘制 (6th/8th Mental Projection: 19210 large circle drawing)
    private long _sixthRoundDrawTime = 0;
    private long _eighthRoundDrawTime = 0;
    private readonly List<Vector3> _sixthRoundCirclePositions = new();
    private readonly List<Vector3> _eighthRoundCirclePositions = new();
    private bool _waitingForTimeWarp = false; // 等待时空重现触发第6次绘制 (Waiting for Time Warp to trigger 6th drawing)

    // 第5次心象投影: 小世界站位绘制 (5th Mental Projection: Small world positioning drawing)
    private long _fifthRoundStandDrawTime = 0;

    // 标点坐标 (4A1顺序) (Waymark coordinates - 4A1 order)
    private static readonly Vector3 WaymarkA = new(99.9f, 0f, 88.96f);    // 北 (North)
    private static readonly Vector3 WaymarkB = new(110.89f, 0f, 99.74f);  // 东 (East)
    private static readonly Vector3 WaymarkC = new(100.10f, 0f, 110.90f); // 南 (South)
    private static readonly Vector3 WaymarkD = new(89.11f, 0f, 99.93f);   // 西 (West)
    private static readonly Vector3 Waymark1 = new(108.28f, 0f, 91.70f);  // 东北 (Northeast)
    private static readonly Vector3 Waymark2 = new(108.43f, 0f, 108.10f); // 东南 (Southeast)
    private static readonly Vector3 Waymark3 = new(91.80f, 0f, 108.17f);  // 西南 (Southwest)
    private static readonly Vector3 Waymark4 = new(91.62f, 0f, 91.75f);   // 西北 (Northwest)

    // 19210 固定坐标 (19210 Fixed Positions)
    private static readonly Vector3 Spawn19210_A = new(100.00f, 0f, 86.00f);   // A点 (北)
    private static readonly Vector3 Spawn19210_B = new(114.00f, 0f, 100.00f);  // B点 (东)
    private static readonly Vector3 Spawn19210_C = new(100.00f, 0f, 114.00f);  // C点 (南)
    private static readonly Vector3 Spawn19210_D = new(86.00f, 0f, 100.00f);   // D点 (西)
    private static readonly Vector3 Spawn19210_1 = new(109.90f, 0f, 90.10f);   // 1点 (东北)
    private static readonly Vector3 Spawn19210_2 = new(109.90f, 0f, 109.90f);  // 2点 (东南)
    private static readonly Vector3 Spawn19210_3 = new(90.10f, 0f, 109.90f);   // 3点 (西南)
    private static readonly Vector3 Spawn19210_4 = new(90.10f, 0f, 90.10f);    // 4点 (西北)

    #endregion

    #region 配置 (Configuration)

    private Config C => Controller.GetConfig<Config>();

    public class Config : IEzConfig
    {
        public int DelayMs = 9000;  // AOE绘制延迟时间 (AOE drawing delay time)
        public int DurationMs = 4000; // AOE持续时间 (4秒) (AOE duration - 4 sec)
        public int GuidanceDelayMs = 5700; // 引导延迟时间 (Guidance delay time)
        public int EighthCircleDelayMs = 5700; // 第8次大圈延迟 (8th circle delay)
        public int EighthCircleDurationMs = 5000; // 第8次大圈持续时间 (8th circle duration)
        public int MissingBladeDelayMs = 13000; // 缺少的刀延迟 (Missing cleave delay)
        public int MissingBladeDurationMs = 5000; // 缺少的刀持续时间 (Missing cleave duration)
        public int FifthStandDelayMs = 8000; // 第5次小世界站位延迟 (5th small world positioning delay)
        public int FifthStandDurationMs = 7000; // 第5次小世界站位持续时间 (5th small world positioning duration)
        public uint ColorDanger = 0xFFFF0000;  // 红色 (Red)
        public bool EnableDraw = true; // 是否启用绘制 (Enable drawing)
        public bool EnableGuidance = true; // 是否启用指路绘制 (Enable guidance drawing)
        public bool DebugTetherAll = true; // Debug模式: 连线所有玩家; 正常模式: 只连线自己 (Debug mode: tether all players; Normal mode: only tether self)
    }

    #endregion

    #region 元素设置 (Element Setup)

    public override void OnSetup()
    {
        // 为每个可能的分身位置注册元素 (最多8个分身) (Register elements for each possible clone position - Maximum 8 clones)
        for(int i = 0; i < 8; i++)
        {
            // 圆形AOE (钢铁) - type 0 = 固定坐标圆形 (Circle AOE (in) - type 0 = fixed coordinate circle)
            Controller.RegisterElement($"Circle_{i}", new Element(0)
            {
                radius = 10f,
                Filled = true,
                fillIntensity = 0.5f,
                thicc = 3f,
                Enabled = false,
            });

            // 左右刀 - 右侧扇形 - type 5 = 固定坐标锥形 (Left/Right Cleave - Right side cone - type 5 = fixed coordinate cone)
            Controller.RegisterElement($"LeftRight_Right_{i}", new Element(5)
            {
                radius = 40f,
                coneAngleMin = 45,
                coneAngleMax = 135,
                Filled = true,
                fillIntensity = 0.5f,
                thicc = 3f,
                Enabled = false,
            });

            // 左右刀 - 左侧扇形 (Left/Right Cleave - Left side cone)
            Controller.RegisterElement($"LeftRight_Left_{i}", new Element(5)
            {
                radius = 40f,
                coneAngleMin = 225,
                coneAngleMax = 315,
                Filled = true,
                fillIntensity = 0.5f,
                thicc = 3f,
                Enabled = false,
            });

            // 前后刀 - 前方扇形 (Front/Back Cleave - Front cone)
            Controller.RegisterElement($"FrontBack_Front_{i}", new Element(5)
            {
                radius = 40f,
                coneAngleMin = -45,
                coneAngleMax = 45,
                Filled = true,
                fillIntensity = 0.5f,
                thicc = 3f,
                Enabled = false,
            });

            // 前后刀 - 后方扇形 (Front/Back Cleave - Back cone)
            Controller.RegisterElement($"FrontBack_Back_{i}", new Element(5)
            {
                radius = 40f,
                coneAngleMin = 135,
                coneAngleMax = 225,
                Filled = true,
                fillIntensity = 0.5f,
                thicc = 3f,
                Enabled = false,
            });
        }

        // 第三阶段引导元素: 左右组站位点 (Phase 3 guidance elements: Left/Right group positions)
        // 右组分摊站位 (北侧) (Right group stack position - north)
        Controller.RegisterElement("RightGroup_Share", new Element(0)
        {
            refX = 105.76f,
            refY = 0,
            refZ = 91.98f,
            radius = 5f,
            Filled = true,
            fillIntensity = 0.5f,
            color = 0xFF00FF00, // 绿色 (Green)
            Enabled = false,
        });

        // 右组分散站位 (南侧) (Right group spread position - south)
        Controller.RegisterElement("RightGroup_Spread", new Element(0)
        {
            refX = 112.73f,
            refY = 0,
            refZ = 113.91f,
            radius = 1f,
            Filled = true,
            fillIntensity = 0.5f,
            color = 0xFFFF00FF, // 紫色 (Purple)
            Enabled = false,
        });

        // 左组分摊站位 (北侧) (Left group stack position - north)
        Controller.RegisterElement("LeftGroup_Share", new Element(0)
        {
            refX = 94.16f,
            refY = 0,
            refZ = 92.20f,
            radius = 5f,
            Filled = true,
            fillIntensity = 0.5f,
            color = 0xFF00FF00, // 绿色 (Green)
            Enabled = false,
        });

        // 左组分散站位 (南侧) (Left group spread position - south)
        Controller.RegisterElement("LeftGroup_Spread", new Element(0)
        {
            refX = 89.18f,
            refY = 0,
            refZ = 115.38f,
            radius = 1f,
            Filled = true,
            fillIntensity = 0.5f,
            color = 0xFFFF00FF, // 紫色 (Purple)
            Enabled = false,
        });

        // 连线指路元素 (8个玩家) (Tether guidance elements - 8 players)
        for(int i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"PlayerTether_{i}", new Element(2) // type 2 = 连线 (type 2 = tether)
            {
                thicc = 2.5f,
                Enabled = false,
            });

            // 大圈玩家身上的圆形指示 (Circle indicator on spread player)
            Controller.RegisterElement($"PlayerCircle_{i}", new Element(1) // type 1 = 跟随玩家的圆形 (type 1 = circle following player)
            {
                radius = 20f,
                Filled = false,
                thicc = 3f,
                color = 0xFFFF00FF, // 紫色 (Purple)
                Enabled = false,
            });
        }

        // 第6/8次心象投影: 19210大圈 (2个) (6th/8th Mental Projection: 19210 large circles - 2)
        for(int i = 0; i < 2; i++)
        {
            Controller.RegisterElement($"SpawnCircle_{i}", new Element(0)
            {
                radius = 20f,
                Filled = true,
                fillIntensity = 0.5f,
                thicc = 3f,
                Enabled = false,
            });
        }
    }

    #endregion

    public override void OnStartingCast(uint source, uint castId)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        // 检测四运开始 (Detect Four Movements start)
        if(castId == MirrorCastId)
        {
            _isMechanicActive = true;
            AddLog($"[{timestamp}] === 四运开始 (Four Movements Start) === (镜中奇梦 (Mirror Dreams) {castId})");
            _firstSpawnPositions.Clear();
            _cloneInfos.Clear();
            _connectionType = "";
            _frontBackClonePos = Vector3.Zero;
            _executeTime = 0;
            _round1Tethers.Clear();
            _round2VfxTypes.Clear();
            _round2Tethers.Clear();
            _projectionCastCount = 0; // 重置心象投影计数器 (Reset Mental Projection counter)
            return;
        }

        if(!_isMechanicActive) return;

        // 获取source对象信息 (Get source object info)
        if(source.GetObject() is IBattleNpc npc)
        {
            var baseId = npc.BaseId;
            var pos = npc.Position;
            var rot = npc.Rotation;
            var pointType = GetPointType(pos);

            AddLog($"[{timestamp}] StartCast: {npc.Name} (BaseId:{baseId}) 读条 (Casting):{castId} 位置 (Position):({pos.X:F1},{pos.Z:F1}) 朝向 (Rotation):{rot:F2} {pointType}");

            // 记录分身技能 (Record clone skills)
            if(baseId == CloneDataId)
            {
                if(castId is CloneLeftRight or CloneFrontBack or CloneCircle or CloneCircle2)
                {
                    string actionName = castId switch
                    {
                        CloneLeftRight => "左右双刀 (Left/Right Cleave)",
                        CloneFrontBack => "前后双刀 (Front/Back Cleave)",
                        CloneCircle or CloneCircle2 => "圆形AOE (Circle AOE)",
                        _ => $"未知 (Unknown)({castId})"
                    };

                    // 根据阶段记录到不同列表 (Record to different lists based on phase)
                    if(_recordingSecondRound)
                    {
                        _secondRoundCloneInfos.Add(new CloneInfo(castId, pos, rot));
                        AddLog($"  -> [第二轮 (Second Round)] 分身技能 (Clone Skill): {actionName} (共 (Total){_secondRoundCloneInfos.Count}个)");

                        // 记录刀的类型用于第三轮绘制 (Record cleave type for third round drawing)
                        if(castId is CloneLeftRight or CloneFrontBack)
                        {
                            _fifthRoundBladeType = castId;
                            var bladeType = castId == CloneLeftRight ? "左右双刀 (Left/Right Cleave)" : "前后双刀 (Front/Back Cleave)";
                            AddLog($"  -> 第5次记录刀类型 (5th recorded cleave type): {bladeType}");
                        }
                    }
                    else
                    {
                        _cloneInfos.Add(new CloneInfo(castId, pos, rot));
                        AddLog($"  -> 分身技能 (Clone Skill): {actionName}");
                    }

                    // 记录前后刀位置 (Record front/back cleave position)
                    if(castId == CloneFrontBack)
                    {
                        _frontBackClonePos = pos;
                        AddLog($"  -> 记录前后刀位置 (Recorded front/back cleave position): ({pos.X:F1},{pos.Z:F1})");
                    }
                }
            }

            // Boss执行读条 (仅记录，不触发绘制) (Boss execute cast - record only, does not trigger drawing)
            if(baseId == BossDataId && castId == ExecuteCastId)
            {
                AddLog($"  -> Boss执行读条 (Boss execute cast) ({castId})");
            }

            // 时空重现 - 触发第6次大圈绘制 (Time Warp - Triggers 6th circle drawing)
            // 正点先刷影子：第一次大圈 C/D，斜点先刷影子：第一次大圈 1/2
            if(baseId == BossDataId && castId == TimeWarpCastId && _waitingForTimeWarp)
            {
                _waitingForTimeWarp = false;
                _sixthRoundCirclePositions.Clear();

                // 使用固定坐标
                if(_firstSpawnType == "正点")
                {
                    // 正点先: 第一次大圈是C/D
                    _sixthRoundCirclePositions.Add(Spawn19210_C);
                    _sixthRoundCirclePositions.Add(Spawn19210_D);
                }
                else
                {
                    // 斜点先: 第一次大圈是1/2
                    _sixthRoundCirclePositions.Add(Spawn19210_1);
                    _sixthRoundCirclePositions.Add(Spawn19210_2);
                }

                _sixthRoundDrawTime = Environment.TickCount64;
                var waymarkNames = _firstSpawnType == "正点" ? "C/D" : "1/2";
                AddLog($"  -> 时空重现 (Time Warp)! 立即绘制第6次大圈 (Immediately draw 6th circles) {waymarkNames} (使用固定坐标)");
            }

            // Boss心象投影读条 - 第4次触发引导，第5次开始记录第二轮，第7次绘制 (Boss Mental Projection cast - 4th triggers guidance, 5th starts recording second round, 7th draws)
            if(baseId == BossDataId && castId == ProjectionCastId)
            {
                _projectionCastCount++;
                AddLog($"  -> 心象投影读条 (Mental Projection cast) 第 (Number){_projectionCastCount}次 ({castId})");

                if(_projectionCastCount == 4)
                {
                    // 第4次读条，计算引导并设置延迟触发 (4th cast, calculate guidance and set delayed trigger)
                    CalculateGuidanceSequence();
                    CalculatePlayerGroups();
                    // 引导延迟后开始 (Start after guidance delay)
                    _guidanceStartTime = Environment.TickCount64 + C.GuidanceDelayMs;
                    _currentPhase = 3;
                    _currentGuidanceRound = 0;
                    _lastLoggedRound = -1; // 重置日志轮次，确保第一轮能输出日志 (Reset log round to ensure first round outputs log)
                    AddLog($"  -> 第4次心象投影 (4th Mental Projection)! {C.GuidanceDelayMs}ms后开始引导绘制 (ms until guidance drawing starts)");
                    AddLog($"  -> 引导顺序 (Guidance sequence): {string.Join(" → ", _guidanceSequence)}");
                }
                else if(_projectionCastCount == 5)
                {
                    // 第5次：开始记录第二轮分身技能 (5th: Start recording second round clone skills)
                    _secondRoundCloneInfos.Clear();
                    _recordingSecondRound = true;
                    // 第5次：触发小世界站位绘制 (5th: Trigger small world positioning drawing)
                    _fifthRoundStandDrawTime = Environment.TickCount64 + C.FifthStandDelayMs;
                    AddLog($"  -> 第5次心象投影 (5th Mental Projection)! 开始记录第二轮分身技能 (Start recording second round clone skills), {C.FifthStandDelayMs / 1000f}秒后绘制小世界站位 (sec until small world positioning drawing)");
                }
                else if(_projectionCastCount == 6)
                {
                    // 第6次：设置标志，等待时空重现时再搜索19210 (6th: Set flag, wait for Time Warp to search for 19210)
                    _waitingForTimeWarp = true;
                    var waymarkNames = _firstSpawnType == "正点" ? "C/D" : "1/2";
                    AddLog($"  -> 第6次心象投影 (6th Mental Projection)! 等待时空重现触发 (Waiting for Time Warp to trigger) {waymarkNames}附近 (nearby) 19210大圈 (large circles)");
                }
                else if(_projectionCastCount == 7)
                {
                    // 第7次：触发第二轮AOE绘制，延迟3秒，并根据第5次记录的刀类型确定缺少的刀 (7th: Trigger second round AOE drawing, delay 3 sec, determine missing cleave based on 5th recorded type)
                    _recordingSecondRound = false;
                    _secondRoundDrawTime = Environment.TickCount64 + 3000;

                    // 根据第5次记录的刀类型确定缺少的刀 (第5次是左右刀则缺前后刀，反之亦然) (Determine missing cleave based on 5th recorded type - if 5th was left/right, missing is front/back, and vice versa)
                    _missingBladeType = _fifthRoundBladeType == CloneLeftRight ? CloneFrontBack : CloneLeftRight;

                    string missingName = _missingBladeType == CloneLeftRight ? "左右双刀 (Left/Right Cleave)" : "前后双刀 (Front/Back Cleave)";
                    string fifthName = _fifthRoundBladeType == CloneLeftRight ? "左右双刀 (Left/Right Cleave)" : "前后双刀 (Front/Back Cleave)";
                    AddLog($"  -> 第7次心象投影 (7th Mental Projection)! 3秒后绘制第二轮AOE (3 sec until second round AOE drawing) (共 (Total){_secondRoundCloneInfos.Count}个分身 (clones))");
                    AddLog($"  -> 第5次是 (5th was) {fifthName}，缺少的刀 (missing cleave): {missingName}");
                }
                else if(_projectionCastCount == 8)
                {
                    // 第8次：绘制缺少的刀 (8th: Draw missing cleave)
                    _thirdRoundDrawTime = Environment.TickCount64 + C.MissingBladeDelayMs;
                    AddLog($"  -> 第8次心象投影 (8th Mental Projection)! {C.MissingBladeDelayMs / 1000f}秒后绘制缺少的刀 (sec until missing cleave drawing)");

                    // 第8次：根据正点先/斜点先绘制19210大圈 (使用固定坐标)
                    // 正点先刷影子：第二次大圈 1/2
                    // 斜点先刷影子：第二次大圈 C/D
                    _eighthRoundCirclePositions.Clear();

                    // 使用固定坐标，不再搜索
                    if(_firstSpawnType == "正点")
                    {
                        // 正点先: 第二次大圈是1/2
                        _eighthRoundCirclePositions.Add(Spawn19210_1);
                        _eighthRoundCirclePositions.Add(Spawn19210_2);
                    }
                    else
                    {
                        // 斜点先: 第二次大圈是C/D
                        _eighthRoundCirclePositions.Add(Spawn19210_C);
                        _eighthRoundCirclePositions.Add(Spawn19210_D);
                    }

                    _eighthRoundDrawTime = Environment.TickCount64 + C.EighthCircleDelayMs;
                    var waymarkNames = _firstSpawnType == "正点" ? "1/2" : "C/D";
                    AddLog($"  -> 第8次心象投影 (8th Mental Projection)! {C.EighthCircleDelayMs / 1000f}秒后绘制 (sec until drawing) {waymarkNames} 19210大圈 (使用固定坐标)");
                }
            }
        }
        else
        {
            AddLog($"[{timestamp}] StartCast: source={source} castId={castId}");
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!_isMechanicActive) return;
        if(set.Action == null) return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var actionId = set.Action.Value.RowId;
        var actionName = set.Action.Value.Name.ToString();

        if(set.Source is IBattleNpc npc)
        {
            var baseId = npc.BaseId;
            AddLog($"[{timestamp}] ActionEffect: {npc.Name} (BaseId:{baseId}) {actionId} {actionName}");
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(!_isMechanicActive) return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        // 第一阶段: 连线VFX - 记录连线玩家 (VFX在19204上) (Phase 1: Tether VFX - Record tethered players - VFX on 19204)
        if(vfxPath == VfxTether)
        {
            // 第一次VfxTether触发阶段1 (First VfxTether triggers Phase 1)
            if(_currentPhase == 0)
            {
                _currentPhase = 1;
                AddLog($"[{timestamp}] === 进入阶段1: 连线检测 (Entering Phase 1: Tether Detection) ===");
            }

            var sourceObj = target.GetObject();
            if(sourceObj is IBattleNpc npc && npc.BaseId == CloneDataId)
            {
                var npcPos = npc.Position;
                var pointName = GetPointName(npcPos);
                var npcPointType = GetPointType(npcPos);
                AddLog($"[{timestamp}] 连线VFX (Tether VFX): 19204 {npcPointType} 位置 (Position):({npcPos.X:F1},{npcPos.Z:F1})");

                // 通过 AttachedInfo.TetherInfos 查找该分身连线的玩家 (Find tethered player via AttachedInfo.TetherInfos)
                if(AttachedInfo.TetherInfos.TryGetValue(npc.Address, out var tethers) && tethers.Count > 0)
                {
                    foreach(var tether in tethers)
                    {
                        var tetherTarget = Svc.Objects.FirstOrDefault(x => x.EntityId == tether.Target);
                        if(tetherTarget is IPlayerCharacter player)
                        {
                            var playerName = player.Name.ToString();
                            _round1Tethers[pointName] = playerName;
                            AddLog($"  -> 第一阶段 (Phase 1): {pointName}点——>{playerName}");
                        }
                    }
                }
                else
                {
                    // 如果 AttachedInfo 还没有数据，使用 npc.TargetObjectId (If AttachedInfo has no data yet, use npc.TargetObjectId)
                    var npcTargetId = npc.TargetObjectId;
                    var npcTargetObj = Svc.Objects.FirstOrDefault(x => x.EntityId == npcTargetId);
                    if(npcTargetObj is IPlayerCharacter player)
                    {
                        var playerName = player.Name.ToString();
                        _round1Tethers[pointName] = playerName;
                        AddLog($"  -> 第一阶段 (Phase 1): {pointName}点——>{playerName}");
                    }
                }
            }
        }
        // 第二阶段: 分摊/大圈VFX - 记录VFX类型和位置 (VFX在19204上) (Phase 2: Stack/Spread VFX - Record VFX type and position - VFX on 19204)
        else if(vfxPath == VfxShare || vfxPath == VfxCircle)
        {
            // 第一次VfxShare/VfxCircle触发阶段2 (First VfxShare/VfxCircle triggers Phase 2)
            if(_currentPhase == 1)
            {
                _currentPhase = 2;
                AddLog($"[{timestamp}] === 进入阶段2: VFX类型检测 (Entering Phase 2: VFX Type Detection) ===");
            }

            var targetObj = target.GetObject();
            var targetName = targetObj?.Name.ToString() ?? target.ToString();
            var pos = targetObj?.Position ?? Vector3.Zero;
            var pointType = GetPointType(pos);
            var pointName = GetPointName(pos);

            string vfxType = vfxPath == VfxShare ? "分摊 (Stack)" : "大圈 (Spread)";
            string vfxTypeDisplay = vfxPath == VfxShare ? "四人分摊 (4-person Stack)" : "大圈 (Large Circle)";
            AddLog($"[{timestamp}] VFX: {vfxTypeDisplay} 目标 (Target):{targetName} 位置 (Position):({pos.X:F1},{pos.Z:F1}) {pointType}");

            // 记录到 _round2VfxTypes (19204分身) (Record to _round2VfxTypes - 19204 clones)
            if(targetObj is IBattleNpc npc && npc.BaseId == CloneDataId)
            {
                _round2VfxTypes[pointName] = vfxPath == VfxShare ? "分摊" : "大圈";
                AddLog($"  -> 记录VFX类型 (Recorded VFX type): {pointName}点【{vfxType}】 (共 (Total){_round2VfxTypes.Count}/8)");

                // VfxShare/VfxCircle 出现时触发绘制 (VfxShare/VfxCircle triggers drawing when appearing)
                if(_executeTime == 0)
                {
                    _executeTime = Environment.TickCount64;
                    AddLog($"  -> VFX触发绘制 (VFX triggered drawing)! 将在 (Will draw in){C.DelayMs}ms后绘制AOE (ms)");
                }

                // 注: 引导现在由第4次心象投影(48098)触发，不再由VFX触发 (Note: Guidance now triggered by 4th Mental Projection (48098), no longer by VFX)
            }
        }
        else if(vfxPath.Contains("channeling") || vfxPath.Contains("lockon"))
        {
            var targetObj = target.GetObject();
            var targetName = targetObj?.Name.ToString() ?? target.ToString();
            AddLog($"[{timestamp}] VFX: {vfxPath} 目标 (Target):{targetName}");
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(!_isMechanicActive) return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var sourceObj = source.GetObject();
        var targetObj = target.GetObject();

        var sourceName = sourceObj?.Name.ToString() ?? source.ToString();
        var targetName = targetObj?.Name.ToString() ?? target.ToString();
        var sourcePos = sourceObj?.Position ?? Vector3.Zero;

        AddLog($"[{timestamp}] Tether: {sourceName} -> {targetName} data2:{data2} data3:{data3} 位置 (Position):({sourcePos.X:F1},{sourcePos.Z:F1})");

        // 检测人形分身(19204)连线玩家 (Detect humanoid clone (19204) tethered to player)
        if(sourceObj is IBattleNpc npc && npc.BaseId == CloneDataId && targetObj is IPlayerCharacter player)
        {
            var pointName = GetPointName(sourcePos);
            var playerName = player.Name.ToString();

            // 根据data3区分阶段: 368=第一阶段, 369/373=第二阶段 (Distinguish phase by data3: 368=Phase 1, 369/373=Phase 2)
            if(data3 == 368)
            {
                // 第一阶段连线 (Phase 1 tether)
                _round1Tethers[pointName] = playerName;
                AddLog($"  -> 第一阶段 (Phase 1)(data3=368): {pointName}点——>{playerName}");
            }
            else if(data3 == 369 || data3 == 373)
            {
                // 第二阶段连线，从 _round2VfxTypes 获取VFX类型 (Phase 2 tether, get VFX type from _round2VfxTypes)
                var vfxType = _round2VfxTypes.TryGetValue(pointName, out var type) ? type : "未知 (Unknown)";
                _round2Tethers[pointName] = (playerName, vfxType);
                AddLog($"  -> 第二阶段 (Phase 2)(data3={data3}): {pointName}点【{vfxType}】——>{playerName}");
            }
        }
    }

    // 获取NPC身上的VFX类型 (分摊/大圈) (Get VFX type on NPC - stack/spread)
    private string GetNpcVfxType(IBattleNpc npc)
    {
        // 调试: 输出NPC的VFX信息 (Debug: Output NPC VFX info)
        AddLog($"    [VFX检测 (VFX Detection)] NPC:{npc.Name} BaseId:{npc.BaseId} Address:{npc.Address:X}");

        if(AttachedInfo.VFXInfos.TryGetValue(npc.Address, out var vfxDict))
        {
            AddLog($"    [VFX检测 (VFX Detection)] 找到VFX字典 (Found VFX dictionary)，共 (Total){vfxDict.Count}个VFX");
            foreach(var kvp in vfxDict)
            {
                AddLog($"      - {kvp.Key}");
            }

            if(vfxDict.ContainsKey(VfxShare))
                return "分摊 (Stack)";
            if(vfxDict.ContainsKey(VfxCircle))
                return "大圈 (Spread)";
        }
        else
        {
            AddLog($"    [VFX检测 (VFX Detection)] 未找到VFX字典 (VFX dictionary not found)");
        }
        return "未知 (Unknown)";
    }

    // 计算引导顺序 (4轮) - 固定顺序: AC → 13 → BD → 24 (Calculate guidance sequence (4 rounds) - Fixed order: AC → 13 → BD → 24)
    private void CalculateGuidanceSequence()
    {
        _guidanceSequence.Clear();
        _roundPoints.Clear();

        // 固定顺时针顺序: AC → 13 → BD → 24 (Fixed clockwise order: AC → 13 → BD → 24)
        _roundPoints.Add(new[] { "A", "C" });
        _roundPoints.Add(new[] { "1", "3" });
        _roundPoints.Add(new[] { "B", "D" });
        _roundPoints.Add(new[] { "4", "2" });

        // 根据第一轮VFX类型确定顺序，后续固定交替 (Determine sequence based on first round VFX type, subsequent rounds alternate)
        var firstRoundPoint = _roundPoints[0][0]; // A点 (A point)
        var firstVfxType = _round2VfxTypes.TryGetValue(firstRoundPoint, out var type) ? type : "未知 (Unknown)";

        // 固定交替模式 (Fixed alternating pattern)
        if(firstVfxType == "大圈")
        {
            _guidanceSequence.AddRange(new[] { "大圈 (Spread)", "分摊 (Stack)", "大圈 (Spread)", "分摊 (Stack)" });
        }
        else
        {
            _guidanceSequence.AddRange(new[] { "分摊 (Stack)", "大圈 (Spread)", "分摊 (Stack)", "大圈 (Spread)" });
        }

        AddLog($"  -> 标点顺序 (Waymark order): {string.Join(" → ", _roundPoints.Select(p => $"[{string.Join(",", p)}]"))}");
        AddLog($"  -> 第一轮VFX (First round VFX): {firstVfxType} -> 固定交替顺序 (Fixed alternating order): {string.Join(" → ", _guidanceSequence)}");
    }

    // 根据第二阶段连线计算玩家分组 (Calculate player groups based on Phase 2 tethers)
    private void CalculatePlayerGroups()
    {
        _playerGroups.Clear();
        _playerPoints.Clear();

        foreach(var kvp in _round2Tethers)
        {
            var point = kvp.Key;
            var playerName = kvp.Value.PlayerName;

            // 记录玩家对应的标点 (Record player's corresponding waymark)
            _playerPoints[playerName] = point;

            // 右组: A, 1, B, 2 (4A1顺序) (Right group: A, 1, B, 2 - 4A1 order)
            // 左组: C, 3, D, 4 (Left group: C, 3, D, 4)
            if(point == "A" || point == "1" || point == "B" || point == "2")
                _playerGroups[playerName] = "右组 (Right Group)";
            else if(point == "C" || point == "3" || point == "D" || point == "4")
                _playerGroups[playerName] = "左组 (Left Group)";
        }

        // 日志输出分组结果 (Log grouping results)
        var rightGroup = _playerGroups.Where(x => x.Value == "右组 (Right Group)").Select(x => x.Key).ToList();
        var leftGroup = _playerGroups.Where(x => x.Value == "左组 (Left Group)").Select(x => x.Key).ToList();
        AddLog($"  -> 右组 (Right Group)(A1B2): {string.Join(", ", rightGroup)}");
        AddLog($"  -> 左组 (Left Group)(C3D4): {string.Join(", ", leftGroup)}");
    }

    // 绘制引导站位点 (Draw guidance positions)
    private void DrawGuidanceElements(string action, int roundIndex)
    {
        // 分摊时：显示两组分摊点 (Stack: Show both group stack points)
        // 大圈时：只显示该轮需要出去放圈的组的分散点 (Spread: Only show spread points for the group that needs to go out)
        bool isSpread = action.Contains("大圈");

        if(isSpread && roundIndex < _roundPoints.Count)
        {
            // 大圈：只显示分散点 (Spread: Only show spread points)
            var currentPoints = _roundPoints[roundIndex];
            // 右组点: A, B, 1, 2 (Right group points: A, B, 1, 2)
            bool rightNeedsSpread = currentPoints.Any(p => p == "A" || p == "B" || p == "1" || p == "2");
            // 左组点: C, D, 3, 4 (Left group points: C, D, 3, 4)
            bool leftNeedsSpread = currentPoints.Any(p => p == "C" || p == "D" || p == "3" || p == "4");

            if(Controller.TryGetElementByName("RightGroup_Share", out var rs))
                rs.Enabled = false;
            if(Controller.TryGetElementByName("RightGroup_Spread", out var rsp))
                rsp.Enabled = rightNeedsSpread;
            if(Controller.TryGetElementByName("LeftGroup_Share", out var ls))
                ls.Enabled = false;
            if(Controller.TryGetElementByName("LeftGroup_Spread", out var lsp))
                lsp.Enabled = leftNeedsSpread;
        }
        else
        {
            // 分摊：显示两组分摊点 (Stack: Show both group stack points)
            if(Controller.TryGetElementByName("RightGroup_Share", out var rs))
                rs.Enabled = true;
            if(Controller.TryGetElementByName("RightGroup_Spread", out var rsp))
                rsp.Enabled = false;
            if(Controller.TryGetElementByName("LeftGroup_Share", out var ls))
                ls.Enabled = true;
            if(Controller.TryGetElementByName("LeftGroup_Spread", out var lsp))
                lsp.Enabled = false;
        }
    }

    // 绘制玩家连线指路 - 所有玩家都需要指路 (Draw player tether guidance - All players need guidance)
    private void DrawPlayerTethers(string action, int roundIndex)
    {
        int tetherIndex = 0;
        var party = FakeParty.Get().ToList();
        var localPlayer = Svc.ClientState.LocalPlayer;

        // 当前轮次需要处理的标点 (Waymarks to process for current round)
        var currentPoints = roundIndex < _roundPoints.Count ? _roundPoints[roundIndex] : Array.Empty<string>();

        // 只在轮次切换时输出一次日志 (Only output log once when round changes)
        bool shouldLog = _lastLoggedRound != roundIndex;
        if(shouldLog)
        {
            _lastLoggedRound = roundIndex;
            AddLog($"[连线Debug (Tether Debug)] 轮次 (Round){roundIndex + 1} action={action} currentPoints=[{string.Join(",", currentPoints)}]");
        }

        foreach(var player in party)
        {
            var playerName = player.Name.ToString();

            // 获取玩家对应的标点 (Get player's corresponding waymark)
            if(!_playerPoints.TryGetValue(playerName, out var playerPoint))
                continue;

            // 获取玩家所属组 (Get player's group)
            if(!_playerGroups.TryGetValue(playerName, out var group))
                continue;

            // 非Debug模式下只连线自己 (Non-debug mode only tethers self)
            if(!C.DebugTetherAll && player.EntityId != localPlayer?.EntityId)
                continue;

            Vector3 targetPos;
            uint tetherColor;

            // 判断该玩家是否是当前轮次需要放大圈的人 (Determine if player needs to spread this round)
            // 条件: 当前轮action是"大圈" AND 玩家标点在当前轮标点列表中 (Condition: current round action is "spread" AND player's waymark is in current round's waymark list)
            bool isSpreadPlayer = action.Contains("大圈") && currentPoints.Contains(playerPoint);

            // 只在轮次切换时输出每个玩家的判断结果 (Only output each player's determination when round changes)
            if(shouldLog)
            {
                var vfxType = _round2VfxTypes.TryGetValue(playerPoint, out var t) ? t : "无 (None)";
                AddLog($"  [{playerName}] point={playerPoint} vfx={vfxType} action={action} isSpread={isSpreadPlayer}");
            }

            if(isSpreadPlayer)
            {
                // 当前轮次放大圈的玩家 -> 分散点 (Players spreading this round -> spread point)
                targetPos = group.Contains("右组")
                    ? new Vector3(112.73f, 0, 113.91f)  // 右组分散点 (Right group spread point)
                    : new Vector3(89.18f, 0, 115.38f); // 左组分散点 (Left group spread point)
                tetherColor = 0xFFFF00FFu; // 紫色 (大圈) (Purple - spread)
            }
            else
            {
                // 其他玩家 -> 分摊点 (Other players -> stack point)
                targetPos = group.Contains("右组")
                    ? new Vector3(105.76f, 0, 91.98f)  // 右组分摊点 (Right group stack point)
                    : new Vector3(94.16f, 0, 92.20f); // 左组分摊点 (Left group stack point)
                tetherColor = 0xFF00FF00u; // 绿色 (分摊) (Green - stack)
            }

            // 设置连线 (Set tether)
            if(Controller.TryGetElementByName($"PlayerTether_{tetherIndex}", out var tether))
            {
                tether.Enabled = true;
                tether.SetRefPosition(player.Position);
                tether.SetOffPosition(targetPos);
                tether.color = tetherColor;
            }

            // 大圈玩家身上绘制圆形 (Draw circle on spread player)
            if(Controller.TryGetElementByName($"PlayerCircle_{tetherIndex}", out var circle))
            {
                circle.Enabled = isSpreadPlayer;
                if(isSpreadPlayer)
                {
                    circle.refActorObjectID = player.EntityId;
                    circle.refActorComparisonType = 2; // ObjectID
                }
            }

            tetherIndex++;
        }

        // 隐藏未使用的连线和圆形 (Hide unused tethers and circles)
        for(int i = tetherIndex; i < 8; i++)
        {
            if(Controller.TryGetElementByName($"PlayerTether_{i}", out var t))
                t.Enabled = false;
            if(Controller.TryGetElementByName($"PlayerCircle_{i}", out var c))
                c.Enabled = false;
        }
    }

    // 隐藏引导元素 (Hide guidance elements)
    private void HideGuidanceElements()
    {
        if(Controller.TryGetElementByName("RightGroup_Share", out var rs)) rs.Enabled = false;
        if(Controller.TryGetElementByName("RightGroup_Spread", out var rsp)) rsp.Enabled = false;
        if(Controller.TryGetElementByName("LeftGroup_Share", out var ls)) ls.Enabled = false;
        if(Controller.TryGetElementByName("LeftGroup_Spread", out var lsp)) lsp.Enabled = false;

        for(int i = 0; i < 8; i++)
        {
            if(Controller.TryGetElementByName($"PlayerTether_{i}", out var t))
                t.Enabled = false;
            if(Controller.TryGetElementByName($"PlayerCircle_{i}", out var c))
                c.Enabled = false;
        }
    }

    public override void OnUpdate()
    {
        // 四运激活后检测19210的位置 (After Four Movements activated, detect 19210 positions)
        if(_isMechanicActive && _firstSpawnPositions.Count == 0)
        {
            var firstSpawns = Svc.Objects.OfType<IBattleNpc>()
                .Where(x => x.DataId == FirstSpawnId && x.IsCharacterVisible())
                .ToList();

            if(firstSpawns.Count > 0)
            {
                foreach(var spawn in firstSpawns)
                {
                    var pointType = GetPointType(spawn.Position);
                    _firstSpawnPositions.Add((spawn.EntityId, spawn.Position, pointType));
                    AddLog($"[检测 (Detection)] 19210可见 (visible): X={spawn.Position.X:F2}, Y={spawn.Position.Y:F2} -> {pointType}");
                }

                // 判断先出现的是正点还是斜点 (Determine if first to appear is cardinal or intercardinal)
                if(_firstSpawnPositions.Count > 0)
                {
                    var firstPoint = _firstSpawnPositions[0].PointType;
                    if(firstPoint.Contains("正点"))
                    {
                        _firstSpawnType = "正点";
                    }
                    else if(firstPoint.Contains("斜点"))
                    {
                        _firstSpawnType = "斜点";
                    }
                    AddLog($"[检测 (Detection)] 先出现的是 (First to appear is): {_firstSpawnType} (共 (Total){_firstSpawnPositions.Count}个)");
                }
            }
        }

        if(!_isMechanicActive && !_manualDrawTest)
        {
            // 非激活状态：隐藏所有元素 (Inactive state: Hide all elements)
            HideAllElements();
            return;
        }

        // 绘制AOE (正常触发或手动测试) (Draw AOE - normal trigger or manual test)
        if((C.EnableDraw && _executeTime > 0) || _manualDrawTest)
        {
            _drawDebugLog.Clear();
            _drawDebugLog.Add($"=== 绘制调试 (Drawing Debug) ===");
            _drawDebugLog.Add($"EnableDraw: {C.EnableDraw}");
            _drawDebugLog.Add($"_executeTime: {_executeTime}");
            _drawDebugLog.Add($"_manualDrawTest: {_manualDrawTest}");
            _drawDebugLog.Add($"_cloneInfos.Count: {_cloneInfos.Count}");

            if(!_manualDrawTest)
            {
                var elapsed = Environment.TickCount64 - _executeTime;
                _drawDebugLog.Add($"elapsed: {elapsed}ms");

                // 延迟期间不绘制 (Don't draw during delay period)
                if(elapsed < C.DelayMs)
                {
                    _drawDebugLog.Add($"等待延迟 (Waiting for delay)... ({elapsed}/{C.DelayMs})");
                    HideAllElements();
                    return;
                }

                // 超过持续时间后清空执行时间 (Clear execute time after duration exceeded)
                if(elapsed > C.DelayMs + C.DurationMs)
                {
                    _drawDebugLog.Add($"超时，重置 (Timeout, reset)");
                    _executeTime = 0;
                    HideAllElements();
                    return;
                }
            }

            // 绘制所有记录的分身AOE (Draw all recorded clone AOEs)
            int drawnCount = 0;
            for(int i = 0; i < _cloneInfos.Count && i < 8; i++)
            {
                var clone = _cloneInfos[i];
                _drawDebugLog.Add($"--- 分身 (Clone) {i}: ActionId={clone.ActionId}, Pos=({clone.Position.X:F1},{clone.Position.Z:F1}), Rot={clone.Rotation:F2}");

                switch(clone.ActionId)
                {
                    case CloneCircle:
                        if(Controller.TryGetElementByName($"Circle_{i}", out var circle))
                        {
                            circle.Enabled = true;
                            circle.SetRefPosition(clone.Position);
                            circle.color = C.ColorDanger;
                            drawnCount++;
                            _drawDebugLog.Add($"  ✓ Circle_{i} 已启用 (Enabled)");
                        }
                        else
                        {
                            _drawDebugLog.Add($"  ✗ Circle_{i} 获取失败 (Failed to get)!");
                        }
                        break;

                    case CloneLeftRight:
                        bool lrRightOk = false, lrLeftOk = false;
                        if(Controller.TryGetElementByName($"LeftRight_Right_{i}", out var lrRight))
                        {
                            lrRight.Enabled = true;
                            lrRight.SetRefPosition(clone.Position);
                            lrRight.AdditionalRotation = clone.Rotation;
                            lrRight.color = C.ColorDanger;
                            lrRightOk = true;
                            drawnCount++;
                        }
                        if(Controller.TryGetElementByName($"LeftRight_Left_{i}", out var lrLeft))
                        {
                            lrLeft.Enabled = true;
                            lrLeft.SetRefPosition(clone.Position);
                            lrLeft.AdditionalRotation = clone.Rotation;
                            lrLeft.color = C.ColorDanger;
                            lrLeftOk = true;
                            drawnCount++;
                        }
                        _drawDebugLog.Add($"  {(lrRightOk ? "✓" : "✗")} LeftRight_Right_{i}, {(lrLeftOk ? "✓" : "✗")} LeftRight_Left_{i}");
                        break;

                    case CloneFrontBack:
                        bool fbFrontOk = false, fbBackOk = false;
                        if(Controller.TryGetElementByName($"FrontBack_Front_{i}", out var fbFront))
                        {
                            fbFront.Enabled = true;
                            fbFront.SetRefPosition(clone.Position);
                            fbFront.AdditionalRotation = clone.Rotation;
                            fbFront.color = C.ColorDanger;
                            fbFrontOk = true;
                            drawnCount++;
                        }
                        if(Controller.TryGetElementByName($"FrontBack_Back_{i}", out var fbBack))
                        {
                            fbBack.Enabled = true;
                            fbBack.SetRefPosition(clone.Position);
                            fbBack.AdditionalRotation = clone.Rotation;
                            fbBack.color = C.ColorDanger;
                            fbBackOk = true;
                            drawnCount++;
                        }
                        _drawDebugLog.Add($"  {(fbFrontOk ? "✓" : "✗")} FrontBack_Front_{i}, {(fbBackOk ? "✓" : "✗")} FrontBack_Back_{i}");
                        break;

                    default:
                        _drawDebugLog.Add($"  ✗ 未知ActionId (Unknown ActionId): {clone.ActionId}");
                        break;
                }
            }

            _drawDebugLog.Add($"=== 总共绘制了 (Total drawn) {drawnCount} 个元素 (elements) ===");

            // 隐藏未使用的元素 (Hide unused elements)
            for(int i = _cloneInfos.Count; i < 8; i++)
            {
                if(Controller.TryGetElementByName($"Circle_{i}", out var c)) c.Enabled = false;
                if(Controller.TryGetElementByName($"LeftRight_Right_{i}", out var lr)) lr.Enabled = false;
                if(Controller.TryGetElementByName($"LeftRight_Left_{i}", out var ll)) ll.Enabled = false;
                if(Controller.TryGetElementByName($"FrontBack_Front_{i}", out var ff)) ff.Enabled = false;
                if(Controller.TryGetElementByName($"FrontBack_Back_{i}", out var fb)) fb.Enabled = false;
            }
        }
        else
        {
            // 不在绘制窗口：隐藏所有元素 (Not in drawing window: Hide all elements)
            HideAllElements();
        }

        // 第二轮AOE绘制 (第7次心象投影触发，延迟1秒，持续5秒) (Second round AOE drawing - triggered by 7th Mental Projection, delay 1 sec, duration 5 sec)
        if(_secondRoundDrawTime > 0)
        {
            var now = Environment.TickCount64;
            if(now >= _secondRoundDrawTime && now < _secondRoundDrawTime + 7000)
            {
                for(int i = 0; i < _secondRoundCloneInfos.Count && i < 8; i++)
                {
                    var clone = _secondRoundCloneInfos[i];
                    switch(clone.ActionId)
                    {
                        case CloneCircle:
                        case CloneCircle2:
                            if(Controller.TryGetElementByName($"Circle_{i}", out var circle))
                            {
                                circle.Enabled = true;
                                circle.SetRefPosition(clone.Position);
                                circle.color = C.ColorDanger;
                            }
                            break;
                        case CloneLeftRight:
                            if(Controller.TryGetElementByName($"LeftRight_Right_{i}", out var lrRight))
                            {
                                lrRight.Enabled = true;
                                lrRight.SetRefPosition(clone.Position);
                                lrRight.color = C.ColorDanger;
                            }
                            if(Controller.TryGetElementByName($"LeftRight_Left_{i}", out var lrLeft))
                            {
                                lrLeft.Enabled = true;
                                lrLeft.SetRefPosition(clone.Position);
                                lrLeft.color = C.ColorDanger;
                            }
                            break;
                        case CloneFrontBack:
                            if(Controller.TryGetElementByName($"FrontBack_Front_{i}", out var fbFront))
                            {
                                fbFront.Enabled = true;
                                fbFront.SetRefPosition(clone.Position);
                                fbFront.color = C.ColorDanger;
                            }
                            if(Controller.TryGetElementByName($"FrontBack_Back_{i}", out var fbBack))
                            {
                                fbBack.Enabled = true;
                                fbBack.SetRefPosition(clone.Position);
                                fbBack.color = C.ColorDanger;
                            }
                            break;
                    }
                }
            }
            else if(now >= _secondRoundDrawTime + 7000)
            {
                _secondRoundDrawTime = 0;
                HideAllElements();
            }
        }

        // 第三轮: 缺少的刀绘制 (Third round: Missing cleave drawing)
        if(_thirdRoundDrawTime > 0 && _missingBladeType != 0)
        {
            var now = Environment.TickCount64;
            if(now >= _thirdRoundDrawTime && now < _thirdRoundDrawTime + C.MissingBladeDurationMs)
            {
                // 绘制缺少的刀 (使用索引7，避免与其他元素冲突) (Draw missing cleave - use index 7 to avoid conflict with other elements)
                if(_missingBladeType == CloneLeftRight)
                {
                    if(Controller.TryGetElementByName("LeftRight_Right_7", out var lrRight))
                    {
                        lrRight.Enabled = true;
                        lrRight.SetRefPosition(ThirdRoundPosition);
                        lrRight.color = C.ColorDanger;
                    }
                    if(Controller.TryGetElementByName("LeftRight_Left_7", out var lrLeft))
                    {
                        lrLeft.Enabled = true;
                        lrLeft.SetRefPosition(ThirdRoundPosition);
                        lrLeft.color = C.ColorDanger;
                    }
                }
                else if(_missingBladeType == CloneFrontBack)
                {
                    if(Controller.TryGetElementByName("FrontBack_Front_7", out var fbFront))
                    {
                        fbFront.Enabled = true;
                        fbFront.SetRefPosition(ThirdRoundPosition);
                        fbFront.color = C.ColorDanger;
                    }
                    if(Controller.TryGetElementByName("FrontBack_Back_7", out var fbBack))
                    {
                        fbBack.Enabled = true;
                        fbBack.SetRefPosition(ThirdRoundPosition);
                        fbBack.color = C.ColorDanger;
                    }
                }
            }
            else if(now >= _thirdRoundDrawTime + C.MissingBladeDurationMs)
            {
                _thirdRoundDrawTime = 0;
                HideAllElements();
            }
        }

        // 第6次心象投影: 19210大圈绘制 (时空重现触发，延迟0秒，持续5秒) (6th Mental Projection: 19210 large circle drawing - triggered by Time Warp, delay 0 sec, duration 5 sec)
        if(_sixthRoundDrawTime > 0)
        {
            var now = Environment.TickCount64;
            if(now >= _sixthRoundDrawTime && now < _sixthRoundDrawTime + 5000)
            {
                for(int i = 0; i < _sixthRoundCirclePositions.Count && i < 2; i++)
                {
                    if(Controller.TryGetElementByName($"SpawnCircle_{i}", out var circle))
                    {
                        circle.Enabled = true;
                        circle.SetRefPosition(_sixthRoundCirclePositions[i]);
                        circle.color = C.ColorDanger;
                    }
                }
            }
            else if(now >= _sixthRoundDrawTime + 5000)
            {
                _sixthRoundDrawTime = 0;
                for(int i = 0; i < 2; i++)
                    if(Controller.TryGetElementByName($"SpawnCircle_{i}", out var c)) c.Enabled = false;
            }
        }

        // 第8次心象投影: 19210大圈绘制 (8th Mental Projection: 19210 large circle drawing)
        if(_eighthRoundDrawTime > 0)
        {
            var now = Environment.TickCount64;
            if(now >= _eighthRoundDrawTime && now < _eighthRoundDrawTime + C.EighthCircleDurationMs)
            {
                for(int i = 0; i < _eighthRoundCirclePositions.Count && i < 2; i++)
                {
                    if(Controller.TryGetElementByName($"SpawnCircle_{i}", out var circle))
                    {
                        circle.Enabled = true;
                        circle.SetRefPosition(_eighthRoundCirclePositions[i]);
                        circle.color = C.ColorDanger;
                    }
                }
            }
            else if(now >= _eighthRoundDrawTime + C.EighthCircleDurationMs)
            {
                _eighthRoundDrawTime = 0;
                for(int i = 0; i < 2; i++)
                    if(Controller.TryGetElementByName($"SpawnCircle_{i}", out var c)) c.Enabled = false;
            }
        }

        // 阶段3: 引导绘制 (Phase 3: Guidance drawing)
        if(C.EnableGuidance && _currentPhase == 3 && _guidanceSequence.Count == 4)
        {
            var now = Environment.TickCount64;

            // 等待延迟时间过去 (Wait for delay time to pass)
            if(now < _guidanceStartTime)
            {
                // 延迟期间隐藏引导元素 (Hide guidance elements during delay)
                HideGuidanceElements();
                return;
            }

            var elapsed = now - _guidanceStartTime;

            // 计算当前轮次 (第一轮8秒，后续轮6秒) (Calculate current round - first round 8 sec, subsequent rounds 6 sec)
            // 轮次0: 0-8000ms, 轮次1: 8000-14000ms, 轮次2: 14000-20000ms, 轮次3: 20000-26000ms (Round 0: 0-8000ms, Round 1: 8000-14000ms, Round 2: 14000-20000ms, Round 3: 20000-26000ms)
            if(elapsed < FirstRoundDurationMs)
                _currentGuidanceRound = 0;
            else if(elapsed < FirstRoundDurationMs + OtherRoundDurationMs)
                _currentGuidanceRound = 1;
            else if(elapsed < FirstRoundDurationMs + OtherRoundDurationMs * 2)
                _currentGuidanceRound = 2;
            else
                _currentGuidanceRound = 3;

            // 获取当前轮次的动作 (Get current round's action)
            var currentAction = _guidanceSequence[_currentGuidanceRound];

            // 绘制站位点 (Draw position points)
            DrawGuidanceElements(currentAction, _currentGuidanceRound);

            // 绘制连线指路 (Draw tether guidance)
            DrawPlayerTethers(currentAction, _currentGuidanceRound);

            // 4轮结束后隐藏 (总时长: 8 + 6*3 = 26秒) (Hide after 4 rounds end - total duration: 8 + 6*3 = 26 sec)
            var totalDuration = FirstRoundDurationMs + OtherRoundDurationMs * 3;
            if(_currentGuidanceRound >= 3 && elapsed > totalDuration)
            {
                HideGuidanceElements();
                _currentPhase = 0; // 重置阶段 (Reset phase)
            }
        }
        else if(_currentPhase != 3)
        {
            // 非阶段3时隐藏引导元素 (Hide guidance elements when not in Phase 3)
            HideGuidanceElements();
        }
    }

    private void HideAllElements()
    {
        for(int i = 0; i < 8; i++)
        {
            if(Controller.TryGetElementByName($"Circle_{i}", out var c)) c.Enabled = false;
            if(Controller.TryGetElementByName($"LeftRight_Right_{i}", out var lr)) lr.Enabled = false;
            if(Controller.TryGetElementByName($"LeftRight_Left_{i}", out var ll)) ll.Enabled = false;
            if(Controller.TryGetElementByName($"FrontBack_Front_{i}", out var ff)) ff.Enabled = false;
            if(Controller.TryGetElementByName($"FrontBack_Back_{i}", out var fb)) fb.Enabled = false;
        }
        for(int i = 0; i < 2; i++)
        {
            if(Controller.TryGetElementByName($"SpawnCircle_{i}", out var sc)) sc.Enabled = false;
        }
    }

    public override void OnReset()
    {
        _isMechanicActive = false;
        _firstSpawnPositions.Clear();
        _cloneInfos.Clear();
        _connectionType = "";
        _frontBackClonePos = Vector3.Zero;
        _executeTime = 0;
        _manualDrawTest = false;
        _drawDebugLog.Clear();
        _firstSpawnType = "";
        _projectionCastCount = 0; // 重置心象投影计数器 (Reset Mental Projection counter)
        _round1Tethers.Clear();
        _round2VfxTypes.Clear();
        _round2Tethers.Clear();
        // 阶段3引导相关 (Phase 3 guidance related)
        _currentPhase = 0;
        _guidanceSequence.Clear();
        _roundPoints.Clear();
        _currentGuidanceRound = 0;
        _lastLoggedRound = -1; // 重置日志轮次 (Reset log round)
        _guidanceStartTime = 0;
        _playerGroups.Clear();
        _playerPoints.Clear();
        // 第二轮AOE (Second round AOE)
        _secondRoundCloneInfos.Clear();
        _recordingSecondRound = false;
        _secondRoundDrawTime = 0;
        // 第三轮 (Third round)
        _missingBladeType = 0;
        _fifthRoundBladeType = 0;
        _thirdRoundDrawTime = 0;
        // 第6/8次19210大圈 (6th/8th 19210 large circles)
        _sixthRoundDrawTime = 0;
        _eighthRoundDrawTime = 0;
        _sixthRoundCirclePositions.Clear();
        _eighthRoundCirclePositions.Clear();
        _waitingForTimeWarp = false;
        _fifthRoundStandDrawTime = 0;
    }

    private void AddLog(string entry)
    {
        _eventLog.Insert(0, entry);
        while(_eventLog.Count > MaxLogEntries)
            _eventLog.RemoveAt(_eventLog.Count - 1);
    }

    private static string GetPointType(Vector3 pos)
    {
        // 判断是正点还是斜点 (Determine if cardinal or intercardinal)
        // 游戏坐标系: X是东西, Z是南北 (根据实际标点坐标) (Game coordinate system: X is East/West, Z is North/South - based on actual waymark coordinates)
        // 标点坐标 (4A1顺序): (Waymark coordinates - 4A1 order)
        // A(北 North): X=99.9, Z=88.96
        // B(东 East): X=110.89, Z=99.74
        // C(南 South): X=100.10, Z=110.90
        // D(西 West): X=89.11, Z=99.93
        // 1(东北 Northeast): X=108.28, Z=91.70
        // 2(东南 Southeast): X=108.43, Z=108.10
        // 3(西南 Southwest): X=91.80, Z=108.17
        // 4(西北 Northwest): X=91.62, Z=91.75

        var x = pos.X;
        var z = pos.Z;  // Z是南北轴 (Z is North/South axis)
        var center = 100f;
        var threshold = 5f;

        // 检查是否在正点轴上 (Check if on cardinal axis)
        bool onXAxis = Math.Abs(x - center) < threshold;  // X≈100 -> A或C点 (A or C point)
        bool onZAxis = Math.Abs(z - center) < threshold;  // Z≈100 -> B或D点 (B or D point)

        // A(北 North): X≈100, Z<100
        if(onXAxis && z < center) return "[正点 (Cardinal)-A(北 North)]";
        // C(南 South): X≈100, Z>100
        if(onXAxis && z > center) return "[正点 (Cardinal)-C(南 South)]";
        // B(东 East): Z≈100, X>100
        if(onZAxis && x > center) return "[正点 (Cardinal)-B(东 East)]";
        // D(西 West): Z≈100, X<100
        if(onZAxis && x < center) return "[正点 (Cardinal)-D(西 West)]";

        // 斜点判断 (4A1顺序) (Intercardinal determination - 4A1 order)
        // 4(西北 Northwest): X<100, Z<100
        if(x < center && z < center) return "[斜点 (Intercardinal)-4(西北 Northwest)]";
        // 1(东北 Northeast): X>100, Z<100
        if(x > center && z < center) return "[斜点 (Intercardinal)-1(东北 Northeast)]";
        // 2(东南 Southeast): X>100, Z>100
        if(x > center && z > center) return "[斜点 (Intercardinal)-2(东南 Southeast)]";
        // 3(西南 Southwest): X<100, Z>100
        if(x < center && z > center) return "[斜点 (Intercardinal)-3(西南 Southwest)]";

        return "[未知位置 (Unknown Position)]";
    }

    private static string GetPointName(Vector3 pos)
    {
        // 返回简化的标点名 (A, B, C, D, 1, 2, 3, 4) - 4A1顺序 (Return simplified waymark name - 4A1 order)
        var x = pos.X;
        var z = pos.Z;
        var center = 100f;
        var threshold = 5f;

        bool onXAxis = Math.Abs(x - center) < threshold;
        bool onZAxis = Math.Abs(z - center) < threshold;

        // 正点 (Cardinal)
        if(onXAxis && z < center) return "A";
        if(onXAxis && z > center) return "C";
        if(onZAxis && x > center) return "B";
        if(onZAxis && x < center) return "D";

        // 斜点 (4A1顺序) (Intercardinal - 4A1 order)
        if(x < center && z < center) return "4";  // 西北 (Northwest)
        if(x > center && z < center) return "1";  // 东北 (Northeast)
        if(x > center && z > center) return "2";  // 东南 (Southeast)
        if(x < center && z > center) return "3";  // 西南 (Southwest)

        return "?";
    }

    private string GetPlayerRole(IPlayerCharacter player)
    {
        // 获取队伍成员并按职能排序 (Get party members and sort by role)
        var party = FakeParty.Get().ToList();

        // 分类 (Categorize)
        var tanks = party.Where(p => p.GetRole() == CombatRole.Tank).OrderBy(p => GetJobPriority(p)).ToList();
        var healers = party.Where(p => p.GetRole() == CombatRole.Healer).OrderBy(p => GetJobPriority(p)).ToList();
        var dps = party.Where(p => p.GetRole() == CombatRole.DPS).OrderBy(p => GetJobPriority(p)).ToList();

        // 判断玩家职能 (Determine player role)
        var playerEntityId = player.EntityId;

        // 坦克: MT, ST (Tanks: MT, ST)
        for(int i = 0; i < tanks.Count; i++)
        {
            if(tanks[i].EntityId == playerEntityId)
                return i == 0 ? "MT" : "ST";
        }

        // 治疗: H1, H2 (Healers: H1, H2)
        for(int i = 0; i < healers.Count; i++)
        {
            if(healers[i].EntityId == playerEntityId)
                return $"H{i + 1}";
        }

        // DPS: D1, D2, D3, D4
        for(int i = 0; i < dps.Count; i++)
        {
            if(dps[i].EntityId == playerEntityId)
                return $"D{i + 1}";
        }

        return "未知 (Unknown)";
    }

    private static int GetJobPriority(IPlayerCharacter player)
    {
        // 职业优先级排序 (用于确定MT/ST, H1/H2, D1-D4) (Job priority sorting - for determining MT/ST, H1/H2, D1-D4)
        // 坦克 (Tanks): PLD > WAR > DRK > GNB
        // 治疗 (Healers): WHM > AST > SCH > SGE
        // DPS: 近战 (Melee) > 远程物理 (Ranged Physical) > 法系 (Caster)
        var job = player.GetJob();
        return job switch
        {
            // 坦克 (Tanks)
            Job.PLD => 1,
            Job.WAR => 2,
            Job.DRK => 3,
            Job.GNB => 4,
            // 治疗 (Healers)
            Job.WHM => 1,
            Job.AST => 2,
            Job.SCH => 3,
            Job.SGE => 4,
            // 近战DPS (Melee DPS)
            Job.MNK => 1,
            Job.DRG => 2,
            Job.NIN => 3,
            Job.SAM => 4,
            Job.RPR => 5,
            Job.VPR => 6,
            // 远程物理DPS (Ranged Physical DPS)
            Job.BRD => 10,
            Job.MCH => 11,
            Job.DNC => 12,
            // 法系DPS (Caster DPS)
            Job.BLM => 20,
            Job.SMN => 21,
            Job.RDM => 22,
            Job.PCT => 23,
            _ => 99
        };
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("M12S 四运 (Four Movements) 绘制设置 (Drawing Settings)");

        var enableDraw = C.EnableDraw;
        if(ImGui.Checkbox("启用AOE绘制 (Enable AOE Drawing)", ref enableDraw))
            C.EnableDraw = enableDraw;

        var enableGuidance = C.EnableGuidance;
        if(ImGui.Checkbox("启用指路绘制 (Enable Guidance Drawing)", ref enableGuidance))
            C.EnableGuidance = enableGuidance;

        ImGui.Separator();
        ImGui.Text("延迟设置 (Delay Settings):");

        var delay = C.DelayMs;
        if(ImGui.SliderInt("AOE延迟绘制(ms) (AOE Drawing Delay)", ref delay, 0, 5000))
            C.DelayMs = delay;

        var duration = C.DurationMs;
        if(ImGui.SliderInt("AOE持续时间(ms) (AOE Duration)", ref duration, 1000, 10000))
            C.DurationMs = duration;

        var guidanceDelay = C.GuidanceDelayMs;
        if(ImGui.SliderInt("引导延迟(ms) (Guidance Delay)", ref guidanceDelay, 0, 5000))
            C.GuidanceDelayMs = guidanceDelay;

        var eighthCircleDelay = C.EighthCircleDelayMs;
        if(ImGui.SliderInt("第8次大圈延迟(ms) (8th Circle Delay)", ref eighthCircleDelay, 0, 15000))
            C.EighthCircleDelayMs = eighthCircleDelay;

        var eighthCircleDuration = C.EighthCircleDurationMs;
        if(ImGui.SliderInt("第8次大圈持续(ms) (8th Circle Duration)", ref eighthCircleDuration, 1000, 15000))
            C.EighthCircleDurationMs = eighthCircleDuration;

        var missingBladeDelay = C.MissingBladeDelayMs;
        if(ImGui.SliderInt("缺少的刀延迟(ms) (Missing Cleave Delay)", ref missingBladeDelay, 0, 20000))
            C.MissingBladeDelayMs = missingBladeDelay;

        var missingBladeDuration = C.MissingBladeDurationMs;
        if(ImGui.SliderInt("缺少的刀持续(ms) (Missing Cleave Duration)", ref missingBladeDuration, 1000, 15000))
            C.MissingBladeDurationMs = missingBladeDuration;

        var fifthStandDelay = C.FifthStandDelayMs;
        if(ImGui.SliderInt("小世界站位延迟(ms) (Small World Position Delay)", ref fifthStandDelay, 0, 15000))
            C.FifthStandDelayMs = fifthStandDelay;

        var fifthStandDuration = C.FifthStandDurationMs;
        if(ImGui.SliderInt("小世界站位持续(ms) (Small World Position Duration)", ref fifthStandDuration, 1000, 15000))
            C.FifthStandDurationMs = fifthStandDuration;

        var color = C.ColorDanger.ToVector4();
        if(ImGui.ColorEdit4("AOE颜色 (AOE Color)", ref color))
            C.ColorDanger = color.ToUint();

        ImGui.Separator();
        var debugTether = C.DebugTetherAll;
        if(ImGui.Checkbox("Debug模式 (连线所有玩家) (Debug Mode - Tether All Players)", ref debugTether))
            C.DebugTetherAll = debugTether;

        if(ImGui.Button("保存配置 (Save Config)"))
            Controller.SaveConfig();

        // ========== Debug内容 (仅在Debug模式下显示) (Debug Content - Only shown in Debug mode) ==========
        if(!C.DebugTetherAll) return;

        ImGui.Separator();
        ImGuiEx.Text(EColor.YellowBright, "===== Debug信息 (Debug Info) =====");

        ImGui.Text($"Boss DataID: {BossDataId}");
        ImGui.Text($"分身 (Clone) DataID: {CloneDataId}");
        ImGui.Text($"先刷新 (First Spawn) DataID: {FirstSpawnId}");

        ImGui.Separator();
        ImGui.Text("技能ID (Skill IDs):");
        ImGui.Text($"  镜中奇梦(开始) (Mirror Dreams - Start): {MirrorCastId}");
        ImGui.Text($"  自我复制 (Self Copy): {SelfCopyCastId}");
        ImGui.Text($"  执行(绘制) (Execute - Drawing): {ExecuteCastId}");
        ImGui.Text($"  心象投影(引导触发) (Mental Projection - Guidance Trigger): {ProjectionCastId}");
        ImGui.Text($"  左右双刀 (Left/Right Cleave): {CloneLeftRight}");
        ImGui.Text($"  前后双刀 (Front/Back Cleave): {CloneFrontBack}");
        ImGui.Text($"  圆形AOE (Circle AOE): {CloneCircle}");

        ImGui.Separator();
        ImGuiEx.Text($"四运激活 (Four Movements Active): {_isMechanicActive}");
        ImGuiEx.Text($"先出现 (First Appeared): {(_firstSpawnType == "" ? "未检测 (Not detected)" : _firstSpawnType)}");
        ImGuiEx.Text($"心象投影读条 (Mental Projection Casts): {_projectionCastCount}/4");
        ImGuiEx.Text($"连线类型 (Tether Type): {_connectionType}");
        ImGuiEx.Text($"前后刀位置 (Front/Back Cleave Position): ({_frontBackClonePos.X:F1},{_frontBackClonePos.Z:F1})");

        var now = Environment.TickCount64;
        if(_executeTime > 0)
        {
            var elapsed = now - _executeTime;
            var state = elapsed < C.DelayMs ? "等待中 (Waiting)" :
                       elapsed < C.DelayMs + C.DurationMs ? "绘制中 (Drawing)" : "已结束 (Ended)";
            ImGuiEx.Text($"绘制状态 (Drawing State): {state} ({elapsed}ms)");
        }

        ImGui.Separator();
        ImGui.Text($"19210位置 (Positions) ({_firstSpawnPositions.Count}):");
        foreach(var (entityId, pos, pointType) in _firstSpawnPositions)
        {
            ImGuiEx.Text($"  ({pos.X:F1},{pos.Z:F1}) {pointType}");
        }

        ImGui.Separator();
        ImGui.Text($"分身技能 (Clone Skills) ({_cloneInfos.Count}):");
        foreach(var clone in _cloneInfos)
        {
            string actionName = clone.ActionId switch
            {
                CloneLeftRight => "左右双刀 (Left/Right Cleave)",
                CloneFrontBack => "前后双刀 (Front/Back Cleave)",
                CloneCircle => "圆形AOE (Circle AOE)",
                _ => $"未知 (Unknown)({clone.ActionId})"
            };
            ImGuiEx.Text($"  {actionName} ({clone.Position.X:F1},{clone.Position.Z:F1}) R:{clone.Rotation:F2}");
        }

        ImGui.Separator();
        ImGui.Text($"第一阶段连线 (Phase 1 Tethers) ({_round1Tethers.Count}):");
        if(_round1Tethers.Count > 0)
        {
            var pointOrder = new[] { "A", "B", "C", "D", "1", "2", "3", "4" };
            foreach(var point in pointOrder)
            {
                if(_round1Tethers.TryGetValue(point, out var playerName))
                {
                    ImGuiEx.Text(EColor.PurpleBright, $"  {point}点小怪 (point mob)——>{playerName}");
                }
            }
        }

        ImGui.Separator();
        ImGui.Text($"第二阶段连线 (Phase 2 Tethers) ({_round2Tethers.Count}):");
        if(_round2Tethers.Count > 0)
        {
            var pointOrder = new[] { "A", "B", "C", "D", "1", "2", "3", "4" };
            foreach(var point in pointOrder)
            {
                if(_round2Tethers.TryGetValue(point, out var info))
                {
                    var displayColor = info.VfxType == "分摊" ? EColor.CyanBright : EColor.OrangeBright;
                    ImGuiEx.Text(displayColor, $"  {point}点【{info.VfxType}】——>{info.PlayerName}");
                }
            }
        }

        ImGui.Separator();
        ImGui.Text("引导状态 (Guidance State):");
        ImGuiEx.Text($"当前阶段 (Current Phase): {_currentPhase} (0=未开始 Not started, 1=连线 Tether, 2=VFX, 3=引导中 Guiding)");
        if(_guidanceSequence.Count == 4 && _roundPoints.Count == 4)
        {
            var pointsDisplay = string.Join(" → ", _roundPoints.Select(p => $"[{string.Join(",", p)}]"));
            ImGuiEx.Text($"标点顺序 (Waymark Order): {pointsDisplay}");
            ImGuiEx.Text(EColor.YellowBright, $"VFX顺序 (VFX Order): {string.Join(" → ", _guidanceSequence)}");

            if(_currentPhase == 3)
            {
                var currentTime = Environment.TickCount64;
                if(currentTime < _guidanceStartTime)
                {
                    var delayRemaining = _guidanceStartTime - currentTime;
                    ImGuiEx.Text(EColor.YellowBright, $"引导延迟 (Guidance Delay): {delayRemaining / 1000f:F1}秒后开始 (sec until start)");
                }
                else
                {
                    var elapsed = currentTime - _guidanceStartTime;
                    long nextRoundIn;
                    if(_currentGuidanceRound == 0)
                        nextRoundIn = FirstRoundDurationMs - elapsed;
                    else if(_currentGuidanceRound == 1)
                        nextRoundIn = FirstRoundDurationMs + OtherRoundDurationMs - elapsed;
                    else if(_currentGuidanceRound == 2)
                        nextRoundIn = FirstRoundDurationMs + OtherRoundDurationMs * 2 - elapsed;
                    else
                        nextRoundIn = FirstRoundDurationMs + OtherRoundDurationMs * 3 - elapsed;

                    var currentPoints = _roundPoints[_currentGuidanceRound];
                    var currentAction = _guidanceSequence[_currentGuidanceRound];
                    var durationText = _currentGuidanceRound == 0 ? "8秒 (8 sec)" : "6秒 (6 sec)";
                    ImGuiEx.Text(EColor.GreenBright, $"当前轮次 (Current Round): 第 (Round){_currentGuidanceRound + 1}轮 ({durationText}) - [{string.Join(",", currentPoints)}] - {currentAction}");
                    ImGuiEx.Text($"下一轮倒计时 (Next Round Countdown): {Math.Max(0, nextRoundIn) / 1000f:F1}秒 (sec)");

                    if(currentAction.Contains("大圈"))
                    {
                        var spreadPlayers = new List<string>();
                        foreach(var point in currentPoints)
                        {
                            if(_round2Tethers.TryGetValue(point, out var info))
                                spreadPlayers.Add($"{point}:{info.PlayerName}");
                        }
                        ImGuiEx.Text(EColor.OrangeBright, $"  需要出去放圈 (Need to go out for spread): {string.Join(", ", spreadPlayers)}");
                    }
                    else
                    {
                        ImGuiEx.Text(EColor.CyanBright, $"  本轮分摊，大家待命 (This round is stack, everyone standby)");
                    }
                }
            }
        }

        if(_playerGroups.Count > 0)
        {
            ImGui.Separator();
            ImGui.Text("玩家分组 (Player Groups):");
            var rightGroup = _playerGroups.Where(x => x.Value.Contains("右组")).Select(x => x.Key).ToList();
            var leftGroup = _playerGroups.Where(x => x.Value.Contains("左组")).Select(x => x.Key).ToList();
            ImGuiEx.Text(EColor.CyanBright, $"  右组 (Right Group)(A1B2): {string.Join(", ", rightGroup)}");
            ImGuiEx.Text(EColor.OrangeBright, $"  左组 (Left Group)(C3D4): {string.Join(", ", leftGroup)}");
        }

        ImGui.Separator();
        if(ImGui.Button("清除日志 (Clear Log)"))
            _eventLog.Clear();
        ImGui.SameLine();
        if(ImGui.Button("重置状态 (Reset State)"))
            OnReset();
        ImGui.SameLine();
        if(ImGui.Button("打印19210位置 (Print 19210 Positions)"))
        {
            var spawns = Svc.Objects.OfType<IBattleNpc>()
                .Where(x => x.BaseId == FirstSpawnId && x.IsCharacterVisible())
                .ToList();
            AddLog($"[Debug] === 当前可见19210列表 (Current Visible 19210 List) ({spawns.Count}个) ===");
            foreach(var spawn in spawns)
            {
                var pointName = GetPointName(spawn.Position);
                AddLog($"  [{pointName}] Pos:({spawn.Position.X:F2}, {spawn.Position.Z:F2}) EntityId:{spawn.EntityId}");
            }
            if(spawns.Count == 0)
                AddLog("  (未找到任何可见的19210)");
        }

        ImGui.Separator();
        ImGui.Text("测试功能 (Test Functions):");
        if(ImGui.Button(_manualDrawTest ? "停止测试绘制 (Stop Test Drawing)" : "开始测试绘制 (Start Test Drawing)"))
        {
            _manualDrawTest = !_manualDrawTest;
            AddLog(_manualDrawTest ? "[测试 (Test)] 手动开启绘制测试 (Manually started drawing test)" : "[测试 (Test)] 停止绘制测试 (Stopped drawing test)");
        }
        ImGui.SameLine();
        ImGuiEx.Text(_manualDrawTest ? EColor.GreenBright : EColor.RedBright,
            _manualDrawTest ? "● 测试中 (Testing)" : "○ 未测试 (Not Testing)");

        if(_drawDebugLog.Count > 0)
        {
            ImGui.Separator();
            ImGui.Text("绘制调试信息 (Drawing Debug Info):");
            ImGui.BeginChild("DrawDebug", new System.Numerics.Vector2(0, 200), true);
            foreach(var log in _drawDebugLog)
            {
                if(log.Contains("✓"))
                    ImGuiEx.Text(EColor.GreenBright, log);
                else if(log.Contains("✗"))
                    ImGuiEx.Text(EColor.RedBright, log);
                else if(log.Contains("==="))
                    ImGuiEx.Text(EColor.YellowBright, log);
                else
                    ImGui.Text(log);
            }
            ImGui.EndChild();
        }

        ImGui.Separator();
        ImGui.Text($"事件日志 (Event Log) ({_eventLog.Count}):");
        ImGui.BeginChild("EventLog", new System.Numerics.Vector2(0, 400), true);
        foreach(var log in _eventLog)
        {
            if(log.Contains("四运开始"))
                ImGuiEx.Text(EColor.GreenBright, log);
            else if(log.Contains("分身技能") || log.Contains("执行读条"))
                ImGuiEx.Text(EColor.YellowBright, log);
            else if(log.Contains("VFX"))
                ImGuiEx.Text(EColor.CyanBright, log);
            else if(log.Contains("Tether"))
                ImGuiEx.Text(EColor.OrangeBright, log);
            else if(log.Contains("19210"))
                ImGuiEx.Text(EColor.PurpleBright, log);
            else
                ImGui.Text(log);
        }
        ImGui.EndChild();
    }
}
