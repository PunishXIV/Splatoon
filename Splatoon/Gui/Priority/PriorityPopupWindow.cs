using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ECommons.ChatMethods;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.LanguageHelpers;
using ECommons.PartyFunctions;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Gui.Priority;
#nullable enable
public class PriorityPopupWindow : Window
{
    public uint TerritoryType;
    public List<JobbedPlayer> Assignments = [];
    public static readonly IReadOnlyList<RolePosition> RolePositions = [RolePosition.T1,  RolePosition.T2, RolePosition.H1, RolePosition.H2, RolePosition.M1, RolePosition.M2, RolePosition.R1, RolePosition.R2,];
    public static readonly IReadOnlyDictionary<RolePosition, string> NormalNames = new Dictionary<RolePosition, string>()
    {
        [RolePosition.Not_Selected] = "Not Selected",
        [RolePosition.T1] = "T1",
        [RolePosition.T2] = "T2",
        [RolePosition.H1] = "H1",
        [RolePosition.H2] = "H2",
        [RolePosition.M1] = "M1",
        [RolePosition.M2] = "M2",
        [RolePosition.R1] = "R1",
        [RolePosition.R2] = "R2",
    };
    public static readonly IReadOnlyDictionary<RolePosition, string> DpsUniformNames = new Dictionary<RolePosition, string>()
    {
        [RolePosition.Not_Selected] = "Not Selected",
        [RolePosition.T1] = "T1",
        [RolePosition.T2] = "T2",
        [RolePosition.H1] = "H1",
        [RolePosition.H2] = "H2",
        [RolePosition.M1] = "D1",
        [RolePosition.M2] = "D2",
        [RolePosition.R1] = "D3",
        [RolePosition.R2] = "D4",
    };
    public static IReadOnlyDictionary<RolePosition, string> ConfiguredNames => (P.Config.PrioUnifyDps ? DpsUniformNames : NormalNames);
    private ImGuiEx.RealtimeDragDrop<JobbedPlayer> DragDrop = new("PrioAss", x => x.ID);

    public PriorityPopupWindow() : base("Splatoon Priority Editor", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        this.SetSizeConstraints(new(500, 100), new(500, float.MaxValue));
        this.ShowCloseButton = false;
    }

    public override void Draw()
    {
        while(this.Assignments.Count < 8)
        {
            this.Assignments.Add(new());
        }
        while(this.Assignments.Count > 8)
        {
            this.Assignments.RemoveAt(this.Assignments.Count - 1);
        }
        ImGuiEx.TextWrapped($"You have entered a zone for which you have enabled scripts that use priority lists. If you have any priority lists set to \"Placeholder\" mode, please configure them here.");
        ImGuiEx.CollectionCheckbox($"Display this popup in {ExcelTerritoryHelper.GetName(TerritoryType)}", TerritoryType, P.Config.NoPrioPopupTerritories, true);
        ImGui.Checkbox("Display DPS as D1/D2/D3/D4", ref P.Config.PrioUnifyDps);
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.List, "Fill automatically", enabled:ImGuiEx.Ctrl || this.Assignments.All(x => x.Name == "" && x.Jobs.Count == 0)))
        {
            Autofill();
        }
        ImGuiEx.Tooltip("Hold CTRL and click");

        DragDrop.Begin();
        if(ImGui.BeginTable("PrioAssTable", 3, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Position");
            ImGui.TableSetupColumn("DragDrop");
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);

            for(int i = 0; i < Assignments.Count; i++)
            {
                var item = Assignments[i];
                ImGui.PushID(item.ID);
                ImGui.TableNextRow();
                DragDrop.SetRowColor(item.ID);
                ImGui.TableNextColumn();
                DragDrop.NextRow();
                var col = (int)RolePositions[i] >= 3000 ? ImGuiColors.DPSRed : ((int)RolePositions[i] >= 2000 ? ImGuiColors.HealerGreen : ImGuiColors.TankBlue);
                ImGuiEx.TextV(col, ConfiguredNames[RolePositions[i]].FancySymbols());
                ImGui.TableNextColumn();
                DragDrop.DrawButtonDummy(item, Assignments, i);
                ImGui.TableNextColumn();
                item.DrawSelector(false);
                ImGui.PopID();
            }

            ImGui.EndTable();
        }
        DragDrop.End();

        ImGuiEx.LineCentered("PrioPopupWindow1", () =>
        {
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Check, "Apply".Loc()))
            {
                this.IsOpen = false;
            }
            ImGui.SameLine();
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Save, "Apply and save".Loc()))
            {
                this.IsOpen = false;
                Save();
            }
            ImGuiEx.Tooltip($"When you will join {ExcelTerritoryHelper.GetName(TerritoryType)} again with same players on same jobs, this priority list will be loaded again.");
        });
    }

    public void Save()
    {
        while(true)
        {
            var ass = GetMatchingAssignment(TerritoryType);
            if(ass != null)
            {
                P.Config.RolePlayerAssignments.Remove(ass);
            }
            else
            {
                break;
            }
        }
        P.Config.RolePlayerAssignments.Add(new()
        {
            Players = Assignments.JSONClone(),
            Territory = this.TerritoryType
        });
        P.Config.Save();
    }

    public bool IsValid()
    {
        return UniversalParty.LengthPlayback == this.Assignments.Count(x => x.IsInParty(false, out _));
    }

    public void Autofill()
    {
        new TickScheduler(() =>
        {
            Assignments = UniversalParty.MembersPlayback.OrderBy(x => GetOrderedRoleIndex(x.ClassJob)).Select(x => new JobbedPlayer() { Jobs = [x.ClassJob], Name = x.NameWithWorld }).ToList();
        });
    }

    int GetOrderedRoleIndex(Job job)
    {
        if(Svc.Data.GetExcelSheet<ClassJob>().TryGetRow((uint)job, out var data))
        {
            if(data.Role == 1) return 1;
            if(data.Role == 2) return 3;
            if(data.Role == 3) return 4;
            if(data.Role == 4) return 2;
        }
        return 999;
    }

    public bool ShouldAutoOpen()
    {
        return ScriptingProcessor.Scripts.Any(x => !x.IsDisabledByUser && x.ValidTerritories?.Contains(this.TerritoryType) == true && !P.Config.NoPrioPopupTerritories.Contains(this.TerritoryType));
    }

    public void Open(bool force)
    {
        this.IsOpen = false;
        this.TerritoryType = Svc.ClientState.TerritoryType;
        P.TaskManager.Abort();
        if(force)
        {
            open();
        }
        else
        {
            P.TaskManager.Enqueue(() =>
            {
                if(Player.Available)
                {
                    var ass = GetMatchingAssignment(this.TerritoryType);
                    if(ass != null)
                    {
                        this.Assignments = ass.Players.JSONClone();
                        ChatPrinter.Green($"[Splatoon] Priority assignments loaded for {ExcelTerritoryHelper.GetName(this.TerritoryType)}:\n{RolePositions.Select(x => $"{x}: {Assignments.SafeSelect(RolePositions.IndexOf(x)).GetNameAndJob()}").Print("\n")}");
                    }
                    open();
                    return true;
                }
                return false;
            });
        }
        void open()
        {
            if(force || ShouldAutoOpen())
            {
                this.IsOpen = true;
            }
        }
    }

    public RolePlayerAssignment? GetMatchingAssignment(uint territory)
    {
        foreach(var x in P.Config.RolePlayerAssignments)
        {
            if(x.Territory == territory)
            {
                var members = UniversalParty.Members;
                foreach(var player in x.Players)
                {
                    if(player.Jobs.Count > 0)
                    {
                        if(members.TryGetFirst(p => p.ClassJob.EqualsAny(player.Jobs) 
                        && 
                        (player.Name == "" || p.NameWithWorld.EqualsIgnoreCase(player.Name) || p.Name.EqualsIgnoreCase(player.Name))
                        , out var upm))
                        {
                            members.Remove(upm);
                        }
                    }
                    else
                    {
                        if(members.TryGetFirst(p => p.NameWithWorld.EqualsIgnoreCase(player.Name) || p.Name.EqualsIgnoreCase(player.Name), out var upm))
                        {
                            members.Remove(upm);
                        }
                    }
                }
                if(members.Count == 0)
                {
                    return x;
                }
            }
        }
        return null;
    }
}
