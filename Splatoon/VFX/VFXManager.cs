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
            Penumbra.Api.Ipc.Initialized.Subscriber(Svc.PluginInterface, Init);
            Penumbra.Api.Ipc.Disposed.Subscriber(Svc.PluginInterface, Shutdown);
        }

        void Init()
        {
            Penumbra.Api.Ipc.CreateNamedTemporaryCollection.Subscriber(Svc.PluginInterface).Invoke("Splatoon");
        }

        void Shutdown()
        {
            Penumbra.Api.Ipc.RemoveTemporaryCollectionByName.Subscriber(Svc.PluginInterface).Invoke("Splatoon");
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
