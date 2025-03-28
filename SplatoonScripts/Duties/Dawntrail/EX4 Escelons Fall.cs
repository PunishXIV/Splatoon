using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ECommons.Logging;
using ECommons.CircularBuffers;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe class EX4_Escelons_Fall : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1271];

    public override Metadata? Metadata => new(1, "NightmareXIV");

    uint StatusCloseFar = 2970;
    uint StatusParamClose = 758;
    uint StatusParamFar = 759;
    uint[] CastSwitcher = [43182, 43181];
    uint CastStandard = 43181;
    uint NpcNameId = 13861;//Name NPC ID: 13861
    int NumSwitches = 0;
    long ForceResetAt = long.MaxValue;
    List<bool> SequenceIsClose = [];

    IBattleNpc? Zelenia => Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.NameId == this.NpcNameId && x.IsTargetable);

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Out", "{\"Name\":\"Out\",\"type\":1,\"Enabled\":false,\"radius\":6.0,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13861,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        Controller.RegisterElementFromCode("In", "{\"Name\":\"In\",\"type\":1,\"Enabled\":false,\"radius\":6.0,\"Donut\":20.0,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":13861,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");

        Controller.RegisterElementFromCode("InIncorrect", "{\"Name\":\"\",\"type\":1,\"radius\":1.0,\"Filled\":false,\"fillIntensity\":0.5,\"overlayTextColor\":4278190335,\"thicc\":5.0,\"overlayText\":\">>> IN <<<\",\"refActorType\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("InCorrect", "{\"Name\":\"\",\"type\":1,\"radius\":1.0,\"color\":3355508480,\"Filled\":false,\"fillIntensity\":0.5,\"overlayTextColor\":3355508480,\"thicc\":5.0,\"overlayText\":\"> IN <\",\"refActorType\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("OutIncorrect", "{\"Name\":\"\",\"type\":1,\"radius\":1.0,\"color\":3372155135,\"Filled\":false,\"fillIntensity\":0.5,\"overlayTextColor\":3372155135,\"thicc\":5.0,\"overlayText\":\"<<< OUT >>>\",\"refActorType\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("OutCorrect", "{\"Name\":\"\",\"type\":1,\"radius\":1.0,\"color\":3355508480,\"Filled\":false,\"fillIntensity\":0.5,\"overlayTextColor\":3355508480,\"thicc\":5.0,\"overlayText\":\"< OUT >\",\"refActorType\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"My bait if close first:");
        ImGuiEx.RadioButtonBool("First", "Second", ref C.TakeFirstIfClose);
        ImGuiEx.Text($"My bait if far first:");
        ImGuiEx.RadioButtonBool("First##2", "Second##2", ref C.TakeFirstIfFar);
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"SequenceIsClose: {SequenceIsClose.Print()}");
            ImGuiEx.Text($"{Svc.Objects.OfType<IPlayerCharacter>().OrderBy(x => Vector3.Distance(x.Position, Zelenia.Position)).Print("\n")}");
            ImGuiEx.Text($"GetMyCloses: {GetMyCloses().Print()}");
            ImGuiEx.Text($"IsSelfClose: {IsSelfClose()}");
        }
    }

    public override void OnGainBuffEffect(uint sourceId, Status status)
    {
        if(sourceId == Zelenia?.EntityId && status.StatusId == this.StatusCloseFar)
        {
            //ForceResetAt = Environment.TickCount64 + 30000;
            SequenceIsClose.Add(this.StatusParamClose == status.Param);
            PluginLog.Debug($"Registered: {(SequenceIsClose.Last() ? "Close" : "Far")}");
        }
    }

    List<bool> GetMyCloses()
    {
        var myCloseFirst = this.SequenceIsClose[0] ? C.TakeFirstIfClose : C.TakeFirstIfFar;
        List<bool> seq = [SequenceIsClose.SafeSelect(0) == myCloseFirst, SequenceIsClose.SafeSelect(1) != myCloseFirst, SequenceIsClose.SafeSelect(2) == myCloseFirst, SequenceIsClose.SafeSelect(3) != myCloseFirst];
        return seq;
    }

    bool IsSelfClose()
    {
        if(Zelenia == null) return false;
        return Svc.Objects.OfType<IPlayerCharacter>().OrderBy(x => Vector3.Distance(x.Position, Zelenia.Position)).Take(4).Any(x => x.AddressEquals(Player.Object));
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

        if(this.SequenceIsClose.Count >= 1)
        {
            var isMyClose = GetMyCloses()[this.NumSwitches];
            var correct = IsSelfClose() == isMyClose;
            Controller.GetElementByName(isMyClose ? $"In" : $"Out")!.Enabled = true;
            Controller.GetElementByName(isMyClose ? $"In{(correct?"Correct":"Incorrect")}" : $"Out{(correct ? "Correct" : "Incorrect")}")!.Enabled = true;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null) return;
        if(set.Action.Value.RowId.EqualsAny(this.CastSwitcher))
        {
            PluginLog.Information($"Switch");
            NumSwitches++;
        }
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Complete, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe)) Reset();
    }

    void Reset()
    {
        this.SequenceIsClose.Clear();
        NumSwitches = 0;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public bool TakeFirstIfClose = false;
        public bool TakeFirstIfFar = false;
    }
}
