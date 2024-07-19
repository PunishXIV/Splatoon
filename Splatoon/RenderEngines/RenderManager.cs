using Splatoon.RenderEngines.DirectX11;
using Splatoon.RenderEngines.ImGuiLegacy;
using Splatoon.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.RenderEngines;
public class RenderManager : IDisposable
{
    internal readonly DirectX11Renderer DirectX11Renderer;
    internal readonly ImGuiLegacyRenderer ImGuiLegacyRenderer;

    private RenderManager()
    {
        DirectX11Renderer = new();
        ImGuiLegacyRenderer = new();
    }

    internal RenderEngine GetRenderer()
    {
        if (P.Config.RenderEngineKind == RenderEngineKind.DirectX11 && DirectX11Renderer.Enabled && DirectX11Renderer.LoadError == null) return DirectX11Renderer;
        return ImGuiLegacyRenderer;
    }

    internal RenderEngine GetRenderer(Element element)
    {
        if (element.RenderEngineKind == RenderEngineKind.Unspecified) return GetRenderer();
        if (element.RenderEngineKind == RenderEngineKind.DirectX11 && DirectX11Renderer.Enabled && DirectX11Renderer.LoadError == null) return DirectX11Renderer;
        return ImGuiLegacyRenderer;
    }

    internal void ClearDisplayObjects()
    {
        DirectX11Renderer.DisplayObjects.Clear();
        ImGuiLegacyRenderer.DisplayObjects.Clear();
    }

    internal void StoreDisplayObjects()
    {
        DirectX11Renderer.StoreDisplayObjects();
        ImGuiLegacyRenderer.StoreDisplayObjects();
    }

    internal void RestoreDisplayObjects()
    {
        DirectX11Renderer.RestoreDisplayObjects();
        ImGuiLegacyRenderer.RestoreDisplayObjects();
    }

    internal List<DisplayObject> GetUnifiedDisplayObjects()
    {
        var ret = new List<DisplayObject>();
        foreach(var x in DirectX11Renderer.DisplayObjects)
        {
            x.RenderEngineKind = RenderEngineKind.DirectX11;
            ret.Add(x);
        }
        foreach (var x in ImGuiLegacyRenderer.DisplayObjects)
        {
            x.RenderEngineKind = RenderEngineKind.ImGui_Legacy;
            ret.Add(x);
        }
        return ret;
    }

    internal void InjectDisplayObjects(List<DisplayObject> displayObjects)
    {
        foreach(var x in displayObjects)
        {
            if (x.RenderEngineKind == RenderEngineKind.DirectX11) DirectX11Renderer.DisplayObjects.Add(x);
            if (x.RenderEngineKind == RenderEngineKind.ImGui_Legacy) ImGuiLegacyRenderer.DisplayObjects.Add(x);
        }
    }

    internal void DrawCommonSettings(RenderEngineKind kind)
    {
        if (kind == RenderEngineKind.DirectX11) DirectX11Renderer.DrawSettings();
        if (kind == RenderEngineKind.ImGui_Legacy) ImGuiLegacyRenderer.DrawSettings();
    }

    public void Dispose()
    {
        DirectX11Renderer.Dispose();
        ImGuiLegacyRenderer.Dispose();
    }
}
