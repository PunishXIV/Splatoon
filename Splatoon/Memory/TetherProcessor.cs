using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Splatoon.SplatoonScripting;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Splatoon.Memory;

internal unsafe class TetherProcessor
{
    delegate long ProcessTether(Character* a1, byte a2, byte a3, long targetOID, byte a5);
    [Signature("E8 ?? ?? ?? ?? EB 48 41 81 FF", DetourName = nameof(ProcessTetherDetour), Fallibility = Fallibility.Fallible)]
    Hook<ProcessTether> ProcessTetherHook = null;

    delegate long ProcessTetherRemoval(Character* a1, byte a2, byte a3, byte a4, byte a5);
    [Signature("E8 ?? ?? ?? ?? EB 64 F3 0F 10 05", DetourName = nameof(ProcessTetherRemovalDetour), Fallibility = Fallibility.Fallible)]
    Hook<ProcessTetherRemoval> ProcessTetherRemovalHook = null;

    internal TetherProcessor()
    {
        SignatureHelper.Initialise(this);
        Enable();
    }

    internal void Enable()
    {
        if (ProcessTetherHook?.IsEnabled == false) ProcessTetherHook?.Enable();
        if (ProcessTetherRemovalHook?.IsEnabled == false) ProcessTetherRemovalHook?.Enable();
    }

    internal void Disable()
    {
        if (ProcessTetherHook?.IsEnabled == true) ProcessTetherHook?.Disable();
        if (ProcessTetherRemovalHook?.IsEnabled == true) ProcessTetherRemovalHook?.Disable();
    }

    internal void Dispose()
    {
        Disable();
        ProcessTetherHook?.Dispose();
        ProcessTetherRemovalHook?.Dispose();
    }

    long ProcessTetherDetour(Character* a1, byte a2, byte a3, long targetOID, byte a5)
    {
        try
        {
            if (targetOID == 0xE0000000)
            {
                ScriptingProcessor.OnTetherRemoval(a1->GameObject.ObjectID, a2, a3, a5);
            }
            else
            {
                ScriptingProcessor.OnTetherCreate(a1->GameObject.ObjectID, (uint)targetOID, a2, a3, a5);
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
        return ProcessTetherHook.Original(a1, a2, a3, targetOID, a5);
    }

    long ProcessTetherRemovalDetour(Character* a1, byte a2, byte a3, byte a4, byte a5)
    {
        try
        {
            ScriptingProcessor.OnTetherRemoval(a1->GameObject.ObjectID, a2, a3, a5);
        }
        catch (Exception e)
        {
            e.Log();
        }
        return ProcessTetherRemovalHook.Original(a1, a2, a3, a4, a5);
    }
}
