using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Forked_Tower_Magic;

public class FT_Boss4_Predict : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1346];

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, uint p7, uint p8, ulong targetId, byte replaying)
    {
        base.OnActorControl(sourceId, command, p1, p2, p3, p4, p5, p6, p7, p8, targetId, replaying);
    }
}
