using Splatoon.Structures;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Splatoon.Render;

public unsafe class Renderer : IDisposable
{
    public RenderContext RenderContext { get; init; } = new();

    public RenderTarget? RenderTarget { get; private set; }
    public FanFill FanFill { get; init; }
    public LineFill LineFill { get; init; }
    public Stroke Stroke { get; init; }
    public ClipZone ClipZone { get; init; }

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
    public Renderer()
    {
        FanFill = new(RenderContext);
        _fanFillDynamicData = new(RenderContext, 512, true);
        LineFill = new(RenderContext);
        _lineFillDynamicData = new(RenderContext, 512, true);
        Stroke = new(RenderContext);
        _strokeDynamicData = new(RenderContext, 4096, true);
        ClipZone = new(RenderContext);
        _clipDynamicData = new(RenderContext, 2 * MAX_CLIP_ZONES, true);
        // https://github.com/goatcorp/Dalamud/blob/d52118b3ad366a61216129c80c0fa250c885abac/Dalamud/Game/Gui/GameGuiAddressResolver.cs#L69
        _engineCoreSingleton = Marshal.GetDelegateForFunctionPointer<GetEngineCoreSingletonDelegate>(Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4C 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??"))();
    }

    public void Dispose()
    {
        RenderTarget?.Dispose();
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

        if (RenderTarget == null || RenderTarget.Size != ViewportSize)
        {
            RenderTarget?.Dispose();
            RenderTarget = new(RenderContext, (int)ViewportSize.X, (int)ViewportSize.Y);
        }

        RenderTarget.Bind(RenderContext);
    }

    public RenderTarget EndFrame()
    {
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
        RenderContext.Execute();
        return RenderTarget;
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

    public void DrawLine(DisplayObjectLine line)
    {
        if (line.radius == 0)
        {
            DrawStroke(
                [line.start, line.stop],
                line.style.strokeThickness,
                line.style.strokeColor.ToVector4(),
                false);
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
