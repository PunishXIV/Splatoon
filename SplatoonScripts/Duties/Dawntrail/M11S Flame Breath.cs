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
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M11S_Flame_Breath : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1325];

    public override Metadata? Metadata => new(9, "Errer");

    #region 常量

    // 火焰吐息相关
    private const uint BossDataId = 19180;
    private const uint TriggerCastId = 46143;  // 触发读条ID
    private const string MarkerVfx = "vfx/lockon/eff/lockon8_t0w.avfx";

    // 传送门直线 MapEffect位置对应的直线X坐标
    // 22=正左(X=79), 23=正右(X=89), 24=正上(X=111), 25=正下(X=121)
    private static readonly Dictionary<uint, float> MapEffectToX = new()
    {
        { 22, 79f },   // 正左
        { 23, 89f },   // 左2
        { 24, 111f },  // 右1
        { 25, 121f },  // 右2
    };

    #endregion

    #region 状态变量

    // 公共状态
    private bool _isCasting = false;  // 是否已触发读条46143

    // 火焰吐息状态 (VFX标记)
    private readonly HashSet<uint> _markedPlayers = new();
    private long _vfxTime = 0;
    private bool _vfxActive = false;

    // 传送门直线状态 (MapEffect)
    private long _mapEffectTime = 0;
    private readonly HashSet<uint> _activeMapEffects = new();

    #endregion

    #region 配置

    private Config C => Controller.GetConfig<Config>();

    public class Config : IEzConfig
    {
        // 火焰吐息配置
        public float BreathLineWidth = 3.0f;
        public float BreathLineLength = 30.0f;
        public int BreathDelayMs = 5000;
        public int BreathDurationMs = 3700;
        public float BreathFillIntensity = 0.35f;
        public float BreathLineThickness = 3.0f;
        public uint BreathLineColor = 0xC8FF0000;

        // 传送门直线配置
        public float PortalLineWidth = 5.0f;
        public int PortalDelayMs = 23000;
        public int PortalDurationMs = 5000;
        public float PortalFillIntensity = 0.35f;
        public float PortalLineThickness = 3.0f;
        public uint PortalLineColor = 0xC8FF0000;
    }

    #endregion

    #region 生命周期方法

    public override void OnSetup()
    {
        // 注册火焰吐息直线元素 (4个)
        for (int i = 0; i < 4; i++)
        {
            Controller.RegisterElement($"Breath_{i}", new Element(2)
            {
                radius = C.BreathLineWidth,
                thicc = C.BreathLineThickness,
                Filled = true,
                fillIntensity = C.BreathFillIntensity,
                Enabled = false,
            });
        }

        // 注册传送门直线元素 (4个)
        for (int i = 0; i < 4; i++)
        {
            Controller.RegisterElement($"Portal_{i}", new Element(2)
            {
                radius = C.PortalLineWidth,
                thicc = C.PortalLineThickness,
                Filled = true,
                fillIntensity = C.PortalFillIntensity,
                Enabled = false,
            });
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId != TriggerCastId) return;
        _isCasting = true;
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        // 只有在读条46143后才监听VFX
        if (!_isCasting) return;
        if (vfxPath != MarkerVfx) return;

        // 验证目标是玩家
        if (target.GetObject() is not IPlayerCharacter player) return;

        // 记录被点名玩家
        _markedPlayers.Add(player.EntityId);

        // 首次检测到VFX时记录时间并激活
        if (!_vfxActive)
        {
            _vfxActive = true;
            _vfxTime = Environment.TickCount64;
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        // 只有在读条46143后才监听MapEffect
        if (!_isCasting) return;

        // 检查是否是我们关心的MapEffect位置 (22, 23, 24, 25)
        if (!MapEffectToX.ContainsKey(position)) return;

        // data1=1, data2=2 表示激活
        if (data1 == 1 && data2 == 2)
        {
            // 首次检测到MapEffect时记录时间
            if (_activeMapEffects.Count == 0)
            {
                _mapEffectTime = Environment.TickCount64;
            }
            _activeMapEffects.Add(position);
        }
    }

    public override void OnUpdate()
    {
        // 隐藏所有元素（默认状态）
        for (int i = 0; i < 4; i++)
        {
            if (Controller.TryGetElementByName($"Breath_{i}", out var breath))
                breath.Enabled = false;
            if (Controller.TryGetElementByName($"Portal_{i}", out var portal))
                portal.Enabled = false;
        }

        // 更新火焰吐息绘制
        UpdateBreathLines();

        // 更新传送门直线绘制
        UpdatePortalLines();
    }

    private void UpdateBreathLines()
    {
        if (!_vfxActive || _markedPlayers.Count == 0) return;

        var now = Environment.TickCount64;
        var elapsed = now - _vfxTime;

        // 延迟期间不绘制
        if (elapsed < C.BreathDelayMs) return;

        // 超过持续时间后清除本轮
        if (elapsed > C.BreathDelayMs + C.BreathDurationMs)
        {
            ClearBreathRound();
            return;
        }

        // 获取Boss
        var boss = Svc.Objects.OfType<IBattleNpc>()
            .FirstOrDefault(x => x.BaseId == BossDataId && x.IsTargetable);
        if (boss == null) return;

        // 绘制直线AOE
        int index = 0;
        foreach (var entityId in _markedPlayers)
        {
            if (index >= 4) break;

            var player = Svc.Objects.FirstOrDefault(x => x.EntityId == entityId);
            if (player == null) continue;

            if (Controller.TryGetElementByName($"Breath_{index}", out var line))
            {
                // 计算从Boss到玩家的方向，并延长到指定长度
                var direction = Vector3.Normalize(player.Position - boss.Position);
                var endPosition = boss.Position + direction * C.BreathLineLength;

                line.SetRefPosition(boss.Position);
                line.SetOffPosition(endPosition);
                line.color = C.BreathLineColor;
                line.radius = C.BreathLineWidth;
                line.fillIntensity = C.BreathFillIntensity;
                line.thicc = C.BreathLineThickness;
                line.Enabled = true;
            }
            index++;
        }
    }

    private void UpdatePortalLines()
    {
        if (_activeMapEffects.Count == 0) return;

        var now = Environment.TickCount64;
        var elapsed = now - _mapEffectTime;

        // 延迟期间不绘制
        if (elapsed < C.PortalDelayMs) return;

        // 超过持续时间后清除本轮
        if (elapsed > C.PortalDelayMs + C.PortalDurationMs)
        {
            ClearPortalRound();
            return;
        }

        // 绘制直线AOE
        int index = 0;
        foreach (var position in _activeMapEffects)
        {
            if (index >= 4) break;
            if (!MapEffectToX.TryGetValue(position, out var xPos)) continue;

            if (Controller.TryGetElementByName($"Portal_{index}", out var line))
            {
                // 绘制南北方向的直线 (Y从80到120)
                line.SetRefPosition(new Vector3(xPos, 0, 80f));
                line.SetOffPosition(new Vector3(xPos, 0, 120f));
                line.color = C.PortalLineColor;
                line.radius = C.PortalLineWidth;
                line.fillIntensity = C.PortalFillIntensity;
                line.thicc = C.PortalLineThickness;
                line.Enabled = true;
            }
            index++;
        }
    }

    public override void OnReset()
    {
        _isCasting = false;

        // 重置火焰吐息
        _markedPlayers.Clear();
        _vfxTime = 0;
        _vfxActive = false;

        // 重置传送门直线
        _mapEffectTime = 0;
        _activeMapEffects.Clear();

        for (int i = 0; i < 4; i++)
        {
            if (Controller.TryGetElementByName($"Breath_{i}", out var breath))
                breath.Enabled = false;
            if (Controller.TryGetElementByName($"Portal_{i}", out var portal))
                portal.Enabled = false;
        }
    }

    private void ClearBreathRound()
    {
        _markedPlayers.Clear();
        _vfxTime = 0;
        _vfxActive = false;
        // 不重置 _isCasting，继续监听

        for (int i = 0; i < 4; i++)
        {
            if (Controller.TryGetElementByName($"Breath_{i}", out var line))
                line.Enabled = false;
        }
    }

    private void ClearPortalRound()
    {
        _mapEffectTime = 0;
        _activeMapEffects.Clear();
        // 不重置 _isCasting，继续监听

        for (int i = 0; i < 4; i++)
        {
            if (Controller.TryGetElementByName($"Portal_{i}", out var line))
                line.Enabled = false;
        }
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
        if (ImGui.SliderFloat("直线长度##breath", ref breathLength, 10f, 50f))
            C.BreathLineLength = breathLength;

        var breathDelay = C.BreathDelayMs;
        if (ImGui.SliderInt("延迟绘制(ms)##breath", ref breathDelay, 0, 10000))
            C.BreathDelayMs = breathDelay;

        var breathDuration = C.BreathDurationMs;
        if (ImGui.SliderInt("持续时间(ms)##breath", ref breathDuration, 1000, 10000))
            C.BreathDurationMs = breathDuration;

        var breathColor = ImGuiEx.Vector4FromRGBA(C.BreathLineColor);
        if (ImGui.ColorEdit4("直线颜色##breath", ref breathColor))
            C.BreathLineColor = breathColor.ToUint();

        ImGui.Separator();

        // === 传送门直线设置 ===
        ImGui.Text("=== 传送门直线 (MapEffect) ===");

        var portalWidth = C.PortalLineWidth;
        if (ImGui.SliderFloat("直线宽度##portal", ref portalWidth, 1f, 15f))
            C.PortalLineWidth = portalWidth;

        var portalDelay = C.PortalDelayMs;
        if (ImGui.SliderInt("延迟绘制(ms)##portal", ref portalDelay, 0, 30000))
            C.PortalDelayMs = portalDelay;

        var portalDuration = C.PortalDurationMs;
        if (ImGui.SliderInt("持续时间(ms)##portal", ref portalDuration, 1000, 10000))
            C.PortalDurationMs = portalDuration;

        var portalColor = ImGuiEx.Vector4FromRGBA(C.PortalLineColor);
        if (ImGui.ColorEdit4("直线颜色##portal", ref portalColor))
            C.PortalLineColor = portalColor.ToUint();

        ImGui.Separator();

        if (ImGui.Button("保存配置"))
            Controller.SaveConfig();

        ImGui.SameLine();
        if (ImGui.Button("清除标记"))
        {
            OnReset();
        }

        // 调试信息
        if (ImGui.CollapsingHeader("调试信息"))
        {
            ImGuiEx.Text($"读条监听: {_isCasting}");

            ImGui.Separator();
            ImGuiEx.Text("火焰吐息:");
            ImGuiEx.Text($"  VFX激活: {_vfxActive}");
            ImGuiEx.Text($"  被点名玩家数: {_markedPlayers.Count}");
            if (_vfxActive)
            {
                var elapsed = Environment.TickCount64 - _vfxTime;
                var state = elapsed < C.BreathDelayMs ? "等待中" :
                           elapsed < C.BreathDelayMs + C.BreathDurationMs ? "绘制中" : "已结束";
                ImGuiEx.Text($"  状态: {state} ({elapsed}ms)");
            }

            ImGui.Separator();
            ImGuiEx.Text("传送门直线:");
            ImGuiEx.Text($"  激活MapEffect数: {_activeMapEffects.Count}");
            if (_activeMapEffects.Count > 0)
            {
                ImGuiEx.Text($"  激活位置: {string.Join(", ", _activeMapEffects)}");
                var elapsed = Environment.TickCount64 - _mapEffectTime;
                var state = elapsed < C.PortalDelayMs ? "等待中" :
                           elapsed < C.PortalDelayMs + C.PortalDurationMs ? "绘制中" : "已结束";
                ImGuiEx.Text($"  状态: {state} ({elapsed}ms)");
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
