using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ECommons.LanguageHelpers;
using NightmareUI.PrimaryUI;
using Splatoon.Modules;
using Splatoon.Serializables;
using Splatoon.Utility;
using Localization = ECommons.LanguageHelpers.Localization;

namespace Splatoon;

partial class CGui
{
    void DisplayGeneralSettings()
    {
        ImGuiEx.Text("Game version: ".Loc());
        ImGui.SameLine(0, 0);
        ImGuiEx.TextCopy(p.loader.gVersion);
        ImGuiUtils.SizedText("Use web API".Loc(), WidthLayout);
        ImGui.SameLine();
        if (ImGui.Checkbox("##usewebapi", ref p.Config.UseHttpServer))
        {
            p.SetupShutdownHttp(p.Config.UseHttpServer);
        }
        ImGui.SameLine();
        if (p.Config.UseHttpServer)
        {
            ImGuiEx.Text("http://127.0.0.1:" + p.Config.port + "/");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && ImGui.GetMouseDragDelta(ImGuiMouseButton.Left) == Vector2.Zero)
                {
                    Utils.ProcessStart("http://127.0.0.1:" + p.Config.port + "/");
                }
            }
        }
        else
        {
            ImGuiEx.Text("Port: ".Loc());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f);
            ImGui.DragInt("##webapiport", ref p.Config.port, float.Epsilon, 1, 65535);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Please only change if you have really good reason".Loc());
            }
            if (p.Config.port < 1 || p.Config.port > 65535) p.Config.port = 47774;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f);
            if (ImGui.Button("Default".Loc()))
            {
                p.Config.port = 47774;
            }
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(250f);
        if (ImGui.Button("Open web API guide".Loc()))
        {
            Utils.ProcessStart("https://github.com/PunishXIV/Splatoon#web-api-beta");
        }

        if (ImGui.Checkbox("Enable logging".Loc(), ref P.Config.Logging))
        {
            Logger.OnTerritoryChanged();
        }
        ImGuiComponents.HelpMarker("Enable logging, which will log chat messages, casts and VFX info into log files. ".Loc());
        ImGui.SameLine();
        ImGui.Checkbox("Log position".Loc(), ref P.Config.LogPosition);
        ImGuiComponents.HelpMarker("Log object position in casting information log lines".Loc());

        ImGui.Separator();
        ImGuiEx.TextV("Splatoon language: ".Loc());
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150f.Scale());
        if (ImGui.BeginCombo("##langsel", P.Config.PluginLanguage == null ? "Game language".Loc() : P.Config.PluginLanguage.Loc()))
        {
            if (ImGui.Selectable("Game language".Loc()))
            {
                P.Config.PluginLanguage = null;
                Localization.Init(GameLanguageString);
            }
            foreach (var x in GetAvaliableLanguages())
            {
                if (ImGui.Selectable(x.Loc()))
                {
                    P.Config.PluginLanguage = x;
                    Localization.Init(P.Config.PluginLanguage);
                }
            }
            ImGui.EndCombo();
        }
        ImGui.Checkbox("Localization logging".Loc(), ref Localization.Logging);
        ImGui.SameLine();
        if (ImGui.Button("Save entries: ??".Loc(P.Config.PluginLanguage ?? GameLanguageString)))
        {
            Localization.Save(P.Config.PluginLanguage ?? GameLanguageString);
        }
        ImGui.SameLine();
        if (ImGui.Button("Rescan language files".Loc()))
        {
            GetAvaliableLanguages(true);
        }
        ImGui.Separator();

        ImGui.Checkbox("Use hexadecimal numbers".Loc(), ref p.Config.Hexadecimal);
        ImGui.Checkbox("Enable tether on Splatoon find command".Loc(), ref p.Config.TetherOnFind);
        ImGui.Checkbox("Force show Splatoon's UI when game UI is hidden".Loc(), ref p.Config.ShowOnUiHide);
        ImGui.Checkbox("Disable script cache".Loc(), ref p.Config.DisableScriptCache);
        Svc.PluginInterface.UiBuilder.DisableUserUiHide = p.Config.ShowOnUiHide;
        //ImGui.Checkbox("Always compare names directly (debug option, ~4x performance loss)", ref p.Config.DirectNameComparison);
        if (ImGui.Button("Open backup directory".Loc()))
        {
            Utils.ProcessStart(Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "Backups"));
        }
        ImGui.Separator();
        ImGuiEx.Text("Contact developer:".Loc());
        ImGui.SameLine();
        if (ImGui.Button("Github".Loc()))
        {
            Utils.ProcessStart("https://github.com/PunishXIV/Splatoon/issues");
        }
        ImGui.SameLine();
        if (ImGui.Button("Discord".Loc()))
        {
            ImGui.SetClipboardText(Splatoon.DiscordURL);
            Svc.Chat.Print("[Splatoon] Server invite link: ".Loc() + Splatoon.DiscordURL);
            Utils.ProcessStart(Splatoon.DiscordURL);
        }
        ImGui.Checkbox("Disable stream notice (effective only after restart)".Loc(), ref P.Config.NoStreamWarning);

        new NuiBuilder().Section("Script auto-reloading (for developers)", collapsible: true)
        .TextWrapped("Add pathes to folders that contain scripts that you are editing. Do NOT add Splatoon's own configuration folder here.")
        .Widget(() =>
        {
            for(int i = 0; i < P.Config.FileWatcherPathes.Count; i++)
            {
                var index = i;
                var f = P.Config.FileWatcherPathes[i];
                ImGuiEx.InputWithRightButtonsArea(() =>
                {
                    if(ImGui.InputTextWithHint("##path to folder", "Path to folder...", ref f, 2000))
                    {
                        P.Config.FileWatcherPathes[index] = f;
                    }
                }, () =>
                {
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                    {
                        new TickScheduler(() => P.Config.FileWatcherPathes.RemoveAt(index));
                    }
                });
            }
            ImGuiEx.LineCentered(() =>
            {
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add New"))
                {
                    P.Config.FileWatcherPathes.Add("");
                }
                ImGui.SameLine();
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Check, "Apply settings"))
                {
                    S.ScriptFileWatcher.StartWatching();
                }
            });
        }).Draw();
    }
}
