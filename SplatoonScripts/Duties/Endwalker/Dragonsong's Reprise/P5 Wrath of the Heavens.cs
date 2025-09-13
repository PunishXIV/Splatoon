using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;
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

    public override Metadata? Metadata => new(4, "Enthusiastus, Garume, damolitionn");

    private IBattleNpc? Ignasse =>
        Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == IgnasseDataId) as IBattleNpc;

    private IBattleNpc? Vellguine =>
        Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == VellguineDataId) as IBattleNpc;

    private IPlayerCharacter PC =>
        !string.IsNullOrWhiteSpace(TestOverride) &&
        FakeParty.Get().FirstOrDefault(x => x.Name.TextValue == TestOverride) is IPlayerCharacter pc
            ? pc
            : Svc.ClientState.LocalPlayer!;

    private Config Conf => Controller.GetConfig<Config>();

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
    }


    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 27529) _active = true;
        if(castId == 27538) _active = false;
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(vfxPath == "vfx/lockon/eff/m0005sp_19o0t.avfx")
            if(target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc)
            {
                //DuoLog.Information($"Local player is {PC.Name}");
                if(PC == pvc)
                {
                    //DuoLog.Information($"Skyward Leap is on me, tether other side");
                    _skydiveTargetElement.Enabled = true;
                }
                else
                {
                    //DuoLog.Information($"Skyward Leap is on someone else tether side");
                    if(_gottether)
                        return;
                    _noSkydiveTargetElement.Enabled = true;
                }

                Task.Delay(8000).ContinueWith(_ =>
                {
                    _skydiveTargetElement.Enabled = false;
                    _noSkydiveTargetElement.Enabled = false;
                });
            }

        if(vfxPath == "vfx/lockon/eff/bahamut_wyvn_glider_target_02tm.avfx")
            if(target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc && pvc == PC)
            {
                //DuoLog.Information($"Oh no BahamutWYVNGLIDER on {pvc}");
                _bahamutDiveTargetElement.Enabled = true;
                Task.Delay(10000).ContinueWith(_ => { _bahamutDiveTargetElement.Enabled = false; });
            }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        // Look for tethers only in p5 wrath (see OnMessage)
        if(!_active) return;
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
                Task.Delay(6200).ContinueWith(_ => { _ignasseTargetElement.Enabled = false; });
            }
            else
            {
                _ignasseHitboxElement.SetRefPosition(ignasse.Position);
                _ignasseHitboxElement.SetOffPosition(_ignassePlayer.Position);
                _ignasseHitboxElement.Enabled = true;
                Task.Delay(7000).ContinueWith(_ => { _ignasseHitboxElement.Enabled = false; });
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
                Task.Delay(6200).ContinueWith(_ => { _vellguineTargetElement.Enabled = false; });
            }
            else
            {
                _vellguineHitboxElement.SetRefPosition(vellguine.Position);
                _vellguineHitboxElement.SetOffPosition(_vellguinePlayer.Position);
                _vellguineHitboxElement.Enabled = true;
                Task.Delay(7000).ContinueWith(_ => { _vellguineHitboxElement.Enabled = false; });
            }
        }
    }


    private void Off()
    {
        _active = false;
        _gottether = false;
        if(_skydiveTargetElement != null)
            _skydiveTargetElement.Enabled = false;
        if(_noSkydiveTargetElement != null)
            _noSkydiveTargetElement.Enabled = false;
        if(_bahamutDiveTargetElement != null)
            _bahamutDiveTargetElement.Enabled = false;
        if(_ignasseTargetElement != null)
            _ignasseTargetElement.Enabled = false;
        if(_vellguineTargetElement != null)
            _vellguineTargetElement.Enabled = false;
        if(_ignasseHitboxElement != null)
            _ignasseHitboxElement.Enabled = false;
        if(_vellguineHitboxElement != null)
            _vellguineHitboxElement.Enabled = false;
    }

    public override void OnUpdate()
    {
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
                DirectorUpdateCategory.Wipe)) Off();
    }

    public static unsafe Vector4 Vector4FromRGBA(uint col)
    {
        var bytes = (byte*)&col;
        return new Vector4(bytes[3] / 255f, bytes[2] / 255f, bytes[1] / 255f, bytes[0] / 255f);
    }

    public class Config : IEzConfig
    {
        public Vector4 ColDoom = Vector4FromRGBA(0x0000ffC8);
        public Vector4 ColNoDoom = Vector4FromRGBA(0xFF0000C8);
        public float offZ = 1.8f;
        public float tScale = 7f;
    }
}