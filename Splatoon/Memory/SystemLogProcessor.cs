using ECommons.EzHookManager;
using Reloaded.Hooks.Definitions.Structs;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Splatoon.Memory;

public unsafe class SystemLogProcessor
{
    delegate nint EventFramework_ProcessSystemLogMessageDelegate(void* eventFramework, uint a2, uint a3, nint a4, byte a5);
    [EzHook("40 55 56 41 54 41 55 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 FF")]
    EzHook<EventFramework_ProcessSystemLogMessageDelegate> EventFramework_ProcessSystemLogMessageHook;

    private SystemLogProcessor()
    {
        EzSignatureHelper.Initialize(this);
    }

    nint EventFramework_ProcessSystemLogMessageDetour(void* eventFramework, uint a2, uint messageId, nint a4, byte a5)
    {
        try
        {
            //PluginLog.Information($"{a2} {messageId} {a4} {a5}");\
            if(messageId == 2012 || messageId == 2014)
            {
                ScriptingProcessor.ResetAllScripts();
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        return EventFramework_ProcessSystemLogMessageHook.Original(eventFramework, a2, messageId, a4, a5);
    }
}
