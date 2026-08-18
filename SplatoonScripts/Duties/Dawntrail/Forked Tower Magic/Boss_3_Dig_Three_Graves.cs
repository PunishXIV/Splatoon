using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Hooks.ActionEffectTypes;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Forked_Tower_Magic;

public class Boss_3_Dig_Three_Graves : SplatoonScript
{
    #region Metadata

    public override Metadata Metadata { get; } = new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryForkedTowerMagic];

    #endregion

    #region Constant

    private const uint TerritoryForkedTowerMagic = 1346;

    private const uint DataIdNecrophobia = 0x4BE7;
    private const uint DataIdHead = 0x4BE8;

    private const uint CastInfusion = 0xB97E;
    private const uint CastDigThreeGraves = 47506;

    private const uint TetherFire = 400;
    private const uint TetherIce = 401;
    private const uint TetherThunder = 402;

    private const string VfxFire = "vfx/common/eff/m0810_stlp_npcast_c0x.avfx";
    private const string VfxIce = "vfx/common/eff/m0810_stlp_npcast_c1x.avfx";
    private const string VfxThunder = "vfx/common/eff/m0810_stlp_npcast_c2x.avfx";

    private const uint ActionFire = 47510;
    private const uint ActionIce = 47511;
    private const uint ActionThunder = 47513;

    private const int WaveCountMax = 3;

    private static readonly string[] DarkStreams = ["DarkStream0", "DarkStream1", "DarkStream2"];

    #endregion

    #region Config

    // No IEzConfig in this script.

    #endregion

    #region State

    private bool _active;
    private readonly Dictionary<uint, Elemental> _heads = [];
    private readonly List<Elemental> _elementOrder = [];
    private int _count;
    private bool _waveEffected;

    #endregion

    #region Private Class

    private enum Elemental
    {
        Fire,
        Ice,
        Thunder,
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Fire0",
            """{"Name":"Circle","type":1,"Enabled":false,"radius":18.0,"color":4278190335,"fillIntensity":0.3,"refActorObjectID":0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true}""",
            overwrite: true);
        Controller.RegisterElementFromCode("Fire1",
            """{"Name":"Circle","type":1,"Enabled":false,"radius":18.0,"color":4278190335,"fillIntensity":0.3,"refActorObjectID":0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true}""",
            overwrite: true);

        Controller.RegisterElementFromCode("Ice0H",
            """{"Name":"Cross1","type":3,"Enabled":false,"refY":50.0,"offY":-50.0,"radius":7.5,"color":4294967040,"fillIntensity":0.3,"refActorObjectID":0,"refActorComparisonType":2,"includeRotation":true,"FillStep":2.0}""",
            overwrite: true);
        Controller.RegisterElementFromCode("Ice0V",
            """{"Name":"Cross2","type":3,"Enabled":false,"refY":50.0,"offY":-50.0,"radius":7.5,"color":4294967040,"fillIntensity":0.3,"refActorObjectID":0,"refActorComparisonType":2,"includeRotation":true,"AdditionalRotation":1.5707964,"FillStep":2.0}""",
            overwrite: true);
        Controller.RegisterElementFromCode("Ice1H",
            """{"Name":"Cross1","type":3,"Enabled":false,"refY":50.0,"offY":-50.0,"radius":7.5,"color":4294967040,"fillIntensity":0.3,"refActorObjectID":0,"refActorComparisonType":2,"includeRotation":true,"FillStep":2.0}""",
            overwrite: true);
        Controller.RegisterElementFromCode("Ice1V",
            """{"Name":"Cross2","type":3,"Enabled":false,"refY":50.0,"offY":-50.0,"radius":7.5,"color":4294967040,"fillIntensity":0.3,"refActorObjectID":0,"refActorComparisonType":2,"includeRotation":true,"AdditionalRotation":1.5707964,"FillStep":2.0}""",
            overwrite: true);

        Controller.RegisterElementFromCode("Thunder00",
            """{"Name":"Cone","type":4,"Enabled":false,"radius":50.0,"coneAngleMin":-23,"coneAngleMax":22,"color":4286578816,"fillIntensity":0.3,"thicc":3.0,"refActorObjectID":0,"refActorComparisonType":2,"includeHitbox":true,"includeRotation":true,"AdditionalRotation":0.7853982,"DistanceMax":13.2,"FillStep":4.0}""",
            overwrite: true);
        Controller.RegisterElementFromCode("Thunder01",
            """{"Name":"Cone","type":4,"Enabled":false,"radius":50.0,"coneAngleMin":-23,"coneAngleMax":22,"color":4286578816,"fillIntensity":0.3,"thicc":3.0,"refActorObjectID":0,"refActorComparisonType":2,"includeHitbox":true,"includeRotation":true,"AdditionalRotation":2.3561945,"DistanceMax":13.2,"FillStep":4.0}""",
            overwrite: true);
        Controller.RegisterElementFromCode("Thunder02",
            """{"Name":"Cone","type":4,"Enabled":false,"radius":50.0,"coneAngleMin":-23,"coneAngleMax":22,"color":4286578816,"fillIntensity":0.3,"thicc":3.0,"refActorObjectID":0,"refActorComparisonType":2,"includeHitbox":true,"includeRotation":true,"AdditionalRotation":3.9269908,"DistanceMax":13.2,"FillStep":4.0}""",
            overwrite: true);
        Controller.RegisterElementFromCode("Thunder03",
            """{"Name":"Cone","type":4,"Enabled":false,"radius":50.0,"coneAngleMin":-23,"coneAngleMax":22,"color":4286578816,"fillIntensity":0.3,"thicc":3.0,"refActorObjectID":0,"refActorComparisonType":2,"includeHitbox":true,"includeRotation":true,"AdditionalRotation":5.497787,"DistanceMax":13.2,"FillStep":4.0}""",
            overwrite: true);
        Controller.RegisterElementFromCode("Thunder10",
            """{"Name":"Cone","type":4,"Enabled":false,"radius":50.0,"coneAngleMin":-23,"coneAngleMax":22,"color":4286578816,"fillIntensity":0.3,"thicc":3.0,"refActorObjectID":0,"refActorComparisonType":2,"includeHitbox":true,"includeRotation":true,"AdditionalRotation":0.7853982,"DistanceMax":13.2,"FillStep":4.0}""",
            overwrite: true);
        Controller.RegisterElementFromCode("Thunder11",
            """{"Name":"Cone","type":4,"Enabled":false,"radius":50.0,"coneAngleMin":-23,"coneAngleMax":22,"color":4286578816,"fillIntensity":0.3,"thicc":3.0,"refActorObjectID":0,"refActorComparisonType":2,"includeHitbox":true,"includeRotation":true,"AdditionalRotation":2.3561945,"DistanceMax":13.2,"FillStep":4.0}""",
            overwrite: true);
        Controller.RegisterElementFromCode("Thunder12",
            """{"Name":"Cone","type":4,"Enabled":false,"radius":50.0,"coneAngleMin":-23,"coneAngleMax":22,"color":4286578816,"fillIntensity":0.3,"thicc":3.0,"refActorObjectID":0,"refActorComparisonType":2,"includeHitbox":true,"includeRotation":true,"AdditionalRotation":3.9269908,"DistanceMax":13.2,"FillStep":4.0}""",
            overwrite: true);
        Controller.RegisterElementFromCode("Thunder13",
            """{"Name":"Cone","type":4,"Enabled":false,"radius":50.0,"coneAngleMin":-23,"coneAngleMax":22,"color":4286578816,"fillIntensity":0.3,"thicc":3.0,"refActorObjectID":0,"refActorComparisonType":2,"includeHitbox":true,"includeRotation":true,"AdditionalRotation":5.497787,"DistanceMax":13.2,"FillStep":4.0}""",
            overwrite: true);

        Controller.RegisterElementFromCode("DarkStream0",
            """{"Name":"","type":2,"Enabled":false,"refX":100.0,"refY":775.0,"refZ":-724.0,"offX":100.0,"offY":825.0,"offZ":-724.0,"radius":5.0}""",
            overwrite: true);
        Controller.RegisterElementFromCode("DarkStream1",
            """{"Name":"","type":2,"Enabled":false,"refX":121.650635,"refY":812.5,"refZ":-724.0,"offX":78.349365,"offY":787.5,"offZ":-724.0,"radius":5.0}""",
            overwrite: true);
        Controller.RegisterElementFromCode("DarkStream2",
            """{"Name":"","type":2,"Enabled":false,"refX":78.349365,"refY":812.5,"refZ":-724.0,"offX":121.650635,"offY":787.5,"offZ":-724.0,"radius":5.0}""",
            overwrite: true);
    }

    public override void OnUpdate()
    {
        HideAllElements();
        if(!_active || _count >= WaveCountMax || _count >= _elementOrder.Count) return;

        var elemental = _elementOrder[_count];
        var ids = _heads.Where(x => x.Value == elemental).Select(x => x.Key).ToList();
        switch(elemental)
        {
            case Elemental.Fire:
                EnableElementsAt(ids, 0, "Fire0");
                EnableElementsAt(ids, 1, "Fire1");
                break;
            case Elemental.Ice:
                EnableElementsAt(ids, 0, "Ice0H", "Ice0V");
                EnableElementsAt(ids, 1, "Ice1H", "Ice1V");
                break;
            case Elemental.Thunder:
                EnableElementsAt(ids, 0, "Thunder00", "Thunder01", "Thunder02", "Thunder03");
                EnableElementsAt(ids, 1, "Thunder10", "Thunder11", "Thunder12", "Thunder13");
                break;
        }

        ApplyDarkStream(_count, elemental);
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == CastInfusion)
        {
            ResetMechanic();
            return;
        }

        if(castId == CastDigThreeGraves)
            _active = true;
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(data2 != 0 || data5 != 15) return;
        if(!TryGetElementalFromTether(data3, out var elemental)) return;

        TryGetTetherObject(source, out var sourceObj);
        TryGetTetherObject(target, out var targetObj);
        if(!IsDataId(sourceObj, source, DataIdNecrophobia) && !IsDataId(targetObj, target, DataIdNecrophobia)) return;

        var head = GetHead(sourceObj) ?? GetHead(targetObj);
        if(head == null) return;

        _heads[head.EntityId] = elemental;
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(!target.TryGetObject(out var obj) || obj.DataId != DataIdNecrophobia) return;
        if(!TryGetElementalFromVfx(vfxPath, out var elemental)) return;
        if(_elementOrder.Contains(elemental)) return;

        _elementOrder.Add(elemental);
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(_waveEffected || _count >= _elementOrder.Count) return;
        if(set.Action == null) return;
        if(set.Action.Value.RowId != GetActionId(_elementOrder[_count])) return;

        _waveEffected = true;
        _count++;
        if(_count >= WaveCountMax)
        {
            _active = false;
            ResetMechanic();
            return;
        }

        _waveEffected = false;
    }

    public override void OnReset()
    {
        _active = false;
        ResetMechanic();
        Controller.Hide();
    }

    public override void OnSettingsDraw()
    {
        if(!ImGui.CollapsingHeader("Debug")) return;

        ImGui.Text($"Active: {_active}");
        ImGui.Text($"Count: {_count}");
        ImGui.Text($"WaveEffected: {_waveEffected}");
        ImGui.Text($"ElementOrder: {string.Join(", ", _elementOrder)}");
        ImGui.Text($"Heads: {_heads.Count}");
        foreach(var (entityId, elemental) in _heads)
            ImGui.Text($"  {entityId:X} -> {elemental}");
    }

    #endregion

    #region Private Method

    // Clear mechanic flags, head map, order, and disable layouts. Active is left unchanged.
    private void ResetMechanic()
    {
        _heads.Clear();
        _elementOrder.Clear();
        _count = 0;
        _waveEffected = false;
        HideAllElements();
    }

    // Disable every registered layout.
    private void HideAllElements()
        => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    // Resolve tether ids by EntityId, GameObjectId, or DataId.
    private static bool TryGetTetherObject(uint id, out IGameObject obj)
    {
        if(id.TryGetObject(out var byEntityId))
        {
            obj = byEntityId;
            return true;
        }

        var byGameObjectId = Svc.Objects.FirstOrDefault(x => x.GameObjectId == id);
        if(byGameObjectId != null)
        {
            obj = byGameObjectId;
            return true;
        }

        var byDataId = Svc.Objects.FirstOrDefault(x => x.DataId == id);
        obj = byDataId!;
        return byDataId != null;
    }

    // True when the resolved object or raw id is the given DataId.
    private static bool IsDataId(IGameObject? obj, uint id, uint dataId)
        => obj?.DataId == dataId || id == dataId;

    // Head actor when DataId matches.
    private static IGameObject? GetHead(IGameObject? obj)
        => obj?.DataId == DataIdHead ? obj : null;

    // Enable the Dark Stream line for the current wave and color it by element.
    private void ApplyDarkStream(int count, Elemental elemental)
    {
        if(count >= DarkStreams.Length) return;
        if(!Controller.TryGetElementByName(DarkStreams[count], out var element)) return;

        element.color = GetElementalLayoutColor(elemental);
        element.Enabled = true;
    }

    // Copy Fire / Ice / Thunder layout color so Dark Stream matches exactly.
    private uint GetElementalLayoutColor(Elemental elemental)
    {
        var name = elemental switch
        {
            Elemental.Ice => "Ice0H",
            Elemental.Thunder => "Thunder00",
            _ => "Fire0",
        };
        return Controller.TryGetElementByName(name, out var layout) ? layout.color : 0;
    }

    // ActionEffect id that advances the current wave.
    private static uint GetActionId(Elemental elemental)
        => elemental switch
        {
            Elemental.Fire => ActionFire,
            Elemental.Ice => ActionIce,
            Elemental.Thunder => ActionThunder,
            _ => 0u,
        };

    // Bind layouts to the head EntityId at index.
    private void EnableElementsAt(List<uint> ids, int index, params string[] names)
    {
        if(index < 0 || index >= ids.Count) return;
        foreach(var name in names)
        {
            if(!Controller.TryGetElementByName(name, out var element)) continue;
            element.refActorObjectID = ids[index];
            element.Enabled = true;
        }
    }

    // Map tether data3 to Fire / Ice / Thunder.
    private static bool TryGetElementalFromTether(uint data3, out Elemental elemental)
    {
        switch(data3)
        {
            case TetherFire:
                elemental = Elemental.Fire;
                return true;
            case TetherIce:
                elemental = Elemental.Ice;
                return true;
            case TetherThunder:
                elemental = Elemental.Thunder;
                return true;
            default:
                elemental = default;
                return false;
        }
    }

    // Map Necrophobia VFX path to Fire / Ice / Thunder.
    private static bool TryGetElementalFromVfx(string vfxPath, out Elemental elemental)
    {
        switch(vfxPath)
        {
            case VfxFire:
                elemental = Elemental.Fire;
                return true;
            case VfxIce:
                elemental = Elemental.Ice;
                return true;
            case VfxThunder:
                elemental = Elemental.Thunder;
                return true;
            default:
                elemental = default;
                return false;
        }
    }

    #endregion
}
