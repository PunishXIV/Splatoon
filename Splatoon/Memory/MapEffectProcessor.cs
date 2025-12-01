using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.Hooks;
using Splatoon.Modules;
using Splatoon.SplatoonScripting;

namespace Splatoon.Memory;

internal class MapEffectProcessor
{
    internal static Dictionary<uint, (ushort Param1, ushort Param2)> History = [];
    internal MapEffectProcessor()
    {
        MapEffect.Init((a1, a2, a3, a4) =>
        {
            var text = $"MapEffect: {a2}, {a3}, {a4}";
            History[a2] = (a3, a4);
            P.ChatMessageQueue.Enqueue(text);
            Logger.Log(text);
            P.LogWindow.Log(text);
            ScriptingProcessor.OnMapEffect(a2, a3, a4);
        });
    }

    public void Dispose()
    {

    }

}
