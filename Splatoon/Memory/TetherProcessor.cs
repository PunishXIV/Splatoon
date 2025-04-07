using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices.Legacy;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Splatoon.Modules;
using Splatoon.SplatoonScripting;
using static System.Net.Mime.MediaTypeNames;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Splatoon.Memory;

internal unsafe class TetherProcessor
{
    delegate long ProcessTether(VfxContainer* a1, byte a2, ushort a3, long targetOID, byte a5);
    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 41 54 41 55 41 56 41 57 48 83 EC 20 0F B6 74 24", DetourName = nameof(ProcessTetherDetour), Fallibility = Fallibility.Fallible)]
    Hook<ProcessTether> ProcessTetherHook = null;

    delegate long ProcessTetherRemoval(VfxContainer* a1, byte a2, ushort a3, byte a4, byte a5);
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 0F B6 F2 41 0F B6 E9", DetourName = nameof(ProcessTetherRemovalDetour), Fallibility = Fallibility.Fallible)]
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

    long ProcessTetherDetour(VfxContainer* a1, byte a2, ushort a3, long targetOID, byte a5)
    {
        var ret = ProcessTetherHook.Original(a1, a2, a3, targetOID, a5);
        try
        {
            if(a1->OwnerObject == null)
            {
                PluginLog.Warning($"Tether owner object is null");
            }
            if (a1 != null && a1->OwnerObject != null)
            {
                if (targetOID == 0xE0000000)
                {
                    ScriptingProcessor.OnTetherRemoval(a1->OwnerObject->EntityId, a2, a3, a5);
                    var text = $"Tether removal: {a1->OwnerObject->EntityId}, {a2}, {a3}, {a5}";
                    PluginLog.Verbose(text);
                    Logger.Log(text);
                    P.LogWindow.Log(text);
                    AttachedInfo.GetOrCreateTetherInfo(a1->OwnerObject).RemoveAll(x => x.ParamEqual(a2, a3, a5));
                }
                else
                {
                    ScriptingProcessor.OnTetherCreate(a1->OwnerObject->EntityId, (uint)targetOID, a2, a3, a5);
                    var isSourceYou = (nint)a1->OwnerObject == Player.Object.Address;
                    var isTargetYou = (uint)targetOID == Player.Object.EntityId;
                    var sourceText = isSourceYou ? "YOU" : (a1->OwnerObject->ObjectKind == ObjectKind.Pc ? ((Job)a1->OwnerObject->ClassJob).ToString() : a1->OwnerObject->BaseId.ToString());
                    IGameObject targetObj = null;
                    foreach(var x in Svc.Objects) 
                    {
                        if(x.GameObjectId == (ulong)targetOID)
                        {
                            targetObj = x;
                            break;
                        }
                    }
                    var targetText = isTargetYou ? "YOU" : (targetObj is IPlayerCharacter pc ? pc.GetJob().ToString() : targetObj?.DataId.ToString() ?? targetOID.ToString());
                    var text = $"Tether create: TetherSource:{sourceText} TetherParam:({a2}, {a3}, {a5}) TetherTarget:{targetText}";
                    PluginLog.Verbose(text);
                    Logger.Log(text);
                    P.LogWindow.Log(text);
                    P.ChatMessageQueue.Enqueue(text);
                    AttachedInfo.GetOrCreateTetherInfo(a1->OwnerObject).Add(new(a2, a3, a5, (uint)targetOID));
                }
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
        return ret;
    }

    long ProcessTetherRemovalDetour(VfxContainer* a1, byte a2, ushort a3, byte a4, byte a5)
    {
        var ret = ProcessTetherRemovalHook.Original(a1, a2, a3, a4, a5);
        try
        {
            if (a1 != null && a1->OwnerObject != null)
            {
                ScriptingProcessor.OnTetherRemoval(a1->OwnerObject->EntityId, a2, a3, a5);
                var text = $"Tether removal2: {a1->OwnerObject->EntityId}, {a2}, {a3}, {a5}";
                PluginLog.Verbose(text);
                Logger.Log(text);
                P.LogWindow.Log(text);
                AttachedInfo.GetOrCreateTetherInfo(a1->OwnerObject).RemoveAll(x => x.ParamEqual(a2, a3, a5));
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
        return ret;
    }
}
