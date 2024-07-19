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
    internal List<DisplayObject> DisplayObjects = [];
    private List<DisplayObject> TempObjects = null;

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
