global using static Splatoon.Modules.Logging;

namespace Splatoon.Modules;

internal static class Logging
{
    public static void LogErrorAndNotify(Exception exception, string additionalDescription = null)
    {
        PluginLog.Error($"Error occurred during Splatoon plugin execution{(additionalDescription == null ? "" : $": {additionalDescription}")}");
        PluginLog.Error($"{exception.Message}\n{exception.StackTrace ?? "No stack trace smh"}");
        Notify.Error(additionalDescription ?? exception.Message);
    }

    public static void LogErrorAndNotify(string additionalDescription)
    {
        PluginLog.Error($"Error occurred during Splatoon plugin execution");
        Notify.Error(additionalDescription);
    }
}
