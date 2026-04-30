using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices.Legacy;
using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Splatoon.Modules;
using Splatoon.SplatoonScripting;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Splatoon.Memory;

internal unsafe class ObjectEffectProcessor
{
    [EzHookFromCS]
    internal EzHook<EventObject.Delegates.PlayAnimation> ProcessObjectEffectHook = null;
    internal void ProcessObjectEffectDetour(EventObject* thisPtr, uint entityId, uint actionId, ulong a4)
    {
        try
        {
            if(P.Config.Logging)
            {
                var text = $"ObjectEffect: on {thisPtr->Name.Read()} {thisPtr->EntityId.Format()}/{thisPtr->BaseId.Format()} data {entityId}, {actionId}";
                Logger.Log(text);
                if(thisPtr->ObjectKind != ObjectKind.Pc) P.LogWindow.Log(text);
            }
            var ptr = (nint)thisPtr;
            if(!AttachedInfo.ObjectEffectInfos.ContainsKey(ptr))
            {
                AttachedInfo.ObjectEffectInfos[ptr] = [];
            }
            AttachedInfo.ObjectEffectInfos[ptr].Add(new()
            {
                StartTime = Environment.TickCount64,
                data1 = entityId,
                data2 = actionId
            });
            ScriptingProcessor.OnObjectEffect(thisPtr->EntityId, entityId, actionId);
        }
        catch(Exception e)
        {
            e.Log();
        }
        ProcessObjectEffectHook.Original(thisPtr, entityId, actionId, a4);
    }

    private ObjectEffectProcessor()
    {
        try
        {
            EzSignatureHelper.Initialize(this);
        }
        catch(Exception e)
        {
            e.LogWarning();
        }
    }
}
