using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P10S_Debuffs : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1150 };

        public override Metadata? Metadata => new(1, "NightmareXIV");

        const uint SingleSpread = 3550;
        const uint TwoStack = 3551;
        const uint FourStack = 3696;

        readonly Dictionary<uint, string> Alerts = new()
        {
            { SingleSpread, "Spread" },
            { TwoStack, "2 people stack" },
            { FourStack, "4 people stack" },
        };

        public override void OnSetup()
        {
            Controller.RegisterElementFromCode("Alert", "{\"Name\":\"\",\"type\":1,\"radius\":0.0,\"color\":4294902011,\"overlayBGColor\":4294902011,\"overlayTextColor\":4294967295,\"overlayVOffset\":2.0,\"overlayFScale\":2.0,\"thicc\":0.0,\"overlayText\":\"ALERT\",\"refActorType\":1}");
        }

        public override void OnUpdate()
        {
            if (Controller.TryGetElementByName("Alert", out var e))
            {
                e.Enabled = false;
                var entity = FakeParty.Get().FirstOrDefault(x => x.StatusList.Count(z => z.StatusId.EqualsAny(SingleSpread, TwoStack, FourStack)) == 2) ?? FakeParty.Get().FirstOrDefault(x => x.StatusList.Count(z => z.StatusId.EqualsAny(SingleSpread, TwoStack, FourStack)) == 1);
                if (entity != null)
                {
                    var status = entity.StatusList.Where(z => z.StatusId.EqualsAny(SingleSpread, TwoStack, FourStack)).OrderBy(x => x.RemainingTime).ToArray();
                    var text = status.Select(x => Alerts[x.StatusId]).Join(" -> ");
                    e.Enabled = status.Any(x => x.RemainingTime < 10f);
                    e.overlayText = text;
                }
            }
        }
    }
}
