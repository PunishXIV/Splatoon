using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.ChatMethods;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.LanguageHelpers;
using ECommons.PartyFunctions;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace Splatoon.Gui.Scripting;
#nullable enable
public class ScriptUpdateWindow : Window
{
    public ScriptUpdateWindow() : base("Splatoon Scripting - load results", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse)
    {
        this.SetSizeConstraints(new(500, 100), new(float.MaxValue, float.MaxValue));
        this.RespectCloseHotkey = false;
    }

    internal List<SplatoonScript> UpdatedScripts = [];
    internal List<string> FailedScripts = [];

    public override void Draw()
    {
        if(UpdatedScripts.Count > 0)
        {
            ImGuiEx.TextWrapped($"The following scripts have been updated. Please check that your settings are intact, and if needed, reconfigure it.");
            if(ImGui.BeginTable("##table1", 2, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("2");

                foreach(var x in UpdatedScripts)
                {
                    ImGui.PushID(x.InternalData.FullName);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGuiEx.TextWrapped($"{x.InternalData.FullName} - v{x.Metadata?.Version ?? 0}");
                    var change = x.Changelog?.SafeSelect((int?)x.Metadata?.Version ?? 0);
                    if(change != null)
                    {
                        ImGui.Indent();
                        ImGuiEx.TextWrapped(ImGuiColors.DalamudGrey, change);
                        ImGui.Unindent();
                    }
                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Cog))
                    {
                        P.ConfigGui.Open = true;
                        P.ConfigGui.TabRequest = "Scripts".Loc();
                        Svc.Framework.RunOnTick(() =>
                        {
                            TabScripting.RequestOpen = x.InternalData.FullName;
                        }, delayTicks:2);
                    }
                    ImGui.PopID();
                }
                ImGui.EndTable();
            }
            
        }
        if(FailedScripts.Count > 0)
        {
            ImGuiEx.TextWrapped($"The following scripts have failed to load. ");
            if(ImGui.BeginTable("##table1", 2, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("2");

                var rep = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "Scripts");
                foreach(var x in FailedScripts)
                {
                    ImGui.PushID(x);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGuiEx.TextWrapped($"{x.Replace(rep, "..")}");
                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                    {
                        new TickScheduler(() =>
                        {
                            FailedScripts.Remove(x);
                            GenericHelpers.DeleteFileToRecycleBin(x);
                        });
                    }
                    ImGuiEx.Tooltip("Delete this script");
                    ImGui.PopID();
                }
                ImGui.EndTable();
            }
        }
    }

    public void Open()
    {
        this.IsOpen = true;
    }

    public void Reset()
    {
        this.UpdatedScripts.Clear();
        this.FailedScripts.Clear();
    }
}
