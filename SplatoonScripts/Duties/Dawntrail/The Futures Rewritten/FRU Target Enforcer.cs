using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;
public class FRU_Target_Enforcer : SplatoonScript
{

    public override HashSet<uint>? ValidTerritories { get; } = [1238];
    public override Metadata? Metadata => new(2, "NightmareXIV");
    Config C => Controller.GetConfig<Config>();

    public static class Enemies
    {
        public const uint Fatebreaker = 9707;
        public const uint UsurperOfFrost = 12809;
        public const uint CrystalOfLight = 13555;
        public const uint IceVeil = 9358;
        public const uint OracleOfDarkness = 9832;
    }

    public Dictionary<CrystalDirection, Vector2> CrystalPositions = new()
    {
        [CrystalDirection.Disabled] = new(100, 100),
        [CrystalDirection.North] = new(100,85),
        [CrystalDirection.West] = new(85,100),
        [CrystalDirection.South] = new(100,115),
        [CrystalDirection.East] = new(115,100),
    };

    public bool Throttle() => EzThrottler.Throttle($"{this.InternalData.FullName}_SetTarget", 250);

    public override void OnUpdate()
    {
        if(!Controller.InCombat) return;
        if(Controller.CombatSeconds < C.CombatTreshold) return;
        if(Player.Object.IsDead || Player.Object.CurrentHp == 0)
        {
            EzThrottler.Throttle($"{this.InternalData.FullName}_SetTarget", 10000, true);
            return;
        } 
        if(!GenericHelpers.IsScreenReady()) return;
        if(C.DisableWhenMemberDead && Svc.Party.Count(x => x.GameObject is IPlayerCharacter pc && !pc.IsDead) < 6) return;
        if(Svc.Targets.Target is IBattleNpc npc)
        {
            if(npc.NameId.EqualsAny(Enemies.Fatebreaker, Enemies.UsurperOfFrost, Enemies.OracleOfDarkness)) return;
        }
        var t = GetTargetToSet();
        if(t != null && !t.IsTarget() && t.IsTargetable && Throttle())
        {
            Svc.Targets.Target = t;
        }
    }

    IBattleNpc? GetTargetToSet()
    {
        var sortedObj = Svc.Objects.OfType<IBattleNpc>().OrderBy(Player.DistanceTo);
        if(C.EnableCrystals != CrystalDirection.Disabled)
        {
            //special handling for crystals of light
            var priorityCrystal = sortedObj.Where(x => x.NameId == Enemies.CrystalOfLight && x.IsTargetable && !x.IsDead && x.CurrentHp > 0).OrderBy(x => Vector2.Distance(x.Position.ToVector2(), CrystalPositions[C.EnableCrystals]));
            if(priorityCrystal.TryGetFirst(crystal => Player.DistanceTo(crystal) <= C.MaxDistance, out var crystal))
            {
                return crystal;
            }
            else if(!priorityCrystal.Any())
            {
                var veil = sortedObj.FirstOrDefault(x => x.IsTargetable && x.NameId == Enemies.IceVeil);
                if(veil != null)
                {
                    return veil;
                }
            }
        }
        if(C.EnableFatebreaker)
        {
            if(sortedObj.TryGetFirst(x => x.IsTargetable && x.NameId == Enemies.Fatebreaker, out var obj))
            {
                return obj;
            }
        }
        if(C.EnableUsurper)
        {
            if(sortedObj.TryGetFirst(x => x.IsTargetable && x.NameId == Enemies.UsurperOfFrost, out var obj))
            {
                return obj;
            }
        }
        if(C.EnableOracle)
        {
            if(sortedObj.TryGetFirst(x => x.IsTargetable && x.NameId == Enemies.OracleOfDarkness, out var obj))
            {
                return obj;
            }
        }
        return null;
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(150f);
        ImGui.InputInt($"Only enforce after this amount of combat seconds", ref C.CombatTreshold);
        ImGui.Separator();
        ImGui.Checkbox("Fatebreaker", ref C.EnableFatebreaker);
        ImGui.Checkbox("Usurper of Frost", ref C.EnableUsurper);
        ImGui.Checkbox("Oracle of Darkness", ref C.EnableOracle);
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("Crystals of Light", ref C.EnableCrystals);
        ImGui.Indent();
        ImGui.SetNextItemWidth(150f);
        ImGui.InputFloat($"Limit distance", ref C.MaxDistance);
        ImGui.Unindent();
        ImGui.Separator();
        var t = GetTargetToSet();
        ImGuiEx.Text($"Current suggested target: {t} at {t?.Position} ({t?.IsTarget()})");
    }

    public class Config : IEzConfig
    {
        public bool EnableFatebreaker = true;
        public int CombatTreshold = 60;
        public float MaxDistance = 24.9f;
        public bool EnableUsurper = true;
        public CrystalDirection EnableCrystals = CrystalDirection.Disabled;
        public bool DisableWhenMemberDead = true;
        public bool EnableOracle = true;
    }

    public enum CrystalDirection { Disabled, North, West, South, East };
}
