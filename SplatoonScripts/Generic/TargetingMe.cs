using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public class TargetingMe : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [];

    public override void OnSettingsDraw()
    {
        if(Player.Available)
        {
            foreach(var x in Svc.Objects)
            {
                if(x is IPlayerCharacter pc && pc.TargetObject.AddressEquals(Player.Object))
                {
                    ImGuiEx.Text(pc.GetNameWithWorld());
                    if(ImGuiEx.HoveredAndClicked() && pc.IsTargetable)
                    {
                        Svc.Targets.Target = pc;
                    }
                }
            }
        }
    }
}
