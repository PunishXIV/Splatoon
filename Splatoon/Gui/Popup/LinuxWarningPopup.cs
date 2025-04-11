using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using ECommons.ChatMethods;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods.TerritorySelection;
using ECommons.LanguageHelpers;
using ECommons.PartyFunctions;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Splatoon.Gui.Priority;
#nullable enable
public class LinuxWarningPopup : Window
{
    public LinuxWarningPopup() : base("Splatoon - Linux/Mac OS detected - Warning", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        this.SetSizeConstraints(new(500, 100), new(500, float.MaxValue));
        ShowCloseButton = false;
        RespectCloseHotkey = false;
        IsOpen = Utils.IsLinux() && !P.Config.DX11EnabledOnMacLinux && !P.Config.DX11MacLinuxWarningHidden;
    }

    public override void Draw()
    {
        ImGuiEx.TextWrapped($"""
            Linux or Mac OS environment detected and by default DirectX11 renderer was disabled due to crashing issues. 
            If you wish, you can open Splatoon settings, go to "Render" tab to test it and reenable it if test succeeds. 
            If it doesn't works for you, simply hide this window and use Legacy rendered.
            """);
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Cog, "Open configuration"))
        {
            P.ConfigGui.Open = true;
        }
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.EyeSlash, "Permanently hide this window"))
        {
            P.Config.DX11MacLinuxWarningHidden = true;
            IsOpen = false;
        }
    }
}
