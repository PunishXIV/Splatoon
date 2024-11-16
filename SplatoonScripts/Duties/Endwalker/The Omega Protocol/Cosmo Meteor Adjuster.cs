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
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;
internal unsafe class Cosmo_Meteor_Adjuster :SplatoonScript
{
    #region PublicDef
    public override HashSet<uint> ValidTerritories => new() { 1122 };
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
    private List<FlareContainer> _flarePos = new List<FlareContainer>();
    private bool _gimmickActive = false;
    private bool _isFlareMine = false;
    private GameObjectManager* _gom = GameObjectManager.Instance();
    private (string, string)[] _flareData = new (string, string)[3];
    private Config C => Controller.GetConfig<Config>();
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
                    _stackPos = StackPos.South;
                }
                else if(_flarePos.Count >= 3)
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

        if(set.Action.Value.RowId == CastID.CosmoMeteorFlare)
        {
            this.OnReset();
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
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public class Config :IEzConfig
    {
        public bool Debug = false;
        public List<string[]> clockWisePriorities = new();
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
        for(int i = 0; i < C.clockWisePriorities.Count; i++)
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

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text("Stack Position: " + _stackPos.ToString());
            ImGui.Text("Gimmick Active: " + _gimmickActive.ToString());
            ImGui.Text("Flare Mine: " + _isFlareMine.ToString());

            foreach((string, string) data in _flareData)
            {
                if(data.Item1 == null || data.Item2 == null) continue;
                ImGui.Text(data.Item1 + ": " + data.Item2);
            }

            List<ImGuiEx.EzTableEntry> entries = [];
            foreach(FlareContainer character in _flarePos)
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
        values = new();
        return false;
    }

    private bool DrawPrioList(int num)
    {
        var prio = C.clockWisePriorities[num];
        ImGuiEx.Text($"# Priority list {num + 1}");
        ImGui.PushID($"prio{num}");
        ImGuiEx.Text($"    North (Ranged DPS) ClockWise");
        for(int i = 0; i < prio.Length; i++)
        {
            ImGui.PushID($"prio{num}element{i}");
            ImGui.SetNextItemWidth(200);
            ImGui.InputText($"Player {i + 1}", ref prio[i], 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if(ImGui.BeginCombo("##partysel", "Select from party"))
            {
                foreach(var x in FakeParty.Get())
                {
                    if(ImGui.Selectable(x.Name.ToString()))
                    {
                        prio[i] = x.Name.ToString();
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.PopID();
        }
        ImGuiEx.Text($"    NorthWest");
        if(ImGui.Button("Delete this list (ctrl+click)") && ImGui.GetIO().KeyCtrl)
        {
            return true;
        }
        ImGui.PopID();
        return false;
    }

    private void ArbitPosition()
    {
        string[] northElementsArray = { "FlareEast", "FlareSouth", "FlareWest" };
        string[] southElementsArray = { "FlareNorth", "FlareEast", "FlareWest" };

        List<IPlayerCharacter> priorityList = new List<IPlayerCharacter>();

        if(!TryGetPriorityList(out priorityList)) return;

        if(_stackPos == StackPos.North)
        {
            int i = 0;
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
            int i = 0;
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
