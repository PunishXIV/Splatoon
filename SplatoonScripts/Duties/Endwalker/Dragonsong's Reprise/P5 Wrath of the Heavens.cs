using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public class P5_Wrath_of_the_Heavens : SplatoonScript
{
    private const uint IgnasseDataId = 12635;

    private const string TestOverride = "";
    private const uint VellguineDataId = 12633;
    private bool _active;
    private Element? _bahamutDiveTargetElement;

    private bool _gottether;
    private Element? _ignasseHitboxElement;
    private IPlayerCharacter? _ignassePlayer;
    private Element? _ignasseTargetElement;
    private Element? _noSkydiveTargetElement;

    private Element? _skydiveTargetElement;
    private Element? _vellguineHitboxElement;
    private IPlayerCharacter? _vellguinePlayer;
    private Element? _vellguineTargetElement;
    public override HashSet<uint>? ValidTerritories => [968];

    public override Metadata? Metadata => new(6, "Enthusiastus, Garume, damolitionn, NightmareXIV");

    private IBattleNpc? Ignasse =>
        Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == IgnasseDataId) as IBattleNpc;

    private IBattleNpc? Vellguine =>
        Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == VellguineDataId) as IBattleNpc;

    private IPlayerCharacter PC => BasePlayer;

    public override void OnSetup()
    {
        var skydiveTargetTether =
            "{\"Name\":\"markerTargetTether\",\"type\":1,\"offX\":17.42,\"offY\":12.22,\"radius\":0.6,\"color\":4294901787,\"thicc\":7.6,\"refActorNPCNameID\":3984,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}";
        var noSkydiveTargetTether =
            "{\"Name\":\"nomarkerTargetTether\",\"type\":1,\"offX\":-19.5,\"offY\":23.0,\"radius\":0.6,\"color\":4294901787,\"thicc\":7.6,\"refActorNPCNameID\":3984,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}";
        var bahamutDiveTargetTether =
            "{\"Name\":\"bahamutDiveTargetTether\",\"type\":1,\"offY\":28.0,\"radius\":0.6,\"color\":4294901787,\"thicc\":7.6,\"refActorNPCNameID\":3639,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}";
        var ignasseTargetTether =
            "{\"Name\":\"ignasseTargetTether\",\"type\":1,\"offX\":-2.7,\"offY\":41.7,\"radius\":0.6,\"color\":4294901787,\"thicc\":7.6,\"refActorDataID\":12635,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}";
        var ignasseHitbox =
            "{\"Name\":\"ignasseHitbox\",\"type\":2,\"radius\":7.0,\"color\":1258291455,\"thicc\":7.0,\"FillStep\":1.5}";
        var vellguineTargetTether =
            "{\"Name\":\"vellguineTargetTether\",\"type\":1,\"offX\":4.7,\"offY\":41.7,\"radius\":0.6,\"color\":4294901787,\"thicc\":7.6,\"refActorDataID\":12633,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}";
        var vellguineHitbox =
            "{\"Name\":\"vellguineHitbox\",\"type\":2,\"radius\":7.0,\"color\":1258291455,\"thicc\":7.0,\"FillStep\":1.5}";
        _skydiveTargetElement = Controller.RegisterElementFromCode("skydivetether", skydiveTargetTether);
        _skydiveTargetElement.Enabled = false;
        _noSkydiveTargetElement = Controller.RegisterElementFromCode("noskydivetether", noSkydiveTargetTether);
        _noSkydiveTargetElement.Enabled = false;
        _bahamutDiveTargetElement = Controller.RegisterElementFromCode("bahamuttether", bahamutDiveTargetTether);
        _bahamutDiveTargetElement.Enabled = false;
        _ignasseTargetElement = Controller.RegisterElementFromCode("ignassetether", ignasseTargetTether);
        _ignasseTargetElement.Enabled = false;
        _ignasseHitboxElement = Controller.RegisterElementFromCode("ignassehitbox", ignasseHitbox);
        _ignasseHitboxElement.Enabled = false;
        _vellguineTargetElement = Controller.RegisterElementFromCode("vellgunietether", vellguineTargetTether);
        _vellguineTargetElement.Enabled = false;
        _vellguineHitboxElement = Controller.RegisterElementFromCode("vellguinehitbox", vellguineHitbox);
        _vellguineHitboxElement.Enabled = false;

        Controller.RegisterElementFromCode("""
            {"Name":"Pointer","refX":93.88083,"refY":122.337746,"refZ":-1.9073485E-06,"radius":0.5,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"tether":true}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Name":"DsrIgnasseTetherCapture","ZoneLockH":[968],"ElementsL":[{"Name":"Pos","type":1,"offX":15.86,"offY":32.66,"color":3356425984,"fillIntensity":0.5,"refActorNPCNameID":3638,"refActorComparisonType":6,"includeRotation":true,"tether":true,"IsCapturing":true,"Nodraw":true}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Name":"DsrVellguineTetherCapture","ZoneLockH":[968],"ElementsL":[{"Name":"Pos","type":1,"offX":5.34,"offY":40.6,"color":3356425984,"fillIntensity":0.5,"refActorNPCNameID":3638,"refActorComparisonType":6,"includeRotation":true,"tether":true,"IsCapturing":true,"Nodraw":true}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Name":"DsrDefamationCapture","ZoneLockH":[968],"Nodraw":true,"ElementsL":[{"Name":"Pos","Nodraw":true,"type":1,"offX":18.64,"offY":17.7,"color":3356425984,"fillIntensity":0.5,"refActorNPCNameID":3638,"refActorComparisonType":6,"includeRotation":true,"tether":true,"IsCapturing":true}]}
            """);
        Controller.Hide(layouts: false);
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 27529)
        {
            _active = true;
        }

        if(castId == 27538)
        {
            _active = false;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(vfxPath == "vfx/lockon/eff/m0005sp_19o0t.avfx")
        {
            if(target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc)
            {
                //DuoLog.Information($"Local player is {PC.Name}");
                if(PC == pvc)
                {
                    //DuoLog.Information($"Skyward Leap is on me, tether other side");
                    _skydiveTargetElement.Enabled = true;

                    var pos = this.Pos["DsrDefamationCapture"].Value;
                    Controller.Schedule(() =>
                    {
                        Controller.GetElementByName("Pointer").Enabled = true;
                        Controller.GetElementByName("Pointer").SetRefPosition(pos);
                        Controller.Schedule(() => Controller.GetElementByName("Pointer").Enabled = false, 6200);
                    }, 6000);
                }
                else
                {
                    //DuoLog.Information($"Skyward Leap is on someone else tether side");
                    if(_gottether)
                    {
                        return;
                    }

                    _noSkydiveTargetElement.Enabled = true;
                }

                Controller.Schedule(() =>
                {
                    _skydiveTargetElement.Enabled = false;
                }, 6200);
                Controller.Schedule(() =>
                {
                    _noSkydiveTargetElement.Enabled = false;
                }, 8000);
            }
        }

        if(vfxPath == "vfx/lockon/eff/bahamut_wyvn_glider_target_02tm.avfx")
        {
            if(target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc && pvc == PC)
            {
                //DuoLog.Information($"Oh no BahamutWYVNGLIDER on {pvc}");
                _bahamutDiveTargetElement.Enabled = true;

                Controller.Schedule(() => { _bahamutDiveTargetElement.Enabled = false;

                }, 10000);
            }
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        // Look for tethers only in p5 wrath (see OnMessage)
        if(!_active)
        {
            return;
        }

        if(source.TryGetObject(out var ignasse) && ignasse is IBattleChara ig && ig.NameId == 3638 &&
            target.TryGetObject(out var pi) && pi is IPlayerCharacter pic)
        {
            _ignassePlayer = pic;
            //DuoLog.Information($"Ignasse tether from {ignasse.Name} to {IgnassePlayer.Name} data {data2} || {data3} || {data5}");
            if(PC == pic)
            {
                _gottether = true;
                _noSkydiveTargetElement.Enabled = false;
                _skydiveTargetElement.Enabled = false;
                _ignasseTargetElement.Enabled = true;
                var pos = this.Pos["DsrIgnasseTetherCapture"].Value;
                Controller.Schedule(() => 
                { 
                    _ignasseTargetElement.Enabled = false;
                    Controller.GetElementByName("Pointer").Enabled = true;
                    Controller.GetElementByName("Pointer").SetRefPosition(pos);
                    Controller.Schedule(() => Controller.GetElementByName("Pointer").Enabled = false, 6000);
                }, 6200);

            }
            else
            {
                _ignasseHitboxElement.SetRefPosition(ignasse.Position);
                _ignasseHitboxElement.SetOffPosition(_ignassePlayer.Position);
                _ignasseHitboxElement.Enabled = true;
                Controller.Schedule(() => { _ignasseHitboxElement.Enabled = false; }, 7000);
            }
        }
        else if(source.TryGetObject(out var vellguine) && vellguine is IBattleChara vg && vg.NameId == 3636 &&
                 target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc)
        {
            _vellguinePlayer = pvc;
            //DuoLog.Information($"Vellguine tether from {vellguine.Name} to {VellguinePlayer.Name} data {data2} || {data3} || {data5}");
            if(PC == pvc)
            {
                _gottether = true;
                _noSkydiveTargetElement.Enabled = false;
                _skydiveTargetElement.Enabled = false;
                _vellguineTargetElement.Enabled = true;

                var pos = this.Pos["DsrVellguineTetherCapture"].Value;
                Controller.Schedule(() => { 
                    _vellguineTargetElement.Enabled = false;
                    Controller.GetElementByName("Pointer").Enabled = true;
                    Controller.GetElementByName("Pointer").SetRefPosition(pos);
                    Controller.Schedule(() => Controller.GetElementByName("Pointer").Enabled = false, 6000);
                }, 6200);
            }
            else
            {
                _vellguineHitboxElement.SetRefPosition(vellguine.Position);
                _vellguineHitboxElement.SetOffPosition(_vellguinePlayer.Position);
                _vellguineHitboxElement.Enabled = true;
                Controller.Schedule(() => { _vellguineHitboxElement.Enabled = false; }, 7000);
            }
        }
    }

    private void Off()
    {
        _active = false;
        _gottether = false;
        _skydiveTargetElement?.Enabled = false;
        _noSkydiveTargetElement?.Enabled = false;
        _bahamutDiveTargetElement?.Enabled = false;
        _ignasseTargetElement?.Enabled = false;
        _vellguineTargetElement?.Enabled = false;
        _ignasseHitboxElement?.Enabled = false;
        _vellguineHitboxElement?.Enabled = false;
    }


    Dictionary<string, Vector3?> Pos = [];
    public override void OnUpdate()
    {
        foreach(var x in Controller.GetRegisteredLayouts())
        {
            Pos[x.Key] = x.Value.GetCapturedPositions()?.SafeSelect("Pos")?.SafeSelect(0) ?? null;
        }
        Controller.GetRegisteredElements().Where(x => x.Value.tether).Each(x => x.Value.color = Controller.AttentionColor);
        if(_ignasseHitboxElement.Enabled)
        {
            _ignasseHitboxElement.SetRefPosition(Ignasse.Position);
            _ignasseHitboxElement.SetOffPosition(_ignassePlayer.Position);
        }

        if(_vellguineHitboxElement.Enabled)
        {
            _vellguineHitboxElement.SetRefPosition(Vellguine.Position);
            _vellguineHitboxElement.SetOffPosition(_vellguinePlayer.Position);
        }
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe))
        {
            Off();
        }
    }
}