using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
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
    internal DirectX11Renderer DirectX11Renderer;
    internal ImGuiLegacyRenderer ImGuiLegacyRenderer;

    private RenderManager()
    {
        DirectX11Renderer = new();
        ImGuiLegacyRenderer = new();
    }

    internal void ReloadEngine(RenderEngineKind kind)
    {
        if(kind == RenderEngineKind.DirectX11)
        {
            try
            {
                DirectX11Renderer?.Dispose();
            }
            catch(Exception e) { e.Log(); }
            DirectX11Renderer = new();
        }
        if(kind == RenderEngineKind.ImGui_Legacy)
        {
            try
            {
                ImGuiLegacyRenderer?.Dispose();
            }
            catch(Exception e) { e.Log(); }
            ImGuiLegacyRenderer = new();
        }
    }

    internal RenderEngine GetRenderer()
    {
        if(P.Config.RenderEngineKind == RenderEngineKind.DirectX11 && DirectX11Renderer.Enabled && DirectX11Renderer.LoadError == null) return DirectX11Renderer;
        return ImGuiLegacyRenderer;
    }

    internal RenderEngine GetRenderer(Element element)
    {
        if(element.RenderEngineKind == RenderEngineKind.Unspecified) return GetRenderer();
        if(element.RenderEngineKind == RenderEngineKind.DirectX11 && DirectX11Renderer.Enabled && DirectX11Renderer.LoadError == null) return DirectX11Renderer;
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
        foreach(var x in ImGuiLegacyRenderer.DisplayObjects)
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
            if(x.RenderEngineKind == RenderEngineKind.DirectX11) DirectX11Renderer.DisplayObjects.Add(x);
            if(x.RenderEngineKind == RenderEngineKind.ImGui_Legacy) ImGuiLegacyRenderer.DisplayObjects.Add(x);
        }
    }

    internal void DrawCommonSettings(RenderEngineKind kind)
    {
        if(kind == RenderEngineKind.DirectX11) DirectX11Renderer.DrawSettings();
        if(kind == RenderEngineKind.ImGui_Legacy) ImGuiLegacyRenderer.DrawSettings();
    }

    private unsafe uint* FrameCounter = &Framework.Instance()->FrameCounter;
    private uint NearPlaneFrame = 0;
    public unsafe Vector4? NearPlane
    {
        get
        {
            if(NearPlaneFrame != *FrameCounter)
            {
                try
                {
                    field = GetNearPlane();
                }
                catch(Exception e)
                {
                    var m = e.ToStringFull();
                    if(EzThrottler.Throttle(m, 10000))
                    {
                        PluginLog.Error(m);
                    }
                }
                NearPlaneFrame = *FrameCounter;
            }
            return field;
        }
    }

    unsafe Vector4? GetNearPlane()
    {
        Matrix4x4 viewProj = Control.Instance()->ViewProjectionMatrix;

        // The view matrix in CameraManager is 1 frame stale compared to the Control viewproj matrix.
        // Computing the near plane using the stale view matrix results in clipping errors that look really bad when moving the camera.
        // Instead, compute the view matrix using the accurate viewproj matrix multiplied by the stale inverse proj matrix (Which rarely changes)
        var controlCamera = Control.Instance()->CameraManager.GetActiveCamera();
        var renderCamera = controlCamera != null ? controlCamera->SceneCamera.RenderCamera : null;
        if(renderCamera == null) return null;
        var Proj = renderCamera->ProjectionMatrix;
        if(!Matrix4x4.Invert(Proj, out var InvProj)) return null;
        var View = viewProj * InvProj;

        return new(View.M13, View.M23, View.M33, View.M43 + renderCamera->NearPlane);
    }

    public void Dispose()
    {
        DirectX11Renderer.Dispose();
        ImGuiLegacyRenderer.Dispose();
    }
}
