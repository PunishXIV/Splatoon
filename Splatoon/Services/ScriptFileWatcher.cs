using ECommons.ChatMethods;
using Splatoon.SplatoonScripting;
using System.Threading;

namespace Splatoon.Services;
public class ScriptFileWatcher : IDisposable
{
    List<FileSystemWatcher> Watchers = [];
    private ScriptFileWatcher()
    {
        StartWatching();
    }

    public void Dispose()
    {
        StopWatching();
    }

    void StopWatching()
    {
        foreach(var w in Watchers)
        {
            w.EnableRaisingEvents = false;
            w.Dispose();
        }
        Watchers.Clear();
    }

    internal void StartWatching()
    {
        StopWatching();
        foreach(var s in P.Config.FileWatcherPathes)
        {
            var x = s.Replace("\"", "");
            if(x == "") continue;
            try
            {
                if(!Directory.Exists(x)) throw new DirectoryNotFoundException();
                var watcher = new FileSystemWatcher(x, "*.cs")
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.Attributes |
                    NotifyFilters.CreationTime |
                    NotifyFilters.FileName |
                    NotifyFilters.LastAccess |
                    NotifyFilters.LastWrite |
                    NotifyFilters.Size |
                    NotifyFilters.Security,
                    EnableRaisingEvents = true
                };
                watcher.Created += Watcher_Created;
                watcher.Changed += Watcher_Created;
                PluginLog.Information($"Watching for changed scripts in {x}");
                Watchers.Add(watcher);
            }
            catch(Exception e)
            {
                PluginLog.Error($"Error starting FileSystemWatcher on {x}");
                e.Log();
            }
        }
    }

    private void Watcher_Created(object sender, FileSystemEventArgs e)
    {
        PluginLog.Debug($"File changed: {e.FullPath} / {e.ChangeType}");
        if(e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Changed)
        {
            try
            {
                S.ThreadPool.Run(() =>
                {
                    try
                    {
                        Thread.Sleep(1000);
                        using var reader = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var sreader = new StreamReader(reader);
                        var text = sreader.ReadToEnd();
                        var scriptNamespace = ScriptingProcessor.ExtractNamespaceFromCode(text);
                        var scriptName = ScriptingProcessor.ExtractClassFromCode(text);
                        if(scriptName == null || scriptNamespace == null)
                        {
                            throw new NullReferenceException($"Could not parse namespace or name from script {e.FullPath}");
                        }
                        var script = ScriptingProcessor.Scripts.FirstOrDefault(x => x.InternalData.Name == scriptName && x.InternalData.Namespace == scriptNamespace);
                        if(script != null)
                        {
                            if(script.IsDisabledByUser)
                            {
                                PluginLog.Warning($"Script {script.InternalData.FullName} is disabled by user, skipping reload for {e.FullPath}");
                            }
                            else
                            {
                                PluginLog.Information($"Auto-reloading {script.InternalData.FullName} from {e.FullPath}");
                                ChatPrinter.Green($"Auto-reloading script {script.InternalData.Name}");
                                ScriptingProcessor.CompileAndLoad(text, script.InternalData.Path);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        ex.Log();
                    }
                });
            }
            catch(Exception ex)
            {
                ex.Log();
            }
        }
    }
}
