using Dalamud.Hooking;
using ECommons.EzHookManager;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Memory;
internal class ActorControlProcessor
{
    private delegate void ProcessPacketActorControlDelegate(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, uint p7, uint p8, ulong targetId, byte replaying);
    private EzHook<ProcessPacketActorControlDelegate> ProcessPacketActorControlHook;

    internal ActorControlProcessor()
    {
        try
        {
            ProcessPacketActorControlHook = new EzHook<ProcessPacketActorControlDelegate>("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", ProcessPacketActorControlDetour);
            ProcessPacketActorControlHook?.Enable();
        }
        catch(Exception ex)
        {
            PluginLog.Error($"Could not create ActorControl hook.");
            ex.Log();
        }
    }

    private void ProcessPacketActorControlDetour(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, uint p7, uint p8, ulong targetId, byte replaying)
    {
        ProcessPacketActorControlHook.Original(sourceId, command, p1, p2, p3, p4, p5, p6, p7, p8, targetId, replaying);
        ScriptingProcessor.OnActorControl(sourceId, command, p1, p2, p3, p4, p5, p6, p7, p8, targetId, replaying);
    }
}
