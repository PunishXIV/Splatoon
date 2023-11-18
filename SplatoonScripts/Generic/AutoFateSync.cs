using ECommons.Automation;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Generic;

public class AutoFateSync : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => new(OpenAreas.List.Select(x => (uint)x));
    public override Metadata? Metadata => new(1, "NightmareXIV");

    public override void OnMessage(string Message)
    {
        if(Message.Contains("You are 5 or more levels above the recommended level for this FATE."))
        {
            if (EzThrottler.Throttle("AutoFateSync.Throttle")) Chat.Instance.SendMessage("/levelsync on");
        }
    }
}
