using ECommons.DalamudServices;
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
    public class VentureOverrideTest : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new();

        public override void OnEnable()
        {
            Svc.PluginInterface.GetIpcSubscriber<string, object>("AutoRetainer.OnSendRetainerToVenture").Subscribe(OnRetainerSend);
        }

        public override void OnDisable()
        {
            Svc.PluginInterface.GetIpcSubscriber<string, object>("AutoRetainer.OnSendRetainerToVenture").Unsubscribe(OnRetainerSend);
        }

        void OnRetainerSend(string name)
        {
            DuoLog.Information($"{name} is about to be sent to venture!");
            var randomVenture = (uint)new Random().Next(1, 27);
            Svc.PluginInterface.GetIpcSubscriber<uint, object>("AutoRetainer.SetVenture").InvokeAction(randomVenture);
        }
    }
}
