using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol
{
    public class Hello_Near_Far_World : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1122 };

        public override Metadata? Metadata => new(2, "NightmareXIV");

        //  _rsv_3442_-1_1_0_0_S74CFC3B0_E74CFC3B0 (3442), Remains = 21.7, Param = 0, Count = 0
        const uint EffectNear = 3442;
        //  _rsv_3443_-1_1_0_0_S74CFC3B0_E74CFC3B0 (3443), Remains = 21.7, Param = 0, Count = 0
        const uint EffectFar = 3443;

        public override void OnSetup()
        {
            Controller.RegisterElementFromCode("Near1", "{\"Name\":\"Near1\",\"Enabled\":false,\"radius\":4.0,\"thicc\":5.0}");
            Controller.RegisterElementFromCode("Near2", "{\"Name\":\"Near2\",\"Enabled\":false,\"radius\":4.0,\"thicc\":5.0}");
            Controller.RegisterElementFromCode("NearT1", "{\"Name\":\"NearT1\",\"type\":2,\"Enabled\":false,\"radius\":0.0}");
            Controller.RegisterElementFromCode("NearT2", "{\"Name\":\"NearT2\",\"type\":2,\"Enabled\":false,\"radius\":0.0,\"thicc\":5.0}");
            Controller.RegisterElementFromCode("Far1", "{\"Name\":\"Far1\",\"Enabled\":false,\"radius\":4.0,\"color\":3372220160,\"thicc\":5.0}");
            Controller.RegisterElementFromCode("Far2", "{\"Name\":\"Far2\",\"Enabled\":false,\"radius\":4.0,\"color\":3372220160,\"thicc\":5.0}");
            Controller.RegisterElementFromCode("FarT1", "{\"Name\":\"FarT1\",\"type\":2,\"Enabled\":false,\"radius\":0.0,\"color\":3372220160}");
            Controller.RegisterElementFromCode("FarT2", "{\"Name\":\"FarT2\",\"type\":2,\"Enabled\":false,\"radius\":0.0,\"color\":3372220160,\"thicc\":5.0}");
            base.OnSetup();
        }

        public override void OnUpdate()
        {
            if(FakeParty.Get().Any(x => x.StatusList.Any(z => z.StatusId == EffectFar && z.RemainingTime < 10f)))
            {
                var near = FakeParty.Get().FirstOrDefault(x => HasNFDebuff(x, EffectNear));
                var nearTarget = FakeParty.Get().Where(x => x.Address != near.Address).OrderBy(x => Vector3.Distance(near.Position, x.Position)).FirstOrDefault();
                var nearTarget2 = FakeParty.Get().Where(x => !x.Address.EqualsAny(near.Address, nearTarget.Address)).OrderBy(x => Vector3.Distance(nearTarget.Position, x.Position)).FirstOrDefault();

                var far = FakeParty.Get().FirstOrDefault(x => HasNFDebuff(x, EffectFar));
                var farTarget = FakeParty.Get().Where(x => x.Address != far.Address).OrderByDescending(x => Vector3.Distance(far.Position, x.Position)).FirstOrDefault();
                var farTarget2 = FakeParty.Get().Where(x => !x.Address.EqualsAny(far.Address, farTarget.Address)).OrderByDescending(x => Vector3.Distance(farTarget.Position, x.Position)).FirstOrDefault();

                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = true);

                Controller.GetElementByName("Near1").SetRefPosition(nearTarget.Position);
                Controller.GetElementByName("Near2").SetRefPosition(nearTarget2.Position);
                Controller.GetElementByName("NearT1").SetRefPosition(near.Position);
                Controller.GetElementByName("NearT1").SetOffPosition(nearTarget.Position);
                Controller.GetElementByName("NearT2").SetRefPosition(nearTarget.Position);
                Controller.GetElementByName("NearT2").SetOffPosition(nearTarget2.Position);

                Controller.GetElementByName("Far1").SetRefPosition(farTarget.Position);
                Controller.GetElementByName("Far2").SetRefPosition(farTarget2.Position);
                Controller.GetElementByName("FarT1").SetRefPosition(far.Position);
                Controller.GetElementByName("FarT1").SetOffPosition(farTarget.Position);
                Controller.GetElementByName("FarT2").SetRefPosition(farTarget.Position);
                Controller.GetElementByName("FarT2").SetOffPosition(farTarget2.Position);
            }
            else
            {
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            }
        }

        const uint FirstInLine = 3004;
        const uint SecondInLine = 3005;
        static bool HasNFDebuff(PlayerCharacter pc, uint debuff)
        {
            var isFirst = FakeParty.Get().Any(x => x.StatusList.Any(z => z.StatusId == FirstInLine) && x.StatusList.Any(z => z.StatusId == EffectFar));
            if (isFirst)
            {
                return pc.StatusList.Any(x => x.StatusId == debuff) && pc.StatusList.Any(x => x.StatusId == FirstInLine);
            }
            else
            {
                return pc.StatusList.Any(x => x.StatusId == debuff);
            }
        }
    }
}
