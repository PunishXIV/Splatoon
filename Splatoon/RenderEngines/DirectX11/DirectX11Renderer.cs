using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.RenderEngines.DirectX11;
public sealed class DirectX11Renderer : RenderEngine
{
    DirectX11Scene DirectX11Scene;

    internal DirectX11Renderer()
    {
        try
        {
            DirectX11Scene = new(this);
        }
        catch(Exception e)
        {
            this.LoadError = e;
            e.Log();
        }
    }

    public override void Dispose()
    {
        Safe(() => DirectX11Scene?.Dispose());
    }
}
