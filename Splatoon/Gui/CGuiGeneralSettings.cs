using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ECommons.LanguageHelpers;
using Splatoon.Modules;
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
            ProcessStart("https://github.com/NightmareXIV/Splatoon#web-api-beta");
        }

        if(ImGui.Checkbox("Enable logging".Loc(), ref P.Config.Logging))
        {
            Logger.OnTerritoryChanged();
        }
        ImGuiComponents.HelpMarker("Enable logging, which will log chat messages, casts and VFX info into log files. ".Loc());

        ImGui.Separator();
        ImGuiEx.TextV("Splatoon language: ".Loc());
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150f.Scale());
        if(ImGui.BeginCombo("##langsel", P.Config.PluginLanguage == null?"Game language".Loc() : P.Config.PluginLanguage.Loc()))
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
        if(ImGui.Button("Save entries: ??".Loc(P.Config.PluginLanguage ?? GameLanguageString)))
        {
            Localization.Save(P.Config.PluginLanguage ?? GameLanguageString);
        }
        ImGui.SameLine();
        if(ImGui.Button("Rescan language files".Loc()))
        {
            GetAvaliableLanguages(true);
        }
        ImGui.Separator();

        SImGuiEx.SizedText("Circle smoothness:".Loc(), WidthLayout);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        ImGui.DragInt("##circlesmoothness", ref p.Config.segments, 0.1f, 10, 150);
        ImGuiComponents.HelpMarker("Higher - smoother circle, higher cpu usage".Loc());

        ImGui.Checkbox("Disable circle fix while enabling drawing circles above your point of view", ref P.Config.NoCircleFix);
        ImGuiComponents.HelpMarker("Do not enable it unless you actually need it. Large circles may be rendered incorrectly under certain camera angle with this option enabled.");

        SImGuiEx.SizedText("Drawing distance:".Loc(), WidthLayout);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        ImGui.DragFloat("##maxdistance", ref p.Config.maxdistance, 0.25f, 10f, 200f);
        ImGuiComponents.HelpMarker("Only try to draw objects that are not further away from you than this value".Loc());

        SImGuiEx.SizedText("Line segments:".Loc(), WidthLayout);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        ImGui.DragInt("##linesegments", ref p.Config.lineSegments, 0.1f, 10, 50);
        p.Config.lineSegments.ValidateRange(10, 100);
        ImGuiComponents.HelpMarker("Increase this if your lines stop drawing too far from the screen edges or if line disappears when you are zoomed in and near it's edge. Increasing this setting hurts performance EXTRAORDINARILY.".Loc());
        if(p.Config.lineSegments > 10)
        {
            ImGuiEx.TextWrapped(ImGuiColors.DalamudOrange, "Non-standard line segment setting. Performance of your game may be impacted. Please CAREFULLY increase this setting until everything works as intended and do not increase it further. \nConsider increasing minimal rectangle fill line thickness to mitigate performance loss, if you will experience it.".Loc());
        }
        if (p.Config.lineSegments > 25)
        {
            ImGuiEx.TextWrapped(Environment.TickCount % 1000 > 500 ? ImGuiColors.DalamudRed : ImGuiColors.DalamudYellow,
                "Your line segment setting IS EXTREMELY HIGH AND MAY SIGNIFICANTLY IMPACT PERFORMANCE.\nIf you really have to set it to this value to make it work, please contact developer and provide details.".Loc());
        }
        ImGui.Separator();
        ImGuiEx.Text("Fill settings:".Loc());
        ImGui.SameLine();
        ImGuiEx.Text("            Screwed up?".Loc());
        ImGui.SameLine();
        if(ImGui.SmallButton("Reset this section".Loc()))
        {
            var def = new Configuration();
            P.Config.AltConeStep = def.AltConeStep;
            P.Config.AltConeStepOverride = def.AltConeStepOverride;
            P.Config.AltDonutStep = def.AltDonutStep;
            P.Config.AltDonutStepOverride = def.AltDonutStepOverride;
            P.Config.AltRectFill = def.AltRectFill;
            P.Config.AltRectForceMinLineThickness = def.AltRectForceMinLineThickness;
            P.Config.AltRectHighlightOutline = def.AltRectHighlightOutline;
            P.Config.AltRectMinLineThickness = def.AltRectMinLineThickness;
            P.Config.AltRectStep = def.AltRectStep;
            P.Config.AltRectStepOverride = def.AltRectStepOverride;
        }
        ImGui.Checkbox("Use line rectangle filling".Loc(), ref p.Config.AltRectFill);
        ImGuiComponents.HelpMarker("Fill rectangles with stroke instead of full color. This will remove clipping issues, but may feel more disturbing.".Loc());
        

        ImGui.SetNextItemWidth(60f);
        ImGui.DragFloat("Minimal rectangle fill line interval".Loc(), ref p.Config.AltRectStep, 0.001f, 0, float.MaxValue);
        ImGui.SameLine();
        ImGui.Checkbox($"{Loc("Always force this value")}##1", ref P.Config.AltRectStepOverride);

        ImGui.SetNextItemWidth(60f);
        ImGui.DragFloat("Minimal rectangle fill line thickness".Loc(), ref p.Config.AltRectMinLineThickness, 0.001f, 0.01f, float.MaxValue);
        ImGuiComponents.HelpMarker("Problems with performance while rectangles are visible? Increase this value.".Loc());
        ImGui.SameLine();
        ImGui.Checkbox($"{Loc("Always force this value")}##2", ref P.Config.AltRectForceMinLineThickness);
        ImGui.Checkbox("Additionally highlight rectangle outline".Loc(), ref p.Config.AltRectHighlightOutline);

        ImGui.SetNextItemWidth(60f);
        ImGui.DragFloat("Minimal donut fill line interval".Loc(), ref p.Config.AltDonutStep, 0.001f, 0.01f, float.MaxValue);
        ImGuiComponents.HelpMarker("Problems with performance while rectangles are visible? Increase this value.".Loc());
        ImGui.SameLine();
        ImGui.Checkbox("Always force this value".Loc()+"##3", ref P.Config.AltDonutStepOverride);

        ImGui.SetNextItemWidth(60f);
        ImGui.DragInt("Minimal cone fill line interval".Loc(), ref p.Config.AltConeStep, 0.1f, 1, int.MaxValue);
        ImGui.SameLine();
        ImGui.Checkbox("Always force this value".Loc()+"##4", ref P.Config.AltConeStepOverride);

        ImGui.Separator();
        ImGui.Checkbox("Use hexadecimal numbers".Loc(), ref p.Config.Hexadecimal);
        ImGui.Checkbox("Enable tether on Splatoon find command".Loc(), ref p.Config.TetherOnFind);
        ImGui.Checkbox("Force show Splatoon's UI when game UI is hidden".Loc(), ref p.Config.ShowOnUiHide);
        ImGui.Checkbox("Disable script cache".Loc(), ref p.Config.DisableScriptCache);
        Svc.PluginInterface.UiBuilder.DisableUserUiHide = p.Config.ShowOnUiHide;
        //ImGui.Checkbox("Always compare names directly (debug option, ~4x performance loss)", ref p.Config.DirectNameComparison);
        if(ImGui.Button("Open backup directory".Loc()))
        {
            ProcessStart(Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "Backups"));
        }
        ImGui.Separator();
        ImGuiEx.Text("Contact developer:".Loc());
        ImGui.SameLine();
        if (ImGui.Button("Github".Loc()))
        {
            ProcessStart("https://github.com/NightmareXIV/Splatoon/issues");
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
