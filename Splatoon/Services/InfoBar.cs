using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using ECommons.Events;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using Splatoon.Gui.Priority;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Services;
public class InfoBar : IDisposable
{
    public static readonly string EntryName = "SplatoonPriority";
    private int CurrentRole = -1;
    private IDtrBarEntry Entry;
    private InfoBar()
    {
        Entry = Svc.DtrBar.Get(EntryName, "");
        Entry.OnClick = () => P.PriorityPopupWindow.Open(true);
        Entry.Tooltip = "Edit Splatoon priority";
        ProperOnLogin.RegisterAvailable(() => Update(true), true);
    }

    public void Dispose()
    {
        Entry.Remove();
    }

    public void Update(bool force)
    {
        try
        {
            Entry.Shown = ScriptingProcessor.AnyScriptUsesPriority();
            var newRole = -1;
            if(P.PriorityPopupWindow?.Assignments != null)
            {
                for(var i = 0; i < P.PriorityPopupWindow.Assignments.Count; i++)
                {
                    var ass = P.PriorityPopupWindow.Assignments[i];
                    if(ass.IsInParty(false, out var m) && m.NameWithWorld == Player.NameWithWorld)
                    {
                        newRole = i;
                        break;
                    }
                }

            }
            if(force || newRole != CurrentRole)
            {
                PluginLog.Debug($"Role change: {CurrentRole}");
                CurrentRole = newRole;
                UpdateText();
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public void UpdateText()
    {
        uint col = 0;
        if(CurrentRole >= 0 && CurrentRole <= 1)
        {
            col = 37;
        }
        else if(CurrentRole >= 2 && CurrentRole <= 3)
        {
            col = 45;
        }
        else if(CurrentRole >= 4 && CurrentRole <= 7)
        {
            col = 17;
        }
        if(col == 0)
        {
            Entry.Text = $"";
        }
        else
        {
            Entry.Text = new SeStringBuilder().AddUiGlow(PriorityPopupWindow.ConfiguredNames[PriorityPopupWindow.RolePositions[CurrentRole]].FancySymbols(), (ushort)col).Build();
        }
    }
}
