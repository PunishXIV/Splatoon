
> [!IMPORTANT]
> Play around with the projections and use them or blacklist actions as you feel necessary

Darya The Sea-maid

### [Script] Darya Serenade Script - Copy and install from clipboard in scripts section
```
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.Schedulers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.AnotherMerchantTale;

public class Darya_Serenade_Script : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1317];
    public override Metadata? Metadata => new(1, "Poneglyph");

    private bool isWaitingForVFX = false;
    private List<string> capturedElements = new(); 
    private List<TickScheduler> activeSchedulers = new();

    private const string VFX_CHOCOBO = "vfx/common/eff/m0941_chocobo_c0h.avfx";
    private const string VFX_SEAHORSE = "vfx/common/eff/m0941_seahorse_c0h.avfx";
    private const string VFX_PUFFER = "vfx/common/eff/m0941_puffer_c0h.avfx";
    private const string VFX_CRAB = "vfx/common/eff/m0941_crab_c0h.avfx";
    private const string VFX_TURTLE = "vfx/common/eff/m0941_turtle_c0h.avfx";

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Chocobo", "{\"Name\":\"Chocobo\",\"type\":4,\"refY\":40.0,\"radius\":45.0,\"coneAngleMin\":-30,\"coneAngleMax\":30,\"color\":3355462399,\"fillIntensity\":0.8,\"thicc\":0.0,\"refActorModelID\":4777,\"refActorComparisonType\":1,\"includeRotation\":true,\"DistanceSourceX\":375.0,\"DistanceSourceY\":522.0,\"DistanceSourceZ\":-29.5,\"DistanceMax\":13.3}");
        Controller.RegisterElementFromCode("Seahorse", "{\"Name\":\"Seahorse\",\"type\":3,\"refY\":40.0,\"radius\":4.0,\"color\":3355508719,\"fillIntensity\":0.8,\"thicc\":0.0,\"refActorModelID\":4773,\"refActorComparisonType\":1,\"includeRotation\":true,\"DistanceSourceX\":375.0,\"DistanceSourceY\":522.0,\"DistanceSourceZ\":-29.5,\"DistanceMax\":13.3}");
        Controller.RegisterElementFromCode("Puffer", "{\"Name\":\"Puffer\",\"type\":4,\"refY\":40.0,\"radius\":20.0,\"coneAngleMin\":-90,\"coneAngleMax\":90,\"color\":3355479807,\"fillIntensity\":0.8,\"thicc\":0.0,\"refActorModelID\":4778,\"refActorComparisonType\":1,\"includeRotation\":true,\"DistanceSourceX\":375.0,\"DistanceSourceY\":522.0,\"DistanceSourceZ\":-29.5,\"DistanceMax\":13.3}");
        Controller.RegisterElementFromCode("Crab", "{\"Name\":\"Crab\",\"type\":3,\"refY\":40.0,\"radius\":4.0,\"fillIntensity\":0.8,\"thicc\":0.0,\"refActorModelID\":4776,\"refActorComparisonType\":1,\"includeRotation\":true}");
        Controller.RegisterElementFromCode("Turtle", "{\"Name\":\"Turtle\",\"type\":3,\"refY\":40.0,\"radius\":4.0,\"color\":3355508509,\"fillIntensity\":0.8,\"thicc\":0.0,\"refActorModelID\":4775,\"refActorComparisonType\":1,\"includeRotation\":true}");
        OnReset();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 45773)
        {
            isWaitingForVFX = true;
            capturedElements.Clear();
        }

        if (castId == 45844)
        {
            if (capturedElements.Count == 4)
            {
                ExecuteDisplay();
            }
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (!isWaitingForVFX) return;

        string? detected = vfxPath switch
        {
            VFX_CHOCOBO => "Chocobo",
            VFX_SEAHORSE => "Seahorse",
            VFX_PUFFER => "Puffer",
            VFX_CRAB => "Crab",
            VFX_TURTLE => "Turtle",
            _ => null
        };

        if (detected != null)
        {
            capturedElements.Add(detected);

            if (capturedElements.Count == 4)
            {
                isWaitingForVFX = false;
                ExecuteDisplay();
            }
        }
    }

    private void ExecuteDisplay()
    {
        var timings = new (uint StartAt, uint Duration)[]
        {
            (1000, 7000), 
            (8000, 3000), 
            (10500, 3500), 
            (14000, 3000) 
        };

        for (int i = 0; i < capturedElements.Count && i < timings.Length; i++)
        {
            string elementName = capturedElements[i];
            var (startAt, duration) = timings[i];
            int stepNum = i + 1;

            activeSchedulers.Add(new TickScheduler(() =>
            {
                if (Controller.TryGetElementByName(elementName, out var element))
                {
                    element.Enabled = true;

                    activeSchedulers.Add(new TickScheduler(() => 
                    {
                        element.Enabled = false;
                    }, duration));
                }
            }, startAt));
        }
    }

    public override void OnReset()
    {
        isWaitingForVFX = false;
        capturedElements.Clear();
        foreach (var sched in activeSchedulers) sched?.Dispose();
        activeSchedulers.Clear();
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }
}
```

### Defamations
```
~Lv2~{"Name":"Defamations","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"Defamation","type":1,"radius":15.0,"Filled":false,"fillIntensity":0.44,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/lockon3_t0h.avfx","refActorVFXMax":3000}]}
```

### Stack
```
~Lv2~{"Name":"Stack","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"Stack","type":1,"radius":5.0,"color":3355508515,"Filled":false,"fillIntensity":0.5,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[4726]}]}
```

### Swimming in the Air
```
~Lv2~{"Name":"Swimming in the Air","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"Swimming in the Air","type":1,"radius":12.0,"color":4278190335,"fillIntensity":0.3,"refActorNPCID":2015003,"refActorComparisonType":4,"mechanicType":1}]}
```

### Divebomb
```
~Lv2~{"Name":"Divebomb","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"ew","type":3,"refY":-25.0,"offY":25.0,"radius":0.25,"fillIntensity":0.3,"thicc":0.0,"refActorTargetingYou":1,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/bahamut_wyvn_glider_target_02tm.avfx","refActorVFXMax":7000},{"Name":"ns","type":3,"refX":-25.0,"offX":25.0,"radius":0.25,"fillIntensity":0.3,"thicc":0.0,"refActorTargetingYou":1,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/bahamut_wyvn_glider_target_02tm.avfx","refActorVFXMax":7000}]}
```

### Surging Current
```
~Lv2~{"Name":"Surging Current","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"Surging Current","type":4,"radius":30.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.2,"castAnimation":2,"animationColor":2516582655,"thicc":0.0,"refActorNPCNameID":14291,"refActorRequireCast":true,"refActorCastId":[45866],"refActorUseCastTime":true,"refActorCastTimeMax":5.7,"refActorComparisonType":6,"includeRotation":true}]}```
```

Lone Swordmaster

### Malefic sides
```
~Lv2~{"Name":"Malefic Sides","Group":"Another Merchant's Tale","ZoneLockH":[1317],"UseTriggers":true,"Triggers":[{"TimeBegin":60.0}],"ElementsL":[{"Name":"g w","type":3,"refX":-2.0,"refY":-2.0,"offX":-2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorTargetingYou":1,"refActorComparisonType":7,"refActorVFXPath":"vfx/channeling/eff/chn_ambd_w_p.avfx","refActorVFXMax":20000},{"Name":"g e","type":3,"refX":2.0,"refY":-2.0,"offX":2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorTargetingYou":1,"refActorComparisonType":7,"refActorVFXPath":"vfx/channeling/eff/chn_ambd_e_p.avfx","refActorVFXMax":20000},{"Name":"g s","type":3,"refX":-2.0,"refY":2.0,"offX":2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/channeling/eff/chn_ambd_s_p.avfx","refActorVFXMax":20000},{"Name":"g n","type":3,"refX":-2.0,"refY":-2.0,"offX":2.0,"offY":-2.0,"radius":0.2,"fillIntensity":1.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/channeling/eff/chn_ambd_n_p.avfx","refActorVFXMax":20000},{"Name":"n-w n","type":3,"refX":-2.0,"refY":-2.0,"offX":2.0,"offY":-2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[4782]},{"Name":"n-w w","type":3,"refX":-2.0,"refY":-2.0,"offX":-2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorTargetingYou":1,"refActorRequireBuff":true,"refActorBuffId":[4782]},{"Name":"n-e n","type":3,"refX":-2.0,"refY":-2.0,"offX":2.0,"offY":-2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[4781]},{"Name":"n-e e","type":3,"refX":2.0,"refY":-2.0,"offX":2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorTargetingYou":1,"refActorRequireBuff":true,"refActorBuffId":[4781]},{"Name":"s-e e","type":3,"refX":2.0,"refY":-2.0,"offX":2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorTargetingYou":1,"refActorRequireBuff":true,"refActorBuffId":[4777]},{"Name":"s-e s","type":3,"refX":-2.0,"refY":2.0,"offX":2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[4777]},{"Name":"s-w s","type":3,"refX":-2.0,"refY":2.0,"offX":2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[4778]},{"Name":"s-w w","type":3,"refX":-2.0,"refY":-2.0,"offX":-2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorTargetingYou":1,"refActorRequireBuff":true,"refActorBuffId":[4778]},{"Name":"s-e-w","type":4,"refX":-2.0,"refY":-2.0,"radius":2.0,"Donut":0.5,"coneAngleMin":45,"coneAngleMax":315,"color":4294967295,"fillIntensity":1.0,"refActorTargetingYou":1,"refActorPlaceholder":["<1>"],"refActorRequireBuff":true,"refActorBuffId":[4779],"refActorComparisonType":5,"includeRotation":true,"RotationOverride":true,"RotationOverrideAngleOnlyMode":true,"RotationOverridePoint":{}},{"Name":"n-e-w","type":4,"refX":-2.0,"refY":-2.0,"radius":2.0,"Donut":0.5,"coneAngleMin":-135,"coneAngleMax":135,"color":4294967295,"fillIntensity":1.0,"refActorTargetingYou":1,"refActorPlaceholder":["<1>"],"refActorRequireBuff":true,"refActorBuffId":[4783],"refActorComparisonType":5,"includeRotation":true,"RotationOverride":true,"RotationOverrideAngleOnlyMode":true,"RotationOverridePoint":{}},{"Name":"n-s-w","type":4,"refX":-2.0,"refY":-2.0,"radius":2.0,"Donut":0.5,"coneAngleMin":-225,"coneAngleMax":45,"color":4294967295,"fillIntensity":1.0,"refActorTargetingYou":1,"refActorPlaceholder":["<1>"],"refActorRequireBuff":true,"refActorBuffId":[4786],"refActorComparisonType":5,"includeRotation":true,"RotationOverride":true,"RotationOverrideAngleOnlyMode":true,"RotationOverridePoint":{}},{"Name":"n-s-e","type":4,"refX":-2.0,"refY":-2.0,"radius":2.0,"Donut":0.5,"coneAngleMin":-45,"coneAngleMax":225,"color":4294967295,"fillIntensity":1.0,"refActorTargetingYou":1,"refActorPlaceholder":["<1>"],"refActorRequireBuff":true,"refActorBuffId":[4785],"refActorComparisonType":5,"includeRotation":true,"RotationOverride":true,"RotationOverrideAngleOnlyMode":true,"RotationOverridePoint":{}}]}```
```

### Heaven Mechanic
```
~Lv2~{"Name":"Heaven","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"Near to Heaven 2 Swords","type":1,"refActorNPCNameID":14323,"refActorRequireCast":true,"refActorCastId":[47568],"refActorUseCastTime":true,"refActorCastTimeMax":25.0,"refActorUseOvercast":true,"refActorComparisonType":6,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"1","type":1,"radius":8.0,"fillIntensity":0.5,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num01_s5p.avfx","refActorVFXMax":6000},{"Name":"2","type":1,"radius":8.0,"Donut":20.0,"color":4278190335,"fillIntensity":0.2,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num02_s5p.avfx","refActorVFXMin":3500,"refActorVFXMax":25000},{"Name":"Swords","type":1,"radius":5.0,"color":3371412498,"Filled":false,"fillIntensity":0.5,"overlayText":"Stack","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/lockon5_line_1p.avfx","refActorVFXMax":25000},{"Name":"Far from Heaven 2 Swords","type":1,"refActorNPCNameID":14323,"refActorRequireCast":true,"refActorCastId":[47569],"refActorUseCastTime":true,"refActorCastTimeMax":999.0,"refActorUseOvercast":true,"refActorComparisonType":6,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"1","type":1,"radius":8.0,"Donut":20.0,"color":4278190335,"fillIntensity":0.2,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num01_s5p.avfx","refActorVFXMax":6000},{"Name":"2","type":1,"radius":8.0,"fillIntensity":0.5,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num02_s5p.avfx","refActorVFXMin":3500,"refActorVFXMax":25000},{"Name":"Swords","type":1,"radius":5.0,"color":3371412498,"Filled":false,"fillIntensity":0.5,"overlayText":"Stack","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/lockon5_line_1p.avfx","refActorVFXMax":25000},{"Name":"Near to Heaven 1 Sword","type":1,"refActorNPCNameID":14323,"refActorRequireCast":true,"refActorCastId":[47566],"refActorUseCastTime":true,"refActorCastTimeMax":25.0,"refActorUseOvercast":true,"refActorComparisonType":6,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"1","type":1,"radius":8.0,"fillIntensity":0.5,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num01_s5p.avfx","refActorVFXMax":6000},{"Name":"2","type":1,"radius":8.0,"Donut":20.0,"color":4278190335,"fillIntensity":0.2,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num02_s5p.avfx","refActorVFXMin":3500,"refActorVFXMax":25000},{"Name":"Swords","type":1,"radius":5.0,"color":3371412498,"Filled":false,"fillIntensity":0.5,"overlayText":"Go away!","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/lockon5_line_1p.avfx","refActorVFXMax":25000},{"Name":"Far from Heaven 1 Sword","type":1,"refActorNPCNameID":14323,"refActorRequireCast":true,"refActorCastId":[47567],"refActorUseCastTime":true,"refActorCastTimeMax":25.0,"refActorUseOvercast":true,"refActorComparisonType":6,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"1","type":1,"radius":8.0,"Donut":20.0,"color":4278190335,"fillIntensity":0.2,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num01_s5p.avfx","refActorVFXMax":6000},{"Name":"2","type":1,"radius":8.0,"fillIntensity":0.5,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num02_s5p.avfx","refActorVFXMin":3500,"refActorVFXMax":25000},{"Name":"Swords","type":1,"radius":5.0,"color":3371412498,"Filled":false,"fillIntensity":0.5,"overlayText":"Go away!","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/lockon5_line_1p.avfx","refActorVFXMax":25000}]}```
```

### Rock
```
~Lv2~{"Name":"Rock","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"Rock","type":1,"radius":2.5,"color":4278190080,"fillIntensity":1.0,"overrideFillColor":true,"originFillColor":1694498815,"endFillColor":4278190080,"thicc":0.0,"refActorName":"Fallen Rock"}]}```
```

Pari of Plenty

### Icy Bauble
```
~Lv2~{"Name":"Icy Bauble","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"","type":3,"refX":40.0,"offX":-40.0,"radius":5.0,"color":3372191232,"fillIntensity":0.4,"refActorDataID":19059,"refActorComparisonType":3,"onlyVisible":true},{"Name":"","type":3,"refY":40.0,"offY":-40.0,"radius":5.0,"color":3372191232,"fillIntensity":0.4,"refActorDataID":19059,"refActorComparisonType":3,"onlyVisible":true}]}```
```

### Cleaves
```
~Lv2~{"Name":"Cleaves","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"","type":4,"radius":35.0,"coneAngleMin":-90,"coneAngleMax":90,"color":3365338880,"fillIntensity":0.2,"thicc":0.0,"refActorRequireCast":true,"refActorCastId":[45478],"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/lockon/eff/m0973_turning_r_left_8sec_c0e1.avfx","refActorVFXMax":9999000},{"Name":"","type":4,"radius":35.0,"coneAngleMin":90,"coneAngleMax":270,"color":3365338880,"fillIntensity":0.2,"thicc":0.0,"refActorRequireCast":true,"refActorCastId":[45478],"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/lockon/eff/m0973_turning_r_right_8sec_c0e1.avfx","refActorVFXMax":9999000},{"Name":"","type":4,"radius":35.0,"coneAngleMin":-90,"coneAngleMax":90,"color":3365338880,"fillIntensity":0.2,"thicc":0.0,"refActorRequireCast":true,"refActorCastId":[45479],"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/lockon/eff/m0973_turning_right_8sec_c0e1.avfx","refActorVFXMax":9999000},{"Name":"","type":4,"radius":35.0,"coneAngleMin":90,"coneAngleMax":270,"color":3365338880,"fillIntensity":0.2,"thicc":0.0,"refActorRequireCast":true,"refActorCastId":[45479],"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/lockon/eff/m0973_turning_left_8sec_c0e1.avfx","refActorVFXMax":9999000}]}
```
