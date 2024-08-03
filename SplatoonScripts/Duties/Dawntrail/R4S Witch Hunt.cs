using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
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
    public override Metadata? Metadata => new(2, "NightmareXIV");

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

    IBattleNpc? WickedThunder => Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.NameId == 13057 && x.IsTargetable);

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("In", "{\"Name\":\"In\",\"type\":1,\"Enabled\":false,\"radius\":10.0,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        Controller.RegisterElementFromCode("Out", "{\"Name\":\"Out\",\"type\":1,\"Enabled\":false,\"radius\":10.0,\"Donut\":20.0,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");

        for(int i = 0; i < 4; i++)
        {
            Controller.RegisterElementFromCode($"Target{i}", "{\"Name\":\"Hunted\",\"type\":1,\"radius\":6.0,\"color\":3355508712,\"fillIntensity\":0.05,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"refActorComparisonType\":2,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        }

        Controller.RegisterElementFromCode("InClose", "{\"Name\":\"BaitersAreaInClose\",\"type\":1,\"Enabled\":false,\"radius\":7.0,\"color\":3355508490,\"fillIntensity\":0.4,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        Controller.RegisterElementFromCode("InFar", "{\"Name\":\"BaitersAreaInFar\",\"type\":1,\"Enabled\":false,\"radius\":9.0,\"Donut\":1.0,\"color\":3355508490,\"fillIntensity\":0.4,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        Controller.RegisterElementFromCode("OutClose", "{\"Name\":\"BaitersAreaOutClose\",\"type\":1,\"Enabled\":false,\"radius\":10.0,\"Donut\":1.0,\"color\":3355508490,\"fillIntensity\":0.4,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        Controller.RegisterElementFromCode("OutFar", "{\"Name\":\"BaitersAreaOutFar\",\"type\":1,\"Enabled\":false,\"radius\":12.0,\"Donut\":10.0,\"color\":3355508490,\"fillIntensity\":0.4,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13057,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");

        Controller.RegisterElementFromCode("Warning", "{\"Name\":\"\",\"type\":1,\"Enabled\":false,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278255360,\"overlayFScale\":2.0,\"overlayVOffset\":1.5,\"radius\":0,\"thicc\":0,\"overlayText\":\"GO GREEN\",\"refActorType\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");

        Controller.RegisterElementFromCode("Prepare", "{\"Name\":\"\",\"type\":1,\"Enabled\":false,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278255360,\"overlayFScale\":1.0,\"overlayVOffset\":1.5,\"radius\":0,\"thicc\":0,\"overlayText\":\"Prepare...\",\"refActorType\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(150f);
        ImGui.InputInt("Your turn to bait (close first or both if far first isn't set)", ref C.MyTurn.ValidateRange(1, 4));

        ImGuiEx.InputInt(150f, "Your turn to bait (far first)", ref C.MyTurnFarFirst);

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
        }
        var myTurn = IsInitialClose == false ? C.MyTurn : (C.MyTurnFarFirst ?? C.MyTurn);
        if(IsUnsafeMiddle != null)
        {
            Controller.GetElementByName(IsUnsafeMiddle.Value ? "In" : "Out")!.Enabled = true;
            if(IsInitialClose != null)
            {
                var baitsClose = NumSwitches % 2 != 0 ? IsInitialClose.Value : !IsInitialClose.Value;
                
                var baiterZone = Controller.GetElementByName(!IsUnsafeMiddle.Value?(baitsClose?"InClose":"InFar"):(baitsClose?"OutClose":"OutFar"))!;
                baiterZone.Enabled = true;


                if(myTurn - 1 == this.NumSwitches)
                {
                    var players = Svc.Objects.OfType<IPlayerCharacter>().OrderBy(x => Vector3.Distance(x.Position, this.WickedThunder!.Position)).ToList();
                    if(!baitsClose) players.Reverse();
                    for(int i = 0; i < 2; i++)
                    {
                        var e = Controller.GetElementByName($"Target{i}");
                        e.Enabled = true;
                        e.refActorObjectID = players[i].EntityId;
                    }
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
        public int MyTurn = 1;
        public int? MyTurnFarFirst = null;
    }
}
