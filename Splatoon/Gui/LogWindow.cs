using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Gui;
public class LogWindow : Window
{
    public LogWindow() : base("Splatoon Log")
    {
    }

    public override void Draw()
    {
        InternalLog.PrintImgui();
    }
}
