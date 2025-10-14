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

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Quantum40;
public class Quantum_Target_Enforcer : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1290, 1311, 1333];
    public override Metadata? Metadata => new(5, "Poneglyph, NightmareXIV, Redmoon");

    private static class Buffs
    {
        public const uint DarkBuff = 4559;
        public const uint LightBuff = 4560;
    }

    private static class Enemies
    {
        public const uint EminentGrief = 14037;
        public const uint DevouredEater = 14038;
        public const uint VodorigaMinion = 14039;
        public const uint MagicCircle = 14042;
    }

    private bool Throttle() => EzThrottler.Throttle($"{InternalData.FullName}_SetTarget", 250);

    public override void OnUpdate()
    {
        if(!Controller.InCombat) return;
        if(Player.Object == null || Player.Object.IsDead) return;
        if(!GenericHelpers.IsScreenReady()) return;
        if(C.NoSwitchOffPlayers && Svc.Targets.Target is IPlayerCharacter)
        {
            return;
        }

        if(C.NoSwitchMagicCircle && Svc.Targets.Target is ICharacter npc && npc.NameId == Enemies.MagicCircle)
        {
            return;
        }

        var player = Player.Object;
        if(player == null) return;

        var vodoriga = Svc.Objects.OfType<IBattleNpc>()
            .FirstOrDefault(x => x.IsTargetable && !x.IsDead && x.NameId == Enemies.VodorigaMinion);

        if(vodoriga != null)
        {
            if(C.TargetVodoriga)
            {
                EnforceTarget(vodoriga);
            }
            return;
        }

        var hasDark = player.StatusList.Any(x => x.StatusId == Buffs.DarkBuff);
        var hasLight = player.StatusList.Any(x => x.StatusId == Buffs.LightBuff);

        if(Svc.Targets.Target is IBattleNpc currentTarget)
        {

            bool isCurrentlyValidTarget =
                currentTarget.NameId == Enemies.EminentGrief ||
                currentTarget.NameId == Enemies.DevouredEater;

            if(!isCurrentlyValidTarget)
            {
                return;
            }
        }

        IBattleNpc? target = null;

        if(hasLight)
        {
            target = Svc.Objects.OfType<IBattleNpc>()
                .FirstOrDefault(x => x.IsTargetable && !x.IsDead && x.NameId == Enemies.EminentGrief);
        }
        else if(hasDark)
        {
            target = Svc.Objects.OfType<IBattleNpc>()
                .FirstOrDefault(x => x.IsTargetable && !x.IsDead && x.NameId == Enemies.DevouredEater);
        }

        if(target != null)
        {
            EnforceTarget(target);
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Target Vodoriga", ref C.TargetVodoriga);
        ImGui.Checkbox("Don't switch off players", ref C.NoSwitchOffPlayers);
        ImGui.Checkbox("Don't switch off Magic Circle", ref C.NoSwitchMagicCircle);
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
        public bool TargetVodoriga = true;
        public bool NoSwitchOffPlayers = false;
        public bool NoSwitchMagicCircle = false;
    }
}