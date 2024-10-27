using ECommons.LanguageHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Gui;
public static class TabTrustedRepos
{
    public static void Draw()
    {
        ref var pass = ref Ref<bool>.Get();
        var display = pass || P.Config.ExtraTrustedRepos != "" || P.Config.ExtraUpdateLinks != "";
        if(!display)
        {
            ImGuiEx.TextWrapped(EColor.RedBright, "You are about to access EXTRAORDINARELY DANGEROUS OPTIONS. Normally, the ONLY time you'd want to use it if you are the developer. ".Loc());
            ImGui.Checkbox($"I understand that improper use of these functions may result in irrecoverable damages.", ref pass);
        }
        if(!pass) return;
        pass = true;
        ImGuiEx.Text($"Extra trusted sources");
        ImGui.Indent();
        ImGuiEx.TextWrapped($"Add extra trusted sources from which you would like to import scripts. One per line. Any URL that starts with any of the lines you add will be considered trusted. You should choose wisely. Splatoon developers and publishers are NOT responsible for any possible damage that will happen to your game, characters, personal data, operating system, and PC if you will use this function incorrectly.".Loc());
        ImGui.Unindent();
        ImGuiEx.InputTextMultilineExpanding("trustSource", ref P.Config.ExtraTrustedRepos, 2000, 5);
        ImGui.Separator();
        ImGuiEx.Text($"Extra update sources");
        ImGui.Indent();
        ImGuiEx.TextWrapped(EColor.RedBright, $"In addition to official Splatoon repo, Splatoon will check scripts for updates from the following lists, one per line. WARNING. By adding an extra list here you will allow maintainer of such list to run ANY CODE ON YOUR COMPUTER, without any restrictions. Splatoon developers and publishers are NOT responsible for any possible damage that will happen to your game, characters, personal data, operating system, and PC if you will use this function incorrectly.".Loc());
        ImGui.Unindent();
        ImGuiEx.InputTextMultilineExpanding("trustRepo", ref P.Config.ExtraUpdateLinks, 2000, 5);
    }
}
