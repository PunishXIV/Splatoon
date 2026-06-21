using Dalamud.Hooking;
using Dalamud.Interface.Colors;
using ECommons.LanguageHelpers;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Shell;
using Newtonsoft.Json;
using Splatoon.Utility;
using TerraFX.Interop.Windows;

namespace Splatoon.ConfigGui.CGuiLayouts;

internal static class LayoutDrawSelector
{
    internal static Layout CurrentLayout = null;
    internal static Element CurrentElement = null;
    internal static void DrawSelector(this Layout layout, string group, int index)
    {
        if(CGui.LayoutFilter != "" && !layout.GetDisplayName().Contains(CGui.LayoutFilter, StringComparison.OrdinalIgnoreCase))
        {
            scroll();
            return;
        }
        if(P.ConfigGui.FilteredTerritory == 0)
        {
            if(CGui.ActiveExpansion != null && layout.DetermineExpansion() != CGui.ActiveExpansion.Value)
            {
                scroll();
                return;
            }
            if(CGui.ActiveContentCategory != null && layout.DetermineContentCategory() != CGui.ActiveContentCategory.Value)
            {
                scroll();
                return;
            }
        }
        else
        {
            if(!layout.ZoneLockH.Contains((ushort)P.ConfigGui.FilteredTerritory) && layout.ZoneLockH.Count > 0)
            {
                scroll();
                return;
            }
        }
        void scroll()
        {
            if(CGui.ScrollTo == layout)
            {
                CGui.ScrollTo = null;
            }
        }
        ImGui.PushID(layout.GUID);
        {
            var col = layout.PushTextColors();
            ImGui.SetCursorPosX(group == null ? 0 : 10);
            var curpos = ImGui.GetCursorScreenPos();
            var contRegion = ImGui.GetContentRegionAvail().X;
            if(CGui.ScrollTo == layout)
            {
                ImGui.SetScrollHereY();
                CGui.ScrollTo = null;
            }
            if(ImGui.Selectable($"{layout.GetDisplayName()}", CurrentLayout == layout))
            {
                if(CurrentLayout == layout && CurrentElement == null)
                {
                    CurrentLayout = null;
                    if(P.Config.FocusMode)
                    {
                        CGui.ScrollTo = layout;
                    }
                }
                else
                {
                    CGui.OpenedGroup.Add(group);
                    CurrentLayout = layout;
                    CurrentElement = null;
                }
            }
            if(col > 0)
            {
                ImGui.PopStyleColor(col);
            }
            if(ImGui.IsItemClicked(ImGuiMouseButton.Middle))
            {
                layout.Enabled = !layout.Enabled;
            }
            if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup("LayoutContext");
            }
            try
            {
                if(index != -1)
                {
                    if(ImGui.BeginDragDropSource())
                    {
                        ImGuiDragDrop.SetDragDropPayload("MoveLayout", index);
                        ImGuiEx.Text($"Moving layout\n??".Loc(layout.GetDisplayName()));
                        ImGui.EndDragDropSource();
                    }
                    if(ImGui.BeginDragDropTarget())
                    {
                        if(ImGuiDragDrop.AcceptDragDropPayload("MoveLayout", out int indexOfMovedObj,
                            ImGuiDragDropFlags.AcceptNoDrawDefaultRect | ImGuiDragDropFlags.AcceptBeforeDelivery))
                        {
                            ImGuiUtils.DrawLine(curpos, contRegion);
                            if(ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                            {
                                var exch = P.Config.LayoutsL[indexOfMovedObj];
                                exch.Group = group ?? "";
                                P.Config.LayoutsL[indexOfMovedObj] = null;
                                P.Config.LayoutsL.Insert(index, exch);
                                P.Config.LayoutsL.RemoveAll(x => x == null);
                            }
                        }
                        ImGui.EndDragDropTarget();
                    }
                }
            }
            catch(Exception e)
            {
                e.Log();
            }
            if(ImGui.BeginPopup("LayoutContext"))
            {
                ImGuiEx.Text($"Layout ??".Loc(layout.GetDisplayName()));
                if(ImGui.Selectable("Archive layout".Loc()))
                {
                    P.Archive.LayoutsL.Add(layout.JSONClone());
                    P.SaveArchive();
                    new TickScheduler(() => P.Config.LayoutsL.Remove(layout));
                }
                ImGui.Separator();
                if(ImGui.Selectable("Delete layout".Loc()))
                {
                    new TickScheduler(() => P.Config.LayoutsL.Remove(layout));
                }
                ImGui.EndPopup();
            }
        }
        if(CurrentLayout == layout)
        {
            layout.DrawLayoutElementSelector(group, index, ref CurrentElement);
        }
        ImGui.PopID();
    }

    internal static void DrawLayoutElementSelector(this Layout layout, string group, int layoutIndex, ref Element currentElement)
    {
        for(var i = 0; i < layout.GetElementsWithSubconfiguration().Count; i++)
        {
            var e = layout.GetElementsWithSubconfiguration()[i];
            ImGui.PushID(e.GUID.ToString());
            ImGui.SetCursorPosX(group == null ? 10 : 20);
            var col = layout.PushTextColors(e);
            var curpos = ImGui.GetCursorScreenPos();
            var contRegion = ImGui.GetContentRegionAvail().X;
            var cond = layout.Enabled && e.Enabled && e.Conditional;
            if(ImGui.Selectable($"{(cond && e.IsVisible() == !e.ConditionalInvert ? "↓" : null)}{(cond && layout.ConditionalAnd && e.IsVisible() == e.ConditionalInvert ? "×" : null)}{(cond && e.ConditionalReset ? "§" : null)}{(e.IsCapturing ? "©" : "")}{(e.Nodraw ? "Ø" : "")}{e.GetName(layout)}", currentElement == e))
            {
                if(currentElement == e)
                {
                    currentElement = null;
                }
                else
                {
                    CGui.OpenedGroup.Add(group);
                    currentElement = e;
                }
            }
            if(col > 0)
            {
                ImGui.PopStyleColor(col);
            }
            if(ImGui.IsItemClicked(ImGuiMouseButton.Middle))
            {
                e.Enabled = !e.Enabled;
            }
            if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup("ElementContext");
            }
            if(ImGui.BeginDragDropSource())
            {
                ImGuiDragDrop.SetDragDropPayload($"MoveElement{layoutIndex}", i);
                ImGuiEx.Text($"Moving element\n??".Loc(layout.GetDisplayName()));
                ImGui.EndDragDropSource();
            }
            if(ImGui.BeginDragDropTarget())
            {
                if(ImGuiDragDrop.AcceptDragDropPayload($"MoveElement{layoutIndex}", out int indexOfMovedObj,
                    ImGuiDragDropFlags.AcceptNoDrawDefaultRect | ImGuiDragDropFlags.AcceptBeforeDelivery))
                {
                    ImGuiUtils.DrawLine(curpos, contRegion);
                    if(ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        var exch = layout.GetElementsWithSubconfiguration()[indexOfMovedObj];
                        layout.GetElementsWithSubconfiguration()[indexOfMovedObj] = null;
                        layout.GetElementsWithSubconfiguration().Insert(i, exch);
                        layout.GetElementsWithSubconfiguration().RemoveAll(x => x == null);
                    }
                }
                ImGui.EndDragDropTarget();
            }
            if(ImGui.BeginPopup("ElementContext"))
            {
                ImGuiEx.Text($"{"Layout".Loc()} {layout.GetDisplayName()}\n{"Element".Loc()} {e.GetName(layout)}");
                if(ImGui.Selectable("Delete element".Loc()))
                {
                    var l = layout.GetElementsWithSubconfiguration();
                    new TickScheduler(() => l.Remove(e));
                }
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }
        var elementRefCopy = currentElement;
        ImGuiEx.LineCentered("AddElement", delegate
        {
            if(ImGui.SmallButton("Add element".Loc()))
            {
                layout.GetElementsWithSubconfiguration().Add(new(0));
            }
            ImGui.SameLine();
            if(ImGui.SmallButton("Paste".Loc()))
            {
                try
                {
                    var l = JsonConvert.DeserializeObject<Element>(ImGui.GetClipboardText()) ?? throw new NullReferenceException();
                    if(l.GetType().GetFieldPropertyUnions().Any(x => !x.UnionType.IsValueType && x.GetValue(l) == null)) throw new NullReferenceException();
                    layout.GetElementsWithSubconfiguration().Add(l);
                }
                catch(Exception e)
                {
                    e.Log();
                    Notify.Error($"{e.Message}");
                }
            }
            ImGui.SameLine();
            if(ImGuiEx.SmallButton("Clone".Loc(), elementRefCopy != null))
            {
                try
                {
                    layout.GetElementsWithSubconfiguration().Add(elementRefCopy.DSFClone());
                }
                catch(Exception e)
                {
                    e.Log();
                }
            }
        });
    }

    private static int PushTextColors(this Layout l)
    {
        var ret = 0;
        if(l.Enabled)
        {
            if(l.IsVisible())
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Colors.ElementLayoutIsVisible);
                ret++;
            }
        }
        else
        {
            var col = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
            ImGui.PushStyleColor(ImGuiCol.Text, col with { W = col.W * 0.5f });
            ret++;
        }
        return ret;
    }

    private static int PushTextColors(this Layout l, Element e)
    {
        var ret = 0;
        if(e.Enabled)
        {
            if(l.Enabled)
            {
                if(e.IsVisible())
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, e.Conditional ? Colors.ElementIsConditionalVisible : Colors.ElementLayoutIsVisible);
                    ret++;
                }
                else if(e.Conditional)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Colors.ElementIsConditional);
                    ret++;
                }
            }
            else
            {
                var col = e.Conditional ? Colors.ElementIsConditional : ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
                ImGui.PushStyleColor(ImGuiCol.Text, col with { W = col.W * 0.75f * (l.Enabled ? 1f : 0.5f) });
                ret++;
            }
        }
        else
        {
            var col = e.Conditional ? Colors.ElementIsConditional : ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
            ImGui.PushStyleColor(ImGuiCol.Text, col with { W = col.W * 0.75f * (l.Enabled ? 0.5f : 0.25f) });
            ret++;
        }
        return ret;
    }
}
