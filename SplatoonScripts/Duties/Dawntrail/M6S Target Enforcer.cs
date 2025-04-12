using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Action = Lumina.Excel.Sheets.Action;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe class M6S_Target_Enforcer : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1259];
    ImGuiEx.RealtimeDragDrop<Mob> DragDrop = new("Mob", x => x.ToString());
    Dictionary<uint, long> EntityAgeTracker = [];
    int JabberCount = 0;
    int FeatherRayCount = 0;

    public enum Mob : uint
    {
        GimmeCat = 13835,
        Yan = 13832, //goat
        Mu = 13831, //squirrel
        FeatherRay = 13834, //bait water
        SugarRiot = 13822, //boss
        Jabberwock = 13833, //stun him
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Attack", "{\"Name\":\"\",\"radius\":0.5,\"color\":3372155106,\"fillIntensity\":0.5,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        foreach(var x in Svc.Objects)
        {
            if(x.IsTargetable && x is IBattleNpc npc && Enum.GetValues<Mob>().Contains((Mob)npc.NameId) && !EntityAgeTracker.ContainsKey(x.EntityId))
            {
                EntityAgeTracker[x.EntityId] = Environment.TickCount64;
                if((Mob)npc.NameId == Mob.Jabberwock) JabberCount++;
                if((Mob)npc.NameId == Mob.FeatherRay) FeatherRayCount++;
            } 
        }
        var suggestedTarget = GetSuggestedTarget(out var useAction);
        if(suggestedTarget != null)
        {
            if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat] && C.Switch)
            {
                if(suggestedTarget.NameId == (uint)Mob.Jabberwock)
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
        if(Svc.Targets.Target is IBattleNpc n && n.NameId == (uint)Mob.Jabberwock)
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

    public override void OnReset()
    {
        EntityAgeTracker.Clear();
        JabberCount = 0;
        FeatherRayCount = 0;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Automatically switch targets", ref C.Switch);
        ImGuiEx.Text($"Lockin priority:");
        DragDrop.Begin();
        for(int i = 0; i < C.Prio.Count; i++)
        {
            DragDrop.NextRow();
            DragDrop.DrawButtonDummy(C.Prio[i].ToString(), C.Prio, i);
            ImGui.SameLine();
            ImGuiEx.Text(C.Prio[i].ToString());
        }
        DragDrop.End();
        ImGui.SetNextItemWidth(150f);
        ImGui.SliderFloat("Max range", ref C.MaxRadius, 0, 25f);
        ImGui.Separator();
        ImGuiEx.Text("Bait Feather Ray:");
        ImGui.Checkbox("1st", ref C.BaitWater1);
        ImGui.SameLine();
        ImGui.Checkbox("2nd", ref C.BaitWater2);
        ImGuiEx.RadioButtonBool("West", "East", ref C.BaitWaterWest, true);
        ImGui.Separator();
        ImGuiEx.Text("First Jabberwock control action:");
        DrawJabber(ref C.FirstJabberAction, ref C.FirstJabberDelay);
        ImGuiEx.Text("Second Jabberwock control action:");
        ImGui.PushID("2ndj");
        DrawJabber(ref C.SecondJabberAction, ref C.SecondJabberDelay);
        ImGui.PopID();

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"Suggested target: {GetSuggestedTarget(out var useAction)} / use action: {ExcelActionHelper.GetActionName(useAction, true)}");
            ImGui.InputInt("Num jabbers", ref JabberCount);
            ImGui.InputInt("Num feather rays", ref FeatherRayCount);
            if(C.FirstJabberAction != 0)
            {
                ImGuiEx.Text($"CD1: {ExcelActionHelper.GetActionCooldown(C.FirstJabberAction)}");
            }
            if(C.SecondJabberAction != 0)
            {
                ImGuiEx.Text($"CD2: {ExcelActionHelper.GetActionCooldown(C.SecondJabberAction)}");
            }
            ImGuiEx.Text($"Mob age:");
            ImGui.Indent();
            foreach(var x in EntityAgeTracker)
            {
                ImGuiEx.Text($"{x.Key}/{x.Key.GetObject()}: {GetMobAge(x.Key.GetObject() is IBattleNpc i? i:null):F1}");
            }
            ImGui.Unindent();
        }
    }

    IBattleNpc? GetSuggestedTarget(out uint useAction)
    {
        useAction = 0;
        var objects = Svc.Objects.OfType<IBattleNpc>().Where(x => !x.IsDead && x.IsTargetable && Enum.GetValues<Mob>().Contains((Mob)x.NameId)).Where(x => Player.DistanceTo(x) < C.MaxRadius).OrderBy(Player.DistanceTo).ToArray();
        if(objects.Length <= 1) return null;
        IBattleNpc? processJabber(uint action, int count, int delay, out uint useAction)
        {
            useAction = 0;
            if(action != 0 && JabberCount == count)
            {
                var cd = ExcelActionHelper.GetActionCooldown(action);
                if(cd == 0 && objects.TryGetFirst(x => x.NameId == (uint)Mob.Jabberwock && GetMobAge(x) >= delay, out var ret))
                {
                    useAction = action;
                    return ret;
                }
            }
            return null;
        }
        IBattleNpc? processFeatherRay(bool doBait, int[] count)
        {
            if(doBait && this.FeatherRayCount.EqualsAny(count))
            {
                if(objects.TryGetFirst(x => x.NameId == (uint)Mob.FeatherRay && GetHPPercent(x) >= 0.99f && (C.BaitWaterWest?x.Position.X<100:x.Position.X>100), out var result))
                {
                    return result;
                }
            }
            return null;
        }
        var forced = processFeatherRay(C.BaitWater1, [1, 2]) 
            ?? processFeatherRay(C.BaitWater2, [3, 4]) 
            ?? processJabber(C.FirstJabberAction, 1, C.FirstJabberDelay, out useAction) 
            ?? processJabber(C.SecondJabberAction, 2, C.SecondJabberDelay, out useAction);
        if(forced != null) return forced;
        foreach(var m in C.Prio)
        {
            if(objects.TryGetFirst(x => x.NameId == (uint)m, out var ret))
            {
                if(m == Mob.FeatherRay && GetHPPercent(ret) > 99f) continue;
                return ret;
            }
        }
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
        public List<Mob> Prio = [.. Enum.GetValues<Mob>()];
        public float MaxRadius = 25f;
        public bool BaitWater1 = false;
        public bool BaitWater2 = false;
        public bool BaitWaterWest = true;
        public uint FirstJabberAction = 0;
        public int FirstJabberDelay = 0;
        public uint SecondJabberAction = 0;
        public int SecondJabberDelay = 0;
    }

    Dictionary<uint, string> StunActions = [new KeyValuePair<uint, string>(0, "Disabled") ,
        ..Svc.Data.GetExcelSheet<Action>()
        .Where(x => (x.CooldownGroup == 58 && x.AdditionalCooldownGroup != 0) || x.CooldownGroup != 58)
        .Where(x => x.IsPlayerAction)
        .Where(x => x.ClassJobCategory.ValueNullable != null)
        .Where(x => Enum.GetValues<Job>().Where(x => x.IsCombat()).Any(s => x.ClassJobCategory.Value.IsJobInCategory(s)))
        .ToDictionary(x => x.RowId, x=> ExcelActionHelper.GetActionName(x, true) + "##" + x.RowId.ToString())];
    void DrawJabber(ref uint action, ref int delay)
    {
        ImGui.SetNextItemWidth(150f);
        var name = action == 0 ? "Disabled" : ExcelActionHelper.GetActionName(action);
        ImGuiEx.Combo("Select control action", ref action, StunActions.Keys, names: StunActions);
        ImGui.Indent();
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.SliderIntAsFloat("Delay, seconds", ref delay, 0, 30000);
        ImGui.Unindent();
    }
}
