using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Splatoon.Modules;
using Splatoon.SplatoonScripting;

namespace Splatoon.Memory;

internal class MapEffectProcessor
{
    internal delegate long ProcessMapEffect(long a1, uint a2, ushort a3, ushort a4);
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B FA 41 0F B7 E8", DetourName = nameof(ProcessMapEffectDetour), Fallibility = Fallibility.Fallible)]
    internal Hook<ProcessMapEffect> ProcessMapEffectHook = null;
    internal long ProcessMapEffectDetour(long a1, uint a2, ushort a3, ushort a4)
    {
        try
        {
            var text = $"MapEffect: {a2}, {a3}, {a4}";
            P.ChatMessageQueue.Enqueue(text);
            Logger.Log(text);
            ScriptingProcessor.OnMapEffect(a2, a3, a4);
        }
        catch (Exception e)
        {
            e.Log();
        }
        return ProcessMapEffectHook.Original(a1, a2, a3, a4);
    }

    internal MapEffectProcessor()
    {
        SignatureHelper.Initialise(this);
        this.Enable();
    }

    internal void Enable()
    {
        if (!ProcessMapEffectHook.IsEnabled) ProcessMapEffectHook.Enable();
    }

    internal void Disable()
    {
        if (ProcessMapEffectHook.IsEnabled) ProcessMapEffectHook.Disable();
    }

    public void Dispose()
    {
        this.Disable();
        ProcessMapEffectHook.Dispose();
    }

}
