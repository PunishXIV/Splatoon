using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
internal class M7S_Target_enforcer_for_tanks :SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1261];
    public override Metadata? Metadata => new(1, "Redmoon");


}
