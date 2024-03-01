using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Splatoon.Serializables;
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
    private UnorderedAccessView _unorderedAccessView;
    private BlendState _defaultBlendState;
    private BlendState _clipBlendState;

    public nint ImguiHandle => _rtSRV.NativePointer;

    public RenderTarget(RenderContext ctx, int width, int height, AlphaBlendMode blendMode)
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
        if (blendMode != AlphaBlendMode.None)
        {
            blendDescription.RenderTarget[0].IsBlendEnabled = true;
            blendDescription.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            blendDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            blendDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blendDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            blendDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
            if (blendMode == AlphaBlendMode.Add)
            {
                blendDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            }
            else if (blendMode == AlphaBlendMode.Max)
            {
                blendDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Maximum;
            }
            blendDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
        }
        _defaultBlendState = new(ctx.Device, blendDescription);

        var blendDescription2 = BlendStateDescription.Default();
        blendDescription2.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
        blendDescription2.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
        blendDescription2.RenderTarget[0].AlphaBlendOperation = BlendOperation.Maximum;
        blendDescription2.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.Alpha;
        _clipBlendState = new(ctx.Device, blendDescription2);
    }

    public void Dispose()
    {
        _rt.Dispose();
        _renderTargetView.Dispose();
        _rtSRV.Dispose();
        _defaultBlendState.Dispose();
    }
    public void Bind(RenderContext ctx, RenderTarget r = null)
    {
        ctx.Context.ClearRenderTargetView(_renderTargetView, new());
        ctx.Context.Rasterizer.SetViewport(0, 0, Size.X, Size.Y);
        ctx.Context.OutputMerger.SetBlendState(_defaultBlendState);
        ctx.Context.OutputMerger.SetTargets(_renderTargetView);
        if (r != null) ctx.Context.PixelShader.SetShaderResource(0, r._rtSRV);
    }
    public void Clip(RenderContext ctx)
    {
        ctx.Context.OutputMerger.SetBlendState(_clipBlendState);
    }

    public void PostProcess(RenderContext ctx)
    {
        ctx.Context.OutputMerger.SetBlendState(_defaultBlendState);
        ctx.Context.ClearUnorderedAccessView(_unorderedAccessView, new SharpDX.Mathematics.Interop.RawInt4());
        ctx.Context.OutputMerger.SetUnorderedAccessView(0, _unorderedAccessView);
    }
}
