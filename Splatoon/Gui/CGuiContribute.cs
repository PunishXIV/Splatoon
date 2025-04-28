﻿using ECommons.LanguageHelpers;

namespace Splatoon.ConfigGui;

internal class Contribute
{
    internal static void OpenGithubPresetSubmit()
    {
        var url = "https://github.com/PunishXIV/Splatoon/tree/main/Presets#adding-your-preset";
        Svc.Chat.Print("[Splatoon] How to submit your preset: ".Loc() + url);
        Utils.ProcessStart(url);
    }

    internal static void OpenDiscordLink()
    {
        Svc.Chat.Print("[Splatoon] Server invite link: ".Loc() + Splatoon.DiscordURL);
        Utils.ProcessStart(Splatoon.DiscordURL);
    }

    internal static void Draw()
    {
        ImGui.PushID("contribute");
        ImGui.PushTextWrapPos();
        ImGuiEx.Text("If you like Splatoon, you may consider contributing in any following way:".Loc());
        ImGui.Separator();
        ImGuiEx.Text("- Contributing combat data of new battles".Loc());
        ImGuiEx.Text("When a new battle comes, I would greatly benefit from obtaining it's combat data. If you are doing these battles early and wish to contribute combat data, please contact me via Discord to receive instructions on how to do so.".Loc());
        if(ImGui.Button("Open Discord server##2".Loc()))
        {
            OpenDiscordLink();
        }
        ImGui.Separator();
        ImGuiEx.Text("- Sending your own presets to public".Loc());
        ImGuiEx.Text("Did Splatoon helped you to clear a raid, to resolve a mechanic, to improve your gameplay in any way? Please consider submitting your preset to the public so others may enjoy it as well!".Loc());
        ImGuiEx.Text("You may send it to Github if you have account or to my Discord server.".Loc());
        if(ImGui.Button("Open Github page".Loc()))
        {
            OpenGithubPresetSubmit();
        }
        ImGui.SameLine();
        if(ImGui.Button("Open Discord server".Loc()))
        {
            OpenDiscordLink();
        }
        ImGui.Separator();
        ImGuiEx.Text("- Adding a star to the repo".Loc());
        ImGuiEx.Text("Don't have any presets to send? You may still help by simply adding a star to Splatoon and my plugins' repo!".Loc());
        ImGuiEx.Text("To do so, all you need is Github account. After logging in, proceed to the links below and click \"Star\" button in top right corner of the page.".Loc());
        if(ImGui.Button("Open Splatoon repo".Loc()))
        {
            var url = "https://github.com/PunishXIV/Splatoon";
            Svc.Chat.Print("[Splatoon] Splatoon repo: ".Loc() + url);
            Utils.ProcessStart(url);
        }
        /*ImGui.SameLine();
        if (ImGui.Button("Open NightmareXIV plugins repo".Loc()))
        {
            var url = "https://github.com/NightmareXIV/MyDalamudPlugins";
            Svc.Chat.Print("[Splatoon] NightmareXIV plugin repo: ".Loc() + url);
            ProcessStart(url);
        }*/
        ImGui.Separator();
        ImGuiEx.Text("- Financial".Loc());
        ImGuiEx.Text("If you would like to financially support me, you may use Patreon, Ko-Fi or cryptocurrency. Financial support allows me to put extra time into plugin!".Loc());

        if(ImGui.Button("Patreon"))
        {
            ShellStart("https://subscribe.nightmarexiv.com/");
        }
        ImGui.SameLine();
        if(ImGui.Button("Ko-Fi"))
        {
            ShellStart("https://donate.nightmarexiv.com/");
        }
        ImGui.SameLine();
        if(ImGui.Button("Crypto"))
        {
            ShellStart("https://crypto.nightmarexiv.com/");
        }

        ImGuiEx.Text("Thank you for your contributions!".Loc());
        ImGui.PopTextWrapPos();
        ImGui.PopID();
    }
}
