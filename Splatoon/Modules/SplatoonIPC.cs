namespace Splatoon.Modules;

internal static class SplatoonIPC
{
    internal static void Init()
    {
        Svc.PluginInterface.GetIpcProvider<bool>("Splatoon.Loaded").SendMessage();
        Svc.PluginInterface.GetIpcProvider<bool>("Splatoon.IsLoaded").RegisterFunc(() => { return true; });
    }

    internal static void Dispose()
    {
        Svc.PluginInterface.GetIpcProvider<bool>("Splatoon.IsLoaded").UnregisterFunc();
        Svc.PluginInterface.GetIpcProvider<bool>("Splatoon.Unloaded").SendMessage();
    }
}
