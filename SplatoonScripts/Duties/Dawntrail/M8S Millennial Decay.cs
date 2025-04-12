using ECommons;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
internal class M8S_Millennial_Decay :SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1263];
    public override Metadata? Metadata => new(1, "Redmoon");

    private const uint kMillennialDecay = 41906;
    private const uint kWind = 41907;
    private const uint kMillennialWind = 41908;

    private bool _isActive = false;
    private uint _dragonCount = 0;
    private uint _windCount = 0;

    public override void OnSetup()
    {
        Controller.TryRegisterLayoutFromCode("Clock", "~Lv2~{\"Name\":\"a\",\"Group\":\"\",\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"refZ\":-1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.96813,\"refY\":97.22127,\"refZ\":1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":95.92647,\"refY\":97.18153,\"refZ\":-1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":102.51564,\"offY\":95.04247,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":102.631516,\"offY\":96.98153,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":102.76445,\"refY\":101.71465,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":105.53386,\"refY\":101.99625,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"offX\":98.856384,\"offY\":101.83813,\"offZ\":3.8146973E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"offX\":98.87363,\"offY\":104.19578,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}]}", out _);
        Controller.TryRegisterLayoutFromCode("CounterClock", "~Lv2~{\"Name\":\"as\",\"Group\":\"\",\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"refZ\":-1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"refZ\":-1.9073486E-06,\"offX\":95.90114,\"offY\":101.83096,\"offZ\":1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"refZ\":-1.9073486E-06,\"offX\":98.18604,\"offY\":101.70221,\"offZ\":3.8146973E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":98.58479,\"refY\":95.15524,\"refZ\":1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":98.342224,\"refY\":96.79275,\"refZ\":3.8146973E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":105.338264,\"offY\":97.11067,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":102.768,\"offY\":97.18019,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":102.25408,\"refY\":104.04354,\"refZ\":1.9073486E-06,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":102.36715,\"refY\":101.80373,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}]}", out _);
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == kMillennialDecay)
        {
            _isActive = true;
        }
        else if (castId == kMillennialWind)
        {
            _dragonCount++;

            if (_dragonCount == 2)
            {
                if (source.TryGetObject(out var obj))
                {
                    // first Dragon pos X = 100f Z = 88f
                    float X = (float)(int)obj.Position.X;
                    float Z = (float)(int)obj.Position.Z;
                    if (X == 107 && Z == 90)
                    {
                        if (Controller.TryGetLayoutByName("Clock", out var layout))
                        {
                            layout.ElementsL.Each(x => x.color = 0xC800FF00u);
                            layout.Enabled = true;
                        }
                    }
                    if (X == 92 && Z == 90)
                    {
                        if (Controller.TryGetLayoutByName("CounterClock", out var layout))
                        {
                            layout.ElementsL.Each(x => x.color = 0xC800FF00u);
                            layout.Enabled = true;
                        }
                    }
                    // second Dragon pos X = 100f Z = 112f
                    if (X == 107 && Z == 112)
                    {
                        if (Controller.TryGetLayoutByName("Clock", out var layout))
                        {
                            layout.ElementsL.Each(x => x.color = 0xC800FF00u);
                            layout.Enabled = true;
                        }
                    }
                    if (X == 92 && Z == 112)
                    {
                        if (Controller.TryGetLayoutByName("CounterClock", out var layout))
                        {
                            layout.ElementsL.Each(x => x.color = 0xC800FF00u);
                            layout.Enabled = true;
                        }
                    }

                }
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!set.Action.HasValue) return;

        if (set.Action.Value.RowId == kWind)
        {
            _windCount++;
            if (_windCount >= 8)
            {
                this.OnReset();
            }
        }
    }

    public override void OnReset()
    {
        _isActive = false;
        _windCount = 0;
        _dragonCount = 0;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        Controller.GetRegisteredLayouts().Each(x => x.Value.Enabled = false);
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"Millennial Decay: {_isActive}");
            ImGui.Text($"Wind Count: {_windCount}");
            ImGui.Text($"Dragon Count: {_dragonCount}");
        }
    }
}
