using ECommons.LanguageHelpers;
using Splatoon.Gui.Windows;
using Splatoon.Modules.TranslationWorkspace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Gui.Tabs;
public unsafe static class TabTranslator
{
    public static void Draw()
    {
        ImGuiEx.TextWrapped($"""
            Beta feature - may contain issues. Please proceed with caution, translate and submit PRs in small batches.
            To begin working with translator, copy the text of the whole GitHub .md file and press "Import Page from Clipboard" button, or select previously imported page.
            When you have finished translating, copy text with "Copy Result to Clipboard" button and submit a pull request to the original repository.
            """);
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Paste, "Import Page from Clipboard".Loc()))
        {
            try
            {
                var page = new Page(Paste());
                if(page != null)
                {
                    P.Config.TranslatorPages.Add(page);
                    new TranslationWorkspaceWindow($"Translation Workspace".Loc() + $"##{page.ID}", page);
                }
            }
            catch(Exception e)
            {
                e.Log();
                Notify.Error(e.Message);
            }
        }
        foreach(var page in P.Config.TranslatorPages)
        {
            if(ImGui.Selectable($"{page.Name}##{page.ID}"))
            {
                try
                {
                    new TranslationWorkspaceWindow($"Translation Workspace".Loc() + $"##{page.ID}", page);
                }
                catch(Exception e)
                {
                    e.Log();
                    Notify.Error(e.Message);
                }
            }
        }
    }
}