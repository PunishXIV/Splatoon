using ECommons;
using ECommons.LanguageHelpers;
using ECommons.Reflection;
using Splatoon.SplatoonScripting;

namespace Splatoon.Gui;

internal static class RapidImport
{
    internal static bool RapidImportEnabled = false;
    internal static void Draw() 
    {
        if(ImGui.Checkbox("Enable Rapid Import", ref RapidImportEnabled))
        {
            GenericHelpers.Copy("");
        }
        ImGuiEx.TextWrapped("Import multiple presets with ease by simply copying them. Splatoon will read your clipboard and attempt to import whatever you copy. Your clipboard will be cleared upon enabling.".Loc());
        if (RapidImportEnabled)
        {
            try
            {
                var text = GenericHelpers.Paste();
                if (text != "")
                {
                    if (ScriptingProcessor.IsUrlTrusted(text))
                    {
                        TryNotify("Downloading script from trusted URL".Loc());
                        ScriptingProcessor.DownloadScript(text);
                    }
                    else
                    {
                        if (CGui.ImportFromClipboard())
                        {
                            TryNotify("Import success".Loc());
                        }
                        else
                        {
                            TryNotify("Import failed".Loc());
                        }
                    }
                    GenericHelpers.Copy("");
                }
            }
            catch(Exception e)
            {
                //
            }
        }
    }

    static void TryNotify(string s)
    {
        P.NotificationMasterApi.DisplayTrayNotification("Splatoon", s);
    }
}
