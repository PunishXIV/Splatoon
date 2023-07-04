using ECommons;
using Splatoon.VFX.Items.Donut;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Splatoon.VFX.Loader.VFXMemory;

namespace Splatoon.VFX.Interfaces
{
    internal unsafe abstract class ItemController<T, Descriptor> where T : unmanaged where Descriptor : ItemDescriptor<T>
    {
        internal abstract string FileName { get; }
        internal T* Reference = null;
        internal long Length;
        internal Dictionary<Descriptor, string> ExistingPathes = new();
        internal Dictionary<Descriptor, IntPtr> LoadedVFX = new();
        internal List<Descriptor> RequestedDescriptors = new();

        internal ItemController()
        {
            if (Reference != null) throw new Exception("Already initialized");
            var file = new FileInfo(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", FileName));
            Length = file.Length;
            Reference = (T*)Marshal.AllocHGlobal((nint)Length);
            using var stream = file.Open(FileMode.Open);
            using var ustream = new UnmanagedMemoryStream((byte*)Reference, 0, file.Length, FileAccess.ReadWrite);
            stream.CopyTo(ustream);
        }

        internal void LoadIfNotLoaded(Descriptor d)
        {
            Get(d);
            if (!LoadedVFX.ContainsKey(d))
            {
                LoadedVFX[d] = (nint)P.VFXManager.Memory.SpawnStatic("bg/ffxiv/fst_f1/common/vfx/eff/b0941trp1a_o.avfx", d.Position, Quaternion.Zero);
            }
            RequestedDescriptors.Add(d);
        }

        internal void UnloadNotRequested()
        {
            foreach(var x in LoadedVFX.Keys)
            {
                if (!RequestedDescriptors.Contains(x))
                {
                    var d = LoadedVFX[x];
                    if (d != IntPtr.Zero)
                    {
                        P.VFXManager.Memory.RemoveStatic((VfxStruct*)d);
                    }
                    LoadedVFX.Remove(x);
                }
            }
            RequestedDescriptors.Clear();
        }

        internal void Unload(Descriptor d)
        {
            if(LoadedVFX.TryGetValue(d, out var x))
            {
                if(x != IntPtr.Zero)
                {
                    P.VFXManager.Memory.RemoveStatic((VfxStruct*)x);
                }
                LoadedVFX.Remove(d);
            }
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
                DuoLog.Warning($"ItemController already disposed");
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
