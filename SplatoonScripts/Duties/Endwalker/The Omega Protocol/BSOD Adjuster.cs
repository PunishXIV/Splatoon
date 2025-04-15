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
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;
internal class BSOD_Adjuster : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => [1122];
    public override Metadata? Metadata => new(3, "Redmoon");

    public class CastID
    {
        public const uint StackMarker = 22393u;
        public const uint Ion = 31560u;
        public const uint StackCannon = 31615u;
        public const uint BSOD = 31611u;
    }

    private Config Conf => Controller.GetConfig<Config>();

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

    private const bool _debug = false;

    private List<IPlayerCharacter> _sortedList = [];
    private List<IPlayerCharacter> _stackedList = [];
    private bool _mechanicActive = false;
    private readonly ImGuiEx.RealtimeDragDrop<Job> DragDrop = new("DragDropJob", x => x.ToString());


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
                    ImGui.Image(texture.ImGuiHandle, new Vector2(24f));
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

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("StackLeft", "{\"Name\":\"\",\"refX\":95.74,\"refY\":112.62,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":5.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("StackRight", "{\"Name\":\"\",\"Enabled\":false,\"refX\":103.92,\"refY\":112.46,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":5.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(source.GetObject() is IBattleNpc npc)
        {
            if(castId == CastID.Ion)
            {
                if(!TryGetPriorityList(out _sortedList))
                    return;
                _mechanicActive = true;
            }
            else if(castId == CastID.BSOD)
            {
                OnReset();
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!_mechanicActive || set.Action == null)
            return;

        if(set.Action.Value.RowId == CastID.StackMarker)
        {
            try
            {
                DebugLog($"StackMarker: {set.Target.Name}");
                if(set.Target is not IPlayerCharacter pcObj)
                    return;

                _stackedList.Add(pcObj);
                DebugLog($"_stackedList: {_stackedList.Print()}");
                if(_stackedList.Count == 2)
                {
                    if(_stackedList.Exists(x => x.Address == Svc.ClientState.LocalPlayer.Address))
                    {
                        DebugLog("Stacker");
                        // stacker show element
                        var myStacker = _stackedList.Where(x => x.Address == Svc.ClientState.LocalPlayer.Address).First();
                        var otherStacker = _stackedList.Where(x => x.Address != Svc.ClientState.LocalPlayer.Address).First();
                        DebugLog($"myStacker: {myStacker.Name}, otherStacker: {otherStacker.Name}");
                        var myIndex = _sortedList.IndexOf(myStacker);
                        var otherIndex = _sortedList.IndexOf(otherStacker);
                        if(myIndex == -1 || otherIndex == -1)
                        {
                            DuoLog.Warning($"Could not find player in priority list");
                            _mechanicActive = false;
                            OnReset();
                            return;
                        }
                        var myPos = myIndex < otherIndex ? "Left" : "Right";
                        var otherPos = myIndex < otherIndex ? "Right" : "Left";

                        Controller.GetElementByName($"Stack{myPos}").Enabled = true;
                        Controller.GetElementByName($"Stack{myPos}").tether = true;

                        DebugLog($"myStacker: {myStacker.Name} {myPos}, otherStacker: {otherStacker.Name} {otherPos}");
                        var noneStackers = _sortedList.Where(x => !_stackedList.Contains(x)).ToList();
                        foreach(var x in noneStackers)
                        {
                            var dpos = noneStackers.IndexOf(x) < 3 ? "Left" : "Right";
                            DebugLog($"noneStacker: {x.Name} {dpos}");
                        }
                    }
                    else
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
                        var myPos = myIndex < 3 ? "Left" : "Right";

                        Controller.GetElementByName($"Stack{myPos}").Enabled = true;
                        Controller.GetElementByName($"Stack{myPos}").tether = true;

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
                        myPos = myIndex < otherIndex ? "Left" : "Right";
                        var otherPos = myIndex < otherIndex ? "Right" : "Left";
                        DebugLog($"FirstStacker: {myStacker.Name} {myPos}, LastStacker: {otherStacker.Name} {otherPos}");

                        foreach(var x in noneStackers)
                        {
                            var dpos = noneStackers.IndexOf(x) < 3 ? "Left" : "Right";
                            DebugLog($"noneStacker: {x.Name} {dpos}");
                        }
                    }
                }
            }
            catch(Exception e)
            {
                DuoLog.Error(e.Message);
            }
            return;
        }

        if(set.Action.Value.RowId == CastID.StackCannon)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            _stackedList.Clear();
            DebugLog("===============================");
            return;
        }
    }

    public override void OnReset()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        _stackedList.Clear();
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
