using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
#pragma warning disable SYSLIB1045
public sealed partial class EXMH_Clamorous_Chase : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1306];

    IPlayerCharacter BasePlayer
    {
        get
        {
            if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback] && C.BPO != "" && Controller.GetPartyMembers().TryGetFirst(x => x.GetNameWithWorld() == C.BPO, out var p))
            {
                return p;
            }
            return Player.Object;
        }
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("West", """{"Name":"","refX":81.0,"refY":100.0,"radius":1.0,"Donut":0.5,"color":3358850816,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("East", """{"Name":"","refX":119.0,"refY":100.0,"radius":1.0,"Donut":0.5,"color":3358850816,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("North", """{"Name":"","refX":100.0,"refY":81.0,"radius":1.0,"Donut":0.5,"color":3358850816,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("South", """{"Name":"","refX":100.0,"refY":119.0,"radius":1.0,"Donut":0.5,"color":3358850816,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("Center", """{"Name":"","refX":100.0,"refY":100.0,"radius":1.0,"Donut":0.5,"color":3358850816,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("MidSouth", """{"Name":"","refX":100.0,"refY":110.0,"radius":1.0,"Donut":0.5,"color":3358850816,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("MidNorth", """{"Name":"","refX":100.0,"refY":90.0,"radius":1.0,"Donut":0.5,"color":3358850816,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("TextStep1", """{"Name":"","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4280418048,"overlayVOffset":2.0,"thicc":0.0,"overlayText":"Wait...","refActorType":1}""");
        Controller.RegisterElementFromCode("TextStep2", """{"Name":"","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4278252031,"overlayVOffset":2.0,"overlayFScale":1.5,"thicc":0.0,"overlayText":"! Get Ready !","refActorType":1}""");
        Controller.RegisterElementFromCode("TextStep3", """{"Name":"","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4278190335,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"!!! BAIT !!!","refActorType":1}""");
        Controller.RegisterElementFromCode("TextStep4", """{"Name":"","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294902005,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"!!! Return !!!","refActorType":1}""");
        Controller.RegisterElementFromCode("Number", """{"Name":"","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":4294963968,"overlayVOffset":1.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3","refActorType":1}""");
        Controller.RegisterElementFromCode("AOELkL", """{"Name":"","type":5,"refX":98.40646,"refY":98.1911,"refZ":2.861023E-06,"radius":40.0,"coneAngleMin":90,"coneAngleMax":270,"fillIntensity":0.4,"includeRotation":true}""");
        Controller.RegisterElementFromCode("AOEL", """{"Name":"","type":5,"refX":98.40646,"refY":98.1911,"refZ":2.861023E-06,"radius":40.0,"coneAngleMin":90,"coneAngleMax":270,"color":3355506687,"fillIntensity":0.4,"includeRotation":true}""");
        Controller.RegisterElementFromCode("AOELkR", """{"Name":"","type":5,"refX":98.40646,"refY":98.1911,"refZ":2.861023E-06,"radius":40.0,"coneAngleMin":-90,"coneAngleMax":90,"fillIntensity":0.4,"includeRotation":true}""");
        Controller.RegisterElementFromCode("AOER", """{"Name":"","type":5,"refX":98.40646,"refY":98.1911,"refZ":2.861023E-06,"radius":40.0,"coneAngleMin":-90,"coneAngleMax":90,"color":3355506687,"fillIntensity":0.4,"includeRotation":true}""");
    }

    Dictionary<uint, int> Marks = [];
    uint RefCol;
    int CurrentTarget = 1;
    int CurrentCleaveTarget = 1;
    bool PositionLocked = false;

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(IsReversed == null) return;
        if(MyNumber == 0 || CurrentTarget > 8) return;
        ResolveDiamond();
        ResolveAoe();
    }

    int MyNumber => Marks.SafeSelect(BasePlayer.EntityId);
    int Target => (MyNumber - CurrentTarget) switch
    {
        < 0 => 4,
        <= 3 => 3,
        4 => 2,
        _ => 1
    };

    bool? IsReversed;

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 43955)
        {
            IsReversed = false;
        }
        if(castId == 43958)
        {
            IsReversed = true;
        }
    }

    void ResolveAoe()
    {
        var npc = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.IsTargetable && x.NameId == 14237);
        var player = this.Marks.FirstOrDefault(x => x.Value == CurrentCleaveTarget).Key.GetObject();
        if(npc != null && (player != null || PositionLocked))
        {
            var e = Controller.GetElementByName("AOE" + (CurrentTarget == CurrentCleaveTarget ? "" : "Lk") + (IsReversed == true ? "L" : "R"))!;
            var elk = Controller.GetElementByName("AOELk" + (IsReversed == true ? "L" : "R"))!;
            (CurrentCleaveTarget == CurrentCleaveTarget?e:elk).Enabled = true;
            if(!PositionLocked)
            {
                e.SetRefPosition(player.Position);
                elk.SetRefPosition(player.Position);
                e.AdditionalRotation = MathHelper.GetAngleBetweenPoints(player.Position.ToVector2(), npc.Position.ToVector2());
                elk.AdditionalRotation = e.AdditionalRotation;
            }
        }
    }

    void ResolveDiamond()
    {
        if(Target != 3)
        {
            Controller.GetElementByName("Center")!.Enabled = true;
        }
        else
        {
            Controller.GetElementByName((MyNumber < 5? MyNumber : MyNumber - 4) switch
            {
                1 => IsReversed != true?"East":"West",
                2 => "South",
                3 => IsReversed != true?"West":"East",
                4 => "North",
            })!.Enabled = true;
        }
        if(Target != 4 || Vector2.Distance(BasePlayer.Position.ToVector2(), new Vector2(100, 100)) > 10f)
        {
            Controller.GetElementByName($"TextStep{Target}")!.Enabled = true;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action != null && set.Source is not IPlayerCharacter)
        {
            PluginLog.Information($"{ExcelActionHelper.GetActionName(set.Action.Value.RowId, true)} from {set.Source}");
            if(set.Action.Value.RowId == 43956  //cleaving right
                || set.Action.Value.RowId == 43959) //cleaving left
            {
                CurrentTarget++;
                PositionLocked = true;
            }
            if(set.Action.Value.RowId == 43957  //cleaving right
                || set.Action.Value.RowId == 43960) //cleaving left
            {
                CurrentCleaveTarget++;
                PositionLocked = false;
            }
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        var num = ExtractTargetNumber(vfxPath);
        if(target.GetObject() is IPlayerCharacter pc && num != null)
        {
            PluginLog.Information($"{pc.GetNameWithWorld()} = {num}");
            Marks[target] = num.Value;
        }
    }

    public override void OnReset()
    {
        CurrentTarget = 1;
        CurrentCleaveTarget = 1;
        Marks.Clear();
        IsReversed = null;
        PositionLocked = false;
    }

    Regex ExtractTargetNumberRegex = new Regex(@"^vfx/lockon/eff/m0361trg_b([1-8])t\.avfx$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    public int? ExtractTargetNumber(string path)
    {
        var match = ExtractTargetNumberRegex.Match(path);

        if(match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }

        return null;
    }

    Config C => Controller.GetConfig<Config>();
    public override void OnSettingsDraw()
    {
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"""
                CurrentCleaveTarget {this.CurrentCleaveTarget}
                CurrentTarget {this.CurrentTarget}
                IsReversed {IsReversed}
                """);
            ImGui.InputText("BPO", ref C.BPO);
            if(ImGui.BeginCombo("##sel", "Select base player"))
            {
                foreach(var x in Controller.GetPartyMembers())
                {
                    if(ImGuiEx.Selectable($"{x.GetNameWithWorld()}"))
                    {
                        C.BPO = x.GetNameWithWorld();
                    }
                }
                ImGui.EndCombo();
            }
        }
    }

    public class Config : IEzConfig
    {
        public string BPO = "";
    }
}