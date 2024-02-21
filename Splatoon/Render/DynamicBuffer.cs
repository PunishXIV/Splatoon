using SharpDX;
using SharpDX.Direct3D11;
using System;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Splatoon.Render;

public unsafe class DynamicBuffer : IDisposable
{
    public class Builder : IDisposable
    {
        public int NextElement { get; private set; }
        private DeviceContext _ctx;
        private DynamicBuffer _buffer;
        private DataStream _stream;

        internal Builder(DeviceContext ctx, DynamicBuffer buffer)
        {
            _ctx = ctx;
            _buffer = buffer;
            ctx.MapSubresource(buffer.Buffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out _stream);
        }

        public void Dispose()
        {
            _ctx.UnmapSubresource(_buffer.Buffer, 0);
        }

        // TODO: reconsider this api
        public DataStream Stream => _stream;
        public void Advance(int count)
        {
            NextElement += count;
            if (NextElement > _buffer.NumElements)
                throw new ArgumentOutOfRangeException("Buffer overflow");
        }
    }

    public int ElementSize { get; init; }
    public int NumElements { get; init; }
    public Buffer Buffer { get; init; }

    public DynamicBuffer(SharpDX.Direct3D11.Device device, int elementSize, int numElements, BindFlags bindFlags)
    {
        ElementSize = elementSize;
        NumElements = numElements;
        Buffer = new(device, new()
        {
            SizeInBytes = elementSize * numElements,
            Usage = ResourceUsage.Dynamic,
            BindFlags = bindFlags,
            CpuAccessFlags = CpuAccessFlags.Write,
        });
    }

    public void Dispose()
    {
        Buffer.Dispose();
    }

    public Builder Map(DeviceContext ctx) => new Builder(ctx, this);
}
