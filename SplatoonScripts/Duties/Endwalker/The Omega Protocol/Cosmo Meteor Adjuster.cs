using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;
internal unsafe class Cosmo_Meteor_Adjuster :SplatoonScript
{
    #region PublicDef
    // TODO: add common enum from bossMod
    public enum Class :byte
    {
        None = 0,
        GLA = 1,
        PGL = 2,
        MRD = 3,
        LNC = 4,
        ARC = 5,
        CNJ = 6,
        THM = 7,
        CRP = 8,
        BSM = 9,
        ARM = 10,
        GSM = 11,
        LTW = 12,
        WVR = 13,
        ALC = 14,
        CUL = 15,
        MIN = 16,
        BTN = 17,
        FSH = 18,
        PLD = 19,
        MNK = 20,
        WAR = 21,
        DRG = 22,
        BRD = 23,
        WHM = 24,
        BLM = 25,
        ACN = 26,
        SMN = 27,
        SCH = 28,
        ROG = 29,
        NIN = 30,
        MCH = 31,
        DRK = 32,
        AST = 33,
        SAM = 34,
        RDM = 35,
        BLU = 36,
        GNB = 37,
        DNC = 38,
        RPR = 39,
        SGE = 40,
        VPR = 41,
        PCT = 42,
    }
    public override HashSet<uint> ValidTerritories => new() { 1122 };
    public override Metadata? Metadata => new(1, "Redmoon");
    #endregion

    #region PrivateDef
    private class FlareContainer
    {
        public ICharacter character;
        public bool mine = false;
        public float NorthDistance = 0;
        public float EastDistance = 0;
        public float WestDistance = 0;
        public float SouthDistance = 0;

        public FlareContainer(ICharacter character, bool mine)
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
        Controller.RegisterElementFromCode("FlareNorth", "{\"Name\":\"\",\"refX\":100.0,\"refY\":80.36,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":5.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FlareEast", "{\"Name\":\"\",\"refX\":119.46,\"refY\":99.96,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":5.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FlareWest", "{\"Name\":\"\",\"refX\":80.56,\"refY\":99.96,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":5.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FlareSouth", "{\"Name\":\"\",\"refX\":100.0,\"refY\":119.56,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":5.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        Controller.RegisterElementFromCode("StackNorth", "{\"Name\":\"\",\"refX\":100.0,\"refY\":85.32,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":5.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("StackSouth", "{\"Name\":\"\",\"refX\":100.0,\"refY\":115.1,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":5.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (target.GetObject() is IPlayerCharacter character && _gimmickActive)
        {
            if (vfxPath == VfxPath.Flare)
            {
                _flarePos.Add(new FlareContainer(character, false));
                if (character.Address == Svc.ClientState.LocalPlayer.Address)
                {
                    _flarePos.Last().mine = true;
                    _isFlareMine = true;
                }

                if ((GetClassJobByEntityId(character.EntityId) == Class.BRD) ||
                    (GetClassJobByEntityId(character.EntityId) == Class.MCH) ||
                    (GetClassJobByEntityId(character.EntityId) == Class.DNC))
                {
                    _stackPos = StackPos.South;
                }
                else if (_flarePos.Count >= 3)
                {
                    _stackPos = StackPos.North;
                }

                if (_flarePos.Count >= 3)
                {
                    Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
                    ArbitPosition();

                    if (!_isFlareMine)
                    {
                        if (_stackPos == StackPos.North)
                        {
                            Controller.GetElementByName("StackNorth").Enabled = true;
                            Controller.GetElementByName("StackNorth").tether = true;
                        }
                        else if (_stackPos == StackPos.South)
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
        if (castId == CastID.CosmoMeteor)
        {
            _gimmickActive = true;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action == null)
            return;

        if (set.Action.RowId == CastID.CosmoMeteorFlare)
        {
            this.OnReset();
        }
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
    }

    public override void OnSettingsDraw()
    {
        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text("Stack Position: " + _stackPos.ToString());
            ImGui.Text("Gimmick Active: " + _gimmickActive.ToString());
            ImGui.Text("Flare Mine: " + _isFlareMine.ToString());

            foreach ((string, string) data in _flareData)
            {
                if (data.Item1 == null || data.Item2 == null)
                    continue;
                ImGui.Text(data.Item1 + ": " + data.Item2);
            }

            List<ImGuiEx.EzTableEntry> entries = [];
            foreach (FlareContainer character in _flarePos)
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
    private Class GetClassJobByEntityId(uint EntityId)
    {
        GameObject* myPcObj = _gom->Objects.GetObjectByEntityId(EntityId);
        Character* myPc = (Character*)myPcObj;
        return (Class)myPc->ClassJob;
    }

    private void ArbitPosition()
    {
        CalcDistance();

        if (_stackPos == StackPos.North)
        {
            // East
            FlareContainer EastPc = _flarePos.OrderBy(x => x.EastDistance).First();
            List<FlareContainer> temp = _flarePos.Where(x => x.character.Address != EastPc.character.Address).ToList();

            if (EastPc.mine)
            {
                Controller.GetElementByName("StackEast").tether = true;
                Controller.GetElementByName("StackEast").Enabled = true;
            }

            // South
            FlareContainer SouthPc = temp.OrderBy(x => x.SouthDistance).First();
            temp = temp.Where(x => x.character.Address != SouthPc.character.Address).ToList();

            if (SouthPc.mine)
            {
                Controller.GetElementByName("StackSouth").tether = true;
                Controller.GetElementByName("StackSouth").Enabled = true;
            }

            // West
            FlareContainer WestPc = temp.OrderBy(x => x.WestDistance).First();

            if (WestPc.mine)
            {
                Controller.GetElementByName("StackWest").tether = true;
                Controller.GetElementByName("StackWest").Enabled = true;
            }

            // Write debug data
            _flareData[0] = ("East", EastPc.character.Name.ToString());
            _flareData[1] = ("South", SouthPc.character.Name.ToString());
            _flareData[2] = ("West", WestPc.character.Name.ToString());
        }
        else if (_stackPos == StackPos.South)
        {
            // East
            FlareContainer EastPc = _flarePos.OrderBy(x => x.EastDistance).First();
            List<FlareContainer> temp = _flarePos.Where(x => x.character.Address != EastPc.character.Address).ToList();

            if (EastPc.mine)
            {
                Controller.GetElementByName("StackNorth").tether = true;
                Controller.GetElementByName("StackNorth").Enabled = true;
            }

            // North
            FlareContainer NorthPc = temp.OrderBy(x => x.NorthDistance).First();
            temp = temp.Where(x => x.character.Address != NorthPc.character.Address).ToList();

            if (NorthPc.mine)
            {
                Controller.GetElementByName("StackEast").tether = true;
                Controller.GetElementByName("StackEast").Enabled = true;
            }

            // West
            FlareContainer WestPc = temp.OrderBy(x => x.WestDistance).First();

            if (WestPc.mine)
            {
                Controller.GetElementByName("StackWest").tether = true;
                Controller.GetElementByName("StackWest").Enabled = true;
            }

            // Write debug data
            _flareData[0] = ("North", NorthPc.character.Name.ToString());
            _flareData[1] = ("East", EastPc.character.Name.ToString());
            _flareData[2] = ("West", WestPc.character.Name.ToString());
        }
    }

    private void CalcDistance()
    {
        foreach (FlareContainer character in _flarePos)
        {
            PluginLog.Information("Character: " + character.character.Name.ToString());
            Element element = Controller.GetElementByName("FlareEast");
            Vector3 elementPos = elementPos = new Vector3(element.refX, 0, element.refY);
            PluginLog.Information("East ElementPos: " + elementPos.ToString());
            PluginLog.Information("East CharacterPos: " + character.character.Position.ToString());
            character.EastDistance = Vector3.Distance(character.character.Position, elementPos);

            element = Controller.GetElementByName("FlareWest");
            elementPos = elementPos = new Vector3(element.refX, 0, element.refY);
            PluginLog.Information("West ElementPos: " + elementPos.ToString());
            PluginLog.Information("West CharacterPos: " + character.character.Position.ToString());
            character.WestDistance = Vector3.Distance(character.character.Position, elementPos);

            element = Controller.GetElementByName("FlareNorth");
            elementPos = elementPos = new Vector3(element.refX, 0, element.refY);
            PluginLog.Information("North ElementPos: " + elementPos.ToString());
            PluginLog.Information("North CharacterPos: " + character.character.Position.ToString());
            character.NorthDistance = Vector3.Distance(character.character.Position, elementPos);

            element = Controller.GetElementByName("FlareSouth");
            elementPos = elementPos = new Vector3(element.refX, 0, element.refY);
            PluginLog.Information("South ElementPos: " + elementPos.ToString());
            PluginLog.Information("South CharacterPos: " + character.character.Position.ToString());
            character.SouthDistance = Vector3.Distance(character.character.Position, elementPos);
        }
    }
    #endregion
}
