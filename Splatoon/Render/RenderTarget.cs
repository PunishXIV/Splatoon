using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Format = SharpDX.DXGI.Format;
using Vector2 = SharpDX.Vector2;

namespace Splatoon.Render;

// render target texture with utilities to render to self
public unsafe class RenderTarget : IDisposable
{
    public Vector2 Size { get; private set; }
    private Texture2D _rt;
    private RenderTargetView _renderTargetView;
    private ShaderResourceView _rtSRV;
    private BlendState _blendState;

    public nint ImguiHandle => _rtSRV.NativePointer;

    public RenderTarget(RenderContext ctx, int width, int height)
    {
        Size = new(width, height);

        _rt = new(ctx.Device, new()
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        });

        _renderTargetView = new(ctx.Device, _rt, new()
        {
            Format = Format.R8G8B8A8_UNorm,
            Dimension = RenderTargetViewDimension.Texture2D,
            Texture2D = new() { }
        });

        _rtSRV = new(ctx.Device, _rt, new()
        {
            Format = Format.R8G8B8A8_UNorm,
            Dimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new()
            {
                MostDetailedMip = 0,
                MipLevels = 1
            }
        });

        var blendDescription = BlendStateDescription.Default();
        blendDescription.RenderTarget[0].IsBlendEnabled = true;
        blendDescription.RenderTarget[0].SourceBlend = BlendOption.One;
        blendDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
        blendDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
        blendDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
        blendDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
        blendDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Maximum;
        blendDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
        _blendState = new(ctx.Device, blendDescription);
    }

    public void Dispose()
    {
        _rt.Dispose();
        _renderTargetView.Dispose();
        _rtSRV.Dispose();
        _blendState.Dispose();
    }
    public void Bind(RenderContext ctx)
    {
        ctx.Context.ClearRenderTargetView(_renderTargetView, new());
        ctx.Context.Rasterizer.SetViewport(0, 0, Size.X, Size.Y);
        ctx.Context.OutputMerger.SetBlendState(_blendState);
        ctx.Context.OutputMerger.SetTargets(_renderTargetView);
    }
}
