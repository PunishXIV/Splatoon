using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public sealed class R5S_Disco_Infernal : SplatoonScript
{
    public enum Direction
    {
        NorthWestInside,
        NorthWestOutside,
        NorthEastInside,
        NorthEastOutside,
        SouthEastInside,
        SouthEastOutside,
        SouthWestInside,
        SouthWestOutside
    }

    private const uint SpotLightDebuffId = 0x116D;
    private int _aoeCount;
    private bool _isSafeOutSideNorthWest;
    private State _state = State.None;

    private (int x, int y) _targetIndex = (0, 0);
    public override HashSet<uint>? ValidTerritories => [1257];
    public override Metadata? Metadata => new Metadata(1, "Garume");
    private static IBattleNpc[] SpotLights => [.. Svc.Objects.Where(x => x.DataId == 0x47BB).OfType<IBattleNpc>()];

    private Config C => Controller.GetConfig<Config>();

    public override void OnSettingsDraw()
    {
        ImGuiEx.CollectionCheckbox("NorthEastInside", Direction.NorthEastInside, C.Directions);
        ImGuiEx.CollectionCheckbox("NorthEastOutside", Direction.NorthEastOutside, C.Directions);
        ImGuiEx.CollectionCheckbox("NorthWestInside", Direction.NorthWestInside, C.Directions);
        ImGuiEx.CollectionCheckbox("NorthWestOutside", Direction.NorthWestOutside, C.Directions);
        ImGuiEx.CollectionCheckbox("SouthEastInside", Direction.SouthEastInside, C.Directions);
        ImGuiEx.CollectionCheckbox("SouthEastOutside", Direction.SouthEastOutside, C.Directions);
        ImGuiEx.CollectionCheckbox("SouthWestInside", Direction.SouthWestInside, C.Directions);
        ImGuiEx.CollectionCheckbox("SouthWestOutside", Direction.SouthWestOutside, C.Directions);

        ImGui.Text("Bait Color:");
        ImGuiComponents.HelpMarker(
            "Change the color of the bait and the text that will be displayed on your bait.\nSetting different values makes it rainbow.");
        ImGui.Indent();
        ImGui.ColorEdit4("Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.SameLine();
        ImGui.ColorEdit4("Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
        ImGui.Unindent();

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"isSafeOutSideNorthWest: {_isSafeOutSideNorthWest}");
            ImGui.Text($"aoeCount: {_aoeCount}");
            ImGui.Text($"targetIndex:{_targetIndex}");
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (_state == State.None && castId == 42838) _state = State.Casting;
    }

    public override void OnSetup()
    {
        for (var x = 0; x < 5; x++)
        for (var y = 0; y < 5; y++)
        {
            var pos = new Vector3(0, 0, 0);
            pos.X = 0.5f * x - 1.0f;
            pos.Z = 0.5f * y - 1.0f;
            // Controller.RegisterElement($"aoe{x}{y}", new Element(0));
        }

        var baitElement = new Element(0)
        {
            radius = 2f,
            Donut = 0.35f,
            fillIntensity = 1f,
            tether = true
        };
        Controller.RegisterElement("Bait", baitElement);

        var predictBaitElement = new Element(0)
        {
            radius = 2f,
            Donut = 0.35f,
            fillIntensity = 1f
        };
        Controller.RegisterElement("PredictBait", predictBaitElement);
    }

    public override void OnReset()
    {
        _state = State.None;
        _aoeCount = 0;
    }

    public override void OnUpdate()
    {
        if (_state is not (State.None or State.End))
        {
            if (Controller.TryGetElementByName("Bait", out var baitElement))
                baitElement.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
        }
        else
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        switch (_state)
        {
            case State.Casting:
            {
                if (position == 3 && data1 == 1 && data2 == 2)
                {
                    _state = State.Aoe;
                    _isSafeOutSideNorthWest = true;
                }

                if (position == 3 && data1 == 16 && data2 == 32)
                {
                    _state = State.Aoe;
                    _isSafeOutSideNorthWest = false;
                }

                break;
            }
            case State.Aoe:
            {
                if ((position == 3 && data1 == 1 && data2 == 2) ||
                    (position == 3 && data1 == 16 && data2 == 32) ||
                    (position == 3 && data1 == 4 && data2 == 8) ||
                    (position == 3 && data1 == 4 && data2 == 128)
                   )
                {
                    _aoeCount++;
                    var isShortDebuff =
                        Player.Status.Any(x => x is { StatusId: SpotLightDebuffId, RemainingTime: < 10f });
                    if (_aoeCount == 6)
                    {
                        SetBait(isShortDebuff);
                        if (isShortDebuff) EnableElement("PredictBait");
                    }
                    else if (_aoeCount == 9 && isShortDebuff)
                    {
                        DisableElement("PredictBait");
                        EnableElement("Bait");
                    }
                    else if (_aoeCount == 9)
                    {
                        EnableElement("PredictBait");
                    }
                    else if (_aoeCount == 13 && isShortDebuff)
                    {
                        DisableElement("PredictBait");
                        EnableElement("Bait");
                    }
                    else if (_aoeCount is 12 or 16)
                    {
                        DisableElement("Bait");
                    }
                    else if (_aoeCount > 17)
                    {
                        _state = State.End;
                    }
                }

                break;
            }
        }
    }

    private void SetBait(bool isShortDebuff)
    {
        if (_isSafeOutSideNorthWest)
        {
            if (C.Directions.Contains(Direction.NorthWestOutside))
            {
                SetElementFromIndex(1, 1);
            }
            else if (C.Directions.Contains(Direction.SouthEastOutside))
            {
                SetElementFromIndex(6, 6);
            }
            else if (C.Directions.Contains(Direction.NorthEastInside))

            {
                if (HasSpotLightAt(4, 2) && isShortDebuff)
                    SetElementFromIndex(5, 3);
                else
                    SetElementFromIndex(4, 2);
            }
            else if (C.Directions.Contains(Direction.SouthWestInside))
            {
                if (HasSpotLightAt(2, 4) && isShortDebuff)
                    SetElementFromIndex(3, 5);
                else
                    SetElementFromIndex(2, 4);
            }
        }
        else
        {
            if (C.Directions.Contains(Direction.NorthEastOutside))
            {
                SetElementFromIndex(6, 1);
            }
            else if (C.Directions.Contains(Direction.SouthWestOutside))
            {
                SetElementFromIndex(1, 6);
            }
            else if (C.Directions.Contains(Direction.NorthWestInside))
            {
                if (HasSpotLightAt(3, 2) && isShortDebuff)
                    SetElementFromIndex(2, 3);
                else
                    SetElementFromIndex(3, 2);
            }
            else if (C.Directions.Contains(Direction.SouthEastInside))
            {
                if (HasSpotLightAt(5, 4) && isShortDebuff)
                    SetElementFromIndex(4, 5);
                else
                    SetElementFromIndex(5, 4);
            }
        }
    }

    private void SetElementFromIndex(int x, int y)
    {
        _targetIndex = (x, y);
        var pos = ToPosition(x, y);
        if (Controller.TryGetElementByName("Bait", out var baitElement)) baitElement.SetOffPosition(pos);
        if (Controller.TryGetElementByName("PredictBait", out var predictElement)) predictElement.SetOffPosition(pos);
    }

    private void EnableElement(string name)
    {
        if (Controller.TryGetElementByName(name, out var element)) element.Enabled = true;
    }

    private void DisableElement(string name)
    {
        if (Controller.TryGetElementByName(name, out var element)) element.Enabled = false;
    }

    private static Vector3 ToPosition(int x, int y)
    {
        return new Vector3(82.5f + 5f * x, 0, 82.5f + 5f * y);
    }

    private bool HasSpotLightAt(int x, int y)
    {
        return SpotLights.Any(spotLight => Vector3.Distance(spotLight.Position, ToPosition(x, y)) < 2.5f);
    }

    private enum State
    {
        None,
        Casting,
        Aoe,
        End
    }

    private class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();

        public List<Direction> Directions =
        [
            Direction.NorthWestInside,
            Direction.SouthWestInside
        ];
    }
}