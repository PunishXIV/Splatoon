using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.Schedulers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

internal class R4S_Replica_Wing_and_Line: SplatoonScript
{
    public override HashSet<uint> ValidTerritories => new() { 1232 };

    public override Metadata? Metadata => new(1, "Fragile");

    private delegate void ProcessPacketActorControlDelegate(uint actorID, uint category, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, ulong targetID, byte replaying);
    private static Hook<ProcessPacketActorControlDelegate>? ProcessPacketActorControlHook;

    public override void OnEnable()
    {
        ProcessPacketActorControlHook = Svc.Hook.HookFromSignature<ProcessPacketActorControlDelegate>("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", ProcessPacketActorControlDetour);
        ProcessPacketActorControlHook?.Enable();
    }

    private void ProcessPacketActorControlDetour(uint actorID, uint category, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, ulong targetID, byte replaying)
    {
        ProcessPacketActorControlHook!.Original(actorID, category, p1, p2, p3, p4, p5, p6, targetID, replaying);
        if (category == 407)
        {
            var obj = Svc.Objects.SearchById(actorID);
            switch (p1)
            {
                case 4561:
                    FirstLine(obj);
                    break;
                case 4562:
                    AfterLine(obj);
                    break;
                case 4563:
                    FirstWing(obj);
                    break;
                case 4564:
                    AfterWing(obj);
                    break;
            }
        }
    }

    private void FirstLine(IGameObject obj)
    {
        Controller.RegisterElementFromCode(obj.EntityId.ToString(), $"{{\"Name\":\"\",\"type\":3,\"refY\":30.0,\"radius\":5.0,\"fillIntensity\":0.345,\"originFillColor\":1157628159,\"endFillColor\":1157628159,\"refActorObjectID\":{obj.EntityId},\"refActorUseCastTime\":true,\"refActorCastTimeMax\":5.0,\"refActorUseOvercast\":true,\"refActorComparisonType\":2,\"includeRotation\":true,\"onlyVisible\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorUseTransformation\":true,\"refActorTetherConnectedWithPlayer\":[],\"refActorTransformationID\":7}}");
        _ = new TickScheduler(delegate
        {
            Controller.ClearRegisteredElements();
        }, 20000);
    }

    private void FirstWing(IGameObject obj)
    {
        Controller.RegisterElementFromCode(obj.EntityId.ToString(), $"{{\"Name\":\"\",\"type\":1,\"radius\":5.0,\"Donut\":10.0,\"fillIntensity\":0.345,\"originFillColor\":1157628159,\"endFillColor\":1157628159,\"refActorObjectID\":{obj.EntityId},\"refActorUseCastTime\":true,\"refActorCastTimeMax\":5.0,\"refActorUseOvercast\":true,\"refActorComparisonType\":2,\"includeRotation\":true,\"onlyVisible\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorUseTransformation\":true,\"refActorTetherConnectedWithPlayer\":[],\"refActorTransformationID\":31}}");
        _ = new TickScheduler(delegate
        {
            Controller.ClearRegisteredElements();
        }, 20000);
    }

    private void AfterLine(IGameObject obj)
    {
        _ = new TickScheduler(delegate
        {
            Controller.RegisterElementFromCode(obj.EntityId.ToString(), $"{{\"Name\":\"\",\"type\":3,\"refY\":30.0,\"radius\":5.0,\"fillIntensity\":0.345,\"originFillColor\":1157628159,\"endFillColor\":1157628159,\"refActorObjectID\":{obj.EntityId},\"refActorUseCastTime\":true,\"refActorCastTimeMax\":5.0,\"refActorUseOvercast\":true,\"refActorComparisonType\":2,\"includeRotation\":true,\"onlyVisible\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorUseTransformation\":true,\"refActorTetherConnectedWithPlayer\":[],\"refActorTransformationID\":7}}");
        }, 8000);
        _ = new TickScheduler(delegate
        {
            Controller.ClearRegisteredElements();
        }, 20000);
    }

    private void AfterWing(IGameObject obj)
    {
        _ = new TickScheduler(delegate
        {
            Controller.RegisterElementFromCode(obj.EntityId.ToString(), $"{{\"Name\":\"\",\"type\":1,\"radius\":5.0,\"Donut\":10.0,\"fillIntensity\":0.345,\"originFillColor\":1157628159,\"endFillColor\":1157628159,\"refActorObjectID\":{obj.EntityId},\"refActorUseCastTime\":true,\"refActorCastTimeMax\":5.0,\"refActorUseOvercast\":true,\"refActorComparisonType\":2,\"includeRotation\":true,\"onlyVisible\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorUseTransformation\":true,\"refActorTetherConnectedWithPlayer\":[],\"refActorTransformationID\":31}}");
        }, 8000);
        _ = new TickScheduler(delegate
        {
            Controller.ClearRegisteredElements();
        }, 20000);

    }


    public override void OnDisable()
    {
        ProcessPacketActorControlHook?.Dispose();
    }


}
