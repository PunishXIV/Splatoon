using ECommons;
using Lumina.Excel.GeneratedSheets;

namespace Splatoon.Modules;

internal static class Logger
{
    static SimpleLogger currentLogger = null;

    internal static void BeginLogging()
    {
        Safe(delegate
        {
            EndLogging();
            var dirName = $"{DateTimeOffset.Now:yyyy-MM-ddzzz}".Replace(":", "_");
            var directory = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "Logs", dirName);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var fileName = $"{DateTimeOffset.Now:yyyy-MM-ddzzz HH.mm.ss} - {Svc.Data.GetExcelSheet<TerritoryType>().GetRow(Svc.ClientState.TerritoryType).ContentFinderCondition.Value.Name.ToString()}.txt".Replace(":", "_");
            currentLogger = new SimpleLogger(directory, fileName);
        });
    }

    internal static void OnTerritoryChanged()
    {
        EndLogging();
        if (P.Config.Logging)
        {
            var name = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(Svc.ClientState.TerritoryType)?.ContentFinderCondition?.Value?.Name?.ToString();
            if (name != String.Empty && name != null)
            {
                BeginLogging();
            }
        }
    }

    internal static void Log(string message)
    {
        if(currentLogger != null)
        {
            var combatTime = Environment.TickCount64 - P.CombatStarted; ;
            currentLogger.Log($"[{(P.CombatStarted != 0?$"Combat: {((float)combatTime / 1000f):F1}s":"Not in combat")}] {message}");
        }
    }

    internal static void EndLogging()
    {
        if (currentLogger != null)
        {
            currentLogger.Dispose();
            currentLogger = null;
        }
    }
}
