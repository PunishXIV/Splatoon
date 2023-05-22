using Dalamud.Game.Command;
using ECommons.GameFunctions;
using Splatoon.Structures;

namespace Splatoon.Modules;

class Commands : IDisposable
{
    Splatoon p;
    internal unsafe Commands(Splatoon p)
    {
        this.p = p;
        Svc.Commands.AddHandler("/splatoon", new CommandInfo(delegate (string command, string arguments)
        {
            if (arguments == "")
            {
                p.ConfigGui.Open = true;
            }
            else if(arguments == "r" || arguments == "reset")
            {
                var phase = P.Phase;
                P.TerritoryChangedEvent(null, 0);
                Notify.Success("Reset");
                if (P.Phase != phase)
                {
                    P.Phase = phase;
                    Notify.Info($"Returned to phase {phase}");
                }
            }
            else if (arguments.StartsWith("enable "))
            {
                try
                {
                    var name = arguments.Substring(arguments.IndexOf("enable ") + 7);
                    SwitchState(name, true);
                }
                catch (Exception e)
                {
                    p.Log(e.Message);
                }
            }
            else if (arguments.StartsWith("disable "))
            {
                try
                {
                    var name = arguments.Substring(arguments.IndexOf("disable ") + 8);
                    SwitchState(name, false);
                }
                catch (Exception e)
                {
                    p.Log(e.Message);
                }
            }
            else if (arguments.StartsWith("toggle "))
            {
                try
                {
                    var name = arguments.Substring(arguments.IndexOf("toggle ") + 7);
                    SwitchState(name, null);
                }
                catch (Exception e)
                {
                    p.Log(e.Message);
                }
            }
            else if (arguments.StartsWith("settarget "))
            {
                try
                {
                    if (Svc.Targets.Target == null)
                    {
                        Notify.Error("Target not selected");
                    }
                    else
                    {
                        var name = arguments.Substring(arguments.IndexOf("settarget ") + 10).Split('~');
                        var el = p.Config.LayoutsL.First(x => x.Name == name[0]).ElementsL.First(x => x.Name == name[1]);
                        el.refActorNameIntl.CurrentLangString = Svc.Targets.Target.Name.ToString();
                        el.refActorDataID = Svc.Targets.Target.DataId;
                        el.refActorObjectID = Svc.Targets.Target.ObjectId;
                        if (Svc.Targets.Target is Character c) el.refActorModelID = (uint)c.Struct()->ModelCharaId;
                        Notify.Success("Successfully set target");
                    }
                }
                catch (Exception e)
                {
                    p.Log(e.Message);
                }
            }
            else if (arguments.StartsWith("floodchat "))
            {
                Safe(delegate
                {
                    for (var i = 0; i < uint.Parse(arguments.Replace("floodchat ", "")); i++)
                    {
                        Svc.Chat.Print(new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 30).Select(s => s[new Random().Next(30)]).ToArray()));
                    }
                });
            }
        })
        {
            HelpMessage = "open Splatoon configuration menu \n" +
            "/splatoon toggle <PresetName> → toggle specified preset \n" +
            "/splatoon disable <PresetName> → disable specified preset \n" +
            "/splatoon enable <PresetName> → enable specified preset"
        });

        Svc.Commands.AddHandler("/sf", new CommandInfo(delegate (string command, string args)
        {
            if (args == "")
            {
                if (p.SFind.Count > 0)
                {
                    Notify.Info("Search stopped");
                    p.SFind.Clear();
                }
                else
                {
                    Notify.Error("Please specify target name");
                }
            }
            else
            {
                if (args.StartsWith("+"))
                {
                    args = args[1..];
                }
                else
                {
                    p.SFind.Clear();
                }
                var list = args.Split(",");
                foreach (var arguments in list)
                {
                    var e = new SearchInfo()
                    {
                        name = arguments.Trim(),
                        includeUntargetable = arguments.StartsWith("!!")
                    };
                    p.SFind.Add(e);
                    if (e.includeUntargetable)
                    {
                        e.name = arguments[2..];
                    }
                    Notify.Success("Searching for: " + e.name + (e.includeUntargetable ? " (+untargetable)" : ""));
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
            if (name.Contains("~"))
            {
                var aname = name.Split('~');
                foreach (var x in P.Config.LayoutsL.Where(x => x.Name == aname[0]))
                {
                    if (web && x.DisableDisabling) continue;
                    foreach (var z in x.ElementsL.Where(z => z.Name == aname[1]))
                    {
                        z.Enabled = enable ?? !z.Enabled;
                    }
                }
            }
            else
            {
                foreach (var x in P.Config.LayoutsL.Where(x => x.Name == name))
                {
                    if (web && x.DisableDisabling) continue;
                    x.Enabled = enable ?? !x.Enabled;
                }
            }
        }
        catch (Exception e)
        {
            p.Log(e.Message, true);
            p.Log(e.StackTrace);
        }
    }

    public void Dispose()
    {
        Svc.Commands.RemoveHandler("/splatoon");
        Svc.Commands.RemoveHandler("/sf");
    }
}
