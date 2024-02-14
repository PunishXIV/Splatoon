using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ECommons.LanguageHelpers;
using Splatoon.Modules;
using Splatoon.Serializables;
using Splatoon.Utils;
using Localization = ECommons.LanguageHelpers.Localization;

namespace Splatoon;

partial class CGui
{
    void DisplayGeneralSettings()
    {
        ImGuiEx.Text("Game version: ".Loc());
        ImGui.SameLine(0, 0);
        ImGuiEx.TextCopy(p.loader.gVersion);
        SImGuiEx.SizedText("Use web API".Loc(), WidthLayout);
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
                    ProcessStart("http://127.0.0.1:" + p.Config.port + "/");
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
            ProcessStart("https://github.com/PunishXIV/Splatoon#web-api-beta");
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
        ImGui.SetNextItemWidth(100f);
        if (ImGui.CollapsingHeader("Global Style Overrides"))
        {
            ImGui.Indent();
            SImGuiEx.SizedText("Minimum Fill Alpha:", CGui.WidthElement);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200f);
            ImGui.SliderInt("##minfillalpha", ref P.Config.MinFillAlpha, 0, P.Config.MaxFillAlpha);

            SImGuiEx.SizedText("Maximum Fill Alpha:", CGui.WidthElement);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200f);
            ImGui.SliderInt("##maxfillalpha", ref P.Config.MaxFillAlpha, P.Config.MinFillAlpha, 255);
            // If min == max, users can break ints out of min and max values in the UI. Clamp to sane values for safety.
            P.Config.MinFillAlpha = Math.Clamp(P.Config.MinFillAlpha, 0, P.Config.MaxFillAlpha);
            P.Config.MaxFillAlpha = Math.Clamp(P.Config.MaxFillAlpha, P.Config.MinFillAlpha, 255);
            ImGui.Separator();
            foreach (MechanicType mech in MechanicTypes.Values)
            {
                if (!MechanicTypes.CanOverride(mech)) continue;
                string name = MechanicTypes.Names[(int)mech];
                bool hasOverride = P.Config.StyleOverrides.ContainsKey(mech);

                bool enableOverride = false;
                DisplayStyle style = MechanicTypes.DefaultMechanicColors[mech];
                if (hasOverride)
                {
                    (enableOverride, style) = P.Config.StyleOverrides[mech];
                }

                ImGui.PushStyleColor(ImGuiCol.Text, style.strokeColor);
                SImGuiEx.SizedText(name, CGui.WidthElement);
                ImGui.PopStyleColor();

                ImGui.SameLine();
                ImGui.Checkbox("Override##" + name, ref enableOverride);
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, style.strokeColor);
                if (ImGui.Button("Reset To Default##" + name))
                {
                    style = MechanicTypes.DefaultMechanicColors[mech];
                }
                ImGui.PopStyleColor();

                SImGuiEx.StyleEdit(name, ref style);

                P.Config.StyleOverrides[mech] = new(enableOverride, style);
                ImGui.Separator();
            }
            ImGui.Unindent();
        }

        SImGuiEx.SizedText("Drawing distance:".Loc(), WidthLayout);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        ImGui.DragFloat("##maxdistance", ref p.Config.maxdistance, 0.25f, 10f, 200f);
        ImGuiComponents.HelpMarker("Only try to draw objects that are not further away from you than this value".Loc());

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(
                "Choose a mechanic type that best represents this element.\n" +
                "This is used for automatically setting default colors.");
        }

        if (ImGui.Button("Edit Draw Zones" ))
        {
            P.RenderableZoneSelector.IsOpen = true;
        }
        ImGuiComponents.HelpMarker("Configure screen zones where Splatoon will draw its elements".Loc());

        if (ImGui.Button("Edit Clip Zones"))
        {
            P.ClipZoneSelector.IsOpen = true;
        }
        ImGuiComponents.HelpMarker("Configure screen zones where Splatoon will NOT draw elements. Text is currently not clipped.".Loc());

        ImGui.Checkbox($"Draw Splatoon's element under other plugins elements and windows", ref P.Config.SplatoonLowerZ);
        ImGui.Separator();

        ImGui.Checkbox("Use hexadecimal numbers".Loc(), ref p.Config.Hexadecimal);
        ImGui.Checkbox("Enable tether on Splatoon find command".Loc(), ref p.Config.TetherOnFind);
        ImGui.Checkbox("Force show Splatoon's UI when game UI is hidden".Loc(), ref p.Config.ShowOnUiHide);
        ImGui.Checkbox("Disable script cache".Loc(), ref p.Config.DisableScriptCache);
        Svc.PluginInterface.UiBuilder.DisableUserUiHide = p.Config.ShowOnUiHide;
        //ImGui.Checkbox("Always compare names directly (debug option, ~4x performance loss)", ref p.Config.DirectNameComparison);
        if (ImGui.Button("Open backup directory".Loc()))
        {
            ProcessStart(Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "Backups"));
        }
        ImGui.Separator();
        ImGuiEx.Text("Contact developer:".Loc());
        ImGui.SameLine();
        if (ImGui.Button("Github".Loc()))
        {
            ProcessStart("https://github.com/PunishXIV/Splatoon/issues");
        }
        ImGui.SameLine();
        if (ImGui.Button("Discord".Loc()))
        {
            ImGui.SetClipboardText(Splatoon.DiscordURL);
            Svc.Chat.Print("[Splatoon] Server invite link: ".Loc() + Splatoon.DiscordURL);
            ProcessStart(Splatoon.DiscordURL);
        }
        ImGui.Checkbox("Disable stream notice (effective only after restart)".Loc(), ref P.Config.NoStreamWarning);
    }
}
