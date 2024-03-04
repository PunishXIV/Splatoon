using Dalamud.Game;
using Dalamud.Interface.Colors;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using ECommons.LanguageHelpers;
using ECommons.Reflection;
using System.Net.Http;
using System.Threading;

namespace Splatoon.Modules;

internal class Loader
{
    const string url = "";
    Splatoon p;
    HttpClient client;
    internal volatile Verdict verdict = Verdict.Unknown;
    internal volatile Version maxVersion = new("0.0.0.0");
    internal string gVersion;
    internal Version splatoonVersion;
    string file;

    internal Loader(Splatoon p)
    {
        PluginLog.Information("Splatoon loader started");
        client = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        this.p = p;
        Svc.Commands.AddHandler("/loadsplatoon", new(delegate { Load(Svc.Framework); }) { HelpMessage = "Manually load Splatoon" });
        splatoonVersion = p.GetType().Assembly.GetName().Version;
        file = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "safeVersion.nfo");
        if (DalamudReflector.TryGetDalamudStartInfo(out var startInfo, Svc.PluginInterface))
        {
            gVersion = startInfo.GameVersion.ToString();
            PluginLog.Information($"Game version: {gVersion}, Splatoon version: {splatoonVersion}");
            new Thread(() =>
            {
                PluginLog.Information("Splatoon loader thread started");
                //Thread.Sleep(5000);
                try
                {
                    if (File.Exists(file))
                    {
                        if (File.ReadAllText(file) == gVersion)
                        {
                            PluginLog.Information("Loading is allowed via file...");
                            verdict = Verdict.Confirmed;
                        }
                    }
                    if (verdict != Verdict.Confirmed)
                    {
                        PluginLog.Debug("Obtaining version list");
                        var res = client.GetAsync("https://raw.githubusercontent.com/PunishXIV/Splatoon/main/versions.txt").Result;
                        res.EnsureSuccessStatusCode();
                        foreach (var x in res.Content.ReadAsStringAsync().Result.Split("\n"))
                        {
                            PluginLog.Debug(x);
                            var s = x.Split(":");
                            if (s.Length != 2) continue;
                            var ver = Version.Parse(s[1]);
                            if (ver > maxVersion) maxVersion = ver;
                            if (s[0] == gVersion && splatoonVersion >= ver)
                            {
                                verdict = Verdict.Confirmed;
                                break;
                            }
                        }
                    }
                    try
                    {
                        PluginLog.Debug("Obtaining revocation list");
                        var res = client.GetAsync("https://raw.githubusercontent.com/PunishXIV/Splatoon/main/revocationList.txt").Result;
                        res.EnsureSuccessStatusCode();
                        foreach (var x in res.Content.ReadAsStringAsync().Result.Split("\n"))
                        {
                            PluginLog.Debug($"Revoked version: {x}");
                            if (!x.IsNullOrWhitespace() && Version.TryParse(x, out var ver) && ver == splatoonVersion)
                            {
                                PluginLog.Warning($"Detected revoked version");
                                verdict = Verdict.Revoked;
                                break;
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        PluginLog.Error($"{ex.Message}\n{ex.StackTrace}");
                    }
                }
                catch (Exception e)
                {
                    PluginLog.Error($"{e.Message}\n{e.StackTrace}");
                    verdict = Verdict.Error;
                }
                Safe(delegate
                {
                    if (p.Disposed)
                    {
                        PluginLog.Fatal("Splatoon has been disposed, loading is impossible");
                        return;
                    }
                    if (verdict == Verdict.Confirmed)
                    {
                        PluginLog.Information("Splatoon loading allowed, continuing");
                        Svc.Framework.Update += Load;
                        File.WriteAllText(file, gVersion);
                    }
                    else
                    {
                        PluginLog.Warning("Splatoon loading disallowed. Displaying confirmation window.");
                        Svc.PluginInterface.UiBuilder.Draw += Draw;
                    }
                });
            }).Start();
        }
        else
        {
            PluginLog.Error("Could not get Dalamud start info");
        }
    }

    void Load(IFramework fr)
    {
        PluginLog.Information("Splatoon begins loading process");
        Svc.Framework.Update -= Load;
        p.Load(Svc.PluginInterface);
        PluginLog.Information("Splatoon has been loaded");
    }

    Vector2 size = Vector2.Zero;
    internal void Draw()
    {
        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(ImGuiHelpers.MainViewport.Size / 2 - size / 2);
        if (ImGui.Begin("Splatoon - Can not confirm compatibility with current game version".Loc(), ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
        {
            if (verdict == Verdict.Revoked)
            {
                ImGuiEx.Text(GradientColor.Get(ImGuiColors.DalamudRed, ImGuiColors.DalamudYellow, 500), "This version of Splatoon plugin is marked as revoked.".Loc());
                ImGuiEx.Text($"Loading and using this version may lead to game crashes, \ndata corruption or other unpredictable consequences.".Loc());
                ImGuiEx.Text(ImGuiColors.DalamudViolet, $"Usually there is already updated version that fixes the problem,\nor it will be released very soon.".Loc());
                if (ImGui.Button("Open plugin installer".Loc()))
                {
                    Svc.Commands.ProcessCommand("/xlplugins");
                }
            }
            else
            {
                if (verdict == Verdict.Error)
                {
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "Splatoon could not connect to the GitHub to verify\nif it could be running on current game version.".Loc());
                    ImGuiEx.Text("If the game has just updated, please test if the plugin works fine and if it does,\npress \"Load Splatoon and never display this window until next game update\" \nbutton in this window next time you start the game.".Loc());
                }
                else
                {
                    ImGuiEx.Text(ImGuiColors.DalamudOrange, "There is no information about compatibility of current version of\nSplatoon with current version of the game.".Loc());
                    ImGuiEx.Text("You may try to load plugin and continue using it. \nOn a smaller patches it will usually work, but it may crash your game\nin which case please wait for an update.".Loc());
                }
                if (maxVersion > splatoonVersion)
                {
                    ImGuiEx.Text(ImGuiColors.DalamudViolet, "An update for Splatoon is available. \nPlease open plugin installer and update Splatoon plugin.".Loc());
                    if (ImGui.Button("Open plugin installer".Loc()))
                    {
                        Svc.Commands.ProcessCommand("/xlplugins");
                    }
                }
            }
            if (ImGui.Button("Load Splatoon anyway".Loc()))
            {
                Svc.PluginInterface.UiBuilder.Draw -= Draw;
                PluginLog.Warning("Received confirmation to load Splatoon with unverified game version");
                Svc.Framework.Update += Load;
            }
            if (ImGui.Button("Close this window".Loc()))
            {
                Svc.PluginInterface.UiBuilder.Draw -= Draw;
            }
            if (verdict != Verdict.Revoked && ImGui.Button("Load Splatoon and never display this window until next game update".Loc()))
            {
                Svc.PluginInterface.UiBuilder.Draw -= Draw;
                PluginLog.Warning("Received confirmation to load Splatoon with unverified game version and override game version");
                Svc.Framework.Update += Load;
                Safe(delegate
                {
                    File.WriteAllText(file, gVersion);
                });
            }
            if (ImGui.Button($"Join discord to be notified for update immediately."))
            {
                ShellStart("https://discord.gg/m8NRt4X8Gf");
            }
        }
        size = ImGui.GetWindowSize();
        ImGui.End();
    }

    internal enum Verdict
    {
        Unknown, Error, Outdated, Confirmed, Revoked
    }
}
