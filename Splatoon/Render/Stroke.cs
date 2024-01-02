using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Runtime.InteropServices;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Splatoon.Render;

public class Stroke : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Constants
    {
        public Matrix ViewProj;
        public Vector2 RenderTargetSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Instance
    {
        public Vector3 World;
        public float Thickness;
        public Vector4 Color;
        public ushort Index;
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
            public void Add(Vector3[] world, float thickness, Vector4 color, bool closed)
            {
                for (int i = 0; i < world.Length; i++)
                {
                    _lines.Add(new Instance()
                    {
                        World = world[i],
                        Thickness = thickness,
                        Color = color,
                        Index = (ushort) (i+1)
                    });
                }
                if (closed)
                {
                    _lines.Add(new Instance()
                    {
                        World = world[0],
                        Thickness = thickness,
                        Color = color,
                        Index = (ushort)world.Length
                    });
                }
            }
        }

        private RenderBuffer<Instance> _buffer;

        public Data(RenderContext ctx, int maxCount, bool dynamic)
        {
            _buffer = new(ctx, maxCount, BindFlags.VertexBuffer, dynamic);
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }

        public Builder Map(RenderContext ctx) => new(ctx, this);

        // Draw* should be called after Stroke.Bind set up its state
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

    public Stroke(RenderContext ctx)
    {
        var shader = """
            struct Constants
            {
                float4x4 viewProj;
                float2 renderTargetSize;
            };
            Constants k : register(c0);

            struct Line
            {
                float3 world : World;
                float thickness : Thickness;
                float4 color : Color;
                min16uint index : Index;
            };

            struct VSOutput
            {
                float4 projPos : SV_Position;
                float thickness : Thickness;
                float4 color : Color;
                min16uint index : Index;
            };

            struct GSOutput
            {
                float4 projPos : SV_Position;
                float4 color : Color;
            };

            VSOutput vs(Line l)
            {
                VSOutput v;

                v.thickness = l.thickness;
                v.color = l.color;
                v.index = l.index;

                v.projPos = mul(float4(l.world, 1), k.viewProj);
                return v;
            }

            float4 get_normal(float4 p0, float4 p1)
            {
                float3 dir = normalize(p1 - p0);

                float3 ratio = float3(k.renderTargetSize.y, k.renderTargetSize.x, 0);
                ratio = normalize(ratio);
            
                float3 unit_z = normalize(float3(0, 0, -1));
            
                float3 normal = normalize(cross(unit_z, dir) * ratio);
            
                return float4(normal * ratio, 0);
            }

            float unscale(inout float4 v)
            {
                float scale = v.w;
                v /= v.w;
                return scale;
            }

            [maxvertexcount(4)]
            void gs(line VSOutput input[2], inout TriangleStream<GSOutput> output)
            {
                VSOutput start = input[0];
                VSOutput stop = input[1];
            
                if (start.index > stop.index) {
                    return;
                }

                float thickness = start.thickness / k.renderTargetSize.y;
            
                float4 p0 = start.projPos;
                float w0 = unscale(p0);
                float4 p1 = stop.projPos;
                float w1 = unscale(p1);

                float4 normal = get_normal(p0, p1);

                normal.xy *= thickness;

                GSOutput v;
            
                v.color = start.color;
                v.projPos = w0 * (p0 + normal);
                output.Append(v);
                v.projPos = w0 * (p0 - normal);
                output.Append(v);

                v.color = stop.color;
                v.projPos = w1 * (p1 + normal);
                output.Append(v);
                v.projPos = w1 * (p1 - normal);
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


        _constantBuffer = new(ctx.Device, 16 * 4 * 2, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
        _il = new(ctx.Device, vs.Bytecode,
        [
            new InputElement("World", 0, Format.R32G32B32_Float, -1, 0),
            new InputElement("Thickness", 0, Format.R32_Float, -1, 0),
            new InputElement("Color", 0, Format.R32G32B32A32_Float, -1, 0),
            new InputElement("Index", 0, Format.R16_UInt, -1, 0),
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
        ctx.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineStrip;
        ctx.Context.InputAssembler.InputLayout = _il;
        ctx.Context.VertexShader.Set(_vs);
        ctx.Context.VertexShader.SetConstantBuffer(0, _constantBuffer);
        ctx.Context.GeometryShader.Set(_gs);
        ctx.Context.GeometryShader.SetConstantBuffer(0, _constantBuffer);
        ctx.Context.PixelShader.Set(_ps);
    }

    // shortcut to bind + draw
    public void Draw(RenderContext ctx, Data data)
    {
        Bind(ctx);
        data.DrawAll(ctx);
    }
}
