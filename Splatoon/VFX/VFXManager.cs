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

        internal VFXManager()
        {
        }

        void Init()
        {
        }

        void Shutdown()
        {
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
