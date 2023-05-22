using ECommons.DalamudServices;
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

        private void ClientState_TerritoryChanged(object? sender, ushort e)
        {
            throw new NotImplementedException();
        }

        void SetVolume()
        {
            Svc.GameConfig.
        }
    }
}
