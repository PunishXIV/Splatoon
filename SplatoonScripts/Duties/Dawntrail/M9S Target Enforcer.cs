using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using static Splatoon.Splatoon;


namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M9S_Target_Enforcer : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1321];

    public enum Enemies
    {
        Coffinmaker = 14301,
        Vamp_Fatale = 14300,
        Deadly_Doornail = 14303,
        Fatal_Flail = 14302,
    }

    string LastMsg;

    public override void OnUpdate()
    {
        if(!C.NoEarly || Controller.CombatSeconds > 60f || Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback])
        {
            if(EzThrottler.Throttle(this.InternalData.FullName, 125))
            {
                var wt = GetWantedTarget();
                if(wt != null)
                {
                    if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
                    {
                        Svc.Targets.Target = wt;
                    }
                    else
                    {
                        var msg = $"Would target {wt}";
                        if(LastMsg != msg)
                        {
                            LastMsg = msg;
                            DuoLog.Information(msg);
                        }
                    }
                }
            }
        }
    }

    public override void OnReset()
    {
        LastMsg = null;
    }

    IBattleNpc? GetWantedTarget()
    {
        if(C.DontSwitchOffPlayers && Svc.Targets.Target is IPlayerCharacter) return null;
        if(C.DontSwitchWhenSoftTarget && Svc.Targets.SoftTarget != null) return null;
        if(C.OnlyNullTarget && Svc.Targets.Target != null) return null;
        foreach(var e in C.List)
        {
            var candidates = Svc.Objects.OfType<IBattleNpc>().Where(x => x.IsTargetable && !x.IsDead && Vector3.Distance(x.Position, BasePlayer.Position) < C.Range + BasePlayer.HitboxRadius + x.HitboxRadius && x.NameId == (uint)e).OrderBy(x => Vector3.Distance(BasePlayer.Position, x.Position));
            if(candidates.Any())
            {
                if(Svc.Targets.Target is IBattleNpc b && b.NameId == (uint)e && Vector3.Distance(b.Position, BasePlayer.Position) < C.Range + BasePlayer.HitboxRadius + b.HitboxRadius)
                {
                    return null;
                }
                return candidates.First();
            }
        }
        return null;
    }

    ImGuiEx.RealtimeDragDrop<Enemies> DragDrop = new("Enemies", x => x.ToString());

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text("Due to specifics of target manipulation, this script does not support base player override feature.");
        ImGui.Separator();
        ImGuiEx.Text("Order:");
        DragDrop.Begin();
        for(int i = 0; i < C.List.Count; i++)
        {
            Enemies x = C.List[i];
            DragDrop.NextRow();
            DragDrop.DrawButtonDummy(x.ToString(), C.List, i);
            ImGui.SameLine();
            ImGuiEx.TextV(x.ToString());
        }
        DragDrop.End();
        ImGui.Separator();
        ImGui.Checkbox("Don't switch off players (healers/target buffers w/o reaction enable)", ref C.DontSwitchOffPlayers);
        ImGui.Checkbox("Don't switch off when have soft target", ref C.DontSwitchWhenSoftTarget);
        ImGui.Checkbox("Only target if no other target is set at all", ref C.OnlyNullTarget);
        ImGui.Checkbox("Don't enforce first 60 seconds of combat", ref C.NoEarly);
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.SliderFloat("Max range (double-click to input value)", ref C.Range, 3f, 25f);
        ImGui.Separator();
        ImGuiEx.Text($"Wanted target: {GetWantedTarget()}");
    }

    public class Config : IEzConfig
    {
        public List<Enemies> List = [Enemies.Deadly_Doornail, Enemies.Fatal_Flail, Enemies.Coffinmaker, Enemies.Vamp_Fatale];
        public bool DontSwitchOffPlayers = false;
        public float Range = 25f;
        public bool DontSwitchWhenSoftTarget = false;
        public bool OnlyNullTarget = false;
        public bool NoEarly = true;
    }
    public Config C => Controller.GetConfig<Config>();
}
