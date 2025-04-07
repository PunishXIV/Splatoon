using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ECommons;
using ECommons.Configuration;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public sealed class M5S_Lets_Dance_NavigateStandPosition: SplatoonScript
{
    private enum State
    {
        None,
        Casting,
        End
    }

    private const ushort AlphaDebuff = 0x116E;
    private const ushort BetaDebuff = 0x116F;

    private State _state = State.None;
    public override HashSet<uint>? ValidTerritories => [1257];

    private Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            radius = 1.5f,
            thicc = 15f,
            tether = true,
            Donut = 0.3f
        };
        Controller.RegisterElement("Bait", element);
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text($"State: {_state}");
    }

    public override void OnUpdate()
    {
        if (_state == State.Casting)
            Controller.GetRegisteredElements()
                .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
        else
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnReset()
    {
        _state = State.None;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 42858 && _state == State.None)
        {
            _state = State.Casting;
            var remainingTime = 0f;
            if (Player.Status.Any(x => x.StatusId == AlphaDebuff))
                remainingTime = Player.Status.First(x => x.StatusId == AlphaDebuff).RemainingTime;
            else if (Player.Status.Any(x => x.StatusId == BetaDebuff))
                remainingTime = Player.Status.First(x => x.StatusId == BetaDebuff).RemainingTime;

            PluginLog.Warning($"Remaining time: {remainingTime}");
            
            if (!Controller.TryGetElementByName("Bait", out var baitElement))
                return;

            switch (remainingTime)
            {
                case > 20:
                    baitElement.SetOffPosition(new Vector3(100, 0, 106));
                    break;
                case > 15:
                    baitElement.SetOffPosition(new Vector3(100, 0, 102));
                    break;
                case > 10:
                    baitElement.SetOffPosition(new Vector3(100, 0, 98));
                    break;
                case > 5:
                    baitElement.SetOffPosition(new Vector3(100, 0, 94));
                    break;
            }

            baitElement.Enabled = true;
        }
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status status)
    {
        if (_state is State.Casting && status.StatusId is (AlphaDebuff or BetaDebuff)) _state = State.End;
    }

    private class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
    }
}