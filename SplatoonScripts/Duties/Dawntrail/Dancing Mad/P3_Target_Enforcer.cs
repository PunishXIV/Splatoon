using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;

using ECommons.DalamudServices.Legacy;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;
public unsafe class P3_Target_Enforcer : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1363];
    public override Metadata Metadata => new(2, "Poneglyph, NightmareXIV");

    private static class Buffs
    {
        public const uint FatedHero = 4194;
        public const uint EpicHero = 4192;
    }

    private static class Enemies
    {
        public const uint Exdeath = 6052;
        public const uint Chaos = 7691;
    }

    public uint DebuffHeadwindChaos = 1602;
    public uint DebuffTailwindExdeath = 1603;

    private bool Throttle() => EzThrottler.Throttle($"{InternalData.FullName}_SetTarget", 250);

    public override void OnUpdate()
    {
        if(BasePlayer == null || BasePlayer.IsDead) return;
        if(!GenericHelpers.IsScreenReady()) return;
        if(Svc.Targets.SoftTarget != null)
        {
            FrameThrottler.Throttle("SoftTargetThrottle", 10);
        }
        if(!FrameThrottler.Check("SoftTargetThrottle")) return;
        if(C.NoSwitchOffPlayers && Svc.Targets.Target is IPlayerCharacter)
        {
            return;
        }

        var player = BasePlayer;
        if(player == null) return;

        var hasFatedHero = player.StatusList.Any(x => x.StatusId == Buffs.FatedHero);
        var hasEpicHero = player.StatusList.Any(x => x.StatusId == Buffs.EpicHero);

        uint requiredNameId = 0;
        if(!EzThrottler.Check("ForceExdeath")) requiredNameId = Enemies.Exdeath;
        if(!EzThrottler.Check("ForceChaos")) requiredNameId = Enemies.Chaos;
        if(hasFatedHero)
        {
            requiredNameId = Enemies.Exdeath;
        }
        else if(hasEpicHero)
        {
            requiredNameId = Enemies.Chaos;
        }
        else
        {
            if(C.PreKnockbackEnforce != null && BasePlayer.HasStatus([DebuffHeadwindChaos, DebuffTailwindExdeath], out var data, lessThan: 4f + C.PreKnockbackEnforce.Value))
            {
                requiredNameId = data[0].ID == DebuffHeadwindChaos ? Enemies.Chaos : Enemies.Exdeath;
            }
        }
        if(requiredNameId == 0) return;

        if(Svc.Targets.Target is IBattleNpc currentTarget && currentTarget.NameId == requiredNameId)
        {
            LogLine = $"{Framework.Instance()->FrameCounter} already targeted: {currentTarget}";

            return;
        }

        var target = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.IsTargetable && !x.IsDead && x.NameId == requiredNameId);

        if(target != null)
        {
            EnforceTarget(target);
        }
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status Status)
    {
        if(sourceId == BasePlayer.ObjectId && C.PreKnockbackEnforce != null && C.SwitchBackExdeath != null && !BasePlayer.HasStatus([Buffs.FatedHero, Buffs.EpicHero]))
        {
            if(((uint)Status.StatusId).EqualsAny(DebuffHeadwindChaos, DebuffTailwindExdeath))
            {
                EzThrottler.Throttle($"Force{(C.SwitchBackExdeath.Value ? "Exdeath" : "Chaos")}", 2000);
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Don't switch off players", ref C.NoSwitchOffPlayers);
        ImGuiEx.DragFloat(100f, "Switch target according to your debuff before knockback, seconds", ref C.PreKnockbackEnforce, vMin:2f, vMax:10f, defaultValue: 5f);
        if(C.PreKnockbackEnforce != null)
        {
            ImGui.Indent();
            ImGuiEx.Text("And after knockback switch it back to:");
            ImGui.RadioButton("Don't switch", ref C.SwitchBackExdeath, null);
            ImGui.RadioButton("Exdeath", ref C.SwitchBackExdeath, true);
            ImGui.RadioButton("Chaos", ref C.SwitchBackExdeath, false);
            ImGui.Unindent();
        }
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"{LogLine}");
            EzThrottler.ImGuiPrintDebugInfo();
        }
    }

    private void EnforceTarget(IBattleNpc target)
    {
        if(!Throttle()) return;
        if(target == null || !target.IsTargetable || target.IsDead) return;
        LogLine = $"{Framework.Instance()->FrameCounter} Enforcing target: {target}";
        if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback]) return;
        if(Svc.Targets.Target != target)
        {
            Svc.Targets.Target = target;
        }
    }

    string LogLine = "";

    Config C => Controller.GetConfig<Config>();
    public class Config
    {
        public bool NoSwitchOffPlayers = false;
        public float? PreKnockbackEnforce = null;
        public float? UseDistanceLimit = null;
        public bool? SwitchBackExdeath = null;
    }
}
