using ECommons.Events;
using ECommons.GameHelpers;
using Lumina.Excel.GeneratedSheets;
using Splatoon.Utils;
using Splatoon.VFX.Items.Donut;
using Splatoon.VFX.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Splatoon.VFX.Loader.VFXMemory;

namespace Splatoon.VFX
{
    internal unsafe class VFXManager : IDisposable
    {
        internal List<VFXRequest> RequestList = new();
        internal string TempDir = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "fxtemp");
        internal FXDonutController FXDonutController;
        internal VFXMemory Memory = new();
        internal VFXReplacer Replacer = new();
        internal VfxStruct* DebugVfx;

        internal VFXManager()
        {
            return;
            if (!Directory.Exists(TempDir)) Directory.CreateDirectory(TempDir);
            FXDonutController = new();
            new TickScheduler(delegate
            {
                ProperOnLogin.Register(delegate
                {
                    FXDonutController.Get(new(Vector3.Zero, 1, Colors.Red, 3f, 5f));
                    DebugVfx = Memory.SpawnStatic("bg/ffxiv/fst_f1/common/vfx/eff/b0941trp1a_o.avfx", Player.Object.Position, Quaternion.Zero);
                    DuoLog.Information($"ptr: {(nint)DebugVfx}");
                }, true);
            });
            
        }

        internal void Tick()
        {
            return;
            if (DebugVfx != null)
            {
                DebugVfx->Position = Player.Object.Position;
                var s = 1 * Player.Object.Rotation;
                DebugVfx->Scale = new Vector3(s, 1, s);
                DebugVfx->Flags |= 2;
                Memory.Run(DebugVfx);
                PluginLog.Verbose($"Updated position {DebugVfx->Position}");
            }
            /*if(Svc.ClientState.LocalPlayer != null)
            {
                Request(new()
                {
                    Color = Colors.Red,
                    donutRadius = 5f,
                    Position = Svc.ClientState.LocalPlayer.Position,
                    radius = 3f
                });
            }
            foreach(var x in RequestList)
            {
                FXDonutController.LoadIfNotLoaded(new(x.Position, 1.0f, x.Color, x.radius, x.donutRadius));
            }
            FXDonutController.UnloadNotRequested();
            RequestList.Clear();*/
        }

        internal void Request(VFXRequest request)
        {
            RequestList.Add(request);
        }

        internal IEnumerable<string> AllExistingPathes()
        {
            foreach(var x in FXDonutController.ExistingPathes)
            {
                yield return x.Value;
            }
        }

        internal static void Debug(string s)
        {
            PluginLog.Debug($"[VFXManager] {s}");
        }

        public void Dispose()
        {
            FXDonutController?.Dispose();
            Memory?.Dispose();
            Replacer?.Dispose();
            if(DebugVfx != null)
            {
                Memory.RemoveStatic(DebugVfx);
            }
        }
    }
}
