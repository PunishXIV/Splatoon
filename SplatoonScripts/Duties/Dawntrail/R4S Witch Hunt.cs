using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Throttlers;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class R4S_Witch_Hunt : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1232];
    public override Metadata? Metadata => new(4, "NightmareXIV");

    uint CastNarrowing = 38369;
    uint CastWidening = 38368;
    uint StatusCloseFar = 2970;
    uint StatusParamClose = 758;
    uint StatusParamFar = 759;
    uint[] CastSwitcher = [19730, 19729];
    bool? IsUnsafeMiddle = null;
    bool? IsInitialClose = null;
    int NumSwitches = 0;
    long ForceResetAt = long.MaxValue;
    uint CastStandard = 38366;

    IBattleNpc? WickedThunder => Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.NameId == 13057 && x.IsTargetable);

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("In", "{\"Name\":\"In\",\"type\":1,\"Enabled\":false,\"radius\":10.0,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        Controller.RegisterElementFromCode("Out", "{\"Name\":\"Out\",\"type\":1,\"Enabled\":false,\"radius\":10.0,\"Donut\":20.0,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");

        for(int i = 0; i < 4; i++)
        {
            Controller.RegisterElementFromCode($"Target{i}", "{\"Name\":\"Hunted\",\"type\":1,\"radius\":6.0,\"color\":3355508712,\"fillIntensity\":0.05,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":3.0,\"refActorComparisonType\":2,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        }

        Controller.RegisterElementFromCode("InClose", "{\"Name\":\"BaitersAreaInClose\",\"type\":1,\"Enabled\":false,\"radius\":7.0,\"color\":3355508490,\"fillIntensity\":0.4,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        Controller.RegisterElementFromCode("InFar", "{\"Name\":\"BaitersAreaInFar\",\"type\":1,\"Enabled\":false,\"radius\":9.0,\"Donut\":1.0,\"color\":3355508490,\"fillIntensity\":0.4,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        Controller.RegisterElementFromCode("OutClose", "{\"Name\":\"BaitersAreaOutClose\",\"type\":1,\"Enabled\":false,\"radius\":10.0,\"Donut\":1.0,\"color\":3355508490,\"fillIntensity\":0.4,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        Controller.RegisterElementFromCode("OutFar", "{\"Name\":\"BaitersAreaOutFar\",\"type\":1,\"Enabled\":false,\"radius\":12.0,\"Donut\":10.0,\"color\":3355508490,\"fillIntensity\":0.4,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");

        Controller.RegisterElementFromCode("Warning", "{\"Name\":\"\",\"type\":1,\"Enabled\":false,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278255360,\"overlayFScale\":2.0,\"overlayVOffset\":1.5,\"radius\":0,\"thicc\":0,\"overlayText\":\"GO GREEN\",\"refActorType\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");

        Controller.RegisterElementFromCode("Prepare", "{\"Name\":\"\",\"type\":1,\"Enabled\":false,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278255360,\"overlayFScale\":1.0,\"overlayVOffset\":1.5,\"radius\":0,\"thicc\":0,\"overlayText\":\"Prepare...\",\"refActorType\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");

        Controller.RegisterElementFromCode("NormalMid", "{\"Name\":\"\",\"type\":2,\"refX\":120.0,\"refY\":100.0,\"offX\":80.0,\"offY\":100.0,\"radius\":12.0,\"color\":3355508223,\"fillIntensity\":0.2,\"originFillColor\":1157628159,\"endFillColor\":1157628159,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("NormalSide1", "{\"Name\":\"\",\"type\":2,\"refX\":120.0,\"refY\":85.0,\"offX\":80.0,\"offY\":85.0,\"radius\":7.0,\"color\":3355508223,\"fillIntensity\":0.2,\"originFillColor\":1157628159,\"endFillColor\":1157628159,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("NormalSide2", "{\"Name\":\"\",\"type\":2,\"refX\":120.0,\"refY\":115.0,\"offX\":80.0,\"offY\":115.0,\"radius\":7.0,\"color\":3355508223,\"fillIntensity\":0.2,\"originFillColor\":1157628159,\"endFillColor\":1157628159,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        Controller.RegisterElementFromCode("IdlersAreaOutClose", "{\"Name\":\"IdlersAreaOutClose\",\"type\":1,\"Enabled\":false,\"radius\":12.0,\"Donut\":1.0,\"color\":3372154884,\"fillIntensity\":0.4,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("IdlersAreaOutFar", "{\"Name\":\"IdlersAreaOutFar\",\"type\":1,\"Enabled\":false,\"radius\":10.0,\"Donut\":1.0,\"color\":3372154884,\"fillIntensity\":0.4,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("IdlersAreaInFar", "{\"Name\":\"IdlersAreaInFar\",\"type\":1,\"Enabled\":false,\"radius\":7.0,\"Donut\":1.0,\"color\":3372158464,\"fillIntensity\":0.4,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("IdlersAreaInClose", "{\"Name\":\"IdlersAreaInClose\",\"type\":1,\"Enabled\":false,\"radius\":9.0,\"Donut\":1.0,\"color\":3372158464,\"fillIntensity\":0.4,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Resolve normal witch hunt", ref C.Normal);
        ImGui.Separator();
        ImGui.Checkbox("Always show baits", ref C.Uncond);

        ImGui.SetNextItemWidth(150f);
        ImGui.InputInt("Your turn to bait (close first or both if far first isn't set)", ref C.MyTurn.ValidateRange(0, 4));

        ImGuiEx.InputInt(150f, "Your turn to bait (far first)", ref C.MyTurnFarFirst);

        ImGui.Checkbox("Show standby zone", ref C.ShowIdle);

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"Wicked thunder: {WickedThunder} / {WickedThunder?.CastActionId}");
            ImGuiEx.Checkbox("IsUnsafeMiddle", ref IsUnsafeMiddle);
            ImGuiEx.Checkbox("IsInitialClose", ref IsInitialClose);
        }
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(Environment.TickCount64 > ForceResetAt || NumSwitches >= 4)
        {
            ForceResetAt = long.MaxValue;
            Reset();
            return;
        }
        var w = WickedThunder;
        if(w != null)
        {
            if(w.IsCasting && w.CurrentCastTime < 2f && w.CastActionId.EqualsAny(this.CastWidening, this.CastNarrowing))
            {
                this.IsUnsafeMiddle = w.CastActionId != this.CastNarrowing;
                NumSwitches = 0;
                if(w.StatusList.Any(x => x.StatusId == this.StatusCloseFar && x.Param == this.StatusParamClose))
                {
                    IsInitialClose = false;
                }
                if(w.StatusList.Any(x => x.StatusId == this.StatusCloseFar && x.Param == this.StatusParamFar))
                {
                    IsInitialClose = true;
                }
                ForceResetAt = Environment.TickCount64 + 30 * 1000;
            }
            if(C.Normal && w.IsCasting && w.CastActionId.EqualsAny(this.CastStandard))
            {
                var players = Svc.Objects.OfType<IPlayerCharacter>().OrderBy(x => Vector3.Distance(x.Position, this.WickedThunder!.Position)).ToList();
                if(w.StatusList.Any(x => x.StatusId == this.StatusCloseFar && x.Param == this.StatusParamClose))
                {
                    if(Player.Object.StatusList.Any(x => x.StatusId == 587))
                    {
                        Controller.GetElementByName("NormalMid")!.Enabled = true;
                    }
                    else
                    {
                        Controller.GetElementByName("NormalSide1")!.Enabled = true;
                        Controller.GetElementByName("NormalSide2")!.Enabled = true;
                    }
                }
                else
                {
                    players.Reverse();
                    if(Player.Object.StatusList.Any(x => x.StatusId == 587))
                    {
                        
                        Controller.GetElementByName("NormalSide1")!.Enabled = true;
                        Controller.GetElementByName("NormalSide2")!.Enabled = true;
                    }
                    else
                    {
                        Controller.GetElementByName("NormalMid")!.Enabled = true;
                    }
                }
                for(int i = 0; i < 4; i++)
                {
                    var e = Controller.GetElementByName($"Target{i}");
                    e.Enabled = true;
                    e.refActorObjectID = players[i].EntityId;
                }
            }
        }
        var myTurn = IsInitialClose == false ? C.MyTurn : (C.MyTurnFarFirst ?? C.MyTurn);
        if(IsUnsafeMiddle != null)
        {
            Controller.GetElementByName(IsUnsafeMiddle.Value ? "In" : "Out")!.Enabled = true;
            if(IsInitialClose != null)
            {
                var baitsClose = NumSwitches % 2 != 0 ? IsInitialClose.Value : !IsInitialClose.Value;

                var baiterZone = Controller.GetElementByName(!IsUnsafeMiddle.Value ? (baitsClose ? "InClose" : "InFar") : (baitsClose ? "OutClose" : "OutFar"))!;
                var idlerZone = Controller.GetElementByName("IdlersArea" + (!IsUnsafeMiddle.Value ? (baitsClose ? "InClose" : "InFar") : (baitsClose ? "OutClose" : "OutFar")));
                if(idlerZone != null && C.ShowIdle && myTurn - 1 != this.NumSwitches)
                {
                    idlerZone.Enabled = true;
                }
                baiterZone.Enabled = true;


                if(C.Uncond || myTurn - 1 == this.NumSwitches)
                {
                    var players = Svc.Objects.OfType<IPlayerCharacter>().OrderBy(x => Vector3.Distance(x.Position, this.WickedThunder!.Position)).ToList();
                    if(!baitsClose) players.Reverse();
                    for(int i = 0; i < 2; i++)
                    {
                        var e = Controller.GetElementByName($"Target{i}")!;
                        e.Enabled = true;
                        e.refActorObjectID = players[i].EntityId;
                    }
                }

                if(myTurn - 1 == this.NumSwitches)
                {
                    var warning = Controller.GetElementByName("Warning")!;
                    warning.Enabled = true;
                    warning.color = GradientColor.Get(EColor.GreenBright, EColor.White, 500).ToUint();
                    baiterZone.color = ImGuiEx.Vector4FromRGBA(0x00FF00C8).ToUint();
                }
                else
                {
                    baiterZone.color = ImGuiEx.Vector4FromRGBA(0xFFFF00C8).ToUint();
                }
                if(myTurn - 1 == this.NumSwitches + 1)
                {
                    Controller.GetElementByName("Prepare")!.Enabled = true;
                }
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(IsUnsafeMiddle == null || set.Action == null) return;
        if(set.Action.RowId.EqualsAny(this.CastSwitcher))
        {
            IsUnsafeMiddle = !IsUnsafeMiddle.Value;
            NumSwitches++;
        }
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Complete, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe)) Reset();
    }

    void Reset()
    {
        IsUnsafeMiddle = null;
        NumSwitches = 0;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public bool Uncond = false;
        public bool Normal = true;
        public int MyTurn = 1;
        public int? MyTurnFarFirst = null;
        public bool ShowIdle = false;
    }
}
