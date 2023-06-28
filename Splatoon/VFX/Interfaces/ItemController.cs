using Splatoon.VFX.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.VFX
{
    internal unsafe abstract class ItemController<T, Descriptor> where T : unmanaged where Descriptor : IItemDescriptor<T>
    {
        internal abstract string FileName { get; }
        internal T* Reference = null;
        internal long Length;
        internal Dictionary<Descriptor, string> ExistingPathes = new();

        internal void Init()
        {
            if (Reference != null) throw new Exception("Already initialized");
            var file = new FileInfo(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", FileName));
            Length = file.Length;
            Reference = (T*)Marshal.AllocHGlobal((nint)Length);
            using var stream = file.Open(FileMode.Open);
            using var ustream = new UnmanagedMemoryStream((byte*)Reference, 0, file.Length, FileAccess.ReadWrite);
            stream.CopyTo(ustream);
        }

        internal void Dispose()
        {
            if (Reference != null)
            {
                Marshal.FreeHGlobal((nint)Reference);
                ExistingPathes = null;
            }
            else
            {
                DuoLog.Warning($"{nameof(FXDonut)} already disposed");
            }
        }

        internal string Get(Descriptor d)
        {
            if (ExistingPathes.TryGetValue(d, out var path)) return path;
            var outname = Path.Combine(P.VFXManager.TempDir, d.Name);
            if (File.Exists(d.Name))
            {
                ExistingPathes[d] = d.Name;
                return d.Name;
            }
            d.Transform(Reference);
            using var ustream = new UnmanagedMemoryStream((byte*)Reference, Length);
            using var ostream = new FileInfo(outname).Open(FileMode.OpenOrCreate, FileAccess.ReadWrite);
            ustream.CopyTo(ostream);
            ExistingPathes[d] = d.Name;
            return d.Name;
        }
    }
}
