using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests
{
    public class PotionTest : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 561, 562, 563, 564, 565, 570, 593, 594, 595, 596, 597, 598, 599, 600, 601, 602, 603, 604, 605, 606, 607 };

        const uint Sustaining_Potion = 20309, Empyrean_Potion = 23163, Orthos_Potion = 38944;

        unsafe void UseItem(uint itemId)
        {
            if (EzThrottler.Throttle("PotionTest.UseItem", 1000))
            {
                ActionManager.Instance()->UseAction(ActionType.Item, itemId, 0xE0000000, 65535, 0, 0, null);
            }
        }

        float PlayerHealthPercentageHp => (float)Svc.ClientState.LocalPlayer.CurrentHp / (float)Svc.ClientState.LocalPlayer.MaxHp;

        public override void OnUpdate()
        {
            if (PlayerHealthPercentageHp <= 0.7)
            {
                UseItem(Sustaining_Potion);
            }
        }
    }
}
