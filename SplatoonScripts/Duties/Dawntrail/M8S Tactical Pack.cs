using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M8S_Tactical_Pack :SplatoonScript
{
    public enum State
    {
        None,
        TetherCreated,
        Running,
        End
    }

    private const ushort GreenDebuff = 4392;
    private const ushort OrangeDebuff = 4391;
    private const ushort OrangeDragonDataId = 18225;
    private const ushort GreenDragonDataId = 18219;
    private const ushort OrangeCubeDataId = 18262;
    private const ushort GreenSphereDataId = 18261;

    private bool _isGreen;

    private State _state = State.None;
    public override HashSet<uint>? ValidTerritories => [1263];
    public override Metadata? Metadata => new(3, "Garume");

    private static IBattleNpc? OrangeDragon => Svc.Objects
        .FirstOrDefault(x => x.DataId == OrangeDragonDataId) as IBattleNpc;

    private static IBattleNpc? GreenDragon => Svc.Objects
        .FirstOrDefault(x => x.DataId == GreenDragonDataId) as IBattleNpc;

    private static IBattleNpc? OrangeCube => Svc.Objects
        .FirstOrDefault(x => x.DataId == OrangeCubeDataId) as IBattleNpc;

    private static IBattleNpc? GreenSphere => Svc.Objects
        .FirstOrDefault(x => x.DataId == GreenSphereDataId) as IBattleNpc;

    private Config C => Controller.GetConfig<Config>();

    public override void OnRemoveBuffEffect(uint sourceId, Status status)
    {
        if (status.StatusId is GreenDebuff or OrangeDebuff)
        {
            var greenCount = FakeParty.Get().Count(x => x.StatusList.Any(y => y.StatusId == GreenDebuff));
            var orangeCount = FakeParty.Get().Count(x => x.StatusList.Any(y => y.StatusId == OrangeDebuff));
            if (greenCount == 0 && orangeCount == 0) _state = State.End;
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _isGreen = false;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        Controller.GetRegisteredLayouts().Each(x => x.Value.Enabled = false);
    }

    public override void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
    {
        if (_state == State.TetherCreated)
        {
            _state = State.Running;
            if (Controller.TryGetElementByName("Tether", out var element)) element.Enabled = false;
        }
    }

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            radius = 1.5f,
            thicc = 15f,
            tether = true,
            Donut = 0.3f
        };
        Controller.RegisterElement("Tether", element);

        Controller.TryRegisterLayoutFromCode("Clock", "~Lv2~{\"Name\":\"a\",\"Group\":\"\",\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"refZ\":-1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.96813,\"refY\":97.22127,\"refZ\":1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":95.92647,\"refY\":97.18153,\"refZ\":-1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":102.51564,\"offY\":95.04247,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":102.631516,\"offY\":96.98153,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":102.76445,\"refY\":101.71465,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":105.53386,\"refY\":101.99625,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"offX\":98.856384,\"offY\":101.83813,\"offZ\":3.8146973E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"offX\":98.87363,\"offY\":104.19578,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}]}", out _);
        Controller.TryRegisterLayoutFromCode("CounterClock", "~Lv2~{\"Name\":\"as\",\"Group\":\"\",\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"refZ\":-1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"refZ\":-1.9073486E-06,\"offX\":95.90114,\"offY\":101.83096,\"offZ\":1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":97.0,\"refY\":103.0,\"refZ\":-1.9073486E-06,\"offX\":98.18604,\"offY\":101.70221,\"offZ\":3.8146973E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":98.58479,\"refY\":95.15524,\"refZ\":1.9073486E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":98.342224,\"refY\":96.79275,\"refZ\":3.8146973E-06,\"offX\":97.0,\"offY\":96.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":105.338264,\"offY\":97.11067,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":104.0,\"refY\":96.0,\"offX\":102.768,\"offY\":97.18019,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":102.25408,\"refY\":104.04354,\"refZ\":1.9073486E-06,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0},{\"Name\":\"\",\"type\":2,\"refX\":102.36715,\"refY\":101.80373,\"offX\":104.0,\"offY\":103.0,\"offZ\":-1.9073486E-06,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":12.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}]}", out _);
    }

    public override void OnUpdate()
    {
        if (_state is State.Running)
        {
            var orangeDragon = Svc.Objects.FirstOrDefault(
                x => x.DataId == OrangeDragonDataId && x is IBattleNpc npc && npc.IsCharacterVisible()) as IBattleNpc;
            var greenDragon = Svc.Objects.FirstOrDefault(
                x => x.DataId == GreenDragonDataId && x is IBattleNpc npc && npc.IsCharacterVisible()) as IBattleNpc;
            var orangeCube = Svc.Objects.FirstOrDefault(
                x => x.DataId == OrangeCubeDataId && x is IBattleNpc npc && npc.IsCharacterVisible()) as IBattleNpc;
            var greenSphere = Svc.Objects.FirstOrDefault(
                x => x.DataId == GreenSphereDataId && x is IBattleNpc npc && npc.IsCharacterVisible()) as IBattleNpc;
            if (orangeDragon is null || greenDragon is null || orangeCube is null || greenSphere is null) return;

            var isClock = orangeDragon.Position is { X: 90f, Z: 100f } && greenSphere.Position is { X: 100f, Z: 90f };
            isClock = isClock || (greenDragon.Position is { X: 90f, Z: 100f } && orangeCube.Position is { X: 100f, Z: 90f });

            if (isClock)
            {
                if (Controller.TryGetLayoutByName("Clock", out var layout))
                {
                    layout.Enabled = true;
                }
            }
            else
            {
                if (Controller.TryGetLayoutByName("CounterClock", out var layout))
                {
                    layout.Enabled = true;
                }
            }
        }

        if (_state is State.TetherCreated or State.Running)
        {
            if (Controller.TryGetElementByName("Tether", out var element))
                element.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
        }
        else
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }

        if (_state == State.Running)
        {
            if (_isGreen)
            {
                var remainingTime = Player.Status
                    .FirstOrDefault(x => x.StatusId == GreenDebuff)?.RemainingTime ?? 0;
                if (remainingTime == 0)
                {
                    _state = State.End;
                    return;
                }

                if (remainingTime < C.WindRemainingTime)
                {
                    var pos = GreenSphere!.Position;
                    if (Controller.TryGetElementByName("Tether", out var element))
                    {
                        element.Enabled = true;
                        element.SetOffPosition(pos);
                    }
                }
            }
            else
            {
                var remainingTime = Player.Status
                    .FirstOrDefault(x => x.StatusId == OrangeDebuff)?.RemainingTime ?? 0;
                if (remainingTime == 0)
                {
                    _state = State.End;
                    return;
                }

                if (remainingTime < C.StoneRemainingTime)
                {
                    var pos = OrangeCube!.Position;
                    if (Controller.TryGetElementByName("Tether", out var element))
                    {
                        element.Enabled = true;
                        element.SetOffPosition(pos);
                    }
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.SliderInt("Wind Remaining Time", ref C.WindRemainingTime, 0, 20);
        ImGuiEx.SliderInt("Stone Remaining Time", ref C.StoneRemainingTime, 0, 20);
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        PluginLog.Warning($"taget: {target}, source: {source}, data2: {data2}, data3: {data3}, data5: {data5}");
        PluginLog.Warning($"source: {source.GetObject().Address} player: {Player.Object.Address}");
        if (target.GetObject().DataId is OrangeDragonDataId or GreenDragonDataId &&
            _state == State.None &&
            source.GetObject().Address == Player.Object.Address)
        {
            _state = State.TetherCreated;
            _isGreen = target.GetObject().DataId == OrangeDragonDataId;
            var pos = _isGreen ? GreenDragon!.Position : OrangeDragon!.Position;
            if (Controller.TryGetElementByName("Tether", out var element))
            {
                element.Enabled = true;
                element.SetOffPosition(pos);
            }
        }
    }

    public class Config :IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public int WindRemainingTime = 16;
        public int StoneRemainingTime = 8;
    }
}