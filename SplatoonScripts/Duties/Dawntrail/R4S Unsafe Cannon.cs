using ECommons;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class R4S_Unsafe_Cannon : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1232];
    public override Metadata? Metadata => new(2, "NightmareXIV");
    private uint DebuffYellow = 4000;
    private uint DebuffBlue = 4001;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Yellow", "{\"Name\":\"yellow\",\"type\":3,\"refY\":20.0,\"radius\":5.0,\"color\":3355508223,\"fillIntensity\":0.5,\"originFillColor\":1157628159,\"endFillColor\":1157628159,\"refActorName\":\"*\",\"refActorRequireCast\":true,\"refActorCastId\":[38360],\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        Controller.RegisterElementFromCode("Blue", "{\"Name\":\"blue\",\"type\":3,\"refY\":20.0,\"radius\":5.0,\"color\":3372218624,\"fillIntensity\":0.5,\"originFillColor\":1157628159,\"endFillColor\":1157628159,\"refActorName\":\"*\",\"refActorRequireCast\":true,\"refActorCastId\":[38361],\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
    }

    public override void OnUpdate()
    {
        if(Player.Available)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            if(Player.Object.StatusList.Any(x => x.StatusId == DebuffYellow))
            {
                Controller.GetElementByName("Yellow")!.Enabled = true;
            }
            if(Player.Object.StatusList.Any(x => x.StatusId == DebuffBlue))
            {
                Controller.GetElementByName("Blue")!.Enabled = true;
            }
        }
    }
}
