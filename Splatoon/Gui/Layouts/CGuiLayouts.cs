﻿using Dalamud.Interface.Colors;
using ECommons.LanguageHelpers;
using Splatoon.SplatoonScripting;
using static Splatoon.ConfigGui.CGuiLayouts.LayoutDrawSelector;

namespace Splatoon;

partial class CGui
{
    public class LayoutFolder
    {
        public string Name;
        public string FullName;
        public List<LayoutFolder> Folders = [];
        public List<Layout> Layouts = [];

        public LayoutFolder(string name, string fullName)
        {
            Name = name;
            FullName = fullName;
        }
    }

    internal static string layoutFilter = "";
    string PopupRename = "";
    //internal static string CurrentGroup = null;
    internal static string HighlightGroup = null;
    internal static HashSet<string> OpenedGroup = new();
    internal static string NewLayoytName = "";
    internal static Layout ScrollTo = null;
    internal LayoutFolder LayoutFolderStructure;

    void BuildLayoutFolderStructure()
    {
        LayoutFolderStructure = new("", "");
        foreach(var x in P.Config.LayoutsL)
        {
            var currentLayout = LayoutFolderStructure;
            if(x.Group != "")
            {
                var path = x.Group.Split("/");
                foreach(var subFolder in path)
                {
                    if(currentLayout.Folders.TryGetFirst(x => x.Name == subFolder, out var result))
                    {
                        currentLayout = result;
                    }
                    else
                    {
                        var n = new LayoutFolder(subFolder, currentLayout.FullName + "/" + subFolder);
                        currentLayout.Folders.Add(n);
                        currentLayout = n;
                    }
                }
            }
            currentLayout.Layouts.Add(x);
        }
        OrderFolders(LayoutFolderStructure);
    }

    void OrderFolders(LayoutFolder f)
    {
        f.Folders.Sort((x, y) => FindOrderIndex(x.FullName).CompareTo(FindOrderIndex(y.FullName)));
        foreach(var x in f.Folders) OrderFolders(x);
    }

    int FindOrderIndex(string fullPath)
    {
        var i = P.Config.GroupOrder.IndexOf(fullPath);
        return i == -1 ? int.MaxValue : i;
    }

    void DislayLayouts()
    {
        {
            var deleted = P.Config.LayoutsL.RemoveAll(x => x.Delete);
            if (deleted > 0)
            {
                Notify.Info($"Removed ?? layouts".Loc(deleted));
                if (!P.Config.LayoutsL.Contains(CurrentLayout))
                {
                    CurrentLayout = null;
                    CurrentElement = null;
                }
            }
        }
        ImGui.BeginChild("TableWrapper", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (ImGui.BeginTable("LayoutsTable", 2, ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("Layout list".Loc()+"###Layout id", ImGuiTableColumnFlags.None, 200);
            ImGui.TableSetupColumn($"{(CurrentLayout == null ? "" : $"{CurrentLayout.GetName()}") + (CurrentElement == null ? "" : $" | {CurrentElement.GetName()}")}###Layout edit", ImGuiTableColumnFlags.None, 600);

            ImGui.TableHeadersRow();

            ImGui.TableNextColumn();
            ImGuiEx.InputWithRightButtonsArea("Search layouts", delegate
            {
                ImGui.InputTextWithHint("##layoutFilter", "Search layouts...".Loc(), ref layoutFilter, 100);
            }, delegate
            {
                if (ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                {
                    ImGui.OpenPopup("Add layout");
                }
                ImGuiEx.Tooltip("Add new layout...".Loc());
                ImGui.SameLine(0, 1);
                if(ImGuiEx.IconButton(P.Config.FocusMode? FontAwesomeIcon.SearchMinus: FontAwesomeIcon.SearchPlus))
                {
                    P.Config.FocusMode = !P.Config.FocusMode;
                }
                ImGuiEx.Tooltip("Toggle focus mode.\nFocus mode: when layout is selected, hide all other layouts.".Loc());
                ImGui.SameLine(0, 2);
                if(ImGuiEx.IconButton(FontAwesomeIcon.Sort))
                {
                    P.Config.GroupOrder.Sort();
                }
                ImGuiEx.Tooltip("Sorts groups alphabetically.".Loc());
            });
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            if (ImGui.Button("Import from clipboard".Loc(), new(ImGui.GetContentRegionAvail().X, ImGui.CalcTextSize("A").Y)))
            {
                Safe(() =>
                {
                    var text = ImGui.GetClipboardText();
                    if (ScriptingProcessor.IsUrlTrusted(text))
                    {
                        ScriptingProcessor.DownloadScript(text);
                    }
                    else
                    {
                        ImportFromClipboard();
                    }
                });

            }
            ImGui.PopStyleVar();
            if (ImGui.BeginPopup("Add layout"))
            {
                ImGui.InputTextWithHint("", "Layout name".Loc(), ref NewLayoytName, 100);
                ImGui.SameLine();
                if (ImGui.Button("Add".Loc()))
                {
                    if (CGui.AddEmptyLayout(out var newLayout))
                    {
                        ImGui.CloseCurrentPopup();
                        Notify.Success($"Layout created: ??".Loc(newLayout.GetName()));
                        ScrollTo = newLayout;
                        CurrentLayout = newLayout;
                    }
                }
                ImGui.EndPopup();
            }
            ImGui.BeginChild("LayoutsTableSelector");
            //DrawNewSelector();
            DrawOldSelector();
            ImGui.EndChild();

            ImGui.TableNextColumn();

            ImGui.BeginChild("LayoutsTableEdit", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.HorizontalScrollbar);
            if(CurrentLayout != null)
            {
                if(CurrentElement != null && CurrentLayout.ElementsL.Contains(CurrentElement))
                {
                    LayoutDrawElement(CurrentLayout, CurrentElement);
                }
                else
                {
                    LayoutDrawHeader(CurrentLayout);
                }
            }
            else
            {
                ImGuiEx.Text("UI Help:\n- Left panel contains groups, layouts and elements.\n- You can drag and drop layouts, elements and groups to reorder them.\n- Right click on a group to rename or delete it.\n- Right click on a layout/element to delete it.\n- Middle click on layout/element for quick enable/disable".Loc());
            }
            ImGui.EndChild();

            ImGui.EndTable();
        }
        ImGui.EndChild();
    }

    private void DrawNewSelector()
    {
        BuildLayoutFolderStructure();
        ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 10f);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(1, 1));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0));
        DrawFolder(LayoutFolderStructure);
        ImGui.PopStyleVar(3);
    }

    private void DrawFolder(LayoutFolder f)
    {
        ImGui.PushID(f.FullName);
        foreach(var x in f.Folders)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiEx.Vector4FromRGB(0xfae97d));
            if(ImGuiEx.TreeNode(x.Name))
            {
                ImGui.PopStyleColor();
                DrawFolder(x);
                ImGui.TreePop();
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }
        foreach(var x in f.Layouts)
        {
            if(ImGui.TreeNodeEx($"{x.Name}###{x.GUID}", ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Bullet | (CurrentLayout == x ? ImGuiTreeNodeFlags.Selected : ImGuiTreeNodeFlags.None)))
            {
                CurrentLayout = x;
                ImGui.GetStateStorage().SetInt(ImGui.GetID($"{x.Name}###{x.GUID}"), 0);
            }
        }
        ImGui.PopID();
    }


    private void DrawOldSelector()
    {

        foreach(var x in P.Config.LayoutsL)
        {
            x.ElementsL.RemoveAll(z => z == null);
            var deleted = x.ElementsL.RemoveAll(k => k.Delete);
            if(deleted > 0)
            {
                Notify.Info($"Deleted ?? elements".Loc(deleted));
                if(!P.Config.LayoutsL.Any(l => l.ElementsL.Contains(CurrentElement)))
                {
                    CurrentElement = null;
                }
            }
            if(x.Group == null) x.Group = "";
            if(x.Group != "" && !P.Config.GroupOrder.Contains(x.Group))
            {
                P.Config.GroupOrder.Add(x.Group);
            }
        }
        P.Config.GroupOrder.RemoveAll(x => x.IsNullOrEmpty());
        Layout[] takenLayouts = P.Config.LayoutsL.ToArray();
        var groupToRemove = -1;
        if(!P.Config.FocusMode || CurrentLayout == null)
        {
            for(var i = 0; i < P.Config.GroupOrder.Count; i++)
            {
                var g = P.Config.GroupOrder[i];
                if(layoutFilter != "" &&
                    !P.Config.LayoutsL.Any(x => x.Group == g && x.GetName().Contains(layoutFilter, StringComparison.OrdinalIgnoreCase))) continue;

                ImGui.PushID(g);
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);

                if(HighlightGroup == g)
                {
                    ImGui.PushStyleColor(ImGuiCol.Header, ImGuiColors.DalamudYellow with { W = 0.5f });
                    ImGui.PushStyleColor(ImGuiCol.HeaderActive, ImGuiColors.DalamudYellow with { W = 0.5f });
                    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ImGuiColors.DalamudYellow with { W = 0.5f });
                }
                var curpos = ImGui.GetCursorScreenPos();
                var contRegion = ImGui.GetContentRegionAvail().X;
                if(ImGui.Selectable($"[{g}]", HighlightGroup == g))
                {
                    if(!OpenedGroup.Toggle(g))
                    {
                        if(CurrentLayout?.Group == g)
                        {
                            CurrentLayout = null;
                            CurrentElement = null;
                        }
                    }
                }
                if(HighlightGroup == g)
                {
                    ImGui.PopStyleColor(3);
                    HighlightGroup = null;
                }
                ImGui.PopStyleColor();
                if(ImGui.BeginDragDropSource())
                {
                    ImGuiDragDrop.SetDragDropPayload("MoveGroup", i);
                    ImGuiEx.Text($"Moving group\n[??]".Loc(g));
                    ImGui.EndDragDropSource();
                }
                if(ImGui.BeginDragDropTarget())
                {
                    if(ImGuiDragDrop.AcceptDragDropPayload("MoveLayout", out int indexOfMovedObj
                        , ImGuiDragDropFlags.AcceptNoDrawDefaultRect | ImGuiDragDropFlags.AcceptBeforeDelivery))
                    {
                        HighlightGroup = g;
                        if(ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                        {
                            P.Config.LayoutsL[indexOfMovedObj].Group = g;
                        }
                    }
                    if(ImGuiDragDrop.AcceptDragDropPayload("MoveGroup", out int indexOfMovedGroup
                        , ImGuiDragDropFlags.AcceptNoDrawDefaultRect | ImGuiDragDropFlags.AcceptBeforeDelivery))
                    {
                        ImGuiUtils.DrawLine(curpos, contRegion);
                        if(ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                        {
                            var exch = P.Config.GroupOrder[indexOfMovedGroup];
                            P.Config.GroupOrder[indexOfMovedGroup] = null;
                            P.Config.GroupOrder.Insert(i, exch);
                            P.Config.GroupOrder.RemoveAll(x => x == null);
                        }
                    }
                    ImGui.EndDragDropTarget();
                }
                if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    ImGui.OpenPopup("GroupPopup");
                }
                if(ImGui.BeginPopup("GroupPopup"))
                {
                    ImGuiEx.Text($"[{g}]");
                    ImGui.SetNextItemWidth(200f);
                    var result = ImGui.InputTextWithHint("##GroupRename", "Enter new name...".Loc(), ref PopupRename, 100, ImGuiInputTextFlags.EnterReturnsTrue);
                    PopupRename = PopupRename.SanitizeName();
                    ImGui.SameLine();
                    if(ImGui.Button("OK".Loc()) || result)
                    {
                        if(P.Config.GroupOrder.Contains(PopupRename))
                        {
                            Notify.Error("Error: this name is already exists".Loc());
                        }
                        else if(PopupRename.Length == 0)
                        {
                            Notify.Error("Error: empty names are not allowed".Loc());
                        }
                        else
                        {
                            if(OpenedGroup.Contains(g))
                            {
                                OpenedGroup.Add(PopupRename);
                                OpenedGroup.Remove(g);
                            }
                            foreach(var x in P.Config.LayoutsL)
                            {
                                if(x.Group == g)
                                {
                                    x.Group = PopupRename;
                                }
                            }
                            P.Config.GroupOrder[i] = PopupRename;
                            PopupRename = "";
                        }
                    }
                    if(ImGui.Selectable("Archive group".Loc()) && ImGui.GetIO().KeyCtrl)
                    {
                        foreach(var l in P.Config.LayoutsL)
                        {
                            if(l.Group == g)
                            {
                                P.Archive.LayoutsL.Add(l.JSONClone());
                                l.Group = "";
                                l.Delete = true;
                            }
                        }
                        groupToRemove = i;
                        P.SaveArchive();
                    }
                    ImGuiEx.Tooltip("Hold CTRL+click".Loc());
                    ImGui.Separator();
                    if(ImGui.Selectable("Remove group and disband layouts".Loc()) && ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift)
                    {
                        foreach(var l in P.Config.LayoutsL)
                        {
                            if(l.Group == g)
                            {
                                l.Group = "";
                            }
                        }
                        groupToRemove = i;
                    }
                    ImGuiEx.Tooltip("Hold CTRL+SHIFT+click".Loc());
                    if(ImGui.Selectable("Remove group and it's layouts".Loc()) && ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift)
                    {
                        foreach(var l in P.Config.LayoutsL)
                        {
                            if(l.Group == g)
                            {
                                l.Group = "";
                                l.Delete = true;
                            }
                        }
                        groupToRemove = i;
                    }
                    ImGuiEx.Tooltip("Hold CTRL+SHIFT+click".Loc());
                    if(ImGui.Selectable("Export Group".Loc()))
                    {
                        List<string> Export = new();
                        foreach(var l in P.Config.LayoutsL)
                        {
                            if(l.Group == g)
                            {
                                Export.Add(l.Serialize());
                            }
                        }
                        ImGui.SetClipboardText(Export.Join("\n"));
                    }
                    ImGui.EndPopup();
                }
                for(var n = 0; n < takenLayouts.Length; n++)
                {
                    var x = takenLayouts[n];
                    if(x != null && (x.Group == g))
                    {
                        if(OpenedGroup.Contains(g) || layoutFilter != "")
                        {
                            x.DrawSelector(g, n);
                        }
                        takenLayouts[n] = null;
                    }
                }
                ImGui.PopID();
            }
        }
        for(var i = 0; i < takenLayouts.Length; i++)
        {
            var x = takenLayouts[i];
            if(!P.Config.FocusMode || CurrentLayout == x || CurrentLayout == null)
            {
                if(x != null)
                {
                    x.DrawSelector(null, i);
                }
            }
        }
        if(groupToRemove != -1)
        {
            P.Config.GroupOrder.RemoveAt(groupToRemove);
        }
    }

    internal static bool ImportFromClipboard()
    {
        var ls = Utils.ImportLayouts(ImGui.GetClipboardText());
        {
            foreach (var l in ls)
            {
                CurrentLayout = l;
                if (l.Group != "")
                {
                    OpenedGroup.Add(l.Group);
                }
            }
        }
        return ls.Count > 0;
    }
}
