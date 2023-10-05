using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices.Legacy;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Splatoon.SplatoonScripting;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Splatoon.Memory;

internal unsafe class TetherProcessor
{
    delegate long ProcessTether(Character.VfxContainer* a1, byte a2, ushort a3, long targetOID, byte a5);
    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 41 54 41 55 41 56 41 57 48 83 EC 20 0F B6 74 24", DetourName = nameof(ProcessTetherDetour), Fallibility = Fallibility.Fallible)]
    Hook<ProcessTether> ProcessTetherHook = null;

    delegate long ProcessTetherRemoval(Character.VfxContainer* a1, byte a2, ushort a3, byte a4, byte a5);
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 57 48 83 EC 20 41 0F B6 E9 0F B6 DA", DetourName = nameof(ProcessTetherRemovalDetour), Fallibility = Fallibility.Fallible)]
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

    long ProcessTetherDetour(Character.VfxContainer* a1, byte a2, ushort a3, long targetOID, byte a5)
    {
        var ret = ProcessTetherHook.Original(a1, a2, a3, targetOID, a5);
        try
        {
            if (a1 != null && a1->OwnerObject != null)
            {
                if (targetOID == 0xE0000000)
                {
                    ScriptingProcessor.OnTetherRemoval(a1->OwnerObject->Character.GameObject.ObjectID, a2, a3, a5);
                    PluginLog.Verbose($"Tether removal: {a1->OwnerObject->Character.GameObject.ObjectID}, {a2}, {a3}, {a5}");
                }
                else
                {
                    ScriptingProcessor.OnTetherCreate(a1->OwnerObject->Character.GameObject.ObjectID, (uint)targetOID, a2, a3, a5);
                    PluginLog.Verbose($"Tether create: {(nint)a1:X16}{a1->OwnerObject->Character.GameObject.ObjectID}, {targetOID}/{(uint)targetOID}, {a2}, {a3}, {a5}");
                }
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
        return ret;
    }

    long ProcessTetherRemovalDetour(Character.VfxContainer* a1, byte a2, ushort a3, byte a4, byte a5)
    {
        var ret = ProcessTetherRemovalHook.Original(a1, a2, a3, a4, a5);
        try
        {
            if (a1 != null && a1->OwnerObject != null)
            {
                ScriptingProcessor.OnTetherRemoval(a1->OwnerObject->Character.GameObject.ObjectID, a2, a3, a5);
                PluginLog.Verbose($"Tether removal2: {a1->OwnerObject->Character.GameObject.ObjectID}, {a2}, {a3}, {a5}");
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
        return ret;
    }
}
