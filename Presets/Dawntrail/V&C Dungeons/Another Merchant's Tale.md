
> [!IMPORTANT]
> Play around with the projections and use them or blacklist actions as you feel necessary

Darya The Sea-maid

### [Script] Darya Serenade Script
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Darya_Serenade_Script.cs
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
~Lv2~{"Name":"Surging Current","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"Surging Current","type":4,"radius":30.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.2,"castAnimation":2,"animationColor":2516582655,"thicc":0.0,"refActorNPCNameID":14291,"refActorRequireCast":true,"refActorCastId":[45866],"refActorUseCastTime":true,"refActorCastTimeMax":5.7,"refActorComparisonType":6,"includeRotation":true}]}
```

Lone Swordmaster

### [Script] Malefic Quartering 3 - Copy and install from clipboard in scripts section
```
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.GameFunctions;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.AnotherMerchantTale;

public class Malefic_Quartering_3 : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1317];
    public override Metadata? Metadata => new(1, "Poneglyph");

    private bool isActive = false;
    private uint pdebuff = 0;
    private int mechanicStep = 1;
    private HashSet<uint> procslashes = [];
    private List<Vector3> forces = [];
    private string currentElement = "";
    private string activeGreenTether = "";
    private DateTime? mechanicEndTime = null;
    
    private int? step4PSlot = null;
    private int? step4SSlot = null;

    private readonly Vector3 N1 = new(165f, -16f, -835f);
    private readonly Vector3 N2 = new(175f, -16f, -835f);
    private readonly Vector3 W1 = new(150f, -16f, -820f);
    private readonly Vector3 W2 = new(150f, -16f, -810f);
    private readonly Vector3 E1 = new(190f, -16f, -820f);
    private readonly Vector3 E2 = new(190f, -16f, -810f);
    private readonly Vector3 S1 = new(165f, -16f, -795f);
    private readonly Vector3 S2 = new(175f, -16f, -795f);

    public override void OnSetup()
    {
        RegisterElements();
        OnReset();
    }

    private void RegisterElements()
    {
        Controller.RegisterElementFromCode("Tether NW", """{"Name":"Tether NW","refX":165.0,"refY":-820.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether NE", """{"Name":"Tether NE","refX":175.0,"refY":-820.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether SW", """{"Name":"Tether SW","refX":165.0,"refY":-810.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether SE", """{"Name":"Tether SE","refX":175.0,"refY":-810.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether N", """{"Name":"Tether N","refX":164.5,"refY":-830.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether E", """{"Name":"Tether E","refX":185.0,"refY":-820.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether S", """{"Name":"Tether S","refX":175.0,"refY":-800.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether W", """{"Name":"Tether W","refX":155.0,"refY":-810.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 46693) { OnReset(); isActive = true; }
    }

    public override void OnUpdate()
    {
        if (!isActive) return;

        if (mechanicEndTime.HasValue && (DateTime.Now - mechanicEndTime.Value).TotalSeconds >= 10)
        {
            OnReset();
            return;
        }

        if (pdebuff == 0 && Svc.ClientState.LocalPlayer is { } p)
        {
            var status = p.StatusList;
            if (status.Any(s => s.StatusId == 4782)) pdebuff = 4782;
            else if (status.Any(s => s.StatusId == 4781)) pdebuff = 4781;
            else if (status.Any(s => s.StatusId == 4778)) pdebuff = 4778;
            else if (status.Any(s => s.StatusId == 4777)) pdebuff = 4777;
        }

        var slashes = Svc.Objects.Where(o => o.DataId == 19227 && !procslashes.Contains((uint)o.GameObjectId)).ToList();
        foreach (var obj in slashes)
        {
            procslashes.Add((uint)obj.GameObjectId);
            forces.Add(obj.Position);
            if (forces.Count >= 4) { ResolveForces(); forces.Clear(); }
        }

        if (mechanicStep == 4 && string.IsNullOrEmpty(currentElement) && !string.IsNullOrEmpty(activeGreenTether))
        {
            ResolveStep4();
        }

        string[] allTethers = { "Tether NW", "Tether NE", "Tether SW", "Tether SE", "Tether N", "Tether E", "Tether S", "Tether W" };
        
        foreach (var name in allTethers)
        {
            if (Controller.GetElementByName(name) is { } e)
            {
                e.Enabled = (name == currentElement);
                if (e.Enabled)
                {
                    e.color = GetRainbowColor(1f).ToUint();
                }
            }
        }
    }

    public Vector4 GetRainbowColor(double cycleSeconds)
    {
        if(cycleSeconds <= 0d)
        {
            cycleSeconds = 1d;
        }

        var ms = Environment.TickCount64;
        var t = (ms / 1000d) / cycleSeconds;
        var hue = t % 1f;
        return HsvToVector4(hue, 1f, 1f);
    }

    public static Vector4 HsvToVector4(double h, double s, double v)
    {
        double r = 0f, g = 0f, b = 0f;
        var i = (int)(h * 6f);
        var f = h * 6f - i;
        var p = v * (1f - s);
        var q = v * (1f - f * s);
        var t = v * (1f - (1f - f) * s);

        switch(i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new Vector4((float)r, (float)g, (float)b, 1f);
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (!isActive || Svc.ClientState.LocalPlayer == null) return;
        if (target != (uint)Svc.ClientState.LocalPlayer.GameObjectId) return;
        if (vfxPath.Contains("chn_ambd_n_p")) activeGreenTether = "N";
        else if (vfxPath.Contains("chn_ambd_s_p")) activeGreenTether = "S";
        else if (vfxPath.Contains("chn_ambd_e_p")) activeGreenTether = "E";
        else if (vfxPath.Contains("chn_ambd_w_p")) activeGreenTether = "W";
    }

    private void ResolveForces()
    {
        int? pSlot = null; int? sSlot = null;
        foreach (var pos in forces)
        {
            switch (pdebuff)
            {
                case 4782: if (Vector3.Distance(pos, S1) < 3f) pSlot = 1; if (Vector3.Distance(pos, S2) < 3f) pSlot = 2; if (Vector3.Distance(pos, E1) < 3f) sSlot = 1; if (Vector3.Distance(pos, E2) < 3f) sSlot = 2; break;
                case 4781: if (Vector3.Distance(pos, S1) < 3f) pSlot = 1; if (Vector3.Distance(pos, S2) < 3f) pSlot = 2; if (Vector3.Distance(pos, W1) < 3f) sSlot = 1; if (Vector3.Distance(pos, W2) < 3f) sSlot = 2; break;
                case 4778: if (Vector3.Distance(pos, N1) < 3f) pSlot = 1; if (Vector3.Distance(pos, N2) < 3f) pSlot = 2; if (Vector3.Distance(pos, E1) < 3f) sSlot = 1; if (Vector3.Distance(pos, E2) < 3f) sSlot = 2; break;
                case 4777: if (Vector3.Distance(pos, N1) < 3f) pSlot = 1; if (Vector3.Distance(pos, N2) < 3f) pSlot = 2; if (Vector3.Distance(pos, W1) < 3f) sSlot = 1; if (Vector3.Distance(pos, W2) < 3f) sSlot = 2; break;
            }
        }
        if (mechanicStep <= 3) {
            if (pSlot == 1 && sSlot == 1) currentElement = "Tether NW";
            else if (pSlot == 1 && sSlot == 2) currentElement = "Tether SW";
            else if (pSlot == 2 && sSlot == 1) currentElement = "Tether NE";
            else if (pSlot == 2 && sSlot == 2) currentElement = "Tether SE";
            mechanicStep++;
        }
        else if (mechanicStep == 4) { step4PSlot = pSlot; step4SSlot = sSlot; ResolveStep4(); }
    }

    private void ResolveStep4()
    {
        if (string.IsNullOrEmpty(activeGreenTether) || step4PSlot == null || step4SSlot == null) return;
        switch (pdebuff)
        {
            case 4777: if (activeGreenTether == "N") currentElement = step4SSlot == 2 ? "Tether W" : "Tether E"; else if (activeGreenTether == "W") currentElement = step4PSlot == 2 ? "Tether S" : "Tether N"; break;
            case 4778: if (activeGreenTether == "N") currentElement = step4SSlot == 2 ? "Tether W" : "Tether E"; else if (activeGreenTether == "E") currentElement = step4PSlot == 2 ? "Tether S" : "Tether N"; break;
            case 4782: if (activeGreenTether == "S") currentElement = step4SSlot == 2 ? "Tether W" : "Tether E"; else if (activeGreenTether == "E") currentElement = step4PSlot == 2 ? "Tether S" : "Tether N"; break;
            case 4781: if (activeGreenTether == "S") currentElement = step4SSlot == 2 ? "Tether W" : "Tether E"; else if (activeGreenTether == "W") currentElement = step4PSlot == 2 ? "Tether S" : "Tether N"; break;
        }
        if (!string.IsNullOrEmpty(currentElement)) { mechanicStep++; mechanicEndTime = DateTime.Now; }
    }

    public override void OnReset()
    {
        isActive = false; pdebuff = 0; mechanicStep = 1; activeGreenTether = "";
        step4PSlot = null; step4SSlot = null; mechanicEndTime = null;
        procslashes.Clear(); forces.Clear(); currentElement = "";
        string[] all = { "Tether NW", "Tether NE", "Tether SW", "Tether SE", "Tether N", "Tether E", "Tether S", "Tether W" };
        foreach (var name in all) if (Controller.GetElementByName(name) is { } e) e.Enabled = false;
    }
}
```

### Malefic sides
```
~Lv2~{"Name":"Malefic Sides","Group":"Another Merchant's Tale","ZoneLockH":[1317],"UseTriggers":true,"Triggers":[{"TimeBegin":60.0}],"ElementsL":[{"Name":"g w","type":3,"refX":-2.0,"refY":-2.0,"offX":-2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorTargetingYou":1,"refActorComparisonType":7,"refActorVFXPath":"vfx/channeling/eff/chn_ambd_w_p.avfx","refActorVFXMax":20000},{"Name":"g e","type":3,"refX":2.0,"refY":-2.0,"offX":2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorTargetingYou":1,"refActorComparisonType":7,"refActorVFXPath":"vfx/channeling/eff/chn_ambd_e_p.avfx","refActorVFXMax":20000},{"Name":"g s","type":3,"refX":-2.0,"refY":2.0,"offX":2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/channeling/eff/chn_ambd_s_p.avfx","refActorVFXMax":20000},{"Name":"g n","type":3,"refX":-2.0,"refY":-2.0,"offX":2.0,"offY":-2.0,"radius":0.2,"fillIntensity":1.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/channeling/eff/chn_ambd_n_p.avfx","refActorVFXMax":20000},{"Name":"n-w n","type":3,"refX":-2.0,"refY":-2.0,"offX":2.0,"offY":-2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[4782]},{"Name":"n-w w","type":3,"refX":-2.0,"refY":-2.0,"offX":-2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorTargetingYou":1,"refActorRequireBuff":true,"refActorBuffId":[4782]},{"Name":"n-e n","type":3,"refX":-2.0,"refY":-2.0,"offX":2.0,"offY":-2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[4781]},{"Name":"n-e e","type":3,"refX":2.0,"refY":-2.0,"offX":2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorTargetingYou":1,"refActorRequireBuff":true,"refActorBuffId":[4781]},{"Name":"s-e e","type":3,"refX":2.0,"refY":-2.0,"offX":2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorTargetingYou":1,"refActorRequireBuff":true,"refActorBuffId":[4777]},{"Name":"s-e s","type":3,"refX":-2.0,"refY":2.0,"offX":2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[4777]},{"Name":"s-w s","type":3,"refX":-2.0,"refY":2.0,"offX":2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[4778]},{"Name":"s-w w","type":3,"refX":-2.0,"refY":-2.0,"offX":-2.0,"offY":2.0,"radius":0.2,"fillIntensity":1.0,"refActorName":"*","refActorTargetingYou":1,"refActorRequireBuff":true,"refActorBuffId":[4778]},{"Name":"s-e-w","type":4,"refX":-2.0,"refY":-2.0,"radius":2.0,"Donut":0.5,"coneAngleMin":45,"coneAngleMax":315,"color":4294967295,"fillIntensity":1.0,"refActorTargetingYou":1,"refActorPlaceholder":["<1>"],"refActorRequireBuff":true,"refActorBuffId":[4779],"refActorComparisonType":5,"includeRotation":true,"RotationOverride":true,"RotationOverrideAngleOnlyMode":true,"RotationOverridePoint":{}},{"Name":"n-e-w","type":4,"refX":-2.0,"refY":-2.0,"radius":2.0,"Donut":0.5,"coneAngleMin":-135,"coneAngleMax":135,"color":4294967295,"fillIntensity":1.0,"refActorTargetingYou":1,"refActorPlaceholder":["<1>"],"refActorRequireBuff":true,"refActorBuffId":[4783],"refActorComparisonType":5,"includeRotation":true,"RotationOverride":true,"RotationOverrideAngleOnlyMode":true,"RotationOverridePoint":{}},{"Name":"n-s-w","type":4,"refX":-2.0,"refY":-2.0,"radius":2.0,"Donut":0.5,"coneAngleMin":-225,"coneAngleMax":45,"color":4294967295,"fillIntensity":1.0,"refActorTargetingYou":1,"refActorPlaceholder":["<1>"],"refActorRequireBuff":true,"refActorBuffId":[4786],"refActorComparisonType":5,"includeRotation":true,"RotationOverride":true,"RotationOverrideAngleOnlyMode":true,"RotationOverridePoint":{}},{"Name":"n-s-e","type":4,"refX":-2.0,"refY":-2.0,"radius":2.0,"Donut":0.5,"coneAngleMin":-45,"coneAngleMax":225,"color":4294967295,"fillIntensity":1.0,"refActorTargetingYou":1,"refActorPlaceholder":["<1>"],"refActorRequireBuff":true,"refActorBuffId":[4785],"refActorComparisonType":5,"includeRotation":true,"RotationOverride":true,"RotationOverrideAngleOnlyMode":true,"RotationOverridePoint":{}}]}
```

### Heaven Mechanic
```
~Lv2~{"Name":"Heaven","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"Near to Heaven 2 Swords","type":1,"refActorNPCNameID":14323,"refActorRequireCast":true,"refActorCastId":[47568],"refActorUseCastTime":true,"refActorCastTimeMax":25.0,"refActorUseOvercast":true,"refActorComparisonType":6,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"1","type":1,"radius":8.0,"fillIntensity":0.5,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num01_s5p.avfx","refActorVFXMax":6000},{"Name":"2","type":1,"radius":8.0,"Donut":20.0,"color":4278190335,"fillIntensity":0.2,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num02_s5p.avfx","refActorVFXMin":3500,"refActorVFXMax":25000},{"Name":"Swords","type":1,"radius":5.0,"color":3371412498,"Filled":false,"fillIntensity":0.5,"overlayText":"Stack","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/lockon5_line_1p.avfx","refActorVFXMax":25000},{"Name":"Far from Heaven 2 Swords","type":1,"refActorNPCNameID":14323,"refActorRequireCast":true,"refActorCastId":[47569],"refActorUseCastTime":true,"refActorCastTimeMax":999.0,"refActorUseOvercast":true,"refActorComparisonType":6,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"1","type":1,"radius":8.0,"Donut":20.0,"color":4278190335,"fillIntensity":0.2,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num01_s5p.avfx","refActorVFXMax":6000},{"Name":"2","type":1,"radius":8.0,"fillIntensity":0.5,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num02_s5p.avfx","refActorVFXMin":3500,"refActorVFXMax":25000},{"Name":"Swords","type":1,"radius":5.0,"color":3371412498,"Filled":false,"fillIntensity":0.5,"overlayText":"Stack","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/lockon5_line_1p.avfx","refActorVFXMax":25000},{"Name":"Near to Heaven 1 Sword","type":1,"refActorNPCNameID":14323,"refActorRequireCast":true,"refActorCastId":[47566],"refActorUseCastTime":true,"refActorCastTimeMax":25.0,"refActorUseOvercast":true,"refActorComparisonType":6,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"1","type":1,"radius":8.0,"fillIntensity":0.5,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num01_s5p.avfx","refActorVFXMax":6000},{"Name":"2","type":1,"radius":8.0,"Donut":20.0,"color":4278190335,"fillIntensity":0.2,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num02_s5p.avfx","refActorVFXMin":3500,"refActorVFXMax":25000},{"Name":"Swords","type":1,"radius":5.0,"color":3371412498,"Filled":false,"fillIntensity":0.5,"overlayText":"Go away!","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/lockon5_line_1p.avfx","refActorVFXMax":25000},{"Name":"Far from Heaven 1 Sword","type":1,"refActorNPCNameID":14323,"refActorRequireCast":true,"refActorCastId":[47567],"refActorUseCastTime":true,"refActorCastTimeMax":25.0,"refActorUseOvercast":true,"refActorComparisonType":6,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"1","type":1,"radius":8.0,"Donut":20.0,"color":4278190335,"fillIntensity":0.2,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num01_s5p.avfx","refActorVFXMax":6000},{"Name":"2","type":1,"radius":8.0,"fillIntensity":0.5,"thicc":0.0,"refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num02_s5p.avfx","refActorVFXMin":3500,"refActorVFXMax":25000},{"Name":"Swords","type":1,"radius":5.0,"color":3371412498,"Filled":false,"fillIntensity":0.5,"overlayText":"Go away!","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/lockon5_line_1p.avfx","refActorVFXMax":25000}]}
```

### Rock
```
~Lv2~{"Name":"Rock","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"Rock","type":1,"radius":2.5,"color":4278190080,"fillIntensity":1.0,"overrideFillColor":true,"originFillColor":1694498815,"endFillColor":4278190080,"thicc":0.0,"refActorName":"Fallen Rock"}]}
```

### Malefic 4
```
~Lv2~{"Name":"Malefic 4 - NSW","Group":"MT Criterion","ZoneLockH":[1317],"ConditionalAnd":true,"FreezeDisplayDelay":5.0,"ElementsL":[{"Name":"Debuff Check - NSW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4786],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"East 1","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"North 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":164.55246,"refY":-817.1402,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4786],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"East 1","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"South 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":164.5,"refY":-824.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4786],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"East 2","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"South 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":180.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":176.0,"refY":-813.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4786],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"East 2","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"North 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":180.5,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":175.5,"refY":-806.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4786],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"East 1","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"North 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":180.5,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":175.5,"refY":-817.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4786],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"East 1","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"South 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":180.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":175.5,"refY":-824.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4786],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"East 2","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"North 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":164.5,"refY":-806.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4786],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"East 2","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"South 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":164.5,"refY":-813.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1}]}
~Lv2~{"Name":"Malefic 4 - NSE","Group":"MT Criterion","ZoneLockH":[1317],"ConditionalAnd":true,"FreezeDisplayDelay":5.0,"ElementsL":[{"Name":"Debuff Check - NSE","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4785],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"West 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"North 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":164.55246,"refY":-817.1402,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSE","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4785],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"West 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"South 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":164.5,"refY":-824.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSE","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4785],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"West 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"South 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":180.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":176.0,"refY":-813.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSE","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4785],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"West 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"North 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":180.5,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":175.5,"refY":-806.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSE","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4785],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"West 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"North 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":180.5,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":175.5,"refY":-817.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSE","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4785],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"West 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"South 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":180.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":175.5,"refY":-824.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSE","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4785],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"West 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"North 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":164.5,"refY":-806.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NSE","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4785],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"West 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"South 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":164.5,"refY":-813.0,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1}]}
~Lv2~{"Name":"Malefic 4 - SEW","Group":"MT Criterion","ZoneLockH":[1317],"ConditionalAnd":true,"FreezeDisplayDelay":6.0,"ElementsL":[{"Name":"Debuff Check - SEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4779],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"North 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"East 1","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":161.0,"refY":-820.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - SEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4779],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"North 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"West 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":168.0,"refY":-820.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - SEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4779],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"North 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":180.5,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"East 2","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":172.5,"refY":-809.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - SEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4779],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"North 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":180.5,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"West 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":178.5,"refY":-809.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - SEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4779],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"North 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":180.5,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"East 1","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":172.5,"refY":-820.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - SEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4779],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"North 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":180.5,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"West 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":178.5,"refY":-820.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - SEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4779],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"North 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"East 2","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":161.5,"refY":-809.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - SEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4779],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"North 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-835.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"West 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":168.0,"refY":-809.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1}]}
~Lv2~{"Name":"Malefic 4 - NEW","Group":"MT Criterion","ZoneLockH":[1317],"ConditionalAnd":true,"FreezeDisplayDelay":5.0,"ElementsL":[{"Name":"Debuff Check - NEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4783],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"South 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"East 1","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":161.0,"refY":-820.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4783],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"South 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"West 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":168.0,"refY":-820.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4783],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"South 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":180.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"East 2","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":172.5,"refY":-809.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4783],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"South 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":180.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"West 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":178.5,"refY":-809.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4783],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"South 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":180.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"East 1","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":172.5,"refY":-820.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4783],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"South 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":180.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock NE","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":175.5,"DistanceSourceY":-820.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"West 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-825.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":178.5,"refY":-820.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4783],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"South 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"East 2","type":3,"offY":40.0,"radius":5.0,"color":3355508552,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":190.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":161.5,"refY":-809.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1},{"Name":"Debuff Check - NEW","type":1,"color":3355508540,"fillIntensity":0.5,"refActorRequireBuff":true,"refActorBuffId":[4783],"refActorType":1,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"South 1","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":160.0,"DistanceSourceY":-795.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Rock SW","type":1,"refActorDataID":19229,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":164.5,"DistanceSourceY":-809.5,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"West 2","type":3,"refY":10.0,"offY":30.0,"radius":5.0,"fillIntensity":0.5,"refActorModelID":4763,"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.5,"refActorComparisonType":1,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":150.0,"DistanceSourceY":-805.0,"DistanceSourceZ":-16.0,"DistanceMax":3.0,"Conditional":true,"Nodraw":true},{"Name":"Safespot","refX":168.0,"refY":-809.5,"refZ":-16.0,"radius":1.0,"color":3355508509,"fillIntensity":0.5,"tether":true,"LineEndA":1}]}
```

Pari of Plenty

### Icy Bauble
```
~Lv2~{"Name":"Icy Bauble","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"","type":3,"refX":40.0,"offX":-40.0,"radius":5.0,"color":3372191232,"fillIntensity":0.4,"refActorDataID":19059,"refActorComparisonType":3,"onlyVisible":true},{"Name":"","type":3,"refY":40.0,"offY":-40.0,"radius":5.0,"color":3372191232,"fillIntensity":0.4,"refActorDataID":19059,"refActorComparisonType":3,"onlyVisible":true}]}
```

### Cleaves
```
~Lv2~{"Name":"Cleaves","Group":"Another Merchant's Tale","ZoneLockH":[1317],"ElementsL":[{"Name":"","type":4,"radius":35.0,"coneAngleMin":-90,"coneAngleMax":90,"color":3365338880,"fillIntensity":0.2,"thicc":0.0,"refActorRequireCast":true,"refActorCastId":[45478],"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/lockon/eff/m0973_turning_r_left_8sec_c0e1.avfx","refActorVFXMax":9999000},{"Name":"","type":4,"radius":35.0,"coneAngleMin":90,"coneAngleMax":270,"color":3365338880,"fillIntensity":0.2,"thicc":0.0,"refActorRequireCast":true,"refActorCastId":[45478],"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/lockon/eff/m0973_turning_r_right_8sec_c0e1.avfx","refActorVFXMax":9999000},{"Name":"","type":4,"radius":35.0,"coneAngleMin":-90,"coneAngleMax":90,"color":3365338880,"fillIntensity":0.2,"thicc":0.0,"refActorRequireCast":true,"refActorCastId":[45479],"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/lockon/eff/m0973_turning_right_8sec_c0e1.avfx","refActorVFXMax":9999000},{"Name":"","type":4,"radius":35.0,"coneAngleMin":90,"coneAngleMax":270,"color":3365338880,"fillIntensity":0.2,"thicc":0.0,"refActorRequireCast":true,"refActorCastId":[45479],"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/lockon/eff/m0973_turning_left_8sec_c0e1.avfx","refActorVFXMax":9999000}]}
```

### [Script] Pari Carpet tracker
```
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.GameFunctions;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.AnotherMerchantTale;

public class Pari_Carpet_Tracker : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1317];
    public override Metadata? Metadata => new(2, "Poneglyph");

    private uint lockedCarpetObjectId = 0;
    private bool isScanning = false;
    private DateTime scanStartTime = DateTime.MinValue;

    private const uint ANCHOR_DATA_ID = 19059;
    private const uint CARPET_DATA_ID = 19060;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Carpet 1", """{"Name":"Carpet 1","type":3,"refY":-50.0,"offY":50.0,"radius":5.0,"color":3371826944,"fillIntensity":0.345,"thicc":0.0,"refActorDataID":19059,"refActorComparisonType":3,"includeRotation":false,"onlyVisible":true}""");
        Controller.RegisterElementFromCode("Carpet 2", """{"Name":"Carpet 2","type":3,"refX":-50.0,"offX":50.0,"radius":5.0,"color":3371826944,"fillIntensity":0.345,"thicc":0.0,"refActorDataID":19059,"refActorComparisonType":3,"includeRotation":false,"onlyVisible":true}""");
        
        OnReset();
    }

    public override void OnStartingCast(uint sourceId, uint castId)
    {
        if (castId == 45438 || castId == 45439)
        {
            OnReset();
            isScanning = true;
            scanStartTime = DateTime.Now;
        }
    }

    public override void OnUpdate()
    {
        Controller.Hide();

        if (isScanning && lockedCarpetObjectId == 0)
        {
            if ((DateTime.Now - scanStartTime).TotalSeconds > 15)
            {
                isScanning = false;
                return;
            }

            var anchor = Svc.Objects.FirstOrDefault(x => x.DataId == ANCHOR_DATA_ID);

            if (anchor != null)
            {
                var correctCarpet = Svc.Objects
                    .Where(x => x.DataId == CARPET_DATA_ID)
                    .OrderBy(x => Vector2.Distance(new Vector2(x.Position.X, x.Position.Z), 
                                                 new Vector2(anchor.Position.X, anchor.Position.Z)))
                    .FirstOrDefault();

                if (correctCarpet != null && Vector2.Distance(new Vector2(correctCarpet.Position.X, correctCarpet.Position.Z), 
                                                              new Vector2(anchor.Position.X, anchor.Position.Z)) < 4.0f)
                {
                    lockedCarpetObjectId = correctCarpet.ObjectId;
                    isScanning = false;
                }
            }
        }

        if (lockedCarpetObjectId != 0 && lockedCarpetObjectId.TryGetBattleNpc(out var carpetNpc))
        {
            var e1 = Controller.GetElementByName("Carpet 1");
            var e2 = Controller.GetElementByName("Carpet 2");

            if (e1 != null) 
            { 
                e1.Enabled = true; 
                e1.refActorComparisonType = 2;
                e1.refActorObjectID = carpetNpc.ObjectId; 
            }
            if (e2 != null) 
            { 
                e2.Enabled = true; 
                e2.refActorComparisonType = 2;
                e2.refActorObjectID = carpetNpc.ObjectId; 
            }
        }
    }

    public override void OnReset()
    {
        lockedCarpetObjectId = 0;
        isScanning = false;
        scanStartTime = DateTime.MinValue;
    }
}
```

### [Script] Pari rotation script
```
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.Schedulers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.AnotherMerchantTale;

public class Pari_Rotation_Script : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1317];
    public override Metadata? Metadata => new(3, "Poneglyph");

    private List<string> turningVFX = new();
    private List<string> distanceVFX = new();
    private List<string> sideSteps = new();
    private HashSet<ulong> processedActorIds = new();
    private List<TickScheduler> activeSchedulers = new();
    private bool isWaiting = false;
    private bool isTurningOnly = false;

    private const string R1 = "vfx/lockon/eff/m0973_turning_right_3sec_c0e1.avfx";
    private const string R2 = "vfx/lockon/eff/m0973_turning_r_right_3sec_c0e1.avfx";
    private const string L1 = "vfx/lockon/eff/m0973_turning_left_3sec_c0e1.avfx";
    private const string L2 = "vfx/lockon/eff/m0973_turning_r_left_3sec_c0e1.avfx";
    private const string FAR = "vfx/common/eff/m0973_stlpf_c0e1.avfx";
    private const string CLOSE = "vfx/common/eff/m0973_stlpn_c0e1.avfx";

    private readonly Vector3[] SidePositions = 
    {
        new(-755.0f, -54.0f, -815.0f),
        new(-755.0f, -54.0f, -825.0f),
        new(-765.0f, -54.0f, -815.0f),
        new(-765.0f, -54.0f, -825.0f)
    };

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("RotationOverlay", "{\"Name\":\"RotationOverlay\",\"type\":1,\"radius\":0.0,\"fillIntensity\":0.5,\"overlayBGColor\":3355443200,\"overlayTextColor\":4294967295,\"overlayVOffset\":2.48,\"thicc\":0.0,\"overlayText\":\" \",\"refActorType\":1}");
        Controller.RegisterElementFromCode("DistanceOverlay", "{\"Name\":\"DistanceOverlay\",\"type\":1,\"radius\":0.0,\"fillIntensity\":0.5,\"overlayBGColor\":3355443200,\"overlayTextColor\":4294967295,\"overlayVOffset\":3.0,\"thicc\":0.0,\"overlayText\":\" \",\"refActorType\":1}");
        Controller.RegisterElementFromCode("SideOverlay", "{\"Name\":\"SideOverlay\",\"type\":1,\"radius\":0.0,\"fillIntensity\":0.5,\"overlayBGColor\":3355443200,\"overlayTextColor\":4294967295,\"overlayVOffset\":3.0,\"thicc\":0.0,\"overlayText\":\" \",\"refActorType\":1}");
        OnReset();
    }

    public override void OnUpdate()
    {
        if (!isWaiting || !isTurningOnly) return;

        var actors = Svc.Objects.Where(o => o.DataId == 19058);

        foreach (var actor in actors)
        {
            if (processedActorIds.Contains(actor.GameObjectId)) continue;

            bool isAtPoint = SidePositions.Any(p => Vector3.Distance(actor.Position, p) < 1.0f);
            
            if (isAtPoint)
            {
                processedActorIds.Add(actor.GameObjectId);

                string safeSide = actor.Position.X < -760.0f ? "Right" : "Left";
                sideSteps.Add(safeSide);

                string displayText = string.Join(" ", sideSteps);
                bool isFinal = sideSteps.Count >= 4;
                SetOverlayText("SideOverlay", displayText, isFinal);
            }
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId is 45467 or 45468 or 47031 or 47032)
        {
            OnReset();
            isWaiting = true;
            isTurningOnly = (castId == 47031 || castId == 47032);
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (!isWaiting) return;

        if (vfxPath is R1 or R2 or L1 or L2)
        {
            turningVFX.Add((vfxPath == R1 || vfxPath == R2) ? "Right" : "Left");
            ProcessRotationStep();
        }

        if (!isTurningOnly && (vfxPath is FAR or CLOSE))
        {
            distanceVFX.Add(vfxPath == FAR ? "Far" : "Close");
            ProcessDistanceStep();
        }
    }

    private void ProcessRotationStep()
    {
        string text = "";
        int count = turningVFX.Count;
        bool isFinal = false;

        if (count == 2)
        {
            text = (turningVFX[0] != turningVFX[1]) ? "Stay" : "Move";
        }
        else if (count == 3)
        {
            isFinal = true;
            bool firstStepNoMove = turningVFX[0] != turningVFX[1];
            bool secondStepNoMove = turningVFX[1] != turningVFX[2];

            if (firstStepNoMove) text = "Stay - Move - Move";
            else if (secondStepNoMove) text = "Move - Stay - Move";
            else text = "Move - Move - Stay";
        }

        if (!string.IsNullOrEmpty(text)) SetOverlayText("RotationOverlay", text, isFinal);
    }

    private void ProcessDistanceStep()
    {
        string text = "";
        int count = distanceVFX.Count;
        bool isFinal = false;

        if (count == 1) text = distanceVFX[0];
        else if (count == 2) text = $"{distanceVFX[0]} {distanceVFX[1]}";
        else if (count == 3)
        {
            isFinal = true;
            string f = distanceVFX[0], s = distanceVFX[1], t = distanceVFX[2];

            if (f == s) text = (f == "Close") ? "Close Close Far Far" : "Far Far Close Close";
            else if (f == t) text = (f == "Close") ? "Close Far Close Far" : "Far Close Far Close";
            else text = (f == "Close") ? "Close Far Far Close" : "Far Close Close Far";
        }

        if (!string.IsNullOrEmpty(text)) SetOverlayText("DistanceOverlay", text, isFinal);
    }

    private void SetOverlayText(string name, string text, bool startTimer)
    {
        if (Controller.TryGetElementByName(name, out var element))
        {
            element.Enabled = true;
            element.overlayText = text;

            if (startTimer)
            {
                activeSchedulers.Add(new TickScheduler(() => 
                {
                    element.Enabled = false;
                    element.overlayText = " ";
                }, 25000));
            }
        }
    }

    public override void OnReset()
    {
        isWaiting = false;
        isTurningOnly = false;
        turningVFX.Clear();
        distanceVFX.Clear();
        sideSteps.Clear();
        processedActorIds.Clear();
        foreach (var s in activeSchedulers) s?.Dispose();
        activeSchedulers.Clear();

        if (Controller.TryGetElementByName("RotationOverlay", out var rot)) { rot.Enabled = false; rot.overlayText = " "; }
        if (Controller.TryGetElementByName("DistanceOverlay", out var dist)) { dist.Enabled = false; dist.overlayText = " "; }
        if (Controller.TryGetElementByName("SideOverlay", out var side)) { side.Enabled = false; side.overlayText = " "; }
    }
}
```