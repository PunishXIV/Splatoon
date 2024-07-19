using Splatoon.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.RenderEngines;
public abstract class RenderEngine : IDisposable
{
    internal Exception LoadError { get; set; } = null;
    internal List<DisplayObject> DisplayObjects { get; private set; } = [];
    private List<DisplayObject> TempObjects = null;

    internal abstract void DrawCircle(Element e, float x, float y, float z, float r, float angle, IGameObject go = null);
    internal abstract void DrawCone(Element e, Vector3 origin, float? radius = null, float baseAngle = 0f);
    internal abstract void AddRotatedLine(Vector3 tPos, float angle, Element e, float aradius, float hitboxRadius);

    internal void StoreDisplayObjects()
    {
        TempObjects = DisplayObjects;
        DisplayObjects = [];
    }

    internal void RestoreDisplayObjects()
    {
        DisplayObjects = TempObjects;
        TempObjects = null;
    }

    public abstract void Dispose();
}
