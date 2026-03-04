using System.Collections.Generic;
using System.Numerics;
using ECommons;
using ECommons.Configuration;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using static Splatoon.Splatoon;
using static Dalamud.Bindings.ImGui.ImGui;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class Another_Merchants_Tale_B1_Echoed_Serenade : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => [1317,];
    public override Metadata Metadata => new(1, "redmoon");

    private enum MonsterRotation
    {
        seahorse,
        chocobo,
        crab,
        puffer,
    }

    private List<MonsterRotation> _rotations = [];
    private bool _isActivated = false;
    private int _castCount = 0;
    private Config C => Controller.GetConfig<Config>();

    private readonly Vector3 center = new(375f, -29.5f, 530f);

    public override void OnSetup()
    {
        if(C.PlayerData.PriorityLists.Count == 0)
        {
            C.PlayerData.PriorityLists.Add(new PriorityList
            {
                IsRole = true,
                List =
                {
                    new JobbedPlayer { Role = RolePosition.T1, },
                    new JobbedPlayer { Role = RolePosition.H1, },
                    new JobbedPlayer { Role = RolePosition.M1, },
                    new JobbedPlayer { Role = RolePosition.R1, },
                },
            });
        }

        Controller.RegisterElementFromCode("seahorse",
            """{"Name":"","type":3,"refY":40.0,"radius":4.0,"refActorDataID":19098,"refActorComparisonType":3,"includeRotation":true}""");

        Controller.RegisterElementFromCode("chocobo",
            """{"Name":"","type":4,"radius":45.0,"coneAngleMin":-30,"coneAngleMax":30,"refActorDataID":19102,"refActorComparisonType":3,"includeRotation":true}""");
        Controller.RegisterElementFromCode("crab",
            """{"Name":"","type":3,"refY":40.0,"radius":4.0,"refActorDataID":19100,"refActorComparisonType":3,"includeRotation":true}""");
        Controller.RegisterElementFromCode("puffer",
            """{"Name":"","type":4,"refY":40.0,"radius":20.0,"coneAngleMin":-90,"coneAngleMax":90,"refActorDataID":19101,"refActorComparisonType":3,"includeRotation":true}""");

        Controller.RegisterElement("guide", new Element(0)
        {
            radius = 0.35f,
            Donut = 0.2f,
            fillIntensity = 1f,
        });
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if(packet->ActionID == 45771)
        {
            _rotations.Clear();
            _isActivated = true;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!_isActivated) return;
        var castId = set.Action?.RowId ?? 0;

        if(castId == 45839) // seahorse
        {
            if(_castCount == 0) _rotations.RemoveAt(0);
            _castCount++;
            if(_castCount == 4) _castCount = 0;
            if(_rotations.Count == 0)
                OnReset();
        }
        else if(castId == 45843) // chocobo
        {
            if(_castCount == 0) _rotations.RemoveAt(0);
            _castCount++;
            if(_castCount == 2) _castCount = 0;
            if(_rotations.Count == 0)
                OnReset();
        }
        else if(castId == 45841) // crab
        {
            if(_castCount == 0) _rotations.RemoveAt(0);
            _castCount++;
            if(_castCount == 4) _castCount = 0;
            if(_rotations.Count == 0)
                OnReset();
        }
        else if(castId == 45842) // puffer
        {
            if(_castCount == 0) _rotations.RemoveAt(0);
            _castCount++;
            if(_castCount == 2) _castCount = 0;
            if(_rotations.Count == 0)
                OnReset();
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(!_isActivated) return;
        if(vfxPath.Contains("vfx/common/eff/m0941_seahorse_c0h.avfx"))
            _rotations.Add(MonsterRotation.seahorse);
        else if(vfxPath.Contains("vfx/common/eff/m0941_chocobo_c0h.avfx"))
            _rotations.Add(MonsterRotation.chocobo);
        else if(vfxPath.Contains("vfx/common/eff/m0941_crab_c0h.avfx"))
            _rotations.Add(MonsterRotation.crab);
        else if(vfxPath.Contains("vfx/common/eff/m0941_puffer_c0h.avfx"))
            _rotations.Add(MonsterRotation.puffer);
    }

    public override void OnUpdate()
    {
        if(!_isActivated) return;
        Controller.Hide();
        switch(_rotations[0])
        {
            case MonsterRotation.seahorse:
                {
                    if(Controller.TryGetElementByName("seahorse", out var seahorse))
                        seahorse.Enabled = true;
                    // var guidePos = MirroringByIndex(new Vector3(387.644f, -29.5f, 510.6f),
                    //     C.PlayerData.GetOwnIndex(_ => true));

                    // if (Controller.TryGetElementByName("guide", out var guide))
                    // {
                    //     guide.SetRefPosition(guidePos);
                    //     guide.Enabled = true;
                    // }

                    break;
                }
            case MonsterRotation.chocobo:
                {
                    if(Controller.TryGetElementByName("chocobo", out var chocobo))
                        chocobo.Enabled = true;
                    break;
                }
            case MonsterRotation.crab:
                {
                    if(Controller.TryGetElementByName("crab", out var crab))
                        crab.Enabled = true;
                    break;
                }
            case MonsterRotation.puffer:
                {
                    if(Controller.TryGetElementByName("puffer", out var puffer))
                        puffer.Enabled = true;
                    break;
                }
            default:
                break;
        }

        // {
        //     if (Controller.TryGetElementByName("guide", out var guide) && guide.Enabled) // Rainbow
        //         guide.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        // }
    }

    public override void OnReset()
    {
        _rotations.Clear();
        _isActivated = false;
        _castCount = 0;
        Controller.Hide();
    }

    public class PriorityData4 : PriorityData
    {
        public override int GetNumPlayers() => 4;
    }

    public class Config : IEzConfig
    {
        public PriorityData4 PlayerData = new();
    }

    public override void OnSettingsDraw()
    {
        C.PlayerData.Draw();

        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            Text($"_isActivated: {_isActivated}");
            Text($"_castCount: {_castCount}");
            Separator();
            ImGuiEx.Text("_rotations:");
            foreach(var r in _rotations) ImGuiEx.Text(r.ToString());
        }
    }

    private Vector3 MirroringByIndex(Vector3 original, int index)
    {
        return index switch
        {
            // 0 Mirror X
            0 => original with { X = center.X - (original.X - center.X), },
            // 1 Mirror Z
            1 => original with { Z = center.Z - (original.Z - center.Z), },
            // 2 No Mirror
            2 => original,
            // 3 Mirror XZ
            3 => original with
            {
                X = center.X - (original.X - center.X),
                Z = center.Z - (original.Z - center.Z),
            },
            _ => original,
        };
    }
}