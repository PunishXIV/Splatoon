using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.PartyFunctions;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;
internal class P1_Turn_of_the_Heavens : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1238];
    public override Metadata? Metadata => new(2, "Redmoon");

    private Config Conf => Controller.GetConfig<Config>();
    private List<IPlayerCharacter> _sortedList = [];
    private List<IPlayerCharacter> _stackedList = [];
    private bool _mechanicActive = false;

    private readonly ImGuiEx.RealtimeDragDrop<Job> DragDrop = new("DragDropJob", x => x.ToString());

    public class Config : IEzConfig
    {
        public bool DebugPrint = false;
        public List<string[]> LeftRightPriorities = [];

        public List<Job> Jobs =
        [
            Job.PLD,
            Job.WAR,
            Job.DRK,
            Job.GNB,
            Job.WHM,
            Job.SCH,
            Job.AST,
            Job.SGE,
            Job.VPR,
            Job.DRG,
            Job.MNK,
            Job.SAM,
            Job.RPR,
            Job.NIN,
            Job.BRD,
            Job.MCH,
            Job.DNC,
            Job.BLM,
            Job.SMN,
            Job.RDM,
            Job.PCT
        ];
    }

    public override void OnSetup()
    {
        Controller.RegisterElement("Stack1", new Splatoon.Element(1) { thicc = 15f, radius = 1.0f, Filled = false, refActorComparisonType = 2 });
        Controller.RegisterElement("Stack2", new Splatoon.Element(1) { thicc = 15f, radius = 1.0f, Filled = false, refActorComparisonType = 2 });
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("# How to determine left/right priority : ");
        ImGui.SameLine();
        if(ImGui.SmallButton("Test"))
        {
            if(TryGetPriorityList(out var list))
            {
                DuoLog.Information($"Success: priority list {list.Print()}");
            }
            else
            {
                DuoLog.Warning($"Could not get priority list");
            }
        }
        var toRem = -1;
        for(var i = 0; i < Conf.LeftRightPriorities.Count; i++)
        {
            if(DrawPrioList(i))
            {
                toRem = i;
            }
        }
        if(toRem != -1)
        {
            Conf.LeftRightPriorities.RemoveAt(toRem);
        }
        if(ImGui.Button("Create new priority list"))
        {
            Conf.LeftRightPriorities.Add(new string[] { "", "", "", "", "", "", "", "" });
        }

        if(ImGuiEx.CollapsingHeader("Option"))
        {
            DragDrop.Begin();
            foreach(var job in Conf.Jobs)
            {
                DragDrop.NextRow();
                ImGui.Text(job.ToString());
                ImGui.SameLine();

                if(ThreadLoadImageHandler.TryGetIconTextureWrap((uint)job.GetIcon(), false, out var texture))
                {
                    ImGui.Image(texture.Handle, new Vector2(24f));
                    ImGui.SameLine();
                }

                ImGui.SameLine();
                DragDrop.DrawButtonDummy(job, Conf.Jobs, Conf.Jobs.IndexOf(job));
            }

            DragDrop.End();
        }

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Checkbox("DebugPrint", ref Conf.DebugPrint);
            ImGui.Text($"_mechanicActive : {_mechanicActive}");
            ImGui.Text($"_stackedList : {_stackedList.Print()}");
            ImGui.Text($"_sortedList : {_sortedList.Print()}");
            ImGui.Text($"Svc.ClientState.LocalPlayer.Name: {Svc.ClientState.LocalPlayer.Name}");
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(source.GetObject() is IBattleChara npc)
        {
            if(castId is 40151 or 40150)
            {
                if(!TryGetPriorityList(out _sortedList))
                    return;
                _mechanicActive = true;
            }
        }
    }

    public override void OnUpdate()
    {
        if(!_mechanicActive) return;
        if(Controller.GetElementByName("Stack1").Enabled)
        {
            Controller.GetElementByName("Stack1").color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }

        if(Controller.GetElementByName("Stack2").Enabled)
        {
            Controller.GetElementByName("Stack2").color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!_mechanicActive || set.Action == null)
            return;

        if(set.Action.Value.RowId == 40165)
        {
            OnReset();
            return;
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(!_mechanicActive) return;
        if(data2 != 0 || data3 != 249 || data5 != 15) return;

        try
        {
            DebugLog($"StackMarker: {target.GetObject().Name}");
            if(target.GetObject() is not IPlayerCharacter pcObj)
                return;

            _stackedList.Add(pcObj);
            if(_stackedList.Count == 1)
            {
                Controller.GetElementByName("Stack1").refActorObjectID = target;
            }
            else if(_stackedList.Count == 2)
            {
                Controller.GetElementByName("Stack2").refActorObjectID = target;
            }
            DebugLog($"_stackedList: {_stackedList.Print()}");
            if(_stackedList.Count == 2)
            {
                if(!_stackedList.Exists(x => x.Address == Svc.ClientState.LocalPlayer.Address))
                {
                    DebugLog("Non stacker");
                    // non stacker show element
                    var noneStackers = _sortedList.Where(x => !_stackedList.Contains(x)).ToList();
                    var myIndex = noneStackers.IndexOf(Svc.ClientState.LocalPlayer);
                    if(myIndex == -1)
                    {
                        DuoLog.Warning($"Could not find player in priority list");
                        _mechanicActive = false;
                        OnReset();
                        return;
                    }
                    var myNum = myIndex < 3 ? "1" : "2";

                    Controller.GetElementByName($"Stack{myNum}").Enabled = true;
                    Controller.GetElementByName($"Stack{myNum}").tether = true;

                    //Debug
                    var myStacker = _stackedList.First();
                    var otherStacker = _stackedList.Last();
                    myIndex = _sortedList.IndexOf(myStacker);
                    var otherIndex = _sortedList.IndexOf(otherStacker);
                    if(myIndex == -1 || otherIndex == -1)
                    {
                        DuoLog.Warning($"Could not find player in priority list");
                        _mechanicActive = false;
                        OnReset();
                        return;
                    }
                    myNum = myIndex < otherIndex ? "1" : "2";
                    var otherPos = myIndex < otherIndex ? "2" : "1";
                    DebugLog($"FirstStacker: {myStacker.Name} {myNum}, LastStacker: {otherStacker.Name} {otherPos}");

                    foreach(var x in noneStackers)
                    {
                        var dpos = noneStackers.IndexOf(x) < 3 ? "1" : "2";
                        DebugLog($"noneStacker: {x.Name} {dpos}");
                    }
                }
            }
        }
        catch(Exception e)
        {
            DuoLog.Error(e.Message);
        }
    }

    public override void OnReset()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        _stackedList.Clear();
        _sortedList.Clear();
        _mechanicActive = false;
    }

    private bool TryGetPriorityList([NotNullWhen(true)] out List<IPlayerCharacter> values)
    {
        foreach(var p in Conf.LeftRightPriorities)
        {
            var valid = true;
            var l = FakeParty.Get().Select(x => x.Name.ToString()).ToHashSet();
            foreach(var x in p)
            {
                if(!l.Remove(x))
                {
                    valid = false;
                    break;
                }
            }
            if(valid)
            {
                values = FakeParty.Get().ToList().OrderBy(x => Array.IndexOf(p, x.Name.ToString())).ToList();
                return true;
            }
        }
        values = [];
        return false;
    }

    private unsafe bool DrawPrioList(int num)
    {
        var prio = Conf.LeftRightPriorities[num];
        ImGuiEx.Text($"# Priority list {num + 1}");
        ImGui.PushID($"prio{num}");
        ImGuiEx.Text($"    NorthWest");
        for(var i = 0; i < prio.Length; i++)
        {
            ImGui.PushID($"prio{num}element{i}");
            ImGui.SetNextItemWidth(200);
            ImGui.InputText($"Player {i + 1}", ref prio[i], 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if(ImGui.BeginCombo("##partysel", "Select from party"))
            {
                foreach(var x in FakeParty.Get().Select(x => x.Name.ToString())
                             .Union(UniversalParty.Members.Select(x => x.Name)).ToHashSet())
                    if(ImGui.Selectable(x))
                        prio[i] = x;
                ImGui.EndCombo();
            }
            ImGui.PopID();
        }
        ImGuiEx.Text($"    NorthEast");
        if(ImGui.Button("Delete this list (ctrl+click)") && ImGui.GetIO().KeyCtrl)
        {
            return true;
        }

        ImGui.SameLine();
        if(ImGui.Button("Fill by job"))
        {
            HashSet<(string, Job)> party = [];
            foreach(var x in FakeParty.Get())
                party.Add((x.Name.ToString(), x.GetJob()));

            var proxy = InfoProxyCrossRealm.Instance();
            for(var i = 0; i < proxy->GroupCount; i++)
            {
                var group = proxy->CrossRealmGroups[i];
                for(var c = 0; c < proxy->CrossRealmGroups[i].GroupMemberCount; c++)
                {
                    var x = group.GroupMembers[c];
                    party.Add((x.Name.Read(), (Job)x.ClassJobId));
                }
            }

            var index = 0;
            foreach(var job in Conf.Jobs.Where(job => party.Any(x => x.Item2 == job)))
            {
                prio[index] = party.First(x => x.Item2 == job).Item1;
                index++;
            }

            for(var i = index; i < prio.Length; i++)
                prio[i] = "";
        }
        ImGuiEx.Tooltip("The list is populated based on the job.\nYou can adjust the priority from the option header.");

        ImGui.PopID();
        return false;
    }

    private void DebugLog(string log, [CallerLineNumber] int lineNum = 0)
    {
        if(Conf.DebugPrint)
            DuoLog.Information(log + $" : L({lineNum})");
    }
}
