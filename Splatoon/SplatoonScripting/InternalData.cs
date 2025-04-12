#nullable enable
using ECommons.Configuration;
using ECommons.Reflection;
using Splatoon.Services;
using Splatoon.SplatoonScripting.Priority;

namespace Splatoon.SplatoonScripting;

public class InternalData
{
    internal static char[] IdentifierLetters = ['q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p', 'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'z', 'x', 'c', 'v', 'b', 'n', 'm', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0'];

    public string Path { get; internal set; }
    public string Namespace { get; internal set; }
    public string Name { get; internal set; }
    public string GUID { get; } = "Script" + Guid.NewGuid().ToString();
    public string FullName { get; internal set; }

    internal bool ConfigOpen = false;

    internal bool Allowed = false;

    internal bool Blacklisted = false;

    internal SplatoonScript Script;

    internal OverrideData Overrides;

    internal string CurrentConfigurationKey = "";

    internal string OverridesPath => GetOverridesPathForConfigurationKey(CurrentConfigurationKey);
    internal string ConfigurationPath => GetConfigPathForConfigurationKey(CurrentConfigurationKey);

    internal bool UnconditionalDraw = false;
    internal HashSet<string> UnconditionalDrawElements = [];
    internal HashSet<string> ElementsResets = [];

    public InternalData(string path, SplatoonScript instance)
    {
        Script = instance;
        Path = path;
        Namespace = instance.GetType().Namespace ?? "Default";
        Name = instance.GetType().Name;
        FullName = $"{Namespace}@{Name}";
        InitializeConfiguration();
        Overrides = EzConfig.LoadConfiguration<OverrideData>($"{OverridesPath}", false);
        PluginLog.Information($"Script {FullName} ready.");
    }

    public string GetOverridesPathForConfigurationKey(string key)
    {
        return $"{Path}.overrides{(key == "" ? "" : $".{key}")}.json";
    }

    public string GetConfigPathForConfigurationKey(string key)
    {
        return $"{Path}{(key == "" ? "" : $".{key}")}.json";
    }

    public bool ContainsPriorityLists()
    {
        foreach(var s in Script.GetType().Assembly.GetTypes())
        {
            foreach(var x in s.GetFieldPropertyUnions(ReflectionHelper.AllFlags))
            {
                if(x.UnionType.FullName == typeof(PriorityData).FullName) return true;
                var t = x.UnionType.BaseType;
                while(t != null)
                {
                    if(t.FullName == typeof(PriorityData).FullName) return true;
                    t = t.BaseType;
                }
            }
        }
        return false;
    }

    internal string GetFreeConfigurationKey()
    {
        for(var i = 0; i < 1000000; i++)
        {
            var id = new StringBuilder();
            for(var x = 0; x < 8; x++)
            {
                id.Append(IdentifierLetters.GetRandom());
            }
            var idstr = id.ToString();
            if(!File.Exists($"{Path}{idstr}.json") && !File.Exists($"{Path}.overrides{idstr}.json"))
            {
                return idstr;
            }
        }
        throw new InvalidOperationException($"Could not find free configuration identifier for {Path}. You may have to manually clean up old configurations.");
    }

    internal void InitializeConfiguration()
    {
        if(P.Config.ActiveScriptConfigurations.TryGetValue(FullName, out var config))
        {
            CurrentConfigurationKey = config;
            PluginLog.Information($"For script {FullName}, switched configuration to {config} / {Utils.GetScriptConfigurationName(FullName, config)}");
        }
    }

    internal List<string> GetConfigurationIdentifiersFromFilesystem()
    {
        var ret = new List<string>();
        var dir = new DirectoryInfo(System.IO.Path.GetDirectoryName(Path)!);
        foreach(var file in dir.GetFiles())
        {
            try
            {
                if(file.Name.StartsWith(Name))
                {
                    var identifier = file.Name[Name.Length..].Split(".")[^2];
                    if(identifier.Length == 8 && identifier.All(x => IdentifierLetters.Contains(x)))
                    {
                        ret.Add(identifier);
                    }
                }
            }
            catch(Exception e)
            {
                e.Log();
            }
        }
        return ret;
    }

    public void ReloadOverrides()
    {
        Overrides = EzConfig.LoadConfiguration<OverrideData>($"{OverridesPath}", false);
    }

    public bool SettingsPresent => Script.GetType().GetMethod("OnSettingsDraw")!.DeclaringType != typeof(SplatoonScript);
}
