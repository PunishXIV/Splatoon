﻿using Dalamud.Game.Command;
using ECommons;
using ECommons.GameFunctions;
using Splatoon.Memory;
using Splatoon.Structures;

namespace Splatoon.Modules;

internal class Commands : IDisposable
{
    private Splatoon p;
    internal unsafe Commands(Splatoon P)
    {
        p = P;
        Svc.Commands.AddHandler("/splatoon", new CommandInfo(delegate (string command, string arguments)
        {
            if(arguments == "")
            {
                P.ConfigGui.Open = !P.ConfigGui.Open;
            }
            else if(arguments.EqualsIgnoreCaseAny("p", "priority", "prio"))
            {
                P.PriorityPopupWindow.Open(true);
            }
            else if(GenericHelpers.EqualsIgnoreCase(arguments, "log"))
            {
                P.LogWindow.IsOpen = true;
            }
            else if(arguments == "r" || arguments == "reset")
            {
                var phase = Splatoon.P.Phase;
                Splatoon.P.TerritoryChangedEvent(0);
                foreach(var x in P.Config.LayoutsL)
                {
                    x.FreezeInfo = new();
                }
                Notify.Success("Reset");
                if(Splatoon.P.Phase != phase)
                {
                    Splatoon.P.Phase = phase;
                    Notify.Info($"Returned to phase {phase}");
                }
                AttachedInfo.CastInfos.Clear();
                AttachedInfo.VFXInfos.Clear();
                AttachedInfo.TetherInfos.Clear();
            }
            else if(arguments.StartsWith("enable "))
            {
                try
                {
                    var name = arguments.Substring(arguments.IndexOf("enable ") + 7);
                    SwitchState(name, true);
                }
                catch(Exception e)
                {
                    P.Log(e.ToStringFull());
                }
            }
            else if(arguments.StartsWith("disable "))
            {
                try
                {
                    var name = arguments.Substring(arguments.IndexOf("disable ") + 8);
                    SwitchState(name, false);
                }
                catch(Exception e)
                {
                    P.Log(e.ToStringFull());
                }
            }
            else if(arguments.StartsWith("toggle "))
            {
                try
                {
                    var name = arguments.Substring(arguments.IndexOf("toggle ") + 7);
                    SwitchState(name, null);
                }
                catch(Exception e)
                {
                    P.Log(e.ToStringFull());
                }
            }
            else if(arguments.StartsWith("settarget "))
            {
                try
                {
                    if(Svc.Targets.Target == null)
                    {
                        Notify.Error("Target not selected");
                    }
                    else
                    {
                        var name = arguments.Substring(arguments.IndexOf("settarget ") + 10).Split('~');
                        var el = P.Config.LayoutsL.First(x => x.Name == name[0]).ElementsL.First(x => x.Name == name[1]);
                        el.refActorNameIntl.CurrentLangString = Svc.Targets.Target.Name.ToString();
                        el.refActorDataID = Svc.Targets.Target.DataId;
                        el.refActorObjectID = Svc.Targets.Target.EntityId;
                        if(Svc.Targets.Target is ICharacter c) el.refActorModelID = (uint)c.Struct()->ModelContainer.ModelCharaId;
                        Notify.Success("Successfully set target");
                    }
                }
                catch(Exception e)
                {
                    P.Log(e.ToStringFull());
                }
            }
            else if(arguments.StartsWith("floodchat "))
            {
                Safe(delegate
                {
                    for(var i = 0; i < uint.Parse(arguments.Replace("floodchat ", "")); i++)
                    {
                        Svc.Chat.Print(new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 30).Select(s => s[new Random().Next(30)]).ToArray()));
                    }
                });
            }
        })
        {
            HelpMessage = """
            open Splatoon configuration menu 
            /splatoon toggle <PresetName> → toggle specified preset 
            /splatoon disable <PresetName> → disable specified preset 
            /splatoon enable <PresetName> → enable specified preset
            /splatoon p → open script priority editor
            """
        });

        Svc.Commands.AddHandler("/sf", new CommandInfo(delegate (string command, string args)
        {
            if(args == "")
            {
                if(P.SFind.Count > 0)
                {
                    Notify.Info("Search stopped");
                    P.SFind.Clear();
                }
                else
                {
                    Notify.Error("Please specify target name");
                }
            }
            else
            {
                if(args.StartsWith("("))
                {
                    var split = args.Replace("(", "").Replace(")", "").Split(",", StringSplitOptions.TrimEntries);
                    if(split.Length == 3 && float.TryParse(split[0], out var x) && float.TryParse(split[1], out var y) && float.TryParse(split[2], out var z))
                    {
                        P.SFind.Clear();
                        var e = new SearchInfo()
                        {
                            Coords = new(x, y, z)
                        };
                        P.SFind.Add(e);
                    }
                }
                else
                {
                    if(args.StartsWith("+"))
                    {
                        args = args[1..];
                    }
                    else
                    {
                        P.SFind.Clear();
                    }
                    var list = args.Split(",");
                    foreach(var arguments in list)
                    {
                        var e = new SearchInfo()
                        {
                            Name = arguments.Trim(),
                            IncludeUntargetable = arguments.StartsWith("!!")
                        };
                        P.SFind.Add(e);
                        if(e.IncludeUntargetable)
                        {
                            e.Name = arguments[2..];
                        }
                        Notify.Success("Searching for: " + e.Name + (e.IncludeUntargetable ? " (+untargetable)" : ""));
                    }
                }
            }
        })
        {
            HelpMessage = "highlight objects containing specified phrase"
        });
    }

    internal void SwitchState(string name, bool? enable, bool web = false)
    {
        try
        {
            if(name.Contains("~"))
            {
                var aname = name.Split('~');
                foreach(var x in P.Config.LayoutsL.Where(x => x.Name == aname[0]))
                {
                    if(web && x.DisableDisabling) continue;
                    foreach(var z in x.ElementsL.Where(z => z.Name == aname[1]))
                    {
                        z.Enabled = enable ?? !z.Enabled;
                    }
                }
            }
            else
            {
                foreach(var x in P.Config.LayoutsL.Where(x => x.Name == name))
                {
                    if(web && x.DisableDisabling) continue;
                    x.Enabled = enable ?? !x.Enabled;
                }
            }
        }
        catch(Exception e)
        {
            p.Log(e.Message, true);
            p.Log(e.ToStringFull());
        }
    }

    public void Dispose()
    {
        Svc.Commands.RemoveHandler("/splatoon");
        Svc.Commands.RemoveHandler("/sf");
    }
}
