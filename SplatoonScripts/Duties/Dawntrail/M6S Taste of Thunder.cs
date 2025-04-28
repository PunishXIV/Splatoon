using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe class M6S_Taste_of_Thunder : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1259];

    public override void OnSetup()
    {
        for(var i = 0; i < 8; i++)
        {
            Controller.RegisterElementFromCode($"Twister{i}", "{\"Name\":\"\",\"refX\":86.37486,\"refY\":92.822395,\"radius\":4.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }
        OnReset();
    }

    public override void OnReset()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action?.RowId == 42652)
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
            Controller.ScheduleReset(5000);
        }
        if(set.Action?.RowId == 42653)
        {
            Controller.Reset();
        }
    }
}
