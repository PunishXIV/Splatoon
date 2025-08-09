using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

internal class M8S_Millennial_Decay : SplatoonScript
{
    public enum Direction
    {
        North = 270,
        NorthEast = 300,
        East = 0,
        SouthEast = 60,
        South = 90,
        SouthWest = 120,
        West = 180,
        NorthWest = 240
    }

    private const uint kMillennialDecay = 41906;
    private const uint kMillennialWind = 41908;
    private const string MarkerVfxPath = "vfx/lockon/eff/loc05sp_05a_se_p.avfx";
    private uint _dragonCount;

    private bool _hasSecondAoe;

    private bool _isActive;
    private bool _isClockWise;
    private int _windCount;
    public override HashSet<uint>? ValidTerritories => [1263];
    public override Metadata? Metadata => new(2, "Redmoon, Garume");

    private Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            radius = 0.5f,
            tether = true,
            thicc = 10f
        };
        Controller.RegisterElement("Bait", element);

        Controller.TryRegisterLayoutFromCode("Clock",
            "~Lv2~{\"Name\":\"a\",\"Group\":\"\",\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"refZ\":-1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.96813,\"refY\":97.22127,\"refZ\":1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":95.92647,\"refY\":97.18153,\"refZ\":-1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":102.51564,\"offY\":95.04247,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":102.631516,\"offY\":96.98153,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":102.76445,\"refY\":101.71465,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":105.53386,\"refY\":101.99625,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"offX\":98.856384,\"offY\":101.83813,\"offZ\":3.8146973E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"offX\":98.87363,\"offY\":104.19578,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}]}",
            out _);
        Controller.TryRegisterLayoutFromCode("CounterClock",
            "~Lv2~{\"Name\":\"as\",\"Group\":\"\",\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"refZ\":-1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"refZ\":-1.9073486E-06,\"offX\":95.90114,\"offY\":101.83096,\"offZ\":1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"refZ\":-1.9073486E-06,\"offX\":98.18604,\"offY\":101.70221,\"offZ\":3.8146973E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":98.58479,\"refY\":95.15524,\"refZ\":1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":98.342224,\"refY\":96.79275,\"refZ\":3.8146973E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":105.338264,\"offY\":97.11067,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":102.768,\"offY\":97.18019,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":102.25408,\"refY\":104.04354,\"refZ\":1.9073486E-06,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":102.36715,\"refY\":101.80373,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}]}",
            out _);
    }

    public override void OnUpdate()
    {
        if (_isActive)
            Controller.GetRegisteredElements().Each(x =>
                x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
    }


    public override void OnStartingCast(uint source, uint castId)
    {
        switch (castId)
        {
            case kMillennialDecay:
                _isActive = true;
                break;
            case kMillennialWind:
            {
                _dragonCount++;

                if (_dragonCount == 1)
                {
                    var angle = 0;
                    if (C.FirstAoeDirection is Direction.SouthWest or Direction.NorthWest)
                        angle = (int)Direction.West;
                    else
                        angle = (int)Direction.East;

                    ApplyPosition(5f, angle);
                }

                if (_dragonCount == 2)
                    if (source.TryGetObject(out var obj))
                    {
                        // first Dragon pos X = 100f Z = 88f
                        var positionX = (int)obj.Position.X;
                        var positionZ = (int)obj.Position.Z;
                        if (positionX == 107 && positionZ == 90)
                        {
                            _isClockWise = true;
                            if (Controller.TryGetLayoutByName("Clock", out var layout))
                            {
                                layout.ElementsL.Each(x => x.color = 0xC800FF00u);
                                layout.Enabled = true;
                            }
                        }

                        if (positionX == 92 && positionZ == 90)
                            if (Controller.TryGetLayoutByName("CounterClock", out var layout))
                            {
                                layout.ElementsL.Each(x => x.color = 0xC800FF00u);
                                layout.Enabled = true;
                            }

                        // second Dragon pos X = 100f Z = 112f
                        if (positionX == 107 && positionZ == 112)
                        {
                            _isClockWise = true;
                            if (Controller.TryGetLayoutByName("Clock", out var layout))
                            {
                                layout.ElementsL.Each(x => x.color = 0xC800FF00u);
                                layout.Enabled = true;
                            }
                        }

                        if (positionX == 92 && positionZ == 112)
                            if (Controller.TryGetLayoutByName("CounterClock", out var layout))
                            {
                                layout.ElementsL.Each(x => x.color = 0xC800FF00u);
                                layout.Enabled = true;
                            }
                    }

                break;
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!set.Action.HasValue) return;

        if (set.Action.Value.RowId == kMillennialWind)
        {
            _windCount++;
            var angle = 0;
            var radius = 5f;

            if (C.FirstAoeDirection is Direction.SouthWest or Direction.NorthWest)
                angle = (int)Direction.West;
            else
                angle = (int)Direction.East;

            if (_isClockWise)
                angle += _windCount * 40;
            else
                angle -= _windCount * 40;

            if (_hasSecondAoe)
            {
                var direction = _isClockWise
                    ? C.SecondAoeDirectionWhenClockWise
                    : C.SecondAoeDirectionWhenCounterClockWise;
                if (direction is Direction.North or Direction.South)
                {
                    angle = (int)direction;
                    radius = 10f;
                }
                else if (_windCount == 3)
                {
                    angle = _isClockWise ? (int)direction - 10 : (int)direction + 10;
                    radius = 10f;
                }
            }

            ApplyPosition(radius, angle);

            if (_windCount >= 4) _hasSecondAoe = false;
            if (_windCount >= 5) OnReset();
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (_isActive && vfxPath == MarkerVfxPath && target.GetObject() is IPlayerCharacter player &&
            player.Address == Player.Object.Address)
        {
            if (_windCount is 0)
            {
                var angle = (int)C.FirstAoeDirection;
                ApplyPosition(10f, angle);
            }
            else if (_windCount is 1)
            {
                _hasSecondAoe = true;
            }
        }
    }

    private void ApplyPosition(float radius, int angle)
    {
        var targetPosition = new Vector3(100, 0, 100);
        var x = radius * MathF.Cos(angle * MathF.PI / 180);
        var y = radius * MathF.Sin(angle * MathF.PI / 180);
        targetPosition += new Vector3(x, 0, y);
        if (Controller.TryGetElementByName("Bait", out var element))
        {
            element.Enabled = true;
            element.SetOffPosition(targetPosition);
            if (C.useNavMesh) UseNavMesh(targetPosition);
        }
    }

    private void UseNavMesh(Vector3 targetPosition)
    {
        if (Svc.Condition[ConditionFlag.InCombat])
            Chat.Instance.ExecuteCommand("/vnav moveto " + targetPosition.X + " " +
                                         targetPosition.Y + " " + targetPosition.Z);
        else
            DuoLog.Information($"Run to \"{targetPosition}");
    }

    public override void OnReset()
    {
        _isActive = false;
        _isClockWise = false;
        _windCount = 0;
        _dragonCount = 0;
        _hasSecondAoe = false;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        Controller.GetRegisteredLayouts().Each(x => x.Value.Enabled = false);
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.EnumCombo("First Aoe Direction", ref C.FirstAoeDirection,
            x => x is Direction.SouthEast or Direction.SouthWest or Direction.NorthWest or Direction.NorthEast);
        ImGuiEx.EnumCombo("Second Aoe Direction When Clock Wise", ref C.SecondAoeDirectionWhenClockWise,
            x => x is Direction.East or Direction.West or Direction.North or Direction.South);
        ImGuiEx.EnumCombo("Second Aoe Direction When Counter Clock Wise", ref C.SecondAoeDirectionWhenCounterClockWise,
            x => x is Direction.East or Direction.West or Direction.North or Direction.South);

        ImGui.ColorEdit4("Color1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Color2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);

        ImGuiEx.CenterColumnText(EColor.RedBright,
            "-- This uses VNavMesh to automatically dodge AOEs. Please use it with caution!!! -- ");
        ImGui.Checkbox("Use NavMesh", ref C.useNavMesh);

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"Millennial Decay: {_isActive}");
            ImGui.Text($"Wind Count: {_windCount}");
            ImGui.Text($"Dragon Count: {_dragonCount}");
            ImGui.Text($"Is Clock Wise: {_isClockWise}");
            ImGui.Text($"Has Aoe: {_hasSecondAoe}");
        }
    }

    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public Direction FirstAoeDirection = Direction.SouthWest;
        public Direction SecondAoeDirectionWhenClockWise = Direction.West;
        public Direction SecondAoeDirectionWhenCounterClockWise = Direction.East;
        public bool useNavMesh;
    }
}