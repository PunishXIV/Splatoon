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

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;
public class P3_Target_Enforcer : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1363];
    public override Metadata? Metadata => new(1, "Poneglyph, NightmareXIV");

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

    private bool Throttle() => EzThrottler.Throttle($"{InternalData.FullName}_SetTarget", 250);

    public override void OnUpdate()
    {
        if(!Controller.InCombat) return;
        if(Player.Object == null || Player.Object.IsDead) return;
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

        var player = Player.Object;
        if(player == null) return;

        var hasFatedHero = player.StatusList.Any(x => x.StatusId == Buffs.FatedHero);
        var hasEpicHero = player.StatusList.Any(x => x.StatusId == Buffs.EpicHero);

        uint requiredNameId;
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
            return;
        }

        if(Svc.Targets.Target is IBattleNpc currentTarget && currentTarget.NameId == requiredNameId)
        {
            return;
        }

        var target = Svc.Objects.OfType<IBattleNpc>()
            .FirstOrDefault(x => x.IsTargetable && !x.IsDead && x.NameId == requiredNameId);

        if(target != null)
        {
            EnforceTarget(target);
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Don't switch off players", ref C.NoSwitchOffPlayers);
    }

    private void EnforceTarget(IBattleNpc target)
    {
        if(!Throttle()) return;
        if(target == null || !target.IsTargetable || target.IsDead) return;
        if(Svc.Targets.Target != target)
        {
            Svc.Targets.Target = target;
        }
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public bool NoSwitchOffPlayers = false;
    }
}
