using Dalamud;
using Dalamud.Game;
using Dalamud.Interface.Components;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ECommons.LanguageHelpers;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using Splatoon.Serializables;
using Splatoon.Utility;
using System;

namespace Splatoon;

internal unsafe partial class CGui
{
    private string ActionName = "";
    private string BuffName = "";
    internal void LayoutDrawElement(Layout l, Element el, bool forceEnable = false)
    {
        var i = l.Name;
        var k = el.Name;
        ImGui.Checkbox("Enabled".Loc() + "##" + i + k, ref el.Enabled);
        ImGui.SameLine();
        if (ImGui.Button("Copy as HTTP param".Loc() + "##" + i + k))
        {
            HTTPExportToClipboard(el);
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Hold ALT to copy raw JSON (for usage with post body or you'll have to urlencode it yourself)\nHold CTRL and click to copy urlencoded raw".Loc());
        }
        ImGui.SameLine();
        if (ImGui.Button("Copy to clipboard".Loc() + "##" + i + k))
        {
            ImGui.SetClipboardText(JsonConvert.SerializeObject(el, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }));
            Notify.Success("Copied to clipboard".Loc());
        }

        ImGui.SameLine();
        if (ImGui.Button("Copy style".Loc() + "##" + i + k))
        {
            p.Clipboard = JsonConvert.DeserializeObject<Element>(JsonConvert.SerializeObject(el));
        }
        if (p.Clipboard != null)
        {
            ImGui.SameLine();
            if (ImGui.Button("Paste style".Loc() + "##" + i + k))
            {
                el.color = p.Clipboard.color;
                el.overlayBGColor = p.Clipboard.overlayBGColor;
                el.overlayTextColor = p.Clipboard.overlayTextColor;
                el.tether = p.Clipboard.tether;
                el.thicc = p.Clipboard.thicc;
                el.overlayVOffset = p.Clipboard.overlayVOffset;
                if (ImGui.GetIO().KeyCtrl)
                {
                    el.radius = p.Clipboard.radius;
                    el.includeHitbox = p.Clipboard.includeHitbox;
                    el.includeOwnHitbox = p.Clipboard.includeOwnHitbox;
                    el.includeRotation = p.Clipboard.includeRotation;
                    el.onlyTargetable = p.Clipboard.onlyTargetable;
                }
                if (ImGui.GetIO().KeyShift && el.type != 2)
                {
                    el.refX = p.Clipboard.refX;
                    el.refY = p.Clipboard.refY;
                    el.refZ = p.Clipboard.refZ;
                }
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGuiEx.Text("Copied style:".Loc());
                ImGuiEx.Text($"Color: 0x{p.Clipboard.color:X8}");
                ImGui.SameLine();
                ImGuiUtils.DisplayColor(p.Clipboard.color);
                ImGuiEx.Text($"Overlay BG color: 0x{p.Clipboard.overlayBGColor:X8}");
                ImGui.SameLine();
                ImGuiUtils.DisplayColor(p.Clipboard.overlayBGColor);
                ImGuiEx.Text($"Overlay text color: 0x{p.Clipboard.overlayTextColor:X8}");
                ImGui.SameLine();
                ImGuiUtils.DisplayColor(p.Clipboard.overlayTextColor);
                ImGuiEx.Text($"Overlay vertical offset: {p.Clipboard.overlayVOffset}");
                ImGuiEx.Text($"Thickness: {p.Clipboard.thicc}");
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


        ImGuiUtils.SizedText("Name:".Loc(), WidthElement);
        ImGui.SameLine();
        ImGuiEx.SetNextItemFullWidth();
        ImGui.InputText("##Name", ref el.Name, 100);

        ImGuiUtils.SizedText("Element type:".Loc(), WidthElement);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(WidthCombo);
        if (ImGui.Combo("##elemselecttype" + i + k, ref el.type, Element.ElementTypes, Element.ElementTypes.Length))
        {
            if ((el.type == 2 || el.type == 3) && el.radius == 0.35f)
            {
                el.radius = 0;
            }
        }
        if (el.type.EqualsAny(4, 5))
        {
            el.includeRotation = true;
        }
        if (el.type.EqualsAny(1, 3, 4, 5))
        {
            ImGuiUtils.SizedText("Account for rotation:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox("##rota" + i + k, ref el.includeRotation);
            if (el.includeRotation)
            {
                DrawRotationSelector(el, i, k);
            }
        }
        if (el.type.EqualsAny(1, 3, 4))
        {

            ImGuiUtils.SizedText("Targeted object: ".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(WidthCombo);
            ImGui.Combo("##actortype" + i + k, ref el.refActorType, Element.ActorTypes, Element.ActorTypes.Length);
            if (el.refActorType == 0)
            {
                ImGui.SameLine();
                if (ImGui.Button("Copy settarget command".Loc() + "##" + i + k))
                {
                    ImGui.SetClipboardText("/splatoon settarget " + i + "~" + k);
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("This command allows you to quickly change\nsearch attributes to your active target's name.\nYou can use it with macro.".Loc());
                }
                ImGui.SetNextItemWidth(WidthElement + ImGui.GetStyle().ItemSpacing.X);
                if (ImGui.BeginCombo("##compare", el.refActorComparisonAnd ? "Multiple attributes".Loc() : "Single attribute".Loc()))
                {
                    if (ImGui.Selectable("Match one attribute".Loc()))
                    {
                        el.refActorComparisonAnd = false;
                    }
                    if (ImGui.Selectable("Match multiple attributes (AND logic)".Loc()))
                    {
                        el.refActorComparisonAnd = true;
                    }
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(75f);
                ImGui.Combo($"##attrSelect{i + k}", ref el.refActorComparisonType, Element.ComparisonTypes, Element.ComparisonTypes.Length);
                ImGui.SameLine();
                if (el.refActorComparisonType == 0)
                {
                    ImGui.SetNextItemWidth(150f);
                    //ImGui.InputText("##actorname" + i + k, ref el.refActorName, 100);
                    el.refActorNameIntl.ImGuiEdit(ref el.refActorName);
                    if (NameNpcIDs.TryGetValue(el.refActorNameIntl.Get(el.refActorName).ToLower(), out var nameid))
                    {
                        ImGui.SameLine();
                        if (ImGui.Button($"Name ID: ??, convert?".Loc(nameid.Format()) + "##{i + k}"))
                        {
                            el.refActorComparisonType = 6;
                            el.refActorNPCNameID = nameid;
                        }
                        ImGuiComponents.HelpMarker("Name ID has been found for this string. If you will convert comparison to Name ID, it will make element work with any languare. In addition, such conversion will provide a performance boost.\n\nSelection by Name ID will target only Characters (usually it's fine). If you're targeting GameObject, EventObj or EventNpc, do not convert.".Loc());
                    }
                }
                else if (el.refActorComparisonType == 1)
                {
                    ImGuiUtils.InputUintDynamic("##actormid" + i + k, ref el.refActorModelID);
                }
                else if (el.refActorComparisonType == 2)
                {
                    ImGuiUtils.InputUintDynamic("##actoroid" + i + k, ref el.refActorObjectID);
                }
                else if (el.refActorComparisonType == 3)
                {
                    ImGuiUtils.InputUintDynamic("##actordid" + i + k, ref el.refActorDataID);
                }
                else if (el.refActorComparisonType == 4)
                {
                    ImGuiUtils.InputUintDynamic("##npcid" + i + k, ref el.refActorNPCID);
                }
                else if (el.refActorComparisonType == 5)
                {
                    ImGui.SetNextItemWidth(200f);
                    ImGuiEx.InputListString("##pholder" + i + k, el.refActorPlaceholder);
                    ImGui.SameLine();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.AngleDoubleDown))
                    {
                        ImGui.OpenPopup("PlaceholderFastSelect");
                    }
                    if(ImGui.BeginPopup("PlaceholderFastSelect"))
                    {
                        for(int s = 1; s <= 8; s++)
                        {
                            if(ImGui.Selectable($"<{s}>", false, ImGuiSelectableFlags.DontClosePopups)) el.refActorPlaceholder.Add($"<{s}>");
                        }
                        if(ImGui.Selectable("2-8", false, ImGuiSelectableFlags.DontClosePopups))
                        {
                            for(int s = 2; s <= 8; s++)
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
                else if (el.refActorComparisonType == 6)
                {
                    ImGuiUtils.InputUintDynamic("##nameID" + i + k, ref el.refActorNPCNameID);
                    var npcnames = NameNpcIDsAll.FindKeysByValue(el.refActorNPCNameID);
                    if (npcnames.Any())
                    {
                        ImGuiComponents.HelpMarker($"{"NPC".Loc()}: \n{npcnames.Join("\n")}");
                    }
                }
                else if (el.refActorComparisonType == 7)
                {
                    ImGui.SetNextItemWidth(150f);
                    ImGui.InputText("##vfx", ref el.refActorVFXPath, 500);
                    ImGui.SameLine();
                    ImGuiEx.Text("Age:".Loc());
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    var a1 = (float)el.refActorVFXMin / 1000f;
                    if (ImGui.DragFloat("##age1", ref a1, 0.1f, 0, 99999, $"{a1:F1}"))
                    {
                        el.refActorVFXMin = (int)(a1 * 1000);
                    }
                    ImGui.SameLine();
                    ImGuiEx.Text("-");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);

                    var a2 = (float)el.refActorVFXMax / 1000f;
                    if (ImGui.DragFloat("##age2", ref a2, 0.1f, 0, 99999, $"{a2:F1}"))
                    {
                        el.refActorVFXMax = (int)(a2 * 1000);
                    }
                }
                else if (el.refActorComparisonType == 8)
                {
                    ImGui.SetNextItemWidth(50f);
                    ImGuiEx.InputUint("##edata1", ref el.refActorObjectEffectData1);
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGuiEx.InputUint("##edata2", ref el.refActorObjectEffectData2);
                    ImGui.SameLine();
                    ImGui.Checkbox($"Last only", ref el.refActorObjectEffectLastOnly);
                    if (!el.refActorObjectEffectLastOnly)
                    {
                        ImGui.SameLine();
                        ImGuiEx.Text("Age:".Loc());
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(50f);
                        var a1 = (float)el.refActorObjectEffectMin / 1000f;
                        if (ImGui.DragFloat("##eage1", ref a1, 0.1f, 0, 99999, $"{a1:F1}"))
                        {
                            el.refActorObjectEffectMin = (int)(a1 * 1000);
                        }
                        ImGui.SameLine();
                        ImGuiEx.Text("-");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(50f);

                        var a2 = (float)el.refActorObjectEffectMax / 1000f;
                        if (ImGui.DragFloat("##eage2", ref a2, 0.1f, 0, 99999, $"{a2:F1}"))
                        {
                            el.refActorObjectEffectMax = (int)(a2 * 1000);
                        }
                    }
                }
                else if (el.refActorComparisonType == 9)
                {
                    ImGui.SetNextItemWidth(200f);
                    ImGuiEx.InputUint("##nameplateiconid" + i + k, ref el.refActorNamePlateIconID);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Decimal input");
                    }
                }

                if (Svc.Targets.Target != null && !el.refActorComparisonType.EqualsAny(7, 8))
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Target".Loc() + "##btarget" + i + k))
                    {
                        el.refActorNameIntl.CurrentLangString = Svc.Targets.Target.Name.ToString();
                        el.refActorDataID = Svc.Targets.Target.DataId;
                        el.refActorObjectID = Svc.Targets.Target.EntityId;
                        if (Svc.Targets.Target is ICharacter c)
                        {
                            el.refActorModelID = (uint)c.Struct()->CharacterData.ModelCharaId;
                            el.refActorNPCNameID = c.NameId;
                        }
                        el.refActorNPCID = Svc.Targets.Target.Struct()->GetNameId();
                        el.refActorNamePlateIconID = Svc.Targets.Target.Struct()->NamePlateIconId;
                    }
                }
                ImGuiUtils.SizedText("Targetability: ".Loc(), WidthElement);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                if (ImGui.BeginCombo($"##TargetabilityCombo{i + k}", el.onlyTargetable ? "Targetable".Loc() : (el.onlyUnTargetable ? "Untargetable".Loc() : "Any".Loc())))
                {
                    if (ImGui.Selectable("Any".Loc()))
                    {
                        el.onlyTargetable = false;
                        el.onlyUnTargetable = false;
                    }
                    if (ImGui.Selectable("Targetable only".Loc()))
                    {
                        el.onlyTargetable = true;
                        el.onlyUnTargetable = false;
                    }
                    if (ImGui.Selectable("Untargetable only".Loc()))
                    {
                        el.onlyTargetable = false;
                        el.onlyUnTargetable = true;
                    }
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                ImGui.Checkbox("Visible characters only".Loc() + "##" + i + k, ref el.onlyVisible);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Setting this checkbox will also restrict search to characters ONLY. \n(character - is a player, companion or friendly/hostile NPC that can fight and have HP)".Loc());
                }
            }

            ImGui.SetNextItemWidth(WidthElement + ImGui.GetStyle().ItemSpacing.X);
            if (ImGui.BeginCombo("##whilecasting", el.refActorCastReverse ? "While NOT casting".Loc() : "While casting".Loc()))
            {
                if (ImGui.Selectable("While casting".Loc())) el.refActorCastReverse = false;
                if (ImGui.Selectable("While NOT casting".Loc())) el.refActorCastReverse = true;
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            ImGui.Checkbox("##casting" + i + k, ref el.refActorRequireCast);
            if (el.refActorRequireCast)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(WidthCombo);
                ImGuiEx.InputListUint("##casts" + i + k, el.refActorCastId, ActionNames);
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.Text("Add all by name:".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.InputText("##ActionName" + i + k, ref ActionName, 100);
                ImGui.SameLine();
                if (ImGui.Button("Add".Loc() + "##byactionname" + i + k))
                {
                    foreach (var x in Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>().Union(Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>(ClientLanguage.English)))
                    {
                        if (x.Name.ToString().Equals(ActionName, StringComparison.OrdinalIgnoreCase))
                        {
                            el.refActorCastId.Add(x.RowId);
                        }
                    }
                }
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGui.Checkbox("Limit by cast time".Loc(), ref el.refActorUseCastTime);
                if (el.refActorUseCastTime)
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
            ImGui.Checkbox("##buffreq" + i + k, ref el.refActorRequireBuff);
            if (el.refActorRequireBuff)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(WidthCombo);
                ImGuiEx.InputListUint("##buffs" + i + k, el.refActorBuffId, BuffNames);
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.Text("Add all by name:".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.InputText("##BuffNames" + i + k, ref BuffName, 100);
                ImGui.SameLine();
                if (ImGui.Button("Add".Loc() + "##bybuffname" + i + k))
                {
                    foreach (var x in Svc.Data.GetExcelSheet<Status>().Union(Svc.Data.GetExcelSheet<Status>(ClientLanguage.English)))
                    {
                        if (x.Name.ToString().Equals(BuffName, StringComparison.OrdinalIgnoreCase))
                        {
                            el.refActorBuffId.Add(x.RowId);
                        }
                    }
                }
                if (Svc.Targets.Target != null && Svc.Targets.Target is IBattleChara bchr)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Add from target".Loc() + "##bybuffname" + i + k))
                    {
                        el.refActorBuffId.AddRange(bchr.StatusList.Select(x => x.StatusId));
                    }
                }
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGui.Checkbox("Limit by remaining time".Loc(), ref el.refActorUseBuffTime);
                if (el.refActorUseBuffTime)
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
                if (el.refActorUseBuffParam)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150f);
                    ImGui.InputInt("##btime1", ref el.refActorBuffParam);
                }
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGui.Checkbox((el.refActorRequireBuffsInvert ? "Require ANY status to be missing".Loc() + "##" : "Require ALL listed statuses to be present".Loc() + "##") + i + k, ref el.refActorRequireAllBuffs);
                ImGui.SameLine();
                ImGui.Checkbox("Invert behavior".Loc() + "##" + i + k, ref el.refActorRequireBuffsInvert);
            }

            ImGuiUtils.SizedText("Distance limit".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox("##dstLim", ref el.LimitDistance);
            if (el.LimitDistance)
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
                if (ImGuiEx.IconButton(FontAwesomeIcon.Circle, "0 0 0##dist"))
                {
                    el.DistanceSourceX = 0;
                    el.DistanceSourceY = 0;
                    el.DistanceSourceZ = 0;
                }
                ImGuiEx.Tooltip("0 0 0");
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.MapMarked, "My position".Loc() + "##dist"))
                {
                    el.DistanceSourceX = Utils.GetPlayerPositionXZY().X;
                    el.DistanceSourceY = Utils.GetPlayerPositionXZY().Y;
                    el.DistanceSourceZ = Utils.GetPlayerPositionXZY().Z;
                }
                ImGuiEx.Tooltip("My position");
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.MousePointer, "Screen2World".Loc() + "##dist"))
                {
                    if (LayoutUtils.IsLayoutVisible(l) && (el.Enabled || forceEnable))
                    {
                        SetCursorTo(el.DistanceSourceX, el.DistanceSourceY, el.DistanceSourceZ);
                        p.BeginS2W(el, "DistanceSourceX", "DistanceSourceY", "DistanceSourceZ");
                    }
                    else
                    {
                        Notify.Error("Unable to use for hidden element".Loc());
                    }
                }
                ImGuiEx.Tooltip("Select on screen".Loc());
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
            if (el.LimitRotation)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                var rot1 = 180 - el.RotationMin.RadiansToDegrees();
                if (ImGui.DragFloat("##rotamax", ref rot1, 0.1f, -360f, 360f, $"{rot1:F1}"))
                {
                    el.RotationMin = (180 - rot1).DegreesToRadians();
                }

                ImGui.SameLine();
                ImGuiEx.Text("-");
                ImGui.SameLine();


                ImGui.SetNextItemWidth(50f);
                var rot2 = 180 - el.RotationMax.RadiansToDegrees();
                if (ImGui.DragFloat("##rotamin", ref rot2, 0.1f, -360f, 360f, $"{rot2:F1}"))
                {
                    el.RotationMax = (180 - rot2).DegreesToRadians();
                }
            }

            if (el.refActorType == 0)
            {
                ImGuiUtils.SizedText("Object life time:".Loc(), WidthElement);
                ImGui.SameLine();
                ImGui.Checkbox("##life" + i + k, ref el.refActorObjectLife);
                if (el.refActorObjectLife)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##life1" + i + k, ref el.refActorLifetimeMin, 0.1f, 0f, float.MaxValue);
                    ImGui.SameLine();
                    ImGuiEx.Text("-");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##life2" + i + k, ref el.refActorLifetimeMax, 0.1f, 0f, float.MaxValue);
                    ImGui.SameLine();
                    ImGuiEx.Text("(in seconds)".Loc());
                }
            }

            ImGuiUtils.SizedText("Transformation ID:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox("##trans" + i + k, ref el.refActorUseTransformation);
            if (el.refActorUseTransformation)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.InputInt("##transid" + i + k, ref el.refActorTransformationID);
            }

            ImGuiUtils.SizedText("Head markings:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox("##marks" + i + k, ref el.refMark);
            if (el.refMark)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                string[] markOptions = { "attack1".Loc(), "attack2".Loc(), "attack3".Loc(), "attack4".Loc(), "attack5".Loc(), "bind1".Loc(), "bind2".Loc(), "bind3".Loc(), "stop1".Loc(), "stop2".Loc(), "square".Loc(), "circle".Loc(), "cross".Loc(), "triangle".Loc(), "attack6".Loc(), "attack7".Loc(), "attack8".Loc() };
                if (ImGui.BeginCombo("##marks type" + i + k, markOptions[el.refMarkID]))
                {
                    for (var j = 0; j < markOptions.Length; j++)
                    {
                        if (ImGui.Selectable(markOptions[j]))
                        {
                            el.refMarkID = j;
                        }
                    }
                    ImGui.EndCombo();
                }
            }

            ImGuiUtils.SizedText("Targeting you:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.Checkbox($"##targetYou" + i + k, ref el.refTargetYou);
            if (el.refTargetYou)
            {
                ImGui.SameLine();
                if (ImGui.RadioButton("No".Loc(), el.refActorTargetingYou == 1))
                {
                    el.refActorTargetingYou = 1;
                }
                ImGui.SameLine();
                if (ImGui.RadioButton("Yes".Loc(), el.refActorTargetingYou == 2))
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
                ImGui.DragFloat("##tetherlife1" + i + k, ref el.refActorTetherTimeMin, 0.1f, 0f, float.MaxValue);
                ImGui.SameLine();
                ImGuiEx.Text("-");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                ImGui.DragFloat("##tetherlife2" + i + k, ref el.refActorTetherTimeMax, 0.1f, 0f, float.MaxValue);
                ImGui.SameLine();
                ImGuiEx.Text("(in seconds)".Loc());

                ImGuiUtils.SizedText("         "+"Params:".Loc(), WidthElement);
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
                ImGui.Checkbox("Invert condition", ref el.refActorIsTetherInvert);

                ImGuiUtils.SizedText("         "+"Connected with:".Loc(), WidthElement);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(200f);
                ImGuiEx.InputListString("##pholderConnectedWith", el.refActorTetherConnectedWithPlayer);
                ImGui.SameLine();
                ImGuiEx.Text("Empty = with any");
            }
        }

        if (el.type.EqualsAny(0, 2, 3, 5))
        {
            ImGuiUtils.SizedText((el.type == 2 || el.type == 3) ? "Point A".Loc() : "Reference position: ".Loc(), WidthElement);
            ImGui.SameLine();
            ImGuiEx.Text("X:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##refx" + i + k, ref el.refX, 0.02f, float.MinValue, float.MaxValue);
            ImGui.SameLine();
            ImGuiEx.Text("Y:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##refy" + i + k, ref el.refY, 0.02f, float.MinValue, float.MaxValue);
            ImGui.SameLine();
            ImGuiEx.Text("Z:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##refz" + i + k, ref el.refZ, 0.02f, float.MinValue, float.MaxValue);
            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.Copy))
            {
                ImGui.SetClipboardText(JsonConvert.SerializeObject(new Vector3(el.refX, el.refZ, el.refY)));
            }
            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.Paste))
            {
                try
                {
                    var v = JsonConvert.DeserializeObject<Vector3>(ImGui.GetClipboardText());
                    el.refX = v.X;
                    el.refY = v.Z;
                    el.refZ = v.Y;
                }
                catch (Exception e)
                {
                    e.Log();
                    Notify.Error(e.Message);
                }
            }
            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.Circle, "0 0 0##ref" + i + k))
            {
                el.refX = 0;
                el.refY = 0;
                el.refZ = 0;
            }
            ImGuiEx.Tooltip("0 0 0");
            if (el.type != 3)
            {
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.MapMarked, "My position".Loc() + "##ref" + i + k))
                {
                    el.refX = Utils.GetPlayerPositionXZY().X;
                    el.refY = Utils.GetPlayerPositionXZY().Y;
                    el.refZ = Utils.GetPlayerPositionXZY().Z;
                }
                ImGuiEx.Tooltip("My position".Loc());
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.MousePointer, "Screen2World".Loc() + "##s2w1" + i + k))
                {
                    if (LayoutUtils.IsLayoutVisible(l) && (el.Enabled || forceEnable))
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
            }

            if (el.type.EqualsAny(1, 3) && el.includeRotation)
            {
                ImGui.SameLine();
                ImGuiEx.Text("Angle: ".Loc() + Utils.RadToDeg(Utils.AngleBetweenVectors(0, 0, 10, 0, el.type == 1 ? 0 : el.refX, el.type == 1 ? 0 : el.refY, el.offX, el.offY)));
            }

            if ((el.type == 3) && el.refActorType != 1)
            {
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.Text("+my hitbox (XYZ):".Loc());
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxXam{i + k}", ref el.LineAddPlayerHitboxLengthXA);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxYam{i + k}", ref el.LineAddPlayerHitboxLengthYA);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxZam{i + k}", ref el.LineAddPlayerHitboxLengthZA);
                ImGui.SameLine();
                ImGuiEx.Text("+target hitbox (XYZ):".Loc());
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxXa{i + k}", ref el.LineAddHitboxLengthXA);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxYa{i + k}", ref el.LineAddHitboxLengthYA);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxZa{i + k}", ref el.LineAddHitboxLengthZA);
            }
        }

        if (true)
        {

            ImGuiUtils.SizedText((el.type == 2 || el.type == 3) ? "Point B".Loc() : "Offset: ".Loc(), WidthElement);
            ImGui.SameLine();
            ImGuiEx.Text("X:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##offx" + i + k, ref el.offX, 0.02f, float.MinValue, float.MaxValue);
            ImGui.SameLine();
            ImGuiEx.Text("Y:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##offy" + i + k, ref el.offY, 0.02f, float.MinValue, float.MaxValue);
            ImGui.SameLine();
            ImGuiEx.Text("Z:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##offz" + i + k, ref el.offZ, 0.02f, float.MinValue, float.MaxValue);
            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.Circle, "0 0 0##off" + i + k))
            {
                el.offX = 0;
                el.offY = 0;
                el.offZ = 0;
            }
            ImGuiEx.Tooltip("0 0 0");
            if (el.type == 2)
            {
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.MapMarked, "My position".Loc() + "##off" + i + k))
                {
                    el.offX = Utils.GetPlayerPositionXZY().X;
                    el.offY = Utils.GetPlayerPositionXZY().Y;
                    el.offZ = Utils.GetPlayerPositionXZY().Z;
                }
                ImGuiEx.Tooltip("My position".Loc());
            }
            if ((el.type == 3) && el.refActorType != 1)
            {
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.Text("+my hitbox (XYZ):".Loc());
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxXm{i + k}", ref el.LineAddPlayerHitboxLengthX);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxYm{i + k}", ref el.LineAddPlayerHitboxLengthY);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxZm{i + k}", ref el.LineAddPlayerHitboxLengthZ);
                ImGui.SameLine();
                ImGuiEx.Text("+target hitbox (XYZ):".Loc());
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxX{i + k}", ref el.LineAddHitboxLengthX);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxY{i + k}", ref el.LineAddHitboxLengthY);
                ImGui.SameLine();
                ImGui.Checkbox($"##lineTHitboxZ{i + k}", ref el.LineAddHitboxLengthZ);
            }
        }

        if (el.type.EqualsAny(4, 5))
        {
            ImGuiUtils.SizedText("Angle:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("##angle" + i + k, ref el.coneAngleMin, 0.1f);
            ImGui.SameLine();
            ImGuiEx.Text("-");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("##angle2" + i + k, ref el.coneAngleMax, 0.1f);
        }

        //ImGui.SameLine();
        //ImGui.Checkbox("Actor relative##rota"+i+k, ref el.includeRotation);
        if (el.type == 2)
        {
            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.MousePointer, "Screen2World".Loc() + "##s2w2" + i + k))
            {
                if (LayoutUtils.IsLayoutVisible(l) && (el.Enabled || forceEnable)/* && p.CamAngleY <= p.Config.maxcamY*/)
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
        }

        var style = el.GetDisplayStyle();
        if (ImGuiUtils.StyleEdit("Style", ref style))
        {
            el.SetDisplayStyle(style);
        }

        if ((el.type != 3) || el.includeRotation)
        {
            if (!(el.type == 3 && !el.includeRotation))
            {
                ImGuiUtils.SizedText("Radius:".Loc(), WidthElement);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60f);
                ImGui.DragFloat("##radius" + i + k, ref el.radius, 0.01f, 0, float.MaxValue);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Leave at 0 to draw single dot".Loc());
                if (el.type == 1 || (el.type == 3 && el.includeRotation) || el.type == 4)
                {
                    if (el.refActorType != 1)
                    {
                        ImGui.SameLine();
                        ImGui.Checkbox("+target hitbox".Loc() + "##" + i + k, ref el.includeHitbox);
                    }
                    ImGui.SameLine();
                    ImGui.Checkbox("+your hitbox".Loc() + "##" + i + k, ref el.includeOwnHitbox);
                    ImGui.SameLine();
                    ImGuiEx.Text("(?)");
                    if (ImGui.IsItemHovered())
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
                if (el.type.EqualsAny(0, 1, 4, 5))
                {
                    ImGui.SameLine();
                    ImGuiEx.Text("Donut:".Loc());
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(60f);
                    ImGui.DragFloat("##radiusdonut" + i + k, ref el.Donut, 0.01f, 0, float.MaxValue);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Leave at 0 to not draw a donut.\n" +
                            "If greater than 0, the radius is the donut hole radius\n" +
                            "and this is the thickness of the donut.".Loc());
                    el.Donut.ValidateRange(0, float.MaxValue);
                }
            }
            if (el.type != 2 && el.type != 3)
            {
                ImGuiUtils.SizedText("Tether:".Loc(), WidthElement);
                ImGui.SameLine();
                ImGui.Checkbox("Enable##TetherEnable" + i + k, ref el.tether);
                ImGui.SameLine();
                ImGuiEx.Text("Extra Length:".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60f);
                ImGui.DragFloat("##extratetherlength" + i + k, ref el.ExtraTetherLength, 0.01f, 0, float.MaxValue);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Add extra length to the tether to visualize knockbacks.".Loc());
            }
            var canSetLineEnds = el.tether ||
                ((el.type == 2 || el.type == 3) && el.radius == 0);
            if (!canSetLineEnds) ImGui.BeginDisabled();
            ImGuiUtils.SizedText("Line End Style:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGuiEx.Text("A: ".Loc());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGuiUtils.EnumCombo("##LineEndA" + i + k, ref el.LineEndA, LineEnds.Names, LineEnds.Tooltips);
            ImGui.SameLine();
            ImGuiEx.Text("B: ".Loc());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGuiUtils.EnumCombo("##LineEndB" + i + k, ref el.LineEndB, LineEnds.Names, LineEnds.Tooltips);
            if (!canSetLineEnds) ImGui.EndDisabled();
        }
        if (el.type == 0 || el.type == 1 || el.type == 4 || el.type == 5)
        {
            ImGuiUtils.SizedText("Overlay text:".Loc(), WidthElement);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##overlaytext" + i + k, "Text to display as overlay".Loc(), ref el.overlayText, 30);
            if (el.overlayPlaceholders && el.type == 1)
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
                ImGuiEx.TextCopy("\\n");
            }
            if (el.overlayText.Length > 0)
            {
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.Text("Vertical offset:".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60f);
                ImGui.DragFloat("##vtextadj" + i + k, ref el.overlayVOffset, 0.02f);
                ImGui.SameLine();
                ImGuiEx.Text("Font scale:".Loc());
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60f);
                ImGui.DragFloat("##vtextsize" + i + k, ref el.overlayFScale, 0.02f, 0.1f, 50f);
                if (el.overlayFScale < 0.1f) el.overlayFScale = 0.1f;
                if (el.overlayFScale > 50f) el.overlayFScale = 50f;

                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGuiEx.Text("BG color:".Loc());
                ImGui.SameLine();
                var v4b = ImGui.ColorConvertU32ToFloat4(el.overlayBGColor);
                if (ImGui.ColorEdit4("##colorbuttonbg" + i + k, ref v4b, ImGuiColorEditFlags.NoInputs))
                {
                    el.overlayBGColor = ImGui.ColorConvertFloat4ToU32(v4b);
                }
                ImGui.SameLine();
                ImGuiEx.Text("Text color:".Loc());
                ImGui.SameLine();
                var v4t = ImGui.ColorConvertU32ToFloat4(el.overlayTextColor);
                if (ImGui.ColorEdit4("##colorbuttonfg" + i + k, ref v4t, ImGuiColorEditFlags.NoInputs))
                {
                    el.overlayTextColor = ImGui.ColorConvertFloat4ToU32(v4t);
                }
            }
            if (el.type == 1)
            {
                ImGuiUtils.SizedText("", WidthElement);
                ImGui.SameLine();
                ImGui.Checkbox("Enable placeholders".Loc() + "##" + i + k, ref el.overlayPlaceholders);
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
        ImGui.SetNextItemWidth(WidthCombo);
        ImGuiUtils.EnumCombo("##mechtype" + i + k, ref el.mechanicType, MechanicTypes.Names, MechanicTypes.Tooltips);

        if ((el.type.EqualsAny(0, 1) && el.Donut > 0) || el.type == 4 || (el.type.EqualsAny(2, 3) && (el.radius > 0 || el.includeHitbox || el.includeOwnHitbox)))
        {
            ImGuiUtils.SizedText("Fill step:".Loc(), WidthElement);
            ImGuiEx.HelpMarker("Only for ImGui Legacy renderer");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("##fillstep" + i + k, ref el.FillStep, 0.001f, 0, float.MaxValue);
            el.FillStep.ValidateRange(0.01f, float.MaxValue);
        }

    }
}
