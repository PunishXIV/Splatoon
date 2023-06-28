using Splatoon.VFX.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.VFX
{
    internal class VFXManager : IDisposable
    {
        internal List<VFXRequest> RequestList = new();
        internal string TempDir = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "fxtemp");
        internal FXDonutController FXDonutController;

        internal VFXManager()
        {
            if (!Directory.Exists(TempDir)) Directory.CreateDirectory(TempDir);
            FXDonutController = new();
        }

        public void Dispose()
        {
            FXDonutController.Dispose();
        }
    }
}
