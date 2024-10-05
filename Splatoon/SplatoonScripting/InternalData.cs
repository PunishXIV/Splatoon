#nullable enable
using ECommons.Configuration;

namespace Splatoon.SplatoonScripting;

public class InternalData
{
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

    internal ScriptConfigurationsList ScriptConfigurationsList;

    internal string OverridesPath => $"{Path}.overrides.json";
    internal string ConfigurationsPath => $"{Path}.conf.json";

    internal bool UnconditionalDraw = false;
    internal HashSet<string> UnconditionalDrawElements = new();
    internal HashSet<string> ElementsResets = new();

    public InternalData(string path, SplatoonScript instance)
    {
        Script = instance;
        Path = path;
        Namespace = instance.GetType().Namespace ?? "Default";
        Name = instance.GetType().Name;
        FullName = $"{Namespace}@{Name}";
        Overrides = EzConfig.LoadConfiguration<OverrideData>($"{OverridesPath}", false);
        ScriptConfigurationsList = EzConfig.LoadConfiguration<ScriptConfigurationsList>($"{ConfigurationsPath}", false);
        PluginLog.Information($"Script {FullName} ready.");
    }

    public void ReloadOverrides()
    {
        Overrides = EzConfig.LoadConfiguration<OverrideData>($"{OverridesPath}", false);
    }

    public bool SettingsPresent => Script.GetType().GetMethod("OnSettingsDraw")!.DeclaringType != typeof(SplatoonScript);
}
