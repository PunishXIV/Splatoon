using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using NotificationMasterAPI;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public sealed class SimpleMitigations : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [];

    Dictionary<string, long> TrackedTimes = [];

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category == DirectorUpdateCategory.Commence || category == DirectorUpdateCategory.Recommence || category == DirectorUpdateCategory.Wipe)
        {
            TrackedTimes.Clear();
        }
    }

    public override void OnCombatStart()
    {
        TrackedTimes.Clear();
    }

    public override void OnCombatEnd()
    {
        TrackedTimes.Clear();
    }

    public unsafe override void OnUpdate()
    {
        if(!Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]) return;
        var names = C.Data.Select(x => x.Name).ToHashSet();
        foreach(var x in Svc.Objects)
        {
            if(x is IBattleNpc && x.IsTargetable)
            {
                var n = x.Name.ToString();
                if(names.Contains(n))
                {
                    if(!TrackedTimes.ContainsKey(n))
                    {
                        TrackedTimes[n] = Environment.TickCount64;
                    }
                    if(C.Data.TryGetFirst(d => d.Name == n && GetTime(d.Name).InRange(d.Time, d.Time + 5), out var data))
                    {
                        var s = ActionManager.Instance()->GetActionStatus(data.General ? ActionType.GeneralAction : ActionType.Action, (uint)data.Action);
                        if(s == 0)
                        {
                            if(EzThrottler.Throttle("Cast", 50) && !Player.Object.IsDead && GenericHelpers.IsScreenReady())
                            {
                                if(data.General)
                                {
                                    Chat.ExecuteGeneralAction((uint)data.Action);
                                }
                                else
                                {
                                    Chat.ExecuteAction((uint)data.Action);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        if(ImGuiEx.IconButtonWithText(Dalamud.Interface.FontAwesomeIcon.Plus, "Add")) C.Data.Add(new());
        if(ImGuiEx.BeginDefaultTable(["~Boss Name", "Action", "Time", "##trash"]))
        {
            foreach(var x in C.Data)
            {
                ImGui.PushID($"{x.GUID}");
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputText("##name", ref x.Name);
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(100f);
                ImGui.InputInt("##action", ref x.Action);
                ImGuiEx.Tooltip($"{ExcelActionHelper.GetActionName((uint)x.Action)}");
                ImGui.SameLine();
                ImGuiEx.ButtonCheckbox(Dalamud.Interface.FontAwesomeIcon.PeopleGroup, ref x.General);
                ImGuiEx.Tooltip("Is general action");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(100f);
                ImGui.InputFloat("##time", ref x.Time);
                ImGui.TableNextColumn();
                if(ImGuiEx.IconButton(Dalamud.Interface.FontAwesomeIcon.Trash))
                {
                    new TickScheduler(() => C.Data.Remove(x));
                }
                ImGui.PopID();
            }
            ImGui.EndTable();
        }

        if(ImGui.CollapsingHeader("Debug"))
        {

            var names = C.Data.Select(x => x.Name).ToHashSet();
            foreach(var n in names)
            {
                ImGuiEx.Text($"{n}: {GetTime(n)}");
            }
        }
    }

    float GetTime(string s)
    {
        if(TrackedTimes.TryGetValue(s, out var ret))
        {
            return (Environment.TickCount64 - ret) / 1000f;
        }
        return -999;
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public List<MitData> Data = [];
    }

    public class MitData
    {
        internal Guid GUID = Guid.NewGuid();
        public string Name;
        public float Time;
        public int Action;
        public bool General = false;
    }
}