using Dalamud.Game.Gui.Dtr;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public class StatusbarFpsSwitcher : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;

    IDtrBarEntry Entry;

    public override void OnEnable()
    {
        Entry = Svc.DtrBar.Get("Splatoon.FpsSwitcher", "");
        Entry.Shown = true;
        Entry.OnClick = () =>
        {
            Toggle();
        };
        Update();
    }

    void Update()
    {
        Entry = Svc.DtrBar.Get("Splatoon.FpsSwitcher", "");
        var cfg = Svc.GameConfig.System.GetUInt("Fps");
        if(cfg == 0)
        {
            Entry.Text = $"FPS: U";
        }
        else if(cfg == 1)
        {
            Entry.Text = $"FPS: D";
        }
        else if(cfg == 2)
        {
            Entry.Text = $"FPS: 60";
        }
        else if(cfg == 3)
        {
            Entry.Text = $"FPS: 30";
        }
    }

    void Toggle()
    {
        var cfg = Svc.GameConfig.System.GetUInt("Fps");
        Svc.GameConfig.System.Set("Fps", cfg == 3 ? 2u : 3u);
        Update();
    }

    public override void OnDisable()
    {
        Entry = Svc.DtrBar.Get("Splatoon.FpsSwitcher", "");
        Entry.Remove();
    }
}
