using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.VFX.Loader
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct SeFileDescriptor
    {
        [FieldOffset(0x00)]
        public SeFileMode FileMode;

        [FieldOffset(0x30)]
        public void* FileDescriptor;

        [FieldOffset(0x50)]
        public ResourceHandle* ResourceHandle;

        [FieldOffset(0x70)]
        public char Utf16FileName;
    }
}
