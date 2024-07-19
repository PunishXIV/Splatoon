using Splatoon.RenderEngines.DirectX11;
using Splatoon.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.RenderEngines;
public class RenderManager
{
    private DirectX11Renderer DirectX11Renderer;
    private RenderManager()
    {
        DirectX11Renderer = new();
    }

    internal RenderEngine GetRenderer()
    {
        return DirectX11Renderer;
    }

    internal RenderEngine GetRenderer(Element element)
    {
        return DirectX11Renderer;
    }

    internal void ClearDisplayObjects()
    {
        DirectX11Renderer.DisplayObjects.Clear();
    }

    internal void StoreDisplayObjects()
    {
        DirectX11Renderer.StoreDisplayObjects();
    }

    internal void RestoreDisplayObjects()
    {
        DirectX11Renderer.RestoreDisplayObjects();
    }

    internal List<DisplayObject> GetUnifiedDisplayObjects()
    {
        var ret = new List<DisplayObject>();
        foreach(var x in DirectX11Renderer.DisplayObjects)
        {
            x.RenderEngineKind = RenderEngineKind.DirectX11;
            ret.Add(x);
        }
        return ret;
    }

    internal void InjectOwnDisplayObjects(List<DisplayObject> displayObjects)
    {
        foreach(var x in displayObjects)
        {
            if(x.RenderEngineKind == RenderEngineKind.DirectX11) DirectX11Renderer.DisplayObjects.Add(x);
        }
    }
}
