using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.LanguageHelpers;
using Splatoon.Gui.Scripting;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;

namespace Splatoon.SplatoonScripting;

internal static partial class ScriptingProcessor
{
    internal static ImmutableList<SplatoonScript> Scripts = ImmutableList<SplatoonScript>.Empty;
    internal static ConcurrentQueue<(string code, string path)> LoadScriptQueue = new();
    internal static volatile bool ThreadIsRunning = false;
    internal readonly static string[] TrustedURLs = new string[]
    {
        "https://github.com/NightmareXIV/",
        "https://www.github.com/NightmareXIV/",
        "https://raw.githubusercontent.com/NightmareXIV/",
        "https://github.com/PunishXIV/",
        "https://www.github.com/PunishXIV/",
        "https://raw.githubusercontent.com/PunishXIV/",
        "https://nightmarexiv.com/"
    };
    internal static ImmutableList<BlacklistData> Blacklist = ImmutableList<BlacklistData>.Empty;
    internal static volatile bool UpdateCompleted = false;
    internal static List<string> ForceUpdate = [];

    internal static string ExtractNamespaceFromCode(string code)
    {
        var regex = NamespaceRegex();
        var matches = regex.Match(code);
        if (matches.Success && matches.Groups.Count > 1)
        {
            return matches.Groups[1].Value;
        }
        return null;
    }

    internal static string ExtractClassFromCode(string code)
    {
        var regex = ClassRegex();
        var matches = regex.Match(code);
        if (matches.Success && matches.Groups.Count > 1)
        {
            return matches.Groups[1].Value;
        }
        return null;
    }

    internal static void BlockingBeginUpdate(bool force = false)
    {
        if (UpdateCompleted || force)
        {
            Blacklist = ImmutableList<BlacklistData>.Empty;

            try
            {
                PluginLog.Debug($"Starting downloading blacklist...");
                var result = P.HttpClient.GetAsync("https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/blacklist.csv").Result;
                result.EnsureSuccessStatusCode();
                PluginLog.Debug($"Blacklist download complete");
                var blacklist = result.Content.ReadAsStringAsync().Result;

                foreach (var line in blacklist.Replace("\r", "").Split("\n"))
                {
                    var data = line.Split(",");
                    if (data.Length == 2 && int.TryParse(data[1], out var ver))
                    {
                        Blacklist = Blacklist.Add(new(data[0], ver));
                        PluginLog.Debug($"Found new valid blacklist data: {data[0]} v{ver}");
                    }
                    else
                    {
                        PluginLog.Debug($"Found invalid blacklist data: {line}");
                    }
                }
            }
            catch (Exception e)
            {
                e.Log();
            }

            Svc.Framework.RunOnFrameworkThread(delegate
            {
                PluginLog.Information($"Blacklist: {Blacklist.Select(x => $"{x.FullName} v{x.Version}").Print()}");
                foreach (var x in Scripts)
                {
                    x.InternalData.Allowed = true;
                    if (Blacklist.Any(z => z.FullName == x.InternalData.FullName && z.Version >= (x.Metadata?.Version ?? 0)))
                    {
                        PluginLog.Information($"Script {x.InternalData.FullName} is blacklisted and will not be enabled");
                        x.InternalData.Blacklisted = true;
                    }
                    x.UpdateState();
                    PluginLog.Debug($"Processed script {x.InternalData.FullName}");
                }
            }).Wait();
            UpdateCompleted = true;

            try
            {
                PluginLog.Debug($"Starting downloading update list...");
                var result = P.HttpClient.GetAsync("https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/update.csv").Result;
                result.EnsureSuccessStatusCode();
                PluginLog.Debug($"Update list downloaded");
                var updateList = result.Content.ReadAsStringAsync().Result;

                List<string> Updates = new();
                foreach (var line in updateList.Replace("\r", "").Split("\n"))
                {
                    var data = line.Split(",");
                    if (data.Length >= 3 && int.TryParse(data[1], out var ver))
                    {
                        PluginLog.Debug($"Found new valid update data: {data[0]} v{ver} = {data[2]}");
                        if ((ForceUpdate != null && ForceUpdate.Contains(data[0])) || Scripts.Any(x => x.InternalData.FullName == data[0] && ((x.Metadata?.Version ?? 0) < ver || TabScripting.ForceUpdate))) // possible CME
                        {
                            PluginLog.Debug($"Adding  {data[2]} to download list");
                            Updates.Add(new(data[2]));
                        }
                    }
                    else
                    {
                        PluginLog.Debug($"Found invalid update data: {line}");
                    }
                }
                ForceUpdate = null;
                foreach (var x in Updates)
                {
                    PluginLog.Information($"Downloading script from {x}");
                    BlockingDownloadScript(x);
                }
            }
            catch (Exception e)
            {
                e.Log();
            }
        }
        else
        {
            PluginLog.Error("Can not start new update before previous has finished");
        }
    }

    internal static bool IsUrlTrusted(string url)
    {
        return url.StartsWithAny(TrustedURLs, StringComparison.OrdinalIgnoreCase);
    }

    internal static void DownloadScript(string url)
    {
        Task.Run(delegate
        {
            BlockingDownloadScript(url);
        });

        Notify.Info("Downloading script from trusted URL...".Loc());
    }

    static void BlockingDownloadScript(string url)
    {
        try
        {
            var result = P.HttpClient.GetStringAsync(url).Result;
            CompileAndLoad(result, null);
        }
        catch (Exception e)
        {
            e.Log();
        }
    }

    internal static void ReloadAll()
    {
        if (ThreadIsRunning)
        {
            DuoLog.Error("Can not reload yet, please wait");
            return;
        }
        UpdateCompleted = false;
        Scripts.ForEach(x => x.Disable());
        Scripts = ImmutableList<SplatoonScript>.Empty;
        var dir = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "Scripts");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        foreach (var f in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
        {
            CompileAndLoad(File.ReadAllText(f, Encoding.UTF8), f);
        }
    }

    internal static void ReloadScript(SplatoonScript s)
    {
        if (ThreadIsRunning)
        {
            DuoLog.Error("Can not reload yet, please wait");
            return;
        }
        s.Disable();
        Scripts = Scripts.Remove(s);
        CompileAndLoad(File.ReadAllText(s.InternalData.Path, Encoding.UTF8), s.InternalData.Path);
    }

    internal static void CompileAndLoad(string sourceCode, string fpath)
    {
        PluginLog.Debug($"Requested script loading");
        LoadScriptQueue.Enqueue((sourceCode, fpath));
        if (!ThreadIsRunning)
        {
            ThreadIsRunning = true;
            PluginLog.Debug($"Beginning new thread");
            new Thread(() =>
            {
                try
                {
                    PluginLog.Debug($"Compiler thread started");
                    int idleCount = 0;
                    var dir = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "ScriptCache");
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    while (idleCount < 10)
                    {
                        if (LoadScriptQueue.TryDequeue(out var result))
                        {
                            try
                            {
                                byte[] code = null;
                                if (!P.Config.DisableScriptCache)
                                {
                                    var md5 = MD5.HashData(Encoding.UTF8.GetBytes(result.code)).Select(x => $"{x:X2}").Join("");
                                    var cacheFile = Path.Combine(dir, $"{md5}-{P.loader.splatoonVersion}.bin");
                                    PluginLog.Debug($"Cache path: {cacheFile}");
                                    if (File.Exists(cacheFile))
                                    {
                                        PluginLog.Debug($"Loading from cache...");
                                        code = File.ReadAllBytes(cacheFile);
                                    }
                                    else
                                    {
                                        PluginLog.Debug($"Compiling...");
                                        code = Compiler.Compile(result.code, result.path == null ? "" : Path.GetFileNameWithoutExtension(result.path));
                                        if (code != null)
                                        {
                                            File.WriteAllBytes(cacheFile, code);
                                            PluginLog.Debug($"Compiled and saved");
                                        }
                                    }
                                }
                                else
                                {
                                    PluginLog.Debug($"Compiling, cache bypassed...");
                                    code = Compiler.Compile(result.code, result.path == null ? "" : Path.GetFileNameWithoutExtension(result.path));
                                }
                                if (code != null)
                                {
                                    Svc.Framework.RunOnFrameworkThread(delegate
                                    {
                                        if (P != null && !P.Disposed)
                                        {
                                            var assembly = Compiler.Load(code);
                                            foreach (var t in assembly.GetTypes())
                                            {
                                                if (t.BaseType?.FullName == "Splatoon.SplatoonScripting.SplatoonScript")
                                                {
                                                    var instance = (SplatoonScript)assembly.CreateInstance(t.FullName);
                                                    instance.InternalData = new(result.path, instance);
                                                    instance.InternalData.Allowed = UpdateCompleted;
                                                    bool rewrite = false;
                                                    if (Scripts.TryGetFirst(z => z.InternalData.FullName == instance.InternalData.FullName, out var loadedScript))
                                                    {
                                                        DuoLog.Information($"Script {instance.InternalData.FullName} already loaded, replacing.");
                                                        result.path = loadedScript.InternalData.Path;
                                                        loadedScript.Disable();
                                                        Scripts = Scripts.RemoveAll(x => ReferenceEquals(loadedScript, x));
                                                        rewrite = true;
                                                    }
                                                    Scripts = Scripts.Add(instance);
                                                    if (result.path == null)
                                                    {
                                                        var dir = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "Scripts", instance.InternalData.Namespace);
                                                        if (!Directory.Exists(dir))
                                                        {
                                                            Directory.CreateDirectory(dir);
                                                        }
                                                        var newPath = Path.Combine(dir, $"{instance.InternalData.Name}.cs");
                                                        instance.InternalData.Path = newPath;
                                                        File.WriteAllText(newPath, result.code, Encoding.UTF8);
                                                        DuoLog.Debug($"Script installed to {newPath}");
                                                    }
                                                    else if (rewrite)
                                                    {
                                                        //DeleteFileToRecycleBin(result.path);
                                                        File.WriteAllText(result.path, result.code, Encoding.UTF8);
                                                        instance.InternalData.Path = result.path;
                                                        DuoLog.Debug($"Script overwritten at {instance.InternalData.Path}");
                                                    }
                                                    instance.OnSetup();
                                                    instance.Controller.ApplyOverrides();
                                                    PluginLog.Debug($"Load success");
                                                    instance.UpdateState();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            PluginLog.Fatal("Plugin was disposed during script loading");
                                        }
                                    }).Wait();
                                }
                                else
                                {
                                    PluginLog.Error("Loading process ended with error");
                                }
                            }
                            catch (Exception e)
                            {
                                e.Log();
                            }
                            idleCount = 0;
                        }
                        else
                        {
                            //PluginLog.Verbose($"Script loading thread is idling, count {idleCount}");
                            idleCount++;
                            Thread.Sleep(250);
                        }
                    }
                }
                catch (Exception e)
                {
                    e.Log();
                }
                ThreadIsRunning = false;
                PluginLog.Debug($"Compiler part of thread is finished");

                if (!UpdateCompleted)
                {
                    PluginLog.Debug($"Starting updating...");
                    try
                    {
                        BlockingBeginUpdate(true);
                    }
                    catch (Exception e)
                    {
                        e.Log();
                    }
                    PluginLog.Debug($"Update finished");
                }

            }).Start();
        }
    }

    internal static void OnUpdate()
    {
        var tickCount = Environment.TickCount64;
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                var script = Scripts[i];
                try
                {
                    script.OnUpdate();
                }
                catch (Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnUpdate)); }
                if(tickCount > script.Controller.AutoResetAt)
                {
                    PluginLog.Debug($"Resetting script {script.InternalData.Name} because of timer");
                    OnReset(script);
                }
            }
        }
    }

    internal static void OnCombatStart()
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                OnReset(i);
                try
                {
                    Scripts[i].OnCombatStart();
                }
                catch(Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnCombatStart)); }
            }
        }
    }

    internal static void OnCombatEnd()
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                OnReset(i);
                try
                {
                    Scripts[i].OnCombatEnd();
                }
                catch (Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnCombatEnd)); }
            }
        }
    }

    internal static void OnReset(int i) => OnReset(Scripts[i]);

    internal static void OnReset(SplatoonScript script)
    {
        try
        {
            PluginLog.Debug($"OnReset called for script {script.InternalData.Name}");
            script.Controller.CancelSchedulers();
            script.OnReset();
            script.Controller.AutoResetAt = long.MaxValue;
        }
        catch(Exception e) { script.LogError(e, nameof(SplatoonScript.OnReset)); }
    }

    internal static void OnMapEffect(uint Position, ushort Param1, ushort Param2)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnMapEffect(Position, Param1, Param2);
                }
                catch (Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnMapEffect)); }
            }
        }
    }

    internal static void OnObjectEffect(uint Target, ushort Param1, ushort Param2)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnObjectEffect(Target, Param1, Param2);
                }
                catch (Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnObjectEffect)); }
            }
        }
    }

    internal static void OnMessage(string Message)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnMessage(Message);
                }
                catch (Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnMessage)); }
            }
        }
    }

    internal static void OnVFXSpawn(uint target, string vfxPath)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnVFXSpawn(target, vfxPath);
                }
                catch (Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnVFXSpawn)); }
            }
        }
    }

    internal static void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnTetherCreate(source, target, data2, data3, data5);
                }
                catch (Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnTetherCreate)); }
            }
        }
    }

    internal static void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnTetherRemoval(source, data2, data3, data5);
                }
                catch (Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnTetherRemoval)); }
            }
        }
    }

    internal static void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                if(category == DirectorUpdateCategory.Commence || category == DirectorUpdateCategory.Recommence || category == DirectorUpdateCategory.Wipe)
                {
                    OnReset(i);
                }
                try
                {
                    Scripts[i].OnDirectorUpdate(category);
                }
                catch (Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnDirectorUpdate)); }
            }
        }
    }

    internal static void OnPhaseChange(int phase)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnPhaseChange(phase);
                }
                catch (Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnPhaseChange)); }
            }
        }
    }

    internal static void OnObjectCreation(nint newObjectPointer)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnObjectCreation(newObjectPointer);
                }
                catch (Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnObjectCreation)); }
            }
        }
    }

    internal static void OnActionEffect(uint ActionID, ushort animationID, ActionEffectType type, uint sourceID, ulong targetOID, uint damage)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnActionEffect(ActionID, animationID, type, sourceID, targetOID, damage);
                }
                catch (Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnActionEffect)); }
            }
        }
    }

    internal static void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, ulong targetId, byte replaying)
    {
        for(var i = 0; i < Scripts.Count; i++)
        {
            if(Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnActorControl(sourceId, command, p1, p2, p3, p4, p5, p6, targetId, replaying);
                }
                catch(Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnActorControl)); }
            }
        }
    }

    internal static void OnActionEffectEvent(ActionEffectSet set)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnActionEffectEvent(set);
                }
                catch (Exception e) { Scripts[i].LogError(e, nameof(SplatoonScript.OnActionEffectEvent)); }
            }
        }
    }

    internal static void TerritoryChanged()
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            var s = Scripts[i];
            s.UpdateState();
        }
    }

    internal static void UpdateState(this SplatoonScript s)
    {
        var territoryIsValid = s.ValidTerritories == null || (Svc.ClientState.IsLoggedIn && (s.ValidTerritories.Count == 0 || s.ValidTerritories.Contains(Svc.ClientState.TerritoryType)));
        if (territoryIsValid && !P.Config.DisabledScripts.Contains(s.InternalData.FullName))
        {
            if (!s.IsEnabled)
            {
                s.Enable();
            }
        }
        else if (s.IsEnabled)
        {
            s.Disable();
        }
    }

    internal static void Dispose()
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            Scripts[i].Disable();
        }
        Scripts = ImmutableList<SplatoonScript>.Empty;
    }

    internal static void LogError(this SplatoonScript s, Exception e, string methodName)
    {
        PluginLog.Error($"[{s?.InternalData?.Name}] Exception in script {s?.InternalData?.FullName} while executing {methodName}:\n{e}");
    }

    [GeneratedRegex("namespace[\\s]+([a-z0-9_\\.]+)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NamespaceRegex();

    [GeneratedRegex("([a-z0-9_\\.]+)\\s*:\\s*SplatoonScript", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ClassRegex();
}