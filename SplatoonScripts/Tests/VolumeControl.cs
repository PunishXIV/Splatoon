using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Interop;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests
{
    public class VolumeControl : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new();
        int storeVol;

        public override void OnEnable()
        {
            Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        }
        public override void OnDisable()
        {
            Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        }

        private void ClientState_TerritoryChanged(ushort e)
        {
            throw new NotImplementedException();
        }

        void SetVolume()
        {
            
        }

        class UiBuilderSched
        {
            Action Action;
            public UiBuilderSched(Action a)
            {
                Action = a;
                Svc.PluginInterface.UiBuilder.Draw += UiBuilder_Draw;
            }

            private void UiBuilder_Draw()
            {
                Svc.PluginInterface.UiBuilder.Draw -= UiBuilder_Draw;
                GenericHelpers.Safe(Action);
            }
        }
    }
}
