using Dalamud.Utility.Signatures;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.VFX.Loader
{
    internal unsafe class VFXMemory : IDisposable
    {
        private static readonly byte[] Pool = Encoding.UTF8.GetBytes("Client.System.Scheduler.Instance.VfxObject");

        [Signature("E8 ?? ?? ?? ?? F3 0F 10 35 ?? ?? ?? ?? 48 89 43 08")]
        private delegate* unmanaged<byte*, byte*, VfxStruct*> _staticVfxCreate;

        [Signature("E8 ?? ?? ?? ?? 8B 4B 7C 85 C9")]
        private delegate* unmanaged<VfxStruct*, float, uint, ulong> _staticVfxRun;

        [Signature("40 53 48 83 EC 20 48 8B D9 48 8B 89 ?? ?? ?? ?? 48 85 C9 74 28 33 D2 E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9")]
        private delegate* unmanaged<VfxStruct*, void> _staticVfxRemove;

        internal List<nint> Spawned = new();

        internal VFXMemory()
        {
            SignatureHelper.Initialise(this);
        }

        public void Dispose()
        {
            this.RemoveAll();
        }

        internal void RemoveAll()
        {
            foreach (var spawned in this.Spawned.ToArray())
            {
                this.RemoveStatic((VfxStruct*)spawned);
            }

            this.Spawned.Clear();
        }

        internal void Run(VfxStruct* vfx)
        {
            this._staticVfxRun(vfx, 0.0f, 0xFFFFFFFF);
        }

        internal VfxStruct* SpawnStatic(string path, Vector3 pos, Quaternion rotation)
        {
            VFXManager.Debug($"Spawning {path}");
            VfxStruct* vfx;
            fixed (byte* p = Encoding.UTF8.GetBytes(path))
            {
                fixed (byte* pool = Pool)
                {
                    //P.VFXManager.Replacer.Operation = true;
                    vfx = this._staticVfxCreate(p, pool);
                    //P.VFXManager.Replacer.Operation = false;
                }
            }

            if (vfx == null)
            {
                return null;
            }

            if (this._staticVfxRun(vfx, 0.0f, 0xFFFFFFFF) != 0)
            {
                this.RemoveStatic(vfx);
                return null;
            }

            // update position
            vfx->Position = new Vector3(pos.X, pos.Y, pos.Z);
            // update rotation
            vfx->Rotation = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);

            // update
            vfx->Flags |= 2;

            this.Spawned.Add((nint)vfx);

            return vfx;
        }

        internal void RemoveStatic(VfxStruct* vfx)
        {
            this._staticVfxRemove(vfx);
            this.Spawned.Remove((nint)vfx);
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct VfxStruct
        {
            [FieldOffset(0x38)]
            public byte Flags;

            [FieldOffset(0x50)]
            public Vector3 Position;

            [FieldOffset(0x60)]
            public Quaternion Rotation;

            [FieldOffset(0x70)]
            public Vector3 Scale;

            [FieldOffset(0x128)]
            public int ActorCaster;

            [FieldOffset(0x130)]
            public int ActorTarget;

            [FieldOffset(0x1B8)]
            public int StaticCaster;

            [FieldOffset(0x1C0)]
            public int StaticTarget;
        }
    }
}
