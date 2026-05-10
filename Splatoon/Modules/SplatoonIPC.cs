using Snapshot = (int version, uint frame, uint territoryId, long generatedAtTickMs, System.Collections.Generic.List<(string id, string source, string Namespace, string layout, string element, string kind, string renderEngine, uint color, System.Numerics.Vector3 center, System.Numerics.Vector3 start, System.Numerics.Vector3 end, float? radius, float? innerRadius, float? outerRadius, float? lineRadius, float? facingRad, float? halfAngleRad, float? angleMinRad, float? angleMaxRad)> items);

namespace Splatoon.Modules;

internal static class SplatoonIPC
{
    internal static void Init()
    {
        Svc.PluginInterface.GetIpcProvider<bool>("Splatoon.Loaded").SendMessage();
        Svc.PluginInterface.GetIpcProvider<bool>("Splatoon.IsLoaded").RegisterFunc(() => { return true; });
        Svc.PluginInterface.GetIpcProvider<Snapshot>("Splatoon.GetActiveDrawGeometryV1").RegisterFunc(GetActiveDrawGeometry);
    }

    internal static void Dispose()
    {
        Svc.PluginInterface.GetIpcProvider<Snapshot>("Splatoon.GetActiveDrawGeometryV1").UnregisterFunc();
        Svc.PluginInterface.GetIpcProvider<bool>("Splatoon.IsLoaded").UnregisterFunc();
        Svc.PluginInterface.GetIpcProvider<bool>("Splatoon.Unloaded").SendMessage();
    }

    private static Snapshot GetActiveDrawGeometry()
    {
        return ActiveDrawGeometrySnapshot.Build(S.RenderManager.GetUnifiedDisplayObjects());
    }
}
