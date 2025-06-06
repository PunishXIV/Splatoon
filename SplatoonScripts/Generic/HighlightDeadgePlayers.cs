using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.DalamudServices;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class HighlightDeadgePlayers : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [];

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Dead", """
            {"Name":"","Enabled":false,"radius":0.7,"color":3372155112,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4294967295,"overlayText":"Deadge","ObjectKinds":[]}
            """);
    }

    public override void OnUpdate()
    {
        foreach(var x in Controller.GetRegisteredElements()) x.Value.Enabled = false;
        int i = 0;
        foreach(var x in Svc.Objects)
        {
            if(x is IPlayerCharacter pc && pc.CurrentHp == 0)
            {
                var e = GetElement(i++);
                e.Enabled = true;
                e.SetRefPosition(pc.Position);
            }
        }
    }

    Element GetElement(int i)
    {
        if(Controller.TryGetElementByName($"_{i}", out var ret))
        {
            return ret;
        }
        else
        {
            var e = Controller.GetElementByName("Dead").JSONClone()!;
            Controller.RegisterElement($"_{i}", e);
            return e;
        }
    }
}
