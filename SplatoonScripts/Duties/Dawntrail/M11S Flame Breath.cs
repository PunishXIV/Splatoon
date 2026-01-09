using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using ECommons.GameHelpers;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M11S_Flame_Breath : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1325];

    public override Metadata? Metadata => new(10, "Errer,slvrky");

    #region 常量

    // 技能ID
    private const uint TriggerCastId = 46143;   // Flatliner - 机制触发
    private const uint MeteorCastId = 46145;    // Meteor - 陨石落下
    private const uint MeteorLineId = 46146;    // MeteorLine - 传送门直线
    private const uint MeteorWrathId = 46147;   // MeteorWrath - 陨石愤怒(线连)
    private const uint FireBreathId = 46151;    // FireBreath - 火焰吐息

    // Boss DataID
    private const uint BossDataId = 19180;

    // VFX
    private const string MarkerVfx = "vfx/lockon/eff/lockon8_t0w.avfx";

    // 线连ID
    private const uint TetherDanger = 57;
    private const uint TetherSafe = 249;

    // 传送门直线 MapEffect位置对应的直线坐标
    private static readonly Dictionary<uint, Vector2> MapEffectToPos = new()
    {
        { 22, new(79f, 75f) },   // 正左
        { 23, new(89f, 75f) },   // 左2
        { 24, new(111f, 75f) },  // 右1
        { 25, new(121f, 75f) },  // 右2
    };

    #endregion

    #region 状态变量

    // 公共状态
    private bool _isMechanicActive = false;
    private int _meteorCastCount = 0;
    private int _meteorEndCount = 0;
    private int _completeCount = 0;

    // 火焰吐息状态 (VFX标记)
    private readonly List<IPlayerCharacter> _fireBreathTargets = new();

    // 传送门直线状态 (MapEffect)
    private readonly List<Vector2> _meteorLinePositions = new();

    // 线连状态 (Tether)
    private record struct TetherInfo(IGameObject Source, IGameObject Target, bool IsDanger);
    private readonly List<TetherInfo> _meteorWrathTethers = new();

    // 是否达到3次陨石（即将生效）
    private bool IsMechanicIncoming => _meteorCastCount >= 3;

    #endregion

    #region 配置

    private Config C => Controller.GetConfig<Config>();

    public class Config : IEzConfig
    {
        // 火焰吐息配置
        public float BreathLineWidth = 3.0f;
        public float BreathLineLength = 30.0f;
        public float BreathFillIntensity = 0.125f;
        public float BreathLineThickness = 1.0f;

        // 传送门直线配置
        public float PortalLineWidth = 5.0f;
        public float PortalFillIntensity = 0.1f;
        public float PortalLineThickness = 1.0f;

        // 线连扇形配置
        public float WrathRadius = 5.0f;
        public float WrathLength = 75.0f;
        public float WrathFillIntensity = 0.125f;
        public float WrathLineThickness = 1.0f;

        // 颜色配置
        public uint ColorSelf = 0xFF00FF00;      // 绿色 - 自己
        public uint ColorOther = 0xFF0000FF;     // 红色 - 他人
        public uint ColorPending = 0xFF00A5FF;   // 橙色 - 等待中
    }

    #endregion

    #region 生命周期方法

    public override void OnSetup()
    {
        // 注册火焰吐息扇形元素 (4个) - type=3 扇形
        for (int i = 0; i < 4; i++)
        {
            Controller.RegisterElement($"Breath_{i}", new Element(3)
            {
                radius = C.BreathLineWidth,
                offY = C.BreathLineLength,
                thicc = C.BreathLineThickness,
                fillIntensity = C.BreathFillIntensity,
                includeRotation = true,
                refActorComparisonType = 2,
                Enabled = false,
            });
        }

        // 注册传送门直线元素 (2个) - type=2 直线
        for (int i = 0; i < 2; i++)
        {
            Controller.RegisterElement($"Portal_{i}", new Element(2)
            {
                radius = C.PortalLineWidth,
                thicc = C.PortalLineThickness,
                fillIntensity = C.PortalFillIntensity,
                Enabled = false,
            });
        }

        // 注册线连扇形元素 (4个) - type=3 扇形
        for (int i = 0; i < 4; i++)
        {
            Controller.RegisterElement($"Wrath_{i}", new Element(3)
            {
                radius = C.WrathRadius,
                offY = C.WrathLength,
                thicc = C.WrathLineThickness,
                fillIntensity = C.WrathFillIntensity,
                includeRotation = true,
                refActorComparisonType = 2,
                Enabled = false,
            });
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == TriggerCastId)
        {
            _isMechanicActive = true;
            return;
        }

        if (!_isMechanicActive) return;

        if (castId == MeteorCastId)
        {
            if (!EzThrottler.Throttle("M11S_Meteor_Cast", 100)) return;
            _meteorCastCount++;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!_isMechanicActive) return;
        if (set.Action == null) return;

        var actionId = set.Action.Value.RowId;

        switch (actionId)
        {
            case MeteorLineId:
                _meteorLinePositions.Clear();
                break;

            case MeteorWrathId:
                _meteorWrathTethers.Clear();
                break;

            case FireBreathId:
                _fireBreathTargets.Clear();
                break;

            case MeteorCastId:
                if (!EzThrottler.Throttle("M11S_Meteor_End", 100)) return;
                _meteorEndCount++;
                if (_meteorEndCount == 3)
                {
                    _completeCount++;
                    if (_completeCount >= 2)
                        OnReset();
                    else
                        ResetState();
                }
                break;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (!_isMechanicActive) return;
        if (vfxPath != MarkerVfx) return;

        if (target.GetObject() is IPlayerCharacter player)
        {
            _fireBreathTargets.Add(player);
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if (!_isMechanicActive) return;

        if (position is >= 22 and <= 25 && data1 == 1 && MapEffectToPos.TryGetValue(position, out var pos))
        {
            _meteorLinePositions.Add(pos);
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (!_isMechanicActive) return;

        // 检查是否是我们关心的线连类型
        if (data3 != TetherDanger && data3 != TetherSafe) return;

        if (!source.TryGetObject(out var sourceObj) || !target.TryGetObject(out var targetObj))
            return;

        _meteorWrathTethers.Add(new TetherInfo(sourceObj, targetObj, data3 == TetherDanger));
    }

    public override void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
    {
        if (!_isMechanicActive) return;

        _meteorWrathTethers.RemoveAll(x => x.Source.EntityId == source);
    }

    public override void OnUpdate()
    {
        // 隐藏所有元素
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

        if (!_isMechanicActive) return;

        DrawPortalLines();
        DrawWrathAoE();
        DrawFireBreathAoE();
    }

    private void DrawPortalLines()
    {
        int i = 0;
        foreach (var pos in _meteorLinePositions)
        {
            if (i >= 2) break;

            if (Controller.TryGetElementByName($"Portal_{i}", out var line))
            {
                line.Enabled = true;
                line.refX = pos.X;
                line.refY = pos.Y;
                line.offX = pos.X;
                line.offY = pos.Y + 50f;

                // 即将生效时变红，否则橙色
                line.color = IsMechanicIncoming ? C.ColorOther : C.ColorPending;
                line.fillIntensity = IsMechanicIncoming ? 0.2f : C.PortalFillIntensity;
            }
            i++;
        }
    }

    private void DrawWrathAoE()
    {
        if (!IsMechanicIncoming) return;

        int i = 0;
        foreach (var tether in _meteorWrathTethers)
        {
            if (i >= 4) break;

            if (Controller.TryGetElementByName($"Wrath_{i}", out var e))
            {
                e.Enabled = true;
                e.refActorObjectID = tether.Source.EntityId;
                e.AdditionalRotation = GetRelativeAngle(tether.Source.Position.ToVector2(), tether.Target.Position.ToVector2()) + tether.Source.Rotation;

                // 自己的绿色，他人的红色
                e.color = tether.Target == Player.Object ? C.ColorSelf : C.ColorOther;
            }
            i++;
        }
    }

    private void DrawFireBreathAoE()
    {
        if (!IsMechanicIncoming) return;

        var boss = Svc.Objects.OfType<IBattleNpc>()
            .FirstOrDefault(x => x.BaseId == BossDataId);
        if (boss == null) return;

        int i = 0;
        foreach (var target in _fireBreathTargets)
        {
            if (i >= 4) break;

            if (Controller.TryGetElementByName($"Breath_{i}", out var e))
            {
                e.Enabled = true;
                e.refActorObjectID = boss.EntityId;
                e.AdditionalRotation = GetRelativeAngle(boss.Position.ToVector2(), target.Position.ToVector2()) + boss.Rotation;

                // 自己的绿色，他人的红色
                e.color = target == Player.Object ? C.ColorSelf : C.ColorOther;
            }
            i++;
        }
    }

    public override void OnReset()
    {
        _isMechanicActive = false;
        _completeCount = 0;
        ResetState();
    }

    private void ResetState()
    {
        _fireBreathTargets.Clear();
        _meteorLinePositions.Clear();
        _meteorWrathTethers.Clear();
        _meteorCastCount = 0;
        _meteorEndCount = 0;
    }

    private static float GetRelativeAngle(Vector2 origin, Vector2 target)
    {
        var vector2 = target - origin;
        var vector1 = new Vector2(0, 1);
        return MathF.Atan2(vector2.Y, vector2.X) - MathF.Atan2(vector1.Y, vector1.X);
    }

    #endregion

    #region 设置界面

    public override void OnSettingsDraw()
    {
        // === 火焰吐息设置 ===
        ImGui.Text("=== 火焰吐息 (VFX标记) ===");

        var breathWidth = C.BreathLineWidth;
        if (ImGui.SliderFloat("直线宽度##breath", ref breathWidth, 1f, 15f))
            C.BreathLineWidth = breathWidth;

        var breathLength = C.BreathLineLength;
        if (ImGui.SliderFloat("直线长度##breath", ref breathLength, 10f, 100f))
            C.BreathLineLength = breathLength;

        ImGui.Separator();

        // === 传送门直线设置 ===
        ImGui.Text("=== 传送门直线 (MapEffect) ===");

        var portalWidth = C.PortalLineWidth;
        if (ImGui.SliderFloat("直线宽度##portal", ref portalWidth, 1f, 15f))
            C.PortalLineWidth = portalWidth;

        ImGui.Separator();

        // === 线连扇形设置 ===
        ImGui.Text("=== 陨石愤怒 (线连) ===");

        var wrathRadius = C.WrathRadius;
        if (ImGui.SliderFloat("直线宽度##wrath", ref wrathRadius, 1f, 15f))
            C.WrathRadius = wrathRadius;

        var wrathLength = C.WrathLength;
        if (ImGui.SliderFloat("直线长度##wrath", ref wrathLength, 10f, 100f))
            C.WrathLength = wrathLength;

        ImGui.Separator();

        // === 颜色设置 ===
        ImGui.Text("=== 颜色设置 ===");

        var colorSelf = ImGuiEx.Vector4FromRGBA(C.ColorSelf);
        if (ImGui.ColorEdit4("自己的AOE", ref colorSelf))
            C.ColorSelf = colorSelf.ToUint();

        var colorOther = ImGuiEx.Vector4FromRGBA(C.ColorOther);
        if (ImGui.ColorEdit4("他人的AOE", ref colorOther))
            C.ColorOther = colorOther.ToUint();

        var colorPending = ImGuiEx.Vector4FromRGBA(C.ColorPending);
        if (ImGui.ColorEdit4("等待中的AOE", ref colorPending))
            C.ColorPending = colorPending.ToUint();

        ImGui.Separator();

        if (ImGui.Button("保存配置"))
            Controller.SaveConfig();

        ImGui.SameLine();
        if (ImGui.Button("重置状态"))
        {
            OnReset();
        }

        // 调试信息
        if (ImGui.CollapsingHeader("调试信息"))
        {
            ImGuiEx.Text($"机制激活: {_isMechanicActive}");
            ImGuiEx.Text($"陨石计数(开始): {_meteorCastCount}");
            ImGuiEx.Text($"陨石计数(结束): {_meteorEndCount}");
            ImGuiEx.Text($"完成轮数: {_completeCount}");
            ImGuiEx.Text($"即将生效: {IsMechanicIncoming}");

            ImGui.Separator();
            ImGuiEx.Text($"火焰吐息目标: {_fireBreathTargets.Count}");
            foreach (var t in _fireBreathTargets)
            {
                var isSelf = t == Player.Object;
                ImGuiEx.Text(isSelf ? EColor.GreenBright : EColor.White, $"  - {t.Name}{(isSelf ? " (自己)" : "")}");
            }

            ImGui.Separator();
            ImGuiEx.Text($"传送门直线: {_meteorLinePositions.Count}");
            foreach (var p in _meteorLinePositions)
            {
                ImGuiEx.Text($"  - X={p.X}, Y={p.Y}");
            }

            ImGui.Separator();
            ImGuiEx.Text($"陨石愤怒线连: {_meteorWrathTethers.Count}");
            foreach (var t in _meteorWrathTethers)
            {
                var isSelf = t.Target == Player.Object;
                ImGuiEx.Text(t.IsDanger ? EColor.RedBright : EColor.White,
                    $"  - {t.Source.Name} -> {t.Target.Name} ({(t.IsDanger ? "危险" : "安全")}){(isSelf ? " (自己)" : "")}");
            }

            ImGui.Separator();
            ImGuiEx.Text("MapEffect状态 (22-25):");
            foreach (var pos in new uint[] { 22, 23, 24, 25 })
            {
                var state = Controller.GetMapEffect(pos);
                ImGuiEx.Text($"  位置{pos}: State={state}");
            }
        }
    }

    #endregion
}
