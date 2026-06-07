using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ECommons.Configuration;
using ECommons.LanguageHelpers;
using ECommons.SimpleGui;
using Splatoon.ConfigGui.CGuiLayouts;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;
using TerraFX.Interop.Windows;
using static Splatoon.CGui;

namespace Splatoon.Gui.Windows;

internal class PinnedLayoutEdit : Window
{
    internal Layout EditingLayout;
    internal Element CurrentElement = null;
    internal string Key;
    internal SplatoonScript Script;
    public PinnedLayoutEdit() : base("###Pinned layout editor")
    {
        SizeConstraints = new()
        {
            MinimumSize = new(200, 200),
            MaximumSize = new(float.MaxValue, float.MaxValue),
        };
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    public override void Draw()
    {
        if(EditingLayout != null && Script != null)
        {
            if(ImGui.BeginTable("LayoutsTable", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Layout list".Loc() + "###Layout id", ImGuiTableColumnFlags.None, 200);
                ImGui.TableSetupColumn($"{EditingLayout.GetName()}{(CurrentElement == null ? "" : $" | {CurrentElement.GetName(EditingLayout)}")}###Pinned Layout edit", ImGuiTableColumnFlags.None, 600);

                //ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                ImGui.BeginChild("LayoutsTableSelector");
                Vector4? col = EditingLayout.Enabled ? null : ImGuiColors.DalamudGrey3;
                if(col != null) ImGui.PushStyleColor(ImGuiCol.Text, col.Value);
                if(ImGui.Selectable($"{EditingLayout.GetName()}", true)) CurrentElement = null;
                if(col != null) ImGui.PopStyleColor();
                if(ImGui.IsItemClicked(ImGuiMouseButton.Middle)) EditingLayout.Enabled = !EditingLayout.Enabled;
                EditingLayout.DrawLayoutElementSelector(null, -1, ref CurrentElement);
                ImGui.EndChild();

                ImGui.TableNextColumn();

                ImGui.BeginChild("LayoutsTableEdit", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.HorizontalScrollbar);
                if(CurrentElement != null)
                {
                    P.ConfigGui.LayoutDrawElement(EditingLayout, CurrentElement);
                }
                else
                {
                    ImGuiEx.TextWrapped("Select element to edit. Changing element's name may break the script.");
                }
                ImGui.EndChild();

                ImGui.EndTable();
            }
        }
        else
        {
            ImGuiEx.Text($"An error has occurred.");
        }
    }

    public override void OnClose()
    {
        Script.Controller.SaveOverrides();
        Notify.Info("Override saved");
        Script.Controller.ApplyOverrides();
        EditingLayout = null;
        Script = null;
    }

    internal void Open(SplatoonScript s, string key)
    {
        Key = key;
        if(EditingLayout != null && Script != null)
        {
            OnClose();
        }
        EditingLayout = s.InternalData.Overrides.Layouts[key];
        Script = s;
        WindowName = $"Editing element [{key}] from {s.InternalData.FullName}###Pinned element editor";
        IsOpen = true;
    }

    public override bool DrawConditions()
    {
        return P.s2wInfo == null;
    }

    public override void Update()
    {
        Script.Controller.ApplySingleLayoutOverride(Key, EditingLayout);
    }
}
