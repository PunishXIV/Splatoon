using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Stormblood;
public unsafe sealed class UWU_Feather_Rain : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [Raids.the_Weapons_Refrain_Ultimate];

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode($"CaseAOE", """{"Name":"","type":1,"radius":3.0,"refActorNPCNameID":1644,"refActorRequireCast":true,"refActorCastId":[11085],"refActorComparisonType":6}""");
        for(var i = 0; i < 8; i++)
        {
            Controller.RegisterElementFromCode($"Twister{i}", "{\"Name\":\"\",\"refX\":86.37486,\"refY\":92.822395,\"radius\":4.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }
        OnReset();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 11085) this.Controller.Reset();
    }

    public override void OnReset()
    {
        Controller.GetRegisteredElements().Where(x => x.Key.StartsWith("Twister")).Each(x => x.Value.Enabled = false);
    }

    //07:45:27.583 | INF | [Splatoon] 400114D0(Garuda - BattleNpc) at 243BE92A350, 54, 0, 0, 0, 0, 0, 0, 3758096384
    //07:45:28.020 | INF | [Splatoon] 400114D0(Garuda - BattleNpc) at 243BE92A350, 407, 7738, 0, 0, 0, 0, 0, 3758096384

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, ulong targetId, byte replaying)
    {
        if(sourceId.GetObject() is IBattleNpc n && n.DataId.EqualsAny(8722u, 8723u))
        {
            if(command == 407 && p1 == 7738)
            {
                var obj = Svc.Objects.OfType<IPlayerCharacter>().ToArray();
                for(var i = 0; i < obj.Length; i++)
                {
                    if(Controller.TryGetElementByName($"Twister{i}", out var e))
                    {
                        e.Enabled = true;
                        e.SetRefPosition(obj[i].Position);
                    }
                }
                Controller.ScheduleReset(3000);
            }
        }
        /*
        if(sourceId.GetObject() is IPlayerCharacter) return;
        PluginLog.Information($"{sourceId.GetObject()}, {command}, {p1}, {p2}, {p3}, {p4}, {p5}, {p6}, {targetId}");
        */
    }
}