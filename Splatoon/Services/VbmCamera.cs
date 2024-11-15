using Dalamud.Interface.Utility;
using Dalamud.Utility;
using ECommons;
using ECommons.EzEventManager;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using ImGuiNET;
using PInvoke;

namespace Splatoon.Services;

internal unsafe class VbmCamera
{
    public static VbmCamera? Instance;

    public Vector3 Origin;
    public Matrix4x4 View;
    public Matrix4x4 Proj;
    public Matrix4x4 ViewProj;
    public Vector4 NearPlane;
    public float CameraAzimuth; // facing north = 0, facing west = pi/4, facing south = +-pi/2, facing east = -pi/4
    public float CameraAltitude; // facing horizontally = 0, facing down = pi/4, facing up = -pi/4
    public Vector2 ViewportSize;

    private readonly List<(Vector2 from, Vector2 to, uint col)> _worldDrawLines = [];

    private VbmCamera()
    {
        new EzFrameworkUpdate(Update);
    }

    public unsafe void Update()
    {
        var controlCamera = CameraManager.Instance()->GetActiveCamera();
        var renderCamera = controlCamera != null ? controlCamera->SceneCamera.RenderCamera : null;
        if(renderCamera == null)
            return;

        Origin = renderCamera->Origin;
        View = renderCamera->ViewMatrix;
        View.M44 = 1; // for whatever reason, game doesn't initialize it...
        Proj = renderCamera->ProjectionMatrix;
        ViewProj = View * Proj;

        // note that game uses reverse-z by default, so we can't just get full plane equation by reading column 3 of vp matrix
        // so just calculate it manually: column 3 of view matrix is plane equation for a plane equation going through origin
        // proof:
        // plane equation p is such that p.dot(Q, 1) = 0 if Q lines on the plane => pw = -Q.dot(n); for view matrix, V43 is -origin.dot(forward)
        // plane equation for near plane has Q.dot(n) = O.dot(n) - near => pw = V43 + near
        NearPlane = new(View.M13, View.M23, View.M33, View.M43 + renderCamera->NearPlane);

        CameraAzimuth = MathF.Atan2(View.M13, View.M33);
        CameraAltitude = MathF.Asin(View.M23);
        var device = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Device.Instance();
        ViewportSize = new(device->Width, device->Height);
    }

    public bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos)
    {
        // Read current ViewProjectionMatrix plus game window size
        var windowPos = ImGuiHelpers.MainViewport.Pos;
        var viewProjectionMatrix = this.ViewProj;
        var device = Device.Instance();
        float width = device->Width;
        float height = device->Height;

        var pCoords = Vector4.Transform(new Vector4(worldPos, 1.0f), viewProjectionMatrix);
        var inFront = pCoords.W > 0.0f;
        var inView = false;
        if(Math.Abs(pCoords.W) < float.Epsilon)
        {
            screenPos = Vector2.Zero;
            inView = false;
            return false;
        }

        pCoords *= MathF.Abs(1.0f / pCoords.W);
        screenPos = new Vector2(pCoords.X, pCoords.Y);

        screenPos.X = (0.5f * width * (screenPos.X + 1f)) + windowPos.X;
        screenPos.Y = (0.5f * height * (1f - screenPos.Y)) + windowPos.Y;

        inView = inFront &&
                 screenPos.X > windowPos.X && screenPos.X < windowPos.X + width &&
                 screenPos.Y > windowPos.Y && screenPos.Y < windowPos.Y + height;

        return inFront && inView;
    }

    /*public bool ScreenToWorld(Vector3 start, out Vector2 screenPos)
    {
        var windowPos = ImGuiHelpers.MainViewport.Pos;
        var p1p = Vector4.Transform(start, ViewProj);
        var p1c = XY(p1p) * (1 / p1p.W);
        var p1screen = new Vector2(0.5f * ViewportSize.X * (1 + p1c.X), 0.5f * ViewportSize.Y * (1 - p1c.Y)) + windowPos;

        var inFront = p1p.W > 0.0f;
        var inView = false;
        if(Math.Abs(p1p.W) < float.Epsilon)
        {
            screenPos = Vector2.Zero;
            inView = false;
        }

        screenPos = p1screen;
        inView = inFront &&
                 screenPos.X > windowPos.X && screenPos.X < windowPos.X + this.ViewportSize.X &&
                 screenPos.Y > windowPos.Y && screenPos.Y < windowPos.Y + this.ViewportSize.Y;
        return inView && IsOnScreen(start);
    }*/
    static Vector2 XY(Vector4 v) => new(v.X, v.Y);
    
    private bool IsOnScreen(Vector3 a)
    {
        var an = Vector4.Dot(new(a, 1), NearPlane);
        if(an >= 0)
            return false;
        return true;
    }
}