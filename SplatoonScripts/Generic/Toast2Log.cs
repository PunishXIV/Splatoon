using ECommons.DalamudServices;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic
{
    public class Toast2Log : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new();

        public override void OnEnable()
        {
            Svc.Toasts.ErrorToast += Toasts_ErrorToast;
        }

        public override void OnDisable()
        {
            Svc.Toasts.ErrorToast -= Toasts_ErrorToast;
        }

        private void Toasts_ErrorToast(ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
        {
            DuoLog.Information($"[ErrorToast] {message.ToString()}");
        }
    }
}
