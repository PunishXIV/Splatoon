using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX;
using System.Runtime.InteropServices;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Splatoon.Render;

public class FanFill : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Constants
    {
        public Matrix ViewProj;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Instance
    {
        public Vector3 Origin;
        public float InnerRadius;
        public float OuterRadius;
        public float MinAngle;
        public float MaxAngle;
        public Vector4 ColorOrigin;
        public Vector4 ColorEnd;
    }

    public class Data : IDisposable
    {
        public class Builder : IDisposable
        {
            private RenderBuffer<Instance>.Builder _circles;

            internal Builder(RenderContext ctx, Data data)
            {
                _circles = data._buffer.Map(ctx);
            }

            public void Dispose()
            {
                _circles.Dispose();
            }

            public void Add(ref Instance inst) => _circles.Add(ref inst);
            public void Add(Vector3 world, float innerRadius, float outerRadius, float minAngle, float maxAngle, Vector4 colorOrigin, Vector4 colorEnd) =>
                _circles.Add(new Instance()
                {
                    Origin = world,
                    InnerRadius = innerRadius,
                    OuterRadius = outerRadius,
                    MinAngle = minAngle,
                    MaxAngle = maxAngle,
                    ColorOrigin = colorOrigin,
                    ColorEnd = colorEnd
                });
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

        // Draw* should be called after FanFill.Bind set up its state
        public void DrawSubset(RenderContext ctx, int firstCircle, int numCircles)
        {
            ctx.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_buffer.Buffer, _buffer.ElementSize, 0));
            ctx.Context.Draw(numCircles, firstCircle);
        }

        public void DrawAll(RenderContext ctx) => DrawSubset(ctx, 0, _buffer.CurElements);
    }

    private SharpDX.Direct3D11.Buffer _constantBuffer;
    private InputLayout _il;
    private VertexShader _vs;
    private GeometryShader _gs;
    private PixelShader _ps;

    public FanFill(RenderContext ctx)
    {
        var shader = """
            #define PI 3.14159265359f
            struct Circle
            {
                float3 origin : World;
                float innerRadius : Radius0;
                float outerRadius : Radius1;
                float minAngle : Angle0;
                float maxAngle : Angle1;
                float4 colorOrigin : Color0;
                float4 colorEnd : Color1;
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

            Circle vs(Circle v)
            {
                return v;
            }

            [maxvertexcount(128)]
            void gs(point Circle input[1], inout TriangleStream<GSOutput> output)
            {
                int segments = 63;
                Circle circle = input[0];
                float3 center = circle.origin;

                GSOutput v;
                float totalAngle = circle.maxAngle - circle.minAngle;
                float angleStep = totalAngle / segments;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = PI / 2 + circle.minAngle + i * angleStep;
                    float3 offset = float3(cos(angle), 0, sin(angle));

                    v.color = circle.colorOrigin;
                    v.projPos = mul(float4(center + circle.innerRadius * offset, 1), k.viewProj);
                    output.Append(v);

                    v.color = circle.colorEnd;
                    v.projPos = mul(float4(center + circle.outerRadius * offset, 1), k.viewProj);
                    output.Append(v);
                }
            }

            float4 ps(GSOutput input) : SV_Target
            {
                return input.color;
            }
            """;

        var vs = ShaderBytecode.Compile(shader, "vs", "vs_5_0");
        Svc.Log.Debug($"Circle VS compile: {vs.Message}");
        _vs = new(ctx.Device, vs.Bytecode);

        var gs = ShaderBytecode.Compile(shader, "gs", "gs_5_0");
        Svc.Log.Debug($"Circle GS compile: {gs.Message}");
        _gs = new(ctx.Device, gs.Bytecode);

        var ps = ShaderBytecode.Compile(shader, "ps", "ps_5_0");
        Svc.Log.Debug($"Circle PS compile: {ps.Message}");
        _ps = new(ctx.Device, ps.Bytecode);

        _constantBuffer = new(ctx.Device, 16 * 4, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
        _il = new(ctx.Device, vs.Bytecode,
        [
            new InputElement("World", 0, Format.R32G32B32_Float, -1, 0),
            new InputElement("Radius", 0, Format.R32_Float, -1, 0),
            new InputElement("Radius", 1, Format.R32_Float, -1, 0),
            new InputElement("Angle", 0, Format.R32_Float, -1, 0),
            new InputElement("Angle", 1, Format.R32_Float, -1, 0),
            new InputElement("Color", 0, Format.R32G32B32A32_Float, -1, 0),
            new InputElement("Color", 1, Format.R32G32B32A32_Float, -1, 0),
            new InputElement("Color", 2, Format.R32G32B32A32_Float, -1, 0),
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
        ctx.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
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
