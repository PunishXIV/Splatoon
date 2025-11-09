using ECommons.DalamudServices;
using ECommons.Events;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe sealed class CommandOnLogin : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1);
    public override HashSet<uint>? ValidTerritories { get; } = null;

    public override void OnEnable()
    {
        ProperOnLogin.RegisterInteractable(Action);
    }

    static void Action()
    {
        Svc.Commands.ProcessCommand("/li workshop");
    }

    public override void OnDisable()
    {
        ProperOnLogin.Unregister(Action);
    }
}