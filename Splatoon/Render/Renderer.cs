using Splatoon.Gui;
using Splatoon.Serializables;
using Splatoon.Structures;
using System.Drawing;
using System.Runtime.InteropServices;
using static System.Windows.Forms.AxHost;

namespace Splatoon.Render;

public unsafe class Renderer : IDisposable
{
    public const int MAX_FANS = 1024;
    public const int MAX_LINES = 1024;
    public const int MAX_STROKE_SEGMENTS = MAX_FANS * OverlayGui.MAXIMUM_CIRCLE_SEGMENTS;
    public const int MAX_CLIP_ZONES = 256 + MAX_CONFIGURABLE_CLIP_ZONES;

    public RenderContext RenderContext { get; init; } = new();

    public RenderTarget? RenderTarget { get; private set; }
    public RenderTarget? FSPRenderTarget { get; private set; }

    public FanFill FanFill { get; init; }
    public LineFill LineFill { get; init; }
    public Stroke Stroke { get; init; }
    public ClipZone ClipZone { get; init; }
    public FullScreenPass FSP { get; init; }

    public SharpDX.Matrix ViewProj { get; private set; }
    public SharpDX.Matrix Proj { get; private set; }
    public SharpDX.Matrix View { get; private set; }
    public SharpDX.Matrix CameraWorld { get; private set; }
    public SharpDX.Vector2 ViewportSize { get; private set; }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint GetEngineCoreSingletonDelegate();
    private nint _engineCoreSingleton;

    private FanFill.Data _fanFillDynamicData;
    private FanFill.Data.Builder? _fanFillDynamicBuilder;

    private LineFill.Data _lineFillDynamicData;
    private LineFill.Data.Builder? _lineFillDynamicBuilder;

    private Stroke.Data _strokeDynamicData;
    private Stroke.Data.Builder? _strokeDynamicBuilder;

    private ClipZone.Data _clipDynamicData;
    private ClipZone.Data.Builder? _clipDynamicBuilder;

    private AlphaBlendMode _alphaBlendMode;
    public Renderer()
    {
        FanFill = new(RenderContext);
        _fanFillDynamicData = new(RenderContext, MAX_FANS, true);
        LineFill = new(RenderContext);
        _lineFillDynamicData = new(RenderContext, MAX_LINES, true);
        Stroke = new(RenderContext);
        _strokeDynamicData = new(RenderContext, MAX_STROKE_SEGMENTS, true);
        ClipZone = new(RenderContext);
        _clipDynamicData = new(RenderContext, MAX_CLIP_ZONES, true);
        FSP = new(RenderContext);
        // https://github.com/goatcorp/Dalamud/blob/d52118b3ad366a61216129c80c0fa250c885abac/Dalamud/Game/Gui/GameGuiAddressResolver.cs#L69
        _engineCoreSingleton = Marshal.GetDelegateForFunctionPointer<GetEngineCoreSingletonDelegate>(Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4C 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??"))();
    }

    public void Dispose()
    {
        RenderTarget?.Dispose();
        FSPRenderTarget?.Dispose();
        _fanFillDynamicBuilder?.Dispose();
        _fanFillDynamicData?.Dispose();
        _lineFillDynamicBuilder?.Dispose();
        _lineFillDynamicData?.Dispose();
        _strokeDynamicBuilder?.Dispose();
        _strokeDynamicData?.Dispose();
        _clipDynamicBuilder?.Dispose();
        _clipDynamicData?.Dispose();
        FanFill.Dispose();
        LineFill.Dispose();
        Stroke.Dispose();
        ClipZone.Dispose();
        FSP.Dispose();
        RenderContext.Dispose();
    }

    public void BeginFrame()
    {
        ViewProj = ReadMatrix(_engineCoreSingleton + 0x1B4);
        Proj = ReadMatrix(_engineCoreSingleton + 0x174);
        View = ViewProj * SharpDX.Matrix.Invert(Proj);
        CameraWorld = SharpDX.Matrix.Invert(View);
        ViewportSize = ReadVec2(_engineCoreSingleton + 0x1F4);

        FanFill.UpdateConstants(RenderContext, new() { ViewProj = ViewProj });
        LineFill.UpdateConstants(RenderContext, new() { ViewProj = ViewProj });
        Stroke.UpdateConstants(RenderContext, new() { ViewProj = ViewProj, RenderTargetSize = new(ViewportSize.X, ViewportSize.Y) });
        ClipZone.UpdateConstants(RenderContext, new() { RenderTargetSize = new(ViewportSize.X, ViewportSize.Y) });

        if (RenderTarget == null || RenderTarget.Size != ViewportSize || P.Config.AlphaBlendMode != _alphaBlendMode)
        {
            RenderTarget?.Dispose();
            RenderTarget = new(RenderContext, (int)ViewportSize.X, (int)ViewportSize.Y, P.Config.AlphaBlendMode);
            _alphaBlendMode = P.Config.AlphaBlendMode;
        }
        if (FSPRenderTarget == null || FSPRenderTarget.Size != ViewportSize)
        {
            FSPRenderTarget?.Dispose();
            FSPRenderTarget = new(RenderContext, (int)ViewportSize.X, (int)ViewportSize.Y, AlphaBlendMode.None);
        }
        RenderTarget.Bind(RenderContext);
    }

    public RenderTarget EndFrame()
    {
        // Draw all shapes and and perform clipping for the main RenderTarget.
        if (_fanFillDynamicBuilder != null)
        {
            _fanFillDynamicBuilder.Dispose();
            _fanFillDynamicBuilder = null;
            FanFill.Draw(RenderContext, _fanFillDynamicData);
        }
        if (_lineFillDynamicBuilder != null)
        {
            _lineFillDynamicBuilder.Dispose();
            _lineFillDynamicBuilder = null;
            LineFill.Draw(RenderContext, _lineFillDynamicData);
        }
        if (_strokeDynamicBuilder != null)
        {
            _strokeDynamicBuilder.Dispose();
            _strokeDynamicBuilder = null;
            Stroke.Draw(RenderContext, _strokeDynamicData);
        }
        RenderTarget.Clip(RenderContext);
        if (_clipDynamicBuilder != null)
        {
            _clipDynamicBuilder.Dispose();
            _clipDynamicBuilder = null;
            ClipZone.Draw(RenderContext, _clipDynamicData);
        }
        // Plumb the main RenderTarget to the full screen pass for alpha correction.
        FSPRenderTarget.Bind(RenderContext, RenderTarget);
        FSP.Draw(RenderContext);

        RenderContext.Execute();
        return FSPRenderTarget;
    }
    public void DrawFan(DisplayObjectFan fan, int segments)
    {
        if (fan.style.filled)
        {
            GetFanFills().Add(
                fan.origin,
                fan.innerRadius,
                fan.outerRadius,
                fan.angleMin,
                fan.angleMax,
                fan.style.originFillColor.ToVector4(),
                fan.style.endFillColor.ToVector4());
        }
        Fan.Stroke(GetStroke(), fan, segments);
    }
    private FanFill.Data.Builder GetFanFills() => _fanFillDynamicBuilder ??= _fanFillDynamicData.Map(RenderContext);

    private void DrawStrokeLine(Vector3 a, Vector3 b, DisplayStyle style)
    {
        DrawStroke([a, b], style.strokeThickness, style.strokeColor.ToVector4(), false);
    }

    public void DrawLine(DisplayObjectLine line)
    {
        if (line.radius == 0)
        {
            DrawStrokeLine(line.start, line.stop, line.style);
            if (line.startStyle == LineEnd.Arrow)
            {
                var arrowStart = line.start + 0.4f * line.Direction;
                var offset = 0.3f * line.Perpendicular;
                DrawStrokeLine(line.start, arrowStart + offset, line.style);
                DrawStrokeLine(line.start, arrowStart - offset, line.style);
            }

            if (line.endStyle == LineEnd.Arrow)
            {
                var arrowStart = line.stop - 0.4f * line.Direction;
                var offset = 0.3f * line.Perpendicular;
                DrawStrokeLine(line.stop, arrowStart + offset, line.style);
                DrawStrokeLine(line.stop, arrowStart - offset, line.style);
            }
        }
        else
        {
            if (line.style.filled)
            {
                GetLines().Add(
                line.start,
                line.stop,
                line.radius,
                line.style.originFillColor.ToVector4(),
                line.style.endFillColor.ToVector4());
            }
            var leftStart = line.start - line.PerpendicularRadius;
            var leftStop = line.stop - line.PerpendicularRadius;

            var rightStart = line.start + line.PerpendicularRadius;
            var rightStop = line.stop + line.PerpendicularRadius;

            DrawStroke(
                [leftStart, leftStop, rightStop, rightStart],
                line.style.strokeThickness,
                line.style.strokeColor.ToVector4(),
                true);
        }
    }
    private LineFill.Data.Builder GetLines() => _lineFillDynamicBuilder ??= _lineFillDynamicData.Map(RenderContext);

    public void DrawStroke(Vector3[] world, float thickness, Vector4 color, bool closed = false)
    {
        GetStroke().Add(world, thickness, color, closed);
    }
    private Stroke.Data.Builder GetStroke() => _strokeDynamicBuilder ??= _strokeDynamicData.Map(RenderContext);

    public void AddClipZone(Rectangle rect)
    {
        Vector2 upperleft = new(rect.X, rect.Y);
        Vector2 size = new(rect.Width, rect.Height);
        GetClipZones().Add(upperleft, size);
    }
    private ClipZone.Data.Builder GetClipZones() => _clipDynamicBuilder ??= _clipDynamicData.Map(RenderContext);

    private unsafe SharpDX.Matrix ReadMatrix(IntPtr address)
    {
        var p = (float*)address;
        SharpDX.Matrix mtx = new();
        for (var i = 0; i < 16; i++)
            mtx[i] = *p++;
        return mtx;
    }

    private unsafe SharpDX.Vector2 ReadVec2(IntPtr address)
    {
        var p = (float*)address;
        return new(p[0], p[1]);
    }
}
