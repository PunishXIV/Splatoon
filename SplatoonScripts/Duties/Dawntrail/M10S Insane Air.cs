using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M10S_Insane_Air : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1323];

    public override Metadata? Metadata => new(3, "Errer");

    #region 数据结构

    private enum MarkerDirection
    {
        Up,     // 左上 ↖
        Middle, // 正左 ←
        Down    // 左下 ↙
    }

    // Position 到 Vector3 坐标映射 (3x3 网格)
    private static readonly Dictionary<uint, Vector3> PositionToCoord = new()
    {
        { 14, new Vector3(87f, 0f, 87f) },   // 左下角
        { 15, new Vector3(100f, 0f, 87f) },  // 下排中间
        { 16, new Vector3(113f, 0f, 87f) },  // 右下角
        { 17, new Vector3(87f, 0f, 100f) },  // 左排中间
        { 18, new Vector3(100f, 0f, 100f) }, // 正中心
        { 19, new Vector3(113f, 0f, 100f) }, // 右排中间
        { 20, new Vector3(87f, 0f, 113f) },  // 左上角
        { 21, new Vector3(100f, 0f, 113f) }, // 上排中间
        { 22, new Vector3(113f, 0f, 113f) }, // 右上角
    };

    // Data2 到方向的映射
    private static readonly Dictionary<ushort, MarkerDirection> Data2ToDirection = new()
    {
        // 水标记
        { 2, MarkerDirection.Down },      // 水-左下
        { 32, MarkerDirection.Middle },   // 水-正左
        { 128, MarkerDirection.Up },      // 水-左上
        // 火标记
        { 512, MarkerDirection.Down },    // 火-左下
        { 2048, MarkerDirection.Middle }, // 火-正左
        { 8192, MarkerDirection.Up },     // 火-左上
    };

    // 火标记的data2值集合（用于判断是否绘制圆形）
    private static readonly HashSet<ushort> FireMarkerData2 = new() { 512, 2048, 8192 };

    // 存储每个位置是火还是水标记
    private readonly Dictionary<uint, bool> _isFireMarker = new();

    // 存储活跃标记和时间信息 (触发时间, 方向)
    private readonly Dictionary<uint, (MarkerDirection direction, long triggerAt)> _activeMarkers = new();

    #endregion

    #region 配置

    private Config C => Controller.GetConfig<Config>();

    public class Config : IEzConfig
    {
        public float ConeRadius = 25.0f;       // 扇形半径
        public int ConeAngle = 25;             // 扇形半角 (总角度50° = 2 * 25)
        public float FillIntensity = 0.35f;    // 填充透明度
        public int DelayMs = 3000;             // 触发后延迟绘制时间（毫秒）
        public int DurationMs = 6500;          // 绘制持续时间（毫秒）
        public int TimeoutMs = 9500;           // 超时时间（毫秒）
        public float LineThickness = 3.0f;     // 线条粗细
        public float CircleRadius = 8.0f;      // 火标记上方向圆形半径

        // 颜色配置 (ARGB)
        public uint ColorUp = 0xC8FF0000;      // 红色 (上方向)
        public uint ColorMiddle = 0xC8FFFF00;  // 黄色 (中方向)
        public uint ColorDown = 0xC800FF00;    // 绿色 (下方向)
    }

    #endregion

    #region 生命周期方法

    public override void OnSetup()
    {
        // 9个位置 × 最多4个扇形 = 36个扇形元素
        // 命名格式: Cone_{position}_{index}
        for (uint pos = 14; pos <= 22; pos++)
        {
            for (int i = 0; i < 4; i++)
            {
                Controller.RegisterElement($"Cone_{pos}_{i}", new Element(5) // type 5 = 固定坐标锥形
                {
                    radius = C.ConeRadius,
                    coneAngleMin = -C.ConeAngle,
                    coneAngleMax = C.ConeAngle,
                    Filled = true,
                    fillIntensity = C.FillIntensity,
                    thicc = C.LineThickness,
                    includeRotation = true,
                    FaceMe = true,
                    Enabled = false,
                });
            }

            // 为每个位置注册一个圆形元素（用于上方向死刑）
            Controller.RegisterElement($"Circle_{pos}", new Element(1) // type 1 = 跟随Actor圆形
            {
                radius = C.CircleRadius,
                refActorComparisonType = 2, // 按ObjectID匹配
                Filled = true,
                fillIntensity = C.FillIntensity,
                thicc = C.LineThickness,
                Enabled = false,
            });
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        // 验证 position 有效 (14-22)
        if (position < 14 || position > 22) return;

        // 验证 data2 是有效的水火标记值
        if (!Data2ToDirection.TryGetValue(data2, out var direction)) return;

        // 记录是否为火标记
        _isFireMarker[position] = FireMarkerData2.Contains(data2);

        // 添加/更新标记，记录触发时间
        _activeMarkers[position] = (direction, Environment.TickCount64);
    }

    public override void OnUpdate()
    {
        var now = Environment.TickCount64;

        // 清理超时标记
        var expiredKeys = _activeMarkers
            .Where(x => now - x.Value.triggerAt > C.TimeoutMs)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _activeMarkers.Remove(key);
            _isFireMarker.Remove(key);
            // 隐藏该位置的扇形和圆形
            for (int i = 0; i < 4; i++)
            {
                if (Controller.TryGetElementByName($"Cone_{key}_{i}", out var cone))
                    cone.Enabled = false;
            }
            if (Controller.TryGetElementByName($"Circle_{key}", out var circle))
                circle.Enabled = false;
        }

        if (_activeMarkers.Count == 0) return;

        foreach (var (position, (direction, triggerAt)) in _activeMarkers)
        {
            var elapsed = now - triggerAt;
            // 上方向都是圆形死刑（火标记和水标记都一样）
            var isUpDirection = direction == MarkerDirection.Up;

            // 延迟期间不绘制
            if (elapsed < C.DelayMs)
            {
                // 确保扇形和圆形隐藏
                for (int i = 0; i < 4; i++)
                {
                    if (Controller.TryGetElementByName($"Cone_{position}_{i}", out var cone))
                        cone.Enabled = false;
                }
                if (Controller.TryGetElementByName($"Circle_{position}", out var circle))
                    circle.Enabled = false;
                continue;
            }

            // 超过绘制持续时间后隐藏
            if (elapsed > C.DelayMs + C.DurationMs)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Controller.TryGetElementByName($"Cone_{position}_{i}", out var cone))
                        cone.Enabled = false;
                }
                if (Controller.TryGetElementByName($"Circle_{position}", out var circle))
                    circle.Enabled = false;
                continue;
            }

            // 在绘制时间窗口内
            if (!PositionToCoord.TryGetValue(position, out var coord)) continue;

            // 根据方向确定扇形数量和颜色
            // 上=死刑(1个), 中=分摊(1个), 下=分散(4个)
            int coneCount = direction == MarkerDirection.Down ? 4 : 1;
            uint color = direction switch
            {
                MarkerDirection.Up => C.ColorUp,
                MarkerDirection.Middle => C.ColorMiddle,
                MarkerDirection.Down => C.ColorDown,
                _ => 0xC8FFFFFF
            };

            // 获取距离该位置最近的玩家
            var nearestPlayers = FakeParty.Get()
                .OrderBy(p => Vector3.Distance(coord, p.Position))
                .Take(coneCount)
                .ToList();

            // 上方向：绘制圆形死刑（火/水标记都一样）
            if (isUpDirection)
            {
                // 隐藏所有扇形
                for (int i = 0; i < 4; i++)
                {
                    if (Controller.TryGetElementByName($"Cone_{position}_{i}", out var cone))
                        cone.Enabled = false;
                }

                // 绘制圆形在最近玩家位置
                if (nearestPlayers.Count > 0 && Controller.TryGetElementByName($"Circle_{position}", out var circle))
                {
                    circle.refActorObjectID = nearestPlayers[0].EntityId;
                    circle.color = color;
                    circle.radius = C.CircleRadius;
                    circle.fillIntensity = C.FillIntensity;
                    circle.thicc = C.LineThickness;
                    circle.Enabled = true;
                }
            }
            else
            {
                // 隐藏圆形
                if (Controller.TryGetElementByName($"Circle_{position}", out var circle))
                    circle.Enabled = false;

                // 更新该位置的扇形
                for (int i = 0; i < 4; i++)
                {
                    if (Controller.TryGetElementByName($"Cone_{position}_{i}", out var cone))
                    {
                        if (i < coneCount && i < nearestPlayers.Count)
                        {
                            cone.SetRefPosition(coord);
                            cone.faceplayer = $"<{GetPlayerOrder(nearestPlayers[i])}>";
                            cone.color = color;
                            cone.radius = C.ConeRadius;
                            cone.coneAngleMin = -C.ConeAngle;
                            cone.coneAngleMax = C.ConeAngle;
                            cone.fillIntensity = C.FillIntensity;
                            cone.thicc = C.LineThickness;
                            cone.Enabled = true;
                        }
                        else
                        {
                            cone.Enabled = false;
                        }
                    }
                }
            }
        }
    }

    public override void OnReset()
    {
        _activeMarkers.Clear();
        _isFireMarker.Clear();
        HideAllElements();
    }

    #endregion

    #region 辅助方法

    private unsafe int GetPlayerOrder(IPlayerCharacter player)
    {
        for (var i = 1; i <= 8; i++)
        {
            if ((nint)FakePronoun.Resolve($"<{i}>") == player.Address)
                return i;
        }
        return 0;
    }

    private void HideAllElements()
    {
        for (uint pos = 14; pos <= 22; pos++)
        {
            for (int i = 0; i < 4; i++)
            {
                if (Controller.TryGetElementByName($"Cone_{pos}_{i}", out var cone))
                    cone.Enabled = false;
            }
            if (Controller.TryGetElementByName($"Circle_{pos}", out var circle))
                circle.Enabled = false;
        }
    }

    #endregion

    #region 设置界面

    public override void OnSettingsDraw()
    {
        ImGui.Text("扇形参数:");

        var radius = C.ConeRadius;
        if (ImGui.SliderFloat("扇形半径", ref radius, 5f, 50f))
            C.ConeRadius = radius;

        var angle = C.ConeAngle;
        if (ImGui.SliderInt("角度 (半角)", ref angle, 10, 90))
            C.ConeAngle = angle;
        ImGui.SameLine();
        ImGui.TextDisabled($"(总角度: {angle * 2}°)");

        var circleRadius = C.CircleRadius;
        if (ImGui.SliderFloat("圆形半径 (火标记上)", ref circleRadius, 1f, 10f))
            C.CircleRadius = circleRadius;

        var fill = C.FillIntensity;
        if (ImGui.SliderFloat("填充透明度", ref fill, 0.1f, 1f))
            C.FillIntensity = fill;

        var thickness = C.LineThickness;
        if (ImGui.SliderFloat("线条粗细", ref thickness, 1f, 10f))
            C.LineThickness = thickness;

        ImGui.Separator();
        ImGui.Text("时间设置:");

        var delay = C.DelayMs;
        if (ImGui.SliderInt("延迟绘制(ms)", ref delay, 0, 10000))
            C.DelayMs = delay;

        var duration = C.DurationMs;
        if (ImGui.SliderInt("持续时间(ms)", ref duration, 1000, 15000))
            C.DurationMs = duration;

        var timeout = C.TimeoutMs;
        if (ImGui.SliderInt("超时清除(ms)", ref timeout, 1000, 30000))
            C.TimeoutMs = timeout;

        ImGui.Separator();
        ImGui.Text("颜色设置:");

        // 上方向 - 红色
        var colorUp = ImGuiEx.Vector4FromRGBA(C.ColorUp);
        if (ImGui.ColorEdit4("上方向 (红)", ref colorUp))
            C.ColorUp = colorUp.ToUint();

        // 中方向 - 黄色
        var colorMiddle = ImGuiEx.Vector4FromRGBA(C.ColorMiddle);
        if (ImGui.ColorEdit4("中方向 (黄)", ref colorMiddle))
            C.ColorMiddle = colorMiddle.ToUint();

        // 下方向 - 绿色
        var colorDown = ImGuiEx.Vector4FromRGBA(C.ColorDown);
        if (ImGui.ColorEdit4("下方向 (绿)", ref colorDown))
            C.ColorDown = colorDown.ToUint();

        ImGui.Separator();

        if (ImGui.Button("保存配置"))
            Controller.SaveConfig();

        ImGui.SameLine();
        if (ImGui.Button("清除所有标记"))
        {
            _activeMarkers.Clear();
            _isFireMarker.Clear();
            HideAllElements();
        }

        // 调试信息
        if (ImGui.CollapsingHeader("调试信息"))
        {
            ImGuiEx.Text($"活跃标记数: {_activeMarkers.Count}");
            var now = Environment.TickCount64;
            foreach (var (pos, (dir, triggerAt)) in _activeMarkers)
            {
                var elapsed = now - triggerAt;
                var state = elapsed < C.DelayMs ? "等待中" :
                           elapsed < C.DelayMs + C.DurationMs ? "绘制中" : "已结束";
                ImGuiEx.Text($"  Position {pos}: {dir} - {state} ({elapsed}ms)");
            }
        }
    }

    #endregion
}
