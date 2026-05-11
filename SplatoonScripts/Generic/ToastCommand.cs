using ECommons.DalamudServices;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace SplatoonScriptsOfficial.Generic;

public class ToastCommand : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;
    public override void OnEnable()
    {
        Svc.Commands.AddHandler("/showtoast", new(OnCommand));
    }
    public override void OnDisable()
    {
        Svc.Commands.RemoveHandler("/showtoast");
    }

    private void OnCommand(string command, string arguments)
    {
        Svc.Toasts.ShowQuest(arguments);
    }
}
