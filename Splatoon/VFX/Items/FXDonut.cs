using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.VFX.Items
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct FXDonut
    {
        internal const string FileName = "fxdonut.avfx";
        [FieldOffset(0x1F38)] internal Vector3 Color;

        internal static string Get(uint col)
        {
            var name = $"donut_c{col:X8}.avfx";
            var tempDir = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "fxtemp");
            if(!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
            var outname = Path.Combine(tempDir, name);
            if (File.Exists(name)) return name;
            var file = new FileInfo(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", FileName));
            var mem = (FXDonut*)Marshal.AllocHGlobal((nint)file.Length);
            using var stream = file.Open(FileMode.Open);
            using var ustream = new UnmanagedMemoryStream((byte*)mem, 0, file.Length, FileAccess.ReadWrite);
            stream.CopyTo(ustream);
            var v4 = col.ToVector4();
            mem->Color = new(v4.X, v4.Y, v4.Z);
            using var ostream = new FileInfo(outname).Open(FileMode.OpenOrCreate, FileAccess.ReadWrite);
            ustream.Position = 0;
            ustream.CopyTo(ostream);
            Marshal.FreeHGlobal((nint)mem);
            return name;
        }
    }
}
