using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Tests;
public unsafe class TestE1N : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [849];
    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("ShowAOE", $$"""
            {"Name":"","type":1,"radius":5.0,"Donut":40.0,"fillIntensity":0.417,"overlayPlaceholders":true,"overlayText":"$CAST","refActorNPCNameID":8345,"refActorRequireCast":true,"refActorCastId":[15767],"refActorUseCastTime":true,"refActorCastTimeMax":3.69,"refActorComparisonType":6,"onlyTargetable":true}
            """);
    }

    public override void OnUpdate()
    {
        if(Svc.Party.Length > 1) return;
        var cast = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.CastActionId == 15767);
        if(cast != null && cast.CurrentCastTime >= C.CastEnd && EzThrottler.Throttle(this.InternalData.FullName, 999999999))
        {
            Chat.ExecuteCommand("/automove on");
            Svc.Chat.Print($"Moving at {cast.CurrentCastTime}");
        }
    }

    public override void OnReset()
    {
        EzThrottler.Reset(this.InternalData.FullName);
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.SliderFloat("Automove at", ref C.CastEnd, 0, 3.69f);
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public float CastEnd = 3.69f;
    }
}
