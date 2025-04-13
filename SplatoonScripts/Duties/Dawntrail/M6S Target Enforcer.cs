using Dalamud.Game.ClientState.Objects.Types;
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
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Action = Lumina.Excel.Sheets.Action;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe class M6S_Target_Enforcer : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1259];
    ImGuiEx.RealtimeDragDrop<MobKind> DragDrop = new("Mob", x => x.ToString());
    Dictionary<uint, long> EntityAgeTracker = [];
    Dictionary<uint, MobKind> Classifier = [];
    int JabberCount = 0;
    int FeatherRayCount = 0;

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
        Controller.RegisterElementFromCode("Attack", "{\"Name\":\"\",\"radius\":0.5,\"color\":3372155106,\"fillIntensity\":0.5,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
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
                if(Classifier.TryGetValue(npc.EntityId, out var value) && Controller.TryGetElementByName($"Debug{i++}", out var e))
                {
                    e.Enabled = true;
                    e.SetRefPosition(npc.Position);
                    e.overlayText = $"{value} : {GetMobAge(npc):F1}";
                }
            } 
        }
        var suggestedTarget = GetSuggestedTarget(out var useAction);
        if(suggestedTarget != null)
        {
            if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat] && C.Switch)
            {
                if(suggestedTarget.NameId == (uint)MobName.Jabberwock)
                {
                    if(!Svc.Targets.Target.AddressEquals(suggestedTarget) && EzThrottler.Throttle("M6SSwitchTarget", 200))
                    {
                        Svc.Targets.Target = suggestedTarget;
                    }
                }
            }
            if(Controller.TryGetElementByName("Attack", out var e))
            {
                e.Enabled = true;
                e.SetRefPosition(suggestedTarget.Position);
            }
        }
        if(Svc.Targets.Target is IBattleNpc n && n.NameId == (uint)MobName.Jabberwock)
        {
            if(useAction != 0)
            {
                if(EzThrottler.Throttle("M6SUseAction", 100))
                {
                    if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
                    {
                        Chat.Instance.ExecuteAction(useAction);
                    }
                    else
                    {
                        DuoLog.Information($"Would use action \"{ExcelActionHelper.GetActionName(useAction)}\" ({Framework.Instance()->FrameCounter})");
                    }
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
        JabberCount = 0;
        FeatherRayCount = 0;
        Classifier.Clear();
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
        ImGuiEx.Text($"Lockin priority:");
        DragDrop.Begin();
        if(ImGuiEx.BeginDefaultTable(["##drag", "Mob Kind", "~Action"]))
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
                if(C.Priority[i].EqualsAny(MobKind.Jabberwock_Wave_3, MobKind.Jabberwock_Wave_4))
                {
                    var data = C.Stuns.GetOrCreate(C.Priority[i]);
                    DrawStun(ref data.Action, ref data.Delay);
                }
                ImGui.PopID();
            }
            ImGui.EndTable();
        }
        DragDrop.End();
        ImGui.SetNextItemWidth(150f);
        ImGui.SliderFloat("Max range", ref C.MaxRadius, 0, 25f);
        ImGui.Separator();
        ImGui.PopID();

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"Suggested target: {GetSuggestedTarget(out var useAction)} / use action: {ExcelActionHelper.GetActionName(useAction, true)}");
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
        }
    }

    IBattleNpc? GetSuggestedTarget(out uint useAction)
    {
        useAction = 0;
        var objects = Svc.Objects.OfType<IBattleNpc>().Where(x => !x.IsDead && x.IsTargetable && Enum.GetValues<MobName>().Contains((MobName)x.NameId)).Where(x => Player.DistanceTo(x) < C.MaxRadius).OrderBy(Player.DistanceTo).ToArray();
        if(objects.Length <= 1) return null;
        
        return null;
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
        public bool Switch = false;
        public List<MobKind> Priority = [.. Enum.GetValues<MobKind>()];
        public float MaxRadius = 25f;
        public Dictionary<MobKind, StunInfo> Stuns = [];
        public HashSet<MobKind> BaitRay = [];
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
