using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
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
        public override Metadata? Metadata => new(3, "NightmareXIV");
        public override HashSet<uint> ValidTerritories => [];
        public override void OnEnable()
        {
            Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        }

        public override void OnDisable()
        {
            Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        }

        private unsafe void ClientState_TerritoryChanged(ushort e)
        {
            /*if(GenericHelpers.TryGetAddonByName<AtkUnitBase>("FadeMiddle", out var a) && a->IsVisible)
            {
                return;
            }*/
            var t = Svc.Data.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(e);
            if(t != null)
            {
                var name = t.Value.PlaceName.ValueNullable?.Name.ToString();
                var cfc = t.Value.ContentFinderCondition.ValueNullable?.Name.ToString();
                if(!name.IsNullOrEmpty())
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
