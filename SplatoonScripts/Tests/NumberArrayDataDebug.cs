using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests
{
    public unsafe class NumberArrayDataDebug : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new();

        delegate byte Delegate(ulong a1);
        [Signature("48 89 5C 24 ?? 57 48 83 EC 20 8B D9 8B F9", DetourName = nameof(Detour))]
        Hook<Delegate> Hook;

        byte Detour(ulong a1)
        {
            var ret = Hook.Original(a1);
            PluginLog.Information($"{a1} {ret}, {(nint)QuestManager.Instance():X16}");
            return ret;
        }

        public override void OnEnable()
        {
            DuoLog.Information($"UIState: {(nint)UIState.Instance():X16}");
            SignatureHelper.Initialise(this);
            Hook.Enable();
        }

        public override void OnDisable()
        {
            Hook.Disable();
            Hook.Dispose();
        }
    }
}
