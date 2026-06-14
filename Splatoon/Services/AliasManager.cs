using System;
using System.Collections.Generic;
using System.Text;

namespace Splatoon.Services;

public class AliasManager : IDisposable
{
    string CurrentAlias = null;
    private AliasManager()
    {
        SetAlias();
    }

    public void SetAlias()
    {
        if(!CurrentAlias.IsNullOrEmpty())
        {
            Svc.Commands.RemoveHandler(CurrentAlias);
            CurrentAlias = null;
        }
        if(!P.Config.Alias.Trim().IsNullOrEmpty())
        {
            if(P.Config.Alias.Trim().StartsWith('/'))
            {
                CurrentAlias = P.Config.Alias;
                Svc.Commands.AddHandler(P.Config.Alias.Trim(), new(P.CommandManager.OnCommand) { HelpMessage = "User-defined alias of /splatoon command" });
            }
            else
            {
                Notify.Error("Alias should start with a slash /");
            }
        }
    }

    public void Dispose()
    {
        if(CurrentAlias != null)
        {
            Svc.Commands.RemoveHandler(CurrentAlias);
        }
    }
}
