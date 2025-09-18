using ECommons.Hooks;
using Lumina.Excel.Sheets;
using Splatoon.Modules;
using Splatoon.SplatoonScripting;

namespace Splatoon.Memory
{
    internal static unsafe class DirectorUpdateProcessor
    {
        internal static void ProcessDirectorUpdate(long a1, long a2, DirectorUpdateCategory a3, uint a4, uint a5, int a6, int a7)
        {
            if(P.Config.Logging)
            {
                var text = $"Director Update: {a3:X}, {a4:X8}, {a5:X8}, {a6:X8}, {a7:X8}";
                Logger.Log(text);
                PluginLog.Verbose(text);
                P.LogWindow.Log(text);
            }
            PhaseUpdater.UpdateFromDirector(a3);
            ScriptingProcessor.OnDirectorUpdate(a3);
        }
    }
}
