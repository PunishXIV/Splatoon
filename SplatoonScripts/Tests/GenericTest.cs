using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Interface.Colors;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests
{
    public unsafe class GenericTest : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() {  };
        //bool __fastcall sub_1400AA130(__int16 a1)
        //NumberArrayData_SetValueIfDifferentAndNotify(__int64 a1, int a2, int a3)
        delegate void Func(nint a1, int a2, int a3);
        [Signature("3B 51 08 7D 15 48 8B 41 20 48 63 D2 44 39 04 90")]
        Hook<Func> Hook;

        //char __fastcall sub_1409EFD60(__int64 a1, unsigned int a2)
        delegate byte AddonRetainerTaskAsk_OnRequestedUpdate(nint a1, uint a2);
        [Signature("48 89 5C 24 ?? 55 48 83 EC 20 48 8B E9 8B DA 8B CA")]
        Hook<AddonRetainerTaskAsk_OnRequestedUpdate> Hook2;

        public override void OnEnable()
        {
            SignatureHelper.Initialise(this);
            Hook.Enable();
            Hook2?.Enable();
            //DuoLog.Warning($"{Svc.SigScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 0F B7 FA"):X16}");
            base.OnEnable();
            Svc.GameNetwork.NetworkMessage += GameNetwork_NetworkMessage;
        }

        private void GameNetwork_NetworkMessage(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, Dalamud.Game.Network.NetworkMessageDirection direction)
        {
            try
            {
                if (direction == Dalamud.Game.Network.NetworkMessageDirection.ZoneDown)
                {
                    //DuoLog.Information($"{opCode}");
                }
            }
            catch(Exception e)
            {

            }
        }

        public override void OnDisable()
        {
            Hook?.Disable();
            Hook?.Dispose();
            Hook2?.Disable();
            Hook2?.Dispose();
            Svc.GameNetwork.NetworkMessage -= GameNetwork_NetworkMessage;
            base.OnDisable();
        }

        byte Detour2(nint a1, uint a2)
        {
            var ret = Hook2.Original(a1, a2);
            try
            {
                //if (Debugger.IsAttached) Debugger.Break();
                /*var v3 = *(nint*)(a2 + 840);
                var p1 = *(nint*)(v3 + 32);
                var v6 = (uint*)(*(nint*)(v3 + 32) + 1160);
                var ptr2 = *(nint*)(a1 + 560);
                DuoLog.Information($"v3:{v3:X16}, p1:{p1:X16}, v6:{(nint)v6:X16}/{*v6:X16}, ptr2:{ptr2:X16}");*/
                PluginLog.Information($"{a1:X16}, {a2}, {ret:X2}");
            }
            catch (Exception e)
            {
                e.Log();
            }
            return ret;
        }

        void Detour(nint a1, int a2, int a3)
        {
            try
            {
                
                var v3 = *(nint*)(a1 + 32);
                var r = (int*)(v3 + 4 * a2);
                if (a1 > 0x0000020F4BD8ADA8 - 0x50 && a1 < 0x0000020F4BD8ADA8 + 0x50)
                {
                    PluginLog.Information($"{a1}, {a2}, {a3}");
                }
                if ((nint)r == 0x0000020F4BD8ADA8)
                {
                    DuoLog.Information($"{a2}, {a3}");
                    //if (Debugger.IsAttached) Debugger.Break();
                }
            }
            catch (Exception e)
            {
                e.Log();
            }
            Hook.Original(a1, a2, a3);
        }

        public override void OnSettingsDraw()
        {
            if(GenericHelpers.TryGetAddonByName<AtkUnitBase>("GuildLeve", out var addon) && GenericHelpers.IsAddonReady(addon) && ImGui.Button("Click"))
            {
                //DuoLog.Information($"{(nint)(addon->AtkEventListener.vfunc[2]):X16}");
                var list = addon->UldManager.NodeList[11];
            }
        }
    }
}
