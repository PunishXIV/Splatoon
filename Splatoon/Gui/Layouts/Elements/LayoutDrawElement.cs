﻿using Dalamud.Game;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameFunctions;
using ECommons.LanguageHelpers;
using Lumina.Excel.Sheets;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Splatoon.RenderEngines;
using Splatoon.Serializables;
using Splatoon.SplatoonScripting;

namespace Splatoon;

internal unsafe partial class CGui
{
    private string ActionName = "";
    private string BuffName = "";
    internal void LayoutDrawElement(Layout l, Element el, bool forceEnable = false)
    {
        ImGui.Checkbox("Enabled".Loc(), ref el.Enabled);
        if(el.IsVisible())
        {
            ImGuiEx.HelpMarker("This element is currently being rendered".Loc(), EColor.GreenBright, FontAwesomeIcon.Eye.ToIconString());
        }
        else
        {
            ImGuiEx.HelpMarker("This element is currently not being rendered".Loc(), EColor.White, FontAwesomeIcon.EyeSlash.ToIconString());
        }
        ImGui.SameLine();
        if(ImGui.Button("Copy as HTTP param".Loc()))
        {
            HTTPExportToClipboard(el);
        }
        if(ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Hold ALT to copy raw JSON (for usage with post body or you'll have to urlencode it yourself)\nHold CTRL and click to copy urlencoded raw".Loc());
        }
        ImGui.SameLine();
        if(ImGui.Button("Copy to clipboard".Loc()))
        {
            ImGui.SetClipboardText(JsonConvert.SerializeObject(el, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }));
            Notify.Success("Copied to clipboard".Loc());
        }

        ImGui.SameLine();
        if(ImGui.Button("Copy style".Loc()))
        {
            p.Clipboard = JsonConvert.DeserializeObject<Element>(JsonConvert.SerializeObject(el));
        }
        if(p.Clipboard != null)
        {
            ImGui.SameLine();
            if(ImGui.Button("Paste style".Loc()))
            {
                el.color = p.Clipboard.color;
                el.thicc = p.Clipboard.thicc;

                if(p.Clipboard.Filled)
                {
                    el.Filled = p.Clipboard.Filled;
                    el.fillIntensity = p.Clipboard.fillIntensity;
                    if(p.Clipboard.overrideFillColor)
                    {
                        el.overrideFillColor = p.Clipboard.overrideFillColor;
                        el.originFillColor = p.Clipboard.originFillColor;
                        el.endFillColor = p.Clipboard.endFillColor;
                    }
                }

                if(p.Clipboard.castAnimation != CastAnimationKind.Unspecified)
                {
                    el.castAnimation = p.Clipboard.castAnimation;
                    el.animationColor = p.Clipboard.animationColor;
                    el.pulseSize = p.Clipboard.pulseSize;
                    el.pulseFrequency = p.Clipboard.pulseFrequency;
                }

                el.overlayBGColor = p.Clipboard.overlayBGColor;
                el.overlayTextColor = p.Clipboard.overlayTextColor;
                el.tether = p.Clipboard.tether;
                el.ExtraTetherLength = p.Clipboard.ExtraTetherLength;
                el.LineEndA = p.Clipboard.LineEndA;
                el.LineEndB = p.Clipboard.LineEndB;
                el.overlayVOffset = p.Clipboard.overlayVOffset;
                if(ImGui.GetIO().KeyCtrl)
                {
                    el.radius = p.Clipboard.radius;
                    el.includeHitbox = p.Clipboard.includeHitbox;
                    el.includeOwnHitbox = p.Clipboard.includeOwnHitbox;
                    el.includeRotation = p.Clipboard.includeRotation;
                    el.onlyTargetable = p.Clipboard.onlyTargetable;
                }
                if(ImGui.GetIO().KeyShift && el.type != 2)
                {
                    el.refX = p.Clipboard.refX;
                    el.refY = p.Clipboard.refY;
                    el.refZ = p.Clipboard.refZ;
                }
            }
            if(ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGuiEx.Text("Copied style:".Loc());
                ImGuiEx.Text($"Color: 0x{p.Clipboard.color:X8}");
                ImGui.SameLine();
                ImGuiUtils.DisplayColor(p.Clipboard.color);
                ImGuiEx.Text($"Thickness: {p.Clipboard.thicc}");
                if(p.Clipboard.Filled)
                {
                    if(p.Clipboard.overrideFillColor)
                    {
                        ImGuiEx.Text($"Origin Fill Color: 0x{p.Clipboard.originFillColor:X8}");
                        ImGui.SameLine();
                        ImGuiUtils.DisplayColor(p.Clipboard.originFillColor ?? 0);
                        ImGuiEx.Text($"End Fill Color: 0x{p.Clipboard.endFillColor:X8}");
                        ImGui.SameLine();
                        ImGuiUtils.DisplayColor(p.Clipboard.endFillColor ?? 0);
                    }
                    else
                    {
                        ImGuiEx.Text($"Fill Intensity: {p.Clipboard.fillIntensity}");
                    }
                }
                if(p.Clipboard.castAnimation != CastAnimationKind.Unspecified)
                {
                    ImGuiEx.Text($"Animation: {CastAnimations.Names[(int)p.Clipboard.castAnimation]}");
                    ImGuiEx.Text($"Animation Color: 0x{p.Clipboard.animationColor:X8}");
                    ImGui.SameLine();
                    ImGuiUtils.DisplayColor(p.Clipboard.animationColor);
                    if(p.Clipboard.castAnimation == CastAnimationKind.Pulse)
                    {
                        ImGuiEx.Text($"Pulse Size: {p.Clipboard.pulseSize}");
                        ImGuiEx.Text($"Pulse Frequency: {p.Clipboard.pulseFrequency}");
                    }
                }
                ImGuiEx.Text($"Overlay BG color: 0x{p.Clipboard.overlayBGColor:X8}");
                ImGui.SameLine();
                ImGuiUtils.DisplayColor(p.Clipboard.overlayBGColor);
                ImGuiEx.Text($"Overlay text color: 0x{p.Clipboard.overlayTextColor:X8}");
                ImGui.SameLine();
                ImGuiUtils.DisplayColor(p.Clipboard.overlayTextColor);
                ImGuiEx.Text($"Overlay vertical offset: {p.Clipboard.overlayVOffset}");
                ImGuiEx.Text($"Tether: {p.Clipboard.tether}");
                ImGui.Separator();
                ImGuiEx.Text((ImGui.GetIO().KeyCtrl ? Colors.Green : Colors.Gray).ToVector4(),
                    "Holding CTRL when clicking will also paste:".Loc());
                ImGuiEx.Text($"Radius: {p.Clipboard.radius}");
                ImGuiEx.Text($"Include target hitbox: {p.Clipboard.includeHitbox}");
                ImGuiEx.Text($"Include own hitbox: {p.Clipboard.includeOwnHitbox}");
                ImGuiEx.Text($"Include rotation: {p.Clipboard.includeRotation}");
                ImGuiEx.Text($"Only targetable: {p.Clipboard.onlyTargetable}");
                ImGui.Separator();
                ImGuiEx.Text((ImGui.GetIO().KeyShift ? Colors.Green : Colors.Gray).ToVector4(),
                    "Holding SHIFT when clicking will also paste:".Loc());
                ImGuiEx.Text($"X offset: {p.Clipboard.offX}");
                ImGuiEx.Text($"Y offset: {p.Clipboard.offY}");
                ImGuiEx.Text($"Z offset: {p.Clipboard.offZ}");

                ImGui.EndTooltip();
            }
        }


        ImGuiUtils.SizedText("Conditional:".Loc(), WidthElement);
        ImGui.SameLine();

        ImGui.SameLine();
        ImGui.Checkbox("##Conditional", ref el.Conditional);
        ImGuiEx.HelpMarker("""
            Conditional element will serve as conditional trigger for any elements lower than this in current layout. 
            If this element is visible, elements below will also be shown. If there are multiple sequential conditional elements, they will be merged using rules defined in the layout. This is advanced feature that allows displaying elements, for example, when a certain buff is present on a player, boss, allows drawing elements around players when boss is casting something, etc.
            
            If another conditional element is encountered after normal element, it will be processed together with the rules defined in the layout. For example:
            - if element structure is A1B2, where A and B - conditional elements, 1 and 2 - normal elements:
            - - in OR mode if A is shown but B is hidden, both 1 and 2 will be shown; 
            - - in AND mode - only 1 will be shown. 
            - If A is hidden but B is shown:
            - - OR mode will display only 2;
            - - AND mode will display nothing at all. 

            Conditional elements may serve as normal elements and display information on their own, or simply be hidden service elements.
            """);
        ImGui.SameLine();
        ImGui.Checkbox("Invert condition", ref el.ConditionalInvert);
        ImGui.SameLine();
        ImGui.Checkbox("Reset condition", ref el.ConditionalReset);
        ImGuiEx.HelpMarker("Upon reaching this element, previous conditions will be reset");

        ImGuiUtils.SizedText("Name:".Loc(), WidthElement);
        ImGui.SameLine();
        ImGuiEx.SetNextItemFullWidth();
        ImGui.InputText("##Name", ref el.Name, 100);

        ImGuiUtils.SizedText("Intl. Name:".Loc(), WidthElement);
        ImGui.SameLine();
        ImGuiEx.SetNextItemFullWidth();
        el.InternationalName.ImGuiEdit(ref el.Name);

        ImGuiUtils.SizedText("Element type:".Loc(), WidthElement);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(WidthCombo);
        if(ImGui.Combo("##elemselecttype", ref el.type, Element.ElementTypes, Element.ElementTypes.Length))
        {
            if((el.type == 2 || el.type == 3) && el.radius == 0.35f)
            {
                el.radius = 0;
            }
        }
        if(el.type.EqualsAny(4, 5))
        {
            el.includeRotation = true;
        }
        if(el.type.EqualsAny(1, 3, 4, 5))
        {
            ImGuiUtils.SizedText("Account for rotation:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox("##rota", ref el.includeRotation);
            if(el.includeRotation)
            {
                DrawRotationSelector(el);
            }
            ImGuiUtils.SizedText("Override rotation:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox("##rotaOverride", ref el.RotationOverride);
            if(el.RotationOverride)
            {
                ImGui.SameLine();
                ImGuiEx.TextV("Rotate towards:");
                ImGui.SameLine();
                ImGuiEx.Text($"X:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                ImGui.DragFloat("##rotateTowardsX", ref el.RotationOverridePoint.X, 0.1f);
                ImGui.SameLine();
                ImGuiEx.Text($"Y:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                ImGui.DragFloat("##rotateTowardsY", ref el.RotationOverridePoint.Y, 0.1f);
                ImGui.SameLine();
                ImGuiEx.Text($"Add angle:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                ImGui.DragFloat("##rotationOverrideAddAngle", ref el.RotationOverrideAddAngle, 0.1f);
            }
        }
        if(el.type.EqualsAny(1, 3, 4))
        {

            ImGuiUtils.SizedText("Targeted object: ".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(WidthCombo);
            ImGui.Combo("##actortype", ref el.refActorType, Element.ActorTypes, Element.ActorTypes.Length);
            if(el.refActorType == 0)
            {
                ImGui.SameLine();
                if(ImGui.Button("Copy settarget command".Loc()))
                {
                    ImGui.SetClipboardText("/splatoon settarget " + l.Name + "~" + el.Name);
                }
                if(ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("This command allows you to quickly change\nsearch attributes to your active target's name.\nYou can use it with macro.".Loc());
                }
                ImGui.SetNextItemWidth(WidthElement + ImGui.GetStyle().ItemSpacing.X);
                if(ImGui.BeginCombo("##compare", el.refActorComparisonAnd ? "Multiple attributes".Loc() : "Single attribute".Loc()))
                {
                    if(ImGui.Selectable("Match one attribute".Loc()))
                    {
                        el.refActorComparisonAnd = false;
                    }
                    if(ImGui.Selectable("Match multiple attributes (AND logic)".Loc()))
                    {
                        el.refActorComparisonAnd = true;
                    }
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(75f);
                ImGui.Combo($"##attrSelect", ref el.refActorComparisonType, Element.ComparisonTypes, Element.ComparisonTypes.Length);
                ImGui.SameLine();
                if(el.refActorComparisonType == 0)
                {
                    ImGui.SetNextItemWidth(150f);
                    //ImGui.InputText("##actorname", ref el.refActorName, 100);
                    el.refActorNameIntl.ImGuiEdit(ref el.refActorName);
                    if(NameNpcIDs.TryGetValue(el.refActorNameIntl.Get(el.refActorName).ToLower(), out var nameid))
                    {
                        ImGui.SameLine();
                        if(ImGui.Button($"Name ID: ??, convert?".Loc(nameid.Format()) + "##{i + k}"))
                        {
                            el.refActorComparisonType = 6;
                            el.refActorNPCNameID = nameid;
                        }
                        ImGuiComponents.HelpMarker("Name ID has been found for this string. If you will convert comparison to Name ID, it will make element work with any languare. In addition, such conversion will provide a performance boost.\n\nSelection by Name ID will target only Characters (usually it's fine). If you're targeting GameObject, EventObj or EventNpc, do not convert.".Loc());
                    }
                }
                else if(el.refActorComparisonType == 1)
                {
                    ImGuiUtils.InputUintDynamic("##actormid", ref el.refActorModelID);
                }
                else if(el.refActorComparisonType == 2)
                {
                    ImGuiUtils.InputUintDynamic("##actoroid", ref el.refActorObjectID);
                }
                else if(el.refActorComparisonType == 3)
                {
                    ImGuiUtils.InputUintDynamic("##actordid", ref el.refActorDataID);
                }
                else if(el.refActorComparisonType == 4)
                {
                    ImGuiUtils.InputUintDynamic("##npcid", ref el.refActorNPCID);
                }
                else if(el.refActorComparisonType == 5)
                {
                    ImGui.SetNextItemWidth(200f);
                    ImGuiEx.InputListString("##pholder", el.refActorPlaceholder);
                    ImGui.SameLine();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.AngleDoubleDown))
                    {
                        ImGui.OpenPopup("PlaceholderFastSelect");
                    }
                    if(ImGui.BeginPopup("PlaceholderFastSelect"))
                    {
                        for(var s = 1; s <= 8; s++)
                        {
                            if(ImGui.Selectable($"<{s}>", false, ImGuiSelectableFlags.DontClosePopups)) el.refActorPlaceholder.Add($"<{s}>");
                        }
                        if(ImGui.Selectable("2-8", false, ImGuiSelectableFlags.DontClosePopups))
                        {
                            for(var s = 2; s <= 8; s++)
                            {
                                el.refActorPlaceholder.Add($"<{s}>");
                            }
                        }
                        ImGui.EndPopup();
                    }
                    ImGuiComponents.HelpMarker(("Placeholder like you'd type in macro <1>, <2>, <mo> etc. You can add multiple." +
                        "\nAdditional placeholders are supported:" +
                        "\n<d1>, <d2>, <d3> etc - DPS player in a party" +
                        "\n<h1>, <h2> etc - Healer player in a party" +
                        "\n<t1>, <t2> etc - Tank player in a party" +
                        "\nNumber corresponds to the party list.").Loc());
                }
                else if(el.refActorComparisonType == 6)
                {
                    ImGuiUtils.InputUintDynamic("##nameID", ref el.refActorNPCNameID);
                    var npcnames = NameNpcIDsAll.FindKeysByValue(el.refActorNPCNameID);
                    if(npcnames.Any())
                    {
                        ImGuiComponents.HelpMarker($"{"NPC".Loc()}: \n{npcnames.Join("\n")}");
                    }
                }
                else if(el.refActorComparisonType == 7)
                {
                    ImGui.SetNextItemWidth(150f);
                    ImGui.InputText("##vfx", ref el.refActorVFXPath, 500);
                    ImGui.SameLine();
                    ImGuiEx.Text("Age:".Loc());
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    var a1 = (float)el.refActorVFXMin / 1000f;
                    if(ImGui.DragFloat("##age1", ref a1, 0.1f, 0, 99999, $"{a1:F1}"))
                    {
                        el.refActorVFXMin = (int)(a1 * 1000);
                    }
                    ImGui.SameLine();
                    ImGuiEx.Text("-");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);

                    var a2 = (float)el.refActorVFXMax / 1000f;
                    if(ImGui.DragFloat("##age2", ref a2, 0.1f, 0, 99999, $"{a2:F1}"))
                    {
                        el.refActorVFXMax = (int)(a2 * 1000);
                    }
                }
                else if(el.refActorComparisonType == 8)
                {
                    ImGui.SetNextItemWidth(50f);
                    ImGuiEx.InputUint("##edata1", ref el.refActorObjectEffectData1);
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGuiEx.InputUint("##edata2", ref el.refActorObjectEffectData2);
                    ImGui.SameLine();
                    ImGui.Checkbox($"Last only", ref el.refActorObjectEffectLastOnly);
                    if(!el.refActorObjectEffectLastOnly)
                    {
                        ImGui.SameLine();
                        ImGuiEx.Text("Age:".Loc());
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(50f);
                        var a1 = (float)el.refActorObjectEffectMin / 1000f;
                        if(ImGui.DragFloat("##eage1", ref a1, 0.1f, 0, 99999, $"{a1:F1}"))
                        {
                            el.refActorObjectEffectMin = (int)(a1 * 1000);
                        }
                        ImGui.SameLine();
                        ImGuiEx.Text("-");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(50f);

                        var a2 = (float)el.refActorObjectEffectMax / 1000f;
                        if(ImGui.DragFloat("##eage2", ref a2, 0.1f, 0, 99999, $"{a2:F1}"))
                        {
                            el.refActorObjectEffectMax = (int)(a2 * 1000);
                        }
                    }
                }
                else if(el.refActorComparisonType == 9)
                {
                    ImGui.SetNextItemWidth(200f);
                    ImGuiEx.InputUint("##nameplateiconid", ref el.refActorNamePlateIconID);
                    if(ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Decimal input");
                    }
                }

                if(Svc.Targets.Target != null && !el.refActorComparisonType.EqualsAny(7, 8))
                {
                    ImGui.SameLine();
                    if(ImGui.Button("Target".Loc() + "##btarget"))
                    {
                        el.refActorNameIntl.CurrentLangString = Svc.Targets.Target.Name.ToString();
                        el.refActorDataID = Svc.Targets.Target.DataId;
                        el.refActorObjectID = Svc.Targets.Target.EntityId;
                        if(Svc.Targets.Target is ICharacter c)
                        {
                            el.refActorModelID = (uint)c.Struct()->ModelContainer.ModelCharaId;
                            el.refActorNPCNameID = c.NameId;
                        }
                        el.refActorNPCID = Svc.Targets.Target.Struct()->GetNameId();
                        el.refActorNamePlateIconID = Svc.Targets.Target.Struct()->NamePlateIconId;
                    }
                }
                ImGuiUtils.SizedText("Targetability: ".Loc(), WidthElement);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                if(ImGui.BeginCombo($"##TargetabilityCombo", el.onlyTargetable ? "Targetable".Loc() : (el.onlyUnTargetable ? "Untargetable".Loc() : "Any".Loc())))
                {
                    if(ImGui.Selectable("Any".Loc()))
                    {
                        el.onlyTargetable = false;
                        el.onlyUnTargetable = false;
                    }
                    if(ImGui.Selectable("Targetable only".Loc()))
                    {
                        el.onlyTargetable = true;
                        el.onlyUnTargetable = false;
                    }
                    if(ImGui.Selectable("Untargetable only".Loc()))
                    {
                        el.onlyTargetable = false;
                        el.onlyUnTargetable = true;
                    }
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                ImGui.Checkbox("Visible characters only".Loc(), ref el.onlyVisible);
                if(ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Setting this checkbox will also restrict search to characters ONLY. \n(character - is a player, companion or friendly/hostile NPC that can fight and have HP)".Loc());
                }
            }

            ImGui.SetNextItemWidth(WidthElement + ImGui.GetStyle().ItemSpacing.X);
            if(ImGui.BeginCombo("##whilecasting", el.refActorCastReverse ? "While NOT casting".Loc() : "While casting".Loc()))
            {
                if(ImGui.Selectable("While casting".Loc())) el.refActorCastReverse = false;
                if(ImGui.Selectable("While NOT casting".Loc())) el.refActorCastReverse = true;
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            ImGui.Checkbox("##casting", ref el.refActorRequireCast);
            if(el.refActorRequireCast)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(WidthCombo);
                ImGuiEx.InputListUint("##casts", el.refActorCastId, ActionNames);
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.Text("Add all by name:".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.InputText("##ActionName", ref ActionName, 100);
                ImGui.SameLine();
                if(ImGui.Button("Add".Loc() + "##byactionname"))
                {
                    foreach(var lang in (ClientLanguage?[])[null, ClientLanguage.English])
                    {
                        foreach(var x in Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>(lang))
                        {
                            if(x.Name.ToString().Equals(ActionName, StringComparison.OrdinalIgnoreCase))
                            {
                                el.refActorCastId.Add(x.RowId);
                            }
                        }
                    }
                }
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGui.Checkbox("Limit by cast time".Loc(), ref el.refActorUseCastTime);
                if(el.refActorUseCastTime)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##casttime1", ref el.refActorCastTimeMin, 0.1f, 0f, 99999f, $"{el.refActorCastTimeMin:F1}");
                    ImGui.SameLine();
                    ImGuiEx.Text("-");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##casttime2", ref el.refActorCastTimeMax, 0.1f, 0f, 99999f, $"{el.refActorCastTimeMax:F1}");
                    ImGui.SameLine();
                    ImGui.Checkbox("Overcast".Loc(), ref el.refActorUseOvercast);
                    ImGuiComponents.HelpMarker("Enable use of cast values that exceed cast time, effectively behaving like cast bar would continue to be displayed after cast already happened".Loc());
                }
            }

            ImGuiUtils.SizedText("Status requirement:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox("##buffreq", ref el.refActorRequireBuff);
            if(el.refActorRequireBuff)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(WidthCombo);
                ImGuiEx.InputListUint("##buffs", el.refActorBuffId, BuffNames);
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.Text("Add all by name:".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.InputText("##BuffNames", ref BuffName, 100);
                ImGui.SameLine();
                if(ImGui.Button("Add".Loc() + "##bybuffname"))
                {
                    foreach(var lang in (ClientLanguage?[])[null, ClientLanguage.English])
                    {
                        foreach(var x in Svc.Data.GetExcelSheet<Status>(lang))
                        {
                            if(x.Name.ToString().Equals(BuffName, StringComparison.OrdinalIgnoreCase))
                            {
                                el.refActorBuffId.Add(x.RowId);
                            }
                        }
                    }
                }
                if(Svc.Targets.Target != null && Svc.Targets.Target is IBattleChara bchr)
                {
                    ImGui.SameLine();
                    if(ImGui.Button("Add from target".Loc() + "##bybuffname"))
                    {
                        el.refActorBuffId.AddRange(bchr.StatusList.Select(x => x.StatusId));
                    }
                }
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGui.Checkbox("Limit by remaining time".Loc(), ref el.refActorUseBuffTime);
                if(el.refActorUseBuffTime)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##btime1", ref el.refActorBuffTimeMin, 0.1f, 0f, 99999f, $"{el.refActorBuffTimeMin:F1}");
                    ImGui.SameLine();
                    ImGuiEx.Text("-");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##btime2", ref el.refActorBuffTimeMax, 0.1f, 0f, 99999f, $"{el.refActorBuffTimeMax:F1}");
                }
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGui.Checkbox("Check for status param".Loc(), ref el.refActorUseBuffParam);
                if(el.refActorUseBuffParam)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150f);
                    ImGui.InputInt("##btime1", ref el.refActorBuffParam);
                }
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGui.Checkbox((el.refActorRequireBuffsInvert ? "Require ANY status to be missing".Loc() + "##" : "Require ALL listed statuses to be present".Loc() + "##"), ref el.refActorRequireAllBuffs);
                ImGui.SameLine();
                ImGui.Checkbox("Invert behavior".Loc(), ref el.refActorRequireBuffsInvert);
            }

            ImGuiUtils.SizedText("Distance limit".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox("##dstLim", ref el.LimitDistance);
            if(el.LimitDistance)
            {
                ImGui.SameLine();
                ImGuiEx.Text("X:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60f);
                ImGui.DragFloat("##distX", ref el.DistanceSourceX, 0.02f, float.MinValue, float.MaxValue);
                ImGui.SameLine();
                ImGuiEx.Text("Y:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60f);
                ImGui.DragFloat("##distY", ref el.DistanceSourceY, 0.02f, float.MinValue, float.MaxValue);
                ImGui.SameLine();
                ImGuiEx.Text("Z:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60f);
                ImGui.DragFloat("##distZ", ref el.DistanceSourceZ, 0.02f, float.MinValue, float.MaxValue);
                ImGui.SameLine();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Circle, "0 0 0##dist"))
                {
                    el.DistanceSourceX = 0;
                    el.DistanceSourceY = 0;
                    el.DistanceSourceZ = 0;
                }
                ImGuiEx.Tooltip("0 0 0");
                ImGui.SameLine();
                if(ImGuiEx.IconButton(FontAwesomeIcon.MapMarked, "My position".Loc() + "##dist"))
                {
                    el.DistanceSourceX = Utils.GetPlayerPositionXZY().X;
                    el.DistanceSourceY = Utils.GetPlayerPositionXZY().Y;
                    el.DistanceSourceZ = Utils.GetPlayerPositionXZY().Z;
                }
                ImGuiEx.Tooltip("My position");
                ImGui.SameLine();
                if(ImGuiEx.IconButton(FontAwesomeIcon.MousePointer, "Screen2World".Loc() + "##dist"))
                {
                    SetCursorTo(el.DistanceSourceX, el.DistanceSourceY, el.DistanceSourceZ);
                    p.BeginS2W(el, "DistanceSourceX", "DistanceSourceY", "DistanceSourceZ");
                }
                ImGuiEx.Tooltip("Select on screen".Loc());
                ImGui.SameLine();
                DrawRounding(ref el.DistanceSourceX, ref el.DistanceSourceY, ref el.DistanceSourceZ);
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                ImGui.DragFloat("##dstmin", ref el.DistanceMin, 0.1f, 0f, 99999f, $"{el.DistanceMin:F1}");
                ImGui.SameLine();
                ImGuiEx.Text("-");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                ImGui.DragFloat("##dstmax", ref el.DistanceMax, 0.1f, 0f, 99999f, $"{el.DistanceMax:F1}");
                ImGui.SameLine();
                ImGui.Checkbox("Invert".Loc() + "##dist", ref el.LimitDistanceInvert);
            }


            ImGuiUtils.SizedText("Rotation limit".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox("##rotaLimit", ref el.LimitRotation);
            if(el.LimitRotation)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                var rot1 = 180 - el.RotationMin.RadiansToDegrees();
                if(ImGui.DragFloat("##rotamax", ref rot1, 0.1f, -360f, 360f, $"{rot1:F1}"))
                {
                    el.RotationMin = (180 - rot1).DegreesToRadians();
                }

                ImGui.SameLine();
                ImGuiEx.Text("-");
                ImGui.SameLine();


                ImGui.SetNextItemWidth(50f);
                var rot2 = 180 - el.RotationMax.RadiansToDegrees();
                if(ImGui.DragFloat("##rotamin", ref rot2, 0.1f, -360f, 360f, $"{rot2:F1}"))
                {
                    el.RotationMax = (180 - rot2).DegreesToRadians();
                }
            }

            if(el.refActorType == 0)
            {
                ImGuiUtils.SizedText("Object life time:".Loc(), WidthElement);
                ImGui.SameLine();
                ImGui.Checkbox("##life", ref el.refActorObjectLife);
                if(el.refActorObjectLife)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##life1", ref el.refActorLifetimeMin, 0.1f, 0f, float.MaxValue);
                    ImGui.SameLine();
                    ImGuiEx.Text("-");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##life2", ref el.refActorLifetimeMax, 0.1f, 0f, float.MaxValue);
                    ImGui.SameLine();
                    ImGuiEx.Text("(in seconds)".Loc());
                }
            }

            ImGuiUtils.SizedText("Transformation ID:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox("##trans", ref el.refActorUseTransformation);
            if(el.refActorUseTransformation)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.InputInt("##transid", ref el.refActorTransformationID);
            }

            ImGuiUtils.SizedText("Head markings:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox("##marks", ref el.refMark);
            if(el.refMark)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                string[] markOptions = { "attack1".Loc(), "attack2".Loc(), "attack3".Loc(), "attack4".Loc(), "attack5".Loc(), "bind1".Loc(), "bind2".Loc(), "bind3".Loc(), "stop1".Loc(), "stop2".Loc(), "square".Loc(), "circle".Loc(), "cross".Loc(), "triangle".Loc(), "attack6".Loc(), "attack7".Loc(), "attack8".Loc() };
                if(ImGui.BeginCombo("##marks type", markOptions[el.refMarkID]))
                {
                    for(var j = 0; j < markOptions.Length; j++)
                    {
                        if(ImGui.Selectable(markOptions[j]))
                        {
                            el.refMarkID = j;
                        }
                    }
                    ImGui.EndCombo();
                }
            }

            ImGuiUtils.SizedText("Targeting you:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox($"##targetYou", ref el.refTargetYou);
            if(el.refTargetYou)
            {
                ImGui.SameLine();
                if(ImGui.RadioButton("No".Loc(), el.refActorTargetingYou == 1))
                {
                    el.refActorTargetingYou = 1;
                }
                ImGui.SameLine();
                if(ImGui.RadioButton("Yes".Loc(), el.refActorTargetingYou == 2))
                {
                    el.refActorTargetingYou = 2;
                }
            }
            ImGuiUtils.SizedText("Tether info:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox("##tether", ref el.refActorTether);
            if(el.refActorTether)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                ImGui.DragFloat("##tetherlife1", ref el.refActorTetherTimeMin, 0.1f, 0f, float.MaxValue);
                ImGui.SameLine();
                ImGuiEx.Text("-");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                ImGui.DragFloat("##tetherlife2", ref el.refActorTetherTimeMax, 0.1f, 0f, float.MaxValue);
                ImGui.SameLine();
                ImGuiEx.Text("(in seconds)".Loc());

                ImGuiUtils.SizedText("         " + "Params:".Loc(), WidthElement);
                ImGui.SameLine();
                ImGuiEx.InputInt(100f, "##param1", ref el.refActorTetherParam1);
                ImGui.SameLine();
                ImGuiEx.InputInt(100f, "##param2", ref el.refActorTetherParam2);
                ImGui.SameLine();
                ImGuiEx.InputInt(100f, "##param3", ref el.refActorTetherParam3);

                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.Checkbox("Source", ref el.refActorIsTetherSource);
                ImGuiEx.HelpMarker("Checked - only check if object is tether source; unchecked - only check if object is tether target; dot - check if object is either tether source or target.");
                ImGui.SameLine();
                ImGui.Checkbox("Invert condition##tether", ref el.refActorIsTetherInvert);

                ImGuiUtils.SizedText("         " + "Connected with:".Loc(), WidthElement);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(200f);
                ImGuiEx.InputListString("##pholderConnectedWith", el.refActorTetherConnectedWithPlayer);
                ImGui.SameLine();
                ImGuiEx.Text("Empty = with any");
            }
        }

        if(el.type.EqualsAny(0, 2, 3, 5))
        {
            ImGuiUtils.SizedText((el.type == 2 || el.type == 3) ? "Point A".Loc() : "Reference position: ".Loc(), WidthElement);
            ImGui.SameLine();
            ImGuiEx.Text("X:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##refx", ref el.refX, 0.02f, float.MinValue, float.MaxValue);
            ImGui.SameLine();
            ImGuiEx.Text("Y:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##refy", ref el.refY, 0.02f, float.MinValue, float.MaxValue);
            ImGui.SameLine();
            ImGuiEx.Text("Z:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##refz", ref el.refZ, 0.02f, float.MinValue, float.MaxValue);
            ImGui.SameLine();
            if(ImGuiEx.IconButton(FontAwesomeIcon.Copy))
            {
                ImGui.SetClipboardText(JsonConvert.SerializeObject(new Vector3(el.refX, el.refZ, el.refY)));
            }
            ImGui.SameLine();
            if(ImGuiEx.IconButton(FontAwesomeIcon.Paste))
            {
                try
                {
                    var v = JsonConvert.DeserializeObject<Vector3>(ImGui.GetClipboardText());
                    el.refX = v.X;
                    el.refY = v.Z;
                    el.refZ = v.Y;
                }
                catch(Exception e)
                {
                    e.Log();
                    Notify.Error(e.Message);
                }
            }
            ImGui.SameLine();
            if(ImGuiEx.IconButton(FontAwesomeIcon.Circle, "0 0 0##ref"))
            {
                el.refX = 0;
                el.refY = 0;
                el.refZ = 0;
            }
            ImGuiEx.Tooltip("0 0 0");
            if(el.type != 3)
            {
                ImGui.SameLine();
                if(ImGuiEx.IconButton(FontAwesomeIcon.MapMarked, "My position".Loc() + "##ref"))
                {
                    el.refX = Utils.GetPlayerPositionXZY().X;
                    el.refY = Utils.GetPlayerPositionXZY().Y;
                    el.refZ = Utils.GetPlayerPositionXZY().Z;
                }
                ImGuiEx.Tooltip("My position".Loc());
                ImGui.SameLine();
                if(ImGuiEx.IconButton(FontAwesomeIcon.MousePointer, "Screen2World".Loc() + "##s2w1"))
                {
                    if(el.IsVisible())
                    {
                        SetCursorTo(el.refX, el.refZ, el.refY);
                        p.BeginS2W(el, "refX", "refY", "refZ");
                    }
                    else
                    {
                        Notify.Error("Unable to use for hidden element".Loc());
                    }
                }
                ImGuiEx.Tooltip("Select on screen".Loc());
                ImGui.SameLine();
                DrawRounding(ref el.refX, ref el.refY, ref el.refZ);
            }

            if(el.type.EqualsAny(1, 3) && el.includeRotation)
            {
                ImGui.SameLine();
                ImGuiEx.Text("Angle: ".Loc() + Utils.RadToDeg(Utils.AngleBetweenVectors(0, 0, 10, 0, el.type == 1 ? 0 : el.refX, el.type == 1 ? 0 : el.refY, el.offX, el.offY)));
            }

            if((el.type == 3) && el.refActorType != 1)
            {
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.Text("+my hitbox (XYZ):".Loc());
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxXam", ref el.LineAddPlayerHitboxLengthXA);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxYam", ref el.LineAddPlayerHitboxLengthYA);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxZam", ref el.LineAddPlayerHitboxLengthZA);
                ImGui.SameLine();
                ImGuiEx.Text("+target hitbox (XYZ):".Loc());
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxXa", ref el.LineAddHitboxLengthXA);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxYa", ref el.LineAddHitboxLengthYA);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxZa", ref el.LineAddHitboxLengthZA);
            }
        }

        if(true)
        {

            ImGuiUtils.SizedText((el.type == 2 || el.type == 3) ? "Point B".Loc() : "Offset: ".Loc(), WidthElement);
            ImGui.SameLine();
            ImGuiEx.Text("X:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##offx", ref el.offX, 0.02f, float.MinValue, float.MaxValue);
            ImGui.SameLine();
            ImGuiEx.Text("Y:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##offy", ref el.offY, 0.02f, float.MinValue, float.MaxValue);
            ImGui.SameLine();
            ImGuiEx.Text("Z:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##offz", ref el.offZ, 0.02f, float.MinValue, float.MaxValue);
            ImGui.SameLine();
            if(ImGuiEx.IconButton(FontAwesomeIcon.Circle, "0 0 0##off"))
            {
                el.offX = 0;
                el.offY = 0;
                el.offZ = 0;
            }
            ImGuiEx.Tooltip("0 0 0");
            if(el.type == 2)
            {
                ImGui.SameLine();
                if(ImGuiEx.IconButton(FontAwesomeIcon.MapMarked, "My position".Loc() + "##off"))
                {
                    el.offX = Utils.GetPlayerPositionXZY().X;
                    el.offY = Utils.GetPlayerPositionXZY().Y;
                    el.offZ = Utils.GetPlayerPositionXZY().Z;
                }
                ImGuiEx.Tooltip("My position".Loc());
            }
            if((el.type == 3) && el.refActorType != 1)
            {
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.Text("+my hitbox (XYZ):".Loc());
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxXm", ref el.LineAddPlayerHitboxLengthX);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxYm", ref el.LineAddPlayerHitboxLengthY);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxZm", ref el.LineAddPlayerHitboxLengthZ);
                ImGui.SameLine();
                ImGuiEx.Text("+target hitbox (XYZ):".Loc());
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxX", ref el.LineAddHitboxLengthX);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxY", ref el.LineAddHitboxLengthY);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxZ", ref el.LineAddHitboxLengthZ);
            }
        }

        if(el.type.EqualsAny(4, 5))
        {
            ImGuiUtils.SizedText("Angle:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("##angle", ref el.coneAngleMin, 0.1f);
            ImGui.SameLine();
            ImGuiEx.Text("-");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("##angle2", ref el.coneAngleMax, 0.1f);
        }

        //ImGui.SameLine();
        //ImGui.Checkbox("Actor relative##rota"+i+k, ref el.includeRotation);
        if(el.type == 2)
        {
            ImGui.SameLine();
            if(ImGuiEx.IconButton(FontAwesomeIcon.MousePointer, "Screen2World".Loc() + "##s2w2"))
            {
                if(LayoutUtils.IsLayoutVisible(l) && (el.Enabled || forceEnable)/* && p.CamAngleY <= p.Config.maxcamY*/)
                {
                    SetCursorTo(el.offX, el.offZ, el.offY);
                    p.BeginS2W(el, "offX", "offY", "offZ");
                }
                else
                {
                    Notify.Error("Unable to use for hidden element".Loc());
                }
            }
            ImGuiEx.Tooltip("Select on screen".Loc());
            ImGui.SameLine();
            DrawRounding(ref el.offX, ref el.offY, ref el.offZ);
        }

        var style = el.GetDisplayStyle();
        if(ImGuiUtils.StyleEdit("Style", ref style))
        {
            el.SetDisplayStyle(style);
        }
        using(ImRaii.Disabled(!el.Filled))
        {
            if(el.type.EqualsAny(1, 3, 4) && el.Filled)
            {
                var canSetCastAnimation = el.refActorRequireCast && el.ConfiguredRenderEngineKind() == RenderEngineKind.DirectX11;
                using(ImRaii.Disabled(!canSetCastAnimation))
                {
                    ImGuiUtils.SizedText("Cast Animation:".Loc(), WidthElement);
                    ImGui.SameLine();
                }
                ImGuiEx.HelpMarker("Choose a cast animation for this element. Requires 'While Casting' checked.\nUnsupported in ImGui Legacy renderer");
                ImGui.SameLine();
                using(ImRaii.Disabled(!canSetCastAnimation))
                {
                    ImGui.SetNextItemWidth(WidthElement);
                    ImGuiUtils.EnumCombo("##castanimation", ref el.castAnimation, CastAnimations.Names, CastAnimations.Tooltips);
                    using(ImRaii.Disabled(el.castAnimation is CastAnimationKind.Unspecified))
                    {
                        ImGui.SameLine();
                        ImGuiEx.Text("Color:".Loc());
                        ImGui.SameLine();
                        var v4 = ImGui.ColorConvertU32ToFloat4(el.animationColor);
                        if(ImGui.ColorEdit4("##animationcolorbutton", ref v4, ImGuiColorEditFlags.NoInputs))
                        {
                            el.animationColor = ImGui.ColorConvertFloat4ToU32(v4);
                        }
                        ImGui.SameLine();
                        if(ImGui.Button("Copy".Loc() + "##copyfromstroke"))
                        {
                            el.animationColor = style.strokeColor;
                        }
                        if(ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Copy Stroke Color".Loc());
                        }
                        if(el.castAnimation is CastAnimationKind.Pulse)
                        {
                            ImGuiUtils.SizedText("Pulse:".Loc(), WidthElement);
                            ImGui.SameLine();

                            ImGuiEx.Text("Size:".Loc());
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(60f);
                            el.pulseSize = MathF.Min(el.pulseSize, el.EffectiveLength());
                            ImGui.DragFloat("##animationsize", ref el.pulseSize, 0.01f, 0.1f, el.EffectiveLength());
                            ImGui.SameLine();

                            ImGuiEx.Text("Frequency (s):".Loc());
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(60f);
                            ImGui.DragFloat("##animationfreq", ref el.pulseFrequency, 0.01f, 1, 10);
                        }
                    }
                }
            }
        }
        if((el.type != 3) || el.includeRotation)
        {
            if(!(el.type == 3 && !el.includeRotation))
            {
                ImGuiUtils.SizedText("Radius:".Loc(), WidthElement);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60f);
                ImGui.DragFloat("##radius", ref el.radius, 0.01f, 0, float.MaxValue);
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Leave at 0 to draw single dot".Loc());
                if(el.type == 1 || (el.type == 3 && el.includeRotation) || el.type == 4)
                {
                    if(el.refActorType != 1)
                    {
                        ImGui.SameLine();
                        ImGui.Checkbox("+target hitbox".Loc(), ref el.includeHitbox);
                    }
                    ImGui.SameLine();
                    ImGui.Checkbox("+your hitbox".Loc(), ref el.includeOwnHitbox);
                    ImGui.SameLine();
                    ImGuiEx.Text("(?)");
                    if(ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(("When the game tells you that ability A has distance D,\n" +
                            "in fact it means that you are allowed to execute\n" +
                            "ability A if distance between edge of your hitbox\n" +
                            "and enemy's hitbox is less or equal than distance D,\n" +
                            "that is for targeted abilities.\n" +
                            "If an ability is AoE, such check is performed between\n" +
                            "middle point of your character and edge of enemy's hitbox.\n\n" +
                            "Summary: if you are trying to make targeted ability indicator -\n" +
                            "enable both \"+your hitbox\" and \"+target hitbox\".\n" +
                            "If you are trying to make AoE ability indicator - \n" +
                            "enable only \"+target hitbox\" to make indicators valid.").Loc());
                    }
                }
                if(el.type.EqualsAny(0, 1, 4, 5))
                {
                    ImGui.SameLine();
                    ImGuiEx.Text("Donut:".Loc());
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(60f);
                    ImGui.DragFloat("##radiusdonut", ref el.Donut, 0.01f, 0, float.MaxValue);
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Leave at 0 to not draw a donut.\n" +
                            "If greater than 0, the radius is the donut hole radius\n" +
                            "and this is the thickness of the donut.".Loc());
                    el.Donut.ValidateRange(0, float.MaxValue);
                }
            }
            if(el.type != 2 && el.type != 3)
            {
                ImGuiUtils.SizedText("Tether:".Loc(), WidthElement);
                ImGui.SameLine();
                ImGui.Checkbox("Enable##TetherEnable", ref el.tether);
                ImGui.SameLine();
                ImGuiEx.Text("Extra Length:".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60f);
                ImGui.DragFloat("##extratetherlength", ref el.ExtraTetherLength, 0.01f, 0, float.MaxValue);
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Add extra length to the tether to visualize knockbacks.".Loc());
            }
            var canSetLineEnds = el.tether ||
                ((el.type == 2 || el.type == 3) && el.radius == 0);
            if(!canSetLineEnds) ImGui.BeginDisabled();
            ImGuiUtils.SizedText("Line End Style:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGuiEx.Text("A: ".Loc());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGuiUtils.EnumCombo("##LineEndA", ref el.LineEndA, LineEnds.Names, LineEnds.Tooltips);
            ImGui.SameLine();
            ImGuiEx.Text("B: ".Loc());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGuiUtils.EnumCombo("##LineEndB", ref el.LineEndB, LineEnds.Names, LineEnds.Tooltips);
            if(!canSetLineEnds) ImGui.EndDisabled();
        }
        if(el.type == 0 || el.type == 1 || el.type == 4 || el.type == 5)
        {
            ImGuiUtils.SizedText("Overlay text:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##overlaytext", "Text to display as overlay".Loc(), ref el.overlayText, 500);
            if(el.overlayPlaceholders && el.type == 1)
            {
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.TextCopy("$NAME");
                ImGui.SameLine();
                ImGuiEx.TextCopy("$OBJECTID");
                ImGui.SameLine();
                ImGuiEx.TextCopy("$DATAID");
                ImGui.SameLine();
                ImGuiEx.TextCopy("$MODELID");
                ImGui.SameLine();
                ImGuiEx.TextCopy("$HITBOXR");
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.TextCopy("$KIND");
                ImGui.SameLine();
                ImGuiEx.TextCopy("$NPCID");
                ImGui.SameLine();
                ImGuiEx.TextCopy("$LIFE");
                ImGui.SameLine();
                ImGuiEx.TextCopy("$NAMEID");
                ImGui.SameLine();
                ImGuiEx.TextCopy("$DISTANCE");
                ImGui.SameLine();
                ImGuiEx.TextCopy("$TRANSFORM");
                ImGui.SameLine();
                ImGuiEx.TextCopy("\\n");
            }
            if(el.overlayText.Length > 0)
            {
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.Text("Vertical offset:".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60f);
                ImGui.DragFloat("##vtextadj", ref el.overlayVOffset, 0.02f);
                ImGui.SameLine();
                ImGuiEx.Text("Font scale:".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60f);
                ImGui.DragFloat("##vtextsize", ref el.overlayFScale, 0.02f, 0.1f, 50f);
                if(el.overlayFScale < 0.1f) el.overlayFScale = 0.1f;
                if(el.overlayFScale > 50f) el.overlayFScale = 50f;

                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.Text("BG color:".Loc());
                ImGui.SameLine();
                var v4b = ImGui.ColorConvertU32ToFloat4(el.overlayBGColor);
                if(ImGui.ColorEdit4("##colorbuttonbg", ref v4b, ImGuiColorEditFlags.NoInputs))
                {
                    el.overlayBGColor = ImGui.ColorConvertFloat4ToU32(v4b);
                }
                ImGui.SameLine();
                ImGuiEx.Text("Text color:".Loc());
                ImGui.SameLine();
                var v4t = ImGui.ColorConvertU32ToFloat4(el.overlayTextColor);
                if(ImGui.ColorEdit4("##colorbuttonfg", ref v4t, ImGuiColorEditFlags.NoInputs))
                {
                    el.overlayTextColor = ImGui.ColorConvertFloat4ToU32(v4t);
                }
            }
            if(el.type == 1)
            {
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGui.Checkbox("Enable placeholders".Loc(), ref el.overlayPlaceholders);
            }
        }

        ImGui.Separator();

        ImGuiUtils.SizedText("Renderer:".Loc(), WidthElement);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("##renderer", ref el.RenderEngineKind);

        ImGuiUtils.SizedText("Mechanic type:", WidthElement);
        ImGuiEx.HelpMarker("Choose a mechanic type that best represents this element.\n" +
                "This is used for automatically setting default colors.\nOnly for DirectX11 renderer.");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(WidthElement);
        ImGuiUtils.EnumCombo("##mechtype", ref el.mechanicType, MechanicTypes.Names, MechanicTypes.Tooltips);

        if((el.type.EqualsAny(0, 1) && el.Donut > 0) || el.type == 4 || (el.type.EqualsAny(2, 3) && (el.radius > 0 || el.includeHitbox || el.includeOwnHitbox)))
        {
            ImGuiUtils.SizedText("Fill step:".Loc(), WidthElement);
            ImGuiEx.HelpMarker("Only for ImGui Legacy renderer");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##fillstep", ref el.FillStep, 0.001f, 0, float.MaxValue);
            el.FillStep.ValidateRange(0.01f, float.MaxValue);
        }

    }
}
