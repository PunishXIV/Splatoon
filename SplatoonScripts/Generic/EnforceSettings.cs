using Dalamud.Game.Config;
using ECommons.DalamudServices;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class EnforceSettings : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [];

    public override void OnEnable()
    {
        EnforceOption(Svc.GameConfig.UiConfig, "TelepoTicketUseType", 4);
        EnforceOption(Svc.GameConfig.UiConfig, "TelepoTicketGilSetting", 999);
    }

    void EnforceOption(GameConfigSection section, string option, uint value)
    {
        if(section.GetUInt(option) != value)
        {
            section.Set(option, value);
            DuoLog.Information($"{option} set to {value}");
        }
    }
}
