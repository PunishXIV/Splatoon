namespace Splatoon.Modules;

internal static class SplatoonIPC
{
    private static string activeDrawGeometryJson = "";

    internal static void Init()
    {
        UpdateActiveDrawGeometrySnapshot();
        Svc.PluginInterface.GetIpcProvider<bool>("Splatoon.Loaded").SendMessage();
        Svc.PluginInterface.GetIpcProvider<bool>("Splatoon.IsLoaded").RegisterFunc(() => { return true; });
        Svc.PluginInterface.GetIpcProvider<string>("Splatoon.GetActiveDrawGeometryV1").RegisterFunc(() => activeDrawGeometryJson);
    }

    internal static void Dispose()
    {
        Svc.PluginInterface.GetIpcProvider<string>("Splatoon.GetActiveDrawGeometryV1").UnregisterFunc();
        Svc.PluginInterface.GetIpcProvider<bool>("Splatoon.IsLoaded").UnregisterFunc();
        Svc.PluginInterface.GetIpcProvider<bool>("Splatoon.Unloaded").SendMessage();
    }

    internal static void UpdateActiveDrawGeometrySnapshot()
    {
        activeDrawGeometryJson = ActiveDrawGeometrySnapshot.BuildJson(S.RenderManager.GetUnifiedDisplayObjects());
    }
}
