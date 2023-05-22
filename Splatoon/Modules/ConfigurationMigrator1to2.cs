#pragma warning disable CS0612

namespace Splatoon.Modules;

internal static class ConfigurationMigrator1to2
{
    internal static void Migrate(Configuration config)
    {
        if (config.Version == 1)
        {
            DuoLog.Warning("Migrating configuration from version 1 to version 2");
            var bkpPath = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "configV1.json");
            var cfgPath = Path.Combine(new DirectoryInfo(Svc.PluginInterface.GetPluginConfigDirectory()).Parent.FullName, "Splatoon.json");
            try
            {
                File.Copy(cfgPath, bkpPath, true);
                DuoLog.Warning($"Backup created at {bkpPath}");
                foreach (var x in config.Layouts)
                {
                    config.LayoutsL.Add(x.Value);
                    x.Value.Name = x.Key;
                    foreach (var e in x.Value.Elements)
                    {
                        x.Value.ElementsL.Add(e.Value);
                        e.Value.Name = e.Key;
                    }
                    DuoLog.Warning($"Layout {x.Key}: old size: {x.Value.Elements.Count}, new size: {x.Value.ElementsL.Count}");
                    if (x.Value.Elements.Count > x.Value.ElementsL.Count)
                    {
                        throw new Exception($"Migration failed: original collection contains more items than new.");
                    }
                    x.Value.Elements.Clear();
                }
                DuoLog.Warning($"Layout size: old: {config.Layouts.Count}, new: {config.LayoutsL.Count}");
                if (config.Layouts.Count > config.LayoutsL.Count)
                {
                    throw new Exception($"Migration failed: original collection contains more items than new.");
                }
                config.Version = 2;
                config.Layouts.Clear();
                config.Save(true);
                DuoLog.Warning($"Migration success!");
            }
            catch (Exception e)
            {
                DuoLog.Error("Failed to migrate configuration (1 -> 2). Please contact developer.");
                DuoLog.Error($"{e.Message}\n{e.StackTrace}");
            }
        }
    }
}
