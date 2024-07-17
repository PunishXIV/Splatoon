using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Runtime.InteropServices;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Splatoon.Render;

public class LineFill : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Constants
    {
        public Matrix ViewProj;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Instance
    {
        public Vector3 World;
        public float Radius;
        public Vector4 Color;
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
            public void Add(Vector3 start, Vector3 stop, float radius, Vector4 colorOrigin, Vector4 colorEnd)
            {
                _lines.Add(new Instance()
                {
                    World = start,
                    Radius = radius,
                    Color = colorOrigin,
                });
                _lines.Add(new Instance()
                {
                    World = stop,
                    Radius = radius,
                    Color = colorEnd,
                });
            }
        }

        private RenderBuffer<Instance> _buffer;

        public Data(RenderContext ctx, int maxCount, bool dynamic)
        {
            _buffer = new("Line", ctx, maxCount, BindFlags.VertexBuffer, dynamic);
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }

        public Builder Map(RenderContext ctx) => new(ctx, this);

        // Draw* should be called after LineFill.Bind set up its state
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

    public LineFill(RenderContext ctx)
    {
        var shader = """
            struct Line
            {
                float3 world : World;
                float radius : Radius;
                float4 color : Color;
            };

            struct GSOutput
            {
                float4 projPos : SV_Position;
                float4 color : Color;
            };

            struct Constants
            {
                float4x4 viewProj;
            };
            Constants k : register(c0);

            Line vs(Line v)
            {
                return v;
            }

            [maxvertexcount(4)]
            void gs(line Line input[2], inout TriangleStream<GSOutput> output)
            {
                Line start = input[0];
                Line stop = input[1];
            
                float3 perpendicular = normalize(cross(stop.world - start.world, float3(0,1,0)));

                GSOutput v;
                v.color = start.color;
                v.projPos = mul(float4( start.world + perpendicular * start.radius, 1), k.viewProj);
                output.Append(v);
                v.projPos = mul(float4( start.world - perpendicular * start.radius, 1), k.viewProj);
                output.Append(v);
            
                v.color = stop.color;
                v.projPos = mul(float4( stop.world + perpendicular * stop.radius, 1), k.viewProj);
                output.Append(v);
                v.projPos = mul(float4( stop.world - perpendicular * stop.radius, 1), k.viewProj);
                output.Append(v);
            }

            float4 ps(GSOutput input) : SV_Target
            {
                return input.color;
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

        _constantBuffer = new(ctx.Device, 16 * 4, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
        _il = new(ctx.Device, vs.Bytecode,
        [
            new InputElement("World", 0, Format.R32G32B32_Float, -1, 0),
            new InputElement("Radius", 0, Format.R32_Float, -1, 0),
            new InputElement("Color", 0, Format.R32G32B32A32_Float, -1, 0),
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
        consts.ViewProj.Transpose();
        ctx.Context.UpdateSubresource(ref consts, _constantBuffer);
    }

    public void Bind(RenderContext ctx)
    {
        ctx.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
        ctx.Context.InputAssembler.InputLayout = _il;
        ctx.Context.VertexShader.Set(_vs);
        ctx.Context.GeometryShader.Set(_gs);
        ctx.Context.GeometryShader.SetConstantBuffer(0, _constantBuffer);
        ctx.Context.PixelShader.Set(_ps);
    }

    public void Draw(RenderContext ctx, Data data)
    {
        Bind(ctx);
        data.DrawAll(ctx);
    }
}
