using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using Splatoon.Modules;
using Splatoon.SplatoonScripting;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Splatoon.Memory;

internal unsafe class ObjectEffectProcessor
{
    internal delegate long ProcessObjectEffect(GameObject* a1, ushort a2, ushort a3, long a4);
    [Signature("40 53 55 56 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B7 FA", DetourName = nameof(ProcessObjectEffectDetour), Fallibility = Fallibility.Fallible)]
    internal Hook<ProcessObjectEffect> ProcessObjectEffectHook = null;
    internal long ProcessObjectEffectDetour(GameObject* a1, ushort a2, ushort a3, long a4)
    {
        try
        {
            if (P.Config.Logging)
            {
                var text = $"ObjectEffect: on {MemoryHelper.ReadStringNullTerminated((nint)a1->Name)} {a1->ObjectID.Format()}/{a1->DataID.Format()} data {a2}, {a3}";
                Logger.Log(text);
            }
            var ptr = (nint)a1;
            if (!AttachedInfo.ObjectEffectInfos.ContainsKey(ptr))
            {
                AttachedInfo.ObjectEffectInfos[ptr] = new();
            }
            AttachedInfo.ObjectEffectInfos[ptr].Add(new()
            {
                StartTime = Environment.TickCount64,
                data1 = a2,
                data2 = a3
            });
            ScriptingProcessor.OnObjectEffect(a1->ObjectID, a2, a3);
        }
        catch (Exception e)
        {
            e.Log();
        }
        return ProcessObjectEffectHook.Original(a1, a2, a3, a4);
    }

    internal ObjectEffectProcessor()
    {
        SignatureHelper.Initialise(this);
        this.Enable();
    }

    internal void Enable()
    {
        if (!ProcessObjectEffectHook.IsEnabled) ProcessObjectEffectHook.Enable();
    }

    internal void Disable()
    {
        if (ProcessObjectEffectHook.IsEnabled) ProcessObjectEffectHook.Disable();
    }

    public void Dispose()
    {
        this.Disable();
        ProcessObjectEffectHook.Dispose();
    }
}
