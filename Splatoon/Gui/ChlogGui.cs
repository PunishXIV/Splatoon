using ECommons.LanguageHelpers;
using Splatoon.SplatoonScripting;

namespace Splatoon.Gui;

class ChlogGui
{
    public const int ChlogVersion = 63;
    readonly Splatoon p;
    bool open = true;
    internal bool openLoggedOut = false;
    bool understood = false;
    public ChlogGui(Splatoon p)
    {
        this.p = p;
        Svc.PluginInterface.UiBuilder.Draw += Draw;
    }

    public void Dispose()
    {
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
    }

    void Draw()
    {
        if (!open) return;
        if (!Svc.ClientState.IsLoggedIn && !openLoggedOut) return;
        ImGui.Begin("Splatoon has been updated".Loc(), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);
        ImGuiEx.Text(
@"Attention!
This update brings breaking changes to the scripting system. 
!!! Dominion script needs to be reinstalled manually if you have used it !!!");
        if(ImGui.Button("Reinstall dominion script"))
        {
            ScriptingProcessor.DownloadScript("https://github.com/NightmareXIV/Splatoon/raw/master/SplatoonScripts/Duties/Endwalker/P8S2%20Dominion.cs");
        }
        if (ImGui.Button("Close this window".Loc()))
        {
            open = false;
        }
        ImGui.End();
        if (!open) Close();
    }

    void Close()
    {
        p.Config.Backup(true);
        p.Config.ChlogReadVer = ChlogVersion;
        p.Config.Save();
        Dispose();
    }
}
