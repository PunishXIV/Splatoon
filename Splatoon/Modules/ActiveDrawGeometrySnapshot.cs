using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Splatoon.RenderEngines.DirectX11;
using Splatoon.RenderEngines.ImGuiLegacy;
using Splatoon.Serializables;
using Splatoon.Structures;
using Item = (string id, string source, string Namespace, string layout, string element, string kind, string renderEngine, uint color, System.Numerics.Vector3 center, System.Numerics.Vector3 start, System.Numerics.Vector3 end, float? radius, float? innerRadius, float? outerRadius, float? lineRadius, float? facingRad, float? halfAngleRad, float? angleMinRad, float? angleMaxRad);
using Snapshot = (int version, uint frame, uint territoryId, long generatedAtTickMs, System.Collections.Generic.List<(string id, string source, string Namespace, string layout, string element, string kind, string renderEngine, uint color, System.Numerics.Vector3 center, System.Numerics.Vector3 start, System.Numerics.Vector3 end, float? radius, float? innerRadius, float? outerRadius, float? lineRadius, float? facingRad, float? halfAngleRad, float? angleMinRad, float? angleMaxRad)> items);

namespace Splatoon.Modules;

internal static unsafe class ActiveDrawGeometrySnapshot
{
    internal const int Version = 1;

    private static Snapshot cachedSnapshot;
    private static ulong cachedFrame;
    private static bool hasCachedSnapshot;

    internal static Snapshot Build(List<DisplayObject> displayObjects)
    {
        var frame = Framework.Instance()->FrameCounter;
        if (hasCachedSnapshot && cachedFrame == frame)
        {
            return cachedSnapshot;
        }

        var items = new List<Item>(displayObjects.Count);
        for (var i = 0; i < displayObjects.Count; i++)
        {
            if (TryCreateItem(displayObjects[i], i, frame, out var item))
            {
                items.Add(item);
            }
        }

        cachedSnapshot = (Version, (uint)frame, Svc.ClientState.TerritoryType, Environment.TickCount64, items);
        cachedFrame = frame;
        hasCachedSnapshot = true;
        return cachedSnapshot;
    }

    internal static void Invalidate()
    {
        hasCachedSnapshot = false;
        cachedSnapshot = default;
        cachedFrame = 0;
    }

    private static bool TryCreateItem(DisplayObject displayObject, int index, ulong frame, out Item item)
    {
        item = default;
        item.id = GetId(displayObject, index, frame);
        item.source = "render";
        item.renderEngine = displayObject.RenderEngineKind.ToString();

        if (displayObject is DirectX11DisplayObjects.DisplayObjectCircle dxCircle)
        {
            item.kind = "circle";
            item.color = GetDisplayStyleColor(dxCircle.style);
            item.center = dxCircle.origin;
            item.radius = dxCircle.outerRadius;
            return true;
        }

        if (displayObject is DirectX11DisplayObjects.DisplayObjectDonut dxDonut)
        {
            item.kind = "donut";
            item.color = GetDisplayStyleColor(dxDonut.style);
            item.center = dxDonut.origin;
            item.innerRadius = dxDonut.innerRadius;
            item.outerRadius = dxDonut.outerRadius;
            item.radius = dxDonut.outerRadius;
            return true;
        }

        if (displayObject is DirectX11DisplayObjects.DisplayObjectFan dxFan)
        {
            item.kind = "cone";
            item.color = GetDisplayStyleColor(dxFan.style);
            item.center = dxFan.origin;
            item.innerRadius = dxFan.innerRadius;
            item.outerRadius = dxFan.outerRadius;
            item.radius = dxFan.outerRadius;
            item.angleMinRad = dxFan.angleMin;
            item.angleMaxRad = dxFan.angleMax;
            return true;
        }

        if (displayObject is DirectX11DisplayObjects.DisplayObjectLine dxLine)
        {
            item.kind = "line";
            item.color = dxLine.style.strokeColor;
            item.start = dxLine.start;
            item.end = dxLine.stop;
            item.lineRadius = dxLine.radius;
            item.radius = dxLine.radius;
            return true;
        }

        if (displayObject is DirectX11DisplayObjects.DisplayObjectDot dxDot)
        {
            item.kind = "dot";
            item.color = dxDot.color;
            item.center = new Vector3(dxDot.x, dxDot.y, dxDot.z);
            item.radius = dxDot.thickness;
            return true;
        }

        if (displayObject is DirectX11DisplayObjects.DisplayObjectText dxText)
        {
            item.kind = "text";
            item.color = dxText.fgcolor;
            item.center = FromXzy(dxText.x, dxText.y, dxText.z);
            return true;
        }

        if (displayObject is ImGuiLegacyDisplayObjects.DisplayObjectDonut imDonut)
        {
            item.kind = "donut";
            item.color = imDonut.color;
            item.center = FromXzy(imDonut.x, imDonut.y, imDonut.z);
            item.innerRadius = imDonut.radius;
            item.outerRadius = imDonut.radius + imDonut.donut;
            item.radius = item.outerRadius;
            return true;
        }

        if (displayObject is ImGuiLegacyDisplayObjects.DisplayObjectCircle imCircle)
        {
            item.kind = "circle";
            item.color = imCircle.color;
            item.center = FromXzy(imCircle.x, imCircle.y, imCircle.z);
            item.radius = imCircle.radius;
            return true;
        }

        if (displayObject is ImGuiLegacyDisplayObjects.DisplayObjectCone imCone)
        {
            item.kind = "cone";
            item.color = imCone.color;
            item.center = FromXzy(imCone.x, imCone.y, imCone.z);
            item.radius = imCone.radius;
            item.outerRadius = imCone.radius;
            item.angleMinRad = imCone.startRad;
            item.angleMaxRad = imCone.endRad;
            return true;
        }

        if (displayObject is ImGuiLegacyDisplayObjects.DisplayObjectLine imLine)
        {
            item.kind = "line";
            item.color = imLine.color;
            item.start = FromXzy(imLine.ax, imLine.ay, imLine.az);
            item.end = FromXzy(imLine.bx, imLine.by, imLine.bz);
            item.lineRadius = 0f;
            item.radius = 0f;
            return true;
        }

        if (displayObject is ImGuiLegacyDisplayObjects.DisplayObjectDot imDot)
        {
            item.kind = "dot";
            item.color = imDot.color;
            item.center = FromXzy(imDot.x, imDot.y, imDot.z);
            item.radius = imDot.thickness;
            return true;
        }

        if (displayObject is ImGuiLegacyDisplayObjects.DisplayObjectText imText)
        {
            item.kind = "text";
            item.color = imText.fgcolor;
            item.center = FromXzy(imText.x, imText.y, imText.z);
            return true;
        }

        return false;
    }

    private static string GetId(DisplayObject displayObject, int index, ulong frame)
    {
        if(displayObject is DirectX11DisplayObjects.VfxDisplayObject vfx && !vfx.id.IsNullOrEmpty())
        {
            return vfx.id;
        }

        return $"{displayObject.RenderEngineKind}:{frame}:{index}";
    }

    private static uint GetDisplayStyleColor(DisplayStyle style)
    {
        if((style.strokeColor & 0xFF000000) != 0)
        {
            return style.strokeColor;
        }
        if((style.originFillColor & 0xFF000000) != 0)
        {
            return style.originFillColor;
        }
        return style.endFillColor;
    }

    private static Vector3 FromXzy(float x, float y, float z) => new(x, z, y);
}
