using Newtonsoft.Json;
using Splatoon.RenderEngines.DirectX11;
using Splatoon.RenderEngines.ImGuiLegacy;
using Splatoon.Serializables;
using Splatoon.Structures;

namespace Splatoon.Modules;

internal static class ActiveDrawGeometrySnapshot
{
    internal const int Version = 1;

    internal static string BuildJson(List<DisplayObject> displayObjects)
    {
        var snapshot = new Snapshot()
        {
            version = Version,
            frame = P.FrameCounter,
            territoryId = Svc.ClientState.TerritoryType,
            generatedAtTickMs = Environment.TickCount64,
        };

        for(var i = 0; i < displayObjects.Count; i++)
        {
            if(TryCreateItem(displayObjects[i], i, out var item))
            {
                snapshot.items.Add(item);
            }
        }

        return JsonConvert.SerializeObject(snapshot, Formatting.None);
    }

    private static bool TryCreateItem(DisplayObject displayObject, int index, out Item item)
    {
        item = new()
        {
            id = GetId(displayObject, index),
            source = "render",
            Namespace = null,
            layout = null,
            element = null,
            renderEngine = displayObject.RenderEngineKind.ToString(),
            facingRad = null,
            halfAngleRad = null,
        };

        if(displayObject is DirectX11DisplayObjects.DisplayObjectCircle dxCircle)
        {
            item.kind = "circle";
            item.color = GetDisplayStyleColor(dxCircle.style);
            item.center = MakePoint(dxCircle.origin);
            item.radius = dxCircle.outerRadius;
            return true;
        }
        if(displayObject is DirectX11DisplayObjects.DisplayObjectDonut dxDonut)
        {
            item.kind = "donut";
            item.color = GetDisplayStyleColor(dxDonut.style);
            item.center = MakePoint(dxDonut.origin);
            item.innerRadius = dxDonut.innerRadius;
            item.outerRadius = dxDonut.outerRadius;
            item.radius = dxDonut.outerRadius;
            return true;
        }
        if(displayObject is DirectX11DisplayObjects.DisplayObjectFan dxFan)
        {
            item.kind = "cone";
            item.color = GetDisplayStyleColor(dxFan.style);
            item.center = MakePoint(dxFan.origin);
            item.innerRadius = dxFan.innerRadius;
            item.outerRadius = dxFan.outerRadius;
            item.radius = dxFan.outerRadius;
            item.angleMinRad = dxFan.angleMin;
            item.angleMaxRad = dxFan.angleMax;
            return true;
        }
        if(displayObject is DirectX11DisplayObjects.DisplayObjectLine dxLine)
        {
            item.kind = "line";
            item.color = dxLine.style.strokeColor;
            item.start = MakePoint(dxLine.start);
            item.end = MakePoint(dxLine.stop);
            item.lineRadius = dxLine.radius;
            item.radius = dxLine.radius;
            return true;
        }
        if(displayObject is DirectX11DisplayObjects.DisplayObjectDot dxDot)
        {
            item.kind = "dot";
            item.color = dxDot.color;
            item.center = MakePoint(dxDot.x, dxDot.y, dxDot.z);
            item.radius = dxDot.thickness;
            return true;
        }
        if(displayObject is DirectX11DisplayObjects.DisplayObjectText dxText)
        {
            item.kind = "text";
            item.color = dxText.fgcolor;
            item.center = MakePointFromXzy(dxText.x, dxText.y, dxText.z);
            return true;
        }
        if(displayObject is ImGuiLegacyDisplayObjects.DisplayObjectDonut imDonut)
        {
            item.kind = "donut";
            item.color = imDonut.color;
            item.center = MakePointFromXzy(imDonut.x, imDonut.y, imDonut.z);
            item.innerRadius = imDonut.radius;
            item.outerRadius = imDonut.radius + imDonut.donut;
            item.radius = item.outerRadius;
            return true;
        }
        if(displayObject is ImGuiLegacyDisplayObjects.DisplayObjectCircle imCircle)
        {
            item.kind = "circle";
            item.color = imCircle.color;
            item.center = MakePointFromXzy(imCircle.x, imCircle.y, imCircle.z);
            item.radius = imCircle.radius;
            return true;
        }
        if(displayObject is ImGuiLegacyDisplayObjects.DisplayObjectCone imCone)
        {
            item.kind = "cone";
            item.color = imCone.color;
            item.center = MakePointFromXzy(imCone.x, imCone.y, imCone.z);
            item.radius = imCone.radius;
            item.outerRadius = imCone.radius;
            item.angleMinRad = imCone.startRad;
            item.angleMaxRad = imCone.endRad;
            return true;
        }
        if(displayObject is ImGuiLegacyDisplayObjects.DisplayObjectLine imLine)
        {
            item.kind = "line";
            item.color = imLine.color;
            item.start = MakePointFromXzy(imLine.ax, imLine.ay, imLine.az);
            item.end = MakePointFromXzy(imLine.bx, imLine.by, imLine.bz);
            item.lineRadius = 0f;
            item.radius = 0f;
            return true;
        }
        if(displayObject is ImGuiLegacyDisplayObjects.DisplayObjectDot imDot)
        {
            item.kind = "dot";
            item.color = imDot.color;
            item.center = MakePointFromXzy(imDot.x, imDot.y, imDot.z);
            item.radius = imDot.thickness;
            return true;
        }
        if(displayObject is ImGuiLegacyDisplayObjects.DisplayObjectText imText)
        {
            item.kind = "text";
            item.color = imText.fgcolor;
            item.center = MakePointFromXzy(imText.x, imText.y, imText.z);
            return true;
        }

        return false;
    }

    private static string GetId(DisplayObject displayObject, int index)
    {
        if(displayObject is DirectX11DisplayObjects.VfxDisplayObject vfx && !vfx.id.IsNullOrEmpty())
        {
            return vfx.id;
        }

        return $"{displayObject.RenderEngineKind}:{P.FrameCounter}:{index}";
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

    private static Point MakePoint(Vector3 point) => new()
    {
        x = point.X,
        y = point.Y,
        z = point.Z,
    };

    private static Point MakePoint(float x, float y, float z) => new()
    {
        x = x,
        y = y,
        z = z,
    };

    private static Point MakePointFromXzy(float x, float y, float z) => new()
    {
        x = x,
        y = z,
        z = y,
    };

    private sealed class Snapshot
    {
        public int version;
        public uint frame;
        public uint territoryId;
        public long generatedAtTickMs;
        public List<Item> items = [];
    }

    private sealed class Item
    {
        public string id;
        public string source;
        [JsonProperty("namespace")]
        public string Namespace;
        public string layout;
        public string element;
        public string kind;
        public string renderEngine;
        public uint color;
        public Point center;
        public Point start;
        public Point end;
        public float? radius;
        public float? innerRadius;
        public float? outerRadius;
        public float? lineRadius;
        public float? facingRad;
        public float? halfAngleRad;
        public float? angleMinRad;
        public float? angleMaxRad;
    }

    private sealed class Point
    {
        public float x;
        public float y;
        public float z;
    }
}
