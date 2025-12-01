using ECommons;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Quantum_40;

public class Spineslash : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1311];

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("1", """
            {"Name":"","refX":-613.5,"refY":-305.5,"radius":5.0,"Donut":0.2,"color":3355639552,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            """);
        Controller.RegisterElementFromCode("2", """
            {"Name":"","refX":-600.0,"refY":-305.5,"radius":5.0,"Donut":0.2,"color":3355639552,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            """);
        Controller.RegisterElementFromCode("3", """
            {"Name":"","refX":-586.5,"refY":-304.0,"radius":5.0,"Donut":0.2,"color":3355639552,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            """);
    }

    public override void OnEnable()
    {
        this.Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    int Num = 0;
    public override void OnReset()
    {
        this.Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        Num = 0;
    }
    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 44125)
        {
            Num++;
            if(Controller.TryGetElementByName(Num.ToString(), out var e))
            {
                e.Enabled = true;
                this.Controller.Schedule(() => e.Enabled = false, 12000);
            }
        }
    }
}
