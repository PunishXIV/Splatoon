using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic
{
    public class ZoneNameToast : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new();
        public override void OnEnable()
        {
            Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        }

        public override void OnDisable()
        {
            Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        }

        private unsafe void ClientState_TerritoryChanged(object? sender, ushort e)
        {
            /*if(GenericHelpers.TryGetAddonByName<AtkUnitBase>("FadeMiddle", out var a) && a->IsVisible)
            {
                return;
            }*/
            var t = Svc.Data.GetExcelSheet<TerritoryType>()?.GetRow(e);
            if(t != null)
            {
                var name = t.PlaceName.Value?.Name.ToString();
                var cfc = t.ContentFinderCondition.Value?.Name.ToString();
                if (!name.IsNullOrEmpty())
                {
                    if(!cfc.IsNullOrEmpty() && name != cfc)
                    {
                        Svc.Toasts.ShowQuest($"{name} ({cfc})");
                    }
                    else
                    {
                        Svc.Toasts.ShowQuest($"{name}");
                    }
                }
            }
        }
    }
}
