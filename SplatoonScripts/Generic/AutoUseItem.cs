using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class AutoUseItem : SplatoonScript
{
    private const uint PotionID = 12345;
    public override HashSet<uint>? ValidTerritories { get; } = [];
    public override Metadata Metadata => new(1, "NightmareXIV");

    public override void OnUpdate()
    {
        if(Player.Available && (float)Player.Object.CurrentHp / (float)Player.Object.MaxHp < 0.3f && InventoryManager.Instance()->GetInventoryItemCount(PotionID) + InventoryManager.Instance()->GetInventoryItemCount(PotionID, true) > 0)
        {
            if(!Player.IsAnimationLocked && ActionManager.Instance()->GetActionStatus(ActionType.Item, PotionID) == 0 && EzThrottler.Throttle("AutoUsePot"))
            {
                AgentInventoryContext.Instance()->UseItem(PotionID);
            }
        }
    }
}
