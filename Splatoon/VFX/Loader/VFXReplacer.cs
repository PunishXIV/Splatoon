using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.VFX.Loader
{
    internal unsafe class VFXReplacer : IDisposable
    {
        private delegate byte ReadSqPackDelegate(void* resourceManager, SeFileDescriptor* pFileDesc, int priority, bool isSync);

        [Signature("E8 ?? ?? ?? ?? EB 05 E8 ?? ?? ?? ?? 84 C0 0F 84 ?? 00 00 00 4C 8B C3", DetourName = nameof(ReadSqPackDetour))]
        private Hook<ReadSqPackDelegate> _readSqPackHook;

        [Signature("E8 ?? ?? ?? ?? 84 C0 0F 84 ?? 00 00 00 4C 8B C3 BA 05")]
        private delegate* unmanaged<void*, SeFileDescriptor*, int, bool, byte> _readFile;

        internal volatile bool Operation = true;

        internal VFXReplacer()
        {
            SignatureHelper.Initialise(this);

            this._readSqPackHook!.Enable();
        }

        public void Dispose()
        {
            this._readSqPackHook.Dispose();
        }

        private byte ReadSqPackDetour(void* resourceManager, SeFileDescriptor* fileDescriptor, int priority, bool isSync)
        {
            if (!Operation)
            {
                goto Original;
            }

            VFXManager.Debug($"Operation mode detected");

            if (fileDescriptor == null || fileDescriptor->ResourceHandle == null)
            {
                goto Original;
            }

            var path = fileDescriptor->ResourceHandle->FileName.ToString();
            VFXManager.Debug($"Path is {path}");
            if (path != "bg/ffxiv/fst_f1/common/vfx/eff/b0941trp1a_o.avfx" && !P.VFXManager.AllExistingPathes().Contains(path))
            {
                VFXManager.Debug($"{path} is not present in list");
                goto Original;
            }

            VFXManager.Debug($"{path} IS present in list");

            var newPath = Path.Combine(P.VFXManager.TempDir, path);
            newPath = @"\Splatoon\fxtemp\d3F800000FF0000FF4040000040A00000.avfx";
            VFXManager.Debug($"new path is {newPath}");
            return this.DefaultRootedResourceLoad(newPath, resourceManager, fileDescriptor, priority, isSync);

        Original:
            return this._readSqPackHook.Original(resourceManager, fileDescriptor, priority, isSync);
        }

        // Load the resource from a path on the users hard drives.
        private byte DefaultRootedResourceLoad(string gamePath, void* resourceManager, SeFileDescriptor* fileDescriptor, int priority, bool isSync)
        {
            // Specify that we are loading unpacked files from the drive.
            // We need to copy the actual file path in UTF16 (Windows-Unicode) on two locations,
            // but since we only allow ASCII in the game paths, this is just a matter of upcasting.
            fileDescriptor->FileMode = SeFileMode.LoadUnpackedResource;

            var fd = stackalloc byte[0x20 + 2 * gamePath.Length + 0x16];
            fileDescriptor->FileDescriptor = fd;
            var fdPtr = (char*)(fd + 0x21);
            for (var i = 0; i < gamePath.Length; ++i)
            {
                (&fileDescriptor->Utf16FileName)[i] = gamePath[i];
                fdPtr[i] = gamePath[i];
            }

            (&fileDescriptor->Utf16FileName)[gamePath.Length] = '\0';
            fdPtr[gamePath.Length] = '\0';

            // Use the SE ReadFile function.
            var ret = this._readFile(resourceManager, fileDescriptor, priority, isSync);
            return ret;
        }
    }
}
