using Dalamud.Game.ClientState.Objects.SubKinds;
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
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;
internal unsafe class Cosmo_Meteor_Adjuster : SplatoonScript
{
    #region PublicDef
    public override HashSet<uint> ValidTerritories => [1122];
    public override Metadata? Metadata => new(2, "Redmoon");
    #endregion

    #region PrivateDef
    private class FlareContainer
    {
        public IPlayerCharacter character;
        public bool mine = false;
        public float NorthDistance = 0;
        public float EastDistance = 0;
        public float WestDistance = 0;
        public float SouthDistance = 0;

        public FlareContainer(IPlayerCharacter character, bool mine)
        {
            this.character = character;
            this.mine = mine;
        }
    }

    private class CastID
    {
        public const uint CosmoMeteor = 31664;
        public const uint CosmoMeteorFlare = 31668;
    }

    private class VfxPath
    {
        public const string Flare = "vfx/lockon/eff/all_at8s_0v.avfx";
    }

    private enum StackPos
    {
        Undefined = 0,
        North = 1,
        South = 2
    }
    private readonly Job[] RangedDps = { Job.BRD, Job.MCH, Job.DNC };
    private StackPos _stackPos = StackPos.Undefined;
    private List<FlareContainer> _flarePos = [];
    private bool _gimmickActive = false;
    private bool _isFlareMine = false;
    private bool _isFindRange = false;
    private GameObjectManager* _gom = GameObjectManager.Instance();
    private (string, string)[] _flareData = new (string, string)[3];
    private Config C => Controller.GetConfig<Config>();
    private readonly ImGuiEx.RealtimeDragDrop<Job> DragDrop = new("DragDropJob", x => x.ToString());
    #endregion

    #region public
    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("FlareNorth", "{\"Name\":\"\",\"refX\":100.0,\"refY\":80.36,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FlareEast", "{\"Name\":\"\",\"refX\":119.46,\"refY\":99.96,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FlareWest", "{\"Name\":\"\",\"refX\":80.56,\"refY\":99.96,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FlareSouth", "{\"Name\":\"\",\"refX\":100.0,\"refY\":119.56,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        Controller.RegisterElementFromCode("StackNorth", "{\"Name\":\"\",\"refX\":100.0,\"refY\":85.32,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("StackSouth", "{\"Name\":\"\",\"refX\":100.0,\"refY\":115.1,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(target.GetObject() is IPlayerCharacter character && _gimmickActive)
        {
            if(vfxPath == VfxPath.Flare)
            {
                _flarePos.Add(new FlareContainer(character, false));
                if(character.Address == Svc.ClientState.LocalPlayer.Address)
                {
                    _flarePos.Last().mine = true;
                    _isFlareMine = true;
                }

                if(RangedDps.Contains(character.GetJob()))
                {
                    _isFindRange = true;
                    _stackPos = StackPos.South;
                }
                else if(_flarePos.Count >= 3 && _isFindRange == false)
                {
                    _stackPos = StackPos.North;
                }

                if(_flarePos.Count >= 3)
                {
                    Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
                    ArbitPosition();

                    if(!_isFlareMine)
                    {
                        if(_stackPos == StackPos.North)
                        {
                            Controller.GetElementByName("StackNorth").Enabled = true;
                            Controller.GetElementByName("StackNorth").tether = true;
                        }
                        else if(_stackPos == StackPos.South)
                        {
                            Controller.GetElementByName("StackSouth").Enabled = true;
                            Controller.GetElementByName("StackSouth").tether = true;
                        }
                    }
                }
            }
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == CastID.CosmoMeteor)
        {
            _gimmickActive = true;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null)
            return;

        if(set.Action.Value.RowId == 40165)
        {
            OnReset();
        }
    }

    public override void OnUpdate()
    {
        if(Controller.GetElementByName("FlareNorth").Enabled == true)
            Controller.GetElementByName("FlareNorth").color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        if(Controller.GetElementByName("FlareEast").Enabled == true)
            Controller.GetElementByName("FlareEast").color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        if(Controller.GetElementByName("FlareWest").Enabled == true)
            Controller.GetElementByName("FlareWest").color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        if(Controller.GetElementByName("FlareSouth").Enabled == true)
            Controller.GetElementByName("FlareSouth").color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        if(Controller.GetElementByName("StackNorth").Enabled == true)
            Controller.GetElementByName("StackNorth").color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        if(Controller.GetElementByName("StackSouth").Enabled == true)
            Controller.GetElementByName("StackSouth").color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
    }

    public override void OnReset()
    {
        _flarePos.Clear();
        _stackPos = StackPos.Undefined;
        _isFlareMine = false;
        _gimmickActive = false;
        _flareData = new (string, string)[3];
        _isFindRange = false;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public class Config : IEzConfig
    {
        public bool Debug = false;
        public List<string[]> clockWisePriorities = [];

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
        for(var i = 0; i < C.clockWisePriorities.Count; i++)
        {
            if(DrawPrioList(i))
            {
                toRem = i;
            }
        }
        if(toRem != -1)
        {
            C.clockWisePriorities.RemoveAt(toRem);
        }
        if(ImGui.Button("Create new priority list"))
        {
            C.clockWisePriorities.Add(new string[] { "", "", "", "", "", "", "", "" });
        }

        if(ImGuiEx.CollapsingHeader("Option"))
        {
            DragDrop.Begin();
            foreach(var job in C.Jobs)
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
                DragDrop.DrawButtonDummy(job, C.Jobs, C.Jobs.IndexOf(job));
            }

            DragDrop.End();
        }

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text("Stack Position: " + _stackPos.ToString());
            ImGui.Text("Gimmick Active: " + _gimmickActive.ToString());
            ImGui.Text("Flare Mine: " + _isFlareMine.ToString());

            foreach(var data in _flareData)
            {
                if(data.Item1 == null || data.Item2 == null) continue;
                ImGui.Text(data.Item1 + ": " + data.Item2);
            }

            List<ImGuiEx.EzTableEntry> entries = [];
            foreach(var character in _flarePos)
            {
                entries.Add(new ImGuiEx.EzTableEntry("Name", () => ImGui.Text(character.character.Name.ToString())));
                entries.Add(new ImGuiEx.EzTableEntry("Mine", () => ImGui.Text(character.mine.ToString())));
                entries.Add(new ImGuiEx.EzTableEntry("East", () => ImGui.Text(character.EastDistance.ToString())));
                entries.Add(new ImGuiEx.EzTableEntry("West", () => ImGui.Text(character.WestDistance.ToString())));
                entries.Add(new ImGuiEx.EzTableEntry("North", () => ImGui.Text(character.NorthDistance.ToString())));
                entries.Add(new ImGuiEx.EzTableEntry("South", () => ImGui.Text(character.SouthDistance.ToString())));
            }
            ImGuiEx.EzTable(entries);
        }
    }
    #endregion

    #region private
    private bool TryGetPriorityList([NotNullWhen(true)] out List<IPlayerCharacter> values)
    {
        foreach(var p in C.clockWisePriorities)
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

    private bool DrawPrioList(int num)
    {
        var prio = C.clockWisePriorities[num];
        ImGuiEx.Text($"# Priority list {num + 1}");
        ImGui.PushID($"prio{num}");
        ImGuiEx.Text($"    North (Ranged DPS) ClockWise");
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
        ImGuiEx.Text($"    NorthWest");
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
            foreach(var job in C.Jobs.Where(job => party.Any(x => x.Item2 == job)))
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

    private void ArbitPosition()
    {
        string[] northElementsArray = { "FlareEast", "FlareSouth", "FlareWest" };
        string[] southElementsArray = { "FlareNorth", "FlareEast", "FlareWest" };

        List<IPlayerCharacter> priorityList = [];

        if(!TryGetPriorityList(out priorityList)) return;

        if(_stackPos == StackPos.North)
        {
            var i = 0;
            foreach(var priorityMember in priorityList)
            {
                if(_flarePos.Any(x => x.character.Address == priorityMember.Address))
                {
                    _flareData[i] = (northElementsArray[i], priorityMember.Name.ToString());
                    if(Svc.ClientState.LocalPlayer.Address == priorityMember.Address)
                    {
                        Controller.GetElementByName(northElementsArray[i]).Enabled = true;
                        Controller.GetElementByName(northElementsArray[i]).tether = true;
                    }
                    i++;
                }
            }
        }
        else if(_stackPos == StackPos.South)
        {
            var i = 0;
            foreach(var priorityMember in priorityList)
            {
                if(_flarePos.Any(x => x.character.Address == priorityMember.Address))
                {
                    _flareData[i] = (southElementsArray[i], priorityMember.Name.ToString());
                    if(Svc.ClientState.LocalPlayer.Address == priorityMember.Address)
                    {
                        Controller.GetElementByName(southElementsArray[i]).Enabled = true;
                        Controller.GetElementByName(southElementsArray[i]).tether = true;
                    }
                    i++;
                }
            }
        }
    }
    #endregion
}
