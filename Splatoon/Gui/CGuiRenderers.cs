using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ECommons.ImGuiMethods;
using ECommons.LanguageHelpers;
using NightmareUI.PrimaryUI;
using Splatoon.RenderEngines;
using Splatoon.Serializables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon;
partial class CGui
{
    void DisplayRenderers()
    {
        new NuiBuilder()
            .Section("Common Settings")
            .Widget(() =>
            {
                ImGuiEx.TextWrapped($"""
                    Splatoon supports few renderers. On this page, you can select which ones you want to use, configure them and set one of them as default.
                    Render engine can be set globally and per-element. When set render engine is not available, either due to load error or because user has disabled it, other available render engine will be used automatically.
                    Settings present in this section affect all available renderes.
                    """);
                ImGui.Separator();
                ImGuiUtils.SizedText("Drawing distance:".Loc(), WidthLayout);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.DragFloat("##maxdistance", ref p.Config.maxdistance, 0.25f, 10f, 200f);
                ImGuiComponents.HelpMarker("Only try to draw objects that are not further away from you than this value".Loc());

                if (ImGui.Button("Edit Draw Zones"))
                {
                    P.RenderableZoneSelector.IsOpen = true;
                }
                ImGuiComponents.HelpMarker("Configure screen zones where Splatoon will draw its elements".Loc());
                ImGui.Checkbox($"Draw Splatoon's element under other plugins elements and windows".Loc(), ref P.Config.SplatoonLowerZ);
            })

            .Section("DirectX11 Renderer")
            .Widget(() =>
            {
                ImGuiEx.Text($"DirectX11 Render made by SourP. ");
                S.RenderManager.DrawCommonSettings(RenderEngineKind.DirectX11);

                ImGuiUtils.SizedText("Alpha Blend Mode:".Loc(), WidthLayout);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGuiUtils.EnumCombo("##alphablendmode", ref p.Config.AlphaBlendMode, AlphaBlendModes.Names, AlphaBlendModes.Tooltips);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Change how overlapping elements' transparency is blended");
                }

                ImGui.Checkbox("Automatically clip Splatoon's elements around native UI elements and windows", ref P.Config.AutoClipNativeUI);
                ImGuiComponents.HelpMarker("Some native elements are not supported, but they may be added later. Text is currently not clipped.".Loc());

                if (ImGui.Button("Edit Clip Zones"))
                {
                    P.ClipZoneSelector.IsOpen = true;
                }
                ImGuiComponents.HelpMarker("Configure screen zones where Splatoon will NOT draw elements. Text is currently not clipped.".Loc());

                if (ImGui.CollapsingHeader("Global Style Overrides"))
                {
                    ImGui.Indent();
                    ImGuiUtils.SizedText("Minimum Fill Alpha:", CGui.WidthElement);
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200f);
                    ImGui.SliderInt("##minfillalpha", ref P.Config.ElementMinFillAlpha, 0, P.Config.ElementMaxFillAlpha);

                    ImGuiUtils.SizedText("Maximum Fill Alpha:", CGui.WidthElement);
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200f);
                    ImGui.SliderInt("##maxfillalpha", ref P.Config.ElementMaxFillAlpha, P.Config.ElementMinFillAlpha, 255);

                    ImGuiUtils.SizedText("Maximum Alpha:", CGui.WidthElement);
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200f);
                    ImGui.SliderInt("##maxalpha", ref P.Config.MaxAlpha, P.Config.ElementMaxFillAlpha, 255);
                    ImGuiComponents.HelpMarker("The maximum alpha used for drawing.\nThis will only take effect for strokes or if using Alpha Blend Mode 'Add'. ".Loc());
                    // If min == max, users can break ints out of min and max values in the UI. Clamp to sane values for safety.
                    P.Config.ElementMinFillAlpha = Math.Clamp(P.Config.ElementMinFillAlpha, 0, P.Config.ElementMaxFillAlpha);
                    P.Config.ElementMaxFillAlpha = Math.Clamp(P.Config.ElementMaxFillAlpha, P.Config.ElementMinFillAlpha, 255);
                    P.Config.MaxAlpha = Math.Clamp(P.Config.MaxAlpha, P.Config.ElementMaxFillAlpha, 255);

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
                        ImGuiUtils.SizedText(name, CGui.WidthElement);
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

                        ImGuiUtils.StyleEdit(name, ref style);

                        P.Config.StyleOverrides[mech] = new(enableOverride, style);
                        ImGui.Separator();
                    }
                    ImGui.Unindent();
                }
            })

            .Section("Legacy ImGui Renderer")
            .Widget(() =>
            {
                ImGuiEx.Text($"Default rendering engine. ");
                S.RenderManager.DrawCommonSettings(RenderEngineKind.ImGui_Legacy);

                ImGuiUtils.SizedText("Circle smoothness:".Loc(), WidthLayout);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.DragInt("##circlesmoothness", ref p.Config.segments, 0.1f, 10, 150);
                ImGuiComponents.HelpMarker("Higher - smoother circle, higher cpu usage".Loc());

                ImGui.Checkbox("Disable circle fix while enabling drawing circles above your point of view".Loc(), ref P.Config.NoCircleFix);
                ImGuiComponents.HelpMarker("Do not enable it unless you actually need it. Large circles may be rendered incorrectly under certain camera angle with this option enabled.");

                ImGuiUtils.SizedText("Line segments:".Loc(), WidthLayout);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.DragInt("##linesegments", ref p.Config.lineSegments, 0.1f, 10, 50);
                p.Config.lineSegments.ValidateRange(10, 100);
                ImGuiComponents.HelpMarker("Increase this if your lines stop drawing too far from the screen edges or if line disappears when you are zoomed in and near it's edge. Increasing this setting hurts performance EXTRAORDINARILY.".Loc());
                if (p.Config.lineSegments > 10)
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
                if (ImGui.SmallButton("Reset this section".Loc()))
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

                ImGui.Checkbox("Fill cones".Loc(), ref p.Config.FillCone);

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
                ImGui.Checkbox("Always force this value".Loc() + "##3", ref P.Config.AltDonutStepOverride);

                ImGui.SetNextItemWidth(60f);
                ImGui.DragInt("Minimal cone fill line interval".Loc(), ref p.Config.AltConeStep, 0.1f, 1, int.MaxValue);
                ImGui.SameLine();
                ImGui.Checkbox("Always force this value".Loc() + "##4", ref P.Config.AltConeStepOverride);
                ImGui.Checkbox($"Use full donut filling".Loc(), ref P.Config.UseFullDonutFill);

            })

            .Draw();
    }
}
