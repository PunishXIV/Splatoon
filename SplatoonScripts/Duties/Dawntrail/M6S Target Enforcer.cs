using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using ImGuiNET;
using SharpDX.DXGI;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using Action = Lumina.Excel.Sheets.Action;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe class M6S_Target_Enforcer : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1259];
    public override Metadata? Metadata => new(5, "NightmareXIV");

    public override Dictionary<int, string> Changelog => new()
    {
        [3] = """
        M6S Target Enforcer script has been completely remade to allow significantly more precise operation. Please configure it again.
        """
    };

    ImGuiEx.RealtimeDragDrop<MobKind> DragDrop = new("Mob", x => x.ToString());
    Dictionary<uint, long> EntityAgeTracker = [];
    Dictionary<uint, MobKind> Classifier = [];
    bool Switched = false;

    public enum MobName : uint
    {
        GimmeCat = 13835,
        Yan = 13832, //goat
        Mu = 13831, //squirrel
        FeatherRay = 13834, //bait water
        Jabberwock = 13833, //stun him
    }

    public enum MobKind
    {
        GimmeCat_Wave_1,
        Yan_Wave_1,
        Mu_Wave_1, //there's 2 of them
        West_Feather_Ray_Wave_2, //there's 2 of them
        East_Feather_Ray_Wave_2, //there's 2 of them
        Mu_Wave_2, //there's 2 of them
        GimmeCat_Wave_3,
        Yan_Wave_3,
        Jabberwock_Wave_3,
        Gimme_Cat_Wave_4,
        West_Feather_Ray_Wave_4, //there's 2 of them
        East_Feather_Ray_Wave_4, //there's 2 of them
        Yan_Wave_4,
        Mu_Wave_4, //there's 2 of them,
        Jabberwock_Wave_4,
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("AttackInRange", "{\"Name\":\"\",\"radius\":0.5,\"Filled\":false,\"color\":3372155106,\"fillIntensity\":0.5,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Attack", "{\"Name\":\"\",\"thicc\":4,\"radius\":0.5,\"Filled\":false,\"color\":3372155106,\"fillIntensity\":0.5,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        for(int i = 0; i < 20; i++)
        {
            Controller.RegisterElementFromCode($"Debug{i}", "{\"Name\":\"\",\"radius\":0.0,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":0.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }
    }

    public override void OnUpdate()
    {
        int i = 0;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        foreach(var x in Svc.Objects)
        {
            if(x.IsTargetable && x is IBattleNpc npc && Enum.GetValues<MobName>().Contains((MobName)npc.NameId))
            {
                if(!EntityAgeTracker.ContainsKey(npc.EntityId))
                {
                    EntityAgeTracker[npc.EntityId] = Environment.TickCount64;
                }
                ClassifyMob(npc);
                if(C.Debug && Classifier.TryGetValue(npc.EntityId, out var value) && Controller.TryGetElementByName($"Debug{i++}", out var e))
                {
                    e.Enabled = true;
                    e.SetRefPosition(npc.Position);
                    e.overlayText = $"{value} : {GetMobAge(npc):F1}";
                }
            } 
        }

        GetSuggestedTarget(out var inRangeTarget, out var target);
        if(C.Switch && inRangeTarget == null && target == null && Classifier.Count > 0 && !Switched)
        {
            if(Svc.Objects.OfType<IBattleNpc>().TryGetFirst(x => x.NameId == 13822 && x.IsTargetable, out var sugarRiot))
            {
                if(EzThrottler.Throttle(this.InternalData.FullName + "Retarget", 200))
                {
                    if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
                    {
                        if(!Svc.Targets.Target.AddressEquals(sugarRiot))
                        {
                            Svc.Targets.Target = sugarRiot;
                        }
                    }
                    else if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback])
                    {
                        DuoLog.Information("Would switch to Sugar Riot");
                    }
                    if(Classifier.ContainsValue(MobKind.Yan_Wave_4) || Controller.Scene != 1)
                    {
                        Switched = true;
                    }
                }
            }
        }
        if(inRangeTarget != null)
        {
            if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat] && C.Switch && !inRangeTarget.AddressEquals(Svc.Targets.Target))
            {
                if(!C.SoftTargetMobs.Contains(Classifier[inRangeTarget.EntityId]) || Svc.Targets.Target == null)
                {
                    if(EzThrottler.Throttle(this.InternalData.FullName + "Retarget", 200))
                    {
                        Svc.Targets.Target = inRangeTarget;
                    }
                }
            }
            DrawAttackInRange(inRangeTarget);
        }
        if(target != null && !inRangeTarget.AddressEquals(target))
        {
            DrawAttack(target);
        }
        var useAction = GetSuggestedActionOnTarget();
        if(useAction != 0)
        {
            if(EzThrottler.Throttle("M6SUseAction", 100))
            {
                if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
                {
                    Chat.Instance.ExecuteAction(useAction);
                }
                else if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback])
                {
                    DuoLog.Information($"Would use action \"{ExcelActionHelper.GetActionName(useAction)}\" ({Framework.Instance()->FrameCounter})");
                }
            }
        }
    }

    void ClassifyMob(IBattleNpc mob)
    {
        if(Classifier.ContainsKey(mob.EntityId)) return;
        if(mob.NameId == (uint)MobName.GimmeCat)
        {
            MobKind[] values = [MobKind.GimmeCat_Wave_1, MobKind.GimmeCat_Wave_3, MobKind.Gimme_Cat_Wave_4];
            Classifier[mob.EntityId] = values[Classifier.Values.Count(values.Contains)];
        }
        else if(mob.NameId == (uint)MobName.Jabberwock)
        {
            Classifier[mob.EntityId] = !Classifier.ContainsValue(MobKind.Jabberwock_Wave_3) ? MobKind.Jabberwock_Wave_3 : MobKind.Jabberwock_Wave_4;
        }
        else if(mob.NameId == (uint)MobName.Mu)
        {
            MobKind[] values = [MobKind.Mu_Wave_1, MobKind.Mu_Wave_2, MobKind.Mu_Wave_4];
            Classifier[mob.EntityId] = values[Classifier.Values.Count(values.Contains) / 2];
        }
        else if(mob.NameId == (uint)MobName.Yan)
        {
            MobKind[] values = [MobKind.Yan_Wave_1, MobKind.Yan_Wave_3, MobKind.Yan_Wave_4];
            Classifier[mob.EntityId] = values[Classifier.Values.Count(values.Contains)];
        }
        else if(mob.NameId == (uint)MobName.FeatherRay && mob.Position.X < 100)
        {
            Classifier[mob.EntityId] = !Classifier.ContainsValue(MobKind.West_Feather_Ray_Wave_2) ? MobKind.West_Feather_Ray_Wave_2 : MobKind.West_Feather_Ray_Wave_4;
        }
        else if(mob.NameId == (uint)MobName.FeatherRay && mob.Position.X > 100)
        {
            Classifier[mob.EntityId] = !Classifier.ContainsValue(MobKind.East_Feather_Ray_Wave_2) ? MobKind.East_Feather_Ray_Wave_2 : MobKind.East_Feather_Ray_Wave_4;
        }
    }

    public override void OnReset()
    {
        EntityAgeTracker.Clear();
        Classifier.Clear();
        Switched = false;
    }

    public override void OnSettingsDraw()
    {
        C.Priority.RemoveAll(x => !Enum.GetValues<MobKind>().Contains(x));
        foreach(var x in Enum.GetValues<MobKind>())
        {
            if(C.Priority.Count(v => v == x) > 1) C.Priority.RemoveAll(v => v == x);
            if(!C.Priority.Contains(x)) C.Priority.Add(x);
        }
        ImGui.Checkbox("Automatically switch targets", ref C.Switch);
        ImGuiEx.TextV($"Lockin priority:");
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(Dalamud.Interface.FontAwesomeIcon.ArrowAltCircleLeft, "Restore Defaults (hold ctrl)", ImGuiEx.Ctrl))
        {
            C.Priority = new Config().Priority;
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150f);
        ImGui.SliderFloat("Max range", ref C.MaxRadius, 0, 25f);
        DragDrop.Begin();
        if(ImGuiEx.BeginDefaultTable(["##drag", "Mob Kind", "##control", "~Action"]))
        {
            for(int i = 0; i < C.Priority.Count; i++)
            {
                var n = C.Priority[i].ToString();
                ImGui.PushID(n);
                ImGui.TableNextRow();
                DragDrop.SetRowColor(n);
                ImGui.TableNextColumn();
                DragDrop.NextRow();
                DragDrop.DrawButtonDummy(n, C.Priority, i);
                ImGui.TableNextColumn();
                Vector4? col = n.Contains('2') ? ImGuiColors.ParsedGold : (n.Contains('3') ? ImGuiColors.ParsedGreen : (n.Contains('4') ? ImGuiColors.ParsedOrange : null));
                ImGuiEx.TextV(col, n.Replace("_", " "));

                ImGui.TableNextColumn();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.CollectionButtonCheckbox(FontAwesomeIcon.UserSlash.ToIconString(), C.Priority[i], C.DisabledMobs);
                ImGui.PopFont();
                ImGuiEx.Tooltip($"Completely ignore this mob (except jabberwock stuns and feather ray baits)");
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.CollectionButtonCheckbox(FontAwesomeIcon.PaintBrush.ToIconString(), C.Priority[i], C.SoftTargetMobs);
                ImGui.PopFont();
                ImGuiEx.Tooltip($"Only highlight this mob, but do not auto-switch target to it unless you do not have target at all");

                ImGui.TableNextColumn();
                if(C.Priority[i].EqualsAny(MobKind.Jabberwock_Wave_3, MobKind.Jabberwock_Wave_4))
                {
                    var data = C.Stuns.GetOrCreate(C.Priority[i]);
                    DrawStun(ref data.Action, ref data.Delay);
                }
                if(C.Priority[i].EqualsAny(MobKind.West_Feather_Ray_Wave_4, MobKind.West_Feather_Ray_Wave_2, MobKind.East_Feather_Ray_Wave_4, MobKind.East_Feather_Ray_Wave_2))
                {
                    ImGuiEx.CollectionCheckbox("Bait", C.Priority[i], C.BaitRay);
                }
                ImGui.PopID();
            }
            ImGui.EndTable();
        }
        DragDrop.End();
        ImGui.Separator();

        if(ImGui.CollapsingHeader("Debug"))
        {
            GetSuggestedTarget(out var inRange, out var target);
            ImGuiEx.Text($"""
                Suggested target in range: {inRange}
                Suggested target in range: {target}
                Suggested action: {ExcelActionHelper.GetActionName(GetSuggestedActionOnTarget(), true)}
                """);
            ImGuiEx.Text($"Mob age:");
            ImGui.Indent();
            foreach(var x in EntityAgeTracker)
            {
                ImGuiEx.Text($"{x.Key}/{x.Key.GetObject()}: {GetMobAge(x.Key.GetObject() is IBattleNpc i? i:null):F1}");
            }
            ImGui.Unindent();
            ImGui.Separator();
            foreach(var x in Classifier)
            {
                ImGuiEx.Text($"Mob {x.Key}/{x.Key.GetObject()}: {x.Value}");
            }
            ImGui.Separator();
            ImGuiEx.Text($"""
                Enemy order list:
                {GetOrderedPotentialTargets().Print("\n")}
                """);
            ImGui.Checkbox("Draw debug information", ref C.Debug);
        }
    }

    void DrawAttackInRange(IBattleNpc mob)
    {
        if(Controller.TryGetElementByName("AttackInRange", out var e))
        {
            e.Enabled = true;
            e.SetRefPosition(mob.Position);
            e.color = GradientColor.Get(EColor.RedBright, EColor.YellowBright).ToUint();
            e.overlayText = Classifier[mob.EntityId].ToString().Replace("_", " ");
        }
    }

    void DrawAttack(IBattleNpc mob)
    {
        if(Controller.TryGetElementByName("Attack", out var e))
        {
            e.Enabled = true;
            e.SetRefPosition(mob.Position);
            e.overlayText = Classifier[mob.EntityId].ToString().Replace("_", " ");
        }
    }

    void GetSuggestedTarget(out IBattleNpc? inRangeTarget, out IBattleNpc? uncheckedRangeTarget)
    {
        inRangeTarget = GetSuggestedTargetWithRangeLimit(C.MaxRadius);
        uncheckedRangeTarget = GetSuggestedTargetWithRangeLimit(100f);
    }

    IBattleNpc? GetSuggestedTargetWithRangeLimit(float rangeLimit)
    {
        var enemies = GetOrderedPotentialTargets().Where(x => Player.DistanceTo(x) < rangeLimit + x.HitboxRadius);
        var rayBaits = enemies.Where(x => GetHPPercent(x) >= 0.99f && C.BaitRay.Contains(Classifier[x.EntityId]));
        if(rayBaits.Any())
        {
            return rayBaits.First();
        }
        foreach(var x in enemies)
        {
            if(C.DisabledMobs.Contains(Classifier[x.EntityId])) continue;
            if(Classifier[x.EntityId].EqualsAny(MobKind.West_Feather_Ray_Wave_2, MobKind.East_Feather_Ray_Wave_2, MobKind.West_Feather_Ray_Wave_4, MobKind.East_Feather_Ray_Wave_4) && GetHPPercent(x) >= 0.99f) continue;
            return x;
        }
        return null;
    }

    IEnumerable<IBattleNpc> GetOrderedPotentialTargets()
    {
        return Svc.Objects.OfType<IBattleNpc>().Where(x => x.IsTargetable && x.CurrentHp > 0 && Classifier.ContainsKey(x.EntityId)).OrderBy(x => C.Priority.IndexOf(Classifier[x.EntityId]));
    }

    uint GetSuggestedActionOnTarget()
    {
        if(Svc.Targets.Target is IBattleNpc b)
        {
            if(Classifier.TryGetValue(b.EntityId, out var value) && C.Stuns.TryGetValue(value, out var stunInfo))
            {
                return stunInfo.Action;
            }
        }
        return 0;
    }

    float GetHPPercent(IBattleNpc mob)
    {
        return (float)mob.CurrentHp / (float)mob.MaxHp;
    }

    float GetMobAge(IBattleNpc? mob)
    {
        if(mob == null) return -1;
        if(this.EntityAgeTracker.TryGetValue(mob.EntityId, out var x))
        {
            return (float)((double)(Environment.TickCount64 - x) / 1000.0);
        }
        return -1;
    }

    Config C => this.Controller.GetConfig<Config>();
    public class Config: IEzConfig
    {
        public bool Debug = false;
        public bool Switch = false;
        public List<MobKind> Priority = [
            MobKind.Jabberwock_Wave_3,
            MobKind.Jabberwock_Wave_4,
            MobKind.East_Feather_Ray_Wave_2,
            MobKind.West_Feather_Ray_Wave_2,
            MobKind.GimmeCat_Wave_1,
            MobKind.GimmeCat_Wave_3,
            MobKind.East_Feather_Ray_Wave_4,
            MobKind.Gimme_Cat_Wave_4,
            MobKind.West_Feather_Ray_Wave_4,
            MobKind.Yan_Wave_1,
            MobKind.Mu_Wave_1,
            MobKind.Mu_Wave_2,
            MobKind.Mu_Wave_4,
            MobKind.Yan_Wave_3,
            MobKind.Yan_Wave_4,
            ];
        public float MaxRadius = 25f;
        public Dictionary<MobKind, StunInfo> Stuns = [];
        public HashSet<MobKind> BaitRay = [];
        public HashSet<MobKind> DisabledMobs = [];
        public HashSet<MobKind> SoftTargetMobs = [];
    }

    public class StunInfo()
    {
        public uint Action = 0;
        public int Delay = 0;
    }

    Dictionary<uint, string> StunActions = [new KeyValuePair<uint, string>(0, "Disabled") ,
        ..Svc.Data.GetExcelSheet<Action>()
        .Where(x => (x.CooldownGroup == 58 && x.AdditionalCooldownGroup != 0) || x.CooldownGroup != 58)
        .Where(x => x.IsPlayerAction)
        .Where(x => x.ClassJobCategory.ValueNullable != null)
        .Where(x => Enum.GetValues<Job>().Where(x => x.IsCombat()).Any(s => x.ClassJobCategory.Value.IsJobInCategory(s)))
        .ToDictionary(x => x.RowId, x=> ExcelActionHelper.GetActionName(x, true) + "##" + x.RowId.ToString())];
    void DrawStun(ref uint action, ref int delay)
    {
        ImGui.SetNextItemWidth(100f.Scale());
        var name = action == 0 ? "Disabled" : ExcelActionHelper.GetActionName(action);
        ImGuiEx.Combo("Select control action", ref action, StunActions.Keys, names: StunActions);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f.Scale());
        ImGuiEx.SliderIntAsFloat("Delay, seconds", ref delay, 0, 30000);
    }
}
