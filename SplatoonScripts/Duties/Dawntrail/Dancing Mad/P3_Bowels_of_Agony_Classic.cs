using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public class P3_Bowels_of_Agony_Classic : SplatoonScript<P3_Bowels_of_Agony_Classic.Config>
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];

    uint DebuffHeadwind = 1602;
    uint DebuffTailwind = 1603;

    uint DebuffWater = 1601;
    uint DebuffFire = 1600;

    public override void OnSetup()
    {
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"WindBaitTowardsChaosLeft","ZoneLockH":[1363],"ElementsL":[{"Name":"Chaos","type":1,"refActorDataID":19508,"refActorComparisonType":3,"onlyTargetable":true,"IsCapturing":true,"Nodraw":true},{"Name":"Left","type":1,"offX":-6.0,"offY":3.0,"radius":1.0,"color":3356884736,"Filled":false,"fillIntensity":0.5,"overlayVOffset":1.0,"thicc":3.0,"overlayText":"Bait wind","refActorDataID":2015292,"refActorComparisonType":3,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverrideFaceMode":true,"RotationOverrideFaceModePlaceholders":["<element:Chaos>"],"RotationOverridePoint":{}}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"WindBaitTowardsChaosRight","ZoneLockH":[1363],"ElementsL":[{"Name":"Chaos","type":1,"refActorDataID":19508,"refActorComparisonType":3,"onlyTargetable":true,"IsCapturing":true,"Nodraw":true},{"Name":"Right","type":1,"offX":6.0,"offY":3.0,"radius":1.0,"color":3356884736,"Filled":false,"fillIntensity":0.5,"overlayVOffset":1.0,"thicc":3.0,"overlayText":"Bait wind","refActorDataID":2015292,"refActorComparisonType":3,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverrideFaceMode":true,"RotationOverrideFaceModePlaceholders":["<element:Chaos>"],"RotationOverridePoint":{}}]}
            
            """);
            Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"WindBaitTowardsExdeathLeft","ZoneLockH":[1363],"ElementsL":[{"Name":"Exd","type":1,"refActorDataID":19509,"refActorComparisonType":3,"onlyTargetable":true,"IsCapturing":true,"Nodraw":true},{"Name":"Left","type":1,"offX":-6.0,"offY":3.0,"radius":1.0,"color":3356884736,"Filled":false,"fillIntensity":0.5,"overlayVOffset":1.0,"thicc":3.0,"overlayText":"Bait wind","refActorDataID":2015292,"refActorComparisonType":3,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverrideFaceMode":true,"RotationOverrideFaceModePlaceholders":["<element:Exd>"],"RotationOverridePoint":{}}]}
            """);
            Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"WindBaitTowardsExdeathRight","ZoneLockH":[1363],"ElementsL":[{"Name":"Exd","type":1,"refActorDataID":19509,"refActorComparisonType":3,"onlyTargetable":true,"IsCapturing":true,"Nodraw":true},{"Name":"Right","type":1,"offX":6.0,"offY":3.0,"radius":1.0,"color":3356884736,"Filled":false,"fillIntensity":0.5,"overlayVOffset":1.0,"thicc":3.0,"overlayText":"Bait wind","refActorDataID":2015292,"refActorComparisonType":3,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverrideFaceMode":true,"RotationOverrideFaceModePlaceholders":["<element:Exd>"],"RotationOverridePoint":{}}]}
            """);
            Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"BetweenWindFire","ZoneLockH":[1363],"ElementsL":[{"Name":"Pos","type":1,"refActorDataID":2015290,"refActorComparisonType":3,"IsCapturing":true,"Nodraw":true},{"Name":"","type":1,"offY":10.0,"radius":1.0,"color":3355508540,"Filled":false,"fillIntensity":0.5,"overlayVOffset":1.0,"thicc":3.0,"overlayText":"Bring boss here","refActorDataID":2015292,"refActorComparisonType":3,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverrideFaceMode":true,"RotationOverrideFaceModePlaceholders":["<element:Pos>"],"RotationOverridePoint":{}}]}
            """);
            Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"BetweenWindWater","ZoneLockH":[1363],"ElementsL":[{"Name":"Pos","type":1,"refActorDataID":2015291,"refActorComparisonType":3,"IsCapturing":true,"Nodraw":true},{"Name":"","type":1,"offY":10.0,"radius":1.0,"color":3355508540,"Filled":false,"fillIntensity":0.5,"overlayVOffset":1.0,"thicc":3.0,"overlayText":"Bring boss here","refActorDataID":2015292,"refActorComparisonType":3,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverrideFaceMode":true,"RotationOverrideFaceModePlaceholders":["<element:Pos>"],"RotationOverridePoint":{}}]}
            """);
        Controller.RegisterElementsFromMultilineCode("""
            {"Name":"RelposWaterMelee","type":1,"Enabled":false,"offY":7.0,"radius":1.0,"color":3356425984,"Filled":false,"fillIntensity":0.5,"overlayVOffset":1.0,"thicc":3.0,"overlayText":"Bait water","refActorDataID":2015291,"refActorComparisonType":3,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            {"Name":"RelposWaterRanged","type":1,"Enabled":false,"offY":-5.0,"radius":1.0,"color":3356425984,"Filled":false,"fillIntensity":0.5,"overlayVOffset":1.0,"thicc":3.0,"overlayText":"Bait water","refActorDataID":2015291,"refActorComparisonType":3,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            {"Name":"RelposWaterCleanser","type":1,"Enabled":false,"offY":9.0,"radius":1.0,"color":3356425984,"Filled":false,"fillIntensity":0.5,"overlayVOffset":1.0,"thicc":3.0,"overlayText":"Cleanse (water)","refActorDataID":2015291,"refActorComparisonType":3,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            {"Name":"RelposFireMelee","type":1,"Enabled":false,"offY":7.0,"radius":1.0,"color":3356425984,"Filled":false,"fillIntensity":0.5,"overlayVOffset":1.0,"thicc":3.0,"overlayText":"Bait fire","refActorDataID":2015290,"refActorComparisonType":3,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            {"Name":"RelposFireRanged","type":1,"Enabled":false,"offY":-5.0,"radius":1.0,"color":3356425984,"Filled":false,"fillIntensity":0.5,"overlayVOffset":1.0,"thicc":3.0,"overlayText":"Bait fire","refActorDataID":2015290,"refActorComparisonType":3,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            {"Name":"RelposFireCleanser","type":1,"Enabled":false,"offY":14.0,"radius":1.0,"color":3356425984,"Filled":false,"fillIntensity":0.5,"overlayVOffset":1.0,"thicc":3.0,"overlayText":"Cleanse (fire)","refActorDataID":2015290,"refActorComparisonType":3,"includeRotation":true,"tether":true,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            {"Name":"Stack1","type":1,"offY":3.0,"radius":1.0,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":3.0,"refActorNPCNameID":6052,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"tether":true,"AdditionalRotation":0.7853982,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            {"Name":"Stack4","type":1,"offY":3.0,"radius":1.0,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":3.0,"refActorNPCNameID":6052,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"tether":true,"AdditionalRotation":5.497787,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            {"Name":"Stack2","type":1,"offY":3.0,"radius":1.0,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":3.0,"refActorNPCNameID":6052,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"tether":true,"AdditionalRotation":0.2617994,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            {"Name":"Stack3","type":1,"offY":3.0,"radius":1.0,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":3.0,"refActorNPCNameID":6052,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"tether":true,"AdditionalRotation":6.021386,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            """);
    }

    float TimeRemaining
    {
        get
        {
            float time = 0;
            foreach(var x in Svc.Objects.OfType<IPlayerCharacter>())
            {
                if(x.HasStatus([DebuffHeadwind, DebuffTailwind], out var t))
                {
                    time = t[0].Time;
                }
            }
            return time;
        }
    }

    bool? IsFireFirst = false;
    bool? IsDebuff = false;
    bool? IsFireDebuff = false;

    public override void OnReset()
    {
        IsFireFirst = null;
        IsDebuff = null;
        IsFireDebuff = null;
    }

    bool IsDps => BasePlayer.Job.IsDps();
    bool IsTank => BasePlayer.Job.IsTank();

    public override void OnUpdate()
    {
        Controller.Hide();
        if(CurrentPhase == Phase.Initial)
        {
            var fireStatus = Controller.GetPartyMembers().FirstOrDefault(x => x.HasStatus(DebuffFire))!.HasStatus(DebuffFire, out var fireTime);
            var waterStatus = Controller.GetPartyMembers().FirstOrDefault(x => x.HasStatus(DebuffWater))!.HasStatus(DebuffWater, out var waterTime);
            IsFireFirst ??= fireTime < waterTime;
            IsDebuff ??= BasePlayer.HasStatus([DebuffWater, DebuffFire]);
            IsFireDebuff ??= BasePlayer.HasStatus(DebuffFire);
            if(IsDebuff.Value)
            {
                if(IsFireDebuff.Value) //if having fire debuff
                {
                    if(IsFireFirst.Value) //if fire debuff first
                    {
                        Controller.GetElementByName(IsDps ? "RelposFireMelee" : "RelposFireRanged")!.Enabled = true;
                    }
                    else //if fire debuff second
                    {
                        Controller.GetElementByName("RelposFireCleanser")!.Enabled = true;
                    }
                }
                else //if having water debuff
                {
                    if(IsFireFirst.Value) //if water debuff second
                    {
                        Controller.GetElementByName("RelposWaterCleanser")!.Enabled = true;
                    }
                    else // if water debuff first
                    {
                        Controller.GetElementByName(IsDps ? "RelposWaterMelee" : "RelposWaterRanged")!.Enabled = true;
                    }
                }
            }
            else
            {
                if(Controller.TryGetLayoutByName(IsDps ? "WindBaitTowardsChaosRight" : "WindBaitTowardsChaosLeft", out var l))
                {
                    l.Enabled = true;
                }
            }
        }
        if(CurrentPhase == Phase.Tankbuster)
        {
            if(BasePlayer.Job.IsTank() && C.TankingExdeath)
            {
                if(Controller.TryGetLayoutByName(IsFireFirst.Value? "BetweenWindWater": "BetweenWindFire", out var l))
                {
                    l.Enabled = true;
                }
            }
        }
        if(CurrentPhase == Phase.Cleaves || CurrentPhase == Phase.Tankbuster)
        {
            if(IsDebuff.Value)
            {
                if(IsFireDebuff.Value) //if have fire debuff
                {
                    if(IsFireFirst.Value) //if fire debuff first
                    {
                        Controller.GetElementByName("RelposWaterCleanser")!.Enabled = true;
                    }
                    else //if fire second
                    {
                        Controller.GetElementByName(IsDps ? "RelposFireMelee" : "RelposFireRanged")!.Enabled = true;
                    }
                }
                else //if have water
                {
                    if(IsFireFirst.Value) //if water second
                    {
                        Controller.GetElementByName(IsDps ? "RelposWaterMelee" : "RelposWaterRanged")!.Enabled = true;
                    }
                    else //if water first
                    {
                        Controller.GetElementByName("RelposFireCleanser")!.Enabled = true;
                    }
                }
            }
            else
            {
                if(Controller.TryGetLayoutByName(IsDps ? "WindBaitTowardsExdeathRight" : "WindBaitTowardsExdeathLeft", out var l))
                {
                    l.Enabled = true;
                }
            }
        }
        if(CurrentPhase == Phase.Cleansed2)
        {
            if(Controller.TryGetElementByName($"Stack{C.MyStack}", out var e))
            {
                e.Enabled = true;
            }
        }
        if(CurrentPhase == Phase.SmashBaited)
        {
            if(Controller.TryGetElementByName($"Stack{C.MyStack}", out var e))
            {
                e.Enabled = true;
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Will be tanking exdeath after tankbuster", ref C.TankingExdeath);
        ImGui.Checkbox("Will be baiting chaos's jump", ref C.BaitingChaos);
        ImGui.SetNextItemWidth(150f);
        ImGui.SliderInt("My stack group, looking at exdeath, left to right", ref C.MyStack, 1, 4);
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Separator();
            ImGuiEx.Text($"Time:");
            ImGui.SameLine();
            ImGuiEx.TextCopy($"{TimeRemaining:F2}");
            ImGuiEx.Text($"Phase: {CurrentPhase}");
        }
    }

    public class Config
    {
        public bool BaitingChaos = false;
        public bool TankingExdeath = false;
        public int MyStack = 0;
    }

    Phase CurrentPhase
    {
        get
        {
            if(TimeRemaining == 0) return Phase.None;
            if(TimeRemaining < 11.11f) return Phase.SmashBaited;
            if(TimeRemaining < 19.91) return Phase.Cleansed2;
            if(TimeRemaining < 30.18) return Phase.Cleaves;
            if(TimeRemaining < 35.83) return Phase.Tankbuster;
            if(TimeRemaining < 45.20) return Phase.Cleansed1;
            if(TimeRemaining < 64.62) return Phase.Initial;
            return Phase.None;
        }
    }

    public enum Phase
    {
        None,
        /// <summary>
        /// Goal: cleanse long debuffs
        /// </summary>
        Initial,
        /// <summary>
        /// Goal: Tankbuster swap, the party can just circlejerk or something
        /// </summary>
        Cleansed1, //45.20
        /// <summary>
        /// Goal: exdeath to the wall between wind and long debuff crystal, chaos tank faces chaos north, cleanse of short debuffs happens
        /// </summary>
        Tankbuster, //35.83
        Cleaves,
        /// <summary>
        /// Ranged goes and baits bullshit, the rest of the party smokes
        /// </summary>
        Cleansed2, //19.91
        /// <summary>
        /// Form pairs and get knocked back
        /// </summary>
        SmashBaited, //11.11
    }
}
