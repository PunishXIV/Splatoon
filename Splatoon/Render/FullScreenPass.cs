using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System.Runtime.InteropServices;

namespace Splatoon.Render;

/**
 * Full screen pass shader for correcting alpha multiplication.
 * 
 * The main RenderTarget uses alpha blending when rendering.
 * Imgui uses alpha blending when rendering the main RenderTarget's output.
 * The result of both of these combined is the color is multiplied by the
 * alpha twice which results in an output that is too dark.
 * 
 * This shader corrects for this extra multiplication by dividing the main
 * RenderTarget's output by the alpha. This second RenderTarget is then used
 * for Imgui rendering.
 */
public class FullScreenPass : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Constants
    {
        public float MaxAlpha;
    }

    private SharpDX.Direct3D11.Buffer _constantBuffer;
    private VertexShader _vs;
    private PixelShader _ps;
    public FullScreenPass(RenderContext ctx)
    {
        var shader = """
            struct Constants
            {
                float maxAlpha;
            };
            Constants k : register(b0);

            Texture2D<float4> inputTexture : register(t0);
            SamplerState TextureSampler
            {
                Filter = MIN_MAG_MIP_POINT;
                AddressU = CLAMP;
                AddressV = CLAMP;
            };

            struct VSOutput
            {
                float4 pos : SV_POSITION;
                float2 uv: TEXCOORD;
            };
            
            VSOutput vs(uint id : SV_VertexID)
            {
                VSOutput output;
            	float2 uv = float2((id << 1) & 2, id & 2);
            	output.pos = float4(uv * float2(2, -2) + float2(-1, 1), 0, 1);
                output.uv = uv;
                return output;
            }

            float4 ps(VSOutput input) : SV_Target
            {
                float4 color = inputTexture.Sample(TextureSampler, input.uv);
                if (color.a > 0)
                {
                    color.rbg /= color.a;
                }
                color.a = min(color.a, k.maxAlpha);
                return color;
            }
            """;

        var vs = ShaderBytecode.Compile(shader, "vs", "vs_5_0");
        Svc.Log.Debug($"FSP VS compile: {vs.Message}");
        _vs = new(ctx.Device, vs.Bytecode);

        var ps = ShaderBytecode.Compile(shader, "ps", "ps_5_0");
        Svc.Log.Debug($"FSP PS compile: {ps.Message}");
        _ps = new(ctx.Device, ps.Bytecode);

        _constantBuffer = new(ctx.Device, 16, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
    }

    public void Dispose()
    {
        _constantBuffer.Dispose();
        _vs.Dispose();
        _ps.Dispose();
    }

    public void UpdateConstants(RenderContext ctx, Constants consts)
    {
        ctx.Context.UpdateSubresource(ref consts, _constantBuffer);
    }

    public void Bind(RenderContext ctx)
    {
        ctx.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        ctx.Context.VertexShader.Set(_vs);
        ctx.Context.PixelShader.Set(_ps);
        ctx.Context.PixelShader.SetConstantBuffer(0, _constantBuffer);
        ctx.Context.GeometryShader.Set(null);
    }

    public void Draw(RenderContext ctx)
    {
        Bind(ctx);
        ctx.Context.Draw(3, 0);
    }
}
