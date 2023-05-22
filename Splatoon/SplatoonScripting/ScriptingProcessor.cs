using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.LanguageHelpers;
using Splatoon.Gui.Scripting;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Threading;

namespace Splatoon.SplatoonScripting;

internal static class ScriptingProcessor
{
    internal static ImmutableList<SplatoonScript> Scripts = ImmutableList<SplatoonScript>.Empty;
    internal static ConcurrentQueue<(string code, string path)> LoadScriptQueue = new();
    internal static volatile bool ThreadIsRunning = false;
    internal readonly static string[] TrustedURLs = new string[]
    {
        "https://github.com/NightmareXIV/",
        "https://www.github.com/NightmareXIV/",
        "https://raw.githubusercontent.com/NightmareXIV/"
    };
    internal static ImmutableList<BlacklistData> Blacklist = ImmutableList<BlacklistData>.Empty;
    internal static volatile bool UpdateCompleted = false;

    internal static void BlockingBeginUpdate(bool force = false)
    {
        if (UpdateCompleted || force)
        {
            Blacklist = ImmutableList<BlacklistData>.Empty;

            try
            {
                PluginLog.Debug($"Starting downloading blacklist...");
                var result = P.HttpClient.GetAsync("https://github.com/NightmareXIV/Splatoon/raw/master/SplatoonScripts/blacklist.csv").Result;
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
            catch(Exception e)
            {
                e.Log();
            }

            Svc.Framework.RunOnFrameworkThread(delegate
            {
                PluginLog.Information($"Blacklist: {Blacklist.Select(x => $"{x.FullName} v{x.Version}").Print()}");
                foreach(var x in ScriptingProcessor.Scripts)
                {
                    x.InternalData.Allowed = true;
                    if (ScriptingProcessor.Blacklist.Any(z => z.FullName == x.InternalData.FullName && z.Version >= (x.Metadata?.Version ?? 0) ))
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
                var result = P.HttpClient.GetAsync("https://github.com/NightmareXIV/Splatoon/raw/master/SplatoonScripts/update.csv").Result;
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
                        if(Scripts.Any(x => x.InternalData.FullName == data[0] && ((x.Metadata?.Version ?? 0) < ver || TabScripting.ForceUpdate) )) // possible CME
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
                foreach (var x in Updates)
                {
                    PluginLog.Information($"Downloading script from {x}");
                    BlockingDownloadScript(x);
                }
            }
            catch(Exception e)
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
        return url.StartsWithAny(ScriptingProcessor.TrustedURLs, StringComparison.OrdinalIgnoreCase);
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
            ScriptingProcessor.CompileAndLoad(result, null);
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
        foreach(var f in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
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
        PluginLog.Information($"Requested script loading");
        LoadScriptQueue.Enqueue((sourceCode, fpath));
        if (!ThreadIsRunning)
        {
            ThreadIsRunning = true;
            PluginLog.Information($"Beginning new thread");
            new Thread(() =>
            {
                try
                {
                    PluginLog.Information($"Compiler thread started");
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
                                    PluginLog.Information($"Cache path: {cacheFile}");
                                    if (File.Exists(cacheFile))
                                    {
                                        PluginLog.Information($"Loading from cache...");
                                        code = File.ReadAllBytes(cacheFile);
                                    }
                                    else
                                    {
                                        PluginLog.Information($"Compiling...");
                                        code = Compiler.Compile(result.code, result.path == null ? "" : Path.GetFileNameWithoutExtension(result.path));
                                        if (code != null)
                                        {
                                            File.WriteAllBytes(cacheFile, code);
                                            PluginLog.Information($"Compiled and saved");
                                        }
                                    }
                                }
                                else
                                {
                                    PluginLog.Information($"Compiling, cache bypassed...");
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
                                                        ScriptingProcessor.Scripts = ScriptingProcessor.Scripts.RemoveAll(x => ReferenceEquals(loadedScript, x));
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
                                                        DuoLog.Information($"Script installed to {newPath}");
                                                    }
                                                    else if (rewrite)
                                                    {
                                                        //DeleteFileToRecycleBin(result.path);
                                                        File.WriteAllText(result.path, result.code, Encoding.UTF8);
                                                        instance.InternalData.Path = result.path;
                                                        DuoLog.Information($"Script overwritten at {instance.InternalData.Path}");
                                                    }
                                                    instance.OnSetup();
                                                    instance.Controller.ApplyOverrides();
                                                    PluginLog.Information($"Load success");
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
                catch(Exception e)
                {
                    e.Log();
                }
                ThreadIsRunning = false;
                PluginLog.Information($"Compiler part of thread is finished");

                if (!UpdateCompleted)
                {
                    PluginLog.Information($"Starting updating...");
                    try
                    {
                        BlockingBeginUpdate(true);
                    }
                    catch(Exception e)
                    {
                        e.Log();
                    }
                    PluginLog.Information($"Update finished");
                }

            }).Start();
        }
    }

    internal static void OnUpdate()
    {
        for(var i = 0;i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnUpdate();
                }
                catch (Exception e) { e.Log(); }
            }
        }
    }

    internal static void OnCombatStart()
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnCombatStart();
                }
                catch (Exception e) { e.Log(); }
            }
        }
    }

    internal static void OnCombatEnd()
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnCombatEnd();
                }
                catch (Exception e) { e.Log(); }
            }
        }
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
                catch (Exception e) { e.Log(); }
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
                catch (Exception e) { e.Log(); }
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
                catch (Exception e) { e.Log(); }
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
                catch (Exception e) { e.Log(); }
            }
        }
    }

    internal static void OnTetherCreate(uint source, uint target, byte data2, byte data3, byte data5)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnTetherCreate(source, target, data2, data3, data5);
                }
                catch (Exception e) { e.Log(); }
            }
        }
    }

    internal static void OnTetherRemoval(uint source, byte data2, byte data3, byte data5)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnTetherRemoval(source, data2, data3, data5);
                }
                catch (Exception e) { e.Log(); }
            }
        }
    }

    internal static void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            if (Scripts[i].IsEnabled)
            {
                try
                {
                    Scripts[i].OnDirectorUpdate(category);
                }
                catch (Exception e) { e.Log(); }
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
                catch (Exception e) { e.Log(); }
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
                catch (Exception e) { e.Log(); }
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
                catch (Exception e) { e.Log(); }
            }
        }
    }

    internal static void TerritoryChanged()
    {
        for (var i = 0; i < Scripts.Count; i++)
        {
            var s = Scripts[i];
            UpdateState(s);
        }
    }

    internal static void UpdateState(this SplatoonScript s)
    {
        var territoryIsValid = Svc.ClientState.IsLoggedIn && (s.ValidTerritories.Count == 0 || s.ValidTerritories.Contains(Svc.ClientState.TerritoryType));
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
}
