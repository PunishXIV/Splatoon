using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Automation.UIInput;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices.Sheets;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.IPC;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static ECommons.IPC.ECommonsIPC;
using static SplatoonScriptsOfficial.Duties.Dawntrail.Seasonal.HatchingTide2026.AttackPlanner;

using ECommons.DalamudServices.Legacy;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Seasonal;

public unsafe class HatchingTide2026 : SplatoonScript
{
    public override Metadata Metadata { get; } = new(2, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1336, 133];

    public static class Enemies
    {
        public static readonly uint[] NormalShrub = [2015072, 2015073, 2015074, 2015075];
        public static readonly uint[] BuffShrub = [2015077, 2015078, 2015076, 2015079];
    }

    private Vector3? ExitPoint = null;
    private AttackPlanner? Plan = null;

    public override void OnReset()
    {
        Plan = null;
        ExitPoint = null;
        EzThrottler.ThrottleNames.Each(EzThrottler.Reset);
    }

    public override void OnDisable()
    {
        Vnavmesh.Stop();
    }

    private Vector2? CurrentPoint;

    private Enemy[] EnemyList => [.. Svc.Objects.Where(x => x.DataId.EqualsAny([.. Enemies.NormalShrub, .. Enemies.BuffShrub])).Select(x => new Enemy(x.Position.ToVector2(), Enemies.BuffShrub.Contains(x.DataId)))];

    public override void OnUpdate()
    {
        if(!GenericHelpers.IsScreenReady() || Svc.Condition[ConditionFlag.Occupied33] || Svc.Condition[ConditionFlag.SufferingStatusAffliction2] || Svc.Condition[ConditionFlag.SufferingStatusAffliction63])
        {
            if(GenericHelpers.TryGetAddonByName<AtkUnitBase>("EasterMowingResult", out var addon) && addon->IsReady())
            {
                var btn = addon->GetComponentButtonById(65);
                if(EzThrottler.Throttle("Confirm")) btn->ClickAddonButton(addon);
            }
            Controller.Reset();
            return;
        }
        if(Svc.ClientState.TerritoryType == 133)
        {
            if(C.Farm && InventoryManager.Instance()->GetInventoryItemCount(50089) < C.FarmStop && QuestManager.IsQuestComplete(5425))
            {
                var npc = Svc.Objects.FirstOrDefault(x => x.IsTargetable && x.DataId == 1056067);
                if(npc != null && Player.DistanceTo(npc) < 6f)
                {
                    {
                        if(GenericHelpers.TryGetAddonMaster<AddonMaster.Talk>(out var m) && m.IsAddonReady) m.Click();
                    }
                    {
                        if(GenericHelpers.TryGetAddonMaster<AddonMaster.SelectString>(out var m) && m.IsAddonReady)
                        {
                            if(FrameThrottler.Throttle("Select", 8))
                            {
                                foreach(var x in m.Entries)
                                {
                                    if(x.Text.EqualsAny(Svc.Data.GetExcelSheet<QuestDialogueText>(name: "custom/009/FesEst2026Entrance_00960").GetRow(8).Value.GetText()))
                                    {
                                        x.Select();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if(!Svc.Condition[ConditionFlag.OccupiedInQuestEvent])
                    {
                        if(EzThrottler.Throttle("Target", 2000))
                        {
                            Svc.Targets.Target = npc;
                            TargetSystem.Instance()->InteractWithObject(npc.Struct(), false);
                        }
                    }
                }
            }
        }
        else
        {
            ExitPoint ??= Player.Position + new Vector3(0, 0, -43);
            if(Plan == null)
            {

                if(EnemyList.Length > 0)
                {
                    Plan = new(EnemyList);
                }
            }
            else
            {
                if(EzThrottler.Throttle("DontRecalculate", 100))
                {
                    Plan = new(EnemyList);
                }
            }

            if(Vnavmesh.PathfindInProgress()) return;

            if(!Vnavmesh.IsRunning() && !Vnavmesh.PathfindInProgress()) CurrentPoint = null;

            if(Vnavmesh.Available && Vnavmesh.IsReady() && Plan != null)
            {
                if(Plan.Enemies.Count != 0)
                {
                    var result = Plan.Calculate(Player.Position.ToVector2(), ActionManager.Instance()->GetActionStatus(ActionType.Action, 42038u) == 0 && Plan.Enemies.Count >= 12);
                    var point = result.OrderBy(x => Player.DistanceTo(x.Position)).First();
                    if(Vector2.Distance(Player.Position.ToVector2(), point.Position) < 0.25f)
                    {
                        if(!Vnavmesh.IsRunning())
                        {
                            var action = point.IsBigAttack ? 42038u : 45127u;
                            if(ActionManager.Instance()->GetActionStatus(ActionType.Action, action) == 0 && !Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Mounting71] && EzThrottler.Throttle("UseAction"))
                            {
                                ActionManager.Instance()->UseAction(ActionType.Action, action);
                            }
                        }
                    }
                    else
                    {
                        if(CurrentPoint != point.Position)
                        {
                            if(EzThrottler.Throttle("Path"))
                            {
                                Vnavmesh.Stop();
                                Vnavmesh.PathfindAndMoveTo(point.Position.ToVector3(), false);
                                CurrentPoint = point.Position;
                            }
                        }
                    }
                }
                else if(ExitPoint != null)
                {
                    if(EzThrottler.Throttle("Path"))
                    {
                        ECommonsIPC.Vnavmesh.PathfindAndMoveTo(ExitPoint.Value, false);
                    }
                }
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Source is IPlayerCharacter pc)
        {
            PluginLog.Information($"Used: {set.Header.ActionID}/{set.Header.ActionType}");
            if(set.Header.ActionType == FFXIVClientStructs.FFXIV.Client.Game.ActionType.Action)
            {
                if(set.Header.ActionID.EqualsAny(45127u))
                {
                    EzThrottler.Throttle("DontRecalculate", 5000, true);
                    Plan?.ApplyHit(Player.Position.ToVector2(), false);
                }
                if(set.Header.ActionID.EqualsAny(42038u))
                {
                    EzThrottler.Throttle("DontRecalculate", 5000, true);
                    Plan?.ApplyHit(Player.Position.ToVector2(), true);
                }
            }
        }
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public bool Farm = false;
        public int FarmStop = 267;
    }

    public override void OnSettingsDraw()
    {
        if(!Vnavmesh.Available)
        {
            ImGuiEx.Text(EColor.RedBright, "vnavmesh plugin is required");
        }
        ImGui.Checkbox("Enable farm", ref C.Farm);
        ImGui.Indent();
        ImGuiEx.TextWrapped("Stay next to NPC");
        ImGui.SetNextItemWidth(200f);
        ImGui.InputInt("Stop at this amount of eggs", ref C.FarmStop);
        ImGui.Unindent();
        if(ImGui.CollapsingHeader("Debug"))
        {
            EzThrottler.ImGuiPrintDebugInfo();
        }
    }

    public sealed class AttackPlanner(IEnumerable<AttackPlanner.Enemy> enemies)
    {
        public sealed class Enemy(Vector2 position, bool isPriority = false)
        {
            public Vector2 Position = position;
            public bool IsPriority = isPriority;
        }

        public readonly struct AttackPoint(Vector2 position, bool isBigAttack = false)
        {
            public readonly Vector2 Position = position;
            public readonly bool IsBigAttack = isBigAttack;

            public override string ToString() => $"{(IsBigAttack ? "BIG" : "normal")} attack at {Position}";
        }

        public const float NormalRadius = 5f - 0.3f;
        public const float BigRadius = 16f - 1.3f;

        public List<Enemy> Enemies = [.. enemies];

        public void ApplyHit(Vector2 position, bool isBigAttack)
        {
            var radiusSq = Sq(isBigAttack ? BigRadius : NormalRadius);
            Enemies.RemoveAll(e => Dist2(e.Position, position) <= radiusSq);
        }

        public List<AttackPoint> Calculate(Vector2 playerPosition, bool bigAttackReady)
        {
            if(Enemies.Count == 0) return [];

            var priorityEnemies = Enemies.Where(e => e.IsPriority).ToList();

            if(priorityEnemies.Count > 0)
            {
                return CalculatePriorityAttack(playerPosition, priorityEnemies, bigAttackReady);
            }
            else
            {
                return CalculateCoverageAttacks(bigAttackReady);
            }
        }

        private List<AttackPoint> CalculatePriorityAttack(Vector2 playerPosition, List<Enemy> priorityEnemies, bool bigAttackReady)
        {
            var nearest = priorityEnemies.OrderBy(e => Dist2(e.Position, playerPosition)).First();

            var normalSq = Sq(NormalRadius);
            var bigSq = Sq(BigRadius);

            var bestPos = nearest.Position;
            var bestCoverage = 0;

            foreach(var candidate in Enemies)
            {
                if(Dist2(candidate.Position, nearest.Position) > normalSq) continue;

                var coverage = Enemies.Count(e => Dist2(e.Position, candidate.Position) <= normalSq);
                if(coverage > bestCoverage)
                {
                    bestCoverage = coverage;
                    bestPos = candidate.Position;
                }
            }

            if(bigAttackReady)
            {
                var bigCoverage = Enemies.Count(e => Dist2(e.Position, bestPos) <= bigSq);
                if(bigCoverage > bestCoverage) return [new AttackPoint(bestPos, true)];
            }

            return [new AttackPoint(bestPos, false)];
        }

        private List<AttackPoint> CalculateCoverageAttacks(bool bigAttackReady)
        {
            var remaining = new List<Enemy>(Enemies);
            var result = new List<AttackPoint>();
            var normalSq = Sq(NormalRadius);
            var bigSq = Sq(BigRadius);
            var bigLeft = bigAttackReady;

            while(remaining.Count > 0)
            {
                var bestPos = remaining[0].Position;
                var bestCoverage = 0;
                var useBig = false;

                foreach(var candidate in remaining)
                {
                    var normalCoverage = remaining.Count(e => Dist2(e.Position, candidate.Position) <= normalSq);
                    if(normalCoverage > bestCoverage)
                    {
                        bestCoverage = normalCoverage;
                        bestPos = candidate.Position;
                        useBig = false;
                    }

                    if(bigLeft)
                    {
                        var bigCoverage = remaining.Count(e => Dist2(e.Position, candidate.Position) <= bigSq);
                        if(bigCoverage > bestCoverage)
                        {
                            bestCoverage = bigCoverage;
                            bestPos = candidate.Position;
                            useBig = true;
                        }
                    }
                }

                result.Add(new AttackPoint(bestPos, useBig));
                if(useBig) bigLeft = false;

                var radius = useBig ? bigSq : normalSq;
                remaining.RemoveAll(e => Dist2(e.Position, bestPos) <= radius);
            }

            return result;
        }

        private static float Dist2(Vector2 a, Vector2 b) => (a - b).LengthSquared();
        private static float Sq(float x) => x * x;
    }
}
