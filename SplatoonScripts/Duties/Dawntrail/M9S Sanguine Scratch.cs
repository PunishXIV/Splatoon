using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M9S_Sanguine_Scratch : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1321];

    public override void OnSetup()
    {
        for(int i = 0; i < 8; i++)
        {
            Controller.RegisterElementFromCode($"{i}", """{"Name":"","type":5,"refX":100.0,"refY":100.0,"radius":30.0,"coneAngleMin":-15,"coneAngleMax":15,"refActorNPCNameID":14300,"refActorRequireCast":true,"refActorCastId":[45989],"refActorComparisonType":6,"includeRotation":true}""");
        }
    }

    int CastNum = 0;
    int ElementNum = 0;

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if(packet->ActionDescriptor == new Splatoon.Data.ActionDescriptor(FFXIVClientStructs.FFXIV.Client.Game.ActionType.Action, 45989))
        {
            CastNum = 0;
            if(Controller.TryGetElementByName($"{ElementNum}", out var e))
            {
                e.AdditionalRotation = packet->Rotation;
                PluginLog.Information($"Rotation: {e.AdditionalRotation}");
            }
            ElementNum++;
        }
        if(packet->ActionDescriptor == new Splatoon.Data.ActionDescriptor(FFXIVClientStructs.FFXIV.Client.Game.ActionType.Action, 45992))
        {
            this.Controller.Reset();
        }
    }

    public override void OnReset()
    {
        CastNum = 0;
        ElementNum = 0;
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        if(CastNum.InRange(1, 5))
        {
            foreach(var x in Controller.GetRegisteredElements())
            {
                x.Value.Enabled = true;
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action?.RowId == 45989 || set.Action?.RowId == 45991)
        {
            if(EzThrottler.Throttle(this.InternalData.FullName + "Cast", 250))
            {
                CastNum++;
                foreach(var x in Controller.GetRegisteredElements())
                {
                    x.Value.AdditionalRotation += (22.5f).DegToRad();
                }
                PluginLog.Information($"CastNum: {CastNum}");
            }
        }
    }
}
