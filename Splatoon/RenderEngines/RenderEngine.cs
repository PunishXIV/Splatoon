using Splatoon.Serializables;
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

    internal abstract void AddLine(float ax, float ay, float az, float bx, float by, float bz, float thickness, uint color, LineEnd startStyle = LineEnd.None, LineEnd endStyle = LineEnd.None);

    internal abstract void ProcessElement(Element e, Layout i = null, bool forceEnable = false);

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
