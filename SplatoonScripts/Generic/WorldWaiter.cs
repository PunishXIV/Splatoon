using ECommons;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using NightmareUI.ImGuiElements;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public class WorldWaiter : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;

    int World = 0;

    public override void OnUpdate()
    {
        if(World > 0)
        {
            if(Player.Interactable && GenericHelpers.IsScreenReady() && Svc.ClientState.LocalPlayer?.CurrentWorld.Id == World)
            {
                Environment.Exit(0);
            }
        }
    }


    WorldSelector WorldSelector = new();
    public override void OnSettingsDraw()
    {
        WorldSelector.Draw(ref World);
    }
}
