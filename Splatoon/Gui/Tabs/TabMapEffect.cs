using ECommons.CSExtensions;
using ECommons.Hooks;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.Interop;
using Splatoon.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Gui.Tabs;

internal unsafe static class TabMapEffect
{
    public class SavedEffectList
    {
        public string Name = "";
        public ContentDirector.MapEffectItem[] List;
    }
    public static SavedEffectList SelectedList = null;

    public static List<SavedEffectList> SavedEffects = [];
    public static void Draw()
    {
        var cd = EventFramework.Instance()->GetInstanceContentDirector();
        if(cd == null || cd->MapEffects == null)
        {
            ImGuiEx.Text($"Content director is not available");
        }
        else
        {
            ImGuiEx.InputWithRightButtonsArea(() =>
            {
                if(ImGui.BeginCombo("##SavedEffects", "Select saved list", ImGuiComboFlags.HeightLarge))
                {
                    if(ImGui.Selectable("Unselect")) SelectedList = null;
                    foreach(var x in SavedEffects)
                    {
                        if(ImGui.Selectable($"{x.Name}"))
                        {
                            SelectedList = x;
                        }
                    }
                    ImGui.EndCombo();
                }
            }, () =>
            {
                if(ImGuiEx.IconButton(FontAwesomeIcon.Save))
                {
                    SavedEffects.Add(new()
                    {
                        Name = $"List {SavedEffects.Count + 1}",
                        List = cd->MapEffects->Items.ToArray()
                    });
                }
                ImGui.SameLine();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                {
                    SavedEffects.Clear();
                }
            });
            if(ImGuiEx.BeginDefaultTable("Effects", ["Effect ID", "State", "Flags", "Last call", "Call"]))
            {
                for(int i = 0; i < cd->MapEffects->Items.Length; i++)
                {
                    ImGui.PushID($"Item{i}");
                    var eff = cd->MapEffects->Items[i];
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"{i}");
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"{eff.State:X4}");
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"{eff.Flags:X2}");
                    ImGui.TableNextColumn();
                    if(MapEffectProcessor.History.TryGetValue((uint)i, out var ret))
                    {
                        ImGuiEx.TextV($"{ret.Param1:X4}, {ret.Param2:X4}");
                    }

                    ImGui.TableNextColumn();

                    ref var inp1 = ref Ref<uint>.Get($"EffectEdit1{i}");
                    ImGui.SetNextItemWidth(100f);
                    ImGuiEx.InputHex($"##eff1{i}", ref inp1);
                    ImGui.SameLine();
                    ref var inp2 = ref Ref<uint>.Get($"EffectEdit2{i}");
                    ImGui.SetNextItemWidth(100f);
                    ImGuiEx.InputHex($"##eff2{i}", ref inp2);
                    ImGui.SameLine();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Check, "Apply2"))
                    {
                        MapEffect.Delegate((long)cd, (uint)i, (ushort)inp1, (ushort)inp2);
                    }

                    ImGui.PopID();
                }
                ImGui.EndTable();
            }
        }
    }
}
