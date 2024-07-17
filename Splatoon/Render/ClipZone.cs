using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Runtime.InteropServices;
using Vector2 = System.Numerics.Vector2;

namespace Splatoon.Render;

public class ClipZone : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Constants
    {
        public Vector2 RenderTargetSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Instance
    {
        public Vector2 Screen;
        public Vector2 Size;
    }

    public class Data : IDisposable
    {
        public class Builder : IDisposable
        {
            private RenderBuffer<Instance>.Builder _lines;

            internal Builder(RenderContext ctx, Data data)
            {
                _lines = data._buffer.Map(ctx);
            }

            public void Dispose()
            {
                _lines.Dispose();
            }

            public void Add(ref Instance inst) => _lines.Add(ref inst);
            public void Add(Vector2 screen, Vector2 size)
            {
                _lines.Add(new Instance()
                {
                    Screen = screen,
                    Size = size
                });
            }
        }

        private RenderBuffer<Instance> _buffer;

        public Data(RenderContext ctx, int maxCount, bool dynamic)
        {
            _buffer = new("ClipZone", ctx, maxCount, BindFlags.VertexBuffer, dynamic);
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }

        public Builder Map(RenderContext ctx) => new(ctx, this);

        // Draw* should be called after ClipZone.Bind set up its state
        public void DrawSubset(RenderContext ctx, int firstLine, int numLines)
        {
            ctx.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_buffer.Buffer, _buffer.ElementSize, 0));
            ctx.Context.Draw(numLines, firstLine);
        }

        public void DrawAll(RenderContext ctx) => DrawSubset(ctx, 0, _buffer.CurElements);
    }

    private SharpDX.Direct3D11.Buffer _constantBuffer;
    private InputLayout _il;
    private VertexShader _vs;
    private GeometryShader _gs;
    private PixelShader _ps;

    public ClipZone(RenderContext ctx)
    {
        var shader = """
            struct Constants
            {
                float2 renderTargetSize;
            };
            Constants k : register(c0);

            struct ClipZone
            {
                float2 screen : ScreenPos;
                float2 size : Size;
            };

            ClipZone vs(ClipZone z)
            {
                z.screen.x *= 2 / k.renderTargetSize.x;
                z.screen.y *= 2 / -k.renderTargetSize.y;
                z.screen += float2(-1, 1);

                z.size.x *= 2 / k.renderTargetSize.x;
                z.size.y *= 2 / -k.renderTargetSize.y;
            
                return z;
            }

            struct GSOutput
            {
                float4 pos : SV_Position;
            };

            [maxvertexcount(4)]
            void gs(point ClipZone input[1], inout TriangleStream<GSOutput> output)
            {
                ClipZone zone = input[0];

                GSOutput v;
                v.pos = float4(zone.screen, 0, 1);
                output.Append(v);
                v.pos = float4(zone.screen + float2(zone.size.x, 0), 0, 1);
                output.Append(v);
                v.pos = float4(zone.screen + float2(0, zone.size.y), 0, 1);
                output.Append(v);
                v.pos = float4(zone.screen + zone.size, 0, 1);
                output.Append(v);
            }

            float4 ps(GSOutput input) : SV_Target
            {
                // Alpha 0 is the important bit here.
                return float4(0,0,0,0);
            }
            """;

        var vs = ShaderBytecode.Compile(shader, "vs", "vs_5_0");
        Svc.Log.Debug($"Line VS compile: {vs.Message}");
        _vs = new(ctx.Device, vs.Bytecode);

        var gs = ShaderBytecode.Compile(shader, "gs", "gs_5_0");
        Svc.Log.Debug($"Line GS compile: {gs.Message}");
        _gs = new(ctx.Device, gs.Bytecode);

        var ps = ShaderBytecode.Compile(shader, "ps", "ps_5_0");
        Svc.Log.Debug($"Line PS compile: {ps.Message}");
        _ps = new(ctx.Device, ps.Bytecode);

        _constantBuffer = new(ctx.Device, 16 * 4 * 2, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
        _il = new(ctx.Device, vs.Bytecode,
        [
            new InputElement("ScreenPos", 0, Format.R32G32_Float, -1, 0),
            new InputElement("Size", 0, Format.R32G32_Float, -1, 0),
        ]);
    }

    public void Dispose()
    {
        _constantBuffer.Dispose();
        _il.Dispose();
        _vs.Dispose();
        _gs.Dispose();
        _ps.Dispose();
    }

    public void UpdateConstants(RenderContext ctx, Constants consts)
    {
        ctx.Context.UpdateSubresource(ref consts, _constantBuffer);
    }

    public void Bind(RenderContext ctx)
    {
        ctx.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
        ctx.Context.InputAssembler.InputLayout = _il;
        ctx.Context.VertexShader.Set(_vs);
        ctx.Context.VertexShader.SetConstantBuffer(0, _constantBuffer);
        ctx.Context.GeometryShader.Set(_gs);
        ctx.Context.PixelShader.Set(_ps);
    }

    public void Draw(RenderContext ctx, Data data)
    {
        Bind(ctx);
        data.DrawAll(ctx);
    }
}
