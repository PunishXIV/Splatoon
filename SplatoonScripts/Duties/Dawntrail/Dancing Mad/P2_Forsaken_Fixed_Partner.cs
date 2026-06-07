using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public class P2_Forsaken_Fixed_Partner : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];

    public override void OnSetup()
    {
        Controller.RegisterLayoutFromCode("Test Layout", """
            ~Lv2~{"ElementsL":[{"Name":"","refX":-1.035179,"refY":3.9711714,"refZ":0.013618118,"radius":1.92},{"Name":"","refX":2.2761102,"refY":2.9127426,"refZ":0.013645958,"color":3364456841,"fillIntensity":0.5}]}
            """);
    }
}
