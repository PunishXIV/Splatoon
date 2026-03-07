using System;
using System.Collections.Generic;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using static Dalamud.Bindings.ImGui.ImGui;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class Another_Merchants_Tale_B1_Echoed_Serenade : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => [1317,];
    public override Metadata? Metadata => new(2, "redmoon");

    private enum MonsterRotation
    {
        Seahorse,
        Chocobo,
        Crab,
        Puffer,
        Turtle,
    }

    private readonly List<MonsterRotation> _rotations = [];
    private bool _isActivated = false;
    private int _rotationIndex = 0;
    private int _gimmickCount = 0;
    private long _lockNextTime = 0L;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("seahorse",
            """{"Name":"","type":3,"refY":40.0,"radius":4.0,"refActorDataID":19098,"refActorComparisonType":3,"includeRotation":true}""");
        Controller.RegisterElementFromCode("chocobo",
            """{"Name":"","type":4,"radius":45.0,"coneAngleMin":-30,"coneAngleMax":30,"refActorDataID":19102,"refActorComparisonType":3,"includeRotation":true}""");
        Controller.RegisterElementFromCode("crab",
            """{"Name":"","type":3,"refY":40.0,"radius":4.0,"refActorDataID":19100,"refActorComparisonType":3,"includeRotation":true}""");
        Controller.RegisterElementFromCode("puffer",
            """{"Name":"","type":4,"refY":40.0,"radius":20.0,"coneAngleMin":-90,"coneAngleMax":90,"refActorDataID":19101,"refActorComparisonType":3,"includeRotation":true}""");
        Controller.RegisterElementFromCode("turtle",
            """{"Name":"","type":3,"refY":40.0,"radius":4.0,"refActorDataID":19099,"refActorComparisonType":3,"includeRotation":true}""");

        Controller.RegisterElementFromCode("seahorsePre",
            """{"Name":"","type":3,"refY":40.0,"radius":3.8,"color":3355508719,"Filled":false,"fillIntensity":0.345,"thicc":20.0,"refActorDataID":19098,"refActorComparisonType":3,"includeRotation":true}""");
        Controller.RegisterElementFromCode("chocoboPre",
            """{"Name":"","type":4,"refY":40.0,"radius":19.0,"coneAngleMin":-30,"coneAngleMax":30,"color":3355508719,"Filled":false,"fillIntensity":0.345,"thicc":20.0,"refActorDataID":19102,"refActorComparisonType":3,"includeRotation":true}""");
        Controller.RegisterElementFromCode("crabPre",
            """{"Name":"","type":3,"refY":40.0,"radius":3.8,"color":3355508719,"Filled":false,"fillIntensity":0.345,"thicc":20.0,"refActorDataID":19100,"refActorComparisonType":3,"includeRotation":true}""");
        Controller.RegisterElementFromCode("pufferPre",
            """{"Name":"","type":4,"refY":40.0,"radius":19.0,"coneAngleMin":-90,"coneAngleMax":90,"color":3355508719,"Filled":false,"fillIntensity":0.345,"thicc":20.0,"refActorDataID":19101,"refActorComparisonType":3,"includeRotation":true}""");
        Controller.RegisterElementFromCode("turtlePre",
            """{"Name":"","type":3,"refY":40.0,"radius":3.8,"color":3355508719,"Filled":false,"fillIntensity":0.345,"thicc":20.0,"refActorDataID":19099,"refActorComparisonType":3,"includeRotation":true}""");
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if (packet->ActionID == 45771)
        {
            if (_gimmickCount != 2) _rotations.Clear();
            _isActivated = true;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!_isActivated) return;
        var castId = set.Action?.RowId ?? 0;

        if (castId is 45839 or 45840 or 45841 or 45842 or 45843 && _lockNextTime == 0)
        {
            _rotationIndex++;
            _lockNextTime = Environment.TickCount64 + 300;
        }

        if (_rotationIndex == 4)
        {
            _isActivated = false;
            _rotationIndex = 0;
            if (_gimmickCount != 1) _rotations.Clear();
            _gimmickCount++;
            Controller.Hide();
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (!_isActivated) return;
        if (vfxPath.Contains("vfx/common/eff/m0941_seahorse_c0h.avfx"))
            _rotations.Add(MonsterRotation.Seahorse);
        else if (vfxPath.Contains("vfx/common/eff/m0941_chocobo_c0h.avfx"))
            _rotations.Add(MonsterRotation.Chocobo);
        else if (vfxPath.Contains("vfx/common/eff/m0941_crab_c0h.avfx"))
            _rotations.Add(MonsterRotation.Crab);
        else if (vfxPath.Contains("vfx/common/eff/m0941_puffer_c0h.avfx"))
            _rotations.Add(MonsterRotation.Puffer);
        else if (vfxPath.Contains("vfx/common/eff/m0941_turtle_c0h.avfx"))
            _rotations.Add(MonsterRotation.Turtle);
    }

    public override void OnUpdate()
    {
        if (!_isActivated) return;
        Controller.Hide();
        if (_lockNextTime != 0 && Environment.TickCount64 >= _lockNextTime) _lockNextTime = 0;
        if (_rotations.Count == 0) return;
        switch (_rotations[_rotationIndex])
        {
            case MonsterRotation.Seahorse:
            {
                if (Controller.TryGetElementByName("seahorse", out var seahorse)) seahorse.Enabled = true;
                break;
            }
            case MonsterRotation.Chocobo:
            {
                if (Controller.TryGetElementByName("chocobo", out var chocobo)) chocobo.Enabled = true;
                break;
            }
            case MonsterRotation.Crab:
            {
                if (Controller.TryGetElementByName("crab", out var crab)) crab.Enabled = true;
                break;
            }
            case MonsterRotation.Puffer:
            {
                if (Controller.TryGetElementByName("puffer", out var puffer)) puffer.Enabled = true;
                break;
            }
            case MonsterRotation.Turtle:
            {
                if (Controller.TryGetElementByName("turtle", out var turtle)) turtle.Enabled = true;
                break;
            }
        }

        if (_rotations.Count <= 1 || _rotationIndex == 3) return;
        switch (_rotations[_rotationIndex + 1]) // Pre
        {
            case MonsterRotation.Seahorse:
            {
                if (Controller.TryGetElementByName("seahorsePre", out var seahorsePre)) seahorsePre.Enabled = true;
                break;
            }
            case MonsterRotation.Chocobo:
            {
                if (Controller.TryGetElementByName("chocoboPre", out var chocoboPre)) chocoboPre.Enabled = true;
                break;
            }
            case MonsterRotation.Crab:
            {
                if (Controller.TryGetElementByName("crabPre", out var crabPre)) crabPre.Enabled = true;
                break;
            }
            case MonsterRotation.Puffer:
            {
                if (Controller.TryGetElementByName("pufferPre", out var pufferPre)) pufferPre.Enabled = true;
                break;
            }
            case MonsterRotation.Turtle:
            {
                if (Controller.TryGetElementByName("turtlePre", out var turtlePre)) turtlePre.Enabled = true;
                break;
            }
        }
    }

    public override void OnReset()
    {
        _rotations.Clear();
        _isActivated = false;
        Controller.Hide();
        _gimmickCount = 0;
        _rotationIndex = 0;
    }

    public override void OnSettingsDraw()
    {
        // Debug
        if (!ImGuiEx.CollapsingHeader("Debug")) return;
        Text($"_isActivated: {_isActivated}");
        Text($"_rotations.Count: {_rotations.Count}");
        InputInt("_rotationIndex", ref _rotationIndex);
        InputInt("_gimmickCount", ref _gimmickCount);
        Separator();
        ImGuiEx.Text("_rotations:");
        foreach (var r in _rotations) ImGuiEx.Text(r.ToString());
        Separator();
        Text("Elements:");
        foreach (var e in Controller.GetRegisteredElements())
            ImGuiEx.Text(
                $"{e.Key}: Enabled={e.Value.Enabled} Pos=({e.Value.refX}, {e.Value.refZ}, {e.Value.refY})");
    }
}