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
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M12S_Bloodshed : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1327];

    public override Metadata? Metadata => new(6, "Errer");

    #region 常量

    private const uint BossDataId = 19195;

    // 技能ID
    private const uint LeftKnockback = 0xB4CC;   // 46284 左击退 Left knockback
    private const uint RightKnockback = 0xB4CE;  // 46286 右击退 Right knockback
    private const uint LeftPoison = 0xB4CB;      // 46283 左喷毒 Left AOE
    private const uint RightPoison = 0xB4CD;     // 46285 右喷毒 Right AOE
    private const uint TriggerCastId = 0xB495;   // 46229 启动读条 Start cast

    #endregion

    #region 状态变量

    private enum MechanicType { LeftPoison, RightPoison, LeftKnockback, RightKnockback }

    private record struct MechanicState(MechanicType Type, long TriggerTime, bool IsSecond);

    private readonly List<MechanicState> _activeMechanics = new();
    private int _triggerCastCount = 0;
    private bool _isActive = false;

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
        public Vector4 CleaveColorV4 = 0xC8FF5500.ToVector4();  // 橙色
        public Vector4 KnockbackColorV4 = 0xC8FF5500.ToVector4();  // 橙色
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
            color = C.KnockbackColorV4.ToUint(),
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
            color = C.KnockbackColorV4.ToUint(),
            fillIntensity = 0.5f,
            thicc = 4.1f,
            overlayText = "击退",
            overlayFScale = 2.0f,
            includeRotation = true,
            Enabled = false,
        });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId != TriggerCastId) return;

        _triggerCastCount++;
        if (_triggerCastCount >= 2)
        {
            _isActive = true;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!_isActive) return;

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
                        cleaveLeft.color = C.CleaveColorV4.ToUint();
                        cleaveLeft.fillIntensity = C.CleaveFillIntensity;
                        cleaveLeft.thicc = C.CleaveThickness;
                        cleaveLeft.Enabled = true;
                    }
                    break;

                case MechanicType.RightPoison:
                    if (cleaveRight != null)
                    {
                        cleaveRight.color = C.CleaveColorV4.ToUint() ;
                        cleaveRight.fillIntensity = C.CleaveFillIntensity;
                        cleaveRight.thicc = C.CleaveThickness;
                        cleaveRight.Enabled = true;
                    }
                    break;

                case MechanicType.LeftKnockback:
                    if (kbLeft != null)
                    {
                        kbLeft.color = C.KnockbackColorV4.ToUint();
                        kbLeft.Enabled = true;
                    }
                    break;

                case MechanicType.RightKnockback:
                    if (kbRight != null)
                    {
                        kbRight.color = C.KnockbackColorV4.ToUint();
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
        _triggerCastCount = 0;
        _isActive = false;
    }

    #endregion

    #region 设置界面

    public override void OnSettingsDraw()
    {
        ImGui.Text("M12S Bloodshed 半场刀/击退/AOE/Knockback");

        ImGui.Separator();
        ImGui.Text("时间设置/Timings:");

        var delay = C.DelayMs;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderInt("第一次延迟/First mechanic delay(ms)", ref delay, 0, 15000))
            C.DelayMs = delay;

        var duration = C.DurationMs;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderInt("持续时间/Duration(ms)", ref duration, 1000, 10000))
            C.DurationMs = duration;

        var secondDelay = C.SecondDelayMs;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderInt("第二次延迟/Second mechanic delay(ms)", ref secondDelay, 0, 5000))
            C.SecondDelayMs = secondDelay;

        ImGui.Separator();
        ImGui.Text("半场刀设置/Cleave:");

        var cleaveFill = C.CleaveFillIntensity;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderFloat("填充透明度/Fill percentage##cleave", ref cleaveFill, 0.1f, 1f))
            C.CleaveFillIntensity = cleaveFill;

        var cleaveThick = C.CleaveThickness;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderFloat("线条粗细//Thickness##cleave", ref cleaveThick, 1f, 10f))
            C.CleaveThickness = cleaveThick;

        ImGui.SetNextItemWidth(200f);
        ImGui.SetNextItemWidth(200f);
        ImGui.ColorEdit4("半场刀颜色/Color", ref C.CleaveColorV4);

        ImGui.Separator();
        ImGui.Text("击退设置/Knockback:");

        ImGui.SetNextItemWidth(200f);
        ImGui.SetNextItemWidth(200f);
        ImGui.ColorEdit4("击退颜色/Color", ref C.KnockbackColorV4);

        ImGui.Separator();

        if (ImGui.Button("保存配置/Save"))
            Controller.SaveConfig();

        ImGui.SameLine();
        if (ImGui.Button("重置状态/Reset mechanic"))
        {
            this.Controller.Reset();
        }

        // 调试信息
        if (ImGui.CollapsingHeader("调试信息"))
        {
            ImGuiEx.Text($"脚本激活: {_isActive}");
            ImGuiEx.Text($"触发读条次数: {_triggerCastCount}/2");
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
            ImGui.Text($"  触发读条: 0xB4DB ({TriggerCastId})");
            ImGui.Text($"  左喷毒: 0xB4CB ({LeftPoison})");
            ImGui.Text($"  右喷毒: 0xB4CD ({RightPoison})");
            ImGui.Text($"  左击退: 0xB4CC ({LeftKnockback})");
            ImGui.Text($"  右击退: 0xB4CE ({RightKnockback})");
        }
    }

    #endregion
}
