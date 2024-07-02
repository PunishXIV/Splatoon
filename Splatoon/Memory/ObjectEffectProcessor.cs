using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices.Legacy;
using Splatoon.Modules;
using Splatoon.SplatoonScripting;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Splatoon.Memory;

internal unsafe class ObjectEffectProcessor
{
    internal delegate long ProcessObjectEffect(GameObject* a1, ushort a2, ushort a3, long a4);
    [Signature("40 53 55 57 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 44 0F B7 F2", DetourName = nameof(ProcessObjectEffectDetour), Fallibility = Fallibility.Fallible)]
    internal Hook<ProcessObjectEffect> ProcessObjectEffectHook = null;
    internal long ProcessObjectEffectDetour(GameObject* a1, ushort a2, ushort a3, long a4)
    {
        try
        {
            if (P.Config.Logging)
            {
                var text = $"ObjectEffect: on {a1->Name.Read()} {a1->EntityId.Format()}/{a1->BaseId.Format()} data {a2}, {a3}";
                Logger.Log(text);
            }
            var ptr = (nint)a1;
            if (!AttachedInfo.ObjectEffectInfos.ContainsKey(ptr))
            {
                AttachedInfo.ObjectEffectInfos[ptr] = [];
            }
            AttachedInfo.ObjectEffectInfos[ptr].Add(new()
            {
                StartTime = Environment.TickCount64,
                data1 = a2,
                data2 = a3
            });
            ScriptingProcessor.OnObjectEffect(a1->EntityId, a2, a3);
        }
        catch (Exception e)
        {
            e.Log();
        }
        return ProcessObjectEffectHook.Original(a1, a2, a3, a4);
    }

    internal ObjectEffectProcessor()
    {
        try
        {
            SignatureHelper.Initialise(this);
            Enable();
        }
        catch (Exception e)
        {
            e.LogWarning();
        }
    }

    internal void Enable()
    {
        try
        {
            if (!ProcessObjectEffectHook.IsEnabled) ProcessObjectEffectHook.Enable();
        }

        catch (Exception e)
        {
            e.LogWarning();
        }
    }

    internal void Disable()
    {
        try
        {
            if (ProcessObjectEffectHook.IsEnabled) ProcessObjectEffectHook.Disable();
        }
        catch (Exception e)
        {
            e.LogWarning();
        }
    }

    public void Dispose()
    {
        try
        {
            Disable();
            ProcessObjectEffectHook.Dispose();
        }
        catch (Exception e)
        {
            e.LogWarning();
        }
    }
}
