using ECommons.DalamudServices;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Generic;
public class ChangeCommand : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => new();
    const string NewCommand = "/splatoon2";
    public override void OnSetup()
    {
        if (Svc.Commands.Commands.TryGetValue("/splatoon", out var command))
        {
            if(Svc.Commands.Commands.ContainsKey(NewCommand))
            {
                Svc.Commands.RemoveHandler(NewCommand);
            }
            Svc.Commands.AddHandler(NewCommand, command);
            Svc.Commands.RemoveHandler("/splatoon");
        }
    }
}
