using Dalamud.Game.Gui.Dtr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Services;
public class InfoBar : IDisposable
{
    private InfoBar()
    {
        IDtrBarEntry entry = Svc.DtrBar.Get("SplatoonPriority", "");
    }

    public void Dispose()
    {
        
    }

    public void Update()
    {
        
    }
}
