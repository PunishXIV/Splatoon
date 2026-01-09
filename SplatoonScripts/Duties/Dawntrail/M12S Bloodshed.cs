using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M12S_Bloodshed : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1327];

    public override Metadata? Metadata => new(2, "Errer");

    #region 常量

    private const uint BossDataId = 19195;

    // 技能ID
    private const uint LeftKnockback = 0xB4CC;   // 46284 左击退
    private const uint RightKnockback = 0xB4CE;  // 46286 右击退
    private const uint LeftPoison = 0xB4CB;      // 46283 左喷毒
    private const uint RightPoison = 0xB4CD;     // 46285 右喷毒

    #endregion

    #region 状态变量

    private enum MechanicType { LeftPoison, RightPoison, LeftKnockback, RightKnockback }

    private record struct MechanicState(MechanicType Type, long TriggerTime, bool IsSecond);

    private readonly List<MechanicState> _activeMechanics = new();

    #endregion

    #region 配置

    private Config C => Controller.GetConfig<Config>();

    public class Config : IEzConfig
    {
        public int DelayMs = 10000;          // 延迟绘制时间(毫秒)
        public int DurationMs = 4000;        // 持续显示时间(毫秒)
        public int SecondDelayMs = 1000;     // 第二次绘制延迟(毫秒)
        public float CleaveFillIntensity = 0.5f;
        public float CleaveThickness = 4.0f;
        public uint CleaveColor = 0xC8FF5500;  // 橙色
        public uint KnockbackColor = 0xC8FF5500;  // 橙色
    }

    #endregion

    #region 生命周期方法

    public override void OnSetup()
    {
        // 半场刀 - 左
        Controller.RegisterElement("Cleave_Left", new Element(2)
        {
            refX = 90f,
            refY = 85f,
            offX = 90f,
            offY = 115f,
            radius = 10f,
            fillIntensity = C.CleaveFillIntensity,
            thicc = C.CleaveThickness,
            Enabled = false,
        });

        // 半场刀 - 右
        Controller.RegisterElement("Cleave_Right", new Element(2)
        {
            refX = 110f,
            refY = 85f,
            offX = 110f,
            offY = 115f,
            radius = 10f,
            fillIntensity = C.CleaveFillIntensity,
            thicc = C.CleaveThickness,
            Enabled = false,
        });

        // 左击退
        Controller.RegisterElement("Knockback_Left", new Element(5)
        {
            refX = 82f,
            refY = 89f,
            radius = 8.45f,
            coneAngleMin = -60,
            coneAngleMax = -40,
            color = C.KnockbackColor,
            fillIntensity = 0.5f,
            thicc = 4.1f,
            overlayText = "击退",
            overlayFScale = 2.0f,
            includeRotation = true,
            Enabled = false,
        });

        // 右击退
        Controller.RegisterElement("Knockback_Right", new Element(5)
        {
            refX = 118f,
            refY = 89f,
            radius = 8.45f,
            coneAngleMin = 40,
            coneAngleMax = 60,
            color = C.KnockbackColor,
            fillIntensity = 0.5f,
            thicc = 4.1f,
            overlayText = "击退",
            overlayFScale = 2.0f,
            includeRotation = true,
            Enabled = false,
        });
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Source is not IBattleNpc npc || npc.BaseId != BossDataId)
            return;

        if (set.Action == null) return;

        var actionId = set.Action.Value.RowId;
        var now = Environment.TickCount64;

        switch (actionId)
        {
            case LeftPoison:
                _activeMechanics.Add(new MechanicState(MechanicType.LeftPoison, now, false));
                break;
            case RightPoison:
                _activeMechanics.Add(new MechanicState(MechanicType.RightPoison, now, false));
                break;
            case LeftKnockback:
                _activeMechanics.Add(new MechanicState(MechanicType.LeftKnockback, now, false));
                break;
            case RightKnockback:
                _activeMechanics.Add(new MechanicState(MechanicType.RightKnockback, now, false));
                break;
        }
    }

    public override void OnUpdate()
    {
        // 隐藏所有元素
        if (Controller.TryGetElementByName("Cleave_Left", out var cleaveLeft))
            cleaveLeft.Enabled = false;
        if (Controller.TryGetElementByName("Cleave_Right", out var cleaveRight))
            cleaveRight.Enabled = false;
        if (Controller.TryGetElementByName("Knockback_Left", out var kbLeft))
            kbLeft.Enabled = false;
        if (Controller.TryGetElementByName("Knockback_Right", out var kbRight))
            kbRight.Enabled = false;

        var now = Environment.TickCount64;

        // 检查第一次绘制是否结束，需要添加第二次绘制
        var toAdd = new List<MechanicState>();
        var toRemove = new List<MechanicState>();

        foreach (var mechanic in _activeMechanics)
        {
            var delay = mechanic.IsSecond ? C.SecondDelayMs : C.DelayMs;
            var elapsed = now - mechanic.TriggerTime;

            // 超过持续时间后，检查是否需要添加第二次
            if (elapsed > delay + C.DurationMs)
            {
                toRemove.Add(mechanic);

                // 如果是第一次，添加相反方向的第二次
                if (!mechanic.IsSecond)
                {
                    var oppositeType = GetOppositeType(mechanic.Type);
                    toAdd.Add(new MechanicState(oppositeType, now, true));
                }
            }
        }

        foreach (var m in toRemove)
            _activeMechanics.Remove(m);
        foreach (var m in toAdd)
            _activeMechanics.Add(m);

        // 遍历所有活跃机制进行绘制
        foreach (var mechanic in _activeMechanics)
        {
            var delay = mechanic.IsSecond ? C.SecondDelayMs : C.DelayMs;
            var elapsed = now - mechanic.TriggerTime;

            // 延迟期间不绘制
            if (elapsed < delay) continue;

            // 根据机制类型绘制
            switch (mechanic.Type)
            {
                case MechanicType.LeftPoison:
                    if (cleaveLeft != null)
                    {
                        cleaveLeft.color = C.CleaveColor;
                        cleaveLeft.fillIntensity = C.CleaveFillIntensity;
                        cleaveLeft.thicc = C.CleaveThickness;
                        cleaveLeft.Enabled = true;
                    }
                    break;

                case MechanicType.RightPoison:
                    if (cleaveRight != null)
                    {
                        cleaveRight.color = C.CleaveColor;
                        cleaveRight.fillIntensity = C.CleaveFillIntensity;
                        cleaveRight.thicc = C.CleaveThickness;
                        cleaveRight.Enabled = true;
                    }
                    break;

                case MechanicType.LeftKnockback:
                    if (kbLeft != null)
                    {
                        kbLeft.color = C.KnockbackColor;
                        kbLeft.Enabled = true;
                    }
                    break;

                case MechanicType.RightKnockback:
                    if (kbRight != null)
                    {
                        kbRight.color = C.KnockbackColor;
                        kbRight.Enabled = true;
                    }
                    break;
            }
        }
    }

    private static MechanicType GetOppositeType(MechanicType type) => type switch
    {
        MechanicType.LeftPoison => MechanicType.RightPoison,
        MechanicType.RightPoison => MechanicType.LeftPoison,
        MechanicType.LeftKnockback => MechanicType.RightKnockback,
        MechanicType.RightKnockback => MechanicType.LeftKnockback,
        _ => type
    };

    public override void OnReset()
    {
        _activeMechanics.Clear();
    }

    #endregion

    #region 设置界面

    public override void OnSettingsDraw()
    {
        ImGui.Text("M12S Bloodshed 半场刀/击退");

        ImGui.Separator();
        ImGui.Text("时间设置:");

        var delay = C.DelayMs;
        if (ImGui.SliderInt("第一次延迟(ms)", ref delay, 0, 15000))
            C.DelayMs = delay;

        var duration = C.DurationMs;
        if (ImGui.SliderInt("持续时间(ms)", ref duration, 1000, 10000))
            C.DurationMs = duration;

        var secondDelay = C.SecondDelayMs;
        if (ImGui.SliderInt("第二次延迟(ms)", ref secondDelay, 0, 5000))
            C.SecondDelayMs = secondDelay;

        ImGui.Separator();
        ImGui.Text("半场刀设置:");

        var cleaveFill = C.CleaveFillIntensity;
        if (ImGui.SliderFloat("填充透明度##cleave", ref cleaveFill, 0.1f, 1f))
            C.CleaveFillIntensity = cleaveFill;

        var cleaveThick = C.CleaveThickness;
        if (ImGui.SliderFloat("线条粗细##cleave", ref cleaveThick, 1f, 10f))
            C.CleaveThickness = cleaveThick;

        var cleaveColor = ImGuiEx.Vector4FromRGBA(C.CleaveColor);
        if (ImGui.ColorEdit4("半场刀颜色", ref cleaveColor))
            C.CleaveColor = ImGui.ColorConvertFloat4ToU32(cleaveColor);

        ImGui.Separator();
        ImGui.Text("击退设置:");

        var kbColor = ImGuiEx.Vector4FromRGBA(C.KnockbackColor);
        if (ImGui.ColorEdit4("击退颜色", ref kbColor))
            C.KnockbackColor = ImGui.ColorConvertFloat4ToU32(kbColor);

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
            ImGuiEx.Text($"活跃机制数: {_activeMechanics.Count}");
            var now = Environment.TickCount64;
            foreach (var mechanic in _activeMechanics)
            {
                var mechanicDelay = mechanic.IsSecond ? C.SecondDelayMs : C.DelayMs;
                var elapsed = now - mechanic.TriggerTime;
                var state = elapsed < mechanicDelay ? "等待中" :
                           elapsed < mechanicDelay + C.DurationMs ? "绘制中" : "已结束";
                var phase = mechanic.IsSecond ? "[第二次]" : "[第一次]";
                ImGuiEx.Text($"  - {phase} {mechanic.Type}: {state} ({elapsed}ms)");
            }

            ImGui.Separator();
            ImGui.Text("技能ID对照:");
            ImGui.Text($"  左喷毒: 0xB4CB ({LeftPoison})");
            ImGui.Text($"  右喷毒: 0xB4CD ({RightPoison})");
            ImGui.Text($"  左击退: 0xB4CC ({LeftKnockback})");
            ImGui.Text($"  右击退: 0xB4CE ({RightKnockback})");
        }
    }

    #endregion
}
