using ECommons.Hooks;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests
{
    public class DirectorUpdateTest : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new();

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            DuoLog.Information($"{category}/{category:X}");
        }
    }
}
