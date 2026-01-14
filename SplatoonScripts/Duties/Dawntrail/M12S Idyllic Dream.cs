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

    public override Metadata? Metadata => new(1, "Errer");

    #region 常量

    // Boss和分身ID
    private const uint BossDataId = 19202;      // 主Boss
    private const uint CloneDataId = 19204;     // 分身 (存储技能)
    private const uint FirstSpawnId = 19210;    // 先刷新的4个

    // 技能ID
    private const uint MirrorCastId = 46345;    // 镜中奇梦 - 四运开始
    private const uint SelfCopyCastId = 46296;  // 自我复制
    private const uint ExecuteCastId = 48089;   // Boss执行 - 触发绘制
    private const uint ProjectionCastId = 48098; // 心象投影 - 第4次触发引导
    private const uint TimeWarpCastId = 46316;  // 时空重现 - 触发第6次大圈绘制

    // 分身技能
    private const uint CloneLeftRight = 46351;  // 面相左右双刀
    private const uint CloneCircle = 46353;     // 脚下圆形AOE

    private const uint CloneCircle2 = 48303;     // 脚下圆形AOE
    private const uint CloneFrontBack = 46352;  // 面相前后双刀

    // VFX
    private const string VfxShare = "vfx/channeling/eff/chn_x6rc_fr_share01x.avfx";  // 四人分摊
    private const string VfxCircle = "vfx/channeling/eff/chn_x6rc_fr_tgae01x.avfx";  // 大圈
    private const string VfxTether = "vfx/channeling/eff/chn_tergetfix1f.avfx";      // 连线VFX (19210)

    // 属性头标
    // 2015013 风+雷, 2015014 暗+雷, 2015015 土, 2015016 火

    #endregion

    #region 状态变量

    private record struct CloneInfo(uint ActionId, Vector3 Position, float Rotation);

    private readonly List<string> _eventLog = new();
    private const int MaxLogEntries = 100;

    // 四运状态
    private bool _isMechanicActive = false;
    private readonly List<(uint EntityId, Vector3 Position, string PointType)> _firstSpawnPositions = new();
    private readonly List<CloneInfo> _cloneInfos = new();
    private string _connectionType = ""; // 正点分摊 or 斜点分摊
    private Vector3 _frontBackClonePos = Vector3.Zero; // 前后刀分身位置
    private long _executeTime = 0;
    private bool _manualDrawTest = false; // 手动测试绘制
    private readonly List<string> _drawDebugLog = new(); // 绘制调试日志
    private string _firstSpawnType = ""; // 先出现的是正点还是斜点 ("正点" / "斜点")

    // 心象投影读条计数器
    private int _projectionCastCount = 0;

    // 玩家连线追踪 (职能, 连线类型) - 保留record struct用于其他用途
    private record struct PlayerTetherInfo(string PlayerName, string Role, string TetherType, string PointType);

    // 第一阶段: 连线检测 - 标点→玩家名 (data3=368)
    private readonly Dictionary<string, string> _round1Tethers = new();

    // 第二阶段: VFX类型检测 - 标点→VFX类型 (分摊/大圈) - 由VfxShare/VfxCircle填充
    private readonly Dictionary<string, string> _round2VfxTypes = new();

    // 第二阶段: 连线检测 - 标点→(玩家名, VFX类型) - 由OnTetherCreate (data3=369/373) 填充
    private readonly Dictionary<string, (string PlayerName, string VfxType)> _round2Tethers = new();

    // 阶段计数器: 0=未开始, 1=第一阶段(VfxTether), 2=第二阶段(VfxShare/VfxCircle), 3=引导中
    private int _currentPhase = 0;

    // 第三阶段: 引导相关
    private readonly List<string> _guidanceSequence = new(); // 4轮顺序 (分摊/大圈)
    private readonly List<string[]> _roundPoints = new(); // 每轮处理的标点对
    private int _currentGuidanceRound = 0; // 当前轮次 0-3
    private int _lastLoggedRound = -1; // 上次输出日志的轮次 (防止每帧重复输出)
    private long _guidanceStartTime = 0; // 引导开始时间
    private const int FirstRoundDurationMs = 7000; // 第一轮持续5秒
    private const int OtherRoundDurationMs = 5000; // 后续轮持续5秒

    // 玩家分组缓存: 玩家名→组名(右组/左组)
    private readonly Dictionary<string, string> _playerGroups = new();

    // 玩家标点缓存: 玩家名→标点名
    private readonly Dictionary<string, string> _playerPoints = new();

    // 第二轮AOE: 第5次心象投影后记录，第7次绘制
    private readonly List<CloneInfo> _secondRoundCloneInfos = new();
    private bool _recordingSecondRound = false;
    private long _secondRoundDrawTime = 0;

    // 第三轮: 第8次心象投影绘制缺少的刀
    private uint _missingBladeType = 0; // CloneLeftRight 或 CloneFrontBack
    private uint _fifthRoundBladeType = 0; // 第5次记录的刀类型
    private long _thirdRoundDrawTime = 0;
    private static readonly Vector3 ThirdRoundPosition = new(100f, 0f, 92.5f);

    // 第6/8次心象投影: 19210大圈绘制
    private long _sixthRoundDrawTime = 0;
    private long _eighthRoundDrawTime = 0;
    private readonly List<Vector3> _sixthRoundCirclePositions = new();
    private readonly List<Vector3> _eighthRoundCirclePositions = new();
    private bool _waitingForTimeWarp = false; // 等待时空重现触发第6次绘制

    // 第5次心象投影: 小世界站位绘制
    private long _fifthRoundStandDrawTime = 0;

    // 标点坐标 (4A1顺序)
    private static readonly Vector3 WaymarkA = new(99.9f, 0f, 88.96f);    // 北
    private static readonly Vector3 WaymarkB = new(110.89f, 0f, 99.74f);  // 东
    private static readonly Vector3 WaymarkC = new(100.10f, 0f, 110.90f); // 南
    private static readonly Vector3 WaymarkD = new(89.11f, 0f, 99.93f);   // 西
    private static readonly Vector3 Waymark1 = new(108.28f, 0f, 91.70f);  // 东北
    private static readonly Vector3 Waymark2 = new(108.43f, 0f, 108.10f); // 东南

    #endregion

    #region 配置

    private Config C => Controller.GetConfig<Config>();

    public class Config : IEzConfig
    {
        public int DelayMs = 9000;  // AOE绘制延迟时间
        public int DurationMs = 4000; // AOE持续时间 (4秒)
        public int GuidanceDelayMs = 5700; // 引导延迟时间
        public int EighthCircleDelayMs = 5700; // 第8次大圈延迟
        public int EighthCircleDurationMs = 5000; // 第8次大圈持续时间
        public int MissingBladeDelayMs = 13000; // 缺少的刀延迟
        public int MissingBladeDurationMs = 5000; // 缺少的刀持续时间
        public int FifthStandDelayMs = 8000; // 第5次小世界站位延迟
        public int FifthStandDurationMs = 7000; // 第5次小世界站位持续时间
        public uint ColorDanger = 0xFFFF0000;  // 红色
        public bool EnableDraw = true; // 是否启用绘制
        public bool EnableGuidance = true; // 是否启用指路绘制
        public bool DebugTetherAll = true; // Debug模式: 连线所有玩家; 正常模式: 只连线自己
    }

    #endregion

    #region 元素设置

    public override void OnSetup()
    {
        // 为每个可能的分身位置注册元素 (最多8个分身)
        for(int i = 0; i < 8; i++)
        {
            // 圆形AOE (钢铁) - type 0 = 固定坐标圆形
            Controller.RegisterElement($"Circle_{i}", new Element(0)
            {
                radius = 10f,
                Filled = true,
                fillIntensity = 0.5f,
                thicc = 3f,
                Enabled = false,
            });

            // 左右刀 - 右侧扇形 - type 5 = 固定坐标锥形
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

            // 左右刀 - 左侧扇形
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

            // 前后刀 - 前方扇形
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

            // 前后刀 - 后方扇形
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

        // 第三阶段引导元素: 左右组站位点
        // 右组分摊站位 (北侧)
        Controller.RegisterElement("RightGroup_Share", new Element(0)
        {
            refX = 105.76f,
            refY = 0,
            refZ = 91.98f,
            radius = 5f,
            Filled = true,
            fillIntensity = 0.5f,
            color = 0xFF00FF00, // 绿色
            Enabled = false,
        });

        // 右组分散站位 (南侧)
        Controller.RegisterElement("RightGroup_Spread", new Element(0)
        {
            refX = 112.73f,
            refY = 0,
            refZ = 113.91f,
            radius = 1f,
            Filled = true,
            fillIntensity = 0.5f,
            color = 0xFFFF00FF, // 紫色
            Enabled = false,
        });

        // 左组分摊站位 (北侧)
        Controller.RegisterElement("LeftGroup_Share", new Element(0)
        {
            refX = 94.16f,
            refY = 0,
            refZ = 92.20f,
            radius = 5f,
            Filled = true,
            fillIntensity = 0.5f,
            color = 0xFF00FF00, // 绿色
            Enabled = false,
        });

        // 左组分散站位 (南侧)
        Controller.RegisterElement("LeftGroup_Spread", new Element(0)
        {
            refX = 89.18f,
            refY = 0,
            refZ = 115.38f,
            radius = 1f,
            Filled = true,
            fillIntensity = 0.5f,
            color = 0xFFFF00FF, // 紫色
            Enabled = false,
        });

        // 连线指路元素 (8个玩家)
        for(int i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"PlayerTether_{i}", new Element(2) // type 2 = 连线
            {
                thicc = 2.5f,
                Enabled = false,
            });

            // 大圈玩家身上的圆形指示
            Controller.RegisterElement($"PlayerCircle_{i}", new Element(1) // type 1 = 跟随玩家的圆形
            {
                radius = 20f,
                Filled = false,
                thicc = 3f,
                color = 0xFFFF00FF, // 紫色
                Enabled = false,
            });
        }

        // 第6/8次心象投影: 19210大圈 (2个)
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

        // 检测四运开始
        if(castId == MirrorCastId)
        {
            _isMechanicActive = true;
            AddLog($"[{timestamp}] === 四运开始 === (镜中奇梦 {castId})");
            _firstSpawnPositions.Clear();
            _cloneInfos.Clear();
            _connectionType = "";
            _frontBackClonePos = Vector3.Zero;
            _executeTime = 0;
            _round1Tethers.Clear();
            _round2VfxTypes.Clear();
            _round2Tethers.Clear();
            _projectionCastCount = 0; // 重置心象投影计数器
            return;
        }

        if(!_isMechanicActive) return;

        // 获取source对象信息
        if(source.GetObject() is IBattleNpc npc)
        {
            var baseId = npc.BaseId;
            var pos = npc.Position;
            var rot = npc.Rotation;
            var pointType = GetPointType(pos);

            AddLog($"[{timestamp}] StartCast: {npc.Name} (BaseId:{baseId}) 读条:{castId} 位置:({pos.X:F1},{pos.Z:F1}) 朝向:{rot:F2} {pointType}");

            // 记录分身技能
            if(baseId == CloneDataId)
            {
                if(castId is CloneLeftRight or CloneFrontBack or CloneCircle or CloneCircle2)
                {
                    string actionName = castId switch
                    {
                        CloneLeftRight => "左右双刀",
                        CloneFrontBack => "前后双刀",
                        CloneCircle or CloneCircle2 => "圆形AOE",
                        _ => $"未知({castId})"
                    };

                    // 根据阶段记录到不同列表
                    if(_recordingSecondRound)
                    {
                        _secondRoundCloneInfos.Add(new CloneInfo(castId, pos, rot));
                        AddLog($"  -> [第二轮] 分身技能: {actionName} (共{_secondRoundCloneInfos.Count}个)");

                        // 记录刀的类型用于第三轮绘制
                        if(castId is CloneLeftRight or CloneFrontBack)
                        {
                            _fifthRoundBladeType = castId;
                            var bladeType = castId == CloneLeftRight ? "左右双刀" : "前后双刀";
                            AddLog($"  -> 第5次记录刀类型: {bladeType}");
                        }
                    }
                    else
                    {
                        _cloneInfos.Add(new CloneInfo(castId, pos, rot));
                        AddLog($"  -> 分身技能: {actionName}");
                    }

                    // 记录前后刀位置
                    if(castId == CloneFrontBack)
                    {
                        _frontBackClonePos = pos;
                        AddLog($"  -> 记录前后刀位置: ({pos.X:F1},{pos.Z:F1})");
                    }
                }
            }

            // Boss执行读条 (仅记录，不触发绘制)
            if(baseId == BossDataId && castId == ExecuteCastId)
            {
                AddLog($"  -> Boss执行读条 ({castId})");
            }

            // 时空重现 - 触发第6次大圈绘制
            if(baseId == BossDataId && castId == TimeWarpCastId && _waitingForTimeWarp)
            {
                _waitingForTimeWarp = false;
                _sixthRoundCirclePositions.Clear();
                Vector3[] targetWaymarks = _firstSpawnType == "正点"
                    ? new[] { WaymarkC, WaymarkD }
                    : new[] { Waymark1, Waymark2 };

                foreach(var waymark in targetWaymarks)
                {
                    var nearby = Svc.Objects.OfType<IBattleNpc>()
                        .FirstOrDefault(x => x.BaseId == FirstSpawnId && x.IsCharacterVisible()
                            && Vector3.Distance(x.Position, waymark) < 6f);
                    if(nearby != null)
                        _sixthRoundCirclePositions.Add(nearby.Position);
                }

                _sixthRoundDrawTime = Environment.TickCount64;
                var waymarkNames = _firstSpawnType == "正点" ? "C/D" : "1/2";
                AddLog($"  -> 时空重现! 立即绘制第6次大圈 {waymarkNames}附近 (找到{_sixthRoundCirclePositions.Count}个)");
            }

            // Boss心象投影读条 - 第4次触发引导，第5次开始记录第二轮，第7次绘制
            if(baseId == BossDataId && castId == ProjectionCastId)
            {
                _projectionCastCount++;
                AddLog($"  -> 心象投影读条 第{_projectionCastCount}次 ({castId})");

                if(_projectionCastCount == 4)
                {
                    // 第4次读条，计算引导并设置延迟触发
                    CalculateGuidanceSequence();
                    CalculatePlayerGroups();
                    // 引导延迟后开始
                    _guidanceStartTime = Environment.TickCount64 + C.GuidanceDelayMs;
                    _currentPhase = 3;
                    _currentGuidanceRound = 0;
                    _lastLoggedRound = -1; // 重置日志轮次，确保第一轮能输出日志
                    AddLog($"  -> 第4次心象投影! {C.GuidanceDelayMs}ms后开始引导绘制");
                    AddLog($"  -> 引导顺序: {string.Join(" → ", _guidanceSequence)}");
                }
                else if(_projectionCastCount == 5)
                {
                    // 第5次：开始记录第二轮分身技能
                    _secondRoundCloneInfos.Clear();
                    _recordingSecondRound = true;
                    // 第5次：触发小世界站位绘制
                    _fifthRoundStandDrawTime = Environment.TickCount64 + C.FifthStandDelayMs;
                    AddLog($"  -> 第5次心象投影! 开始记录第二轮分身技能, {C.FifthStandDelayMs / 1000f}秒后绘制小世界站位");
                }
                else if(_projectionCastCount == 6)
                {
                    // 第6次：设置标志，等待时空重现时再搜索19210
                    _waitingForTimeWarp = true;
                    var waymarkNames = _firstSpawnType == "正点" ? "C/D" : "1/2";
                    AddLog($"  -> 第6次心象投影! 等待时空重现触发{waymarkNames}附近19210大圈");
                }
                else if(_projectionCastCount == 7)
                {
                    // 第7次：触发第二轮AOE绘制，延迟3秒，并根据第5次记录的刀类型确定缺少的刀
                    _recordingSecondRound = false;
                    _secondRoundDrawTime = Environment.TickCount64 + 3000;

                    // 根据第5次记录的刀类型确定缺少的刀 (第5次是左右刀则缺前后刀，反之亦然)
                    _missingBladeType = _fifthRoundBladeType == CloneLeftRight ? CloneFrontBack : CloneLeftRight;

                    string missingName = _missingBladeType == CloneLeftRight ? "左右双刀" : "前后双刀";
                    string fifthName = _fifthRoundBladeType == CloneLeftRight ? "左右双刀" : "前后双刀";
                    AddLog($"  -> 第7次心象投影! 3秒后绘制第二轮AOE (共{_secondRoundCloneInfos.Count}个分身)");
                    AddLog($"  -> 第5次是{fifthName}，缺少的刀: {missingName}");
                }
                else if(_projectionCastCount == 8)
                {
                    // 第8次：绘制缺少的刀
                    _thirdRoundDrawTime = Environment.TickCount64 + C.MissingBladeDelayMs;
                    AddLog($"  -> 第8次心象投影! {C.MissingBladeDelayMs / 1000f}秒后绘制缺少的刀");

                    // 第8次：根据正点先/斜点先绘制19210大圈
                    _eighthRoundCirclePositions.Clear();
                    Vector3[] targetWaymarks = _firstSpawnType == "正点"
                        ? new[] { Waymark1, Waymark2 }  // 正点先: 1/2
                        : new[] { WaymarkA, WaymarkB }; // 斜点先: A/B

                    foreach(var waymark in targetWaymarks)
                    {
                        var nearby = Svc.Objects.OfType<IBattleNpc>()
                            .FirstOrDefault(x => x.BaseId == FirstSpawnId && x.IsCharacterVisible()
                                && Vector3.Distance(x.Position, waymark) < 6f);
                        if(nearby != null)
                            _eighthRoundCirclePositions.Add(nearby.Position);
                    }

                    _eighthRoundDrawTime = Environment.TickCount64 + C.EighthCircleDelayMs;
                    var waymarkNames = _firstSpawnType == "正点" ? "1/2" : "A/B";
                    AddLog($"  -> 第8次心象投影! {C.EighthCircleDelayMs / 1000f}秒后绘制{waymarkNames}附近19210大圈 (找到{_eighthRoundCirclePositions.Count}个)");
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

        // 第一阶段: 连线VFX - 记录连线玩家 (VFX在19204上)
        if(vfxPath == VfxTether)
        {
            // 第一次VfxTether触发阶段1
            if(_currentPhase == 0)
            {
                _currentPhase = 1;
                AddLog($"[{timestamp}] === 进入阶段1: 连线检测 ===");
            }

            var sourceObj = target.GetObject();
            if(sourceObj is IBattleNpc npc && npc.BaseId == CloneDataId)
            {
                var npcPos = npc.Position;
                var pointName = GetPointName(npcPos);
                var npcPointType = GetPointType(npcPos);
                AddLog($"[{timestamp}] 连线VFX: 19204 {npcPointType} 位置:({npcPos.X:F1},{npcPos.Z:F1})");

                // 通过 AttachedInfo.TetherInfos 查找该分身连线的玩家
                if(AttachedInfo.TetherInfos.TryGetValue(npc.Address, out var tethers) && tethers.Count > 0)
                {
                    foreach(var tether in tethers)
                    {
                        var tetherTarget = Svc.Objects.FirstOrDefault(x => x.EntityId == tether.Target);
                        if(tetherTarget is IPlayerCharacter player)
                        {
                            var playerName = player.Name.ToString();
                            _round1Tethers[pointName] = playerName;
                            AddLog($"  -> 第一阶段: {pointName}点——>{playerName}");
                        }
                    }
                }
                else
                {
                    // 如果 AttachedInfo 还没有数据，使用 npc.TargetObjectId
                    var npcTargetId = npc.TargetObjectId;
                    var npcTargetObj = Svc.Objects.FirstOrDefault(x => x.EntityId == npcTargetId);
                    if(npcTargetObj is IPlayerCharacter player)
                    {
                        var playerName = player.Name.ToString();
                        _round1Tethers[pointName] = playerName;
                        AddLog($"  -> 第一阶段: {pointName}点——>{playerName}");
                    }
                }
            }
        }
        // 第二阶段: 分摊/大圈VFX - 记录VFX类型和位置 (VFX在19204上)
        else if(vfxPath == VfxShare || vfxPath == VfxCircle)
        {
            // 第一次VfxShare/VfxCircle触发阶段2
            if(_currentPhase == 1)
            {
                _currentPhase = 2;
                AddLog($"[{timestamp}] === 进入阶段2: VFX类型检测 ===");
            }

            var targetObj = target.GetObject();
            var targetName = targetObj?.Name.ToString() ?? target.ToString();
            var pos = targetObj?.Position ?? Vector3.Zero;
            var pointType = GetPointType(pos);
            var pointName = GetPointName(pos);

            string vfxType = vfxPath == VfxShare ? "分摊" : "大圈";
            string vfxTypeDisplay = vfxPath == VfxShare ? "四人分摊" : "大圈";
            AddLog($"[{timestamp}] VFX: {vfxTypeDisplay} 目标:{targetName} 位置:({pos.X:F1},{pos.Z:F1}) {pointType}");

            // 记录到 _round2VfxTypes (19204分身)
            if(targetObj is IBattleNpc npc && npc.BaseId == CloneDataId)
            {
                _round2VfxTypes[pointName] = vfxType;
                AddLog($"  -> 记录VFX类型: {pointName}点【{vfxType}】 (共{_round2VfxTypes.Count}/8)");

                // VfxShare/VfxCircle 出现时触发绘制
                if(_executeTime == 0)
                {
                    _executeTime = Environment.TickCount64;
                    AddLog($"  -> VFX触发绘制! 将在{C.DelayMs}ms后绘制AOE");
                }

                // 注: 引导现在由第4次心象投影(48098)触发，不再由VFX触发
            }
        }
        else if(vfxPath.Contains("channeling") || vfxPath.Contains("lockon"))
        {
            var targetObj = target.GetObject();
            var targetName = targetObj?.Name.ToString() ?? target.ToString();
            AddLog($"[{timestamp}] VFX: {vfxPath} 目标:{targetName}");
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

        AddLog($"[{timestamp}] Tether: {sourceName} -> {targetName} data2:{data2} data3:{data3} 位置:({sourcePos.X:F1},{sourcePos.Z:F1})");

        // 检测人形分身(19204)连线玩家
        if(sourceObj is IBattleNpc npc && npc.BaseId == CloneDataId && targetObj is IPlayerCharacter player)
        {
            var pointName = GetPointName(sourcePos);
            var playerName = player.Name.ToString();

            // 根据data3区分阶段: 368=第一阶段, 369/373=第二阶段
            if(data3 == 368)
            {
                // 第一阶段连线
                _round1Tethers[pointName] = playerName;
                AddLog($"  -> 第一阶段(data3=368): {pointName}点——>{playerName}");
            }
            else if(data3 == 369 || data3 == 373)
            {
                // 第二阶段连线，从 _round2VfxTypes 获取VFX类型
                var vfxType = _round2VfxTypes.TryGetValue(pointName, out var type) ? type : "未知";
                _round2Tethers[pointName] = (playerName, vfxType);
                AddLog($"  -> 第二阶段(data3={data3}): {pointName}点【{vfxType}】——>{playerName}");
            }
        }
    }

    // 获取NPC身上的VFX类型 (分摊/大圈)
    private string GetNpcVfxType(IBattleNpc npc)
    {
        // 调试: 输出NPC的VFX信息
        AddLog($"    [VFX检测] NPC:{npc.Name} BaseId:{npc.BaseId} Address:{npc.Address:X}");

        if(AttachedInfo.VFXInfos.TryGetValue(npc.Address, out var vfxDict))
        {
            AddLog($"    [VFX检测] 找到VFX字典，共{vfxDict.Count}个VFX");
            foreach(var kvp in vfxDict)
            {
                AddLog($"      - {kvp.Key}");
            }

            if(vfxDict.ContainsKey(VfxShare))
                return "分摊";
            if(vfxDict.ContainsKey(VfxCircle))
                return "大圈";
        }
        else
        {
            AddLog($"    [VFX检测] 未找到VFX字典");
        }
        return "未知";
    }

    // 计算引导顺序 (4轮) - 固定顺序: AC → 13 → BD → 24
    private void CalculateGuidanceSequence()
    {
        _guidanceSequence.Clear();
        _roundPoints.Clear();

        // 固定顺时针顺序: AC → 13 → BD → 24
        _roundPoints.Add(new[] { "A", "C" });
        _roundPoints.Add(new[] { "1", "3" });
        _roundPoints.Add(new[] { "B", "D" });
        _roundPoints.Add(new[] { "4", "2" });

        // 根据第一轮VFX类型确定顺序，后续固定交替
        var firstRoundPoint = _roundPoints[0][0]; // A点
        var firstVfxType = _round2VfxTypes.TryGetValue(firstRoundPoint, out var type) ? type : "未知";

        // 固定交替模式
        if(firstVfxType == "大圈")
        {
            _guidanceSequence.AddRange(new[] { "大圈", "分摊", "大圈", "分摊" });
        }
        else
        {
            _guidanceSequence.AddRange(new[] { "分摊", "大圈", "分摊", "大圈" });
        }

        AddLog($"  -> 标点顺序: {string.Join(" → ", _roundPoints.Select(p => $"[{string.Join(",", p)}]"))}");
        AddLog($"  -> 第一轮VFX: {firstVfxType} -> 固定交替顺序: {string.Join(" → ", _guidanceSequence)}");
    }

    // 根据第二阶段连线计算玩家分组
    private void CalculatePlayerGroups()
    {
        _playerGroups.Clear();
        _playerPoints.Clear();

        foreach(var kvp in _round2Tethers)
        {
            var point = kvp.Key;
            var playerName = kvp.Value.PlayerName;

            // 记录玩家对应的标点
            _playerPoints[playerName] = point;

            // 右组: A, 1, B, 2 (4A1顺序)
            // 左组: C, 3, D, 4
            if(point == "A" || point == "1" || point == "B" || point == "2")
                _playerGroups[playerName] = "右组";
            else if(point == "C" || point == "3" || point == "D" || point == "4")
                _playerGroups[playerName] = "左组";
        }

        // 日志输出分组结果
        var rightGroup = _playerGroups.Where(x => x.Value == "右组").Select(x => x.Key).ToList();
        var leftGroup = _playerGroups.Where(x => x.Value == "左组").Select(x => x.Key).ToList();
        AddLog($"  -> 右组(A1B2): {string.Join(", ", rightGroup)}");
        AddLog($"  -> 左组(C3D4): {string.Join(", ", leftGroup)}");
    }

    // 绘制引导站位点
    private void DrawGuidanceElements(string action, int roundIndex)
    {
        // 分摊时：显示两组分摊点
        // 大圈时：只显示该轮需要出去放圈的组的分散点
        bool isSpread = action == "大圈";

        if(isSpread && roundIndex < _roundPoints.Count)
        {
            // 大圈：只显示分散点
            var currentPoints = _roundPoints[roundIndex];
            // 右组点: A, B, 1, 2
            bool rightNeedsSpread = currentPoints.Any(p => p == "A" || p == "B" || p == "1" || p == "2");
            // 左组点: C, D, 3, 4
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
            // 分摊：显示两组分摊点
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

    // 绘制玩家连线指路 - 所有玩家都需要指路
    private void DrawPlayerTethers(string action, int roundIndex)
    {
        int tetherIndex = 0;
        var party = FakeParty.Get().ToList();
        var localPlayer = Svc.ClientState.LocalPlayer;

        // 当前轮次需要处理的标点
        var currentPoints = roundIndex < _roundPoints.Count ? _roundPoints[roundIndex] : Array.Empty<string>();

        // 只在轮次切换时输出一次日志
        bool shouldLog = _lastLoggedRound != roundIndex;
        if(shouldLog)
        {
            _lastLoggedRound = roundIndex;
            AddLog($"[连线Debug] 轮次{roundIndex + 1} action={action} currentPoints=[{string.Join(",", currentPoints)}]");
        }

        foreach(var player in party)
        {
            var playerName = player.Name.ToString();

            // 获取玩家对应的标点
            if(!_playerPoints.TryGetValue(playerName, out var playerPoint))
                continue;

            // 获取玩家所属组
            if(!_playerGroups.TryGetValue(playerName, out var group))
                continue;

            // 非Debug模式下只连线自己
            if(!C.DebugTetherAll && player.EntityId != localPlayer?.EntityId)
                continue;

            Vector3 targetPos;
            uint tetherColor;

            // 判断该玩家是否是当前轮次需要放大圈的人
            // 条件: 当前轮action是"大圈" AND 玩家标点在当前轮标点列表中
            bool isSpreadPlayer = action == "大圈" && currentPoints.Contains(playerPoint);

            // 只在轮次切换时输出每个玩家的判断结果
            if(shouldLog)
            {
                var vfxType = _round2VfxTypes.TryGetValue(playerPoint, out var t) ? t : "无";
                AddLog($"  [{playerName}] point={playerPoint} vfx={vfxType} action={action} isSpread={isSpreadPlayer}");
            }

            if(isSpreadPlayer)
            {
                // 当前轮次放大圈的玩家 -> 分散点
                targetPos = group == "右组"
                    ? new Vector3(112.73f, 0, 113.91f)  // 右组分散点
                    : new Vector3(89.18f, 0, 115.38f); // 左组分散点
                tetherColor = 0xFFFF00FFu; // 紫色 (大圈)
            }
            else
            {
                // 其他玩家 -> 分摊点
                targetPos = group == "右组"
                    ? new Vector3(105.76f, 0, 91.98f)  // 右组分摊点
                    : new Vector3(94.16f, 0, 92.20f); // 左组分摊点
                tetherColor = 0xFF00FF00u; // 绿色 (分摊)
            }

            // 设置连线
            if(Controller.TryGetElementByName($"PlayerTether_{tetherIndex}", out var tether))
            {
                tether.Enabled = true;
                tether.SetRefPosition(player.Position);
                tether.SetOffPosition(targetPos);
                tether.color = tetherColor;
            }

            // 大圈玩家身上绘制圆形
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

        // 隐藏未使用的连线和圆形
        for(int i = tetherIndex; i < 8; i++)
        {
            if(Controller.TryGetElementByName($"PlayerTether_{i}", out var t))
                t.Enabled = false;
            if(Controller.TryGetElementByName($"PlayerCircle_{i}", out var c))
                c.Enabled = false;
        }
    }

    // 隐藏引导元素
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
        // 四运激活后检测19210的位置
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
                    AddLog($"[检测] 19210可见: X={spawn.Position.X:F2}, Y={spawn.Position.Y:F2} -> {pointType}");
                }

                // 判断先出现的是正点还是斜点
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
                    AddLog($"[检测] 先出现的是: {_firstSpawnType} (共{_firstSpawnPositions.Count}个)");
                }
            }
        }

        if(!_isMechanicActive && !_manualDrawTest)
        {
            // 非激活状态：隐藏所有元素
            HideAllElements();
            return;
        }

        // 绘制AOE (正常触发或手动测试)
        if((C.EnableDraw && _executeTime > 0) || _manualDrawTest)
        {
            _drawDebugLog.Clear();
            _drawDebugLog.Add($"=== 绘制调试 ===");
            _drawDebugLog.Add($"EnableDraw: {C.EnableDraw}");
            _drawDebugLog.Add($"_executeTime: {_executeTime}");
            _drawDebugLog.Add($"_manualDrawTest: {_manualDrawTest}");
            _drawDebugLog.Add($"_cloneInfos.Count: {_cloneInfos.Count}");

            if(!_manualDrawTest)
            {
                var elapsed = Environment.TickCount64 - _executeTime;
                _drawDebugLog.Add($"elapsed: {elapsed}ms");

                // 延迟期间不绘制
                if(elapsed < C.DelayMs)
                {
                    _drawDebugLog.Add($"等待延迟... ({elapsed}/{C.DelayMs})");
                    HideAllElements();
                    return;
                }

                // 超过持续时间后清空执行时间
                if(elapsed > C.DelayMs + C.DurationMs)
                {
                    _drawDebugLog.Add($"超时，重置");
                    _executeTime = 0;
                    HideAllElements();
                    return;
                }
            }

            // 绘制所有记录的分身AOE
            int drawnCount = 0;
            for(int i = 0; i < _cloneInfos.Count && i < 8; i++)
            {
                var clone = _cloneInfos[i];
                _drawDebugLog.Add($"--- 分身 {i}: ActionId={clone.ActionId}, Pos=({clone.Position.X:F1},{clone.Position.Z:F1}), Rot={clone.Rotation:F2}");

                switch(clone.ActionId)
                {
                    case CloneCircle:
                        if(Controller.TryGetElementByName($"Circle_{i}", out var circle))
                        {
                            circle.Enabled = true;
                            circle.SetRefPosition(clone.Position);
                            circle.color = C.ColorDanger;
                            drawnCount++;
                            _drawDebugLog.Add($"  ✓ Circle_{i} 已启用");
                        }
                        else
                        {
                            _drawDebugLog.Add($"  ✗ Circle_{i} 获取失败!");
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
                        _drawDebugLog.Add($"  ✗ 未知ActionId: {clone.ActionId}");
                        break;
                }
            }

            _drawDebugLog.Add($"=== 总共绘制了 {drawnCount} 个元素 ===");

            // 隐藏未使用的元素
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
            // 不在绘制窗口：隐藏所有元素
            HideAllElements();
        }

        // 第二轮AOE绘制 (第7次心象投影触发，延迟1秒，持续5秒)
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

        // 第三轮: 缺少的刀绘制
        if(_thirdRoundDrawTime > 0 && _missingBladeType != 0)
        {
            var now = Environment.TickCount64;
            if(now >= _thirdRoundDrawTime && now < _thirdRoundDrawTime + C.MissingBladeDurationMs)
            {
                // 绘制缺少的刀 (使用索引7，避免与其他元素冲突)
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

        // 第6次心象投影: 19210大圈绘制 (时空重现触发，延迟0秒，持续5秒)
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

        // 第8次心象投影: 19210大圈绘制
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

        // 阶段3: 引导绘制
        if(C.EnableGuidance && _currentPhase == 3 && _guidanceSequence.Count == 4)
        {
            var now = Environment.TickCount64;

            // 等待延迟时间过去
            if(now < _guidanceStartTime)
            {
                // 延迟期间隐藏引导元素
                HideGuidanceElements();
                return;
            }

            var elapsed = now - _guidanceStartTime;

            // 计算当前轮次 (第一轮8秒，后续轮6秒)
            // 轮次0: 0-8000ms, 轮次1: 8000-14000ms, 轮次2: 14000-20000ms, 轮次3: 20000-26000ms
            if(elapsed < FirstRoundDurationMs)
                _currentGuidanceRound = 0;
            else if(elapsed < FirstRoundDurationMs + OtherRoundDurationMs)
                _currentGuidanceRound = 1;
            else if(elapsed < FirstRoundDurationMs + OtherRoundDurationMs * 2)
                _currentGuidanceRound = 2;
            else
                _currentGuidanceRound = 3;

            // 获取当前轮次的动作
            var currentAction = _guidanceSequence[_currentGuidanceRound];

            // 绘制站位点
            DrawGuidanceElements(currentAction, _currentGuidanceRound);

            // 绘制连线指路
            DrawPlayerTethers(currentAction, _currentGuidanceRound);

            // 4轮结束后隐藏 (总时长: 8 + 6*3 = 26秒)
            var totalDuration = FirstRoundDurationMs + OtherRoundDurationMs * 3;
            if(_currentGuidanceRound >= 3 && elapsed > totalDuration)
            {
                HideGuidanceElements();
                _currentPhase = 0; // 重置阶段
            }
        }
        else if(_currentPhase != 3)
        {
            // 非阶段3时隐藏引导元素
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
        _projectionCastCount = 0; // 重置心象投影计数器
        _round1Tethers.Clear();
        _round2VfxTypes.Clear();
        _round2Tethers.Clear();
        // 阶段3引导相关
        _currentPhase = 0;
        _guidanceSequence.Clear();
        _roundPoints.Clear();
        _currentGuidanceRound = 0;
        _lastLoggedRound = -1; // 重置日志轮次
        _guidanceStartTime = 0;
        _playerGroups.Clear();
        _playerPoints.Clear();
        // 第二轮AOE
        _secondRoundCloneInfos.Clear();
        _recordingSecondRound = false;
        _secondRoundDrawTime = 0;
        // 第三轮
        _missingBladeType = 0;
        _fifthRoundBladeType = 0;
        _thirdRoundDrawTime = 0;
        // 第6/8次19210大圈
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
        // 判断是正点还是斜点
        // 游戏坐标系: X是东西, Z是南北 (根据实际标点坐标)
        // 标点坐标 (4A1顺序):
        // A(北): X=99.9, Z=88.96
        // B(东): X=110.89, Z=99.74
        // C(南): X=100.10, Z=110.90
        // D(西): X=89.11, Z=99.93
        // 1(东北): X=108.28, Z=91.70
        // 2(东南): X=108.43, Z=108.10
        // 3(西南): X=91.80, Z=108.17
        // 4(西北): X=91.62, Z=91.75

        var x = pos.X;
        var z = pos.Z;  // Z是南北轴
        var center = 100f;
        var threshold = 5f;

        // 检查是否在正点轴上
        bool onXAxis = Math.Abs(x - center) < threshold;  // X≈100 -> A或C点
        bool onZAxis = Math.Abs(z - center) < threshold;  // Z≈100 -> B或D点

        // A(北): X≈100, Z<100
        if(onXAxis && z < center) return "[正点-A(北)]";
        // C(南): X≈100, Z>100
        if(onXAxis && z > center) return "[正点-C(南)]";
        // B(东): Z≈100, X>100
        if(onZAxis && x > center) return "[正点-B(东)]";
        // D(西): Z≈100, X<100
        if(onZAxis && x < center) return "[正点-D(西)]";

        // 斜点判断 (4A1顺序)
        // 4(西北): X<100, Z<100
        if(x < center && z < center) return "[斜点-4(西北)]";
        // 1(东北): X>100, Z<100
        if(x > center && z < center) return "[斜点-1(东北)]";
        // 2(东南): X>100, Z>100
        if(x > center && z > center) return "[斜点-2(东南)]";
        // 3(西南): X<100, Z>100
        if(x < center && z > center) return "[斜点-3(西南)]";

        return "[未知位置]";
    }

    private static string GetPointName(Vector3 pos)
    {
        // 返回简化的标点名 (A, B, C, D, 1, 2, 3, 4) - 4A1顺序
        var x = pos.X;
        var z = pos.Z;
        var center = 100f;
        var threshold = 5f;

        bool onXAxis = Math.Abs(x - center) < threshold;
        bool onZAxis = Math.Abs(z - center) < threshold;

        // 正点
        if(onXAxis && z < center) return "A";
        if(onXAxis && z > center) return "C";
        if(onZAxis && x > center) return "B";
        if(onZAxis && x < center) return "D";

        // 斜点 (4A1顺序)
        if(x < center && z < center) return "4";  // 西北
        if(x > center && z < center) return "1";  // 东北
        if(x > center && z > center) return "2";  // 东南
        if(x < center && z > center) return "3";  // 西南

        return "?";
    }

    private string GetPlayerRole(IPlayerCharacter player)
    {
        // 获取队伍成员并按职能排序
        var party = FakeParty.Get().ToList();

        // 分类
        var tanks = party.Where(p => p.GetRole() == CombatRole.Tank).OrderBy(p => GetJobPriority(p)).ToList();
        var healers = party.Where(p => p.GetRole() == CombatRole.Healer).OrderBy(p => GetJobPriority(p)).ToList();
        var dps = party.Where(p => p.GetRole() == CombatRole.DPS).OrderBy(p => GetJobPriority(p)).ToList();

        // 判断玩家职能
        var playerEntityId = player.EntityId;

        // 坦克: MT, ST
        for(int i = 0; i < tanks.Count; i++)
        {
            if(tanks[i].EntityId == playerEntityId)
                return i == 0 ? "MT" : "ST";
        }

        // 治疗: H1, H2
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

        return "未知";
    }

    private static int GetJobPriority(IPlayerCharacter player)
    {
        // 职业优先级排序 (用于确定MT/ST, H1/H2, D1-D4)
        // 坦克: PLD > WAR > DRK > GNB
        // 治疗: WHM > AST > SCH > SGE
        // DPS: 近战 > 远程物理 > 法系
        var job = player.GetJob();
        return job switch
        {
            // 坦克
            Job.PLD => 1,
            Job.WAR => 2,
            Job.DRK => 3,
            Job.GNB => 4,
            // 治疗
            Job.WHM => 1,
            Job.AST => 2,
            Job.SCH => 3,
            Job.SGE => 4,
            // 近战DPS
            Job.MNK => 1,
            Job.DRG => 2,
            Job.NIN => 3,
            Job.SAM => 4,
            Job.RPR => 5,
            Job.VPR => 6,
            // 远程物理DPS
            Job.BRD => 10,
            Job.MCH => 11,
            Job.DNC => 12,
            // 法系DPS
            Job.BLM => 20,
            Job.SMN => 21,
            Job.RDM => 22,
            Job.PCT => 23,
            _ => 99
        };
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("M12S 四运 绘制设置");

        var enableDraw = C.EnableDraw;
        if(ImGui.Checkbox("启用AOE绘制", ref enableDraw))
            C.EnableDraw = enableDraw;

        var enableGuidance = C.EnableGuidance;
        if(ImGui.Checkbox("启用指路绘制", ref enableGuidance))
            C.EnableGuidance = enableGuidance;

        ImGui.Separator();
        ImGui.Text("延迟设置:");

        var delay = C.DelayMs;
        if(ImGui.SliderInt("AOE延迟绘制(ms)", ref delay, 0, 5000))
            C.DelayMs = delay;

        var duration = C.DurationMs;
        if(ImGui.SliderInt("AOE持续时间(ms)", ref duration, 1000, 10000))
            C.DurationMs = duration;

        var guidanceDelay = C.GuidanceDelayMs;
        if(ImGui.SliderInt("引导延迟(ms)", ref guidanceDelay, 0, 5000))
            C.GuidanceDelayMs = guidanceDelay;

        var eighthCircleDelay = C.EighthCircleDelayMs;
        if(ImGui.SliderInt("第8次大圈延迟(ms)", ref eighthCircleDelay, 0, 15000))
            C.EighthCircleDelayMs = eighthCircleDelay;

        var eighthCircleDuration = C.EighthCircleDurationMs;
        if(ImGui.SliderInt("第8次大圈持续(ms)", ref eighthCircleDuration, 1000, 15000))
            C.EighthCircleDurationMs = eighthCircleDuration;

        var missingBladeDelay = C.MissingBladeDelayMs;
        if(ImGui.SliderInt("缺少的刀延迟(ms)", ref missingBladeDelay, 0, 20000))
            C.MissingBladeDelayMs = missingBladeDelay;

        var missingBladeDuration = C.MissingBladeDurationMs;
        if(ImGui.SliderInt("缺少的刀持续(ms)", ref missingBladeDuration, 1000, 15000))
            C.MissingBladeDurationMs = missingBladeDuration;

        var fifthStandDelay = C.FifthStandDelayMs;
        if(ImGui.SliderInt("小世界站位延迟(ms)", ref fifthStandDelay, 0, 15000))
            C.FifthStandDelayMs = fifthStandDelay;

        var fifthStandDuration = C.FifthStandDurationMs;
        if(ImGui.SliderInt("小世界站位持续(ms)", ref fifthStandDuration, 1000, 15000))
            C.FifthStandDurationMs = fifthStandDuration;

        var color = C.ColorDanger.ToVector4();
        if(ImGui.ColorEdit4("AOE颜色", ref color))
            C.ColorDanger = color.ToUint();

        ImGui.Separator();
        var debugTether = C.DebugTetherAll;
        if(ImGui.Checkbox("Debug模式 (连线所有玩家)", ref debugTether))
            C.DebugTetherAll = debugTether;

        if(ImGui.Button("保存配置"))
            Controller.SaveConfig();

        // ========== Debug内容 (仅在Debug模式下显示) ==========
        if(!C.DebugTetherAll) return;

        ImGui.Separator();
        ImGuiEx.Text(EColor.YellowBright, "===== Debug信息 =====");

        ImGui.Text($"Boss DataID: {BossDataId}");
        ImGui.Text($"分身 DataID: {CloneDataId}");
        ImGui.Text($"先刷新 DataID: {FirstSpawnId}");

        ImGui.Separator();
        ImGui.Text("技能ID:");
        ImGui.Text($"  镜中奇梦(开始): {MirrorCastId}");
        ImGui.Text($"  自我复制: {SelfCopyCastId}");
        ImGui.Text($"  执行(绘制): {ExecuteCastId}");
        ImGui.Text($"  心象投影(引导触发): {ProjectionCastId}");
        ImGui.Text($"  左右双刀: {CloneLeftRight}");
        ImGui.Text($"  前后双刀: {CloneFrontBack}");
        ImGui.Text($"  圆形AOE: {CloneCircle}");

        ImGui.Separator();
        ImGuiEx.Text($"四运激活: {_isMechanicActive}");
        ImGuiEx.Text($"先出现: {(_firstSpawnType == "" ? "未检测" : _firstSpawnType)}");
        ImGuiEx.Text($"心象投影读条: {_projectionCastCount}/4");
        ImGuiEx.Text($"连线类型: {_connectionType}");
        ImGuiEx.Text($"前后刀位置: ({_frontBackClonePos.X:F1},{_frontBackClonePos.Z:F1})");

        var now = Environment.TickCount64;
        if(_executeTime > 0)
        {
            var elapsed = now - _executeTime;
            var state = elapsed < C.DelayMs ? "等待中" :
                       elapsed < C.DelayMs + C.DurationMs ? "绘制中" : "已结束";
            ImGuiEx.Text($"绘制状态: {state} ({elapsed}ms)");
        }

        ImGui.Separator();
        ImGui.Text($"19210位置 ({_firstSpawnPositions.Count}):");
        foreach(var (entityId, pos, pointType) in _firstSpawnPositions)
        {
            ImGuiEx.Text($"  ({pos.X:F1},{pos.Z:F1}) {pointType}");
        }

        ImGui.Separator();
        ImGui.Text($"分身技能 ({_cloneInfos.Count}):");
        foreach(var clone in _cloneInfos)
        {
            string actionName = clone.ActionId switch
            {
                CloneLeftRight => "左右双刀",
                CloneFrontBack => "前后双刀",
                CloneCircle => "圆形AOE",
                _ => $"未知({clone.ActionId})"
            };
            ImGuiEx.Text($"  {actionName} ({clone.Position.X:F1},{clone.Position.Z:F1}) R:{clone.Rotation:F2}");
        }

        ImGui.Separator();
        ImGui.Text($"第一阶段连线 ({_round1Tethers.Count}):");
        if(_round1Tethers.Count > 0)
        {
            var pointOrder = new[] { "A", "B", "C", "D", "1", "2", "3", "4" };
            foreach(var point in pointOrder)
            {
                if(_round1Tethers.TryGetValue(point, out var playerName))
                {
                    ImGuiEx.Text(EColor.PurpleBright, $"  {point}点小怪——>{playerName}");
                }
            }
        }

        ImGui.Separator();
        ImGui.Text($"第二阶段连线 ({_round2Tethers.Count}):");
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
        ImGui.Text("引导状态:");
        ImGuiEx.Text($"当前阶段: {_currentPhase} (0=未开始, 1=连线, 2=VFX, 3=引导中)");
        if(_guidanceSequence.Count == 4 && _roundPoints.Count == 4)
        {
            var pointsDisplay = string.Join(" → ", _roundPoints.Select(p => $"[{string.Join(",", p)}]"));
            ImGuiEx.Text($"标点顺序: {pointsDisplay}");
            ImGuiEx.Text(EColor.YellowBright, $"VFX顺序: {string.Join(" → ", _guidanceSequence)}");

            if(_currentPhase == 3)
            {
                var currentTime = Environment.TickCount64;
                if(currentTime < _guidanceStartTime)
                {
                    var delayRemaining = _guidanceStartTime - currentTime;
                    ImGuiEx.Text(EColor.YellowBright, $"引导延迟: {delayRemaining / 1000f:F1}秒后开始");
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
                    var durationText = _currentGuidanceRound == 0 ? "8秒" : "6秒";
                    ImGuiEx.Text(EColor.GreenBright, $"当前轮次: 第{_currentGuidanceRound + 1}轮 ({durationText}) - [{string.Join(",", currentPoints)}] - {currentAction}");
                    ImGuiEx.Text($"下一轮倒计时: {Math.Max(0, nextRoundIn) / 1000f:F1}秒");

                    if(currentAction == "大圈")
                    {
                        var spreadPlayers = new List<string>();
                        foreach(var point in currentPoints)
                        {
                            if(_round2Tethers.TryGetValue(point, out var info))
                                spreadPlayers.Add($"{point}:{info.PlayerName}");
                        }
                        ImGuiEx.Text(EColor.OrangeBright, $"  需要出去放圈: {string.Join(", ", spreadPlayers)}");
                    }
                    else
                    {
                        ImGuiEx.Text(EColor.CyanBright, $"  本轮分摊，大家待命");
                    }
                }
            }
        }

        if(_playerGroups.Count > 0)
        {
            ImGui.Separator();
            ImGui.Text("玩家分组:");
            var rightGroup = _playerGroups.Where(x => x.Value == "右组").Select(x => x.Key).ToList();
            var leftGroup = _playerGroups.Where(x => x.Value == "左组").Select(x => x.Key).ToList();
            ImGuiEx.Text(EColor.CyanBright, $"  右组(A1B2): {string.Join(", ", rightGroup)}");
            ImGuiEx.Text(EColor.OrangeBright, $"  左组(C3D4): {string.Join(", ", leftGroup)}");
        }

        ImGui.Separator();
        if(ImGui.Button("清除日志"))
            _eventLog.Clear();
        ImGui.SameLine();
        if(ImGui.Button("重置状态"))
            OnReset();

        ImGui.Separator();
        ImGui.Text("测试功能:");
        if(ImGui.Button(_manualDrawTest ? "停止测试绘制" : "开始测试绘制"))
        {
            _manualDrawTest = !_manualDrawTest;
            AddLog(_manualDrawTest ? "[测试] 手动开启绘制测试" : "[测试] 停止绘制测试");
        }
        ImGui.SameLine();
        ImGuiEx.Text(_manualDrawTest ? EColor.GreenBright : EColor.RedBright,
            _manualDrawTest ? "● 测试中" : "○ 未测试");

        if(_drawDebugLog.Count > 0)
        {
            ImGui.Separator();
            ImGui.Text("绘制调试信息:");
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
        ImGui.Text($"事件日志 ({_eventLog.Count}):");
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
